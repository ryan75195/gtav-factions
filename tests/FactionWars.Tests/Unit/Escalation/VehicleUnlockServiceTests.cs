using System;
using System.Collections.Generic;
using System.Linq;
using Moq;
using Xunit;
using FactionWars.Escalation.Models;
using FactionWars.Escalation.Interfaces;
using FactionWars.Escalation.Services;

namespace FactionWars.Tests.Unit.Escalation
{
    /// <summary>
    /// Tests for the IVehicleUnlockService interface and its implementations.
    /// The service provides business logic for vehicle unlocks based on escalation tiers.
    /// </summary>
    public class VehicleUnlockServiceTests
    {
        private readonly Mock<IVehicleUnlockRepository> _mockRepository;
        private readonly Mock<IEscalationService> _mockEscalationService;
        private readonly IVehicleUnlockService _service;

        public VehicleUnlockServiceTests()
        {
            _mockRepository = new Mock<IVehicleUnlockRepository>();
            _mockEscalationService = new Mock<IEscalationService>();
            _service = new VehicleUnlockService(_mockRepository.Object, _mockEscalationService.Object);
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithNullRepository_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new VehicleUnlockService(null!, _mockEscalationService.Object));
        }

        [Fact]
        public void Constructor_WithNullEscalationService_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new VehicleUnlockService(_mockRepository.Object, null!));
        }

        [Fact]
        public void Constructor_WithValidDependencies_CreatesService()
        {
            var service = new VehicleUnlockService(_mockRepository.Object, _mockEscalationService.Object);

            Assert.NotNull(service);
        }

        #endregion

        #region GetAvailableVehicles Tests

        [Fact]
        public void GetAvailableVehicles_ForFaction_ReturnsVehiclesUpToCurrentTier()
        {
            var vehicles = new List<VehicleUnlock>
            {
                new VehicleUnlock("BLISTA", "Blista", VehicleCategory.Compact, EscalationTier.Tier1),
                new VehicleUnlock("SCHAFTER2", "Schafter", VehicleCategory.Sedan, EscalationTier.Tier2)
            };
            _mockEscalationService.Setup(x => x.GetCurrentTier("faction1")).Returns(EscalationTier.Tier2);
            _mockRepository.Setup(x => x.GetUnlockedAtTier(EscalationTier.Tier2)).Returns(vehicles);

            var result = _service.GetAvailableVehicles("faction1").ToList();

            Assert.Equal(2, result.Count);
        }

        [Fact]
        public void GetAvailableVehicles_WithNullFactionId_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _service.GetAvailableVehicles(null!));
        }

        [Fact]
        public void GetAvailableVehicles_WithEmptyFactionId_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => _service.GetAvailableVehicles(""));
        }

        [Fact]
        public void GetAvailableVehicles_Tier1Faction_ReturnsOnlyTier1Vehicles()
        {
            var tier1Vehicles = new List<VehicleUnlock>
            {
                new VehicleUnlock("BLISTA", "Blista", VehicleCategory.Compact, EscalationTier.Tier1)
            };
            _mockEscalationService.Setup(x => x.GetCurrentTier("faction1")).Returns(EscalationTier.Tier1);
            _mockRepository.Setup(x => x.GetUnlockedAtTier(EscalationTier.Tier1)).Returns(tier1Vehicles);

            var result = _service.GetAvailableVehicles("faction1").ToList();

            Assert.Single(result);
            Assert.Equal("BLISTA", result[0].VehicleModel);
        }

        #endregion

        #region GetAvailableVehiclesByCategory Tests

        [Fact]
        public void GetAvailableVehiclesByCategory_ReturnsFilteredVehicles()
        {
            var vehicles = new List<VehicleUnlock>
            {
                new VehicleUnlock("BLISTA", "Blista", VehicleCategory.Compact, EscalationTier.Tier1),
                new VehicleUnlock("ISSI2", "Issi", VehicleCategory.Compact, EscalationTier.Tier2)
            };
            _mockEscalationService.Setup(x => x.GetCurrentTier("faction1")).Returns(EscalationTier.Tier2);
            _mockRepository.Setup(x => x.GetUnlockedAtTierByCategory(EscalationTier.Tier2, VehicleCategory.Compact))
                .Returns(vehicles);

            var result = _service.GetAvailableVehiclesByCategory("faction1", VehicleCategory.Compact).ToList();

            Assert.Equal(2, result.Count);
            _mockRepository.Verify(x => x.GetUnlockedAtTierByCategory(EscalationTier.Tier2, VehicleCategory.Compact), Times.Once);
        }

        [Fact]
        public void GetAvailableVehiclesByCategory_WithNullFactionId_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                _service.GetAvailableVehiclesByCategory(null!, VehicleCategory.Compact));
        }

        #endregion

        #region IsVehicleAvailable Tests

        [Fact]
        public void IsVehicleAvailable_VehicleUnlocked_ReturnsTrue()
        {
            var vehicle = new VehicleUnlock("SCHAFTER2", "Schafter", VehicleCategory.Sedan, EscalationTier.Tier2);
            _mockEscalationService.Setup(x => x.GetCurrentTier("faction1")).Returns(EscalationTier.Tier3);
            _mockRepository.Setup(x => x.GetByModel("SCHAFTER2")).Returns(vehicle);

            var result = _service.IsVehicleAvailable("faction1", "SCHAFTER2");

            Assert.True(result);
        }

        [Fact]
        public void IsVehicleAvailable_VehicleNotUnlocked_ReturnsFalse()
        {
            var vehicle = new VehicleUnlock("RHINO", "Rhino Tank", VehicleCategory.Military, EscalationTier.Tier5);
            _mockEscalationService.Setup(x => x.GetCurrentTier("faction1")).Returns(EscalationTier.Tier2);
            _mockRepository.Setup(x => x.GetByModel("RHINO")).Returns(vehicle);

            var result = _service.IsVehicleAvailable("faction1", "RHINO");

            Assert.False(result);
        }

        [Fact]
        public void IsVehicleAvailable_VehicleNotFound_ReturnsFalse()
        {
            _mockRepository.Setup(x => x.GetByModel("NONEXISTENT")).Returns((VehicleUnlock?)null);

            var result = _service.IsVehicleAvailable("faction1", "NONEXISTENT");

            Assert.False(result);
        }

        [Fact]
        public void IsVehicleAvailable_WithNullFactionId_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                _service.IsVehicleAvailable(null!, "BLISTA"));
        }

        [Fact]
        public void IsVehicleAvailable_WithNullVehicleModel_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                _service.IsVehicleAvailable("faction1", null!));
        }

        [Fact]
        public void IsVehicleAvailable_AtExactTier_ReturnsTrue()
        {
            var vehicle = new VehicleUnlock("SCHAFTER2", "Schafter", VehicleCategory.Sedan, EscalationTier.Tier2);
            _mockEscalationService.Setup(x => x.GetCurrentTier("faction1")).Returns(EscalationTier.Tier2);
            _mockRepository.Setup(x => x.GetByModel("SCHAFTER2")).Returns(vehicle);

            var result = _service.IsVehicleAvailable("faction1", "SCHAFTER2");

            Assert.True(result);
        }

        #endregion

        #region GetNewlyUnlockedVehicles Tests

        [Fact]
        public void GetNewlyUnlockedVehicles_TierChanged_ReturnsNewVehicles()
        {
            var tier3Vehicles = new List<VehicleUnlock>
            {
                new VehicleUnlock("CAVALCADE", "Cavalcade", VehicleCategory.SUV, EscalationTier.Tier3),
                new VehicleUnlock("GRANGER", "Granger", VehicleCategory.SUV, EscalationTier.Tier3)
            };
            _mockRepository.Setup(x => x.GetByTier(EscalationTier.Tier3)).Returns(tier3Vehicles);

            var result = _service.GetNewlyUnlockedVehicles(EscalationTier.Tier2, EscalationTier.Tier3).ToList();

            Assert.Equal(2, result.Count);
            Assert.All(result, v => Assert.Equal(EscalationTier.Tier3, v.RequiredTier));
        }

        [Fact]
        public void GetNewlyUnlockedVehicles_SameTier_ReturnsEmpty()
        {
            var result = _service.GetNewlyUnlockedVehicles(EscalationTier.Tier2, EscalationTier.Tier2);

            Assert.Empty(result);
            _mockRepository.Verify(x => x.GetByTier(It.IsAny<EscalationTier>()), Times.Never);
        }

        [Fact]
        public void GetNewlyUnlockedVehicles_TierDecreased_ReturnsEmpty()
        {
            var result = _service.GetNewlyUnlockedVehicles(EscalationTier.Tier3, EscalationTier.Tier2);

            Assert.Empty(result);
            _mockRepository.Verify(x => x.GetByTier(It.IsAny<EscalationTier>()), Times.Never);
        }

        [Fact]
        public void GetNewlyUnlockedVehicles_JumpMultipleTiers_ReturnsAllNewVehicles()
        {
            var tier2Vehicles = new List<VehicleUnlock>
            {
                new VehicleUnlock("SCHAFTER2", "Schafter", VehicleCategory.Sedan, EscalationTier.Tier2)
            };
            var tier3Vehicles = new List<VehicleUnlock>
            {
                new VehicleUnlock("CAVALCADE", "Cavalcade", VehicleCategory.SUV, EscalationTier.Tier3)
            };
            var tier4Vehicles = new List<VehicleUnlock>
            {
                new VehicleUnlock("INSURGENT", "Insurgent", VehicleCategory.Armored, EscalationTier.Tier4)
            };

            _mockRepository.Setup(x => x.GetByTier(EscalationTier.Tier2)).Returns(tier2Vehicles);
            _mockRepository.Setup(x => x.GetByTier(EscalationTier.Tier3)).Returns(tier3Vehicles);
            _mockRepository.Setup(x => x.GetByTier(EscalationTier.Tier4)).Returns(tier4Vehicles);

            var result = _service.GetNewlyUnlockedVehicles(EscalationTier.Tier1, EscalationTier.Tier4).ToList();

            Assert.Equal(3, result.Count);
            Assert.Contains(result, v => v.VehicleModel == "SCHAFTER2");
            Assert.Contains(result, v => v.VehicleModel == "CAVALCADE");
            Assert.Contains(result, v => v.VehicleModel == "INSURGENT");
        }

        #endregion

        #region GetRandomVehicleForTier Tests

        [Fact]
        public void GetRandomVehicleForTier_WithAvailableVehicles_ReturnsVehicle()
        {
            var vehicles = new List<VehicleUnlock>
            {
                new VehicleUnlock("BLISTA", "Blista", VehicleCategory.Compact, EscalationTier.Tier1),
                new VehicleUnlock("ASEA", "Asea", VehicleCategory.Sedan, EscalationTier.Tier1)
            };
            _mockRepository.Setup(x => x.GetUnlockedAtTier(EscalationTier.Tier1)).Returns(vehicles);

            var result = _service.GetRandomVehicleForTier(EscalationTier.Tier1);

            Assert.NotNull(result);
            Assert.Contains(result.VehicleModel, new[] { "BLISTA", "ASEA" });
        }

        [Fact]
        public void GetRandomVehicleForTier_NoVehiclesAvailable_ReturnsNull()
        {
            _mockRepository.Setup(x => x.GetUnlockedAtTier(EscalationTier.Tier5))
                .Returns(Enumerable.Empty<VehicleUnlock>());

            var result = _service.GetRandomVehicleForTier(EscalationTier.Tier5);

            Assert.Null(result);
        }

        #endregion

        #region GetRandomVehicleForFaction Tests

        [Fact]
        public void GetRandomVehicleForFaction_ReturnsVehicleBasedOnFactionTier()
        {
            var vehicles = new List<VehicleUnlock>
            {
                new VehicleUnlock("BLISTA", "Blista", VehicleCategory.Compact, EscalationTier.Tier1),
                new VehicleUnlock("SCHAFTER2", "Schafter", VehicleCategory.Sedan, EscalationTier.Tier2)
            };
            _mockEscalationService.Setup(x => x.GetCurrentTier("faction1")).Returns(EscalationTier.Tier2);
            _mockRepository.Setup(x => x.GetUnlockedAtTier(EscalationTier.Tier2)).Returns(vehicles);

            var result = _service.GetRandomVehicleForFaction("faction1");

            Assert.NotNull(result);
            _mockEscalationService.Verify(x => x.GetCurrentTier("faction1"), Times.Once);
        }

        [Fact]
        public void GetRandomVehicleForFaction_WithNullFactionId_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _service.GetRandomVehicleForFaction(null!));
        }

        #endregion

        #region GetRandomVehicleForFactionByCategory Tests

        [Fact]
        public void GetRandomVehicleForFactionByCategory_ReturnsFilteredVehicle()
        {
            var vehicles = new List<VehicleUnlock>
            {
                new VehicleUnlock("BLISTA", "Blista", VehicleCategory.Compact, EscalationTier.Tier1),
                new VehicleUnlock("ISSI2", "Issi", VehicleCategory.Compact, EscalationTier.Tier2)
            };
            _mockEscalationService.Setup(x => x.GetCurrentTier("faction1")).Returns(EscalationTier.Tier2);
            _mockRepository.Setup(x => x.GetUnlockedAtTierByCategory(EscalationTier.Tier2, VehicleCategory.Compact))
                .Returns(vehicles);

            var result = _service.GetRandomVehicleForFactionByCategory("faction1", VehicleCategory.Compact);

            Assert.NotNull(result);
            Assert.Equal(VehicleCategory.Compact, result!.Category);
        }

        [Fact]
        public void GetRandomVehicleForFactionByCategory_NoMatchingVehicles_ReturnsNull()
        {
            _mockEscalationService.Setup(x => x.GetCurrentTier("faction1")).Returns(EscalationTier.Tier1);
            _mockRepository.Setup(x => x.GetUnlockedAtTierByCategory(EscalationTier.Tier1, VehicleCategory.Military))
                .Returns(Enumerable.Empty<VehicleUnlock>());

            var result = _service.GetRandomVehicleForFactionByCategory("faction1", VehicleCategory.Military);

            Assert.Null(result);
        }

        #endregion

        #region RegisterVehicle Tests

        [Fact]
        public void RegisterVehicle_ValidVehicle_ReturnsTrue()
        {
            var vehicle = new VehicleUnlock("TEST", "Test", VehicleCategory.Compact, EscalationTier.Tier1);
            _mockRepository.Setup(x => x.Add(vehicle)).Returns(true);

            var result = _service.RegisterVehicle(vehicle);

            Assert.True(result);
            _mockRepository.Verify(x => x.Add(vehicle), Times.Once);
        }

        [Fact]
        public void RegisterVehicle_DuplicateVehicle_ReturnsFalse()
        {
            var vehicle = new VehicleUnlock("TEST", "Test", VehicleCategory.Compact, EscalationTier.Tier1);
            _mockRepository.Setup(x => x.Add(vehicle)).Returns(false);

            var result = _service.RegisterVehicle(vehicle);

            Assert.False(result);
        }

        [Fact]
        public void RegisterVehicle_NullVehicle_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _service.RegisterVehicle(null!));
        }

        #endregion

        #region UnregisterVehicle Tests

        [Fact]
        public void UnregisterVehicle_ExistingVehicle_ReturnsTrue()
        {
            _mockRepository.Setup(x => x.Remove("TEST")).Returns(true);

            var result = _service.UnregisterVehicle("TEST");

            Assert.True(result);
            _mockRepository.Verify(x => x.Remove("TEST"), Times.Once);
        }

        [Fact]
        public void UnregisterVehicle_NonExistentVehicle_ReturnsFalse()
        {
            _mockRepository.Setup(x => x.Remove("NONEXISTENT")).Returns(false);

            var result = _service.UnregisterVehicle("NONEXISTENT");

            Assert.False(result);
        }

        [Fact]
        public void UnregisterVehicle_NullModel_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _service.UnregisterVehicle(null!));
        }

        #endregion

        #region GetAllRegisteredVehicles Tests

        [Fact]
        public void GetAllRegisteredVehicles_ReturnsAllVehicles()
        {
            var vehicles = new List<VehicleUnlock>
            {
                new VehicleUnlock("BLISTA", "Blista", VehicleCategory.Compact, EscalationTier.Tier1),
                new VehicleUnlock("SCHAFTER2", "Schafter", VehicleCategory.Sedan, EscalationTier.Tier2)
            };
            _mockRepository.Setup(x => x.GetAll()).Returns(vehicles);

            var result = _service.GetAllRegisteredVehicles().ToList();

            Assert.Equal(2, result.Count);
            _mockRepository.Verify(x => x.GetAll(), Times.Once);
        }

        #endregion

        #region GetVehicleInfo Tests

        [Fact]
        public void GetVehicleInfo_ExistingVehicle_ReturnsVehicle()
        {
            var vehicle = new VehicleUnlock("BLISTA", "Blista", VehicleCategory.Compact, EscalationTier.Tier1);
            _mockRepository.Setup(x => x.GetByModel("BLISTA")).Returns(vehicle);

            var result = _service.GetVehicleInfo("BLISTA");

            Assert.NotNull(result);
            Assert.Equal("BLISTA", result!.VehicleModel);
        }

        [Fact]
        public void GetVehicleInfo_NonExistentVehicle_ReturnsNull()
        {
            _mockRepository.Setup(x => x.GetByModel("NONEXISTENT")).Returns((VehicleUnlock?)null);

            var result = _service.GetVehicleInfo("NONEXISTENT");

            Assert.Null(result);
        }

        [Fact]
        public void GetVehicleInfo_NullModel_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _service.GetVehicleInfo(null!));
        }

        #endregion

        #region GetVehiclesNewlyUnlockedForFaction Tests

        [Fact]
        public void GetVehiclesNewlyUnlockedForFaction_ReturnsVehiclesBasedOnPreviousTier()
        {
            // Start at Tier2 (2000 points), then add points to reach Tier3
            var escalation = new FactionEscalation("faction1", 2000);
            escalation.AddPoints(1500); // Now at 3500 points (Tier3), PreviousTier = Tier2
            _mockEscalationService.Setup(x => x.GetEscalation("faction1")).Returns(escalation);

            var tier3Vehicles = new List<VehicleUnlock>
            {
                new VehicleUnlock("CAVALCADE", "Cavalcade", VehicleCategory.SUV, EscalationTier.Tier3)
            };
            _mockRepository.Setup(x => x.GetByTier(EscalationTier.Tier3)).Returns(tier3Vehicles);

            var result = _service.GetVehiclesNewlyUnlockedForFaction("faction1").ToList();

            Assert.Single(result);
            Assert.Equal("CAVALCADE", result[0].VehicleModel);
        }

        [Fact]
        public void GetVehiclesNewlyUnlockedForFaction_FactionNotFound_ReturnsEmpty()
        {
            _mockEscalationService.Setup(x => x.GetEscalation("nonexistent")).Returns((FactionEscalation?)null);

            var result = _service.GetVehiclesNewlyUnlockedForFaction("nonexistent");

            Assert.Empty(result);
        }

        [Fact]
        public void GetVehiclesNewlyUnlockedForFaction_NullFactionId_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _service.GetVehiclesNewlyUnlockedForFaction(null!));
        }

        #endregion
    }
}
