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

namespace FactionWars.ScriptHookV.Managers
{
    /// <summary>
    /// Manages enemy defenders that spawn when the player enters enemy territory.
    /// Defenders patrol the zone and engage the player and player's troops on sight.
    /// Supports death detection, replacement spawning from reserves, and ground-level spawning.
    /// </summary>
    public class EnemyDefenderManager
    {
        private readonly IGameBridge _gameBridge;
        private readonly IZoneDefenderAllocationService _allocationService;
        private readonly IPedSpawningService _pedSpawningService;
        private readonly IPedDespawnService _pedDespawnService;
        private readonly IDefenderTierService _defenderTierService;
        private readonly IPedBlipService _pedBlipService;
        private readonly IZoneService _zoneService;

        private readonly Dictionary<string, Dictionary<int, DefenderTier>> _spawnedPedTierByZone;
        private readonly Dictionary<int, int> _corpseDeathTimes; // pedHandle -> game time when died
        private string? _currentEnemyZoneId;

        private const float SpawnRadiusFraction = 0.8f;  // 80% of zone radius
        private const int CorpseDelayMs = 15000;  // 15 seconds before despawning corpses

        /// <summary>
        /// Maximum number of enemy defenders that can be spawned at once per zone.
        /// </summary>
        public const int MaxSpawnedDefenders = 12;

        /// <summary>
        /// Creates a new EnemyDefenderManager instance.
        /// </summary>
        public EnemyDefenderManager(
            IGameBridge gameBridge,
            IZoneDefenderAllocationService allocationService,
            IPedSpawningService pedSpawningService,
            IPedDespawnService pedDespawnService,
            IDefenderTierService defenderTierService,
            IPedBlipService pedBlipService,
            IZoneService zoneService)
        {
            _gameBridge = gameBridge ?? throw new ArgumentNullException(nameof(gameBridge));
            _allocationService = allocationService ?? throw new ArgumentNullException(nameof(allocationService));
            _pedSpawningService = pedSpawningService ?? throw new ArgumentNullException(nameof(pedSpawningService));
            _pedDespawnService = pedDespawnService ?? throw new ArgumentNullException(nameof(pedDespawnService));
            _defenderTierService = defenderTierService ?? throw new ArgumentNullException(nameof(defenderTierService));
            _pedBlipService = pedBlipService ?? throw new ArgumentNullException(nameof(pedBlipService));
            _zoneService = zoneService ?? throw new ArgumentNullException(nameof(zoneService));

            _spawnedPedTierByZone = new Dictionary<string, Dictionary<int, DefenderTier>>();
            _corpseDeathTimes = new Dictionary<int, int>();
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
                    _pedBlipService.CreateBlipForPed(pedHandle.Handle, BlipColor.Red);

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

            var newlyDeadPeds = new List<(string zoneId, int pedHandle, DefenderTier tier)>();
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

                    if (!_gameBridge.IsPedAlive(pedHandle))
                    {
                        newlyDeadPeds.Add((zoneId, pedHandle, tier));
                    }
                }
            }

            // Process each newly dead defender (track as corpse, decrement allocation, spawn replacement)
            foreach (var (zoneId, pedHandle, tier) in newlyDeadPeds)
            {
                HandleDefenderDeath(zoneId, pedHandle, tier, enemyFactionId);
            }

