using System;
using FactionWars.Tension.Models;
using Xunit;

namespace FactionWars.Tests.Unit.Tension
{
    /// <summary>
    /// Tests for the WarfareStateTransition model which represents a state change in warfare between factions.
    /// </summary>
    public class WarfareStateTransitionTests
    {
        private const string FactionA = "faction-michael";
        private const string FactionB = "faction-trevor";

        #region Constructor Tests

        [Fact]
        public void Constructor_WithValidParameters_CreatesTransition()
        {
            // Arrange & Act
            var transition = new WarfareStateTransition(
                FactionA,
                FactionB,
                WarfareState.Peace,
                WarfareState.ColdWar,
                WarfareStateTransitionReason.TensionThresholdReached);

            // Assert
            Assert.NotNull(transition);
            Assert.Equal(FactionA, transition.FactionId1);
            Assert.Equal(FactionB, transition.FactionId2);
            Assert.Equal(WarfareState.Peace, transition.PreviousState);
            Assert.Equal(WarfareState.ColdWar, transition.NewState);
            Assert.Equal(WarfareStateTransitionReason.TensionThresholdReached, transition.Reason);
        }

        [Fact]
        public void Constructor_SetsTimestampToUtcNow()
        {
            // Arrange
            var before = DateTime.UtcNow;

            // Act
            var transition = new WarfareStateTransition(
                FactionA,
                FactionB,
                WarfareState.Peace,
                WarfareState.ColdWar,
                WarfareStateTransitionReason.TensionThresholdReached);

            var after = DateTime.UtcNow;

            // Assert
            Assert.True(transition.Timestamp >= before);
            Assert.True(transition.Timestamp <= after);
        }

