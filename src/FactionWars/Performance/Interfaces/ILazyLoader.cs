namespace FactionWars.Performance.Interfaces
{
    /// <summary>
    /// Interface for lazy loading resources with deferred initialization.
    /// </summary>
    /// <typeparam name="T">The type of resource to load.</typeparam>
    public interface ILazyLoader<T> where T : class
    {
        /// <summary>
        /// Gets the loaded value, initializing it if necessary.
        /// </summary>
        T Value { get; }

        /// <summary>
        /// Gets whether the value has been loaded.
        /// </summary>
        bool IsLoaded { get; }

        /// <summary>
        /// Resets the loader, disposing the current value if applicable.
        /// The next access to Value will trigger re-initialization.
        /// </summary>
        void Reset();

        /// <summary>
        /// Gets the value if loaded, without triggering initialization.
        /// </summary>
        /// <returns>The loaded value, or null if not loaded.</returns>
        T? GetValueOrDefault();

        /// <summary>
        /// Tries to get the value without triggering initialization.
        /// </summary>
        /// <param name="value">The loaded value if available.</param>
        /// <returns>True if the value is loaded; otherwise, false.</returns>
        bool TryGetValue(out T? value);
    }
}
