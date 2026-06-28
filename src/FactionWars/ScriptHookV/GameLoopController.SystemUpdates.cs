using System.Collections.Generic;
using FactionWars.Combat.Models;
using FactionWars.Core.Interfaces;
using FactionWars.ScriptHookV.Logging;

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
            TryRestoreRuntimeWorldState();

            _mapBlipManager?.UpdateBlipColors();
            _territoryManager?.Update();
            _aiController?.Update(deltaTime);
            _zoneBattleManager?.Tick(deltaTime);
            _policeSuppressionController?.Update();
            _victoryManager?.Update(deltaTime);
            _followerManager?.Update(CurrentPlayerFactionId ?? "");
            UpdateSquadStance();
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

        private string _lastSquadStanceSummary = "";

        private void UpdateSquadStance()
        {
            if (_squadStanceController == null || _followerManager == null) return;

            var handles = _followerManager.OnFootBodyguardHandles;
            var anchor = ResolveSquadAnchor();
            IReadOnlyList<EnemyTarget> enemies = System.Array.Empty<EnemyTarget>();

            int hostileCount = 0;
            if (_squadStanceController.CurrentStance == SquadStance.SearchAndDestroy && handles.Count > 0)
            {
                var hostileHandles = GatherHostileHandles();
                hostileCount = hostileHandles.Count;
                enemies = _enemyTargetCollector!.Collect(hostileHandles, anchor.Center, anchor.Radius);
            }

            var summary = $"stance={_squadStanceController.CurrentStance} onFoot={handles.Count} inVehicle={_gameBridge.IsPlayerInVehicle()} hostiles={hostileCount} enemiesInRange={enemies.Count}";
            if (summary != _lastSquadStanceSummary)
            {
                FileLogger.AI($"UpdateSquadStance: {summary}");
                _lastSquadStanceSummary = summary;
            }

            _squadStanceController.Update(anchor.Center, anchor.Radius, handles, enemies);
        }

        private AreaAnchor ResolveSquadAnchor()
        {
            var zone = _territoryManager?.CurrentZone;
            Vector3? zoneCenter = zone != null ? zone.Center : (Vector3?)null;
            float zoneRadius = zone?.Radius ?? 0f;
            return _areaAnchorResolver!.Resolve(zoneCenter, zoneRadius, _gameBridge.GetPlayerPosition(), SquadDefaultLooseRadius);
        }

        private IReadOnlyList<int> GatherHostileHandles()
        {
            var handles = new List<int>();
            if (_enemyDefenderManager != null) handles.AddRange(_enemyDefenderManager.GetHostilePedHandles());
            if (_battleAttackerManager != null) handles.AddRange(_battleAttackerManager.GetHostilePedHandles());
            return handles;
        }

        /// <summary>
        /// Updates HUD data and draws HUD elements to the screen.
        /// </summary>
    }
}
