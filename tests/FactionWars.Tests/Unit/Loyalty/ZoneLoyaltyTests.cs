using FactionWars.Loyalty.Models;
using System;
using Xunit;

namespace FactionWars.Tests.Unit.Loyalty
{
    public class ZoneLoyaltyTests
    {
        #region Constructor and Required Properties

        [Fact]
        public void ZoneLoyalty_ShouldRequireZoneId()
        {
            // Arrange & Act
            var loyalty = new ZoneLoyalty("zone_downtown", "faction_michael");

            // Assert
            Assert.Equal("zone_downtown", loyalty.ZoneId);
        }

        [Fact]
        public void ZoneLoyalty_ShouldRequireControllingFactionId()
        {
            // Arrange & Act
            var loyalty = new ZoneLoyalty("zone_downtown", "faction_michael");

            // Assert
            Assert.Equal("faction_michael", loyalty.ControllingFactionId);
        }

        [Fact]
        public void ZoneLoyalty_ShouldThrowOnNullZoneId()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentNullException>(() => new ZoneLoyalty(null!, "faction_michael"));
        }

        [Fact]
        public void ZoneLoyalty_ShouldThrowOnEmptyZoneId()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentException>(() => new ZoneLoyalty("", "faction_michael"));
        }

        [Fact]
        public void ZoneLoyalty_ShouldThrowOnWhitespaceZoneId()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentException>(() => new ZoneLoyalty("   ", "faction_michael"));
        }

        [Fact]
        public void ZoneLoyalty_ShouldThrowOnNullFactionId()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentNullException>(() => new ZoneLoyalty("zone_downtown", null!));
        }

        [Fact]
        public void ZoneLoyalty_ShouldThrowOnEmptyFactionId()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentException>(() => new ZoneLoyalty("zone_downtown", ""));
        }

        [Fact]
        public void ZoneLoyalty_ShouldThrowOnWhitespaceFactionId()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentException>(() => new ZoneLoyalty("zone_downtown", "   "));
        }

        #endregion

        #region Loyalty Value

        [Fact]
        public void ZoneLoyalty_ShouldHaveDefaultLoyaltyValue()
        {
            // Arrange & Act
            var loyalty = new ZoneLoyalty("zone_downtown", "faction_michael");

            // Assert - Default loyalty when a faction takes over is 50%
            Assert.Equal(50, loyalty.LoyaltyValue);
        }

        [Fact]
        public void ZoneLoyalty_ShouldAllowCustomInitialLoyalty()
        {
            // Arrange & Act
            var loyalty = new ZoneLoyalty("zone_downtown", "faction_michael", initialLoyalty: 75);

            // Assert
            Assert.Equal(75, loyalty.LoyaltyValue);
        }

        [Fact]
        public void ZoneLoyalty_ShouldClampInitialLoyaltyToMinimum()
        {
            // Arrange & Act
            var loyalty = new ZoneLoyalty("zone_downtown", "faction_michael", initialLoyalty: -10);

            // Assert
            Assert.Equal(0, loyalty.LoyaltyValue);
        }

        [Fact]
        public void ZoneLoyalty_ShouldClampInitialLoyaltyToMaximum()
        {
            // Arrange & Act
            var loyalty = new ZoneLoyalty("zone_downtown", "faction_michael", initialLoyalty: 150);

            // Assert
            Assert.Equal(100, loyalty.LoyaltyValue);
        }

        [Fact]
        public void ZoneLoyalty_AdjustLoyalty_ShouldIncreaseLoyalty()
        {
            // Arrange
            var loyalty = new ZoneLoyalty("zone_downtown", "faction_michael", initialLoyalty: 50);

            // Act
            loyalty.AdjustLoyalty(25);

            // Assert
            Assert.Equal(75, loyalty.LoyaltyValue);
        }

        [Fact]
        public void ZoneLoyalty_AdjustLoyalty_ShouldDecreaseLoyalty()
        {
            // Arrange
            var loyalty = new ZoneLoyalty("zone_downtown", "faction_michael", initialLoyalty: 50);

            // Act
            loyalty.AdjustLoyalty(-25);

            // Assert
            Assert.Equal(25, loyalty.LoyaltyValue);
        }

        [Fact]
        public void ZoneLoyalty_AdjustLoyalty_ShouldCapAtMaximum()
        {
            // Arrange
            var loyalty = new ZoneLoyalty("zone_downtown", "faction_michael", initialLoyalty: 90);

            // Act
            loyalty.AdjustLoyalty(50);

            // Assert
            Assert.Equal(100, loyalty.LoyaltyValue);
        }

        [Fact]
        public void ZoneLoyalty_AdjustLoyalty_ShouldCapAtMinimum()
        {
            // Arrange
            var loyalty = new ZoneLoyalty("zone_downtown", "faction_michael", initialLoyalty: 20);

            // Act
            loyalty.AdjustLoyalty(-50);

            // Assert
            Assert.Equal(0, loyalty.LoyaltyValue);
        }

        [Fact]
        public void ZoneLoyalty_SetLoyalty_ShouldSetValue()
        {
            // Arrange
            var loyalty = new ZoneLoyalty("zone_downtown", "faction_michael", initialLoyalty: 50);

            // Act
            loyalty.SetLoyalty(75);

            // Assert
            Assert.Equal(75, loyalty.LoyaltyValue);
        }

        [Fact]
        public void ZoneLoyalty_SetLoyalty_ShouldClampValue()
        {
            // Arrange
            var loyalty = new ZoneLoyalty("zone_downtown", "faction_michael", initialLoyalty: 50);

            // Act
            loyalty.SetLoyalty(150);

            // Assert
            Assert.Equal(100, loyalty.LoyaltyValue);
        }

        #endregion

        #region Loyalty Level Classification

        [Fact]
        public void ZoneLoyalty_Level_ShouldBeHostileWhenBelowThreshold()
        {
            // Arrange
            var loyalty = new ZoneLoyalty("zone_downtown", "faction_michael", initialLoyalty: 10);

            // Assert - Below 20 is Hostile
            Assert.Equal(LoyaltyLevel.Hostile, loyalty.Level);
        }

        [Fact]
        public void ZoneLoyalty_Level_ShouldBeResistantWhenLow()
        {
            // Arrange
            var loyalty = new ZoneLoyalty("zone_downtown", "faction_michael", initialLoyalty: 30);

            // Assert - 20-39 is Resistant
            Assert.Equal(LoyaltyLevel.Resistant, loyalty.Level);
        }

        [Fact]
        public void ZoneLoyalty_Level_ShouldBeNeutralWhenModerate()
        {
            // Arrange
            var loyalty = new ZoneLoyalty("zone_downtown", "faction_michael", initialLoyalty: 50);

            // Assert - 40-59 is Neutral
            Assert.Equal(LoyaltyLevel.Neutral, loyalty.Level);
        }

        [Fact]
        public void ZoneLoyalty_Level_ShouldBeSupportiveWhenHigh()
        {
            // Arrange
            var loyalty = new ZoneLoyalty("zone_downtown", "faction_michael", initialLoyalty: 70);

            // Assert - 60-79 is Supportive
            Assert.Equal(LoyaltyLevel.Supportive, loyalty.Level);
        }

        [Fact]
        public void ZoneLoyalty_Level_ShouldBeFanaticalWhenVeryHigh()
        {
            // Arrange
            var loyalty = new ZoneLoyalty("zone_downtown", "faction_michael", initialLoyalty: 90);

            // Assert - 80+ is Fanatical
            Assert.Equal(LoyaltyLevel.Fanatical, loyalty.Level);
        }

        #endregion

        #region Loyalty State Flags

        [Fact]
        public void ZoneLoyalty_IsStable_ShouldReturnTrueWhenNeutralOrHigher()
        {
            // Arrange
            var loyalty = new ZoneLoyalty("zone_downtown", "faction_michael", initialLoyalty: 50);

            // Assert
            Assert.True(loyalty.IsStable);
        }

        [Fact]
        public void ZoneLoyalty_IsStable_ShouldReturnFalseWhenResistant()
        {
            // Arrange
            var loyalty = new ZoneLoyalty("zone_downtown", "faction_michael", initialLoyalty: 30);

            // Assert
            Assert.False(loyalty.IsStable);
        }

        [Fact]
        public void ZoneLoyalty_IsAtRiskOfInsurgency_ShouldReturnTrueWhenHostile()
        {
            // Arrange
            var loyalty = new ZoneLoyalty("zone_downtown", "faction_michael", initialLoyalty: 15);

            // Assert
            Assert.True(loyalty.IsAtRiskOfInsurgency);
        }

        [Fact]
        public void ZoneLoyalty_IsAtRiskOfInsurgency_ShouldReturnFalseWhenResistant()
        {
            // Arrange
            var loyalty = new ZoneLoyalty("zone_downtown", "faction_michael", initialLoyalty: 30);

            // Assert
            Assert.False(loyalty.IsAtRiskOfInsurgency);
        }

        [Fact]
        public void ZoneLoyalty_IsFullyLoyal_ShouldReturnTrueWhenFanatical()
        {
            // Arrange
            var loyalty = new ZoneLoyalty("zone_downtown", "faction_michael", initialLoyalty: 85);

            // Assert
            Assert.True(loyalty.IsFullyLoyal);
        }

        [Fact]
        public void ZoneLoyalty_IsFullyLoyal_ShouldReturnFalseWhenSupportive()
        {
            // Arrange
            var loyalty = new ZoneLoyalty("zone_downtown", "faction_michael", initialLoyalty: 70);

            // Assert
            Assert.False(loyalty.IsFullyLoyal);
        }

        #endregion

        #region Faction Ownership History

        [Fact]
        public void ZoneLoyalty_PreviousFactionId_ShouldBeNullByDefault()
        {
            // Arrange & Act
            var loyalty = new ZoneLoyalty("zone_downtown", "faction_michael");

            // Assert
            Assert.Null(loyalty.PreviousFactionId);
        }

        [Fact]
        public void ZoneLoyalty_ShouldAllowSettingPreviousFaction()
        {
            // Arrange & Act
            var loyalty = new ZoneLoyalty("zone_downtown", "faction_michael", previousFactionId: "faction_trevor");

            // Assert
            Assert.Equal("faction_trevor", loyalty.PreviousFactionId);
        }

        [Fact]
        public void ZoneLoyalty_TransferControl_ShouldUpdateControllingFaction()
        {
            // Arrange
            var loyalty = new ZoneLoyalty("zone_downtown", "faction_michael", initialLoyalty: 80);

            // Act
            loyalty.TransferControl("faction_trevor");

            // Assert
            Assert.Equal("faction_trevor", loyalty.ControllingFactionId);
        }

        [Fact]
        public void ZoneLoyalty_TransferControl_ShouldStorePreviousFaction()
        {
            // Arrange
            var loyalty = new ZoneLoyalty("zone_downtown", "faction_michael", initialLoyalty: 80);

            // Act
            loyalty.TransferControl("faction_trevor");

            // Assert
            Assert.Equal("faction_michael", loyalty.PreviousFactionId);
        }

        [Fact]
        public void ZoneLoyalty_TransferControl_ShouldResetLoyalty()
        {
            // Arrange
            var loyalty = new ZoneLoyalty("zone_downtown", "faction_michael", initialLoyalty: 80);

            // Act
            loyalty.TransferControl("faction_trevor");

            // Assert - When control changes, loyalty resets to a low value (30 - Resistant)
            Assert.Equal(30, loyalty.LoyaltyValue);
        }

        [Fact]
        public void ZoneLoyalty_TransferControl_ShouldThrowWhenTransferringToSameFaction()
        {
            // Arrange
            var loyalty = new ZoneLoyalty("zone_downtown", "faction_michael", initialLoyalty: 80);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => loyalty.TransferControl("faction_michael"));
        }

        [Fact]
        public void ZoneLoyalty_TransferControl_ShouldIncrementTransferCount()
        {
            // Arrange
            var loyalty = new ZoneLoyalty("zone_downtown", "faction_michael");

            // Act
            loyalty.TransferControl("faction_trevor");
            loyalty.TransferControl("faction_franklin");

            // Assert
            Assert.Equal(2, loyalty.TransferCount);
        }

        [Fact]
        public void ZoneLoyalty_TransferCount_ShouldBeZeroByDefault()
        {
            // Arrange & Act
            var loyalty = new ZoneLoyalty("zone_downtown", "faction_michael");

            // Assert
            Assert.Equal(0, loyalty.TransferCount);
        }

        #endregion

        #region Time Tracking

        [Fact]
        public void ZoneLoyalty_DaysUnderControl_ShouldBeZeroByDefault()
        {
            // Arrange & Act
            var loyalty = new ZoneLoyalty("zone_downtown", "faction_michael");

            // Assert
            Assert.Equal(0, loyalty.DaysUnderControl);
        }

        [Fact]
        public void ZoneLoyalty_AdvanceDay_ShouldIncrementDaysUnderControl()
        {
            // Arrange
            var loyalty = new ZoneLoyalty("zone_downtown", "faction_michael");

            // Act
            loyalty.AdvanceDay();

            // Assert
            Assert.Equal(1, loyalty.DaysUnderControl);
        }

        [Fact]
        public void ZoneLoyalty_AdvanceDays_ShouldIncrementByAmount()
        {
            // Arrange
            var loyalty = new ZoneLoyalty("zone_downtown", "faction_michael");

            // Act
            loyalty.AdvanceDays(5);

            // Assert
            Assert.Equal(5, loyalty.DaysUnderControl);
        }

        [Fact]
        public void ZoneLoyalty_TransferControl_ShouldResetDaysUnderControl()
        {
            // Arrange
            var loyalty = new ZoneLoyalty("zone_downtown", "faction_michael");
            loyalty.AdvanceDays(10);

            // Act
            loyalty.TransferControl("faction_trevor");

            // Assert
            Assert.Equal(0, loyalty.DaysUnderControl);
        }

        #endregion

        #region Resource Multiplier

        [Fact]
        public void ZoneLoyalty_ResourceMultiplier_ShouldBeLowWhenHostile()
        {
            // Arrange
            var loyalty = new ZoneLoyalty("zone_downtown", "faction_michael", initialLoyalty: 10);

            // Assert - Hostile zones produce at 50% efficiency
            Assert.Equal(0.5f, loyalty.ResourceMultiplier, 2);
        }

        [Fact]
        public void ZoneLoyalty_ResourceMultiplier_ShouldBeReducedWhenResistant()
        {
            // Arrange
            var loyalty = new ZoneLoyalty("zone_downtown", "faction_michael", initialLoyalty: 30);

            // Assert - Resistant zones produce at 70% efficiency
            Assert.Equal(0.7f, loyalty.ResourceMultiplier, 2);
        }

        [Fact]
        public void ZoneLoyalty_ResourceMultiplier_ShouldBeNormalWhenNeutral()
        {
            // Arrange
            var loyalty = new ZoneLoyalty("zone_downtown", "faction_michael", initialLoyalty: 50);

            // Assert - Neutral zones produce at 100% efficiency
            Assert.Equal(1.0f, loyalty.ResourceMultiplier, 2);
        }

        [Fact]
        public void ZoneLoyalty_ResourceMultiplier_ShouldBeBoostedWhenSupportive()
        {
            // Arrange
            var loyalty = new ZoneLoyalty("zone_downtown", "faction_michael", initialLoyalty: 70);

            // Assert - Supportive zones produce at 115% efficiency
            Assert.Equal(1.15f, loyalty.ResourceMultiplier, 2);
        }

        [Fact]
        public void ZoneLoyalty_ResourceMultiplier_ShouldBeHighWhenFanatical()
        {
            // Arrange
            var loyalty = new ZoneLoyalty("zone_downtown", "faction_michael", initialLoyalty: 90);

            // Assert - Fanatical zones produce at 130% efficiency
            Assert.Equal(1.3f, loyalty.ResourceMultiplier, 2);
        }

        #endregion

        #region Defense Bonus

        [Fact]
        public void ZoneLoyalty_DefenseBonus_ShouldBeNegativeWhenHostile()
        {
            // Arrange
            var loyalty = new ZoneLoyalty("zone_downtown", "faction_michael", initialLoyalty: 10);

            // Assert - Hostile zones give -20% defense
            Assert.Equal(-20, loyalty.DefenseBonus);
        }

        [Fact]
        public void ZoneLoyalty_DefenseBonus_ShouldBeNegativeWhenResistant()
        {
            // Arrange
            var loyalty = new ZoneLoyalty("zone_downtown", "faction_michael", initialLoyalty: 30);

            // Assert - Resistant zones give -10% defense
            Assert.Equal(-10, loyalty.DefenseBonus);
        }

        [Fact]
        public void ZoneLoyalty_DefenseBonus_ShouldBeZeroWhenNeutral()
        {
            // Arrange
            var loyalty = new ZoneLoyalty("zone_downtown", "faction_michael", initialLoyalty: 50);

            // Assert - Neutral zones give 0% defense bonus
            Assert.Equal(0, loyalty.DefenseBonus);
        }

        [Fact]
        public void ZoneLoyalty_DefenseBonus_ShouldBePositiveWhenSupportive()
        {
            // Arrange
            var loyalty = new ZoneLoyalty("zone_downtown", "faction_michael", initialLoyalty: 70);

            // Assert - Supportive zones give +10% defense
            Assert.Equal(10, loyalty.DefenseBonus);
        }

        [Fact]
        public void ZoneLoyalty_DefenseBonus_ShouldBeHighWhenFanatical()
        {
            // Arrange
            var loyalty = new ZoneLoyalty("zone_downtown", "faction_michael", initialLoyalty: 90);

            // Assert - Fanatical zones give +25% defense
            Assert.Equal(25, loyalty.DefenseBonus);
        }

        #endregion

        #region Equality

        [Fact]
        public void ZoneLoyalty_ShouldBeEqualByZoneId()
        {
            // Arrange
            var loyalty1 = new ZoneLoyalty("zone_downtown", "faction_michael", initialLoyalty: 50);
            var loyalty2 = new ZoneLoyalty("zone_downtown", "faction_trevor", initialLoyalty: 80);

            // Act & Assert - ZoneLoyalty is equal if it refers to the same zone
            Assert.Equal(loyalty1, loyalty2);
        }

        [Fact]
        public void ZoneLoyalty_ShouldNotBeEqualWithDifferentZoneId()
        {
            // Arrange
            var loyalty1 = new ZoneLoyalty("zone_downtown", "faction_michael");
            var loyalty2 = new ZoneLoyalty("zone_vinewood", "faction_michael");

            // Act & Assert
            Assert.NotEqual(loyalty1, loyalty2);
        }

        [Fact]
        public void ZoneLoyalty_GetHashCode_ShouldBeConsistentWithEquals()
        {
            // Arrange
            var loyalty1 = new ZoneLoyalty("zone_downtown", "faction_michael", initialLoyalty: 50);
            var loyalty2 = new ZoneLoyalty("zone_downtown", "faction_trevor", initialLoyalty: 80);

            // Act & Assert - Equal objects must have equal hash codes
            Assert.Equal(loyalty1.GetHashCode(), loyalty2.GetHashCode());
        }

        [Fact]
        public void ZoneLoyalty_ShouldNotBeEqualToNull()
        {
            // Arrange
            var loyalty = new ZoneLoyalty("zone_downtown", "faction_michael");

            // Act & Assert
            Assert.False(loyalty.Equals(null));
        }

        [Fact]
        public void ZoneLoyalty_EqualityOperator_ShouldWork()
        {
            // Arrange
            var loyalty1 = new ZoneLoyalty("zone_downtown", "faction_michael");
            var loyalty2 = new ZoneLoyalty("zone_downtown", "faction_trevor");

            // Act & Assert
            Assert.True(loyalty1 == loyalty2);
        }

        [Fact]
        public void ZoneLoyalty_InequalityOperator_ShouldWork()
        {
            // Arrange
            var loyalty1 = new ZoneLoyalty("zone_downtown", "faction_michael");
            var loyalty2 = new ZoneLoyalty("zone_vinewood", "faction_michael");

            // Act & Assert
            Assert.True(loyalty1 != loyalty2);
        }

        [Fact]
        public void ZoneLoyalty_NullEquality_ShouldHandleNullLeft()
        {
            // Arrange
            ZoneLoyalty? loyalty1 = null;
            var loyalty2 = new ZoneLoyalty("zone_downtown", "faction_michael");

            // Act & Assert
            Assert.True(loyalty1 != loyalty2);
            Assert.False(loyalty1 == loyalty2);
        }

        [Fact]
        public void ZoneLoyalty_NullEquality_ShouldHandleBothNull()
        {
            // Arrange
            ZoneLoyalty? loyalty1 = null;
            ZoneLoyalty? loyalty2 = null;

            // Act & Assert
            Assert.True(loyalty1 == loyalty2);
        }

        #endregion

        #region ToString

        [Fact]
        public void ZoneLoyalty_ToString_ShouldReturnReadableFormat()
        {
            // Arrange
            var loyalty = new ZoneLoyalty("zone_downtown", "faction_michael", initialLoyalty: 75);

            // Act
            var result = loyalty.ToString();

            // Assert
            Assert.Contains("zone_downtown", result);
            Assert.Contains("75", result);
        }

        #endregion
    }
}
