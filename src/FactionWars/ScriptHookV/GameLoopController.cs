using System;
using System.Collections.Generic;
using FactionWars.AI.Interfaces;
using FactionWars.AI.Models;
using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Models;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.Economy.Interfaces;
using FactionWars.Factions.Interfaces;
using FactionWars.ScriptHookV.Data;
using FactionWars.ScriptHookV.Logging;
using FactionWars.ScriptHookV.Managers;
using FactionWars.ScriptHookV.Persistence;
using FactionWars.ScriptHookV.UI;
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
        private readonly IResourceTickService _resourceTickService;
        private readonly FactionInitializer _factionInitializer;
        private readonly ZoneDataLoader _zoneDataLoader;
        private MapBlipManager? _mapBlipManager;
        private EconomyManager? _economyManager;
        private FollowerManager? _followerManager;
        private TerritoryManager? _territoryManager;
        private CombatManager? _combatManager;
        private AIManager? _aiManager;
        private VictoryManager? _victoryManager;
        private IAutoSaveService? _autoSaveService;
        private MainMenuController? _mainMenuController;
        private ArmyMenuController? _armyMenuController;
        private OverviewMenuController? _overviewMenuController;
        private ZoneManagementMenuController? _zoneManagementMenuController;
        private ResourcesMenuController? _resourcesMenuController;
        private SettingsMenuController? _settingsMenuController;
        private CombatHudRenderer? _combatHudRenderer;
        private TerritoryIndicatorRenderer? _territoryIndicatorRenderer;
        private IZoneService? _zoneService;
        private DateTime _lastTickTime;
        private bool _isInitialized;
        private bool _characterSwitchInitialized;
        private bool _gameDataInitialized;

        // Debug tracking
        private int _debugLogCounter;
        private bool _wasInCombat;

        // Neutral zone claim state
        private Zone? _currentNeutralZone;
        private bool _showingClaimPrompt;
        private const int ClaimKeyCode = 0x45; // E key

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
        /// Gets the TerritoryManager for zone detection.
        /// Returns null if not yet initialized.
        /// </summary>
        public TerritoryManager? TerritoryManager => _territoryManager;

        /// <summary>
        /// Gets the CombatManager for combat encounters.
        /// Returns null if not yet initialized.
        /// </summary>
        public CombatManager? CombatManager => _combatManager;

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
            var allocationService = _container.Resolve<IZoneDefenderAllocationService>();
            _factionInitializer = new FactionInitializer(factionRepository, _zoneRepository, allocationService);

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

            // Update territory detection (checks player position against zones)
            _territoryManager?.Update();

            // Update combat manager (handles ped spawning, combat state, takeover)
            _combatManager?.Update();

            // Debug: Log combat state every tick if we're supposed to be in combat
            if (_combatManager != null)
            {
                bool isInCombat = _combatManager.IsInCombat;
                bool waveComplete = _combatManager.IsWaveSpawningComplete();
                int remaining = _combatManager.GetRemainingDefendersToSpawn();

                // Only log once per second to reduce spam (deltaTime is in seconds)
                if (isInCombat && (_debugLogCounter++ % 60 == 0))
                {
                    FileLogger.Debug($"Combat state: IsInCombat={isInCombat}, WaveComplete={waveComplete}, Remaining={remaining}");
                }

                // Log if combat ends unexpectedly
                if (!isInCombat && _wasInCombat)
                {
                    FileLogger.Combat($"Combat ended! Was in combat but now IsInCombat={isInCombat}");
                }
                _wasInCombat = isInCombat;
            }

            // Spawn defender waves during active combat
            if (_combatManager?.IsInCombat == true && !_combatManager.IsWaveSpawningComplete())
            {
                var modelsByTier = new Dictionary<DefenderTier, string>
                {
                    { DefenderTier.Basic, "a_m_y_mexthug_01" },
                    { DefenderTier.Medium, "g_m_y_salvagoon_01" },
                    { DefenderTier.Heavy, "s_m_y_swat_01" }
                };

                string defenderFactionId = _combatManager.CurrentEncounter?.DefendingFactionId ?? "";
                var currentTier = _combatManager.GetNextWaveTier();
                int remaining = _combatManager.GetRemainingDefendersToSpawn();

                FileLogger.Spawn($"Attempting spawn: Tier={currentTier}, Remaining={remaining}, FactionId={defenderFactionId}");

                try
                {
                    var spawnedPeds = _combatManager.SpawnNextWave(modelsByTier, defenderFactionId, maxPerTick: 2);
                    FileLogger.Spawn($"SpawnNextWave returned {spawnedPeds.Count} peds");

                    // Configure spawned peds with weapons, stats, and combat behavior
                    if (spawnedPeds.Count > 0 && currentTier.HasValue)
                    {
                        FileLogger.Spawn($"Configuring {spawnedPeds.Count} spawned peds as {currentTier.Value}");
                        ConfigureSpawnedDefenders(spawnedPeds, currentTier.Value, defenderFactionId);
                        _gameBridge.ShowNotification($"~g~Spawned {spawnedPeds.Count} {currentTier.Value} defenders! ({remaining - spawnedPeds.Count} left)");

                        foreach (var ped in spawnedPeds)
                        {
                            FileLogger.Spawn($"  Ped Handle={ped.Handle}, Valid={ped.IsValid}");
                        }
                    }
                    else if (spawnedPeds.Count == 0)
                    {
                        FileLogger.Warn($"No peds spawned! Tier={currentTier}, Remaining={remaining}");
                    }
                }
                catch (Exception ex)
                {
                    FileLogger.Error("Exception during SpawnNextWave", ex);
                }
            }

            // Update AI manager (makes decisions for non-player factions)
            _aiManager?.Update(deltaTime);

            // Update victory manager (checks for 100% control)
            _victoryManager?.Update(deltaTime);

            // Update follower manager (updates follower positions and behavior)
            _followerManager?.Update(CurrentPlayerFactionId ?? "");

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
                    bool isContested = _combatManager?.IsInCombat == true &&
                                       _combatManager.CurrentEncounter?.ZoneId == currentZone.Id;

                    var territoryData = new TerritoryIndicatorData(
                        currentZone.Name,
                        ownerFactionName,
                        ownerColor,
                        currentZone.ControlPercentage,
                        isContested,
                        isPlayerOwned);

                    _territoryIndicatorRenderer.Render(territoryData);
                }
                else
                {
                    _territoryIndicatorRenderer.Hide();
                }

                _territoryIndicatorRenderer.Draw();
            }

            // Update combat HUD when in combat
            if (_combatHudRenderer != null && _combatManager != null)
            {
                if (_combatManager.IsInCombat && _combatManager.CurrentEncounter != null)
                {
                    var encounter = _combatManager.CurrentEncounter;

                    // Get zone name
                    string zoneName = "Unknown Zone";
                    if (_zoneService != null)
                    {
                        var zone = _zoneService.GetZone(encounter.ZoneId);
                        zoneName = zone?.Name ?? "Unknown Zone";
                    }

                    bool isPlayerAttacker = encounter.AttackingFactionId == playerFactionId;

                    var combatData = new CombatHudData(
                        encounter.ZoneId,
                        zoneName,
                        encounter.AttackingFactionId,
                        encounter.DefendingFactionId,
                        encounter.AttackerControlPercentage,
                        encounter.DefenderControlPercentage,
                        encounter.AttackerPedCount,
                        encounter.DefenderPedCount,
                        0f, // Reinforcement cooldown not tracked yet
                        isPlayerAttacker,
                        encounter.GetDuration());

                    _combatHudRenderer.RenderCombatHud(combatData);
                }
                else
                {
                    _combatHudRenderer.HideCombatHud();
                }

                _combatHudRenderer.Draw();
            }
        }

        /// <summary>
        /// Initializes game data including zones and factions.
        /// Called on the first tick to ensure the game is ready.
        /// </summary>
        private void InitializeGameData()
        {
            FileLogger.Separator("INITIALIZATION START");
            FileLogger.Info("InitializeGameData() called");

            // Load zones first
            FileLogger.Info("Loading default zones...");
            _zoneDataLoader.LoadDefaultZones();
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
            var pedSpawningService = _container.Resolve<IPedSpawningService>();
            var defenderTierService = _container.Resolve<IDefenderTierService>();
            _followerManager = new FollowerManager(_gameBridge, followerService, pedSpawningService, defenderTierService);

            // Initialize territory manager for zone detection
            _zoneService = _container.Resolve<IZoneService>();
            _territoryManager = new TerritoryManager(_gameBridge, _zoneService);

            // Initialize combat manager for combat encounters
            var pedPool = _container.Resolve<IPedPool>();
            var spawnPositionCalculator = _container.Resolve<ISpawnPositionCalculator>();
            var controlCalculator = _container.Resolve<IControlPercentageCalculator>();
            var takeoverDetector = _container.Resolve<ITakeoverDetector>();
            var combatResultHandler = _container.Resolve<ICombatResultHandler>();
            var waveSpawnerService = _container.Resolve<IWaveSpawnerService>();
            // followerService already resolved above for FollowerManager
            _combatManager = new CombatManager(
                _gameBridge,
                pedPool,
                pedSpawningService,
                spawnPositionCalculator,
                controlCalculator,
                takeoverDetector,
                combatResultHandler,
                waveSpawnerService,
                followerService);

            // Subscribe to combat ended event to show claim prompt after victory
            _combatManager.CombatEnded += OnCombatEnded;

            // Initialize AI manager for AI faction decisions
            var strategies = _container.Resolve<IDictionary<string, IAIStrategy>>();
            _aiManager = new AIManager(_factionService, _zoneService, strategies);
            _aiManager.Start();
            _aiManager.SetPlayerFactionId(CurrentPlayerFactionId);
            _aiManager.OnAIDecision += HandleAIDecision;

            // Initialize victory manager for victory condition checking
            var victoryConditionService = _container.Resolve<IVictoryConditionService>();
            var notificationService = _container.Resolve<INotificationService>();
            _victoryManager = new VictoryManager(victoryConditionService, _factionService, notificationService);
            _victoryManager.Start();

            // Initialize HUD renderers for combat and territory display
            _combatHudRenderer = new CombatHudRenderer();
            _territoryIndicatorRenderer = new TerritoryIndicatorRenderer();

            // Wire territory events to combat manager
            _territoryManager.ZoneEntered += OnZoneEntered;
            _territoryManager.ZoneExited += OnZoneExited;

            // Wire neutral zone claim events
            _territoryManager.NeutralZoneEntered += OnNeutralZoneEntered;
            _territoryManager.ZoneExited += OnZoneExitedForClaim;

            // Initialize main menu controller for UI
            var menuProvider = _container.Resolve<IMenuProvider>();
            _mainMenuController = new MainMenuController(menuProvider);

            // Initialize submenu controllers
            var playerContext = _container.Resolve<IPlayerContext>();
            var purchaseService = _container.Resolve<ITroopPurchaseService>();
            var allocationService = _container.Resolve<IZoneDefenderAllocationService>();

            _overviewMenuController = new OverviewMenuController(
                menuProvider,
                _factionService,
                _zoneService,
                playerContext);
            _overviewMenuController.BackRequested += (s, e) => _mainMenuController.OnKeyDown(MainMenuController.MenuToggleKeyCode);

            _zoneManagementMenuController = new ZoneManagementMenuController(
                menuProvider,
                _factionService,
                _zoneService,
                playerContext,
                allocationService);
            _zoneManagementMenuController.BackRequested += (s, e) => _mainMenuController.OnKeyDown(MainMenuController.MenuToggleKeyCode);

            _armyMenuController = new ArmyMenuController(
                menuProvider,
                _factionService,
                purchaseService,
                followerService,
                defenderTierService,
                playerContext,
                _followerManager,
                _gameBridge);
            _armyMenuController.BackRequested += (s, e) => _mainMenuController.OnKeyDown(MainMenuController.MenuToggleKeyCode);

            // Initialize resources menu controller
            var resourceModifier = _container.Resolve<IZoneTraitResourceModifier>();
            var supplyLineService = _container.Resolve<ISupplyLineService>();
            _resourcesMenuController = new ResourcesMenuController(
                menuProvider,
                _factionService,
                _zoneService,
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

            FileLogger.Separator("INITIALIZATION COMPLETE");
            FileLogger.Info($"Player faction: {CurrentPlayerFactionId ?? "UNKNOWN"}");
            FileLogger.Info($"Log file: {FileLogger.LogPath}");
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

            // Handle claim key when in neutral zone
            if (keyCode == ClaimKeyCode && _showingClaimPrompt && _currentNeutralZone != null)
            {
                TryClaimNeutralZone();
                return;
            }

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

            // Unsubscribe from territory events and clean up
            if (_territoryManager != null)
            {
                _territoryManager.ZoneEntered -= OnZoneEntered;
                _territoryManager.ZoneExited -= OnZoneExited;
                _territoryManager.NeutralZoneEntered -= OnNeutralZoneEntered;
                _territoryManager.ZoneExited -= OnZoneExitedForClaim;
                _territoryManager = null;
            }

            // Stop and clean up combat manager
            if (_combatManager != null)
            {
                _combatManager.CombatEnded -= OnCombatEnded;
                _combatManager.EndCombat(CombatStatus.Aborted);
            }
            _combatManager = null;

            // Unsubscribe from AI events and stop AI manager
            if (_aiManager != null)
            {
                _aiManager.OnAIDecision -= HandleAIDecision;
                _aiManager.Stop();
            }
            _aiManager = null;

            // Stop victory manager
            _victoryManager?.Stop();
            _victoryManager = null;

            // Clean up follower manager
            _followerManager = null;
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

            // Update managers with new faction
            _economyManager?.SetPlayerFactionId(newFactionId);
            _aiManager?.SetPlayerFactionId(newFactionId);

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

        /// <summary>
        /// Called when the player enters a zone.
        /// Triggers combat if entering enemy territory.
        /// </summary>
        private void OnZoneEntered(object? sender, Zone zone)
        {
            FileLogger.Separator("ZONE ENTERED");
            FileLogger.Zone($"OnZoneEntered triggered");

            if (_combatManager == null || zone == null)
            {
                FileLogger.Error($"OnZoneEntered: combatManager={_combatManager != null}, zone={zone != null}");
                return;
            }

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

                // Start combat in enemy zone
                var encounter = _combatManager.StartCombat(zone, playerFactionId);
                FileLogger.Combat($"Combat encounter created: ID={encounter?.Id ?? "NULL"}");
                FileLogger.Combat($"Defending Faction: {encounter?.DefendingFactionId ?? "NULL"}");
                _gameBridge.ShowNotification($"~r~COMBAT STARTED in:~w~ {zone.Name}");

                // Initialize defender spawning - use allocation if exists, else default
                var allocationService = _container.Resolve<IZoneDefenderAllocationService>();
                var allocation = allocationService.GetAllocation(zone.OwnerFactionId, zone.Id);
                FileLogger.Combat($"Zone allocation: {(allocation != null ? $"TotalTroops={allocation.TotalTroops}" : "NULL")}");

                DefenderSpawnPlan spawnPlan;
                if (allocation != null && allocation.TotalTroops > 0)
                {
                    spawnPlan = new DefenderSpawnPlan(
                        basicPeds: allocation.GetTroopCount(DefenderTier.Basic),
                        mediumPeds: allocation.GetTroopCount(DefenderTier.Medium),
                        heavyPeds: allocation.GetTroopCount(DefenderTier.Heavy));
                    FileLogger.Combat($"Using allocated troops: Basic={allocation.GetTroopCount(DefenderTier.Basic)}, Medium={allocation.GetTroopCount(DefenderTier.Medium)}, Heavy={allocation.GetTroopCount(DefenderTier.Heavy)}");
                    _gameBridge.ShowNotification($"~y~Spawning {spawnPlan.TotalPeds} defenders (allocated)");
                }
                else
                {
                    // Default: spawn 3 basic defenders if no allocation
                    spawnPlan = new DefenderSpawnPlan(basicPeds: 3, mediumPeds: 0, heavyPeds: 0);
                    FileLogger.Combat("Using default spawn plan: 3 Basic defenders");
                    _gameBridge.ShowNotification($"~y~Spawning 3 default defenders");
                }

                FileLogger.Combat($"Spawn plan total: {spawnPlan.TotalPeds} peds");
                _combatManager.InitializeWaveSpawning(spawnPlan);
                FileLogger.Combat($"Wave spawning initialized. IsInCombat={_combatManager.IsInCombat}, WaveComplete={_combatManager.IsWaveSpawningComplete()}");
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
        }

        /// <summary>
        /// Called when the player exits a zone.
        /// May end combat if leaving the contested zone.
        /// </summary>
        private void OnZoneExited(object? sender, Zone zone)
        {
            if (_combatManager == null || zone == null)
                return;

            // If we were in combat in this zone, end it (retreat)
            if (_combatManager.IsInCombat && _combatManager.CurrentEncounter?.ZoneId == zone.Id)
            {
                _combatManager.EndCombat(CombatStatus.PlayerRetreat);
                _gameBridge.ShowNotification($"~y~Retreated from:~w~ {zone.Name}");
            }
        }

        /// <summary>
        /// Called when a combat encounter ends.
        /// If the player won (AttackerVictory), shows the claim prompt for the now-neutral zone.
        /// </summary>
        private void OnCombatEnded(object? sender, CombatEncounter encounter)
        {
            if (encounter.Status == CombatStatus.AttackerVictory)
            {
                // Zone is now neutral after attacker victory, show claim prompt
                var zone = _zoneService?.GetZone(encounter.ZoneId);
                if (zone != null)
                {
                    OnNeutralZoneEntered(this, zone);
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
            _gameBridge.ShowNotification($"~y~Unclaimed territory: {zone.Name}~n~Press ~g~E~w~ to claim for ~g~${cost}");
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

            // Deduct cost
            _gameBridge.AddPlayerMoney(-cost);

            // Transfer ownership
            _zoneService!.TransferZoneOwnership(_currentNeutralZone.Id, playerFaction);

            // Allocate 1 Basic troop
            var allocationService = _container.Resolve<IZoneDefenderAllocationService>();
            allocationService.SetAllocation(playerFaction!, _currentNeutralZone.Id, DefenderTier.Basic, 1);

            _gameBridge.ShowNotification($"~g~You now control {_currentNeutralZone.Name}!");

            // Clear prompt state
            _currentNeutralZone = null;
            _showingClaimPrompt = false;
        }

        /// <summary>
        /// Handles AI faction decisions.
        /// Processes attack decisions to simulate AI territorial battles.
        /// </summary>
        private void HandleAIDecision(object? sender, AIDecisionEventArgs e)
        {
            if (e.Decision.DecisionType == AIDecisionType.Attack && e.Decision.TargetZoneId != null)
            {
                // AI faction is attacking a zone
                // This could trigger background battles or update zone ownership based on troop counts
                // For now, we log the decision for debugging purposes
                // Future enhancement: simulate AI battles and update zone ownership
            }
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
