using System;
using System.Collections.Generic;
using System.Linq;
using FactionWars.Combat.Events;
using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Models;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.Factions.Interfaces;
using FactionWars.Territory.Interfaces;
using FactionWars.Territory.Models;
using FactionWars.UI.Interfaces;
using FactionWars.ScriptHookV.Logging;
using FactionWars.ScriptHookV.Models;
using FactionWars.ScriptHookV.Utils;

namespace FactionWars.ScriptHookV.Managers
{
    /// <summary>
    /// Manages enemy attackers that spawn when the player enters their own zone that is under attack.
    /// Attackers are spawned based on the active battle's attacker troops and engage the player on sight.
    /// </summary>
    public partial class BattleAttackerManager
    {
        private readonly IGameBridge _gameBridge;
        private readonly IZoneBattleManager _zoneBattleManager;
        private readonly IPedSpawningService _pedSpawningService;
        private readonly IPedDespawnService _pedDespawnService;
        private readonly IDefenderTierService _defenderTierService;
        private readonly IPedBlipService _pedBlipService;
        private readonly IZoneService _zoneService;
        private readonly IFactionService _factionService;
        private string _playerFactionId;

        private readonly Dictionary<DefenderTier, string> _modelsByTier;
        private readonly Dictionary<string, Dictionary<int, DefenderTier>> _spawnedPedTierByZone;
        private readonly Dictionary<int, int> _corpseDeathTimes;  // pedHandle -> game time when died
        private string? _currentBattleZoneId;

        private const float MinSpawnRadiusFraction = 0.3f;  // Min 30% of zone radius
        private const int CorpseDelayMs = 15000;  // 15 seconds before despawning corpses

        /// <summary>
        /// Maximum number of enemy attackers that can be spawned at once per zone.
        /// </summary>
        public const int MaxSpawnedAttackers = 12;

        /// <summary>
        /// Raised when a tracked enemy attacker ped is detected dead during Update().
        /// Args expose the killer ped handle so consumers can resolve player-attributed kills.
        /// </summary>
        public event EventHandler<AttackerKilledEventArgs>? AttackerKilled;

        /// <summary>
        /// Creates a new BattleAttackerManager instance.
        /// </summary>
        public BattleAttackerManager(BattleAttackerManagerDependencies dependencies, string playerFactionId)
        {
            if (dependencies == null) throw new ArgumentNullException(nameof(dependencies));
            _gameBridge = dependencies.GameBridge ?? throw new ArgumentNullException(nameof(dependencies.GameBridge));
            _zoneBattleManager = dependencies.ZoneBattleManager ?? throw new ArgumentNullException(nameof(dependencies.ZoneBattleManager));
            _pedSpawningService = dependencies.PedSpawningService ?? throw new ArgumentNullException(nameof(dependencies.PedSpawningService));
            _pedDespawnService = dependencies.PedDespawnService ?? throw new ArgumentNullException(nameof(dependencies.PedDespawnService));
            _defenderTierService = dependencies.DefenderTierService ?? throw new ArgumentNullException(nameof(dependencies.DefenderTierService));
            _pedBlipService = dependencies.PedBlipService ?? throw new ArgumentNullException(nameof(dependencies.PedBlipService));
            _zoneService = dependencies.ZoneService ?? throw new ArgumentNullException(nameof(dependencies.ZoneService));
            _factionService = dependencies.FactionService ?? throw new ArgumentNullException(nameof(dependencies.FactionService));
            _playerFactionId = playerFactionId ?? throw new ArgumentNullException(nameof(playerFactionId));

            // Enemy faction ped models (hostile attackers)
            _modelsByTier = new Dictionary<DefenderTier, string>
            {
                { DefenderTier.Basic, "g_m_y_famca_01" },
                { DefenderTier.Medium, "g_m_y_famdnf_01" },
                { DefenderTier.Heavy, "g_m_y_famfor_01" },
                { DefenderTier.Elite, "s_m_y_armymech_01" }  // Military mechanic for RPG specialist
            };

            _spawnedPedTierByZone = new Dictionary<string, Dictionary<int, DefenderTier>>();
            _corpseDeathTimes = new Dictionary<int, int>();
        }

        public BattleAttackerManager(params object?[] dependencies)
            : this(
                new BattleAttackerManagerDependencies
                {
                    GameBridge = (IGameBridge?)dependencies[0],
                    ZoneBattleManager = (IZoneBattleManager?)dependencies[1],
                    PedSpawningService = (IPedSpawningService?)dependencies[2],
                    PedDespawnService = (IPedDespawnService?)dependencies[3],
                    DefenderTierService = (IDefenderTierService?)dependencies[4],
                    PedBlipService = (IPedBlipService?)dependencies[5],
                    ZoneService = (IZoneService?)dependencies[6],
                    FactionService = (IFactionService?)dependencies[7]
                },
                (string)dependencies[8]!)
        {
        }

