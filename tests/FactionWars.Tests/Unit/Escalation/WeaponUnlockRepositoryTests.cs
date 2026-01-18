using System;
using System.Linq;
using Xunit;
using FactionWars.Escalation.Models;
using FactionWars.Escalation.Interfaces;
using FactionWars.Escalation.Repositories;

namespace FactionWars.Tests.Unit.Escalation
{
    /// <summary>
    /// Tests for the IWeaponUnlockRepository interface and its implementations.
    /// The repository manages the collection of weapon unlocks tied to escalation tiers.
    /// </summary>
    public class WeaponUnlockRepositoryTests
    {
        private IWeaponUnlockRepository CreateRepository()
        {
            return new InMemoryWeaponUnlockRepository();
        }

        #region Add Tests

        [Fact]
        public void Add_ValidWeapon_ReturnsTrue()
        {
            var repository = CreateRepository();
            var weapon = new WeaponUnlock(
                "WEAPON_PISTOL",
                "Pistol",
                WeaponCategory.Pistol,
                EscalationTier.Tier1);

            var result = repository.Add(weapon);

            Assert.True(result);
        }

        [Fact]
        public void Add_DuplicateWeapon_ReturnsFalse()
        {
            var repository = CreateRepository();
            var weapon = new WeaponUnlock(
                "WEAPON_PISTOL",
                "Pistol",
                WeaponCategory.Pistol,
                EscalationTier.Tier1);

            repository.Add(weapon);
            var result = repository.Add(weapon);

            Assert.False(result);
        }

        [Fact]
        public void Add_NullWeapon_ThrowsArgumentNullException()
        {
            var repository = CreateRepository();

            Assert.Throws<ArgumentNullException>(() => repository.Add(null!));
        }

        [Fact]
        public void Add_MultipleWeapons_AllAdded()
        {
            var repository = CreateRepository();
            var pistol = new WeaponUnlock("WEAPON_PISTOL", "Pistol", WeaponCategory.Pistol, EscalationTier.Tier1);
            var smg = new WeaponUnlock("WEAPON_SMG", "SMG", WeaponCategory.SMG, EscalationTier.Tier2);
            var rifle = new WeaponUnlock("WEAPON_CARBINERIFLE", "Carbine Rifle", WeaponCategory.AssaultRifle, EscalationTier.Tier3);

            Assert.True(repository.Add(pistol));
            Assert.True(repository.Add(smg));
            Assert.True(repository.Add(rifle));

            Assert.Equal(3, repository.GetAll().Count());
        }

        #endregion

        #region Remove Tests

        [Fact]
        public void Remove_ExistingWeapon_ReturnsTrue()
        {
            var repository = CreateRepository();
            var weapon = new WeaponUnlock(
                "WEAPON_PISTOL",
                "Pistol",
                WeaponCategory.Pistol,
                EscalationTier.Tier1);

            repository.Add(weapon);
            var result = repository.Remove("WEAPON_PISTOL");

            Assert.True(result);
        }

        [Fact]
        public void Remove_NonExistentWeapon_ReturnsFalse()
        {
            var repository = CreateRepository();

            var result = repository.Remove("WEAPON_NONEXISTENT");

            Assert.False(result);
        }

        [Fact]
        public void Remove_NullWeaponHash_ThrowsArgumentNullException()
        {
            var repository = CreateRepository();

            Assert.Throws<ArgumentNullException>(() => repository.Remove(null!));
        }

        [Fact]
        public void Remove_EmptyWeaponHash_ThrowsArgumentException()
        {
            var repository = CreateRepository();

            Assert.Throws<ArgumentException>(() => repository.Remove(""));
        }

        [Fact]
        public void Remove_AfterRemoval_WeaponNotFound()
        {
            var repository = CreateRepository();
            var weapon = new WeaponUnlock(
                "WEAPON_PISTOL",
                "Pistol",
                WeaponCategory.Pistol,
                EscalationTier.Tier1);

            repository.Add(weapon);
            repository.Remove("WEAPON_PISTOL");
            var result = repository.GetByHash("WEAPON_PISTOL");

            Assert.Null(result);
        }

        #endregion

        #region GetByHash Tests

        [Fact]
        public void GetByHash_ExistingWeapon_ReturnsWeapon()
        {
            var repository = CreateRepository();
            var weapon = new WeaponUnlock(
                "WEAPON_PISTOL",
                "Pistol",
                WeaponCategory.Pistol,
                EscalationTier.Tier1);

            repository.Add(weapon);
            var result = repository.GetByHash("WEAPON_PISTOL");

            Assert.NotNull(result);
            Assert.Equal("WEAPON_PISTOL", result.WeaponHash);
        }

