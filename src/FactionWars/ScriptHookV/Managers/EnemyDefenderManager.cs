using System;
using System.Collections.Generic;
using System.Linq;
using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Models;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.Territory.Interfaces;
using FactionWars.Territory.Models;
using FactionWars.UI.Interfaces;
using FactionWars.ScriptHookV.Logging;
using FactionWars.ScriptHookV.Models;
using FactionWars.ScriptHookV.Services;
using FactionWars.ScriptHookV.Utils;

namespace FactionWars.ScriptHookV.Managers
{
    /// <summary>
    /// Manages enemy defenders that spawn when the player enters enemy territory.
    /// Defenders patrol the zone and engage the player and player's troops on sight.
    /// Supports death detection, replacement spawning from reserves, and ground-level spawning.
    /// </summary>
    public partial class EnemyDefenderManager
    {
        private readonly IGameBridge _gameBridge;
        private readonly IZoneDefenderAllocationService _allocationService;
        private readonly IPedSpawningService _pedSpawningService;
        private readonly IPedDespawnService _pedDespawnService;
        private readonly IDefenderTierService _defenderTierService;
        private readonly IPedBlipService _pedBlipService;
        private readonly IZoneService _zoneService;
        private readonly IZoneBattleManager? _zoneBattleManager;

        private readonly Dictionary<string, Dictionary<int, DefenderTier>> _spawnedPedTierByZone;
        private readonly Dictionary<int, int> _corpseDeathTimes; // pedHandle -> game time when died
        private string? _currentEnemyZoneId;
        private int _lastLeashCheckMs = 0;
        private readonly Random _leashRandom = new Random();

        private const float SpawnRadiusFraction = 0.8f;  // 80% of zone radius
        private const int CorpseDelayMs = 15000;  // 15 seconds before despawning corpses

        /// <summary>
        /// Maximum number of enemy defenders that can be spawned at once per zone.
        /// </summary>
        public const int MaxSpawnedDefenders = 12;

        /// <summary>
        /// Creates a new EnemyDefenderManager instance.
        /// </summary>
        public EnemyDefenderManager(EnemyDefenderManagerDependencies dependencies)
        {
            if (dependencies == null) throw new ArgumentNullException(nameof(dependencies));
            _gameBridge = dependencies.GameBridge ?? throw new ArgumentNullException(nameof(dependencies.GameBridge));
            _allocationService = dependencies.AllocationService ?? throw new ArgumentNullException(nameof(dependencies.AllocationService));
            _pedSpawningService = dependencies.PedSpawningService ?? throw new ArgumentNullException(nameof(dependencies.PedSpawningService));
            _pedDespawnService = dependencies.PedDespawnService ?? throw new ArgumentNullException(nameof(dependencies.PedDespawnService));
            _defenderTierService = dependencies.DefenderTierService ?? throw new ArgumentNullException(nameof(dependencies.DefenderTierService));
            _pedBlipService = dependencies.PedBlipService ?? throw new ArgumentNullException(nameof(dependencies.PedBlipService));
            _zoneService = dependencies.ZoneService ?? throw new ArgumentNullException(nameof(dependencies.ZoneService));
            _zoneBattleManager = dependencies.ZoneBattleManager;

            _spawnedPedTierByZone = new Dictionary<string, Dictionary<int, DefenderTier>>();
            _corpseDeathTimes = new Dictionary<int, int>();
        }

        public EnemyDefenderManager(params object?[] dependencies)
            : this(new EnemyDefenderManagerDependencies
            {
                GameBridge = (IGameBridge?)dependencies[0],
                AllocationService = (IZoneDefenderAllocationService?)dependencies[1],
                PedSpawningService = (IPedSpawningService?)dependencies[2],
                PedDespawnService = (IPedDespawnService?)dependencies[3],
                DefenderTierService = (IDefenderTierService?)dependencies[4],
                PedBlipService = (IPedBlipService?)dependencies[5],
                ZoneService = (IZoneService?)dependencies[6],
                ZoneBattleManager = dependencies.Length > 7 ? (IZoneBattleManager?)dependencies[7] : null
            })
        {
        }

