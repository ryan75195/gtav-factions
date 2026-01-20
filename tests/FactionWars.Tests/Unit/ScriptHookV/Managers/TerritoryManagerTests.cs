using FactionWars.Core.Interfaces;
using FactionWars.ScriptHookV.Managers;
using FactionWars.Territory.Interfaces;
using FactionWars.Territory.Models;
using Moq;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV.Managers
{
    public class TerritoryManagerTests
    {
        private readonly Mock<IGameBridge> _gameBridgeMock;
        private readonly Mock<IZoneService> _zoneServiceMock;
        private readonly TerritoryManager _manager;

        public TerritoryManagerTests()
        {
            _gameBridgeMock = new Mock<IGameBridge>();
            _zoneServiceMock = new Mock<IZoneService>();
            _manager = new TerritoryManager(_gameBridgeMock.Object, _zoneServiceMock.Object);
        }

        [Fact]
        public void CurrentZone_Initially_ShouldBeNull()
        {
            // Assert
            Assert.Null(_manager.CurrentZone);
        }

        [Fact]
        public void Update_WhenPlayerNotInAnyZone_ShouldSetCurrentZoneToNull()
        {
            // Arrange
            var position = new Vector3(100f, 200f, 30f);
            _gameBridgeMock.Setup(g => g.GetPlayerPosition()).Returns(position);
            _zoneServiceMock.Setup(z => z.GetZoneAtPosition(position)).Returns((Zone?)null);

            // Act
            _manager.Update();

            // Assert
            Assert.Null(_manager.CurrentZone);
        }

        [Fact]
        public void Update_WhenPlayerEntersZone_ShouldSetCurrentZone()
        {
            // Arrange
            var position = new Vector3(100f, 200f, 30f);
            var zone = new Zone("zone1", "Downtown", new Vector3(100f, 200f, 30f), 100f, 10);
            _gameBridgeMock.Setup(g => g.GetPlayerPosition()).Returns(position);
            _zoneServiceMock.Setup(z => z.GetZoneAtPosition(position)).Returns(zone);

            // Act
            _manager.Update();

            // Assert
            Assert.Same(zone, _manager.CurrentZone);
        }

        [Fact]
        public void Update_WhenPlayerLeavesZone_ShouldSetCurrentZoneToNull()
        {
            // Arrange - first enter a zone
            var position1 = new Vector3(100f, 200f, 30f);
            var zone = new Zone("zone1", "Downtown", new Vector3(100f, 200f, 30f), 100f, 10);
            _gameBridgeMock.Setup(g => g.GetPlayerPosition()).Returns(position1);
            _zoneServiceMock.Setup(z => z.GetZoneAtPosition(position1)).Returns(zone);
            _manager.Update();

            // Act - leave the zone
            var position2 = new Vector3(500f, 500f, 30f);
            _gameBridgeMock.Setup(g => g.GetPlayerPosition()).Returns(position2);
            _zoneServiceMock.Setup(z => z.GetZoneAtPosition(position2)).Returns((Zone?)null);
            _manager.Update();

            // Assert
            Assert.Null(_manager.CurrentZone);
        }

        [Fact]
        public void Update_WhenPlayerMovesFromOneZoneToAnother_ShouldUpdateCurrentZone()
        {
            // Arrange - enter first zone
            var position1 = new Vector3(100f, 200f, 30f);
            var zone1 = new Zone("zone1", "Downtown", new Vector3(100f, 200f, 30f), 100f, 10);
            _gameBridgeMock.Setup(g => g.GetPlayerPosition()).Returns(position1);
            _zoneServiceMock.Setup(z => z.GetZoneAtPosition(position1)).Returns(zone1);
            _manager.Update();
            Assert.Same(zone1, _manager.CurrentZone);

            // Act - move to second zone
            var position2 = new Vector3(500f, 600f, 30f);
            var zone2 = new Zone("zone2", "Industrial", new Vector3(500f, 600f, 30f), 150f, 8);
            _gameBridgeMock.Setup(g => g.GetPlayerPosition()).Returns(position2);
            _zoneServiceMock.Setup(z => z.GetZoneAtPosition(position2)).Returns(zone2);
            _manager.Update();

            // Assert
            Assert.Same(zone2, _manager.CurrentZone);
        }

        [Fact]
        public void ZoneEntered_WhenPlayerEntersZone_ShouldRaiseEvent()
        {
            // Arrange
            var position = new Vector3(100f, 200f, 30f);
            var zone = new Zone("zone1", "Downtown", new Vector3(100f, 200f, 30f), 100f, 10);
            _gameBridgeMock.Setup(g => g.GetPlayerPosition()).Returns(position);
            _zoneServiceMock.Setup(z => z.GetZoneAtPosition(position)).Returns(zone);

            Zone? enteredZone = null;
            _manager.ZoneEntered += (sender, z) => enteredZone = z;

            // Act
            _manager.Update();

            // Assert
            Assert.Same(zone, enteredZone);
        }

        [Fact]
        public void ZoneExited_WhenPlayerLeavesZone_ShouldRaiseEvent()
        {
            // Arrange - first enter a zone
            var position1 = new Vector3(100f, 200f, 30f);
            var zone = new Zone("zone1", "Downtown", new Vector3(100f, 200f, 30f), 100f, 10);
            _gameBridgeMock.Setup(g => g.GetPlayerPosition()).Returns(position1);
            _zoneServiceMock.Setup(z => z.GetZoneAtPosition(position1)).Returns(zone);
            _manager.Update();

            Zone? exitedZone = null;
            _manager.ZoneExited += (sender, z) => exitedZone = z;

            // Act - leave the zone
            var position2 = new Vector3(500f, 500f, 30f);
            _gameBridgeMock.Setup(g => g.GetPlayerPosition()).Returns(position2);
            _zoneServiceMock.Setup(z => z.GetZoneAtPosition(position2)).Returns((Zone?)null);
            _manager.Update();

            // Assert
            Assert.Same(zone, exitedZone);
        }

        [Fact]
        public void ZoneChanged_WhenPlayerMovesFromOneZoneToAnother_ShouldRaiseBothEvents()
        {
            // Arrange - enter first zone
            var position1 = new Vector3(100f, 200f, 30f);
            var zone1 = new Zone("zone1", "Downtown", new Vector3(100f, 200f, 30f), 100f, 10);
            _gameBridgeMock.Setup(g => g.GetPlayerPosition()).Returns(position1);
            _zoneServiceMock.Setup(z => z.GetZoneAtPosition(position1)).Returns(zone1);
            _manager.Update();

            Zone? exitedZone = null;
            Zone? enteredZone = null;
            _manager.ZoneExited += (sender, z) => exitedZone = z;
            _manager.ZoneEntered += (sender, z) => enteredZone = z;

            // Act - move to second zone
            var position2 = new Vector3(500f, 600f, 30f);
            var zone2 = new Zone("zone2", "Industrial", new Vector3(500f, 600f, 30f), 150f, 8);
            _gameBridgeMock.Setup(g => g.GetPlayerPosition()).Returns(position2);
            _zoneServiceMock.Setup(z => z.GetZoneAtPosition(position2)).Returns(zone2);
            _manager.Update();

            // Assert
            Assert.Same(zone1, exitedZone);
            Assert.Same(zone2, enteredZone);
        }

        [Fact]
        public void Update_WhenStayingInSameZone_ShouldNotRaiseEvents()
        {
            // Arrange - enter zone
            var position = new Vector3(100f, 200f, 30f);
            var zone = new Zone("zone1", "Downtown", new Vector3(100f, 200f, 30f), 100f, 10);
            _gameBridgeMock.Setup(g => g.GetPlayerPosition()).Returns(position);
            _zoneServiceMock.Setup(z => z.GetZoneAtPosition(position)).Returns(zone);
            _manager.Update();

            int enteredCount = 0;
            int exitedCount = 0;
            _manager.ZoneEntered += (sender, z) => enteredCount++;
            _manager.ZoneExited += (sender, z) => exitedCount++;

            // Act - update again in same zone
            _manager.Update();

            // Assert - no events should fire
            Assert.Equal(0, enteredCount);
            Assert.Equal(0, exitedCount);
        }

        [Fact]
        public void IsInEnemyTerritory_WhenNotInAnyZone_ShouldReturnFalse()
        {
            // Arrange
            var position = new Vector3(100f, 200f, 30f);
            _gameBridgeMock.Setup(g => g.GetPlayerPosition()).Returns(position);
            _zoneServiceMock.Setup(z => z.GetZoneAtPosition(position)).Returns((Zone?)null);
            _manager.Update();

            // Act & Assert
            Assert.False(_manager.IsInEnemyTerritory("faction1"));
        }

        [Fact]
        public void IsInEnemyTerritory_WhenInOwnZone_ShouldReturnFalse()
        {
            // Arrange
            var position = new Vector3(100f, 200f, 30f);
            var zone = new Zone("zone1", "Downtown", new Vector3(100f, 200f, 30f), 100f, 10);
            zone.OwnerFactionId = "faction1";
            _gameBridgeMock.Setup(g => g.GetPlayerPosition()).Returns(position);
            _zoneServiceMock.Setup(z => z.GetZoneAtPosition(position)).Returns(zone);
            _manager.Update();

            // Act & Assert
            Assert.False(_manager.IsInEnemyTerritory("faction1"));
        }

        [Fact]
        public void IsInEnemyTerritory_WhenInEnemyZone_ShouldReturnTrue()
        {
            // Arrange
            var position = new Vector3(100f, 200f, 30f);
            var zone = new Zone("zone1", "Downtown", new Vector3(100f, 200f, 30f), 100f, 10);
            zone.OwnerFactionId = "faction2";
            _gameBridgeMock.Setup(g => g.GetPlayerPosition()).Returns(position);
            _zoneServiceMock.Setup(z => z.GetZoneAtPosition(position)).Returns(zone);
            _manager.Update();

            // Act & Assert
            Assert.True(_manager.IsInEnemyTerritory("faction1"));
        }

        [Fact]
        public void IsInEnemyTerritory_WhenInNeutralZone_ShouldReturnFalse()
        {
            // Arrange
            var position = new Vector3(100f, 200f, 30f);
            var zone = new Zone("zone1", "Downtown", new Vector3(100f, 200f, 30f), 100f, 10);
            // Zone has no owner (neutral)
            _gameBridgeMock.Setup(g => g.GetPlayerPosition()).Returns(position);
            _zoneServiceMock.Setup(z => z.GetZoneAtPosition(position)).Returns(zone);
            _manager.Update();

            // Act & Assert
            Assert.False(_manager.IsInEnemyTerritory("faction1"));
        }
    }
}
