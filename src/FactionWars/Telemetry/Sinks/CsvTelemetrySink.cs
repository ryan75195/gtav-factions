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
    public sealed class CsvTelemetrySink : ITelemetrySink
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

        public void Dispose()
        {
            lock (_lock)
            {
                if (_disposed) return;
                _disposed = true;
                _bufSnap.Clear(); _bufZone.Clear(); _bufBattle.Clear();
                _bufDecision.Clear(); _bufRecruit.Clear(); _bufAlloc.Clear();
                _bufTick.Clear(); _bufMeta.Clear(); _bufPlayer.Clear();
            }
        }

        private static void BufferLocked<T>(List<T> buffer, IEnumerable<T> rows)
        {
            foreach (var r in rows)
            {
                if (buffer.Count >= BufferCapPerType) buffer.RemoveAt(0);
                buffer.Add(r);
            }
        }

        private void FlushBuffersLocked()
        {
            if (_bufSnap.Count > 0)
            {
                AppendLocked("snapshots.csv", SnapshotHeader, _bufSnap.Select(SerializeSnapshot));
                _bufSnap.Clear();
            }
            if (_bufZone.Count > 0)
            {
                AppendLocked("zone_events.csv", ZoneEventHeader, _bufZone.Select(SerializeZoneEvent));
                _bufZone.Clear();
            }
            if (_bufBattle.Count > 0)
            {
                AppendLocked("battles.csv", BattleHeader, _bufBattle.Select(SerializeBattle));
                _bufBattle.Clear();
            }
            if (_bufDecision.Count > 0)
            {
                AppendLocked("decisions.csv", DecisionHeader, _bufDecision.Select(SerializeDecision));
                _bufDecision.Clear();
            }
            if (_bufRecruit.Count > 0)
            {
                AppendLocked("recruitments.csv", RecruitmentHeader, _bufRecruit.Select(SerializeRecruitment));
                _bufRecruit.Clear();
            }
            if (_bufAlloc.Count > 0)
            {
                AppendLocked("allocations.csv", AllocationHeader, _bufAlloc.Select(SerializeAllocation));
                _bufAlloc.Clear();
            }
            if (_bufTick.Count > 0)
            {
                AppendLocked("resource_ticks.csv", ResourceTickHeader, _bufTick.Select(SerializeResourceTick));
                _bufTick.Clear();
            }
            if (_bufMeta.Count > 0)
            {
                AppendLocked("match_meta.csv", MatchMetaHeader, _bufMeta.Select(SerializeMatchMeta));
                _bufMeta.Clear();
            }
            if (_bufPlayer.Count > 0)
            {
                AppendLocked("player_events.csv", PlayerEventHeader, _bufPlayer.Select(SerializePlayerEvent));
                _bufPlayer.Clear();
            }
        }

        private void AppendLocked(string fileName, string header, IEnumerable<string> rows)
        {
            if (_saveDir == null) return;
            var path = Path.Combine(_saveDir, fileName);
            try
            {
                bool needsHeader = !File.Exists(path);
                var sb = new StringBuilder();
                if (needsHeader) sb.AppendLine(header);
                foreach (var row in rows) sb.AppendLine(row);
                File.AppendAllText(path, sb.ToString());
            }
            catch (Exception ex)
            {
                if (_erroredFiles.Add(path))
                    FileLogger.Error($"CsvTelemetrySink: failed to append to {path}", ex);
            }
        }

        private static string HudTime(long playTimeSeconds)
        {
            if (playTimeSeconds < 0) playTimeSeconds = 0;
            var hours = playTimeSeconds / 3600;
            var minutes = (playTimeSeconds % 3600) / 60;
            var seconds = playTimeSeconds % 60;
            return string.Format(CultureInfo.InvariantCulture, "{0:D2}:{1:D2}:{2:D2}", hours, minutes, seconds);
        }
        private static string Esc(string? v) => CsvFieldEscaper.Escape(v);
        private static string I(int v) => v.ToString(CultureInfo.InvariantCulture);
        private static string L(long v) => v.ToString(CultureInfo.InvariantCulture);
        private static string D(double v) => v.ToString("G", CultureInfo.InvariantCulture);

        private static string SerializeSnapshot(FactionSnapshot r) => string.Join(",",
            HudTime(r.PlayTimeSeconds), L(r.PlayTimeSeconds), Esc(r.FactionId),
            I(r.Cash), I(r.TotalTroops), I(r.ZonesOwned),
            I(r.Basic), I(r.Medium), I(r.Heavy), I(r.Elite),
            I(r.ReserveTroops), I(r.DeployedTroops));

        private static string SerializeZoneEvent(ZoneEventRow r) => string.Join(",",
            HudTime(r.PlayTimeSeconds), L(r.PlayTimeSeconds),
            r.Type.ToString(), Esc(r.ZoneId),
            Esc(r.PreviousOwner), Esc(r.NewOwner));

        private static string SerializeBattle(BattleEventRow r) => string.Join(",",
            HudTime(r.PlayTimeSeconds), L(r.PlayTimeSeconds),
            r.Type.ToString(), Esc(r.ZoneId),
            Esc(r.AttackerFactionId), Esc(r.DefenderFactionId),
            I(r.AttackerTroops), I(r.DefenderTroops),
            r.Outcome.HasValue ? r.Outcome.Value.ToString() : string.Empty,
            I(r.AttackerCasualties), I(r.DefenderCasualties));

        private static string SerializeDecision(DecisionEventRow r) => string.Join(",",
            HudTime(r.PlayTimeSeconds), L(r.PlayTimeSeconds),
            Esc(r.FactionId), r.Type.ToString(),
            Esc(r.TargetZoneId), I(r.Troops), D(r.Priority),
            r.Executed ? "true" : "false");

        private static string SerializeRecruitment(RecruitmentEventRow r) => string.Join(",",
            HudTime(r.PlayTimeSeconds), L(r.PlayTimeSeconds), Esc(r.FactionId),
            I(r.TroopsRecruited), I(r.Cost), I(r.CashBefore), I(r.CashAfter));

        private static string SerializeAllocation(AllocationEventRow r) => string.Join(",",
            HudTime(r.PlayTimeSeconds), L(r.PlayTimeSeconds),
            Esc(r.FactionId), Esc(r.ZoneId),
            r.Tier.ToString(), I(r.Count), r.Source.ToString());

        private static string SerializeResourceTick(ResourceTickEventRow r) => string.Join(",",
            HudTime(r.PlayTimeSeconds), L(r.PlayTimeSeconds), Esc(r.FactionId),
            I(r.Income), I(r.ZonesContributing));

        private static string SerializeMatchMeta(MatchMetaEventRow r) => string.Join(",",
            HudTime(r.PlayTimeSeconds), L(r.PlayTimeSeconds),
            r.Type.ToString(), Esc(r.Details));

        private static string SerializePlayerEvent(PlayerEventRow r) => string.Join(",",
            HudTime(r.PlayTimeSeconds), L(r.PlayTimeSeconds), r.Type.ToString(),
            Esc(r.ZoneId), Esc(r.TargetFaction),
            r.TargetTier.HasValue ? r.TargetTier.Value.ToString() : string.Empty,
            Esc(r.Details));
    }
}
