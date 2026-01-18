using System;
using FactionWars.Tension.Models;
using Xunit;

namespace FactionWars.Tests.Unit.Tension
{
    /// <summary>
    /// Tests for the AssassinationEffect class which represents the result of an assassination operation.
    /// </summary>
    public class AssassinationEffectTests
    {
        #region Constructor Tests

        [Fact]
        public void Constructor_WithValidParameters_CreatesEffect()
        {
            var effect = new AssassinationEffect("faction1", AssassinationTargetType.Lieutenant, "lt_001");

            Assert.Equal("faction1", effect.TargetFactionId);
            Assert.Equal(AssassinationTargetType.Lieutenant, effect.TargetType);
            Assert.Equal("lt_001", effect.TargetId);
        }

        [Fact]
        public void Constructor_WithNullFactionId_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new AssassinationEffect(null!, AssassinationTargetType.Lieutenant, "lt_001"));
        }

        [Fact]
        public void Constructor_WithEmptyFactionId_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                new AssassinationEffect("", AssassinationTargetType.Lieutenant, "lt_001"));
        }

        [Fact]
        public void Constructor_WithWhitespaceFactionId_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                new AssassinationEffect("   ", AssassinationTargetType.Lieutenant, "lt_001"));
        }

        [Fact]
        public void Constructor_WithNullTargetId_AllowsNull()
        {
            // Target ID is optional for generic targets (like "high value member")
            var effect = new AssassinationEffect("faction1", AssassinationTargetType.HighValueMember, null);
            Assert.Null(effect.TargetId);
        }

        #endregion

        #region Tension Impact Tests

        [Theory]
        [InlineData(AssassinationTargetType.Lieutenant, 25)]
        [InlineData(AssassinationTargetType.HighValueMember, 15)]
        [InlineData(AssassinationTargetType.Enforcer, 10)]
        public void BaseTensionIncrease_ForTargetType_ReturnsExpectedValue(AssassinationTargetType targetType, int expectedTension)
        {
            var effect = new AssassinationEffect("faction1", targetType, "target1");
            Assert.Equal(expectedTension, effect.BaseTensionIncrease);
        }

        [Fact]
        public void DetectionTensionBonus_ReturnsExpectedValue()
        {
            var effect = new AssassinationEffect("faction1", AssassinationTargetType.Lieutenant, "lt_001");
            // Detection should add significant tension bonus
            Assert.Equal(20, effect.DetectionTensionBonus);
        }

        #endregion

        #region Combat Effectiveness Impact Tests

        [Theory]
        [InlineData(AssassinationTargetType.Lieutenant, 0.15f)]
        [InlineData(AssassinationTargetType.HighValueMember, 0.05f)]
        [InlineData(AssassinationTargetType.Enforcer, 0.02f)]
        public void CombatEffectivenessReduction_ForTargetType_ReturnsExpectedValue(
            AssassinationTargetType targetType, float expectedReduction)
        {
            var effect = new AssassinationEffect("faction1", targetType, "target1");
            Assert.Equal(expectedReduction, effect.CombatEffectivenessReduction, 2);
        }

        [Theory]
        [InlineData(AssassinationTargetType.Lieutenant, 600)]
        [InlineData(AssassinationTargetType.HighValueMember, 300)]
        [InlineData(AssassinationTargetType.Enforcer, 180)]
        public void EffectDurationSeconds_ForTargetType_ReturnsExpectedValue(
            AssassinationTargetType targetType, int expectedDuration)
        {
            var effect = new AssassinationEffect("faction1", targetType, "target1");
            Assert.Equal(expectedDuration, effect.EffectDurationSeconds);
        }

        #endregion

        #region Morale Impact Tests

        [Theory]
        [InlineData(AssassinationTargetType.Lieutenant, 20)]
        [InlineData(AssassinationTargetType.HighValueMember, 10)]
        [InlineData(AssassinationTargetType.Enforcer, 5)]
        public void MoraleImpact_ForTargetType_ReturnsExpectedValue(
            AssassinationTargetType targetType, int expectedImpact)
        {
            var effect = new AssassinationEffect("faction1", targetType, "target1");
            Assert.Equal(expectedImpact, effect.MoraleImpact);
        }

        #endregion

        #region Timestamp Tests

        [Fact]
        public void Timestamp_IsSetToCurrentTime()
        {
            var before = DateTime.UtcNow;
            var effect = new AssassinationEffect("faction1", AssassinationTargetType.Lieutenant, "lt_001");
            var after = DateTime.UtcNow;

            Assert.InRange(effect.Timestamp, before, after);
        }

        #endregion

        #region ToString Tests

        [Fact]
        public void ToString_ContainsTargetType()
        {
            var effect = new AssassinationEffect("faction1", AssassinationTargetType.Lieutenant, "lt_001");
            Assert.Contains("Lieutenant", effect.ToString());
        }

        [Fact]
        public void ToString_ContainsFactionId()
        {
            var effect = new AssassinationEffect("faction1", AssassinationTargetType.Lieutenant, "lt_001");
            Assert.Contains("faction1", effect.ToString());
        }

        [Fact]
        public void ToString_ContainsTargetId_WhenProvided()
        {
            var effect = new AssassinationEffect("faction1", AssassinationTargetType.Lieutenant, "lt_001");
            Assert.Contains("lt_001", effect.ToString());
        }

        #endregion
    }
}
