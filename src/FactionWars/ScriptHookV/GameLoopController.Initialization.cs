using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FactionWars.AI.Interfaces;
using FactionWars.AI.Models;
using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Models;
using FactionWars.Configuration;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.Core.Services;
using FactionWars.Persistence.Models;
using FactionWars.Economy.Interfaces;
using FactionWars.Factions.Interfaces;
using FactionWars.Combat.Services;
using FactionWars.ScriptHookV.Combat;
using FactionWars.ScriptHookV.Data;
using FactionWars.ScriptHookV.Logging;
using FactionWars.ScriptHookV.Managers;
using FactionWars.ScriptHookV.Models;
using FactionWars.ScriptHookV.Persistence;
using FactionWars.ScriptHookV.UI;
using FactionWars.Telemetry.Interfaces;
using FactionWars.Telemetry.Services;
using FactionWars.Territory.Interfaces;
using FactionWars.Territory.Models;
using FactionWars.UI.Interfaces;
using FactionWars.UI.Models;
using GTA.Native;

namespace FactionWars.ScriptHookV
{
    public partial class GameLoopController
    {
        private void InitializeGameData()
        {
            LogInitializationStart();

            InitializeWorldDataAndEconomy();

            // Initialize follower manager for bodyguard management
            var spawnServices = ResolveSpawnServices(); InitializeFollowerManager(spawnServices);

            var allocationService = InitializeTerritoryAndFriendlyManagers(spawnServices);
            SubscribeInitializedManagerEvents(allocationService);

            // Initialize battle HUD renderer
            _battleHudRenderer = new BattleHudRenderer();

            InitializeEnemyAndRallyManagers(spawnServices, allocationService);
            _areaAnchorResolver = new AreaAnchorResolver();
            _enemyTargetCollector = new EnemyTargetCollector(_gameBridge);
            _squadStanceController = new SquadStanceController(_gameBridge, new SquadStanceResolver(), new TargetAssignmentResolver());
            ApplyRelationshipMatrix(CurrentPlayerFactionId);

            InitializeAiAndVictorySystems();

            InitializeHudAndEventRenderers();
            InitializeMenuControllers(allocationService);

            InitializeStateTelemetryAndSession();

            LogInitializationComplete();
        }

        private void SubscribeInitializedManagerEvents(IZoneDefenderAllocationService allocationService)
        {
            var friendlyDefenderManager = RequiredFriendlyDefenderManager;
            var territoryManager = RequiredTerritoryManager;

            friendlyDefenderManager.DefenderDied += (sender, e) =>
            {
                var battle = _zoneBattleManager?.GetBattleForZone(e.ZoneId);
                if (battle != null && battle.DefenderFactionId == CurrentPlayerFactionId)
                    _zoneBattleManager?.ReportTroopKilled(e.ZoneId, CurrentPlayerFactionId!, e.Tier);
            };

            friendlyDefenderManager.TerritoryLost += (sender, args) =>
                _commanderManager?.OnTerritoryLost(args.ZoneId);

            territoryManager.ZoneEntered += (sender, zone) => _battleAttackerManager?.OnPlayerZoneEntered(zone);
            territoryManager.ZoneExited += (sender, zone) => _battleAttackerManager?.OnPlayerZoneExited(zone);

            allocationService.TroopsAllocated += (sender, e) =>
                HandleTroopsAllocatedForInitializedManagers(friendlyDefenderManager, e);
        }

        private void HandleTroopsAllocatedForInitializedManagers(
            FriendlyDefenderManager friendlyDefenderManager,
            TroopsAllocatedEventArgs e)
        {
            var zone = _zoneService?.GetZone(e.ZoneId);
            if (zone != null)
            {
                friendlyDefenderManager.OnTroopsAllocated(e.FactionId, e.ZoneId, e.Tier, e.Count, zone.Center, zone.Radius);
                _enemyDefenderManager?.OnTroopsAllocated(e.FactionId, e.ZoneId, e.Tier, e.Count);
            }

            var battle = _zoneBattleManager?.GetBattleForZone(e.ZoneId);
            if (battle != null && battle.DefenderFactionId == e.FactionId)
                battle.AddDefenderTroops(e.Tier, e.Count);
        }

