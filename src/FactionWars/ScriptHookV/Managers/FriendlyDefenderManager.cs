using System;
using System.Collections.Generic;
using System.Linq;
using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Models;
using FactionWars.Combat.Services;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.Core.Utils;
using FactionWars.ScriptHookV.Combat;
using FactionWars.ScriptHookV.Combat.Interfaces;
using FactionWars.ScriptHookV.Logging;
using FactionWars.ScriptHookV.Models;
using FactionWars.ScriptHookV.Services;
using FactionWars.Territory.Interfaces;
using FactionWars.Territory.Models;
using FactionWars.UI.Interfaces;

namespace FactionWars.ScriptHookV.Managers
{
    /// <summary>
    /// Manages friendly defenders that spawn when the player enters their own territory.
    /// Defenders patrol the zone independently (NOT as followers) and despawn when player exits.
    /// Supports death detection, replacement spawning from reserve, and territory loss.
    /// </summary>
    public partial class FriendlyDefenderManager : IFriendlyDefenderQuery
    {
        private readonly IGameBridge _gameBridge;
        private readonly IZoneDefenderAllocationService _allocationService;
        private readonly IPedSpawningService _pedSpawningService;
        private readonly IPedDespawnService _pedDespawnService;
        private readonly IDefenderRoleService _defenderRoleService;
        private readonly IPedBlipService _pedBlipService;
        private readonly IZoneService _zoneService;
        private readonly IZoneCombatantSpawner _spawner;
        private readonly ISniperDeploymentService _sniperDeployment;
        private readonly ICombatantStatsProvider _statsProvider;
        private string _playerFactionId;

        private readonly Dictionary<string, Dictionary<int, DefenderRole>> _spawnedPedTierByZone; // zoneId -> (pedHandle -> tier)
        private readonly Dictionary<int, int> _corpseDeathTimes; // pedHandle -> game time when died
        private readonly HashSet<string> _zonesInBattle; // Track zones with active battles
        private int _lastLeashCheckMs = 0;
        private readonly Random _leashRandom = new Random();
        private string? _currentZoneId; // Track the zone the player is currently in
        private readonly Random _random = new Random();

        /// <summary>
        /// Spawn radius as a fraction of zone radius (80%).
        /// </summary>
        private const float SpawnRadiusFraction = 0.8f;
        /// <summary>
        /// Time in milliseconds before corpses are despawned (15 seconds).
        /// </summary>
        private const int CorpseDelayMs = 15000;
        /// <summary>
        /// Maximum number of defenders that can be spawned at once per zone.
        /// Additional allocated troops are held in reserve.
        /// </summary>
        public const int MaxSpawnedDefenders = 12;

        /// <summary>
        /// Raised when a defender dies.
        /// </summary>
        public event EventHandler<DefenderDiedEventArgs>? DefenderDied;

        /// <summary>
        /// Raised when all defenders in a zone die and the territory is lost.
        /// </summary>
        public event EventHandler<TerritoryLostEventArgs>? TerritoryLost;

        /// <summary>
        /// Creates a new FriendlyDefenderManager instance.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown if any required dependency is null.</exception>
        public FriendlyDefenderManager(FriendlyDefenderManagerDependencies dependencies, string playerFactionId)
        {
            if (dependencies == null) throw new ArgumentNullException(nameof(dependencies));
            _gameBridge = dependencies.GameBridge ?? throw new ArgumentNullException(nameof(dependencies.GameBridge));
            _allocationService = dependencies.AllocationService ?? throw new ArgumentNullException(nameof(dependencies.AllocationService));
            _pedSpawningService = dependencies.PedSpawningService ?? throw new ArgumentNullException(nameof(dependencies.PedSpawningService));
            _pedDespawnService = dependencies.PedDespawnService ?? throw new ArgumentNullException(nameof(dependencies.PedDespawnService));
            _defenderRoleService = dependencies.DefenderRoleService ?? throw new ArgumentNullException(nameof(dependencies.DefenderRoleService));
            _pedBlipService = dependencies.PedBlipService ?? throw new ArgumentNullException(nameof(dependencies.PedBlipService));
            _zoneService = dependencies.ZoneService ?? throw new ArgumentNullException(nameof(dependencies.ZoneService));
            _spawner = dependencies.Spawner
                ?? new ZoneCombatantSpawner(new AllegianceResolver(), _pedSpawningService, _pedBlipService, _gameBridge);
            _sniperDeployment = dependencies.SniperDeployment ?? new SniperDeploymentService(new PerchResolver(), _gameBridge);
            _playerFactionId = playerFactionId ?? throw new ArgumentNullException(nameof(playerFactionId));
            _statsProvider = dependencies.StatsProvider ?? throw new ArgumentNullException(nameof(dependencies.StatsProvider));

            _spawnedPedTierByZone = new Dictionary<string, Dictionary<int, DefenderRole>>();
            _corpseDeathTimes = new Dictionary<int, int>();
            _zonesInBattle = new HashSet<string>();
        }

