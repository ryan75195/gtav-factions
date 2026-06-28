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
    /// Writes per-ped behavior samples to a per-save <c>behavior_trace.csv</c> under a base directory.
    /// Buffers rows in memory until <see cref="SetSaveFile"/> is called, then flushes and switches to
    /// direct-append. Mirrors <see cref="CsvTelemetrySink"/>'s lifecycle but for the single trace file.
    /// Thread-safe via a single lock; first-error-wins logging avoids spam on a persistently broken path.
    /// </summary>
    public sealed class CsvBehaviorTraceSink : IBehaviorTraceSink
    {
        private const int BufferCap = 20000;
        private const string FileName = "behavior_trace.csv";
        private static readonly string Header =
            "session_id,timestamp_utc,sample_ms,handle,kind,role,weapon,is_shooting,in_combat,target_handle,dist_to_target,dist_to_player,pos_x,pos_y,pos_z,in_vehicle,is_following_player,health,combat_ability";

        private readonly object _lock = new object();
        private readonly string _baseDir;
        private readonly string _sessionId;
        private readonly List<BehaviorSampleRow> _buffer = new List<BehaviorSampleRow>();
        private string? _saveDir;
        private bool _disposed;
        private bool _errored;

        /// <param name="baseDirectory">Root telemetry directory (rows land under baseDirectory/&lt;save&gt;/).</param>
        public CsvBehaviorTraceSink(string baseDirectory)
        {
            _baseDir = baseDirectory ?? throw new ArgumentNullException(nameof(baseDirectory));
            _sessionId = CreateSessionId();
        }

        public void Write(BehaviorSampleRow row)
        {
            if (row == null) return;
            lock (_lock)
            {
                if (_disposed) return;
                if (_saveDir == null)
                {
                    if (_buffer.Count >= BufferCap) _buffer.RemoveAt(0);
                    _buffer.Add(row);
                    return;
                }

                AppendLocked(new[] { Serialize(row) });
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
                    FileLogger.Error($"CsvBehaviorTraceSink: failed to create {dir}", ex);
                    return;
                }

                _saveDir = dir;
                if (_buffer.Count > 0)
                {
                    var rows = new List<string>(_buffer.Count);
                    foreach (var r in _buffer) rows.Add(Serialize(r));
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
                    FileLogger.Error($"CsvBehaviorTraceSink: failed to append to {path}", ex);
                }
            }
        }

        private string Serialize(BehaviorSampleRow r) => string.Join(",",
            Esc(_sessionId),
            Utc(DateTime.UtcNow),
            I(r.SampleMs),
            I(r.Handle),
            r.Kind.ToString(),
            r.Role.ToString(),
            Esc(r.Weapon),
            B(r.IsShooting),
            B(r.InCombat),
            I(r.TargetHandle),
            F(r.DistToTarget),
            F(r.DistToPlayer),
            F(r.PosX),
            F(r.PosY),
            F(r.PosZ),
            B(r.InVehicle),
            B(r.IsFollowingPlayer),
            I(r.Health),
            I(r.CombatAbility));

        private static string Esc(string? v) => CsvFieldEscaper.Escape(v);
        private static string I(int v) => v.ToString(CultureInfo.InvariantCulture);
        private static string F(float v) => v.ToString("G", CultureInfo.InvariantCulture);
        private static string B(bool v) => v ? "true" : "false";
        private static string Utc(DateTime v) => v.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);

        private static string CreateSessionId()
            => DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffZ", CultureInfo.InvariantCulture)
                + "-"
                + Guid.NewGuid().ToString("N").Substring(0, 8);
    }
}
