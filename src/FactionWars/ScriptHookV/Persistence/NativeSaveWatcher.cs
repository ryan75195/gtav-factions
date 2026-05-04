using FactionWars.ScriptHookV.Logging;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;

namespace FactionWars.ScriptHookV.Persistence
{
    internal interface INativeFileSystemWatcher : IDisposable
    {
        event FileSystemEventHandler Changed;
        event FileSystemEventHandler Created;
        event RenamedEventHandler Renamed;

        bool EnableRaisingEvents { get; set; }
    }

    internal sealed class NativeFileSystemWatcher : INativeFileSystemWatcher
    {
        private readonly FileSystemWatcher _watcher;

        private NativeFileSystemWatcher(FileSystemWatcher watcher)
        {
            _watcher = watcher ?? throw new ArgumentNullException(nameof(watcher));
        }

        public event FileSystemEventHandler Changed
        {
            add => _watcher.Changed += value;
            remove => _watcher.Changed -= value;
        }

        public event FileSystemEventHandler Created
        {
            add => _watcher.Created += value;
            remove => _watcher.Created -= value;
        }

        public event RenamedEventHandler Renamed
        {
            add => _watcher.Renamed += value;
            remove => _watcher.Renamed -= value;
        }

        public bool EnableRaisingEvents
        {
            get => _watcher.EnableRaisingEvents;
            set => _watcher.EnableRaisingEvents = value;
        }

        public static NativeFileSystemWatcher Create(string directory)
        {
            var watcher = new FileSystemWatcher(directory)
            {
                Filter = "SGTA*",
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
                IncludeSubdirectories = false,
            };

            return new NativeFileSystemWatcher(watcher);
        }

        public void Dispose() => _watcher.Dispose();
    }

    /// <summary>
    /// Watches Rockstar's profile dir for SGTA file writes. Debounces FS event
    /// bursts (saves typically fire 2-3 events per file) and emits one
    /// OnNativeSaveWritten per logical save.
    /// </summary>
    public sealed class NativeSaveWatcher : IDisposable
    {
        public event EventHandler<SaveEvent>? OnNativeSaveWritten;

        private readonly string _directory;
        private readonly int _debounceMs;
        private readonly INativeFileSystemWatcher _fsw;
        private readonly ConcurrentDictionary<string, Timer> _timers = new ConcurrentDictionary<string, Timer>(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, DateTime> _pendingWriteTimes = new ConcurrentDictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
        private Timer? _pollTimer;
        private bool _disposed;

        public NativeSaveWatcher(string directory, int debounceMs = 200)
            : this(directory, NativeFileSystemWatcher.Create(directory), debounceMs)
        {
        }

        internal NativeSaveWatcher(string directory, INativeFileSystemWatcher fileSystemWatcher, int debounceMs = 200)
        {
            if (string.IsNullOrEmpty(directory)) throw new ArgumentException("directory required", nameof(directory));
            _directory = directory;
            _debounceMs = debounceMs;
            _fsw = fileSystemWatcher ?? throw new ArgumentNullException(nameof(fileSystemWatcher));

            _fsw.Changed += OnFsEvent;
            _fsw.Created += OnFsEvent;
            _fsw.Renamed += OnFsRenamed;
        }

        public void Start()
        {
            _fsw.EnableRaisingEvents = true;
            _pollTimer = new Timer(_ => PollDirectory(), null, _debounceMs, _debounceMs);
            FileLogger.Info($"NativeSaveWatcher: started on {_directory}");
        }

        private void OnFsEvent(object sender, FileSystemEventArgs e) => Schedule(e.FullPath);
        private void OnFsRenamed(object sender, RenamedEventArgs e) => Schedule(e.FullPath);

        private void Schedule(string path)
        {
            if (_disposed) return;
            if (!IsSgtaFile(path)) return;

            DateTime currentWriteTimeUtc;
            try
            {
                var info = new FileInfo(path);
                if (!info.Exists) return;
                currentWriteTimeUtc = info.LastWriteTimeUtc;
            }
            catch (Exception ex)
            {
                FileLogger.Error($"NativeSaveWatcher: failed reading mtime for {path}", ex);
                return;
            }

            if (_pendingWriteTimes.TryGetValue(path, out var pendingWriteTimeUtc) && currentWriteTimeUtc <= pendingWriteTimeUtc)
            {
                return;
            }

            _pendingWriteTimes[path] = currentWriteTimeUtc;

            _timers.AddOrUpdate(
                path,
                p => new Timer(_ => Fire(p), null, _debounceMs, Timeout.Infinite),
                (_, existing) =>
                {
                    existing.Change(_debounceMs, Timeout.Infinite);
                    return existing;
                });
        }

        private void PollDirectory()
        {
            if (_disposed) return;

            try
            {
                foreach (var path in Directory.EnumerateFiles(_directory, "SGTA*"))
                {
                    Schedule(path);
                }
            }
            catch (Exception ex)
            {
                FileLogger.Error("NativeSaveWatcher: polling failed", ex);
            }
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
                if (_pendingWriteTimes.TryGetValue(path, out var pendingWriteTimeUtc))
                {
                    var info = new FileInfo(path);
                    if (!info.Exists) return;

                    if (info.LastWriteTimeUtc < pendingWriteTimeUtc)
                    {
                        return;
                    }

                    var args = new SaveEvent(path, info.LastWriteTimeUtc);
                    FileLogger.Info($"NativeSaveWatcher: detected save {Path.GetFileName(path)} mtime={info.LastWriteTimeUtc:O}");
                    OnNativeSaveWritten?.Invoke(this, args);
                }
            }
            catch (Exception ex)
            {
                FileLogger.Error("NativeSaveWatcher: failed firing save event", ex);
            }
            finally
            {
                if (_timers.TryRemove(path, out var t)) { try { t.Dispose(); } catch { } }
                _pendingWriteTimes.TryRemove(path, out _);
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            try { _fsw.EnableRaisingEvents = false; } catch { }
            try { _fsw.Dispose(); } catch { }
            try { _pollTimer?.Dispose(); } catch { }
            foreach (var kv in _timers) { try { kv.Value.Dispose(); } catch { } }
            _timers.Clear();
            _pendingWriteTimes.Clear();
        }
    }
}