        public FriendlyDefenderManager(params object?[] dependencies)
            : this(
                new FriendlyDefenderManagerDependencies
                {
                    GameBridge = (IGameBridge?)dependencies[0],
                    AllocationService = (IZoneDefenderAllocationService?)dependencies[1],
                    PedSpawningService = (IPedSpawningService?)dependencies[2],
                    PedDespawnService = (IPedDespawnService?)dependencies[3],
                    DefenderRoleService = (IDefenderRoleService?)dependencies[4],
                    PedBlipService = (IPedBlipService?)dependencies[5],
                    ZoneService = (IZoneService?)dependencies[6],
                    StatsProvider = dependencies.Length > 8 ? (ICombatantStatsProvider?)dependencies[8] : null
                },
                (string)dependencies[7]!)
        {
        }

        /// <summary>
        /// Sets the player's current faction and despawns all existing defenders.
        /// </summary>
        /// <param name="factionId">The new faction ID.</param>
        /// <exception cref="ArgumentNullException">Thrown if factionId is null or empty.</exception>
        public void SetPlayerFaction(string factionId)
        {
            if (string.IsNullOrEmpty(factionId))
                throw new ArgumentNullException(nameof(factionId));

            FileLogger.Spawn($"FriendlyDefenderManager: Faction changed {_playerFactionId ?? "NULL"} -> {factionId}");
            DespawnAllDefenders();
            _playerFactionId = factionId;
        }

        /// <summary>
        /// Called when the player enters a zone. Spawns friendly defenders if the zone
        /// belongs to the player's faction. Respects MaxSpawnedDefenders limit.
        /// </summary>
        /// <param name="zone">The zone that was entered.</param>
        public void OnZoneEntered(Zone zone)
        {
            if (zone == null) return;

            // Track current zone for immediate spawning when allocating
            _currentZoneId = zone.Id;

            FileLogger.Spawn($"FriendlyDefenderManager.OnZoneEntered: Zone='{zone.Name}', Owner={zone.OwnerFactionId ?? "NONE"}, PlayerFaction={_playerFactionId}");

            if (zone.OwnerFactionId != _playerFactionId)
            {
                FileLogger.Spawn($"FriendlyDefenderManager: Skipping spawn - zone owner ({zone.OwnerFactionId}) != player faction ({_playerFactionId})");
                return;
            }

            var allocation = _allocationService.GetAllocation(_playerFactionId, zone.Id);
            if (allocation == null)
            {
                FileLogger.Spawn($"FriendlyDefenderManager: No allocation found for {_playerFactionId} in zone {zone.Id}");
                return;
            }

            FileLogger.Spawn($"FriendlyDefenderManager: Spawning friendly defenders for '{zone.Name}'");

            // Initialize tracking for this zone
            if (!_spawnedPedTierByZone.ContainsKey(zone.Id))
            {
                _spawnedPedTierByZone[zone.Id] = new Dictionary<int, DefenderRole>();
            }

            var totalSpawned = 0;

            foreach (DefenderRole tier in Enum.GetValues(typeof(DefenderRole)))
            {
                var count = allocation.GetTroopCount(tier);
                if (count <= 0) continue;

                var model = FactionPedModels.GetModel(_playerFactionId, tier);
                var roleConfig = _defenderRoleService.GetRoleConfig(tier);

                for (int i = 0; i < count && totalSpawned < MaxSpawnedDefenders; i++)
                {
                    if (!_pedSpawningService.CanSpawn()) break;

                    var spawnPos = CalculateRandomSpawnPosition(zone.Center, zone.Radius);
                    // Single spawn site: friendly defenders live in the player's faction group (so
                    // the matrix makes them player-companions, rival-haters), faction-coloured.
                    var pedHandle = _spawner.Spawn(_playerFactionId, _playerFactionId, model, spawnPos, zone.Id);
                    if (!pedHandle.IsValid) continue;

                    ConfigureDefenderCombat(pedHandle.Handle, roleConfig, zone.Center);
                    // Bounded native wander — keeps idle peds inside the zone
                    // without per-tick checks. The leash sweep handles the
                    // combat-chase case separately.
                    _gameBridge.TaskPedWanderInBoundedArea(pedHandle.Handle, zone.Center, zone.Radius);

                    // Track ped with its tier
                    _spawnedPedTierByZone[zone.Id][pedHandle.Handle] = tier;
                    totalSpawned++;
                }
            }
        }

