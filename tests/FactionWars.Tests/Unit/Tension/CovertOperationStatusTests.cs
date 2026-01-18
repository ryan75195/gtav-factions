using FactionWars.Tension.Models;
using Xunit;

namespace FactionWars.Tests.Unit.Tension
{
    /// <summary>
    /// Tests for the CovertOperationStatus enum.
    /// </summary>
    public class CovertOperationStatusTests
    {
        [Fact]
        public void CovertOperationStatus_Pending_HasExpectedValue()
        {
            Assert.Equal(0, (int)CovertOperationStatus.Pending);
        }

        [Fact]
        public void CovertOperationStatus_InProgress_HasExpectedValue()
        {
            Assert.Equal(1, (int)CovertOperationStatus.InProgress);
        }

        [Fact]
        public void CovertOperationStatus_Succeeded_HasExpectedValue()
        {
            Assert.Equal(2, (int)CovertOperationStatus.Succeeded);
        }

        [Fact]
        public void CovertOperationStatus_Failed_HasExpectedValue()
        {
            Assert.Equal(3, (int)CovertOperationStatus.Failed);
        }

        [Fact]
        public void CovertOperationStatus_Detected_HasExpectedValue()
        {
            Assert.Equal(4, (int)CovertOperationStatus.Detected);
        }

        [Fact]
        public void CovertOperationStatus_Cancelled_HasExpectedValue()
        {
            Assert.Equal(5, (int)CovertOperationStatus.Cancelled);
        }

        [Fact]
        public void CovertOperationStatus_HasExactlySixValues()
        {
            var values = System.Enum.GetValues(typeof(CovertOperationStatus));
            Assert.Equal(6, values.Length);
        }

        [Theory]
        [InlineData(CovertOperationStatus.Pending, false)]
        [InlineData(CovertOperationStatus.InProgress, false)]
        [InlineData(CovertOperationStatus.Succeeded, true)]
        [InlineData(CovertOperationStatus.Failed, true)]
        [InlineData(CovertOperationStatus.Detected, true)]
        [InlineData(CovertOperationStatus.Cancelled, true)]
        public void CovertOperationStatus_IsTerminalStatus_ReturnsCorrectValue(CovertOperationStatus status, bool expected)
        {
            // Terminal statuses are those that represent a completed operation
            bool isTerminal = status == CovertOperationStatus.Succeeded ||
                             status == CovertOperationStatus.Failed ||
                             status == CovertOperationStatus.Detected ||
                             status == CovertOperationStatus.Cancelled;
            Assert.Equal(expected, isTerminal);
        }
    }
}
