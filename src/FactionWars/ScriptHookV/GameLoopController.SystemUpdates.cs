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
            // Only Escort boards the player's vehicle; HoldArea/S&D keep the squad on the ground.
            var boardPlayerVehicle = _squadStanceController == null
                || _squadStanceController.CurrentStance == SquadStance.Escort;
            _followerManager?.Update(CurrentPlayerFactionId ?? "", boardPlayerVehicle);
            UpdateSquadStance();
            UpdateFollowerSniperWeapons();
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
            SampleSquadState(handles, _squadStanceController.CurrentStance);
        }

        private int _lastSquadSampleMs;
        private const int SquadSampleIntervalMs = 2000;

        // Periodically samples each on-foot bodyguard's OUTCOME (distance to player, ped-group
        // membership, combat state) during non-Escort stances. Where the on-change task logs show
        // intent, this shows what the peds actually do over time: a bodyguard tasked to hold a ring
        // point or attack a distant enemy that stays at distToPlayer~0 with inPlayerGroup=true is
        // being overridden by native group-follow.
        private void SampleSquadState(IReadOnlyList<int> handles, SquadStance stance)
        {
            if (stance == SquadStance.Escort || handles.Count == 0) return;

            int now = _gameBridge.GetGameTime();
            if (now - _lastSquadSampleMs < SquadSampleIntervalMs) return;
            _lastSquadSampleMs = now;

            var playerPos = _gameBridge.GetPlayerPosition();
            foreach (var ped in handles)
            {
                float dist = playerPos.DistanceTo(_gameBridge.GetPedPosition(ped));
                FileLogger.AI($"SquadSample[{stance}]: ped {ped} distToPlayer={dist:F1} inPlayerGroup={_gameBridge.IsPedFollowingPlayer(ped)} inCombat={_gameBridge.IsPedInCombat(ped)}");
            }
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

        // Sniper bodyguards hold a scoped rifle that the AI barely fires at point-blank range.
        // Mirror the zone-defender close defense: switch each sniper to a pistol when any hostile
        // is within SidearmThresholdMeters, back to the rifle when they're all beyond it.
        private void UpdateFollowerSniperWeapons()
        {
            if (_followerManager == null || _sharedSniperDeployment == null) return;

            var snipers = _followerManager.SniperBodyguardHandles;
            if (snipers.Count == 0) return;

            var hostilePositions = new List<Vector3>();
            foreach (var handle in GatherHostileHandles())
            {
                hostilePositions.Add(_gameBridge.GetPedPosition(handle));
            }

            foreach (var sniper in snipers)
            {
                _sharedSniperDeployment.UpdateCloseDefense(sniper, hostilePositions);
            }
        }

        /// <summary>
        /// Updates HUD data and draws HUD elements to the screen.
        /// </summary>
    }
}
