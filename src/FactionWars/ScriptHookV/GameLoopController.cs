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
    /// <summary>
    /// Controller for the game loop logic. This class contains all testable game logic
    /// that FactionWarsScript delegates to. Separating this from GTA.Script allows
    /// for unit testing without ScriptHookVDotNet dependencies.
    /// </summary>
    public class GameLoopController
    {
        private readonly ServiceContainer _container;
        private readonly CharacterSwitchDetector _characterSwitchDetector;
        private readonly IGameBridge _gameBridge;
        private readonly IZoneRepository _zoneRepository;
        private readonly IFactionService _factionService;
        private readonly IFactionRepository _factionRepository;
        private readonly IResourceTickService _resourceTickService;
        private IDifficultyService? _difficultyService;
        private readonly FactionInitializer _factionInitializer;
        private readonly ZoneDataLoader _zoneDataLoader;
        private MapBlipManager? _mapBlipManager;
        private EconomyManager? _economyManager;
        private FollowerManager? _followerManager;
        private IFollowerService? _followerService;
        private TerritoryManager? _territoryManager;
        private ZoneBoundaryBlipManager? _zoneBoundaryBlipManager;
        private AIManager? _aiManager;
        private BackgroundBattleSimulator? _backgroundBattleSimulator;
        private AIDecisionExecutor? _aiDecisionExecutor;
        private VictoryManager? _victoryManager;
        private FriendlyDefenderManager? _friendlyDefenderManager;
        private DefenderRallyController? _defenderRallyController;
        private EnemyDefenderManager? _enemyDefenderManager;
        private BattleAttackerManager? _battleAttackerManager;
        private CommanderManager? _commanderManager;
        private IAIController? _aiController;
        private IGameStateManager? _gameStateManager;
        private MainMenuController? _mainMenuController;
        private IMenuProvider? _menuProvider;
        private RecruitmentMenuController? _recruitmentMenuController;
        private DefendersMenuController? _defendersMenuController;
        private SquadMenuController? _squadMenuController;
        private OverviewMenuController? _overviewMenuController;
        private ZoneManagementMenuController? _zoneManagementMenuController;
        private ResourcesMenuController? _resourcesMenuController;
        private SettingsMenuController? _settingsMenuController;
        private ShopMenuController? _shopMenuController;
        private CombatHudRenderer? _combatHudRenderer;
        private TerritoryIndicatorRenderer? _territoryIndicatorRenderer;
        private EventFeedRenderer? _eventFeedRenderer;
        private IEventFeedService? _eventFeedService;
        private IZoneService? _zoneService;
        private IZoneDefenderAllocationService? _allocationService;
        private IZoneBattleManager? _zoneBattleManager;
        private IVehicleThreatService? _vehicleThreatService;
        private IAntiVehicleResponseService? _antiVehicleResponseService;
        private TelemetryService? _telemetryService;
        private BattleHudRenderer? _battleHudRenderer;
        private PlayTimeHudRenderer? _playTimeHudRenderer;
        private int _currentBattleHudIndex = 0;
        private const int BattleCycleKeyCode = 0x42; // B key
        private DateTime _lastTickTime;
        private bool _isInitialized;
        private bool _characterSwitchInitialized;
        private bool _gameDataInitialized;

        // Threat decay tracking
        private float _threatDecayTimer = 0f;
        private const float ThreatDecayInterval = 60f;  // Decay every 60 seconds
        private const float ThreatDecayRate = 0.1f;     // 10% decay per interval

        // AI recruitment tracking
        private float _aiRecruitmentTimer = 0f;
        private const float AIRecruitmentInterval = 60f;  // Recruit every 60 seconds (sync with resource ticks)

        // Neutral zone claim state
        private Zone? _currentNeutralZone;
        private bool _showingClaimPrompt;
        private const int ClaimKeyCode = 0x45; // E key

        // Menu hold-to-repeat state
        private const int EnterKeyCode = 0x0D; // Enter key
        private const int NumpadEnterKeyCode = 0x0D; // Same as Enter
        private bool _enterKeyHeld;

        // Controller input constants (GTA V control IDs)
        private const int ControlDpadRight = 175;   // INPUT_PHONE_RIGHT
        private const int ControlDpadDown = 173;     // INPUT_PHONE_DOWN
        private const int ControlLB = 37;            // INPUT_AIM (LB/L1)
        private const int ControlFrontendAccept = 201; // A/Cross button

        /// <summary>
        /// Event raised when the player switches to a different character.
        /// </summary>
        public event CharacterSwitchedHandler? OnCharacterSwitched;

        /// <summary>
        /// Gets whether the controller is initialized and ready to process events.
        /// </summary>
        public bool IsInitialized => _isInitialized;

        /// <summary>
        /// Gets the service container used by this controller.
        /// </summary>
        public ServiceContainer ServiceContainer => _container;

        /// <summary>
        /// Gets the current player's faction ID based on their character model.
        /// Returns null if the detector hasn't been initialized or the character is unknown.
        /// </summary>
        public string? CurrentPlayerFactionId => _characterSwitchDetector.CurrentFactionId;

        /// <summary>
        /// How many "alive" combatants the player counts as for an active zone battle.
        /// Used as the alive-count callback the player participant carries into a battle.
        /// Survives the GTA death/respawn fade window because the natural ZoneExited
        /// flow ends battles when the corpse is teleported out of the zone — collapsing
        /// to 0 here would create stillborn battles on respawn-into-enemy-zone.
        /// </summary>
        public int GetPlayerCombatAliveCount(string playerFactionId)
        {
            return 1 + (_followerService?.GetFollowerCount(playerFactionId) ?? 0);
        }

        /// <summary>
        /// Gets the MapBlipManager for zone blip management.
        /// Returns null if not yet initialized.
        /// </summary>
        public MapBlipManager? MapBlipManager => _mapBlipManager;

        /// <summary>
        /// Gets the EconomyManager for resource tick management.
        /// Returns null if not yet initialized.
        /// </summary>
        public EconomyManager? EconomyManager => _economyManager;

        /// <summary>
        /// Gets the FollowerManager for managing player followers.
        /// Returns null if not yet initialized.
        /// </summary>
        public FollowerManager? FollowerManager => _followerManager;

        /// <summary>
        /// Gets the MainMenuController for handling menu interactions.
        /// Returns null if not yet initialized.
        /// </summary>
        public MainMenuController? MainMenuController => _mainMenuController;

        /// <summary>
        /// Gets the RecruitmentMenuController for handling recruitment menu interactions.
        /// Returns null if not yet initialized.
        /// </summary>
        public RecruitmentMenuController? RecruitmentMenuController => _recruitmentMenuController;

        /// <summary>
        /// Gets the TerritoryManager for zone detection.
        /// Returns null if not yet initialized.
        /// </summary>
        public TerritoryManager? TerritoryManager => _territoryManager;

        /// <summary>
        /// Gets the AIManager for AI faction decisions.
        /// Returns null if not yet initialized.
        /// </summary>
        public AIManager? AIManager => _aiManager;

        /// <summary>
        /// Gets the VictoryManager for victory condition checking.
        /// Returns null if not yet initialized.
        /// </summary>
        public VictoryManager? VictoryManager => _victoryManager;

        /// <summary>
        /// Gets the FriendlyDefenderManager for friendly zone defender spawning.
        /// Returns null if not yet initialized.
        /// </summary>
        public FriendlyDefenderManager? FriendlyDefenderManager => _friendlyDefenderManager;

        /// <summary>
        /// Creates a new GameLoopController with the specified service container.
        /// </summary>
        /// <param name="container">The service container with all wired services.</param>
        /// <exception cref="ArgumentNullException">Thrown if container is null.</exception>
        public GameLoopController(ServiceContainer container)
        {
            _container = container ?? throw new ArgumentNullException(nameof(container));

            // Resolve dependencies from container
            _gameBridge = _container.Resolve<IGameBridge>();
            var factionDetector = _container.Resolve<IPlayerFactionDetector>();
            _zoneRepository = _container.Resolve<IZoneRepository>();
            _factionRepository = _container.Resolve<IFactionRepository>();
            _factionService = _container.Resolve<IFactionService>();
            _resourceTickService = _container.Resolve<IResourceTickService>();

            // Create character switch detector
            _characterSwitchDetector = new CharacterSwitchDetector(_gameBridge, factionDetector);
            _characterSwitchDetector.OnCharacterSwitched += HandleCharacterSwitched;

            // Create zone and faction initializers
            _zoneDataLoader = new ZoneDataLoader(_zoneRepository);
            var allocationService = _container.Resolve<IZoneDefenderAllocationService>();
            _factionInitializer = new FactionInitializer(_factionRepository, _zoneRepository, allocationService);

            _isInitialized = true;
            _characterSwitchInitialized = false;
            _gameDataInitialized = false;
            _lastTickTime = DateTime.UtcNow;
        }

        /// <summary>
        /// Called every frame by the game loop. Handles periodic updates.
        /// </summary>
        public void OnTick()
        {
            if (!_isInitialized)
                return;

            // Initialize character switch detection FIRST so CurrentPlayerFactionId is available
            if (!_characterSwitchInitialized)
            {
                _characterSwitchDetector.Initialize();
                _characterSwitchInitialized = true;
            }

            // Initialize game data on first tick (zones and factions)
            // Must happen AFTER character switch detector so EconomyManager gets correct faction ID
            if (!_gameDataInitialized)
            {
                InitializeGameData();
                _gameDataInitialized = true;
            }

            // Check for character switches
            _characterSwitchDetector.CheckForSwitch();

            // Calculate delta time for this frame
            var now = DateTime.UtcNow;
            var deltaTime = (float)(now - _lastTickTime).TotalSeconds;
            _lastTickTime = now;

            // Update economy manager
            _economyManager?.Update(deltaTime);

            // Update play time tracker
            _gameStateManager?.UpdatePlayTime(deltaTime);

            // Update telemetry snapshot timer after play time has advanced for this frame.
            _telemetryService?.Update(deltaTime);

            // Poll controller input
            PollControllerInput();

            // Update menu system with key state for hold-to-repeat
            _menuProvider?.SetSelectKeyHeld(_enterKeyHeld);
            _mainMenuController?.Update();

            // Update map blip colors to reflect current zone ownership
            _mapBlipManager?.UpdateBlipColors();

            // Update territory detection (checks player position against zones)
            _territoryManager?.Update();

            // Update AI controller (handles decisions, recruitment, battles)
            _aiController?.Update(deltaTime);

            // Update zone battle manager (unified battle lifecycle)
            _zoneBattleManager?.Tick(deltaTime);

            // Update victory manager (checks for 100% control)
            _victoryManager?.Update(deltaTime);

            // Update follower manager (updates follower positions and behavior)
            _followerManager?.Update(CurrentPlayerFactionId ?? "");

            // Update friendly defender manager (death detection, replacement spawning)
            _friendlyDefenderManager?.Update();
            _defenderRallyController?.Update();

            // Update commander manager (death detection, interaction prompt)
            _commanderManager?.Update();

            // Update enemy defender manager (death detection, replacement spawning)
            var currentZone = _territoryManager?.CurrentZone;
            var enemyFactionId = currentZone?.OwnerFactionId;
            if (enemyFactionId != null && enemyFactionId != CurrentPlayerFactionId)
            {
                _enemyDefenderManager?.Update(enemyFactionId);
            }

            // Update battle attacker manager (death detection for attackers in player's defending zone)
            _battleAttackerManager?.Update();

            // Update and draw HUD elements
            UpdateAndDrawHud();
        }

        /// <summary>
        /// Updates HUD data and draws HUD elements to the screen.
        /// </summary>
        private void UpdateAndDrawHud()
        {
            var playerFactionId = CurrentPlayerFactionId;

            // Update territory indicator based on current zone
            if (_territoryIndicatorRenderer != null && _territoryManager != null)
            {
                var currentZone = _territoryManager.CurrentZone;
                if (currentZone != null)
                {
                    // Get faction info for the zone owner
                    string? ownerFactionName = null;
                    Factions.Models.FactionColor? ownerColor = null;

                    if (currentZone.OwnerFactionId != null)
                    {
                        var ownerFaction = _factionService.GetFaction(currentZone.OwnerFactionId);
                        ownerFactionName = ownerFaction?.Name;
                        ownerColor = ownerFaction?.Color;
                    }

                    bool isPlayerOwned = currentZone.OwnerFactionId == playerFactionId;

                    // Check if there's an active battle in this zone
                    var activeBattle = _zoneBattleManager?.GetBattleForZone(currentZone.Id);
                    bool isDefendingBattle = activeBattle != null &&
                                             activeBattle.DefenderFactionId == playerFactionId;

                    // Check if the player is attacking this zone
                    var playerBattle = _zoneBattleManager?.GetPlayerCurrentBattle();
                    bool isPlayerAttackingHere = playerBattle != null && playerBattle.ZoneId == currentZone.Id &&
                                                 playerBattle.IsPlayerAttacking;

                    bool isContested = isDefendingBattle || isPlayerAttackingHere;

                    // Get deployed and reserve counts for player-owned zones
                    int deployedCount = 0;
                    int reserveCount = 0;
                    int playerTroopCount = 0;
                    int enemyDefenderCount = 0;
                    int enemyReserveCount = 0;

                    if (isDefendingBattle && activeBattle != null)
                    {
                        // Player is defending their zone - use battle troop counts
                        // Show spawned defenders from FriendlyDefenderManager as "deployed"
                        deployedCount = _friendlyDefenderManager?.GetSpawnedDefenderCount(currentZone.Id) ?? 0;

                        // Reserve = battle's defender troops minus spawned (what can still spawn)
                        reserveCount = Math.Max(0, activeBattle.TotalDefenderTroops - deployedCount);

                        // Also populate enemy attacker counts for the Territory HUD
                        enemyDefenderCount = _battleAttackerManager?.GetSpawnedAttackerCount(currentZone.Id) ?? 0;
                        enemyReserveCount = Math.Max(0, activeBattle.TotalAttackerTroops - enemyDefenderCount);
                    }
                    else if (isPlayerOwned && _friendlyDefenderManager != null)
                    {
                        deployedCount = _friendlyDefenderManager.GetSpawnedDefenderCount(currentZone.Id);

                        // Get total allocation as reserve
                        if (_allocationService != null && playerFactionId != null)
                        {
                            var allocation = _allocationService.GetAllocation(playerFactionId, currentZone.Id);
                            if (allocation != null)
                            {
                                reserveCount = Math.Max(0, allocation.TotalTroops - deployedCount);
                            }
                        }
                    }
                    else if (isPlayerAttackingHere && playerBattle != null)
                    {
                        // Player is attacking this enemy zone - use ZoneBattleManager data
                        var playerParticipant = playerBattle.Attackers.FirstOrDefault(p => p.IsPlayer);
                        playerTroopCount = playerParticipant?.AliveCount ?? 0;
                        enemyDefenderCount = playerBattle.Defender.AliveCount;

                        // Get enemy reserves from enemy defender manager
                        if (_enemyDefenderManager != null)
                        {
                            enemyReserveCount = _enemyDefenderManager.GetRemainingReserves(
                                playerBattle.ZoneId, playerBattle.Defender.FactionId);
                        }
                    }

                    var territoryData = new TerritoryIndicatorData(
                        currentZone.Name,
                        ownerFactionName,
                        ownerColor,
                        currentZone.ControlPercentage,
                        isContested,
                        isPlayerOwned,
                        deployedDefenderCount: deployedCount,
                        reserveDefenderCount: reserveCount,
                        playerTroopCount: playerTroopCount,
                        enemyDefenderCount: enemyDefenderCount,
                        enemyReserveCount: enemyReserveCount);

                    _territoryIndicatorRenderer.Render(territoryData);
                }
                else
                {
                    _territoryIndicatorRenderer.Hide();
                }

                _territoryIndicatorRenderer.Draw();
            }

            // Draw battle HUD showing active AI battles
            UpdateAndDrawBattleHud();

            // Draw play time HUD (only when game is loaded)
            if (_playTimeHudRenderer != null && _gameStateManager != null && _gameStateManager.HasGameLoaded)
            {
                _playTimeHudRenderer.SetPlayTime(_gameStateManager.TotalPlayTimeSeconds);
                _playTimeHudRenderer.Draw();
            }

            // Show claim prompt as help text (auto-adapts button label for controller)
            if (_showingClaimPrompt && _currentNeutralZone != null)
            {
                var cost = GetBasicTroopCost();
                _gameBridge.DisplayHelpText($"Press ~INPUT_CONTEXT~ to claim for ~g~${cost}");
            }

            // Combat HUD disabled - TerritoryIndicatorRenderer now shows all combat info
            // including reserves in the "nicer graphics" top-right display
            _combatHudRenderer?.HideCombatHud();

            // Event feed disabled - using native GTA V notifications instead
            // if (_eventFeedRenderer != null && _eventFeedService != null)
            // {
            //     _eventFeedRenderer.Render(_eventFeedService.Entries);
            //     _eventFeedRenderer.Draw();
            // }
        }

        /// <summary>
        /// Updates and draws the battle HUD showing active AI battles.
        /// </summary>
        private void UpdateAndDrawBattleHud()
        {
            if (_zoneBattleManager == null || _battleHudRenderer == null)
                return;

            var battles = _zoneBattleManager.GetAllActiveBattles();
            if (battles.Count == 0)
            {
                _battleHudRenderer.Hide();
                return;
            }

            // Clamp index
            if (_currentBattleHudIndex >= battles.Count)
                _currentBattleHudIndex = 0;

            var battle = battles[_currentBattleHudIndex];
            var zone = _zoneService?.GetZone(battle.ZoneId);
            var attackerFaction = _factionService.GetFaction(battle.AttackerFactionId);
            var defenderFaction = _factionService.GetFaction(battle.DefenderFactionId);

            var hudData = new BattleHudData(
                zone?.Name ?? battle.ZoneId,
                attackerFaction?.Name ?? battle.AttackerFactionId,
                battle.TotalAttackerTroops,
                defenderFaction?.Name ?? battle.DefenderFactionId,
                battle.TotalDefenderTroops,
                _currentBattleHudIndex + 1,
                battles.Count);

            _battleHudRenderer.SetData(hudData);
            _battleHudRenderer.Draw();
        }

        /// <summary>
        /// Initializes game data including zones and factions.
        /// Called on the first tick to ensure the game is ready.
        /// </summary>
        private void InitializeGameData()
        {
            FileLogger.Separator("INITIALIZATION START");
            FileLogger.Info("InitializeGameData() called");

            // Load zones from file if exists, otherwise use defaults
            var scriptsDir = _gameBridge.GetScriptsDirectory();
            var zonesFilePath = Path.Combine(scriptsDir, "FactionWars", "zones.json");
            FileLogger.Info($"Looking for zones at: {zonesFilePath}");
            _zoneDataLoader.LoadZonesWithFallback(zonesFilePath);
            FileLogger.Info($"Loaded {_zoneRepository.Count} zones");

            // Then initialize factions with their starting conditions
            FileLogger.Info("Initializing factions...");
            _factionInitializer.Initialize();

            // Log zone ownership
            FileLogger.Separator("ZONE OWNERSHIP");
            foreach (var zone in _zoneRepository.GetAll())
            {
                FileLogger.Zone($"Zone '{zone.Name}' (ID: {zone.Id}) -> Owner: {zone.OwnerFactionId ?? "NONE"}");
            }

            // Initialize map blips to show zone ownership on the map
            _mapBlipManager = new MapBlipManager(_gameBridge, _zoneRepository, _factionService);
            _mapBlipManager.Initialize();

            // Initialize economy manager for resource ticks
            _economyManager = new EconomyManager(_resourceTickService, _gameBridge);
            _economyManager.Start();
            _economyManager.SetPlayerFactionId(CurrentPlayerFactionId);

            // Initialize follower manager for bodyguard management
            var followerService = _container.Resolve<IFollowerService>();
            _followerService = followerService;
            var pedSpawningService = _container.Resolve<IPedSpawningService>();
            var pedDespawnService = _container.Resolve<IPedDespawnService>();
            var defenderTierService = _container.Resolve<IDefenderTierService>();
            var pedBlipService = _container.Resolve<IPedBlipService>();
            var seatPriorityService = new VehicleSeatPriorityService(_gameBridge);
            _followerManager = new FollowerManager(_gameBridge, followerService, pedSpawningService, defenderTierService, pedBlipService, seatPriorityService);

            // Initialize territory manager for zone detection
            _zoneService = _container.Resolve<IZoneService>();
            _territoryManager = new TerritoryManager(_gameBridge, _zoneService);

            // Initialize zone battle manager and subscribe to its events for domain operations
            _zoneBattleManager = _container.Resolve<IZoneBattleManager>();
            _zoneBattleManager.SetPlayerFaction(CurrentPlayerFactionId);
            _zoneBattleManager.BattleEnded += OnZoneBattleEnded;
            _zoneBattleManager.TroopKilled += OnZoneBattleTroopKilled;
            _zoneBattleManager.BattleStarted += OnZoneBattleStarted;

            // Initialize friendly defender manager for spawning defenders in player-owned zones
            var allocationService = _container.Resolve<IZoneDefenderAllocationService>();
            _allocationService = allocationService;
            _friendlyDefenderManager = new FriendlyDefenderManager(
                _gameBridge,
                allocationService,
                pedSpawningService,
                pedDespawnService,
                defenderTierService,
                pedBlipService,
                _zoneService,
                CurrentPlayerFactionId ?? "");

            // Create CommanderManager
            var playerFactionId = CurrentPlayerFactionId ?? "";
            _commanderManager = new CommanderManager(
                _gameBridge,
                pedSpawningService,
                pedDespawnService,
                pedBlipService,
                _zoneService,
                playerFactionId,
                _ => _mainMenuController?.ShowMainMenu());

            // Subscribe to zone events for friendly defender spawning
            _territoryManager.ZoneEntered += (sender, zone) => _friendlyDefenderManager.OnZoneEntered(zone);
            _territoryManager.ZoneExited += (sender, zone) => _friendlyDefenderManager.OnZoneExited(zone);

            // Subscribe to zone events for commander spawning
            _territoryManager.ZoneEntered += (sender, zone) => _commanderManager?.OnZoneEntered(zone);
            _territoryManager.ZoneExited += (sender, zone) => _commanderManager?.OnZoneExited(zone);

            // Show a translucent radius blip around the zone the player is currently in
            _zoneBoundaryBlipManager = new ZoneBoundaryBlipManager(_gameBridge, _territoryManager);

            // Subscribe to defender death events to sync with ZoneBattleManager
            _friendlyDefenderManager.DefenderDied += (sender, e) =>
            {
                // If there's an active battle in this zone where player is defender,
                // report the kill to the battle manager to keep troop counts in sync
                var battle = _zoneBattleManager?.GetBattleForZone(e.ZoneId);
                if (battle != null && battle.DefenderFactionId == CurrentPlayerFactionId)
                {
                    _zoneBattleManager?.ReportTroopKilled(e.ZoneId, CurrentPlayerFactionId!, e.Tier);
                }
            };

            // Subscribe to territory loss events to despawn commander when all defenders die
            _friendlyDefenderManager.TerritoryLost += (sender, args) =>
            {
                _commanderManager?.OnTerritoryLost(args.ZoneId);
            };

            // Subscribe battle attacker manager to zone events
            _territoryManager.ZoneEntered += (sender, zone) => _battleAttackerManager?.OnPlayerZoneEntered(zone);
            _territoryManager.ZoneExited += (sender, zone) => _battleAttackerManager?.OnPlayerZoneExited(zone);

            // Subscribe to troop allocation events for immediate spawning when player is in zone
            // and for updating active battles when player allocates reinforcements
            allocationService.TroopsAllocated += (sender, e) =>
            {
                var zone = _zoneService?.GetZone(e.ZoneId);
                if (zone != null)
                {
                    _friendlyDefenderManager.OnTroopsAllocated(e.FactionId, e.ZoneId, e.Tier, e.Count, zone.Center, zone.Radius);
                }

                // If there's an active battle in this zone where the allocating
                // faction is the defender, add troops to the battle so the
                // defender participant stays in sync with the allocation. This
                // applies to AI Defend reinforcement just as much as to player
                // allocations — without it, AI mid-battle reinforcements grow
                // the allocation but not the participant, and the player can
                // win by clearing the original participant count, leaving
                // phantom troops in the allocation.
                {
                    var battle = _zoneBattleManager?.GetBattleForZone(e.ZoneId);
                    if (battle != null && battle.DefenderFactionId == e.FactionId)
                    {
                        battle.AddDefenderTroops(e.Tier, e.Count);
                    }
                }
            };

            // Initialize battle HUD renderer
            _battleHudRenderer = new BattleHudRenderer();

            // Initialize enemy defender manager for spawning defenders in enemy zones
            _enemyDefenderManager = new EnemyDefenderManager(
                _gameBridge,
                allocationService,
                pedSpawningService,
                pedDespawnService,
                defenderTierService,
                pedBlipService,
                _zoneService,
                _zoneBattleManager);

            // Initialize battle attacker manager for spawning attackers when player defends their zone
            _battleAttackerManager = new BattleAttackerManager(
                _gameBridge,
                _zoneBattleManager,
                pedSpawningService,
                pedDespawnService,
                defenderTierService,
                pedBlipService,
                _zoneService,
                _factionService,
                CurrentPlayerFactionId ?? "");

            _defenderRallyController = new DefenderRallyController(
                _gameBridge,
                _territoryManager,
                _friendlyDefenderManager,
                new ZoneBattleCombatActivityAdapter(_zoneBattleManager),
                () => CurrentPlayerFactionId,
                () => System.Environment.TickCount);

            // Initialize AI manager for AI faction decisions
            var strategies = _container.Resolve<IDictionary<string, IAIStrategy>>();
            _aiManager = new AIManager(_factionService, _zoneService, strategies);
            _aiManager.Start();
            _aiManager.SetPlayerFactionId(CurrentPlayerFactionId);
            _aiManager.OnAIDecision += HandleAIDecision;

            // Wire background battle simulator for AI vs AI combat
            _backgroundBattleSimulator = _container.Resolve<BackgroundBattleSimulator>();
            _aiManager.OnAIDecision += _backgroundBattleSimulator.HandleAIDecision;

            // Initialize AI decision executor
            _aiDecisionExecutor = _container.Resolve<AIDecisionExecutor>();

            // Initialize consolidated AI controller
            _aiController = _container.Resolve<IAIController>();
            _aiController.SetPlayerFactionId(CurrentPlayerFactionId);
            _aiController.Start();

            // Initialize vehicle threat services for anti-vehicle response
            _vehicleThreatService = _container.Resolve<IVehicleThreatService>();
            _antiVehicleResponseService = _container.Resolve<IAntiVehicleResponseService>();

            // Initialize victory manager for victory condition checking
            var victoryConditionService = _container.Resolve<IVictoryConditionService>();
            var notificationService = _container.Resolve<INotificationService>();
            _victoryManager = new VictoryManager(victoryConditionService, _factionService, notificationService);
            _victoryManager.Start();

            // Initialize HUD renderers for combat and territory display
            _combatHudRenderer = new CombatHudRenderer();
            _territoryIndicatorRenderer = new TerritoryIndicatorRenderer();

            // Initialize play time HUD renderer (only in real game, not tests)
            // Tests use MockGameBridge, real game uses GameBridge
            if (_gameBridge.GetType().FullName == "FactionWars.ScriptHookV.GameBridge")
            {
                _playTimeHudRenderer = new PlayTimeHudRenderer();
            }

            // Event feed renderer for displaying world events
            _eventFeedRenderer = new EventFeedRenderer(_container.Resolve<IFactionRepository>());
            _eventFeedService = _container.Resolve<IEventFeedService>();

            // Wire territory events to combat manager
            _territoryManager.ZoneEntered += OnZoneEntered;
            _territoryManager.ZoneExited += OnZoneExited;

            // Wire neutral zone claim events
            _territoryManager.NeutralZoneEntered += OnNeutralZoneEntered;
            _territoryManager.ZoneExited += OnZoneExitedForClaim;

            // Initialize main menu controller for UI
            _menuProvider = _container.Resolve<IMenuProvider>();
            _mainMenuController = new MainMenuController(_menuProvider);

            // Initialize submenu controllers
            var playerContext = _container.Resolve<IPlayerContext>();
            var purchaseService = _container.Resolve<ITroopPurchaseService>();
            // allocationService already resolved above for FriendlyDefenderManager

            _overviewMenuController = new OverviewMenuController(
                _menuProvider,
                _factionService,
                _zoneService,
                playerContext);
            _overviewMenuController.BackRequested += (s, e) => _mainMenuController.OnKeyDown(MainMenuController.MenuToggleKeyCode);

            _zoneManagementMenuController = new ZoneManagementMenuController(
                _menuProvider,
                _factionService,
                _zoneService,
                playerContext,
                allocationService);
            _zoneManagementMenuController.BackRequested += (s, e) => _mainMenuController.OnKeyDown(MainMenuController.MenuToggleKeyCode);

            // Initialize recruitment menu hierarchy
            _recruitmentMenuController = new RecruitmentMenuController(_menuProvider, _gameBridge);
            _recruitmentMenuController.BackRequested += (s, e) => _mainMenuController.OnKeyDown(MainMenuController.MenuToggleKeyCode);

            _defendersMenuController = new DefendersMenuController(
                _menuProvider,
                _factionService,
                purchaseService,
                playerContext);
            _defendersMenuController.BackRequested += (s, e) => _recruitmentMenuController.Show();

            _squadMenuController = new SquadMenuController(
                _menuProvider,
                purchaseService,
                followerService,
                playerContext,
                _followerManager,
                _gameBridge);
            _squadMenuController.BackRequested += (s, e) => _recruitmentMenuController.Show();

            // Wire up recruitment submenu navigation
            _recruitmentMenuController.DefendersRequested += (s, e) => _defendersMenuController.Show();
            _recruitmentMenuController.SquadRequested += (s, e) => _squadMenuController.Show();

            // Initialize resources menu controller
            var resourceModifier = _container.Resolve<IZoneTraitResourceModifier>();
            var supplyLineService = _container.Resolve<ISupplyLineService>();
            _resourcesMenuController = new ResourcesMenuController(
                _menuProvider,
                _factionService,
                _zoneService,
                playerContext,
                _resourceTickService,
                resourceModifier,
                supplyLineService);
            _resourcesMenuController.BackRequested += (s, e) => _mainMenuController.OnKeyDown(MainMenuController.MenuToggleKeyCode);

            // Initialize settings menu controller
            _difficultyService = _container.Resolve<IDifficultyService>();

            // Apply initial difficulty settings to resource tick service
            _resourceTickService.SetAiIncomeMultiplier(_difficultyService.Current.AiIncomeMultiplier);
            _resourceTickService.SetTickInterval(_difficultyService.Current.TickIntervalSeconds);
            _resourceTickService.SetPlayerFactionId(CurrentPlayerFactionId);

            // Subscribe to difficulty changes to update resource tick service
            _difficultyService.DifficultyChanged += OnDifficultyChanged;

            _settingsMenuController = new SettingsMenuController(
                _menuProvider,
                _difficultyService,
                _gameBridge);
            _settingsMenuController.BackRequested += (s, e) => _mainMenuController.OnKeyDown(MainMenuController.MenuToggleKeyCode);

            // Initialize shop menu controller
            _shopMenuController = new ShopMenuController(_menuProvider, _gameBridge);
            _shopMenuController.BackRequested += (s, e) => _mainMenuController.OnKeyDown(MainMenuController.MenuToggleKeyCode);

            // Wire up main menu item selection to show submenus
            _menuProvider.ItemSelected += OnMainMenuItemSelected;

            // Mark the game as loaded so the state manager begins tracking play time.
            _gameStateManager = _container.Resolve<IGameStateManager>();
            _gameStateManager.NewGame();
            _gameStateManager.SetCurrentDifficulty(_difficultyService.Current.Level);

            // Subscribe to game state events for difficulty persistence
            _gameStateManager.OnGameLoaded += OnGameLoaded;

            InitializeTelemetryService();

            // Per-session GTA flags (e.g. don't drop weapons on death)
            ConfigureSessionSettings();

            FileLogger.Separator("INITIALIZATION COMPLETE");
            FileLogger.Info($"Player faction: {CurrentPlayerFactionId ?? "UNKNOWN"}");
            FileLogger.Info($"Log file: {FileLogger.LogPath}");
        }

        private void InitializeTelemetryService()
        {
            if (_gameStateManager == null || _zoneService == null)
            {
                FileLogger.Warn("TelemetryService not initialized: game state or zone service missing.");
                return;
            }

            var config = _container.Resolve<GameConfig>();
            var telemetryRoot = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                config.Persistence.SaveDirectoryName,
                "Telemetry");

            _telemetryService = new TelemetryService(
                _container.Resolve<ITelemetrySink>(),
                _factionService,
                _zoneService,
                _gameStateManager,
                new TelemetryServiceOptions
                {
                    GetPlayerPedHandle = () => _gameBridge.GetPlayerPedHandle(),
                    IsPlayerDead = () => _gameBridge.IsPlayerDead(),
                    GetCurrentZoneId = () => _territoryManager?.CurrentZone?.Id,
                    GetPlayerPosition = () => _gameBridge.GetPlayerPosition(),
                    IsFirstTimeSeenSave = save => !Directory.Exists(Path.Combine(telemetryRoot, save)),
                    ZoneBattleManager = _zoneBattleManager,
                    AIManager = _aiManager,
                    AIController = _aiController,
                    AllocationService = _allocationService,
                    ResourceTickService = _resourceTickService,
                    BattleAttackerManager = _battleAttackerManager,
                    VictoryManager = _victoryManager,
                    DifficultyService = _difficultyService,
                    NativeSaveWatcher = _container.Resolve<NativeSaveWatcher>()
                });
        }

        /// <summary>
        /// Applies per-session GTA flags. Must run every launch because GTA does
        /// not persist these across sessions. Does NOT touch weapons or cash —
        /// those are owned by the native save now.
        /// </summary>
        private void ConfigureSessionSettings()
        {
            _gameBridge.ConfigurePlayerSettings();
        }

        /// <summary>
        /// Syncs player state to their faction's economic state on character switch.
        /// Clears weapons and sets cash to faction capital.
        /// </summary>
        private void SyncPlayerToFactionState(string? factionId)
        {
            if (string.IsNullOrEmpty(factionId))
                return;

            // Clear weapons for fresh start
            _gameBridge.RemoveAllPlayerWeapons();

            // Get faction state and sync player's GTA money to faction's cash
            var factionState = _factionRepository.GetState(factionId);
            if (factionState != null)
            {
                var factionCash = factionState.Cash;
                _gameBridge.SetPlayerMoney(factionCash);
                FileLogger.Info($"SyncPlayerToFactionState: {factionId} - Set cash to faction's ${factionCash:N0}");
            }
            else
            {
                FileLogger.Warn($"SyncPlayerToFactionState: No state found for faction {factionId}");
            }

            // Configure weapon persistence
            _gameBridge.ConfigurePlayerSettings();
        }

        /// <summary>
        /// Handles item selection from the main menu to navigate to submenus.
        /// </summary>
        private void OnMainMenuItemSelected(object? sender, MenuItemSelectedEventArgs e)
        {
            if (e.MenuId != MainMenuController.MainMenuId)
                return;

            switch (e.ItemId)
            {
                case MainMenuController.ZoneManagementItemId:
                    _zoneManagementMenuController?.Show();
                    break;
                case MainMenuController.RecruitmentItemId:
                    _recruitmentMenuController?.Show();
                    break;
                case MainMenuController.ShopItemId:
                    _shopMenuController?.Show();
                    break;
                case MainMenuController.SettingsItemId:
                    _settingsMenuController?.Show();
                    break;
            }
        }

        /// <summary>
        /// Handles difficulty changed events from the difficulty service.
        /// Updates the resource tick service with the new difficulty settings.
        /// </summary>
        private void OnDifficultyChanged(object? sender, DifficultySettings settings)
        {
            _resourceTickService?.SetAiIncomeMultiplier(settings.AiIncomeMultiplier);
            _resourceTickService?.SetTickInterval(settings.TickIntervalSeconds);
            _gameStateManager?.SetCurrentDifficulty(settings.Level);
            FileLogger.Info($"Difficulty changed to {settings.Level}: AI={settings.AiIncomeMultiplier}x, Tick={settings.TickIntervalMinutes}min");

            // Show notification to player
            _gameBridge?.ShowNotification($"~b~Difficulty:~w~ {settings.Level} (AI {settings.AiIncomeMultiplier}x, {settings.TickIntervalMinutes}min ticks)");
        }

        /// <summary>
        /// Handles game loaded events from the game state manager.
        /// Restores difficulty settings from the loaded game state.
        /// </summary>
        private void OnGameLoaded(object? sender, GameStateLoadedEventArgs e)
        {
            if (!e.Success || _difficultyService == null || _gameStateManager == null)
                return;

            // Get the loaded game state to retrieve the difficulty
            var gameState = _gameStateManager.GetCurrentGameState();
            if (gameState != null)
            {
                _difficultyService.SetDifficulty(gameState.Difficulty);
                FileLogger.Info($"Restored difficulty from save: {gameState.Difficulty}");
            }
        }

        /// <summary>
        /// Polls controller (gamepad) input each frame and triggers the same actions as keyboard keys.
        /// </summary>
        private void PollControllerInput()
        {
            if (!_isInitialized) return;

            // LB + D-pad Right = Toggle menu (same as F7)
            if (_gameBridge.IsControlJustPressed(ControlDpadRight) && _gameBridge.IsControlPressed(ControlLB))
            {
                _mainMenuController?.OnKeyDown(MainMenuController.MenuToggleKeyCode);
                return; // Don't also process D-pad Right as claim
            }

            // D-pad Right (no LB) = Claim zone (same as E key)
            if (_gameBridge.IsControlJustPressed(ControlDpadRight) && !_gameBridge.IsControlPressed(ControlLB))
            {
                if (_showingClaimPrompt && _currentNeutralZone != null)
                {
                    TryClaimNeutralZone();
                }
                // Also pass to commander manager for E key interaction
                _commanderManager?.OnKeyDown(ClaimKeyCode);
            }

            // D-pad Down = Cycle battle HUD (same as B key)
            if (_gameBridge.IsControlJustPressed(ControlDpadDown))
            {
                if (_zoneBattleManager != null)
                {
                    var battles = _zoneBattleManager.GetAllActiveBattles();
                    if (battles.Count > 1)
                    {
                        _currentBattleHudIndex = (_currentBattleHudIndex + 1) % battles.Count;
                    }
                }
            }

            // A button held = Menu hold-to-repeat (same as Enter held)
            if (_gameBridge.IsControlPressed(ControlFrontendAccept))
            {
                _enterKeyHeld = true;
            }
        }

        /// <summary>
        /// Called when a key is pressed.
        /// </summary>
        /// <param name="keyCode">The virtual key code of the pressed key.</param>
        public void OnKeyDown(int keyCode)
        {
            if (!_isInitialized)
                return;

            // Track Enter key for menu hold-to-repeat
            if (keyCode == EnterKeyCode)
            {
                _enterKeyHeld = true;
            }

            // Handle claim key when in neutral zone
            if (keyCode == ClaimKeyCode && _showingClaimPrompt && _currentNeutralZone != null)
            {
                TryClaimNeutralZone();
                return;
            }

            // Handle battle HUD cycle key (B)
            if (keyCode == BattleCycleKeyCode && _zoneBattleManager != null)
            {
                var battles = _zoneBattleManager.GetAllActiveBattles();
                if (battles.Count > 1)
                {
                    _currentBattleHudIndex = (_currentBattleHudIndex + 1) % battles.Count;
                }
                return;
            }

            // Pass key events to the main menu controller
            _mainMenuController?.OnKeyDown(keyCode);

            // Pass key events to commander manager (for E key interaction)
            _commanderManager?.OnKeyDown(keyCode);
        }

        /// <summary>
        /// Called when a key is released.
        /// </summary>
        /// <param name="keyCode">The virtual key code of the released key.</param>
        public void OnKeyUp(int keyCode)
        {
            // Track Enter key release for menu hold-to-repeat
            if (keyCode == EnterKeyCode)
            {
                _enterKeyHeld = false;
            }
        }

        /// <summary>
        /// Called when the script is aborted/unloaded.
        /// </summary>
        public void OnAbort()
        {
            _isInitialized = false;
            _characterSwitchInitialized = false;

            // Unsubscribe from events
            _characterSwitchDetector.OnCharacterSwitched -= HandleCharacterSwitched;

            _telemetryService?.Dispose();
            _telemetryService = null;
            if (_container.TryResolve<ITelemetrySink>(out var telemetrySink) && telemetrySink != null)
            {
                telemetrySink.Dispose();
            }

            // Stop economy manager
            _economyManager?.Stop();
            _economyManager = null;

            // Clean up map blips
            _mapBlipManager?.Dispose();
            _mapBlipManager = null;

            _zoneBoundaryBlipManager?.Dispose();
            _zoneBoundaryBlipManager = null;

            // Unsubscribe from territory events and clean up
            if (_territoryManager != null)
            {
                _territoryManager.ZoneEntered -= OnZoneEntered;
                _territoryManager.ZoneExited -= OnZoneExited;
                _territoryManager.NeutralZoneEntered -= OnNeutralZoneEntered;
                _territoryManager.ZoneExited -= OnZoneExitedForClaim;
                _territoryManager = null;
            }

            _defenderRallyController = null;

            // Unsubscribe from AI events and stop AI manager
            if (_aiManager != null && _backgroundBattleSimulator != null)
            {
                _aiManager.OnAIDecision -= _backgroundBattleSimulator.HandleAIDecision;
            }
            _backgroundBattleSimulator = null;
            _aiDecisionExecutor = null;

            if (_aiManager != null)
            {
                _aiManager.OnAIDecision -= HandleAIDecision;
                _aiManager.Stop();
            }
            _aiManager = null;

            // Stop consolidated AI controller
            _aiController?.Stop();
            _aiController = null;

            // Stop victory manager
            _victoryManager?.Stop();
            _victoryManager = null;

            // Cleanup zone battle manager
            if (_zoneBattleManager != null)
            {
                _zoneBattleManager.BattleEnded -= OnZoneBattleEnded;
                _zoneBattleManager.TroopKilled -= OnZoneBattleTroopKilled;
                _zoneBattleManager.BattleStarted -= OnZoneBattleStarted;
            }
            _zoneBattleManager = null;
            _battleHudRenderer = null;
            _currentBattleHudIndex = 0;

            // Clean up follower manager
            _followerManager = null;
            _followerService = null;

            // Clean up friendly defender manager
            _friendlyDefenderManager?.DespawnAllDefenders();
            _friendlyDefenderManager = null;

            // Clean up enemy defender manager
            _enemyDefenderManager?.DespawnAllDefenders();
            _enemyDefenderManager = null;

            // Clean up battle attacker manager
            _battleAttackerManager?.DespawnAllAttackers();
            _battleAttackerManager = null;

            // Clean up event feed renderer and service
            _eventFeedRenderer = null;
            _eventFeedService = null;

            // Unsubscribe from difficulty events
            if (_difficultyService != null)
            {
                _difficultyService.DifficultyChanged -= OnDifficultyChanged;
            }
            _difficultyService = null;

            // Unsubscribe from game state manager events
            if (_gameStateManager != null)
            {
                _gameStateManager.OnGameLoaded -= OnGameLoaded;
            }
            _gameStateManager = null;
        }

        /// <summary>
        /// Handles character switch events from the detector.
        /// Shows a notification, dismisses followers for the old faction, and raises the public event.
        /// </summary>
        private void HandleCharacterSwitched(string? oldFactionId, string? newFactionId)
        {
            // Dismiss all followers for the old faction when switching characters
            if (!string.IsNullOrEmpty(oldFactionId) && _followerManager != null)
            {
                _followerManager.DismissAllFollowers(oldFactionId);
            }

            // Despawn all friendly defenders and update faction for the new character
            if (_friendlyDefenderManager != null)
            {
                _friendlyDefenderManager.DespawnAllDefenders();
                if (!string.IsNullOrEmpty(newFactionId))
                {
                    _friendlyDefenderManager.SetPlayerFaction(newFactionId);
                }
            }

            // Despawn all enemy defenders and battle attackers - they have stale
            // relationship groups that were configured against the OLD player
            // character's group. New peds will spawn fresh with correct
            // relationships when the player enters zones.
            FileLogger.Spawn("HandleCharacterSwitched: Despawning all enemy defenders (stale relationships)");
            _enemyDefenderManager?.DespawnAllDefenders();
            FileLogger.Spawn("HandleCharacterSwitched: Despawning all battle attackers (stale relationships)");
            _battleAttackerManager?.DespawnAllAttackers();

            // Update battle attacker manager faction
            if (!string.IsNullOrEmpty(newFactionId))
            {
                _battleAttackerManager?.SetPlayerFaction(newFactionId);
            }

            // Update managers with new faction
            FileLogger.Info($"HandleCharacterSwitched: Updating all managers to new faction: {newFactionId}");
            _economyManager?.SetPlayerFactionId(newFactionId);
            _aiManager?.SetPlayerFactionId(newFactionId);
            _aiController?.SetPlayerFactionId(newFactionId);
            _zoneBattleManager?.SetPlayerFaction(newFactionId);

            // Sync player state to new faction (clear weapons, set cash to faction capital)
            SyncPlayerToFactionState(newFactionId);

            // Show notification to player
            var newCharacterName = GetCharacterDisplayName(newFactionId);
            _gameBridge.ShowNotification($"~b~FactionWars:~w~ Switched to {newCharacterName}'s faction");

            // Raise the public event for other systems to respond
            OnCharacterSwitched?.Invoke(oldFactionId, newFactionId);
            FileLogger.Info("HandleCharacterSwitched: Complete");
        }

        /// <summary>
        /// Gets the display name for a character based on their faction ID.
        /// </summary>
        private static string GetCharacterDisplayName(string? factionId)
        {
            return factionId switch
            {
                CharacterModelFactionDetector.MichaelFactionId => "Michael",
                CharacterModelFactionDetector.FranklinFactionId => "Franklin",
                CharacterModelFactionDetector.TrevorFactionId => "Trevor",
                _ => "Unknown"
            };
        }

        /// <summary>
        /// Called when the player enters a zone.
        /// Triggers combat if entering enemy territory.
        /// </summary>
        private void OnZoneEntered(object? sender, Zone zone)
        {
            FileLogger.Separator("ZONE ENTERED");
            FileLogger.Zone($"OnZoneEntered triggered");

            if (_zoneBattleManager == null || zone == null)
            {
                FileLogger.Error($"OnZoneEntered: zoneBattleManager={_zoneBattleManager != null}, zone={zone != null}");
                return;
            }

            // Tell battle simulator to skip battles in player's current zone
            _backgroundBattleSimulator?.SetPlayerZone(zone.Id);
            _aiController?.SetPlayerZone(zone.Id);

            FileLogger.Zone($"Zone: {zone.Name} (ID: {zone.Id})");
            FileLogger.Zone($"Zone Owner: {zone.OwnerFactionId ?? "NULL/NONE"}");
            FileLogger.Zone($"Zone Center: ({zone.Center.X:F1}, {zone.Center.Y:F1}, {zone.Center.Z:F1}), Radius: {zone.Radius}");

            var playerFactionId = CurrentPlayerFactionId;
            FileLogger.Zone($"Player Faction: {playerFactionId ?? "NULL"}");

            if (string.IsNullOrEmpty(playerFactionId))
            {
                FileLogger.Error("No player faction detected!");
                _gameBridge.ShowNotification("~r~DEBUG: No player faction detected!");
                return;
            }

            // Debug: Show zone info
            _gameBridge.ShowNotification($"~b~Entered:~w~ {zone.Name} (Owner: {zone.OwnerFactionId ?? "NONE"})");

            // Check if this is enemy territory
            bool isEnemyTerritory = zone.OwnerFactionId != null && zone.OwnerFactionId != playerFactionId;
            FileLogger.Zone($"Is Enemy Territory: {isEnemyTerritory}");

            if (isEnemyTerritory)
            {
                FileLogger.Combat($"Starting combat in {zone.Name}");

                // Add combat started event to event feed
                if (_eventFeedService != null)
                {
                    var attackerFaction = _factionService.GetFaction(playerFactionId);
                    var defenderFaction = _factionService.GetFaction(zone.OwnerFactionId!);
                    _eventFeedService.AddCombatStarted(
                        zone.Name,
                        attackerFaction?.Name ?? "Player",
                        defenderFaction?.Name ?? "Defender");
                }

                // Start combat in enemy zone via ZoneBattleManager.
                Func<int> aliveCountCallback = () => GetPlayerCombatAliveCount(playerFactionId);
                var battle = _zoneBattleManager.StartPlayerCombat(zone, playerFactionId, aliveCountCallback);
                if (battle == null)
                {
                    FileLogger.Combat($"OnZoneEntered: StartPlayerCombat returned null for zone {zone.Id} — caller skipping.");
                    return;
                }
                FileLogger.Combat($"Combat battle created: ID={battle.Id}");
                FileLogger.Combat($"Defending Faction: {battle.Defender.FactionId}");
                _gameBridge.ShowNotification($"~r~COMBAT STARTED in:~w~ {zone.Name}");

                // Check for vehicle threat and allocate Elite anti-vehicle units BEFORE spawning
                // This ensures Elite units are included in the allocation when spawning begins
                CheckAndRespondToVehicleThreat(zone, zone.OwnerFactionId!);

                // Spawn enemy defenders using EnemyDefenderManager (wander + engage behavior)
                // This includes any Elite units allocated by the vehicle threat response
                _enemyDefenderManager?.OnEnemyZoneEntered(zone, zone.OwnerFactionId!);
            }
            else if (zone.OwnerFactionId == null)
            {
                FileLogger.Zone($"{zone.Name} is NEUTRAL");
                _gameBridge.ShowNotification($"~y~{zone.Name} is NEUTRAL (no owner)");
            }
            else
            {
                FileLogger.Zone($"{zone.Name} is FRIENDLY (player owns)");
                _gameBridge.ShowNotification($"~g~{zone.Name} is YOUR territory");
            }

            // Notify zone battle manager that player entered this zone
            _zoneBattleManager?.OnPlayerEnteredZone(zone);
        }

        /// <summary>
        /// Checks if the player is in a vehicle and responds to vehicle threats by deploying Elite units.
        /// </summary>
        /// <param name="zone">The enemy zone entered.</param>
        /// <param name="enemyFactionId">The faction defending the zone.</param>
        private void CheckAndRespondToVehicleThreat(Zone zone, string enemyFactionId)
        {
            // Check if services are available
            if (_vehicleThreatService == null || _antiVehicleResponseService == null)
            {
                FileLogger.AI("CheckAndRespondToVehicleThreat: Vehicle threat services not initialized");
                return;
            }

            // Check if player is in a vehicle
            if (!_gameBridge.IsPlayerInVehicle())
            {
                FileLogger.AI("CheckAndRespondToVehicleThreat: Player is not in a vehicle, no threat response needed");
                return;
            }

            // Get the player's vehicle
            int vehicleHandle = _gameBridge.GetPlayerVehicle();
            if (vehicleHandle <= 0)
            {
                FileLogger.AI($"CheckAndRespondToVehicleThreat: Invalid vehicle handle ({vehicleHandle})");
                return;
            }

            // Get vehicle model name
            string vehicleModel = _gameBridge.GetVehicleModelName(vehicleHandle);
            if (string.IsNullOrEmpty(vehicleModel))
            {
                FileLogger.AI("CheckAndRespondToVehicleThreat: Could not get vehicle model name");
                return;
            }

            FileLogger.AI($"CheckAndRespondToVehicleThreat: Player vehicle detected - model={vehicleModel}");

            // Get threat level
            var threatLevel = _vehicleThreatService.GetThreatLevel(vehicleModel);
            FileLogger.AI($"CheckAndRespondToVehicleThreat: Threat level for {vehicleModel} = {threatLevel}");

            // If no threat, don't deploy
            if (threatLevel == VehicleThreatLevel.None)
            {
                FileLogger.AI("CheckAndRespondToVehicleThreat: No significant threat, skipping Elite deployment");
                return;
            }

            // Deploy Elite units as anti-vehicle response
            FileLogger.AI($"CheckAndRespondToVehicleThreat: Deploying Elite units for {threatLevel} threat in zone {zone.Id}");
            int deployed = _antiVehicleResponseService.RespondToVehicleThreat(enemyFactionId, zone.Id, threatLevel);

            if (deployed > 0)
            {
                FileLogger.Combat($"CheckAndRespondToVehicleThreat: Allocated {deployed} Elite RPG defenders against {vehicleModel} ({threatLevel} threat)");
                _gameBridge.ShowNotification($"~o~Enemy deploying {deployed} RPG units against your {vehicleModel}!");
                // Elite units will be spawned by the subsequent call to OnEnemyZoneEntered
            }
            else
            {
                FileLogger.AI($"CheckAndRespondToVehicleThreat: Failed to allocate Elite units (insufficient funds or reserves)");
            }
        }

        /// <summary>
        /// Called when the player exits a zone.
        /// May end combat if leaving the contested zone.
        /// </summary>
        private void OnZoneExited(object? sender, Zone zone)
        {
            // Clear player zone tracking
            _backgroundBattleSimulator?.SetPlayerZone(null);
            _aiController?.SetPlayerZone(null);

            // Notify zone battle manager that player exited this zone
            _zoneBattleManager?.OnPlayerExitedZone(zone);

            if (zone == null)
                return;

            // If exiting an enemy zone, despawn enemy defenders
            if (zone.OwnerFactionId != null && zone.OwnerFactionId != CurrentPlayerFactionId)
            {
                _enemyDefenderManager?.OnEnemyZoneExited(zone);
            }

            // If we were in combat in this zone, remove the player as a participant (retreat)
            if (_zoneBattleManager != null && _zoneBattleManager.IsPlayerInBattle())
            {
                var currentBattle = _zoneBattleManager.GetPlayerCurrentBattle();
                string? playerFactionId = CurrentPlayerFactionId;
                if (currentBattle != null && currentBattle.ZoneId == zone.Id && playerFactionId is { Length: > 0 })
                {
                    _zoneBattleManager.RemoveParticipant(zone.Id, playerFactionId);
                    _gameBridge.ShowNotification($"~y~Retreated from:~w~ {zone.Name}");
                }
            }
        }

        /// <summary>
        /// Called when the player enters a neutral (unowned) zone.
        /// Shows the claim prompt.
        /// </summary>
        private void OnNeutralZoneEntered(object? sender, Zone zone)
        {
            _currentNeutralZone = zone;
            _showingClaimPrompt = true;

            var cost = GetBasicTroopCost();
            _gameBridge.ShowNotification($"~y~Unclaimed territory: {zone.Name}");
        }

        /// <summary>
        /// Called when the player exits a zone (for claim state tracking).
        /// Clears the claim prompt if exiting the current neutral zone.
        /// </summary>
        private void OnZoneExitedForClaim(object? sender, Zone zone)
        {
            if (_currentNeutralZone?.Id == zone.Id)
            {
                _currentNeutralZone = null;
                _showingClaimPrompt = false;
            }
        }

        /// <summary>
        /// Gets the cost of a basic troop from the defender tier service.
        /// </summary>
        private int GetBasicTroopCost()
        {
            var tierService = _container.Resolve<IDefenderTierService>();
            return tierService.GetTierConfig(DefenderTier.Basic).Cost;
        }

        /// <summary>
        /// Attempts to claim the current neutral zone by paying for a guard troop.
        /// </summary>
        private void TryClaimNeutralZone()
        {
            if (_currentNeutralZone == null) return;

            var cost = GetBasicTroopCost();
            var playerMoney = _gameBridge.GetPlayerMoney();
            var playerFaction = CurrentPlayerFactionId;

            if (playerMoney < cost)
            {
                _gameBridge.ShowNotification($"~r~Not enough cash! Need ${cost}");
                return;
            }

            // Store zone ID before clearing
            var zoneId = _currentNeutralZone.Id;
            var zoneName = _currentNeutralZone.Name;

            // Cancel any existing AI simulated battle for this zone
            // This prevents AI battles from continuing after the player captures a zone
            var existingBattle = _zoneBattleManager?.GetBattleForZone(zoneId);
            if (existingBattle != null)
            {
                FileLogger.Combat($"Cancelling existing battle in {zoneName} - player claimed zone");
                _zoneBattleManager!.EndBattle(zoneId, BattleOutcome.Cancelled);
            }

            // Deduct cost
            _gameBridge.AddPlayerMoney(-cost);

            // Transfer ownership
            _zoneService!.TransferZoneOwnership(zoneId, playerFaction);

            // Allocate 1 Basic troop
            var allocationService = _container.Resolve<IZoneDefenderAllocationService>();
            allocationService.SetAllocation(playerFaction!, zoneId, DefenderTier.Basic, 1);

            _gameBridge.ShowNotification($"~g~You now control {zoneName}!");

            // Clear prompt state
            _currentNeutralZone = null;
            _showingClaimPrompt = false;

            // Spawn defender and commander immediately
            // Get the updated zone (with new OwnerFactionId) from the service
            var claimedZone = _zoneService.GetZone(zoneId);
            if (claimedZone != null)
            {
                // Spawn friendly defender(s)
                _friendlyDefenderManager?.OnZoneEntered(claimedZone);

                // Spawn commander
                _commanderManager?.OnZoneEntered(claimedZone);
            }
        }

        /// <summary>
        /// Handles AI faction decisions.
        /// Routes through the decision executor for budget enforcement.
        /// </summary>
        private void HandleAIDecision(object? sender, AIDecisionEventArgs e)
        {
            // Route through decision executor for budget enforcement
            _aiDecisionExecutor?.ProcessDecisionCycle(e.FactionId, e.Decision);
        }

        /// <summary>
        /// Handles troop killed events from ZoneBattleManager for the kill feed.
        /// </summary>
        private void OnZoneBattleTroopKilled(ZoneBattle battle, DefenderTier tier, string side)
        {
            // Determine killer and victim based on who got killed
            string killerFactionId = side == "attacker" ? battle.DefenderFactionId : battle.AttackerFactionId;
            string victimFactionId = side == "attacker" ? battle.AttackerFactionId : battle.DefenderFactionId;

            var killerFaction = _factionService.GetFaction(killerFactionId);
            var victimFaction = _factionService.GetFaction(victimFactionId);

            string killerName = killerFaction?.Name ?? killerFactionId;
            string victimName = victimFaction?.Name ?? victimFactionId;

            var zone = _zoneService?.GetZone(battle.ZoneId);
            string zoneName = zone?.Name ?? battle.ZoneId;

            // Format: "[Ballas] killed [Grove St] Basic in Davis"
            string message = $"~y~[{killerName}]~w~ killed ~r~[{victimName}]~w~ {tier} in {zoneName}";
            _gameBridge.ShowNotification(message);
        }

        /// <summary>
        /// Handles battle ended events from ZoneBattleManager.
        /// Performs domain operations: applies casualties, transfers zone ownership, allocates defenders.
        /// </summary>
        private void OnZoneBattleEnded(ZoneBattle battle, BattleOutcome outcome)
        {
            _commanderManager?.OnBattleEnded(battle.ZoneId);

            // Mirror of OnZoneBattleStarted's IsContested=true. The
            // TransferZoneOwnership path also clears this, but only fires on
            // capture/neutralize — so a defended outcome (no ownership change)
            // would otherwise leave the blip flashing forever.
            _zoneService?.SetZoneContested(battle.ZoneId, false);

            // Identify the surviving participant (if any). Source of truth for
            // outcome routing in 3-way battles — Attackers[0] may be the wiped one.
            var winner = battle.Participants.FirstOrDefault(p => p.AliveCount > 0);
            bool playerWon = winner?.IsPlayer == true;

            // Resolve names from the participant list rather than the legacy
            // AttackerFactionId getter, which throws when Attackers is empty
            // (e.g. the player retreated as the only attacker).
            string? someAttackerFactionId =
                battle.Participants.FirstOrDefault(p => p.Role == BattleRole.Attacker)?.FactionId;
            string defenderFactionId = battle.DefenderFactionId;

            // Casualty debits only apply to AI factions — player troop count is
            // callback-based (1 + alive followers) and not pool-managed. We can
            // only debit attacker losses if there's still an attacker we know
            // about; if all attackers retreated, their losses are unaccounted
            // (matches prior behaviour for the retreat path).
            int attackerCasualties = battle.InitialAttackerTroops - battle.TotalAttackerTroops;
            int defenderCasualties = battle.InitialDefenderTroops - battle.TotalDefenderTroops;
            if (attackerCasualties > 0
                && someAttackerFactionId != null
                && someAttackerFactionId != CurrentPlayerFactionId)
            {
                _factionService.LoseTroops(someAttackerFactionId, attackerCasualties);
            }
            if (defenderCasualties > 0 && defenderFactionId != CurrentPlayerFactionId)
            {
                _factionService.LoseTroops(defenderFactionId, defenderCasualties);
            }

            if (outcome == BattleOutcome.AttackersWon && _zoneService != null && !playerWon)
            {
                // AI attacker wins — use the actual surviving participant's faction id
                // (in 3-way battles Attackers[0] may be the wiped AI, not the winner).
                string? winnerFactionId = winner?.FactionId ?? someAttackerFactionId;
                if (winnerFactionId != null)
                {
                    _zoneService.TransferZoneOwnership(battle.ZoneId, winnerFactionId);

                    int survivors = winner?.AliveCount ?? battle.TotalAttackerTroops;
                    int toAllocate = survivors > 0 ? Math.Max(1, Math.Min((survivors + 1) / 2, 5)) : 0;
                    if (toAllocate > 0)
                    {
                        _allocationService?.SetAllocation(winnerFactionId, battle.ZoneId, DefenderTier.Basic, toAllocate);
                    }

                    CheckFactionEliminated(defenderFactionId);
                }
            }
            // Player wins: ZoneBattleManager.ApplyBattleOutcome already neutralized
            // the zone via TransferZoneOwnership(zoneId, null). Re-transferring here
            // would defeat Q5.A.

            string attackerName = someAttackerFactionId != null
                ? (_factionService.GetFaction(someAttackerFactionId)?.Name ?? someAttackerFactionId)
                : "Attacker";
            var defenderFaction = _factionService.GetFaction(defenderFactionId);
            string defenderName = defenderFaction?.Name ?? defenderFactionId;
            var zone = _zoneService?.GetZone(battle.ZoneId);
            string zoneName = zone?.Name ?? battle.ZoneId;

            if (playerWon)
            {
                _gameBridge.ShowNotification($"~g~[{attackerName}]~w~ liberated ~b~{zoneName}~w~ — now neutral");
                FileLogger.Combat($"OnZoneBattleEnded: player liberated {zoneName} (now neutral)");
            }
            else if (outcome == BattleOutcome.AttackersWon)
            {
                string? winnerFactionIdForName = winner?.FactionId ?? someAttackerFactionId;
                string winnerName = winnerFactionIdForName != null
                    ? (_factionService.GetFaction(winnerFactionIdForName)?.Name ?? winnerFactionIdForName)
                    : attackerName;
                _gameBridge.ShowNotification($"~g~[{winnerName}]~w~ captured ~b~{zoneName}~w~ from ~r~[{defenderName}]");
                FileLogger.Combat($"OnZoneBattleEnded: {winnerName} captured {zoneName} from {defenderName}");
            }
            else
            {
                _gameBridge.ShowNotification($"~g~[{defenderName}]~w~ defended ~b~{zoneName}~w~ against ~r~[{attackerName}]");
                FileLogger.Combat($"OnZoneBattleEnded: {defenderName} defended {zoneName} against {attackerName}");
            }
        }

        /// <summary>
        /// Checks if a faction has been eliminated (lost all territory) and shows a notification if so.
        /// </summary>
        private void CheckFactionEliminated(string factionId)
        {
            if (string.IsNullOrEmpty(factionId) || _zoneService == null)
                return;

            var zoneCount = _zoneService.GetZoneCount(factionId);
            if (zoneCount == 0)
            {
                var faction = _factionService.GetFaction(factionId);
                var factionName = faction?.Name ?? factionId;
                _gameBridge.ShowNotification($"~r~[{factionName}]~w~ has been eliminated!");
                FileLogger.Combat($"CheckFactionEliminated: {factionName} has been eliminated (0 zones remaining)");
            }
        }

        /// <summary>
        /// Handles battle started events. If player is in the zone being attacked
        /// and is the defender, spawns enemy attackers immediately.
        /// </summary>
        private void OnZoneBattleStarted(ZoneBattle battle)
        {
            // Mark the zone contested so the minimap blip flashes red/white.
            // CombatResultHandler clears this when the battle resolves.
            _zoneService?.SetZoneContested(battle.ZoneId, true);

            // Notify commander manager to switch to sprinting wander
            _commanderManager?.OnBattleStarted(battle.ZoneId);

            // Check if player is in this zone
            var currentZone = _territoryManager?.CurrentZone;
            if (currentZone == null || currentZone.Id != battle.ZoneId)
                return;

            // Check if player is the defender
            if (battle.DefenderFactionId != CurrentPlayerFactionId)
                return;

            FileLogger.Combat($"OnZoneBattleStarted: Player is in zone {battle.ZoneId} as defender, triggering attacker spawn");

            // Spawn attackers immediately since player is already in zone
            _battleAttackerManager?.OnPlayerZoneEntered(currentZone);
        }

        /// <summary>
        /// Configures spawned defender peds with weapons, stats, and combat behavior.
        /// Makes them hostile to the player and ready to fight.
        /// </summary>
        /// <param name="spawnedPeds">The list of spawned ped handles.</param>
        /// <param name="tier">The defender tier for stat configuration.</param>
        /// <param name="defenderFactionId">The faction ID of the defenders.</param>
        private void ConfigureSpawnedDefenders(IList<PedHandle> spawnedPeds, DefenderTier tier, string defenderFactionId)
        {
            FileLogger.Combat($"ConfigureSpawnedDefenders: {spawnedPeds.Count} peds, tier={tier}, faction={defenderFactionId}");

            var defenderTierService = _container.Resolve<IDefenderTierService>();
            var config = defenderTierService.GetTierConfig(tier);

            // Map weapon names to GTA V weapon names
            var weaponName = tier switch
            {
                DefenderTier.Basic => "WEAPON_PISTOL",
                DefenderTier.Medium => "WEAPON_SMG",
                DefenderTier.Heavy => "WEAPON_CARBINERIFLE",
                _ => "WEAPON_PISTOL"
            };

            foreach (var ped in spawnedPeds)
            {
                if (!ped.IsValid)
                {
                    FileLogger.Warn($"Skipping invalid ped handle {ped.Handle}");
                    continue;
                }

                FileLogger.Combat($"Configuring defender ped {ped.Handle}");

                // Set health and armor based on tier
                _gameBridge.SetPedHealth(ped.Handle, config.Health);
                _gameBridge.SetPedArmor(ped.Handle, config.Armor);

                // Give weapon
                _gameBridge.GivePedWeapon(ped.Handle, weaponName);

                // Set accuracy
                _gameBridge.SetPedAccuracy(ped.Handle, config.Accuracy);

                // Set combat attributes - make them aggressive fighters
                _gameBridge.SetPedCombatAttributes(ped.Handle, canUseCover: true, willFightArmedPeds: true);

                // Set hostile relationship to player
                SetPedHostileToPlayer(ped.Handle, defenderFactionId);

                // CRITICAL: Give defender a task to fight the player
                _gameBridge.SetPedToAttackPlayer(ped.Handle);

                FileLogger.Combat($"Defender {ped.Handle} configured with weapon={weaponName}, health={config.Health}, accuracy={config.Accuracy}");
            }
        }

        /// <summary>
        /// Sets up a ped to be hostile to the player by configuring relationship groups.
        /// </summary>
        /// <param name="pedHandle">The ped handle.</param>
        /// <param name="factionId">The faction ID for the relationship group.</param>
        private void SetPedHostileToPlayer(int pedHandle, string factionId)
        {
            // The ped's relationship group is already set by PedSpawningService
            // We need to make that group hostile to the player's group
            // This is done via native calls to SET_RELATIONSHIP_BETWEEN_GROUPS

            // Get player faction to set up hostility
            var playerFactionId = CurrentPlayerFactionId ?? "";
            if (string.IsNullOrEmpty(playerFactionId) || string.IsNullOrEmpty(factionId))
                return;

            // Set relationship groups to be enemies using native call through GameBridge
            // Relationship types: 0=Companion, 1=Respect, 2=Like, 3=Neutral, 4=Dislike, 5=Hate
            SetFactionRelationship(factionId, playerFactionId, 5); // Defenders hate player
            SetFactionRelationship(playerFactionId, factionId, 5); // Player faction hates defenders
        }

        /// <summary>
        /// Sets up faction relationship between two groups.
        /// </summary>
        private void SetFactionRelationship(string factionId1, string factionId2, int relationship)
        {
            try
            {
                // This uses native function through game bridge extension
                // The relationship will make peds attack each other
                var group1 = factionId1.ToUpperInvariant();
                var group2 = factionId2.ToUpperInvariant();

                // Use GTA native: SET_RELATIONSHIP_BETWEEN_GROUPS(int relationship, Hash group1, Hash group2)
                GTA.Native.Function.Call(
                    GTA.Native.Hash.SET_RELATIONSHIP_BETWEEN_GROUPS,
                    relationship,
                    GTA.World.AddRelationshipGroup(group1),
                    GTA.World.AddRelationshipGroup(group2));
            }
            catch
            {
                // Silently ignore - relationship setup failed but combat may still work
            }
        }
    }

}