            // Clean up corpses that have exceeded the delay
            CleanupExpiredCorpses(currentGameTime);
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
        private bool TrySpawnReplacement(string zoneId, DefenderTier preferredTier, string enemyFactionId, ZoneDefenderAllocation? allocation)
        {
            if (allocation == null) return false;

            var currentSpawned = GetSpawnedDefenderCount(zoneId);
            if (currentSpawned >= MaxSpawnedDefenders) return false;

            var zone = _zoneService.GetZone(zoneId);
            if (zone == null) return false;

            // Try preferred tier first
            var allocatedCount = allocation.GetTroopCount(preferredTier);
            var spawnedOfTier = GetSpawnedCountByTier(zoneId, preferredTier);

            if (allocatedCount > spawnedOfTier)
            {
                SpawnSingleDefender(zoneId, preferredTier, enemyFactionId, zone);
                FileLogger.Combat($"EnemyDefenderManager: Spawned replacement {preferredTier} in {zoneId}");
                return true;
            }

            // Try other tiers (highest first)
            foreach (var tier in new[] { DefenderTier.Elite, DefenderTier.Heavy, DefenderTier.Medium, DefenderTier.Basic })
            {
                if (tier == preferredTier) continue;

                allocatedCount = allocation.GetTroopCount(tier);
                spawnedOfTier = GetSpawnedCountByTier(zoneId, tier);

                if (allocatedCount > spawnedOfTier)
                {
                    SpawnSingleDefender(zoneId, tier, enemyFactionId, zone);
                    FileLogger.Combat($"EnemyDefenderManager: Spawned replacement {tier} in {zoneId}");
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Spawns a single enemy defender.
        /// </summary>
        private void SpawnSingleDefender(string zoneId, DefenderTier tier, string enemyFactionId, Zone zone)
        {
            if (!_pedSpawningService.CanSpawn()) return;

            var model = FactionPedModels.GetModel(enemyFactionId, tier);
            var tierConfig = _defenderTierService.GetTierConfig(tier);
            var random = new Random();

            var spawnPos = CalculateRandomSpawnPosition(zone.Center, zone.Radius, random);
            var pedHandle = _pedSpawningService.SpawnPed(model, spawnPos, enemyFactionId, zoneId);

            if (!pedHandle.IsValid) return;

            ConfigureEnemyDefender(pedHandle.Handle, tierConfig, zone.Center, zone.Radius);
            _pedBlipService.CreateBlipForPed(pedHandle.Handle, BlipColor.Red);

            if (!_spawnedPedTierByZone.ContainsKey(zoneId))
            {
                _spawnedPedTierByZone[zoneId] = new Dictionary<int, DefenderTier>();
            }
            _spawnedPedTierByZone[zoneId][pedHandle.Handle] = tier;
        }

        /// <summary>
        /// Calculates a random spawn position around the zone center at ground level.
        /// Uses 80% of zone radius for spawn distance and navmesh-based safe coordinates
        /// to avoid spawning on rooftops.
        /// </summary>
        private Vector3 CalculateRandomSpawnPosition(Vector3 center, float zoneRadius, Random random)
        {
            var angle = random.NextDouble() * 2 * Math.PI;
            var distance = zoneRadius * SpawnRadiusFraction;
            var x = center.X + (float)(Math.Cos(angle) * distance);
            var y = center.Y + (float)(Math.Sin(angle) * distance);

            // Use navmesh-based safe coordinate to avoid rooftop spawns
            var targetPos = new Vector3(x, y, center.Z);
            return _gameBridge.GetSafeCoordForPed(targetPos);
        }

        /// <summary>
        /// Configures an enemy defender's combat attributes and behavior.
        /// </summary>
        private void ConfigureEnemyDefender(int pedHandle, DefenderTierConfig tierConfig, Vector3 zoneCenter, float wanderRadius)
        {
            // Give weapons
            _gameBridge.GivePedWeapon(pedHandle, "weapon_pistol");
            _gameBridge.GivePedWeapon(pedHandle, tierConfig.Weapon);
            _gameBridge.SetPedAccuracy(pedHandle, tierConfig.Accuracy);
            _gameBridge.SetPedArmor(pedHandle, tierConfig.Armor);
            _gameBridge.SetPedHealth(pedHandle, tierConfig.Health);
            _gameBridge.SetPedCombatAttributes(pedHandle, canUseCover: true, willFightArmedPeds: true);

            // Elite tier uses RPG - prevent AI from switching to pistol (AI prefers pistol to avoid self-damage)
            if (tierConfig.Tier == DefenderTier.Elite)
            {
                _gameBridge.SetPedCanSwitchWeapons(pedHandle, false);
            }

            // Set zone-wide perception so enemies can detect friendly defenders across the zone
            var perceptionRange = wanderRadius * 1.2f;
            _gameBridge.SetPedSeeingRange(pedHandle, perceptionRange);
            _gameBridge.SetPedHearingRange(pedHandle, perceptionRange);

            // Set as hostile wanderer - will engage player and followers on sight
            _gameBridge.SetPedAsHostileWanderer(pedHandle);

            // Task to seek and fight hated targets (player, followers, friendly defenders)
            _gameBridge.TaskCombatHatedTargetsAroundPed(pedHandle, wanderRadius);

            // CRITICAL: Also task to attack player immediately for immediate engagement
            _gameBridge.SetPedToAttackPlayer(pedHandle);
        }

        /// <summary>
        /// Gets the number of remaining reserves (allocated troops that haven't been spawned yet).
        /// </summary>
        /// <param name="zoneId">The zone ID.</param>
        /// <param name="enemyFactionId">The enemy faction ID.</param>
        /// <returns>The number of troops in reserve (allocation total - spawned count).</returns>
        public int GetRemainingReserves(string zoneId, string enemyFactionId)
        {
            var allocation = _allocationService.GetAllocation(enemyFactionId, zoneId);
            if (allocation == null) return 0;

            var spawnedCount = GetSpawnedDefenderCount(zoneId);
            var totalAllocated = allocation.TotalTroops;

            // Reserves = allocation total - currently spawned (clamped to 0)
            return Math.Max(0, totalAllocated - spawnedCount);
        }
    }
}
