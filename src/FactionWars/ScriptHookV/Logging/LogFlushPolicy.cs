using System;

namespace FactionWars.ScriptHookV.Logging
{
    /// <summary>
    /// Decides which log levels must be flushed to disk immediately. Chatty levels are
    /// buffered and flushed on a timer (I/O off the script thread); crash-critical levels
    /// (WARN/ERROR) flush at once so they survive a blocking-script abort.
    /// </summary>
    public static class LogFlushPolicy
    {
        /// <summary>True if a line at this level must be flushed to disk immediately.</summary>
        public static bool RequiresImmediateFlush(string level)
        {
            return string.Equals(level, "ERROR", StringComparison.OrdinalIgnoreCase)
                || string.Equals(level, "WARN", StringComparison.OrdinalIgnoreCase);
        }
    }
}
