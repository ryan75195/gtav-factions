using FactionWars.ScriptHookV.Persistence;
using System;
using System.IO;
using System.Threading;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV.Persistence
{
    public class NativeSaveWatcherTests : IDisposable
    {
        private readonly string _tempDir;

        public NativeSaveWatcherTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "fw_watcher_test_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
        }

        public void Dispose()
        {
            try { Directory.Delete(_tempDir, recursive: true); } catch { }
        }

        private void WriteSgta(string name)
        {
            File.WriteAllText(Path.Combine(_tempDir, name), Guid.NewGuid().ToString());
        }

        [Fact]
        public void SingleSave_FiresOneEvent()
        {
            int eventCount = 0;
            using var watcher = new NativeSaveWatcher(_tempDir, debounceMs: 100);
            watcher.OnNativeSaveWritten += (_, _) => Interlocked.Increment(ref eventCount);
            watcher.Start();

            WriteSgta("SGTA00003");
            Thread.Sleep(400);

            Assert.Equal(1, eventCount);
        }

        [Fact]
        public void MultipleRapidWritesSamePath_DebouncesToOneEvent()
        {
            int eventCount = 0;
            using var watcher = new NativeSaveWatcher(_tempDir, debounceMs: 100);
            watcher.OnNativeSaveWritten += (_, _) => Interlocked.Increment(ref eventCount);
            watcher.Start();

            WriteSgta("SGTA00003");
            Thread.Sleep(20);
            WriteSgta("SGTA00003");
            Thread.Sleep(20);
            WriteSgta("SGTA00003");
            Thread.Sleep(400);

            Assert.Equal(1, eventCount);
        }

        [Fact]
        public void DistinctSaves_FireDistinctEvents()
        {
            int eventCount = 0;
            using var watcher = new NativeSaveWatcher(_tempDir, debounceMs: 100);
            watcher.OnNativeSaveWritten += (_, _) => Interlocked.Increment(ref eventCount);
            watcher.Start();

            WriteSgta("SGTA00003");
            Thread.Sleep(300);
            WriteSgta("SGTA00007");
            Thread.Sleep(400);

            Assert.Equal(2, eventCount);
        }

        [Fact]
        public void NonSgtaFile_IsIgnored()
        {
            int eventCount = 0;
            using var watcher = new NativeSaveWatcher(_tempDir, debounceMs: 100);
            watcher.OnNativeSaveWritten += (_, _) => Interlocked.Increment(ref eventCount);
            watcher.Start();

            File.WriteAllText(Path.Combine(_tempDir, "ignore.txt"), "hello");
            Thread.Sleep(300);

            Assert.Equal(0, eventCount);
        }
    }
}
