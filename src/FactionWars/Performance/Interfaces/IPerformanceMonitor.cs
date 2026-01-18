using System;
using System.Collections.Generic;
using FactionWars.Performance.Models;

namespace FactionWars.Performance.Interfaces
{
    /// <summary>
    /// Interface for monitoring performance metrics.
    /// </summary>
    public interface IPerformanceMonitor
    {
        /// <summary>
        /// Gets or sets whether performance monitoring is enabled.
        /// When disabled, all operations become no-ops.
        /// </summary>
        bool IsEnabled { get; set; }

        /// <summary>
        /// Starts timing an operation.
        /// </summary>
        /// <param name="operationName">The name of the operation to time.</param>
        /// <returns>A disposable context that records the elapsed time when disposed.</returns>
        /// <exception cref="ArgumentNullException">Thrown if operationName is null.</exception>
        IDisposable StartTiming(string operationName);

        /// <summary>
        /// Records a timing measurement directly.
        /// </summary>
        /// <param name="operationName">The name of the operation.</param>
        /// <param name="milliseconds">The elapsed time in milliseconds.</param>
        void Record(string operationName, double milliseconds);

        /// <summary>
        /// Gets the metric for a specific operation.
        /// </summary>
        /// <param name="operationName">The name of the operation.</param>
        /// <returns>The performance metric, or null if not found.</returns>
        PerformanceMetric? GetMetric(string operationName);

        /// <summary>
        /// Gets all recorded metrics.
        /// </summary>
        /// <returns>A list of all performance metrics.</returns>
        IList<PerformanceMetric> GetAllMetrics();

        /// <summary>
        /// Gets the slowest operations by average time.
        /// </summary>
        /// <param name="count">The number of operations to return.</param>
        /// <returns>A list of the slowest operations.</returns>
        IList<PerformanceMetric> GetSlowestOperations(int count);

        /// <summary>
        /// Gets the most frequently called operations.
        /// </summary>
        /// <param name="count">The number of operations to return.</param>
        /// <returns>A list of the most called operations.</returns>
        IList<PerformanceMetric> GetMostCalledOperations(int count);

        /// <summary>
        /// Clears all recorded metrics.
        /// </summary>
        void Clear();
    }
}
