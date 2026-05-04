using FactionWars.Configuration;
using FactionWars.Core.Models;
using FactionWars.ScriptHookV.Logging;
using FactionWars.UI.Models;

namespace FactionWars.ScriptHookV.UI
{
    public partial class SettingsMenuController
    {
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
