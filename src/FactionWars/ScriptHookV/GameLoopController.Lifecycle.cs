using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FactionWars.AI.Interfaces;
using FactionWars.AI.Models;
using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Models;
using FactionWars.Configuration;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.Core.Services;
using FactionWars.Persistence.Models;
using FactionWars.Economy.Interfaces;
using FactionWars.Factions.Interfaces;
using FactionWars.ScriptHookV.Data;
using FactionWars.ScriptHookV.Logging;
using FactionWars.ScriptHookV.Managers;
using FactionWars.ScriptHookV.Models;
using FactionWars.ScriptHookV.Persistence;
using FactionWars.ScriptHookV.UI;
using FactionWars.Telemetry.Interfaces;
using FactionWars.Telemetry.Services;
using FactionWars.Territory.Interfaces;
using FactionWars.Territory.Models;
using FactionWars.UI.Interfaces;
using FactionWars.UI.Models;
using GTA.Native;

namespace FactionWars.ScriptHookV
{
    public partial class GameLoopController
    {
        private void OnMainMenuItemSelected(object? sender, MenuItemSelectedEventArgs e)
        {
            if (e.MenuId != MainMenuController.MainMenuId)
                return;

            switch (e.ItemId)
            {
                case MainMenuController.ZoneManagementItemId:
                    _zoneManagementMenuController?.Show();
                    break;
                case MainMenuController.RecruitmentItemId:
                    _recruitmentMenuController?.Show();
                    break;
                case MainMenuController.ShopItemId:
                    _shopMenuController?.Show();
                    break;
                case MainMenuController.SettingsItemId:
                    _settingsMenuController?.Show();
                    break;
            }
        }

        /// <summary>
        /// Handles difficulty changed events from the difficulty service.
        /// Updates the resource tick service with the new difficulty settings.
        /// </summary>
        private void OnDifficultyChanged(object? sender, DifficultySettings settings)
        {
            _resourceTickService?.SetAiIncomeMultiplier(settings.AiIncomeMultiplier);
            _resourceTickService?.SetTickInterval(settings.TickIntervalSeconds);
            _gameStateManager?.SetCurrentDifficulty(settings.Level);
            FileLogger.Info($"Difficulty changed to {settings.Level}: AI={settings.AiIncomeMultiplier}x, Tick={settings.TickIntervalMinutes}min");

            // Show notification to player
            _gameBridge?.ShowNotification($"~b~Difficulty:~w~ {settings.Level} (AI {settings.AiIncomeMultiplier}x, {settings.TickIntervalMinutes}min ticks)");
        }

        /// <summary>
        /// Handles game loaded events from the game state manager.
        /// Restores difficulty settings from the loaded game state.
        /// </summary>
        private void OnGameLoaded(object? sender, GameStateLoadedEventArgs e)
        {
            if (!e.Success || _difficultyService == null || _gameStateManager == null)
                return;

            // Get the loaded game state to retrieve the difficulty
            var gameState = _gameStateManager.GetCurrentGameState();
            if (gameState != null)
            {
                _difficultyService.SetDifficulty(gameState.Difficulty);
                FileLogger.Info($"Restored difficulty from save: {gameState.Difficulty}");
            }

            _pendingRuntimeWorldRestore = e.RuntimeWorldState;
        }

        /// <summary>
        /// Polls controller (gamepad) input each frame and triggers the same actions as keyboard keys.
        /// </summary>
        private void PollControllerInput()
        {
            if (!_isInitialized) return;

            // LB + D-pad Right = Toggle menu (same as F7)
            if (_gameBridge.IsControlJustPressed(ControlDpadRight) && _gameBridge.IsControlPressed(ControlLB))
            {
                _mainMenuController?.OnKeyDown(MainMenuController.MenuToggleKeyCode);
                return; // Don't also process D-pad Right as claim
            }

            // D-pad Right (no LB) = Claim zone (same as E key)
            if (_gameBridge.IsControlJustPressed(ControlDpadRight) && !_gameBridge.IsControlPressed(ControlLB))
            {
                if (_showingClaimPrompt && _currentNeutralZone != null)
                {
                    TryClaimNeutralZone();
                }
                // Also pass to commander manager for E key interaction
                _commanderManager?.OnKeyDown(ClaimKeyCode);
            }

            // D-pad Down = Cycle battle HUD (same as B key)
            if (_gameBridge.IsControlJustPressed(ControlDpadDown))
            {
                if (_zoneBattleManager != null)
                {
                    var battles = _zoneBattleManager.GetAllActiveBattles();
                    if (battles.Count > 1)
                    {
                        _currentBattleHudIndex = (_currentBattleHudIndex + 1) % battles.Count;
                    }
                }
            }

            // A button held = Menu hold-to-repeat (same as Enter held)
            if (_gameBridge.IsControlPressed(ControlFrontendAccept))
            {
                _enterKeyHeld = true;
            }
        }

        /// <summary>
        /// Called when a key is pressed.
        /// </summary>
        /// <param name="keyCode">The virtual key code of the pressed key.</param>
        public void OnKeyDown(int keyCode)
        {
            if (!_isInitialized)
                return;

            // Track Enter key for menu hold-to-repeat
            if (keyCode == EnterKeyCode)
            {
                _enterKeyHeld = true;
            }

            // Handle claim key when in neutral zone
            if (keyCode == ClaimKeyCode && _showingClaimPrompt && _currentNeutralZone != null)
            {
                TryClaimNeutralZone();
                return;
            }

            // Handle battle HUD cycle key (B)
            if (keyCode == BattleCycleKeyCode && _zoneBattleManager != null)
            {
                var battles = _zoneBattleManager.GetAllActiveBattles();
                if (battles.Count > 1)
                {
                    _currentBattleHudIndex = (_currentBattleHudIndex + 1) % battles.Count;
                }
                return;
            }

            // Pass key events to the main menu controller
            _mainMenuController?.OnKeyDown(keyCode);

            // Pass key events to commander manager (for E key interaction)
            _commanderManager?.OnKeyDown(keyCode);
        }

        /// <summary>
        /// Called when a key is released.
        /// </summary>
        /// <param name="keyCode">The virtual key code of the released key.</param>
        public void OnKeyUp(int keyCode)
        {
            // Track Enter key release for menu hold-to-repeat
            if (keyCode == EnterKeyCode)
            {
                _enterKeyHeld = false;
            }
        }

        /// <summary>
        /// Called when the script is aborted/unloaded.
        /// </summary>
    }
}