        private void LogInitializationComplete()
        {
            FileLogger.Separator("INITIALIZATION COMPLETE");
            FileLogger.Info($"Player faction: {CurrentPlayerFactionId ?? "UNKNOWN"}");
            FileLogger.Info($"Log file: {FileLogger.LogPath}");
        }

        private static void LogInitializationStart()
        {
            FileLogger.Separator("INITIALIZATION START");
            FileLogger.Info("InitializeGameData() called");
        }

        private void InitializeStateTelemetryAndSession()
        {
            _gameStateManager = _container.Resolve<IGameStateManager>();
            _gameStateManager.OnGameLoaded += OnGameLoaded;
            InitializeTelemetryService();
            _gameStateManager.NewGame();
            _gameStateManager.SetCurrentDifficulty(RequiredDifficultyService.Current.Level);
            ConfigureSessionSettings();
        }

        private void InitializeFollowerManager(SpawnServices spawnServices)
        {
            _followerService = _container.Resolve<IFollowerService>();
            var seatPriorityService = new VehicleSeatPriorityService(_gameBridge);
            _followerManager = new FollowerManager(new FollowerManagerDependencies
            {
                GameBridge = _gameBridge,
                FollowerService = _followerService,
                PedSpawningService = spawnServices.PedSpawning,
                DefenderRoleService = spawnServices.DefenderRole,
                PedBlipService = spawnServices.PedBlip,
                SeatPriorityService = seatPriorityService
            });
        }

        private IZoneDefenderAllocationService InitializeTerritoryAndFriendlyManagers(SpawnServices spawnServices)
        {
            _zoneService = _container.Resolve<IZoneService>();
            _territoryManager = new TerritoryManager(_gameBridge, _zoneService);
            _zoneBattleManager = _container.Resolve<IZoneBattleManager>();
            _zoneBattleManager.SetPlayerFaction(CurrentPlayerFactionId);
            _zoneBattleManager.BattleEnded += OnZoneBattleEnded;
            _zoneBattleManager.TroopKilled += OnZoneBattleTroopKilled;
            _zoneBattleManager.BattleStarted += OnZoneBattleStarted;
            _policeSuppressionController = new PoliceSuppressionController(_gameBridge, _zoneBattleManager);

            var allocationService = _container.Resolve<IZoneDefenderAllocationService>();
            _allocationService = allocationService;
            _friendlyDefenderManager = new FriendlyDefenderManager(
                new FriendlyDefenderManagerDependencies
                {
                    GameBridge = _gameBridge,
                    AllocationService = allocationService,
                    PedSpawningService = spawnServices.PedSpawning,
                    PedDespawnService = spawnServices.PedDespawn,
                    DefenderRoleService = spawnServices.DefenderRole,
                    PedBlipService = spawnServices.PedBlip,
                    ZoneService = _zoneService
                },
                CurrentPlayerFactionId ?? "");

            InitializeCommanderManager(spawnServices);
            _territoryManager.ZoneEntered += (sender, zone) => _friendlyDefenderManager.OnZoneEntered(zone);
            _territoryManager.ZoneExited += (sender, zone) => _friendlyDefenderManager.OnZoneExited(zone);
            _territoryManager.ZoneEntered += (sender, zone) => _commanderManager?.OnZoneEntered(zone);
            _territoryManager.ZoneExited += (sender, zone) => _commanderManager?.OnZoneExited(zone);
            _zoneBoundaryBlipManager = new ZoneBoundaryBlipManager(_gameBridge, _territoryManager);
            WireZoneOwnershipReconciliation();
            return allocationService;
        }

