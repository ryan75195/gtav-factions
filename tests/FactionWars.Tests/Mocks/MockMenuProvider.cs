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

        /// <inheritdoc />
        public void ShowMenu(MenuDefinition definition)
        {
            _currentDefinition = definition ?? throw new ArgumentNullException(nameof(definition));
        }

        /// <inheritdoc />
        public void CloseMenu()
        {
            if (_currentDefinition == null)
                return;

            _currentDefinition = null;
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
