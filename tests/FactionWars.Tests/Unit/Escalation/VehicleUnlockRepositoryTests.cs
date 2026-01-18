using System;
using System.Linq;
using Xunit;
using FactionWars.Escalation.Models;
using FactionWars.Escalation.Interfaces;
using FactionWars.Escalation.Repositories;

namespace FactionWars.Tests.Unit.Escalation
{
    /// <summary>
    /// Tests for the IVehicleUnlockRepository interface and its implementations.
    /// The repository manages the collection of vehicle unlocks tied to escalation tiers.
    /// </summary>
    public class VehicleUnlockRepositoryTests
    {
        private IVehicleUnlockRepository CreateRepository()
        {
            return new InMemoryVehicleUnlockRepository();
        }

        #region Add Tests

        [Fact]
        public void Add_ValidVehicle_ReturnsTrue()
        {
            var repository = CreateRepository();
            var vehicle = new VehicleUnlock(
                "BLISTA",
                "Blista",
                VehicleCategory.Compact,
                EscalationTier.Tier1);

            var result = repository.Add(vehicle);

            Assert.True(result);
        }

        [Fact]
        public void Add_DuplicateVehicle_ReturnsFalse()
        {
            var repository = CreateRepository();
            var vehicle = new VehicleUnlock(
                "BLISTA",
                "Blista",
                VehicleCategory.Compact,
                EscalationTier.Tier1);

            repository.Add(vehicle);
            var result = repository.Add(vehicle);

            Assert.False(result);
        }

        [Fact]
        public void Add_NullVehicle_ThrowsArgumentNullException()
        {
            var repository = CreateRepository();

            Assert.Throws<ArgumentNullException>(() => repository.Add(null!));
        }

        [Fact]
        public void Add_MultipleVehicles_AllAdded()
        {
            var repository = CreateRepository();
            var compact = new VehicleUnlock("BLISTA", "Blista", VehicleCategory.Compact, EscalationTier.Tier1);
            var sedan = new VehicleUnlock("SCHAFTER2", "Schafter", VehicleCategory.Sedan, EscalationTier.Tier2);
            var suv = new VehicleUnlock("CAVALCADE", "Cavalcade", VehicleCategory.SUV, EscalationTier.Tier3);

            Assert.True(repository.Add(compact));
            Assert.True(repository.Add(sedan));
            Assert.True(repository.Add(suv));

            Assert.Equal(3, repository.GetAll().Count());
        }

        #endregion

        #region Remove Tests

        [Fact]
        public void Remove_ExistingVehicle_ReturnsTrue()
        {
            var repository = CreateRepository();
            var vehicle = new VehicleUnlock(
                "BLISTA",
                "Blista",
                VehicleCategory.Compact,
                EscalationTier.Tier1);

            repository.Add(vehicle);
            var result = repository.Remove("BLISTA");

            Assert.True(result);
        }

        [Fact]
        public void Remove_NonExistentVehicle_ReturnsFalse()
        {
            var repository = CreateRepository();

            var result = repository.Remove("NONEXISTENT");

            Assert.False(result);
        }

        [Fact]
        public void Remove_NullVehicleModel_ThrowsArgumentNullException()
        {
            var repository = CreateRepository();

            Assert.Throws<ArgumentNullException>(() => repository.Remove(null!));
        }

        [Fact]
        public void Remove_EmptyVehicleModel_ThrowsArgumentException()
        {
            var repository = CreateRepository();

            Assert.Throws<ArgumentException>(() => repository.Remove(""));
        }

        [Fact]
        public void Remove_AfterRemoval_VehicleNotFound()
        {
            var repository = CreateRepository();
            var vehicle = new VehicleUnlock(
                "BLISTA",
                "Blista",
                VehicleCategory.Compact,
                EscalationTier.Tier1);

            repository.Add(vehicle);
            repository.Remove("BLISTA");
            var result = repository.GetByModel("BLISTA");

            Assert.Null(result);
        }

        #endregion

        #region GetByModel Tests

