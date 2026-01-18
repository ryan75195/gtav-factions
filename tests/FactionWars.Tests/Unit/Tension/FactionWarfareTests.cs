using System;
using System.Collections.Generic;
using System.Linq;
using FactionWars.Tension.Models;
using Xunit;

namespace FactionWars.Tests.Unit.Tension
{
    /// <summary>
    /// Tests for the FactionWarfare model which tracks the warfare state between two factions.
    /// </summary>
    public class FactionWarfareTests
    {
        private const string FactionA = "faction-michael";
        private const string FactionB = "faction-trevor";
        private const string FactionC = "faction-franklin";

        #region Constructor Tests

        [Fact]
        public void Constructor_WithValidParameters_CreatesFactionWarfare()
        {
            // Arrange & Act
            var warfare = new FactionWarfare(FactionA, FactionB);

            // Assert
            Assert.NotNull(warfare);
            Assert.Equal(FactionA, warfare.FactionId1);
            Assert.Equal(FactionB, warfare.FactionId2);
        }

        [Fact]
        public void Constructor_InitializesToPeaceState()
        {
            // Arrange & Act
            var warfare = new FactionWarfare(FactionA, FactionB);

            // Assert
            Assert.Equal(WarfareState.Peace, warfare.CurrentState);
        }

        [Fact]
        public void Constructor_SetsStateEnteredTimeToUtcNow()
        {
            // Arrange
            var before = DateTime.UtcNow;

            // Act
            var warfare = new FactionWarfare(FactionA, FactionB);

            var after = DateTime.UtcNow;

            // Assert
            Assert.True(warfare.StateEnteredTime >= before);
            Assert.True(warfare.StateEnteredTime <= after);
        }

        [Fact]
        public void Constructor_InitializesEmptyTransitionHistory()
        {
            // Arrange & Act
            var warfare = new FactionWarfare(FactionA, FactionB);

            // Assert
            Assert.Empty(warfare.TransitionHistory);
        }

        [Fact]
        public void Constructor_WithInitialState_SetsCurrentState()
        {
            // Arrange & Act
            var warfare = new FactionWarfare(FactionA, FactionB, WarfareState.ColdWar);

            // Assert
            Assert.Equal(WarfareState.ColdWar, warfare.CurrentState);
        }

