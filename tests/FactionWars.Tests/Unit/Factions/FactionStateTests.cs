using FactionWars.Core.Models;
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
        public void FactionState_TroopCount_ShouldReflectReservePool()
        {
            // Arrange - After consolidation, TroopCount is computed from reserve pool
            var state = new FactionState("faction_michael");

            // Act - Add troops through reserve pool
            state.AddReserveTroops(DefenderTier.Basic, 30);
            state.AddReserveTroops(DefenderTier.Medium, 20);

            // Assert - TroopCount reflects total reserve
            Assert.Equal(50, state.TroopCount);
        }

        [Fact]
        public void FactionState_TroopCount_ShouldUpdateWhenReservePoolChanges()
        {
            // Arrange - After consolidation, TroopCount is read-only computed property
            var state = new FactionState("faction_michael");
            state.AddReserveTroops(DefenderTier.Basic, 10);

            // Act - Remove some from reserve
            state.RemoveReserveTroops(DefenderTier.Basic, 3);

            // Assert - TroopCount reflects change
            Assert.Equal(7, state.TroopCount);
        }

        [Fact]
        public void FactionState_ShouldAllowInitialTroopCount()
        {
            // Arrange & Act
            // After consolidation, initialTroopCount adds to Basic tier reserve
            var state = new FactionState("faction_michael", initialTroopCount: 20);

            // Assert - TroopCount is computed from reserve pool
            Assert.Equal(20, state.TroopCount);
            Assert.Equal(20, state.GetReserveTroops(DefenderTier.Basic));
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
        public void FactionState_RecruitTroops_ShouldAddToBasicReserve()
        {
            // Arrange - After consolidation, RecruitTroops adds to Basic tier reserve
            var state = new FactionState("faction_michael", initialTroopCount: 10);

            // Act
            state.RecruitTroops(5);

            // Assert - TroopCount reflects reserve pool, Basic tier increased
            Assert.Equal(15, state.TroopCount);
            Assert.Equal(15, state.GetReserveTroops(DefenderTier.Basic));
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
        public void FactionState_LoseTroops_ShouldRemoveFromBasicFirst()
        {
            // Arrange - After consolidation, LoseTroops removes Basic first
            var state = new FactionState("faction_michael");
            state.AddReserveTroops(DefenderTier.Basic, 10);
            state.AddReserveTroops(DefenderTier.Medium, 5);

            // Act
            state.LoseTroops(8);

            // Assert - 8 removed from Basic (10->2), Medium unchanged
            Assert.Equal(7, state.TroopCount);
            Assert.Equal(2, state.GetReserveTroops(DefenderTier.Basic));
            Assert.Equal(5, state.GetReserveTroops(DefenderTier.Medium));
        }

        [Fact]
        public void FactionState_LoseTroops_ShouldOverflowToMediumTier()
        {
            // Arrange - When Basic is depleted, overflow to Medium
            var state = new FactionState("faction_michael");
            state.AddReserveTroops(DefenderTier.Basic, 5);
            state.AddReserveTroops(DefenderTier.Medium, 10);

            // Act - Lose 8 troops (5 from Basic, 3 from Medium)
            state.LoseTroops(8);

            // Assert
            Assert.Equal(7, state.TroopCount);
            Assert.Equal(0, state.GetReserveTroops(DefenderTier.Basic));
            Assert.Equal(7, state.GetReserveTroops(DefenderTier.Medium));
        }

        [Fact]
        public void FactionState_LoseTroops_ShouldClampToZero()
        {
            // Arrange - After consolidation, LoseTroops removes from reserve pool
            var state = new FactionState("faction_michael", initialTroopCount: 10);

            // Act
            state.LoseTroops(15);

            // Assert - Should clamp to 0, not go negative
            Assert.Equal(0, state.TroopCount);
            Assert.Equal(0, state.GetReserveTroops(DefenderTier.Basic));
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
        public void FactionState_MilitaryStrength_ShouldCombineReserveAndWeapons()
        {
            // Arrange - After consolidation, MilitaryStrength uses TotalReserveTroops
            var state = new FactionState("faction_michael");
            state.AddReserveTroops(DefenderTier.Basic, 10);
            state.Weapons = 5;

            // Act
            var strength = state.MilitaryStrength;

            // Assert - Strength = TotalReserveTroops + (Weapons * WeaponMultiplier)
            // 10 + (5 * 2) = 20
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
        public void FactionState_MilitaryStrength_ShouldScaleWithAllTiers()
        {
            // Arrange - After consolidation, all tiers contribute to strength
            var state = new FactionState("faction_michael");
            state.AddReserveTroops(DefenderTier.Basic, 20);
            state.AddReserveTroops(DefenderTier.Medium, 20);
            state.AddReserveTroops(DefenderTier.Heavy, 10);

            // Act & Assert - 50 total reserve troops, no weapons
            Assert.Equal(50, state.MilitaryStrength);
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

        #region Reserve Pool - Troops by Tier

        [Fact]
        public void FactionState_ShouldHaveEmptyReservePoolByDefault()
        {
            // Arrange & Act
            var state = new FactionState("faction_michael");

            // Assert
            Assert.Equal(0, state.GetReserveTroops(DefenderTier.Basic));
            Assert.Equal(0, state.GetReserveTroops(DefenderTier.Medium));
            Assert.Equal(0, state.GetReserveTroops(DefenderTier.Heavy));
        }

        [Fact]
        public void FactionState_AddReserveTroops_ShouldAddToCorrectTier()
        {
            // Arrange
            var state = new FactionState("faction_michael");

            // Act
            state.AddReserveTroops(DefenderTier.Basic, 10);
            state.AddReserveTroops(DefenderTier.Medium, 5);
            state.AddReserveTroops(DefenderTier.Heavy, 2);

            // Assert
            Assert.Equal(10, state.GetReserveTroops(DefenderTier.Basic));
            Assert.Equal(5, state.GetReserveTroops(DefenderTier.Medium));
            Assert.Equal(2, state.GetReserveTroops(DefenderTier.Heavy));
        }

        [Fact]
        public void FactionState_AddReserveTroops_ShouldAccumulateTroops()
        {
            // Arrange
            var state = new FactionState("faction_michael");

            // Act
            state.AddReserveTroops(DefenderTier.Basic, 5);
            state.AddReserveTroops(DefenderTier.Basic, 3);

            // Assert
            Assert.Equal(8, state.GetReserveTroops(DefenderTier.Basic));
        }

        [Fact]
        public void FactionState_AddReserveTroops_ShouldThrowOnNegativeCount()
        {
            // Arrange
            var state = new FactionState("faction_michael");

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                state.AddReserveTroops(DefenderTier.Basic, -1));
        }

        [Fact]
        public void FactionState_RemoveReserveTroops_ShouldRemoveFromCorrectTier()
        {
            // Arrange
            var state = new FactionState("faction_michael");
            state.AddReserveTroops(DefenderTier.Basic, 10);

            // Act
            var removed = state.RemoveReserveTroops(DefenderTier.Basic, 3);

            // Assert
            Assert.True(removed);
            Assert.Equal(7, state.GetReserveTroops(DefenderTier.Basic));
        }

        [Fact]
        public void FactionState_RemoveReserveTroops_ShouldReturnFalseIfInsufficientTroops()
        {
            // Arrange
            var state = new FactionState("faction_michael");
            state.AddReserveTroops(DefenderTier.Basic, 5);

            // Act
            var removed = state.RemoveReserveTroops(DefenderTier.Basic, 10);

            // Assert
            Assert.False(removed);
            Assert.Equal(5, state.GetReserveTroops(DefenderTier.Basic)); // Unchanged
        }

        [Fact]
        public void FactionState_RemoveReserveTroops_ShouldThrowOnNegativeCount()
        {
            // Arrange
            var state = new FactionState("faction_michael");

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                state.RemoveReserveTroops(DefenderTier.Basic, -1));
        }

        [Fact]
        public void FactionState_HasReserveTroops_ShouldReturnTrueIfSufficientTroops()
        {
            // Arrange
            var state = new FactionState("faction_michael");
            state.AddReserveTroops(DefenderTier.Heavy, 5);

            // Act & Assert
            Assert.True(state.HasReserveTroops(DefenderTier.Heavy, 3));
            Assert.True(state.HasReserveTroops(DefenderTier.Heavy, 5));
        }

        [Fact]
        public void FactionState_HasReserveTroops_ShouldReturnFalseIfInsufficientTroops()
        {
            // Arrange
            var state = new FactionState("faction_michael");
            state.AddReserveTroops(DefenderTier.Heavy, 5);

            // Act & Assert
            Assert.False(state.HasReserveTroops(DefenderTier.Heavy, 10));
        }

        [Fact]
        public void FactionState_TotalReserveTroops_ShouldSumAllTiers()
        {
            // Arrange
            var state = new FactionState("faction_michael");
            state.AddReserveTroops(DefenderTier.Basic, 10);
            state.AddReserveTroops(DefenderTier.Medium, 5);
            state.AddReserveTroops(DefenderTier.Heavy, 2);

            // Act & Assert
            Assert.Equal(17, state.TotalReserveTroops);
        }

        [Fact]
        public void FactionState_TroopCount_ShouldEqualTotalReserveTroops()
        {
            // Arrange - After consolidation, TroopCount is computed from reserve pool
            var state = new FactionState("faction_michael");
            state.AddReserveTroops(DefenderTier.Basic, 10);
            state.AddReserveTroops(DefenderTier.Medium, 5);
            state.AddReserveTroops(DefenderTier.Heavy, 2);

            // Act & Assert - TroopCount should equal sum of all reserve troops
            Assert.Equal(17, state.TroopCount);
            Assert.Equal(state.TotalReserveTroops, state.TroopCount);
        }

        [Fact]
        public void FactionState_GetReservePoolCopy_ShouldReturnDictionaryOfTiers()
        {
            // Arrange
            var state = new FactionState("faction_michael");
            state.AddReserveTroops(DefenderTier.Basic, 10);
            state.AddReserveTroops(DefenderTier.Medium, 5);

            // Act
            var pool = state.GetReservePoolCopy();

            // Assert
            Assert.Equal(10, pool[DefenderTier.Basic]);
            Assert.Equal(5, pool[DefenderTier.Medium]);
            Assert.Equal(0, pool[DefenderTier.Heavy]);
        }

        [Fact]
        public void FactionState_GetReservePoolCopy_ShouldBeIndependentOfInternalState()
        {
            // Arrange
            var state = new FactionState("faction_michael");
            state.AddReserveTroops(DefenderTier.Basic, 10);

            // Act - modify the returned copy
            var pool = state.GetReservePoolCopy();
            pool[DefenderTier.Basic] = 100;

            // Assert - internal state should be unchanged
            Assert.Equal(10, state.GetReserveTroops(DefenderTier.Basic));
        }

        #endregion
    }
}
