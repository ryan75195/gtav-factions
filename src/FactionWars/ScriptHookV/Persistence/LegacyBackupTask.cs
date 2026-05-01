using FactionWars.ScriptHookV.Logging;
using System;
using System.IO;
using System.Linq;

namespace FactionWars.ScriptHookV.Persistence
{
    /// <summary>
    /// One-shot first-launch task: relocates legacy save_slot_*.json files to a
    /// dated backup subfolder and ensures the sidecars/ subdirectory exists.
    /// Safe to run repeatedly — no-ops if no legacy files are present.
    /// </summary>
    public sealed class LegacyBackupTask
    {
        private const string LegacyPattern = "save_slot_*.json";
        private const string SidecarsSubdir = "sidecars";
        private const string BackupPrefix = "legacy_backup_";

        private readonly string _saveDirectory;

        public LegacyBackupTask(string saveDirectory)
        {
            if (string.IsNullOrEmpty(saveDirectory)) throw new ArgumentException("required", nameof(saveDirectory));
            _saveDirectory = saveDirectory;
        }

        public void Run()
        {
            Directory.CreateDirectory(_saveDirectory);
            Directory.CreateDirectory(Path.Combine(_saveDirectory, SidecarsSubdir));

            var legacyFiles = Directory.EnumerateFiles(_saveDirectory, LegacyPattern, SearchOption.TopDirectoryOnly).ToList();
            if (legacyFiles.Count == 0)
            {
                FileLogger.Debug("LegacyBackupTask: no legacy save_slot_*.json files; no-op.");
                return;
            }

            var stamp = DateTime.UtcNow.ToString("yyyyMMddTHHmmssZ");
            var backupDir = Path.Combine(_saveDirectory, BackupPrefix + stamp);
            int suffix = 0;
            while (Directory.Exists(backupDir))
            {
                suffix++;
                backupDir = Path.Combine(_saveDirectory, BackupPrefix + stamp + "_" + suffix);
            }
            Directory.CreateDirectory(backupDir);

            foreach (var file in legacyFiles)
            {
                var dest = Path.Combine(backupDir, Path.GetFileName(file));
                File.Move(file, dest);
            }

            FileLogger.Info($"LegacyBackupTask: moved {legacyFiles.Count} legacy save(s) to {backupDir}");
        }
    }
}
