using FactionWars.Factions.Models;
using System;
using Xunit;

namespace FactionWars.Tests.Unit.Factions
{
    public class FactionRelationshipTests
    {
        private const string FactionA = "faction-a";
        private const string FactionB = "faction-b";

        #region Construction Tests

        [Fact]
        public void Constructor_WithValidFactionIds_CreatesRelationship()
        {
            var relationship = new FactionRelationship(FactionA, FactionB);

            Assert.Equal(FactionA, relationship.FactionId1);
            Assert.Equal(FactionB, relationship.FactionId2);
        }

        [Fact]
        public void Constructor_WithDefaultValue_StartsAtNeutral()
        {
            var relationship = new FactionRelationship(FactionA, FactionB);

            Assert.Equal(0, relationship.Value);
            Assert.Equal(RelationshipStatus.Neutral, relationship.Status);
        }

        [Fact]
        public void Constructor_WithCustomValue_SetsValueCorrectly()
        {
            var relationship = new FactionRelationship(FactionA, FactionB, 50);

            Assert.Equal(50, relationship.Value);
        }

        [Fact]
        public void Constructor_WithNullFirstFactionId_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new FactionRelationship(null!, FactionB));
        }

        [Fact]
        public void Constructor_WithNullSecondFactionId_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new FactionRelationship(FactionA, null!));
        }

        [Fact]
        public void Constructor_WithEmptyFirstFactionId_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new FactionRelationship("", FactionB));
        }

        [Fact]
        public void Constructor_WithEmptySecondFactionId_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new FactionRelationship(FactionA, ""));
        }

        [Fact]
        public void Constructor_WithWhitespaceFirstFactionId_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new FactionRelationship("  ", FactionB));
        }

        [Fact]
        public void Constructor_WithWhitespaceSecondFactionId_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new FactionRelationship(FactionA, "  "));
        }

        [Fact]
        public void Constructor_WithSameFactionIds_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new FactionRelationship(FactionA, FactionA));
        }

        [Fact]
        public void Constructor_ClampsValueToMinimum()
        {
            var relationship = new FactionRelationship(FactionA, FactionB, -150);

            Assert.Equal(FactionRelationship.MinValue, relationship.Value);
        }

        [Fact]
        public void Constructor_ClampsValueToMaximum()
        {
            var relationship = new FactionRelationship(FactionA, FactionB, 150);

            Assert.Equal(FactionRelationship.MaxValue, relationship.Value);
        }

        #endregion

        #region Value Modification Tests

        [Fact]
        public void SetValue_WithinRange_SetsCorrectly()
        {
            var relationship = new FactionRelationship(FactionA, FactionB);

            relationship.SetValue(75);

            Assert.Equal(75, relationship.Value);
        }

        [Fact]
        public void SetValue_AboveMax_ClampsToMax()
        {
            var relationship = new FactionRelationship(FactionA, FactionB);

            relationship.SetValue(200);

            Assert.Equal(FactionRelationship.MaxValue, relationship.Value);
        }

        [Fact]
        public void SetValue_BelowMin_ClampsToMin()
        {
            var relationship = new FactionRelationship(FactionA, FactionB);

            relationship.SetValue(-200);

            Assert.Equal(FactionRelationship.MinValue, relationship.Value);
        }

        [Fact]
        public void AdjustValue_Positive_IncreasesValue()
        {
            var relationship = new FactionRelationship(FactionA, FactionB, 0);

            relationship.AdjustValue(25);

            Assert.Equal(25, relationship.Value);
        }

        [Fact]
        public void AdjustValue_Negative_DecreasesValue()
        {
            var relationship = new FactionRelationship(FactionA, FactionB, 50);

            relationship.AdjustValue(-30);

            Assert.Equal(20, relationship.Value);
        }

        [Fact]
        public void AdjustValue_ClampsToMax()
        {
            var relationship = new FactionRelationship(FactionA, FactionB, 80);

            relationship.AdjustValue(50);

            Assert.Equal(FactionRelationship.MaxValue, relationship.Value);
        }

        [Fact]
        public void AdjustValue_ClampsToMin()
        {
            var relationship = new FactionRelationship(FactionA, FactionB, -80);

            relationship.AdjustValue(-50);

            Assert.Equal(FactionRelationship.MinValue, relationship.Value);
        }

        #endregion

        #region Status Tests

        [Theory]
        [InlineData(-100, RelationshipStatus.War)]
        [InlineData(-75, RelationshipStatus.War)]
        [InlineData(-51, RelationshipStatus.War)]
        public void Status_WarRange_ReturnsWar(int value, RelationshipStatus expected)
        {
            var relationship = new FactionRelationship(FactionA, FactionB, value);

            Assert.Equal(expected, relationship.Status);
        }

        [Theory]
        [InlineData(-50, RelationshipStatus.Hostile)]
        [InlineData(-26, RelationshipStatus.Hostile)]
        public void Status_HostileRange_ReturnsHostile(int value, RelationshipStatus expected)
        {
            var relationship = new FactionRelationship(FactionA, FactionB, value);

            Assert.Equal(expected, relationship.Status);
        }

        [Theory]
        [InlineData(-25, RelationshipStatus.Neutral)]
        [InlineData(0, RelationshipStatus.Neutral)]
        [InlineData(25, RelationshipStatus.Neutral)]
        public void Status_NeutralRange_ReturnsNeutral(int value, RelationshipStatus expected)
        {
            var relationship = new FactionRelationship(FactionA, FactionB, value);

            Assert.Equal(expected, relationship.Status);
        }

        [Theory]
        [InlineData(26, RelationshipStatus.Friendly)]
        [InlineData(50, RelationshipStatus.Friendly)]
        public void Status_FriendlyRange_ReturnsFriendly(int value, RelationshipStatus expected)
        {
            var relationship = new FactionRelationship(FactionA, FactionB, value);

            Assert.Equal(expected, relationship.Status);
        }

        [Theory]
        [InlineData(51, RelationshipStatus.Allied)]
        [InlineData(75, RelationshipStatus.Allied)]
        [InlineData(100, RelationshipStatus.Allied)]
        public void Status_AlliedRange_ReturnsAllied(int value, RelationshipStatus expected)
        {
            var relationship = new FactionRelationship(FactionA, FactionB, value);

            Assert.Equal(expected, relationship.Status);
        }

        #endregion

        #region Contains Faction Tests

        [Fact]
        public void ContainsFaction_WithFirstFaction_ReturnsTrue()
        {
            var relationship = new FactionRelationship(FactionA, FactionB);

            Assert.True(relationship.ContainsFaction(FactionA));
        }

        [Fact]
        public void ContainsFaction_WithSecondFaction_ReturnsTrue()
        {
            var relationship = new FactionRelationship(FactionA, FactionB);

            Assert.True(relationship.ContainsFaction(FactionB));
        }

        [Fact]
        public void ContainsFaction_WithOtherFaction_ReturnsFalse()
        {
            var relationship = new FactionRelationship(FactionA, FactionB);

            Assert.False(relationship.ContainsFaction("faction-c"));
        }

        [Fact]
        public void ContainsFaction_WithNull_ReturnsFalse()
        {
            var relationship = new FactionRelationship(FactionA, FactionB);

            Assert.False(relationship.ContainsFaction(null!));
        }

        #endregion

        #region InvolvesBothFactions Tests

        [Fact]
        public void InvolvesBothFactions_WithBothInOrder_ReturnsTrue()
        {
            var relationship = new FactionRelationship(FactionA, FactionB);

            Assert.True(relationship.InvolvesBothFactions(FactionA, FactionB));
        }

        [Fact]
        public void InvolvesBothFactions_WithBothReversed_ReturnsTrue()
        {
            var relationship = new FactionRelationship(FactionA, FactionB);

            Assert.True(relationship.InvolvesBothFactions(FactionB, FactionA));
        }

        [Fact]
        public void InvolvesBothFactions_WithOnlyFirst_ReturnsFalse()
        {
            var relationship = new FactionRelationship(FactionA, FactionB);

            Assert.False(relationship.InvolvesBothFactions(FactionA, "faction-c"));
        }

        [Fact]
        public void InvolvesBothFactions_WithOnlySecond_ReturnsFalse()
        {
            var relationship = new FactionRelationship(FactionA, FactionB);

            Assert.False(relationship.InvolvesBothFactions("faction-c", FactionB));
        }

        [Fact]
        public void InvolvesBothFactions_WithNeither_ReturnsFalse()
        {
            var relationship = new FactionRelationship(FactionA, FactionB);

            Assert.False(relationship.InvolvesBothFactions("faction-c", "faction-d"));
        }

        #endregion

        #region GetOtherFaction Tests

        [Fact]
        public void GetOtherFaction_WithFirstFaction_ReturnsSecond()
        {
            var relationship = new FactionRelationship(FactionA, FactionB);

            Assert.Equal(FactionB, relationship.GetOtherFaction(FactionA));
        }

        [Fact]
        public void GetOtherFaction_WithSecondFaction_ReturnsFirst()
        {
            var relationship = new FactionRelationship(FactionA, FactionB);

            Assert.Equal(FactionA, relationship.GetOtherFaction(FactionB));
        }

        [Fact]
        public void GetOtherFaction_WithOtherFaction_ReturnsNull()
        {
            var relationship = new FactionRelationship(FactionA, FactionB);

            Assert.Null(relationship.GetOtherFaction("faction-c"));
        }

        #endregion

        #region Equality Tests

        [Fact]
        public void Equals_SameFactions_ReturnsTrue()
        {
            var r1 = new FactionRelationship(FactionA, FactionB);
            var r2 = new FactionRelationship(FactionA, FactionB);

            Assert.True(r1.Equals(r2));
        }

        [Fact]
        public void Equals_SameFactionsReversed_ReturnsTrue()
        {
            var r1 = new FactionRelationship(FactionA, FactionB);
            var r2 = new FactionRelationship(FactionB, FactionA);

            Assert.True(r1.Equals(r2));
        }

        [Fact]
        public void Equals_DifferentFactions_ReturnsFalse()
        {
            var r1 = new FactionRelationship(FactionA, FactionB);
            var r2 = new FactionRelationship(FactionA, "faction-c");

            Assert.False(r1.Equals(r2));
        }

        [Fact]
        public void Equals_WithNull_ReturnsFalse()
        {
            var r1 = new FactionRelationship(FactionA, FactionB);

            Assert.False(r1.Equals(null));
        }

        [Fact]
        public void GetHashCode_SameFactions_ReturnsSameHash()
        {
            var r1 = new FactionRelationship(FactionA, FactionB);
            var r2 = new FactionRelationship(FactionA, FactionB);

            Assert.Equal(r1.GetHashCode(), r2.GetHashCode());
        }

        [Fact]
        public void GetHashCode_SameFactionsReversed_ReturnsSameHash()
        {
            var r1 = new FactionRelationship(FactionA, FactionB);
            var r2 = new FactionRelationship(FactionB, FactionA);

            Assert.Equal(r1.GetHashCode(), r2.GetHashCode());
        }

        [Fact]
        public void EqualityOperator_SameFactions_ReturnsTrue()
        {
            var r1 = new FactionRelationship(FactionA, FactionB);
            var r2 = new FactionRelationship(FactionA, FactionB);

            Assert.True(r1 == r2);
        }

        [Fact]
        public void InequalityOperator_DifferentFactions_ReturnsTrue()
        {
            var r1 = new FactionRelationship(FactionA, FactionB);
            var r2 = new FactionRelationship(FactionA, "faction-c");

            Assert.True(r1 != r2);
        }

        #endregion

        #region Constants Tests

        [Fact]
        public void MinValue_IsNegative100()
        {
            Assert.Equal(-100, FactionRelationship.MinValue);
        }

        [Fact]
        public void MaxValue_IsPositive100()
        {
            Assert.Equal(100, FactionRelationship.MaxValue);
        }

        #endregion

        #region ToString Tests

        [Fact]
        public void ToString_ReturnsReadableFormat()
        {
            var relationship = new FactionRelationship(FactionA, FactionB, 50);

            var result = relationship.ToString();

            Assert.Contains(FactionA, result);
            Assert.Contains(FactionB, result);
            Assert.Contains("50", result);
        }

        #endregion
    }
}
