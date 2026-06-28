using System;

namespace FactionWars.ScriptHookV.Logging
{
    public static partial class FileLogger
    {
        /// <summary>Writes an info-level log message.</summary>
        public static void Info(string message) => Write("INFO", message);

        /// <summary>Writes a debug-level log message.</summary>
        public static void Debug(string message) => Write("DEBUG", message);

        /// <summary>Writes a warning-level log message (flushed immediately).</summary>
        public static void Warn(string message) => Write("WARN", message);

        /// <summary>Writes an error-level log message (flushed immediately).</summary>
        public static void Error(string message) => Write("ERROR", message);

        /// <summary>Writes an error with exception details (flushed immediately).</summary>
        public static void Error(string message, Exception ex)
        {
            Write("ERROR", $"{message}: {ex.GetType().Name}: {ex.Message}");
            Write("ERROR", $"  StackTrace: {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                Write("ERROR", $"  InnerException: {ex.InnerException.Message}");
            }
        }

        /// <summary>Writes a combat-related log message.</summary>
        public static void Combat(string message) => Write("COMBAT", message);

        /// <summary>Writes a zone-related log message.</summary>
        public static void Zone(string message) => Write("ZONE", message);

        /// <summary>Writes a spawn-related log message.</summary>
        public static void Spawn(string message) => Write("SPAWN", message);

        /// <summary>Writes an AI-related log message.</summary>
        public static void AI(string message) => Write("AI", message);

        /// <summary>Writes a separator line for visual organization.</summary>
        public static void Separator(string title = "")
        {
            if (string.IsNullOrEmpty(title))
            {
                WriteRaw("----------------------------------------");
            }
            else
            {
                WriteRaw($"--- {title} ---");
            }
        }
    }
}
