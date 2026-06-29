using System.Collections.Generic;
using FactionWars.Combat.Models;
using FactionWars.Combat.Services;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.Core.Utils;
using FactionWars.ScriptHookV.Managers;
using FactionWars.Telemetry.Interfaces;
using FactionWars.Telemetry.Models;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV.Managers
{
    public class SquadStanceControllerEngagementTelemetryTests
    {
        private readonly MockGameBridge _bridge = new MockGameBridge();
        private readonly SquadStanceController _controller;

        public SquadStanceControllerEngagementTelemetryTests()
        {
            _controller = new SquadStanceController(
                _bridge,
                new SquadStanceResolver(),
                new TargetAssignmentResolver(),
                new PedIntentReconciler(_bridge),
                new SquadEngagementResolver(new EngageRangeProvider()));
            var handles = new List<int> { 0 };
            _controller.CycleStance(handles); // Escort -> HoldArea
            _controller.CycleStance(handles); // HoldArea -> SearchAndDestroy
        }

        private ISquadEngagementStateSource Source => _controller;

        private static IReadOnlyDictionary<int, DefenderRole> Roles(int handle, DefenderRole role)
            => new Dictionary<int, DefenderRole> { [handle] = role };

        [Fact]
        public void TryGetEngagementState_AfterEngaging_ReportsEngagePhaseAndLos()
        {
            int follower = _bridge.CreatePed("f", new Vector3(0f, 0f, 0f));
            int enemy = _bridge.CreatePed("e", new Vector3(10f, 0f, 0f)); // in Grunt range 18
            _bridge.SetLineOfSight(follower, enemy, true);
            var enemies = new List<EnemyTarget> { new EnemyTarget(enemy, _bridge.GetPedPosition(enemy)) };

            _controller.Update(new Vector3(0f, 0f, 0f), 250f,
                new List<int> { follower }, enemies, Roles(follower, DefenderRole.Grunt));

            Assert.True(Source.TryGetEngagementState(follower, out var state));
            Assert.Equal(EngagePhase.Engage, state.Phase);
            Assert.True(state.HasLineOfSight);
            Assert.Equal(0, state.MsSinceLos);
        }

        [Fact]
        public void DrainEngagementTransitions_OnAcquisition_EmitsEngageAcquiredOnce()
        {
            int follower = _bridge.CreatePed("f", new Vector3(0f, 0f, 0f));
            int enemy = _bridge.CreatePed("e", new Vector3(10f, 0f, 0f));
            _bridge.SetLineOfSight(follower, enemy, true);
            var enemies = new List<EnemyTarget> { new EnemyTarget(enemy, _bridge.GetPedPosition(enemy)) };

            _controller.Update(new Vector3(0f, 0f, 0f), 250f,
                new List<int> { follower }, enemies, Roles(follower, DefenderRole.Grunt));

            var first = Source.DrainEngagementTransitions();
            Assert.Single(first);
            Assert.Equal(follower, first[0].Handle);
            Assert.Equal(EngagePhase.Advance, first[0].FromPhase);
            Assert.Equal(EngagePhase.Engage, first[0].ToPhase);
            Assert.Equal(EngagePhaseChangeReason.EngageAcquired, first[0].Reason);

            // Drain is destructive: a second drain with no new transition is empty.
            Assert.Empty(Source.DrainEngagementTransitions());
        }

        [Fact]
        public void Update_WhenFollowerLosesItsTarget_ForgetsEngagementState()
        {
            int follower = _bridge.CreatePed("f", new Vector3(0f, 0f, 0f));
            int enemy = _bridge.CreatePed("e", new Vector3(10f, 0f, 0f));
            _bridge.SetLineOfSight(follower, enemy, true);
            var enemies = new List<EnemyTarget> { new EnemyTarget(enemy, _bridge.GetPedPosition(enemy)) };
            _controller.Update(new Vector3(0f, 0f, 0f), 250f,
                new List<int> { follower }, enemies, Roles(follower, DefenderRole.Grunt));
            Assert.True(Source.TryGetEngagementState(follower, out _)); // engaged: state present

            // Enemies gone (the follower is no longer assigned a target). Its LOS clock must be
            // dropped, otherwise it freezes and a later reassignment reports a stale ms_since_los.
            _controller.Update(new Vector3(0f, 0f, 0f), 250f,
                new List<int> { follower }, new List<EnemyTarget>(), Roles(follower, DefenderRole.Grunt));

            Assert.False(Source.TryGetEngagementState(follower, out _));
        }
    }
}
