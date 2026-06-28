using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.Economy.Interfaces;
using FactionWars.Factions.Interfaces;
using FactionWars.UI.Interfaces;
using FactionWars.UI.Models;
using System;

namespace FactionWars.ScriptHookV.UI
{
    /// <summary>
    /// Controller for the Defenders submenu. Allows purchasing troops for zone defense.
    /// </summary>
    public class DefendersMenuController
    {
        /// <summary>
        /// Menu ID for the defenders menu.
        /// </summary>
        public const string MenuId = "defenders_menu";

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
        /// Item ID for purchasing elite troops.
        /// </summary>
        public const string PurchaseEliteItemId = "purchase_elite";

        /// <summary>
        /// Item ID for purchasing sniper troops.
        /// </summary>
        public const string PurchaseSniperItemId = "purchase_sniper";

        /// <summary>
        /// Item ID for back button.
        /// </summary>
        public const string BackItemId = "back";

        private readonly IMenuProvider _menuProvider;
        private readonly IFactionService _factionService;
        private readonly ITroopPurchaseService _purchaseService;
        private readonly IPlayerContext _playerContext;

        private string? _lastSelectedItemId;

        /// <summary>
        /// Event raised when the user selects the back option.
        /// </summary>
        public event EventHandler? BackRequested;

        /// <summary>
        /// Creates a new DefendersMenuController with the specified dependencies.
        /// </summary>
        public DefendersMenuController(
            IMenuProvider menuProvider,
            IFactionService factionService,
            ITroopPurchaseService purchaseService,
            IPlayerContext playerContext)
        {
            _menuProvider = menuProvider ?? throw new ArgumentNullException(nameof(menuProvider));
            _factionService = factionService ?? throw new ArgumentNullException(nameof(factionService));
            _purchaseService = purchaseService ?? throw new ArgumentNullException(nameof(purchaseService));
            _playerContext = playerContext ?? throw new ArgumentNullException(nameof(playerContext));

            _menuProvider.ItemSelected += OnItemSelected;
        }

        /// <summary>
        /// Shows the defenders menu.
        /// </summary>
        public void Show()
        {
            _lastSelectedItemId = null;
            ShowDefendersMenu();
        }

        private void ShowDefendersMenu()
        {
            var factionId = _playerContext.CurrentFactionId;
            var factionState = factionId != null ? _factionService.GetFactionState(factionId) : null;

            var menu = new MenuDefinition(MenuId, "Defenders", "Zone Defense Troops");

            // Money display
            var playerMoney = _purchaseService.GetPlayerMoney();
            var moneyItem = new MenuItem(
                MoneyDisplayItemId,
                $"Cash: ${playerMoney:N0}",
                "Your available funds");
            moneyItem.IsEnabled = false;
            menu.AddItem(moneyItem);

            // Reserve pool summary
            var basicReserve = factionState?.GetReserveTroops(DefenderRole.Grunt) ?? 0;
            var mediumReserve = factionState?.GetReserveTroops(DefenderRole.Gunner) ?? 0;
            var heavyReserve = factionState?.GetReserveTroops(DefenderRole.Rifleman) ?? 0;
            var eliteReserve = factionState?.GetReserveTroops(DefenderRole.Rocketeer) ?? 0;

            var reserveItem = new MenuItem(
                ReserveSummaryItemId,
                $"Reserves: B:{basicReserve} M:{mediumReserve} H:{heavyReserve} E:{eliteReserve}",
                "Troops available for deployment");
            reserveItem.IsEnabled = false;
            menu.AddItem(reserveItem);

            // Purchase options for each tier
            AddPurchaseItem(menu, PurchaseBasicItemId, DefenderRole.Grunt, "Pistol, light armor", factionId);
            AddPurchaseItem(menu, PurchaseMediumItemId, DefenderRole.Gunner, "SMG, medium armor", factionId);
            AddPurchaseItem(menu, PurchaseHeavyItemId, DefenderRole.Rifleman, "Carbine, full armor", factionId);
            AddPurchaseItem(menu, PurchaseEliteItemId, DefenderRole.Rocketeer, "RPG, anti-vehicle specialist", factionId);
            AddPurchaseItem(menu, PurchaseSniperItemId, DefenderRole.Sniper, "Sniper rifle, long-range specialist", factionId);

            // Back navigation
            var backItem = new MenuItem(
                BackItemId,
                "Back",
                "Return to recruitment menu");
            menu.AddItem(backItem);

            _menuProvider.ShowMenu(menu, _lastSelectedItemId);
            _menuProvider.HoldToRepeatEnabled = true; // Enable hold-to-repeat for troop purchases
        }

        private void AddPurchaseItem(MenuDefinition menu, string itemId, DefenderRole tier, string description, string? factionId)
        {
            var cost = _purchaseService.GetTroopCost(tier);
            var canPurchase = factionId != null && _purchaseService.CanAfford(tier, 1);

            var item = new MenuItem(
                itemId,
                $"Buy {tier} Troop (${cost:N0})",
                description);
            item.IsEnabled = canPurchase;
            menu.AddItem(item);
        }

        private void OnItemSelected(object? sender, MenuItemSelectedEventArgs e)
        {
            if (e.MenuId != MenuId) return;

            var factionId = _playerContext.CurrentFactionId;
            _lastSelectedItemId = e.ItemId;

            switch (e.ItemId)
            {
                case BackItemId:
                    _lastSelectedItemId = null;
                    _menuProvider.CloseMenu();
                    BackRequested?.Invoke(this, EventArgs.Empty);
                    break;

                case PurchaseBasicItemId:
                    PurchaseTroop(factionId, DefenderRole.Grunt);
                    break;

                case PurchaseMediumItemId:
                    PurchaseTroop(factionId, DefenderRole.Gunner);
                    break;

                case PurchaseHeavyItemId:
                    PurchaseTroop(factionId, DefenderRole.Rifleman);
                    break;

                case PurchaseEliteItemId:
                    PurchaseTroop(factionId, DefenderRole.Rocketeer);
                    break;

                case PurchaseSniperItemId:
                    PurchaseTroop(factionId, DefenderRole.Sniper);
                    break;
            }
        }

        private void PurchaseTroop(string? factionId, DefenderRole tier)
        {
            if (factionId != null)
            {
                _purchaseService.PurchaseTroops(factionId, tier, 1);
                ShowDefendersMenu();
            }
        }
    }
}
