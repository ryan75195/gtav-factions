using FactionWars.ScriptHookV.Persistence;
using System;
using System.IO;
using System.Threading;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV.Persistence
{
    [CollectionDefinition("NativeSaveWatcher", DisableParallelization = true)]
    public sealed class NativeSaveWatcherCollection
    {
    }

    [Collection("NativeSaveWatcher")]
    public class NativeSaveWatcherTests : IDisposable
    {
        private const int DebounceMs = 250;
        private const int SettleMs = 1500;

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

        private static bool WaitForEventCount(Func<int> getCount, int target, int timeoutMs)
        {
            var start = Environment.TickCount;
            while (Environment.TickCount - start < timeoutMs)
            {
                if (getCount() >= target) return true;
                Thread.Sleep(20);
            }
            return getCount() >= target;
        }

        [Fact]
        public void SingleSave_FiresOneEvent()
        {
            int eventCount = 0;
            using var watcher = new NativeSaveWatcher(_tempDir, debounceMs: DebounceMs);
            watcher.OnNativeSaveWritten += (_, _) => Interlocked.Increment(ref eventCount);
            watcher.Start();

            WriteSgta("SGTA00003");
            WaitForEventCount(() => Volatile.Read(ref eventCount), 1, SettleMs);
            Thread.Sleep(DebounceMs);

            Assert.Equal(1, eventCount);
        }

        [Fact]
        public void SingleSave_DoesNotRefireWithoutNewWrite()
        {
            int eventCount = 0;
            using var watcher = new NativeSaveWatcher(_tempDir, debounceMs: DebounceMs);
            watcher.OnNativeSaveWritten += (_, _) => Interlocked.Increment(ref eventCount);
            watcher.Start();

            WriteSgta("SGTA00003");
            Assert.True(WaitForEventCount(() => Volatile.Read(ref eventCount), 1, SettleMs), "first event did not fire");
            Thread.Sleep(DebounceMs * 3);

            Assert.Equal(1, eventCount);
        }

        [Fact]
        public void MultipleRapidWritesSamePath_DebouncesToOneEvent()
        {
            int eventCount = 0;
            using var watcher = new NativeSaveWatcher(_tempDir, debounceMs: DebounceMs);
            watcher.OnNativeSaveWritten += (_, _) => Interlocked.Increment(ref eventCount);
            watcher.Start();

            WriteSgta("SGTA00003");
            Thread.Sleep(20);
            WriteSgta("SGTA00003");
            Thread.Sleep(20);
            WriteSgta("SGTA00003");
            WaitForEventCount(() => Volatile.Read(ref eventCount), 1, SettleMs);
            Thread.Sleep(DebounceMs);

            Assert.Equal(1, eventCount);
        }

        [Fact]
        public void DistinctSaves_FireDistinctEvents()
        {
            int eventCount = 0;
            using var watcher = new NativeSaveWatcher(_tempDir, debounceMs: DebounceMs);
            watcher.OnNativeSaveWritten += (_, _) => Interlocked.Increment(ref eventCount);
            watcher.Start();

            WriteSgta("SGTA00003");
            Assert.True(WaitForEventCount(() => Volatile.Read(ref eventCount), 1, SettleMs), "first event did not fire");

            WriteSgta("SGTA00007");
            Assert.True(WaitForEventCount(() => Volatile.Read(ref eventCount), 2, SettleMs), "second event did not fire");

            Assert.Equal(2, eventCount);
        }

        [Fact]
        public void NonSgtaFile_IsIgnored()
        {
            int eventCount = 0;
            using var watcher = new NativeSaveWatcher(_tempDir, debounceMs: DebounceMs);
            watcher.OnNativeSaveWritten += (_, _) => Interlocked.Increment(ref eventCount);
            watcher.Start();

            File.WriteAllText(Path.Combine(_tempDir, "ignore.txt"), "hello");
            Thread.Sleep(SettleMs);

            Assert.Equal(0, eventCount);
        }

        [Fact]
        public void BakFile_IsIgnored()
        {
            int eventCount = 0;
            using var watcher = new NativeSaveWatcher(_tempDir, debounceMs: DebounceMs);
            watcher.OnNativeSaveWritten += (_, _) => Interlocked.Increment(ref eventCount);
            watcher.Start();

            WriteSgta("SGTA00003.bak");
            Thread.Sleep(SettleMs);

            Assert.Equal(0, eventCount);
        }
    }
}
