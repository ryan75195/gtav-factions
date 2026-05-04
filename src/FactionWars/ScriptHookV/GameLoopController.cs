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
    }
}