        /// <summary>
        /// Called when the player enters a zone. If the zone is the player's zone and there's
        /// an active battle where player is defender, spawns enemy attackers.
        /// </summary>
        /// <param name="zone">The zone that was entered.</param>
        public void OnPlayerZoneEntered(Zone zone)
        {
            if (zone == null) return;

            FileLogger.Combat($"BattleAttackerManager: OnPlayerZoneEntered called for zone {zone.Id}, playerFactionId={_playerFactionId}");

            // Get battle for this zone
            var battle = _zoneBattleManager.GetBattleForZone(zone.Id);
            if (battle == null)
            {
                FileLogger.Combat($"BattleAttackerManager: No active battle found for zone {zone.Id}");
                return;
            }

            FileLogger.Combat($"BattleAttackerManager: Found battle - Attacker={battle.AttackerFactionId}, Defender={battle.DefenderFactionId}, AttackerTroops={battle.TotalAttackerTroops}, DefenderTroops={battle.TotalDefenderTroops}");

            // Only spawn attackers if player is the defender (their zone is being attacked)
            if (battle.DefenderFactionId != _playerFactionId)
            {
                FileLogger.Combat($"BattleAttackerManager: Player ({_playerFactionId}) is not the defender ({battle.DefenderFactionId}), skipping");
                return;
            }

            FileLogger.Combat($"BattleAttackerManager: Player entered defended zone {zone.Id} under attack by {battle.AttackerFactionId}");

            _currentBattleZoneId = zone.Id;

            // Initialize tracking for this zone
            if (!_spawnedPedTierByZone.ContainsKey(zone.Id))
            {
                _spawnedPedTierByZone[zone.Id] = new Dictionary<int, DefenderTier>();
            }

            var totalSpawned = 0;
            var random = new Random();

            // Log per-tier values for debugging
            battle.AttackerTroops.TryGetValue(DefenderTier.Elite, out var eliteCount);
            battle.AttackerTroops.TryGetValue(DefenderTier.Heavy, out var heavyCount);
            battle.AttackerTroops.TryGetValue(DefenderTier.Medium, out var mediumCount);
            battle.AttackerTroops.TryGetValue(DefenderTier.Basic, out var basicCount);
            FileLogger.Combat($"BattleAttackerManager: Per-tier attacker counts (after restore) - Elite={eliteCount}, Heavy={heavyCount}, Medium={mediumCount}, Basic={basicCount}");

            // Spawn attackers based on battle.AttackerTroops
            foreach (DefenderTier tier in new[] { DefenderTier.Elite, DefenderTier.Heavy, DefenderTier.Medium, DefenderTier.Basic })
            {
                if (!battle.AttackerTroops.TryGetValue(tier, out var count) || count <= 0) continue;
                FileLogger.Combat($"BattleAttackerManager: Attempting to spawn {count} {tier} attackers (totalSpawned={totalSpawned}, max={MaxSpawnedAttackers})");
                totalSpawned = SpawnAttackersForTier(zone, battle, tier, count, totalSpawned, random);
            }

            FileLogger.Combat($"BattleAttackerManager: Spawned {totalSpawned} enemy attackers in {zone.Id}");
        }

        private int SpawnAttackersForTier(
            Zone zone,
            ZoneBattle battle,
            DefenderTier tier,
            int count,
            int totalSpawned,
            Random random)
        {
            var model = _modelsByTier.TryGetValue(tier, out var m) ? m : "g_m_y_famca_01";
            var tierConfig = _defenderTierService.GetTierConfig(tier);

            for (int i = 0; i < count && totalSpawned < MaxSpawnedAttackers; i++)
            {
                if (!_pedSpawningService.CanSpawn())
                {
                    FileLogger.Combat($"BattleAttackerManager: CanSpawn() returned false, breaking");
                    break;
                }

                var spawnPos = CalculateRandomSpawnPosition(zone.Center, zone.Radius, random);
                var pedHandle = _pedSpawningService.SpawnPed(model, spawnPos, battle.AttackerFactionId, zone.Id);
                if (!pedHandle.IsValid)
                {
                    FileLogger.Combat($"BattleAttackerManager: SpawnPed returned invalid handle");
                    continue;
                }

                ConfigureAttacker(pedHandle.Handle, tierConfig, zone.Center, zone.Radius);
                _pedBlipService.CreateBlipForPed(pedHandle.Handle, FactionBlipColor.ForFactionId(battle.AttackerFactionId));
                _spawnedPedTierByZone[zone.Id][pedHandle.Handle] = tier;
                totalSpawned++;
            }

            return totalSpawned;
        }

