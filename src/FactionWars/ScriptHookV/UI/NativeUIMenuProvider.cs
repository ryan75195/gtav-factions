using FactionWars.UI.Interfaces;
using FactionWars.UI.Models;
using System;

namespace FactionWars.ScriptHookV.UI
{
    /// <summary>
    /// Implementation of IMenuProvider that wraps NativeUI menu system.
    /// In the actual game, this would create and manage NativeUI menus.
    /// For testing and abstraction, it manages menu state internally.
    /// </summary>
    public class NativeUIMenuProvider : IMenuProvider
    {
        private MenuDefinition? _currentMenu;
        private int _selectedIndex;

        /// <inheritdoc />
        public bool IsMenuVisible => _currentMenu != null;

        /// <inheritdoc />
        public string? CurrentMenuId => _currentMenu?.Id;

        /// <summary>
        /// Gets the currently selected item index, or -1 if no menu is open.
        /// </summary>
        public int SelectedIndex => _currentMenu == null ? -1 : _selectedIndex;

        /// <inheritdoc />
        public event EventHandler<MenuItemSelectedEventArgs>? ItemSelected;

        /// <inheritdoc />
        public event EventHandler? MenuClosed;

        /// <summary>
        /// Creates a new NativeUIMenuProvider.
        /// </summary>
        public NativeUIMenuProvider()
        {
            _selectedIndex = 0;
        }

        /// <inheritdoc />
        public void ShowMenu(MenuDefinition definition)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            _currentMenu = definition;
            _selectedIndex = FindFirstEnabledIndex(0, 1);
        }

        /// <inheritdoc />
        public void CloseMenu()
        {
            if (_currentMenu == null)
                return;

            _currentMenu = null;
            _selectedIndex = 0;
            MenuClosed?.Invoke(this, EventArgs.Empty);
        }

        /// <inheritdoc />
        public void Update()
        {
            // In actual implementation, this would process NativeUI's Update() method
            // For now, this is a no-op as menu state is managed internally
        }

        /// <summary>
        /// Gets the current menu definition, or null if no menu is open.
        /// </summary>
        /// <returns>The current menu definition or null.</returns>
        public MenuDefinition? GetCurrentMenuDefinition()
        {
            return _currentMenu;
        }

        /// <summary>
        /// Simulates a user selecting an item by its ID (for testing).
        /// </summary>
        /// <param name="itemId">The ID of the item to select.</param>
        public void SimulateItemSelection(string itemId)
        {
            if (_currentMenu == null)
                return;

            var item = _currentMenu.GetItem(itemId);
            if (item == null || !item.IsEnabled)
                return;

            ItemSelected?.Invoke(this, new MenuItemSelectedEventArgs(_currentMenu.Id, itemId));
        }

        /// <summary>
        /// Moves the selection down to the next enabled item.
        /// </summary>
        public void MoveSelectionDown()
        {
            if (_currentMenu == null || _currentMenu.Items.Count == 0)
                return;

            var nextIndex = (_selectedIndex + 1) % _currentMenu.Items.Count;
            _selectedIndex = FindFirstEnabledIndex(nextIndex, 1);
        }

        /// <summary>
        /// Moves the selection up to the previous enabled item.
        /// </summary>
        public void MoveSelectionUp()
        {
            if (_currentMenu == null || _currentMenu.Items.Count == 0)
                return;

            var prevIndex = _selectedIndex - 1;
            if (prevIndex < 0)
                prevIndex = _currentMenu.Items.Count - 1;

            _selectedIndex = FindFirstEnabledIndex(prevIndex, -1);
        }

        /// <summary>
        /// Selects the currently highlighted item, raising the ItemSelected event.
        /// </summary>
        public void SelectCurrentItem()
        {
            if (_currentMenu == null || _currentMenu.Items.Count == 0)
                return;

            if (_selectedIndex < 0 || _selectedIndex >= _currentMenu.Items.Count)
                return;

            var item = _currentMenu.Items[_selectedIndex];
            if (!item.IsEnabled)
                return;

            ItemSelected?.Invoke(this, new MenuItemSelectedEventArgs(_currentMenu.Id, item.Id));
        }

        /// <summary>
        /// Finds the first enabled item index starting from the given index and moving in the specified direction.
        /// </summary>
        private int FindFirstEnabledIndex(int startIndex, int direction)
        {
            if (_currentMenu == null || _currentMenu.Items.Count == 0)
                return 0;

            var count = _currentMenu.Items.Count;
            var checkedCount = 0;
            var currentIndex = startIndex;

            while (checkedCount < count)
            {
                if (currentIndex < 0)
                    currentIndex = count - 1;
                else if (currentIndex >= count)
                    currentIndex = 0;

                if (_currentMenu.Items[currentIndex].IsEnabled)
                    return currentIndex;

                currentIndex += direction;
                checkedCount++;
            }

            // No enabled items found, return the start index
            return startIndex >= 0 && startIndex < count ? startIndex : 0;
        }
    }
}
