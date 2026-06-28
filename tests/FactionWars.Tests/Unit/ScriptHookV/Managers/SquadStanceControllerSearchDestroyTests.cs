using System.Collections.Generic;
using FactionWars.Combat.Models;
using FactionWars.Combat.Services;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.Core.Utils;
using FactionWars.ScriptHookV.Managers;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV.Managers
{
    public class SquadStanceControllerSearchDestroyTests
    {
        private readonly MockGameBridge _bridge = new MockGameBridge();
        private readonly SquadStanceController _controller;

        public SquadStanceControllerSearchDestroyTests()
        {
            _controller = new SquadStanceController(
                _bridge,
                new SquadStanceResolver(),
                new TargetAssignmentResolver(),
                new PedIntentReconciler(_bridge),
                new SquadEngagementResolver(new EngageRangeProvider()));
            // Put the controller into Search & Destroy (Escort -> HoldArea -> SearchAndDestroy).
            var handles = new List<int> { 0 };
            _controller.CycleStance(handles);
            _controller.CycleStance(handles);
        }

        private static IReadOnlyDictionary<int, DefenderRole> Roles(int handle, DefenderRole role)
            => new Dictionary<int, DefenderRole> { [handle] = role };

        [Fact]
        public void OutOfRange_IssuesAdvance_GoToEntity()
        {
            int follower = _bridge.CreatePed("f", new Vector3(0f, 0f, 0f));
            int enemy = _bridge.CreatePed("e", new Vector3(60f, 0f, 0f)); // 60m > Grunt range 18
            _bridge.SetLineOfSight(follower, enemy, true);
            var enemies = new List<EnemyTarget> { new EnemyTarget(enemy, _bridge.GetPedPosition(enemy)) };

            _controller.Update(new Vector3(0f, 0f, 0f), 250f,
                new List<int> { follower }, enemies, Roles(follower, DefenderRole.Grunt));

            Assert.Equal(enemy, _bridge.GetGoToEntityTarget(follower)!.Value); // advancing
        }

        [Fact]
        public void InRangeWithLos_IssuesCombat()
        {
            int follower = _bridge.CreatePed("f", new Vector3(0f, 0f, 0f));
            int enemy = _bridge.CreatePed("e", new Vector3(10f, 0f, 0f)); // 10m <= Grunt range 18
            _bridge.SetLineOfSight(follower, enemy, true);
            var enemies = new List<EnemyTarget> { new EnemyTarget(enemy, _bridge.GetPedPosition(enemy)) };

            _controller.Update(new Vector3(0f, 0f, 0f), 250f,
                new List<int> { follower }, enemies, Roles(follower, DefenderRole.Grunt));

            Assert.True(_bridge.IsPedCombatingPed(follower)); // TaskCombatPed issued
            Assert.Equal(enemy, _bridge.GetCombatPedTarget(follower));
        }

        [Fact]
        public void InRangeNoLos_StaysAdvance()
        {
            int follower = _bridge.CreatePed("f", new Vector3(0f, 0f, 0f));
            int enemy = _bridge.CreatePed("e", new Vector3(10f, 0f, 0f)); // in range but no LOS set (default false)
            var enemies = new List<EnemyTarget> { new EnemyTarget(enemy, _bridge.GetPedPosition(enemy)) };

            _controller.Update(new Vector3(0f, 0f, 0f), 250f,
                new List<int> { follower }, enemies, Roles(follower, DefenderRole.Grunt));

            Assert.Equal(enemy, _bridge.GetGoToEntityTarget(follower)!.Value); // advancing, not engaging
            Assert.False(_bridge.IsPedCombatingPed(follower));
        }
    }
}
