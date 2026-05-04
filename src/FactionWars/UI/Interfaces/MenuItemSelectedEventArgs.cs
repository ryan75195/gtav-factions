using System;

namespace FactionWars.UI.Interfaces
{
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
