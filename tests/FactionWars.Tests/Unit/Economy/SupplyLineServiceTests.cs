using System;
using System.Collections.Generic;
using FactionWars.Core.Interfaces;
using FactionWars.Economy.Interfaces;
using FactionWars.Economy.Services;
using FactionWars.Territory.Interfaces;
using FactionWars.Territory.Models;
using Moq;
using Xunit;

namespace FactionWars.Tests.Unit.Economy
{
    /// <summary>
    /// Tests for the SupplyLineService which manages supply line connectivity and efficiency.
    /// </summary>
    public class SupplyLineServiceTests
    {
        private readonly Mock<IZoneService> _mockZoneService;
        private readonly SupplyLineService _service;

        public SupplyLineServiceTests()
        {
            _mockZoneService = new Mock<IZoneService>();
            _service = new SupplyLineService(_mockZoneService.Object);
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithValidDependencies_CreatesInstance()
        {
            // Arrange & Act
            var service = new SupplyLineService(_mockZoneService.Object);

            // Assert
            Assert.NotNull(service);
        }

        [Fact]
        public void Constructor_WithNullZoneService_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new SupplyLineService(null!));
            Assert.Equal("zoneService", exception.ParamName);
        }

        #endregion

        #region SetHeadquarters Tests

        [Fact]
        public void SetHeadquarters_WithValidInputs_ReturnsTrue()
        {
            // Arrange
            var hqZone = CreateZone("hq1", "faction1");
            _mockZoneService.Setup(z => z.GetZone("hq1")).Returns(hqZone);

            // Act
            var result = _service.SetHeadquarters("faction1", "hq1");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void SetHeadquarters_WithNullFactionId_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                _service.SetHeadquarters(null!, "zone1"));
            Assert.Equal("factionId", exception.ParamName);
        }

        [Fact]
        public void SetHeadquarters_WithEmptyFactionId_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() =>
                _service.SetHeadquarters("", "zone1"));
            Assert.Equal("factionId", exception.ParamName);
        }

        [Fact]
        public void SetHeadquarters_WithNullZoneId_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                _service.SetHeadquarters("faction1", null!));
            Assert.Equal("zoneId", exception.ParamName);
        }

        [Fact]
        public void SetHeadquarters_WithEmptyZoneId_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() =>
                _service.SetHeadquarters("faction1", ""));
            Assert.Equal("zoneId", exception.ParamName);
        }

        [Fact]
        public void SetHeadquarters_WithNonExistentZone_ReturnsFalse()
        {
            // Arrange
            _mockZoneService.Setup(z => z.GetZone("nonexistent")).Returns((Zone?)null);

            // Act
            var result = _service.SetHeadquarters("faction1", "nonexistent");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void SetHeadquarters_WithZoneNotOwnedByFaction_ReturnsFalse()
        {
            // Arrange
            var zone = CreateZone("zone1", "faction2"); // Owned by different faction
            _mockZoneService.Setup(z => z.GetZone("zone1")).Returns(zone);

            // Act
            var result = _service.SetHeadquarters("faction1", "zone1");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void SetHeadquarters_ReplacesExistingHeadquarters()
        {
            // Arrange
            var hq1 = CreateZone("hq1", "faction1");
            var hq2 = CreateZone("hq2", "faction1");
            _mockZoneService.Setup(z => z.GetZone("hq1")).Returns(hq1);
            _mockZoneService.Setup(z => z.GetZone("hq2")).Returns(hq2);

            // Act
            _service.SetHeadquarters("faction1", "hq1");
            _service.SetHeadquarters("faction1", "hq2");

            // Assert
            Assert.Equal("hq2", _service.GetHeadquarters("faction1"));
        }

        #endregion

        #region GetHeadquarters Tests

        [Fact]
        public void GetHeadquarters_WithSetHeadquarters_ReturnsZoneId()
        {
            // Arrange
            var hqZone = CreateZone("hq1", "faction1");
            _mockZoneService.Setup(z => z.GetZone("hq1")).Returns(hqZone);
            _service.SetHeadquarters("faction1", "hq1");

            // Act
            var result = _service.GetHeadquarters("faction1");

            // Assert
            Assert.Equal("hq1", result);
        }

        [Fact]
        public void GetHeadquarters_WithNoHeadquarters_ReturnsNull()
        {
            // Act
            var result = _service.GetHeadquarters("faction1");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetHeadquarters_WithNullFactionId_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                _service.GetHeadquarters(null!));
            Assert.Equal("factionId", exception.ParamName);
        }

        [Fact]
        public void GetHeadquarters_WithEmptyFactionId_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() =>
                _service.GetHeadquarters(""));
            Assert.Equal("factionId", exception.ParamName);
        }

        #endregion

        #region ClearHeadquarters Tests

        [Fact]
        public void ClearHeadquarters_WithSetHeadquarters_ClearsIt()
        {
            // Arrange
            var hqZone = CreateZone("hq1", "faction1");
            _mockZoneService.Setup(z => z.GetZone("hq1")).Returns(hqZone);
            _service.SetHeadquarters("faction1", "hq1");

            // Act
            _service.ClearHeadquarters("faction1");

            // Assert
            Assert.Null(_service.GetHeadquarters("faction1"));
        }

        [Fact]
        public void ClearHeadquarters_WithNoHeadquarters_DoesNotThrow()
        {
            // Act & Assert (no exception)
            _service.ClearHeadquarters("faction1");
        }

        [Fact]
        public void ClearHeadquarters_WithNullFactionId_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                _service.ClearHeadquarters(null!));
            Assert.Equal("factionId", exception.ParamName);
        }

        #endregion

        #region IsConnectedToHeadquarters Tests

        [Fact]
        public void IsConnectedToHeadquarters_WithZoneAtHeadquarters_ReturnsTrue()
        {
            // Arrange
            var hqZone = CreateZone("hq1", "faction1");
            _mockZoneService.Setup(z => z.GetZone("hq1")).Returns(hqZone);
            _service.SetHeadquarters("faction1", "hq1");

            // Act
            var result = _service.IsConnectedToHeadquarters("faction1", "hq1");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsConnectedToHeadquarters_WithNoHeadquarters_ReturnsFalse()
        {
            // Arrange
            var zone = CreateZone("zone1", "faction1");
            _mockZoneService.Setup(z => z.GetZone("zone1")).Returns(zone);

            // Act
            var result = _service.IsConnectedToHeadquarters("faction1", "zone1");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsConnectedToHeadquarters_WithConnectedZone_ReturnsTrue()
        {
            // Arrange
            var hqZone = CreateZone("hq1", "faction1");
            var zone2 = CreateZone("zone2", "faction1");
            _mockZoneService.Setup(z => z.GetZone("hq1")).Returns(hqZone);
            _mockZoneService.Setup(z => z.GetZone("zone2")).Returns(zone2);
            _mockZoneService.Setup(z => z.GetConnectedZonesByOwner("hq1", "faction1"))
                .Returns(new[] { zone2 });
            _service.SetHeadquarters("faction1", "hq1");

            // Act
            var result = _service.IsConnectedToHeadquarters("faction1", "zone2");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsConnectedToHeadquarters_WithDisconnectedZone_ReturnsFalse()
        {
            // Arrange
            var hqZone = CreateZone("hq1", "faction1");
            var zone2 = CreateZone("zone2", "faction1");
            _mockZoneService.Setup(z => z.GetZone("hq1")).Returns(hqZone);
            _mockZoneService.Setup(z => z.GetZone("zone2")).Returns(zone2);
            _mockZoneService.Setup(z => z.GetConnectedZonesByOwner("hq1", "faction1"))
                .Returns(Array.Empty<Zone>()); // Not connected
            _service.SetHeadquarters("faction1", "hq1");

            // Act
            var result = _service.IsConnectedToHeadquarters("faction1", "zone2");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsConnectedToHeadquarters_WithNonExistentZone_ReturnsFalse()
        {
            // Arrange
            var hqZone = CreateZone("hq1", "faction1");
            _mockZoneService.Setup(z => z.GetZone("hq1")).Returns(hqZone);
            _mockZoneService.Setup(z => z.GetZone("nonexistent")).Returns((Zone?)null);
            _mockZoneService.Setup(z => z.GetConnectedZonesByOwner("hq1", "faction1"))
                .Returns(Array.Empty<Zone>());
            _service.SetHeadquarters("faction1", "hq1");

            // Act
            var result = _service.IsConnectedToHeadquarters("faction1", "nonexistent");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsConnectedToHeadquarters_WithNullFactionId_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                _service.IsConnectedToHeadquarters(null!, "zone1"));
            Assert.Equal("factionId", exception.ParamName);
        }

        [Fact]
        public void IsConnectedToHeadquarters_WithNullZoneId_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                _service.IsConnectedToHeadquarters("faction1", null!));
            Assert.Equal("zoneId", exception.ParamName);
        }

        #endregion

        #region GetSupplyLineEfficiency Tests

        [Fact]
        public void GetSupplyLineEfficiency_WithZoneAtHeadquarters_ReturnsFullEfficiency()
        {
            // Arrange
            var hqZone = CreateZone("hq1", "faction1");
            _mockZoneService.Setup(z => z.GetZone("hq1")).Returns(hqZone);
            _service.SetHeadquarters("faction1", "hq1");

            // Act
            var result = _service.GetSupplyLineEfficiency("faction1", "hq1");

            // Assert
            Assert.Equal(1.0f, result);
        }

        [Fact]
        public void GetSupplyLineEfficiency_WithConnectedZone_ReturnsFullEfficiency()
        {
            // Arrange
            var hqZone = CreateZone("hq1", "faction1");
            var zone2 = CreateZone("zone2", "faction1");
            _mockZoneService.Setup(z => z.GetZone("hq1")).Returns(hqZone);
            _mockZoneService.Setup(z => z.GetZone("zone2")).Returns(zone2);
            _mockZoneService.Setup(z => z.GetConnectedZonesByOwner("hq1", "faction1"))
                .Returns(new[] { zone2 });
            _service.SetHeadquarters("faction1", "hq1");

            // Act
            var result = _service.GetSupplyLineEfficiency("faction1", "zone2");

            // Assert
            Assert.Equal(1.0f, result);
        }

        [Fact]
        public void GetSupplyLineEfficiency_WithDisconnectedZone_ReturnsReducedEfficiency()
        {
            // Arrange
            var hqZone = CreateZone("hq1", "faction1");
            var zone2 = CreateZone("zone2", "faction1");
            _mockZoneService.Setup(z => z.GetZone("hq1")).Returns(hqZone);
            _mockZoneService.Setup(z => z.GetZone("zone2")).Returns(zone2);
            _mockZoneService.Setup(z => z.GetConnectedZonesByOwner("hq1", "faction1"))
                .Returns(Array.Empty<Zone>());
            _service.SetHeadquarters("faction1", "hq1");

            // Act
            var result = _service.GetSupplyLineEfficiency("faction1", "zone2");

            // Assert
            Assert.Equal(SupplyLineService.DisconnectedEfficiency, result);
        }

        [Fact]
        public void GetSupplyLineEfficiency_WithNoHeadquarters_ReturnsFullEfficiency()
        {
            // When no HQ is set, all zones are considered self-sufficient
            // Arrange
            var zone = CreateZone("zone1", "faction1");
            _mockZoneService.Setup(z => z.GetZone("zone1")).Returns(zone);

            // Act
            var result = _service.GetSupplyLineEfficiency("faction1", "zone1");

            // Assert
            Assert.Equal(1.0f, result);
        }

        [Fact]
        public void GetSupplyLineEfficiency_WithNullFactionId_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                _service.GetSupplyLineEfficiency(null!, "zone1"));
            Assert.Equal("factionId", exception.ParamName);
        }

        [Fact]
        public void GetSupplyLineEfficiency_WithNullZoneId_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                _service.GetSupplyLineEfficiency("faction1", null!));
            Assert.Equal("zoneId", exception.ParamName);
        }

        [Fact]
        public void DisconnectedEfficiency_DefaultValue_IsFiftyPercent()
        {
            // Assert
            Assert.Equal(0.5f, SupplyLineService.DisconnectedEfficiency);
        }

        #endregion

        #region GetConnectedZones Tests

        [Fact]
        public void GetConnectedZones_WithHeadquarters_ReturnsAllConnectedZones()
        {
            // Arrange
            var hqZone = CreateZone("hq1", "faction1");
            var zone2 = CreateZone("zone2", "faction1");
            var zone3 = CreateZone("zone3", "faction1");
            _mockZoneService.Setup(z => z.GetZone("hq1")).Returns(hqZone);
            _mockZoneService.Setup(z => z.GetConnectedZonesByOwner("hq1", "faction1"))
                .Returns(new[] { zone2, zone3 });
            _service.SetHeadquarters("faction1", "hq1");

            // Act
            var result = _service.GetConnectedZones("faction1");

            // Assert
            Assert.Equal(3, ((List<Zone>)result).Count); // HQ + 2 connected
            Assert.Contains(hqZone, result);
            Assert.Contains(zone2, result);
            Assert.Contains(zone3, result);
        }

        [Fact]
        public void GetConnectedZones_WithNoHeadquarters_ReturnsEmpty()
        {
            // Act
            var result = _service.GetConnectedZones("faction1");

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void GetConnectedZones_WithNullFactionId_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                _service.GetConnectedZones(null!));
            Assert.Equal("factionId", exception.ParamName);
        }

        #endregion

        #region GetDisconnectedZones Tests

        [Fact]
        public void GetDisconnectedZones_WithAllConnected_ReturnsEmpty()
        {
            // Arrange
            var hqZone = CreateZone("hq1", "faction1");
            var zone2 = CreateZone("zone2", "faction1");
            _mockZoneService.Setup(z => z.GetZone("hq1")).Returns(hqZone);
            _mockZoneService.Setup(z => z.GetZonesByOwner("faction1"))
                .Returns(new[] { hqZone, zone2 });
            _mockZoneService.Setup(z => z.GetConnectedZonesByOwner("hq1", "faction1"))
                .Returns(new[] { zone2 });
            _service.SetHeadquarters("faction1", "hq1");

            // Act
            var result = _service.GetDisconnectedZones("faction1");

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void GetDisconnectedZones_WithSomeDisconnected_ReturnsDisconnectedZones()
        {
            // Arrange
            var hqZone = CreateZone("hq1", "faction1");
            var zone2 = CreateZone("zone2", "faction1"); // Connected
            var zone3 = CreateZone("zone3", "faction1"); // Disconnected
            _mockZoneService.Setup(z => z.GetZone("hq1")).Returns(hqZone);
            _mockZoneService.Setup(z => z.GetZonesByOwner("faction1"))
                .Returns(new[] { hqZone, zone2, zone3 });
            _mockZoneService.Setup(z => z.GetConnectedZonesByOwner("hq1", "faction1"))
                .Returns(new[] { zone2 }); // zone3 not connected
            _service.SetHeadquarters("faction1", "hq1");

            // Act
            var result = _service.GetDisconnectedZones("faction1");

            // Assert
            Assert.Single(result);
            Assert.Contains(zone3, result);
        }

        [Fact]
        public void GetDisconnectedZones_WithNoHeadquarters_ReturnsEmpty()
        {
            // When no HQ, all zones are considered "connected" (self-sufficient)
            // Arrange
            var zone = CreateZone("zone1", "faction1");
            _mockZoneService.Setup(z => z.GetZonesByOwner("faction1"))
                .Returns(new[] { zone });

            // Act
            var result = _service.GetDisconnectedZones("faction1");

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void GetDisconnectedZones_WithNullFactionId_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                _service.GetDisconnectedZones(null!));
            Assert.Equal("factionId", exception.ParamName);
        }

        #endregion

        #region HasSupplyLine Tests

        [Fact]
        public void HasSupplyLine_WithNoHeadquarters_ReturnsTrue()
        {
            // Self-sufficient mode
            // Arrange
            var zone = CreateZone("zone1", "faction1");
            _mockZoneService.Setup(z => z.GetZone("zone1")).Returns(zone);

            // Act
            var result = _service.HasSupplyLine("faction1", "zone1");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void HasSupplyLine_AtHeadquarters_ReturnsTrue()
        {
            // Arrange
            var hqZone = CreateZone("hq1", "faction1");
            _mockZoneService.Setup(z => z.GetZone("hq1")).Returns(hqZone);
            _service.SetHeadquarters("faction1", "hq1");

            // Act
            var result = _service.HasSupplyLine("faction1", "hq1");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void HasSupplyLine_WithConnectedZone_ReturnsTrue()
        {
            // Arrange
            var hqZone = CreateZone("hq1", "faction1");
            var zone2 = CreateZone("zone2", "faction1");
            _mockZoneService.Setup(z => z.GetZone("hq1")).Returns(hqZone);
            _mockZoneService.Setup(z => z.GetZone("zone2")).Returns(zone2);
            _mockZoneService.Setup(z => z.GetConnectedZonesByOwner("hq1", "faction1"))
                .Returns(new[] { zone2 });
            _service.SetHeadquarters("faction1", "hq1");

            // Act
            var result = _service.HasSupplyLine("faction1", "zone2");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void HasSupplyLine_WithDisconnectedZone_ReturnsFalse()
        {
            // Arrange
            var hqZone = CreateZone("hq1", "faction1");
            var zone2 = CreateZone("zone2", "faction1");
            _mockZoneService.Setup(z => z.GetZone("hq1")).Returns(hqZone);
            _mockZoneService.Setup(z => z.GetZone("zone2")).Returns(zone2);
            _mockZoneService.Setup(z => z.GetConnectedZonesByOwner("hq1", "faction1"))
                .Returns(Array.Empty<Zone>());
            _service.SetHeadquarters("faction1", "hq1");

            // Act
            var result = _service.HasSupplyLine("faction1", "zone2");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void HasSupplyLine_WithNullFactionId_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                _service.HasSupplyLine(null!, "zone1"));
            Assert.Equal("factionId", exception.ParamName);
        }

        [Fact]
        public void HasSupplyLine_WithNullZoneId_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                _service.HasSupplyLine("faction1", null!));
            Assert.Equal("zoneId", exception.ParamName);
        }

        #endregion

        #region Headquarters Lost/Changed Scenarios

        [Fact]
        public void SetHeadquarters_WhenHQZoneLostOwnership_CannotSetAsDifferentFaction()
        {
            // Arrange
            var zone = CreateZone("zone1", "faction2"); // Different faction owns it
            _mockZoneService.Setup(z => z.GetZone("zone1")).Returns(zone);

            // Act
            var result = _service.SetHeadquarters("faction1", "zone1");

            // Assert
            Assert.False(result);
        }

        #endregion

        #region Multiple Factions Tests

        [Fact]
        public void SetHeadquarters_MultipleFactions_MaintainsSeparateHeadquarters()
        {
            // Arrange
            var hq1 = CreateZone("hq1", "faction1");
            var hq2 = CreateZone("hq2", "faction2");
            _mockZoneService.Setup(z => z.GetZone("hq1")).Returns(hq1);
            _mockZoneService.Setup(z => z.GetZone("hq2")).Returns(hq2);

            // Act
            _service.SetHeadquarters("faction1", "hq1");
            _service.SetHeadquarters("faction2", "hq2");

            // Assert
            Assert.Equal("hq1", _service.GetHeadquarters("faction1"));
            Assert.Equal("hq2", _service.GetHeadquarters("faction2"));
        }

        #endregion

        #region Helper Methods

        private Zone CreateZone(string id, string? ownerId, int strategicValue = 1)
        {
            var zone = new Zone(id, $"Test Zone {id}", new Vector3(0, 0, 0), 100f, strategicValue);
            zone.OwnerFactionId = ownerId;
            return zone;
        }

        #endregion
    }
}
