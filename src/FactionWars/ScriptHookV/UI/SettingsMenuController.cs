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
    public class SettingsMenuController
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

        public void ShowDifficultyMenu()
        {
            var menu = new MenuDefinition(DifficultyMenuId, "Difficulty", "Select Difficulty");
            var currentLevel = _difficultyService.Current.Level;

            var easySettings = DifficultySettings.Easy;
            var easyText = currentLevel == Difficulty.Easy ? "Easy [Current]" : "Easy";
            menu.AddItem(new MenuItem(
                "difficulty_easy",
                easyText,
                $"{easySettings.AiIncomeMultiplier:F2}x AI income, {easySettings.TickIntervalMinutes} min tick rate"));

            var normalSettings = DifficultySettings.Normal;
            var normalText = currentLevel == Difficulty.Normal ? "Normal [Current]" : "Normal";
            menu.AddItem(new MenuItem(
                "difficulty_normal",
                normalText,
                $"{normalSettings.AiIncomeMultiplier:F2}x AI income, {normalSettings.TickIntervalMinutes} min tick rate"));

            var hardSettings = DifficultySettings.Hard;
            var hardText = currentLevel == Difficulty.Hard ? "Hard [Current]" : "Hard";
            menu.AddItem(new MenuItem(
                "difficulty_hard",
                hardText,
                $"{hardSettings.AiIncomeMultiplier:F2}x AI income, {hardSettings.TickIntervalMinutes} min tick rate"));

            menu.AddItem(new MenuItem(
                BackItemId,
                "Back",
                "Return to settings"));

            _menuProvider.ShowMenu(menu);
        }

        private void HandleDifficultyMenuSelection(string itemId)
        {
            if (itemId == BackItemId)
            {
                Show();
                return;
            }

            Difficulty? selectedDifficulty = null;
            switch (itemId)
            {
                case "difficulty_easy":
                    selectedDifficulty = Difficulty.Easy;
                    break;
                case "difficulty_normal":
                    selectedDifficulty = Difficulty.Normal;
                    break;
                case "difficulty_hard":
                    selectedDifficulty = Difficulty.Hard;
                    break;
            }

            if (selectedDifficulty.HasValue)
            {
                HandleDifficultySelection(selectedDifficulty.Value);
            }
        }

        private void HandleDifficultySelection(Difficulty level)
        {
            if (level == _difficultyService.Current.Level)
            {
                Show();
                return;
            }

            _pendingDifficulty = level;
            ShowDifficultyConfirmation(level);
        }

        private void ShowDifficultyConfirmation(Difficulty level)
        {
            var menu = new MenuDefinition(DifficultyConfirmMenuId, "Confirm Change", $"Change to {level}?");

            menu.AddItem(new MenuItem(
                "confirm",
                "Confirm",
                $"Change difficulty to {level}"));

            menu.AddItem(new MenuItem(
                "cancel",
                "Cancel",
                "Go back without changing"));

            _menuProvider.ShowMenu(menu);
        }

        private void HandleDifficultyConfirmSelection(string itemId)
        {
            if (itemId == "confirm" && _pendingDifficulty.HasValue)
            {
                ConfirmDifficultyChange(_pendingDifficulty.Value);
            }
            else if (itemId == "cancel")
            {
                _pendingDifficulty = null;
                ShowDifficultyMenu();
            }
        }

        private void ConfirmDifficultyChange(Difficulty level)
        {
            _difficultyService.SetDifficulty(level);
            _pendingDifficulty = null;
            Show();
        }

        private void ShowInitConditionsConfirmation()
        {
            var menu = new MenuDefinition(
                InitConditionsConfirmMenuId,
                "Warning",
                "This will modify your GTA V character!");

            menu.AddItem(new MenuItem(
                "confirm",
                "Yes, Apply Changes",
                $"Set cash to ${StartingCashAmount:N0}, remove weapons, give pistol"));

            menu.AddItem(new MenuItem(
                "cancel",
                "Cancel",
                "Go back without changing"));

            _menuProvider.ShowMenu(menu);
        }

        private void HandleInitConditionsConfirmSelection(string itemId)
        {
            if (itemId == "confirm")
            {
                ApplyStartingConditions();
            }
            else if (itemId == "cancel")
            {
                Show();
            }
        }

        private void ApplyStartingConditions()
        {
            FileLogger.Info($"ApplyStartingConditions: Setting player cash to ${StartingCashAmount:N0}, removing weapons, giving {StartingWeapon}");

            _gameBridge.SetPlayerMoney(StartingCashAmount);
            _gameBridge.RemoveAllPlayerWeapons();
            _gameBridge.GivePlayerWeapon(StartingWeapon, StartingAmmo);

            _gameBridge.ShowNotification($"~g~Starting conditions applied!~n~~w~Cash: ${StartingCashAmount:N0}~n~Weapon: Pistol");

            FileLogger.Info("ApplyStartingConditions: Complete");

            Show();
        }
    }
}
