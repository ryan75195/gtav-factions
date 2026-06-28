namespace FactionWars.Performance.Models
{
    /// <summary>
    /// Thresholds for <c>TickProfiler</c>. Defaults chosen against SHVDN's
    /// ScriptTimeoutThreshold=5000ms: breadcrumbs start well before the kill,
    /// and any tick over a second is worth a log line.
    /// </summary>
    public class TickProfilerOptions
    {
        /// <summary>Once a tick's elapsed time reaches this, write a breadcrumb before each remaining phase.</summary>
        public long BreadcrumbAfterMs { get; set; } = 1000;

        /// <summary>Report a SLOW TICK summary when the whole tick meets or exceeds this.</summary>
        public long SlowTickMs { get; set; } = 1000;
    }
}
