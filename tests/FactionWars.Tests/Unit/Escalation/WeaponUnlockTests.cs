using System;
using Xunit;
using FactionWars.Escalation.Models;

namespace FactionWars.Tests.Unit.Escalation
{
    /// <summary>
    /// Tests for the WeaponUnlock model which represents a weapon that can be unlocked
    /// at a specific escalation tier.
    /// </summary>
    public class WeaponUnlockTests
    {
        #region Constructor Tests

        [Fact]
        public void Constructor_WithValidParameters_CreatesWeaponUnlock()
        {
            var weaponUnlock = new WeaponUnlock(
                "WEAPON_PISTOL",
                "Pistol",
                WeaponCategory.Pistol,
                EscalationTier.Tier1);

            Assert.NotNull(weaponUnlock);
            Assert.Equal("WEAPON_PISTOL", weaponUnlock.WeaponHash);
            Assert.Equal("Pistol", weaponUnlock.DisplayName);
            Assert.Equal(WeaponCategory.Pistol, weaponUnlock.Category);
            Assert.Equal(EscalationTier.Tier1, weaponUnlock.RequiredTier);
        }

        [Fact]
        public void Constructor_WithAllParameters_SetsAllProperties()
        {
            var weaponUnlock = new WeaponUnlock(
                "WEAPON_CARBINERIFLE",
                "Carbine Rifle",
                WeaponCategory.AssaultRifle,
                EscalationTier.Tier3,
                "A versatile assault rifle",
                100);

            Assert.Equal("WEAPON_CARBINERIFLE", weaponUnlock.WeaponHash);
            Assert.Equal("Carbine Rifle", weaponUnlock.DisplayName);
            Assert.Equal(WeaponCategory.AssaultRifle, weaponUnlock.Category);
            Assert.Equal(EscalationTier.Tier3, weaponUnlock.RequiredTier);
            Assert.Equal("A versatile assault rifle", weaponUnlock.Description);
            Assert.Equal(100, weaponUnlock.AmmoAmount);
        }

