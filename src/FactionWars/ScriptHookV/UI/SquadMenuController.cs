using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.Economy.Interfaces;
using FactionWars.Factions.Interfaces;
using FactionWars.ScriptHookV.Managers;
using FactionWars.UI.Interfaces;
using FactionWars.UI.Models;
using System;
using System.Linq;

namespace FactionWars.ScriptHookV.UI
{
    /// <summary>
    /// Controller for the Squad submenu. Allows recruiting and managing bodyguard followers.
    /// </summary>
    public class SquadMenuController
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
        public SquadMenuController(
            IMenuProvider menuProvider,
            ITroopPurchaseService purchaseService,
            IFollowerService followerService,
            IPlayerContext playerContext,
            IFollowerManager? followerManager = null,
            IGameBridge? gameBridge = null)
        {
            _menuProvider = menuProvider ?? throw new ArgumentNullException(nameof(menuProvider));
            _purchaseService = purchaseService ?? throw new ArgumentNullException(nameof(purchaseService));
            _followerService = followerService ?? throw new ArgumentNullException(nameof(followerService));
            _playerContext = playerContext ?? throw new ArgumentNullException(nameof(playerContext));
            _followerManager = followerManager;
            _gameBridge = gameBridge;

            _menuProvider.ItemSelected += OnItemSelected;
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
            AddRecruitItem(menu, RecruitBasicItemId, DefenderTier.Basic, "Pistol bodyguard", canRecruit);
            AddRecruitItem(menu, RecruitMediumItemId, DefenderTier.Medium, "SMG bodyguard", canRecruit);
            AddRecruitItem(menu, RecruitHeavyItemId, DefenderTier.Heavy, "Carbine bodyguard", canRecruit);
            AddRecruitItem(menu, RecruitEliteItemId, DefenderTier.Elite, "RPG bodyguard", canRecruit);

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

        private void AddRecruitItem(MenuDefinition menu, string itemId, DefenderTier tier, string description, bool canRecruit)
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

        private void ShowFollowerListMenu()
        {
            var factionId = _playerContext.CurrentFactionId;
            var followers = factionId != null
                ? _followerService.GetFollowers(factionId)
                : Array.Empty<Follower>();

            var menu = new MenuDefinition(FollowerListMenuId, "Squad", "Manage your bodyguards");

            if (followers.Count == 0)
            {
                var noFollowersItem = new MenuItem(
                    NoFollowersItemId,
                    "No bodyguards",
                    "Recruit bodyguards from the Squad menu");
                noFollowersItem.IsEnabled = false;
                menu.AddItem(noFollowersItem);
            }
            else
            {
                foreach (var follower in followers)
                {
                    var followerItem = new MenuItem(
                        $"follower_{follower.Id}",
                        $"{follower.Tier} Bodyguard",
                        $"Recruited {FormatServiceTime(follower.GetServiceTime())} ago");
                    menu.AddItem(followerItem);
                }
            }

            // Back navigation
            var backItem = new MenuItem(
                FollowerListBackItemId,
                "Back",
                "Return to squad menu");
            menu.AddItem(backItem);

            _menuProvider.ShowMenu(menu);
        }

        private void ShowFollowerDetailMenu(Guid followerId)
        {
            _selectedFollowerId = followerId;
            var follower = _followerService.GetFollowerById(followerId);

            if (follower == null)
            {
                ShowFollowerListMenu();
                return;
            }

            var menu = new MenuDefinition(FollowerDetailMenuId, $"{follower.Tier} Bodyguard", "Bodyguard details");

            // Follower info (disabled display items)
            var tierItem = new MenuItem(
                "tier_info",
                $"Tier: {follower.Tier}",
                "Combat tier determines equipment and effectiveness");
            tierItem.IsEnabled = false;
            menu.AddItem(tierItem);

            var serviceItem = new MenuItem(
                "service_info",
                $"Service time: {FormatServiceTime(follower.GetServiceTime())}",
                "Time since recruitment");
            serviceItem.IsEnabled = false;
            menu.AddItem(serviceItem);

            // Dismiss option
            var dismissItem = new MenuItem(
                DismissFollowerItemId,
                "Dismiss Bodyguard",
                "Send this bodyguard away (no refund)");
            menu.AddItem(dismissItem);

            // Back navigation
            var backItem = new MenuItem(
                FollowerDetailBackItemId,
                "Back",
                "Return to squad list");
            menu.AddItem(backItem);

            _menuProvider.ShowMenu(menu);
        }

        private void OnItemSelected(object? sender, MenuItemSelectedEventArgs e)
        {
            if (e.MenuId == MenuId)
            {
                HandleSquadMenuSelection(e.ItemId);
            }
            else if (e.MenuId == FollowerListMenuId)
            {
                HandleFollowerListSelection(e.ItemId);
            }
            else if (e.MenuId == FollowerDetailMenuId)
            {
                HandleFollowerDetailSelection(e.ItemId);
            }
        }

        private void HandleSquadMenuSelection(string itemId)
        {
            var factionId = _playerContext.CurrentFactionId;
            _lastSelectedItemId = itemId;

            switch (itemId)
            {
                case BackItemId:
                    _lastSelectedItemId = null;
                    _menuProvider.CloseMenu();
                    BackRequested?.Invoke(this, EventArgs.Empty);
                    break;

                case RecruitBasicItemId:
                    if (factionId != null) RecruitFollower(factionId, DefenderTier.Basic);
                    break;

                case RecruitMediumItemId:
                    if (factionId != null) RecruitFollower(factionId, DefenderTier.Medium);
                    break;

                case RecruitHeavyItemId:
                    if (factionId != null) RecruitFollower(factionId, DefenderTier.Heavy);
                    break;

                case RecruitEliteItemId:
                    if (factionId != null) RecruitFollower(factionId, DefenderTier.Elite);
                    break;

                case ManageFollowersItemId:
                    _lastSelectedItemId = null;
                    ShowFollowerListMenu();
                    break;
            }
        }

        private void HandleFollowerListSelection(string itemId)
        {
            if (itemId == FollowerListBackItemId)
            {
                ShowSquadMenu();
                return;
            }

            if (itemId.StartsWith("follower_"))
            {
                var followerIdStr = itemId.Substring("follower_".Length);
                if (Guid.TryParse(followerIdStr, out var followerId))
                {
                    ShowFollowerDetailMenu(followerId);
                }
            }
        }

        private void HandleFollowerDetailSelection(string itemId)
        {
            switch (itemId)
            {
                case FollowerDetailBackItemId:
                    ShowFollowerListMenu();
                    break;

                case DismissFollowerItemId:
                    if (_selectedFollowerId.HasValue)
                    {
                        _followerService.DismissFollower(_selectedFollowerId.Value);
                        _selectedFollowerId = null;
                        ShowFollowerListMenu();
                    }
                    break;
            }
        }

        private void RecruitFollower(string factionId, DefenderTier tier)
        {
            if (_followerManager != null)
            {
                var result = _followerManager.RecruitFollower(factionId, tier);

                if (result.Success)
                {
                    _gameBridge?.ShowNotification($"~g~Recruited {tier} bodyguard");
                }
                else
                {
                    var errorMsg = result.FailureReason switch
                    {
                        FollowerRecruitFailureReason.MaxFollowersReached => "Max squad size reached",
                        FollowerRecruitFailureReason.InsufficientFunds => "Insufficient funds",
                        FollowerRecruitFailureReason.InvalidFaction => "Invalid faction",
                        FollowerRecruitFailureReason.SpawnFailed => "Spawn failed",
                        _ => "Unknown error"
                    };
                    _gameBridge?.ShowNotification($"~r~Failed: {errorMsg}");
                }
            }
            else
            {
                // Fallback to domain-only recruitment
                var purchaseResult = _purchaseService.PurchaseTroops(factionId, tier, 1);
                if (purchaseResult.Success)
                {
                    _followerService.Recruit(factionId, tier);
                }
            }
            ShowSquadMenu();
        }

        private static string FormatServiceTime(TimeSpan time)
        {
            if (time.TotalDays >= 1)
            {
                return $"{(int)time.TotalDays}d";
            }
            if (time.TotalHours >= 1)
            {
                return $"{(int)time.TotalHours}h";
            }
            if (time.TotalMinutes >= 1)
            {
                return $"{(int)time.TotalMinutes}m";
            }
            return $"{(int)time.TotalSeconds}s";
        }
    }
}
