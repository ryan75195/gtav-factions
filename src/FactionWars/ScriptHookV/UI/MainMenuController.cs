using FactionWars.UI.Interfaces;
using FactionWars.UI.Models;
using System;

namespace FactionWars.ScriptHookV.UI
{
    /// <summary>
    /// Controller for the main menu system. Handles F7 key to toggle menu visibility
    /// and manages navigation between menu screens.
    /// </summary>
    public class MainMenuController
    {
        /// <summary>
        /// Key code for F7 key used to toggle menu.
        /// </summary>
        public const int MenuToggleKeyCode = 118;

        /// <summary>
        /// Menu ID for the main menu.
        /// </summary>
        public const string MainMenuId = "main_menu";

        /// <summary>
        /// Item ID for the Zone Management submenu option.
        /// </summary>
        public const string ZoneManagementItemId = "zone_management";

        /// <summary>
        /// Item ID for the Recruitment submenu option.
        /// </summary>
        public const string RecruitmentItemId = "recruitment";

        /// <summary>
        /// Item ID for the Shop submenu option.
        /// </summary>
        public const string ShopItemId = "shop";

        /// <summary>
        /// Item ID for the Settings submenu option.
        /// </summary>
        public const string SettingsItemId = "settings";

        private readonly IMenuProvider _menuProvider;

        /// <summary>
        /// Gets whether the menu is currently open.
        /// </summary>
        public bool IsMenuOpen => _menuProvider.IsMenuVisible;

        /// <summary>
        /// Creates a new MainMenuController with the specified menu provider.
        /// </summary>
        /// <param name="menuProvider">The menu provider for displaying menus.</param>
        /// <exception cref="ArgumentNullException">Thrown if menuProvider is null.</exception>
        public MainMenuController(IMenuProvider menuProvider)
        {
            _menuProvider = menuProvider ?? throw new ArgumentNullException(nameof(menuProvider));
        }

        /// <summary>
        /// Handles key down events. Toggles menu visibility when F7 is pressed.
        /// </summary>
        /// <param name="keyCode">The virtual key code of the pressed key.</param>
        public void OnKeyDown(int keyCode)
        {
            if (keyCode != MenuToggleKeyCode)
                return;

            if (_menuProvider.IsMenuVisible)
            {
                _menuProvider.CloseMenu();
            }
            else
            {
                ShowMainMenu();
            }
        }

        /// <summary>
        /// Updates the menu system. Called every frame when the menu is open.
        /// </summary>
        public void Update()
        {
            _menuProvider.Update();
        }

        /// <summary>
        /// Creates and displays the main menu with all submenu options.
        /// </summary>
        public void ShowMainMenu()
        {
            var menu = new MenuDefinition(MainMenuId, "Faction Wars", "Territory Control");

            menu.AddItem(new MenuItem(
                ZoneManagementItemId,
                "Zone Management",
                "View zones, allocate and withdraw troops"));

            menu.AddItem(new MenuItem(
                RecruitmentItemId,
                "Recruitment",
                "Purchase troops and recruit followers"));

            menu.AddItem(new MenuItem(
                ShopItemId,
                "Shop",
                "Purchase military vehicles"));

            menu.AddItem(new MenuItem(
                SettingsItemId,
                "Settings",
                "Save, load, and configure options"));

            _menuProvider.ShowMenu(menu);
        }
    }
}