        [Fact]
        public void Constructor_WithNullWeaponHash_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new WeaponUnlock(
                null!,
                "Pistol",
                WeaponCategory.Pistol,
                EscalationTier.Tier1));
        }

        [Fact]
        public void Constructor_WithEmptyWeaponHash_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new WeaponUnlock(
                "",
                "Pistol",
                WeaponCategory.Pistol,
                EscalationTier.Tier1));
        }

        [Fact]
        public void Constructor_WithWhitespaceWeaponHash_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new WeaponUnlock(
                "   ",
                "Pistol",
                WeaponCategory.Pistol,
                EscalationTier.Tier1));
        }

        [Fact]
        public void Constructor_WithNullDisplayName_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new WeaponUnlock(
                "WEAPON_PISTOL",
                null!,
                WeaponCategory.Pistol,
                EscalationTier.Tier1));
        }

        [Fact]
        public void Constructor_WithEmptyDisplayName_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new WeaponUnlock(
                "WEAPON_PISTOL",
                "",
                WeaponCategory.Pistol,
                EscalationTier.Tier1));
        }

        [Fact]
        public void Constructor_WithWhitespaceDisplayName_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new WeaponUnlock(
                "WEAPON_PISTOL",
                "   ",
                WeaponCategory.Pistol,
                EscalationTier.Tier1));
        }

        [Fact]
        public void Constructor_WithDefaultDescription_SetsDescriptionToNull()
        {
            var weaponUnlock = new WeaponUnlock(
                "WEAPON_PISTOL",
                "Pistol",
                WeaponCategory.Pistol,
                EscalationTier.Tier1);

            Assert.Null(weaponUnlock.Description);
        }

        [Fact]
        public void Constructor_WithDefaultAmmo_SetsAmmoToDefaultValue()
        {
            var weaponUnlock = new WeaponUnlock(
                "WEAPON_PISTOL",
                "Pistol",
                WeaponCategory.Pistol,
                EscalationTier.Tier1);

            Assert.Equal(WeaponUnlock.DefaultAmmoAmount, weaponUnlock.AmmoAmount);
        }

        [Fact]
        public void Constructor_WithNegativeAmmo_ClampsToZero()
        {
            var weaponUnlock = new WeaponUnlock(
                "WEAPON_PISTOL",
                "Pistol",
                WeaponCategory.Pistol,
                EscalationTier.Tier1,
                null,
                -50);

            Assert.Equal(0, weaponUnlock.AmmoAmount);
        }

        [Fact]
        public void Constructor_WithZeroAmmo_AllowsZeroAmmo()
        {
            var weaponUnlock = new WeaponUnlock(
                "WEAPON_PISTOL",
                "Pistol",
                WeaponCategory.Pistol,
                EscalationTier.Tier1,
                null,
                0);

            Assert.Equal(0, weaponUnlock.AmmoAmount);
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
            var weaponUnlock = new WeaponUnlock(
                "WEAPON_TEST",
                "Test Weapon",
                WeaponCategory.Pistol,
                tier);

            Assert.Equal(tier, weaponUnlock.RequiredTier);
        }

        [Theory]
        [InlineData(WeaponCategory.Pistol)]
        [InlineData(WeaponCategory.SMG)]
        [InlineData(WeaponCategory.Shotgun)]
        [InlineData(WeaponCategory.AssaultRifle)]
        [InlineData(WeaponCategory.LMG)]
        [InlineData(WeaponCategory.Sniper)]
        [InlineData(WeaponCategory.Heavy)]
        [InlineData(WeaponCategory.Melee)]
        [InlineData(WeaponCategory.Thrown)]
        public void Category_CanBeAnyCategory(WeaponCategory category)
        {
            var weaponUnlock = new WeaponUnlock(
                "WEAPON_TEST",
                "Test Weapon",
                category,
                EscalationTier.Tier1);

            Assert.Equal(category, weaponUnlock.Category);
        }

        #endregion

        #region IsUnlockedAtTier Tests

        [Fact]
        public void IsUnlockedAtTier_AtExactTier_ReturnsTrue()
        {
            var weaponUnlock = new WeaponUnlock(
                "WEAPON_SMG",
                "SMG",
                WeaponCategory.SMG,
                EscalationTier.Tier2);

            Assert.True(weaponUnlock.IsUnlockedAtTier(EscalationTier.Tier2));
        }

        [Fact]
        public void IsUnlockedAtTier_AboveRequiredTier_ReturnsTrue()
        {
            var weaponUnlock = new WeaponUnlock(
                "WEAPON_SMG",
                "SMG",
                WeaponCategory.SMG,
                EscalationTier.Tier2);

            Assert.True(weaponUnlock.IsUnlockedAtTier(EscalationTier.Tier3));
            Assert.True(weaponUnlock.IsUnlockedAtTier(EscalationTier.Tier4));
            Assert.True(weaponUnlock.IsUnlockedAtTier(EscalationTier.Tier5));
        }

        [Fact]
        public void IsUnlockedAtTier_BelowRequiredTier_ReturnsFalse()
        {
            var weaponUnlock = new WeaponUnlock(
                "WEAPON_CARBINERIFLE",
                "Carbine Rifle",
                WeaponCategory.AssaultRifle,
                EscalationTier.Tier3);

            Assert.False(weaponUnlock.IsUnlockedAtTier(EscalationTier.Tier1));
            Assert.False(weaponUnlock.IsUnlockedAtTier(EscalationTier.Tier2));
        }

        [Fact]
        public void IsUnlockedAtTier_Tier1Weapon_UnlockedAtAllTiers()
        {
            var weaponUnlock = new WeaponUnlock(
                "WEAPON_PISTOL",
                "Pistol",
                WeaponCategory.Pistol,
                EscalationTier.Tier1);

            Assert.True(weaponUnlock.IsUnlockedAtTier(EscalationTier.Tier1));
            Assert.True(weaponUnlock.IsUnlockedAtTier(EscalationTier.Tier2));
            Assert.True(weaponUnlock.IsUnlockedAtTier(EscalationTier.Tier3));
            Assert.True(weaponUnlock.IsUnlockedAtTier(EscalationTier.Tier4));
            Assert.True(weaponUnlock.IsUnlockedAtTier(EscalationTier.Tier5));
        }

        [Fact]
        public void IsUnlockedAtTier_Tier5Weapon_OnlyUnlockedAtTier5()
        {
            var weaponUnlock = new WeaponUnlock(
                "WEAPON_RPG",
                "RPG",
                WeaponCategory.Heavy,
                EscalationTier.Tier5);

            Assert.False(weaponUnlock.IsUnlockedAtTier(EscalationTier.Tier1));
            Assert.False(weaponUnlock.IsUnlockedAtTier(EscalationTier.Tier2));
            Assert.False(weaponUnlock.IsUnlockedAtTier(EscalationTier.Tier3));
            Assert.False(weaponUnlock.IsUnlockedAtTier(EscalationTier.Tier4));
            Assert.True(weaponUnlock.IsUnlockedAtTier(EscalationTier.Tier5));
        }

        #endregion

        #region Equality Tests

        [Fact]
        public void Equals_SameWeaponHash_ReturnsTrue()
        {
            var weapon1 = new WeaponUnlock(
                "WEAPON_PISTOL",
                "Pistol",
                WeaponCategory.Pistol,
                EscalationTier.Tier1);

            var weapon2 = new WeaponUnlock(
                "WEAPON_PISTOL",
                "Different Name",
                WeaponCategory.SMG,
                EscalationTier.Tier5);

            Assert.True(weapon1.Equals(weapon2));
        }

        [Fact]
        public void Equals_DifferentWeaponHash_ReturnsFalse()
        {
            var weapon1 = new WeaponUnlock(
                "WEAPON_PISTOL",
                "Pistol",
                WeaponCategory.Pistol,
                EscalationTier.Tier1);

            var weapon2 = new WeaponUnlock(
                "WEAPON_SMG",
                "SMG",
                WeaponCategory.SMG,
                EscalationTier.Tier2);

            Assert.False(weapon1.Equals(weapon2));
        }

        [Fact]
        public void Equals_NullObject_ReturnsFalse()
        {
            var weapon = new WeaponUnlock(
                "WEAPON_PISTOL",
                "Pistol",
                WeaponCategory.Pistol,
                EscalationTier.Tier1);

            Assert.False(weapon.Equals(null));
        }

        [Fact]
        public void EqualsObject_SameWeaponHash_ReturnsTrue()
        {
            var weapon1 = new WeaponUnlock(
                "WEAPON_PISTOL",
                "Pistol",
                WeaponCategory.Pistol,
                EscalationTier.Tier1);

            object weapon2 = new WeaponUnlock(
                "WEAPON_PISTOL",
                "Pistol",
                WeaponCategory.Pistol,
                EscalationTier.Tier1);

            Assert.True(weapon1.Equals(weapon2));
        }

        [Fact]
        public void EqualsObject_DifferentType_ReturnsFalse()
        {
            var weapon = new WeaponUnlock(
                "WEAPON_PISTOL",
                "Pistol",
                WeaponCategory.Pistol,
                EscalationTier.Tier1);

            Assert.False(weapon.Equals("WEAPON_PISTOL"));
        }

        [Fact]
        public void GetHashCode_SameWeaponHash_ReturnsSameHashCode()
        {
            var weapon1 = new WeaponUnlock(
                "WEAPON_PISTOL",
                "Pistol",
                WeaponCategory.Pistol,
                EscalationTier.Tier1);

            var weapon2 = new WeaponUnlock(
                "WEAPON_PISTOL",
                "Different Name",
                WeaponCategory.SMG,
                EscalationTier.Tier5);

            Assert.Equal(weapon1.GetHashCode(), weapon2.GetHashCode());
        }

        [Fact]
        public void OperatorEquals_SameWeaponHash_ReturnsTrue()
        {
            var weapon1 = new WeaponUnlock(
                "WEAPON_PISTOL",
                "Pistol",
                WeaponCategory.Pistol,
                EscalationTier.Tier1);

            var weapon2 = new WeaponUnlock(
                "WEAPON_PISTOL",
                "Pistol",
                WeaponCategory.Pistol,
                EscalationTier.Tier1);

            Assert.True(weapon1 == weapon2);
        }

        [Fact]
        public void OperatorNotEquals_DifferentWeaponHash_ReturnsTrue()
        {
            var weapon1 = new WeaponUnlock(
                "WEAPON_PISTOL",
                "Pistol",
                WeaponCategory.Pistol,
                EscalationTier.Tier1);

            var weapon2 = new WeaponUnlock(
                "WEAPON_SMG",
                "SMG",
                WeaponCategory.SMG,
                EscalationTier.Tier2);

            Assert.True(weapon1 != weapon2);
        }

        [Fact]
        public void OperatorEquals_BothNull_ReturnsTrue()
        {
            WeaponUnlock? weapon1 = null;
            WeaponUnlock? weapon2 = null;

            Assert.True(weapon1 == weapon2);
        }

        [Fact]
        public void OperatorEquals_OneNull_ReturnsFalse()
        {
            var weapon1 = new WeaponUnlock(
                "WEAPON_PISTOL",
                "Pistol",
                WeaponCategory.Pistol,
                EscalationTier.Tier1);
            WeaponUnlock? weapon2 = null;

            Assert.False(weapon1 == weapon2);
            Assert.True(weapon1 != weapon2);
        }

        #endregion

        #region ToString Tests

        [Fact]
        public void ToString_ReturnsFormattedString()
        {
            var weapon = new WeaponUnlock(
                "WEAPON_CARBINERIFLE",
                "Carbine Rifle",
                WeaponCategory.AssaultRifle,
                EscalationTier.Tier3);

            var result = weapon.ToString();

            Assert.Contains("Carbine Rifle", result);
            Assert.Contains("AssaultRifle", result);
            Assert.Contains("Tier3", result);
        }

        #endregion
    }
}
