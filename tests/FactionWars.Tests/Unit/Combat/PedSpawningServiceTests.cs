using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Models;
using FactionWars.Combat.Pools;
using FactionWars.Combat.Services;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Utils;
using Moq;
using System;
using System.Collections.Generic;
using Xunit;

namespace FactionWars.Tests.Unit.Combat
{
    /// <summary>
    /// Tests for ped spawning service with relationship groups.
    /// Following TDD - these tests define the expected behavior for the PedSpawningService.
    /// </summary>
    public class PedSpawningServiceTests
    {
        #region Test Setup

        private MockGameBridge _gameBridge;
        private IPedPool _pedPool;

        public PedSpawningServiceTests()
        {
            _gameBridge = new MockGameBridge();
            _pedPool = new InMemoryPedPool(30);
        }

        private IPedSpawningService CreateService()
        {
            return new PedSpawningService(_gameBridge, _pedPool);
        }

        #endregion

        #region Constructor Validation

        [Fact]
        public void Constructor_ShouldThrowForNullGameBridge()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new PedSpawningService(null!, _pedPool));
        }

        [Fact]
        public void Constructor_ShouldThrowForNullPedPool()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new PedSpawningService(_gameBridge, null!));
        }

        #endregion

        #region Basic Spawning

        [Fact]
        public void SpawnPed_ShouldReturnValidPedHandle()
        {
            // Arrange
            var service = CreateService();
            var position = new Vector3(100, 200, 50);

            // Act
            var result = service.SpawnPed("a_m_y_hipster_01", position, "faction_michael", "zone_vinewood");

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsValid);
            Assert.True(result.Handle > 0);
        }

        [Fact]
        public void SpawnPed_ShouldSetCorrectMetadata()
        {
            // Arrange
            var service = CreateService();
            var position = new Vector3(100, 200, 50);

            // Act
            var result = service.SpawnPed("a_m_y_hipster_01", position, "faction_michael", "zone_vinewood");

            // Assert
            Assert.Equal("a_m_y_hipster_01", result.ModelName);
            Assert.Equal(position, result.SpawnPosition);
            Assert.Equal("faction_michael", result.FactionId);
            Assert.Equal("zone_vinewood", result.ZoneId);
        }

        [Fact]
        public void SpawnPed_ShouldCallGameBridgeCreatePed()
        {
            // Arrange
            var service = CreateService();
            var position = new Vector3(100, 200, 50);

            // Act
            var result = service.SpawnPed("a_m_y_hipster_01", position, "faction_michael", "zone_vinewood");

            // Assert
            Assert.True(_gameBridge.PedExists(result.Handle));
        }

        [Fact]
        public void SpawnPed_ShouldAddToPedPool()
        {
            // Arrange
            var service = CreateService();
            var position = new Vector3(100, 200, 50);

            // Act
            var result = service.SpawnPed("a_m_y_hipster_01", position, "faction_michael", "zone_vinewood");

            // Assert
            Assert.True(_pedPool.Contains(result));
            Assert.Equal(1, _pedPool.Count);
        }

        #endregion

        #region Relationship Groups

        [Fact]
        public void SpawnPed_ShouldSetRelationshipGroupForMichael()
        {
            // Arrange
            var service = CreateService();
            var position = new Vector3(100, 200, 50);

            // Act
            var result = service.SpawnPed("a_m_y_hipster_01", position, "faction_michael", "zone_vinewood");

            // Assert
            var relationshipGroup = _gameBridge.GetPedRelationshipGroup(result.Handle);
            Assert.Equal("FACTION_MICHAEL", relationshipGroup);
        }

        [Fact]
        public void SpawnPed_ShouldSetRelationshipGroupForTrevor()
        {
            // Arrange
            var service = CreateService();
            var position = new Vector3(100, 200, 50);

            // Act
            var result = service.SpawnPed("a_m_y_hipster_01", position, "faction_trevor", "zone_sandy_shores");

            // Assert
            var relationshipGroup = _gameBridge.GetPedRelationshipGroup(result.Handle);
            Assert.Equal("FACTION_TREVOR", relationshipGroup);
        }

        [Fact]
        public void SpawnPed_ShouldSetRelationshipGroupForFranklin()
        {
            // Arrange
            var service = CreateService();
            var position = new Vector3(100, 200, 50);

            // Act
            var result = service.SpawnPed("a_m_y_hipster_01", position, "faction_franklin", "zone_strawberry");

            // Assert
            var relationshipGroup = _gameBridge.GetPedRelationshipGroup(result.Handle);
            Assert.Equal("FACTION_FRANKLIN", relationshipGroup);
        }

        [Fact]
        public void SpawnPed_ShouldSetGenericRelationshipGroupForUnknownFaction()
        {
            // Arrange
            var service = CreateService();
            var position = new Vector3(100, 200, 50);

            // Act
            var result = service.SpawnPed("a_m_y_hipster_01", position, "faction_unknown", "zone_somewhere");

            // Assert
            var relationshipGroup = _gameBridge.GetPedRelationshipGroup(result.Handle);
            Assert.Equal("FACTION_UNKNOWN", relationshipGroup);
        }

        [Fact]
        public void SpawnPed_ShouldUppercaseRelationshipGroup()
        {
            // Arrange
            var service = CreateService();
            var position = new Vector3(100, 200, 50);

            // Act
            var result = service.SpawnPed("a_m_y_hipster_01", position, "FACTION_MICHAEL", "zone_vinewood");

            // Assert
            var relationshipGroup = _gameBridge.GetPedRelationshipGroup(result.Handle);
            Assert.Equal("FACTION_MICHAEL", relationshipGroup);
        }

        #endregion

        #region Pool Capacity Handling

        [Fact]
        public void SpawnPed_ShouldReturnInvalidWhenPoolIsFull()
        {
            // Arrange
            var smallPool = new InMemoryPedPool(2);
            var service = new PedSpawningService(_gameBridge, smallPool);
            var position = new Vector3(100, 200, 50);

            // Fill the pool
            service.SpawnPed("model1", position, "faction_michael", "zone1");
            service.SpawnPed("model2", position, "faction_michael", "zone1");

            // Act
            var result = service.SpawnPed("model3", position, "faction_michael", "zone1");

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal(PedHandle.Invalid, result);
        }

        [Fact]
        public void SpawnPed_ShouldNotCreatePedInGameWhenPoolIsFull()
        {
            // Arrange
            var smallPool = new InMemoryPedPool(1);
            var service = new PedSpawningService(_gameBridge, smallPool);
            var position = new Vector3(100, 200, 50);

            // Fill the pool
            service.SpawnPed("model1", position, "faction_michael", "zone1");

            // Record current handle count (should be 1 ped created)
            var initialPedExists = _gameBridge.PedExists(1);

            // Act
            var result = service.SpawnPed("model2", position, "faction_michael", "zone1");

            // Assert
            Assert.False(result.IsValid);
            // Only the first ped should exist, handle 2 should not be created
            Assert.True(initialPedExists);
            Assert.False(_gameBridge.PedExists(2));
        }

        #endregion

        #region Input Validation

        [Fact]
        public void SpawnPed_ShouldThrowForNullModelName()
        {
            // Arrange
            var service = CreateService();
            var position = new Vector3(100, 200, 50);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                service.SpawnPed(null!, position, "faction_michael", "zone_vinewood"));
        }

        [Fact]
        public void SpawnPed_ShouldThrowForEmptyModelName()
        {
            // Arrange
            var service = CreateService();
            var position = new Vector3(100, 200, 50);

            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                service.SpawnPed("", position, "faction_michael", "zone_vinewood"));
        }

        [Fact]
        public void SpawnPed_ShouldThrowForWhitespaceModelName()
        {
            // Arrange
            var service = CreateService();
            var position = new Vector3(100, 200, 50);

            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                service.SpawnPed("   ", position, "faction_michael", "zone_vinewood"));
        }

        [Fact]
        public void SpawnPed_ShouldThrowForNullFactionId()
        {
            // Arrange
            var service = CreateService();
            var position = new Vector3(100, 200, 50);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                service.SpawnPed("a_m_y_hipster_01", position, null!, "zone_vinewood"));
        }

        [Fact]
        public void SpawnPed_ShouldThrowForEmptyFactionId()
        {
            // Arrange
            var service = CreateService();
            var position = new Vector3(100, 200, 50);

            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                service.SpawnPed("a_m_y_hipster_01", position, "", "zone_vinewood"));
        }

        #endregion

        #region Optional ZoneId

        [Fact]
        public void SpawnPed_ShouldAllowNullZoneId()
        {
            // Arrange
            var service = CreateService();
            var position = new Vector3(100, 200, 50);

            // Act
            var result = service.SpawnPed("a_m_y_hipster_01", position, "faction_michael", null);

            // Assert
            Assert.True(result.IsValid);
            Assert.Null(result.ZoneId);
        }

        #endregion

        #region Batch Spawning

        [Fact]
        public void SpawnMultiplePeds_ShouldReturnRequestedCount()
        {
            // Arrange
            var service = CreateService();
            var position = new Vector3(100, 200, 50);

            // Act
            var results = service.SpawnMultiplePeds("a_m_y_hipster_01", position, "faction_michael", "zone_vinewood", 5);

            // Assert
            Assert.Equal(5, results.Count);
            Assert.All(results, ped => Assert.True(ped.IsValid));
        }

        [Fact]
        public void SpawnMultiplePeds_ShouldAddAllToPedPool()
        {
            // Arrange
            var service = CreateService();
            var position = new Vector3(100, 200, 50);

            // Act
            var results = service.SpawnMultiplePeds("a_m_y_hipster_01", position, "faction_michael", "zone_vinewood", 5);

            // Assert
            Assert.Equal(5, _pedPool.Count);
            foreach (var ped in results)
            {
                Assert.True(_pedPool.Contains(ped));
            }
        }

        [Fact]
        public void SpawnMultiplePeds_ShouldSetRelationshipGroupForAll()
        {
            // Arrange
            var service = CreateService();
            var position = new Vector3(100, 200, 50);

            // Act
            var results = service.SpawnMultiplePeds("a_m_y_hipster_01", position, "faction_michael", "zone_vinewood", 3);

            // Assert
            foreach (var ped in results)
            {
                var relationshipGroup = _gameBridge.GetPedRelationshipGroup(ped.Handle);
                Assert.Equal("FACTION_MICHAEL", relationshipGroup);
            }
        }

        [Fact]
        public void SpawnMultiplePeds_ShouldStopWhenPoolIsFull()
        {
            // Arrange
            var smallPool = new InMemoryPedPool(3);
            var service = new PedSpawningService(_gameBridge, smallPool);
            var position = new Vector3(100, 200, 50);

            // Act
            var results = service.SpawnMultiplePeds("a_m_y_hipster_01", position, "faction_michael", "zone_vinewood", 5);

            // Assert
            Assert.Equal(3, results.Count);
            Assert.Equal(3, smallPool.Count);
        }

        [Fact]
        public void SpawnMultiplePeds_ShouldReturnEmptyListForZeroCount()
        {
            // Arrange
            var service = CreateService();
            var position = new Vector3(100, 200, 50);

            // Act
            var results = service.SpawnMultiplePeds("a_m_y_hipster_01", position, "faction_michael", "zone_vinewood", 0);

            // Assert
            Assert.Empty(results);
        }

        [Fact]
        public void SpawnMultiplePeds_ShouldThrowForNegativeCount()
        {
            // Arrange
            var service = CreateService();
            var position = new Vector3(100, 200, 50);

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                service.SpawnMultiplePeds("a_m_y_hipster_01", position, "faction_michael", "zone_vinewood", -1));
        }

        #endregion

        #region CanSpawn Check

        [Fact]
        public void CanSpawn_ShouldReturnTrueWhenPoolHasSpace()
        {
            // Arrange
            var service = CreateService();

            // Act
            var result = service.CanSpawn();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void CanSpawn_ShouldReturnFalseWhenPoolIsFull()
        {
            // Arrange
            var smallPool = new InMemoryPedPool(2);
            var service = new PedSpawningService(_gameBridge, smallPool);
            var position = new Vector3(100, 200, 50);

            // Fill the pool
            service.SpawnPed("model1", position, "faction_michael", "zone1");
            service.SpawnPed("model2", position, "faction_michael", "zone1");

            // Act
            var result = service.CanSpawn();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void CanSpawnCount_ShouldReturnAvailableSlots()
        {
            // Arrange
            var smallPool = new InMemoryPedPool(5);
            var service = new PedSpawningService(_gameBridge, smallPool);
            var position = new Vector3(100, 200, 50);

            // Add 2 peds
            service.SpawnPed("model1", position, "faction_michael", "zone1");
            service.SpawnPed("model2", position, "faction_michael", "zone1");

            // Act
            var result = service.CanSpawnCount();

            // Assert
            Assert.Equal(3, result);
        }

        #endregion

        #region GetRelationshipGroup

        [Fact]
        public void GetRelationshipGroup_ShouldReturnCorrectGroupForFaction()
        {
            // Arrange
            var service = CreateService();

            // Act & Assert
            Assert.Equal("FACTION_MICHAEL", service.GetRelationshipGroup("faction_michael"));
            Assert.Equal("FACTION_TREVOR", service.GetRelationshipGroup("faction_trevor"));
            Assert.Equal("FACTION_FRANKLIN", service.GetRelationshipGroup("faction_franklin"));
        }

        [Fact]
        public void GetRelationshipGroup_ShouldNormalizeFactionId()
        {
            // Arrange
            var service = CreateService();

            // Act & Assert
            Assert.Equal("FACTION_MICHAEL", service.GetRelationshipGroup("FACTION_MICHAEL"));
            Assert.Equal("FACTION_MICHAEL", service.GetRelationshipGroup("Faction_Michael"));
            Assert.Equal("FACTION_MICHAEL", service.GetRelationshipGroup("faction_michael"));
        }

        #endregion
    }
}
