using System;
using Xunit;
using FactionWars.Tension.Models;

namespace FactionWars.Tests.Unit.Tension
{
    /// <summary>
    /// Tests for the FactionTension model which tracks tension levels between two factions.
    /// Tension is a separate measure from relationship - it represents the build-up towards conflict
    /// and can escalate or de-escalate based on actions and time.
    /// </summary>
    public class FactionTensionTests
    {
        private const string FactionMichael = "faction-michael";
        private const string FactionTrevor = "faction-trevor";
        private const string FactionFranklin = "faction-franklin";

        #region Construction Tests

        [Fact]
        public void Constructor_WithValidFactionIds_CreatesTension()
        {
            var tension = new FactionTension(FactionMichael, FactionTrevor);

            Assert.Equal(FactionMichael, tension.FactionId1);
            Assert.Equal(FactionTrevor, tension.FactionId2);
        }

        [Fact]
        public void Constructor_DefaultValue_StartsAtZero()
        {
            var tension = new FactionTension(FactionMichael, FactionTrevor);

            Assert.Equal(0, tension.Value);
        }

        [Fact]
        public void Constructor_DefaultLevel_IsCalm()
        {
            var tension = new FactionTension(FactionMichael, FactionTrevor);

            Assert.Equal(TensionLevel.Calm, tension.Level);
        }

        [Fact]
        public void Constructor_WithInitialValue_SetsCorrectly()
        {
            var tension = new FactionTension(FactionMichael, FactionTrevor, 50);

            Assert.Equal(50, tension.Value);
        }