        [Fact]
        public void GetByModel_ExistingVehicle_ReturnsVehicle()
        {
            var repository = CreateRepository();
            var vehicle = new VehicleUnlock(
                "BLISTA",
                "Blista",
                VehicleCategory.Compact,
                EscalationTier.Tier1);

            repository.Add(vehicle);
            var result = repository.GetByModel("BLISTA");

            Assert.NotNull(result);
            Assert.Equal("BLISTA", result.VehicleModel);
        }

        [Fact]
        public void GetByModel_NonExistentVehicle_ReturnsNull()
        {
            var repository = CreateRepository();

            var result = repository.GetByModel("NONEXISTENT");

            Assert.Null(result);
        }

        [Fact]
        public void GetByModel_NullModel_ThrowsArgumentNullException()
        {
            var repository = CreateRepository();

            Assert.Throws<ArgumentNullException>(() => repository.GetByModel(null!));
        }

        [Fact]
        public void GetByModel_EmptyModel_ThrowsArgumentException()
        {
            var repository = CreateRepository();

            Assert.Throws<ArgumentException>(() => repository.GetByModel(""));
        }

        [Fact]
        public void GetByModel_IsCaseSensitive()
        {
            var repository = CreateRepository();
            var vehicle = new VehicleUnlock(
                "BLISTA",
                "Blista",
                VehicleCategory.Compact,
                EscalationTier.Tier1);

            repository.Add(vehicle);
            var result = repository.GetByModel("blista");

            Assert.Null(result);
        }

        #endregion

        #region GetByTier Tests

        [Fact]
        public void GetByTier_ExistingTier_ReturnsVehiclesForThatTier()
        {
            var repository = CreateRepository();
            repository.Add(new VehicleUnlock("BLISTA", "Blista", VehicleCategory.Compact, EscalationTier.Tier1));
            repository.Add(new VehicleUnlock("ASEA", "Asea", VehicleCategory.Sedan, EscalationTier.Tier1));
            repository.Add(new VehicleUnlock("SCHAFTER2", "Schafter", VehicleCategory.Sedan, EscalationTier.Tier2));

            var result = repository.GetByTier(EscalationTier.Tier1).ToList();

            Assert.Equal(2, result.Count);
            Assert.All(result, v => Assert.Equal(EscalationTier.Tier1, v.RequiredTier));
        }

        [Fact]
        public void GetByTier_NoVehiclesForTier_ReturnsEmpty()
        {
            var repository = CreateRepository();
            repository.Add(new VehicleUnlock("BLISTA", "Blista", VehicleCategory.Compact, EscalationTier.Tier1));

            var result = repository.GetByTier(EscalationTier.Tier5);

            Assert.Empty(result);
        }

        [Fact]
        public void GetByTier_OnlyReturnsExactTierMatch()
        {
            var repository = CreateRepository();
            repository.Add(new VehicleUnlock("BLISTA", "Blista", VehicleCategory.Compact, EscalationTier.Tier1));
            repository.Add(new VehicleUnlock("SCHAFTER2", "Schafter", VehicleCategory.Sedan, EscalationTier.Tier2));
            repository.Add(new VehicleUnlock("CAVALCADE", "Cavalcade", VehicleCategory.SUV, EscalationTier.Tier3));

            var result = repository.GetByTier(EscalationTier.Tier2).ToList();

            Assert.Single(result);
            Assert.Equal("SCHAFTER2", result[0].VehicleModel);
        }

        #endregion

        #region GetByCategory Tests

        [Fact]
        public void GetByCategory_ExistingCategory_ReturnsVehicles()
        {
            var repository = CreateRepository();
            repository.Add(new VehicleUnlock("BLISTA", "Blista", VehicleCategory.Compact, EscalationTier.Tier1));
            repository.Add(new VehicleUnlock("ISSI2", "Issi", VehicleCategory.Compact, EscalationTier.Tier2));
            repository.Add(new VehicleUnlock("SCHAFTER2", "Schafter", VehicleCategory.Sedan, EscalationTier.Tier2));

            var result = repository.GetByCategory(VehicleCategory.Compact).ToList();

            Assert.Equal(2, result.Count);
            Assert.All(result, v => Assert.Equal(VehicleCategory.Compact, v.Category));
        }

