using FactionWars.ScriptHookV.Logging;
using FactionWars.Telemetry.Interfaces;

namespace FactionWars.ScriptHookV
{
    public partial class GameLoopController
    {
        public void OnAbort()
        {
            _isInitialized = false;
            _characterSwitchInitialized = false;

            // Unsubscribe from events
            _characterSwitchDetector.OnCharacterSwitched -= HandleCharacterSwitched;
            DisposeTelemetry();
            DisposeMapAndTerritory();
            StopAiSystems();
            CleanupCombatManagers();
            CleanupStateServices();
        }

        private void DisposeTelemetry()
        {
            _telemetryService?.Dispose();
            _telemetryService = null;
            _behaviorSampler = null;
            _behaviorTraceSink?.Dispose();
            _behaviorTraceSink = null;
            _engagementEventRecorder = null;
            _engagementEventSink?.Dispose();
            _engagementEventSink = null;
            if (_container.TryResolve<ITelemetrySink>(out var telemetrySink) && telemetrySink != null)
            {
                telemetrySink.Dispose();
            }
        }

        private void DisposeMapAndTerritory()
        {
            _economyManager?.Stop();
            _economyManager = null;
            _mapBlipManager?.Dispose();
            _mapBlipManager = null;
            _zoneBoundaryBlipManager?.Dispose();
            _zoneBoundaryBlipManager = null;

            // Unsubscribe from territory events and clean up
            if (_territoryManager != null)
            {
                _territoryManager.ZoneEntered -= OnZoneEntered;
                _territoryManager.ZoneExited -= OnZoneExited;
                _territoryManager.NeutralZoneEntered -= OnNeutralZoneEntered;
                _territoryManager.ZoneExited -= OnZoneExitedForClaim;
                _territoryManager = null;
            }

            _defenderRallyController = null;
        }

        private void StopAiSystems()
        {
            // Stop consolidated AI controller
            _aiController?.Stop();
            _aiController = null;

            // Stop victory manager
            _victoryManager?.Stop();
            _victoryManager = null;
        }

        private void CleanupCombatManagers()
        {
            if (_zoneBattleManager != null)
            {
                _zoneBattleManager.BattleEnded -= OnZoneBattleEnded;
                _zoneBattleManager.TroopKilled -= OnZoneBattleTroopKilled;
                _zoneBattleManager.BattleStarted -= OnZoneBattleStarted;
            }
            _zoneBattleManager = null;
            _battleHudRenderer = null;
            _currentBattleHudIndex = 0;
            // Restore time scale in case the squad radial was open when the script aborted.
            _squadRadialMenuRenderer?.Reset();
            _squadRadialMenuRenderer = null;
            _policeSuppressionController?.Dispose();
            _policeSuppressionController = null;

            // Clean up follower manager
            _followerManager = null;
            _followerService = null;

            // Clean up friendly defender manager
            _friendlyDefenderManager?.DespawnAllDefenders();
            _friendlyDefenderManager = null;

            // Clean up enemy defender manager
            _enemyDefenderManager?.DespawnAllDefenders();
            _enemyDefenderManager = null;

            // Clean up battle attacker manager
            _battleAttackerManager?.DespawnAllAttackers();
            _battleAttackerManager = null;
        }

        private void CleanupStateServices()
        {
            _eventFeedRenderer = null;
            _eventFeedService = null;

            // Unsubscribe from difficulty events
            if (_difficultyService != null)
            {
                _difficultyService.DifficultyChanged -= OnDifficultyChanged;
            }
            _difficultyService = null;

            // Unsubscribe from game state manager events
            if (_gameStateManager != null)
            {
                _gameStateManager.OnGameLoaded -= OnGameLoaded;
            }
            _gameStateManager = null;
        }

        /// <summary>
        /// Handles character switch events from the detector.
        /// Shows a notification, dismisses followers for the old faction, and raises the public event.
        /// </summary>
    }
}
