using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.Economy.Interfaces;
using FactionWars.Factions.Interfaces;
using FactionWars.Factions.Models;
using FactionWars.ScriptHookV.Managers;
using FactionWars.ScriptHookV.Models;
using FactionWars.UI.Interfaces;
using FactionWars.UI.Models;
using System;
using System.Linq;

namespace FactionWars.ScriptHookV.UI
{
    /// <summary>
    /// Controller for the Army submenu. Allows purchasing troops to reserve,
    /// viewing reserves, and recruiting/managing followers.
    /// </summary>
    public partial class ArmyMenuController
    {
        /// <summary>
        /// Menu ID for the main army menu.
        /// </summary>
        public const string ArmyMenuId = "army_menu";

        /// <summary>
        /// Menu ID for the follower list menu.
        /// </summary>
        public const string FollowerListMenuId = "follower_list_menu";

        /// <summary>
        /// Menu ID for the follower detail menu.
        /// </summary>
        public const string FollowerDetailMenuId = "follower_detail_menu";

        /// <summary>
        /// Item ID for money display.
        /// </summary>
        public const string MoneyDisplayItemId = "money_display";

        /// <summary>
        /// Item ID for reserve pool summary.
        /// </summary>
        public const string ReserveSummaryItemId = "reserve_summary";

        /// <summary>
        /// Item ID for purchasing basic troops.
        /// </summary>
        public const string PurchaseBasicItemId = "purchase_basic";

        /// <summary>
        /// Item ID for purchasing medium troops.
        /// </summary>
        public const string PurchaseMediumItemId = "purchase_medium";

        /// <summary>
        /// Item ID for purchasing heavy troops.
        /// </summary>
        public const string PurchaseHeavyItemId = "purchase_heavy";

        /// <summary>
        /// Item ID for follower summary display.
        /// </summary>
        public const string FollowerSummaryItemId = "follower_summary";

        /// <summary>
        /// Item ID for recruiting basic follower.
        /// </summary>
        public const string RecruitBasicItemId = "recruit_basic";

        /// <summary>
        /// Item ID for recruiting medium follower.
        /// </summary>
        public const string RecruitMediumItemId = "recruit_medium";

        /// <summary>
        /// Item ID for recruiting heavy follower.
        /// </summary>
        public const string RecruitHeavyItemId = "recruit_heavy";

        /// <summary>
        /// Item ID for managing followers.
        /// </summary>
        public const string ManageFollowersItemId = "manage_followers";

        /// <summary>
        /// Item ID for no followers message.
        /// </summary>
        public const string NoFollowersItemId = "no_followers";

        /// <summary>
        /// Item ID for follower list back button.
        /// </summary>
        public const string FollowerListBackItemId = "follower_list_back";

        /// <summary>
        /// Item ID for dismissing a follower.
        /// </summary>
        public const string DismissFollowerItemId = "dismiss_follower";

        /// <summary>
        /// Item ID for follower detail back button.
        /// </summary>
        public const string FollowerDetailBackItemId = "follower_detail_back";

        /// <summary>
        /// Item ID for main menu back button.
        /// </summary>
        public const string BackItemId = "back";

        private readonly IMenuProvider _menuProvider;
        private readonly IFactionService _factionService;
        private readonly ITroopPurchaseService _purchaseService;
        private readonly IFollowerService _followerService;
        private readonly IDefenderTierService _tierService;
        private readonly IPlayerContext _playerContext;
        private readonly IFollowerManager? _followerManager;
        private readonly IGameBridge? _gameBridge;

        private Guid? _selectedFollowerId;
        private string? _lastSelectedItemId;

        /// <summary>
        /// Event raised when the user selects the back option from the main army menu.
        /// </summary>
        public event EventHandler? BackRequested;

