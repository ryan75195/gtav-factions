using FactionWars.Core.Interfaces;
using FactionWars.Economy.Interfaces;
using FactionWars.UI.Interfaces;
using FactionWars.UI.Models;
using System;

namespace FactionWars.ScriptHookV.UI
{
    /// <summary>
    /// Controller for the Support submenu. Allows purchasing support-squad packages
    /// from the commander NPC.
    /// </summary>
    public class SupportMenuController
    {
        /// <summary>
        /// Menu ID for the support menu.
        /// </summary>
        public const string MenuId = "support_menu";

        /// <summary>
        /// Item ID for the support squad purchase option.
        /// </summary>
        public const string SupportSquadItemId = "buy_support_squad";

        /// <summary>
        /// Item ID for cash display.
        /// </summary>
        public const string CashDisplayItemId = "cash_display";

        /// <summary>
        /// Item ID for owned-count display.
        /// </summary>
        public const string OwnedDisplayItemId = "owned_display";

        /// <summary>
        /// Item ID for back button.
        /// </summary>
        public const string BackItemId = "back";

        private readonly IMenuProvider _menuProvider;
        private readonly IGameBridge _gameBridge;
        private readonly ISupportPackageService _supportService;
        private readonly IPlayerContext _playerContext;

        /// <summary>
        /// Event raised when the user selects the back option.
        /// </summary>
        public event EventHandler? BackRequested;

        /// <summary>
        /// Creates a new SupportMenuController with the specified dependencies.
        /// </summary>
        public SupportMenuController(
            IMenuProvider menuProvider,
            IGameBridge gameBridge,
            ISupportPackageService supportService,
            IPlayerContext playerContext)
        {
            _menuProvider = menuProvider ?? throw new ArgumentNullException(nameof(menuProvider));
            _gameBridge = gameBridge ?? throw new ArgumentNullException(nameof(gameBridge));
            _supportService = supportService ?? throw new ArgumentNullException(nameof(supportService));
            _playerContext = playerContext ?? throw new ArgumentNullException(nameof(playerContext));

            _menuProvider.ItemSelected += OnItemSelected;
        }

        /// <summary>
        /// Shows the support menu.
        /// </summary>
        public void Show()
        {
            var menu = new MenuDefinition(MenuId, "Support", "Battle Reinforcements");
            var factionId = _playerContext.CurrentFactionId;

            AddCashItem(menu);
            AddOwnedItem(menu, factionId);
            AddSupportSquadItem(menu);

            menu.AddItem(new MenuItem(BackItemId, "Back", "Return to main menu"));

            _menuProvider.ShowMenu(menu);
        }

        private void AddCashItem(MenuDefinition menu)
        {
            var playerMoney = _gameBridge.GetPlayerMoney();
            var cashItem = new MenuItem(CashDisplayItemId, $"Cash: ${playerMoney:N0}", "Your available funds");
            cashItem.IsEnabled = false;
            menu.AddItem(cashItem);
        }

        private void AddOwnedItem(MenuDefinition menu, string? factionId)
        {
            var ownedCount = factionId != null ? _supportService.GetOwnedCount(factionId) : 0;
            var ownedItem = new MenuItem(OwnedDisplayItemId, $"Support Squads owned: {ownedCount}", "Squads ready to call in");
            ownedItem.IsEnabled = false;
            menu.AddItem(ownedItem);
        }

        private void AddSupportSquadItem(MenuDefinition menu)
        {
            var cost = _supportService.GetSupportSquadCost();
            var item = new MenuItem(
                SupportSquadItemId,
                $"Support Squad (${cost:N0})",
                "FBI SUV of 8 allies — call one into a zone");
            item.IsEnabled = _supportService.CanAfford();
            menu.AddItem(item);
        }

        private void OnItemSelected(object? sender, MenuItemSelectedEventArgs e)
        {
            if (e.MenuId != MenuId) return;

            if (e.ItemId == BackItemId)
            {
                _menuProvider.CloseMenu();
                BackRequested?.Invoke(this, EventArgs.Empty);
                return;
            }

            if (e.ItemId == SupportSquadItemId)
            {
                PurchaseSupportSquad();
            }
        }

        private void PurchaseSupportSquad()
        {
            var factionId = _playerContext.CurrentFactionId;
            if (factionId != null && _supportService.PurchaseSupportSquad(factionId))
            {
                _gameBridge.ShowNotification("~g~Support Squad purchased!");
            }

            Show();
        }
    }
}
