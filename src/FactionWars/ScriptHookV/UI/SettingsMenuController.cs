using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.ScriptHookV.Logging;
using FactionWars.UI.Interfaces;
using FactionWars.UI.Models;
using System;

namespace FactionWars.ScriptHookV.UI
{
    /// <summary>
    /// Controller for the Settings submenu. Mod state now follows GTA V's native saves,
    /// so save/load UI has been removed; difficulty and starting-conditions options remain.
    /// </summary>
    public partial class SettingsMenuController
    {
        public const string SettingsMenuId = "settings_menu";

        public const string DebugModeItemId = "debug_mode";

        public const string BackItemId = "back";

        public const string DifficultyItemId = "difficulty";

        public const string DifficultyMenuId = "difficulty_menu";

        public const string DifficultyConfirmMenuId = "difficulty_confirm_menu";

        public const string InitializeConditionsItemId = "init_conditions";

        public const string InitConditionsConfirmMenuId = "init_conditions_confirm_menu";

        public const int StartingCashAmount = 15000;

        public const string StartingWeapon = "WEAPON_PISTOL";

        public const int StartingAmmo = 100;

        private readonly IMenuProvider _menuProvider;
        private readonly IDifficultyService _difficultyService;
        private readonly IGameBridge _gameBridge;
        private bool _isDebugModeEnabled;
        private Difficulty? _pendingDifficulty;

        public bool IsDebugModeEnabled => _isDebugModeEnabled;

        public event EventHandler? BackRequested;

        public event EventHandler<bool>? DebugModeChanged;

        public SettingsMenuController(
            IMenuProvider menuProvider,
            IDifficultyService difficultyService,
            IGameBridge gameBridge)
        {
            _menuProvider = menuProvider ?? throw new ArgumentNullException(nameof(menuProvider));
            _difficultyService = difficultyService ?? throw new ArgumentNullException(nameof(difficultyService));
            _gameBridge = gameBridge ?? throw new ArgumentNullException(nameof(gameBridge));
            _isDebugModeEnabled = false;

            _menuProvider.ItemSelected += OnItemSelected;
        }

        public void Show()
        {
            var menu = new MenuDefinition(SettingsMenuId, "Settings", "Game Options");

            var currentDifficulty = _difficultyService.Current.Level;
            menu.AddItem(new MenuItem(
                DifficultyItemId,
                $"Difficulty: {currentDifficulty}",
                "Change game difficulty"));

            var debugStateText = _isDebugModeEnabled ? "On" : "Off";
            menu.AddItem(new MenuItem(
                DebugModeItemId,
                $"Debug Mode: {debugStateText}",
                "Toggle debug information display"));

            menu.AddItem(new MenuItem(
                InitializeConditionsItemId,
                "Initialize Starting Conditions",
                $"Reset to ${StartingCashAmount:N0} and pistol only"));

            menu.AddItem(new MenuItem(
                BackItemId,
                "Back",
                "Return to main menu"));

            _menuProvider.ShowMenu(menu);
        }

        private void OnItemSelected(object? sender, MenuItemSelectedEventArgs e)
        {
            switch (e.MenuId)
            {
                case SettingsMenuId:
                    HandleSettingsMenuSelection(e.ItemId);
                    break;
                case DifficultyMenuId:
                    HandleDifficultyMenuSelection(e.ItemId);
                    break;
                case DifficultyConfirmMenuId:
                    HandleDifficultyConfirmSelection(e.ItemId);
                    break;
                case InitConditionsConfirmMenuId:
                    HandleInitConditionsConfirmSelection(e.ItemId);
                    break;
            }
        }

        private void HandleSettingsMenuSelection(string itemId)
        {
            switch (itemId)
            {
                case DifficultyItemId:
                    ShowDifficultyMenu();
                    break;
                case DebugModeItemId:
                    _isDebugModeEnabled = !_isDebugModeEnabled;
                    DebugModeChanged?.Invoke(this, _isDebugModeEnabled);
                    Show();
                    break;
                case InitializeConditionsItemId:
                    ShowInitConditionsConfirmation();
                    break;
                case BackItemId:
                    _menuProvider.CloseMenu();
                    BackRequested?.Invoke(this, EventArgs.Empty);
                    break;
            }
        }

    }
}
