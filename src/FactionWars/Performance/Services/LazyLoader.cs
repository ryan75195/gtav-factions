using System;
using System.Threading;
using FactionWars.Performance.Interfaces;

namespace FactionWars.Performance.Services
{
    /// <summary>
    /// A thread-safe lazy loader for deferred resource initialization.
    /// </summary>
    /// <typeparam name="T">The type of resource to load.</typeparam>
    public class LazyLoader<T> : ILazyLoader<T> where T : class
    {
        private readonly Func<T> _factory;
        private readonly object _lock = new object();
        private T? _value;
        private volatile bool _isLoaded;

        /// <summary>
        /// Creates a new LazyLoader with the specified factory.
        /// </summary>
        /// <param name="factory">A factory function to create the value.</param>
        /// <exception cref="ArgumentNullException">Thrown if factory is null.</exception>
        public LazyLoader(Func<T> factory)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        /// <inheritdoc />
        public T Value
        {
            get
            {
                // Fast path - already loaded
                if (_isLoaded)
                {
                    return _value!;
                }

                lock (_lock)
                {
                    // Double-check after acquiring lock
                    if (_isLoaded)
                    {
                        return _value!;
                    }

                    _value = _factory();
                    _isLoaded = true;
                    return _value;
                }
            }
        }

        /// <inheritdoc />
        public bool IsLoaded => _isLoaded;

        /// <inheritdoc />
        public void Reset()
        {
            lock (_lock)
            {
                if (_isLoaded && _value is IDisposable disposable)
                {
                    disposable.Dispose();
                }

                _value = null;
                _isLoaded = false;
            }
        }

        /// <inheritdoc />
        public T? GetValueOrDefault()
        {
            return _isLoaded ? _value : null;
        }

        /// <inheritdoc />
        public bool TryGetValue(out T? value)
        {
            if (_isLoaded)
            {
                value = _value;
                return true;
            }

            value = null;
            return false;
        }
    }
}
