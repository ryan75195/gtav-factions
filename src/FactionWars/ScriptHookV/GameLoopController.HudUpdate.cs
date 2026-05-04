using FactionWars.Territory.Models;

namespace FactionWars.ScriptHookV
{
    public partial class GameLoopController
    {
        private void UpdateAndDrawHud()
        {
            var playerFactionId = CurrentPlayerFactionId;

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
