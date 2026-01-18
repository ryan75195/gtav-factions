using FactionWars.Loyalty.Models;
using System;
using Xunit;

namespace FactionWars.Tests.Unit.Loyalty
{
    public class LoyaltyModifierTests
    {
        #region LoyaltyModifierType Enum

        [Fact]
        public void LoyaltyModifierType_ShouldHaveTimeBasedGainType()
        {
            // Assert - Daily passive gain for time under control
            Assert.True(Enum.IsDefined(typeof(LoyaltyModifierType), LoyaltyModifierType.TimeBasedGain));
        }

        [Fact]
        public void LoyaltyModifierType_ShouldHaveCombatVictoryType()
        {
            // Assert - Bonus for winning defensive battles in the zone
            Assert.True(Enum.IsDefined(typeof(LoyaltyModifierType), LoyaltyModifierType.CombatVictory));
        }

        [Fact]
        public void LoyaltyModifierType_ShouldHaveCombatDefeatType()
        {
            // Assert - Penalty for losing defensive battles
            Assert.True(Enum.IsDefined(typeof(LoyaltyModifierType), LoyaltyModifierType.CombatDefeat));
        }

        [Fact]
        public void LoyaltyModifierType_ShouldHaveResourceInvestmentType()
        {
            // Assert - Bonus for spending resources on the zone
            Assert.True(Enum.IsDefined(typeof(LoyaltyModifierType), LoyaltyModifierType.ResourceInvestment));
        }

        [Fact]
        public void LoyaltyModifierType_ShouldHaveOppressionType()
        {
            // Assert - Penalty for aggressive faction presence
            Assert.True(Enum.IsDefined(typeof(LoyaltyModifierType), LoyaltyModifierType.Oppression));
        }

        [Fact]
        public void LoyaltyModifierType_ShouldHavePropagandaType()
        {
            // Assert - Bonus from propaganda operations
            Assert.True(Enum.IsDefined(typeof(LoyaltyModifierType), LoyaltyModifierType.Propaganda));
        }

        [Fact]
        public void LoyaltyModifierType_ShouldHaveNeighborInfluenceType()
        {
            // Assert - Influence from neighboring zone loyalty levels
            Assert.True(Enum.IsDefined(typeof(LoyaltyModifierType), LoyaltyModifierType.NeighborInfluence));
        }

        [Fact]
        public void LoyaltyModifierType_ShouldHaveRecentConquestType()
        {
            // Assert - Penalty from recent faction change
            Assert.True(Enum.IsDefined(typeof(LoyaltyModifierType), LoyaltyModifierType.RecentConquest));
        }

        #endregion

        #region LoyaltyModifier Constructor and Properties

        [Fact]
        public void LoyaltyModifier_ShouldRequireType()
        {
            // Arrange & Act
            var modifier = new LoyaltyModifier(LoyaltyModifierType.TimeBasedGain, 2);

            // Assert
            Assert.Equal(LoyaltyModifierType.TimeBasedGain, modifier.Type);
        }

        [Fact]
        public void LoyaltyModifier_ShouldRequireValue()
        {
            // Arrange & Act
            var modifier = new LoyaltyModifier(LoyaltyModifierType.CombatVictory, 5);

            // Assert
            Assert.Equal(5, modifier.Value);
        }

        [Fact]
        public void LoyaltyModifier_ShouldAllowNegativeValue()
        {
            // Arrange & Act
            var modifier = new LoyaltyModifier(LoyaltyModifierType.CombatDefeat, -10);

            // Assert
            Assert.Equal(-10, modifier.Value);
        }

        [Fact]
        public void LoyaltyModifier_ShouldHaveOptionalDescription()
        {
            // Arrange & Act
            var modifier = new LoyaltyModifier(LoyaltyModifierType.ResourceInvestment, 3, "Community funding");

            // Assert
            Assert.Equal("Community funding", modifier.Description);
        }

        [Fact]
        public void LoyaltyModifier_ShouldHaveDefaultEmptyDescription()
        {
            // Arrange & Act
            var modifier = new LoyaltyModifier(LoyaltyModifierType.TimeBasedGain, 2);

            // Assert
            Assert.NotNull(modifier.Description);
            Assert.Empty(modifier.Description);
        }

        [Fact]
        public void LoyaltyModifier_ShouldHaveCreatedTimestamp()
        {
            // Arrange
            var before = DateTime.UtcNow;

            // Act
            var modifier = new LoyaltyModifier(LoyaltyModifierType.TimeBasedGain, 2);

            // Assert
            var after = DateTime.UtcNow;
            Assert.True(modifier.CreatedAt >= before && modifier.CreatedAt <= after);
        }

        [Fact]
        public void LoyaltyModifier_ShouldHaveOptionalDuration()
        {
            // Arrange & Act - Temporary modifier lasting 5 days
            var modifier = new LoyaltyModifier(LoyaltyModifierType.Propaganda, 5, durationDays: 5);

            // Assert
            Assert.Equal(5, modifier.DurationDays);
        }

