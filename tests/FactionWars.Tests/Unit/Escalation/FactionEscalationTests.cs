using System;
using Xunit;
using FactionWars.Escalation.Models;

namespace FactionWars.Tests.Unit.Escalation
{
    /// <summary>
    /// Tests for the FactionEscalation model which tracks escalation levels for a faction.
    /// Escalation progresses as factions engage in warfare, unlocking better weapons and vehicles.
    /// </summary>
    public class FactionEscalationTests
    {
        private const string FactionMichael = "faction-michael";
        private const string FactionTrevor = "faction-trevor";

        #region Construction Tests

        [Fact]
        public void Constructor_WithValidFactionId_CreatesEscalation()
        {
            var escalation = new FactionEscalation(FactionMichael);

            Assert.Equal(FactionMichael, escalation.FactionId);
        }

        [Fact]
        public void Constructor_DefaultValue_StartsAtZero()
        {
            var escalation = new FactionEscalation(FactionMichael);

            Assert.Equal(0, escalation.Points);
        }

        [Fact]
        public void Constructor_DefaultTier_IsTier1()
        {
            var escalation = new FactionEscalation(FactionMichael);

            Assert.Equal(EscalationTier.Tier1, escalation.CurrentTier);
        }

        [Fact]
        public void Constructor_WithInitialPoints_SetsCorrectly()
        {
            var escalation = new FactionEscalation(FactionMichael, 500);

            Assert.Equal(500, escalation.Points);
        }

