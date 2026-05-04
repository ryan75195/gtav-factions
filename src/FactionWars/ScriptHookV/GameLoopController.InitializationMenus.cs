using FactionWars.Core.Interfaces;
using FactionWars.Economy.Interfaces;
using FactionWars.ScriptHookV.Models;
using FactionWars.ScriptHookV.UI;
using FactionWars.Territory.Interfaces;

namespace FactionWars.ScriptHookV
{
    public partial class GameLoopController
    {
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

    }
}
