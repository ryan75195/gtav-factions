using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Models;
using FactionWars.Combat.Services;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using Moq;
using Xunit;

namespace FactionWars.Tests.Unit.Combat
{
    public class DefenderCasualtyServiceTests
    {
        private readonly Mock<IGameBridge> _gameBridgeMock;
        private readonly Mock<IPedPool> _pedPoolMock;
        private readonly Mock<IZoneDefenderAllocationRepository> _allocationRepositoryMock;
        private readonly IDefenderCasualtyService _service;

        public DefenderCasualtyServiceTests()
        {
            _gameBridgeMock = new Mock<IGameBridge>();
            _pedPoolMock = new Mock<IPedPool>();
            _allocationRepositoryMock = new Mock<IZoneDefenderAllocationRepository>();
            _service = new DefenderCasualtyService(
                _gameBridgeMock.Object,
                _pedPoolMock.Object,
                _allocationRepositoryMock.Object);
        }

        [Fact]
        public void Constructor_WithNullGameBridge_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<System.ArgumentNullException>(() => new DefenderCasualtyService(
                null!,
                _pedPoolMock.Object,
                _allocationRepositoryMock.Object));
        }

        [Fact]
        public void Constructor_WithNullPedPool_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<System.ArgumentNullException>(() => new DefenderCasualtyService(
                _gameBridgeMock.Object,
                null!,
                _allocationRepositoryMock.Object));
        }

        [Fact]
        public void Constructor_WithNullAllocationRepository_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<System.ArgumentNullException>(() => new DefenderCasualtyService(
                _gameBridgeMock.Object,
                _pedPoolMock.Object,
                null!));
        }

        [Fact]
        public void ProcessCasualties_WithNoDeadPeds_ShouldReturnZero()
        {
            // Arrange
            var peds = new[]
            {
                CreatePedHandle(1, "faction1", "zone1", DefenderRole.Grunt)
            };
            _pedPoolMock.Setup(p => p.GetAll()).Returns(peds);
            _gameBridgeMock.Setup(g => g.IsPedAlive(1)).Returns(true);

            // Act
            var result = _service.ProcessCasualties();

            // Assert
            Assert.Equal(0, result.TotalCasualties);
            Assert.Empty(result.CasualtiesByTier);
        }

        [Fact]
        public void ProcessCasualties_WithDeadBasicDefender_ShouldDeductFromAllocation()
        {
            // Arrange
            var ped = CreatePedHandle(1, "faction1", "zone1", DefenderRole.Grunt);
            var peds = new[] { ped };
            _pedPoolMock.Setup(p => p.GetAll()).Returns(peds);
            _gameBridgeMock.Setup(g => g.IsPedAlive(1)).Returns(false);

            var allocation = new ZoneDefenderAllocation("faction1", "zone1");
            allocation.AddTroops(DefenderRole.Grunt, 5);
            _allocationRepositoryMock.Setup(r => r.Get("faction1", "zone1")).Returns(allocation);

            // Act
            var result = _service.ProcessCasualties();

            // Assert
            Assert.Equal(1, result.TotalCasualties);
            Assert.Equal(4, allocation.GetTroopCount(DefenderRole.Grunt));
            _allocationRepositoryMock.Verify(r => r.Update(allocation), Times.Once);
        }

        [Fact]
        public void ProcessCasualties_WithDeadMediumDefender_ShouldDeductFromAllocation()
        {
            // Arrange
            var ped = CreatePedHandle(1, "faction1", "zone1", DefenderRole.Gunner);
            var peds = new[] { ped };
            _pedPoolMock.Setup(p => p.GetAll()).Returns(peds);
            _gameBridgeMock.Setup(g => g.IsPedAlive(1)).Returns(false);

            var allocation = new ZoneDefenderAllocation("faction1", "zone1");
            allocation.AddTroops(DefenderRole.Gunner, 3);
            _allocationRepositoryMock.Setup(r => r.Get("faction1", "zone1")).Returns(allocation);

            // Act
            var result = _service.ProcessCasualties();

            // Assert
            Assert.Equal(1, result.TotalCasualties);
            Assert.Equal(2, allocation.GetTroopCount(DefenderRole.Gunner));
        }

        [Fact]
        public void ProcessCasualties_WithDeadHeavyDefender_ShouldDeductFromAllocation()
        {
            // Arrange
            var ped = CreatePedHandle(1, "faction1", "zone1", DefenderRole.Rifleman);
            var peds = new[] { ped };
            _pedPoolMock.Setup(p => p.GetAll()).Returns(peds);
            _gameBridgeMock.Setup(g => g.IsPedAlive(1)).Returns(false);

            var allocation = new ZoneDefenderAllocation("faction1", "zone1");
            allocation.AddTroops(DefenderRole.Rifleman, 2);
            _allocationRepositoryMock.Setup(r => r.Get("faction1", "zone1")).Returns(allocation);

            // Act
            var result = _service.ProcessCasualties();

            // Assert
            Assert.Equal(1, result.TotalCasualties);
            Assert.Equal(1, allocation.GetTroopCount(DefenderRole.Rifleman));
        }

        [Fact]
        public void ProcessCasualties_WithMultipleDeadDefenders_ShouldDeductAll()
        {
            // Arrange
            var peds = new[]
            {
                CreatePedHandle(1, "faction1", "zone1", DefenderRole.Grunt),
                CreatePedHandle(2, "faction1", "zone1", DefenderRole.Grunt),
                CreatePedHandle(3, "faction1", "zone1", DefenderRole.Gunner)
            };
            _pedPoolMock.Setup(p => p.GetAll()).Returns(peds);
            _gameBridgeMock.Setup(g => g.IsPedAlive(It.IsAny<int>())).Returns(false);

            var allocation = new ZoneDefenderAllocation("faction1", "zone1");
            allocation.AddTroops(DefenderRole.Grunt, 5);
            allocation.AddTroops(DefenderRole.Gunner, 3);
            _allocationRepositoryMock.Setup(r => r.Get("faction1", "zone1")).Returns(allocation);

            // Act
            var result = _service.ProcessCasualties();

            // Assert
            Assert.Equal(3, result.TotalCasualties);
            Assert.Equal(3, allocation.GetTroopCount(DefenderRole.Grunt)); // 5 - 2
            Assert.Equal(2, allocation.GetTroopCount(DefenderRole.Gunner)); // 3 - 1
        }

        [Fact]
        public void ProcessCasualties_WithNoAllocation_ShouldNotThrow()
        {
            // Arrange
            var ped = CreatePedHandle(1, "faction1", "zone1", DefenderRole.Grunt);
            var peds = new[] { ped };
            _pedPoolMock.Setup(p => p.GetAll()).Returns(peds);
            _gameBridgeMock.Setup(g => g.IsPedAlive(1)).Returns(false);
            _allocationRepositoryMock.Setup(r => r.Get("faction1", "zone1")).Returns((ZoneDefenderAllocation?)null);

            // Act
            var result = _service.ProcessCasualties();

            // Assert - should still count as casualty even without allocation
            Assert.Equal(1, result.TotalCasualties);
            _allocationRepositoryMock.Verify(r => r.Update(It.IsAny<ZoneDefenderAllocation>()), Times.Never);
        }

        [Fact]
        public void ProcessCasualties_WithNullTier_ShouldSkipDeduction()
        {
            // Arrange - ped without a tier set (e.g., attacker or player follower)
            var ped = new PedHandle(1, "faction1", default, "model", "zone1");
            var peds = new[] { ped };
            _pedPoolMock.Setup(p => p.GetAll()).Returns(peds);
            _gameBridgeMock.Setup(g => g.IsPedAlive(1)).Returns(false);

            // Act
            var result = _service.ProcessCasualties();

            // Assert
            Assert.Equal(0, result.TotalCasualties);
            _allocationRepositoryMock.Verify(r => r.Get(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void ProcessCasualties_ShouldRemoveDeadPedsFromPool()
        {
            // Arrange
            var ped = CreatePedHandle(1, "faction1", "zone1", DefenderRole.Grunt);
            var peds = new[] { ped };
            _pedPoolMock.Setup(p => p.GetAll()).Returns(peds);
            _gameBridgeMock.Setup(g => g.IsPedAlive(1)).Returns(false);

            var allocation = new ZoneDefenderAllocation("faction1", "zone1");
            allocation.AddTroops(DefenderRole.Grunt, 5);
            _allocationRepositoryMock.Setup(r => r.Get("faction1", "zone1")).Returns(allocation);

            // Act
            _service.ProcessCasualties();

            // Assert
            _pedPoolMock.Verify(p => p.Remove(ped), Times.Once);
            _gameBridgeMock.Verify(g => g.DeletePed(1), Times.Once);
        }

        [Fact]
        public void ProcessCasualties_ShouldReturnCasualtiesByTier()
        {
            // Arrange
            var peds = new[]
            {
                CreatePedHandle(1, "faction1", "zone1", DefenderRole.Grunt),
                CreatePedHandle(2, "faction1", "zone1", DefenderRole.Grunt),
                CreatePedHandle(3, "faction1", "zone1", DefenderRole.Rifleman)
            };
            _pedPoolMock.Setup(p => p.GetAll()).Returns(peds);
            _gameBridgeMock.Setup(g => g.IsPedAlive(It.IsAny<int>())).Returns(false);

            var allocation = new ZoneDefenderAllocation("faction1", "zone1");
            allocation.AddTroops(DefenderRole.Grunt, 5);
            allocation.AddTroops(DefenderRole.Rifleman, 2);
            _allocationRepositoryMock.Setup(r => r.Get("faction1", "zone1")).Returns(allocation);

            // Act
            var result = _service.ProcessCasualties();

            // Assert
            Assert.Equal(3, result.TotalCasualties);
            Assert.Equal(2, result.CasualtiesByTier[DefenderRole.Grunt]);
            Assert.Equal(1, result.CasualtiesByTier[DefenderRole.Rifleman]);
        }

        [Fact]
        public void ProcessCasualties_WithMultipleZones_ShouldDeductFromCorrectAllocations()
        {
            // Arrange
            var peds = new[]
            {
                CreatePedHandle(1, "faction1", "zone1", DefenderRole.Grunt),
                CreatePedHandle(2, "faction1", "zone2", DefenderRole.Grunt)
            };
            _pedPoolMock.Setup(p => p.GetAll()).Returns(peds);
            _gameBridgeMock.Setup(g => g.IsPedAlive(It.IsAny<int>())).Returns(false);

            var allocation1 = new ZoneDefenderAllocation("faction1", "zone1");
            allocation1.AddTroops(DefenderRole.Grunt, 5);
            var allocation2 = new ZoneDefenderAllocation("faction1", "zone2");
            allocation2.AddTroops(DefenderRole.Grunt, 3);

            _allocationRepositoryMock.Setup(r => r.Get("faction1", "zone1")).Returns(allocation1);
            _allocationRepositoryMock.Setup(r => r.Get("faction1", "zone2")).Returns(allocation2);

            // Act
            var result = _service.ProcessCasualties();

            // Assert
            Assert.Equal(2, result.TotalCasualties);
            Assert.Equal(4, allocation1.GetTroopCount(DefenderRole.Grunt));
            Assert.Equal(2, allocation2.GetTroopCount(DefenderRole.Grunt));
        }

        [Fact]
        public void ProcessCasualties_WithAllocationAtZero_ShouldNotGoNegative()
        {
            // Arrange
            var ped = CreatePedHandle(1, "faction1", "zone1", DefenderRole.Grunt);
            var peds = new[] { ped };
            _pedPoolMock.Setup(p => p.GetAll()).Returns(peds);
            _gameBridgeMock.Setup(g => g.IsPedAlive(1)).Returns(false);

            var allocation = new ZoneDefenderAllocation("faction1", "zone1");
            // Allocation has 0 troops (edge case - shouldn't normally happen)
            _allocationRepositoryMock.Setup(r => r.Get("faction1", "zone1")).Returns(allocation);

            // Act
            var result = _service.ProcessCasualties();

            // Assert - should still process but allocation stays at 0
            Assert.Equal(1, result.TotalCasualties);
            Assert.Equal(0, allocation.GetTroopCount(DefenderRole.Grunt));
        }

        [Fact]
        public void ProcessCasualties_WithNullFactionId_ShouldSkip()
        {
            // Arrange - ped without a faction ID
            var ped = new PedHandle(1, null, default, "model", "zone1", DefenderRole.Grunt);
            var peds = new[] { ped };
            _pedPoolMock.Setup(p => p.GetAll()).Returns(peds);
            _gameBridgeMock.Setup(g => g.IsPedAlive(1)).Returns(false);

            // Act
            var result = _service.ProcessCasualties();

            // Assert
            Assert.Equal(0, result.TotalCasualties);
        }

        [Fact]
        public void ProcessCasualties_WithNullZoneId_ShouldSkip()
        {
            // Arrange - ped without a zone ID
            var ped = new PedHandle(1, "faction1", default, "model", null, DefenderRole.Grunt);
            var peds = new[] { ped };
            _pedPoolMock.Setup(p => p.GetAll()).Returns(peds);
            _gameBridgeMock.Setup(g => g.IsPedAlive(1)).Returns(false);

            // Act
            var result = _service.ProcessCasualties();

            // Assert
            Assert.Equal(0, result.TotalCasualties);
        }

        private static PedHandle CreatePedHandle(int handle, string factionId, string zoneId, DefenderRole tier)
        {
            return new PedHandle(handle, factionId, default, "model", zoneId, tier);
        }
    }
}