        [Fact]
        public void GetByCategory_NoVehiclesInCategory_ReturnsEmpty()
        {
            var repository = CreateRepository();
            repository.Add(new VehicleUnlock("BLISTA", "Blista", VehicleCategory.Compact, EscalationTier.Tier1));

            var result = repository.GetByCategory(VehicleCategory.Military);

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
        public void GetAll_WithVehicles_ReturnsAllVehicles()
        {
            var repository = CreateRepository();
            repository.Add(new VehicleUnlock("BLISTA", "Blista", VehicleCategory.Compact, EscalationTier.Tier1));
            repository.Add(new VehicleUnlock("SCHAFTER2", "Schafter", VehicleCategory.Sedan, EscalationTier.Tier2));
            repository.Add(new VehicleUnlock("CAVALCADE", "Cavalcade", VehicleCategory.SUV, EscalationTier.Tier3));

            var result = repository.GetAll().ToList();

            Assert.Equal(3, result.Count);
        }

        #endregion

        #region GetUnlockedAtTier Tests

        [Fact]
        public void GetUnlockedAtTier_Tier1_ReturnsOnlyTier1Vehicles()
        {
            var repository = CreateRepository();
            repository.Add(new VehicleUnlock("BLISTA", "Blista", VehicleCategory.Compact, EscalationTier.Tier1));
            repository.Add(new VehicleUnlock("SCHAFTER2", "Schafter", VehicleCategory.Sedan, EscalationTier.Tier2));
            repository.Add(new VehicleUnlock("CAVALCADE", "Cavalcade", VehicleCategory.SUV, EscalationTier.Tier3));

            var result = repository.GetUnlockedAtTier(EscalationTier.Tier1).ToList();

            Assert.Single(result);
            Assert.Equal("BLISTA", result[0].VehicleModel);
        }

        [Fact]
        public void GetUnlockedAtTier_Tier3_ReturnsAllVehiclesUpToTier3()
        {
            var repository = CreateRepository();
            repository.Add(new VehicleUnlock("BLISTA", "Blista", VehicleCategory.Compact, EscalationTier.Tier1));
            repository.Add(new VehicleUnlock("SCHAFTER2", "Schafter", VehicleCategory.Sedan, EscalationTier.Tier2));
            repository.Add(new VehicleUnlock("CAVALCADE", "Cavalcade", VehicleCategory.SUV, EscalationTier.Tier3));
            repository.Add(new VehicleUnlock("INSURGENT", "Insurgent", VehicleCategory.Armored, EscalationTier.Tier4));
            repository.Add(new VehicleUnlock("RHINO", "Rhino Tank", VehicleCategory.Military, EscalationTier.Tier5));

            var result = repository.GetUnlockedAtTier(EscalationTier.Tier3).ToList();

            Assert.Equal(3, result.Count);
            Assert.Contains(result, v => v.VehicleModel == "BLISTA");
            Assert.Contains(result, v => v.VehicleModel == "SCHAFTER2");
            Assert.Contains(result, v => v.VehicleModel == "CAVALCADE");
            Assert.DoesNotContain(result, v => v.VehicleModel == "INSURGENT");
            Assert.DoesNotContain(result, v => v.VehicleModel == "RHINO");
        }

        [Fact]
        public void GetUnlockedAtTier_Tier5_ReturnsAllVehicles()
        {
            var repository = CreateRepository();
            repository.Add(new VehicleUnlock("BLISTA", "Blista", VehicleCategory.Compact, EscalationTier.Tier1));
            repository.Add(new VehicleUnlock("SCHAFTER2", "Schafter", VehicleCategory.Sedan, EscalationTier.Tier2));
            repository.Add(new VehicleUnlock("CAVALCADE", "Cavalcade", VehicleCategory.SUV, EscalationTier.Tier3));
            repository.Add(new VehicleUnlock("INSURGENT", "Insurgent", VehicleCategory.Armored, EscalationTier.Tier4));
            repository.Add(new VehicleUnlock("RHINO", "Rhino Tank", VehicleCategory.Military, EscalationTier.Tier5));

            var result = repository.GetUnlockedAtTier(EscalationTier.Tier5).ToList();

            Assert.Equal(5, result.Count);
        }

        #endregion

        #region Exists Tests

        [Fact]
        public void Exists_ExistingVehicle_ReturnsTrue()
        {
            var repository = CreateRepository();
            repository.Add(new VehicleUnlock("BLISTA", "Blista", VehicleCategory.Compact, EscalationTier.Tier1));

            var result = repository.Exists("BLISTA");

            Assert.True(result);
        }

        [Fact]
        public void Exists_NonExistentVehicle_ReturnsFalse()
        {
            var repository = CreateRepository();

            var result = repository.Exists("NONEXISTENT");

            Assert.False(result);
        }

        [Fact]
        public void Exists_NullModel_ThrowsArgumentNullException()
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
        public void Count_WithVehicles_ReturnsCorrectCount()
        {
            var repository = CreateRepository();
            repository.Add(new VehicleUnlock("BLISTA", "Blista", VehicleCategory.Compact, EscalationTier.Tier1));
            repository.Add(new VehicleUnlock("SCHAFTER2", "Schafter", VehicleCategory.Sedan, EscalationTier.Tier2));

            Assert.Equal(2, repository.Count);
        }

        [Fact]
        public void Count_AfterRemoval_DecreasesCount()
        {
            var repository = CreateRepository();
            repository.Add(new VehicleUnlock("BLISTA", "Blista", VehicleCategory.Compact, EscalationTier.Tier1));
            repository.Add(new VehicleUnlock("SCHAFTER2", "Schafter", VehicleCategory.Sedan, EscalationTier.Tier2));
            repository.Remove("BLISTA");

            Assert.Equal(1, repository.Count);
        }

        #endregion

        #region Clear Tests

        [Fact]
        public void Clear_WithVehicles_RemovesAllVehicles()
        {
            var repository = CreateRepository();
            repository.Add(new VehicleUnlock("BLISTA", "Blista", VehicleCategory.Compact, EscalationTier.Tier1));
            repository.Add(new VehicleUnlock("SCHAFTER2", "Schafter", VehicleCategory.Sedan, EscalationTier.Tier2));

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
        public void GetUnlockedAtTierByCategory_ReturnsFilteredVehicles()
        {
            var repository = CreateRepository();
            repository.Add(new VehicleUnlock("BLISTA", "Blista", VehicleCategory.Compact, EscalationTier.Tier1));
            repository.Add(new VehicleUnlock("ISSI2", "Issi", VehicleCategory.Compact, EscalationTier.Tier2));
            repository.Add(new VehicleUnlock("BRIOSO", "Brioso R/A", VehicleCategory.Compact, EscalationTier.Tier3));
            repository.Add(new VehicleUnlock("SCHAFTER2", "Schafter", VehicleCategory.Sedan, EscalationTier.Tier2));

            var result = repository.GetUnlockedAtTierByCategory(EscalationTier.Tier2, VehicleCategory.Compact).ToList();

            Assert.Equal(2, result.Count);
            Assert.Contains(result, v => v.VehicleModel == "BLISTA");
            Assert.Contains(result, v => v.VehicleModel == "ISSI2");
            Assert.DoesNotContain(result, v => v.VehicleModel == "BRIOSO");
            Assert.DoesNotContain(result, v => v.VehicleModel == "SCHAFTER2");
        }

        [Fact]
        public void GetUnlockedAtTierByCategory_NoMatches_ReturnsEmpty()
        {
            var repository = CreateRepository();
            repository.Add(new VehicleUnlock("BLISTA", "Blista", VehicleCategory.Compact, EscalationTier.Tier1));

            var result = repository.GetUnlockedAtTierByCategory(EscalationTier.Tier1, VehicleCategory.Military);

            Assert.Empty(result);
        }

        #endregion
    }
}
