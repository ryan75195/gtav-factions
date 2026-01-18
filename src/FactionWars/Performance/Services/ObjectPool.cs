using System;
using System.Collections.Concurrent;
using System.Threading;
using FactionWars.Performance.Interfaces;

namespace FactionWars.Performance.Services
{
    /// <summary>
    /// A generic thread-safe object pool for reusing object instances.
    /// </summary>
    /// <typeparam name="T">The type of objects to pool.</typeparam>
    public class ObjectPool<T> : IObjectPool<T> where T : class
    {
        private readonly ConcurrentStack<T> _pool;
        private readonly Func<T> _factory;
        private readonly Action<T>? _resetAction;
        private readonly int _maxSize;
        private int _poolCount;
        private int _totalCreated;
        private int _totalReturned;
        private int _cacheHits;
        private int _cacheMisses;

        /// <summary>
        /// Creates a new ObjectPool with the specified factory and optional configuration.
        /// </summary>
        /// <param name="factory">A factory function to create new instances.</param>
        /// <param name="maxSize">The maximum number of objects to keep in the pool.</param>
        /// <param name="resetAction">An optional action to reset objects when returned to the pool.</param>
        /// <exception cref="ArgumentNullException">Thrown if factory is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if maxSize is less than 1.</exception>
        public ObjectPool(Func<T> factory, int maxSize = 100, Action<T>? resetAction = null)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));

            if (maxSize < 1)
                throw new ArgumentOutOfRangeException(nameof(maxSize), "Max size must be at least 1.");

            _maxSize = maxSize;
            _resetAction = resetAction;
            _pool = new ConcurrentStack<T>();
            _poolCount = 0;
        }

        /// <inheritdoc />
        public int Count => _poolCount;

        /// <inheritdoc />
        public int MaxSize => _maxSize;

        /// <inheritdoc />
        public int TotalCreated => _totalCreated;

        /// <inheritdoc />
        public int TotalReturned => _totalReturned;

        /// <inheritdoc />
        public int CacheHits => _cacheHits;

        /// <inheritdoc />
        public int CacheMisses => _cacheMisses;

        /// <inheritdoc />
        public float HitRate
        {
            get
            {
                int total = _cacheHits + _cacheMisses;
                return total > 0 ? (float)_cacheHits / total : 0f;
            }
        }

        /// <inheritdoc />
        public T Get()
        {
            if (_pool.TryPop(out T? item))
            {
                Interlocked.Decrement(ref _poolCount);
                Interlocked.Increment(ref _cacheHits);
                _resetAction?.Invoke(item);
                return item;
            }

            Interlocked.Increment(ref _cacheMisses);
            Interlocked.Increment(ref _totalCreated);
            return _factory();
        }

        /// <inheritdoc />
        public void Return(T item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            Interlocked.Increment(ref _totalReturned);

            // Use compare-exchange to atomically check and increment pool count
            int currentCount;
            int newCount;
            do
            {
                currentCount = _poolCount;
                if (currentCount >= _maxSize)
                {
                    // Pool is full, dispose if applicable
                    (item as IDisposable)?.Dispose();
                    return;
                }
                newCount = currentCount + 1;
            } while (Interlocked.CompareExchange(ref _poolCount, newCount, currentCount) != currentCount);

            // Successfully reserved a slot, add to pool
            _resetAction?.Invoke(item);
            _pool.Push(item);
        }

        /// <inheritdoc />
        public void Clear()
        {
            while (_pool.TryPop(out T? item))
            {
                Interlocked.Decrement(ref _poolCount);
                (item as IDisposable)?.Dispose();
            }
        }
    }
}
