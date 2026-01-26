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
    /// Controller for the Army submenu. Allows purchasing troops to reserve,
    /// viewing reserves, and recruiting/managing followers.
    /// </summary>
    public class ArmyMenuController
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
        private readonly FollowerManager? _followerManager;
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
        public ArmyMenuController(
            IMenuProvider menuProvider,
            IFactionService factionService,
            ITroopPurchaseService purchaseService,
            IFollowerService followerService,
            IDefenderTierService tierService,
            IPlayerContext playerContext,
            FollowerManager? followerManager = null,
            IGameBridge? gameBridge = null)
        {
            _menuProvider = menuProvider ?? throw new ArgumentNullException(nameof(menuProvider));
            _factionService = factionService ?? throw new ArgumentNullException(nameof(factionService));
            _purchaseService = purchaseService ?? throw new ArgumentNullException(nameof(purchaseService));
            _followerService = followerService ?? throw new ArgumentNullException(nameof(followerService));
            _tierService = tierService ?? throw new ArgumentNullException(nameof(tierService));
            _playerContext = playerContext ?? throw new ArgumentNullException(nameof(playerContext));
            _followerManager = followerManager;
            _gameBridge = gameBridge;

            // Subscribe to menu item selection events
            _menuProvider.ItemSelected += OnItemSelected;
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

            // Money display
            var playerMoney = _purchaseService.GetPlayerMoney();
            var moneyItem = new MenuItem(
                MoneyDisplayItemId,
                $"Cash: ${playerMoney:N0}",
                "Your available funds");
            moneyItem.IsEnabled = false;
            menu.AddItem(moneyItem);

            // Reserve pool summary
            var basicReserve = factionState?.GetReserveTroops(DefenderTier.Basic) ?? 0;
            var mediumReserve = factionState?.GetReserveTroops(DefenderTier.Medium) ?? 0;
            var heavyReserve = factionState?.GetReserveTroops(DefenderTier.Heavy) ?? 0;

            var reserveItem = new MenuItem(
                ReserveSummaryItemId,
                $"Reserves: B:{basicReserve} M:{mediumReserve} H:{heavyReserve}",
                "Troops available for deployment");
            reserveItem.IsEnabled = false;
            menu.AddItem(reserveItem);

            // Purchase section
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

            // Follower section
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

            var manageFollowersItem = new MenuItem(
                ManageFollowersItemId,
                "Manage Followers",
                "View and dismiss followers");
            menu.AddItem(manageFollowersItem);

            // Back navigation
            var backItem = new MenuItem(
                BackItemId,
                "Back",
                "Return to main menu");
            menu.AddItem(backItem);

            _menuProvider.ShowMenu(menu, _lastSelectedItemId);
        }

        /// <summary>
        /// Shows the follower list menu.
        /// </summary>
        private void ShowFollowerListMenu()
        {
            var factionId = _playerContext.CurrentFactionId;
            var followers = factionId != null
                ? _followerService.GetFollowers(factionId)
                : Array.Empty<Follower>();

            var menu = new MenuDefinition(FollowerListMenuId, "Followers", "Manage your bodyguards");

            if (followers.Count == 0)
            {
                var noFollowersItem = new MenuItem(
                    NoFollowersItemId,
                    "No followers",
                    "Recruit followers from the Army menu");
                noFollowersItem.IsEnabled = false;
                menu.AddItem(noFollowersItem);
            }
            else
            {
                foreach (var follower in followers)
                {
                    var followerItem = new MenuItem(
                        $"follower_{follower.Id}",
                        $"{follower.Tier} Follower",
                        $"Recruited {FormatServiceTime(follower.GetServiceTime())} ago");
                    menu.AddItem(followerItem);
                }
            }

            // Back navigation
            var backItem = new MenuItem(
                FollowerListBackItemId,
                "Back",
                "Return to army menu");
            menu.AddItem(backItem);

            _menuProvider.ShowMenu(menu);
        }

        /// <summary>
        /// Shows the follower detail menu.
        /// </summary>
        private void ShowFollowerDetailMenu(Guid followerId)
        {
            _selectedFollowerId = followerId;
            var follower = _followerService.GetFollowerById(followerId);

            if (follower == null)
            {
                ShowFollowerListMenu();
                return;
            }

            var menu = new MenuDefinition(FollowerDetailMenuId, $"{follower.Tier} Follower", "Follower details");

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
                "Dismiss Follower",
                "Send this follower away (no refund)");
            menu.AddItem(dismissItem);

            // Back navigation
            var backItem = new MenuItem(
                FollowerDetailBackItemId,
                "Back",
                "Return to follower list");
            menu.AddItem(backItem);

            _menuProvider.ShowMenu(menu);
        }

        /// <summary>
        /// Handles menu item selection events.
        /// </summary>
        private void OnItemSelected(object? sender, MenuItemSelectedEventArgs e)
        {
            if (e.MenuId == ArmyMenuId)
            {
                HandleArmyMenuSelection(e.ItemId);
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

        /// <summary>
        /// Handles item selection in the main army menu.
        /// </summary>
        private void HandleArmyMenuSelection(string itemId)
        {
            var factionId = _playerContext.CurrentFactionId;

            // Store the selected item ID for cursor retention on menu refresh
            _lastSelectedItemId = itemId;

            switch (itemId)
            {
                case BackItemId:
                    _lastSelectedItemId = null; // Clear on navigation away
                    _menuProvider.CloseMenu();
                    BackRequested?.Invoke(this, EventArgs.Empty);
                    break;

                case PurchaseBasicItemId:
                    if (factionId != null)
                    {
                        _purchaseService.PurchaseTroops(factionId, DefenderTier.Basic, 1);
                        ShowArmyMenu();
                    }
                    break;

                case PurchaseMediumItemId:
                    if (factionId != null)
                    {
                        _purchaseService.PurchaseTroops(factionId, DefenderTier.Medium, 1);
                        ShowArmyMenu();
                    }
                    break;

                case PurchaseHeavyItemId:
                    if (factionId != null)
                    {
                        _purchaseService.PurchaseTroops(factionId, DefenderTier.Heavy, 1);
                        ShowArmyMenu();
                    }
                    break;

                case RecruitBasicItemId:
                    if (factionId != null)
                    {
                        RecruitFollower(factionId, DefenderTier.Basic);
                    }
                    break;

                case RecruitMediumItemId:
                    if (factionId != null)
                    {
                        RecruitFollower(factionId, DefenderTier.Medium);
                    }
                    break;

                case RecruitHeavyItemId:
                    if (factionId != null)
                    {
                        RecruitFollower(factionId, DefenderTier.Heavy);
                    }
                    break;

                case ManageFollowersItemId:
                    _lastSelectedItemId = null; // Clear when navigating to submenu
                    ShowFollowerListMenu();
                    break;
            }
        }

        /// <summary>
        /// Handles item selection in the follower list menu.
        /// </summary>
        private void HandleFollowerListSelection(string itemId)
        {
            if (itemId == FollowerListBackItemId)
            {
                ShowArmyMenu();
                return;
            }

            // Check if it's a follower item
            if (itemId.StartsWith("follower_"))
            {
                var followerIdStr = itemId.Substring("follower_".Length);
                if (Guid.TryParse(followerIdStr, out var followerId))
                {
                    ShowFollowerDetailMenu(followerId);
                }
            }
        }

        /// <summary>
        /// Handles item selection in the follower detail menu.
        /// </summary>
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

        /// <summary>
        /// Recruits a follower of the specified tier.
        /// Uses FollowerManager if available, which handles both domain and game world spawning.
        /// </summary>
        private void RecruitFollower(string factionId, DefenderTier tier)
        {
            // Use FollowerManager if available (handles both domain and game world spawning)
            if (_followerManager != null)
            {
                var result = _followerManager.RecruitFollower(factionId, tier);

                if (result.Success)
                {
                    _gameBridge?.ShowNotification($"~g~Recruited {tier} follower");
                }
                else
                {
                    var errorMsg = result.FailureReason switch
                    {
                        FollowerRecruitFailureReason.MaxFollowersReached => "Max followers reached",
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
                // Fallback to domain-only recruitment (no actual ped spawning)
                var purchaseResult = _purchaseService.PurchaseTroops(factionId, tier, 1);
                if (purchaseResult.Success)
                {
                    var result = _followerService.Recruit(factionId, tier);
                    if (!result.Success)
                    {
                        // If recruitment fails, troop goes to reserve as a fallback
                    }
                }
            }
            ShowArmyMenu();
        }

        /// <summary>
        /// Formats a time span into a human-readable string.
        /// </summary>
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
