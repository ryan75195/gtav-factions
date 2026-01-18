using System;

namespace FactionWars.Performance.Interfaces
{
    /// <summary>
    /// Interface for a generic object pool that manages reusable object instances.
    /// </summary>
    /// <typeparam name="T">The type of objects managed by the pool.</typeparam>
    public interface IObjectPool<T> where T : class
    {
        /// <summary>
        /// Gets the current number of objects in the pool.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Gets the maximum size of the pool.
        /// </summary>
        int MaxSize { get; }

        /// <summary>
        /// Gets the total number of objects created by this pool.
        /// </summary>
        int TotalCreated { get; }

        /// <summary>
        /// Gets the total number of objects returned to the pool.
        /// </summary>
        int TotalReturned { get; }

        /// <summary>
        /// Gets the number of cache hits (reused objects).
        /// </summary>
        int CacheHits { get; }

        /// <summary>
        /// Gets the number of cache misses (new objects created).
        /// </summary>
        int CacheMisses { get; }

        /// <summary>
        /// Gets the hit rate as a percentage (0.0 to 1.0).
        /// </summary>
        float HitRate { get; }

        /// <summary>
        /// Gets an object from the pool, creating a new one if the pool is empty.
        /// </summary>
        /// <returns>An object instance from the pool or a newly created one.</returns>
        T Get();

        /// <summary>
        /// Returns an object to the pool for reuse.
        /// </summary>
        /// <param name="item">The object to return to the pool.</param>
        /// <exception cref="ArgumentNullException">Thrown if item is null.</exception>
        void Return(T item);

        /// <summary>
        /// Clears all objects from the pool.
        /// Disposes of objects if they implement IDisposable.
        /// </summary>
        void Clear();
    }
}
