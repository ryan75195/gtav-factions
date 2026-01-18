using System;
using Xunit;
using FactionWars.Escalation.Models;

namespace FactionWars.Tests.Unit.Escalation
{
    /// <summary>
    /// Tests for the VehicleUnlock model which represents a vehicle that can be unlocked
    /// at a specific escalation tier.
    /// </summary>
    public class VehicleUnlockTests
    {
        #region Constructor Tests

        [Fact]
        public void Constructor_WithValidParameters_CreatesVehicleUnlock()
        {
            var vehicleUnlock = new VehicleUnlock(
                "BLISTA",
                "Blista",
                VehicleCategory.Compact,
                EscalationTier.Tier1);

            Assert.NotNull(vehicleUnlock);
            Assert.Equal("BLISTA", vehicleUnlock.VehicleModel);
            Assert.Equal("Blista", vehicleUnlock.DisplayName);
            Assert.Equal(VehicleCategory.Compact, vehicleUnlock.Category);
            Assert.Equal(EscalationTier.Tier1, vehicleUnlock.RequiredTier);
        }

        [Fact]
        public void Constructor_WithAllParameters_SetsAllProperties()
        {
            var vehicleUnlock = new VehicleUnlock(
                "INSURGENT",
                "Insurgent",
                VehicleCategory.Armored,
                EscalationTier.Tier4,
                "Heavy armored personnel carrier",
                150);

            Assert.Equal("INSURGENT", vehicleUnlock.VehicleModel);
            Assert.Equal("Insurgent", vehicleUnlock.DisplayName);
            Assert.Equal(VehicleCategory.Armored, vehicleUnlock.Category);
            Assert.Equal(EscalationTier.Tier4, vehicleUnlock.RequiredTier);
            Assert.Equal("Heavy armored personnel carrier", vehicleUnlock.Description);
            Assert.Equal(150, vehicleUnlock.MaxSpeed);
        }

