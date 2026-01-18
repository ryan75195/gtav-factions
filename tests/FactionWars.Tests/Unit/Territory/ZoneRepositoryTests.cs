using FactionWars.Core.Interfaces;
using FactionWars.Territory.Interfaces;
using FactionWars.Territory.Models;
using FactionWars.Territory.Repositories;
using System;
using System.Linq;
using Xunit;

namespace FactionWars.Tests.Unit.Territory
{
    /// <summary>
    /// Tests for IZoneRepository interface behavior and InMemoryZoneRepository implementation.
    /// These tests define the contract that any ZoneRepository implementation must follow.
    /// </summary>
    public class ZoneRepositoryTests
    {
        private readonly IZoneRepository _repository;

        public ZoneRepositoryTests()
        {
            _repository = new InMemoryZoneRepository();
        }

        #region Add Operations

        [Fact]
        public void Add_ShouldStoreZone()
        {
            // Arrange
            var zone = CreateTestZone("zone_1", "Downtown");

            // Act
            _repository.Add(zone);
            var retrieved = _repository.GetById("zone_1");

            // Assert
            Assert.NotNull(retrieved);
            Assert.Equal("zone_1", retrieved!.Id);
            Assert.Equal("Downtown", retrieved.Name);
        }

        [Fact]
        public void Add_ShouldThrowOnNullZone()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentNullException>(() => _repository.Add(null!));
        }

