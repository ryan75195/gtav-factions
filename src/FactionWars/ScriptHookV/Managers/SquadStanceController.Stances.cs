using System.Collections.Generic;
using FactionWars.Combat.Models;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.ScriptHookV.Logging;

namespace FactionWars.ScriptHookV.Managers
{
    public partial class SquadStanceController
    {
        private struct AppliedOrder
        {
            public SquadStance Stance;
            public BodyguardOrderKind Kind;
            public int Discriminator; // target handle for AttackTarget, ring index for HoldAtPoint, else 0
        }

        private bool AlreadyApplied(int handle, SquadStance stance, BodyguardOrderKind kind, int discriminator)
        {
            return _lastApplied.TryGetValue(handle, out var last)
                && last.Stance == stance && last.Kind == kind && last.Discriminator == discriminator;
        }

        private void Remember(int handle, SquadStance stance, BodyguardOrderKind kind, int discriminator)
        {
            _lastApplied[handle] = new AppliedOrder { Stance = stance, Kind = kind, Discriminator = discriminator };
        }

        private void ApplyEscort(IReadOnlyList<int> handles)
        {
            if (_gameBridge.IsPlayerDead()) return;
            int now = _gameBridge.GetGameTime();
            var playerPos = _gameBridge.GetPlayerPosition();
            int playerHandle = _gameBridge.GetPlayerPedHandle();

            foreach (var pedHandle in handles)
            {
                if (_gameBridge.IsPedInVehicle(pedHandle))
                {
                    _reconciler.Submit(pedHandle, PedIntent.LeaveVehicle());
                    continue;
                }

                // Distance, not group membership, decides recovery. IsPedFollowingPlayer only
                // checks ped-group MEMBERSHIP; group-follow can leave a ped nominally "following"
                // yet stranded, and cannot drag it back from beyond its range.
                float dist = playerPos.DistanceTo(_gameBridge.GetPedPosition(pedHandle));
                if (dist <= EscortFollowRepairDistance)
                {
                    ApplyNearEscort(pedHandle, now);
                }
                else
                {
                    ApplyStrandedRecovery(pedHandle, playerHandle, playerPos, dist, now);
                }
            }
        }

        // Within group-follow range: trust the follow flag, otherwise periodically re-apply
        // group-follow (force re-application even though the intent is unchanged).
        private void ApplyNearEscort(int pedHandle, int now)
        {
            if (_gameBridge.IsPedFollowingPlayer(pedHandle))
            {
                _lastFollowReassertMs.Remove(pedHandle);
                return;
            }
            if (_gameBridge.IsPedInCombat(pedHandle)) return;
            if (_lastFollowReassertMs.TryGetValue(pedHandle, out var last) && now - last < FollowerReassertIntervalMs) return;

            _reconciler.Forget(pedHandle);
            _reconciler.Submit(pedHandle, PedIntent.FollowPlayer());
            _lastFollowReassertMs[pedHandle] = now;
            FileLogger.AI($"SquadStance Escort: ped {pedHandle} re-followed (near)");
        }

        // Beyond group-follow range: group re-add can't sprint the ped back. Sprint it to the
        // player (persistent follow task, issued once via reconciler dedup); if it is too far to
        // run back at all (respawn / fast-travel), warp it next to the player and re-follow.
        private void ApplyStrandedRecovery(int pedHandle, int playerHandle, Vector3 playerPos, float dist, int now)
        {
            if (dist > EscortTeleportDistance)
            {
                if (_lastFollowReassertMs.TryGetValue(pedHandle, out var last) && now - last < FollowerReassertIntervalMs) return;
                _gameBridge.SetPedPosition(pedHandle, TeleportPointNear(playerPos));
                _reconciler.Forget(pedHandle);
                _reconciler.Submit(pedHandle, PedIntent.FollowPlayer());
                _lastFollowReassertMs[pedHandle] = now;
                FileLogger.AI($"SquadStance Escort: ped {pedHandle} teleported back dist={dist:F0}");
                return;
            }

            _reconciler.Submit(pedHandle, PedIntent.RegroupOnPlayer(playerHandle, EscortFollowRepairDistance));
            if (_lastFollowReassertMs.TryGetValue(pedHandle, out var lastLog) && now - lastLog < FollowerReassertIntervalMs) return;
            _lastFollowReassertMs[pedHandle] = now;
            FileLogger.AI($"SquadStance Escort: ped {pedHandle} sprinting back dist={dist:F0}");
        }

        // A few metres off the player so warped-in bodyguards don't stack on the player model.
        private static Vector3 TeleportPointNear(Vector3 playerPos)
            => new Vector3(playerPos.X + 2f, playerPos.Y + 2f, playerPos.Z);

        // A bodyguard that boarded the player's vehicle during Escort must get out before it can
        // hold or hunt on the ground. Returns true if it was in a vehicle (and was tasked to leave),
        // so the caller skips ground tasking until it is out.
        private bool DisembarkedThisTick(int pedHandle)
        {
            if (!_gameBridge.IsPedInVehicle(pedHandle)) return false;
            _reconciler.Submit(pedHandle, PedIntent.LeaveVehicle());
            return true;
        }

