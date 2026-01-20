using FactionWars.UI.Interfaces;
using FactionWars.UI.Models;
using System;

namespace FactionWars.Tests.Mocks
{
    /// <summary>
    /// Mock implementation of IMenuProvider for testing.
    /// Does not depend on NativeUI.
    /// </summary>
    public class MockMenuProvider : IMenuProvider
    {
        private MenuDefinition? _currentDefinition;

        /// <inheritdoc />
        public bool IsMenuVisible => _currentDefinition != null;

        /// <inheritdoc />
        public string? CurrentMenuId => _currentDefinition?.Id;

        /// <inheritdoc />
        public event EventHandler<MenuItemSelectedEventArgs>? ItemSelected;

        /// <inheritdoc />
        public event EventHandler? MenuClosed;

        /// <summary>
        /// Gets the currently selected item index, or -1 if no menu is open.
        /// </summary>
        public int SelectedIndex { get; private set; } = -1;

        /// <inheritdoc />
        public void ShowMenu(MenuDefinition definition, string? selectedItemId = null)
        {
            _currentDefinition = definition ?? throw new ArgumentNullException(nameof(definition));

            // Default to first item
            SelectedIndex = 0;

            // Find the index of the selected item if specified
            if (!string.IsNullOrEmpty(selectedItemId))
            {
                for (int i = 0; i < definition.Items.Count; i++)
                {
                    if (definition.Items[i].Id == selectedItemId)
                    {
                        SelectedIndex = i;
                        break;
                    }
                }
            }
        }

        /// <inheritdoc />
        public void CloseMenu()
        {
            if (_currentDefinition == null)
                return;

            _currentDefinition = null;
            SelectedIndex = -1;
            MenuClosed?.Invoke(this, EventArgs.Empty);
        }

        /// <inheritdoc />
        public void Update()
        {
            // No-op for mock
        }

        /// <summary>
        /// Gets the current menu definition, or null if no menu is open.
        /// </summary>
        public MenuDefinition? GetCurrentMenuDefinition()
        {
            return _currentDefinition;
        }

        /// <summary>
        /// Simulates a user selecting an item by its ID.
        /// </summary>
        public void SimulateItemSelection(string itemId)
        {
            if (_currentDefinition == null)
                return;

            ItemSelected?.Invoke(this, new MenuItemSelectedEventArgs(_currentDefinition.Id, itemId));
        }
    }
}
