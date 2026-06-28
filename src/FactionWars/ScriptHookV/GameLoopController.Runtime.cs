using System;
using System.IO;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Services;
using FactionWars.Economy.Interfaces;
using FactionWars.AI.Interfaces;
using FactionWars.Factions.Interfaces;
using FactionWars.Performance.Interfaces;
using FactionWars.Performance.Models;
using FactionWars.Performance.Services;
using FactionWars.ScriptHookV.Data;
using FactionWars.ScriptHookV.Diagnostics;
using FactionWars.ScriptHookV.Logging;
using FactionWars.ScriptHookV.Managers;
using FactionWars.ScriptHookV.UI;
using FactionWars.Territory.Interfaces;
using FactionWars.Territory.Models;

namespace FactionWars.ScriptHookV
{
    public partial class GameLoopController
    {
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
        /// Gets the active consolidated AI controller.
        /// Returns null if not yet initialized.
        /// </summary>
        public IAIController? AIController => _aiController;

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
        /// Profiles each subsystem phase per tick so a >5s blocking-script freeze names the
        /// executing subsystem (breadcrumb) and slow ticks are logged with a per-phase breakdown.
        /// </summary>
        private readonly ITickProfiler _tickProfiler;

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

            var breadcrumbDir = Path.GetDirectoryName(FileLogger.LogPath) ?? ".";
            var diagnosticsSink = new FileTickDiagnosticsSink(
                Path.Combine(breadcrumbDir, "tick_breadcrumb.txt"),
                FileLogger.Warn);
            _tickProfiler = new TickProfiler(new SystemTimeProvider(), diagnosticsSink, new TickProfilerOptions());
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
            var deltaTime = _gameBridge.IsGamePaused()
                ? 0f
                : (float)(now - _lastTickTime).TotalSeconds;
            _lastTickTime = now;

            _tickProfiler.BeginTick();
            try
            {
                RunTickSystems(deltaTime);
            }
            finally
            {
                _tickProfiler.EndTick();
            }
        }

        private void RunTickSystems(float deltaTime)
        {
            UpdateCoreSystems(deltaTime);
            _tickProfiler.Measure("respawnPlacement", () => UpdatePlayerRespawnPlacement());
            _tickProfiler.Measure("controllerInput", () => PollControllerInput());
            _menuProvider?.SetSelectKeyHeld(IsSelectKeyHeld());
            ThrottleMenuNavigation();
            _tickProfiler.Measure("mainMenu", () => _mainMenuController?.Update());
            UpdateWorldSystems(deltaTime);

            try
            {
                _tickProfiler.Measure("hud", () => UpdateAndDrawHud());
            }
            catch (Exception ex)
            {
                FileLogger.Error("UpdateAndDrawHud failed", ex);
            }

            ProcessPendingOwnedTerritoryPlacement();
        }

    }
}
