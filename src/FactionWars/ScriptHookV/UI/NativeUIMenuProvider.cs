using FactionWars.UI.Interfaces;
using FactionWars.UI.Models;
using NativeUI;
using System;
using System.Collections.Generic;

namespace FactionWars.ScriptHookV.UI
{
    /// <summary>
    /// Implementation of IMenuProvider using NativeUI library for actual in-game menu rendering.
    /// </summary>
    public class NativeUIMenuProvider : IMenuProvider
    {
        private readonly MenuPool _menuPool;
        private UIMenu? _currentMenu;
        private MenuDefinition? _currentDefinition;
        private readonly Dictionary<UIMenuItem, string> _itemIdMap;

        // Hold-to-repeat state
        private bool _selectKeyHeld;
        private bool _wasKeyHeld;
        private DateTime _holdStartTime;
        private DateTime _lastRepeatTime;

        // Hold-to-repeat timing (in milliseconds)
        private const int InitialDelayMs = 400;  // Wait before first repeat
        private const int RepeatIntervalMs = 100; // Interval between repeats

        /// <inheritdoc />
        public bool IsMenuVisible => _currentMenu?.Visible ?? false;

        /// <inheritdoc />
        public string? CurrentMenuId => _currentDefinition?.Id;

        /// <inheritdoc />
        public bool HoldToRepeatEnabled { get; set; }

        /// <summary>
        /// Gets the currently selected item index, or -1 if no menu is open.
        /// </summary>
        public int SelectedIndex => _currentMenu?.CurrentSelection ?? -1;

        /// <inheritdoc />
        public event EventHandler<MenuItemSelectedEventArgs>? ItemSelected;

        /// <inheritdoc />
        public event EventHandler? MenuClosed;

        /// <summary>
        /// Creates a new NativeUIMenuProvider with real NativeUI integration.
        /// </summary>
        public NativeUIMenuProvider()
        {
            _menuPool = new MenuPool();
            _itemIdMap = new Dictionary<UIMenuItem, string>();
        }

        /// <inheritdoc />
        public void ShowMenu(MenuDefinition definition, string? selectedItemId = null)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            // Close any existing menu first
            CloseMenu();

            // Reset hold-to-repeat (must be explicitly enabled per menu)
            HoldToRepeatEnabled = false;

            _currentDefinition = definition;

            // Create the NativeUI menu
            _currentMenu = new UIMenu(definition.Title, definition.Subtitle ?? "");

            int selectedIndex = 0;
            int currentIndex = 0;

            // Add items to the menu
            foreach (var item in definition.Items)
            {
                var uiItem = new UIMenuItem(item.Text, item.Description ?? "");
                uiItem.Enabled = item.IsEnabled;

                _itemIdMap[uiItem] = item.Id;
                _currentMenu.AddItem(uiItem);

                // Track index of selected item
                if (item.Id == selectedItemId)
                {
                    selectedIndex = currentIndex;
                }
                currentIndex++;
            }

            // Subscribe to item selection
            _currentMenu.OnItemSelect += OnNativeUIItemSelect;
            _currentMenu.OnMenuClose += OnNativeUIMenuClose;

            // Add to pool and open
            _menuPool.Add(_currentMenu);
            _currentMenu.Visible = true;

            // Set the selected index if specified
            if (!string.IsNullOrEmpty(selectedItemId) && selectedIndex >= 0 && selectedIndex < _currentMenu.MenuItems.Count)
            {
                _currentMenu.CurrentSelection = selectedIndex;
            }
        }

        /// <inheritdoc />
        public void CloseMenu()
        {
            if (_currentMenu == null)
                return;

            _currentMenu.OnItemSelect -= OnNativeUIItemSelect;
            _currentMenu.OnMenuClose -= OnNativeUIMenuClose;
            _currentMenu.Visible = false;

            _menuPool.CloseAllMenus();
            _itemIdMap.Clear();

            _currentMenu = null;
            _currentDefinition = null;

            MenuClosed?.Invoke(this, EventArgs.Empty);
        }

        /// <inheritdoc />
        public void Update()
        {
            // Process NativeUI menus - THIS IS CRITICAL for rendering
            _menuPool.ProcessMenus();

            // Handle hold-to-repeat functionality
            ProcessHoldToRepeat();
        }

        /// <inheritdoc />
        public void SetSelectKeyHeld(bool isHeld)
        {
            _selectKeyHeld = isHeld;
        }

        /// <summary>
        /// Processes hold-to-repeat logic for the select key.
        /// </summary>
        private void ProcessHoldToRepeat()
        {
            if (!HoldToRepeatEnabled || !IsMenuVisible)
            {
                _wasKeyHeld = false;
                return;
            }

            var now = DateTime.UtcNow;

            if (_selectKeyHeld)
            {
                if (!_wasKeyHeld)
                {
                    // Key just pressed - start tracking hold time
                    _holdStartTime = now;
                    _lastRepeatTime = now;
                    _wasKeyHeld = true;
                }
                else
                {
                    // Key is being held - check if we should repeat
                    var holdDuration = (now - _holdStartTime).TotalMilliseconds;
                    var timeSinceLastRepeat = (now - _lastRepeatTime).TotalMilliseconds;

                    if (holdDuration >= InitialDelayMs && timeSinceLastRepeat >= RepeatIntervalMs)
                    {
                        // Fire a repeat selection
                        SelectCurrentItem();
                        _lastRepeatTime = now;
                    }
                }
            }
            else
            {
                _wasKeyHeld = false;
            }
        }

        /// <summary>
        /// Called when a NativeUI menu item is selected.
        /// </summary>
        private void OnNativeUIItemSelect(UIMenu sender, UIMenuItem selectedItem, int index)
        {
            if (_currentDefinition == null)
                return;

            if (_itemIdMap.TryGetValue(selectedItem, out var itemId))
            {
                ItemSelected?.Invoke(this, new MenuItemSelectedEventArgs(_currentDefinition.Id, itemId));
            }
        }

        /// <summary>
        /// Called when a NativeUI menu is closed.
        /// </summary>
        private void OnNativeUIMenuClose(UIMenu sender)
        {
            _itemIdMap.Clear();
            _currentMenu = null;
            _currentDefinition = null;
            MenuClosed?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Gets the current menu definition, or null if no menu is open.
        /// </summary>
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