        [Fact]
        public void Constructor_WithNullFactionId_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new FactionEscalation(null!));
        }

        [Fact]
        public void Constructor_WithEmptyFactionId_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new FactionEscalation(""));
        }

        [Fact]
        public void Constructor_WithWhitespaceFactionId_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new FactionEscalation("   "));
        }

        [Fact]
        public void Constructor_ClampsNegativePoints_ToMinimum()
        {
            var escalation = new FactionEscalation(FactionMichael, -100);

            Assert.Equal(FactionEscalation.MinPoints, escalation.Points);
        }

        [Fact]
        public void Constructor_ClampsExcessivePoints_ToMaximum()
        {
            var escalation = new FactionEscalation(FactionMichael, 99999);

            Assert.Equal(FactionEscalation.MaxPoints, escalation.Points);
        }

        #endregion

        #region Constants Tests

        [Fact]
        public void MinPoints_IsZero()
        {
            Assert.Equal(0, FactionEscalation.MinPoints);
        }

        [Fact]
        public void MaxPoints_Is10000()
        {
            Assert.Equal(10000, FactionEscalation.MaxPoints);
        }

        [Fact]
        public void Tier2Threshold_Is1000()
        {
            Assert.Equal(1000, FactionEscalation.Tier2Threshold);
        }

        [Fact]
        public void Tier3Threshold_Is3000()
        {
            Assert.Equal(3000, FactionEscalation.Tier3Threshold);
        }

        [Fact]
        public void Tier4Threshold_Is6000()
        {
            Assert.Equal(6000, FactionEscalation.Tier4Threshold);
        }

        [Fact]
        public void Tier5Threshold_Is9000()
        {
            Assert.Equal(9000, FactionEscalation.Tier5Threshold);
        }

        #endregion

        #region CurrentTier Tests

        [Theory]
        [InlineData(0, EscalationTier.Tier1)]
        [InlineData(500, EscalationTier.Tier1)]
        [InlineData(999, EscalationTier.Tier1)]
        public void CurrentTier_Tier1Range_ReturnsTier1(int points, EscalationTier expected)
        {
            var escalation = new FactionEscalation(FactionMichael, points);

            Assert.Equal(expected, escalation.CurrentTier);
        }

        [Theory]
        [InlineData(1000, EscalationTier.Tier2)]
        [InlineData(1500, EscalationTier.Tier2)]
        [InlineData(2999, EscalationTier.Tier2)]
        public void CurrentTier_Tier2Range_ReturnsTier2(int points, EscalationTier expected)
        {
            var escalation = new FactionEscalation(FactionMichael, points);

            Assert.Equal(expected, escalation.CurrentTier);
        }

        [Theory]
        [InlineData(3000, EscalationTier.Tier3)]
        [InlineData(4500, EscalationTier.Tier3)]
        [InlineData(5999, EscalationTier.Tier3)]
        public void CurrentTier_Tier3Range_ReturnsTier3(int points, EscalationTier expected)
        {
            var escalation = new FactionEscalation(FactionMichael, points);

            Assert.Equal(expected, escalation.CurrentTier);
        }

        [Theory]
        [InlineData(6000, EscalationTier.Tier4)]
        [InlineData(7500, EscalationTier.Tier4)]
        [InlineData(8999, EscalationTier.Tier4)]
        public void CurrentTier_Tier4Range_ReturnsTier4(int points, EscalationTier expected)
        {
            var escalation = new FactionEscalation(FactionMichael, points);

            Assert.Equal(expected, escalation.CurrentTier);
        }

        [Theory]
        [InlineData(9000, EscalationTier.Tier5)]
        [InlineData(9500, EscalationTier.Tier5)]
        [InlineData(10000, EscalationTier.Tier5)]
        public void CurrentTier_Tier5Range_ReturnsTier5(int points, EscalationTier expected)
        {
            var escalation = new FactionEscalation(FactionMichael, points);

            Assert.Equal(expected, escalation.CurrentTier);
        }

        #endregion

        #region Points Modification Tests

        [Fact]
        public void AddPoints_IncreasesPoints()
        {
            var escalation = new FactionEscalation(FactionMichael, 500);

            escalation.AddPoints(300);

            Assert.Equal(800, escalation.Points);
        }

        [Fact]
        public void AddPoints_ClampsToMaximum()
        {
            var escalation = new FactionEscalation(FactionMichael, 9500);

            escalation.AddPoints(1000);

            Assert.Equal(FactionEscalation.MaxPoints, escalation.Points);
        }

        [Fact]
        public void AddPoints_WithNegativeAmount_ThrowsArgumentException()
        {
            var escalation = new FactionEscalation(FactionMichael);

            Assert.Throws<ArgumentException>(() => escalation.AddPoints(-100));
        }

        [Fact]
        public void AddPoints_WithZero_DoesNotChange()
        {
            var escalation = new FactionEscalation(FactionMichael, 500);

            escalation.AddPoints(0);

            Assert.Equal(500, escalation.Points);
        }

        [Fact]
        public void AddPoints_ReturnsTrue_WhenTierChanges()
        {
            var escalation = new FactionEscalation(FactionMichael, 900);

            var tierChanged = escalation.AddPoints(200);

            Assert.True(tierChanged);
            Assert.Equal(EscalationTier.Tier2, escalation.CurrentTier);
        }

        [Fact]
        public void AddPoints_ReturnsFalse_WhenTierDoesNotChange()
        {
            var escalation = new FactionEscalation(FactionMichael, 500);

            var tierChanged = escalation.AddPoints(100);

            Assert.False(tierChanged);
            Assert.Equal(EscalationTier.Tier1, escalation.CurrentTier);
        }

        [Fact]
        public void RemovePoints_DecreasesPoints()
        {
            var escalation = new FactionEscalation(FactionMichael, 500);

            escalation.RemovePoints(200);

            Assert.Equal(300, escalation.Points);
        }

        [Fact]
        public void RemovePoints_ClampsToMinimum()
        {
            var escalation = new FactionEscalation(FactionMichael, 100);

            escalation.RemovePoints(200);

            Assert.Equal(FactionEscalation.MinPoints, escalation.Points);
        }

        [Fact]
        public void RemovePoints_WithNegativeAmount_ThrowsArgumentException()
        {
            var escalation = new FactionEscalation(FactionMichael, 500);

            Assert.Throws<ArgumentException>(() => escalation.RemovePoints(-100));
        }

        [Fact]
        public void RemovePoints_WithZero_DoesNotChange()
        {
            var escalation = new FactionEscalation(FactionMichael, 500);

            escalation.RemovePoints(0);

            Assert.Equal(500, escalation.Points);
        }

        [Fact]
        public void RemovePoints_ReturnsTrue_WhenTierChanges()
        {
            var escalation = new FactionEscalation(FactionMichael, 1100);

            var tierChanged = escalation.RemovePoints(200);

            Assert.True(tierChanged);
            Assert.Equal(EscalationTier.Tier1, escalation.CurrentTier);
        }

        [Fact]
        public void RemovePoints_ReturnsFalse_WhenTierDoesNotChange()
        {
            var escalation = new FactionEscalation(FactionMichael, 1500);

            var tierChanged = escalation.RemovePoints(200);

            Assert.False(tierChanged);
            Assert.Equal(EscalationTier.Tier2, escalation.CurrentTier);
        }

        [Fact]
        public void SetPoints_SetsCorrectValue()
        {
            var escalation = new FactionEscalation(FactionMichael);

            escalation.SetPoints(5000);

            Assert.Equal(5000, escalation.Points);
        }

        [Fact]
        public void SetPoints_ClampsToMinimum()
        {
            var escalation = new FactionEscalation(FactionMichael, 500);

            escalation.SetPoints(-100);

            Assert.Equal(FactionEscalation.MinPoints, escalation.Points);
        }

        [Fact]
        public void SetPoints_ClampsToMaximum()
        {
            var escalation = new FactionEscalation(FactionMichael);

            escalation.SetPoints(99999);

            Assert.Equal(FactionEscalation.MaxPoints, escalation.Points);
        }

        #endregion

        #region Progress Calculation Tests

        [Fact]
        public void ProgressToNextTier_AtTier1Start_ReturnsZeroPercent()
        {
            var escalation = new FactionEscalation(FactionMichael, 0);

            Assert.Equal(0f, escalation.ProgressToNextTier);
        }

        [Fact]
        public void ProgressToNextTier_HalfwayToTier2_Returns50Percent()
        {
            var escalation = new FactionEscalation(FactionMichael, 500);

            Assert.Equal(50f, escalation.ProgressToNextTier, 0.1f);
        }

        [Fact]
        public void ProgressToNextTier_AtTier2Boundary_ReturnsZeroPercent()
        {
            var escalation = new FactionEscalation(FactionMichael, 1000);

            Assert.Equal(0f, escalation.ProgressToNextTier);
        }

        [Fact]
        public void ProgressToNextTier_HalfwayToTier3_Returns50Percent()
        {
            // Tier2 is 1000-2999, so halfway between 1000 and 3000 is 2000
            var escalation = new FactionEscalation(FactionMichael, 2000);

            Assert.Equal(50f, escalation.ProgressToNextTier, 0.1f);
        }

        [Fact]
        public void ProgressToNextTier_AtMaxTier_Returns100Percent()
        {
            var escalation = new FactionEscalation(FactionMichael, 10000);

            Assert.Equal(100f, escalation.ProgressToNextTier);
        }

        [Fact]
        public void PointsToNextTier_AtZero_Returns1000()
        {
            var escalation = new FactionEscalation(FactionMichael, 0);

            Assert.Equal(1000, escalation.PointsToNextTier);
        }

        [Fact]
        public void PointsToNextTier_At500_Returns500()
        {
            var escalation = new FactionEscalation(FactionMichael, 500);

            Assert.Equal(500, escalation.PointsToNextTier);
        }

        [Fact]
        public void PointsToNextTier_At1000_Returns2000()
        {
            // At Tier2 (1000), next tier is Tier3 at 3000, so 2000 points needed
            var escalation = new FactionEscalation(FactionMichael, 1000);

            Assert.Equal(2000, escalation.PointsToNextTier);
        }

        [Fact]
        public void PointsToNextTier_AtMaxTier_ReturnsZero()
        {
            var escalation = new FactionEscalation(FactionMichael, 9000);

            Assert.Equal(0, escalation.PointsToNextTier);
        }

        #endregion

        #region Tier Transition Events Tests

        [Fact]
        public void PreviousTier_TracksLastTierBeforeChange()
        {
            var escalation = new FactionEscalation(FactionMichael, 900);

            Assert.Equal(EscalationTier.Tier1, escalation.PreviousTier);

            escalation.AddPoints(200); // Now at 1100, Tier2

            Assert.Equal(EscalationTier.Tier1, escalation.PreviousTier);
        }

        [Fact]
        public void GetThresholdForTier_ReturnCorrectValues()
        {
            Assert.Equal(0, FactionEscalation.GetThresholdForTier(EscalationTier.Tier1));
            Assert.Equal(1000, FactionEscalation.GetThresholdForTier(EscalationTier.Tier2));
            Assert.Equal(3000, FactionEscalation.GetThresholdForTier(EscalationTier.Tier3));
            Assert.Equal(6000, FactionEscalation.GetThresholdForTier(EscalationTier.Tier4));
            Assert.Equal(9000, FactionEscalation.GetThresholdForTier(EscalationTier.Tier5));
        }

        #endregion

        #region LastUpdateTime Tests

        [Fact]
        public void LastUpdateTime_DefaultsToConstructionTime()
        {
            var before = DateTime.UtcNow;
            var escalation = new FactionEscalation(FactionMichael);
            var after = DateTime.UtcNow;

            Assert.InRange(escalation.LastUpdateTime, before, after);
        }

        [Fact]
        public void AddPoints_UpdatesLastUpdateTime()
        {
            var escalation = new FactionEscalation(FactionMichael);
            var initialTime = escalation.LastUpdateTime;

            System.Threading.Thread.Sleep(10);
            escalation.AddPoints(100);

            Assert.True(escalation.LastUpdateTime > initialTime);
        }

        [Fact]
        public void RemovePoints_UpdatesLastUpdateTime()
        {
            var escalation = new FactionEscalation(FactionMichael, 500);
            var initialTime = escalation.LastUpdateTime;

            System.Threading.Thread.Sleep(10);
            escalation.RemovePoints(100);

            Assert.True(escalation.LastUpdateTime > initialTime);
        }

        [Fact]
        public void SetPoints_UpdatesLastUpdateTime()
        {
            var escalation = new FactionEscalation(FactionMichael);
            var initialTime = escalation.LastUpdateTime;

            System.Threading.Thread.Sleep(10);
            escalation.SetPoints(5000);

            Assert.True(escalation.LastUpdateTime > initialTime);
        }

        #endregion

        #region Equality Tests

        [Fact]
        public void Equals_SameFactionId_ReturnsTrue()
        {
            var e1 = new FactionEscalation(FactionMichael, 100);
            var e2 = new FactionEscalation(FactionMichael, 500);

            Assert.True(e1.Equals(e2));
        }

        [Fact]
        public void Equals_DifferentFactionId_ReturnsFalse()
        {
            var e1 = new FactionEscalation(FactionMichael);
            var e2 = new FactionEscalation(FactionTrevor);

            Assert.False(e1.Equals(e2));
        }

        [Fact]
        public void Equals_WithNull_ReturnsFalse()
        {
            var e1 = new FactionEscalation(FactionMichael);

            Assert.False(e1.Equals(null));
        }

        [Fact]
        public void GetHashCode_SameFactionId_ReturnsSameHash()
        {
            var e1 = new FactionEscalation(FactionMichael, 100);
            var e2 = new FactionEscalation(FactionMichael, 500);

            Assert.Equal(e1.GetHashCode(), e2.GetHashCode());
        }

        [Fact]
        public void EqualityOperator_SameFactionId_ReturnsTrue()
        {
            var e1 = new FactionEscalation(FactionMichael);
            var e2 = new FactionEscalation(FactionMichael);

            Assert.True(e1 == e2);
        }

        [Fact]
        public void InequalityOperator_DifferentFactionId_ReturnsTrue()
        {
            var e1 = new FactionEscalation(FactionMichael);
            var e2 = new FactionEscalation(FactionTrevor);

            Assert.True(e1 != e2);
        }

        #endregion

        #region ToString Tests

        [Fact]
        public void ToString_ContainsFactionId()
        {
            var escalation = new FactionEscalation(FactionMichael, 1500);

            var result = escalation.ToString();

            Assert.Contains(FactionMichael, result);
        }

        [Fact]
        public void ToString_ContainsPoints()
        {
            var escalation = new FactionEscalation(FactionMichael, 1500);

            var result = escalation.ToString();

            Assert.Contains("1500", result);
        }

        [Fact]
        public void ToString_ContainsTier()
        {
            var escalation = new FactionEscalation(FactionMichael, 1500);

            var result = escalation.ToString();

            Assert.Contains("Tier2", result);
        }

        #endregion
    }
}
