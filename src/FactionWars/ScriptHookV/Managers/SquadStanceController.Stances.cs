using System.Collections.Generic;
using FactionWars.Combat.Models;
using FactionWars.Core.Interfaces;
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

            foreach (var pedHandle in handles)
            {
                if (_gameBridge.IsPedInVehicle(pedHandle))
                {
                    _reconciler.Submit(pedHandle, PedIntent.LeaveVehicle());
                    continue;
                }
                if (_gameBridge.IsPedFollowingPlayer(pedHandle))
                {
                    _lastFollowReassertMs.Remove(pedHandle);
                    continue;
                }
                if (_gameBridge.IsPedInCombat(pedHandle))
                {
                    continue;
                }
                if (_lastFollowReassertMs.TryGetValue(pedHandle, out var last) && now - last < FollowerReassertIntervalMs)
                {
                    continue;
                }

                // Periodic repair: force re-application even though the intent is unchanged.
                _reconciler.Forget(pedHandle);
                _reconciler.Submit(pedHandle, PedIntent.FollowPlayer());
                _lastFollowReassertMs[pedHandle] = now;
                FileLogger.AI($"SquadStance Escort: ped {pedHandle} re-followed");
            }
        }

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

            var assignment = _assignmentResolver.Assign(bodyguards, enemies, BuildPreviousAssignment());
            foreach (var pedHandle in handles)
            {
                if (DisembarkedThisTick(pedHandle)) continue;
                if (!assignment.TryGetValue(pedHandle, out var targetHandle)) continue;
                if (AlreadyApplied(pedHandle, SquadStance.SearchAndDestroy, BodyguardOrderKind.AttackTarget, targetHandle)) continue;

                _reconciler.Submit(pedHandle, PedIntent.CombatTarget(targetHandle));
                Remember(pedHandle, SquadStance.SearchAndDestroy, BodyguardOrderKind.AttackTarget, targetHandle);
                FileLogger.AI($"SquadStance S&D: ped {pedHandle} attack {targetHandle} inPlayerGroup={_gameBridge.IsPedFollowingPlayer(pedHandle)} inCombat={_gameBridge.IsPedInCombat(pedHandle)}");
            }
        }

        private Dictionary<int, int> BuildPreviousAssignment()
        {
            var previous = new Dictionary<int, int>();
            foreach (var kvp in _lastApplied)
            {
                if (kvp.Value.Kind == BodyguardOrderKind.AttackTarget)
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
