using System;
using System.Collections.Generic;
using FactionWars.Combat.Interfaces;
using FactionWars.Core.Interfaces;
using FactionWars.Territory.Models;

namespace FactionWars.ScriptHookV.Managers
{
    /// <summary>
    /// When the player is in a zone and combat is relevant, makes that zone's defenders
    /// converge on the player and stay clustered. See
    /// docs/superpowers/specs/2026-05-01-defender-rally-on-threat-design.md.
    /// </summary>
    public sealed class DefenderRallyController
    {
        public const float RallyStoppingRangeM = 8.0f;
        public const long UnderAttackCoolDownMs = 5000;

        private readonly IGameBridge _bridge;
        private readonly ITerritoryEvents _territory;
        private readonly IFriendlyDefenderQuery _defenders;
        private readonly ICombatActivityQuery _combat;
        private readonly Func<string?> _currentPlayerFactionIdAccessor;
        private readonly Func<long> _nowMs;

        private long _underAttackUntilTickMs;
        private bool _wasUnderAttack;
        private readonly HashSet<int> _rallyingPeds = new HashSet<int>();

        public DefenderRallyController(
            IGameBridge bridge,
            ITerritoryEvents territory,
            IFriendlyDefenderQuery defenders,
            ICombatActivityQuery combat,
            Func<string?> currentPlayerFactionIdAccessor,
            Func<long> nowMs)
        {
            _bridge = bridge ?? throw new ArgumentNullException(nameof(bridge));
            _territory = territory ?? throw new ArgumentNullException(nameof(territory));
            _defenders = defenders ?? throw new ArgumentNullException(nameof(defenders));
            _combat = combat ?? throw new ArgumentNullException(nameof(combat));
            _currentPlayerFactionIdAccessor = currentPlayerFactionIdAccessor ?? throw new ArgumentNullException(nameof(currentPlayerFactionIdAccessor));
            _nowMs = nowMs ?? throw new ArgumentNullException(nameof(nowMs));
        }

        public void Update()
        {
            var zone = _territory.CurrentZone;
            var playerFactionId = _currentPlayerFactionIdAccessor();

            bool inOwnZone = zone != null && zone.OwnerFactionId != null && zone.OwnerFactionId == playerFactionId;
            bool inEnemyZone = zone != null && zone.OwnerFactionId != null && zone.OwnerFactionId != playerFactionId;

            bool isUnderAttackNow = false;
            if (inOwnZone)
            {
                bool wanted = _bridge.GetWantedLevel() > 0;
                bool encounter = _combat.HasActiveEncounter;
                bool damaged = _bridge.ConsumePlayerDamagedByPedFlag();
                isUnderAttackNow = wanted || encounter || damaged;
            }

            long now = _nowMs();
            if (isUnderAttackNow)
                _underAttackUntilTickMs = now + UnderAttackCoolDownMs;

            bool friendlyRally = inOwnZone && now < _underAttackUntilTickMs;
            bool hostileRally = inEnemyZone;

            bool shouldRally = friendlyRally || hostileRally;

            if (shouldRally && !_wasUnderAttack)
            {
                IssueRallyTasks(zone!);
            }
            else if (!shouldRally && _wasUnderAttack)
            {
                IssueStandDownTasks(zone);
            }

            _wasUnderAttack = shouldRally;
        }

        private void IssueRallyTasks(Zone zone)
        {
            int playerHandle = _bridge.GetPlayerPedHandle();
            var defenders = _defenders.GetDefendersInZone(zone.Id);
            _rallyingPeds.Clear();

            foreach (var pedHandle in defenders.Keys)
            {
                // TASK_FOLLOW_TO_OFFSET_OF_ENTITY with persistFollowing=true keeps the
                // defender glued to the player throughout the rally. TASK_GO_TO_ENTITY,
                // by contrast, terminates once the ped is within stoppingRange — they
                // would freeze on arrival and never re-pursue when the player walked off.
                // Engagement of hostiles is left to relationship-based combat AI; chaining
                // TaskCombatHatedTargetsAroundPed here would replace the follow task.
                _bridge.ClearPedTasks(pedHandle);
                _bridge.TaskFollowToOffsetFromEntity(
                    pedHandle,
                    playerHandle,
                    new Vector3(0, 0, 0),
                    moveBlendRatio: 3.0f,
                    stoppingRadius: RallyStoppingRangeM,
                    persistFollowing: true);
                _rallyingPeds.Add(pedHandle);
            }
        }

        private void IssueStandDownTasks(Zone? zone)
        {
            if (zone == null)
            {
                // The player left the zone; defenders that were rallying are out of scope
                // (they belong to a zone we no longer track). Just clear our tracking.
                _rallyingPeds.Clear();
                return;
            }

            foreach (var pedHandle in _rallyingPeds)
            {
                _bridge.TaskPedWanderInArea(pedHandle, zone.Center, zone.Radius);
            }
            _rallyingPeds.Clear();
        }
    }
}
