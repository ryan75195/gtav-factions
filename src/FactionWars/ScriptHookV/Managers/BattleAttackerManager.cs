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
using FactionWars.Combat.Services;
using FactionWars.ScriptHookV.Combat;
using FactionWars.ScriptHookV.Combat.Interfaces;
using FactionWars.ScriptHookV.Logging;
using FactionWars.ScriptHookV.Models;
using FactionWars.Core.Utils;
using FactionWars.ScriptHookV.Managers.Interfaces;

namespace FactionWars.ScriptHookV.Managers
{
    /// <summary>
    /// Manages enemy attackers that spawn when the player enters their own zone that is under attack.
    /// Attackers are spawned based on the active battle's attacker troops and engage the player on sight.
    /// </summary>
    public partial class BattleAttackerManager : IHostilePedHandleSource
    {
        private readonly IGameBridge _gameBridge;
        private readonly IZoneBattleManager _zoneBattleManager;
        private readonly IPedSpawningService _pedSpawningService;
        private readonly IPedDespawnService _pedDespawnService;
        private readonly IDefenderRoleService _defenderRoleService;
        private readonly IPedBlipService _pedBlipService;
        private readonly IZoneService _zoneService;
        private readonly IFactionService _factionService;
        private readonly IZoneCombatantSpawner _spawner;
        private readonly ICombatantStatsProvider _statsProvider;
        private string _playerFactionId;

        private readonly Dictionary<DefenderRole, string> _modelsByTier;
        private readonly Dictionary<string, Dictionary<int, DefenderRole>> _spawnedPedTierByZone;
        private readonly Dictionary<string, Dictionary<int, string>> _spawnedPedFactionByZone;
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
            _defenderRoleService = dependencies.DefenderRoleService ?? throw new ArgumentNullException(nameof(dependencies.DefenderRoleService));
            _pedBlipService = dependencies.PedBlipService ?? throw new ArgumentNullException(nameof(dependencies.PedBlipService));
            _zoneService = dependencies.ZoneService ?? throw new ArgumentNullException(nameof(dependencies.ZoneService));
            _factionService = dependencies.FactionService ?? throw new ArgumentNullException(nameof(dependencies.FactionService));
            _spawner = dependencies.Spawner
                ?? new ZoneCombatantSpawner(new AllegianceResolver(), _pedSpawningService, _pedBlipService, _gameBridge);
            _playerFactionId = playerFactionId ?? throw new ArgumentNullException(nameof(playerFactionId));
            _statsProvider = dependencies.StatsProvider ?? throw new ArgumentNullException(nameof(dependencies.StatsProvider));

            // Enemy faction ped models (hostile attackers)
            _modelsByTier = new Dictionary<DefenderRole, string>
            {
                { DefenderRole.Grunt, "g_m_y_famca_01" },
                { DefenderRole.Gunner, "g_m_y_famdnf_01" },
                { DefenderRole.Rifleman, "g_m_y_famfor_01" },
                { DefenderRole.Rocketeer, "s_m_y_armymech_01" }  // Military mechanic for RPG specialist
            };

            _spawnedPedTierByZone = new Dictionary<string, Dictionary<int, DefenderRole>>();
            _spawnedPedFactionByZone = new Dictionary<string, Dictionary<int, string>>();
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
                    DefenderRoleService = (IDefenderRoleService?)dependencies[4],
                    PedBlipService = (IPedBlipService?)dependencies[5],
                    ZoneService = (IZoneService?)dependencies[6],
                    FactionService = (IFactionService?)dependencies[7],
                    StatsProvider = dependencies.Length > 9 ? (ICombatantStatsProvider?)dependencies[9] : null
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

            var attackerToSpawn = GetHostileAttackerForPlayer(battle);
            if (attackerToSpawn == null)
            {
                FileLogger.Combat($"BattleAttackerManager: No non-player attacker to spawn for player faction {_playerFactionId}, skipping");
                return;
            }

            FileLogger.Combat($"BattleAttackerManager: Player entered zone {zone.Id} with hostile attacker {attackerToSpawn.FactionId}");

            _currentBattleZoneId = zone.Id;
            // Faction-vs-faction and faction-vs-player relationships are wired once at init by
            // RelationshipMatrixInitializer; no per-spawn relationship mutation here.

            EnsureSpawnTracking(zone.Id);

