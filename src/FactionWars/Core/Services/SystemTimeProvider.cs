using System;
using FactionWars.Core.Interfaces;

namespace FactionWars.Core.Services
{
    /// <summary>
    /// Default implementation of ITimeProvider that uses system time.
    /// For use in production code where real time is needed.
    /// </summary>
    public class SystemTimeProvider : ITimeProvider
    {
        /// <inheritdoc />
        public DateTime UtcNow => DateTime.UtcNow;

        /// <inheritdoc />
        public DateTime Now => DateTime.Now;
    }
}
