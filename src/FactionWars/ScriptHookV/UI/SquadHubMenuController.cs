using FactionWars.Core.Interfaces;
using FactionWars.UI.Interfaces;
using FactionWars.UI.Models;
using System;

namespace FactionWars.ScriptHookV.UI
{
    /// <summary>
    /// Controller for the Squad hub submenu. Routes to bodyguard management and to calling
    /// in a purchased support squad.
    /// </summary>
    public class SquadHubMenuController
    {
        /// <summary>
        /// Menu ID for the squad hub menu.
        /// </summary>
        public const string MenuId = "squad_hub_menu";

        /// <summary>
        /// Item ID for the manage-squad option.
        /// </summary>
        public const string ManageSquadItemId = "manage_squad";

        /// <summary>
        /// Item ID for the support option.
        /// </summary>
        public const string SupportItemId = "support";

        /// <summary>
        /// Item ID for back button.
        /// </summary>
        public const string BackItemId = "back";

        private readonly IMenuProvider _menuProvider;
        private readonly IGameBridge _gameBridge;

        /// <summary>
        /// Event raised when the user selects the Manage Squad option.
        /// </summary>
        public event EventHandler? ManageSquadRequested;

        /// <summary>
        /// Event raised when the user selects the Support option.
        /// </summary>
        public event EventHandler? SupportRequested;

        /// <summary>
        /// Event raised when the user selects the back option.
        /// </summary>
        public event EventHandler? BackRequested;

        /// <summary>
        /// Creates a new SquadHubMenuController with the specified dependencies.
        /// </summary>
        public SquadHubMenuController(IMenuProvider menuProvider, IGameBridge gameBridge)
        {
            _menuProvider = menuProvider ?? throw new ArgumentNullException(nameof(menuProvider));
            _gameBridge = gameBridge ?? throw new ArgumentNullException(nameof(gameBridge));

            _menuProvider.ItemSelected += OnItemSelected;
        }

        /// <summary>
        /// Shows the squad hub menu.
        /// </summary>
        public void Show()
        {
            var menu = new MenuDefinition(MenuId, "Squad", "Manage & Support");

            menu.AddItem(new MenuItem(
                ManageSquadItemId,
                "Manage Squad",
                "Recruit and manage bodyguards"));

            menu.AddItem(new MenuItem(
                SupportItemId,
                "Support",
                "Call in a purchased support squad"));

            // The hub can be opened from the Recruitment menu or directly from gameplay
            // (d-pad-left tap), so the back copy stays destination-neutral.
            menu.AddItem(new MenuItem(BackItemId, "Back", "Go back"));

            _menuProvider.ShowMenu(menu);
        }

        private void OnItemSelected(object? sender, MenuItemSelectedEventArgs e)
        {
            if (e.MenuId != MenuId) return;

            switch (e.ItemId)
            {
                case ManageSquadItemId:
                    ManageSquadRequested?.Invoke(this, EventArgs.Empty);
                    break;

                case SupportItemId:
                    SupportRequested?.Invoke(this, EventArgs.Empty);
                    break;

                case BackItemId:
                    _menuProvider.CloseMenu();
                    BackRequested?.Invoke(this, EventArgs.Empty);
                    break;
            }
        }
    }
}
