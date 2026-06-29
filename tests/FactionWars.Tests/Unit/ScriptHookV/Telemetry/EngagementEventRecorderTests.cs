using System;
using System.Collections.Generic;
using FactionWars.Combat.Models;
using FactionWars.ScriptHookV.Telemetry;
using FactionWars.Telemetry.Interfaces;
using FactionWars.Telemetry.Models;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV.Telemetry
{
    public class EngagementEventRecorderTests
    {
        private sealed class FakeSource : ISquadEngagementStateSource
        {
            private readonly Queue<IReadOnlyList<EngagementTransition>> _drains;
            public FakeSource(params IReadOnlyList<EngagementTransition>[] drains)
                => _drains = new Queue<IReadOnlyList<EngagementTransition>>(drains);
            public bool TryGetEngagementState(int handle, out SquadEngagementState state)
            {
                state = default;
                return false;
            }
            public IReadOnlyList<EngagementTransition> DrainEngagementTransitions()
                => _drains.Count > 0 ? _drains.Dequeue() : Array.Empty<EngagementTransition>();
        }

        private sealed class FakeSink : IEngagementEventSink
        {
            public readonly List<EngagementTransition> Written = new List<EngagementTransition>();
            public bool Throw;
            public void Write(EngagementTransition e)
            {
                if (Throw) throw new InvalidOperationException("boom");
                Written.Add(e);
            }
            public void SetSaveFile(string saveFilename) { }
            public void Dispose() { }
        }

        private static EngagementTransition T(int handle) => new EngagementTransition(
            handle, 100, EngagePhase.Advance, EngagePhase.Engage,
            EngagePhaseChangeReason.EngageAcquired, 10f, true, 0);

        [Fact]
        public void Update_WritesEachDrainedTransition()
        {
            var source = new FakeSource(new[] { T(1), T(2) });
            var sink = new FakeSink();
            var recorder = new EngagementEventRecorder(source, sink);

            recorder.Update();

            Assert.Equal(2, sink.Written.Count);
            Assert.Equal(1, sink.Written[0].Handle);
            Assert.Equal(2, sink.Written[1].Handle);
        }

        [Fact]
        public void Update_WhenSinkThrows_DoesNotPropagate()
        {
            var source = new FakeSource(new[] { T(1) });
            var sink = new FakeSink { Throw = true };
            var recorder = new EngagementEventRecorder(source, sink);

            var ex = Record.Exception(() => recorder.Update());

            Assert.Null(ex); // sampling must never crash the game tick
        }
    }
}
