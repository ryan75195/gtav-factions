using System;
using System.Collections.Generic;
using System.Text;
using FactionWars.Core.Interfaces;
using FactionWars.Performance.Interfaces;
using FactionWars.Performance.Models;

namespace FactionWars.Performance.Services
{
    /// <summary>
    /// Times each named phase of a game tick. Once a tick's elapsed time crosses
    /// the breadcrumb threshold it records the phase about to run (so a freeze names
    /// the culprit); on tick end it reports a per-phase breakdown if the tick was slow.
    /// Single-threaded: driven from the script thread only.
    /// </summary>
    public class TickProfiler : ITickProfiler
    {
        private readonly ITimeProvider _time;
        private readonly ITickDiagnosticsSink _sink;
        private readonly TickProfilerOptions _options;
        private readonly List<PhaseTiming> _phases = new List<PhaseTiming>();
        private DateTime _tickStart;

        public TickProfiler(ITimeProvider time, ITickDiagnosticsSink sink, TickProfilerOptions options)
        {
            _time = time ?? throw new ArgumentNullException(nameof(time));
            _sink = sink ?? throw new ArgumentNullException(nameof(sink));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public void BeginTick()
        {
            _phases.Clear();
            _tickStart = _time.UtcNow;
        }

        public void Measure(string phaseName, Action body)
        {
            if (body == null) throw new ArgumentNullException(nameof(body));

            long elapsedIntoTick = (long)(_time.UtcNow - _tickStart).TotalMilliseconds;
            if (elapsedIntoTick >= _options.BreadcrumbAfterMs)
                _sink.WriteBreadcrumb($"{phaseName} (tick+{elapsedIntoTick}ms)");

            var phaseStart = _time.UtcNow;
            try
            {
                body();
            }
            finally
            {
                long durationMs = (long)(_time.UtcNow - phaseStart).TotalMilliseconds;
                _phases.Add(new PhaseTiming(phaseName, durationMs));
            }
        }

        public void EndTick()
        {
            long totalMs = (long)(_time.UtcNow - _tickStart).TotalMilliseconds;
            if (totalMs >= _options.SlowTickMs)
                _sink.ReportSlowTick(FormatSummary(totalMs));
        }

        private string FormatSummary(long totalMs)
        {
            var sb = new StringBuilder();
            sb.Append("SLOW TICK total=").Append(totalMs).Append("ms |");
            foreach (var phase in _phases)
                sb.Append(' ').Append(phase.Name).Append('=').Append(phase.DurationMs).Append("ms");
            return sb.ToString();
        }

        private struct PhaseTiming
        {
            public PhaseTiming(string name, long durationMs)
            {
                Name = name;
                DurationMs = durationMs;
            }

            public string Name { get; }
            public long DurationMs { get; }
        }
    }
}
