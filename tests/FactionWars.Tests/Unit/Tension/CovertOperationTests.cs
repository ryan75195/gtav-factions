using System;
using FactionWars.Tension.Models;
using Xunit;

namespace FactionWars.Tests.Unit.Tension
{
    /// <summary>
    /// Tests for the CovertOperation model class.
    /// </summary>
    public class CovertOperationTests
    {
        #region Constructor Tests

        [Fact]
        public void Constructor_WithValidParameters_CreatesOperation()
        {
            var operation = new CovertOperation(
                CovertOperationType.Sabotage,
                "faction1",
                "faction2",
                "zone1");

            Assert.Equal(CovertOperationType.Sabotage, operation.OperationType);
            Assert.Equal("faction1", operation.InitiatorFactionId);
            Assert.Equal("faction2", operation.TargetFactionId);
            Assert.Equal("zone1", operation.TargetZoneId);
        }

        [Fact]
        public void Constructor_WithNullInitiatorFactionId_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new CovertOperation(CovertOperationType.Sabotage, null!, "faction2", "zone1"));
        }

        [Fact]
        public void Constructor_WithEmptyInitiatorFactionId_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                new CovertOperation(CovertOperationType.Sabotage, "", "faction2", "zone1"));
        }

        [Fact]
        public void Constructor_WithWhitespaceInitiatorFactionId_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                new CovertOperation(CovertOperationType.Sabotage, "   ", "faction2", "zone1"));
        }

        [Fact]
        public void Constructor_WithNullTargetFactionId_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new CovertOperation(CovertOperationType.Sabotage, "faction1", null!, "zone1"));
        }

        [Fact]
        public void Constructor_WithEmptyTargetFactionId_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                new CovertOperation(CovertOperationType.Sabotage, "faction1", "", "zone1"));
        }

        [Fact]
        public void Constructor_WithWhitespaceTargetFactionId_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                new CovertOperation(CovertOperationType.Sabotage, "faction1", "   ", "zone1"));
        }

        [Fact]
        public void Constructor_WithSameFactionIds_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                new CovertOperation(CovertOperationType.Sabotage, "faction1", "faction1", "zone1"));
        }

        [Fact]
        public void Constructor_WithNullTargetZoneId_AllowsNull()
        {
            var operation = new CovertOperation(
                CovertOperationType.Assassination,
                "faction1",
                "faction2",
                null);

            Assert.Null(operation.TargetZoneId);
        }

        [Fact]
        public void Constructor_SetsStatusToPending()
        {
            var operation = new CovertOperation(
                CovertOperationType.Sabotage,
                "faction1",
                "faction2",
                "zone1");

            Assert.Equal(CovertOperationStatus.Pending, operation.Status);
        }

        [Fact]
        public void Constructor_GeneratesUniqueId()
        {
            var operation1 = new CovertOperation(CovertOperationType.Sabotage, "f1", "f2", "z1");
            var operation2 = new CovertOperation(CovertOperationType.Sabotage, "f1", "f2", "z1");

            Assert.NotEqual(operation1.Id, operation2.Id);
        }

        [Fact]
        public void Constructor_SetsCreatedTimeToNow()
        {
            var before = DateTime.UtcNow;
            var operation = new CovertOperation(CovertOperationType.Sabotage, "f1", "f2", "z1");
            var after = DateTime.UtcNow;

            Assert.InRange(operation.CreatedTime, before, after);
        }

        #endregion

        #region Operation Type Specific Tests

        [Theory]
        [InlineData(CovertOperationType.Sabotage, 5000)]
        [InlineData(CovertOperationType.Assassination, 15000)]
        [InlineData(CovertOperationType.Bribery, 10000)]
        public void BaseCost_ForOperationType_ReturnsExpectedValue(CovertOperationType type, int expectedCost)
        {
            var operation = new CovertOperation(type, "f1", "f2", "z1");
            Assert.Equal(expectedCost, operation.BaseCost);
        }

        [Theory]
        [InlineData(CovertOperationType.Sabotage, 60)]
        [InlineData(CovertOperationType.Assassination, 120)]
        [InlineData(CovertOperationType.Bribery, 90)]
        public void BaseDurationSeconds_ForOperationType_ReturnsExpectedValue(CovertOperationType type, int expectedDuration)
        {
            var operation = new CovertOperation(type, "f1", "f2", "z1");
            Assert.Equal(expectedDuration, operation.BaseDurationSeconds);
        }

        [Theory]
        [InlineData(CovertOperationType.Sabotage, 0.7f)]
        [InlineData(CovertOperationType.Assassination, 0.4f)]
        [InlineData(CovertOperationType.Bribery, 0.6f)]
        public void BaseSuccessChance_ForOperationType_ReturnsExpectedValue(CovertOperationType type, float expectedChance)
        {
            var operation = new CovertOperation(type, "f1", "f2", "z1");
            Assert.Equal(expectedChance, operation.BaseSuccessChance, 2);
        }

        [Theory]
        [InlineData(CovertOperationType.Sabotage, 0.3f)]
        [InlineData(CovertOperationType.Assassination, 0.5f)]
        [InlineData(CovertOperationType.Bribery, 0.2f)]
        public void BaseDetectionChance_ForOperationType_ReturnsExpectedValue(CovertOperationType type, float expectedChance)
        {
            var operation = new CovertOperation(type, "f1", "f2", "z1");
            Assert.Equal(expectedChance, operation.BaseDetectionChance, 2);
        }

        #endregion

        #region Status Transition Tests

        [Fact]
        public void Start_FromPending_TransitionsToInProgress()
        {
            var operation = new CovertOperation(CovertOperationType.Sabotage, "f1", "f2", "z1");

            operation.Start();

            Assert.Equal(CovertOperationStatus.InProgress, operation.Status);
        }

        [Fact]
        public void Start_SetsStartTime()
        {
            var operation = new CovertOperation(CovertOperationType.Sabotage, "f1", "f2", "z1");

            var before = DateTime.UtcNow;
            operation.Start();
            var after = DateTime.UtcNow;

            Assert.NotNull(operation.StartTime);
            Assert.InRange(operation.StartTime.Value, before, after);
        }

        [Fact]
        public void Start_WhenAlreadyStarted_ThrowsInvalidOperationException()
        {
            var operation = new CovertOperation(CovertOperationType.Sabotage, "f1", "f2", "z1");
            operation.Start();

            Assert.Throws<InvalidOperationException>(() => operation.Start());
        }

        [Fact]
        public void Start_WhenCompleted_ThrowsInvalidOperationException()
        {
            var operation = new CovertOperation(CovertOperationType.Sabotage, "f1", "f2", "z1");
            operation.Start();
            operation.Complete(true, false);

            Assert.Throws<InvalidOperationException>(() => operation.Start());
        }

        [Fact]
        public void Complete_WithSuccess_SetsStatusToSucceeded()
        {
            var operation = new CovertOperation(CovertOperationType.Sabotage, "f1", "f2", "z1");
            operation.Start();

            operation.Complete(success: true, detected: false);

            Assert.Equal(CovertOperationStatus.Succeeded, operation.Status);
        }

        [Fact]
        public void Complete_WithFailure_SetsStatusToFailed()
        {
            var operation = new CovertOperation(CovertOperationType.Sabotage, "f1", "f2", "z1");
            operation.Start();

            operation.Complete(success: false, detected: false);

            Assert.Equal(CovertOperationStatus.Failed, operation.Status);
        }

        [Fact]
        public void Complete_WithDetection_SetsStatusToDetected()
        {
            var operation = new CovertOperation(CovertOperationType.Sabotage, "f1", "f2", "z1");
            operation.Start();

            operation.Complete(success: false, detected: true);

            Assert.Equal(CovertOperationStatus.Detected, operation.Status);
        }

        [Fact]
        public void Complete_WithSuccessAndDetection_SetsStatusToDetected()
        {
            // Detection takes precedence even if successful
            var operation = new CovertOperation(CovertOperationType.Sabotage, "f1", "f2", "z1");
            operation.Start();

            operation.Complete(success: true, detected: true);

            Assert.Equal(CovertOperationStatus.Detected, operation.Status);
            Assert.True(operation.WasSuccessful);
        }

        [Fact]
        public void Complete_SetsCompletionTime()
        {
            var operation = new CovertOperation(CovertOperationType.Sabotage, "f1", "f2", "z1");
            operation.Start();

            var before = DateTime.UtcNow;
            operation.Complete(true, false);
            var after = DateTime.UtcNow;

            Assert.NotNull(operation.CompletionTime);
            Assert.InRange(operation.CompletionTime.Value, before, after);
        }

        [Fact]
        public void Complete_WhenPending_ThrowsInvalidOperationException()
        {
            var operation = new CovertOperation(CovertOperationType.Sabotage, "f1", "f2", "z1");

            Assert.Throws<InvalidOperationException>(() => operation.Complete(true, false));
        }

        [Fact]
        public void Complete_WhenAlreadyCompleted_ThrowsInvalidOperationException()
        {
            var operation = new CovertOperation(CovertOperationType.Sabotage, "f1", "f2", "z1");
            operation.Start();
            operation.Complete(true, false);

            Assert.Throws<InvalidOperationException>(() => operation.Complete(true, false));
        }

        [Fact]
        public void Cancel_FromPending_SetsStatusToCancelled()
        {
            var operation = new CovertOperation(CovertOperationType.Sabotage, "f1", "f2", "z1");

            operation.Cancel();

            Assert.Equal(CovertOperationStatus.Cancelled, operation.Status);
        }

        [Fact]
        public void Cancel_FromInProgress_SetsStatusToCancelled()
        {
            var operation = new CovertOperation(CovertOperationType.Sabotage, "f1", "f2", "z1");
            operation.Start();

            operation.Cancel();

            Assert.Equal(CovertOperationStatus.Cancelled, operation.Status);
        }

        [Fact]
        public void Cancel_WhenCompleted_ThrowsInvalidOperationException()
        {
            var operation = new CovertOperation(CovertOperationType.Sabotage, "f1", "f2", "z1");
            operation.Start();
            operation.Complete(true, false);

            Assert.Throws<InvalidOperationException>(() => operation.Cancel());
        }

        #endregion

        #region State Query Tests

        [Fact]
        public void IsTerminal_WhenPending_ReturnsFalse()
        {
            var operation = new CovertOperation(CovertOperationType.Sabotage, "f1", "f2", "z1");
            Assert.False(operation.IsTerminal);
        }

        [Fact]
        public void IsTerminal_WhenInProgress_ReturnsFalse()
        {
            var operation = new CovertOperation(CovertOperationType.Sabotage, "f1", "f2", "z1");
            operation.Start();
            Assert.False(operation.IsTerminal);
        }

        [Fact]
        public void IsTerminal_WhenSucceeded_ReturnsTrue()
        {
            var operation = new CovertOperation(CovertOperationType.Sabotage, "f1", "f2", "z1");
            operation.Start();
            operation.Complete(true, false);
            Assert.True(operation.IsTerminal);
        }

        [Fact]
        public void IsTerminal_WhenFailed_ReturnsTrue()
        {
            var operation = new CovertOperation(CovertOperationType.Sabotage, "f1", "f2", "z1");
            operation.Start();
            operation.Complete(false, false);
            Assert.True(operation.IsTerminal);
        }

        [Fact]
        public void IsTerminal_WhenDetected_ReturnsTrue()
        {
            var operation = new CovertOperation(CovertOperationType.Sabotage, "f1", "f2", "z1");
            operation.Start();
            operation.Complete(false, true);
            Assert.True(operation.IsTerminal);
        }

        [Fact]
        public void IsTerminal_WhenCancelled_ReturnsTrue()
        {
            var operation = new CovertOperation(CovertOperationType.Sabotage, "f1", "f2", "z1");
            operation.Cancel();
            Assert.True(operation.IsTerminal);
        }

        [Fact]
        public void WasSuccessful_WhenSucceeded_ReturnsTrue()
        {
            var operation = new CovertOperation(CovertOperationType.Sabotage, "f1", "f2", "z1");
            operation.Start();
            operation.Complete(true, false);
            Assert.True(operation.WasSuccessful);
        }

        [Fact]
        public void WasSuccessful_WhenSucceededButDetected_ReturnsTrue()
        {
            var operation = new CovertOperation(CovertOperationType.Sabotage, "f1", "f2", "z1");
            operation.Start();
            operation.Complete(true, true);
            Assert.True(operation.WasSuccessful);
        }

        [Fact]
        public void WasSuccessful_WhenFailed_ReturnsFalse()
        {
            var operation = new CovertOperation(CovertOperationType.Sabotage, "f1", "f2", "z1");
            operation.Start();
            operation.Complete(false, false);
            Assert.False(operation.WasSuccessful);
        }

        [Fact]
        public void WasSuccessful_WhenPending_ReturnsFalse()
        {
            var operation = new CovertOperation(CovertOperationType.Sabotage, "f1", "f2", "z1");
            Assert.False(operation.WasSuccessful);
        }

        [Fact]
        public void WasDetected_WhenDetected_ReturnsTrue()
        {
            var operation = new CovertOperation(CovertOperationType.Sabotage, "f1", "f2", "z1");
            operation.Start();
            operation.Complete(false, true);
            Assert.True(operation.WasDetected);
        }

        [Fact]
        public void WasDetected_WhenNotDetected_ReturnsFalse()
        {
            var operation = new CovertOperation(CovertOperationType.Sabotage, "f1", "f2", "z1");
            operation.Start();
            operation.Complete(true, false);
            Assert.False(operation.WasDetected);
        }

        #endregion

        #region Faction Query Tests

        [Fact]
        public void InvolvesFaction_WithInitiator_ReturnsTrue()
        {
            var operation = new CovertOperation(CovertOperationType.Sabotage, "f1", "f2", "z1");
            Assert.True(operation.InvolvesFaction("f1"));
        }

        [Fact]
        public void InvolvesFaction_WithTarget_ReturnsTrue()
        {
            var operation = new CovertOperation(CovertOperationType.Sabotage, "f1", "f2", "z1");
            Assert.True(operation.InvolvesFaction("f2"));
        }

        [Fact]
        public void InvolvesFaction_WithUnrelatedFaction_ReturnsFalse()
        {
            var operation = new CovertOperation(CovertOperationType.Sabotage, "f1", "f2", "z1");
            Assert.False(operation.InvolvesFaction("f3"));
        }

        [Fact]
        public void InvolvesFaction_WithNull_ReturnsFalse()
        {
            var operation = new CovertOperation(CovertOperationType.Sabotage, "f1", "f2", "z1");
            Assert.False(operation.InvolvesFaction(null!));
        }

        [Fact]
        public void InvolvesFaction_WithEmpty_ReturnsFalse()
        {
            var operation = new CovertOperation(CovertOperationType.Sabotage, "f1", "f2", "z1");
            Assert.False(operation.InvolvesFaction(""));
        }

        #endregion

        #region ToString Tests

        [Fact]
        public void ToString_ContainsOperationType()
        {
            var operation = new CovertOperation(CovertOperationType.Sabotage, "f1", "f2", "z1");
            Assert.Contains("Sabotage", operation.ToString());
        }

        [Fact]
        public void ToString_ContainsFactionIds()
        {
            var operation = new CovertOperation(CovertOperationType.Sabotage, "faction1", "faction2", "zone1");
            var result = operation.ToString();
            Assert.Contains("faction1", result);
            Assert.Contains("faction2", result);
        }

        [Fact]
        public void ToString_ContainsStatus()
        {
            var operation = new CovertOperation(CovertOperationType.Sabotage, "f1", "f2", "z1");
            Assert.Contains("Pending", operation.ToString());
        }

        #endregion
    }
}
