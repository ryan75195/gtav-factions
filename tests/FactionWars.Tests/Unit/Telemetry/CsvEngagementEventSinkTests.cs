using System.IO;
using FactionWars.Combat.Models;
using FactionWars.Telemetry.Models;
using FactionWars.Telemetry.Sinks;
using Xunit;

namespace FactionWars.Tests.Unit.Telemetry
{
    public class CsvEngagementEventSinkTests
    {
        private static EngagementTransition Sample(int handle) => new EngagementTransition(
            handle, 1234, EngagePhase.Engage, EngagePhase.Advance,
            EngagePhaseChangeReason.LosReposition, 14.5f, false, 1800);

        [Fact]
        public void Write_AfterSetSaveFile_WritesHeaderAndRow()
        {
            var baseDir = Path.Combine(Path.GetTempPath(), "fw_ee_" + Path.GetRandomFileName());
            try
            {
                using var sink = new CsvEngagementEventSink(baseDir);
                sink.SetSaveFile("SGTA0001");
                sink.Write(Sample(42));

                var path = Path.Combine(baseDir, "SGTA0001", "engagement_events.csv");
                var lines = File.ReadAllLines(path);
                Assert.Equal("session_id,timestamp_utc,handle,at_ms,from_phase,to_phase,reason,dist_to_target,has_los,ms_since_los", lines[0]);
                Assert.Contains(",42,1234,Engage,Advance,LosReposition,", lines[1]);
                Assert.Contains(",false,1800", lines[1]);
            }
            finally
            {
                if (Directory.Exists(baseDir)) Directory.Delete(baseDir, true);
            }
        }

        [Fact]
        public void Write_BeforeSetSaveFile_BuffersAndFlushesOnSet()
        {
            var baseDir = Path.Combine(Path.GetTempPath(), "fw_ee_" + Path.GetRandomFileName());
            try
            {
                using var sink = new CsvEngagementEventSink(baseDir);
                sink.Write(Sample(7));
                sink.SetSaveFile("SGTA0002");

                var path = Path.Combine(baseDir, "SGTA0002", "engagement_events.csv");
                Assert.True(File.Exists(path));
                var lines = File.ReadAllLines(path);
                Assert.Equal(2, lines.Length); // header + buffered row
            }
            finally
            {
                if (Directory.Exists(baseDir)) Directory.Delete(baseDir, true);
            }
        }
    }
}
