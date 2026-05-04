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
    /// <summary>
    /// Controller for the game loop logic. This class contains all testable game logic
    /// that FactionWarsScript delegates to. Separating this from GTA.Script allows
    /// for unit testing without ScriptHookVDotNet dependencies.
    /// </summary>
    public partial class GameLoopController
    {
        private readonly IServiceContainer _container;
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
        private bool _hasReadPlayerDeathState;
        private bool _wasPlayerDead;
        private string? _pendingOwnedTerritoryFactionId;
        private string? _pendingOwnedTerritoryReason;
        private int _pendingOwnedTerritoryAttempts;
        private bool _pendingOwnedTerritoryLoggedSuccess;
        private bool _pendingOwnedTerritoryWaitingForControlLogged;
        private const int OwnedTerritoryPlacementRetryTicks = 300;

        private const float ThreatDecayInterval = 60f;  // Decay every 60 seconds
        private const float ThreatDecayRate = 0.1f;     // 10% decay per interval

        // AI recruitment tracking
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
        public IServiceContainer ServiceContainer => _container;

        /// <summary>
        /// Gets the current player's faction ID based on their character model.
        /// Returns null if the detector hasn't been initialized or the character is unknown.
        /// </summary>
        public string? CurrentPlayerFactionId => _characterSwitchDetector.CurrentFactionId;

        private FriendlyDefenderManager RequiredFriendlyDefenderManager =>
            _friendlyDefenderManager ?? throw new InvalidOperationException("Friendly defender manager has not been initialized.");

        private TerritoryManager RequiredTerritoryManager =>
            _territoryManager ?? throw new InvalidOperationException("Territory manager has not been initialized.");

        private IZoneBattleManager RequiredZoneBattleManager =>
            _zoneBattleManager ?? throw new InvalidOperationException("Zone battle manager has not been initialized.");

        private IZoneService RequiredZoneService =>
            _zoneService ?? throw new InvalidOperationException("Zone service has not been initialized.");

        private IMenuProvider RequiredMenuProvider =>
            _menuProvider ?? throw new InvalidOperationException("Menu provider has not been initialized.");

        private MainMenuController RequiredMainMenuController =>
            _mainMenuController ?? throw new InvalidOperationException("Main menu controller has not been initialized.");

        private IDifficultyService RequiredDifficultyService =>
            _difficultyService ?? throw new InvalidOperationException("Difficulty service has not been initialized.");

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
        public GameLoopController(IServiceContainer container)
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
                RequestOwnedTerritoryPlacement(CurrentPlayerFactionId, "initial-load");
            }

            // Check for character switches
            _characterSwitchDetector.CheckForSwitch();

            // Calculate delta time for this frame
            var now = DateTime.UtcNow;
            var deltaTime = (float)(now - _lastTickTime).TotalSeconds;
            _lastTickTime = now;

            UpdateCoreSystems(deltaTime);
            UpdatePlayerRespawnPlacement();
            PollControllerInput();
            _menuProvider?.SetSelectKeyHeld(_enterKeyHeld);
            _mainMenuController?.Update();
            UpdateWorldSystems(deltaTime);

            try
            {
                UpdateAndDrawHud();
            }
            catch (Exception ex)
            {
                FileLogger.Error("UpdateAndDrawHud failed", ex);
            }

            ProcessPendingOwnedTerritoryPlacement();
        }

        private void UpdateCoreSystems(float deltaTime)
        {
            _economyManager?.Update(deltaTime);
            _gameStateManager?.UpdatePlayTime(deltaTime);
            _telemetryService?.Update(deltaTime);
        }

        private void UpdateWorldSystems(float deltaTime)
        {
            _mapBlipManager?.UpdateBlipColors();
            _territoryManager?.Update();
            _aiController?.Update(deltaTime);
            _zoneBattleManager?.Tick(deltaTime);
            _victoryManager?.Update(deltaTime);
            _followerManager?.Update(CurrentPlayerFactionId ?? "");
            _friendlyDefenderManager?.Update();
            _defenderRallyController?.Update();
            _commanderManager?.Update();
            var currentZone = _territoryManager?.CurrentZone;
            var enemyFactionId = currentZone?.OwnerFactionId;
            if (enemyFactionId != null && enemyFactionId != CurrentPlayerFactionId)
            {
                _enemyDefenderManager?.Update(enemyFactionId);
            }

            _battleAttackerManager?.Update();
        }

        /// <summary>
        /// Updates HUD data and draws HUD elements to the screen.
        /// </summary>
        private void UpdateAndDrawHud()
        {
            var playerFactionId = CurrentPlayerFactionId;

            UpdateTerritoryIndicator(playerFactionId);

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

        private void UpdateTerritoryIndicator(string? playerFactionId)
        {
            if (_territoryIndicatorRenderer == null || _territoryManager == null)
                return;

            var currentZone = _territoryManager.CurrentZone;
            if (currentZone == null)
            {
                _territoryIndicatorRenderer.Hide();
                _territoryIndicatorRenderer.Draw();
                return;
            }

            var territoryData = BuildTerritoryIndicatorData(currentZone, playerFactionId);
            _territoryIndicatorRenderer.Render(territoryData);
            _territoryIndicatorRenderer.Draw();
        }

        private TerritoryIndicatorData BuildTerritoryIndicatorData(Zone currentZone, string? playerFactionId)
        {
            var ownerFaction = currentZone.OwnerFactionId != null
                ? _factionService.GetFaction(currentZone.OwnerFactionId)
                : null;
            bool isPlayerOwned = currentZone.OwnerFactionId == playerFactionId;
            var activeBattle = _zoneBattleManager?.GetBattleForZone(currentZone.Id);
            var playerBattle = _zoneBattleManager?.GetPlayerCurrentBattle();
            bool isDefendingBattle = activeBattle != null && activeBattle.DefenderFactionId == playerFactionId;
            bool isPlayerAttackingHere = playerBattle != null && playerBattle.ZoneId == currentZone.Id && playerBattle.IsPlayerAttacking;
            var counts = GetTerritoryHudCounts(currentZone, playerFactionId, isPlayerOwned, activeBattle, playerBattle, isDefendingBattle, isPlayerAttackingHere);

            return new TerritoryIndicatorData(
                currentZone.Name,
                ownerFaction?.Name,
                ownerFaction?.Color,
                currentZone.ControlPercentage,
                isDefendingBattle || isPlayerAttackingHere,
                isPlayerOwned,
                deployedDefenderCount: counts.Deployed,
                reserveDefenderCount: counts.Reserve,
                playerTroopCount: counts.PlayerTroops,
                enemyDefenderCount: counts.EnemyDefenders,
                enemyReserveCount: counts.EnemyReserve);
        }

    }
}
