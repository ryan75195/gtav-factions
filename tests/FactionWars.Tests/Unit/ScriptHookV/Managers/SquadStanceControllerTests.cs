using System.Collections.Generic;
using FactionWars.Combat.Models;
using FactionWars.Combat.Services;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Utils;
using FactionWars.ScriptHookV.Managers;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV.Managers
{
    public class SquadStanceControllerTests
    {
        private readonly MockGameBridge _bridge = new MockGameBridge();
        private SquadStanceController _controller = null!;

        private SquadStanceController Build()
            => new SquadStanceController(_bridge, new SquadStanceResolver(), new TargetAssignmentResolver());

        private static readonly Vector3 Anchor = new Vector3(0f, 0f, 0f);

        [Fact]
        public void CycleStance_AdvancesEscortToHoldAreaToSearchAndDestroyToEscort()
        {
            _controller = Build();
            var party = new List<int> { _bridge.CreatePed("bg", new Vector3(1f, 0f, 0f)) };

            Assert.Equal(SquadStance.Escort, _controller.CurrentStance);
            _controller.CycleStance(party);
            Assert.Equal(SquadStance.HoldArea, _controller.CurrentStance);
            _controller.CycleStance(party);
            Assert.Equal(SquadStance.SearchAndDestroy, _controller.CurrentStance);
            _controller.CycleStance(party);
            Assert.Equal(SquadStance.Escort, _controller.CurrentStance);
        }

        [Fact]
        public void CycleStance_EmptyParty_DoesNotChangeStance()
        {
            _controller = Build();
            _controller.CycleStance(new List<int>());
            Assert.Equal(SquadStance.Escort, _controller.CurrentStance);
        }

        [Fact]
        public void Update_HoldArea_IssuesTaskGuardArea()
        {
            _controller = Build();
            int bg = _bridge.CreatePed("bg", new Vector3(1f, 0f, 0f));
            var party = new List<int> { bg };
            _controller.CycleStance(party); // -> HoldArea

            _controller.Update(Anchor, 50f, party, new List<EnemyTarget>());

            Assert.True(_bridge.IsPedGuardingArea(bg));
        }

        [Fact]
        public void Update_HoldArea_GuardsNearPlayerNotTheZoneAnchor()
        {
            // HoldArea should hold a tight ring around the player. Anchoring on the zone
            // centre/radius scattered bodyguards ~50m across the whole zone.
            _controller = Build();
            _bridge.PlayerPosition = new Vector3(100f, 100f, 0f);
            int bg = _bridge.CreatePed("bg", new Vector3(101f, 100f, 0f));
            var party = new List<int> { bg };
            _controller.CycleStance(party); // -> HoldArea

            // Far-off zone anchor with a large radius.
            _controller.Update(new Vector3(1000f, 1000f, 0f), 80f, party, new List<EnemyTarget>());

            var guardCenter = _bridge.GetGuardAreaCenter(bg);
            float distFromPlayer = _bridge.PlayerPosition.DistanceTo(guardCenter);
            Assert.True(distFromPlayer <= 12f, $"hold point should stay near the player, was {distFromPlayer:F1}m away");
        }

        [Fact]
        public void Update_SearchAndDestroy_WithEnemy_IssuesTaskCombatPed()
        {
            _controller = Build();
            int bg = _bridge.CreatePed("bg", new Vector3(1f, 0f, 0f));
            var party = new List<int> { bg };
            _controller.CycleStance(party); // HoldArea
            _controller.CycleStance(party); // SearchAndDestroy

            var enemy = new EnemyTarget(777, new Vector3(10f, 0f, 0f));
            _controller.Update(Anchor, 50f, party, new List<EnemyTarget> { enemy });

            Assert.True(_bridge.IsPedCombatingPed(bg));
            Assert.Equal(777, _bridge.GetCombatPedTarget(bg));
        }

        [Fact]
        public void Update_SearchAndDestroy_NoEnemies_FallsBackToSeek()
        {
            _controller = Build();
            int bg = _bridge.CreatePed("bg", new Vector3(1f, 0f, 0f));
            var party = new List<int> { bg };
            _controller.CycleStance(party); // HoldArea
            _controller.CycleStance(party); // SearchAndDestroy

            _controller.Update(Anchor, 50f, party, new List<EnemyTarget>());

            Assert.True(_bridge.IsPedCombatTargeting(bg)); // TaskCombatHatedTargetsAroundPed recorded
        }

        [Fact]
        public void Update_SearchAndDestroy_KeepsTargetWhenStillAlive_DespiteNearerEnemy()
        {
            _controller = Build();
            int bg = _bridge.CreatePed("bg", new Vector3(1f, 0f, 0f));
            var party = new List<int> { bg };
            _controller.CycleStance(party);
            _controller.CycleStance(party); // SearchAndDestroy

            // Commit to enemy 100.
            _controller.Update(Anchor, 50f, party, new List<EnemyTarget> { new EnemyTarget(100, new Vector3(5f, 0f, 0f)) });
            Assert.Equal(100, _bridge.GetCombatPedTarget(bg));

            // Enemy 100 is still alive; a NEARER enemy 200 appears. The bodyguard must stay
            // committed to 100 rather than thrash onto the closer target.
            _controller.Update(Anchor, 50f, party, new List<EnemyTarget>
            {
                new EnemyTarget(100, new Vector3(5f, 0f, 0f)),
                new EnemyTarget(200, new Vector3(2f, 0f, 0f))
            });

            Assert.Equal(100, _bridge.GetCombatPedTarget(bg));
        }

        [Fact]
        public void Update_SearchAndDestroy_RetargetsWhenAssignmentChanges()
        {
            _controller = Build();
            int bg = _bridge.CreatePed("bg", new Vector3(1f, 0f, 0f));
            var party = new List<int> { bg };
            _controller.CycleStance(party);
            _controller.CycleStance(party); // SearchAndDestroy

            _controller.Update(Anchor, 50f, party, new List<EnemyTarget> { new EnemyTarget(100, new Vector3(5f, 0f, 0f)) });
            Assert.Equal(100, _bridge.GetCombatPedTarget(bg));

            // Previous target "dies"; a new enemy is the only one left.
            _controller.Update(Anchor, 50f, party, new List<EnemyTarget> { new EnemyTarget(200, new Vector3(5f, 0f, 0f)) });
            Assert.Equal(200, _bridge.GetCombatPedTarget(bg));
        }

        [Fact]
        public void Update_HoldArea_RemovesBodyguardFromPlayerGroupSoGuardTaskIsNotOverridden()
        {
            _controller = Build();
            int bg = _bridge.CreatePed("bg", new Vector3(1f, 0f, 0f));
            _bridge.SetPedAsFollower(bg); // recruited bodyguards start in the player's ped group
            Assert.True(_bridge.IsPedFollowingPlayer(bg));
            var party = new List<int> { bg };
            _controller.CycleStance(party); // -> HoldArea

            _controller.Update(Anchor, 50f, party, new List<EnemyTarget>());

            // Native group-follow overrides TaskGuardArea; entering HoldArea must detach the
            // bodyguard from the player group so the guard task actually holds.
            Assert.False(_bridge.IsPedFollowingPlayer(bg));
        }

        [Fact]
        public void Update_SearchAndDestroy_RemovesBodyguardFromPlayerGroupSoCombatTaskIsNotOverridden()
        {
            _controller = Build();
            int bg = _bridge.CreatePed("bg", new Vector3(1f, 0f, 0f));
            _bridge.SetPedAsFollower(bg);
            Assert.True(_bridge.IsPedFollowingPlayer(bg));
            var party = new List<int> { bg };
            _controller.CycleStance(party); // HoldArea
            _controller.CycleStance(party); // SearchAndDestroy

            _controller.Update(Anchor, 50f, party, new List<EnemyTarget> { new EnemyTarget(777, new Vector3(10f, 0f, 0f)) });

            // The bodyguard is tasked to attack a distant enemy; group-follow would pin it to the
            // player. Entering S&D must detach it so it can engage.
            Assert.False(_bridge.IsPedFollowingPlayer(bg));
        }

        [Fact]
        public void Update_SearchAndDestroy_NoEnemies_SeekFallbackRemovesBodyguardFromPlayerGroup()
        {
            _controller = Build();
            int bg = _bridge.CreatePed("bg", new Vector3(1f, 0f, 0f));
            _bridge.SetPedAsFollower(bg);
            var party = new List<int> { bg };
            _controller.CycleStance(party); // HoldArea
            _controller.CycleStance(party); // SearchAndDestroy

            _controller.Update(Anchor, 50f, party, new List<EnemyTarget>()); // seek fallback

            Assert.False(_bridge.IsPedFollowingPlayer(bg));
        }

        [Fact]
        public void Update_EscortStance_RepairsFollowerGroupForBodyguardNotFollowingPlayer()
        {
            // Controller starts in Escort by default — no cycling needed.
            _controller = Build();
            int bg = _bridge.CreatePed("bg", new Vector3(1f, 0f, 0f));
            var party = new List<int> { bg };

            // CreatePed does not add the ped to the follower group, so it is not following yet.
            Assert.False(_bridge.IsPedFollowingPlayer(bg));

            _controller.Update(Anchor, 50f, party, new List<EnemyTarget>());

            // ApplyEscort must have called SetPedAsFollower to repair the group.
            Assert.True(_bridge.IsPedFollowingPlayer(bg));
        }

        [Fact]
        public void Update_EscortStance_OrdersBodyguardInVehicleToLeave()
        {
            // Arrange: controller starts in Escort by default — no cycling needed.
            _controller = Build();
            int vehicle = _bridge.CreateTestVehicle();
            int bg = _bridge.CreatePed("bg", new Vector3(1f, 0f, 0f));
            _bridge.PutPedInVehicle(bg, vehicle, 1); // seat 1 = front passenger
            var party = new List<int> { bg };

            // Player is on foot by default (IsPlayerInVehicleValue = false).
            Assert.True(_bridge.IsPedInVehicle(bg)); // pre-condition: bodyguard is in a vehicle

            _controller.Update(Anchor, 50f, party, new List<EnemyTarget>());

            // TaskPedLeaveVehicle removes the ped from _pedsInVehicles; IsPedInVehicle becomes false.
            Assert.False(_bridge.IsPedInVehicle(bg));
            // The continue in ApplyEscort skips SetPedAsFollower for this tick.
            Assert.False(_bridge.IsPedFollowingPlayer(bg));
        }
    }
}
