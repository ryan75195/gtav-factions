using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            Assert.StartsWith("timestamp,play_time_seconds,faction_id,cash,", lines[0]);
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
            Assert.StartsWith("timestamp,", lines[0]);
            Assert.DoesNotContain("timestamp,", lines[1]);
            Assert.DoesNotContain("timestamp,", lines[2]);
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

            Assert.StartsWith("01:02:03,3723,", lines[1]);
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