        [Fact]
        public void LoyaltyModifier_ShouldHaveZeroDurationByDefault()
        {
            // Arrange & Act - Permanent modifier (0 = no expiration)
            var modifier = new LoyaltyModifier(LoyaltyModifierType.TimeBasedGain, 2);

            // Assert
            Assert.Equal(0, modifier.DurationDays);
        }

        [Fact]
        public void LoyaltyModifier_IsPermanent_ShouldReturnTrueWhenNoDuration()
        {
            // Arrange
            var modifier = new LoyaltyModifier(LoyaltyModifierType.TimeBasedGain, 2);

            // Assert
            Assert.True(modifier.IsPermanent);
        }

        [Fact]
        public void LoyaltyModifier_IsPermanent_ShouldReturnFalseWhenHasDuration()
        {
            // Arrange
            var modifier = new LoyaltyModifier(LoyaltyModifierType.Propaganda, 5, durationDays: 5);

            // Assert
            Assert.False(modifier.IsPermanent);
        }

        [Fact]
        public void LoyaltyModifier_IsPositive_ShouldReturnTrueForPositiveValue()
        {
            // Arrange
            var modifier = new LoyaltyModifier(LoyaltyModifierType.CombatVictory, 5);

            // Assert
            Assert.True(modifier.IsPositive);
        }

        [Fact]
        public void LoyaltyModifier_IsPositive_ShouldReturnFalseForNegativeValue()
        {
            // Arrange
            var modifier = new LoyaltyModifier(LoyaltyModifierType.CombatDefeat, -5);

            // Assert
            Assert.False(modifier.IsPositive);
        }

        [Fact]
        public void LoyaltyModifier_IsPositive_ShouldReturnFalseForZeroValue()
        {
            // Arrange
            var modifier = new LoyaltyModifier(LoyaltyModifierType.NeighborInfluence, 0);

            // Assert
            Assert.False(modifier.IsPositive);
        }

        #endregion

        #region LoyaltyModifier Equality

        [Fact]
        public void LoyaltyModifier_ShouldBeEqualByProperties()
        {
            // Arrange
            var modifier1 = new LoyaltyModifier(LoyaltyModifierType.CombatVictory, 5, "Battle won");
            var modifier2 = new LoyaltyModifier(LoyaltyModifierType.CombatVictory, 5, "Battle won");

            // Assert - Value equality based on type, value, and description
            Assert.Equal(modifier1.Type, modifier2.Type);
            Assert.Equal(modifier1.Value, modifier2.Value);
            Assert.Equal(modifier1.Description, modifier2.Description);
        }

        #endregion

        #region LoyaltyModifier ToString

        [Fact]
        public void LoyaltyModifier_ToString_ShouldShowPositiveModifier()
        {
            // Arrange
            var modifier = new LoyaltyModifier(LoyaltyModifierType.CombatVictory, 5);

            // Act
            var result = modifier.ToString();

            // Assert
            Assert.Contains("+5", result);
            Assert.Contains("CombatVictory", result);
        }

        [Fact]
        public void LoyaltyModifier_ToString_ShouldShowNegativeModifier()
        {
            // Arrange
            var modifier = new LoyaltyModifier(LoyaltyModifierType.CombatDefeat, -10);

            // Act
            var result = modifier.ToString();

            // Assert
            Assert.Contains("-10", result);
            Assert.Contains("CombatDefeat", result);
        }

        #endregion

        #region Standard Modifier Values

        [Fact]
        public void LoyaltyModifier_TimeBasedGainDefault_ShouldBeSmallPositive()
        {
            // Assert - Daily passive gain should be small (1-3 points)
            var defaultValue = LoyaltyModifier.DefaultTimeBasedGain;
            Assert.InRange(defaultValue, 1, 3);
        }

        [Fact]
        public void LoyaltyModifier_CombatVictoryDefault_ShouldBeModerateBonuse()
        {
            // Assert - Winning battles should give 5-10 points
            var defaultValue = LoyaltyModifier.DefaultCombatVictoryBonus;
            Assert.InRange(defaultValue, 5, 10);
        }

        [Fact]
        public void LoyaltyModifier_CombatDefeatDefault_ShouldBeNegative()
        {
            // Assert - Losing battles should cost 3-8 points
            var defaultValue = LoyaltyModifier.DefaultCombatDefeatPenalty;
            Assert.InRange(defaultValue, -8, -3);
        }

        [Fact]
        public void LoyaltyModifier_ResourceInvestmentDefault_ShouldBeSmallBonus()
        {
            // Assert - Investment should give 2-5 points per unit
            var defaultValue = LoyaltyModifier.DefaultResourceInvestmentBonus;
            Assert.InRange(defaultValue, 2, 5);
        }

