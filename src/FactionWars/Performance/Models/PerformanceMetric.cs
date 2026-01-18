using System;
using System.Threading;

namespace FactionWars.Performance.Models
{
    /// <summary>
    /// Represents performance metrics for a specific operation.
    /// Thread-safe for concurrent updates.
    /// </summary>
    public class PerformanceMetric
    {
        private readonly object _lock = new object();
        private int _callCount;
        private double _totalMilliseconds;
        private double _minMilliseconds;
        private double _maxMilliseconds;
        private bool _hasRecordings;

        /// <summary>
        /// Gets the name of the operation being measured.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the total number of times this operation was called.
        /// </summary>
        public int CallCount => _callCount;

        /// <summary>
        /// Gets the total time spent in this operation across all calls.
        /// </summary>
        public double TotalMilliseconds => _totalMilliseconds;

        /// <summary>
        /// Gets the average time per call.
        /// </summary>
        public double AverageMilliseconds => _callCount > 0 ? _totalMilliseconds / _callCount : 0;

        /// <summary>
        /// Gets the minimum recorded time for this operation.
        /// </summary>
        public double MinMilliseconds => _hasRecordings ? _minMilliseconds : 0;

        /// <summary>
        /// Gets the maximum recorded time for this operation.
        /// </summary>
        public double MaxMilliseconds => _maxMilliseconds;

        /// <summary>
        /// Creates a new PerformanceMetric for the specified operation.
        /// </summary>
        /// <param name="name">The name of the operation.</param>
        /// <exception cref="ArgumentNullException">Thrown if name is null.</exception>
        /// <exception cref="ArgumentException">Thrown if name is empty or whitespace.</exception>
        public PerformanceMetric(string name)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name cannot be empty or whitespace.", nameof(name));

            Name = name;
            _minMilliseconds = double.MaxValue;
        }

        /// <summary>
        /// Records a timing measurement.
        /// </summary>
        /// <param name="milliseconds">The elapsed time in milliseconds.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if milliseconds is negative.</exception>
        public void Record(double milliseconds)
        {
            if (milliseconds < 0)
                throw new ArgumentOutOfRangeException(nameof(milliseconds), "Milliseconds cannot be negative.");

            lock (_lock)
            {
                _callCount++;
                _totalMilliseconds += milliseconds;
                _hasRecordings = true;

                if (milliseconds < _minMilliseconds)
                    _minMilliseconds = milliseconds;

                if (milliseconds > _maxMilliseconds)
                    _maxMilliseconds = milliseconds;
            }
        }

        /// <summary>
        /// Resets all statistics for this metric.
        /// </summary>
        public void Reset()
        {
            lock (_lock)
            {
                _callCount = 0;
                _totalMilliseconds = 0;
                _minMilliseconds = double.MaxValue;
                _maxMilliseconds = 0;
                _hasRecordings = false;
            }
        }
    }
}
