using System;
using System.Collections.Generic;
using FactionWars.Combat.Models;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.Core.Utils;
using FactionWars.ScriptHookV.Telemetry;
using FactionWars.Telemetry.Interfaces;
using FactionWars.Telemetry.Models;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV.Telemetry
{
    public class CombatBehaviorSamplerTests
    {
        private sealed class FakeSource : ITrackedCombatantSource
        {
            private readonly List<TrackedCombatant> _items;
            public FakeSource(params TrackedCombatant[] items) => _items = new List<TrackedCombatant>(items);
            public IReadOnlyList<TrackedCombatant> GetTrackedCombatants() => _items;
        }

        private sealed class ThrowingSource : ITrackedCombatantSource
        {
            public IReadOnlyList<TrackedCombatant> GetTrackedCombatants() => throw new InvalidOperationException("boom");
        }

        private sealed class FakeSink : IBehaviorTraceSink
        {
            public List<BehaviorSampleRow> Rows { get; } = new List<BehaviorSampleRow>();
            public void Write(BehaviorSampleRow row) => Rows.Add(row);
            public void SetSaveFile(string saveFilename) { }
            public void Dispose() { }
        }

        private sealed class FakeEngagementSource : ISquadEngagementStateSource
        {
            private readonly Dictionary<int, SquadEngagementState> _states;
            public FakeEngagementSource(Dictionary<int, SquadEngagementState> states) => _states = states;
            public bool TryGetEngagementState(int handle, out SquadEngagementState state)
                => _states.TryGetValue(handle, out state);
            public IReadOnlyList<EngagementTransition> DrainEngagementTransitions()
                => Array.Empty<EngagementTransition>();
        }

        private readonly MockGameBridge _bridge = new MockGameBridge();
        private readonly FakeSink _sink = new FakeSink();

        private CombatBehaviorSampler Build(int interval, params ITrackedCombatantSource[] sources)
            => new CombatBehaviorSampler(_bridge, sources, _sink, sampleIntervalMs: interval);

        private CombatBehaviorSampler BuildWithEngagement(int interval, ISquadEngagementStateSource? engagement, params ITrackedCombatantSource[] sources)
            => new CombatBehaviorSampler(_bridge, sources, _sink, engagement, interval);

        private int LivePed(Vector3 pos) => _bridge.CreatePed("c", pos);

        [Fact]
        public void Update_BeforeInterval_DoesNotSample()
        {
            var ped = LivePed(new Vector3(0f, 0f, 0f));
            var sampler = Build(1000, new FakeSource(new TrackedCombatant(ped, CombatantKind.Follower, DefenderRole.Grunt)));
            _bridge.AdvanceGameTime(500);
            sampler.Update();
            Assert.Empty(_sink.Rows);
        }

        [Fact]
        public void Update_AfterInterval_SamplesEachLiveCombatant()
        {
            var p1 = LivePed(new Vector3(0f, 0f, 0f));
            var p2 = LivePed(new Vector3(5f, 0f, 0f));
            var sampler = Build(1000, new FakeSource(
                new TrackedCombatant(p1, CombatantKind.Follower, DefenderRole.Sniper),
                new TrackedCombatant(p2, CombatantKind.EnemyDefender, DefenderRole.Grunt)));
            _bridge.AdvanceGameTime(1000);
            sampler.Update();
            Assert.Equal(2, _sink.Rows.Count);
            Assert.Contains(_sink.Rows, r => r.Handle == p1 && r.Kind == CombatantKind.Follower && r.Role == DefenderRole.Sniper);
        }

        [Fact]
        public void Update_SkipsDeadOrMissingHandles()
        {
            var live = LivePed(new Vector3(0f, 0f, 0f));
            const int missing = 999999;
            var sampler = Build(1000, new FakeSource(
                new TrackedCombatant(live, CombatantKind.Follower, DefenderRole.Grunt),
                new TrackedCombatant(missing, CombatantKind.Follower, DefenderRole.Grunt)));
            _bridge.AdvanceGameTime(1000);
            sampler.Update();
            Assert.Single(_sink.Rows);
            Assert.Equal(live, _sink.Rows[0].Handle);
        }

        [Fact]
        public void Sample_PopulatesObservableFields()
        {
            var ped = LivePed(new Vector3(1f, 2f, 3f));
            _bridge.GivePedWeapon(ped, "weapon_sniperrifle");
            _bridge.SetPedShooting(ped, true);
            _bridge.SetPedCombatProfile(ped, 2, -1, -1);
            _bridge.SetPedHealth(ped, 250);
            _bridge.TaskCombatPed(ped, LivePed(new Vector3(9f, 9f, 9f)));
            var sampler = Build(1000, new FakeSource(new TrackedCombatant(ped, CombatantKind.Follower, DefenderRole.Sniper)));
            _bridge.AdvanceGameTime(1000);
            sampler.Update();
            var row = _sink.Rows.Find(r => r.Handle == ped);
            Assert.NotNull(row);
            Assert.Equal("WEAPON_SNIPERRIFLE", row!.Weapon);
            Assert.True(row.IsShooting);
            Assert.True(row.InCombat);
            Assert.Equal(2, row.CombatAbility);
            Assert.Equal(250, row.Health);
            Assert.Equal(1f, row.PosX);
            Assert.Equal(3f, row.PosZ);
        }

        [Fact]
        public void Sample_NearestHostile_SetForOpposingKinds()
        {
            var follower = LivePed(new Vector3(0f, 0f, 0f));
            var enemy = LivePed(new Vector3(10f, 0f, 0f));
            var sampler = Build(1000,
                new FakeSource(new TrackedCombatant(follower, CombatantKind.Follower, DefenderRole.Grunt)),
                new FakeSource(new TrackedCombatant(enemy, CombatantKind.EnemyDefender, DefenderRole.Grunt)));
            _bridge.AdvanceGameTime(1000);
            sampler.Update();
            var fRow = _sink.Rows.Find(r => r.Handle == follower);
            var eRow = _sink.Rows.Find(r => r.Handle == enemy);
            Assert.Equal(enemy, fRow!.TargetHandle);
            Assert.Equal(10f, fRow.DistToTarget, 3);
            Assert.Equal(follower, eRow!.TargetHandle);
        }

        [Fact]
        public void Sample_NoHostile_TargetIsMinusOne()
        {
            var f1 = LivePed(new Vector3(0f, 0f, 0f));
            var f2 = LivePed(new Vector3(5f, 0f, 0f));
            var sampler = Build(1000, new FakeSource(
                new TrackedCombatant(f1, CombatantKind.Follower, DefenderRole.Grunt),
                new TrackedCombatant(f2, CombatantKind.FriendlyDefender, DefenderRole.Grunt)));
            _bridge.AdvanceGameTime(1000);
            sampler.Update();
            Assert.All(_sink.Rows, r => Assert.Equal(-1, r.TargetHandle));
        }

        [Fact]
        public void Update_SourceThrows_DoesNotPropagate()
        {
            var sampler = Build(1000, new ThrowingSource());
            _bridge.AdvanceGameTime(1000);
            var ex = Record.Exception(() => sampler.Update());
            Assert.Null(ex);
        }

        [Fact]
        public void Update_WithEngagementSource_EnrichesRowWithLosAndPhase()
        {
            var ped = LivePed(new Vector3(0f, 0f, 0f));
            var states = new Dictionary<int, SquadEngagementState>
            {
                [ped] = new SquadEngagementState(EngagePhase.Engage, true, 0)
            };
            var sampler = BuildWithEngagement(1000, new FakeEngagementSource(states),
                new FakeSource(new TrackedCombatant(ped, CombatantKind.Follower, DefenderRole.Grunt)));
            _bridge.AdvanceGameTime(1000);
            sampler.Update();

            var row = _sink.Rows.Find(r => r.Handle == ped);
            Assert.NotNull(row);
            Assert.Equal("Engage", row!.EnginePhase);
            Assert.True(row.HasLineOfSight);
            Assert.Equal(0, row.MsSinceLos);
        }

        [Fact]
        public void Update_WithoutEngagementSource_LeavesEngagementFieldsDefault()
        {
            var ped = LivePed(new Vector3(0f, 0f, 0f));
            var sampler = Build(1000, new FakeSource(new TrackedCombatant(ped, CombatantKind.Follower, DefenderRole.Grunt)));
            _bridge.AdvanceGameTime(1000);
            sampler.Update();

            var row = _sink.Rows.Find(r => r.Handle == ped);
            Assert.NotNull(row);
            Assert.Equal(string.Empty, row!.EnginePhase);
            Assert.Equal(-1, row.MsSinceLos);
        }
    }
}
