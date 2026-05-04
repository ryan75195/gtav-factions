using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using FactionWars.ScriptHookV.Logging;
using FactionWars.Telemetry.Interfaces;
using FactionWars.Telemetry.Models;

namespace FactionWars.Telemetry.Sinks
{
    /// <summary>
    /// Writes telemetry rows to per-save CSV files under a base directory.
    /// Buffers events in memory until SetSaveFile is called, then flushes and
    /// switches to direct-append mode. Thread-safe via a single lock.
    /// </summary>
    public sealed partial class CsvTelemetrySink : ITelemetrySink
    {
        private const int BufferCapPerType = 10000;
        private const string PlaceholderSaveName = "Unnamed Save";
        private static readonly string SnapshotHeader =
            "timestamp,play_time_seconds,faction_id,cash,total_troops,zones_owned,basic,medium,heavy,elite,reserve_troops,deployed_troops";
        private static readonly string ZoneEventHeader =
            "timestamp,play_time_seconds,event_type,zone_id,previous_owner,new_owner";
        private static readonly string BattleHeader =
            "timestamp,play_time_seconds,event_type,zone_id,attacker_faction,defender_faction,attacker_troops,defender_troops,outcome,attacker_casualties,defender_casualties";
        private static readonly string DecisionHeader =
            "timestamp,play_time_seconds,faction_id,decision_type,target_zone,troops,priority,executed";
        private static readonly string RecruitmentHeader =
            "timestamp,play_time_seconds,faction_id,troops_recruited,cost,cash_before,cash_after";
        private static readonly string AllocationHeader =
            "timestamp,play_time_seconds,faction_id,zone_id,tier,count,source";
        private static readonly string ResourceTickHeader =
            "timestamp,play_time_seconds,faction_id,income,zones_contributing";
        private static readonly string MatchMetaHeader =
            "timestamp,play_time_seconds,event_type,details";
        private static readonly string PlayerEventHeader =
            "timestamp,play_time_seconds,event_type,zone_id,target_faction,target_tier,details";

        private readonly object _lock = new object();
        private readonly string _baseDir;
        private string? _saveDir;
        private bool _disposed;
        // First-error-wins per file: prevents log spam when a path is persistently
        // broken (disk full, permissions). Never reset — bounded by the number of
        // distinct file paths the sink writes (9 currently).
        private readonly HashSet<string> _erroredFiles = new HashSet<string>();

        // Buffers (used until _saveDir is set)
        private readonly List<FactionSnapshot> _bufSnap = new List<FactionSnapshot>();
        private readonly List<ZoneEventRow> _bufZone = new List<ZoneEventRow>();
        private readonly List<BattleEventRow> _bufBattle = new List<BattleEventRow>();
        private readonly List<DecisionEventRow> _bufDecision = new List<DecisionEventRow>();
        private readonly List<RecruitmentEventRow> _bufRecruit = new List<RecruitmentEventRow>();
        private readonly List<AllocationEventRow> _bufAlloc = new List<AllocationEventRow>();
        private readonly List<ResourceTickEventRow> _bufTick = new List<ResourceTickEventRow>();
        private readonly List<MatchMetaEventRow> _bufMeta = new List<MatchMetaEventRow>();
        private readonly List<PlayerEventRow> _bufPlayer = new List<PlayerEventRow>();

        /// <param name="baseDirectory">Root telemetry directory (e.g., Documents\FactionWars\Telemetry).</param>
        public CsvTelemetrySink(string baseDirectory)
        {
            _baseDir = baseDirectory ?? throw new ArgumentNullException(nameof(baseDirectory));
        }

        public void SetSaveFile(string saveFilename)
        {
            if (string.IsNullOrWhiteSpace(saveFilename))
                throw new ArgumentException("saveFilename cannot be empty", nameof(saveFilename));

            lock (_lock)
            {
                if (_disposed) return;
                if (_saveDir != null)
                {
                    TryPromotePlaceholderSaveLocked(saveFilename);
                    return;
                }

                _saveDir = Path.Combine(_baseDir, saveFilename);
                try
                {
                    Directory.CreateDirectory(_saveDir);
                }
                catch (Exception ex)
                {
                    FileLogger.Error($"CsvTelemetrySink: failed to create {_saveDir}", ex);
                    _saveDir = null;
                    return;
                }

                FlushBuffersLocked();
            }
        }

