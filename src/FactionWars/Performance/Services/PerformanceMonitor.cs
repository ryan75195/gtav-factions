using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using FactionWars.Core.Interfaces;
using FactionWars.Performance.Interfaces;
using FactionWars.Performance.Models;

namespace FactionWars.Performance.Services
{
    /// <summary>
    /// A thread-safe performance monitoring service for tracking operation metrics.
    /// </summary>
    public class PerformanceMonitor : IPerformanceMonitor
    {
        private readonly ConcurrentDictionary<string, PerformanceMetric> _metrics;
        private readonly ITimeProvider _timeProvider;

        /// <summary>
        /// Creates a new PerformanceMonitor.
        /// </summary>
        /// <param name="timeProvider">The time provider for timing operations.</param>
        /// <exception cref="ArgumentNullException">Thrown if timeProvider is null.</exception>
        public PerformanceMonitor(ITimeProvider timeProvider)
        {
            _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
            _metrics = new ConcurrentDictionary<string, PerformanceMetric>();
            IsEnabled = true;
        }

        /// <inheritdoc />
        public bool IsEnabled { get; set; }

        /// <inheritdoc />
        public IDisposable StartTiming(string operationName)
        {
            if (operationName == null)
                throw new ArgumentNullException(nameof(operationName));

            if (!IsEnabled)
            {
                return NoOpTimingContext.Instance;
            }

            return new TimingContext(this, operationName, _timeProvider);
        }

        /// <inheritdoc />
        public void Record(string operationName, double milliseconds)
        {
            if (!IsEnabled)
                return;

            var metric = _metrics.GetOrAdd(operationName, name => new PerformanceMetric(name));
            metric.Record(milliseconds);
        }

        /// <inheritdoc />
        public PerformanceMetric? GetMetric(string operationName)
        {
            return _metrics.TryGetValue(operationName, out var metric) ? metric : null;
        }

        /// <inheritdoc />
        public IList<PerformanceMetric> GetAllMetrics()
        {
            return _metrics.Values.ToList();
        }

        /// <inheritdoc />
        public IList<PerformanceMetric> GetSlowestOperations(int count)
        {
            return _metrics.Values
                .OrderByDescending(m => m.AverageMilliseconds)
                .Take(count)
                .ToList();
        }

        /// <inheritdoc />
        public IList<PerformanceMetric> GetMostCalledOperations(int count)
        {
            return _metrics.Values
                .OrderByDescending(m => m.CallCount)
                .Take(count)
                .ToList();
        }

        /// <inheritdoc />
        public void Clear()
        {
            _metrics.Clear();
        }

        /// <summary>
        /// Context for timing an operation.
        /// </summary>
        private class TimingContext : IDisposable
        {
            private readonly IPerformanceMonitor _monitor;
            private readonly string _operationName;
            private readonly ITimeProvider _timeProvider;
            private readonly DateTime _startTime;
            private bool _disposed;

            public TimingContext(IPerformanceMonitor monitor, string operationName, ITimeProvider timeProvider)
            {
                _monitor = monitor;
                _operationName = operationName;
                _timeProvider = timeProvider;
                _startTime = timeProvider.UtcNow;
            }

            public void Dispose()
            {
                if (_disposed)
                    return;

                _disposed = true;
                var elapsed = (_timeProvider.UtcNow - _startTime).TotalMilliseconds;
                _monitor.Record(_operationName, elapsed);
            }
        }

        /// <summary>
        /// No-op timing context for when monitoring is disabled.
        /// </summary>
        private class NoOpTimingContext : IDisposable
        {
            public static readonly NoOpTimingContext Instance = new NoOpTimingContext();

            private NoOpTimingContext() { }

            public void Dispose() { }
        }
    }
}
