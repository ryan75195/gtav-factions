using FactionWars.Lieutenants.Models;
using Xunit;

namespace FactionWars.Tests.Unit.Lieutenants
{
    /// <summary>
    /// Tests for the FlipMissionStatus enum.
    /// </summary>
    public class FlipMissionStatusTests
    {
        [Fact]
        public void FlipMissionStatus_HasPendingValue()
        {
            // Assert
            Assert.Equal(0, (int)FlipMissionStatus.Pending);
        }

        [Fact]
        public void FlipMissionStatus_HasInProgressValue()
        {
            // Assert
            Assert.Equal(1, (int)FlipMissionStatus.InProgress);
        }

        [Fact]
        public void FlipMissionStatus_HasSucceededValue()
        {
            // Assert
            Assert.Equal(2, (int)FlipMissionStatus.Succeeded);
        }

        [Fact]
        public void FlipMissionStatus_HasFailedValue()
        {
            // Assert
            Assert.Equal(3, (int)FlipMissionStatus.Failed);
        }

        [Fact]
        public void FlipMissionStatus_HasCancelledValue()
        {
            // Assert
            Assert.Equal(4, (int)FlipMissionStatus.Cancelled);
        }

        [Fact]
        public void FlipMissionStatus_HasDetectedValue()
        {
            // Assert
            Assert.Equal(5, (int)FlipMissionStatus.Detected);
        }
    }
}
