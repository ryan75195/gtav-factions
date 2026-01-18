using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using FactionWars.Core.Interfaces;
using FactionWars.Performance.Interfaces;

namespace FactionWars.Performance.Services
{
    /// <summary>
    /// A generic thread-safe caching service with expiration support.
    /// </summary>
    /// <typeparam name="TKey">The type of cache keys.</typeparam>
    /// <typeparam name="TValue">The type of cached values.</typeparam>
    public class CacheService<TKey, TValue> : ICacheService<TKey, TValue> where TKey : notnull
    {
        private readonly ConcurrentDictionary<TKey, CacheEntry> _cache;
        private readonly ITimeProvider _timeProvider;
        private readonly int _maxSize;
        private readonly TimeSpan _defaultExpiration;
        private readonly object _factoryLock = new object();
        private int _hits;
        private int _misses;

        /// <summary>
        /// Creates a new CacheService with the specified configuration.
        /// </summary>
        /// <param name="timeProvider">The time provider for expiration checks.</param>
        /// <param name="maxSize">The maximum number of entries in the cache.</param>
        /// <param name="defaultExpiration">The default expiration time for entries.</param>
        /// <exception cref="ArgumentNullException">Thrown if timeProvider is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if maxSize is less than 1.</exception>
        public CacheService(
            ITimeProvider timeProvider,
            int maxSize = 1000,
            TimeSpan? defaultExpiration = null)
        {
            _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

            if (maxSize < 1)
                throw new ArgumentOutOfRangeException(nameof(maxSize), "Max size must be at least 1.");

            _maxSize = maxSize;
            _defaultExpiration = defaultExpiration ?? TimeSpan.FromMinutes(5);
            _cache = new ConcurrentDictionary<TKey, CacheEntry>();
        }

        /// <inheritdoc />
        public int Count => _cache.Count;

        /// <inheritdoc />
        public int MaxSize => _maxSize;

        /// <inheritdoc />
        public TimeSpan DefaultExpiration => _defaultExpiration;

        /// <inheritdoc />
        public int Hits => _hits;

        /// <inheritdoc />
        public int Misses => _misses;

        /// <inheritdoc />
        public float HitRate
        {
            get
            {
                int total = _hits + _misses;
                return total > 0 ? (float)_hits / total : 0f;
            }
        }

        /// <inheritdoc />
        public void Set(TKey key, TValue value)
        {
            Set(key, value, _defaultExpiration);
        }

        /// <inheritdoc />
        public void Set(TKey key, TValue value, TimeSpan expiration)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            var expiresAt = _timeProvider.UtcNow.Add(expiration);
            var entry = new CacheEntry(value, expiresAt, _timeProvider.UtcNow);

            _cache.AddOrUpdate(key, entry, (k, old) => entry);

            // Evict oldest entries if we're over capacity
            while (_cache.Count > _maxSize)
            {
                EvictOldest();
            }
        }

        /// <inheritdoc />
        public bool TryGet(TKey key, out TValue value)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            if (_cache.TryGetValue(key, out var entry))
            {
                if (entry.ExpiresAt > _timeProvider.UtcNow)
                {
                    Interlocked.Increment(ref _hits);
                    value = entry.Value;
                    return true;
                }

                // Entry expired, remove it
                _cache.TryRemove(key, out _);
            }

            Interlocked.Increment(ref _misses);
            value = default!;
            return false;
        }

        /// <inheritdoc />
        public TValue GetOrAdd(TKey key, Func<TValue> factory)
        {
            return GetOrAdd(key, factory, _defaultExpiration);
        }

        /// <inheritdoc />
        public TValue GetOrAdd(TKey key, Func<TValue> factory, TimeSpan expiration)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            if (TryGet(key, out TValue existing))
            {
                return existing;
            }

            // Use lock to ensure factory is only called once per key
            lock (_factoryLock)
            {
                // Double-check after acquiring lock
                if (TryGet(key, out existing))
                {
                    // Compensate for the miss that was counted in the first TryGet
                    // but we actually found a value this time
                    return existing;
                }

                var value = factory();
                Set(key, value, expiration);
                return value;
            }
        }

        /// <inheritdoc />
        public bool Remove(TKey key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            return _cache.TryRemove(key, out _);
        }

        /// <inheritdoc />
        public bool Contains(TKey key)
        {
            if (key == null)
                return false;

            if (_cache.TryGetValue(key, out var entry))
            {
                if (entry.ExpiresAt > _timeProvider.UtcNow)
                {
                    return true;
                }

                // Entry expired, remove it
                _cache.TryRemove(key, out _);
            }

            return false;
        }

        /// <inheritdoc />
        public void Clear()
        {
            _cache.Clear();
            _hits = 0;
            _misses = 0;
        }

        /// <inheritdoc />
        public int EvictExpired()
        {
            var now = _timeProvider.UtcNow;
            var keysToRemove = _cache
                .Where(kvp => kvp.Value.ExpiresAt <= now)
                .Select(kvp => kvp.Key)
                .ToList();

            int evicted = 0;
            foreach (var key in keysToRemove)
            {
                if (_cache.TryRemove(key, out _))
                {
                    evicted++;
                }
            }

            return evicted;
        }

        private void EvictOldest()
        {
            var oldest = _cache
                .OrderBy(kvp => kvp.Value.CreatedAt)
                .Select(kvp => kvp.Key)
                .FirstOrDefault();

            if (oldest != null)
            {
                _cache.TryRemove(oldest, out _);
            }
        }

        private class CacheEntry
        {
            public TValue Value { get; }
            public DateTime ExpiresAt { get; }
            public DateTime CreatedAt { get; }

            public CacheEntry(TValue value, DateTime expiresAt, DateTime createdAt)
            {
                Value = value;
                ExpiresAt = expiresAt;
                CreatedAt = createdAt;
            }
        }
    }
}