        [Fact]
        public void Constructor_WithNullVehicleModel_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new VehicleUnlock(
                null!,
                "Blista",
                VehicleCategory.Compact,
                EscalationTier.Tier1));
        }

        [Fact]
        public void Constructor_WithEmptyVehicleModel_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new VehicleUnlock(
                "",
                "Blista",
                VehicleCategory.Compact,
                EscalationTier.Tier1));
        }

        [Fact]
        public void Constructor_WithWhitespaceVehicleModel_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new VehicleUnlock(
                "   ",
                "Blista",
                VehicleCategory.Compact,
                EscalationTier.Tier1));
        }

        [Fact]
        public void Constructor_WithNullDisplayName_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new VehicleUnlock(
                "BLISTA",
                null!,
                VehicleCategory.Compact,
                EscalationTier.Tier1));
        }

        [Fact]
        public void Constructor_WithEmptyDisplayName_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new VehicleUnlock(
                "BLISTA",
                "",
                VehicleCategory.Compact,
                EscalationTier.Tier1));
        }

        [Fact]
        public void Constructor_WithWhitespaceDisplayName_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new VehicleUnlock(
                "BLISTA",
                "   ",
                VehicleCategory.Compact,
                EscalationTier.Tier1));
        }

        [Fact]
        public void Constructor_WithDefaultDescription_SetsDescriptionToNull()
        {
            var vehicleUnlock = new VehicleUnlock(
                "BLISTA",
                "Blista",
                VehicleCategory.Compact,
                EscalationTier.Tier1);

            Assert.Null(vehicleUnlock.Description);
        }

        [Fact]
        public void Constructor_WithDefaultMaxSpeed_SetsMaxSpeedToDefaultValue()
        {
            var vehicleUnlock = new VehicleUnlock(
                "BLISTA",
                "Blista",
                VehicleCategory.Compact,
                EscalationTier.Tier1);

            Assert.Equal(VehicleUnlock.DefaultMaxSpeed, vehicleUnlock.MaxSpeed);
        }

        [Fact]
        public void Constructor_WithNegativeMaxSpeed_ClampsToZero()
        {
            var vehicleUnlock = new VehicleUnlock(
                "BLISTA",
                "Blista",
                VehicleCategory.Compact,
                EscalationTier.Tier1,
                null,
                -50);

            Assert.Equal(0, vehicleUnlock.MaxSpeed);
        }

        [Fact]
        public void Constructor_WithZeroMaxSpeed_AllowsZeroMaxSpeed()
        {
            var vehicleUnlock = new VehicleUnlock(
                "BLISTA",
                "Blista",
                VehicleCategory.Compact,
                EscalationTier.Tier1,
                null,
                0);

            Assert.Equal(0, vehicleUnlock.MaxSpeed);
        }

        #endregion

        #region Property Tests

        [Theory]
        [InlineData(EscalationTier.Tier1)]
        [InlineData(EscalationTier.Tier2)]
        [InlineData(EscalationTier.Tier3)]
        [InlineData(EscalationTier.Tier4)]
        [InlineData(EscalationTier.Tier5)]
        public void RequiredTier_CanBeAnyTier(EscalationTier tier)
        {
            var vehicleUnlock = new VehicleUnlock(
                "VEHICLE_TEST",
                "Test Vehicle",
                VehicleCategory.Compact,
                tier);

            Assert.Equal(tier, vehicleUnlock.RequiredTier);
        }

        [Theory]
        [InlineData(VehicleCategory.Compact)]
        [InlineData(VehicleCategory.Sedan)]
        [InlineData(VehicleCategory.SUV)]
        [InlineData(VehicleCategory.Coupe)]
        [InlineData(VehicleCategory.Muscle)]
        [InlineData(VehicleCategory.Sports)]
        [InlineData(VehicleCategory.Motorcycle)]
        [InlineData(VehicleCategory.Van)]
        [InlineData(VehicleCategory.Armored)]
        [InlineData(VehicleCategory.Military)]
        public void Category_CanBeAnyCategory(VehicleCategory category)
        {
            var vehicleUnlock = new VehicleUnlock(
                "VEHICLE_TEST",
                "Test Vehicle",
                category,
                EscalationTier.Tier1);

            Assert.Equal(category, vehicleUnlock.Category);
        }

        #endregion

        #region IsUnlockedAtTier Tests

        [Fact]
        public void IsUnlockedAtTier_AtExactTier_ReturnsTrue()
        {
            var vehicleUnlock = new VehicleUnlock(
                "SCHAFTER2",
                "Schafter",
                VehicleCategory.Sedan,
                EscalationTier.Tier2);

            Assert.True(vehicleUnlock.IsUnlockedAtTier(EscalationTier.Tier2));
        }

        [Fact]
        public void IsUnlockedAtTier_AboveRequiredTier_ReturnsTrue()
        {
            var vehicleUnlock = new VehicleUnlock(
                "SCHAFTER2",
                "Schafter",
                VehicleCategory.Sedan,
                EscalationTier.Tier2);

            Assert.True(vehicleUnlock.IsUnlockedAtTier(EscalationTier.Tier3));
            Assert.True(vehicleUnlock.IsUnlockedAtTier(EscalationTier.Tier4));
            Assert.True(vehicleUnlock.IsUnlockedAtTier(EscalationTier.Tier5));
        }

        [Fact]
        public void IsUnlockedAtTier_BelowRequiredTier_ReturnsFalse()
        {
            var vehicleUnlock = new VehicleUnlock(
                "INSURGENT",
                "Insurgent",
                VehicleCategory.Armored,
                EscalationTier.Tier4);

            Assert.False(vehicleUnlock.IsUnlockedAtTier(EscalationTier.Tier1));
            Assert.False(vehicleUnlock.IsUnlockedAtTier(EscalationTier.Tier2));
            Assert.False(vehicleUnlock.IsUnlockedAtTier(EscalationTier.Tier3));
        }

        [Fact]
        public void IsUnlockedAtTier_Tier1Vehicle_UnlockedAtAllTiers()
        {
            var vehicleUnlock = new VehicleUnlock(
                "BLISTA",
                "Blista",
                VehicleCategory.Compact,
                EscalationTier.Tier1);

            Assert.True(vehicleUnlock.IsUnlockedAtTier(EscalationTier.Tier1));
            Assert.True(vehicleUnlock.IsUnlockedAtTier(EscalationTier.Tier2));
            Assert.True(vehicleUnlock.IsUnlockedAtTier(EscalationTier.Tier3));
            Assert.True(vehicleUnlock.IsUnlockedAtTier(EscalationTier.Tier4));
            Assert.True(vehicleUnlock.IsUnlockedAtTier(EscalationTier.Tier5));
        }

        [Fact]
        public void IsUnlockedAtTier_Tier5Vehicle_OnlyUnlockedAtTier5()
        {
            var vehicleUnlock = new VehicleUnlock(
                "RHINO",
                "Rhino Tank",
                VehicleCategory.Military,
                EscalationTier.Tier5);

            Assert.False(vehicleUnlock.IsUnlockedAtTier(EscalationTier.Tier1));
            Assert.False(vehicleUnlock.IsUnlockedAtTier(EscalationTier.Tier2));
            Assert.False(vehicleUnlock.IsUnlockedAtTier(EscalationTier.Tier3));
            Assert.False(vehicleUnlock.IsUnlockedAtTier(EscalationTier.Tier4));
            Assert.True(vehicleUnlock.IsUnlockedAtTier(EscalationTier.Tier5));
        }

        #endregion

        #region Equality Tests

        [Fact]
        public void Equals_SameVehicleModel_ReturnsTrue()
        {
            var vehicle1 = new VehicleUnlock(
                "BLISTA",
                "Blista",
                VehicleCategory.Compact,
                EscalationTier.Tier1);

            var vehicle2 = new VehicleUnlock(
                "BLISTA",
                "Different Name",
                VehicleCategory.Sedan,
                EscalationTier.Tier5);

            Assert.True(vehicle1.Equals(vehicle2));
        }

        [Fact]
        public void Equals_DifferentVehicleModel_ReturnsFalse()
        {
            var vehicle1 = new VehicleUnlock(
                "BLISTA",
                "Blista",
                VehicleCategory.Compact,
                EscalationTier.Tier1);

            var vehicle2 = new VehicleUnlock(
                "SCHAFTER2",
                "Schafter",
                VehicleCategory.Sedan,
                EscalationTier.Tier2);

            Assert.False(vehicle1.Equals(vehicle2));
        }

        [Fact]
        public void Equals_NullObject_ReturnsFalse()
        {
            var vehicle = new VehicleUnlock(
                "BLISTA",
                "Blista",
                VehicleCategory.Compact,
                EscalationTier.Tier1);

            Assert.False(vehicle.Equals(null));
        }

        [Fact]
        public void EqualsObject_SameVehicleModel_ReturnsTrue()
        {
            var vehicle1 = new VehicleUnlock(
                "BLISTA",
                "Blista",
                VehicleCategory.Compact,
                EscalationTier.Tier1);

            object vehicle2 = new VehicleUnlock(
                "BLISTA",
                "Blista",
                VehicleCategory.Compact,
                EscalationTier.Tier1);

            Assert.True(vehicle1.Equals(vehicle2));
        }

        [Fact]
        public void EqualsObject_DifferentType_ReturnsFalse()
        {
            var vehicle = new VehicleUnlock(
                "BLISTA",
                "Blista",
                VehicleCategory.Compact,
                EscalationTier.Tier1);

            Assert.False(vehicle.Equals("BLISTA"));
        }

        [Fact]
        public void GetHashCode_SameVehicleModel_ReturnsSameHashCode()
        {
            var vehicle1 = new VehicleUnlock(
                "BLISTA",
                "Blista",
                VehicleCategory.Compact,
                EscalationTier.Tier1);

            var vehicle2 = new VehicleUnlock(
                "BLISTA",
                "Different Name",
                VehicleCategory.Sedan,
                EscalationTier.Tier5);

            Assert.Equal(vehicle1.GetHashCode(), vehicle2.GetHashCode());
        }

        [Fact]
        public void OperatorEquals_SameVehicleModel_ReturnsTrue()
        {
            var vehicle1 = new VehicleUnlock(
                "BLISTA",
                "Blista",
                VehicleCategory.Compact,
                EscalationTier.Tier1);

            var vehicle2 = new VehicleUnlock(
                "BLISTA",
                "Blista",
                VehicleCategory.Compact,
                EscalationTier.Tier1);

            Assert.True(vehicle1 == vehicle2);
        }

        [Fact]
        public void OperatorNotEquals_DifferentVehicleModel_ReturnsTrue()
        {
            var vehicle1 = new VehicleUnlock(
                "BLISTA",
                "Blista",
                VehicleCategory.Compact,
                EscalationTier.Tier1);

            var vehicle2 = new VehicleUnlock(
                "SCHAFTER2",
                "Schafter",
                VehicleCategory.Sedan,
                EscalationTier.Tier2);

            Assert.True(vehicle1 != vehicle2);
        }

        [Fact]
        public void OperatorEquals_BothNull_ReturnsTrue()
        {
            VehicleUnlock? vehicle1 = null;
            VehicleUnlock? vehicle2 = null;

            Assert.True(vehicle1 == vehicle2);
        }

        [Fact]
        public void OperatorEquals_OneNull_ReturnsFalse()
        {
            var vehicle1 = new VehicleUnlock(
                "BLISTA",
                "Blista",
                VehicleCategory.Compact,
                EscalationTier.Tier1);
            VehicleUnlock? vehicle2 = null;

            Assert.False(vehicle1 == vehicle2);
            Assert.True(vehicle1 != vehicle2);
        }

        #endregion

        #region ToString Tests

        [Fact]
        public void ToString_ReturnsFormattedString()
        {
            var vehicle = new VehicleUnlock(
                "INSURGENT",
                "Insurgent",
                VehicleCategory.Armored,
                EscalationTier.Tier4);

            var result = vehicle.ToString();

            Assert.Contains("Insurgent", result);
            Assert.Contains("Armored", result);
            Assert.Contains("Tier4", result);
        }

        #endregion
    }
}