        [Fact]
        public void Constructor_WithNullFactionId1_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() => new WarfareStateTransition(
                null!,
                FactionB,
                WarfareState.Peace,
                WarfareState.ColdWar,
                WarfareStateTransitionReason.TensionThresholdReached));
        }

        [Fact]
        public void Constructor_WithNullFactionId2_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() => new WarfareStateTransition(
                FactionA,
                null!,
                WarfareState.Peace,
                WarfareState.ColdWar,
                WarfareStateTransitionReason.TensionThresholdReached));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Constructor_WithEmptyFactionId1_ThrowsArgumentException(string factionId)
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentException>(() => new WarfareStateTransition(
                factionId,
                FactionB,
                WarfareState.Peace,
                WarfareState.ColdWar,
                WarfareStateTransitionReason.TensionThresholdReached));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Constructor_WithEmptyFactionId2_ThrowsArgumentException(string factionId)
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentException>(() => new WarfareStateTransition(
                FactionA,
                factionId,
                WarfareState.Peace,
                WarfareState.ColdWar,
                WarfareStateTransitionReason.TensionThresholdReached));
        }

        [Fact]
        public void Constructor_WithSameFactionIds_ThrowsArgumentException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentException>(() => new WarfareStateTransition(
                FactionA,
                FactionA,
                WarfareState.Peace,
                WarfareState.ColdWar,
                WarfareStateTransitionReason.TensionThresholdReached));
        }

        [Fact]
        public void Constructor_WithSameStates_ThrowsArgumentException()
        {
            // A transition must be between different states
            Assert.Throws<ArgumentException>(() => new WarfareStateTransition(
                FactionA,
                FactionB,
                WarfareState.Peace,
                WarfareState.Peace,
                WarfareStateTransitionReason.TensionThresholdReached));
        }

        [Fact]
        public void Constructor_WithMetadata_StoresMetadata()
        {
            // Arrange & Act
            var transition = new WarfareStateTransition(
                FactionA,
                FactionB,
                WarfareState.Peace,
                WarfareState.ColdWar,
                WarfareStateTransitionReason.TensionThresholdReached,
                "Tension reached 25");

            // Assert
            Assert.Equal("Tension reached 25", transition.Metadata);
        }

        [Fact]
        public void Constructor_WithoutMetadata_MetadataIsNull()
        {
            // Arrange & Act
            var transition = new WarfareStateTransition(
                FactionA,
                FactionB,
                WarfareState.Peace,
                WarfareState.ColdWar,
                WarfareStateTransitionReason.TensionThresholdReached);

            // Assert
            Assert.Null(transition.Metadata);
        }

        #endregion

        #region Property Tests

        [Fact]
        public void IsEscalation_WhenNewStateIsHigher_ReturnsTrue()
        {
            // Arrange
            var transition = new WarfareStateTransition(
                FactionA,
                FactionB,
                WarfareState.ColdWar,
                WarfareState.OpenWarfare,
                WarfareStateTransitionReason.TensionThresholdReached);

            // Assert
            Assert.True(transition.IsEscalation);
        }

        [Fact]
        public void IsEscalation_WhenNewStateIsLower_ReturnsFalse()
        {
            // Arrange
            var transition = new WarfareStateTransition(
                FactionA,
                FactionB,
                WarfareState.OpenWarfare,
                WarfareState.ColdWar,
                WarfareStateTransitionReason.TensionDecay);

            // Assert
            Assert.False(transition.IsEscalation);
        }

        [Fact]
        public void IsDeescalation_WhenNewStateIsLower_ReturnsTrue()
        {
            // Arrange
            var transition = new WarfareStateTransition(
                FactionA,
                FactionB,
                WarfareState.OpenWarfare,
                WarfareState.ColdWar,
                WarfareStateTransitionReason.TensionDecay);

            // Assert
            Assert.True(transition.IsDeescalation);
        }

        [Fact]
        public void IsDeescalation_WhenNewStateIsHigher_ReturnsFalse()
        {
            // Arrange
            var transition = new WarfareStateTransition(
                FactionA,
                FactionB,
                WarfareState.ColdWar,
                WarfareState.OpenWarfare,
                WarfareStateTransitionReason.TensionThresholdReached);

            // Assert
            Assert.False(transition.IsDeescalation);
        }

        [Fact]
        public void EntersCombat_WhenTransitioningToBorderSkirmishes_ReturnsTrue()
        {
            // Arrange
            var transition = new WarfareStateTransition(
                FactionA,
                FactionB,
                WarfareState.ColdWar,
                WarfareState.BorderSkirmishes,
                WarfareStateTransitionReason.TensionThresholdReached);

            // Assert
            Assert.True(transition.EntersCombat);
        }

        [Fact]
        public void EntersCombat_WhenAlreadyInCombat_ReturnsFalse()
        {
            // Arrange
            var transition = new WarfareStateTransition(
                FactionA,
                FactionB,
                WarfareState.BorderSkirmishes,
                WarfareState.OpenWarfare,
                WarfareStateTransitionReason.TensionThresholdReached);

            // Assert
            Assert.False(transition.EntersCombat);
        }

        [Fact]
        public void EntersCombat_WhenLeavingCombat_ReturnsFalse()
        {
            // Arrange
            var transition = new WarfareStateTransition(
                FactionA,
                FactionB,
                WarfareState.BorderSkirmishes,
                WarfareState.ColdWar,
                WarfareStateTransitionReason.TensionDecay);

            // Assert
            Assert.False(transition.EntersCombat);
        }

        [Fact]
        public void ExitsCombat_WhenLeavingBorderSkirmishes_ReturnsTrue()
        {
            // Arrange
            var transition = new WarfareStateTransition(
                FactionA,
                FactionB,
                WarfareState.BorderSkirmishes,
                WarfareState.ColdWar,
                WarfareStateTransitionReason.TensionDecay);

            // Assert
            Assert.True(transition.ExitsCombat);
        }

        [Fact]
        public void ExitsCombat_WhenStillInCombat_ReturnsFalse()
        {
            // Arrange
            var transition = new WarfareStateTransition(
                FactionA,
                FactionB,
                WarfareState.OpenWarfare,
                WarfareState.BorderSkirmishes,
                WarfareStateTransitionReason.TensionDecay);

            // Assert
            Assert.False(transition.ExitsCombat);
        }

        [Fact]
        public void ExitsCombat_WhenEnteringCombat_ReturnsFalse()
        {
            // Arrange
            var transition = new WarfareStateTransition(
                FactionA,
                FactionB,
                WarfareState.ColdWar,
                WarfareState.BorderSkirmishes,
                WarfareStateTransitionReason.TensionThresholdReached);

            // Assert
            Assert.False(transition.ExitsCombat);
        }

        #endregion

        #region InvolvesFaction Tests

        [Fact]
        public void InvolvesFaction_WithFactionId1_ReturnsTrue()
        {
            // Arrange
            var transition = new WarfareStateTransition(
                FactionA,
                FactionB,
                WarfareState.Peace,
                WarfareState.ColdWar,
                WarfareStateTransitionReason.TensionThresholdReached);

            // Assert
            Assert.True(transition.InvolvesFaction(FactionA));
        }

        [Fact]
        public void InvolvesFaction_WithFactionId2_ReturnsTrue()
        {
            // Arrange
            var transition = new WarfareStateTransition(
                FactionA,
                FactionB,
                WarfareState.Peace,
                WarfareState.ColdWar,
                WarfareStateTransitionReason.TensionThresholdReached);

            // Assert
            Assert.True(transition.InvolvesFaction(FactionB));
        }

        [Fact]
        public void InvolvesFaction_WithUnrelatedFaction_ReturnsFalse()
        {
            // Arrange
            var transition = new WarfareStateTransition(
                FactionA,
                FactionB,
                WarfareState.Peace,
                WarfareState.ColdWar,
                WarfareStateTransitionReason.TensionThresholdReached);

            // Assert
            Assert.False(transition.InvolvesFaction("faction-franklin"));
        }

        [Fact]
        public void InvolvesFaction_WithNullFactionId_ReturnsFalse()
        {
            // Arrange
            var transition = new WarfareStateTransition(
                FactionA,
                FactionB,
                WarfareState.Peace,
                WarfareState.ColdWar,
                WarfareStateTransitionReason.TensionThresholdReached);

            // Assert
            Assert.False(transition.InvolvesFaction(null!));
        }

        [Fact]
        public void InvolvesFaction_WithEmptyFactionId_ReturnsFalse()
        {
            // Arrange
            var transition = new WarfareStateTransition(
                FactionA,
                FactionB,
                WarfareState.Peace,
                WarfareState.ColdWar,
                WarfareStateTransitionReason.TensionThresholdReached);

            // Assert
            Assert.False(transition.InvolvesFaction(""));
        }

        #endregion

        #region ToString Tests

        [Fact]
        public void ToString_ReturnsFormattedString()
        {
            // Arrange
            var transition = new WarfareStateTransition(
                FactionA,
                FactionB,
                WarfareState.Peace,
                WarfareState.ColdWar,
                WarfareStateTransitionReason.TensionThresholdReached);

            // Act
            var result = transition.ToString();

            // Assert
            Assert.Contains(FactionA, result);
            Assert.Contains(FactionB, result);
            Assert.Contains("Peace", result);
            Assert.Contains("ColdWar", result);
        }

        #endregion
    }
}