            var totalSpawned = 0;
            var random = new Random();

            // Log per-tier values for debugging
            var attackerTroops = attackerToSpawn.Troops;
            attackerTroops.TryGetValue(DefenderRole.Rocketeer, out var eliteCount);
            attackerTroops.TryGetValue(DefenderRole.Rifleman, out var heavyCount);
            attackerTroops.TryGetValue(DefenderRole.Gunner, out var mediumCount);
            attackerTroops.TryGetValue(DefenderRole.Grunt, out var basicCount);
            FileLogger.Combat($"BattleAttackerManager: Per-tier attacker counts (after restore) - Elite={eliteCount}, Heavy={heavyCount}, Medium={mediumCount}, Basic={basicCount}");

            foreach (DefenderRole tier in new[] { DefenderRole.Rocketeer, DefenderRole.Rifleman, DefenderRole.Gunner, DefenderRole.Grunt })
            {
                if (!attackerTroops.TryGetValue(tier, out var count) || count <= 0) continue;
                FileLogger.Combat($"BattleAttackerManager: Attempting to spawn {count} {tier} attackers (totalSpawned={totalSpawned}, max={MaxSpawnedAttackers})");
                totalSpawned = SpawnAttackersForTier(zone, attackerToSpawn.FactionId, tier, count, totalSpawned, random);
            }

            FileLogger.Combat($"BattleAttackerManager: Spawned {totalSpawned} enemy attackers in {zone.Id}");
        }

        private BattleParticipant? GetHostileAttackerForPlayer(ZoneBattle battle)
        {
            if (battle.DefenderFactionId == _playerFactionId)
                return battle.Attackers.FirstOrDefault(p => !p.IsPlayer);

            if (battle.Attackers.Any(p => p.IsPlayer && p.FactionId == _playerFactionId))
                return battle.Attackers.FirstOrDefault(p => !p.IsPlayer && p.FactionId != _playerFactionId);

            return null;
        }

        private int SpawnAttackersForTier(
            Zone zone,
            string attackerFactionId,
            DefenderRole tier,
            int count,
            int totalSpawned,
            Random random)
        {
            var model = _modelsByTier.TryGetValue(tier, out var m) ? m : "g_m_y_famca_01";
            var roleConfig = _defenderRoleService.GetRoleConfig(tier);

            for (int i = 0; i < count && totalSpawned < MaxSpawnedAttackers; i++)
            {
                if (!_pedSpawningService.CanSpawn())
                {
                    FileLogger.Combat($"BattleAttackerManager: CanSpawn() returned false, breaking");
                    break;
                }

                var spawnPos = CalculateRandomSpawnPosition(zone.Center, zone.Radius, random);
                // Single spawn site owns relationship group, blip colour, and hostile stance.
                var pedHandle = _spawner.Spawn(attackerFactionId, _playerFactionId, model, spawnPos, zone.Id);
                if (!pedHandle.IsValid)
                {
                    FileLogger.Combat($"BattleAttackerManager: SpawnPed returned invalid handle");
                    continue;
                }

                ConfigureAttacker(pedHandle.Handle, roleConfig, attackerFactionId, zone.Center, zone.Radius);
                _spawnedPedTierByZone[zone.Id][pedHandle.Handle] = tier;
                _spawnedPedFactionByZone[zone.Id][pedHandle.Handle] = attackerFactionId;
                totalSpawned++;
            }

            return totalSpawned;
        }

        private void EnsureSpawnTracking(string zoneId)
        {
            if (!_spawnedPedTierByZone.ContainsKey(zoneId))
                _spawnedPedTierByZone[zoneId] = new Dictionary<int, DefenderRole>();
            if (!_spawnedPedFactionByZone.ContainsKey(zoneId))
                _spawnedPedFactionByZone[zoneId] = new Dictionary<int, string>();
        }

        /// <summary>
        /// Called when the player exits a zone. Despawns all attackers in that zone.
        /// Tracks despawned attackers so they can be restored if player re-enters.
        /// </summary>
        /// <param name="zone">The zone that was exited.</param>

        /// <inheritdoc />
        public IReadOnlyList<int> GetHostilePedHandles()
        {
            var handles = new List<int>();
            foreach (var pedsInZone in _spawnedPedTierByZone.Values)
            {
                handles.AddRange(pedsInZone.Keys);
            }
            return handles;
        }
    }
}