        [Fact]
        public void GetByHash_NonExistentWeapon_ReturnsNull()
        {
            var repository = CreateRepository();

            var result = repository.GetByHash("WEAPON_NONEXISTENT");

            Assert.Null(result);
        }

        [Fact]
        public void GetByHash_NullHash_ThrowsArgumentNullException()
        {
            var repository = CreateRepository();

            Assert.Throws<ArgumentNullException>(() => repository.GetByHash(null!));
        }

        [Fact]
        public void GetByHash_EmptyHash_ThrowsArgumentException()
        {
            var repository = CreateRepository();

            Assert.Throws<ArgumentException>(() => repository.GetByHash(""));
        }

        [Fact]
        public void GetByHash_IsCaseSensitive()
        {
            var repository = CreateRepository();
            var weapon = new WeaponUnlock(
                "WEAPON_PISTOL",
                "Pistol",
                WeaponCategory.Pistol,
                EscalationTier.Tier1);

            repository.Add(weapon);
            var result = repository.GetByHash("weapon_pistol");

            Assert.Null(result);
        }

        #endregion

        #region GetByTier Tests

        [Fact]
        public void GetByTier_ExistingTier_ReturnsWeaponsForThatTier()
        {
            var repository = CreateRepository();
            repository.Add(new WeaponUnlock("WEAPON_PISTOL", "Pistol", WeaponCategory.Pistol, EscalationTier.Tier1));
            repository.Add(new WeaponUnlock("WEAPON_SNSPISTOL", "SNS Pistol", WeaponCategory.Pistol, EscalationTier.Tier1));
            repository.Add(new WeaponUnlock("WEAPON_SMG", "SMG", WeaponCategory.SMG, EscalationTier.Tier2));

            var result = repository.GetByTier(EscalationTier.Tier1).ToList();

            Assert.Equal(2, result.Count);
            Assert.All(result, w => Assert.Equal(EscalationTier.Tier1, w.RequiredTier));
        }

        [Fact]
        public void GetByTier_NoWeaponsForTier_ReturnsEmpty()
        {
            var repository = CreateRepository();
            repository.Add(new WeaponUnlock("WEAPON_PISTOL", "Pistol", WeaponCategory.Pistol, EscalationTier.Tier1));

            var result = repository.GetByTier(EscalationTier.Tier5);

            Assert.Empty(result);
        }

        [Fact]
        public void GetByTier_OnlyReturnsExactTierMatch()
        {
            var repository = CreateRepository();
            repository.Add(new WeaponUnlock("WEAPON_PISTOL", "Pistol", WeaponCategory.Pistol, EscalationTier.Tier1));
            repository.Add(new WeaponUnlock("WEAPON_SMG", "SMG", WeaponCategory.SMG, EscalationTier.Tier2));
            repository.Add(new WeaponUnlock("WEAPON_CARBINERIFLE", "Carbine Rifle", WeaponCategory.AssaultRifle, EscalationTier.Tier3));

            var result = repository.GetByTier(EscalationTier.Tier2).ToList();

            Assert.Single(result);
            Assert.Equal("WEAPON_SMG", result[0].WeaponHash);
        }

        #endregion

        #region GetByCategory Tests

        [Fact]
        public void GetByCategory_ExistingCategory_ReturnsWeapons()
        {
            var repository = CreateRepository();
            repository.Add(new WeaponUnlock("WEAPON_PISTOL", "Pistol", WeaponCategory.Pistol, EscalationTier.Tier1));
            repository.Add(new WeaponUnlock("WEAPON_COMBATPISTOL", "Combat Pistol", WeaponCategory.Pistol, EscalationTier.Tier2));
            repository.Add(new WeaponUnlock("WEAPON_SMG", "SMG", WeaponCategory.SMG, EscalationTier.Tier2));

            var result = repository.GetByCategory(WeaponCategory.Pistol).ToList();

            Assert.Equal(2, result.Count);
            Assert.All(result, w => Assert.Equal(WeaponCategory.Pistol, w.Category));
        }

        [Fact]
        public void GetByCategory_NoWeaponsInCategory_ReturnsEmpty()
        {
            var repository = CreateRepository();
            repository.Add(new WeaponUnlock("WEAPON_PISTOL", "Pistol", WeaponCategory.Pistol, EscalationTier.Tier1));

            var result = repository.GetByCategory(WeaponCategory.Heavy);

            Assert.Empty(result);
        }

        #endregion

        #region GetAll Tests

