using System;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Services;
using Xunit;

namespace FactionWars.Tests.Unit.Core
{
    public class TimeProviderTests
    {
        [Fact]
        public void UtcNow_ShouldReturnCurrentUtcTime()
        {
            // Arrange
            var timeProvider = new SystemTimeProvider();
            var before = DateTime.UtcNow;

            // Act
            var result = timeProvider.UtcNow;

            // Assert
            var after = DateTime.UtcNow;
            Assert.True(result >= before && result <= after);
        }

        [Fact]
        public void Now_ShouldReturnCurrentLocalTime()
        {
            // Arrange
            var timeProvider = new SystemTimeProvider();
            var before = DateTime.Now;

            // Act
            var result = timeProvider.Now;

            // Assert
            var after = DateTime.Now;
            Assert.True(result >= before && result <= after);
        }

        [Fact]
        public void SystemTimeProvider_ShouldImplementITimeProvider()
        {
            // Arrange & Act
            var timeProvider = new SystemTimeProvider();

            // Assert
            Assert.IsAssignableFrom<ITimeProvider>(timeProvider);
        }
    }
}