        /// <summary>
        /// Called when the player exits a zone. Despawns all attackers in that zone.
        /// Tracks despawned attackers so they can be restored if player re-enters.
        /// </summary>
        /// <param name="zone">The zone that was exited.</param>
        public void OnPlayerZoneExited(Zone zone)
        {
            if (zone == null) return;

            FileLogger.Combat($"BattleAttackerManager: Player exited zone {zone.Id}");

            if (_currentBattleZoneId == zone.Id)
                _currentBattleZoneId = null;

            if (!_spawnedPedTierByZone.TryGetValue(zone.Id, out var pedTiers)) return;

            // Despawn all attackers - the ZoneBattle already tracks the correct count
            // so when player re-enters, spawning will use the current battle state
            foreach (var pedHandle in pedTiers.Keys)
            {
                _pedBlipService.RemoveBlipForPed(pedHandle);
                _pedDespawnService.DespawnPed(pedHandle);
                _corpseDeathTimes.Remove(pedHandle);  // Clear any corpse tracking
            }
            _spawnedPedTierByZone.Remove(zone.Id);

            // Also delete any corpses that were from this zone
            var corpsesToRemove = new List<int>(_corpseDeathTimes.Keys);
            foreach (var pedHandle in corpsesToRemove)
            {
                _pedDespawnService.DeletePedEntity(pedHandle);
                _corpseDeathTimes.Remove(pedHandle);
            }
        }

        /// <summary>
        /// Despawns all attackers across all zones.
        /// </summary>
        public void DespawnAllAttackers()
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
            _currentBattleZoneId = null;

            // Also delete any remaining corpses
            foreach (var pedHandle in _corpseDeathTimes.Keys)
            {
                _pedDespawnService.DeletePedEntity(pedHandle);
            }
            _corpseDeathTimes.Clear();
        }

        /// <summary>
        /// Gets the number of spawned attackers for a specific zone.
        /// </summary>
        public int GetSpawnedAttackerCount(string zoneId)
        {
            return _spawnedPedTierByZone.TryGetValue(zoneId, out var pedTiers) ? pedTiers.Count : 0;
        }

        /// <summary>
        /// Gets the number of spawned attackers of a specific tier in a zone.
        /// </summary>
        public int GetSpawnedCountByTier(string zoneId, DefenderTier tier)
        {
            if (!_spawnedPedTierByZone.TryGetValue(zoneId, out var pedTiers))
                return 0;

            return pedTiers.Values.Count(t => t == tier);
        }

        /// <summary>
        /// Sets the player's faction ID for determining which zones the player is defending.
        /// Called when the player switches characters.
        /// </summary>
        /// <param name="factionId">The new player faction ID.</param>
        public void SetPlayerFaction(string factionId)
        {
            _playerFactionId = factionId ?? throw new ArgumentNullException(nameof(factionId));
        }

        /// <summary>
        /// Updates attacker state. Should be called each game tick.
        /// Checks for deaths and reports kills to battle manager.
        /// </summary>
        public void Update()
        {
            if (_currentBattleZoneId == null) return;

            var currentGameTime = _gameBridge.GetGameTime();
            var deadPeds = new List<(string zoneId, int pedHandle, DefenderTier tier)>();
            var streamedOutPeds = new List<(string zoneId, int pedHandle)>();

            // Check all spawned attackers for death
            foreach (var kvp in _spawnedPedTierByZone)
            {
                var zoneId = kvp.Key;
                var pedTiers = kvp.Value;

                foreach (var pedKvp in pedTiers)
                {
                    var pedHandle = pedKvp.Key;
                    var tier = pedKvp.Value;

                    // Skip if already tracked as corpse (death already processed)
                    if (_corpseDeathTimes.ContainsKey(pedHandle))
                        continue;

                    // Streamed-out (entity gone) is not a kill — don't report to battle.
                    if (!_gameBridge.DoesPedExist(pedHandle))
                    {
                        streamedOutPeds.Add((zoneId, pedHandle));
                    }
                    else if (!_gameBridge.IsPedAlive(pedHandle))
                    {
                        deadPeds.Add((zoneId, pedHandle, tier));
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

            // Process each dead attacker
            foreach (var (zoneId, pedHandle, tier) in deadPeds)
            {
                HandleAttackerDeath(zoneId, pedHandle, tier);
            }

            // Cleanup corpses that have exceeded the delay
            CleanupExpiredCorpses(currentGameTime);
        }

        /// <summary>
        /// Handles the death of an enemy attacker.
        /// </summary>
    }
}