        [Fact]
        public void GetAll_EmptyRepository_ReturnsEmpty()
        {
            var repository = CreateRepository();

            var result = repository.GetAll();

            Assert.Empty(result);
        }

        [Fact]
        public void GetAll_WithWeapons_ReturnsAllWeapons()
        {
            var repository = CreateRepository();
            repository.Add(new WeaponUnlock("WEAPON_PISTOL", "Pistol", WeaponCategory.Pistol, EscalationTier.Tier1));
            repository.Add(new WeaponUnlock("WEAPON_SMG", "SMG", WeaponCategory.SMG, EscalationTier.Tier2));
            repository.Add(new WeaponUnlock("WEAPON_CARBINERIFLE", "Carbine Rifle", WeaponCategory.AssaultRifle, EscalationTier.Tier3));

            var result = repository.GetAll().ToList();

            Assert.Equal(3, result.Count);
        }

        #endregion

        #region GetUnlockedAtTier Tests

        [Fact]
        public void GetUnlockedAtTier_Tier1_ReturnsOnlyTier1Weapons()
        {
            var repository = CreateRepository();
            repository.Add(new WeaponUnlock("WEAPON_PISTOL", "Pistol", WeaponCategory.Pistol, EscalationTier.Tier1));
            repository.Add(new WeaponUnlock("WEAPON_SMG", "SMG", WeaponCategory.SMG, EscalationTier.Tier2));
            repository.Add(new WeaponUnlock("WEAPON_CARBINERIFLE", "Carbine Rifle", WeaponCategory.AssaultRifle, EscalationTier.Tier3));

            var result = repository.GetUnlockedAtTier(EscalationTier.Tier1).ToList();

            Assert.Single(result);
            Assert.Equal("WEAPON_PISTOL", result[0].WeaponHash);
        }

        [Fact]
        public void GetUnlockedAtTier_Tier3_ReturnsAllWeaponsUpToTier3()
        {
            var repository = CreateRepository();
            repository.Add(new WeaponUnlock("WEAPON_PISTOL", "Pistol", WeaponCategory.Pistol, EscalationTier.Tier1));
            repository.Add(new WeaponUnlock("WEAPON_SMG", "SMG", WeaponCategory.SMG, EscalationTier.Tier2));
            repository.Add(new WeaponUnlock("WEAPON_CARBINERIFLE", "Carbine Rifle", WeaponCategory.AssaultRifle, EscalationTier.Tier3));
            repository.Add(new WeaponUnlock("WEAPON_MG", "Machine Gun", WeaponCategory.LMG, EscalationTier.Tier4));
            repository.Add(new WeaponUnlock("WEAPON_RPG", "RPG", WeaponCategory.Heavy, EscalationTier.Tier5));

            var result = repository.GetUnlockedAtTier(EscalationTier.Tier3).ToList();

            Assert.Equal(3, result.Count);
            Assert.Contains(result, w => w.WeaponHash == "WEAPON_PISTOL");
            Assert.Contains(result, w => w.WeaponHash == "WEAPON_SMG");
            Assert.Contains(result, w => w.WeaponHash == "WEAPON_CARBINERIFLE");
            Assert.DoesNotContain(result, w => w.WeaponHash == "WEAPON_MG");
            Assert.DoesNotContain(result, w => w.WeaponHash == "WEAPON_RPG");
        }

        [Fact]
        public void GetUnlockedAtTier_Tier5_ReturnsAllWeapons()
        {
            var repository = CreateRepository();
            repository.Add(new WeaponUnlock("WEAPON_PISTOL", "Pistol", WeaponCategory.Pistol, EscalationTier.Tier1));
            repository.Add(new WeaponUnlock("WEAPON_SMG", "SMG", WeaponCategory.SMG, EscalationTier.Tier2));
            repository.Add(new WeaponUnlock("WEAPON_CARBINERIFLE", "Carbine Rifle", WeaponCategory.AssaultRifle, EscalationTier.Tier3));
            repository.Add(new WeaponUnlock("WEAPON_MG", "Machine Gun", WeaponCategory.LMG, EscalationTier.Tier4));
            repository.Add(new WeaponUnlock("WEAPON_RPG", "RPG", WeaponCategory.Heavy, EscalationTier.Tier5));

            var result = repository.GetUnlockedAtTier(EscalationTier.Tier5).ToList();

            Assert.Equal(5, result.Count);
        }

        #endregion

        #region Exists Tests

