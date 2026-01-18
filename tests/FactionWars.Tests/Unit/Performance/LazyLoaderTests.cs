using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using FactionWars.Performance.Interfaces;
using FactionWars.Performance.Services;

namespace FactionWars.Tests.Unit.Performance
{
    public class LazyLoaderTests
    {
        #region Constructor Tests

        [Fact]
        public void Constructor_WithValidFactory_CreatesLoader()
        {
            var loader = new LazyLoader<TestResource>(() => new TestResource());

            Assert.NotNull(loader);
            Assert.False(loader.IsLoaded);
        }

        [Fact]
        public void Constructor_WithNullFactory_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new LazyLoader<TestResource>(null!));
        }

        #endregion

        #region Value Property Tests

        [Fact]
        public void Value_FirstAccess_CallsFactory()
        {
            bool factoryCalled = false;
            var loader = new LazyLoader<TestResource>(() =>
            {
                factoryCalled = true;
                return new TestResource();
            });

            var _ = loader.Value;

            Assert.True(factoryCalled);
        }

        [Fact]
        public void Value_SecondAccess_DoesNotCallFactoryAgain()
        {
            int factoryCalls = 0;
            var loader = new LazyLoader<TestResource>(() =>
            {
                factoryCalls++;
                return new TestResource();
            });

            var _ = loader.Value;
            var __ = loader.Value;

            Assert.Equal(1, factoryCalls);
        }

        [Fact]
        public void Value_ReturnsCreatedInstance()
        {
            var expected = new TestResource { Id = 42 };
            var loader = new LazyLoader<TestResource>(() => expected);

            var actual = loader.Value;

            Assert.Same(expected, actual);
        }

        [Fact]
        public void Value_FactoryThrows_PropagatesException()
        {
            var loader = new LazyLoader<TestResource>(() => throw new InvalidOperationException("Test error"));

            Assert.Throws<InvalidOperationException>(() => _ = loader.Value);
        }

        [Fact]
        public void Value_FactoryThrows_RetryOnNextAccess()
        {
            int callCount = 0;
            var loader = new LazyLoader<TestResource>(() =>
            {
                callCount++;
                if (callCount == 1)
                    throw new InvalidOperationException("First call fails");
                return new TestResource { Id = 42 };
            });

            Assert.Throws<InvalidOperationException>(() => _ = loader.Value);
            var value = loader.Value;

            Assert.Equal(42, value.Id);
            Assert.Equal(2, callCount);
        }

        #endregion

        #region IsLoaded Tests

        [Fact]
        public void IsLoaded_BeforeAccess_ReturnsFalse()
        {
            var loader = new LazyLoader<TestResource>(() => new TestResource());

            Assert.False(loader.IsLoaded);
        }

        [Fact]
        public void IsLoaded_AfterAccess_ReturnsTrue()
        {
            var loader = new LazyLoader<TestResource>(() => new TestResource());

            var _ = loader.Value;

            Assert.True(loader.IsLoaded);
        }

        [Fact]
        public void IsLoaded_AfterReset_ReturnsFalse()
        {
            var loader = new LazyLoader<TestResource>(() => new TestResource());
            var _ = loader.Value;

            loader.Reset();

            Assert.False(loader.IsLoaded);
        }

        #endregion

        #region Reset Tests

        [Fact]
        public void Reset_ClearsLoadedValue()
        {
            var loader = new LazyLoader<TestResource>(() => new TestResource());
            var firstValue = loader.Value;

            loader.Reset();
            var secondValue = loader.Value;

            Assert.NotSame(firstValue, secondValue);
        }

        [Fact]
        public void Reset_AllowsFactoryToBeCalledAgain()
        {
            int factoryCalls = 0;
            var loader = new LazyLoader<TestResource>(() =>
            {
                factoryCalls++;
                return new TestResource();
            });

            var _ = loader.Value;
            loader.Reset();
            var __ = loader.Value;

            Assert.Equal(2, factoryCalls);
        }

        [Fact]
        public void Reset_CallsDisposeOnDisposableValue()
        {
            var resource = new DisposableResource();
            var loader = new LazyLoader<DisposableResource>(() => resource);

            var _ = loader.Value;
            loader.Reset();

            Assert.True(resource.IsDisposed);
        }

        [Fact]
        public void Reset_BeforeLoad_DoesNotThrow()
        {
            var loader = new LazyLoader<TestResource>(() => new TestResource());

            var exception = Record.Exception(() => loader.Reset());

            Assert.Null(exception);
        }

        #endregion

        #region Thread Safety Tests

        [Fact]
        public async Task Value_ConcurrentAccess_CallsFactoryOnlyOnce()
        {
            int factoryCalls = 0;
            var loader = new LazyLoader<TestResource>(() =>
            {
                Interlocked.Increment(ref factoryCalls);
                Thread.Sleep(50); // Simulate slow initialization
                return new TestResource();
            });

            var tasks = new Task<TestResource>[10];
            for (int i = 0; i < 10; i++)
            {
                tasks[i] = Task.Run(() => loader.Value);
            }

            var results = await Task.WhenAll(tasks);

            Assert.Equal(1, factoryCalls);
            Assert.All(results, r => Assert.Same(results[0], r));
        }

        [Fact]
        public async Task Reset_DuringAccess_IsThreadSafe()
        {
            var loader = new LazyLoader<TestResource>(() => new TestResource());

            var accessTask = Task.Run(() =>
            {
                for (int i = 0; i < 100; i++)
                {
                    var _ = loader.Value;
                    Thread.Sleep(1);
                }
            });

            var resetTask = Task.Run(() =>
            {
                for (int i = 0; i < 50; i++)
                {
                    loader.Reset();
                    Thread.Sleep(2);
                }
            });

            var exception = await Record.ExceptionAsync(() => Task.WhenAll(accessTask, resetTask));

            Assert.Null(exception);
        }

        #endregion

        #region GetValueOrDefault Tests

        [Fact]
        public void GetValueOrDefault_WhenLoaded_ReturnsValue()
        {
            var expected = new TestResource { Id = 42 };
            var loader = new LazyLoader<TestResource>(() => expected);
            var _ = loader.Value;

            var result = loader.GetValueOrDefault();

            Assert.Same(expected, result);
        }

        [Fact]
        public void GetValueOrDefault_WhenNotLoaded_ReturnsNull()
        {
            var loader = new LazyLoader<TestResource>(() => new TestResource());

            var result = loader.GetValueOrDefault();

            Assert.Null(result);
        }

        [Fact]
        public void GetValueOrDefault_DoesNotTriggerLoad()
        {
            int factoryCalls = 0;
            var loader = new LazyLoader<TestResource>(() =>
            {
                factoryCalls++;
                return new TestResource();
            });

            var _ = loader.GetValueOrDefault();

            Assert.Equal(0, factoryCalls);
            Assert.False(loader.IsLoaded);
        }

        #endregion

        #region TryGetValue Tests

        [Fact]
        public void TryGetValue_WhenLoaded_ReturnsTrueAndValue()
        {
            var expected = new TestResource { Id = 42 };
            var loader = new LazyLoader<TestResource>(() => expected);
            var _ = loader.Value;

            bool result = loader.TryGetValue(out var value);

            Assert.True(result);
            Assert.Same(expected, value);
        }

        [Fact]
        public void TryGetValue_WhenNotLoaded_ReturnsFalse()
        {
            var loader = new LazyLoader<TestResource>(() => new TestResource());

            bool result = loader.TryGetValue(out var value);

            Assert.False(result);
            Assert.Null(value);
        }

        [Fact]
        public void TryGetValue_DoesNotTriggerLoad()
        {
            int factoryCalls = 0;
            var loader = new LazyLoader<TestResource>(() =>
            {
                factoryCalls++;
                return new TestResource();
            });

            var _ = loader.TryGetValue(out var __);

            Assert.Equal(0, factoryCalls);
        }

        #endregion

        #region Helper Classes

        private class TestResource
        {
            public int Id { get; set; }
        }

        private class DisposableResource : IDisposable
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