        /// <summary>
        /// Creates a new ArmyMenuController with the specified dependencies.
        /// </summary>
        /// <param name="menuProvider">The menu provider for displaying menus.</param>
        /// <param name="factionService">The faction service for retrieving faction data.</param>
        /// <param name="purchaseService">The troop purchase service for buying troops.</param>
        /// <param name="followerService">The follower service for managing followers.</param>
        /// <param name="tierService">The defender tier service for tier costs.</param>
        /// <param name="playerContext">The player context for determining the current faction.</param>
        /// <param name="followerManager">The follower manager for spawning actual peds (optional).</param>
        /// <param name="gameBridge">The game bridge for notifications (optional).</param>
        /// <exception cref="ArgumentNullException">Thrown if any required parameter is null.</exception>
        public ArmyMenuController(ArmyMenuControllerDependencies dependencies, IFollowerManager? followerManager = null, IGameBridge? gameBridge = null)
        {
            if (dependencies == null) throw new ArgumentNullException(nameof(dependencies));
            _menuProvider = dependencies.MenuProvider ?? throw new ArgumentNullException(nameof(dependencies.MenuProvider));
            _factionService = dependencies.FactionService ?? throw new ArgumentNullException(nameof(dependencies.FactionService));
            _purchaseService = dependencies.PurchaseService ?? throw new ArgumentNullException(nameof(dependencies.PurchaseService));
            _followerService = dependencies.FollowerService ?? throw new ArgumentNullException(nameof(dependencies.FollowerService));
            _tierService = dependencies.TierService ?? throw new ArgumentNullException(nameof(dependencies.TierService));
            _playerContext = dependencies.PlayerContext ?? throw new ArgumentNullException(nameof(dependencies.PlayerContext));
            _followerManager = followerManager;
            _gameBridge = gameBridge;

            // Subscribe to menu item selection events
            _menuProvider.ItemSelected += OnItemSelected;
        }

        public ArmyMenuController(params object?[] dependencies)
            : this(new ArmyMenuControllerDependencies
            {
                MenuProvider = (IMenuProvider?)dependencies[0],
                FactionService = (IFactionService?)dependencies[1],
                PurchaseService = (ITroopPurchaseService?)dependencies[2],
                FollowerService = (IFollowerService?)dependencies[3],
                TierService = (IDefenderTierService?)dependencies[4],
                PlayerContext = (IPlayerContext?)dependencies[5]
            })
        {
        }

        /// <summary>
        /// Shows the army menu.
        /// </summary>
        public void Show()
        {
            _selectedFollowerId = null;
            _lastSelectedItemId = null;
            ShowArmyMenu();
        }

        /// <summary>
        /// Shows the main army menu.
        /// </summary>
        private void ShowArmyMenu()
        {
            var factionId = _playerContext.CurrentFactionId;
            var factionState = factionId != null ? _factionService.GetFactionState(factionId) : null;

            var menu = new MenuDefinition(ArmyMenuId, "Recruitment", "Troops & Followers");
            AddMoneyItem(menu);
            AddReserveSummaryItem(menu, factionState);
            AddPurchaseItems(menu, factionId);
            AddRecruitmentItems(menu, factionId);
            AddArmyNavigationItems(menu);

            _menuProvider.ShowMenu(menu, _lastSelectedItemId);
            _menuProvider.HoldToRepeatEnabled = true; // Enable hold-to-repeat for troop purchases
        }

        private void AddMoneyItem(MenuDefinition menu)
        {
            var playerMoney = _purchaseService.GetPlayerMoney();
            var moneyItem = new MenuItem(
                MoneyDisplayItemId,
                $"Cash: ${playerMoney:N0}",
                "Your available funds");
            moneyItem.IsEnabled = false;
            menu.AddItem(moneyItem);
        }

        private static void AddReserveSummaryItem(MenuDefinition menu, FactionState? factionState)
        {
            var basicReserve = factionState?.GetReserveTroops(DefenderTier.Basic) ?? 0;
            var mediumReserve = factionState?.GetReserveTroops(DefenderTier.Medium) ?? 0;
            var heavyReserve = factionState?.GetReserveTroops(DefenderTier.Heavy) ?? 0;

            var reserveItem = new MenuItem(
                ReserveSummaryItemId,
                $"Reserves: B:{basicReserve} M:{mediumReserve} H:{heavyReserve}",
                "Troops available for deployment");
            reserveItem.IsEnabled = false;
            menu.AddItem(reserveItem);
        }

