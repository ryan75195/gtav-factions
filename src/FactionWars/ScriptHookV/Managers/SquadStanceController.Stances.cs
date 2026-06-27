using System.Collections.Generic;
using FactionWars.Combat.Models;
using FactionWars.Core.Interfaces;

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
                    _gameBridge.TaskPedLeaveVehicle(pedHandle);
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

                _gameBridge.SetPedAsFollower(pedHandle);
                _lastFollowReassertMs[pedHandle] = now;
            }
        }

        private void ApplyHoldArea(Vector3 anchorCenter, float anchorRadius, IReadOnlyList<int> handles)
        {
            for (int i = 0; i < handles.Count; i++)
            {
                int pedHandle = handles[i];
                if (AlreadyApplied(pedHandle, SquadStance.HoldArea, BodyguardOrderKind.HoldAtPoint, i)) continue;

                var order = _stanceResolver.Resolve(SquadStance.HoldArea, anchorCenter, anchorRadius, i, handles.Count);
                _gameBridge.TaskGuardArea(pedHandle, order.Point, HoldRadiusPerBodyguard);
                Remember(pedHandle, SquadStance.HoldArea, BodyguardOrderKind.HoldAtPoint, i);
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

            var assignment = _assignmentResolver.Assign(bodyguards, enemies);
            foreach (var pedHandle in handles)
            {
                if (!assignment.TryGetValue(pedHandle, out var targetHandle)) continue;
                if (AlreadyApplied(pedHandle, SquadStance.SearchAndDestroy, BodyguardOrderKind.AttackTarget, targetHandle)) continue;

                _gameBridge.TaskCombatPed(pedHandle, targetHandle);
                Remember(pedHandle, SquadStance.SearchAndDestroy, BodyguardOrderKind.AttackTarget, targetHandle);
            }
        }

        private void SeekFallback(float anchorRadius, IReadOnlyList<int> handles)
        {
            foreach (var pedHandle in handles)
            {
                if (AlreadyApplied(pedHandle, SquadStance.SearchAndDestroy, BodyguardOrderKind.SeekInRadius, 0)) continue;
                _gameBridge.TaskCombatHatedTargetsAroundPed(pedHandle, anchorRadius);
                Remember(pedHandle, SquadStance.SearchAndDestroy, BodyguardOrderKind.SeekInRadius, 0);
            }
        }
    }
}
