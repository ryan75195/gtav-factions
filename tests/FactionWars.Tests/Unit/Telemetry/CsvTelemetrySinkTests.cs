using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FactionWars.AI.Models;
using FactionWars.Combat.Models;
using FactionWars.Core.Models;
using FactionWars.Telemetry.Models;
using FactionWars.Telemetry.Sinks;
using Xunit;

namespace FactionWars.Tests.Unit.Telemetry
{
    public class CsvTelemetrySinkTests : IDisposable
    {
        private readonly string _tempDir;

        public CsvTelemetrySinkTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "fw_tel_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
        }

        public void Dispose()
        {
            try { Directory.Delete(_tempDir, recursive: true); } catch { }
        }

        private static FactionSnapshot Snap(string id, int cash = 0) =>
            new FactionSnapshot(new DateTime(2026, 1, 1, 12, 0, 0), 100, id,
                cash, 0, 0, 0, 0, 0, 0, 0, 0);

        [Fact]
        public void WriteSnapshot_AfterSetSaveFile_WritesHeaderAndRow()
        {
            using var sink = new CsvTelemetrySink(_tempDir);
            sink.SetSaveFile("SGTA0001");
            sink.WriteSnapshot(new[] { Snap("michael", 500) });

            var path = Path.Combine(_tempDir, "SGTA0001", "snapshots.csv");
            var lines = File.ReadAllLines(path);
            Assert.Equal(2, lines.Length);
            Assert.StartsWith("session_id,timestamp_utc,hud_time,play_time_seconds,faction_id,cash,", lines[0]);
            Assert.Contains("michael", lines[1]);
            Assert.Contains("500", lines[1]);
        }

        [Fact]
        public void WriteSnapshot_BeforeSetSaveFile_BuffersAndFlushesOnSet()
        {
            using var sink = new CsvTelemetrySink(_tempDir);
            sink.WriteSnapshot(new[] { Snap("trevor", 100) });
            sink.WriteSnapshot(new[] { Snap("franklin", 200) });

            // Pre-flush: no save dir exists
            Assert.False(Directory.Exists(Path.Combine(_tempDir, "SGTA0002")));

            sink.SetSaveFile("SGTA0002");

            var path = Path.Combine(_tempDir, "SGTA0002", "snapshots.csv");
            var lines = File.ReadAllLines(path);
            Assert.Equal(3, lines.Length); // header + 2 rows
            Assert.Contains("trevor", lines[1]);
            Assert.Contains("franklin", lines[2]);
        }

        [Fact]
        public void WriteSnapshot_TwoCalls_WritesHeaderOnce()
        {
            using var sink = new CsvTelemetrySink(_tempDir);
            sink.SetSaveFile("SGTA0003");
            sink.WriteSnapshot(new[] { Snap("a") });
            sink.WriteSnapshot(new[] { Snap("b") });

            var path = Path.Combine(_tempDir, "SGTA0003", "snapshots.csv");
            var lines = File.ReadAllLines(path);
            Assert.Equal(3, lines.Length);
            Assert.StartsWith("session_id,", lines[0]);
            Assert.DoesNotContain("session_id,", lines[1]);
            Assert.DoesNotContain("session_id,", lines[2]);
        }

        [Fact]
        public void SetSaveFile_WhenPlaceholderIsCurrent_PromotesToRealSaveFolder()
        {
            using var sink = new CsvTelemetrySink(_tempDir);
            sink.SetSaveFile("Unnamed Save");
            sink.WriteSnapshot(new[] { Snap("early") });

            sink.SetSaveFile("SGTA50015");
            sink.WriteSnapshot(new[] { Snap("late") });

            Assert.False(Directory.Exists(Path.Combine(_tempDir, "Unnamed Save")));
            var path = Path.Combine(_tempDir, "SGTA50015", "snapshots.csv");
            var lines = File.ReadAllLines(path);
            Assert.Equal(3, lines.Length);
            Assert.Contains("early", lines[1]);
            Assert.Contains("late", lines[2]);
        }

