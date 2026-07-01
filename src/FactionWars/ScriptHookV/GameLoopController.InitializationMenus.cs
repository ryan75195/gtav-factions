using System;
using System.Collections.Generic;
using FactionWars.Core.Interfaces;
using FactionWars.Economy.Interfaces;
using FactionWars.ScriptHookV.Models;
using FactionWars.ScriptHookV.UI;
using FactionWars.Territory.Interfaces;

namespace FactionWars.ScriptHookV
{
    public partial class GameLoopController
    {
        // Maps a submenu's id to the navigation action that returns to its parent. Used for the
        // native back control (B / Backspace / Esc) so backing out of a submenu goes up one level
        // instead of closing the whole menu. The same action backs the menu's "Back" item.
        private readonly Dictionary<string, Action> _menuBackActions = new Dictionary<string, Action>();

        // Registers a menu's parent-navigation action for native back, and returns the matching
        // BackRequested handler so the "Back" item and the native back share one action.
        private EventHandler BackTo(string menuId, Action toParent)
        {
            _menuBackActions[menuId] = toParent;
            return (s, e) => toParent();
        }

        private void OnMenuBackedOut(object? sender, string menuId) => InvokeBackAction(menuId);

        private void InvokeBackAction(string menuId)
        {
            if (_menuBackActions.TryGetValue(menuId, out var toParent))
            {
                toParent();
            }
        }

        // The squad hub has two parents: Recruitment (F7 path) and gameplay (d-pad-left tap).
        // The back target is set at show time so backing out returns to wherever it was opened from.
        private void ShowSquadHub(Action backTarget)
        {
            _menuBackActions[SquadHubMenuController.MenuId] = backTarget;
            _squadHubMenuController?.Show();
        }

        private void InitializeRecruitmentMenus(IPlayerContext playerContext, ITroopPurchaseService purchaseService)
        {
            var menuProvider = RequiredMenuProvider;
            var mainMenuController = RequiredMainMenuController;

            _recruitmentMenuController = new RecruitmentMenuController(menuProvider, _gameBridge);
            _recruitmentMenuController.BackRequested += BackTo(RecruitmentMenuController.MenuId, () => mainMenuController.OnKeyDown(MainMenuController.MenuToggleKeyCode));
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

            _squadHubMenuController = new SquadHubMenuController(menuProvider, _gameBridge);
            _supportCallMenuController = new SupportCallMenuController(new SupportCallMenuControllerDependencies
            {
                MenuProvider = menuProvider,
                SupportPackageService = _container.Resolve<ISupportPackageService>(),
                SupportSquadManager = _supportSquadManager!,
                Territory = _territoryManager!,
                PlayerContext = playerContext,
                GameBridge = _gameBridge
            });

            // Recruitment → Squad opens the hub with Recruitment as the back target; the d-pad-left
            // tap path (see InitializeHudAndEventRenderers) opens the same hub with a
            // close-to-gameplay back target instead.
            _recruitmentMenuController.SquadRequested += (s, e) => ShowSquadHub(() => _recruitmentMenuController.Show());
            _squadHubMenuController.ManageSquadRequested += (s, e) => _squadMenuController.Show();
            _squadHubMenuController.SupportRequested += (s, e) => _supportCallMenuController.Show();

            // Back targets: hub → whatever opened it (set per-show above); manage-squad → hub;
            // support-call → hub.
            _squadHubMenuController.BackRequested += (s, e) => InvokeBackAction(SquadHubMenuController.MenuId);
            _squadMenuController.BackRequested += BackTo(SquadMenuController.MenuId, () => _squadHubMenuController.Show());
            _supportCallMenuController.BackRequested += BackTo(SupportCallMenuController.MenuId, () => _squadHubMenuController.Show());
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
            _resourcesMenuController.BackRequested += BackTo(ResourcesMenuController.ResourcesMenuId, () => mainMenuController.OnKeyDown(MainMenuController.MenuToggleKeyCode));

            _difficultyService = _container.Resolve<IDifficultyService>();
            var difficultyService = RequiredDifficultyService;
            _resourceTickService.SetAiIncomeMultiplier(difficultyService.Current.AiIncomeMultiplier);
            _resourceTickService.SetTickInterval(difficultyService.Current.TickIntervalSeconds);
            _resourceTickService.SetPlayerFactionId(CurrentPlayerFactionId);
            difficultyService.DifficultyChanged += OnDifficultyChanged;
            _settingsMenuController = new SettingsMenuController(menuProvider, difficultyService, _gameBridge);
            _settingsMenuController.BackRequested += BackTo(SettingsMenuController.SettingsMenuId, () => mainMenuController.OnKeyDown(MainMenuController.MenuToggleKeyCode));
            _shopMenuController = new ShopMenuController(menuProvider, _gameBridge);
            _shopMenuController.BackRequested += BackTo(ShopMenuController.ShopMenuId, () => mainMenuController.OnKeyDown(MainMenuController.MenuToggleKeyCode));

            _supportMenuController = new SupportMenuController(
                menuProvider, _gameBridge,
                _container.Resolve<ISupportPackageService>(), playerContext);
            // The Support menu is a main-menu submenu (the commander interaction also opens the
            // main menu), so backing out returns to the main menu like its siblings.
            _supportMenuController.BackRequested += BackTo(SupportMenuController.MenuId, () => mainMenuController.OnKeyDown(MainMenuController.MenuToggleKeyCode));
        }

    }
}
