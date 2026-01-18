using FactionWars.Factions.Models;
using System;
using System.Collections.Generic;
using Xunit;

namespace FactionWars.Tests.Unit.Factions
{
    public class FactionStateTests
    {
        #region Constructor and Required Properties

        [Fact]
        public void FactionState_ShouldRequireFactionId()
        {
            // Arrange & Act
            var state = new FactionState("faction_michael");

            // Assert
            Assert.Equal("faction_michael", state.FactionId);
        }

        [Fact]
        public void FactionState_ShouldThrowOnNullFactionId()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentNullException>(() => new FactionState(null!));
        }

        [Fact]
        public void FactionState_ShouldThrowOnEmptyFactionId()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentException>(() => new FactionState(""));
        }

        [Fact]
        public void FactionState_ShouldThrowOnWhitespaceFactionId()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentException>(() => new FactionState("   "));
        }

        #endregion

        #region Resources - Cash

        [Fact]
        public void FactionState_ShouldHaveZeroCashByDefault()
        {
            // Arrange & Act
            var state = new FactionState("faction_michael");

            // Assert
            Assert.Equal(0, state.Cash);
        }

        [Fact]
        public void FactionState_ShouldAllowSettingCash()
        {
            // Arrange
            var state = new FactionState("faction_michael");

            // Act
            state.Cash = 50000;

            // Assert
            Assert.Equal(50000, state.Cash);
        }

        [Fact]
        public void FactionState_ShouldNotAllowNegativeCash()
        {
            // Arrange
            var state = new FactionState("faction_michael");

            // Act
            state.Cash = -100;

            // Assert - Should clamp to 0
            Assert.Equal(0, state.Cash);
        }

        [Fact]
        public void FactionState_ShouldAllowInitialCash()
        {
            // Arrange & Act
            var state = new FactionState("faction_michael", initialCash: 10000);

            // Assert
            Assert.Equal(10000, state.Cash);
        }

        #endregion

        #region Resources - Recruitment Points

        [Fact]
        public void FactionState_ShouldHaveZeroRecruitmentPointsByDefault()
        {
            // Arrange & Act
            var state = new FactionState("faction_michael");

            // Assert
            Assert.Equal(0, state.RecruitmentPoints);
        }

        [Fact]
        public void FactionState_ShouldAllowSettingRecruitmentPoints()
        {
            // Arrange
            var state = new FactionState("faction_michael");

            // Act
            state.RecruitmentPoints = 100;

            // Assert
            Assert.Equal(100, state.RecruitmentPoints);
        }

        [Fact]
        public void FactionState_ShouldNotAllowNegativeRecruitmentPoints()
        {
            // Arrange
            var state = new FactionState("faction_michael");

            // Act
            state.RecruitmentPoints = -50;

            // Assert - Should clamp to 0
            Assert.Equal(0, state.RecruitmentPoints);
        }

        #endregion

        #region Resources - Weapons

        [Fact]
        public void FactionState_ShouldHaveZeroWeaponsByDefault()
        {
            // Arrange & Act
            var state = new FactionState("faction_michael");

            // Assert
            Assert.Equal(0, state.Weapons);
        }

        [Fact]
        public void FactionState_ShouldAllowSettingWeapons()
        {
            // Arrange
            var state = new FactionState("faction_michael");

            // Act
            state.Weapons = 25;

            // Assert
            Assert.Equal(25, state.Weapons);
        }

        [Fact]
        public void FactionState_ShouldNotAllowNegativeWeapons()
        {
            // Arrange
            var state = new FactionState("faction_michael");

            // Act
            state.Weapons = -10;

            // Assert - Should clamp to 0
            Assert.Equal(0, state.Weapons);
        }

        #endregion

        #region Army - Troop Count

        [Fact]
        public void FactionState_ShouldHaveZeroTroopsByDefault()
        {
            // Arrange & Act
            var state = new FactionState("faction_michael");

            // Assert
            Assert.Equal(0, state.TroopCount);
        }

        [Fact]
        public void FactionState_ShouldAllowSettingTroopCount()
        {
            // Arrange
            var state = new FactionState("faction_michael");

            // Act
            state.TroopCount = 50;

            // Assert
            Assert.Equal(50, state.TroopCount);
        }

        [Fact]
        public void FactionState_ShouldNotAllowNegativeTroopCount()
        {
            // Arrange
            var state = new FactionState("faction_michael");

            // Act
            state.TroopCount = -5;

            // Assert - Should clamp to 0
            Assert.Equal(0, state.TroopCount);
        }

        [Fact]
        public void FactionState_ShouldAllowInitialTroopCount()
        {
            // Arrange & Act
            var state = new FactionState("faction_michael", initialTroopCount: 20);

            // Assert
            Assert.Equal(20, state.TroopCount);
        }

        #endregion

        #region Zones - Owned Zone IDs

        [Fact]
        public void FactionState_ShouldHaveEmptyOwnedZonesByDefault()
        {
            // Arrange & Act
            var state = new FactionState("faction_michael");

            // Assert
            Assert.Empty(state.OwnedZoneIds);
        }

        [Fact]
        public void FactionState_OwnedZoneIds_ShouldBeReadOnly()
        {
            // Arrange
            var state = new FactionState("faction_michael");

            // Assert - The collection should be IReadOnlyCollection
            Assert.IsAssignableFrom<IReadOnlyCollection<string>>(state.OwnedZoneIds);
        }

        [Fact]
        public void FactionState_AddZone_ShouldAddZoneId()
        {
            // Arrange
            var state = new FactionState("faction_michael");

            // Act
            state.AddZone("zone_downtown");

            // Assert
            Assert.Contains("zone_downtown", state.OwnedZoneIds);
            Assert.Single(state.OwnedZoneIds);
        }

        [Fact]
        public void FactionState_AddZone_ShouldNotAddDuplicates()
        {
            // Arrange
            var state = new FactionState("faction_michael");
            state.AddZone("zone_downtown");

            // Act
            state.AddZone("zone_downtown");

            // Assert
            Assert.Single(state.OwnedZoneIds);
        }

        [Fact]
        public void FactionState_AddZone_ShouldThrowOnNullZoneId()
        {
            // Arrange
            var state = new FactionState("faction_michael");

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => state.AddZone(null!));
        }

        [Fact]
        public void FactionState_AddZone_ShouldThrowOnEmptyZoneId()
        {
            // Arrange
            var state = new FactionState("faction_michael");

            // Act & Assert
            Assert.Throws<ArgumentException>(() => state.AddZone(""));
        }

        [Fact]
        public void FactionState_RemoveZone_ShouldRemoveZoneId()
        {
            // Arrange
            var state = new FactionState("faction_michael");
            state.AddZone("zone_downtown");
            state.AddZone("zone_vinewood");

            // Act
            var removed = state.RemoveZone("zone_downtown");

            // Assert
            Assert.True(removed);
            Assert.DoesNotContain("zone_downtown", state.OwnedZoneIds);
            Assert.Contains("zone_vinewood", state.OwnedZoneIds);
        }

        [Fact]
        public void FactionState_RemoveZone_ShouldReturnFalseIfNotFound()
        {
            // Arrange
            var state = new FactionState("faction_michael");

            // Act
            var removed = state.RemoveZone("zone_nonexistent");

            // Assert
            Assert.False(removed);
        }

        [Fact]
        public void FactionState_OwnsZone_ShouldReturnTrueIfOwned()
        {
            // Arrange
            var state = new FactionState("faction_michael");
            state.AddZone("zone_downtown");

            // Act & Assert
            Assert.True(state.OwnsZone("zone_downtown"));
        }

        [Fact]
        public void FactionState_OwnsZone_ShouldReturnFalseIfNotOwned()
        {
            // Arrange
            var state = new FactionState("faction_michael");

            // Act & Assert
            Assert.False(state.OwnsZone("zone_downtown"));
        }

        [Fact]
        public void FactionState_ZoneCount_ShouldReturnCorrectCount()
        {
            // Arrange
            var state = new FactionState("faction_michael");
            state.AddZone("zone_downtown");
            state.AddZone("zone_vinewood");
            state.AddZone("zone_airport");

            // Act & Assert
            Assert.Equal(3, state.ZoneCount);
        }

        #endregion

        #region Resource Operations

        [Fact]
        public void FactionState_AddCash_ShouldIncreaseCash()
        {
            // Arrange
            var state = new FactionState("faction_michael", initialCash: 1000);

            // Act
            state.AddCash(500);

            // Assert
            Assert.Equal(1500, state.Cash);
        }

        [Fact]
        public void FactionState_AddCash_ShouldThrowOnNegativeAmount()
        {
            // Arrange
            var state = new FactionState("faction_michael");

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => state.AddCash(-100));
        }

        [Fact]
        public void FactionState_SpendCash_ShouldDecreaseCash()
        {
            // Arrange
            var state = new FactionState("faction_michael", initialCash: 1000);

            // Act
            var success = state.SpendCash(300);

            // Assert
            Assert.True(success);
            Assert.Equal(700, state.Cash);
        }

        [Fact]
        public void FactionState_SpendCash_ShouldReturnFalseIfInsufficientFunds()
        {
            // Arrange
            var state = new FactionState("faction_michael", initialCash: 100);

            // Act
            var success = state.SpendCash(500);

            // Assert
            Assert.False(success);
            Assert.Equal(100, state.Cash); // Cash unchanged
        }

        [Fact]
        public void FactionState_SpendCash_ShouldThrowOnNegativeAmount()
        {
            // Arrange
            var state = new FactionState("faction_michael");

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => state.SpendCash(-100));
        }

        [Fact]
        public void FactionState_CanAfford_ShouldReturnTrueIfSufficientFunds()
        {
            // Arrange
            var state = new FactionState("faction_michael", initialCash: 1000);

            // Act & Assert
            Assert.True(state.CanAfford(500));
            Assert.True(state.CanAfford(1000));
        }

        [Fact]
        public void FactionState_CanAfford_ShouldReturnFalseIfInsufficientFunds()
        {
            // Arrange
            var state = new FactionState("faction_michael", initialCash: 100);

            // Act & Assert
            Assert.False(state.CanAfford(500));
        }

        #endregion

        #region Troop Operations

        [Fact]
        public void FactionState_RecruitTroops_ShouldIncreaseTroopCount()
        {
            // Arrange
            var state = new FactionState("faction_michael", initialTroopCount: 10);

            // Act
            state.RecruitTroops(5);

            // Assert
            Assert.Equal(15, state.TroopCount);
        }

        [Fact]
        public void FactionState_RecruitTroops_ShouldThrowOnNegativeCount()
        {
            // Arrange
            var state = new FactionState("faction_michael");

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => state.RecruitTroops(-5));
        }

        [Fact]
        public void FactionState_LoseTroops_ShouldDecreaseTroopCount()
        {
            // Arrange
            var state = new FactionState("faction_michael", initialTroopCount: 20);

            // Act
            state.LoseTroops(5);

            // Assert
            Assert.Equal(15, state.TroopCount);
        }

        [Fact]
        public void FactionState_LoseTroops_ShouldClampToZero()
        {
            // Arrange
            var state = new FactionState("faction_michael", initialTroopCount: 10);

            // Act
            state.LoseTroops(15);

            // Assert - Should clamp to 0, not go negative
            Assert.Equal(0, state.TroopCount);
        }

        [Fact]
        public void FactionState_LoseTroops_ShouldThrowOnNegativeCount()
        {
            // Arrange
            var state = new FactionState("faction_michael");

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => state.LoseTroops(-5));
        }

        [Fact]
        public void FactionState_HasTroops_ShouldReturnTrueIfTroopsAvailable()
        {
            // Arrange
            var state = new FactionState("faction_michael", initialTroopCount: 10);

            // Act & Assert
            Assert.True(state.HasTroops(5));
            Assert.True(state.HasTroops(10));
        }

        [Fact]
        public void FactionState_HasTroops_ShouldReturnFalseIfInsufficientTroops()
        {
            // Arrange
            var state = new FactionState("faction_michael", initialTroopCount: 5);

            // Act & Assert
            Assert.False(state.HasTroops(10));
        }

        #endregion

        #region Military Strength

        [Fact]
        public void FactionState_MilitaryStrength_ShouldCombineTroopsAndWeapons()
        {
            // Arrange
            var state = new FactionState("faction_michael", initialTroopCount: 10);
            state.Weapons = 5;

            // Act
            var strength = state.MilitaryStrength;

            // Assert - Strength = TroopCount + (Weapons * WeaponMultiplier)
            // With default weapon multiplier of 2: 10 + (5 * 2) = 20
            Assert.Equal(20, strength);
        }

        [Fact]
        public void FactionState_MilitaryStrength_ShouldBeZeroWithNoTroopsOrWeapons()
        {
            // Arrange
            var state = new FactionState("faction_michael");

            // Act & Assert
            Assert.Equal(0, state.MilitaryStrength);
        }

        [Fact]
        public void FactionState_MilitaryStrength_ShouldScaleWithTroops()
        {
            // Arrange
            var state = new FactionState("faction_michael", initialTroopCount: 50);

            // Act & Assert
            Assert.Equal(50, state.MilitaryStrength); // No weapons, just troops
        }

        #endregion

        #region Equality

        [Fact]
        public void FactionState_ShouldBeEqualByFactionId()
        {
            // Arrange
            var state1 = new FactionState("faction_michael");
            state1.Cash = 1000;

            var state2 = new FactionState("faction_michael");
            state2.Cash = 5000;

            // Act & Assert - States are equal if they have the same faction ID
            Assert.Equal(state1, state2);
        }

        [Fact]
        public void FactionState_ShouldNotBeEqualWithDifferentFactionId()
        {
            // Arrange
            var state1 = new FactionState("faction_michael");
            var state2 = new FactionState("faction_trevor");

            // Act & Assert
            Assert.NotEqual(state1, state2);
        }

        [Fact]
        public void FactionState_GetHashCode_ShouldBeConsistentWithEquals()
        {
            // Arrange
            var state1 = new FactionState("faction_michael");
            var state2 = new FactionState("faction_michael");

            // Act & Assert
            Assert.Equal(state1.GetHashCode(), state2.GetHashCode());
        }

        [Fact]
        public void FactionState_ShouldNotBeEqualToNull()
        {
            // Arrange
            var state = new FactionState("faction_michael");

            // Act & Assert
            Assert.False(state.Equals(null));
        }

        #endregion

        #region ToString

        [Fact]
        public void FactionState_ToString_ShouldReturnReadableFormat()
        {
            // Arrange
            var state = new FactionState("faction_michael", initialCash: 5000, initialTroopCount: 15);
            state.AddZone("zone_downtown");

            // Act
            var result = state.ToString();

            // Assert
            Assert.Contains("faction_michael", result);
            Assert.Contains("5000", result);
            Assert.Contains("15", result);
        }

        #endregion
    }
}
