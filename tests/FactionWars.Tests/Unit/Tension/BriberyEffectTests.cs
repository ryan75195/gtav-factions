using System;
using FactionWars.Tension.Models;
using Xunit;

namespace FactionWars.Tests.Unit.Tension
{
    /// <summary>
    /// Tests for the BriberyEffect class which represents the result of a bribery operation.
    /// </summary>
    public class BriberyEffectTests
    {
        #region Constructor Tests

        [Fact]
        public void Constructor_WithValidParameters_CreatesEffect()
        {
            var effect = new BriberyEffect("faction1", BriberyTargetType.IntelligenceAsset, 5000);

            Assert.Equal("faction1", effect.TargetFactionId);
            Assert.Equal(BriberyTargetType.IntelligenceAsset, effect.TargetType);
            Assert.Equal(5000, effect.BribeAmount);
        }

        [Fact]
        public void Constructor_WithNullFactionId_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new BriberyEffect(null!, BriberyTargetType.IntelligenceAsset, 5000));
        }

        [Fact]
        public void Constructor_WithEmptyFactionId_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                new BriberyEffect("", BriberyTargetType.IntelligenceAsset, 5000));
        }

        [Fact]
        public void Constructor_WithNegativeBribeAmount_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                new BriberyEffect("faction1", BriberyTargetType.IntelligenceAsset, -100));
        }

        [Fact]
        public void Constructor_WithZeroBribeAmount_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                new BriberyEffect("faction1", BriberyTargetType.IntelligenceAsset, 0));
        }

        #endregion

        #region Tension Impact Tests

        [Theory]
        [InlineData(BriberyTargetType.IntelligenceAsset, -5)]
        [InlineData(BriberyTargetType.ResourceDiversion, 8)]
        [InlineData(BriberyTargetType.DefectorRecruitment, 12)]
        [InlineData(BriberyTargetType.TensionReduction, -15)]
        public void BaseTensionChange_ForTargetType_ReturnsExpectedValue(BriberyTargetType targetType, int expectedChange)
        {
            var effect = new BriberyEffect("faction1", targetType, 5000);
            Assert.Equal(expectedChange, effect.BaseTensionChange);
        }

        [Fact]
        public void EffectiveTensionChange_ScalesWithBribeAmount()
        {
            // Higher bribes should have greater effect
            var smallBribe = new BriberyEffect("faction1", BriberyTargetType.TensionReduction, 2000);
            var largeBribe = new BriberyEffect("faction1", BriberyTargetType.TensionReduction, 20000);

            // Large bribe should have more (negative) tension reduction
            Assert.True(largeBribe.EffectiveTensionChange < smallBribe.EffectiveTensionChange);
        }

        #endregion

        #region Duration Tests

        [Theory]
        [InlineData(BriberyTargetType.IntelligenceAsset, 600)]
        [InlineData(BriberyTargetType.ResourceDiversion, 300)]
        [InlineData(BriberyTargetType.DefectorRecruitment, 0)] // Permanent effect
        [InlineData(BriberyTargetType.TensionReduction, 0)] // Instant effect
        public void EffectDurationSeconds_ForTargetType_ReturnsExpectedValue(BriberyTargetType targetType, int expectedDuration)
        {
            var effect = new BriberyEffect("faction1", targetType, 5000);
            Assert.Equal(expectedDuration, effect.EffectDurationSeconds);
        }

        #endregion

        #region Specific Effect Tests

        [Fact]
        public void IntelligenceAsset_ProvidesVisionBonus()
        {
            var effect = new BriberyEffect("faction1", BriberyTargetType.IntelligenceAsset, 5000);
            Assert.True(effect.ProvidesIntelligence);
            Assert.False(effect.DiversResources);
            Assert.False(effect.RecruitDefector);
        }

        [Fact]
        public void ResourceDiversion_DiversResources()
        {
            var effect = new BriberyEffect("faction1", BriberyTargetType.ResourceDiversion, 5000);
            Assert.False(effect.ProvidesIntelligence);
            Assert.True(effect.DiversResources);
            Assert.False(effect.RecruitDefector);
        }

        [Fact]
        public void DefectorRecruitment_RecruitsDefector()
        {
            var effect = new BriberyEffect("faction1", BriberyTargetType.DefectorRecruitment, 5000);
            Assert.False(effect.ProvidesIntelligence);
            Assert.False(effect.DiversResources);
            Assert.True(effect.RecruitDefector);
        }

        [Fact]
        public void TensionReduction_ReducesTension()
        {
            var effect = new BriberyEffect("faction1", BriberyTargetType.TensionReduction, 5000);
            Assert.True(effect.BaseTensionChange < 0);
        }

        [Theory]
        [InlineData(BriberyTargetType.ResourceDiversion, 2000, 0.05f)]
        [InlineData(BriberyTargetType.ResourceDiversion, 11000, 0.15f)]
        [InlineData(BriberyTargetType.ResourceDiversion, 20000, 0.25f)]
        public void ResourceDiversionRate_ScalesWithBribeAmount(BriberyTargetType targetType, int bribeAmount, float expectedRate)
        {
            var effect = new BriberyEffect("faction1", targetType, bribeAmount);
            Assert.Equal(expectedRate, effect.ResourceDiversionRate, 2);
        }

        #endregion

        #region Detection Risk Tests

        [Theory]
        [InlineData(BriberyTargetType.IntelligenceAsset, 0.1f)]
        [InlineData(BriberyTargetType.ResourceDiversion, 0.25f)]
        [InlineData(BriberyTargetType.DefectorRecruitment, 0.4f)]
        [InlineData(BriberyTargetType.TensionReduction, 0.15f)]
        public void BaseDetectionRisk_ForTargetType_ReturnsExpectedValue(BriberyTargetType targetType, float expectedRisk)
        {
            var effect = new BriberyEffect("faction1", targetType, 5000);
            Assert.Equal(expectedRisk, effect.BaseDetectionRisk, 2);
        }

        [Fact]
        public void DetectionTensionBonus_ReturnsExpectedValue()
        {
            var effect = new BriberyEffect("faction1", BriberyTargetType.IntelligenceAsset, 5000);
            // Detection of bribery should increase tension significantly
            Assert.Equal(15, effect.DetectionTensionBonus);
        }

        #endregion

        #region Timestamp Tests

        [Fact]
        public void Timestamp_IsSetToCurrentTime()
        {
            var before = DateTime.UtcNow;
            var effect = new BriberyEffect("faction1", BriberyTargetType.IntelligenceAsset, 5000);
            var after = DateTime.UtcNow;

            Assert.InRange(effect.Timestamp, before, after);
        }

        #endregion

        #region ToString Tests

        [Fact]
        public void ToString_ContainsTargetType()
        {
            var effect = new BriberyEffect("faction1", BriberyTargetType.IntelligenceAsset, 5000);
            Assert.Contains("IntelligenceAsset", effect.ToString());
        }

        [Fact]
        public void ToString_ContainsFactionId()
        {
            var effect = new BriberyEffect("faction1", BriberyTargetType.IntelligenceAsset, 5000);
            Assert.Contains("faction1", effect.ToString());
        }

        [Fact]
        public void ToString_ContainsBribeAmount()
        {
            var effect = new BriberyEffect("faction1", BriberyTargetType.IntelligenceAsset, 5000);
            Assert.Contains("5000", effect.ToString());
        }

        #endregion
    }
}
