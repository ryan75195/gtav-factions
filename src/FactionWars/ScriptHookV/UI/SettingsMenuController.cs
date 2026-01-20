using FactionWars.Core.Interfaces;
using FactionWars.UI.Interfaces;
using FactionWars.UI.Models;
using System;

namespace FactionWars.ScriptHookV.UI
{
    /// <summary>
    /// Controller for the Settings submenu. Provides save, load, and debug options.
    /// </summary>
    public class SettingsMenuController
    {
        /// <summary>
        /// Menu ID for the settings menu.
        /// </summary>
        public const string SettingsMenuId = "settings_menu";

        /// <summary>
        /// Menu ID for the save game menu.
        /// </summary>
        public const string SaveMenuId = "save_menu";

        /// <summary>
        /// Menu ID for the load game menu.
        /// </summary>
        public const string LoadMenuId = "load_menu";

        /// <summary>
        /// Item ID for the save game option.
        /// </summary>
        public const string SaveGameItemId = "save_game";

        /// <summary>
        /// Item ID for the load game option.
        /// </summary>
        public const string LoadGameItemId = "load_game";

        /// <summary>
        /// Item ID for the debug mode toggle.
        /// </summary>
        public const string DebugModeItemId = "debug_mode";

        /// <summary>
        /// Item ID for the back navigation item.
        /// </summary>
        public const string BackItemId = "back";

        private readonly IMenuProvider _menuProvider;
        private readonly ISaveSlotManager _saveSlotManager;
        private readonly IGameStateCoordinator _gameStateCoordinator;
        private bool _isDebugModeEnabled;

        /// <summary>
        /// Gets whether debug mode is currently enabled.
        /// </summary>
        public bool IsDebugModeEnabled => _isDebugModeEnabled;

        /// <summary>
        /// Event raised when the user selects the back option from the main settings menu.
        /// </summary>
        public event EventHandler? BackRequested;

        /// <summary>
        /// Event raised when the user selects back from the save menu.
        /// </summary>
        public event EventHandler? SaveMenuBackRequested;

        /// <summary>
        /// Event raised when the user selects back from the load menu.
        /// </summary>
        public event EventHandler? LoadMenuBackRequested;

        /// <summary>
        /// Event raised when a save operation is completed.
        /// </summary>
        public event EventHandler? SaveCompleted;

        /// <summary>
        /// Event raised when a load operation is completed.
        /// </summary>
        public event EventHandler? LoadCompleted;

        /// <summary>
        /// Event raised when debug mode is toggled. The boolean argument indicates the new state.
        /// </summary>
        public event EventHandler<bool>? DebugModeChanged;

        /// <summary>
        /// Creates a new SettingsMenuController with the specified dependencies.
        /// </summary>
        /// <param name="menuProvider">The menu provider for displaying menus.</param>
        /// <param name="saveSlotManager">The save slot manager for managing save files.</param>
        /// <param name="gameStateCoordinator">The game state coordinator for save/load operations.</param>
        /// <exception cref="ArgumentNullException">Thrown if any parameter is null.</exception>
        public SettingsMenuController(
            IMenuProvider menuProvider,
            ISaveSlotManager saveSlotManager,
            IGameStateCoordinator gameStateCoordinator)
        {
            _menuProvider = menuProvider ?? throw new ArgumentNullException(nameof(menuProvider));
            _saveSlotManager = saveSlotManager ?? throw new ArgumentNullException(nameof(saveSlotManager));
            _gameStateCoordinator = gameStateCoordinator ?? throw new ArgumentNullException(nameof(gameStateCoordinator));
            _isDebugModeEnabled = false;

            // Subscribe to menu item selection events
            _menuProvider.ItemSelected += OnItemSelected;
        }

        /// <summary>
        /// Shows the main settings menu.
        /// </summary>
        public void Show()
        {
            var menu = new MenuDefinition(SettingsMenuId, "Settings", "Game Options");

            menu.AddItem(new MenuItem(
                SaveGameItemId,
                "Save Game",
                "Save your current progress to a slot"));

            menu.AddItem(new MenuItem(
                LoadGameItemId,
                "Load Game",
                "Load a previously saved game"));

            var debugStateText = _isDebugModeEnabled ? "On" : "Off";
            menu.AddItem(new MenuItem(
                DebugModeItemId,
                $"Debug Mode: {debugStateText}",
                "Toggle debug information display"));

            menu.AddItem(new MenuItem(
                BackItemId,
                "Back",
                "Return to main menu"));

            _menuProvider.ShowMenu(menu);
        }