        [Fact]
        public void Constructor_WithNullFirstFaction_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new FactionTension(null!, FactionTrevor));
        }

        [Fact]
        public void Constructor_WithNullSecondFaction_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new FactionTension(FactionMichael, null!));
        }

        [Fact]
        public void Constructor_WithEmptyFirstFaction_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new FactionTension("", FactionTrevor));
        }

        [Fact]
        public void Constructor_WithEmptySecondFaction_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new FactionTension(FactionMichael, ""));
        }

        [Fact]
        public void Constructor_WithWhitespaceFirstFaction_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new FactionTension("   ", FactionTrevor));
        }

        [Fact]
        public void Constructor_WithWhitespaceSecondFaction_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new FactionTension(FactionMichael, "   "));
        }

        [Fact]
        public void Constructor_WithSameFactionIds_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new FactionTension(FactionMichael, FactionMichael));
        }

        [Fact]
        public void Constructor_ClampsNegativeValue_ToMinimum()
        {
            var tension = new FactionTension(FactionMichael, FactionTrevor, -10);

            Assert.Equal(FactionTension.MinValue, tension.Value);
        }

        [Fact]
        public void Constructor_ClampsExcessiveValue_ToMaximum()
        {
            var tension = new FactionTension(FactionMichael, FactionTrevor, 150);

            Assert.Equal(FactionTension.MaxValue, tension.Value);
        }

        #endregion

        #region Constants Tests

        [Fact]
        public void MinValue_IsZero()
        {
            Assert.Equal(0, FactionTension.MinValue);
        }

        [Fact]
        public void MaxValue_Is100()
        {
            Assert.Equal(100, FactionTension.MaxValue);
        }

        #endregion

        #region TensionLevel Tests

        [Theory]
        [InlineData(0, TensionLevel.Calm)]
        [InlineData(10, TensionLevel.Calm)]
        [InlineData(24, TensionLevel.Calm)]
        public void Level_CalmRange_ReturnsCalm(int value, TensionLevel expected)
        {
            var tension = new FactionTension(FactionMichael, FactionTrevor, value);

            Assert.Equal(expected, tension.Level);
        }

        [Theory]
        [InlineData(25, TensionLevel.Uneasy)]
        [InlineData(35, TensionLevel.Uneasy)]
        [InlineData(49, TensionLevel.Uneasy)]
        public void Level_UneasyRange_ReturnsUneasy(int value, TensionLevel expected)
        {
            var tension = new FactionTension(FactionMichael, FactionTrevor, value);

            Assert.Equal(expected, tension.Level);
        }

        [Theory]
        [InlineData(50, TensionLevel.Tense)]
        [InlineData(60, TensionLevel.Tense)]
        [InlineData(74, TensionLevel.Tense)]
        public void Level_TenseRange_ReturnsTense(int value, TensionLevel expected)
        {
            var tension = new FactionTension(FactionMichael, FactionTrevor, value);

            Assert.Equal(expected, tension.Level);
        }

        [Theory]
        [InlineData(75, TensionLevel.Volatile)]
        [InlineData(85, TensionLevel.Volatile)]
        [InlineData(89, TensionLevel.Volatile)]
        public void Level_VolatileRange_ReturnsVolatile(int value, TensionLevel expected)
        {
            var tension = new FactionTension(FactionMichael, FactionTrevor, value);

            Assert.Equal(expected, tension.Level);
        }

        [Theory]
        [InlineData(90, TensionLevel.Critical)]
        [InlineData(95, TensionLevel.Critical)]
        [InlineData(100, TensionLevel.Critical)]
        public void Level_CriticalRange_ReturnsCritical(int value, TensionLevel expected)
        {
            var tension = new FactionTension(FactionMichael, FactionTrevor, value);

            Assert.Equal(expected, tension.Level);
        }

        #endregion

        #region Value Modification Tests

        [Fact]
        public void Increase_AddsToValue()
        {
            var tension = new FactionTension(FactionMichael, FactionTrevor, 20);

            tension.Increase(15);

            Assert.Equal(35, tension.Value);
        }

        [Fact]
        public void Increase_ClampsToMaximum()
        {
            var tension = new FactionTension(FactionMichael, FactionTrevor, 90);

            tension.Increase(20);

            Assert.Equal(FactionTension.MaxValue, tension.Value);
        }

        [Fact]
        public void Increase_WithNegativeAmount_ThrowsArgumentException()
        {
            var tension = new FactionTension(FactionMichael, FactionTrevor);

            Assert.Throws<ArgumentException>(() => tension.Increase(-5));
        }

        [Fact]
        public void Increase_WithZero_DoesNotChange()
        {
            var tension = new FactionTension(FactionMichael, FactionTrevor, 50);

            tension.Increase(0);

            Assert.Equal(50, tension.Value);
        }

        [Fact]
        public void Decrease_SubtractsFromValue()
        {
            var tension = new FactionTension(FactionMichael, FactionTrevor, 50);

            tension.Decrease(20);

            Assert.Equal(30, tension.Value);
        }

        [Fact]
        public void Decrease_ClampsToMinimum()
        {
            var tension = new FactionTension(FactionMichael, FactionTrevor, 10);

            tension.Decrease(20);

            Assert.Equal(FactionTension.MinValue, tension.Value);
        }

        [Fact]
        public void Decrease_WithNegativeAmount_ThrowsArgumentException()
        {
            var tension = new FactionTension(FactionMichael, FactionTrevor, 50);

            Assert.Throws<ArgumentException>(() => tension.Decrease(-5));
        }

        [Fact]
        public void Decrease_WithZero_DoesNotChange()
        {
            var tension = new FactionTension(FactionMichael, FactionTrevor, 50);

            tension.Decrease(0);

            Assert.Equal(50, tension.Value);
        }

        [Fact]
        public void SetValue_SetsCorrectValue()
        {
            var tension = new FactionTension(FactionMichael, FactionTrevor);

            tension.SetValue(75);

            Assert.Equal(75, tension.Value);
        }

        [Fact]
        public void SetValue_ClampsToMinimum()
        {
            var tension = new FactionTension(FactionMichael, FactionTrevor, 50);

            tension.SetValue(-10);

            Assert.Equal(FactionTension.MinValue, tension.Value);
        }

        [Fact]
        public void SetValue_ClampsToMaximum()
        {
            var tension = new FactionTension(FactionMichael, FactionTrevor);

            tension.SetValue(150);

            Assert.Equal(FactionTension.MaxValue, tension.Value);
        }

        #endregion

        #region Faction Query Tests

        [Fact]
        public void ContainsFaction_WithFirstFaction_ReturnsTrue()
        {
            var tension = new FactionTension(FactionMichael, FactionTrevor);

            Assert.True(tension.ContainsFaction(FactionMichael));
        }

        [Fact]
        public void ContainsFaction_WithSecondFaction_ReturnsTrue()
        {
            var tension = new FactionTension(FactionMichael, FactionTrevor);

            Assert.True(tension.ContainsFaction(FactionTrevor));
        }

        [Fact]
        public void ContainsFaction_WithOtherFaction_ReturnsFalse()
        {
            var tension = new FactionTension(FactionMichael, FactionTrevor);

            Assert.False(tension.ContainsFaction(FactionFranklin));
        }

        [Fact]
        public void ContainsFaction_WithNull_ReturnsFalse()
        {
            var tension = new FactionTension(FactionMichael, FactionTrevor);

            Assert.False(tension.ContainsFaction(null!));
        }

        [Fact]
        public void ContainsFaction_WithEmptyString_ReturnsFalse()
        {
            var tension = new FactionTension(FactionMichael, FactionTrevor);

            Assert.False(tension.ContainsFaction(""));
        }

        [Fact]
        public void InvolvesBothFactions_WithBothInOrder_ReturnsTrue()
        {
            var tension = new FactionTension(FactionMichael, FactionTrevor);

            Assert.True(tension.InvolvesBothFactions(FactionMichael, FactionTrevor));
        }

        [Fact]
        public void InvolvesBothFactions_WithBothReversed_ReturnsTrue()
        {
            var tension = new FactionTension(FactionMichael, FactionTrevor);

            Assert.True(tension.InvolvesBothFactions(FactionTrevor, FactionMichael));
        }

        [Fact]
        public void InvolvesBothFactions_WithOnlyFirst_ReturnsFalse()
        {
            var tension = new FactionTension(FactionMichael, FactionTrevor);

            Assert.False(tension.InvolvesBothFactions(FactionMichael, FactionFranklin));
        }

        [Fact]
        public void InvolvesBothFactions_WithNeither_ReturnsFalse()
        {
            var tension = new FactionTension(FactionMichael, FactionTrevor);

            Assert.False(tension.InvolvesBothFactions("faction-a", "faction-b"));
        }

        [Fact]
        public void GetOtherFaction_WithFirstFaction_ReturnsSecond()
        {
            var tension = new FactionTension(FactionMichael, FactionTrevor);

            Assert.Equal(FactionTrevor, tension.GetOtherFaction(FactionMichael));
        }

        [Fact]
        public void GetOtherFaction_WithSecondFaction_ReturnsFirst()
        {
            var tension = new FactionTension(FactionMichael, FactionTrevor);

            Assert.Equal(FactionMichael, tension.GetOtherFaction(FactionTrevor));
        }

        [Fact]
        public void GetOtherFaction_WithOtherFaction_ReturnsNull()
        {
            var tension = new FactionTension(FactionMichael, FactionTrevor);

            Assert.Null(tension.GetOtherFaction(FactionFranklin));
        }

        #endregion

        #region DecayRate Tests

        [Fact]
        public void DecayRate_DefaultValue_IsOne()
        {
            var tension = new FactionTension(FactionMichael, FactionTrevor);

            Assert.Equal(1.0f, tension.DecayRate);
        }

        [Fact]
        public void SetDecayRate_SetsCorrectValue()
        {
            var tension = new FactionTension(FactionMichael, FactionTrevor);

            tension.SetDecayRate(2.5f);

            Assert.Equal(2.5f, tension.DecayRate);
        }

        [Fact]
        public void SetDecayRate_WithNegative_ThrowsArgumentException()
        {
            var tension = new FactionTension(FactionMichael, FactionTrevor);

            Assert.Throws<ArgumentException>(() => tension.SetDecayRate(-1f));
        }

        [Fact]
        public void SetDecayRate_WithZero_AllowsNoDecay()
        {
            var tension = new FactionTension(FactionMichael, FactionTrevor);

            tension.SetDecayRate(0f);

            Assert.Equal(0f, tension.DecayRate);
        }

        #endregion

        #region ApplyDecay Tests

        [Fact]
        public void ApplyDecay_ReducesValueByDecayRate()
        {
            var tension = new FactionTension(FactionMichael, FactionTrevor, 50);
            tension.SetDecayRate(5f);

            tension.ApplyDecay();

            Assert.Equal(45, tension.Value);
        }

        [Fact]
        public void ApplyDecay_ClampsToMinimum()
        {
            var tension = new FactionTension(FactionMichael, FactionTrevor, 3);
            tension.SetDecayRate(5f);

            tension.ApplyDecay();

            Assert.Equal(FactionTension.MinValue, tension.Value);
        }

        [Fact]
        public void ApplyDecay_WithZeroDecayRate_DoesNotChange()
        {
            var tension = new FactionTension(FactionMichael, FactionTrevor, 50);
            tension.SetDecayRate(0f);

            tension.ApplyDecay();

            Assert.Equal(50, tension.Value);
        }

        [Fact]
        public void ApplyDecay_TruncatesDecayRateToInt()
        {
            var tension = new FactionTension(FactionMichael, FactionTrevor, 50);
            tension.SetDecayRate(1.7f);

            tension.ApplyDecay();

            // (int)1.7f truncates to 1, so 50 - 1 = 49
            Assert.Equal(49, tension.Value);
        }

        #endregion

        #region LastUpdateTime Tests

        [Fact]
        public void LastUpdateTime_DefaultsToConstructionTime()
        {
            var before = DateTime.UtcNow;
            var tension = new FactionTension(FactionMichael, FactionTrevor);
            var after = DateTime.UtcNow;

            Assert.InRange(tension.LastUpdateTime, before, after);
        }

        [Fact]
        public void Increase_UpdatesLastUpdateTime()
        {
            var tension = new FactionTension(FactionMichael, FactionTrevor);
            var initialTime = tension.LastUpdateTime;

            System.Threading.Thread.Sleep(10); // Ensure time passes
            tension.Increase(10);

            Assert.True(tension.LastUpdateTime > initialTime);
        }

        [Fact]
        public void Decrease_UpdatesLastUpdateTime()
        {
            var tension = new FactionTension(FactionMichael, FactionTrevor, 50);
            var initialTime = tension.LastUpdateTime;

            System.Threading.Thread.Sleep(10);
            tension.Decrease(10);

            Assert.True(tension.LastUpdateTime > initialTime);
        }

        [Fact]
        public void SetValue_UpdatesLastUpdateTime()
        {
            var tension = new FactionTension(FactionMichael, FactionTrevor);
            var initialTime = tension.LastUpdateTime;

            System.Threading.Thread.Sleep(10);
            tension.SetValue(75);

            Assert.True(tension.LastUpdateTime > initialTime);
        }

        [Fact]
        public void ApplyDecay_UpdatesLastUpdateTime()
        {
            var tension = new FactionTension(FactionMichael, FactionTrevor, 50);
            var initialTime = tension.LastUpdateTime;

            System.Threading.Thread.Sleep(10);
            tension.ApplyDecay();

            Assert.True(tension.LastUpdateTime > initialTime);
        }

        #endregion

        #region Equality Tests

        [Fact]
        public void Equals_SameFactions_ReturnsTrue()
        {
            var t1 = new FactionTension(FactionMichael, FactionTrevor);
            var t2 = new FactionTension(FactionMichael, FactionTrevor);

            Assert.True(t1.Equals(t2));
        }

        [Fact]
        public void Equals_SameFactionsReversed_ReturnsTrue()
        {
            var t1 = new FactionTension(FactionMichael, FactionTrevor);
            var t2 = new FactionTension(FactionTrevor, FactionMichael);

            Assert.True(t1.Equals(t2));
        }

        [Fact]
        public void Equals_DifferentFactions_ReturnsFalse()
        {
            var t1 = new FactionTension(FactionMichael, FactionTrevor);
            var t2 = new FactionTension(FactionMichael, FactionFranklin);

            Assert.False(t1.Equals(t2));
        }

        [Fact]
        public void Equals_WithNull_ReturnsFalse()
        {
            var t1 = new FactionTension(FactionMichael, FactionTrevor);

            Assert.False(t1.Equals(null));
        }

        [Fact]
        public void GetHashCode_SameFactions_ReturnsSameHash()
        {
            var t1 = new FactionTension(FactionMichael, FactionTrevor);
            var t2 = new FactionTension(FactionMichael, FactionTrevor);

            Assert.Equal(t1.GetHashCode(), t2.GetHashCode());
        }

        [Fact]
        public void GetHashCode_SameFactionsReversed_ReturnsSameHash()
        {
            var t1 = new FactionTension(FactionMichael, FactionTrevor);
            var t2 = new FactionTension(FactionTrevor, FactionMichael);

            Assert.Equal(t1.GetHashCode(), t2.GetHashCode());
        }

        [Fact]
        public void EqualityOperator_SameFactions_ReturnsTrue()
        {
            var t1 = new FactionTension(FactionMichael, FactionTrevor);
            var t2 = new FactionTension(FactionMichael, FactionTrevor);

            Assert.True(t1 == t2);
        }

        [Fact]
        public void InequalityOperator_DifferentFactions_ReturnsTrue()
        {
            var t1 = new FactionTension(FactionMichael, FactionTrevor);
            var t2 = new FactionTension(FactionMichael, FactionFranklin);

            Assert.True(t1 != t2);
        }

        #endregion

        #region ToString Tests

        [Fact]
        public void ToString_ContainsFactionIds()
        {
            var tension = new FactionTension(FactionMichael, FactionTrevor, 50);

            var result = tension.ToString();

            Assert.Contains(FactionMichael, result);
            Assert.Contains(FactionTrevor, result);
        }

        [Fact]
        public void ToString_ContainsValue()
        {
            var tension = new FactionTension(FactionMichael, FactionTrevor, 75);

            var result = tension.ToString();

            Assert.Contains("75", result);
        }

        [Fact]
        public void ToString_ContainsLevel()
        {
            var tension = new FactionTension(FactionMichael, FactionTrevor, 75);

            var result = tension.ToString();

            Assert.Contains("Volatile", result);
        }

        #endregion
    }
}
