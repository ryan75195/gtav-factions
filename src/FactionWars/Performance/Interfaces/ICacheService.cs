using System;

namespace FactionWars.Performance.Interfaces
{
    /// <summary>
    /// Interface for a generic caching service with expiration support.
    /// </summary>
    /// <typeparam name="TKey">The type of cache keys.</typeparam>
    /// <typeparam name="TValue">The type of cached values.</typeparam>
    public interface ICacheService<TKey, TValue> where TKey : notnull
    {
        /// <summary>
        /// Gets the current number of items in the cache.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Gets the maximum size of the cache.
        /// </summary>
        int MaxSize { get; }

        /// <summary>
        /// Gets the default expiration time for cache entries.
        /// </summary>
        TimeSpan DefaultExpiration { get; }

        /// <summary>
        /// Gets the number of cache hits.
        /// </summary>
        int Hits { get; }

        /// <summary>
        /// Gets the number of cache misses.
        /// </summary>
        int Misses { get; }

        /// <summary>
        /// Gets the hit rate as a percentage (0.0 to 1.0).
        /// </summary>
        float HitRate { get; }

        /// <summary>
        /// Sets a value in the cache with the default expiration.
        /// </summary>
        /// <param name="key">The cache key.</param>
        /// <param name="value">The value to cache.</param>
        /// <exception cref="ArgumentNullException">Thrown if key is null.</exception>
        void Set(TKey key, TValue value);

        /// <summary>
        /// Sets a value in the cache with a custom expiration.
        /// </summary>
        /// <param name="key">The cache key.</param>
        /// <param name="value">The value to cache.</param>
        /// <param name="expiration">The expiration time for this entry.</param>
        /// <exception cref="ArgumentNullException">Thrown if key is null.</exception>
        void Set(TKey key, TValue value, TimeSpan expiration);

        /// <summary>
        /// Tries to get a value from the cache.
        /// </summary>
        /// <param name="key">The cache key.</param>
        /// <param name="value">The cached value if found.</param>
        /// <returns>True if the value was found and not expired; otherwise, false.</returns>
        /// <exception cref="ArgumentNullException">Thrown if key is null.</exception>
        bool TryGet(TKey key, out TValue value);

        /// <summary>
        /// Gets a value from the cache, or adds it using the factory if not present.
        /// </summary>
        /// <param name="key">The cache key.</param>
        /// <param name="factory">A factory function to create the value if not cached.</param>
        /// <returns>The cached or newly created value.</returns>
        /// <exception cref="ArgumentNullException">Thrown if key or factory is null.</exception>
        TValue GetOrAdd(TKey key, Func<TValue> factory);

        /// <summary>
        /// Gets a value from the cache, or adds it using the factory with custom expiration.
        /// </summary>
        /// <param name="key">The cache key.</param>
        /// <param name="factory">A factory function to create the value if not cached.</param>
        /// <param name="expiration">The expiration time for the new entry.</param>
        /// <returns>The cached or newly created value.</returns>
        /// <exception cref="ArgumentNullException">Thrown if key or factory is null.</exception>
        TValue GetOrAdd(TKey key, Func<TValue> factory, TimeSpan expiration);

        /// <summary>
        /// Removes an entry from the cache.
        /// </summary>
        /// <param name="key">The cache key.</param>
        /// <returns>True if the entry was removed; otherwise, false.</returns>
        /// <exception cref="ArgumentNullException">Thrown if key is null.</exception>
        bool Remove(TKey key);

        /// <summary>
        /// Checks if a key exists in the cache and is not expired.
        /// </summary>
        /// <param name="key">The cache key.</param>
        /// <returns>True if the key exists and is not expired; otherwise, false.</returns>
        bool Contains(TKey key);

        /// <summary>
        /// Clears all entries from the cache.
        /// </summary>
        void Clear();

        /// <summary>
        /// Evicts all expired entries from the cache.
        /// </summary>
        /// <returns>The number of entries evicted.</returns>
        int EvictExpired();
    }
}
