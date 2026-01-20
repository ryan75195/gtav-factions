using System;
using FactionWars.Combat.Interfaces;
using FactionWars.Core.Interfaces;
using FactionWars.Economy.Interfaces;
using FactionWars.Factions.Interfaces;
using FactionWars.ScriptHookV.Data;
using FactionWars.ScriptHookV.Managers;
using FactionWars.ScriptHookV.Persistence;
using FactionWars.ScriptHookV.UI;
using FactionWars.Territory.Interfaces;
using FactionWars.UI.Interfaces;
using FactionWars.UI.Models;

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
        private readonly IResourceTickService _resourceTickService;
        private readonly FactionInitializer _factionInitializer;
        private readonly ZoneDataLoader _zoneDataLoader;
        private MapBlipManager? _mapBlipManager;
        private EconomyManager? _economyManager;
        private FollowerManager? _followerManager;
        private IAutoSaveService? _autoSaveService;
        private MainMenuController? _mainMenuController;
        private ArmyMenuController? _armyMenuController;
        private OverviewMenuController? _overviewMenuController;
        private ZoneManagementMenuController? _zoneManagementMenuController;
        private ResourcesMenuController? _resourcesMenuController;
        private SettingsMenuController? _settingsMenuController;
        private DateTime _lastTickTime;
        private bool _isInitialized;
        private bool _characterSwitchInitialized;
        private bool _gameDataInitialized;

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
        /// Gets the ArmyMenuController for handling army menu interactions.
        /// Returns null if not yet initialized.
        /// </summary>
        public ArmyMenuController? ArmyMenuController => _armyMenuController;

        /// <summary>
        /// Gets the AutoSaveService for automatic game state saving.
        /// Returns null if not yet initialized.
        /// </summary>
        public IAutoSaveService? AutoSaveService => _autoSaveService;

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
            var factionRepository = _container.Resolve<IFactionRepository>();
            _factionService = _container.Resolve<IFactionService>();
            _resourceTickService = _container.Resolve<IResourceTickService>();

            // Create character switch detector
            _characterSwitchDetector = new CharacterSwitchDetector(_gameBridge, factionDetector);
            _characterSwitchDetector.OnCharacterSwitched += HandleCharacterSwitched;

            // Create zone and faction initializers
            _zoneDataLoader = new ZoneDataLoader(_zoneRepository);
            _factionInitializer = new FactionInitializer(factionRepository, _zoneRepository);

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

            // Initialize game data on first tick (zones and factions)
            if (!_gameDataInitialized)
            {
                InitializeGameData();
                _gameDataInitialized = true;
            }

            // Initialize character switch detection on first tick
            if (!_characterSwitchInitialized)
            {
                _characterSwitchDetector.Initialize();
                _characterSwitchInitialized = true;
            }

            // Check for character switches
            _characterSwitchDetector.CheckForSwitch();

            // Calculate delta time for this frame
            var now = DateTime.UtcNow;
            var deltaTime = (float)(now - _lastTickTime).TotalSeconds;
            _lastTickTime = now;

            // Update economy manager
            _economyManager?.Update(deltaTime);

            // Update auto-save service (checks interval and saves if needed)
            _autoSaveService?.Update(TimeSpan.FromSeconds(deltaTime));

            // Update menu system
            _mainMenuController?.Update();

            // Update map blip colors to reflect current zone ownership
            _mapBlipManager?.UpdateBlipColors();

            // TODO: Process game loop updates
            // - Territory detection
            // - Combat management
            // - AI updates
        }

        /// <summary>
        /// Initializes game data including zones and factions.
        /// Called on the first tick to ensure the game is ready.
        /// </summary>
        private void InitializeGameData()
        {
            // Load zones first
            _zoneDataLoader.LoadDefaultZones();

            // Then initialize factions with their starting conditions
            _factionInitializer.Initialize();

            // Initialize map blips to show zone ownership on the map
            _mapBlipManager = new MapBlipManager(_gameBridge, _zoneRepository, _factionService);
            _mapBlipManager.Initialize();

            // Initialize economy manager for resource ticks
            _economyManager = new EconomyManager(_resourceTickService, _gameBridge);
            _economyManager.Start();

            // Initialize follower manager for bodyguard management
            var followerService = _container.Resolve<IFollowerService>();
            var pedSpawningService = _container.Resolve<IPedSpawningService>();
            var defenderTierService = _container.Resolve<IDefenderTierService>();
            _followerManager = new FollowerManager(_gameBridge, followerService, pedSpawningService, defenderTierService);

            // Initialize main menu controller for UI
            var menuProvider = _container.Resolve<IMenuProvider>();
            _mainMenuController = new MainMenuController(menuProvider);

            // Initialize submenu controllers
            var playerContext = _container.Resolve<IPlayerContext>();
            var purchaseService = _container.Resolve<ITroopPurchaseService>();
            var allocationService = _container.Resolve<IZoneDefenderAllocationService>();
            var zoneService = _container.Resolve<IZoneService>();

            _overviewMenuController = new OverviewMenuController(
                menuProvider,
                _factionService,
                zoneService,
                playerContext);
            _overviewMenuController.BackRequested += (s, e) => _mainMenuController.OnKeyDown(MainMenuController.MenuToggleKeyCode);

            _zoneManagementMenuController = new ZoneManagementMenuController(
                menuProvider,
                _factionService,
                zoneService,
                playerContext,
                allocationService);
            _zoneManagementMenuController.BackRequested += (s, e) => _mainMenuController.OnKeyDown(MainMenuController.MenuToggleKeyCode);

            _armyMenuController = new ArmyMenuController(
                menuProvider,
                _factionService,
                purchaseService,
                followerService,
                defenderTierService,
                playerContext);
            _armyMenuController.BackRequested += (s, e) => _mainMenuController.OnKeyDown(MainMenuController.MenuToggleKeyCode);

            // Initialize resources menu controller
            var resourceModifier = _container.Resolve<IZoneTraitResourceModifier>();
            var supplyLineService = _container.Resolve<ISupplyLineService>();
            _resourcesMenuController = new ResourcesMenuController(
                menuProvider,
                _factionService,
                zoneService,
                playerContext,
                _resourceTickService,
                resourceModifier,
                supplyLineService);
            _resourcesMenuController.BackRequested += (s, e) => _mainMenuController.OnKeyDown(MainMenuController.MenuToggleKeyCode);

            // Initialize settings menu controller
            var saveSlotManager = _container.Resolve<ISaveSlotManager>();
            var gameStateCoordinator = _container.Resolve<IGameStateCoordinator>();
            _settingsMenuController = new SettingsMenuController(
                menuProvider,
                saveSlotManager,
                gameStateCoordinator);
            _settingsMenuController.BackRequested += (s, e) => _mainMenuController.OnKeyDown(MainMenuController.MenuToggleKeyCode);

            // Wire up main menu item selection to show submenus
            menuProvider.ItemSelected += OnMainMenuItemSelected;

            // Initialize auto-save service for automatic game state saving
            _autoSaveService = _container.Resolve<IAutoSaveService>();

            // Start auto-save after marking the game as loaded
            var gameStateManager = _container.Resolve<IGameStateManager>();
            gameStateManager.NewGame(); // Mark the game as loaded so auto-save can capture state
            _autoSaveService.Start();
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
                case MainMenuController.OverviewItemId:
                    _overviewMenuController?.Show();
                    break;
                case MainMenuController.ZoneManagementItemId:
                    _zoneManagementMenuController?.Show();
                    break;
                case MainMenuController.ArmyItemId:
                    _armyMenuController?.Show();
                    break;
                case MainMenuController.ResourcesItemId:
                    _resourcesMenuController?.Show();
                    break;
                case MainMenuController.SettingsItemId:
                    _settingsMenuController?.Show();
                    break;
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

            // Pass key events to the main menu controller
            _mainMenuController?.OnKeyDown(keyCode);
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

            // Stop economy manager
            _economyManager?.Stop();
            _economyManager = null;

            // Stop and dispose auto-save service
            _autoSaveService?.Stop();
            _autoSaveService?.Dispose();
            _autoSaveService = null;

            // Clean up map blips
            _mapBlipManager?.Dispose();
            _mapBlipManager = null;

            // TODO: Clean up resources
            // - Despawn all peds
            // - Save state
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

            // Show notification to player
            var newCharacterName = GetCharacterDisplayName(newFactionId);
            _gameBridge.ShowNotification($"~b~FactionWars:~w~ Switched to {newCharacterName}'s faction");

            // Raise the public event for other systems to respond
            OnCharacterSwitched?.Invoke(oldFactionId, newFactionId);
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
    }
}
