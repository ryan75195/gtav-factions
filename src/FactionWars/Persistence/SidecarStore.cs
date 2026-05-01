using FactionWars.Persistence.Models;
using FactionWars.ScriptHookV.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace FactionWars.Persistence
{
    public sealed class SidecarStore : ISidecarStore
    {
        private const string FilePrefix = "sidecar_";
        private const string FileExtension = ".json";

        private readonly string _directory;
        private readonly JsonSerializerSettings _settings;

        public SidecarStore(string directory)
        {
            if (string.IsNullOrEmpty(directory))
            {
                throw new ArgumentException("Directory must be non-empty.", nameof(directory));
            }

            _directory = directory;
            Directory.CreateDirectory(_directory);

            _settings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore,
            };
        }

        public bool WriteSidecar(Sidecar sidecar)
        {
            if (sidecar == null) throw new ArgumentNullException(nameof(sidecar));
            if (sidecar.Fingerprint == null) throw new ArgumentException("Sidecar.Fingerprint required.", nameof(sidecar));

            var finalPath = GetPath(sidecar.Fingerprint.TotalPlayTimeSeconds);
            var tmpPath = finalPath + ".tmp";

            try
            {
                var json = JsonConvert.SerializeObject(sidecar, _settings);
                File.WriteAllText(tmpPath, json);

                if (File.Exists(finalPath))
                {
                    // File.Replace is atomic on Windows — no window where both files
                    // are missing if the process is killed mid-operation.
                    File.Replace(tmpPath, finalPath, destinationBackupFileName: null);
                }
                else
                {
                    File.Move(tmpPath, finalPath);
                }

                FileLogger.Info($"SidecarStore: wrote {Path.GetFileName(finalPath)} (totalPlayTime={sidecar.Fingerprint.TotalPlayTimeSeconds})");
                return true;
            }
            catch (Exception ex)
            {
                FileLogger.Error($"SidecarStore: failed to write {Path.GetFileName(finalPath)}", ex);
                try { if (File.Exists(tmpPath)) File.Delete(tmpPath); } catch { }
                return false;
            }
        }

        public bool TryFindByFingerprint(SaveFingerprint fingerprint, out Sidecar sidecar)
        {
            sidecar = null!;
            if (fingerprint == null) return false;

            var path = GetPath(fingerprint.TotalPlayTimeSeconds);
            if (!File.Exists(path)) return false;

            try
            {
                var json = File.ReadAllText(path);
                var loaded = JsonConvert.DeserializeObject<Sidecar>(json, _settings);
                if (loaded == null || loaded.Fingerprint == null)
                {
                    FileLogger.Warn($"SidecarStore: {Path.GetFileName(path)} deserialized to null; treating as no-match.");
                    return false;
                }

                if (!fingerprint.ExactMatch(loaded.Fingerprint))
                {
                    FileLogger.Warn($"SidecarStore: {Path.GetFileName(path)} primary key matched but tiebreakers diverged; treating as no-match.");
                    return false;
                }

                sidecar = loaded;
                return true;
            }
            catch (Exception ex)
            {
                FileLogger.Error($"SidecarStore: failed to read {Path.GetFileName(path)}", ex);
                return false;
            }
        }

        public bool TryFindClosestByPlayTime(long currentPlayTime, long maxBackwardSeconds, out Sidecar sidecar)
        {
            sidecar = null!;
            Sidecar best = null!;
            long bestPlayTime = -1;

            foreach (var candidate in ListAll())
            {
                if (candidate?.Fingerprint == null) continue;
                var pt = candidate.Fingerprint.TotalPlayTimeSeconds;
                if (pt > currentPlayTime) continue;
                if (currentPlayTime - pt > maxBackwardSeconds) continue;
                if (pt > bestPlayTime)
                {
                    bestPlayTime = pt;
                    best = candidate;
                }
            }

            if (best == null) return false;
            sidecar = best;
            return true;
        }

        public IReadOnlyList<Sidecar> ListAll()
        {
            var results = new List<Sidecar>();
            if (!Directory.Exists(_directory)) return results;

            foreach (var path in Directory.EnumerateFiles(_directory, FilePrefix + "*" + FileExtension))
            {
                try
                {
                    var json = File.ReadAllText(path);
                    var loaded = JsonConvert.DeserializeObject<Sidecar>(json, _settings);
                    if (loaded != null) results.Add(loaded);
                }
                catch (Exception ex)
                {
                    FileLogger.Error($"SidecarStore: skipping unreadable {Path.GetFileName(path)}", ex);
                }
            }

            return results;
        }

        private string GetPath(long totalPlayTimeSeconds)
            => Path.Combine(_directory, $"{FilePrefix}{totalPlayTimeSeconds}{FileExtension}");
    }
}
