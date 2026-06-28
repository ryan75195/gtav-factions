namespace FactionWars.Performance.Interfaces
{
    /// <summary>
    /// Destination for tick-profiling diagnostics. The breadcrumb is a single
    /// last-write-wins record of the phase currently executing during a slow tick;
    /// the slow-tick report is a one-line per-phase breakdown emitted after the tick.
    /// </summary>
    public interface ITickDiagnosticsSink
    {
        /// <summary>Records the phase currently executing (overwrites the previous breadcrumb).</summary>
        void WriteBreadcrumb(string content);

        /// <summary>Reports a completed tick whose total exceeded the slow-tick threshold.</summary>
        void ReportSlowTick(string summary);
    }
}