        [Fact]
        public void Add_ShouldThrowOnDuplicateId()
        {
            // Arrange
            var zone1 = CreateTestZone("zone_1", "Downtown");
            var zone2 = CreateTestZone("zone_1", "Uptown");
            _repository.Add(zone1);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => _repository.Add(zone2));
        }

        #endregion

        #region GetById Operations

        [Fact]
        public void GetById_ShouldReturnZoneWhenExists()
        {
            // Arrange
            var zone = CreateTestZone("zone_1", "Downtown");
            _repository.Add(zone);

            // Act
            var retrieved = _repository.GetById("zone_1");

            // Assert
            Assert.NotNull(retrieved);
            Assert.Equal(zone.Id, retrieved!.Id);
        }

        [Fact]
        public void GetById_ShouldReturnNullWhenNotExists()
        {
            // Arrange - empty repository

            // Act
            var retrieved = _repository.GetById("nonexistent");

            // Assert
            Assert.Null(retrieved);
        }

        [Fact]
        public void GetById_ShouldThrowOnNullId()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentNullException>(() => _repository.GetById(null!));
        }

        [Fact]
        public void GetById_ShouldThrowOnEmptyId()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentException>(() => _repository.GetById(""));
        }

        #endregion

        #region GetAll Operations

        [Fact]
        public void GetAll_ShouldReturnEmptyWhenNoZones()
        {
            // Arrange - empty repository

            // Act
            var zones = _repository.GetAll();

            // Assert
            Assert.Empty(zones);
        }

        [Fact]
        public void GetAll_ShouldReturnAllZones()
        {
            // Arrange
            var zone1 = CreateTestZone("zone_1", "Downtown");
            var zone2 = CreateTestZone("zone_2", "Uptown");
            var zone3 = CreateTestZone("zone_3", "Midtown");
            _repository.Add(zone1);
            _repository.Add(zone2);
            _repository.Add(zone3);

            // Act
            var zones = _repository.GetAll().ToList();

            // Assert
            Assert.Equal(3, zones.Count);
            Assert.Contains(zones, z => z.Id == "zone_1");
            Assert.Contains(zones, z => z.Id == "zone_2");
            Assert.Contains(zones, z => z.Id == "zone_3");
        }

        #endregion

        #region Update Operations

        [Fact]
        public void Update_ShouldModifyExistingZone()
        {
            // Arrange
            var zone = CreateTestZone("zone_1", "Downtown");
            _repository.Add(zone);
            zone.OwnerFactionId = "michael_faction";
            zone.ControlPercentage = 75f;

            // Act
            _repository.Update(zone);
            var retrieved = _repository.GetById("zone_1");

            // Assert
            Assert.Equal("michael_faction", retrieved!.OwnerFactionId);
            Assert.Equal(75f, retrieved.ControlPercentage);
        }

        [Fact]
        public void Update_ShouldThrowOnNullZone()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentNullException>(() => _repository.Update(null!));
        }

        [Fact]
        public void Update_ShouldThrowWhenZoneNotExists()
        {
            // Arrange
            var zone = CreateTestZone("nonexistent", "Ghost Zone");

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => _repository.Update(zone));
        }

        #endregion

        #region Remove Operations

        [Fact]
        public void Remove_ShouldDeleteZone()
        {
            // Arrange
            var zone = CreateTestZone("zone_1", "Downtown");
            _repository.Add(zone);

            // Act
            var removed = _repository.Remove("zone_1");
            var retrieved = _repository.GetById("zone_1");

            // Assert
            Assert.True(removed);
            Assert.Null(retrieved);
        }

        [Fact]
        public void Remove_ShouldReturnFalseWhenNotExists()
        {
            // Arrange - empty repository

            // Act
            var removed = _repository.Remove("nonexistent");

            // Assert
            Assert.False(removed);
        }

        [Fact]
        public void Remove_ShouldThrowOnNullId()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentNullException>(() => _repository.Remove(null!));
        }

        #endregion

        #region GetByOwner Operations

        [Fact]
        public void GetByOwner_ShouldReturnZonesOwnedByFaction()
        {
            // Arrange
            var zone1 = CreateTestZone("zone_1", "Downtown");
            var zone2 = CreateTestZone("zone_2", "Uptown");
            var zone3 = CreateTestZone("zone_3", "Midtown");
            zone1.OwnerFactionId = "michael_faction";
            zone2.OwnerFactionId = "trevor_faction";
            zone3.OwnerFactionId = "michael_faction";
            _repository.Add(zone1);
            _repository.Add(zone2);
            _repository.Add(zone3);

            // Act
            var michaelZones = _repository.GetByOwner("michael_faction").ToList();

            // Assert
            Assert.Equal(2, michaelZones.Count);
            Assert.All(michaelZones, z => Assert.Equal("michael_faction", z.OwnerFactionId));
        }

        [Fact]
        public void GetByOwner_ShouldReturnEmptyWhenNoZonesOwned()
        {
            // Arrange
            var zone = CreateTestZone("zone_1", "Downtown");
            zone.OwnerFactionId = "michael_faction";
            _repository.Add(zone);

            // Act
            var trevorZones = _repository.GetByOwner("trevor_faction");

            // Assert
            Assert.Empty(trevorZones);
        }

        [Fact]
        public void GetByOwner_WithNullOwner_ShouldReturnNeutralZones()
        {
            // Arrange
            var zone1 = CreateTestZone("zone_1", "Downtown");
            var zone2 = CreateTestZone("zone_2", "Uptown");
            zone1.OwnerFactionId = "michael_faction";
            // zone2 has null OwnerFactionId (neutral)
            _repository.Add(zone1);
            _repository.Add(zone2);

            // Act
            var neutralZones = _repository.GetByOwner(null).ToList();

            // Assert
            Assert.Single(neutralZones);
            Assert.Equal("zone_2", neutralZones[0].Id);
        }

        #endregion

        #region GetContested Operations

        [Fact]
        public void GetContested_ShouldReturnOnlyContestedZones()
        {
            // Arrange
            var zone1 = CreateTestZone("zone_1", "Downtown");
            var zone2 = CreateTestZone("zone_2", "Uptown");
            var zone3 = CreateTestZone("zone_3", "Midtown");
            zone1.IsContested = true;
            zone2.IsContested = false;
            zone3.IsContested = true;
            _repository.Add(zone1);
            _repository.Add(zone2);
            _repository.Add(zone3);

            // Act
            var contested = _repository.GetContested().ToList();

            // Assert
            Assert.Equal(2, contested.Count);
            Assert.All(contested, z => Assert.True(z.IsContested));
        }

        [Fact]
        public void GetContested_ShouldReturnEmptyWhenNoContestedZones()
        {
            // Arrange
            var zone = CreateTestZone("zone_1", "Downtown");
            zone.IsContested = false;
            _repository.Add(zone);

            // Act
            var contested = _repository.GetContested();

            // Assert
            Assert.Empty(contested);
        }

        #endregion

        #region Contains Operations

        [Fact]
        public void Contains_ShouldReturnTrueWhenZoneExists()
        {
            // Arrange
            var zone = CreateTestZone("zone_1", "Downtown");
            _repository.Add(zone);

            // Act
            var exists = _repository.Contains("zone_1");

            // Assert
            Assert.True(exists);
        }

        [Fact]
        public void Contains_ShouldReturnFalseWhenZoneNotExists()
        {
            // Arrange - empty repository

            // Act
            var exists = _repository.Contains("nonexistent");

            // Assert
            Assert.False(exists);
        }

        [Fact]
        public void Contains_ShouldThrowOnNullId()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentNullException>(() => _repository.Contains(null!));
        }

        #endregion

        #region Count Operations

        [Fact]
        public void Count_ShouldReturnZeroWhenEmpty()
        {
            // Arrange - empty repository

            // Act
            var count = _repository.Count;

            // Assert
            Assert.Equal(0, count);
        }

        [Fact]
        public void Count_ShouldReturnCorrectNumberOfZones()
        {
            // Arrange
            _repository.Add(CreateTestZone("zone_1", "Downtown"));
            _repository.Add(CreateTestZone("zone_2", "Uptown"));
            _repository.Add(CreateTestZone("zone_3", "Midtown"));

            // Act
            var count = _repository.Count;

            // Assert
            Assert.Equal(3, count);
        }

        #endregion

        #region Clear Operations

        [Fact]
        public void Clear_ShouldRemoveAllZones()
        {
            // Arrange
            _repository.Add(CreateTestZone("zone_1", "Downtown"));
            _repository.Add(CreateTestZone("zone_2", "Uptown"));

            // Act
            _repository.Clear();

            // Assert
            Assert.Equal(0, _repository.Count);
            Assert.Empty(_repository.GetAll());
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
