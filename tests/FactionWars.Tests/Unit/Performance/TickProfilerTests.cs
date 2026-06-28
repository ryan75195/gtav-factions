using System;
using System.Collections.Generic;
using FactionWars.Core.Interfaces;
using FactionWars.Performance.Interfaces;
using FactionWars.Performance.Models;
using FactionWars.Performance.Services;
using Xunit;

namespace FactionWars.Tests.Unit.Performance
{
    public class TickProfilerTests
    {
        private sealed class FakeClock : ITimeProvider
        {
            private DateTime _now = new DateTime(2026, 6, 28, 0, 0, 0, DateTimeKind.Utc);
            public DateTime UtcNow => _now;
            public DateTime Now => _now;
            public void AdvanceMs(long ms) => _now = _now.AddMilliseconds(ms);
        }

        private sealed class FakeSink : ITickDiagnosticsSink
        {
            public List<string> Breadcrumbs { get; } = new List<string>();
            public List<string> SlowTicks { get; } = new List<string>();
            public void WriteBreadcrumb(string content) => Breadcrumbs.Add(content);
            public void ReportSlowTick(string summary) => SlowTicks.Add(summary);
        }

        private static TickProfiler Build(FakeClock clock, FakeSink sink, long breadcrumbAfterMs = 1000, long slowTickMs = 1000)
            => new TickProfiler(clock, sink, new TickProfilerOptions { BreadcrumbAfterMs = breadcrumbAfterMs, SlowTickMs = slowTickMs });

        [Fact]
        public void Measure_InvokesBody()
        {
            var clock = new FakeClock();
            var profiler = Build(clock, new FakeSink());
            var ran = false;
            profiler.BeginTick();
            profiler.Measure("phase", () => ran = true);
            Assert.True(ran);
        }

        [Fact]
        public void EndTick_ReportsSlowTick_WhenTotalMeetsThreshold()
        {
            var clock = new FakeClock();
            var sink = new FakeSink();
            var profiler = Build(clock, sink, slowTickMs: 1000);
            profiler.BeginTick();
            profiler.Measure("slow", () => clock.AdvanceMs(1200));
            profiler.EndTick();
            Assert.Single(sink.SlowTicks);
            Assert.Contains("slow", sink.SlowTicks[0]);
            Assert.Contains("1200", sink.SlowTicks[0]);
        }

        [Fact]
        public void EndTick_DoesNotReport_WhenTickFast()
        {
            var clock = new FakeClock();
            var sink = new FakeSink();
            var profiler = Build(clock, sink, slowTickMs: 1000);
            profiler.BeginTick();
            profiler.Measure("fast", () => clock.AdvanceMs(50));
            profiler.EndTick();
            Assert.Empty(sink.SlowTicks);
        }

        [Fact]
        public void Measure_WritesBreadcrumb_OnlyAfterCumulativeThreshold()
        {
            var clock = new FakeClock();
            var sink = new FakeSink();
            var profiler = Build(clock, sink, breadcrumbAfterMs: 1000);
            profiler.BeginTick();
            profiler.Measure("first", () => clock.AdvanceMs(1100)); // cumulative 0 entering -> no breadcrumb
            profiler.Measure("second", () => clock.AdvanceMs(10));  // cumulative 1100 entering -> breadcrumb
            Assert.Single(sink.Breadcrumbs);
            Assert.Contains("second", sink.Breadcrumbs[0]);
        }

        [Fact]
        public void Measure_RecordsPhase_EvenWhenBodyThrows()
        {
            var clock = new FakeClock();
            var sink = new FakeSink();
            var profiler = Build(clock, sink, slowTickMs: 1);
            profiler.BeginTick();
            Assert.Throws<InvalidOperationException>(() =>
                profiler.Measure("boom", () => { clock.AdvanceMs(5); throw new InvalidOperationException(); }));
            profiler.EndTick();
            Assert.Single(sink.SlowTicks);
            Assert.Contains("boom", sink.SlowTicks[0]);
        }
    }
}