        /// <summary>
        /// Wires reactions to ownership changes that happen while the player is still inside a zone
        /// (no exit/re-enter fires): the reconciler despawns whichever side just lost the zone, and
        /// the boundary blip is recoloured into the new owner's colour.
        /// </summary>
        private void WireZoneOwnershipReconciliation()
        {
            if (_zoneService == null) return;

            var ownershipReconciler = new ZoneOwnershipReconciler(
                despawnFriendlyForZone: zoneId =>
                {
                    var lostZone = _zoneService?.GetZone(zoneId);
                    if (lostZone != null)
                        _friendlyDefenderManager?.OnZoneExited(lostZone);
                },
                despawnEnemyForZone: zoneId => _enemyDefenderManager?.DespawnForZone(zoneId),
                getPlayerFactionId: () => CurrentPlayerFactionId);

            _zoneService.ZoneOwnershipChanged += (sender, e) =>
                ownershipReconciler.OnOwnershipChanged(e.ZoneId, e.PreviousOwner, e.NewOwner);
            _zoneService.ZoneOwnershipChanged += (sender, e) =>
                _zoneBoundaryBlipManager?.OnOwnershipChanged(e.ZoneId, e.NewOwner);
        }

        private void InitializeCommanderManager(SpawnServices spawnServices)
        {
            _commanderManager = new CommanderManager(
                new CommanderManagerDependencies
                {
                    GameBridge = _gameBridge,
                    PedSpawningService = spawnServices.PedSpawning,
                    PedDespawnService = spawnServices.PedDespawn,
                    PedBlipService = spawnServices.PedBlip,
                    ZoneService = _zoneService
                },
                CurrentPlayerFactionId ?? "",
                _ => _mainMenuController?.ShowMainMenu());
        }

        private void InitializeEnemyAndRallyManagers(
            SpawnServices spawnServices,
            IZoneDefenderAllocationService allocationService)
        {
            var zoneService = RequiredZoneService;
            var zoneBattleManager = RequiredZoneBattleManager;
            var territoryManager = RequiredTerritoryManager;
            var friendlyDefenderManager = RequiredFriendlyDefenderManager;

            _enemyDefenderManager = new EnemyDefenderManager(new EnemyDefenderManagerDependencies
            {
                GameBridge = _gameBridge,
                AllocationService = allocationService,
                PedSpawningService = spawnServices.PedSpawning,
                PedDespawnService = spawnServices.PedDespawn,
                DefenderRoleService = spawnServices.DefenderRole,
                PedBlipService = spawnServices.PedBlip,
                ZoneService = zoneService,
                ZoneBattleManager = zoneBattleManager,
                CurrentPlayerFactionIdAccessor = () => CurrentPlayerFactionId
            });

            _battleAttackerManager = new BattleAttackerManager(
                new BattleAttackerManagerDependencies
                {
                    GameBridge = _gameBridge,
                    ZoneBattleManager = zoneBattleManager,
                    PedSpawningService = spawnServices.PedSpawning,
                    PedDespawnService = spawnServices.PedDespawn,
                    DefenderRoleService = spawnServices.DefenderRole,
                    PedBlipService = spawnServices.PedBlip,
                    ZoneService = zoneService,
                    FactionService = _factionService
                },
                CurrentPlayerFactionId ?? "");

            _defenderRallyController = new DefenderRallyController(new DefenderRallyControllerDependencies
            {
                Bridge = _gameBridge,
                Territory = territoryManager,
                Defenders = friendlyDefenderManager,
                Combat = new ZoneBattleCombatActivityAdapter(zoneBattleManager),
                CurrentPlayerFactionIdAccessor = () => CurrentPlayerFactionId,
                NowMs = () => System.Environment.TickCount
            });
        }

        private void InitializeAiAndVictorySystems()
        {
            var strategies = _container.Resolve<IDictionary<string, IAIStrategy>>();
            _aiController = _container.Resolve<IAIController>();
            _aiController.SetPlayerFactionId(CurrentPlayerFactionId);
            _aiController.Start();

            _vehicleThreatService = _container.Resolve<IVehicleThreatService>();
            _antiVehicleResponseService = _container.Resolve<IAntiVehicleResponseService>();

            var victoryConditionService = _container.Resolve<IVictoryConditionService>();
            var notificationService = _container.Resolve<INotificationService>();
            _victoryManager = new VictoryManager(victoryConditionService, _factionService, notificationService);
            _victoryManager.Start();
        }

    }
}
