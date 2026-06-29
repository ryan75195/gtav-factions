using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using FactionWars.ScriptHookV.Logging;
using FactionWars.Telemetry.Interfaces;
using FactionWars.Telemetry.Models;

namespace FactionWars.Telemetry.Sinks
{
    /// <summary>
    /// Writes squad engagement phase-change events to a per-save <c>engagement_events.csv</c>. Buffers
    /// rows until <see cref="SetSaveFile"/> is known, then flushes and appends. Mirrors
    /// <see cref="CsvBehaviorTraceSink"/>'s lifecycle and thread-safety.
    /// </summary>
    public sealed class CsvEngagementEventSink : IEngagementEventSink
    {
        private const int BufferCap = 20000;
        private const string FileName = "engagement_events.csv";
        private static readonly string Header =
            "session_id,timestamp_utc,handle,at_ms,from_phase,to_phase,reason,dist_to_target,has_los,ms_since_los";

        private readonly object _lock = new object();
        private readonly string _baseDir;
        private readonly string _sessionId;
        private readonly List<EngagementTransition> _buffer = new List<EngagementTransition>();
        private string? _saveDir;
        private bool _disposed;
        private bool _errored;

        public CsvEngagementEventSink(string baseDirectory)
        {
            _baseDir = baseDirectory ?? throw new ArgumentNullException(nameof(baseDirectory));
            _sessionId = CreateSessionId();
        }

        public void Write(EngagementTransition e)
        {
            lock (_lock)
            {
                if (_disposed) return;
                if (_saveDir == null)
                {
                    if (_buffer.Count >= BufferCap) _buffer.RemoveAt(0);
                    _buffer.Add(e);
                    return;
                }

                AppendLocked(new[] { Serialize(e) });
            }
        }

        public void SetSaveFile(string saveFilename)
        {
            if (string.IsNullOrWhiteSpace(saveFilename))
                throw new ArgumentException("saveFilename cannot be empty", nameof(saveFilename));

            lock (_lock)
            {
                if (_disposed || _saveDir != null) return;

                var dir = Path.Combine(_baseDir, saveFilename);
                try
                {
                    Directory.CreateDirectory(dir);
                }
                catch (Exception ex)
                {
                    FileLogger.Error($"CsvEngagementEventSink: failed to create {dir}", ex);
                    return;
                }

                _saveDir = dir;
                if (_buffer.Count > 0)
                {
                    var rows = new List<string>(_buffer.Count);
                    foreach (var e in _buffer) rows.Add(Serialize(e));
                    AppendLocked(rows);
                    _buffer.Clear();
                }
            }
        }

        public void Dispose()
        {
            lock (_lock)
            {
                if (_disposed) return;
                _disposed = true;
                _buffer.Clear();
            }
        }

        private void AppendLocked(IReadOnlyCollection<string> rows)
        {
            if (_saveDir == null || rows.Count == 0) return;
            var path = Path.Combine(_saveDir, FileName);
            try
            {
                var sb = new StringBuilder();
                if (!File.Exists(path)) sb.AppendLine(Header);
                foreach (var row in rows) sb.AppendLine(row);
                File.AppendAllText(path, sb.ToString());
            }
            catch (Exception ex)
            {
                if (!_errored)
                {
                    _errored = true;
                    FileLogger.Error($"CsvEngagementEventSink: failed to append to {path}", ex);
                }
            }
        }

        private string Serialize(EngagementTransition e) => string.Join(",",
            CsvFieldEscaper.Escape(_sessionId),
            DateTime.UtcNow.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
            e.Handle.ToString(CultureInfo.InvariantCulture),
            e.AtMs.ToString(CultureInfo.InvariantCulture),
            e.FromPhase.ToString(),
            e.ToPhase.ToString(),
            e.Reason.ToString(),
            e.DistToTarget.ToString("G", CultureInfo.InvariantCulture),
            e.HasLineOfSight ? "true" : "false",
            e.MsSinceLos.ToString(CultureInfo.InvariantCulture));

        private static string CreateSessionId()
            => DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffZ", CultureInfo.InvariantCulture)
                + "-"
                + Guid.NewGuid().ToString("N").Substring(0, 8);
    }
}
