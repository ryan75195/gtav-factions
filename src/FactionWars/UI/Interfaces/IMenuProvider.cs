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
        /// <param name="selectedItemId">Optional ID of the item to select initially for cursor retention.</param>
        void ShowMenu(MenuDefinition definition, string? selectedItemId = null);

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

        /// <summary>
        /// Updates the select key held state for repeat functionality.
        /// When held, the currently selected item will be repeatedly triggered.
        /// </summary>
        /// <param name="isHeld">True if the select key (Enter) is currently held down.</param>
        void SetSelectKeyHeld(bool isHeld);

        /// <summary>
        /// Gets or sets whether the current menu supports hold-to-repeat.
        /// When true, holding Enter will repeatedly trigger the selected item.
        /// </summary>
        bool HoldToRepeatEnabled { get; set; }
    }

}