        private void AddPurchaseItems(MenuDefinition menu, string? factionId)
        {
            var basicCost = _purchaseService.GetTroopCost(DefenderTier.Basic);
            var mediumCost = _purchaseService.GetTroopCost(DefenderTier.Medium);
            var heavyCost = _purchaseService.GetTroopCost(DefenderTier.Heavy);

            var canPurchaseBasic = factionId != null && _purchaseService.CanAfford(DefenderTier.Basic, 1);
            var canPurchaseMedium = factionId != null && _purchaseService.CanAfford(DefenderTier.Medium, 1);
            var canPurchaseHeavy = factionId != null && _purchaseService.CanAfford(DefenderTier.Heavy, 1);

            var purchaseBasicItem = new MenuItem(
                PurchaseBasicItemId,
                $"Buy Basic Troop (${basicCost})",
                "Pistol, no armor");
            purchaseBasicItem.IsEnabled = canPurchaseBasic;
            menu.AddItem(purchaseBasicItem);

            var purchaseMediumItem = new MenuItem(
                PurchaseMediumItemId,
                $"Buy Medium Troop (${mediumCost})",
                "SMG, light armor");
            purchaseMediumItem.IsEnabled = canPurchaseMedium;
            menu.AddItem(purchaseMediumItem);

            var purchaseHeavyItem = new MenuItem(
                PurchaseHeavyItemId,
                $"Buy Heavy Troop (${heavyCost})",
                "Carbine, full armor");
            purchaseHeavyItem.IsEnabled = canPurchaseHeavy;
            menu.AddItem(purchaseHeavyItem);
        }

        private void AddRecruitmentItems(MenuDefinition menu, string? factionId)
        {
            var basicCost = _purchaseService.GetTroopCost(DefenderTier.Basic);
            var mediumCost = _purchaseService.GetTroopCost(DefenderTier.Medium);
            var heavyCost = _purchaseService.GetTroopCost(DefenderTier.Heavy);
            var followerCount = factionId != null ? _followerService.GetFollowerCount(factionId) : 0;
            var maxFollowers = _followerService.GetMaxFollowers();

            var followerSummaryItem = new MenuItem(
                FollowerSummaryItemId,
                $"Followers: {followerCount}/{maxFollowers}",
                "Bodyguards that fight with you");
            followerSummaryItem.IsEnabled = false;
            menu.AddItem(followerSummaryItem);

            var canRecruit = factionId != null && followerCount < maxFollowers;

            var recruitBasicItem = new MenuItem(
                RecruitBasicItemId,
                $"Recruit Basic Follower (${basicCost})",
                "Spawns a basic bodyguard");
            recruitBasicItem.IsEnabled = canRecruit && _purchaseService.CanAfford(DefenderTier.Basic, 1);
            menu.AddItem(recruitBasicItem);

            var recruitMediumItem = new MenuItem(
                RecruitMediumItemId,
                $"Recruit Medium Follower (${mediumCost})",
                "Spawns a medium bodyguard");
            recruitMediumItem.IsEnabled = canRecruit && _purchaseService.CanAfford(DefenderTier.Medium, 1);
            menu.AddItem(recruitMediumItem);

            var recruitHeavyItem = new MenuItem(
                RecruitHeavyItemId,
                $"Recruit Heavy Follower (${heavyCost})",
                "Spawns a heavy bodyguard");
            recruitHeavyItem.IsEnabled = canRecruit && _purchaseService.CanAfford(DefenderTier.Heavy, 1);
            menu.AddItem(recruitHeavyItem);
        }

        private static void AddArmyNavigationItems(MenuDefinition menu)
        {
            var manageFollowersItem = new MenuItem(
                ManageFollowersItemId,
                "Manage Followers",
                "View and dismiss followers");
            menu.AddItem(manageFollowersItem);

            var backItem = new MenuItem(
                BackItemId,
                "Back",
                "Return to main menu");
            menu.AddItem(backItem);
        }

        /// <summary>
        /// Shows the follower list menu.
        /// </summary>
    }
}
