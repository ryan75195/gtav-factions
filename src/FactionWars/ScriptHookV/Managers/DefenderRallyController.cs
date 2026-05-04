using System;
using System.Collections.Generic;
using FactionWars.Combat.Interfaces;
using FactionWars.Core.Interfaces;
using FactionWars.ScriptHookV.Models;
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

        public DefenderRallyController(DefenderRallyControllerDependencies dependencies)
        {
            if (dependencies == null) throw new ArgumentNullException(nameof(dependencies));
            _bridge = dependencies.Bridge ?? throw new ArgumentNullException(nameof(dependencies.Bridge));
            _territory = dependencies.Territory ?? throw new ArgumentNullException(nameof(dependencies.Territory));
            _defenders = dependencies.Defenders ?? throw new ArgumentNullException(nameof(dependencies.Defenders));
            _combat = dependencies.Combat ?? throw new ArgumentNullException(nameof(dependencies.Combat));
            _currentPlayerFactionIdAccessor = dependencies.CurrentPlayerFactionIdAccessor ?? throw new ArgumentNullException(nameof(dependencies.CurrentPlayerFactionIdAccessor));
            _nowMs = dependencies.NowMs ?? throw new ArgumentNullException(nameof(dependencies.NowMs));
        }

        public DefenderRallyController(params object?[] dependencies)
            : this(new DefenderRallyControllerDependencies
            {
                Bridge = (IGameBridge?)dependencies[0],
                Territory = (ITerritoryEvents?)dependencies[1],
                Defenders = (IFriendlyDefenderQuery?)dependencies[2],
                Combat = (ICombatActivityQuery?)dependencies[3],
                CurrentPlayerFactionIdAccessor = (Func<string?>?)dependencies[4],
                NowMs = (Func<long>?)dependencies[5]
            })
        {
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