        [Fact]
        public void SetSaveFile_WhenPromotingToExistingFolder_MergesRowsWithoutDuplicateHeader()
        {
            Directory.CreateDirectory(Path.Combine(_tempDir, "SGTA50015"));
            File.WriteAllLines(Path.Combine(_tempDir, "SGTA50015", "snapshots.csv"), new[]
            {
                "session_id,timestamp_utc,hud_time,play_time_seconds,faction_id,cash,total_troops,zones_owned,basic,medium,heavy,elite,reserve_troops,deployed_troops",
                "session-a,2026-01-01T00:00:01.0000000Z,00:00:01,1,existing,0,0,0,0,0,0,0,0,0"
            });

            using var sink = new CsvTelemetrySink(_tempDir);
            sink.SetSaveFile("Unnamed Save");
            sink.WriteSnapshot(new[] { Snap("early") });

            sink.SetSaveFile("SGTA50015");

            Assert.False(Directory.Exists(Path.Combine(_tempDir, "Unnamed Save")));
            var lines = File.ReadAllLines(Path.Combine(_tempDir, "SGTA50015", "snapshots.csv"));
            Assert.Equal(3, lines.Length);
            Assert.Single(lines, line => line.StartsWith("session_id,"));
            Assert.Contains(lines, line => line.Contains("existing"));
            Assert.Contains(lines, line => line.Contains("early"));
        }

        [Fact]
        public void WriteSnapshot_WhenExistingFileHasOldHeader_RotatesLegacyFile()
        {
            var saveDir = Path.Combine(_tempDir, "SGTA_legacy");
            Directory.CreateDirectory(saveDir);
            File.WriteAllLines(Path.Combine(saveDir, "snapshots.csv"), new[]
            {
                "timestamp,play_time_seconds,faction_id,cash,total_troops,zones_owned,basic,medium,heavy,elite,reserve_troops,deployed_troops",
                "00:00:01,1,old,0,0,0,0,0,0,0,0,0"
            });

            using var sink = new CsvTelemetrySink(_tempDir);
            sink.SetSaveFile("SGTA_legacy");
            sink.WriteSnapshot(new[] { Snap("new", 500) });

            var currentLines = File.ReadAllLines(Path.Combine(saveDir, "snapshots.csv"));
            var legacyFiles = Directory.GetFiles(saveDir, "snapshots.legacy-*.csv");

            Assert.Single(legacyFiles);
            Assert.StartsWith("session_id,timestamp_utc,hud_time,play_time_seconds,", currentLines[0]);
            Assert.Contains("new", currentLines[1]);
            Assert.Contains("old", File.ReadAllText(legacyFiles[0]));
        }

        [Fact]
        public void WriteZoneEvent_WritesToZoneEventsFile()
        {
            using var sink = new CsvTelemetrySink(_tempDir);
            sink.SetSaveFile("SGTA0004");
            sink.WriteZoneEvent(new ZoneEventRow(
                new DateTime(2026, 1, 1, 12, 0, 0), 200,
                ZoneEventType.Captured, "morningwood", "trevor", "michael"));

            var path = Path.Combine(_tempDir, "SGTA0004", "zone_events.csv");
            Assert.True(File.Exists(path));
            var lines = File.ReadAllLines(path);
            Assert.Equal(2, lines.Length);
            Assert.Contains("Captured", lines[1]);
            Assert.Contains("morningwood", lines[1]);
        }

        [Fact]
        public void WriteZoneEvent_UsesPlayTimeAsHudTimestamp()
        {
            using var sink = new CsvTelemetrySink(_tempDir);
            sink.SetSaveFile("SGTA_time");
            sink.WriteZoneEvent(new ZoneEventRow(
                new DateTime(2026, 1, 1, 12, 0, 0), 3723,
                ZoneEventType.Captured, "morningwood", "trevor", "michael"));

            var path = Path.Combine(_tempDir, "SGTA_time", "zone_events.csv");
            var lines = File.ReadAllLines(path);

            var fields = lines[1].Split(',');
            Assert.Equal("01:02:03", fields[2]);
            Assert.Equal("3723", fields[3]);
        }

