using FactionWars.ScriptHookV.Logging;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;

namespace FactionWars.ScriptHookV.Persistence
{
    /// <summary>
    /// Watches Rockstar's profile dir for SGTA file writes. Debounces FS event
    /// bursts (saves typically fire 2-3 events per file) and emits one
    /// OnNativeSaveWritten per logical save.
    /// </summary>
    public sealed class NativeSaveWatcher : IDisposable
    {
        public sealed class SaveEvent : EventArgs
        {
            public string Path { get; }
            public DateTime ModifiedAtUtc { get; }
            public SaveEvent(string path, DateTime modifiedAtUtc) { Path = path; ModifiedAtUtc = modifiedAtUtc; }
        }

        public event EventHandler<SaveEvent>? OnNativeSaveWritten;

        private readonly string _directory;
        private readonly int _debounceMs;
        private readonly FileSystemWatcher _fsw;
        private readonly ConcurrentDictionary<string, Timer> _timers = new ConcurrentDictionary<string, Timer>(StringComparer.OrdinalIgnoreCase);
        private bool _disposed;

        public NativeSaveWatcher(string directory, int debounceMs = 200)
        {
            if (string.IsNullOrEmpty(directory)) throw new ArgumentException("directory required", nameof(directory));
            _directory = directory;
            _debounceMs = debounceMs;

            _fsw = new FileSystemWatcher(_directory)
            {
                Filter = "SGTA*",
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
                IncludeSubdirectories = false,
            };

            _fsw.Changed += OnFsEvent;
            _fsw.Created += OnFsEvent;
            _fsw.Renamed += OnFsRenamed;
        }

        public void Start()
        {
            _fsw.EnableRaisingEvents = true;
            FileLogger.Info($"NativeSaveWatcher: started on {_directory}");
        }

        private void OnFsEvent(object sender, FileSystemEventArgs e) => Schedule(e.FullPath);
        private void OnFsRenamed(object sender, RenamedEventArgs e) => Schedule(e.FullPath);

        private void Schedule(string path)
        {
            if (_disposed) return;
            if (!IsSgtaFile(path)) return;

            _timers.AddOrUpdate(
                path,
                p => new Timer(state => Fire((string)state!), p, _debounceMs, Timeout.Infinite),
                (_, existing) =>
                {
                    existing.Change(_debounceMs, Timeout.Infinite);
                    return existing;
                });
        }

        private static bool IsSgtaFile(string path)
        {
            var name = Path.GetFileName(path);
            if (name == null) return false;
            if (!name.StartsWith("SGTA", StringComparison.OrdinalIgnoreCase)) return false;
            // GTA writes .bak alongside the real save; the .bak holds the previous
            // snapshot, so reacting to it captures stale stats and a stale filename.
            if (name.EndsWith(".bak", StringComparison.OrdinalIgnoreCase)) return false;
            return true;
        }

        private void Fire(string path)
        {
            if (_disposed) return;
            try
            {
                var info = new FileInfo(path);
                if (!info.Exists) return;

                var args = new SaveEvent(path, info.LastWriteTimeUtc);
                FileLogger.Info($"NativeSaveWatcher: detected save {Path.GetFileName(path)} mtime={info.LastWriteTimeUtc:O}");
                OnNativeSaveWritten?.Invoke(this, args);
            }
            catch (Exception ex)
            {
                FileLogger.Error("NativeSaveWatcher: failed firing save event", ex);
            }
            finally
            {
                if (_timers.TryRemove(path, out var t)) { try { t.Dispose(); } catch { } }
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            try { _fsw.EnableRaisingEvents = false; } catch { }
            try { _fsw.Dispose(); } catch { }
            foreach (var kv in _timers) { try { kv.Value.Dispose(); } catch { } }
            _timers.Clear();
        }
    }
}