        /// <summary>
        /// Called when the player exits a zone. Despawns all friendly defenders and corpses
        /// that were spawned for that zone.
        /// </summary>
        /// <param name="zone">The zone that was exited.</param>
        public void OnZoneExited(Zone zone)
        {
            if (zone == null) return;

            // Clear current zone tracking
            if (_currentZoneId == zone.Id)
                _currentZoneId = null;

            if (_spawnedPedTierByZone.TryGetValue(zone.Id, out var pedTiers))
            {
                foreach (var pedHandle in pedTiers.Keys)
                {
                    _pedBlipService.RemoveBlipForPed(pedHandle);
                    _pedDespawnService.DespawnPed(pedHandle);
                    _corpseDeathTimes.Remove(pedHandle);
                }
                _spawnedPedTierByZone.Remove(zone.Id);
            }

            // Also clean up any corpses
            var corpsesToRemove = _corpseDeathTimes.Keys.ToList();
            foreach (var pedHandle in corpsesToRemove)
            {
                _pedDespawnService.DeletePedEntity(pedHandle);
                _corpseDeathTimes.Remove(pedHandle);
            }
        }

        /// <summary>
        /// Called when a battle starts in a zone. Tasks friendly defenders to actively
        /// seek out and engage enemies within range.
        /// </summary>
        /// <param name="zoneId">The zone where the battle started.</param>
        public void OnBattleStarted(string zoneId)
        {
            _zonesInBattle.Add(zoneId);

            if (!_spawnedPedTierByZone.TryGetValue(zoneId, out var pedTiers)) return;

            var zone = _zoneService.GetZone(zoneId);
            if (zone == null) return;

            foreach (var pedHandle in pedTiers.Keys)
            {
                // Set zone-wide perception so defenders can see enemies across the entire zone
                ConfigureBattlePerception(pedHandle, zone.Radius);

                // Task defenders to actively seek out and fight enemies within zone radius
                // This makes them run towards enemies instead of waiting to be engaged
                _gameBridge.TaskCombatHatedTargetsAroundPed(pedHandle, zone.Radius);
            }
        }

        /// <summary>
        /// Configures a ped's seeing and hearing range for zone-wide perception during battles.
        /// </summary>
        private void ConfigureBattlePerception(int pedHandle, float zoneRadius)
        {
            // Set perception range to cover the entire zone (with a small buffer)
            var perceptionRange = zoneRadius * 1.2f;
            _gameBridge.SetPedSeeingRange(pedHandle, perceptionRange);
            _gameBridge.SetPedHearingRange(pedHandle, perceptionRange);
        }

        /// <summary>
        /// Called when a battle ends in a zone. Switches friendly defenders back to walking
        /// wander mode for peaceful patrol.
        /// </summary>
        /// <param name="zoneId">The zone where the battle ended.</param>
    }
}
