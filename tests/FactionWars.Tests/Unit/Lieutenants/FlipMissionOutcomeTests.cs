using FactionWars.Lieutenants.Models;
using Xunit;

namespace FactionWars.Tests.Unit.Lieutenants
{
    /// <summary>
    /// Tests for the FlipMissionOutcome class.
    /// </summary>
    public class FlipMissionOutcomeTests
    {
        #region Factory Methods

        [Fact]
        public void Succeeded_SetsSuccessToTrue()
        {
            // Act
            var outcome = FlipMissionOutcome.Succeeded(false);

            // Assert
            Assert.True(outcome.Success);
        }

        [Fact]
        public void Succeeded_WithDetected_SetsDetectedToTrue()
        {
            // Act
            var outcome = FlipMissionOutcome.Succeeded(detected: true);

            // Assert
            Assert.True(outcome.Detected);
        }

        [Fact]
        public void Succeeded_WithoutDetection_SetsDetectedToFalse()
        {
            // Act
            var outcome = FlipMissionOutcome.Succeeded(detected: false);

            // Assert
            Assert.False(outcome.Detected);
        }

        [Fact]
        public void Succeeded_SetsFailureReasonToNull()
        {
            // Act
            var outcome = FlipMissionOutcome.Succeeded(false);

            // Assert
            Assert.Null(outcome.FailureReason);
        }

        [Fact]
        public void Failed_SetsSuccessToFalse()
        {
            // Act
            var outcome = FlipMissionOutcome.Failed(false, "Test reason");

            // Assert
            Assert.False(outcome.Success);
        }

        [Fact]
        public void Failed_WithDetected_SetsDetectedToTrue()
        {
            // Act
            var outcome = FlipMissionOutcome.Failed(detected: true, "Test reason");

            // Assert
            Assert.True(outcome.Detected);
        }

        [Fact]
        public void Failed_SetsFailureReason()
        {
            // Act
            var outcome = FlipMissionOutcome.Failed(false, "Lieutenant refused offer");

            // Assert
            Assert.Equal("Lieutenant refused offer", outcome.FailureReason);
        }

        [Fact]
        public void Failed_WithNullReason_SetsReasonToNull()
        {
            // Act
            var outcome = FlipMissionOutcome.Failed(false, null);

            // Assert
            Assert.Null(outcome.FailureReason);
        }

        #endregion

        #region Property Combinations

        [Fact]
        public void Success_True_Detected_False_IsCleanSuccess()
        {
            // Act
            var outcome = FlipMissionOutcome.Succeeded(detected: false);

            // Assert
            Assert.True(outcome.Success);
            Assert.False(outcome.Detected);
        }

        [Fact]
        public void Success_True_Detected_True_IsCompromisedSuccess()
        {
            // Act
            var outcome = FlipMissionOutcome.Succeeded(detected: true);

            // Assert
            Assert.True(outcome.Success);
            Assert.True(outcome.Detected);
        }

        [Fact]
        public void Success_False_Detected_False_IsSilentFailure()
        {
            // Act
            var outcome = FlipMissionOutcome.Failed(detected: false, "Test");

            // Assert
            Assert.False(outcome.Success);
            Assert.False(outcome.Detected);
        }

        [Fact]
        public void Success_False_Detected_True_IsDetectedFailure()
        {
            // Act
            var outcome = FlipMissionOutcome.Failed(detected: true, "Test");

            // Assert
            Assert.False(outcome.Success);
            Assert.True(outcome.Detected);
        }

        #endregion

        #region ToString

        [Fact]
        public void ToString_WhenSuccessNotDetected_ContainsSuccess()
        {
            // Arrange
            var outcome = FlipMissionOutcome.Succeeded(false);

            // Act
            var result = outcome.ToString();

            // Assert
            Assert.Contains("Success", result);
        }

        [Fact]
        public void ToString_WhenDetected_ContainsDetected()
        {
            // Arrange
            var outcome = FlipMissionOutcome.Failed(detected: true, "Test");

            // Act
            var result = outcome.ToString();

            // Assert
            Assert.Contains("Detected", result);
        }

        [Fact]
        public void ToString_WhenFailed_ContainsFailed()
        {
            // Arrange
            var outcome = FlipMissionOutcome.Failed(detected: false, "Test");

            // Act
            var result = outcome.ToString();

            // Assert
            Assert.Contains("Failed", result);
        }

        #endregion
    }
}
