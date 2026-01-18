using FactionWars.UI.Models;
using System;

namespace FactionWars.UI.Interfaces
{
    /// <summary>
    /// Abstraction over NativeUI or similar menu systems.
    /// Allows for unit testing menu logic without depending on actual game UI.
    /// </summary>
    public interface IMenuProvider
    {
        /// <summary>
        /// Creates and displays a menu based on the provided definition.
        /// </summary>
        /// <param name="definition">The menu definition containing title and items.</param>
        void ShowMenu(MenuDefinition definition);

        /// <summary>
        /// Closes any currently open menu.
        /// </summary>
        void CloseMenu();

        /// <summary>
        /// Checks if a menu is currently visible.
        /// </summary>
        bool IsMenuVisible { get; }

        /// <summary>
        /// Gets the ID of the currently displayed menu, or null if no menu is open.
        /// </summary>
        string? CurrentMenuId { get; }

        /// <summary>
        /// Event raised when a menu item is selected by the user.
        /// </summary>
        event EventHandler<MenuItemSelectedEventArgs>? ItemSelected;

        /// <summary>
        /// Event raised when the menu is closed by the user.
        /// </summary>
        event EventHandler? MenuClosed;

        /// <summary>
        /// Updates any menu display logic (called each tick if needed).
        /// </summary>
        void Update();
    }

    /// <summary>
    /// Event arguments for when a menu item is selected.
    /// </summary>
    public class MenuItemSelectedEventArgs : EventArgs
    {
        /// <summary>
        /// The ID of the menu containing the selected item.
        /// </summary>
        public string MenuId { get; }

        /// <summary>
        /// The ID of the selected item.
        /// </summary>
        public string ItemId { get; }

        /// <summary>
        /// Creates new event arguments for a menu item selection.
        /// </summary>
        /// <param name="menuId">The menu ID.</param>
        /// <param name="itemId">The item ID.</param>
        public MenuItemSelectedEventArgs(string menuId, string itemId)
        {
            MenuId = menuId ?? string.Empty;
            ItemId = itemId ?? string.Empty;
        }
    }
}