        /// <summary>
        /// Called when the player enters an enemy zone. Spawns enemy defenders
        /// from the zone's allocation. Respects MaxSpawnedDefenders limit.
        /// </summary>
        /// <param name="zone">The enemy zone that was entered.</param>
        /// <param name="enemyFactionId">The faction that owns the zone.</param>
        public void OnEnemyZoneEntered(Zone zone, string enemyFactionId)
        {
            if (zone == null || string.IsNullOrEmpty(enemyFactionId)) return;

            FileLogger.Combat($"EnemyDefenderManager: Player entered enemy zone {zone.Id} owned by {enemyFactionId}");

            _currentEnemyZoneId = zone.Id;

            var allocation = _allocationService.GetAllocation(enemyFactionId, zone.Id);
            if (allocation == null)
            {
                FileLogger.Combat($"EnemyDefenderManager: No allocation found for {enemyFactionId} in {zone.Id}");
                return;
            }

            // Initialize tracking for this zone
            if (!_spawnedPedTierByZone.ContainsKey(zone.Id))
            {
                _spawnedPedTierByZone[zone.Id] = new Dictionary<int, DefenderTier>();
            }

            var totalSpawned = 0;
            var random = new Random();

            foreach (DefenderTier tier in new[] { DefenderTier.Elite, DefenderTier.Heavy, DefenderTier.Medium, DefenderTier.Basic })
            {
                var count = allocation.GetTroopCount(tier);
                if (count <= 0) continue;

                var model = FactionPedModels.GetModel(enemyFactionId, tier);
                var tierConfig = _defenderTierService.GetTierConfig(tier);

                for (int i = 0; i < count && totalSpawned < MaxSpawnedDefenders; i++)
                {
                    if (!_pedSpawningService.CanSpawn()) break;

                    var spawnPos = CalculateRandomSpawnPosition(zone.Center, zone.Radius, random);
                    var pedHandle = _pedSpawningService.SpawnPed(model, spawnPos, enemyFactionId, zone.Id);
                    if (!pedHandle.IsValid) continue;

                    // Configure as hostile wanderer
                    ConfigureEnemyDefender(pedHandle.Handle, tierConfig, zone.Center, zone.Radius);
                    _pedBlipService.CreateBlipForPed(pedHandle.Handle, FactionBlipColor.ForFactionId(enemyFactionId));

                    // Track ped with its tier
                    _spawnedPedTierByZone[zone.Id][pedHandle.Handle] = tier;
                    totalSpawned++;
                }
            }

            FileLogger.Combat($"EnemyDefenderManager: Spawned {totalSpawned} enemy defenders in {zone.Id}");
        }

        /// <summary>
        /// Called when the player exits an enemy zone. Despawns all enemy defenders and corpses.
        /// </summary>
        /// <param name="zone">The zone that was exited.</param>
        public void OnEnemyZoneExited(Zone zone)
        {
            if (zone == null) return;

            FileLogger.Combat($"EnemyDefenderManager: Player exited enemy zone {zone.Id}");

            if (_currentEnemyZoneId == zone.Id)
                _currentEnemyZoneId = null;

            if (_spawnedPedTierByZone.TryGetValue(zone.Id, out var pedTiers))
            {
                foreach (var pedHandle in pedTiers.Keys)
                {
                    _pedBlipService.RemoveBlipForPed(pedHandle);
                    _pedDespawnService.DespawnPed(pedHandle);
                    _corpseDeathTimes.Remove(pedHandle); // Also remove from corpse tracking
                }
                _spawnedPedTierByZone.Remove(zone.Id);
            }

            // Also clean up any corpses from this zone
            var corpsesToRemove = _corpseDeathTimes.Keys.ToList();
            foreach (var pedHandle in corpsesToRemove)
            {
                _pedDespawnService.DeletePedEntity(pedHandle);
                _corpseDeathTimes.Remove(pedHandle);
            }
        }

