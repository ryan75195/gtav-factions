using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace FactionWars.ScriptHookV.Logging
{
    /// <summary>
    /// File logger for the FactionWars mod. Writes timestamped entries to a session log
    /// in the user's Documents folder. Backed by a persistent buffered StreamWriter: chatty
    /// levels are flushed on a background timer (keeping disk I/O off the script thread),
    /// while WARN/ERROR flush immediately so crash-critical lines survive a script abort.
    /// </summary>
    public static partial class FileLogger
    {
        public const string LogDirectoryEnvironmentVariable = "FACTIONWARS_LOG_DIR";

        private const int FlushIntervalMs = 1000;

        private static readonly object _lock = new object();
        private static string? _logPath;
        private static bool _initialized;
        private static StreamWriter? _writer;
        private static Timer? _flushTimer;

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
        /// Flushes any buffered log lines to disk. Safe to call from any thread.
        /// </summary>
        internal static void Flush()
        {
            lock (_lock)
            {
                try { _writer?.Flush(); }
                catch { /* never let logging break the caller */ }
            }
        }

        /// <summary>
        /// Flushes and closes the log. Called on script abort so buffered lines are not lost.
        /// </summary>
        internal static void Shutdown()
        {
            lock (_lock)
            {
                try
                {
                    _flushTimer?.Dispose();
                    _flushTimer = null;
                    _writer?.Flush();
                    _writer?.Dispose();
                    _writer = null;
                }
                catch { /* never let logging break the caller */ }
            }
        }

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

                var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                _logPath = Path.Combine(logDir, $"FactionWars_{timestamp}.log");
                _writer = OpenWriter(_logPath);
                _flushTimer = new Timer(_ => Flush(), null, FlushIntervalMs, FlushIntervalMs);
                _initialized = true;

                WriteHeaderLines();
            }
        }

        private static StreamWriter OpenWriter(string path)
        {
            // FileShare.ReadWrite so the log can be tailed/read while the game holds it open.
            var stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
            return new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = false };
        }

        private static void WriteHeaderLines()
        {
            _writer?.WriteLine("========================================");
            _writer?.WriteLine($"FactionWars Log - {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            _writer?.WriteLine("========================================");
            _writer?.WriteLine(string.Empty);
            _writer?.Flush();
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
        /// Writes a formatted log entry. Buffered unless the level is crash-critical.
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
                    if (_writer == null) return;
                    _writer.WriteLine(logLine);
                    if (LogFlushPolicy.RequiresImmediateFlush(level))
                        _writer.Flush();
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
                try { _writer?.WriteLine(line); }
                catch { /* Silently ignore write failures */ }
            }
        }
    }
}
