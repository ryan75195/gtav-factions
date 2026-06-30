using FactionWars.Territory.Models;

namespace FactionWars.ScriptHookV
{
    public partial class GameLoopController
    {
        private void UpdateAndDrawHud()
        {
            var playerFactionId = CurrentPlayerFactionId;

            // Squad radial: held on the existing squad key, drawn over the HUD each frame.
            _squadRadialMenuRenderer?.Update();

            // Disable native controls that conflict with menu use (weapon wheel, radio, phone,
            // attack, etc.) while any mod menu is open, so menu interaction can't also trigger
            // a native action. Re-applied every frame because DISABLE_CONTROL_ACTION is per-frame.
            if ((_menuProvider?.IsMenuVisible ?? false) || (_squadRadialMenuRenderer?.IsOpen ?? false))
            {
                _gameBridge.DisableMenuConflictControlsThisFrame();
            }

            UpdateTerritoryIndicator(playerFactionId);

            // Draw battle HUD showing active AI battles
            UpdateAndDrawBattleHud();

            // Draw play time HUD (only when game is loaded)
            if (_playTimeHudRenderer != null && _gameStateManager != null && _gameStateManager.HasGameLoaded)
            {
                _playTimeHudRenderer.SetPlayTime(_gameStateManager.TotalPlayTimeSeconds);
                _playTimeHudRenderer.Draw();
            }

            // Show claim prompt as help text (auto-adapts button label for controller)
            if (_showingClaimPrompt && _currentNeutralZone != null)
            {
                var cost = GetBasicTroopCost();
                _gameBridge.DisplayHelpText($"Press ~INPUT_CONTEXT~ to claim for ~g~${cost}");
            }

            // Combat HUD disabled - TerritoryIndicatorRenderer now shows all combat info
            // including reserves in the "nicer graphics" top-right display
            _combatHudRenderer?.HideCombatHud();

            // Event feed disabled - using native GTA V notifications instead
            // if (_eventFeedRenderer != null && _eventFeedService != null)
            // {
            //     _eventFeedRenderer.Render(_eventFeedService.Entries);
            //     _eventFeedRenderer.Draw();
            // }
        }

    }
}