        /// <summary>
        /// Despawns all enemy defenders and corpses across all zones.
        /// </summary>
        public void DespawnAllDefenders()
        {
            foreach (var zonePedTiers in _spawnedPedTierByZone.Values)
            {
                foreach (var pedHandle in zonePedTiers.Keys)
                {
                    _pedBlipService.RemoveBlipForPed(pedHandle);
                    _pedDespawnService.DespawnPed(pedHandle);
                }
            }
            _spawnedPedTierByZone.Clear();

            // Also clean up all corpses
            foreach (var pedHandle in _corpseDeathTimes.Keys)
            {
                _pedDespawnService.DeletePedEntity(pedHandle);
            }
            _corpseDeathTimes.Clear();

            _currentEnemyZoneId = null;
        }

        /// <summary>
        /// Gets the number of spawned enemy defenders for a specific zone.
        /// </summary>
        public int GetSpawnedDefenderCount(string zoneId)
        {
            return _spawnedPedTierByZone.TryGetValue(zoneId, out var pedTiers) ? pedTiers.Count : 0;
        }

        /// <summary>
        /// Gets the number of spawned defenders of a specific tier in a zone.
        /// </summary>
        public int GetSpawnedCountByTier(string zoneId, DefenderTier tier)
        {
            if (!_spawnedPedTierByZone.TryGetValue(zoneId, out var pedTiers))
                return 0;

            return pedTiers.Values.Count(t => t == tier);
        }

        /// <summary>
        /// Updates enemy defender state. Should be called each game tick.
        /// Checks for deaths, handles cleanup, and spawns replacements.
        /// </summary>
        /// <param name="enemyFactionId">The enemy faction ID for the current zone.</param>
        public void Update(string? enemyFactionId)
        {
            if (_currentEnemyZoneId == null || string.IsNullOrEmpty(enemyFactionId)) return;
            var defenderFactionId = enemyFactionId!;

            var newlyDeadPeds = new List<(string zoneId, int pedHandle, DefenderTier tier)>();
            var streamedOutPeds = new List<(string zoneId, int pedHandle)>();
            var currentGameTime = _gameBridge.GetGameTime();

            // Check all spawned enemy defenders for death
            foreach (var kvp in _spawnedPedTierByZone)
            {
                var zoneId = kvp.Key;
                var pedTiers = kvp.Value;

                foreach (var pedKvp in pedTiers)
                {
                    var pedHandle = pedKvp.Key;
                    var tier = pedKvp.Value;

                    // Skip if already tracked as corpse
                    if (_corpseDeathTimes.ContainsKey(pedHandle))
                        continue;

                    // Streamed-out (entity gone) is not a kill — don't shed allocation.
                    if (!_gameBridge.DoesPedExist(pedHandle))
                    {
                        streamedOutPeds.Add((zoneId, pedHandle));
                    }
                    else if (!_gameBridge.IsPedAlive(pedHandle))
                    {
                        newlyDeadPeds.Add((zoneId, pedHandle, tier));
                    }
                }
            }

            // Quietly untrack peds the engine culled.
            foreach (var (zoneId, pedHandle) in streamedOutPeds)
            {
                if (_spawnedPedTierByZone.TryGetValue(zoneId, out var pedTiers))
                {
                    pedTiers.Remove(pedHandle);
                }
                _pedBlipService.RemoveBlipForPed(pedHandle);
                _pedDespawnService.UntrackPed(pedHandle);
            }

            // Process each newly dead defender (track as corpse, decrement allocation, spawn replacement)
            foreach (var (zoneId, pedHandle, tier) in newlyDeadPeds)
            {
                HandleDefenderDeath(zoneId, pedHandle, tier, defenderFactionId);
            }

            // Clean up corpses that have exceeded the delay
            CleanupExpiredCorpses(currentGameTime);

            EnforceZoneLeash(currentGameTime);
        }

