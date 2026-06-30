using FactionWars.Core.Interfaces;
using FactionWars.Economy.Interfaces;
using FactionWars.Factions.Interfaces;
using FactionWars.ScriptHookV.Models;
using FactionWars.ScriptHookV.UI;
using FactionWars.UI.Interfaces;
using FactionWars.UI.Services;

namespace FactionWars.ScriptHookV
{
    public partial class GameLoopController
    {
        private void InitializeHudAndEventRenderers()
        {
            var territoryManager = RequiredTerritoryManager;
            _combatHudRenderer = new CombatHudRenderer();
            _territoryIndicatorRenderer = new TerritoryIndicatorRenderer();

            if (_gameBridge.GetType().FullName == "FactionWars.ScriptHookV.GameBridge")
                _playTimeHudRenderer = new PlayTimeHudRenderer();

            _eventFeedRenderer = new EventFeedRenderer(_container.Resolve<IFactionRepository>());
            _eventFeedService = _container.Resolve<IEventFeedService>();

            // Native draw/input/time-scale: only under the real game bridge (mirrors PlayTimeHudRenderer).
            if (_gameBridge.GetType().FullName == "FactionWars.ScriptHookV.GameBridge")
            {
                _squadRadialMenuRenderer = new SquadRadialMenuRenderer(
                    _gameBridge,
                    () => _squadStanceController!.CurrentStance,
                    (stance, handles) => _squadStanceController!.SetStance(stance, handles),
                    () => _followerManager?.OnFootBodyguardHandles ?? System.Array.Empty<int>());
            }

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
            _menuProvider.MenuBackedOut += OnMenuBackedOut;
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
            _overviewMenuController.BackRequested += BackTo(OverviewMenuController.OverviewMenuId, () => mainMenuController.OnKeyDown(MainMenuController.MenuToggleKeyCode));

            _zoneManagementMenuController = new ZoneManagementMenuController(
                new ZoneManagementMenuControllerDependencies
                {
                    MenuProvider = menuProvider,
                    FactionService = _factionService,
                    ZoneService = zoneService,
                    PlayerContext = playerContext,
                    AllocationService = allocationService,
                    DeploymentService = _container.Resolve<IDefenderDeploymentService>()
                });
            _zoneManagementMenuController.BackRequested += BackTo(ZoneManagementMenuController.ZoneManagementMenuId, () => mainMenuController.OnKeyDown(MainMenuController.MenuToggleKeyCode));
        }

    }
}
