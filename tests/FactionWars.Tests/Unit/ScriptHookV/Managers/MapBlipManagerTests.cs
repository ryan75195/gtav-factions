using System.Collections.Generic;
using FactionWars.Core.Interfaces;
using FactionWars.Factions.Interfaces;
using FactionWars.Factions.Models;
using FactionWars.ScriptHookV.Managers;
using FactionWars.Territory.Interfaces;
using FactionWars.Territory.Models;
using Moq;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV.Managers
{
    public class MapBlipManagerTests
    {
        private readonly Mock<IGameBridge> _gameBridgeMock;
        private readonly Mock<IZoneRepository> _zoneRepositoryMock;
        private readonly Mock<IFactionService> _factionServiceMock;

        public MapBlipManagerTests()
        {
            _gameBridgeMock = new Mock<IGameBridge>();
            _zoneRepositoryMock = new Mock<IZoneRepository>();
            _factionServiceMock = new Mock<IFactionService>();
        }

        [Fact]
        public void Constructor_WithNullGameBridge_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<System.ArgumentNullException>(() =>
                new MapBlipManager(null!, _zoneRepositoryMock.Object, _factionServiceMock.Object));
        }

        [Fact]
        public void Constructor_WithNullZoneRepository_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<System.ArgumentNullException>(() =>
                new MapBlipManager(_gameBridgeMock.Object, null!, _factionServiceMock.Object));
        }

        [Fact]
        public void Constructor_WithNullFactionService_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<System.ArgumentNullException>(() =>
                new MapBlipManager(_gameBridgeMock.Object, _zoneRepositoryMock.Object, null!));
        }

        [Fact]
        public void Initialize_WithZones_ShouldCreateBlipForEachZone()
        {
            // Arrange
            var zones = new List<Zone>
            {
                new Zone("zone1", "Downtown", new Vector3(100f, 200f, 30f), 100f, 5),
                new Zone("zone2", "Industrial", new Vector3(300f, 400f, 30f), 150f, 3)
            };
            _zoneRepositoryMock.Setup(r => r.GetAll()).Returns(zones);
            _gameBridgeMock.Setup(g => g.CreateBlip(It.IsAny<Vector3>())).Returns(1);

            var manager = new MapBlipManager(_gameBridgeMock.Object, _zoneRepositoryMock.Object, _factionServiceMock.Object);

            // Act
            manager.Initialize();

            // Assert - should create a blip for each zone at its center position
            _gameBridgeMock.Verify(g => g.CreateBlip(zones[0].Center), Times.Once);
            _gameBridgeMock.Verify(g => g.CreateBlip(zones[1].Center), Times.Once);
        }

        [Fact]
        public void Initialize_WithEmptyZoneRepository_ShouldNotCreateBlips()
        {
            // Arrange
            _zoneRepositoryMock.Setup(r => r.GetAll()).Returns(new List<Zone>());

            var manager = new MapBlipManager(_gameBridgeMock.Object, _zoneRepositoryMock.Object, _factionServiceMock.Object);

            // Act
            manager.Initialize();

            // Assert
            _gameBridgeMock.Verify(g => g.CreateBlip(It.IsAny<Vector3>()), Times.Never);
        }

        [Fact]
        public void Initialize_WithNeutralZone_ShouldSetBlipColorToWhite()
        {
            // Arrange
            var zone = new Zone("zone1", "Downtown", new Vector3(100f, 200f, 30f), 100f, 5);
            // Zone has no owner (neutral)
            _zoneRepositoryMock.Setup(r => r.GetAll()).Returns(new List<Zone> { zone });
            _gameBridgeMock.Setup(g => g.CreateBlip(It.IsAny<Vector3>())).Returns(42);

            var manager = new MapBlipManager(_gameBridgeMock.Object, _zoneRepositoryMock.Object, _factionServiceMock.Object);

            // Act
            manager.Initialize();

            // Assert
            _gameBridgeMock.Verify(g => g.SetBlipColor(42, BlipColor.White), Times.Once);
        }

        [Fact]
        public void Initialize_WithMichaelOwnedZone_ShouldSetBlipColorToMichaelBlue()
        {
            // Arrange
            var zone = new Zone("zone1", "Downtown", new Vector3(100f, 200f, 30f), 100f, 5);
            zone.OwnerFactionId = "michael";
            _zoneRepositoryMock.Setup(r => r.GetAll()).Returns(new List<Zone> { zone });
            _gameBridgeMock.Setup(g => g.CreateBlip(It.IsAny<Vector3>())).Returns(42);

            var manager = new MapBlipManager(_gameBridgeMock.Object, _zoneRepositoryMock.Object, _factionServiceMock.Object);

            // Act
            manager.Initialize();

            // Assert
            _gameBridgeMock.Verify(g => g.SetBlipColor(42, BlipColor.MichaelBlue), Times.Once);
        }

        [Fact]
        public void Initialize_WithTrevorOwnedZone_ShouldSetBlipColorToTrevorOrange()
        {
            // Arrange
            var zone = new Zone("zone1", "Downtown", new Vector3(100f, 200f, 30f), 100f, 5);
            zone.OwnerFactionId = "trevor";
            _zoneRepositoryMock.Setup(r => r.GetAll()).Returns(new List<Zone> { zone });
            _gameBridgeMock.Setup(g => g.CreateBlip(It.IsAny<Vector3>())).Returns(42);

            var manager = new MapBlipManager(_gameBridgeMock.Object, _zoneRepositoryMock.Object, _factionServiceMock.Object);

            // Act
            manager.Initialize();

            // Assert
            _gameBridgeMock.Verify(g => g.SetBlipColor(42, BlipColor.TrevorOrange), Times.Once);
        }

        [Fact]
        public void Initialize_WithFranklinOwnedZone_ShouldSetBlipColorToFranklinGreen()
        {
            // Arrange
            var zone = new Zone("zone1", "Downtown", new Vector3(100f, 200f, 30f), 100f, 5);
            zone.OwnerFactionId = "franklin";
            _zoneRepositoryMock.Setup(r => r.GetAll()).Returns(new List<Zone> { zone });
            _gameBridgeMock.Setup(g => g.CreateBlip(It.IsAny<Vector3>())).Returns(42);

            var manager = new MapBlipManager(_gameBridgeMock.Object, _zoneRepositoryMock.Object, _factionServiceMock.Object);

            // Act
            manager.Initialize();

            // Assert
            _gameBridgeMock.Verify(g => g.SetBlipColor(42, BlipColor.FranklinGreen), Times.Once);
        }

        [Fact]
        public void UpdateBlipColors_WhenZoneOwnerChanges_ShouldUpdateBlipColor()
        {
            // Arrange
            var zone = new Zone("zone1", "Downtown", new Vector3(100f, 200f, 30f), 100f, 5);
            _zoneRepositoryMock.Setup(r => r.GetAll()).Returns(new List<Zone> { zone });
            _zoneRepositoryMock.Setup(r => r.GetById("zone1")).Returns(zone);
            _gameBridgeMock.Setup(g => g.CreateBlip(It.IsAny<Vector3>())).Returns(42);

            var manager = new MapBlipManager(_gameBridgeMock.Object, _zoneRepositoryMock.Object, _factionServiceMock.Object);
            manager.Initialize();

            // Change zone owner
            zone.OwnerFactionId = "trevor";

            // Act
            manager.UpdateBlipColors();

            // Assert - should set to Trevor's color now
            _gameBridgeMock.Verify(g => g.SetBlipColor(42, BlipColor.TrevorOrange), Times.Once);
        }

        [Fact]
        public void UpdateBlipColor_ForSpecificZone_ShouldOnlyUpdateThatZone()
        {
            // Arrange
            var zone1 = new Zone("zone1", "Downtown", new Vector3(100f, 200f, 30f), 100f, 5);
            var zone2 = new Zone("zone2", "Industrial", new Vector3(300f, 400f, 30f), 150f, 3);
            zone2.OwnerFactionId = "michael";
            _zoneRepositoryMock.Setup(r => r.GetAll()).Returns(new List<Zone> { zone1, zone2 });
            _zoneRepositoryMock.Setup(r => r.GetById("zone1")).Returns(zone1);
            _gameBridgeMock.SetupSequence(g => g.CreateBlip(It.IsAny<Vector3>()))
                .Returns(1)
                .Returns(2);

            var manager = new MapBlipManager(_gameBridgeMock.Object, _zoneRepositoryMock.Object, _factionServiceMock.Object);
            manager.Initialize();

            // Clear invocations from initialize
            _gameBridgeMock.Invocations.Clear();

            // Change zone1 owner
            zone1.OwnerFactionId = "franklin";

            // Act
            manager.UpdateBlipColor("zone1");

            // Assert - should only update zone1's blip
            _gameBridgeMock.Verify(g => g.SetBlipColor(1, BlipColor.FranklinGreen), Times.Once);
            _gameBridgeMock.Verify(g => g.SetBlipColor(2, It.IsAny<BlipColor>()), Times.Never);
        }

        [Fact]
        public void Dispose_ShouldDeleteAllBlips()
        {
            // Arrange
            var zones = new List<Zone>
            {
                new Zone("zone1", "Downtown", new Vector3(100f, 200f, 30f), 100f, 5),
                new Zone("zone2", "Industrial", new Vector3(300f, 400f, 30f), 150f, 3)
            };
            _zoneRepositoryMock.Setup(r => r.GetAll()).Returns(zones);
            _gameBridgeMock.SetupSequence(g => g.CreateBlip(It.IsAny<Vector3>()))
                .Returns(1)
                .Returns(2);

            var manager = new MapBlipManager(_gameBridgeMock.Object, _zoneRepositoryMock.Object, _factionServiceMock.Object);
            manager.Initialize();

            // Act
            manager.Dispose();

            // Assert
            _gameBridgeMock.Verify(g => g.DeleteBlip(1), Times.Once);
            _gameBridgeMock.Verify(g => g.DeleteBlip(2), Times.Once);
        }

        [Fact]
        public void Dispose_WhenNotInitialized_ShouldNotThrow()
        {
            // Arrange
            var manager = new MapBlipManager(_gameBridgeMock.Object, _zoneRepositoryMock.Object, _factionServiceMock.Object);

            // Act & Assert - should not throw
            manager.Dispose();
            // If we reach here, no exception was thrown
        }

        [Fact]
        public void Initialize_WhenCalledTwice_ShouldDeleteOldBlipsFirst()
        {
            // Arrange
            var zone = new Zone("zone1", "Downtown", new Vector3(100f, 200f, 30f), 100f, 5);
            _zoneRepositoryMock.Setup(r => r.GetAll()).Returns(new List<Zone> { zone });
            _gameBridgeMock.SetupSequence(g => g.CreateBlip(It.IsAny<Vector3>()))
                .Returns(1)
                .Returns(2);

            var manager = new MapBlipManager(_gameBridgeMock.Object, _zoneRepositoryMock.Object, _factionServiceMock.Object);
            manager.Initialize();

            // Act
            manager.Initialize();

            // Assert - should have deleted the old blip before creating a new one
            _gameBridgeMock.Verify(g => g.DeleteBlip(1), Times.Once);
            _gameBridgeMock.Verify(g => g.CreateBlip(It.IsAny<Vector3>()), Times.Exactly(2));
        }

        [Fact]
        public void GetBlipHandle_ForExistingZone_ShouldReturnHandle()
        {
            // Arrange
            var zone = new Zone("zone1", "Downtown", new Vector3(100f, 200f, 30f), 100f, 5);
            _zoneRepositoryMock.Setup(r => r.GetAll()).Returns(new List<Zone> { zone });
            _gameBridgeMock.Setup(g => g.CreateBlip(It.IsAny<Vector3>())).Returns(42);

            var manager = new MapBlipManager(_gameBridgeMock.Object, _zoneRepositoryMock.Object, _factionServiceMock.Object);
            manager.Initialize();

            // Act
            var handle = manager.GetBlipHandle("zone1");

            // Assert
            Assert.Equal(42, handle);
        }

        [Fact]
        public void GetBlipHandle_ForNonExistingZone_ShouldReturnMinusOne()
        {
            // Arrange
            var manager = new MapBlipManager(_gameBridgeMock.Object, _zoneRepositoryMock.Object, _factionServiceMock.Object);

            // Act
            var handle = manager.GetBlipHandle("nonexistent");

            // Assert
            Assert.Equal(-1, handle);
        }

        [Fact]
        public void Initialize_WhenBlipCreationFails_ShouldNotTrackBlip()
        {
            // Arrange
            var zone = new Zone("zone1", "Downtown", new Vector3(100f, 200f, 30f), 100f, 5);
            _zoneRepositoryMock.Setup(r => r.GetAll()).Returns(new List<Zone> { zone });
            _gameBridgeMock.Setup(g => g.CreateBlip(It.IsAny<Vector3>())).Returns(-1);

            var manager = new MapBlipManager(_gameBridgeMock.Object, _zoneRepositoryMock.Object, _factionServiceMock.Object);

            // Act
            manager.Initialize();

            // Assert - should not try to set color for failed blip
            _gameBridgeMock.Verify(g => g.SetBlipColor(It.IsAny<int>(), It.IsAny<BlipColor>()), Times.Never);
            Assert.Equal(-1, manager.GetBlipHandle("zone1"));
        }
    }
}
