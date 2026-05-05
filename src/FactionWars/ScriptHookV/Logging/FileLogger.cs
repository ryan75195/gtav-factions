using System;
using System.Diagnostics;
using System.IO;

namespace FactionWars.ScriptHookV.Logging
{
    /// <summary>
    /// Simple file logger for debugging FactionWars mod.
    /// Writes timestamped log entries to a file in the user's Documents folder.
    /// </summary>
    public static class FileLogger
    {
        public const string LogDirectoryEnvironmentVariable = "FACTIONWARS_LOG_DIR";

        private static readonly object _lock = new object();
        private static string? _logPath;
        private static bool _initialized;

        /// <summary>
        /// Gets the path to the log file.
        /// </summary>
        public static string LogPath
        {
            get
            {
                EnsureInitialized();
                return _logPath!;
            }
        }

        /// <summary>
        /// Initializes the logger if not already initialized.
        /// </summary>
        private static void EnsureInitialized()
        {
            if (_initialized) return;

            lock (_lock)
            {
                if (_initialized) return;

                var logDir = ResolveLogDirectory();

                if (!Directory.Exists(logDir))
                {
                    Directory.CreateDirectory(logDir);
                }

                // Create log file with timestamp
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                _logPath = Path.Combine(logDir, $"FactionWars_{timestamp}.log");

                _initialized = true;

                // Write header
                WriteRaw("========================================");
                WriteRaw($"FactionWars Log - {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                WriteRaw("========================================");
                WriteRaw("");
            }
        }

        private static string ResolveLogDirectory()
        {
            var configuredLogDir = Environment.GetEnvironmentVariable(LogDirectoryEnvironmentVariable);
            if (!string.IsNullOrWhiteSpace(configuredLogDir))
            {
                return configuredLogDir;
            }

            if (IsRunningUnderTest())
            {
                return Path.Combine(Path.GetTempPath(), "FactionWars", "TestLogs");
            }

            var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            return Path.Combine(documentsPath, "FactionWars", "Logs");
        }

        private static bool IsRunningUnderTest()
        {
            var processName = Process.GetCurrentProcess().ProcessName;
            var appDomainName = AppDomain.CurrentDomain.FriendlyName;
            return ContainsTestHostSignal(processName) || ContainsTestHostSignal(appDomainName);
        }

        private static bool ContainsTestHostSignal(string? value)
        {
            return value != null
                && (value.IndexOf("testhost", StringComparison.OrdinalIgnoreCase) >= 0
                    || value.IndexOf("vstest", StringComparison.OrdinalIgnoreCase) >= 0);
        }

        /// <summary>
        /// Writes an info-level log message.
        /// </summary>
        public static void Info(string message)
        {
            Write("INFO", message);
        }

        /// <summary>
        /// Writes a debug-level log message.
        /// </summary>
        public static void Debug(string message)
        {
            Write("DEBUG", message);
        }

        /// <summary>
        /// Writes a warning-level log message.
        /// </summary>
        public static void Warn(string message)
        {
            Write("WARN", message);
        }

        /// <summary>
        /// Writes an error-level log message.
        /// </summary>
        public static void Error(string message)
        {
            Write("ERROR", message);
        }

        /// <summary>
        /// Writes an error with exception details.
        /// </summary>
        public static void Error(string message, Exception ex)
        {
            Write("ERROR", $"{message}: {ex.GetType().Name}: {ex.Message}");
            Write("ERROR", $"  StackTrace: {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                Write("ERROR", $"  InnerException: {ex.InnerException.Message}");
            }
        }

        /// <summary>
        /// Writes a combat-related log message.
        /// </summary>
        public static void Combat(string message)
        {
            Write("COMBAT", message);
        }

        /// <summary>
        /// Writes a zone-related log message.
        /// </summary>
        public static void Zone(string message)
        {
            Write("ZONE", message);
        }

        /// <summary>
        /// Writes a spawn-related log message.
        /// </summary>
        public static void Spawn(string message)
        {
            Write("SPAWN", message);
        }

        /// <summary>
        /// Writes an AI-related log message.
        /// </summary>
        public static void AI(string message)
        {
            Write("AI", message);
        }

        /// <summary>
        /// Writes a formatted log entry with timestamp and level.
        /// </summary>
        private static void Write(string level, string message)
        {
            EnsureInitialized();

            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            var logLine = $"[{timestamp}] [{level,-6}] {message}";

            lock (_lock)
            {
                try
                {
                    File.AppendAllText(_logPath!, logLine + Environment.NewLine);
                }
                catch
                {
                    // Silently ignore write failures
                }
            }
        }

        /// <summary>
        /// Writes a raw line without formatting.
        /// </summary>
        private static void WriteRaw(string line)
        {
            EnsureInitialized();

            lock (_lock)
            {
                try
                {
                    File.AppendAllText(_logPath!, line + Environment.NewLine);
                }
                catch
                {
                    // Silently ignore write failures
                }
            }
        }

        /// <summary>
        /// Writes a separator line for visual organization.
        /// </summary>
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
