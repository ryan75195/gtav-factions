using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using FactionWars.ScriptHookV.Logging;
using FactionWars.Telemetry.Interfaces;
using FactionWars.Telemetry.Models;

namespace FactionWars.Telemetry.Sinks
{
    /// <summary>
    /// Writes per-ped behavior samples to a per-save <c>behavior_trace.csv</c> under a base directory.
    /// <see cref="Write"/> only serializes and queues (rows buffer until <see cref="SetSaveFile"/> is
    /// called); a background writer thread drains the queue on an interval with one file append per
    /// flush. File I/O NEVER runs on the caller's thread: in-game evidence (issue #146 lag spikes)
    /// showed individual per-row <c>File.AppendAllText</c> calls stalling 0.8-2.7s on external
    /// file-system contention, freezing the game tick. <see cref="Flush"/> drains synchronously
    /// (tests, and <see cref="Dispose"/> for the shutdown tail). First-error-wins logging avoids
    /// spam on a persistently broken path.
    /// </summary>
    public sealed class CsvBehaviorTraceSink : IBehaviorTraceSink
    {
        private const int BufferCap = 20000;
        private const string FileName = "behavior_trace.csv";
        private static readonly string Header =
            "session_id,timestamp_utc,sample_ms,handle,kind,role,weapon,is_shooting,in_combat,target_handle,dist_to_target,dist_to_player,pos_x,pos_y,pos_z,in_vehicle,is_following_player,health,combat_ability,has_los,engine_phase,ms_since_los";

        private readonly object _lock = new object();
        private readonly object _ioLock = new object();
        private readonly string _baseDir;
        private readonly string _sessionId;
        private readonly int _flushIntervalMs;
        private List<string> _pending = new List<string>();
        private string? _saveDir;
        private bool _disposed;
        private bool _errored;

        // Writer thread machinery, created lazily on the first queued row so idle sinks never
        // spawn a thread. The event doubles as the stop signal (set after _stopRequested).
        private Thread? _writerThread;
        private AutoResetEvent? _wake;
        private volatile bool _stopRequested;

        /// <param name="baseDirectory">Root telemetry directory (rows land under baseDirectory/&lt;save&gt;/).</param>
        /// <param name="flushIntervalMs">How often the background writer drains the queue.</param>
        public CsvBehaviorTraceSink(string baseDirectory, int flushIntervalMs = 5000)
        {
            _baseDir = baseDirectory ?? throw new ArgumentNullException(nameof(baseDirectory));
            _flushIntervalMs = flushIntervalMs;
            _sessionId = CreateSessionId();
        }

        public void Write(BehaviorSampleRow row)
        {
            if (row == null) return;

            // Serialized on the caller's thread so timestamp_utc reflects the sample time,
            // not the (possibly seconds-later) background flush time.
            var line = Serialize(row);
            lock (_lock)
            {
                if (_disposed) return;
                if (_pending.Count >= BufferCap) _pending.RemoveAt(0);
                _pending.Add(line);
            }

            EnsureWriterStarted();
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
            }

            // Nudge the writer so pre-save buffered rows land without waiting a full interval.
            _wake?.Set();
        }

        /// <summary>
        /// Synchronously drains all queued rows to disk. Called by <see cref="Dispose"/> for the
        /// shutdown tail and by tests; gameplay code should rely on the background interval.
        /// </summary>
        public void Flush() => FlushCore();

        public void Dispose()
        {
            lock (_lock)
            {
                if (_disposed) return;
                _disposed = true;
            }

            _stopRequested = true;
            _wake?.Set();
            _writerThread?.Join(2000);
            FlushCore();
        }

        private void EnsureWriterStarted()
        {
            if (_writerThread != null) return;
            lock (_lock)
            {
                if (_writerThread != null || _disposed) return;
                _wake = new AutoResetEvent(false);
                _writerThread = new Thread(WriterLoop)
                {
                    IsBackground = true,
                    Name = "FactionWars.BehaviorTraceWriter"
                };
                _writerThread.Start();
            }
        }

        private void WriterLoop()
        {
            while (!_stopRequested)
            {
                _wake!.WaitOne(_flushIntervalMs);
                if (_stopRequested) break;
                FlushCore();
            }
        }

        // Drains the queue and appends it as ONE file write. _ioLock serializes concurrent
        // flushers (background writer vs Flush/Dispose) so batches land in queue order; the
        // queue swap under _lock is O(1), so Write callers never wait on file I/O.
        private void FlushCore()
        {
            lock (_ioLock)
            {
                List<string> toWrite;
                lock (_lock)
                {
                    if (_saveDir == null || _pending.Count == 0) return;
                    toWrite = _pending;
                    _pending = new List<string>();
                }

                AppendBatch(toWrite);
            }
        }

        private void AppendBatch(IReadOnlyCollection<string> rows)
        {
            var path = Path.Combine(_saveDir!, FileName);
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
            I(r.CombatAbility),
            B(r.HasLineOfSight),
            Esc(r.EnginePhase),
            I(r.MsSinceLos));

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