        /// <summary>
        /// Shows the save game menu with all save slots.
        /// </summary>
        public void ShowSaveMenu()
        {
            var menu = new MenuDefinition(SaveMenuId, "Save Game", "Select a Slot");

            var slotInfos = _saveSlotManager.GetAllSlotInfo();
            foreach (var slotInfo in slotInfos)
            {
                var slotText = slotInfo.IsOccupied
                    ? $"Slot {slotInfo.SlotNumber + 1}: {slotInfo.SaveName}"
                    : $"Slot {slotInfo.SlotNumber + 1}: Empty";

                var description = slotInfo.IsOccupied
                    ? $"Overwrite: {slotInfo.FormattedPlayTime} played"
                    : "Save to this empty slot";

                var slotItem = new MenuItem(
                    $"save_slot_{slotInfo.SlotNumber}",
                    slotText,
                    description);

                menu.AddItem(slotItem);
            }

            menu.AddItem(new MenuItem(
                BackItemId,
                "Back",
                "Return to settings"));

            _menuProvider.ShowMenu(menu);
        }

        /// <summary>
        /// Shows the load game menu with all save slots.
        /// </summary>
        public void ShowLoadMenu()
        {
            var menu = new MenuDefinition(LoadMenuId, "Load Game", "Select a Slot");

            var slotInfos = _saveSlotManager.GetAllSlotInfo();
            foreach (var slotInfo in slotInfos)
            {
                var slotText = slotInfo.IsOccupied
                    ? $"Slot {slotInfo.SlotNumber + 1}: {slotInfo.SaveName}"
                    : $"Slot {slotInfo.SlotNumber + 1}: Empty";

                var description = slotInfo.IsOccupied
                    ? $"Load: {slotInfo.FormattedPlayTime} played"
                    : "No save in this slot";

                var slotItem = new MenuItem(
                    $"load_slot_{slotInfo.SlotNumber}",
                    slotText,
                    description);

                // Disable empty slots - can't load from them
                slotItem.IsEnabled = slotInfo.IsOccupied;

                menu.AddItem(slotItem);
            }

            menu.AddItem(new MenuItem(
                BackItemId,
                "Back",
                "Return to settings"));

            _menuProvider.ShowMenu(menu);
        }

        /// <summary>
        /// Handles menu item selection events.
        /// </summary>
        private void OnItemSelected(object? sender, MenuItemSelectedEventArgs e)
        {
            switch (e.MenuId)
            {
                case SettingsMenuId:
                    HandleSettingsMenuSelection(e.ItemId);
                    break;
                case SaveMenuId:
                    HandleSaveMenuSelection(e.ItemId);
                    break;
                case LoadMenuId:
                    HandleLoadMenuSelection(e.ItemId);
                    break;
            }
        }

        /// <summary>
        /// Handles selections from the main settings menu.
        /// </summary>
        private void HandleSettingsMenuSelection(string itemId)
        {
            switch (itemId)
            {
                case SaveGameItemId:
                    ShowSaveMenu();
                    break;
                case LoadGameItemId:
                    ShowLoadMenu();
                    break;
                case DebugModeItemId:
                    _isDebugModeEnabled = !_isDebugModeEnabled;
                    DebugModeChanged?.Invoke(this, _isDebugModeEnabled);
                    // Refresh menu to show updated state
                    Show();
                    break;
                case BackItemId:
                    _menuProvider.CloseMenu();
                    BackRequested?.Invoke(this, EventArgs.Empty);
                    break;
            }
        }

        /// <summary>
        /// Handles selections from the save menu.
        /// </summary>
        private void HandleSaveMenuSelection(string itemId)
        {
            if (itemId == BackItemId)
            {
                _menuProvider.CloseMenu();
                SaveMenuBackRequested?.Invoke(this, EventArgs.Empty);
                return;
            }

            if (itemId.StartsWith("save_slot_"))
            {
                var slotNumberStr = itemId.Substring("save_slot_".Length);
                if (int.TryParse(slotNumberStr, out int slotNumber))
                {
                    _gameStateCoordinator.SaveToSlot(slotNumber);
                    SaveCompleted?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        /// <summary>
        /// Handles selections from the load menu.
        /// </summary>
        private void HandleLoadMenuSelection(string itemId)
        {
            if (itemId == BackItemId)
            {
                _menuProvider.CloseMenu();
                LoadMenuBackRequested?.Invoke(this, EventArgs.Empty);
                return;
            }

            if (itemId.StartsWith("load_slot_"))
            {
                var slotNumberStr = itemId.Substring("load_slot_".Length);
                if (int.TryParse(slotNumberStr, out int slotNumber))
                {
                    _gameStateCoordinator.LoadFromSlot(slotNumber);
                    LoadCompleted?.Invoke(this, EventArgs.Empty);
                }
            }
        }
    }
}
