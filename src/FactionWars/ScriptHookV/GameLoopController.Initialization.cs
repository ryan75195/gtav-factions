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
                friendlyDefenderManager.OnTroopsAllocated(e.FactionId, e.ZoneId, e.Tier, e.Count, zone.Center, zone.Radius);

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
                DefenderTierService = spawnServices.DefenderTier,
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

            var allocationService = _container.Resolve<IZoneDefenderAllocationService>();
            _allocationService = allocationService;
            _friendlyDefenderManager = new FriendlyDefenderManager(
                new FriendlyDefenderManagerDependencies
                {
                    GameBridge = _gameBridge,
                    AllocationService = allocationService,
                    PedSpawningService = spawnServices.PedSpawning,
                    PedDespawnService = spawnServices.PedDespawn,
                    DefenderTierService = spawnServices.DefenderTier,
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
            return allocationService;
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
                DefenderTierService = spawnServices.DefenderTier,
                PedBlipService = spawnServices.PedBlip,
                ZoneService = zoneService,
                ZoneBattleManager = zoneBattleManager
            });

            _battleAttackerManager = new BattleAttackerManager(
                new BattleAttackerManagerDependencies
                {
                    GameBridge = _gameBridge,
                    ZoneBattleManager = zoneBattleManager,
                    PedSpawningService = spawnServices.PedSpawning,
                    PedDespawnService = spawnServices.PedDespawn,
                    DefenderTierService = spawnServices.DefenderTier,
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
            _aiManager = new AIManager(_factionService, RequiredZoneService, strategies);
            _aiManager.Start();
            _aiManager.SetPlayerFactionId(CurrentPlayerFactionId);
            _aiManager.OnAIDecision += HandleAIDecision;

            _backgroundBattleSimulator = _container.Resolve<BackgroundBattleSimulator>();
            _aiManager.OnAIDecision += _backgroundBattleSimulator.HandleAIDecision;
            _aiDecisionExecutor = _container.Resolve<AIDecisionExecutor>();

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

        private void InitializeHudAndEventRenderers()
        {
            var territoryManager = RequiredTerritoryManager;
            _combatHudRenderer = new CombatHudRenderer();
            _territoryIndicatorRenderer = new TerritoryIndicatorRenderer();

            if (_gameBridge.GetType().FullName == "FactionWars.ScriptHookV.GameBridge")
                _playTimeHudRenderer = new PlayTimeHudRenderer();

            _eventFeedRenderer = new EventFeedRenderer(_container.Resolve<IFactionRepository>());
            _eventFeedService = _container.Resolve<IEventFeedService>();
            territoryManager.ZoneEntered += OnZoneEntered;
            territoryManager.ZoneExited += OnZoneExited;
            territoryManager.NeutralZoneEntered += OnNeutralZoneEntered;
            territoryManager.ZoneExited += OnZoneExitedForClaim;
        }

        private void InitializeMenuControllers(IZoneDefenderAllocationService allocationService)
        {
            _menuProvider = _container.Resolve<IMenuProvider>();
            _mainMenuController = new MainMenuController(_menuProvider);
            var playerContext = _container.Resolve<IPlayerContext>();
            var purchaseService = _container.Resolve<ITroopPurchaseService>();

            InitializeOverviewMenus(allocationService, playerContext);
            InitializeRecruitmentMenus(playerContext, purchaseService);
            InitializeResourcesAndSettingsMenus(playerContext);
            _menuProvider.ItemSelected += OnMainMenuItemSelected;
        }

        private void InitializeOverviewMenus(
            IZoneDefenderAllocationService allocationService,
            IPlayerContext playerContext)
        {
            var menuProvider = RequiredMenuProvider;
            var mainMenuController = RequiredMainMenuController;
            var zoneService = RequiredZoneService;

            _overviewMenuController = new OverviewMenuController(
                menuProvider, _factionService, zoneService, playerContext);
            _overviewMenuController.BackRequested += (s, e) => mainMenuController.OnKeyDown(MainMenuController.MenuToggleKeyCode);

            _zoneManagementMenuController = new ZoneManagementMenuController(
                menuProvider, _factionService, zoneService, playerContext, allocationService);
            _zoneManagementMenuController.BackRequested += (s, e) => mainMenuController.OnKeyDown(MainMenuController.MenuToggleKeyCode);
        }

        private void InitializeRecruitmentMenus(IPlayerContext playerContext, ITroopPurchaseService purchaseService)
        {
            var menuProvider = RequiredMenuProvider;
            var mainMenuController = RequiredMainMenuController;

            _recruitmentMenuController = new RecruitmentMenuController(menuProvider, _gameBridge);
            _recruitmentMenuController.BackRequested += (s, e) => mainMenuController.OnKeyDown(MainMenuController.MenuToggleKeyCode);
            _defendersMenuController = new DefendersMenuController(menuProvider, _factionService, purchaseService, playerContext);
            _defendersMenuController.BackRequested += (s, e) => _recruitmentMenuController.Show();
            _squadMenuController = new SquadMenuController(
                new SquadMenuControllerDependencies
                {
                    MenuProvider = menuProvider,
                    PurchaseService = purchaseService,
                    FollowerService = _followerService!,
                    PlayerContext = playerContext
                },
                _followerManager,
                _gameBridge);
            _squadMenuController.BackRequested += (s, e) => _recruitmentMenuController.Show();
            _recruitmentMenuController.DefendersRequested += (s, e) => _defendersMenuController.Show();
            _recruitmentMenuController.SquadRequested += (s, e) => _squadMenuController.Show();
        }

        private void InitializeResourcesAndSettingsMenus(IPlayerContext playerContext)
        {
            var menuProvider = RequiredMenuProvider;
            var mainMenuController = RequiredMainMenuController;
            var zoneService = RequiredZoneService;
            var resourceModifier = _container.Resolve<IZoneTraitResourceModifier>();
            var supplyLineService = _container.Resolve<ISupplyLineService>();
            _resourcesMenuController = new ResourcesMenuController(new ResourcesMenuControllerDependencies
            {
                MenuProvider = menuProvider,
                FactionService = _factionService,
                ZoneService = zoneService,
                PlayerContext = playerContext,
                ResourceTickService = _resourceTickService,
                ResourceModifier = resourceModifier,
                SupplyLineService = supplyLineService
            });
            _resourcesMenuController.BackRequested += (s, e) => mainMenuController.OnKeyDown(MainMenuController.MenuToggleKeyCode);

            _difficultyService = _container.Resolve<IDifficultyService>();
            var difficultyService = RequiredDifficultyService;
            _resourceTickService.SetAiIncomeMultiplier(difficultyService.Current.AiIncomeMultiplier);
            _resourceTickService.SetTickInterval(difficultyService.Current.TickIntervalSeconds);
            _resourceTickService.SetPlayerFactionId(CurrentPlayerFactionId);
            difficultyService.DifficultyChanged += OnDifficultyChanged;
            _settingsMenuController = new SettingsMenuController(menuProvider, difficultyService, _gameBridge);
            _settingsMenuController.BackRequested += (s, e) => mainMenuController.OnKeyDown(MainMenuController.MenuToggleKeyCode);
            _shopMenuController = new ShopMenuController(menuProvider, _gameBridge);
            _shopMenuController.BackRequested += (s, e) => mainMenuController.OnKeyDown(MainMenuController.MenuToggleKeyCode);
        }

        private void InitializeWorldDataAndEconomy()
        {
            var scriptsDir = _gameBridge.GetScriptsDirectory();
            var zonesFilePath = Path.Combine(scriptsDir, "FactionWars", "zones.json");
            FileLogger.Info($"Looking for zones at: {zonesFilePath}");
            _zoneDataLoader.LoadZonesWithFallback(zonesFilePath);
            FileLogger.Info($"Loaded {_zoneRepository.Count} zones");

            FileLogger.Info("Initializing factions...");
            _factionInitializer.Initialize();

            FileLogger.Separator("ZONE OWNERSHIP");
            foreach (var zone in _zoneRepository.GetAll())
                FileLogger.Zone($"Zone '{zone.Name}' (ID: {zone.Id}) -> Owner: {zone.OwnerFactionId ?? "NONE"}");

            _mapBlipManager = new MapBlipManager(_gameBridge, _zoneRepository, _factionService);
            _mapBlipManager.Initialize();
            _economyManager = new EconomyManager(_resourceTickService, _gameBridge);
            _economyManager.Start();
            _economyManager.SetPlayerFactionId(CurrentPlayerFactionId);
        }

        private SpawnServices ResolveSpawnServices()
        {
            return new SpawnServices
            {
                PedSpawning = _container.Resolve<IPedSpawningService>(),
                PedDespawn = _container.Resolve<IPedDespawnService>(),
                DefenderTier = _container.Resolve<IDefenderTierService>(),
                PedBlip = _container.Resolve<IPedBlipService>()
            };
        }

        private sealed class SpawnServices
        {
            public IPedSpawningService PedSpawning { get; set; } = null!;
            public IPedDespawnService PedDespawn { get; set; } = null!;
            public IDefenderTierService DefenderTier { get; set; } = null!;
            public IPedBlipService PedBlip { get; set; } = null!;
        }

    }
}
