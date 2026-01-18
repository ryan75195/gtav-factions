using FactionWars.Core.Interfaces;
using FactionWars.Territory.Interfaces;
using FactionWars.Territory.Models;
using FactionWars.Territory.Repositories;
using FactionWars.Territory.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace FactionWars.Tests.Unit.Territory
{
    /// <summary>
    /// Tests for zone connectivity and adjacency calculation.
    /// Adjacency is determined by whether zones are close enough that
    /// their boundaries touch or overlap.
    /// </summary>
    public class ZoneAdjacencyTests
    {
        private readonly IZoneRepository _repository;
        private readonly IZoneService _service;

        public ZoneAdjacencyTests()
        {
            _repository = new InMemoryZoneRepository();
            _service = new ZoneService(_repository);
        }

        #region GetAdjacentZones Tests

        [Fact]
        public void GetAdjacentZones_ShouldReturnEmptyWhenZoneNotFound()
        {
            // Act
            var result = _service.GetAdjacentZones("nonexistent");

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void GetAdjacentZones_ShouldThrowOnNullZoneId()
        {
            Assert.Throws<ArgumentNullException>(() => _service.GetAdjacentZones(null!));
        }

        [Fact]
        public void GetAdjacentZones_ShouldReturnEmptyWhenNoZonesAreAdjacent()
        {
            // Arrange - two zones far apart
            var zone1 = new Zone("zone_1", "Downtown", new Vector3(0, 0, 0), 50f);
            var zone2 = new Zone("zone_2", "Uptown", new Vector3(500, 500, 0), 50f);
            _repository.Add(zone1);
            _repository.Add(zone2);

            // Act
            var result = _service.GetAdjacentZones("zone_1");

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void GetAdjacentZones_ShouldReturnAdjacentZonesWhenOverlapping()
        {
            // Arrange - two overlapping zones
            var zone1 = new Zone("zone_1", "Downtown", new Vector3(0, 0, 0), 100f);
            var zone2 = new Zone("zone_2", "Uptown", new Vector3(150, 0, 0), 100f);
            _repository.Add(zone1);
            _repository.Add(zone2);

            // Act
            var result = _service.GetAdjacentZones("zone_1").ToList();

            // Assert
            Assert.Single(result);
            Assert.Equal("zone_2", result[0].Id);
        }

        [Fact]
        public void GetAdjacentZones_ShouldReturnAdjacentZonesWhenTouching()
        {
            // Arrange - two zones exactly touching (sum of radii = distance between centers)
            var zone1 = new Zone("zone_1", "Downtown", new Vector3(0, 0, 0), 100f);
            var zone2 = new Zone("zone_2", "Uptown", new Vector3(200, 0, 0), 100f);
            _repository.Add(zone1);
            _repository.Add(zone2);

            // Act
            var result = _service.GetAdjacentZones("zone_1").ToList();

            // Assert
            Assert.Single(result);
            Assert.Equal("zone_2", result[0].Id);
        }

        [Fact]
        public void GetAdjacentZones_ShouldNotIncludeZonesJustBeyondTouching()
        {
            // Arrange - two zones with a small gap between them
            var zone1 = new Zone("zone_1", "Downtown", new Vector3(0, 0, 0), 100f);
            var zone2 = new Zone("zone_2", "Uptown", new Vector3(201, 0, 0), 100f); // Just beyond touching
            _repository.Add(zone1);
            _repository.Add(zone2);

            // Act
            var result = _service.GetAdjacentZones("zone_1");

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void GetAdjacentZones_ShouldReturnMultipleAdjacentZones()
        {
            // Arrange - one central zone with multiple adjacent zones
            var center = new Zone("center", "Central", new Vector3(0, 0, 0), 100f);
            var north = new Zone("north", "North", new Vector3(0, 180, 0), 100f);
            var east = new Zone("east", "East", new Vector3(180, 0, 0), 100f);
            var south = new Zone("south", "South", new Vector3(0, -180, 0), 100f);
            var west = new Zone("west", "West", new Vector3(-180, 0, 0), 100f);
            var farAway = new Zone("far", "Far", new Vector3(500, 500, 0), 50f);

            _repository.Add(center);
            _repository.Add(north);
            _repository.Add(east);
            _repository.Add(south);
            _repository.Add(west);
            _repository.Add(farAway);

            // Act
            var result = _service.GetAdjacentZones("center").ToList();

            // Assert
            Assert.Equal(4, result.Count);
            Assert.Contains(result, z => z.Id == "north");
            Assert.Contains(result, z => z.Id == "east");
            Assert.Contains(result, z => z.Id == "south");
            Assert.Contains(result, z => z.Id == "west");
            Assert.DoesNotContain(result, z => z.Id == "far");
        }

        [Fact]
        public void GetAdjacentZones_ShouldNotIncludeItself()
        {
            // Arrange
            var zone1 = new Zone("zone_1", "Downtown", new Vector3(0, 0, 0), 100f);
            var zone2 = new Zone("zone_2", "Uptown", new Vector3(150, 0, 0), 100f);
            _repository.Add(zone1);
            _repository.Add(zone2);

            // Act
            var result = _service.GetAdjacentZones("zone_1").ToList();

            // Assert
            Assert.DoesNotContain(result, z => z.Id == "zone_1");
        }

        [Fact]
        public void GetAdjacentZones_ShouldUse2DDistanceIgnoringZ()
        {
            // Arrange - zones at different heights but close in X-Y
            var zone1 = new Zone("zone_1", "Ground", new Vector3(0, 0, 0), 100f);
            var zone2 = new Zone("zone_2", "HighRise", new Vector3(150, 0, 500), 100f);
            _repository.Add(zone1);
            _repository.Add(zone2);

            // Act
            var result = _service.GetAdjacentZones("zone_1").ToList();

            // Assert - should be adjacent based on 2D distance
            Assert.Single(result);
            Assert.Equal("zone_2", result[0].Id);
        }

        #endregion

        #region AreZonesAdjacent Tests

        [Fact]
        public void AreZonesAdjacent_ShouldReturnTrueWhenOverlapping()
        {
            // Arrange
            var zone1 = new Zone("zone_1", "Downtown", new Vector3(0, 0, 0), 100f);
            var zone2 = new Zone("zone_2", "Uptown", new Vector3(150, 0, 0), 100f);
            _repository.Add(zone1);
            _repository.Add(zone2);

            // Act
            var result = _service.AreZonesAdjacent("zone_1", "zone_2");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void AreZonesAdjacent_ShouldReturnFalseWhenFarApart()
        {
            // Arrange
            var zone1 = new Zone("zone_1", "Downtown", new Vector3(0, 0, 0), 50f);
            var zone2 = new Zone("zone_2", "Uptown", new Vector3(500, 500, 0), 50f);
            _repository.Add(zone1);
            _repository.Add(zone2);

            // Act
            var result = _service.AreZonesAdjacent("zone_1", "zone_2");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void AreZonesAdjacent_ShouldBeSymmetric()
        {
            // Arrange
            var zone1 = new Zone("zone_1", "Downtown", new Vector3(0, 0, 0), 100f);
            var zone2 = new Zone("zone_2", "Uptown", new Vector3(150, 0, 0), 100f);
            _repository.Add(zone1);
            _repository.Add(zone2);

            // Act
            var result1to2 = _service.AreZonesAdjacent("zone_1", "zone_2");
            var result2to1 = _service.AreZonesAdjacent("zone_2", "zone_1");

            // Assert
            Assert.Equal(result1to2, result2to1);
        }

        [Fact]
        public void AreZonesAdjacent_ShouldReturnFalseWhenZone1NotFound()
        {
            // Arrange
            var zone2 = new Zone("zone_2", "Uptown", new Vector3(150, 0, 0), 100f);
            _repository.Add(zone2);

            // Act
            var result = _service.AreZonesAdjacent("nonexistent", "zone_2");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void AreZonesAdjacent_ShouldReturnFalseWhenZone2NotFound()
        {
            // Arrange
            var zone1 = new Zone("zone_1", "Downtown", new Vector3(0, 0, 0), 100f);
            _repository.Add(zone1);

            // Act
            var result = _service.AreZonesAdjacent("zone_1", "nonexistent");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void AreZonesAdjacent_ShouldReturnFalseForSameZone()
        {
            // Arrange
            var zone = new Zone("zone_1", "Downtown", new Vector3(0, 0, 0), 100f);
            _repository.Add(zone);

            // Act
            var result = _service.AreZonesAdjacent("zone_1", "zone_1");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void AreZonesAdjacent_ShouldThrowOnNullZoneId1()
        {
            Assert.Throws<ArgumentNullException>(() => _service.AreZonesAdjacent(null!, "zone_2"));
        }

        [Fact]
        public void AreZonesAdjacent_ShouldThrowOnNullZoneId2()
        {
            Assert.Throws<ArgumentNullException>(() => _service.AreZonesAdjacent("zone_1", null!));
        }

        #endregion

        #region GetConnectedZones Tests (Path Finding)

        [Fact]
        public void GetConnectedZones_ShouldReturnAllZonesReachableThroughAdjacency()
        {
            // Arrange - a chain of connected zones
            // A - B - C - D
            var zoneA = new Zone("a", "Zone A", new Vector3(0, 0, 0), 100f);
            var zoneB = new Zone("b", "Zone B", new Vector3(180, 0, 0), 100f);
            var zoneC = new Zone("c", "Zone C", new Vector3(360, 0, 0), 100f);
            var zoneD = new Zone("d", "Zone D", new Vector3(540, 0, 0), 100f);
            var isolated = new Zone("isolated", "Isolated", new Vector3(1000, 1000, 0), 50f);

            _repository.Add(zoneA);
            _repository.Add(zoneB);
            _repository.Add(zoneC);
            _repository.Add(zoneD);
            _repository.Add(isolated);

            // Act
            var result = _service.GetConnectedZones("a").ToList();

            // Assert
            Assert.Equal(3, result.Count);
            Assert.Contains(result, z => z.Id == "b");
            Assert.Contains(result, z => z.Id == "c");
            Assert.Contains(result, z => z.Id == "d");
            Assert.DoesNotContain(result, z => z.Id == "a"); // Should not include self
            Assert.DoesNotContain(result, z => z.Id == "isolated");
        }

        [Fact]
        public void GetConnectedZones_ShouldReturnEmptyWhenZoneIsIsolated()
        {
            // Arrange
            var isolated = new Zone("isolated", "Isolated", new Vector3(0, 0, 0), 50f);
            var other = new Zone("other", "Other", new Vector3(500, 500, 0), 50f);
            _repository.Add(isolated);
            _repository.Add(other);

            // Act
            var result = _service.GetConnectedZones("isolated");

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void GetConnectedZones_ShouldReturnEmptyWhenZoneNotFound()
        {
            // Act
            var result = _service.GetConnectedZones("nonexistent");

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void GetConnectedZones_ShouldThrowOnNullZoneId()
        {
            Assert.Throws<ArgumentNullException>(() => _service.GetConnectedZones(null!));
        }

        [Fact]
        public void GetConnectedZones_ShouldHandleCircularConnectivity()
        {
            // Arrange - zones in a triangle pattern
            // A connected to B, B connected to C, C connected to A
            var zoneA = new Zone("a", "Zone A", new Vector3(0, 0, 0), 100f);
            var zoneB = new Zone("b", "Zone B", new Vector3(150, 0, 0), 100f);
            var zoneC = new Zone("c", "Zone C", new Vector3(75, 130, 0), 100f);

            _repository.Add(zoneA);
            _repository.Add(zoneB);
            _repository.Add(zoneC);

            // Act
            var result = _service.GetConnectedZones("a").ToList();

            // Assert - should find both without infinite loop
            Assert.Equal(2, result.Count);
            Assert.Contains(result, z => z.Id == "b");
            Assert.Contains(result, z => z.Id == "c");
        }

        #endregion

        #region GetConnectedZonesByOwner Tests

        [Fact]
        public void GetConnectedZonesByOwner_ShouldReturnOnlyZonesOwnedBySameFaction()
        {
            // Arrange
            // Michael: A - B - C
            // Trevor: X (adjacent to B but owned by Trevor)
            var zoneA = new Zone("a", "Zone A", new Vector3(0, 0, 0), 100f) { OwnerFactionId = "michael" };
            var zoneB = new Zone("b", "Zone B", new Vector3(180, 0, 0), 100f) { OwnerFactionId = "michael" };
            var zoneC = new Zone("c", "Zone C", new Vector3(360, 0, 0), 100f) { OwnerFactionId = "michael" };
            var zoneX = new Zone("x", "Zone X", new Vector3(180, 150, 0), 100f) { OwnerFactionId = "trevor" };

            _repository.Add(zoneA);
            _repository.Add(zoneB);
            _repository.Add(zoneC);
            _repository.Add(zoneX);

            // Act
            var result = _service.GetConnectedZonesByOwner("a", "michael").ToList();

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Contains(result, z => z.Id == "b");
            Assert.Contains(result, z => z.Id == "c");
            Assert.DoesNotContain(result, z => z.Id == "x");
        }

        [Fact]
        public void GetConnectedZonesByOwner_ShouldReturnEmptyWhenStartZoneHasDifferentOwner()
        {
            // Arrange
            var zone = new Zone("a", "Zone A", new Vector3(0, 0, 0), 100f) { OwnerFactionId = "trevor" };
            _repository.Add(zone);

            // Act
            var result = _service.GetConnectedZonesByOwner("a", "michael");

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void GetConnectedZonesByOwner_ShouldThrowOnNullZoneId()
        {
            Assert.Throws<ArgumentNullException>(() => _service.GetConnectedZonesByOwner(null!, "michael"));
        }

        [Fact]
        public void GetConnectedZonesByOwner_ShouldThrowOnNullFactionId()
        {
            Assert.Throws<ArgumentNullException>(() => _service.GetConnectedZonesByOwner("zone_1", null!));
        }

        [Fact]
        public void GetConnectedZonesByOwner_ShouldReturnEmptyWhenZoneNotFound()
        {
            // Act
            var result = _service.GetConnectedZonesByOwner("nonexistent", "michael");

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void GetConnectedZonesByOwner_ShouldDetectDisconnectedTerritories()
        {
            // Arrange
            // Michael owns: A - B (connected) and D (isolated from A-B by Trevor's zone C)
            var zoneA = new Zone("a", "Zone A", new Vector3(0, 0, 0), 100f) { OwnerFactionId = "michael" };
            var zoneB = new Zone("b", "Zone B", new Vector3(180, 0, 0), 100f) { OwnerFactionId = "michael" };
            var zoneC = new Zone("c", "Zone C", new Vector3(360, 0, 0), 100f) { OwnerFactionId = "trevor" };
            var zoneD = new Zone("d", "Zone D", new Vector3(540, 0, 0), 100f) { OwnerFactionId = "michael" };

            _repository.Add(zoneA);
            _repository.Add(zoneB);
            _repository.Add(zoneC);
            _repository.Add(zoneD);

            // Act - from A, should only find B (D is blocked by Trevor's zone)
            var resultFromA = _service.GetConnectedZonesByOwner("a", "michael").ToList();
            // From D, should find nothing (blocked by Trevor's zone)
            var resultFromD = _service.GetConnectedZonesByOwner("d", "michael").ToList();

            // Assert
            Assert.Single(resultFromA);
            Assert.Contains(resultFromA, z => z.Id == "b");
            Assert.DoesNotContain(resultFromA, z => z.Id == "d");
            Assert.Empty(resultFromD);
        }

        #endregion
    }
}
