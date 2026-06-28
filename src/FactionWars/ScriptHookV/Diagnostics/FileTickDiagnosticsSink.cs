using System;
using System.IO;
using FactionWars.Performance.Interfaces;

namespace FactionWars.ScriptHookV.Diagnostics
{
    /// <summary>
    /// File-backed tick diagnostics. The breadcrumb is written last-write-wins to a
    /// dedicated file (independent of FileLogger) and flushed immediately, so it survives
    /// a SHVDN blocking-script abort (the game process keeps running). Slow-tick summaries
    /// are routed to an injected logger (wired to FileLogger.Warn in production).
    /// </summary>
    public class FileTickDiagnosticsSink : ITickDiagnosticsSink
    {
        private readonly string _breadcrumbFilePath;
        private readonly Action<string> _slowTickLogger;

        public FileTickDiagnosticsSink(string breadcrumbFilePath, Action<string> slowTickLogger)
        {
            _breadcrumbFilePath = breadcrumbFilePath ?? throw new ArgumentNullException(nameof(breadcrumbFilePath));
            _slowTickLogger = slowTickLogger ?? throw new ArgumentNullException(nameof(slowTickLogger));
        }

        /// <inheritdoc />
        public void WriteBreadcrumb(string content)
        {
            try
            {
                File.WriteAllText(_breadcrumbFilePath, content);
            }
            catch
            {
                // Diagnostics must never break the tick.
            }
        }

        /// <inheritdoc />
        public void ReportSlowTick(string summary)
        {
            _slowTickLogger(summary);
        }
    }
}
