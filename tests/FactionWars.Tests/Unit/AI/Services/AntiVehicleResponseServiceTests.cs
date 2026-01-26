using FactionWars.AI.Interfaces;
using FactionWars.AI.Models;
using FactionWars.AI.Services;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.Core.Services;
using FactionWars.Factions.Interfaces;
using FactionWars.Factions.Models;
using Moq;
using Xunit;

namespace FactionWars.Tests.Unit.AI.Services
{
    /// <summary>
    /// Tests for the AntiVehicleResponseService.
    /// This service coordinates the deployment of Elite/RPG units in response to
    /// vehicle threats detected in battle zones.
    /// </summary>
    public class AntiVehicleResponseServiceTests
    {
        private readonly Mock<IFactionService> _mockFactionService;
        private readonly Mock<IZoneDefenderAllocationService> _mockAllocationService;
        private readonly Mock<IVehicleThreatService> _mockVehicleThreatService;
        private readonly IDefenderTierService _tierService;
        private readonly AntiVehicleResponseService _service;

        private const int EliteCost = 2000;

        public AntiVehicleResponseServiceTests()
        {
            _mockFactionService = new Mock<IFactionService>();
            _mockAllocationService = new Mock<IZoneDefenderAllocationService>();
            _mockVehicleThreatService = new Mock<IVehicleThreatService>();
            _tierService = new DefenderTierService();

            _service = new AntiVehicleResponseService(
                _mockFactionService.Object,
                _mockAllocationService.Object,
                _mockVehicleThreatService.Object,
                _tierService);
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_CreatesInstance()
        {
            var service = new AntiVehicleResponseService(
                _mockFactionService.Object,
                _mockAllocationService.Object,
                _mockVehicleThreatService.Object,
                _tierService);

            Assert.NotNull(service);
        }

        [Fact]
        public void Constructor_ImplementsIAntiVehicleResponseService()
        {
            var service = new AntiVehicleResponseService(
                _mockFactionService.Object,
                _mockAllocationService.Object,
                _mockVehicleThreatService.Object,
                _tierService);

            Assert.IsAssignableFrom<IAntiVehicleResponseService>(service);
        }

        #endregion

        #region VehicleThreatLevel.None Tests

        [Fact]
        public void RespondToVehicleThreat_None_ReturnsZero()
        {
            // Arrange
            var factionState = new FactionState("faction1", initialCash: 10000);
            factionState.AddReserveTroops(DefenderTier.Elite, 5);
            _mockFactionService.Setup(f => f.GetFactionState("faction1")).Returns(factionState);
            _mockVehicleThreatService.Setup(v => v.GetRequiredRpgCount(VehicleThreatLevel.None)).Returns(0);

            // Act
            var result = _service.RespondToVehicleThreat("faction1", "zone1", VehicleThreatLevel.None);

            // Assert
            Assert.Equal(0, result);
            _mockAllocationService.Verify(
                a => a.AllocateTroops(It.IsAny<FactionState>(), It.IsAny<string>(), It.IsAny<DefenderTier>(), It.IsAny<int>()),
                Times.Never);
        }

        [Fact]
        public void RespondToVehicleThreat_None_DoesNotConsumeResources()
        {
            // Arrange
            var factionState = new FactionState("faction1", initialCash: 10000);
            factionState.AddReserveTroops(DefenderTier.Elite, 5);
            _mockFactionService.Setup(f => f.GetFactionState("faction1")).Returns(factionState);
            _mockVehicleThreatService.Setup(v => v.GetRequiredRpgCount(VehicleThreatLevel.None)).Returns(0);

            // Act
            _service.RespondToVehicleThreat("faction1", "zone1", VehicleThreatLevel.None);

            // Assert
            _mockFactionService.Verify(f => f.SpendCash(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
            _mockFactionService.Verify(f => f.AddReserveTroops(It.IsAny<string>(), It.IsAny<DefenderTier>(), It.IsAny<int>()), Times.Never);
        }

        #endregion

        #region Deploy from Reserve - Light Threat (1 RPG)

        [Fact]
        public void RespondToVehicleThreat_Light_WithReserve_Deploys1Elite()
        {
            // Arrange
            var factionState = new FactionState("faction1", initialCash: 10000);
            factionState.AddReserveTroops(DefenderTier.Elite, 3);
            _mockFactionService.Setup(f => f.GetFactionState("faction1")).Returns(factionState);
            _mockVehicleThreatService.Setup(v => v.GetRequiredRpgCount(VehicleThreatLevel.Light)).Returns(1);
            _mockAllocationService.Setup(a => a.AllocateTroops(factionState, "zone1", DefenderTier.Elite, 1)).Returns(true);

            // Act
            var result = _service.RespondToVehicleThreat("faction1", "zone1", VehicleThreatLevel.Light);

            // Assert
            Assert.Equal(1, result);
            _mockAllocationService.Verify(a => a.AllocateTroops(factionState, "zone1", DefenderTier.Elite, 1), Times.Once);
        }

        [Fact]
        public void RespondToVehicleThreat_Light_WithReserve_DoesNotSpendCash()
        {
            // Arrange
            var factionState = new FactionState("faction1", initialCash: 10000);
            factionState.AddReserveTroops(DefenderTier.Elite, 3);
            _mockFactionService.Setup(f => f.GetFactionState("faction1")).Returns(factionState);
            _mockVehicleThreatService.Setup(v => v.GetRequiredRpgCount(VehicleThreatLevel.Light)).Returns(1);
            _mockAllocationService.Setup(a => a.AllocateTroops(factionState, "zone1", DefenderTier.Elite, 1)).Returns(true);

            // Act
            _service.RespondToVehicleThreat("faction1", "zone1", VehicleThreatLevel.Light);

            // Assert
            _mockFactionService.Verify(f => f.SpendCash(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        }

        #endregion

        #region Deploy from Reserve - Heavy Threat (2 RPGs)

        [Fact]
        public void RespondToVehicleThreat_Heavy_WithReserve_Deploys2Elite()
        {
            // Arrange
            var factionState = new FactionState("faction1", initialCash: 10000);
            factionState.AddReserveTroops(DefenderTier.Elite, 5);
            _mockFactionService.Setup(f => f.GetFactionState("faction1")).Returns(factionState);
            _mockVehicleThreatService.Setup(v => v.GetRequiredRpgCount(VehicleThreatLevel.Heavy)).Returns(2);
            _mockAllocationService.Setup(a => a.AllocateTroops(factionState, "zone1", DefenderTier.Elite, 2)).Returns(true);

            // Act
            var result = _service.RespondToVehicleThreat("faction1", "zone1", VehicleThreatLevel.Heavy);

            // Assert
            Assert.Equal(2, result);
            _mockAllocationService.Verify(a => a.AllocateTroops(factionState, "zone1", DefenderTier.Elite, 2), Times.Once);
        }

        [Fact]
        public void RespondToVehicleThreat_Heavy_WithPartialReserve_DeploysOnlyAvailable()
        {
            // Arrange - only 1 Elite in reserve, but need 2 for Heavy threat
            var factionState = new FactionState("faction1", initialCash: 0);
            factionState.AddReserveTroops(DefenderTier.Elite, 1);
            _mockFactionService.Setup(f => f.GetFactionState("faction1")).Returns(factionState);
            _mockVehicleThreatService.Setup(v => v.GetRequiredRpgCount(VehicleThreatLevel.Heavy)).Returns(2);
            _mockAllocationService.Setup(a => a.AllocateTroops(factionState, "zone1", DefenderTier.Elite, 1)).Returns(true);

            // Act
            var result = _service.RespondToVehicleThreat("faction1", "zone1", VehicleThreatLevel.Heavy);

            // Assert - can only deploy 1 since that's all available and no cash for emergency
            Assert.Equal(1, result);
            _mockAllocationService.Verify(a => a.AllocateTroops(factionState, "zone1", DefenderTier.Elite, 1), Times.Once);
        }

        #endregion

        #region Emergency Purchase - Reserve Empty, Has Cash

        [Fact]
        public void RespondToVehicleThreat_Light_EmptyReserve_WithCash_EmergencyPurchasesAndDeploys()
        {
            // Arrange - no reserve but has $2000 for emergency purchase
            var factionState = new FactionState("faction1", initialCash: 2000);
            _mockFactionService.Setup(f => f.GetFactionState("faction1")).Returns(factionState);
            _mockVehicleThreatService.Setup(v => v.GetRequiredRpgCount(VehicleThreatLevel.Light)).Returns(1);
            _mockFactionService.Setup(f => f.SpendCash("faction1", EliteCost)).Returns(true);
            _mockFactionService.Setup(f => f.AddReserveTroops("faction1", DefenderTier.Elite, 1)).Returns(true);
            _mockAllocationService.Setup(a => a.AllocateTroops(It.IsAny<FactionState>(), "zone1", DefenderTier.Elite, 1)).Returns(true);

            // Act
            var result = _service.RespondToVehicleThreat("faction1", "zone1", VehicleThreatLevel.Light);

            // Assert
            Assert.Equal(1, result);
            _mockFactionService.Verify(f => f.SpendCash("faction1", EliteCost), Times.Once);
            _mockFactionService.Verify(f => f.AddReserveTroops("faction1", DefenderTier.Elite, 1), Times.Once);
            _mockAllocationService.Verify(a => a.AllocateTroops(It.IsAny<FactionState>(), "zone1", DefenderTier.Elite, 1), Times.Once);
        }

        [Fact]
        public void RespondToVehicleThreat_Heavy_EmptyReserve_WithCash_EmergencyPurchases2AndDeploys()
        {
            // Arrange - no reserve but has $4000 for emergency purchase of 2 Elite
            var factionState = new FactionState("faction1", initialCash: 4000);
            _mockFactionService.Setup(f => f.GetFactionState("faction1")).Returns(factionState);
            _mockVehicleThreatService.Setup(v => v.GetRequiredRpgCount(VehicleThreatLevel.Heavy)).Returns(2);
            // Service purchases one Elite at a time to maximize what can be afforded
            _mockFactionService.Setup(f => f.SpendCash("faction1", EliteCost)).Returns(true);
            _mockFactionService.Setup(f => f.AddReserveTroops("faction1", DefenderTier.Elite, 1)).Returns(true);
            _mockAllocationService.Setup(a => a.AllocateTroops(It.IsAny<FactionState>(), "zone1", DefenderTier.Elite, 2)).Returns(true);

            // Act
            var result = _service.RespondToVehicleThreat("faction1", "zone1", VehicleThreatLevel.Heavy);

            // Assert
            Assert.Equal(2, result);
            // Should spend $2000 twice (once per Elite)
            _mockFactionService.Verify(f => f.SpendCash("faction1", EliteCost), Times.Exactly(2));
            _mockFactionService.Verify(f => f.AddReserveTroops("faction1", DefenderTier.Elite, 1), Times.Exactly(2));
            _mockAllocationService.Verify(a => a.AllocateTroops(It.IsAny<FactionState>(), "zone1", DefenderTier.Elite, 2), Times.Once);
        }

        [Fact]
        public void RespondToVehicleThreat_Heavy_EmptyReserve_WithPartialCash_PurchasesWhatCanAfford()
        {
            // Arrange - no reserve, only $2000 (can only afford 1 Elite for Heavy threat)
            var factionState = new FactionState("faction1", initialCash: 2000);
            _mockFactionService.Setup(f => f.GetFactionState("faction1")).Returns(factionState);
            _mockVehicleThreatService.Setup(v => v.GetRequiredRpgCount(VehicleThreatLevel.Heavy)).Returns(2);
            // Use callback to actually spend cash from the state so CanAfford returns false after first purchase
            _mockFactionService.Setup(f => f.SpendCash("faction1", EliteCost))
                .Callback(() => factionState.SpendCash(EliteCost))
                .Returns(true);
            _mockFactionService.Setup(f => f.AddReserveTroops("faction1", DefenderTier.Elite, 1)).Returns(true);
            _mockAllocationService.Setup(a => a.AllocateTroops(It.IsAny<FactionState>(), "zone1", DefenderTier.Elite, 1)).Returns(true);

            // Act
            var result = _service.RespondToVehicleThreat("faction1", "zone1", VehicleThreatLevel.Heavy);

            // Assert - can only afford 1 Elite even though 2 required (CanAfford on state checks real cash)
            Assert.Equal(1, result);
            _mockFactionService.Verify(f => f.SpendCash("faction1", EliteCost), Times.Once);
            _mockFactionService.Verify(f => f.AddReserveTroops("faction1", DefenderTier.Elite, 1), Times.Once);
        }

        #endregion

        #region Hybrid - Partial Reserve + Emergency Purchase

        [Fact]
        public void RespondToVehicleThreat_Heavy_PartialReserve_WithCash_CombinesReserveAndPurchase()
        {
            // Arrange - 1 Elite in reserve, need 2, has cash for 1 more
            var factionState = new FactionState("faction1", initialCash: 2000);
            factionState.AddReserveTroops(DefenderTier.Elite, 1);
            _mockFactionService.Setup(f => f.GetFactionState("faction1")).Returns(factionState);
            _mockVehicleThreatService.Setup(v => v.GetRequiredRpgCount(VehicleThreatLevel.Heavy)).Returns(2);
            _mockFactionService.Setup(f => f.SpendCash("faction1", EliteCost)).Returns(true);
            _mockFactionService.Setup(f => f.AddReserveTroops("faction1", DefenderTier.Elite, 1)).Returns(true);
            _mockAllocationService.Setup(a => a.AllocateTroops(It.IsAny<FactionState>(), "zone1", DefenderTier.Elite, 2)).Returns(true);

            // Act
            var result = _service.RespondToVehicleThreat("faction1", "zone1", VehicleThreatLevel.Heavy);

            // Assert - 1 from reserve + 1 purchased = 2 deployed
            Assert.Equal(2, result);
            _mockFactionService.Verify(f => f.SpendCash("faction1", EliteCost), Times.Once);
            _mockFactionService.Verify(f => f.AddReserveTroops("faction1", DefenderTier.Elite, 1), Times.Once);
            _mockAllocationService.Verify(a => a.AllocateTroops(It.IsAny<FactionState>(), "zone1", DefenderTier.Elite, 2), Times.Once);
        }

        #endregion

        #region No Response Scenarios

        [Fact]
        public void RespondToVehicleThreat_Light_NoReserve_NoCash_ReturnsZero()
        {
            // Arrange - no reserve, no cash
            var factionState = new FactionState("faction1", initialCash: 0);
            _mockFactionService.Setup(f => f.GetFactionState("faction1")).Returns(factionState);
            _mockVehicleThreatService.Setup(v => v.GetRequiredRpgCount(VehicleThreatLevel.Light)).Returns(1);

            // Act
            var result = _service.RespondToVehicleThreat("faction1", "zone1", VehicleThreatLevel.Light);

            // Assert
            Assert.Equal(0, result);
            _mockAllocationService.Verify(
                a => a.AllocateTroops(It.IsAny<FactionState>(), It.IsAny<string>(), It.IsAny<DefenderTier>(), It.IsAny<int>()),
                Times.Never);
        }

        [Fact]
        public void RespondToVehicleThreat_Light_NoReserve_InsufficientCash_ReturnsZero()
        {
            // Arrange - no reserve, $1999 (not enough for $2000 Elite)
            var factionState = new FactionState("faction1", initialCash: 1999);
            _mockFactionService.Setup(f => f.GetFactionState("faction1")).Returns(factionState);
            _mockVehicleThreatService.Setup(v => v.GetRequiredRpgCount(VehicleThreatLevel.Light)).Returns(1);

            // Act
            var result = _service.RespondToVehicleThreat("faction1", "zone1", VehicleThreatLevel.Light);

            // Assert
            Assert.Equal(0, result);
        }

        [Fact]
        public void RespondToVehicleThreat_FactionNotFound_ReturnsZero()
        {
            // Arrange
            _mockFactionService.Setup(f => f.GetFactionState("nonexistent")).Returns((FactionState?)null);
            _mockVehicleThreatService.Setup(v => v.GetRequiredRpgCount(VehicleThreatLevel.Heavy)).Returns(2);

            // Act
            var result = _service.RespondToVehicleThreat("nonexistent", "zone1", VehicleThreatLevel.Heavy);

            // Assert
            Assert.Equal(0, result);
        }

        [Fact]
        public void RespondToVehicleThreat_NullFactionId_ReturnsZero()
        {
            // Arrange
            _mockVehicleThreatService.Setup(v => v.GetRequiredRpgCount(VehicleThreatLevel.Light)).Returns(1);

            // Act
            var result = _service.RespondToVehicleThreat(null!, "zone1", VehicleThreatLevel.Light);

            // Assert
            Assert.Equal(0, result);
        }

        [Fact]
        public void RespondToVehicleThreat_EmptyFactionId_ReturnsZero()
        {
            // Arrange
            _mockVehicleThreatService.Setup(v => v.GetRequiredRpgCount(VehicleThreatLevel.Light)).Returns(1);

            // Act
            var result = _service.RespondToVehicleThreat("", "zone1", VehicleThreatLevel.Light);

            // Assert
            Assert.Equal(0, result);
        }

        [Fact]
        public void RespondToVehicleThreat_NullZoneId_ReturnsZero()
        {
            // Arrange
            var factionState = new FactionState("faction1", initialCash: 10000);
            factionState.AddReserveTroops(DefenderTier.Elite, 5);
            _mockFactionService.Setup(f => f.GetFactionState("faction1")).Returns(factionState);
            _mockVehicleThreatService.Setup(v => v.GetRequiredRpgCount(VehicleThreatLevel.Light)).Returns(1);

            // Act
            var result = _service.RespondToVehicleThreat("faction1", null!, VehicleThreatLevel.Light);

            // Assert
            Assert.Equal(0, result);
        }

        [Fact]
        public void RespondToVehicleThreat_EmptyZoneId_ReturnsZero()
        {
            // Arrange
            var factionState = new FactionState("faction1", initialCash: 10000);
            factionState.AddReserveTroops(DefenderTier.Elite, 5);
            _mockFactionService.Setup(f => f.GetFactionState("faction1")).Returns(factionState);
            _mockVehicleThreatService.Setup(v => v.GetRequiredRpgCount(VehicleThreatLevel.Light)).Returns(1);

            // Act
            var result = _service.RespondToVehicleThreat("faction1", "", VehicleThreatLevel.Light);

            // Assert
            Assert.Equal(0, result);
        }

        #endregion

        #region Allocation Failure Tests

        [Fact]
        public void RespondToVehicleThreat_AllocationFails_ReturnsZero()
        {
            // Arrange - allocation service fails for some reason
            var factionState = new FactionState("faction1", initialCash: 10000);
            factionState.AddReserveTroops(DefenderTier.Elite, 5);
            _mockFactionService.Setup(f => f.GetFactionState("faction1")).Returns(factionState);
            _mockVehicleThreatService.Setup(v => v.GetRequiredRpgCount(VehicleThreatLevel.Light)).Returns(1);
            _mockAllocationService.Setup(a => a.AllocateTroops(factionState, "zone1", DefenderTier.Elite, 1)).Returns(false);

            // Act
            var result = _service.RespondToVehicleThreat("faction1", "zone1", VehicleThreatLevel.Light);

            // Assert - allocation failed, so no troops deployed
            Assert.Equal(0, result);
        }

        #endregion

        #region RPG Count from Service Tests

        [Theory]
        [InlineData(VehicleThreatLevel.None, 0)]
        [InlineData(VehicleThreatLevel.Light, 1)]
        [InlineData(VehicleThreatLevel.Heavy, 2)]
        public void RespondToVehicleThreat_UsesVehicleThreatServiceForRpgCount(VehicleThreatLevel threatLevel, int expectedRpgCount)
        {
            // Arrange
            var factionState = new FactionState("faction1", initialCash: 10000);
            factionState.AddReserveTroops(DefenderTier.Elite, 5);
            _mockFactionService.Setup(f => f.GetFactionState("faction1")).Returns(factionState);
            _mockVehicleThreatService.Setup(v => v.GetRequiredRpgCount(threatLevel)).Returns(expectedRpgCount);
            _mockAllocationService.Setup(a => a.AllocateTroops(It.IsAny<FactionState>(), It.IsAny<string>(), DefenderTier.Elite, It.IsAny<int>())).Returns(true);

            // Act
            var result = _service.RespondToVehicleThreat("faction1", "zone1", threatLevel);

            // Assert
            _mockVehicleThreatService.Verify(v => v.GetRequiredRpgCount(threatLevel), Times.Once);
            Assert.Equal(expectedRpgCount, result);
        }

        #endregion

        #region Elite Cost from TierService Tests

        [Fact]
        public void RespondToVehicleThreat_EmergencyPurchase_UsesCorrectEliteCostFromTierService()
        {
            // Arrange - verify it uses the cost from DefenderTierService ($2000 for Elite)
            var factionState = new FactionState("faction1", initialCash: 2000);
            _mockFactionService.Setup(f => f.GetFactionState("faction1")).Returns(factionState);
            _mockVehicleThreatService.Setup(v => v.GetRequiredRpgCount(VehicleThreatLevel.Light)).Returns(1);
            _mockFactionService.Setup(f => f.SpendCash("faction1", 2000)).Returns(true);
            _mockFactionService.Setup(f => f.AddReserveTroops("faction1", DefenderTier.Elite, 1)).Returns(true);
            _mockAllocationService.Setup(a => a.AllocateTroops(It.IsAny<FactionState>(), "zone1", DefenderTier.Elite, 1)).Returns(true);

            // Act
            _service.RespondToVehicleThreat("faction1", "zone1", VehicleThreatLevel.Light);

            // Assert - verify SpendCash was called with the correct Elite cost
            _mockFactionService.Verify(f => f.SpendCash("faction1", 2000), Times.Once);
        }

        #endregion
    }
}
