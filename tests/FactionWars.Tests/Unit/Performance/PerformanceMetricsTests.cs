using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using FactionWars.Performance.Models;
using FactionWars.Performance.Interfaces;
using FactionWars.Performance.Services;
using FactionWars.Core.Interfaces;
using Moq;

namespace FactionWars.Tests.Unit.Performance
{
    public class PerformanceMetricsTests
    {
        private readonly Mock<ITimeProvider> _mockTimeProvider;

        public PerformanceMetricsTests()
        {
            _mockTimeProvider = new Mock<ITimeProvider>();
            _mockTimeProvider.Setup(t => t.UtcNow).Returns(DateTime.UtcNow);
        }

        #region PerformanceMetric Model Tests

        [Fact]
        public void PerformanceMetric_Constructor_InitializesProperties()
        {
            var metric = new PerformanceMetric("TestOperation");

            Assert.Equal("TestOperation", metric.Name);
            Assert.Equal(0, metric.CallCount);
            Assert.Equal(0, metric.TotalMilliseconds);
            Assert.Equal(0, metric.AverageMilliseconds);
            Assert.Equal(0, metric.MinMilliseconds);
            Assert.Equal(0, metric.MaxMilliseconds);
        }

        [Fact]
        public void PerformanceMetric_Constructor_WithNullName_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new PerformanceMetric(null!));
        }

        [Fact]
        public void PerformanceMetric_Constructor_WithEmptyName_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new PerformanceMetric(""));
        }

        [Fact]
        public void PerformanceMetric_Record_UpdatesStatistics()
        {
            var metric = new PerformanceMetric("TestOperation");

            metric.Record(100);
            metric.Record(200);
            metric.Record(50);

            Assert.Equal(3, metric.CallCount);
            Assert.Equal(350, metric.TotalMilliseconds);
            Assert.Equal(116.67, metric.AverageMilliseconds, 2);
            Assert.Equal(50, metric.MinMilliseconds);
            Assert.Equal(200, metric.MaxMilliseconds);
        }

        [Fact]
        public void PerformanceMetric_Record_WithNegativeValue_ThrowsArgumentOutOfRangeException()
        {
            var metric = new PerformanceMetric("TestOperation");

            Assert.Throws<ArgumentOutOfRangeException>(() => metric.Record(-1));
        }

        [Fact]
        public void PerformanceMetric_Reset_ClearsStatistics()
        {
            var metric = new PerformanceMetric("TestOperation");
            metric.Record(100);
            metric.Record(200);

            metric.Reset();

            Assert.Equal(0, metric.CallCount);
            Assert.Equal(0, metric.TotalMilliseconds);
            Assert.Equal(0, metric.AverageMilliseconds);
        }

        #endregion

        #region PerformanceMonitor Service Tests

        [Fact]
        public void PerformanceMonitor_Constructor_CreatesMonitor()
        {
            var monitor = new PerformanceMonitor(_mockTimeProvider.Object);

            Assert.NotNull(monitor);
        }

        [Fact]
        public void PerformanceMonitor_Constructor_WithNullTimeProvider_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new PerformanceMonitor(null!));
        }

        [Fact]
        public void PerformanceMonitor_StartTiming_ReturnsTimingContext()
        {
            var monitor = new PerformanceMonitor(_mockTimeProvider.Object);

            using var context = monitor.StartTiming("TestOperation");

            Assert.NotNull(context);
        }

        [Fact]
        public void PerformanceMonitor_StartTiming_WithNullName_ThrowsArgumentNullException()
        {
            var monitor = new PerformanceMonitor(_mockTimeProvider.Object);

            Assert.Throws<ArgumentNullException>(() => monitor.StartTiming(null!));
        }

        [Fact]
        public void PerformanceMonitor_TimingContext_RecordsElapsedTime()
        {
            var now = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            _mockTimeProvider.Setup(t => t.UtcNow).Returns(now);

            var monitor = new PerformanceMonitor(_mockTimeProvider.Object);

            _mockTimeProvider.Setup(t => t.UtcNow).Returns(now);
            using (var context = monitor.StartTiming("TestOperation"))
            {
                _mockTimeProvider.Setup(t => t.UtcNow).Returns(now.AddMilliseconds(150));
            }

            var metric = monitor.GetMetric("TestOperation");
            Assert.NotNull(metric);
            Assert.Equal(1, metric.CallCount);
            Assert.Equal(150, metric.TotalMilliseconds, 1);
        }

        [Fact]
        public void PerformanceMonitor_GetMetric_ReturnsSameInstanceForSameName()
        {
            var monitor = new PerformanceMonitor(_mockTimeProvider.Object);
            monitor.Record("TestOperation", 100);

            var metric1 = monitor.GetMetric("TestOperation");
            var metric2 = monitor.GetMetric("TestOperation");

            Assert.Same(metric1, metric2);
        }

        [Fact]
        public void PerformanceMonitor_GetMetric_WithMissingName_ReturnsNull()
        {
            var monitor = new PerformanceMonitor(_mockTimeProvider.Object);

            var metric = monitor.GetMetric("NonExistent");

            Assert.Null(metric);
        }

        [Fact]
        public void PerformanceMonitor_Record_CreatesMetricIfMissing()
        {
            var monitor = new PerformanceMonitor(_mockTimeProvider.Object);

            monitor.Record("NewOperation", 100);

            var metric = monitor.GetMetric("NewOperation");
            Assert.NotNull(metric);
            Assert.Equal(1, metric.CallCount);
        }

        [Fact]
        public void PerformanceMonitor_GetAllMetrics_ReturnsAllRecordedMetrics()
        {
            var monitor = new PerformanceMonitor(_mockTimeProvider.Object);
            monitor.Record("Op1", 100);
            monitor.Record("Op2", 200);
            monitor.Record("Op3", 300);

            var metrics = monitor.GetAllMetrics();

            Assert.Equal(3, metrics.Count);
        }

        [Fact]
        public void PerformanceMonitor_Clear_RemovesAllMetrics()
        {
            var monitor = new PerformanceMonitor(_mockTimeProvider.Object);
            monitor.Record("Op1", 100);
            monitor.Record("Op2", 200);

            monitor.Clear();

            Assert.Empty(monitor.GetAllMetrics());
        }

        [Fact]
        public void PerformanceMonitor_GetSlowestOperations_ReturnsSortedByAverage()
        {
            var monitor = new PerformanceMonitor(_mockTimeProvider.Object);
            monitor.Record("Fast", 10);
            monitor.Record("Medium", 50);
            monitor.Record("Slow", 100);

            var slowest = monitor.GetSlowestOperations(2);

            Assert.Equal(2, slowest.Count);
            Assert.Equal("Slow", slowest[0].Name);
            Assert.Equal("Medium", slowest[1].Name);
        }

        [Fact]
        public void PerformanceMonitor_GetMostCalledOperations_ReturnsSortedByCallCount()
        {
            var monitor = new PerformanceMonitor(_mockTimeProvider.Object);
            monitor.Record("Rare", 10);
            monitor.Record("Common", 10);
            monitor.Record("Common", 10);
            monitor.Record("VeryCommon", 10);
            monitor.Record("VeryCommon", 10);
            monitor.Record("VeryCommon", 10);

            var mostCalled = monitor.GetMostCalledOperations(2);

            Assert.Equal(2, mostCalled.Count);
            Assert.Equal("VeryCommon", mostCalled[0].Name);
            Assert.Equal("Common", mostCalled[1].Name);
        }

        #endregion

        #region Thread Safety Tests

        [Fact]
        public async Task PerformanceMetric_Record_ConcurrentAccess_IsThreadSafe()
        {
            var metric = new PerformanceMetric("TestOperation");
            var tasks = new List<Task>();

            for (int i = 0; i < 100; i++)
            {
                tasks.Add(Task.Run(() =>
                {
                    for (int j = 0; j < 10; j++)
                    {
                        metric.Record(10);
                    }
                }));
            }

            await Task.WhenAll(tasks);

            Assert.Equal(1000, metric.CallCount);
            Assert.Equal(10000, metric.TotalMilliseconds);
        }

        [Fact]
        public async Task PerformanceMonitor_ConcurrentOperations_IsThreadSafe()
        {
            var monitor = new PerformanceMonitor(_mockTimeProvider.Object);
            var tasks = new List<Task>();

            for (int i = 0; i < 50; i++)
            {
                int index = i;
                tasks.Add(Task.Run(() =>
                {
                    for (int j = 0; j < 20; j++)
                    {
                        monitor.Record($"Operation{index % 5}", 10);
                    }
                }));
            }

            await Task.WhenAll(tasks);

            var metrics = monitor.GetAllMetrics();
            Assert.Equal(5, metrics.Count);

            int totalCalls = 0;
            foreach (var metric in metrics)
            {
                totalCalls += metric.CallCount;
            }
            Assert.Equal(1000, totalCalls);
        }

        #endregion

        #region IsEnabled Tests

        [Fact]
        public void PerformanceMonitor_IsEnabled_DefaultsToTrue()
        {
            var monitor = new PerformanceMonitor(_mockTimeProvider.Object);

            Assert.True(monitor.IsEnabled);
        }

        [Fact]
        public void PerformanceMonitor_WhenDisabled_SkipsRecording()
        {
            var monitor = new PerformanceMonitor(_mockTimeProvider.Object);
            monitor.IsEnabled = false;

            monitor.Record("TestOp", 100);

            Assert.Null(monitor.GetMetric("TestOp"));
        }

        [Fact]
        public void PerformanceMonitor_WhenDisabled_StartTimingReturnsNoOpContext()
        {
            var monitor = new PerformanceMonitor(_mockTimeProvider.Object);
            monitor.IsEnabled = false;

            using (var context = monitor.StartTiming("TestOp"))
            {
                // Context should be a no-op
            }

            Assert.Null(monitor.GetMetric("TestOp"));
        }

        #endregion
    }
}
