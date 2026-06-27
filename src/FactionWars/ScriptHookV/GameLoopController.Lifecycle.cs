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

            // Note: the A button's held state for menu hold-to-repeat is polled
            // live where it is consumed (see IsSelectKeyHeld). It must NOT be
            // latched here: controllers have no key-up event, so latching would
            // leave the flag stuck true and spam hold-to-repeat after release.
        }

        /// <summary>
        /// Single source of truth for whether the menu select key is held, used
        /// to drive menu hold-to-repeat. Combines the keyboard Enter key (tracked
        /// via OnKeyDown/OnKeyUp) with a live poll of the controller A button.
        /// The controller is polled live (not latched) because gamepads have no
        /// key-up event to clear a latched flag.
        /// </summary>
        private bool IsSelectKeyHeld()
        {
            return _enterKeyHeld || _gameBridge.IsControlPressed(ControlFrontendAccept);
        }

        /// <summary>
        /// Throttles held menu navigation so a held Up/Down registers at most one
        /// move per cooldown interval. NativeUI's built-in navigation accelerates
        /// while held, which feels twitchy and skips items on a controller. On
        /// frames inside the cooldown we suppress the frontend Up/Down controls so
        /// NativeUI sees no navigation input. A fresh press always moves immediately.
        /// Must run before the menu pool processes input (see OnTick).
        /// </summary>
        private void ThrottleMenuNavigation()
        {
            if (_menuProvider == null || !_menuProvider.IsMenuVisible)
            {
                _menuNavWasPressed = false;
                return;
            }

            bool navPressed = _gameBridge.IsControlPressed(ControlFrontendUp)
                || _gameBridge.IsControlPressed(ControlFrontendDown);

            if (!navPressed)
            {
                _menuNavWasPressed = false;
                return;
            }

            int now = _gameBridge.GetGameTime();
            bool freshPress = !_menuNavWasPressed;
            _menuNavWasPressed = true;

            if (freshPress || now - _lastMenuNavMoveGameTime >= MenuNavRepeatCooldownMs)
            {
                // Allow this move; NativeUI processes the navigation this frame.
                _lastMenuNavMoveGameTime = now;
                return;
            }

            // Within cooldown: hide navigation inputs from NativeUI this frame.
            _gameBridge.DisableControlThisFrame(ControlFrontendUp);
            _gameBridge.DisableControlThisFrame(ControlFrontendDown);
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