        [Fact]
        public void Exists_ExistingWeapon_ReturnsTrue()
        {
            var repository = CreateRepository();
            repository.Add(new WeaponUnlock("WEAPON_PISTOL", "Pistol", WeaponCategory.Pistol, EscalationTier.Tier1));

            var result = repository.Exists("WEAPON_PISTOL");

            Assert.True(result);
        }

        [Fact]
        public void Exists_NonExistentWeapon_ReturnsFalse()
        {
            var repository = CreateRepository();

            var result = repository.Exists("WEAPON_NONEXISTENT");

            Assert.False(result);
        }

        [Fact]
        public void Exists_NullHash_ThrowsArgumentNullException()
        {
            var repository = CreateRepository();

            Assert.Throws<ArgumentNullException>(() => repository.Exists(null!));
        }

        #endregion

        #region Count Tests

        [Fact]
        public void Count_EmptyRepository_ReturnsZero()
        {
            var repository = CreateRepository();

            Assert.Equal(0, repository.Count);
        }

        [Fact]
        public void Count_WithWeapons_ReturnsCorrectCount()
        {
            var repository = CreateRepository();
            repository.Add(new WeaponUnlock("WEAPON_PISTOL", "Pistol", WeaponCategory.Pistol, EscalationTier.Tier1));
            repository.Add(new WeaponUnlock("WEAPON_SMG", "SMG", WeaponCategory.SMG, EscalationTier.Tier2));

            Assert.Equal(2, repository.Count);
        }

        [Fact]
        public void Count_AfterRemoval_DecreasesCount()
        {
            var repository = CreateRepository();
            repository.Add(new WeaponUnlock("WEAPON_PISTOL", "Pistol", WeaponCategory.Pistol, EscalationTier.Tier1));
            repository.Add(new WeaponUnlock("WEAPON_SMG", "SMG", WeaponCategory.SMG, EscalationTier.Tier2));
            repository.Remove("WEAPON_PISTOL");

            Assert.Equal(1, repository.Count);
        }

        #endregion

        #region Clear Tests

        [Fact]
        public void Clear_WithWeapons_RemovesAllWeapons()
        {
            var repository = CreateRepository();
            repository.Add(new WeaponUnlock("WEAPON_PISTOL", "Pistol", WeaponCategory.Pistol, EscalationTier.Tier1));
            repository.Add(new WeaponUnlock("WEAPON_SMG", "SMG", WeaponCategory.SMG, EscalationTier.Tier2));

            repository.Clear();

            Assert.Equal(0, repository.Count);
            Assert.Empty(repository.GetAll());
        }

        [Fact]
        public void Clear_EmptyRepository_DoesNotThrow()
        {
            var repository = CreateRepository();

            var exception = Record.Exception(() => repository.Clear());

            Assert.Null(exception);
        }

        #endregion

        #region GetUnlockedAtTierByCategory Tests

        [Fact]
        public void GetUnlockedAtTierByCategory_ReturnsFilteredWeapons()
        {
            var repository = CreateRepository();
            repository.Add(new WeaponUnlock("WEAPON_PISTOL", "Pistol", WeaponCategory.Pistol, EscalationTier.Tier1));
            repository.Add(new WeaponUnlock("WEAPON_COMBATPISTOL", "Combat Pistol", WeaponCategory.Pistol, EscalationTier.Tier2));
            repository.Add(new WeaponUnlock("WEAPON_HEAVYPISTOL", "Heavy Pistol", WeaponCategory.Pistol, EscalationTier.Tier3));
            repository.Add(new WeaponUnlock("WEAPON_SMG", "SMG", WeaponCategory.SMG, EscalationTier.Tier2));

            var result = repository.GetUnlockedAtTierByCategory(EscalationTier.Tier2, WeaponCategory.Pistol).ToList();

            Assert.Equal(2, result.Count);
            Assert.Contains(result, w => w.WeaponHash == "WEAPON_PISTOL");
            Assert.Contains(result, w => w.WeaponHash == "WEAPON_COMBATPISTOL");
            Assert.DoesNotContain(result, w => w.WeaponHash == "WEAPON_HEAVYPISTOL");
            Assert.DoesNotContain(result, w => w.WeaponHash == "WEAPON_SMG");
        }

        [Fact]
        public void GetUnlockedAtTierByCategory_NoMatches_ReturnsEmpty()
        {
            var repository = CreateRepository();
            repository.Add(new WeaponUnlock("WEAPON_PISTOL", "Pistol", WeaponCategory.Pistol, EscalationTier.Tier1));

            var result = repository.GetUnlockedAtTierByCategory(EscalationTier.Tier1, WeaponCategory.Heavy);

            Assert.Empty(result);
        }

        #endregion
    }
}
