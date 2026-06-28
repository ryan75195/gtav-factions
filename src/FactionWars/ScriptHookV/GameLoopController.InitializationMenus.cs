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

        private void OnMenuBackedOut(object? sender, string menuId)
        {
            if (_menuBackActions.TryGetValue(menuId, out var toParent))
            {
                toParent();
            }
        }

        private void InitializeRecruitmentMenus(IPlayerContext playerContext, ITroopPurchaseService purchaseService)
        {
            var menuProvider = RequiredMenuProvider;
            var mainMenuController = RequiredMainMenuController;

            _recruitmentMenuController = new RecruitmentMenuController(menuProvider, _gameBridge);
            _recruitmentMenuController.BackRequested += BackTo(RecruitmentMenuController.MenuId, () => mainMenuController.OnKeyDown(MainMenuController.MenuToggleKeyCode));
            _defendersMenuController = new DefendersMenuController(menuProvider, _factionService, purchaseService, playerContext);
            _defendersMenuController.BackRequested += BackTo(DefendersMenuController.MenuId, () => _recruitmentMenuController.Show());
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
            _squadMenuController.BackRequested += BackTo(SquadMenuController.MenuId, () => _recruitmentMenuController.Show());
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
        }

    }
}