        [Fact]
        public void WriteEventRows_ShouldWriteAllTelemetryFiles()
        {
            using var sink = new CsvTelemetrySink(_tempDir);
            var timestamp = new DateTime(2026, 1, 1, 12, 0, 0);
            sink.SetSaveFile("SGTA_events");

            sink.WriteBattle(new BattleEventRow(timestamp, 10, BattleEventType.Started,
                "zone1", "michael", "trevor", 5, 6, null, 0, 0));
            sink.WriteDecision(new DecisionEventRow(timestamp, 11, "trevor",
                AIDecisionType.Attack, "zone1", 4, 0.8, true));
            sink.WriteRecruitment(new RecruitmentEventRow(timestamp, 12, "trevor",
                troopsRecruited: 3, cost: 600, cashBefore: 1000, cashAfter: 400));
            sink.WriteAllocation(new AllocationEventRow(timestamp, 13, "michael",
                "zone1", DefenderTier.Basic, 2, AllocationSource.Player));
            sink.WriteResourceTick(new ResourceTickEventRow(timestamp, 14, "michael",
                income: 250, zonesContributing: 2));
            sink.WriteMatchMeta(new MatchMetaEventRow(timestamp, 15,
                MatchMetaEventType.ModSessionStart, "start"));
            sink.WritePlayerEvent(new PlayerEventRow(timestamp, 16, PlayerEventType.Death,
                "zone1", "trevor", DefenderTier.Basic, "death"));

            var saveDir = Path.Combine(_tempDir, "SGTA_events");
            Assert.True(File.Exists(Path.Combine(saveDir, "battles.csv")));
            Assert.True(File.Exists(Path.Combine(saveDir, "decisions.csv")));
            Assert.True(File.Exists(Path.Combine(saveDir, "recruitments.csv")));
            Assert.True(File.Exists(Path.Combine(saveDir, "allocations.csv")));
            Assert.True(File.Exists(Path.Combine(saveDir, "resource_ticks.csv")));
            Assert.True(File.Exists(Path.Combine(saveDir, "match_meta.csv")));
            Assert.True(File.Exists(Path.Combine(saveDir, "player_events.csv")));
        }

        [Fact]
        public void Dispose_IsIdempotent()
        {
            var sink = new CsvTelemetrySink(_tempDir);
            sink.Dispose();
            sink.Dispose(); // must not throw
        }

        [Fact]
        public void WriteAfterDispose_DoesNotThrow()
        {
            var sink = new CsvTelemetrySink(_tempDir);
            sink.Dispose();
            sink.WriteSnapshot(new[] { Snap("a") }); // must not throw
        }

        [Fact]
        public void WriteSnapshot_BufferOverflow_DropsOldestRows()
        {
            using var sink = new CsvTelemetrySink(_tempDir);

            // Write 10001 single-row snapshots before SetSaveFile.
            // Each row uniquely identified by faction id "f0", "f1", ..., "f10000".
            for (int i = 0; i <= 10000; i++)
            {
                sink.WriteSnapshot(new[] { Snap("f" + i) });
            }

            sink.SetSaveFile("SGTA_overflow");

            var path = Path.Combine(_tempDir, "SGTA_overflow", "snapshots.csv");
            var lines = File.ReadAllLines(path);

            // Header + 10000 rows (oldest "f0" dropped, newest "f10000" kept).
            Assert.Equal(10001, lines.Length);
            Assert.DoesNotContain(lines.Skip(1), l => l.Contains(",f0,"));
            Assert.Contains(lines.Skip(1), l => l.Contains(",f1,"));
            Assert.Contains(lines.Skip(1), l => l.Contains(",f10000,"));
        }
    }
}