        [Fact]
        public void Constructor_WithNullFactionId1_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() => new FactionWarfare(null!, FactionB));
        }

        [Fact]
        public void Constructor_WithNullFactionId2_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() => new FactionWarfare(FactionA, null!));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Constructor_WithEmptyFactionId1_ThrowsArgumentException(string factionId)
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentException>(() => new FactionWarfare(factionId, FactionB));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Constructor_WithEmptyFactionId2_ThrowsArgumentException(string factionId)
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentException>(() => new FactionWarfare(FactionA, factionId));
        }

        [Fact]
        public void Constructor_WithSameFactionIds_ThrowsArgumentException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentException>(() => new FactionWarfare(FactionA, FactionA));
        }

        #endregion

        #region Transition Tests

        [Fact]
        public void TransitionTo_ValidState_ChangesCurrentState()
        {
            // Arrange
            var warfare = new FactionWarfare(FactionA, FactionB);

            // Act
            warfare.TransitionTo(WarfareState.ColdWar, WarfareStateTransitionReason.TensionThresholdReached);

            // Assert
            Assert.Equal(WarfareState.ColdWar, warfare.CurrentState);
        }

        [Fact]
        public void TransitionTo_AddsToTransitionHistory()
        {
            // Arrange
            var warfare = new FactionWarfare(FactionA, FactionB);

            // Act
            warfare.TransitionTo(WarfareState.ColdWar, WarfareStateTransitionReason.TensionThresholdReached);

            // Assert
            Assert.Single(warfare.TransitionHistory);
            var transition = warfare.TransitionHistory.First();
            Assert.Equal(WarfareState.Peace, transition.PreviousState);
            Assert.Equal(WarfareState.ColdWar, transition.NewState);
        }

        [Fact]
        public void TransitionTo_UpdatesStateEnteredTime()
        {
            // Arrange
            var warfare = new FactionWarfare(FactionA, FactionB);
            var originalTime = warfare.StateEnteredTime;

            // Act
            System.Threading.Thread.Sleep(10); // Ensure time difference
            warfare.TransitionTo(WarfareState.ColdWar, WarfareStateTransitionReason.TensionThresholdReached);

            // Assert
            Assert.True(warfare.StateEnteredTime > originalTime);
        }

        [Fact]
        public void TransitionTo_SameState_ThrowsInvalidOperationException()
        {
            // Arrange
            var warfare = new FactionWarfare(FactionA, FactionB);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
                warfare.TransitionTo(WarfareState.Peace, WarfareStateTransitionReason.TensionDecay));
        }

        [Fact]
        public void TransitionTo_WithMetadata_StoresMetadata()
        {
            // Arrange
            var warfare = new FactionWarfare(FactionA, FactionB);

            // Act
            warfare.TransitionTo(WarfareState.ColdWar, WarfareStateTransitionReason.TensionThresholdReached, "Tension hit 25");

            // Assert
            var transition = warfare.TransitionHistory.First();
            Assert.Equal("Tension hit 25", transition.Metadata);
        }

        [Fact]
        public void TransitionTo_MultipleTransitions_MaintainsHistory()
        {
            // Arrange
            var warfare = new FactionWarfare(FactionA, FactionB);

            // Act
            warfare.TransitionTo(WarfareState.ColdWar, WarfareStateTransitionReason.TensionThresholdReached);
            warfare.TransitionTo(WarfareState.BorderSkirmishes, WarfareStateTransitionReason.TensionThresholdReached);
            warfare.TransitionTo(WarfareState.OpenWarfare, WarfareStateTransitionReason.MajorIncident);

            // Assert
            Assert.Equal(3, warfare.TransitionHistory.Count);
            Assert.Equal(WarfareState.OpenWarfare, warfare.CurrentState);
        }

        [Fact]
        public void TransitionTo_CanDeescalate()
        {
            // Arrange
            var warfare = new FactionWarfare(FactionA, FactionB, WarfareState.OpenWarfare);

            // Act
            warfare.TransitionTo(WarfareState.BorderSkirmishes, WarfareStateTransitionReason.TensionDecay);

            // Assert
            Assert.Equal(WarfareState.BorderSkirmishes, warfare.CurrentState);
        }

        [Fact]
        public void TransitionTo_ReturnsTransitionRecord()
        {
            // Arrange
            var warfare = new FactionWarfare(FactionA, FactionB);

            // Act
            var transition = warfare.TransitionTo(WarfareState.ColdWar, WarfareStateTransitionReason.TensionThresholdReached);

            // Assert
            Assert.NotNull(transition);
            Assert.Equal(WarfareState.Peace, transition.PreviousState);
            Assert.Equal(WarfareState.ColdWar, transition.NewState);
        }

        #endregion

        #region State Query Tests

        [Fact]
        public void IsAtPeace_WhenPeace_ReturnsTrue()
        {
            // Arrange
            var warfare = new FactionWarfare(FactionA, FactionB);

            // Assert
            Assert.True(warfare.IsAtPeace);
        }

        [Fact]
        public void IsAtPeace_WhenNotPeace_ReturnsFalse()
        {
            // Arrange
            var warfare = new FactionWarfare(FactionA, FactionB, WarfareState.ColdWar);

            // Assert
            Assert.False(warfare.IsAtPeace);
        }

        [Fact]
        public void IsInCombat_WhenBorderSkirmishes_ReturnsTrue()
        {
            // Arrange
            var warfare = new FactionWarfare(FactionA, FactionB, WarfareState.BorderSkirmishes);

            // Assert
            Assert.True(warfare.IsInCombat);
        }

        [Fact]
        public void IsInCombat_WhenOpenWarfare_ReturnsTrue()
        {
            // Arrange
            var warfare = new FactionWarfare(FactionA, FactionB, WarfareState.OpenWarfare);

            // Assert
            Assert.True(warfare.IsInCombat);
        }

        [Fact]
        public void IsInCombat_WhenTotalWar_ReturnsTrue()
        {
            // Arrange
            var warfare = new FactionWarfare(FactionA, FactionB, WarfareState.TotalWar);

            // Assert
            Assert.True(warfare.IsInCombat);
        }

        [Fact]
        public void IsInCombat_WhenColdWar_ReturnsFalse()
        {
            // Arrange
            var warfare = new FactionWarfare(FactionA, FactionB, WarfareState.ColdWar);

            // Assert
            Assert.False(warfare.IsInCombat);
        }

        [Fact]
        public void IsInCombat_WhenPeace_ReturnsFalse()
        {
            // Arrange
            var warfare = new FactionWarfare(FactionA, FactionB, WarfareState.Peace);

            // Assert
            Assert.False(warfare.IsInCombat);
        }

        [Fact]
        public void IsHostile_WhenColdWarOrAbove_ReturnsTrue()
        {
            // Arrange & Assert
            Assert.True(new FactionWarfare(FactionA, FactionB, WarfareState.ColdWar).IsHostile);
            Assert.True(new FactionWarfare(FactionA, FactionB, WarfareState.BorderSkirmishes).IsHostile);
            Assert.True(new FactionWarfare(FactionA, FactionB, WarfareState.OpenWarfare).IsHostile);
            Assert.True(new FactionWarfare(FactionA, FactionB, WarfareState.TotalWar).IsHostile);
        }

        [Fact]
        public void IsHostile_WhenPeace_ReturnsFalse()
        {
            // Arrange
            var warfare = new FactionWarfare(FactionA, FactionB, WarfareState.Peace);

            // Assert
            Assert.False(warfare.IsHostile);
        }

        [Fact]
        public void IsInTotalWar_WhenTotalWar_ReturnsTrue()
        {
            // Arrange
            var warfare = new FactionWarfare(FactionA, FactionB, WarfareState.TotalWar);

            // Assert
            Assert.True(warfare.IsInTotalWar);
        }

        [Fact]
        public void IsInTotalWar_WhenNotTotalWar_ReturnsFalse()
        {
            // Arrange
            var warfare = new FactionWarfare(FactionA, FactionB, WarfareState.OpenWarfare);

            // Assert
            Assert.False(warfare.IsInTotalWar);
        }

        #endregion

        #region ContainsFaction Tests

        [Fact]
        public void ContainsFaction_WithFactionId1_ReturnsTrue()
        {
            // Arrange
            var warfare = new FactionWarfare(FactionA, FactionB);

            // Assert
            Assert.True(warfare.ContainsFaction(FactionA));
        }

        [Fact]
        public void ContainsFaction_WithFactionId2_ReturnsTrue()
        {
            // Arrange
            var warfare = new FactionWarfare(FactionA, FactionB);

            // Assert
            Assert.True(warfare.ContainsFaction(FactionB));
        }

        [Fact]
        public void ContainsFaction_WithUnrelatedFaction_ReturnsFalse()
        {
            // Arrange
            var warfare = new FactionWarfare(FactionA, FactionB);

            // Assert
            Assert.False(warfare.ContainsFaction(FactionC));
        }

        [Fact]
        public void ContainsFaction_WithNull_ReturnsFalse()
        {
            // Arrange
            var warfare = new FactionWarfare(FactionA, FactionB);

            // Assert
            Assert.False(warfare.ContainsFaction(null!));
        }

        [Fact]
        public void ContainsFaction_WithEmpty_ReturnsFalse()
        {
            // Arrange
            var warfare = new FactionWarfare(FactionA, FactionB);

            // Assert
            Assert.False(warfare.ContainsFaction(""));
        }

        #endregion

        #region InvolvesBothFactions Tests

        [Fact]
        public void InvolvesBothFactions_WithBothFactions_ReturnsTrue()
        {
            // Arrange
            var warfare = new FactionWarfare(FactionA, FactionB);

            // Assert
            Assert.True(warfare.InvolvesBothFactions(FactionA, FactionB));
        }

        [Fact]
        public void InvolvesBothFactions_WithBothFactionsReversed_ReturnsTrue()
        {
            // Arrange
            var warfare = new FactionWarfare(FactionA, FactionB);

            // Assert
            Assert.True(warfare.InvolvesBothFactions(FactionB, FactionA));
        }

        [Fact]
        public void InvolvesBothFactions_WithOnlyOneFaction_ReturnsFalse()
        {
            // Arrange
            var warfare = new FactionWarfare(FactionA, FactionB);

            // Assert
            Assert.False(warfare.InvolvesBothFactions(FactionA, FactionC));
        }

        #endregion

        #region GetOtherFaction Tests

        [Fact]
        public void GetOtherFaction_WithFactionId1_ReturnsFactionId2()
        {
            // Arrange
            var warfare = new FactionWarfare(FactionA, FactionB);

            // Assert
            Assert.Equal(FactionB, warfare.GetOtherFaction(FactionA));
        }

        [Fact]
        public void GetOtherFaction_WithFactionId2_ReturnsFactionId1()
        {
            // Arrange
            var warfare = new FactionWarfare(FactionA, FactionB);

            // Assert
            Assert.Equal(FactionA, warfare.GetOtherFaction(FactionB));
        }

        [Fact]
        public void GetOtherFaction_WithUnrelatedFaction_ReturnsNull()
        {
            // Arrange
            var warfare = new FactionWarfare(FactionA, FactionB);

            // Assert
            Assert.Null(warfare.GetOtherFaction(FactionC));
        }

        #endregion

        #region TimeInCurrentState Tests

        [Fact]
        public void TimeInCurrentState_ReturnsTimeSinceStateEntered()
        {
            // Arrange
            var warfare = new FactionWarfare(FactionA, FactionB);

            // Act
            System.Threading.Thread.Sleep(50); // Wait a bit
            var time = warfare.TimeInCurrentState;

            // Assert
            Assert.True(time.TotalMilliseconds >= 50);
        }

        #endregion

        #region Equality Tests

        [Fact]
        public void Equals_WithSameFactions_ReturnsTrue()
        {
            // Arrange
            var warfare1 = new FactionWarfare(FactionA, FactionB);
            var warfare2 = new FactionWarfare(FactionA, FactionB);

            // Assert
            Assert.True(warfare1.Equals(warfare2));
        }

        [Fact]
        public void Equals_WithSameFactionsReversed_ReturnsTrue()
        {
            // Arrange
            var warfare1 = new FactionWarfare(FactionA, FactionB);
            var warfare2 = new FactionWarfare(FactionB, FactionA);

            // Assert
            Assert.True(warfare1.Equals(warfare2));
        }

        [Fact]
        public void Equals_WithDifferentFactions_ReturnsFalse()
        {
            // Arrange
            var warfare1 = new FactionWarfare(FactionA, FactionB);
            var warfare2 = new FactionWarfare(FactionA, FactionC);

            // Assert
            Assert.False(warfare1.Equals(warfare2));
        }

        [Fact]
        public void Equals_WithNull_ReturnsFalse()
        {
            // Arrange
            var warfare = new FactionWarfare(FactionA, FactionB);

            // Assert
            Assert.False(warfare.Equals(null));
        }

        [Fact]
        public void GetHashCode_SameFactionsHaveSameHashCode()
        {
            // Arrange
            var warfare1 = new FactionWarfare(FactionA, FactionB);
            var warfare2 = new FactionWarfare(FactionB, FactionA);

            // Assert
            Assert.Equal(warfare1.GetHashCode(), warfare2.GetHashCode());
        }

        #endregion

        #region ToString Tests

        [Fact]
        public void ToString_ReturnsFormattedString()
        {
            // Arrange
            var warfare = new FactionWarfare(FactionA, FactionB);

            // Act
            var result = warfare.ToString();

            // Assert
            Assert.Contains(FactionA, result);
            Assert.Contains(FactionB, result);
            Assert.Contains("Peace", result);
        }

        #endregion
    }
}
