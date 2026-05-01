using FactionWars.ScriptHookV.Persistence;
using System;
using System.IO;
using System.Linq;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV.Persistence
{
    public class LegacyBackupTaskTests : IDisposable
    {
        private readonly string _tempDir;

        public LegacyBackupTaskTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "fw_legacy_test_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
        }

        public void Dispose()
        {
            try { Directory.Delete(_tempDir, recursive: true); } catch { }
        }

        [Fact]
        public void NoLegacyFiles_NoOp()
        {
            var sut = new LegacyBackupTask(_tempDir);
            sut.Run();

            Assert.False(Directory.GetDirectories(_tempDir, "legacy_backup_*").Any());
        }

        [Fact]
        public void LegacyFilesPresent_MovedToBackupSubfolder()
        {
            File.WriteAllText(Path.Combine(_tempDir, "save_slot_0.json"), "{}");
            File.WriteAllText(Path.Combine(_tempDir, "save_slot_5.json"), "{}");
            File.WriteAllText(Path.Combine(_tempDir, "unrelated.txt"), "keep me");

            var sut = new LegacyBackupTask(_tempDir);
            sut.Run();

            Assert.False(File.Exists(Path.Combine(_tempDir, "save_slot_0.json")));
            Assert.False(File.Exists(Path.Combine(_tempDir, "save_slot_5.json")));
            Assert.True(File.Exists(Path.Combine(_tempDir, "unrelated.txt")));

            var backups = Directory.GetDirectories(_tempDir, "legacy_backup_*");
            Assert.Single(backups);
            Assert.True(File.Exists(Path.Combine(backups[0], "save_slot_0.json")));
            Assert.True(File.Exists(Path.Combine(backups[0], "save_slot_5.json")));
        }

        [Fact]
        public void Run_CreatesSidecarsSubdirIfMissing()
        {
            var sut = new LegacyBackupTask(_tempDir);
            sut.Run();

            Assert.True(Directory.Exists(Path.Combine(_tempDir, "sidecars")));
        }

        [Fact]
        public void Run_IsIdempotent_WhenNoLegacyFiles()
        {
            var sut = new LegacyBackupTask(_tempDir);
            sut.Run();
            sut.Run();
            sut.Run();

            Assert.False(Directory.GetDirectories(_tempDir, "legacy_backup_*").Any());
        }
    }
}
