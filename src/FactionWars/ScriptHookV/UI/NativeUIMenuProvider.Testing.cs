using FactionWars.UI.Models;
using FactionWars.UI.Interfaces;
using System;

namespace FactionWars.ScriptHookV.UI
{
    public partial class NativeUIMenuProvider
    {
        public MenuDefinition? GetCurrentMenuDefinition()
        {
            return _currentDefinition;
        }

        /// <summary>
        /// Simulates a user selecting an item by its ID (for testing).
        /// </summary>
        public void SimulateItemSelection(string itemId)
        {
            if (_currentDefinition == null)
                return;

            ItemSelected?.Invoke(this, new MenuItemSelectedEventArgs(_currentDefinition.Id, itemId));
        }

        /// <summary>
        /// Moves the selection down to the next enabled item.
        /// </summary>
        public void MoveSelectionDown()
        {
            _currentMenu?.GoDown();
        }

        /// <summary>
        /// Moves the selection up to the previous enabled item.
        /// </summary>
        public void MoveSelectionUp()
        {
            _currentMenu?.GoUp();
        }

        /// <summary>
        /// Selects the currently highlighted item.
        /// </summary>
        public void SelectCurrentItem()
        {
            if (_currentMenu == null || _currentDefinition == null)
                return;

            var index = _currentMenu.CurrentSelection;
            if (index >= 0 && index < _currentMenu.MenuItems.Count)
            {
                var selectedItem = _currentMenu.MenuItems[index];
                if (_itemIdMap.TryGetValue(selectedItem, out var itemId))
                {
                    ItemSelected?.Invoke(this, new MenuItemSelectedEventArgs(_currentDefinition.Id, itemId));
                }
            }
        }
    }
}
