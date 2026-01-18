using FactionWars.Core.Interfaces;
using FactionWars.Core.Utils;
using FactionWars.Factions.Interfaces;
using FactionWars.Factions.Models;
using FactionWars.Territory.Interfaces;
using FactionWars.Territory.Models;
using FactionWars.UI.Interfaces;
using FactionWars.UI.Services;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace FactionWars.Tests.Unit.UI
{
    /// <summary>
    /// Tests for map blip management service.
    /// Following TDD - these tests define the expected behavior for the MapBlipService.
    /// </summary>
    public class MapBlipServiceTests
    {
        #region Test Setup

        private MockGameBridge _gameBridge;
        private Mock<IZoneService> _zoneServiceMock;
        private Mock<IFactionRepository> _factionRepositoryMock;

        public MapBlipServiceTests()
        {
            _gameBridge = new MockGameBridge();
            _zoneServiceMock = new Mock<IZoneService>();
            _factionRepositoryMock = new Mock<IFactionRepository>();
        }

        private IMapBlipService CreateService()
        {
            return new MapBlipService(_gameBridge, _zoneServiceMock.Object, _factionRepositoryMock.Object);
        }

        private Zone CreateTestZone(string id, string name, Vector3 center, string? ownerFactionId = null)
        {
            var zone = new Zone(id, name, center, 150f, 5);
            zone.OwnerFactionId = ownerFactionId;
            return zone;
        }

        private Faction CreateTestFaction(string id, string name, FactionColor color)
        {
            return new Faction(id, name, null, "", color);
        }

        #endregion

        #region Constructor Validation

        [Fact]
        public void Constructor_ShouldThrowForNullGameBridge()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new MapBlipService(null!, _zoneServiceMock.Object, _factionRepositoryMock.Object));
        }

        [Fact]
        public void Constructor_ShouldThrowForNullZoneService()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new MapBlipService(_gameBridge, null!, _factionRepositoryMock.Object));
        }

        [Fact]
        public void Constructor_ShouldThrowForNullFactionRepository()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new MapBlipService(_gameBridge, _zoneServiceMock.Object, null!));
        }

        #endregion

        #region CreateBlipForZone

        [Fact]
        public void CreateBlipForZone_ShouldCreateBlipAtZoneCenter()
        {
            // Arrange
            var service = CreateService();
            var center = new Vector3(100, 200, 50);
            var zone = CreateTestZone("zone_1", "Test Zone", center);

            // Act
            var blipHandle = service.CreateBlipForZone(zone);

            // Assert
            Assert.True(blipHandle > 0);
            Assert.True(_gameBridge.BlipExists(blipHandle));
        }

        [Fact]
        public void CreateBlipForZone_ShouldThrowForNullZone()
        {
            // Arrange
            var service = CreateService();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => service.CreateBlipForZone(null!));
        }

        [Fact]
        public void CreateBlipForZone_ShouldSetNeutralColorForUnownedZone()
        {
            // Arrange
            var service = CreateService();
            var zone = CreateTestZone("zone_1", "Test Zone", new Vector3(100, 200, 50));

            // Act
            var blipHandle = service.CreateBlipForZone(zone);

            // Assert
            var color = _gameBridge.GetBlipColor(blipHandle);
            Assert.Equal(BlipColor.White, color); // Neutral zones are white
        }

        [Fact]
        public void CreateBlipForZone_ShouldSetMichaelColorForMichaelOwnedZone()
        {
            // Arrange
            var service = CreateService();
            var zone = CreateTestZone("zone_1", "Test Zone", new Vector3(100, 200, 50), "faction_michael");
            var michaelFaction = CreateTestFaction("faction_michael", "De Santa Family", new FactionColor(0, 100, 255));
            _factionRepositoryMock.Setup(r => r.GetById("faction_michael")).Returns(michaelFaction);

            // Act
            var blipHandle = service.CreateBlipForZone(zone);

            // Assert
            var color = _gameBridge.GetBlipColor(blipHandle);
            Assert.Equal(BlipColor.MichaelBlue, color);
        }

        [Fact]
        public void CreateBlipForZone_ShouldSetTrevorColorForTrevorOwnedZone()
        {
            // Arrange
            var service = CreateService();
            var zone = CreateTestZone("zone_1", "Test Zone", new Vector3(100, 200, 50), "faction_trevor");
            var trevorFaction = CreateTestFaction("faction_trevor", "Trevor Philips Industries", new FactionColor(255, 128, 0));
            _factionRepositoryMock.Setup(r => r.GetById("faction_trevor")).Returns(trevorFaction);

            // Act
            var blipHandle = service.CreateBlipForZone(zone);

            // Assert
            var color = _gameBridge.GetBlipColor(blipHandle);
            Assert.Equal(BlipColor.TrevorOrange, color);
        }

        [Fact]
        public void CreateBlipForZone_ShouldSetFranklinColorForFranklinOwnedZone()
        {
            // Arrange
            var service = CreateService();
            var zone = CreateTestZone("zone_1", "Test Zone", new Vector3(100, 200, 50), "faction_franklin");
            var franklinFaction = CreateTestFaction("faction_franklin", "Clinton Organization", new FactionColor(0, 200, 100));
            _factionRepositoryMock.Setup(r => r.GetById("faction_franklin")).Returns(franklinFaction);

            // Act
            var blipHandle = service.CreateBlipForZone(zone);

            // Assert
            var color = _gameBridge.GetBlipColor(blipHandle);
            Assert.Equal(BlipColor.FranklinGreen, color);
        }

        [Fact]
        public void CreateBlipForZone_ShouldTrackBlipForZone()
        {
            // Arrange
            var service = CreateService();
            var zone = CreateTestZone("zone_1", "Test Zone", new Vector3(100, 200, 50));

            // Act
            var blipHandle = service.CreateBlipForZone(zone);

            // Assert
            Assert.True(service.HasBlipForZone(zone.Id));
            Assert.Equal(blipHandle, service.GetBlipHandle(zone.Id));
        }

        [Fact]
        public void CreateBlipForZone_ShouldReturnExistingBlipIfAlreadyCreated()
        {
            // Arrange
            var service = CreateService();
            var zone = CreateTestZone("zone_1", "Test Zone", new Vector3(100, 200, 50));

            // Act
            var firstHandle = service.CreateBlipForZone(zone);
            var secondHandle = service.CreateBlipForZone(zone);

            // Assert
            Assert.Equal(firstHandle, secondHandle);
        }

        #endregion

        #region RemoveBlipForZone

        [Fact]
        public void RemoveBlipForZone_ShouldRemoveBlipFromGame()
        {
            // Arrange
            var service = CreateService();
            var zone = CreateTestZone("zone_1", "Test Zone", new Vector3(100, 200, 50));
            var blipHandle = service.CreateBlipForZone(zone);

            // Act
            var result = service.RemoveBlipForZone(zone.Id);

            // Assert
            Assert.True(result);
            Assert.False(_gameBridge.BlipExists(blipHandle));
        }

        [Fact]
        public void RemoveBlipForZone_ShouldReturnFalseIfNoBlipExists()
        {
            // Arrange
            var service = CreateService();

            // Act
            var result = service.RemoveBlipForZone("nonexistent_zone");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void RemoveBlipForZone_ShouldNoLongerTrackZone()
        {
            // Arrange
            var service = CreateService();
            var zone = CreateTestZone("zone_1", "Test Zone", new Vector3(100, 200, 50));
            service.CreateBlipForZone(zone);

            // Act
            service.RemoveBlipForZone(zone.Id);

            // Assert
            Assert.False(service.HasBlipForZone(zone.Id));
        }

        [Fact]
        public void RemoveBlipForZone_ShouldThrowForNullZoneId()
        {
            // Arrange
            var service = CreateService();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => service.RemoveBlipForZone(null!));
        }

        #endregion

        #region UpdateBlipColor

        [Fact]
        public void UpdateBlipColor_ShouldChangeBlipColorForFactionChange()
        {
            // Arrange
            var service = CreateService();
            var zone = CreateTestZone("zone_1", "Test Zone", new Vector3(100, 200, 50));
            var blipHandle = service.CreateBlipForZone(zone);

            var michaelFaction = CreateTestFaction("faction_michael", "De Santa Family", new FactionColor(0, 100, 255));
            _factionRepositoryMock.Setup(r => r.GetById("faction_michael")).Returns(michaelFaction);

            // Act
            var result = service.UpdateBlipColor(zone.Id, "faction_michael");

            // Assert
            Assert.True(result);
            var color = _gameBridge.GetBlipColor(blipHandle);
            Assert.Equal(BlipColor.MichaelBlue, color);
        }

        [Fact]
        public void UpdateBlipColor_ShouldSetNeutralColorForNullFaction()
        {
            // Arrange
            var service = CreateService();
            var zone = CreateTestZone("zone_1", "Test Zone", new Vector3(100, 200, 50), "faction_michael");
            var michaelFaction = CreateTestFaction("faction_michael", "De Santa Family", new FactionColor(0, 100, 255));
            _factionRepositoryMock.Setup(r => r.GetById("faction_michael")).Returns(michaelFaction);

            var blipHandle = service.CreateBlipForZone(zone);

            // Act
            var result = service.UpdateBlipColor(zone.Id, null);

            // Assert
            Assert.True(result);
            var color = _gameBridge.GetBlipColor(blipHandle);
            Assert.Equal(BlipColor.White, color);
        }

        [Fact]
        public void UpdateBlipColor_ShouldReturnFalseIfNoBlipExists()
        {
            // Arrange
            var service = CreateService();

            // Act
            var result = service.UpdateBlipColor("nonexistent_zone", "faction_michael");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void UpdateBlipColor_ShouldThrowForNullZoneId()
        {
            // Arrange
            var service = CreateService();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => service.UpdateBlipColor(null!, "faction_michael"));
        }

        #endregion

        #region CreateBlipsForAllZones

        [Fact]
        public void CreateBlipsForAllZones_ShouldCreateBlipsForAllZones()
        {
            // Arrange
            var service = CreateService();
            var zones = new List<Zone>
            {
                CreateTestZone("zone_1", "Zone 1", new Vector3(100, 100, 50)),
                CreateTestZone("zone_2", "Zone 2", new Vector3(200, 200, 50)),
                CreateTestZone("zone_3", "Zone 3", new Vector3(300, 300, 50))
            };
            _zoneServiceMock.Setup(s => s.GetAllZones()).Returns(zones);

            // Act
            var count = service.CreateBlipsForAllZones();

            // Assert
            Assert.Equal(3, count);
            Assert.True(service.HasBlipForZone("zone_1"));
            Assert.True(service.HasBlipForZone("zone_2"));
            Assert.True(service.HasBlipForZone("zone_3"));
        }

        [Fact]
        public void CreateBlipsForAllZones_ShouldNotDuplicateExistingBlips()
        {
            // Arrange
            var service = CreateService();
            var zones = new List<Zone>
            {
                CreateTestZone("zone_1", "Zone 1", new Vector3(100, 100, 50)),
                CreateTestZone("zone_2", "Zone 2", new Vector3(200, 200, 50))
            };
            _zoneServiceMock.Setup(s => s.GetAllZones()).Returns(zones);

            // Create blip for first zone manually
            service.CreateBlipForZone(zones[0]);

            // Act
            var count = service.CreateBlipsForAllZones();

            // Assert - should return 2 (total blips, not new ones)
            Assert.Equal(2, count);
        }

        #endregion

        #region RemoveAllBlips

        [Fact]
        public void RemoveAllBlips_ShouldRemoveAllTrackedBlips()
        {
            // Arrange
            var service = CreateService();
            var zones = new List<Zone>
            {
                CreateTestZone("zone_1", "Zone 1", new Vector3(100, 100, 50)),
                CreateTestZone("zone_2", "Zone 2", new Vector3(200, 200, 50))
            };

            var handles = zones.Select(z => service.CreateBlipForZone(z)).ToList();

            // Act
            service.RemoveAllBlips();

            // Assert
            foreach (var handle in handles)
            {
                Assert.False(_gameBridge.BlipExists(handle));
            }
            Assert.False(service.HasBlipForZone("zone_1"));
            Assert.False(service.HasBlipForZone("zone_2"));
        }

        [Fact]
        public void RemoveAllBlips_ShouldHandleEmptyBlipList()
        {
            // Arrange
            var service = CreateService();

            // Act & Assert - should not throw
            service.RemoveAllBlips();
        }

        #endregion

        #region GetBlipHandle

        [Fact]
        public void GetBlipHandle_ShouldReturnHandleForTrackedZone()
        {
            // Arrange
            var service = CreateService();
            var zone = CreateTestZone("zone_1", "Test Zone", new Vector3(100, 200, 50));
            var expectedHandle = service.CreateBlipForZone(zone);

            // Act
            var actualHandle = service.GetBlipHandle(zone.Id);

            // Assert
            Assert.Equal(expectedHandle, actualHandle);
        }

        [Fact]
        public void GetBlipHandle_ShouldReturnNullForUntrackedZone()
        {
            // Arrange
            var service = CreateService();

            // Act
            var handle = service.GetBlipHandle("nonexistent_zone");

            // Assert
            Assert.Null(handle);
        }

        [Fact]
        public void GetBlipHandle_ShouldThrowForNullZoneId()
        {
            // Arrange
            var service = CreateService();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => service.GetBlipHandle(null!));
        }

        #endregion

        #region HasBlipForZone

        [Fact]
        public void HasBlipForZone_ShouldReturnTrueForTrackedZone()
        {
            // Arrange
            var service = CreateService();
            var zone = CreateTestZone("zone_1", "Test Zone", new Vector3(100, 200, 50));
            service.CreateBlipForZone(zone);

            // Act
            var result = service.HasBlipForZone(zone.Id);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void HasBlipForZone_ShouldReturnFalseForUntrackedZone()
        {
            // Arrange
            var service = CreateService();

            // Act
            var result = service.HasBlipForZone("nonexistent_zone");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void HasBlipForZone_ShouldThrowForNullZoneId()
        {
            // Arrange
            var service = CreateService();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => service.HasBlipForZone(null!));
        }

        #endregion

        #region GetBlipColorForFaction

        [Fact]
        public void GetBlipColorForFaction_ShouldReturnWhiteForNullFaction()
        {
            // Arrange
            var service = CreateService();

            // Act
            var color = service.GetBlipColorForFaction(null);

            // Assert
            Assert.Equal(BlipColor.White, color);
        }

        [Fact]
        public void GetBlipColorForFaction_ShouldReturnMichaelBlueForMichael()
        {
            // Arrange
            var service = CreateService();
            var michaelFaction = CreateTestFaction("faction_michael", "De Santa Family", new FactionColor(0, 100, 255));
            _factionRepositoryMock.Setup(r => r.GetById("faction_michael")).Returns(michaelFaction);

            // Act
            var color = service.GetBlipColorForFaction("faction_michael");

            // Assert
            Assert.Equal(BlipColor.MichaelBlue, color);
        }

        [Fact]
        public void GetBlipColorForFaction_ShouldReturnTrevorOrangeForTrevor()
        {
            // Arrange
            var service = CreateService();
            var trevorFaction = CreateTestFaction("faction_trevor", "Trevor Philips Industries", new FactionColor(255, 128, 0));
            _factionRepositoryMock.Setup(r => r.GetById("faction_trevor")).Returns(trevorFaction);

            // Act
            var color = service.GetBlipColorForFaction("faction_trevor");

            // Assert
            Assert.Equal(BlipColor.TrevorOrange, color);
        }

        [Fact]
        public void GetBlipColorForFaction_ShouldReturnFranklinGreenForFranklin()
        {
            // Arrange
            var service = CreateService();
            var franklinFaction = CreateTestFaction("faction_franklin", "Clinton Organization", new FactionColor(0, 200, 100));
            _factionRepositoryMock.Setup(r => r.GetById("faction_franklin")).Returns(franklinFaction);

            // Act
            var color = service.GetBlipColorForFaction("faction_franklin");

            // Assert
            Assert.Equal(BlipColor.FranklinGreen, color);
        }

        [Fact]
        public void GetBlipColorForFaction_ShouldReturnDefaultColorForUnknownFaction()
        {
            // Arrange
            var service = CreateService();
            _factionRepositoryMock.Setup(r => r.GetById("unknown_faction")).Returns((Faction?)null);

            // Act
            var color = service.GetBlipColorForFaction("unknown_faction");

            // Assert
            Assert.Equal(BlipColor.White, color); // Default to neutral
        }

        #endregion

        #region SyncBlipsWithZones

        [Fact]
        public void SyncBlipsWithZones_ShouldUpdateColorsForAllTrackedZones()
        {
            // Arrange
            var service = CreateService();
            var michaelFaction = CreateTestFaction("faction_michael", "De Santa Family", new FactionColor(0, 100, 255));
            _factionRepositoryMock.Setup(r => r.GetById("faction_michael")).Returns(michaelFaction);

            // Create zone and blip initially without owner
            var zone = CreateTestZone("zone_1", "Test Zone", new Vector3(100, 200, 50));
            var blipHandle = service.CreateBlipForZone(zone);

            // Update zone to have owner
            zone.OwnerFactionId = "faction_michael";
            _zoneServiceMock.Setup(s => s.GetZone("zone_1")).Returns(zone);

            // Act
            service.SyncBlipWithZone("zone_1");

            // Assert
            var color = _gameBridge.GetBlipColor(blipHandle);
            Assert.Equal(BlipColor.MichaelBlue, color);
        }

        [Fact]
        public void SyncBlipWithZone_ShouldDoNothingForUntrackedZone()
        {
            // Arrange
            var service = CreateService();
            var zone = CreateTestZone("zone_1", "Test Zone", new Vector3(100, 200, 50));
            _zoneServiceMock.Setup(s => s.GetZone("zone_1")).Returns(zone);

            // Act & Assert - should not throw
            service.SyncBlipWithZone("zone_1");
        }

        [Fact]
        public void SyncBlipWithZone_ShouldThrowForNullZoneId()
        {
            // Arrange
            var service = CreateService();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => service.SyncBlipWithZone(null!));
        }

        #endregion

        #region GetTrackedZoneCount

        [Fact]
        public void GetTrackedZoneCount_ShouldReturnZero_WhenNoBlipsCreated()
        {
            // Arrange
            var service = CreateService();

            // Act
            var count = service.GetTrackedZoneCount();

            // Assert
            Assert.Equal(0, count);
        }

        [Fact]
        public void GetTrackedZoneCount_ShouldReturnCorrectCount()
        {
            // Arrange
            var service = CreateService();
            service.CreateBlipForZone(CreateTestZone("zone_1", "Zone 1", new Vector3(100, 100, 50)));
            service.CreateBlipForZone(CreateTestZone("zone_2", "Zone 2", new Vector3(200, 200, 50)));
            service.CreateBlipForZone(CreateTestZone("zone_3", "Zone 3", new Vector3(300, 300, 50)));

            // Act
            var count = service.GetTrackedZoneCount();

            // Assert
            Assert.Equal(3, count);
        }

        [Fact]
        public void GetTrackedZoneCount_ShouldDecreaseAfterRemoval()
        {
            // Arrange
            var service = CreateService();
            service.CreateBlipForZone(CreateTestZone("zone_1", "Zone 1", new Vector3(100, 100, 50)));
            service.CreateBlipForZone(CreateTestZone("zone_2", "Zone 2", new Vector3(200, 200, 50)));

            // Act
            service.RemoveBlipForZone("zone_1");
            var count = service.GetTrackedZoneCount();

            // Assert
            Assert.Equal(1, count);
        }

        #endregion
    }
}
