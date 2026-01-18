using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using FactionWars.Performance.Interfaces;
using FactionWars.Performance.Services;
using FactionWars.Core.Interfaces;
using Moq;

namespace FactionWars.Tests.Unit.Performance
{
    public class CacheServiceTests
    {
        private readonly Mock<ITimeProvider> _mockTimeProvider;

        public CacheServiceTests()
        {
            _mockTimeProvider = new Mock<ITimeProvider>();
            _mockTimeProvider.Setup(t => t.UtcNow).Returns(DateTime.UtcNow);
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithDefaults_CreatesCache()
        {
            var cache = new CacheService<string, int>(_mockTimeProvider.Object);

            Assert.NotNull(cache);
            Assert.Equal(0, cache.Count);
        }

        [Fact]
        public void Constructor_WithNullTimeProvider_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new CacheService<string, int>(null!));
        }

        [Fact]
        public void Constructor_WithMaxSize_SetsMaxSize()
        {
            var cache = new CacheService<string, int>(_mockTimeProvider.Object, maxSize: 100);

            Assert.Equal(100, cache.MaxSize);
        }

        [Fact]
        public void Constructor_WithInvalidMaxSize_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new CacheService<string, int>(_mockTimeProvider.Object, maxSize: 0));
        }

        [Fact]
        public void Constructor_WithDefaultExpiration_SetsDefaultExpiration()
        {
            var expiration = TimeSpan.FromMinutes(5);
            var cache = new CacheService<string, int>(_mockTimeProvider.Object, defaultExpiration: expiration);

            Assert.Equal(expiration, cache.DefaultExpiration);
        }

        #endregion

        #region Set Tests

        [Fact]
        public void Set_AddsItemToCache()
        {
            var cache = new CacheService<string, int>(_mockTimeProvider.Object);

            cache.Set("key1", 42);

            Assert.Equal(1, cache.Count);
        }

        [Fact]
        public void Set_WithNullKey_ThrowsArgumentNullException()
        {
            var cache = new CacheService<string, int>(_mockTimeProvider.Object);

            Assert.Throws<ArgumentNullException>(() => cache.Set(null!, 42));
        }

        [Fact]
        public void Set_OverwritesExistingKey()
        {
            var cache = new CacheService<string, int>(_mockTimeProvider.Object);

            cache.Set("key1", 42);
            cache.Set("key1", 100);

            Assert.Equal(1, cache.Count);
            Assert.True(cache.TryGet("key1", out var value));
            Assert.Equal(100, value);
        }

        [Fact]
        public void Set_WithCustomExpiration_UsesCustomExpiration()
        {
            var now = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            _mockTimeProvider.Setup(t => t.UtcNow).Returns(now);

            var cache = new CacheService<string, int>(_mockTimeProvider.Object);
            cache.Set("key1", 42, TimeSpan.FromMinutes(1));

            // Move time forward by 30 seconds - should still be valid
            _mockTimeProvider.Setup(t => t.UtcNow).Returns(now.AddSeconds(30));
            Assert.True(cache.TryGet("key1", out _));

            // Move time forward to 2 minutes - should be expired
            _mockTimeProvider.Setup(t => t.UtcNow).Returns(now.AddMinutes(2));
            Assert.False(cache.TryGet("key1", out _));
        }

        [Fact]
        public void Set_WhenCacheFull_EvictsOldestEntry()
        {
            var now = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            _mockTimeProvider.Setup(t => t.UtcNow).Returns(now);

            var cache = new CacheService<string, int>(_mockTimeProvider.Object, maxSize: 2);

            cache.Set("key1", 1);

            _mockTimeProvider.Setup(t => t.UtcNow).Returns(now.AddSeconds(1));
            cache.Set("key2", 2);

            _mockTimeProvider.Setup(t => t.UtcNow).Returns(now.AddSeconds(2));
            cache.Set("key3", 3);

            Assert.Equal(2, cache.Count);
            Assert.False(cache.TryGet("key1", out _)); // key1 should be evicted
            Assert.True(cache.TryGet("key2", out _));
            Assert.True(cache.TryGet("key3", out _));
        }

        #endregion

        #region TryGet Tests

        [Fact]
        public void TryGet_WithValidKey_ReturnsTrueAndValue()
        {
            var cache = new CacheService<string, int>(_mockTimeProvider.Object);
            cache.Set("key1", 42);

            bool result = cache.TryGet("key1", out int value);

            Assert.True(result);
            Assert.Equal(42, value);
        }

        [Fact]
        public void TryGet_WithMissingKey_ReturnsFalse()
        {
            var cache = new CacheService<string, int>(_mockTimeProvider.Object);

            bool result = cache.TryGet("missing", out int value);

            Assert.False(result);
            Assert.Equal(default, value);
        }

        [Fact]
        public void TryGet_WithNullKey_ThrowsArgumentNullException()
        {
            var cache = new CacheService<string, int>(_mockTimeProvider.Object);

            Assert.Throws<ArgumentNullException>(() => cache.TryGet(null!, out _));
        }

        [Fact]
        public void TryGet_WithExpiredEntry_ReturnsFalse()
        {
            var now = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            _mockTimeProvider.Setup(t => t.UtcNow).Returns(now);

            var cache = new CacheService<string, int>(
                _mockTimeProvider.Object,
                defaultExpiration: TimeSpan.FromMinutes(1));

            cache.Set("key1", 42);

            // Move time forward past expiration
            _mockTimeProvider.Setup(t => t.UtcNow).Returns(now.AddMinutes(2));

            bool result = cache.TryGet("key1", out _);

            Assert.False(result);
        }

        [Fact]
        public void TryGet_UpdatesHitStatistics()
        {
            var cache = new CacheService<string, int>(_mockTimeProvider.Object);
            cache.Set("key1", 42);

            cache.TryGet("key1", out _);
            cache.TryGet("missing", out _);

            Assert.Equal(1, cache.Hits);
            Assert.Equal(1, cache.Misses);
        }

        #endregion

        #region GetOrAdd Tests

        [Fact]
        public void GetOrAdd_WithMissingKey_CallsFactory()
        {
            var cache = new CacheService<string, int>(_mockTimeProvider.Object);
            int factoryCalls = 0;

            int value = cache.GetOrAdd("key1", () =>
            {
                factoryCalls++;
                return 42;
            });

            Assert.Equal(42, value);
            Assert.Equal(1, factoryCalls);
        }

        [Fact]
        public void GetOrAdd_WithExistingKey_DoesNotCallFactory()
        {
            var cache = new CacheService<string, int>(_mockTimeProvider.Object);
            cache.Set("key1", 42);
            int factoryCalls = 0;

            int value = cache.GetOrAdd("key1", () =>
            {
                factoryCalls++;
                return 100;
            });

            Assert.Equal(42, value);
            Assert.Equal(0, factoryCalls);
        }

        [Fact]
        public void GetOrAdd_WithNullKey_ThrowsArgumentNullException()
        {
            var cache = new CacheService<string, int>(_mockTimeProvider.Object);

            Assert.Throws<ArgumentNullException>(() => cache.GetOrAdd(null!, () => 42));
        }

        [Fact]
        public void GetOrAdd_WithNullFactory_ThrowsArgumentNullException()
        {
            var cache = new CacheService<string, int>(_mockTimeProvider.Object);

            Assert.Throws<ArgumentNullException>(() => cache.GetOrAdd("key1", null!));
        }

        [Fact]
        public void GetOrAdd_CachesFactoryResult()
        {
            var cache = new CacheService<string, int>(_mockTimeProvider.Object);
            int factoryCalls = 0;

            cache.GetOrAdd("key1", () => { factoryCalls++; return 42; });
            cache.GetOrAdd("key1", () => { factoryCalls++; return 100; });

            Assert.Equal(1, factoryCalls);
        }

        #endregion

        #region Remove Tests

        [Fact]
        public void Remove_WithExistingKey_ReturnsTrue()
        {
            var cache = new CacheService<string, int>(_mockTimeProvider.Object);
            cache.Set("key1", 42);

            bool result = cache.Remove("key1");

            Assert.True(result);
            Assert.Equal(0, cache.Count);
        }

        [Fact]
        public void Remove_WithMissingKey_ReturnsFalse()
        {
            var cache = new CacheService<string, int>(_mockTimeProvider.Object);

            bool result = cache.Remove("missing");

            Assert.False(result);
        }

        [Fact]
        public void Remove_WithNullKey_ThrowsArgumentNullException()
        {
            var cache = new CacheService<string, int>(_mockTimeProvider.Object);

            Assert.Throws<ArgumentNullException>(() => cache.Remove(null!));
        }

        #endregion

        #region Clear Tests

        [Fact]
        public void Clear_RemovesAllItems()
        {
            var cache = new CacheService<string, int>(_mockTimeProvider.Object);
            cache.Set("key1", 1);
            cache.Set("key2", 2);
            cache.Set("key3", 3);

            cache.Clear();

            Assert.Equal(0, cache.Count);
        }

        [Fact]
        public void Clear_ResetsStatistics()
        {
            var cache = new CacheService<string, int>(_mockTimeProvider.Object);
            cache.Set("key1", 1);
            cache.TryGet("key1", out _);
            cache.TryGet("missing", out _);

            cache.Clear();

            Assert.Equal(0, cache.Hits);
            Assert.Equal(0, cache.Misses);
        }

        #endregion

        #region Contains Tests

        [Fact]
        public void Contains_WithExistingKey_ReturnsTrue()
        {
            var cache = new CacheService<string, int>(_mockTimeProvider.Object);
            cache.Set("key1", 42);

            Assert.True(cache.Contains("key1"));
        }

        [Fact]
        public void Contains_WithMissingKey_ReturnsFalse()
        {
            var cache = new CacheService<string, int>(_mockTimeProvider.Object);

            Assert.False(cache.Contains("missing"));
        }

        [Fact]
        public void Contains_WithExpiredKey_ReturnsFalse()
        {
            var now = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            _mockTimeProvider.Setup(t => t.UtcNow).Returns(now);

            var cache = new CacheService<string, int>(
                _mockTimeProvider.Object,
                defaultExpiration: TimeSpan.FromMinutes(1));

            cache.Set("key1", 42);
            _mockTimeProvider.Setup(t => t.UtcNow).Returns(now.AddMinutes(2));

            Assert.False(cache.Contains("key1"));
        }

        #endregion

        #region Eviction Tests

        [Fact]
        public void Evict_RemovesExpiredEntries()
        {
            var now = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            _mockTimeProvider.Setup(t => t.UtcNow).Returns(now);

            var cache = new CacheService<string, int>(
                _mockTimeProvider.Object,
                defaultExpiration: TimeSpan.FromMinutes(1));

            cache.Set("key1", 1);
            cache.Set("key2", 2);

            _mockTimeProvider.Setup(t => t.UtcNow).Returns(now.AddMinutes(2));

            int evicted = cache.EvictExpired();

            Assert.Equal(2, evicted);
            Assert.Equal(0, cache.Count);
        }

        [Fact]
        public void Evict_DoesNotRemoveNonExpiredEntries()
        {
            var now = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            _mockTimeProvider.Setup(t => t.UtcNow).Returns(now);

            var cache = new CacheService<string, int>(
                _mockTimeProvider.Object,
                defaultExpiration: TimeSpan.FromMinutes(5));

            cache.Set("key1", 1);
            cache.Set("key2", 2, TimeSpan.FromMinutes(1)); // Short expiration

            _mockTimeProvider.Setup(t => t.UtcNow).Returns(now.AddMinutes(2));

            int evicted = cache.EvictExpired();

            Assert.Equal(1, evicted);
            Assert.Equal(1, cache.Count);
            Assert.True(cache.Contains("key1"));
        }

        #endregion

        #region Statistics Tests

        [Fact]
        public void HitRate_CalculatesCorrectly()
        {
            var cache = new CacheService<string, int>(_mockTimeProvider.Object);
            cache.Set("key1", 42);

            cache.TryGet("key1", out _); // hit
            cache.TryGet("key1", out _); // hit
            cache.TryGet("missing", out _); // miss

            Assert.Equal(2f / 3f, cache.HitRate, 2);
        }

        [Fact]
        public void HitRate_WhenNoAccesses_ReturnsZero()
        {
            var cache = new CacheService<string, int>(_mockTimeProvider.Object);

            Assert.Equal(0f, cache.HitRate);
        }

        #endregion

        #region Thread Safety Tests

        [Fact]
        public async Task ConcurrentAccess_IsThreadSafe()
        {
            var cache = new CacheService<string, int>(_mockTimeProvider.Object, maxSize: 100);
            var tasks = new Task[100];

            for (int i = 0; i < 100; i++)
            {
                int index = i;
                tasks[i] = Task.Run(() =>
                {
                    cache.Set($"key{index}", index);
                    cache.TryGet($"key{index}", out _);
                });
            }

            await Task.WhenAll(tasks);

            Assert.True(cache.Count <= 100);
        }

        [Fact]
        public async Task GetOrAdd_ConcurrentAccess_CallsFactoryOnlyOnce()
        {
            var cache = new CacheService<string, int>(_mockTimeProvider.Object);
            int factoryCalls = 0;
            var tasks = new Task<int>[10];

            for (int i = 0; i < 10; i++)
            {
                tasks[i] = Task.Run(() => cache.GetOrAdd("key1", () =>
                {
                    Interlocked.Increment(ref factoryCalls);
                    Thread.Sleep(10); // Simulate work
                    return 42;
                }));
            }

            var results = await Task.WhenAll(tasks);

            Assert.All(results, r => Assert.Equal(42, r));
            Assert.Equal(1, factoryCalls);
        }

        #endregion
    }
}
