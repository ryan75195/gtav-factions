using FactionWars.Core.Interfaces;
using FactionWars.Territory.Events;
using FactionWars.Territory.Interfaces;
using FactionWars.Territory.Models;
using FactionWars.Territory.Repositories;
using FactionWars.Territory.Services;
using System;
using System.Linq;
using Xunit;

namespace FactionWars.Tests.Unit.Territory
{
    /// <summary>
    /// Tests for ZoneService business logic and zone querying.
    /// </summary>
    public class ZoneServiceTests
    {
        private readonly IZoneRepository _repository;
        private readonly IZoneService _service;

        public ZoneServiceTests()
        {
            _repository = new InMemoryZoneRepository();
            _service = new ZoneService(_repository);
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_ShouldThrowOnNullRepository()
        {
            Assert.Throws<ArgumentNullException>(() => new ZoneService(null!));
        }

        #endregion

        #region GetZone Tests

        [Fact]
        public void GetZone_ShouldReturnZoneWhenExists()
        {
            // Arrange
            var zone = CreateTestZone("zone_1", "Downtown");
            _repository.Add(zone);

            // Act
            var result = _service.GetZone("zone_1");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("zone_1", result!.Id);
        }

        [Fact]
        public void GetZone_ShouldReturnNullWhenNotExists()
        {
            // Act
            var result = _service.GetZone("nonexistent");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetZone_ShouldThrowOnNullId()
        {
            Assert.Throws<ArgumentNullException>(() => _service.GetZone(null!));
        }

        #endregion

        #region GetAllZones Tests

        [Fact]
        public void GetAllZones_ShouldReturnEmptyWhenNoZones()
        {
            // Act
            var result = _service.GetAllZones();

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void GetAllZones_ShouldReturnAllZones()
        {
            // Arrange
            _repository.Add(CreateTestZone("zone_1", "Downtown"));
            _repository.Add(CreateTestZone("zone_2", "Uptown"));

            // Act
            var result = _service.GetAllZones().ToList();

            // Assert
            Assert.Equal(2, result.Count);
        }

        #endregion

        #region GetZoneAtPosition Tests

        [Fact]
        public void GetZoneAtPosition_ShouldReturnZoneWhenInsideRadius()
        {
            // Arrange
            var zone = new Zone("zone_1", "Downtown", new Vector3(100, 100, 0), 50f);
            _repository.Add(zone);
            var positionInside = new Vector3(110, 110, 0);

            // Act
            var result = _service.GetZoneAtPosition(positionInside);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("zone_1", result!.Id);
        }

        [Fact]
        public void GetZoneAtPosition_ShouldReturnNullWhenOutsideAllZones()
        {
            // Arrange
            var zone = new Zone("zone_1", "Downtown", new Vector3(100, 100, 0), 50f);
            _repository.Add(zone);
            var positionOutside = new Vector3(500, 500, 0);

            // Act
            var result = _service.GetZoneAtPosition(positionOutside);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetZoneAtPosition_ShouldReturnClosestZoneWhenInMultiple()
        {
            // Arrange - overlapping zones
            var zone1 = new Zone("zone_1", "Downtown", new Vector3(100, 100, 0), 100f);
            var zone2 = new Zone("zone_2", "Uptown", new Vector3(150, 100, 0), 100f);
            _repository.Add(zone1);
            _repository.Add(zone2);
            // Position closer to zone_1's center
            var position = new Vector3(110, 100, 0);

            // Act
            var result = _service.GetZoneAtPosition(position);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("zone_1", result!.Id);
        }

        [Fact]
        public void GetZoneAtPosition_ShouldUse2DDistanceForGroundPositions()
        {
            // Arrange - zone at ground level, position at different height
            var zone = new Zone("zone_1", "Downtown", new Vector3(100, 100, 0), 50f);
            _repository.Add(zone);
            // Position directly above but would be outside if using 3D distance
            var positionAbove = new Vector3(100, 100, 100);

            // Act
            var result = _service.GetZoneAtPosition(positionAbove);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("zone_1", result!.Id);
        }

        #endregion

        #region GetZonesByOwner Tests

        [Fact]
        public void GetZonesByOwner_ShouldReturnOwnedZones()
        {
            // Arrange
            var zone1 = CreateTestZone("zone_1", "Downtown");
            var zone2 = CreateTestZone("zone_2", "Uptown");
            zone1.OwnerFactionId = "michael";
            zone2.OwnerFactionId = "trevor";
            _repository.Add(zone1);
            _repository.Add(zone2);

            // Act
            var result = _service.GetZonesByOwner("michael").ToList();

            // Assert
            Assert.Single(result);
            Assert.Equal("zone_1", result[0].Id);
        }

        [Fact]
        public void GetZonesByOwner_WithNullOwner_ShouldReturnNeutralZones()
        {
            // Arrange
            var zone1 = CreateTestZone("zone_1", "Downtown");
            var zone2 = CreateTestZone("zone_2", "Uptown");
            zone1.OwnerFactionId = "michael";
            // zone2 is neutral
            _repository.Add(zone1);
            _repository.Add(zone2);

            // Act
            var result = _service.GetZonesByOwner(null).ToList();

            // Assert
            Assert.Single(result);
            Assert.Equal("zone_2", result[0].Id);
        }

        #endregion

        #region GetContestedZones Tests

        [Fact]
        public void GetContestedZones_ShouldReturnOnlyContestedZones()
        {
            // Arrange
            var zone1 = CreateTestZone("zone_1", "Downtown");
            var zone2 = CreateTestZone("zone_2", "Uptown");
            zone1.IsContested = true;
            zone2.IsContested = false;
            _repository.Add(zone1);
            _repository.Add(zone2);

            // Act
            var result = _service.GetContestedZones().ToList();

            // Assert
            Assert.Single(result);
            Assert.True(result[0].IsContested);
        }

        #endregion

        #region GetZonesByTrait Tests

        [Fact]
        public void GetZonesByTrait_ShouldReturnZonesWithTrait()
        {
            // Arrange
            var zone1 = CreateTestZone("zone_1", "Downtown");
            var zone2 = CreateTestZone("zone_2", "Port");
            var zone3 = CreateTestZone("zone_3", "Industrial");
            zone1.Traits = ZoneTrait.Commercial;
            zone2.Traits = ZoneTrait.Port;
            zone3.Traits = ZoneTrait.Industrial | ZoneTrait.Commercial;
            _repository.Add(zone1);
            _repository.Add(zone2);
            _repository.Add(zone3);

            // Act
            var result = _service.GetZonesByTrait(ZoneTrait.Commercial).ToList();

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Contains(result, z => z.Id == "zone_1");
            Assert.Contains(result, z => z.Id == "zone_3");
        }

        [Fact]
        public void GetZonesByTrait_WithNone_ShouldReturnZonesWithNoTraits()
        {
            // Arrange
            var zone1 = CreateTestZone("zone_1", "Downtown");
            var zone2 = CreateTestZone("zone_2", "Port");
            zone1.Traits = ZoneTrait.None;
            zone2.Traits = ZoneTrait.Port;
            _repository.Add(zone1);
            _repository.Add(zone2);

            // Act
            var result = _service.GetZonesByTrait(ZoneTrait.None).ToList();

            // Assert
            Assert.Single(result);
            Assert.Equal("zone_1", result[0].Id);
        }

        #endregion

        #region GetHighValueZones Tests

        [Fact]
        public void GetHighValueZones_ShouldReturnZonesOrderedByStrategicValue()
        {
            // Arrange
            var zone1 = new Zone("zone_1", "Downtown", Vector3.Zero, 100f, 5);
            var zone2 = new Zone("zone_2", "Uptown", Vector3.Zero, 100f, 10);
            var zone3 = new Zone("zone_3", "Midtown", Vector3.Zero, 100f, 7);
            _repository.Add(zone1);
            _repository.Add(zone2);
            _repository.Add(zone3);

            // Act
            var result = _service.GetHighValueZones(2).ToList();

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Equal("zone_2", result[0].Id); // Highest value first
            Assert.Equal("zone_3", result[1].Id);
        }

        [Fact]
        public void GetHighValueZones_ShouldReturnAllWhenCountExceedsTotal()
        {
            // Arrange
            var zone = new Zone("zone_1", "Downtown", Vector3.Zero, 100f, 5);
            _repository.Add(zone);

            // Act
            var result = _service.GetHighValueZones(10).ToList();

            // Assert
            Assert.Single(result);
        }

        [Fact]
        public void GetHighValueZones_WithZeroCount_ShouldReturnEmpty()
        {
            // Arrange
            _repository.Add(new Zone("zone_1", "Downtown", Vector3.Zero, 100f, 5));

            // Act
            var result = _service.GetHighValueZones(0).ToList();

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void GetHighValueZones_WithNegativeCount_ShouldThrow()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _service.GetHighValueZones(-1).ToList());
        }

        #endregion

        #region TransferZoneOwnership Tests

        [Fact]
        public void TransferZoneOwnership_ShouldUpdateOwner()
        {
            // Arrange
            var zone = CreateTestZone("zone_1", "Downtown");
            _repository.Add(zone);

            // Act
            var result = _service.TransferZoneOwnership("zone_1", "michael");
            var updated = _repository.GetById("zone_1");

            // Assert
            Assert.True(result);
            Assert.Equal("michael", updated!.OwnerFactionId);
        }

        [Fact]
        public void TransferZoneOwnership_ShouldSetControlTo100Percent()
        {
            // Arrange
            var zone = CreateTestZone("zone_1", "Downtown");
            zone.ControlPercentage = 50f;
            _repository.Add(zone);

            // Act
            _service.TransferZoneOwnership("zone_1", "michael");
            var updated = _repository.GetById("zone_1");

            // Assert
            Assert.Equal(100f, updated!.ControlPercentage);
        }

        [Fact]
        public void TransferZoneOwnership_ShouldClearContestedState()
        {
            // Arrange
            var zone = CreateTestZone("zone_1", "Downtown");
            zone.IsContested = true;
            _repository.Add(zone);

            // Act
            _service.TransferZoneOwnership("zone_1", "michael");
            var updated = _repository.GetById("zone_1");

            // Assert
            Assert.False(updated!.IsContested);
        }

        [Fact]
        public void TransferZoneOwnership_ShouldReturnFalseWhenZoneNotFound()
        {
            // Act
            var result = _service.TransferZoneOwnership("nonexistent", "michael");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void TransferZoneOwnership_WithNullOwner_ShouldMakeZoneNeutral()
        {
            // Arrange
            var zone = CreateTestZone("zone_1", "Downtown");
            zone.OwnerFactionId = "michael";
            _repository.Add(zone);

            // Act
            _service.TransferZoneOwnership("zone_1", null);
            var updated = _repository.GetById("zone_1");

            // Assert
            Assert.Null(updated!.OwnerFactionId);
        }

        [Fact]
        public void TransferZoneOwnership_ShouldThrowOnNullZoneId()
        {
            Assert.Throws<ArgumentNullException>(() => _service.TransferZoneOwnership(null!, "michael"));
        }

        [Fact]
        public void TransferZoneOwnership_ChangesOwner_RaisesZoneOwnershipChanged()
        {
            var zone = CreateTestZone("morningwood", "Morningwood");
            zone.OwnerFactionId = "trevor";
            _repository.Add(zone);

            ZoneOwnershipChangedEventArgs? captured = null;
            _service.ZoneOwnershipChanged += (_, args) => captured = args;

            var result = _service.TransferZoneOwnership("morningwood", "michael");

            Assert.True(result);
            Assert.NotNull(captured);
            Assert.Equal("morningwood", captured!.ZoneId);
            Assert.Equal("trevor", captured.PreviousOwner);
            Assert.Equal("michael", captured.NewOwner);
        }

        [Fact]
        public void TransferZoneOwnership_SameOwner_DoesNotRaiseEvent()
        {
            var zone = CreateTestZone("morningwood", "Morningwood");
            zone.OwnerFactionId = "trevor";
            _repository.Add(zone);

            bool raised = false;
            _service.ZoneOwnershipChanged += (_, _) => raised = true;

            _service.TransferZoneOwnership("morningwood", "trevor");

            Assert.False(raised);
        }

        [Fact]
        public void TransferZoneOwnership_ZoneNotFound_DoesNotRaiseEvent()
        {
            bool raised = false;
            _service.ZoneOwnershipChanged += (_, _) => raised = true;

            _service.TransferZoneOwnership("nope", "michael");

            Assert.False(raised);
        }

        #endregion

        #region UpdateZoneControl Tests

        [Fact]
        public void UpdateZoneControl_ShouldUpdateControlPercentage()
        {
            // Arrange
            var zone = CreateTestZone("zone_1", "Downtown");
            _repository.Add(zone);

            // Act
            var result = _service.UpdateZoneControl("zone_1", 75f);
            var updated = _repository.GetById("zone_1");

            // Assert
            Assert.True(result);
            Assert.Equal(75f, updated!.ControlPercentage);
        }

        [Fact]
        public void UpdateZoneControl_ShouldClampToValidRange()
        {
            // Arrange
            var zone = CreateTestZone("zone_1", "Downtown");
            _repository.Add(zone);

            // Act - try to set above 100
            _service.UpdateZoneControl("zone_1", 150f);
            var updated = _repository.GetById("zone_1");

            // Assert
            Assert.Equal(100f, updated!.ControlPercentage);
        }

        [Fact]
        public void UpdateZoneControl_ShouldReturnFalseWhenZoneNotFound()
        {
            // Act
            var result = _service.UpdateZoneControl("nonexistent", 50f);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void UpdateZoneControl_ShouldThrowOnNullZoneId()
        {
            Assert.Throws<ArgumentNullException>(() => _service.UpdateZoneControl(null!, 50f));
        }

        #endregion

        #region SetZoneContested Tests

        [Fact]
        public void SetZoneContested_ShouldUpdateContestedState()
        {
            // Arrange
            var zone = CreateTestZone("zone_1", "Downtown");
            _repository.Add(zone);

            // Act
            var result = _service.SetZoneContested("zone_1", true);
            var updated = _repository.GetById("zone_1");

            // Assert
            Assert.True(result);
            Assert.True(updated!.IsContested);
        }

        [Fact]
        public void SetZoneContested_ShouldClearContestedState()
        {
            // Arrange
            var zone = CreateTestZone("zone_1", "Downtown");
            zone.IsContested = true;
            _repository.Add(zone);

            // Act
            _service.SetZoneContested("zone_1", false);
            var updated = _repository.GetById("zone_1");

            // Assert
            Assert.False(updated!.IsContested);
        }

        [Fact]
        public void SetZoneContested_ShouldReturnFalseWhenZoneNotFound()
        {
            // Act
            var result = _service.SetZoneContested("nonexistent", true);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void SetZoneContested_ShouldThrowOnNullZoneId()
        {
            Assert.Throws<ArgumentNullException>(() => _service.SetZoneContested(null!, true));
        }

        #endregion

        #region GetFactionTerritoryValue Tests

        [Fact]
        public void GetFactionTerritoryValue_ShouldSumStrategicValues()
        {
            // Arrange
            var zone1 = new Zone("zone_1", "Downtown", Vector3.Zero, 100f, 5);
            var zone2 = new Zone("zone_2", "Uptown", Vector3.Zero, 100f, 3);
            var zone3 = new Zone("zone_3", "Midtown", Vector3.Zero, 100f, 7);
            zone1.OwnerFactionId = "michael";
            zone2.OwnerFactionId = "michael";
            zone3.OwnerFactionId = "trevor";
            _repository.Add(zone1);
            _repository.Add(zone2);
            _repository.Add(zone3);

            // Act
            var result = _service.GetFactionTerritoryValue("michael");

            // Assert
            Assert.Equal(8, result); // 5 + 3
        }

        [Fact]
        public void GetFactionTerritoryValue_ShouldReturnZeroWhenNoZonesOwned()
        {
            // Arrange
            var zone = new Zone("zone_1", "Downtown", Vector3.Zero, 100f, 5);
            zone.OwnerFactionId = "trevor";
            _repository.Add(zone);

            // Act
            var result = _service.GetFactionTerritoryValue("michael");

            // Assert
            Assert.Equal(0, result);
        }

        [Fact]
        public void GetFactionTerritoryValue_ShouldThrowOnNullFactionId()
        {
            Assert.Throws<ArgumentNullException>(() => _service.GetFactionTerritoryValue(null!));
        }

        #endregion

        #region GetZoneCount Tests

        [Fact]
        public void GetZoneCount_ShouldReturnCountOfOwnedZones()
        {
            // Arrange
            var zone1 = CreateTestZone("zone_1", "Downtown");
            var zone2 = CreateTestZone("zone_2", "Uptown");
            var zone3 = CreateTestZone("zone_3", "Midtown");
            zone1.OwnerFactionId = "michael";
            zone2.OwnerFactionId = "michael";
            zone3.OwnerFactionId = "trevor";
            _repository.Add(zone1);
            _repository.Add(zone2);
            _repository.Add(zone3);

            // Act
            var result = _service.GetZoneCount("michael");

            // Assert
            Assert.Equal(2, result);
        }

        [Fact]
        public void GetZoneCount_WithNullFactionId_ShouldReturnNeutralZoneCount()
        {
            // Arrange
            var zone1 = CreateTestZone("zone_1", "Downtown");
            var zone2 = CreateTestZone("zone_2", "Uptown");
            zone1.OwnerFactionId = "michael";
            // zone2 is neutral
            _repository.Add(zone1);
            _repository.Add(zone2);

            // Act
            var result = _service.GetZoneCount(null);

            // Assert
            Assert.Equal(1, result);
        }

        #endregion

        #region IsPositionInAnyZone Tests

        [Fact]
        public void IsPositionInAnyZone_ShouldReturnTrueWhenInsideZone()
        {
            // Arrange
            var zone = new Zone("zone_1", "Downtown", new Vector3(100, 100, 0), 50f);
            _repository.Add(zone);

            // Act
            var result = _service.IsPositionInAnyZone(new Vector3(110, 110, 0));

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsPositionInAnyZone_ShouldReturnFalseWhenOutsideAllZones()
        {
            // Arrange
            var zone = new Zone("zone_1", "Downtown", new Vector3(100, 100, 0), 50f);
            _repository.Add(zone);

            // Act
            var result = _service.IsPositionInAnyZone(new Vector3(500, 500, 0));

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsPositionInAnyZone_ShouldReturnFalseWhenNoZones()
        {
            // Act
            var result = _service.IsPositionInAnyZone(new Vector3(100, 100, 0));

            // Assert
            Assert.False(result);
        }

        #endregion

        #region Helper Methods

        private static Zone CreateTestZone(string id, string name)
        {
            return new Zone(id, name, new Vector3(0, 0, 0));
        }

        #endregion
    }
}
