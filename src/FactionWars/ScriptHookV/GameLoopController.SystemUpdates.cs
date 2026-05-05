namespace FactionWars.ScriptHookV
{
    public partial class GameLoopController
    {
        private void UpdateCoreSystems(float deltaTime)
        {
            _economyManager?.Update(deltaTime);
            _gameStateManager?.UpdatePlayTime(deltaTime);
            _telemetryService?.Update(deltaTime);
        }

        private void UpdateWorldSystems(float deltaTime)
        {
            _mapBlipManager?.UpdateBlipColors();
            _territoryManager?.Update();
            _aiController?.Update(deltaTime);
            _zoneBattleManager?.Tick(deltaTime);
            _policeSuppressionController?.Update();
            _victoryManager?.Update(deltaTime);
            _followerManager?.Update(CurrentPlayerFactionId ?? "");
            _friendlyDefenderManager?.Update();
            _defenderRallyController?.Update();
            _commanderManager?.Update();
            var currentZone = _territoryManager?.CurrentZone;
            var enemyFactionId = currentZone?.OwnerFactionId;
            if (enemyFactionId != null && enemyFactionId != CurrentPlayerFactionId)
            {
                _enemyDefenderManager?.Update(enemyFactionId);
            }

            _battleAttackerManager?.Update();
        }

        /// <summary>
        /// Updates HUD data and draws HUD elements to the screen.
        /// </summary>
    }
}