        /// <summary>
        /// Every <see cref="ZoneLeashEnforcer.LeashCheckIntervalMs"/>, scan all
        /// tracked enemy defenders. Any whose distance from their zone center
        /// exceeds the hysteresis threshold gets its tasks cleared and a
        /// TaskGoToCoord back to a random point inside the inner half of the
        /// zone.
        /// </summary>
        private void EnforceZoneLeash(int currentGameTime)
        {
            if (currentGameTime - _lastLeashCheckMs < ZoneLeashEnforcer.LeashCheckIntervalMs)
                return;
            _lastLeashCheckMs = currentGameTime;

            foreach (var kvp in _spawnedPedTierByZone)
            {
                var zoneId = kvp.Key;
                var pedTiers = kvp.Value;

                var zone = _zoneService.GetZone(zoneId);
                if (zone == null)
                    continue;

                foreach (var pedHandle in pedTiers.Keys)
                {
                    if (_corpseDeathTimes.ContainsKey(pedHandle))
                        continue;
                    if (!_gameBridge.DoesPedExist(pedHandle) || !_gameBridge.IsPedAlive(pedHandle))
                        continue;

                    var pedPos = _gameBridge.GetPedPosition(pedHandle);
                    if (!ZoneLeashEnforcer.ShouldLeash(pedPos, zone.Center, zone.Radius))
                        continue;

                    var returnPoint = ZoneLeashEnforcer.PickReturnPoint(zone.Center, zone.Radius, _leashRandom);
                    _gameBridge.ClearPedTasks(pedHandle);
                    _gameBridge.TaskGoToCoord(pedHandle, returnPoint);
                    FileLogger.AI($"EnemyDefenderManager: leashed ped {pedHandle} in zone {zoneId} from ({pedPos.X:F1},{pedPos.Y:F1}) back to ({returnPoint.X:F1},{returnPoint.Y:F1})");
                }
            }
        }

        /// <summary>
        /// Handles the death of an enemy defender.
        /// Tracks corpse for delayed cleanup, removes blip, decrements allocation, spawns replacement.
        /// </summary>
        private void HandleDefenderDeath(string zoneId, int pedHandle, DefenderTier tier, string enemyFactionId)
        {
            FileLogger.Combat($"EnemyDefenderManager: Defender died in {zoneId}, tier={tier}");

            // Track death time for corpse cleanup (don't despawn yet - leave corpse visible)
            _corpseDeathTimes[pedHandle] = _gameBridge.GetGameTime();

            // Remove from active tracking (no longer counts toward spawned defenders)
            if (_spawnedPedTierByZone.TryGetValue(zoneId, out var pedTiers))
            {
                pedTiers.Remove(pedHandle);
            }

            // Remove blip immediately (dead peds shouldn't show on radar)
            _pedBlipService.RemoveBlipForPed(pedHandle);

            // IMPORTANT: Untrack from ped pool to free spawn slot (but keep corpse visible)
            _pedDespawnService.UntrackPed(pedHandle);

            // Get allocation and ALWAYS decrement when a defender dies
            var allocation = _allocationService.GetAllocation(enemyFactionId, zoneId);
            if (allocation != null)
            {
                allocation.RemoveTroops(tier, 1);
                FileLogger.Combat($"EnemyDefenderManager: Decremented {tier} allocation in {zoneId}, remaining: {allocation.TotalTroops}");
            }

            // Report kill to ZoneBattleManager so the active battle's troop count stays in sync
            // and victory conditions are checked correctly.
            _zoneBattleManager?.ReportTroopKilled(zoneId, enemyFactionId, tier);

            // Try to spawn replacement from remaining reserves
            TrySpawnReplacement(zoneId, tier, enemyFactionId, allocation);
        }

        /// <summary>
        /// Cleans up corpses that have exceeded the corpse delay time.
        /// </summary>
        private void CleanupExpiredCorpses(int currentGameTime)
        {
            var expiredCorpses = new List<int>();

            foreach (var kvp in _corpseDeathTimes)
            {
                var pedHandle = kvp.Key;
                var deathTime = kvp.Value;

                if (currentGameTime - deathTime >= CorpseDelayMs)
                {
                    expiredCorpses.Add(pedHandle);
                }
            }

            foreach (var pedHandle in expiredCorpses)
            {
                FileLogger.Combat($"EnemyDefenderManager: Cleaning up corpse {pedHandle} after {CorpseDelayMs}ms delay");
                // Delete the visual entity (already untracked from pool on death)
                _pedDespawnService.DeletePedEntity(pedHandle);
                _corpseDeathTimes.Remove(pedHandle);
            }
        }

        /// <summary>
        /// Tries to spawn a replacement defender from reserves.
        /// </summary>
    }
}