        [Fact]
        public void LoyaltyModifier_OppressionDefault_ShouldBeNegative()
        {
            // Assert - Oppression should cost 1-3 points per incident
            var defaultValue = LoyaltyModifier.DefaultOppressionPenalty;
            Assert.InRange(defaultValue, -3, -1);
        }

        [Fact]
        public void LoyaltyModifier_PropagandaDefault_ShouldBeModerateBonus()
        {
            // Assert - Propaganda operations give 3-7 points
            var defaultValue = LoyaltyModifier.DefaultPropagandaBonus;
            Assert.InRange(defaultValue, 3, 7);
        }

        [Fact]
        public void LoyaltyModifier_RecentConquestDefault_ShouldBeNegative()
        {
            // Assert - Recent conquest penalty should be significant (5-15 points)
            var defaultValue = LoyaltyModifier.DefaultRecentConquestPenalty;
            Assert.InRange(defaultValue, -15, -5);
        }

        #endregion

        #region Factory Methods

        [Fact]
        public void LoyaltyModifier_CreateTimeBasedGain_ShouldUseDefaultValue()
        {
            // Act
            var modifier = LoyaltyModifier.CreateTimeBasedGain();

            // Assert
            Assert.Equal(LoyaltyModifierType.TimeBasedGain, modifier.Type);
            Assert.Equal(LoyaltyModifier.DefaultTimeBasedGain, modifier.Value);
            Assert.True(modifier.IsPermanent);
        }

        [Fact]
        public void LoyaltyModifier_CreateCombatVictory_ShouldUseDefaultValue()
        {
            // Act
            var modifier = LoyaltyModifier.CreateCombatVictory();

            // Assert
            Assert.Equal(LoyaltyModifierType.CombatVictory, modifier.Type);
            Assert.Equal(LoyaltyModifier.DefaultCombatVictoryBonus, modifier.Value);
        }

        [Fact]
        public void LoyaltyModifier_CreateCombatDefeat_ShouldUseDefaultValue()
        {
            // Act
            var modifier = LoyaltyModifier.CreateCombatDefeat();

            // Assert
            Assert.Equal(LoyaltyModifierType.CombatDefeat, modifier.Type);
            Assert.Equal(LoyaltyModifier.DefaultCombatDefeatPenalty, modifier.Value);
        }

        [Fact]
        public void LoyaltyModifier_CreateResourceInvestment_ShouldUseDefaultValue()
        {
            // Act
            var modifier = LoyaltyModifier.CreateResourceInvestment();

            // Assert
            Assert.Equal(LoyaltyModifierType.ResourceInvestment, modifier.Type);
            Assert.Equal(LoyaltyModifier.DefaultResourceInvestmentBonus, modifier.Value);
        }

        [Fact]
        public void LoyaltyModifier_CreateResourceInvestment_ShouldAllowCustomAmount()
        {
            // Act
            var modifier = LoyaltyModifier.CreateResourceInvestment(amount: 10);

            // Assert
            Assert.Equal(LoyaltyModifierType.ResourceInvestment, modifier.Type);
            Assert.Equal(10, modifier.Value);
        }

        [Fact]
        public void LoyaltyModifier_CreatePropaganda_ShouldHaveDuration()
        {
            // Act
            var modifier = LoyaltyModifier.CreatePropaganda();

            // Assert
            Assert.Equal(LoyaltyModifierType.Propaganda, modifier.Type);
            Assert.Equal(LoyaltyModifier.DefaultPropagandaBonus, modifier.Value);
            Assert.False(modifier.IsPermanent);
            Assert.True(modifier.DurationDays > 0);
        }

        [Fact]
        public void LoyaltyModifier_CreateRecentConquest_ShouldHaveDuration()
        {
            // Act
            var modifier = LoyaltyModifier.CreateRecentConquest();

            // Assert
            Assert.Equal(LoyaltyModifierType.RecentConquest, modifier.Type);
            Assert.Equal(LoyaltyModifier.DefaultRecentConquestPenalty, modifier.Value);
            Assert.False(modifier.IsPermanent);
        }

        [Fact]
        public void LoyaltyModifier_CreateNeighborInfluence_ShouldAcceptValue()
        {
            // Act - Can be positive or negative based on neighbor loyalty
            var positiveModifier = LoyaltyModifier.CreateNeighborInfluence(3);
            var negativeModifier = LoyaltyModifier.CreateNeighborInfluence(-2);

            // Assert
            Assert.Equal(LoyaltyModifierType.NeighborInfluence, positiveModifier.Type);
            Assert.Equal(3, positiveModifier.Value);
            Assert.Equal(-2, negativeModifier.Value);
        }

        [Fact]
        public void LoyaltyModifier_CreateOppression_ShouldUseDefaultValue()
        {
            // Act
            var modifier = LoyaltyModifier.CreateOppression();

            // Assert
            Assert.Equal(LoyaltyModifierType.Oppression, modifier.Type);
            Assert.Equal(LoyaltyModifier.DefaultOppressionPenalty, modifier.Value);
        }

        #endregion
    }
}