        private void TryPromotePlaceholderSaveLocked(string saveFilename)
        {
            if (_saveDir == null) return;
            if (IsPlaceholderSaveName(saveFilename)) return;

            var currentName = Path.GetFileName(_saveDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            if (!IsPlaceholderSaveName(currentName)) return;

            var targetDir = Path.Combine(_baseDir, saveFilename);
            if (string.Equals(_saveDir, targetDir, StringComparison.OrdinalIgnoreCase)) return;

            try
            {
                if (!Directory.Exists(_saveDir))
                {
                    Directory.CreateDirectory(targetDir);
                    _saveDir = targetDir;
                    return;
                }

                if (!Directory.Exists(targetDir))
                {
                    Directory.Move(_saveDir, targetDir);
                    _saveDir = targetDir;
                    return;
                }

                MergeDirectoryIntoTarget(_saveDir, targetDir);
                Directory.Delete(_saveDir, recursive: true);
                _saveDir = targetDir;
            }
            catch (Exception ex)
            {
                FileLogger.Error($"CsvTelemetrySink: failed to promote telemetry folder '{_saveDir}' to '{targetDir}'", ex);
            }
        }

        private static bool IsPlaceholderSaveName(string? saveName)
            => string.Equals(saveName, PlaceholderSaveName, StringComparison.OrdinalIgnoreCase);

        private static void MergeDirectoryIntoTarget(string sourceDir, string targetDir)
        {
            foreach (var sourceFile in Directory.GetFiles(sourceDir))
            {
                var targetFile = Path.Combine(targetDir, Path.GetFileName(sourceFile));
                if (!File.Exists(targetFile))
                {
                    File.Move(sourceFile, targetFile);
                    continue;
                }

                var sourceLines = File.ReadAllLines(sourceFile);
                if (sourceLines.Length <= 1) continue;

                var linesToAppend = sourceLines.Skip(1).Where(line => !string.IsNullOrWhiteSpace(line));
                File.AppendAllLines(targetFile, linesToAppend);
            }
        }

        public void WriteSnapshot(IReadOnlyList<FactionSnapshot> rows)
        {
            if (rows == null || rows.Count == 0) return;
            lock (_lock)
            {
                if (_disposed) return;
                if (_saveDir == null) { BufferLocked(_bufSnap, rows); return; }
                AppendLocked("snapshots.csv", SnapshotHeader, rows.Select(SerializeSnapshot));
            }
        }

        public void WriteZoneEvent(ZoneEventRow row)
        {
            if (row == null) return;
            lock (_lock)
            {
                if (_disposed) return;
                if (_saveDir == null) { BufferLocked(_bufZone, new[] { row }); return; }
                AppendLocked("zone_events.csv", ZoneEventHeader, new[] { SerializeZoneEvent(row) });
            }
        }

        public void WriteBattle(BattleEventRow row)
        {
            if (row == null) return;
            lock (_lock)
            {
                if (_disposed) return;
                if (_saveDir == null) { BufferLocked(_bufBattle, new[] { row }); return; }
                AppendLocked("battles.csv", BattleHeader, new[] { SerializeBattle(row) });
            }
        }

        public void WriteDecision(DecisionEventRow row)
        {
            if (row == null) return;
            lock (_lock)
            {
                if (_disposed) return;
                if (_saveDir == null) { BufferLocked(_bufDecision, new[] { row }); return; }
                AppendLocked("decisions.csv", DecisionHeader, new[] { SerializeDecision(row) });
            }
        }

        public void WriteRecruitment(RecruitmentEventRow row)
        {
            if (row == null) return;
            lock (_lock)
            {
                if (_disposed) return;
                if (_saveDir == null) { BufferLocked(_bufRecruit, new[] { row }); return; }
                AppendLocked("recruitments.csv", RecruitmentHeader, new[] { SerializeRecruitment(row) });
            }
        }

        public void WriteAllocation(AllocationEventRow row)
        {
            if (row == null) return;
            lock (_lock)
            {
                if (_disposed) return;
                if (_saveDir == null) { BufferLocked(_bufAlloc, new[] { row }); return; }
                AppendLocked("allocations.csv", AllocationHeader, new[] { SerializeAllocation(row) });
            }
        }

        public void WriteResourceTick(ResourceTickEventRow row)
        {
            if (row == null) return;
            lock (_lock)
            {
                if (_disposed) return;
                if (_saveDir == null) { BufferLocked(_bufTick, new[] { row }); return; }
                AppendLocked("resource_ticks.csv", ResourceTickHeader, new[] { SerializeResourceTick(row) });
            }
        }

        public void WriteMatchMeta(MatchMetaEventRow row)
        {
            if (row == null) return;
            lock (_lock)
            {
                if (_disposed) return;
                if (_saveDir == null) { BufferLocked(_bufMeta, new[] { row }); return; }
                AppendLocked("match_meta.csv", MatchMetaHeader, new[] { SerializeMatchMeta(row) });
            }
        }

        public void WritePlayerEvent(PlayerEventRow row)
        {
            if (row == null) return;
            lock (_lock)
            {
                if (_disposed) return;
                if (_saveDir == null) { BufferLocked(_bufPlayer, new[] { row }); return; }
                AppendLocked("player_events.csv", PlayerEventHeader, new[] { SerializePlayerEvent(row) });
            }
        }

    }
}
