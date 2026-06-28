using System;
using System.IO;
using System.Linq;
using FactionWars.Core.Models;
using FactionWars.Telemetry.Models;
using FactionWars.Telemetry.Sinks;
using Xunit;

namespace FactionWars.Tests.Unit.Telemetry
{
    public class CsvBehaviorTraceSinkTests : IDisposable
    {
        private readonly string _tempDir;

        public CsvBehaviorTraceSinkTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "fw_beh_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
        }

        public void Dispose()
        {
            try { Directory.Delete(_tempDir, recursive: true); } catch { }
        }

        private static BehaviorSampleRow Row(int handle, CombatantKind kind = CombatantKind.Follower) =>
            new BehaviorSampleRow
            {
                SampleMs = 12345,
                Handle = handle,
                Kind = kind,
                Role = DefenderRole.Sniper,
                Weapon = "WEAPON_SNIPERRIFLE",
                IsShooting = true,
                InCombat = true,
                TargetHandle = 77,
                DistToTarget = 12.5f,
                DistToPlayer = 3.25f,
                PosX = 1.5f,
                PosY = 2.5f,
                PosZ = 3.5f,
                InVehicle = false,
                IsFollowingPlayer = true,
                Health = 200,
                CombatAbility = 2
            };

        [Fact]
        public void Write_AfterSetSaveFile_WritesHeaderAndRow()
        {
            using var sink = new CsvBehaviorTraceSink(_tempDir);
            sink.SetSaveFile("SGTA0001");
            sink.Write(Row(42));

            var path = Path.Combine(_tempDir, "SGTA0001", "behavior_trace.csv");
            var lines = File.ReadAllLines(path);
            Assert.Equal(2, lines.Length);
            Assert.StartsWith("session_id,timestamp_utc,sample_ms,handle,kind,role,weapon,is_shooting,in_combat,target_handle,dist_to_target,dist_to_player,pos_x,pos_y,pos_z,in_vehicle,is_following_player,health,combat_ability", lines[0]);
            Assert.Contains("12345", lines[1]);
            Assert.Contains("42", lines[1]);
            Assert.Contains("Follower", lines[1]);
            Assert.Contains("Sniper", lines[1]);
            Assert.Contains("WEAPON_SNIPERRIFLE", lines[1]);
            Assert.Contains("true", lines[1]);
        }

        [Fact]
        public void Write_BeforeSetSaveFile_BuffersAndFlushesOnSet()
        {
            using var sink = new CsvBehaviorTraceSink(_tempDir);
            sink.Write(Row(1));
            sink.Write(Row(2));

            Assert.False(Directory.Exists(Path.Combine(_tempDir, "SGTA0002")));

            sink.SetSaveFile("SGTA0002");

            var path = Path.Combine(_tempDir, "SGTA0002", "behavior_trace.csv");
            var lines = File.ReadAllLines(path);
            Assert.Equal(3, lines.Length); // header + 2 rows
        }

        [Fact]
        public void Write_TwoRows_WritesHeaderOnce()
        {
            using var sink = new CsvBehaviorTraceSink(_tempDir);
            sink.SetSaveFile("SGTA0003");
            sink.Write(Row(1));
            sink.Write(Row(2));

            var path = Path.Combine(_tempDir, "SGTA0003", "behavior_trace.csv");
            var lines = File.ReadAllLines(path);
            Assert.Equal(3, lines.Length);
            Assert.Single(lines.Where(l => l.StartsWith("session_id,", StringComparison.Ordinal)));
        }

        [Fact]
        public void Write_NullRow_Ignored()
        {
            using var sink = new CsvBehaviorTraceSink(_tempDir);
            sink.SetSaveFile("SGTA0004");
            sink.Write(null!);

            var path = Path.Combine(_tempDir, "SGTA0004", "behavior_trace.csv");
            Assert.False(File.Exists(path));
        }

        [Fact]
        public void Write_AfterDispose_DoesNotThrowOrWrite()
        {
            var sink = new CsvBehaviorTraceSink(_tempDir);
            sink.SetSaveFile("SGTA0005");
            sink.Dispose();
            sink.Write(Row(9));

            var path = Path.Combine(_tempDir, "SGTA0005", "behavior_trace.csv");
            Assert.False(File.Exists(path));
        }

        [Fact]
        public void DistanceFields_UseInvariantCulture()
        {
            using var sink = new CsvBehaviorTraceSink(_tempDir);
            sink.SetSaveFile("SGTA0006");
            sink.Write(Row(5));

            var path = Path.Combine(_tempDir, "SGTA0006", "behavior_trace.csv");
            var line = File.ReadAllLines(path)[1];
            Assert.Contains("12.5", line);   // dist_to_target — period, never comma
            Assert.Contains("3.25", line);   // dist_to_player
        }
    }
}
