using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Models;
using FactionWars.Combat.Pools;
using FactionWars.Combat.Services;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Utils;
using System;
using System.Linq;
using Xunit;

namespace FactionWars.Tests.Unit.Combat
{
    /// <summary>
    /// Tests for ped despawn management.
    /// Following TDD - these tests define the expected behavior for the IPedDespawnService.
    /// </summary>
    public class PedDespawnServiceTests
    {
        #region Test Setup

        private MockGameBridge _gameBridge;
        private IPedPool _pedPool;

        public PedDespawnServiceTests()
        {
            _gameBridge = new MockGameBridge();
            _pedPool = new InMemoryPedPool(30);
        }

        private IPedDespawnService CreateService()
        {
            return new PedDespawnService(_gameBridge, _pedPool);
        }

        private PedHandle CreateAndAddPed(int handle, string factionId = "faction_michael", string? zoneId = "zone1", Vector3? position = null)
        {
            var pos = position ?? new Vector3(0, 0, 0);
            var ped = new PedHandle(handle, factionId, pos, "model_a", zoneId);
            _pedPool.Add(ped);
            // Simulate ped creation in game
            _gameBridge.CreatePed("model_a", pos);
            return ped;
        }

        #endregion

        #region Constructor Validation

        [Fact]
        public void Constructor_ShouldThrowForNullGameBridge()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new PedDespawnService(null!, _pedPool));
        }

        [Fact]
        public void Constructor_ShouldThrowForNullPedPool()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new PedDespawnService(_gameBridge, null!));
        }

        #endregion

        #region Despawn Single Ped

        [Fact]
        public void DespawnPed_ShouldRemoveFromPool()
        {
            // Arrange
            var service = CreateService();
            var ped = CreateAndAddPed(1);

            // Act
            var result = service.DespawnPed(ped);

            // Assert
            Assert.True(result);
            Assert.False(_pedPool.Contains(ped));
        }

        [Fact]
        public void DespawnPed_ShouldDeleteFromGame()
        {
            // Arrange
            var service = CreateService();
            var ped = CreateAndAddPed(1);
            Assert.True(_gameBridge.PedExists(1));

            // Act
            service.DespawnPed(ped);

            // Assert
            Assert.False(_gameBridge.PedExists(1));
        }

        [Fact]
        public void DespawnPed_ShouldReturnFalseForNonexistentPed()
        {
            // Arrange
            var service = CreateService();
            var ped = new PedHandle(999); // Not in pool

            // Act
            var result = service.DespawnPed(ped);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void DespawnPed_ShouldThrowForNullPed()
        {
            // Arrange
            var service = CreateService();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => service.DespawnPed(null!));
        }

        [Fact]
        public void DespawnPed_ShouldReturnFalseForInvalidPed()
        {
            // Arrange
            var service = CreateService();

            // Act
            var result = service.DespawnPed(PedHandle.Invalid);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region Despawn By Handle

        [Fact]
        public void DespawnPedByHandle_ShouldRemoveFromPool()
        {
            // Arrange
            var service = CreateService();
            var ped = CreateAndAddPed(1);

            // Act
            var result = service.DespawnPed(1);

            // Assert
            Assert.True(result);
            Assert.False(_pedPool.Contains(1));
        }

        [Fact]
        public void DespawnPedByHandle_ShouldDeleteFromGame()
        {
            // Arrange
            var service = CreateService();
            var ped = CreateAndAddPed(1);

            // Act
            service.DespawnPed(1);

            // Assert
            Assert.False(_gameBridge.PedExists(1));
        }

        [Fact]
        public void DespawnPedByHandle_ShouldReturnFalseForNonexistentHandle()
        {
            // Arrange
            var service = CreateService();

            // Act
            var result = service.DespawnPed(999);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region Despawn Dead Peds

        [Fact]
        public void DespawnDeadPeds_ShouldRemoveDeadPedsFromPool()
        {
            // Arrange
            var service = CreateService();
            CreateAndAddPed(1);
            CreateAndAddPed(2);
            CreateAndAddPed(3);

            // Kill ped 2
            _gameBridge.KillPed(2);

            // Act
            var result = service.DespawnDeadPeds();

            // Assert
            Assert.Single(result);
            Assert.Equal(2, result.First().Handle);
            Assert.False(_pedPool.Contains(2));
            Assert.True(_pedPool.Contains(1));
            Assert.True(_pedPool.Contains(3));
        }

        [Fact]
        public void DespawnDeadPeds_ShouldDeleteDeadPedsFromGame()
        {
            // Arrange
            var service = CreateService();
            CreateAndAddPed(1);
            CreateAndAddPed(2);

            _gameBridge.KillPed(1);
            _gameBridge.KillPed(2);

            // Act
            service.DespawnDeadPeds();

            // Assert
            Assert.False(_gameBridge.PedExists(1));
            Assert.False(_gameBridge.PedExists(2));
        }

        [Fact]
        public void DespawnDeadPeds_ShouldReturnEmptyListWhenNoDead()
        {
            // Arrange
            var service = CreateService();
            CreateAndAddPed(1);
            CreateAndAddPed(2);

            // Act
            var result = service.DespawnDeadPeds();

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void DespawnDeadPeds_ShouldHandleEmptyPool()
        {
            // Arrange
            var service = CreateService();

            // Act
            var result = service.DespawnDeadPeds();

            // Assert
            Assert.Empty(result);
        }

        #endregion

        #region Despawn By Distance

        [Fact]
        public void DespawnPedsByDistance_ShouldRemovePedsTooFar()
        {
            // Arrange
            var service = CreateService();
            _gameBridge.PlayerPosition = new Vector3(0, 0, 0);

            // Create peds at various distances
            CreateAndAddPed(1, position: new Vector3(10, 0, 0));   // Distance 10
            CreateAndAddPed(2, position: new Vector3(100, 0, 0));  // Distance 100
            CreateAndAddPed(3, position: new Vector3(200, 0, 0));  // Distance 200

            // Act
            var result = service.DespawnPedsByDistance(150);

            // Assert
            Assert.Single(result);
            Assert.Equal(3, result.First().Handle);
            Assert.True(_pedPool.Contains(1));
            Assert.True(_pedPool.Contains(2));
            Assert.False(_pedPool.Contains(3));
        }

        [Fact]
        public void DespawnPedsByDistance_ShouldRemoveAllFarPeds()
        {
            // Arrange
            var service = CreateService();
            _gameBridge.PlayerPosition = new Vector3(0, 0, 0);

            CreateAndAddPed(1, position: new Vector3(100, 0, 0));
            CreateAndAddPed(2, position: new Vector3(150, 0, 0));
            CreateAndAddPed(3, position: new Vector3(200, 0, 0));

            // Act
            var result = service.DespawnPedsByDistance(50);

            // Assert
            Assert.Equal(3, result.Count);
            Assert.Empty(_pedPool.GetAll());
        }

        [Fact]
        public void DespawnPedsByDistance_ShouldKeepAllNearPeds()
        {
            // Arrange
            var service = CreateService();
            _gameBridge.PlayerPosition = new Vector3(0, 0, 0);

            CreateAndAddPed(1, position: new Vector3(10, 0, 0));
            CreateAndAddPed(2, position: new Vector3(20, 0, 0));

            // Act
            var result = service.DespawnPedsByDistance(100);

            // Assert
            Assert.Empty(result);
            Assert.Equal(2, _pedPool.Count);
        }

        [Fact]
        public void DespawnPedsByDistance_ShouldThrowForNegativeDistance()
        {
            // Arrange
            var service = CreateService();

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => service.DespawnPedsByDistance(-10));
        }

        [Fact]
        public void DespawnPedsByDistance_ShouldUse3DDistance()
        {
            // Arrange
            var service = CreateService();
            _gameBridge.PlayerPosition = new Vector3(0, 0, 0);

            // Create ped at 3D position (3, 4, 0) = distance 5
            CreateAndAddPed(1, position: new Vector3(3, 4, 0));
            // Create ped at 3D position (0, 0, 10) = distance 10
            CreateAndAddPed(2, position: new Vector3(0, 0, 10));

            // Act
            var result = service.DespawnPedsByDistance(7);

            // Assert
            Assert.Single(result);
            Assert.Equal(2, result.First().Handle);
        }

        #endregion

        #region Despawn Marked For Deletion

        [Fact]
        public void DespawnMarkedForDeletion_ShouldRemoveMarkedPeds()
        {
            // Arrange
            var service = CreateService();
            var ped1 = CreateAndAddPed(1);
            var ped2 = CreateAndAddPed(2);
            var ped3 = CreateAndAddPed(3);

            ped1.MarkForDeletion();
            ped3.MarkForDeletion();

            // Act
            var result = service.DespawnMarkedForDeletion();

            // Assert
            Assert.Equal(2, result.Count);
            Assert.False(_pedPool.Contains(1));
            Assert.True(_pedPool.Contains(2));
            Assert.False(_pedPool.Contains(3));
        }

        [Fact]
        public void DespawnMarkedForDeletion_ShouldDeleteFromGame()
        {
            // Arrange
            var service = CreateService();
            var ped = CreateAndAddPed(1);
            ped.MarkForDeletion();

            // Act
            service.DespawnMarkedForDeletion();

            // Assert
            Assert.False(_gameBridge.PedExists(1));
        }

        [Fact]
        public void DespawnMarkedForDeletion_ShouldReturnEmptyWhenNoneMarked()
        {
            // Arrange
            var service = CreateService();
            CreateAndAddPed(1);
            CreateAndAddPed(2);

            // Act
            var result = service.DespawnMarkedForDeletion();

            // Assert
            Assert.Empty(result);
        }

        #endregion

        #region Despawn By Zone

        [Fact]
        public void DespawnPedsByZone_ShouldRemovePedsInZone()
        {
            // Arrange
            var service = CreateService();
            CreateAndAddPed(1, zoneId: "zone_vinewood");
            CreateAndAddPed(2, zoneId: "zone_vinewood");
            CreateAndAddPed(3, zoneId: "zone_downtown");

            // Act
            var result = service.DespawnPedsByZone("zone_vinewood");

            // Assert
            Assert.Equal(2, result.Count);
            Assert.False(_pedPool.Contains(1));
            Assert.False(_pedPool.Contains(2));
            Assert.True(_pedPool.Contains(3));
        }

        [Fact]
        public void DespawnPedsByZone_ShouldDeleteFromGame()
        {
            // Arrange
            var service = CreateService();
            CreateAndAddPed(1, zoneId: "zone_vinewood");

            // Act
            service.DespawnPedsByZone("zone_vinewood");

            // Assert
            Assert.False(_gameBridge.PedExists(1));
        }

        [Fact]
        public void DespawnPedsByZone_ShouldThrowForNullZoneId()
        {
            // Arrange
            var service = CreateService();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => service.DespawnPedsByZone(null!));
        }

        [Fact]
        public void DespawnPedsByZone_ShouldReturnEmptyForNonexistentZone()
        {
            // Arrange
            var service = CreateService();
            CreateAndAddPed(1, zoneId: "zone_vinewood");

            // Act
            var result = service.DespawnPedsByZone("zone_nowhere");

            // Assert
            Assert.Empty(result);
        }

        #endregion

        #region Despawn By Faction

        [Fact]
        public void DespawnPedsByFaction_ShouldRemovePedsOfFaction()
        {
            // Arrange
            var service = CreateService();
            CreateAndAddPed(1, factionId: "faction_michael");
            CreateAndAddPed(2, factionId: "faction_michael");
            CreateAndAddPed(3, factionId: "faction_trevor");

            // Act
            var result = service.DespawnPedsByFaction("faction_michael");

            // Assert
            Assert.Equal(2, result.Count);
            Assert.False(_pedPool.Contains(1));
            Assert.False(_pedPool.Contains(2));
            Assert.True(_pedPool.Contains(3));
        }

        [Fact]
        public void DespawnPedsByFaction_ShouldDeleteFromGame()
        {
            // Arrange
            var service = CreateService();
            CreateAndAddPed(1, factionId: "faction_michael");

            // Act
            service.DespawnPedsByFaction("faction_michael");

            // Assert
            Assert.False(_gameBridge.PedExists(1));
        }

        [Fact]
        public void DespawnPedsByFaction_ShouldThrowForNullFactionId()
        {
            // Arrange
            var service = CreateService();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => service.DespawnPedsByFaction(null!));
        }

        #endregion

        #region Despawn All

        [Fact]
        public void DespawnAll_ShouldRemoveAllPeds()
        {
            // Arrange
            var service = CreateService();
            CreateAndAddPed(1);
            CreateAndAddPed(2);
            CreateAndAddPed(3);

            // Act
            var result = service.DespawnAll();

            // Assert
            Assert.Equal(3, result.Count);
            Assert.Equal(0, _pedPool.Count);
        }

        [Fact]
        public void DespawnAll_ShouldDeleteAllFromGame()
        {
            // Arrange
            var service = CreateService();
            CreateAndAddPed(1);
            CreateAndAddPed(2);

            // Act
            service.DespawnAll();

            // Assert
            Assert.False(_gameBridge.PedExists(1));
            Assert.False(_gameBridge.PedExists(2));
        }

        [Fact]
        public void DespawnAll_ShouldReturnEmptyForEmptyPool()
        {
            // Arrange
            var service = CreateService();

            // Act
            var result = service.DespawnAll();

            // Assert
            Assert.Empty(result);
        }

        #endregion

        #region Despawn Oldest

        [Fact]
        public void DespawnOldest_ShouldRemoveOldestPeds()
        {
            // Arrange
            var service = CreateService();
            // Create peds with slightly different timestamps (order matters)
            var ped1 = new PedHandle(1, "faction_michael", new Vector3(0, 0, 0), "model", "zone1");
            _pedPool.Add(ped1);
            _gameBridge.CreatePed("model", new Vector3(0, 0, 0));

            System.Threading.Thread.Sleep(10); // Ensure different timestamps

            var ped2 = new PedHandle(2, "faction_michael", new Vector3(0, 0, 0), "model", "zone1");
            _pedPool.Add(ped2);
            _gameBridge.CreatePed("model", new Vector3(0, 0, 0));

            System.Threading.Thread.Sleep(10);

            var ped3 = new PedHandle(3, "faction_michael", new Vector3(0, 0, 0), "model", "zone1");
            _pedPool.Add(ped3);
            _gameBridge.CreatePed("model", new Vector3(0, 0, 0));

            // Act
            var result = service.DespawnOldest(2);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Contains(result, p => p.Handle == 1);
            Assert.Contains(result, p => p.Handle == 2);
            Assert.False(_pedPool.Contains(1));
            Assert.False(_pedPool.Contains(2));
            Assert.True(_pedPool.Contains(3));
        }

        [Fact]
        public void DespawnOldest_ShouldThrowForNegativeCount()
        {
            // Arrange
            var service = CreateService();

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => service.DespawnOldest(-1));
        }

        [Fact]
        public void DespawnOldest_ShouldReturnEmptyForZeroCount()
        {
            // Arrange
            var service = CreateService();
            CreateAndAddPed(1);

            // Act
            var result = service.DespawnOldest(0);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void DespawnOldest_ShouldHandleCountGreaterThanPoolSize()
        {
            // Arrange
            var service = CreateService();
            CreateAndAddPed(1);
            CreateAndAddPed(2);

            // Act
            var result = service.DespawnOldest(10);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Equal(0, _pedPool.Count);
        }

        #endregion

        #region Despawn By Faction And Zone

        [Fact]
        public void DespawnPedsByFactionAndZone_ShouldRemoveMatchingPeds()
        {
            // Arrange
            var service = CreateService();
            CreateAndAddPed(1, factionId: "faction_michael", zoneId: "zone_vinewood");
            CreateAndAddPed(2, factionId: "faction_michael", zoneId: "zone_vinewood");
            CreateAndAddPed(3, factionId: "faction_michael", zoneId: "zone_downtown");
            CreateAndAddPed(4, factionId: "faction_trevor", zoneId: "zone_vinewood");

            // Act
            var result = service.DespawnPedsByFactionAndZone("faction_michael", "zone_vinewood");

            // Assert
            Assert.Equal(2, result.Count);
            Assert.False(_pedPool.Contains(1));
            Assert.False(_pedPool.Contains(2));
            Assert.True(_pedPool.Contains(3));
            Assert.True(_pedPool.Contains(4));
        }

        [Fact]
        public void DespawnPedsByFactionAndZone_ShouldThrowForNullFactionId()
        {
            // Arrange
            var service = CreateService();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                service.DespawnPedsByFactionAndZone(null!, "zone_vinewood"));
        }

        [Fact]
        public void DespawnPedsByFactionAndZone_ShouldThrowForNullZoneId()
        {
            // Arrange
            var service = CreateService();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                service.DespawnPedsByFactionAndZone("faction_michael", null!));
        }

        #endregion
    }
}
