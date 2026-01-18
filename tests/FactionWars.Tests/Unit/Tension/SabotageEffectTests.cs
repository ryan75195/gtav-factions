using System;
using FactionWars.Tension.Models;
using Xunit;

namespace FactionWars.Tests.Unit.Tension
{
    /// <summary>
    /// Tests for the SabotageEffect class which represents the result of a sabotage operation.
    /// </summary>
    public class SabotageEffectTests
    {
        #region Constructor Tests

        [Fact]
        public void Constructor_WithValidParameters_CreatesEffect()
        {
            var effect = new SabotageEffect("zone1", SabotageTargetType.ResourceProduction, 0.25f);

            Assert.Equal("zone1", effect.TargetZoneId);
            Assert.Equal(SabotageTargetType.ResourceProduction, effect.TargetType);
            Assert.Equal(0.25f, effect.DisruptionAmount, 2);
        }

        [Fact]
        public void Constructor_WithNullZoneId_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new SabotageEffect(null!, SabotageTargetType.ResourceProduction, 0.25f));
        }

        [Fact]
        public void Constructor_WithEmptyZoneId_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                new SabotageEffect("", SabotageTargetType.ResourceProduction, 0.25f));
        }

        [Fact]
        public void Constructor_WithNegativeDisruptionAmount_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                new SabotageEffect("zone1", SabotageTargetType.ResourceProduction, -0.1f));
        }

        [Fact]
        public void Constructor_WithDisruptionAmountOverOne_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                new SabotageEffect("zone1", SabotageTargetType.ResourceProduction, 1.5f));
        }

        [Fact]
        public void Constructor_WithZeroDisruptionAmount_Succeeds()
        {
            var effect = new SabotageEffect("zone1", SabotageTargetType.ResourceProduction, 0f);
            Assert.Equal(0f, effect.DisruptionAmount);
        }

        [Fact]
        public void Constructor_WithMaxDisruptionAmount_Succeeds()
        {
            var effect = new SabotageEffect("zone1", SabotageTargetType.ResourceProduction, 1f);
            Assert.Equal(1f, effect.DisruptionAmount);
        }

        #endregion

        #region Duration Tests

        [Theory]
        [InlineData(SabotageTargetType.ResourceProduction, 300)]
        [InlineData(SabotageTargetType.DefenseRating, 180)]
        [InlineData(SabotageTargetType.RecruitmentRate, 240)]
        [InlineData(SabotageTargetType.SupplyLine, 120)]
        public void BaseDurationSeconds_ForTargetType_ReturnsExpectedValue(SabotageTargetType targetType, int expectedDuration)
        {
            var effect = new SabotageEffect("zone1", targetType, 0.25f);
            Assert.Equal(expectedDuration, effect.BaseDurationSeconds);
        }

        [Fact]
        public void EffectiveDurationSeconds_ScalesWithDisruptionAmount()
        {
            var effect = new SabotageEffect("zone1", SabotageTargetType.ResourceProduction, 0.5f);
            // Base duration is 300s, with 0.5 disruption should scale
            int expected = (int)(300 * (1.0f + 0.5f));
            Assert.Equal(expected, effect.EffectiveDurationSeconds);
        }

        #endregion

        #region Tension Impact Tests

        [Theory]
        [InlineData(SabotageTargetType.ResourceProduction, 10)]
        [InlineData(SabotageTargetType.DefenseRating, 15)]
        [InlineData(SabotageTargetType.RecruitmentRate, 8)]
        [InlineData(SabotageTargetType.SupplyLine, 12)]
        public void BaseTensionIncrease_ForTargetType_ReturnsExpectedValue(SabotageTargetType targetType, int expectedTension)
        {
            var effect = new SabotageEffect("zone1", targetType, 0.25f);
            Assert.Equal(expectedTension, effect.BaseTensionIncrease);
        }

        [Fact]
        public void EffectiveTensionIncrease_ScalesWithDisruptionAmount()
        {
            var effect = new SabotageEffect("zone1", SabotageTargetType.ResourceProduction, 0.5f);
            // Base tension is 10, with 0.5 disruption should scale
            int expected = (int)(10 * (1.0f + 0.5f));
            Assert.Equal(expected, effect.EffectiveTensionIncrease);
        }

        #endregion

        #region State Tests

        [Fact]
        public void IsActive_WhenNotExpired_ReturnsTrue()
        {
            var effect = new SabotageEffect("zone1", SabotageTargetType.ResourceProduction, 0.25f);
            Assert.True(effect.IsActive);
        }

        [Fact]
        public void GetRemainingSeconds_WhenJustCreated_ReturnsEffectiveDuration()
        {
            var effect = new SabotageEffect("zone1", SabotageTargetType.ResourceProduction, 0.25f);
            var remaining = effect.GetRemainingSeconds();
            // Allow small tolerance for test execution time
            Assert.InRange(remaining, effect.EffectiveDurationSeconds - 1, effect.EffectiveDurationSeconds);
        }

        #endregion

        #region ToString Tests

        [Fact]
        public void ToString_ContainsTargetType()
        {
            var effect = new SabotageEffect("zone1", SabotageTargetType.ResourceProduction, 0.25f);
            Assert.Contains("ResourceProduction", effect.ToString());
        }

        [Fact]
        public void ToString_ContainsZoneId()
        {
            var effect = new SabotageEffect("zone1", SabotageTargetType.ResourceProduction, 0.25f);
            Assert.Contains("zone1", effect.ToString());
        }

        [Fact]
        public void ToString_ContainsDisruptionPercentage()
        {
            var effect = new SabotageEffect("zone1", SabotageTargetType.ResourceProduction, 0.25f);
            Assert.Contains("25%", effect.ToString());
        }

        #endregion
    }
}