        private void ApplyHoldArea(IReadOnlyList<int> handles)
        {
            // Center the hold ring on the player so bodyguards hold near you, not across the zone.
            var holdCenter = _gameBridge.GetPlayerPosition();
            for (int i = 0; i < handles.Count; i++)
            {
                int pedHandle = handles[i];
                if (DisembarkedThisTick(pedHandle)) continue;
                if (AlreadyApplied(pedHandle, SquadStance.HoldArea, BodyguardOrderKind.HoldAtPoint, i)) continue;

                var order = _stanceResolver.Resolve(SquadStance.HoldArea, holdCenter, HoldRingRadius, i, handles.Count);
                _reconciler.Submit(pedHandle, PedIntent.GuardArea(order.Point, HoldRadiusPerBodyguard, i));
                Remember(pedHandle, SquadStance.HoldArea, BodyguardOrderKind.HoldAtPoint, i);
                FileLogger.AI($"SquadStance HoldArea: ped {pedHandle} guard ({order.Point.X:F0},{order.Point.Y:F0}) inPlayerGroup={_gameBridge.IsPedFollowingPlayer(pedHandle)} inCombat={_gameBridge.IsPedInCombat(pedHandle)}");
            }
        }

        private void ApplySearchAndDestroy(Vector3 anchorCenter, float anchorRadius, IReadOnlyList<int> handles, IReadOnlyList<EnemyTarget> enemies)
        {
            if (enemies == null || enemies.Count == 0)
            {
                SeekFallback(anchorRadius, handles);
                return;
            }

            var bodyguards = new List<BodyguardPosition>();
            foreach (var pedHandle in handles)
            {
                bodyguards.Add(new BodyguardPosition(pedHandle, _gameBridge.GetPedPosition(pedHandle)));
            }

            var enemyPositions = new Dictionary<int, Vector3>();
            foreach (var enemy in enemies) enemyPositions[enemy.Handle] = enemy.Position;

            var assignment = _assignmentResolver.Assign(bodyguards, enemies, BuildPreviousAssignment());
            foreach (var pedHandle in handles)
            {
                if (DisembarkedThisTick(pedHandle)) continue;
                if (!assignment.TryGetValue(pedHandle, out var targetHandle)) continue;
                if (!enemyPositions.TryGetValue(targetHandle, out var targetPos)) continue;

                ApplyEngagement(pedHandle, targetHandle, targetPos);
            }
        }

        // Advance toward the assigned enemy until in weapon range with line of sight, then engage.
        // Hysteresis (in the resolver) keeps the phase from flipping every tick.
        private void ApplyEngagement(int pedHandle, int targetHandle, Vector3 targetPos)
        {
            float dist = _gameBridge.GetPedPosition(pedHandle).DistanceTo(targetPos);
            bool los = _gameBridge.HasClearLineOfSight(pedHandle, targetHandle);
            var role = _rolesByHandle.TryGetValue(pedHandle, out var r) ? r : DefenderRole.Grunt;
            var phase = _enginePhase.TryGetValue(pedHandle, out var p) ? p : EngagePhase.Advance;
            int misses = _losMisses.TryGetValue(pedHandle, out var m) ? m : 0;

            var decision = _engagementResolver.Resolve(dist, los, role, phase, misses);
            _enginePhase[pedHandle] = decision.Phase;
            _losMisses[pedHandle] = decision.ConsecutiveLosMisses;

            if (decision.Phase == EngagePhase.Engage)
            {
                if (AlreadyApplied(pedHandle, SquadStance.SearchAndDestroy, BodyguardOrderKind.AttackTarget, targetHandle)) return;
                _reconciler.Submit(pedHandle, PedIntent.CombatTarget(targetHandle));
                Remember(pedHandle, SquadStance.SearchAndDestroy, BodyguardOrderKind.AttackTarget, targetHandle);
                FileLogger.AI($"SquadStance S&D: ped {pedHandle} engage {targetHandle} dist={dist:F0} los={los}");
            }
            else
            {
                if (AlreadyApplied(pedHandle, SquadStance.SearchAndDestroy, BodyguardOrderKind.AdvanceOnTarget, targetHandle)) return;
                _reconciler.Submit(pedHandle, PedIntent.AdvanceOnTarget(targetHandle, decision.EngageRange));
                Remember(pedHandle, SquadStance.SearchAndDestroy, BodyguardOrderKind.AdvanceOnTarget, targetHandle);
                FileLogger.AI($"SquadStance S&D: ped {pedHandle} advance {targetHandle} dist={dist:F0} stop={decision.EngageRange:F0}");
            }
        }

        private Dictionary<int, int> BuildPreviousAssignment()
        {
            var previous = new Dictionary<int, int>();
            foreach (var kvp in _lastApplied)
            {
                // Both phases pin a follower to its enemy (Discriminator = target handle): keeping
                // AdvanceOnTarget here preserves stickiness across the whole advance->engage lifecycle,
                // not just once a follower is already engaging.
                if (kvp.Value.Kind == BodyguardOrderKind.AttackTarget
                    || kvp.Value.Kind == BodyguardOrderKind.AdvanceOnTarget)
                {
                    previous[kvp.Key] = kvp.Value.Discriminator;
                }
            }

            return previous;
        }

        private void SeekFallback(float anchorRadius, IReadOnlyList<int> handles)
        {
            foreach (var pedHandle in handles)
            {
                if (DisembarkedThisTick(pedHandle)) continue;
                if (AlreadyApplied(pedHandle, SquadStance.SearchAndDestroy, BodyguardOrderKind.SeekInRadius, 0)) continue;
                _reconciler.Submit(pedHandle, PedIntent.SeekHatedTargets(new Vector3(0f, 0f, 0f), anchorRadius));
                Remember(pedHandle, SquadStance.SearchAndDestroy, BodyguardOrderKind.SeekInRadius, 0);
                FileLogger.AI($"SquadStance S&D-seek: ped {pedHandle} seek r{anchorRadius:F0} inPlayerGroup={_gameBridge.IsPedFollowingPlayer(pedHandle)} inCombat={_gameBridge.IsPedInCombat(pedHandle)}");
            }
        }
    }
}
