using System;
using FactionWars.Lieutenants.Models;
using Xunit;

namespace FactionWars.Tests.Unit.Lieutenants
{
    /// <summary>
    /// Tests for the FlipMission model class.
    /// </summary>
    public class FlipMissionTests
    {
        #region Constructor Validation

        [Fact]
        public void Constructor_WithNullTargetLieutenant_ThrowsArgumentNullException()
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentNullException>(() =>
                new FlipMission(null!, "faction_player"));
            Assert.Equal("targetLieutenant", ex.ParamName);
        }

        [Fact]
        public void Constructor_WithNullInitiatorFactionId_ThrowsArgumentNullException()
        {
            // Arrange
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_enemy");

            // Act & Assert
            var ex = Assert.Throws<ArgumentNullException>(() =>
                new FlipMission(lieutenant, null!));
            Assert.Equal("initiatorFactionId", ex.ParamName);
        }

        [Fact]
        public void Constructor_WithEmptyInitiatorFactionId_ThrowsArgumentException()
        {
            // Arrange
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_enemy");

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() =>
                new FlipMission(lieutenant, ""));
            Assert.Equal("initiatorFactionId", ex.ParamName);
        }

        [Fact]
        public void Constructor_WithWhitespaceInitiatorFactionId_ThrowsArgumentException()
        {
            // Arrange
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_enemy");

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() =>
                new FlipMission(lieutenant, "   "));
            Assert.Equal("initiatorFactionId", ex.ParamName);
        }

        [Fact]
        public void Constructor_InitiatorSameAsFaction_ThrowsArgumentException()
        {
            // Arrange
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_michael");

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() =>
                new FlipMission(lieutenant, "faction_michael"));
            Assert.Contains("own faction", ex.Message);
        }

        [Fact]
        public void Constructor_WithDeceasedLieutenant_ThrowsArgumentException()
        {
            // Arrange
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_enemy");
            lieutenant.Kill();

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() =>
                new FlipMission(lieutenant, "faction_player"));
            Assert.Contains("deceased", ex.Message.ToLower());
        }

        #endregion

        #region Constructor Success

        [Fact]
        public void Constructor_WithValidParameters_SetsTargetLieutenantId()
        {
            // Arrange
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_enemy");

            // Act
            var mission = new FlipMission(lieutenant, "faction_player");

            // Assert
            Assert.Equal("lt_001", mission.TargetLieutenantId);
        }

        [Fact]
        public void Constructor_WithValidParameters_SetsInitiatorFactionId()
        {
            // Arrange
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_enemy");

            // Act
            var mission = new FlipMission(lieutenant, "faction_player");

            // Assert
            Assert.Equal("faction_player", mission.InitiatorFactionId);
        }

        [Fact]
        public void Constructor_WithValidParameters_SetsTargetFactionId()
        {
            // Arrange
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_enemy");

            // Act
            var mission = new FlipMission(lieutenant, "faction_player");

            // Assert
            Assert.Equal("faction_enemy", mission.TargetFactionId);
        }

        [Fact]
        public void Constructor_GeneratesUniqueId()
        {
            // Arrange
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_enemy");

            // Act
            var mission1 = new FlipMission(lieutenant, "faction_player");
            var mission2 = new FlipMission(lieutenant, "faction_player");

            // Assert
            Assert.NotNull(mission1.Id);
            Assert.NotNull(mission2.Id);
            Assert.NotEqual(mission1.Id, mission2.Id);
        }

        [Fact]
        public void Constructor_StartsInPendingStatus()
        {
            // Arrange
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_enemy");

            // Act
            var mission = new FlipMission(lieutenant, "faction_player");

            // Assert
            Assert.Equal(FlipMissionStatus.Pending, mission.Status);
        }

        [Fact]
        public void Constructor_SetsCreatedTime()
        {
            // Arrange
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_enemy");
            var beforeCreation = DateTime.UtcNow;

            // Act
            var mission = new FlipMission(lieutenant, "faction_player");
            var afterCreation = DateTime.UtcNow;

            // Assert
            Assert.InRange(mission.CreatedTime, beforeCreation, afterCreation);
        }

        [Fact]
        public void Constructor_WithBribeAmount_SetsBribeAmount()
        {
            // Arrange
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_enemy");

            // Act
            var mission = new FlipMission(lieutenant, "faction_player", bribeAmount: 50000);

            // Assert
            Assert.Equal(50000, mission.BribeAmount);
        }

        [Fact]
        public void Constructor_WithNegativeBribeAmount_TreatsAsZero()
        {
            // Arrange
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_enemy");

            // Act
            var mission = new FlipMission(lieutenant, "faction_player", bribeAmount: -100);

            // Assert
            Assert.Equal(0, mission.BribeAmount);
        }

        [Fact]
        public void Constructor_WithCapturedLieutenant_Succeeds()
        {
            // Arrange
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_enemy");
            lieutenant.Capture("faction_player");

            // Act
            var mission = new FlipMission(lieutenant, "faction_player");

            // Assert
            Assert.Equal("lt_001", mission.TargetLieutenantId);
        }

        #endregion

        #region Base Cost and Duration

        [Fact]
        public void BaseCost_ReturnsExpectedValue()
        {
            // Arrange
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_enemy");
            var mission = new FlipMission(lieutenant, "faction_player");

            // Act
            var baseCost = mission.BaseCost;

            // Assert - Flip missions have significant base cost
            Assert.True(baseCost >= 10000);
        }

        [Fact]
        public void TotalCost_IncludesBribeAmount()
        {
            // Arrange
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_enemy");
            var mission = new FlipMission(lieutenant, "faction_player", bribeAmount: 25000);

            // Act
            var totalCost = mission.TotalCost;

            // Assert
            Assert.Equal(mission.BaseCost + 25000, totalCost);
        }

        [Fact]
        public void BaseDurationSeconds_ReturnsExpectedValue()
        {
            // Arrange
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_enemy");
            var mission = new FlipMission(lieutenant, "faction_player");

            // Act
            var baseDuration = mission.BaseDurationSeconds;

            // Assert - Should take reasonable time
            Assert.True(baseDuration >= 60);
            Assert.True(baseDuration <= 300);
        }

        #endregion

        #region Start

        [Fact]
        public void Start_WhenPending_SetsStatusToInProgress()
        {
            // Arrange
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_enemy");
            var mission = new FlipMission(lieutenant, "faction_player");

            // Act
            mission.Start();

            // Assert
            Assert.Equal(FlipMissionStatus.InProgress, mission.Status);
        }

        [Fact]
        public void Start_WhenPending_SetsStartTime()
        {
            // Arrange
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_enemy");
            var mission = new FlipMission(lieutenant, "faction_player");
            var beforeStart = DateTime.UtcNow;

            // Act
            mission.Start();
            var afterStart = DateTime.UtcNow;

            // Assert
            Assert.NotNull(mission.StartTime);
            Assert.InRange(mission.StartTime.Value, beforeStart, afterStart);
        }

        [Fact]
        public void Start_WhenNotPending_ThrowsInvalidOperationException()
        {
            // Arrange
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_enemy");
            var mission = new FlipMission(lieutenant, "faction_player");
            mission.Start();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => mission.Start());
        }

        #endregion

        #region Complete

        [Fact]
        public void Complete_WhenInProgress_Success_SetsStatusToSucceeded()
        {
            // Arrange
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_enemy");
            var mission = new FlipMission(lieutenant, "faction_player");
            mission.Start();

            // Act
            mission.Complete(success: true, detected: false);

            // Assert
            Assert.Equal(FlipMissionStatus.Succeeded, mission.Status);
        }

        [Fact]
        public void Complete_WhenInProgress_Failure_SetsStatusToFailed()
        {
            // Arrange
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_enemy");
            var mission = new FlipMission(lieutenant, "faction_player");
            mission.Start();

            // Act
            mission.Complete(success: false, detected: false);

            // Assert
            Assert.Equal(FlipMissionStatus.Failed, mission.Status);
        }

        [Fact]
        public void Complete_WhenDetected_SetsStatusToDetected()
        {
            // Arrange
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_enemy");
            var mission = new FlipMission(lieutenant, "faction_player");
            mission.Start();

            // Act
            mission.Complete(success: false, detected: true);

            // Assert
            Assert.Equal(FlipMissionStatus.Detected, mission.Status);
        }

        [Fact]
        public void Complete_WhenSuccessAndDetected_SetsStatusToDetected()
        {
            // Arrange
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_enemy");
            var mission = new FlipMission(lieutenant, "faction_player");
            mission.Start();

            // Act
            mission.Complete(success: true, detected: true);

            // Assert - Detected status takes precedence for tracking tension implications
            Assert.Equal(FlipMissionStatus.Detected, mission.Status);
            Assert.True(mission.WasSuccessful);
        }

        [Fact]
        public void Complete_SetsCompletionTime()
        {
            // Arrange
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_enemy");
            var mission = new FlipMission(lieutenant, "faction_player");
            mission.Start();
            var beforeComplete = DateTime.UtcNow;

            // Act
            mission.Complete(success: true, detected: false);
            var afterComplete = DateTime.UtcNow;

            // Assert
            Assert.NotNull(mission.CompletionTime);
            Assert.InRange(mission.CompletionTime.Value, beforeComplete, afterComplete);
        }

        [Fact]
        public void Complete_SetsWasSuccessful()
        {
            // Arrange
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_enemy");
            var mission = new FlipMission(lieutenant, "faction_player");
            mission.Start();

            // Act
            mission.Complete(success: true, detected: false);

            // Assert
            Assert.True(mission.WasSuccessful);
        }

        [Fact]
        public void Complete_SetsWasDetected()
        {
            // Arrange
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_enemy");
            var mission = new FlipMission(lieutenant, "faction_player");
            mission.Start();

            // Act
            mission.Complete(success: false, detected: true);

            // Assert
            Assert.True(mission.WasDetected);
        }

        [Fact]
        public void Complete_WhenNotInProgress_ThrowsInvalidOperationException()
        {
            // Arrange
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_enemy");
            var mission = new FlipMission(lieutenant, "faction_player");

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => mission.Complete(true, false));
        }

        [Fact]
        public void Complete_WhenAlreadyCompleted_ThrowsInvalidOperationException()
        {
            // Arrange
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_enemy");
            var mission = new FlipMission(lieutenant, "faction_player");
            mission.Start();
            mission.Complete(true, false);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => mission.Complete(true, false));
        }

        #endregion

        #region Cancel

        [Fact]
        public void Cancel_WhenPending_SetsStatusToCancelled()
        {
            // Arrange
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_enemy");
            var mission = new FlipMission(lieutenant, "faction_player");

            // Act
            mission.Cancel();

            // Assert
            Assert.Equal(FlipMissionStatus.Cancelled, mission.Status);
        }

        [Fact]
        public void Cancel_WhenInProgress_SetsStatusToCancelled()
        {
            // Arrange
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_enemy");
            var mission = new FlipMission(lieutenant, "faction_player");
            mission.Start();

            // Act
            mission.Cancel();

            // Assert
            Assert.Equal(FlipMissionStatus.Cancelled, mission.Status);
        }

        [Fact]
        public void Cancel_SetsCompletionTime()
        {
            // Arrange
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_enemy");
            var mission = new FlipMission(lieutenant, "faction_player");
            var beforeCancel = DateTime.UtcNow;

            // Act
            mission.Cancel();
            var afterCancel = DateTime.UtcNow;

            // Assert
            Assert.NotNull(mission.CompletionTime);
            Assert.InRange(mission.CompletionTime.Value, beforeCancel, afterCancel);
        }

        [Fact]
        public void Cancel_WhenAlreadyTerminal_ThrowsInvalidOperationException()
        {
            // Arrange
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_enemy");
            var mission = new FlipMission(lieutenant, "faction_player");
            mission.Cancel();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => mission.Cancel());
        }

        #endregion

        #region IsTerminal

        [Fact]
        public void IsTerminal_WhenPending_ReturnsFalse()
        {
            // Arrange
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_enemy");
            var mission = new FlipMission(lieutenant, "faction_player");

            // Assert
            Assert.False(mission.IsTerminal);
        }

        [Fact]
        public void IsTerminal_WhenInProgress_ReturnsFalse()
        {
            // Arrange
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_enemy");
            var mission = new FlipMission(lieutenant, "faction_player");
            mission.Start();

            // Assert
            Assert.False(mission.IsTerminal);
        }

        [Fact]
        public void IsTerminal_WhenSucceeded_ReturnsTrue()
        {
            // Arrange
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_enemy");
            var mission = new FlipMission(lieutenant, "faction_player");
            mission.Start();
            mission.Complete(true, false);

            // Assert
            Assert.True(mission.IsTerminal);
        }

        [Fact]
        public void IsTerminal_WhenFailed_ReturnsTrue()
        {
            // Arrange
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_enemy");
            var mission = new FlipMission(lieutenant, "faction_player");
            mission.Start();
            mission.Complete(false, false);

            // Assert
            Assert.True(mission.IsTerminal);
        }

        [Fact]
        public void IsTerminal_WhenCancelled_ReturnsTrue()
        {
            // Arrange
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_enemy");
            var mission = new FlipMission(lieutenant, "faction_player");
            mission.Cancel();

            // Assert
            Assert.True(mission.IsTerminal);
        }

        [Fact]
        public void IsTerminal_WhenDetected_ReturnsTrue()
        {
            // Arrange
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_enemy");
            var mission = new FlipMission(lieutenant, "faction_player");
            mission.Start();
            mission.Complete(false, true);

            // Assert
            Assert.True(mission.IsTerminal);
        }

        #endregion

        #region Base Success and Detection Chances

        [Fact]
        public void BaseSuccessChance_ReturnsValueBetweenZeroAndOne()
        {
            // Arrange
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_enemy");
            var mission = new FlipMission(lieutenant, "faction_player");

            // Assert
            Assert.InRange(mission.BaseSuccessChance, 0.0f, 1.0f);
        }

        [Fact]
        public void BaseDetectionChance_ReturnsValueBetweenZeroAndOne()
        {
            // Arrange
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_enemy");
            var mission = new FlipMission(lieutenant, "faction_player");

            // Assert
            Assert.InRange(mission.BaseDetectionChance, 0.0f, 1.0f);
        }

        #endregion

        #region InvolvesFaction

        [Fact]
        public void InvolvesFaction_WithInitiatorFaction_ReturnsTrue()
        {
            // Arrange
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_enemy");
            var mission = new FlipMission(lieutenant, "faction_player");

            // Act
            var result = mission.InvolvesFaction("faction_player");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void InvolvesFaction_WithTargetFaction_ReturnsTrue()
        {
            // Arrange
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_enemy");
            var mission = new FlipMission(lieutenant, "faction_player");

            // Act
            var result = mission.InvolvesFaction("faction_enemy");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void InvolvesFaction_WithUnrelatedFaction_ReturnsFalse()
        {
            // Arrange
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_enemy");
            var mission = new FlipMission(lieutenant, "faction_player");

            // Act
            var result = mission.InvolvesFaction("faction_third");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void InvolvesFaction_WithNullFaction_ReturnsFalse()
        {
            // Arrange
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_enemy");
            var mission = new FlipMission(lieutenant, "faction_player");

            // Act
            var result = mission.InvolvesFaction(null!);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void InvolvesFaction_WithEmptyFaction_ReturnsFalse()
        {
            // Arrange
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_enemy");
            var mission = new FlipMission(lieutenant, "faction_player");

            // Act
            var result = mission.InvolvesFaction("");

            // Assert
            Assert.False(result);
        }

        #endregion

        #region ToString

        [Fact]
        public void ToString_ContainsMissionInfo()
        {
            // Arrange
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_enemy");
            var mission = new FlipMission(lieutenant, "faction_player");

            // Act
            var result = mission.ToString();

            // Assert
            Assert.Contains("Flip", result);
            Assert.Contains("faction_player", result);
            Assert.Contains("lt_001", result);
        }

        #endregion
    }
}
