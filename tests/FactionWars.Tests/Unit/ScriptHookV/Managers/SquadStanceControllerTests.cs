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
    public class SquadStanceControllerTests
    {
        private readonly MockGameBridge _bridge = new MockGameBridge();
        private SquadStanceController _controller = null!;

        private SquadStanceController Build()
            => new SquadStanceController(_bridge, new SquadStanceResolver(), new TargetAssignmentResolver(), new PedIntentReconciler(_bridge), new SquadEngagementResolver(new EngageRangeProvider()));

        private static readonly Vector3 Anchor = new Vector3(0f, 0f, 0f);

        // Most stance tests don't depend on role-specific engage ranges; an empty map falls back to
        // the Grunt range (18m), which the in-range S&D tests stay within.
        private static readonly IReadOnlyDictionary<int, DefenderRole> NoRoles = new Dictionary<int, DefenderRole>();

        [Fact]
        public void SetStance_AppliesTargetDirectly()
        {
            _controller = Build();
            var party = new List<int> { _bridge.CreatePed("bg", new Vector3(1f, 0f, 0f)) };

            _controller.SetStance(SquadStance.SearchAndDestroy, party);

            Assert.Equal(SquadStance.SearchAndDestroy, _controller.CurrentStance);
        }

        [Fact]
        public void SetStance_EmptyParty_DoesNotChangeStance()
        {
            _controller = Build();

            _controller.SetStance(SquadStance.SearchAndDestroy, new List<int>());

            Assert.Equal(SquadStance.Escort, _controller.CurrentStance);
        }

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

            _controller.Update(Anchor, 50f, party, new List<EnemyTarget>(), NoRoles);

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
            _controller.Update(new Vector3(1000f, 1000f, 0f), 80f, party, new List<EnemyTarget>(), NoRoles);

            var guardCenter = _bridge.GetGuardAreaCenter(bg);
            float distFromPlayer = _bridge.PlayerPosition.DistanceTo(guardCenter);
            Assert.True(distFromPlayer <= 12f, $"hold point should stay near the player, was {distFromPlayer:F1}m away");
        }

        [Fact]
        public void Update_HoldArea_DisembarksBodyguardStillInVehicle()
        {
            // Drop-off flow: a bodyguard that boarded during Escort must get out when the
            // player switches to HoldArea, so the squad can hold the ground and the player drives off.
            _controller = Build();
            int vehicle = _bridge.CreateTestVehicle();
            int bg = _bridge.CreatePed("bg", new Vector3(1f, 0f, 0f));
            _bridge.PutPedInVehicle(bg, vehicle, 1);
            var party = new List<int> { bg };
            _controller.CycleStance(party); // -> HoldArea

            _controller.Update(Anchor, 50f, party, new List<EnemyTarget>(), NoRoles);

            Assert.False(_bridge.IsPedInVehicle(bg));
        }

        [Fact]
        public void Update_SearchAndDestroy_DisembarksBodyguardStillInVehicle()
        {
            _controller = Build();
            int vehicle = _bridge.CreateTestVehicle();
            int bg = _bridge.CreatePed("bg", new Vector3(1f, 0f, 0f));
            _bridge.PutPedInVehicle(bg, vehicle, 1);
            var party = new List<int> { bg };
            _controller.CycleStance(party); // HoldArea
            _controller.CycleStance(party); // SearchAndDestroy

            _controller.Update(Anchor, 50f, party, new List<EnemyTarget> { new EnemyTarget(777, new Vector3(10f, 0f, 0f)) }, NoRoles);

            Assert.False(_bridge.IsPedInVehicle(bg));
        }

        [Fact]
        public void Update_SearchAndDestroy_InRangeWithLos_IssuesTaskCombatPed()
        {
            _controller = Build();
            int bg = _bridge.CreatePed("bg", new Vector3(1f, 0f, 0f));
            var party = new List<int> { bg };
            _controller.CycleStance(party); // HoldArea
            _controller.CycleStance(party); // SearchAndDestroy

            // 9m away (<= Grunt range 18) with line of sight -> engage.
            var enemy = new EnemyTarget(777, new Vector3(10f, 0f, 0f));
            _bridge.SetLineOfSight(bg, 777, true);
            _controller.Update(Anchor, 50f, party, new List<EnemyTarget> { enemy }, NoRoles);

            Assert.True(_bridge.IsPedCombatingPed(bg));
            Assert.Equal(777, _bridge.GetCombatPedTarget(bg));
        }

        [Fact]
        public void Update_SearchAndDestroy_OutOfRange_AdvancesOnTarget()
        {
            _controller = Build();
            int bg = _bridge.CreatePed("bg", new Vector3(0f, 0f, 0f));
            var party = new List<int> { bg };
            _controller.CycleStance(party); // HoldArea
            _controller.CycleStance(party); // SearchAndDestroy

            // 60m away (> Grunt range 18): advance toward the enemy entity, do not engage yet.
            var enemy = new EnemyTarget(777, new Vector3(60f, 0f, 0f));
            _bridge.SetLineOfSight(bg, 777, true);
            _controller.Update(Anchor, 250f, party, new List<EnemyTarget> { enemy }, NoRoles);

            Assert.Equal(777, _bridge.GetGoToEntityTarget(bg)!.Value);
            Assert.False(_bridge.IsPedCombatingPed(bg));
        }

        [Fact]
        public void Update_SearchAndDestroy_NoEnemies_FallsBackToSeek()
        {
            _controller = Build();
            int bg = _bridge.CreatePed("bg", new Vector3(1f, 0f, 0f));
            var party = new List<int> { bg };
            _controller.CycleStance(party); // HoldArea
            _controller.CycleStance(party); // SearchAndDestroy

            _controller.Update(Anchor, 50f, party, new List<EnemyTarget>(), NoRoles);

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
            _bridge.SetLineOfSight(bg, 100, true);

            // Commit to enemy 100 (4m away, in range + LOS -> engage).
            _controller.Update(Anchor, 50f, party, new List<EnemyTarget> { new EnemyTarget(100, new Vector3(5f, 0f, 0f)) }, NoRoles);
            Assert.Equal(100, _bridge.GetCombatPedTarget(bg));

            // Enemy 100 is still alive; a NEARER enemy 200 appears. The bodyguard must stay
            // committed to 100 rather than thrash onto the closer target.
            _controller.Update(Anchor, 50f, party, new List<EnemyTarget>
            {
                new EnemyTarget(100, new Vector3(5f, 0f, 0f)),
                new EnemyTarget(200, new Vector3(2f, 0f, 0f))
            }, NoRoles);

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
            _bridge.SetLineOfSight(bg, 100, true);
            _bridge.SetLineOfSight(bg, 200, true);

            _controller.Update(Anchor, 50f, party, new List<EnemyTarget> { new EnemyTarget(100, new Vector3(5f, 0f, 0f)) }, NoRoles);
            Assert.Equal(100, _bridge.GetCombatPedTarget(bg));

            // Previous target "dies"; a new enemy is the only one left.
            _controller.Update(Anchor, 50f, party, new List<EnemyTarget> { new EnemyTarget(200, new Vector3(5f, 0f, 0f)) }, NoRoles);
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

            _controller.Update(Anchor, 50f, party, new List<EnemyTarget>(), NoRoles);

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

            // A distant enemy: the follower advances (and detaching from the group happens whether it
            // advances or engages). Group-follow would otherwise pin it to the player.
            _controller.Update(Anchor, 50f, party, new List<EnemyTarget> { new EnemyTarget(777, new Vector3(10f, 0f, 0f)) }, NoRoles);

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

            _controller.Update(Anchor, 50f, party, new List<EnemyTarget>(), NoRoles); // seek fallback

            Assert.False(_bridge.IsPedFollowingPlayer(bg));
        }

        [Fact]
        public void Update_EscortStance_RunsNearBodyguardToPlayerInsteadOfTrustingGroupFollow()
        {
            // Escort now actively sprints every bodyguard to the player rather than relying on native
            // group-follow (which leashes/strands them across the zone). Even a nearby bodyguard gets
            // an explicit run-to-player task, not group membership.
            _controller = Build();
            _bridge.PlayerPosition = new Vector3(0f, 0f, 0f);
            _bridge.PlayerPedHandle = 7777;
            int bg = _bridge.CreatePed("bg", new Vector3(3f, 0f, 0f)); // near the player
            var party = new List<int> { bg };

            _controller.Update(Anchor, 50f, party, new List<EnemyTarget>(), NoRoles);

            Assert.True(_bridge.IsPedFollowingEntity(bg));        // run-to-player task issued
            Assert.Equal(7777, _bridge.GetFollowEntityTarget(bg)); // toward the player ped
        }

        [Fact]
        public void Update_EscortStance_StrandedBodyguard_SprintsBackInsteadOfTrustingGroupFollow()
        {
            // A stranded ped (here nominally still a group member) can't be recovered by GTA
            // group-follow from beyond its range. Escort must give it an explicit sprint-to-player
            // task rather than re-applying group membership, which doesn't sprint it back.
            _controller = Build();
            _bridge.PlayerPosition = new Vector3(0f, 0f, 0f);
            _bridge.PlayerPedHandle = 7777;
            int bg = _bridge.CreatePed("bg", new Vector3(50f, 0f, 0f)); // 50m: stranded, recoverable on foot
            _bridge.SetPedAsFollower(bg); // nominally in the player's group
            var party = new List<int> { bg };

            _controller.Update(Anchor, 50f, party, new List<EnemyTarget>(), NoRoles);

            Assert.True(_bridge.IsPedFollowingEntity(bg)); // sprint-to-player task issued
            Assert.Equal(7777, _bridge.GetFollowEntityTarget(bg)); // toward the player ped
        }

        [Fact]
        public void Update_EscortStance_BodyguardAtZoneRange_RunsBackInsteadOfTeleport()
        {
            // "Run to me regardless of range in the zone": an in-zone straggler (200m) sprints back
            // rather than teleporting. Teleport is reserved for absurd, out-of-zone distances.
            _controller = Build();
            _bridge.PlayerPosition = new Vector3(0f, 0f, 0f);
            _bridge.PlayerPedHandle = 7777;
            int bg = _bridge.CreatePed("bg", new Vector3(200f, 0f, 0f));
            var party = new List<int> { bg };

            _controller.Update(Anchor, 50f, party, new List<EnemyTarget>(), NoRoles);

            float dist = _bridge.PlayerPosition.DistanceTo(_bridge.GetPedPosition(bg));
            Assert.True(dist > 25f, $"expected run (position unchanged), but ped was teleported to {dist:F0}m");
            Assert.True(_bridge.IsPedFollowingEntity(bg)); // sprint-to-player task issued
        }

        [Fact]
        public void Update_EscortStance_VeryFarStrandedBodyguard_IsTeleportedNearPlayer()
        {
            // Beyond the teleport threshold (e.g. left behind by a respawn/fast-travel), running
            // back is absurd; the bodyguard is warped next to the player instead.
            _controller = Build();
            _bridge.PlayerPosition = new Vector3(0f, 0f, 0f);
            _bridge.PlayerPedHandle = 7777;
            int bg = _bridge.CreatePed("bg", new Vector3(300f, 0f, 0f)); // 300m: too far to run back
            var party = new List<int> { bg };

            _controller.Update(Anchor, 50f, party, new List<EnemyTarget>(), NoRoles);

            float dist = _bridge.PlayerPosition.DistanceTo(_bridge.GetPedPosition(bg));
            Assert.True(dist <= 25f, $"expected teleport near player, ped is {dist:F0}m away");
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

            _controller.Update(Anchor, 50f, party, new List<EnemyTarget>(), NoRoles);

            // TaskPedLeaveVehicle removes the ped from _pedsInVehicles; IsPedInVehicle becomes false.
            Assert.False(_bridge.IsPedInVehicle(bg));
            // The continue in ApplyEscort skips SetPedAsFollower for this tick.
            Assert.False(_bridge.IsPedFollowingPlayer(bg));
        }
    }
}
