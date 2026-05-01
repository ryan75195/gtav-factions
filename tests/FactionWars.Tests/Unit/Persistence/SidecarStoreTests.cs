using FactionWars.Persistence;
using FactionWars.Persistence.Models;
using System;
using System.IO;
using System.Linq;
using Xunit;

namespace FactionWars.Tests.Unit.Persistence
{
    public class SidecarStoreTests : IDisposable
    {
        private readonly string _tempDir;

        public SidecarStoreTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "fw_sidecar_test_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
        }

        public void Dispose()
        {
            try { Directory.Delete(_tempDir, recursive: true); } catch { }
        }

        private static Sidecar Make(long playTime = 12340, int money = 50000, int missions = 23, int clock = 854)
            => new Sidecar
            {
                Fingerprint = new SaveFingerprint
                {
                    TotalPlayTimeSeconds = playTime,
                    Money = money,
                    CompletedMissionCount = missions,
                    InGameClockMinutes = clock,
                },
                NativeSaveFilename = "SGTA00003",
                WrittenAtUtc = new DateTime(2026, 5, 1, 12, 0, 0, DateTimeKind.Utc),
                GameState = new GameState { SaveName = "test" },
            };

        [Fact]
        public void WriteSidecar_Successful_ReturnsTrue()
        {
            var store = new SidecarStore(_tempDir);

            Assert.True(store.WriteSidecar(Make()));
        }

        [Fact]
        public void WriteSidecar_ThenTryFind_RoundTrips()
        {
            var store = new SidecarStore(_tempDir);
            var original = Make();
            store.WriteSidecar(original);

            Assert.True(store.TryFindByFingerprint(original.Fingerprint, out var loaded));
            Assert.Equal(12340L, loaded.Fingerprint.TotalPlayTimeSeconds);
            Assert.Equal(50000, loaded.Fingerprint.Money);
            Assert.Equal("test", loaded.GameState.SaveName);
        }

        [Fact]
        public void TryFindByFingerprint_NoSuchSidecar_ReturnsFalse()
        {
            var store = new SidecarStore(_tempDir);
            var fp = new SaveFingerprint { TotalPlayTimeSeconds = 999999 };

            Assert.False(store.TryFindByFingerprint(fp, out _));
        }

        [Fact]
        public void TryFindByFingerprint_PrimaryMatchButTiebreakerMismatch_ReturnsFalse()
        {
            var store = new SidecarStore(_tempDir);
            store.WriteSidecar(Make(playTime: 12340, money: 50000));

            var differentFp = new SaveFingerprint
            {
                TotalPlayTimeSeconds = 12340,
                Money = 99999,
                CompletedMissionCount = 23,
                InGameClockMinutes = 854,
            };

            Assert.False(store.TryFindByFingerprint(differentFp, out _));
        }

        [Fact]
        public void TryFindByFingerprint_CorruptJson_ReturnsFalseAndDoesNotThrow()
        {
            var store = new SidecarStore(_tempDir);
            var corruptPath = Path.Combine(_tempDir, "sidecar_12340.json");
            File.WriteAllText(corruptPath, "{ this is not valid json");

            var fp = new SaveFingerprint { TotalPlayTimeSeconds = 12340 };
            Assert.False(store.TryFindByFingerprint(fp, out _));
            Assert.True(File.Exists(corruptPath));
        }

        [Fact]
        public void WriteSidecar_OverwritesExistingFile()
        {
            var store = new SidecarStore(_tempDir);
            store.WriteSidecar(Make(money: 50000));
            store.WriteSidecar(Make(money: 70000));

            Assert.True(store.TryFindByFingerprint(new SaveFingerprint { TotalPlayTimeSeconds = 12340, Money = 70000, CompletedMissionCount = 23, InGameClockMinutes = 854 }, out var loaded));
            Assert.Equal(70000, loaded.Fingerprint.Money);
        }

        [Fact]
        public void ListAll_ReturnsAllSidecarsOnDisk()
        {
            var store = new SidecarStore(_tempDir);
            store.WriteSidecar(Make(playTime: 100));
            store.WriteSidecar(Make(playTime: 200));
            store.WriteSidecar(Make(playTime: 300));

            var all = store.ListAll();
            Assert.Equal(3, all.Count);
            Assert.Contains(all, s => s.Fingerprint.TotalPlayTimeSeconds == 100);
            Assert.Contains(all, s => s.Fingerprint.TotalPlayTimeSeconds == 200);
            Assert.Contains(all, s => s.Fingerprint.TotalPlayTimeSeconds == 300);
        }

        [Fact]
        public void WriteSidecar_FilenameUsesPrimaryFingerprintKey()
        {
            var store = new SidecarStore(_tempDir);
            store.WriteSidecar(Make(playTime: 12340));

            Assert.True(File.Exists(Path.Combine(_tempDir, "sidecar_12340.json")));
        }

        [Fact]
        public void TryFindClosestByPlayTime_EmptyStore_ReturnsFalse()
        {
            var store = new SidecarStore(_tempDir);

            Assert.False(store.TryFindClosestByPlayTime(12340, maxBackwardSeconds: 60, out _));
        }

        [Fact]
        public void TryFindClosestByPlayTime_ExactMatch_Returns()
        {
            var store = new SidecarStore(_tempDir);
            store.WriteSidecar(Make(playTime: 12340));

            Assert.True(store.TryFindClosestByPlayTime(12340, maxBackwardSeconds: 60, out var found));
            Assert.Equal(12340L, found.Fingerprint.TotalPlayTimeSeconds);
        }

        [Fact]
        public void TryFindClosestByPlayTime_PicksLargestUnderCurrent()
        {
            var store = new SidecarStore(_tempDir);
            store.WriteSidecar(Make(playTime: 100));
            store.WriteSidecar(Make(playTime: 200));
            store.WriteSidecar(Make(playTime: 300));

            // Current = 250 → nearest under is 200 (within 60s window).
            Assert.True(store.TryFindClosestByPlayTime(250, maxBackwardSeconds: 60, out var found));
            Assert.Equal(200L, found.Fingerprint.TotalPlayTimeSeconds);
        }

        [Fact]
        public void TryFindClosestByPlayTime_OutsideWindow_ReturnsFalse()
        {
            var store = new SidecarStore(_tempDir);
            store.WriteSidecar(Make(playTime: 100));

            // Current = 1000, window = 60. 1000 - 100 = 900 > 60 → no match.
            Assert.False(store.TryFindClosestByPlayTime(1000, maxBackwardSeconds: 60, out _));
        }

        [Fact]
        public void TryFindClosestByPlayTime_AllAboveCurrent_ReturnsFalse()
        {
            var store = new SidecarStore(_tempDir);
            store.WriteSidecar(Make(playTime: 200));
            store.WriteSidecar(Make(playTime: 300));

            // Current = 100. All sidecars represent saves "from the future" relative to now.
            Assert.False(store.TryFindClosestByPlayTime(100, maxBackwardSeconds: 60, out _));
        }
    }
}
