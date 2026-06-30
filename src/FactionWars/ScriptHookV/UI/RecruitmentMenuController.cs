using FactionWars.Core.Interfaces;
using FactionWars.UI.Interfaces;
using FactionWars.UI.Models;
using System;

namespace FactionWars.ScriptHookV.UI
{
    /// <summary>
    /// Controller for the Recruitment submenu. Provides navigation to the Squad submenu.
    /// </summary>
    public class RecruitmentMenuController
    {
        /// <summary>
        /// Menu ID for the recruitment menu.
        /// </summary>
        public const string MenuId = "recruitment_menu";

        /// <summary>
        /// Item ID for cash display.
        /// </summary>
        public const string CashDisplayItemId = "cash_display";

        /// <summary>
        /// Item ID for squad option.
        /// </summary>
        public const string SquadItemId = "squad";

        /// <summary>
        /// Item ID for back button.
        /// </summary>
        public const string BackItemId = "back";

        private readonly IMenuProvider _menuProvider;
        private readonly IGameBridge _gameBridge;

        /// <summary>
        /// Event raised when the user selects the Squad option.
        /// </summary>
        public event EventHandler? SquadRequested;

        /// <summary>
        /// Event raised when the user selects the back option.
        /// </summary>
        public event EventHandler? BackRequested;

        /// <summary>
        /// Creates a new RecruitmentMenuController with the specified dependencies.
        /// </summary>
        public RecruitmentMenuController(IMenuProvider menuProvider, IGameBridge gameBridge)
        {
            _menuProvider = menuProvider ?? throw new ArgumentNullException(nameof(menuProvider));
            _gameBridge = gameBridge ?? throw new ArgumentNullException(nameof(gameBridge));

            _menuProvider.ItemSelected += OnItemSelected;
        }

        /// <summary>
        /// Shows the recruitment menu.
        /// </summary>
        public void Show()
        {
            var menu = new MenuDefinition(MenuId, "Recruitment", "Troops & Bodyguards");

            // Cash display
            var playerMoney = _gameBridge.GetPlayerMoney();
            var cashItem = new MenuItem(CashDisplayItemId, $"Cash: ${playerMoney:N0}", "Your available funds");
            cashItem.IsEnabled = false;
            menu.AddItem(cashItem);

            // Squad option
            var squadItem = new MenuItem(
                SquadItemId,
                "Squad",
                "Recruit and manage bodyguards");
            menu.AddItem(squadItem);

            // Back button
            menu.AddItem(new MenuItem(BackItemId, "Back", "Return to main menu"));

            _menuProvider.ShowMenu(menu);
        }

        private void OnItemSelected(object? sender, MenuItemSelectedEventArgs e)
        {
            if (e.MenuId != MenuId) return;

            switch (e.ItemId)
            {
                case SquadItemId:
                    SquadRequested?.Invoke(this, EventArgs.Empty);
                    break;

                case BackItemId:
                    _menuProvider.CloseMenu();
                    BackRequested?.Invoke(this, EventArgs.Empty);
                    break;
            }
        }
    }
}
