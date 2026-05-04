using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using FactionWars.Performance.Interfaces;
using FactionWars.Performance.Services;

namespace FactionWars.Tests.Unit.Performance
{
    public class ObjectPoolTests
    {
        #region Constructor Tests

        [Fact]
        public void Constructor_WithValidFactory_CreatesPool()
        {
            var pool = new ObjectPool<TestPooledObject>(() => new TestPooledObject());

            Assert.NotNull(pool);
            Assert.Equal(0, pool.Count);
        }

        [Fact]
        public void Constructor_WithNullFactory_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new ObjectPool<TestPooledObject>(null!));
        }

        [Fact]
        public void Constructor_WithMaxSize_SetsMaxSize()
        {
            var pool = new ObjectPool<TestPooledObject>(() => new TestPooledObject(), maxSize: 10);

            Assert.Equal(10, pool.MaxSize);
        }

        [Fact]
        public void Constructor_WithInvalidMaxSize_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new ObjectPool<TestPooledObject>(() => new TestPooledObject(), maxSize: 0));
        }

        [Fact]
        public void Constructor_WithResetAction_StoresResetAction()
        {
            bool resetCalled = false;
            var pool = new ObjectPool<TestPooledObject>(
                () => new TestPooledObject(),
                resetAction: obj => resetCalled = true);

            var item = pool.Get();
            pool.Return(item);
            pool.Get(); // Should trigger reset on reused item

            Assert.True(resetCalled);
        }

        #endregion

        #region Get Tests

        [Fact]
        public void Get_WhenPoolEmpty_CreatesNewObject()
        {
            int createCount = 0;
            var pool = new ObjectPool<TestPooledObject>(() =>
            {
                createCount++;
                return new TestPooledObject();
            });

            var item = pool.Get();

            Assert.NotNull(item);
            Assert.Equal(1, createCount);
        }

        [Fact]
        public void Get_WhenPoolHasItems_ReturnsPooledObject()
        {
            int createCount = 0;
            var pool = new ObjectPool<TestPooledObject>(() =>
            {
                createCount++;
                return new TestPooledObject();
            });

            var item1 = pool.Get();
            pool.Return(item1);
            var item2 = pool.Get();

            Assert.Same(item1, item2);
            Assert.Equal(1, createCount);
        }

        [Fact]
        public void Get_MultipleCalls_ReturnsUniqueObjects()
        {
            var pool = new ObjectPool<TestPooledObject>(() => new TestPooledObject());

            var item1 = pool.Get();
            var item2 = pool.Get();

            Assert.NotSame(item1, item2);
        }

        [Fact]
        public void Get_IncrementsTotalCreated()
        {
            var pool = new ObjectPool<TestPooledObject>(() => new TestPooledObject());

            pool.Get();
            pool.Get();

            Assert.Equal(2, pool.TotalCreated);
        }

        #endregion

        #region Return Tests

        [Fact]
        public void Return_AddsItemToPool()
        {
            var pool = new ObjectPool<TestPooledObject>(() => new TestPooledObject());
            var item = pool.Get();

            pool.Return(item);

            Assert.Equal(1, pool.Count);
        }

        [Fact]
        public void Return_NullItem_ThrowsArgumentNullException()
        {
            var pool = new ObjectPool<TestPooledObject>(() => new TestPooledObject());

            Assert.Throws<ArgumentNullException>(() => pool.Return(null!));
        }

        [Fact]
        public void Return_WhenPoolFull_DiscardsItem()
        {
            var pool = new ObjectPool<TestPooledObject>(() => new TestPooledObject(), maxSize: 2);

            var items = new List<TestPooledObject>();
            for (int i = 0; i < 5; i++)
            {
                items.Add(pool.Get());
            }

            foreach (var item in items)
            {
                pool.Return(item);
            }

            Assert.Equal(2, pool.Count);
        }

        [Fact]
        public void Return_CallsResetAction()
        {
            var resetItems = new List<TestPooledObject>();
            var pool = new ObjectPool<TestPooledObject>(
                () => new TestPooledObject(),
                resetAction: obj => resetItems.Add(obj));

            var item = pool.Get();
            pool.Return(item);

            Assert.Single(resetItems);
            Assert.Same(item, resetItems[0]);
        }

        [Fact]
        public void Return_IncrementsReturned()
        {
            var pool = new ObjectPool<TestPooledObject>(() => new TestPooledObject());
            var item = pool.Get();

            pool.Return(item);

            Assert.Equal(1, pool.TotalReturned);
        }

        #endregion

        #region Clear Tests

        [Fact]
        public void Clear_RemovesAllItems()
        {
            var pool = new ObjectPool<TestPooledObject>(() => new TestPooledObject());
            var item1 = pool.Get();
            var item2 = pool.Get();
            pool.Return(item1);
            pool.Return(item2);

            pool.Clear();

            Assert.Equal(0, pool.Count);
        }

        [Fact]
        public void Clear_CallsDisposeOnDisposableItems()
        {
            var pool = new ObjectPool<DisposableTestObject>(() => new DisposableTestObject());
            var item = pool.Get();
            pool.Return(item);

            pool.Clear();

            Assert.True(item.IsDisposed);
        }

        #endregion

        #region Statistics Tests

        [Fact]
        public void Statistics_TrackHitRate()
        {
            var pool = new ObjectPool<TestPooledObject>(() => new TestPooledObject());

            var item1 = pool.Get();
            pool.Return(item1);
            var item2 = pool.Get(); // Reuse - hit
            var item3 = pool.Get(); // New - miss

            Assert.Equal(1, pool.CacheHits);
            Assert.Equal(2, pool.CacheMisses);
        }

        [Fact]
        public void HitRate_ReturnsCorrectPercentage()
        {
            var pool = new ObjectPool<TestPooledObject>(() => new TestPooledObject());

            // Create 2 items, return them, get them again
            var item1 = pool.Get(); // miss
            var item2 = pool.Get(); // miss
            pool.Return(item1);
            pool.Return(item2);
            pool.Get(); // hit
            pool.Get(); // hit

            Assert.Equal(0.5f, pool.HitRate, 2);
        }

        [Fact]
        public void HitRate_WhenNoAccesses_ReturnsZero()
        {
            var pool = new ObjectPool<TestPooledObject>(() => new TestPooledObject());

            Assert.Equal(0f, pool.HitRate);
        }

        #endregion

        #region Thread Safety Tests

        [Fact]
        public async Task Get_ConcurrentAccess_IsThreadSafe()
        {
            var pool = new ObjectPool<TestPooledObject>(() => new TestPooledObject(), maxSize: 100);
            var items = new System.Collections.Concurrent.ConcurrentBag<TestPooledObject>();

            var tasks = new List<Task>();
            for (int i = 0; i < 100; i++)
            {
                tasks.Add(Task.Run(() =>
                {
                    var item = pool.Get();
                    items.Add(item);
                }));
            }

            await Task.WhenAll(tasks);

            Assert.Equal(100, items.Count);
            Assert.Equal(100, pool.TotalCreated);
        }

        [Fact]
        public async Task GetAndReturn_ConcurrentAccess_IsThreadSafe()
        {
            var pool = new ObjectPool<TestPooledObject>(() => new TestPooledObject(), maxSize: 10);

            var tasks = new List<Task>();
            for (int i = 0; i < 100; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    var item = pool.Get();
                    await Task.Delay(1);
                    pool.Return(item);
                }));
            }

            await Task.WhenAll(tasks);

            Assert.True(pool.Count <= pool.MaxSize);
        }

        #endregion

        #region Helper Classes

        private class TestPooledObject
        {
            public int Value { get; set; }
        }

        private class DisposableTestObject : IDisposable
        {
            public bool IsDisposed { get; private set; }

            public void Dispose()
            {
                IsDisposed = true;
            }
        }

        #endregion
    }
}
