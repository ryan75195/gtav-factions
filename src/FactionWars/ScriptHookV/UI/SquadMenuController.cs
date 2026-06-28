using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.Economy.Interfaces;
using FactionWars.Factions.Interfaces;
using FactionWars.ScriptHookV.Managers;
using FactionWars.ScriptHookV.Models;
using FactionWars.UI.Interfaces;
using FactionWars.UI.Models;
using System;
using System.Linq;

namespace FactionWars.ScriptHookV.UI
{
    /// <summary>
    /// Controller for the Squad submenu. Allows recruiting and managing bodyguard followers.
    /// </summary>
    public partial class SquadMenuController
    {
        /// <summary>
        /// Menu ID for the squad menu.
        /// </summary>
        public const string MenuId = "squad_menu";

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
        /// Item ID for recruiting elite follower.
        /// </summary>
        public const string RecruitEliteItemId = "recruit_elite";

        /// <summary>
        /// Item ID for recruiting sniper follower.
        /// </summary>
        public const string RecruitSniperItemId = "recruit_sniper";

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
        /// Item ID for back button.
        /// </summary>
        public const string BackItemId = "back";

        private readonly IMenuProvider _menuProvider;
        private readonly ITroopPurchaseService _purchaseService;
        private readonly IFollowerService _followerService;
        private readonly IPlayerContext _playerContext;
        private readonly IFollowerManager? _followerManager;
        private readonly IGameBridge? _gameBridge;

        private Guid? _selectedFollowerId;
        private string? _lastSelectedItemId;

        /// <summary>
        /// Event raised when the user selects the back option.
        /// </summary>
        public event EventHandler? BackRequested;

        /// <summary>
        /// Creates a new SquadMenuController with the specified dependencies.
        /// </summary>
        public SquadMenuController(SquadMenuControllerDependencies dependencies, IFollowerManager? followerManager = null, IGameBridge? gameBridge = null)
        {
            if (dependencies == null) throw new ArgumentNullException(nameof(dependencies));
            _menuProvider = dependencies.MenuProvider ?? throw new ArgumentNullException(nameof(dependencies.MenuProvider));
            _purchaseService = dependencies.PurchaseService ?? throw new ArgumentNullException(nameof(dependencies.PurchaseService));
            _followerService = dependencies.FollowerService ?? throw new ArgumentNullException(nameof(dependencies.FollowerService));
            _playerContext = dependencies.PlayerContext ?? throw new ArgumentNullException(nameof(dependencies.PlayerContext));
            _followerManager = followerManager;
            _gameBridge = gameBridge;

            _menuProvider.ItemSelected += OnItemSelected;
        }

        public SquadMenuController(params object?[] dependencies)
            : this(new SquadMenuControllerDependencies
            {
                MenuProvider = (IMenuProvider?)dependencies[0],
                PurchaseService = (ITroopPurchaseService?)dependencies[1],
                FollowerService = (IFollowerService?)dependencies[2],
                PlayerContext = (IPlayerContext?)dependencies[3]
            })
        {
        }

        /// <summary>
        /// Shows the squad menu.
        /// </summary>
        public void Show()
        {
            _selectedFollowerId = null;
            _lastSelectedItemId = null;
            ShowSquadMenu();
        }

        private void ShowSquadMenu()
        {
            var factionId = _playerContext.CurrentFactionId;

            var menu = new MenuDefinition(MenuId, "Squad", "Bodyguard Recruitment");

            // Money display
            var playerMoney = _purchaseService.GetPlayerMoney();
            var moneyItem = new MenuItem(
                MoneyDisplayItemId,
                $"Cash: ${playerMoney:N0}",
                "Your available funds");
            moneyItem.IsEnabled = false;
            menu.AddItem(moneyItem);

            // Follower summary
            var followerCount = factionId != null ? _followerService.GetFollowerCount(factionId) : 0;
            var maxFollowers = _followerService.GetMaxFollowers();

            var followerSummaryItem = new MenuItem(
                FollowerSummaryItemId,
                $"Squad: {followerCount}/{maxFollowers}",
                "Bodyguards that fight with you");
            followerSummaryItem.IsEnabled = false;
            menu.AddItem(followerSummaryItem);

            var canRecruit = factionId != null && followerCount < maxFollowers;

            // Recruit options for each tier
            AddRecruitItem(menu, RecruitBasicItemId, DefenderRole.Grunt, "Pistol bodyguard", canRecruit);
            AddRecruitItem(menu, RecruitMediumItemId, DefenderRole.Gunner, "SMG bodyguard", canRecruit);
            AddRecruitItem(menu, RecruitHeavyItemId, DefenderRole.Rifleman, "Carbine bodyguard", canRecruit);
            AddRecruitItem(menu, RecruitEliteItemId, DefenderRole.Rocketeer, "RPG bodyguard", canRecruit);
            AddRecruitItem(menu, RecruitSniperItemId, DefenderRole.Sniper, "Sniper rifle bodyguard, long-range", canRecruit);

            // Manage followers
            var manageFollowersItem = new MenuItem(
                ManageFollowersItemId,
                "Manage Squad",
                "View and dismiss bodyguards");
            menu.AddItem(manageFollowersItem);

            // Back navigation
            var backItem = new MenuItem(
                BackItemId,
                "Back",
                "Return to recruitment menu");
            menu.AddItem(backItem);

            _menuProvider.ShowMenu(menu, _lastSelectedItemId);
        }

        private void AddRecruitItem(MenuDefinition menu, string itemId, DefenderRole tier, string description, bool canRecruit)
        {
            var cost = _purchaseService.GetTroopCost(tier);
            var canAfford = _purchaseService.CanAfford(tier, 1);

            var item = new MenuItem(
                itemId,
                $"Recruit {tier} (${cost:N0})",
                description);
            item.IsEnabled = canRecruit && canAfford;
            menu.AddItem(item);
        }

    }
}
