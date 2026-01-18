using System;

namespace FactionWars.Core.Interfaces
{
    /// <summary>
    /// Abstraction over system time for testability.
    /// Allows unit tests to control time-based behavior.
    /// </summary>
    public interface ITimeProvider
    {
        /// <summary>
        /// Gets the current UTC date and time.
        /// </summary>
        DateTime UtcNow { get; }

        /// <summary>
        /// Gets the current local date and time.
        /// </summary>
        DateTime Now { get; }
    }
}
