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
    /// Tests for ped recycling functionality.
    /// Following TDD - these tests define the expected behavior for the IPedRecyclingService.
    /// </summary>
    public class PedRecyclingServiceTests
    {
        #region Test Setup

        private MockGameBridge _gameBridge;
        private IPedPool _pedPool;

        public PedRecyclingServiceTests()
        {
            _gameBridge = new MockGameBridge();
            _pedPool = new InMemoryPedPool(30);
        }

        private IPedRecyclingService CreateService()
        {
            return new PedRecyclingService(_gameBridge, _pedPool);
        }

        private PedHandle CreateAndAddPed(int handle, string factionId = "faction_michael", string? zoneId = "zone1", Vector3? position = null, bool isDead = false)
        {
            var pos = position ?? new Vector3(0, 0, 0);
            var ped = new PedHandle(handle, factionId, pos, "model_a", zoneId);
            _pedPool.Add(ped);
            // Simulate ped creation in game
            _gameBridge.CreatePed("model_a", pos);

            if (isDead)
            {
                _gameBridge.KillPed(handle);
            }

            return ped;
        }

        #endregion

        #region Constructor Validation

        [Fact]
        public void Constructor_ShouldThrowForNullGameBridge()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new PedRecyclingService(null!, _pedPool));
        }

        [Fact]
        public void Constructor_ShouldThrowForNullPedPool()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new PedRecyclingService(_gameBridge, null!));
        }

        #endregion

        #region GetRecyclablePeds

        [Fact]
        public void GetRecyclablePeds_ShouldReturnDeadPeds()
        {
            // Arrange
            var service = CreateService();
            CreateAndAddPed(1, isDead: false);
            CreateAndAddPed(2, isDead: true);
            CreateAndAddPed(3, isDead: true);

            // Act
            var result = service.GetRecyclablePeds().ToList();

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Contains(result, p => p.Handle == 2);
            Assert.Contains(result, p => p.Handle == 3);
        }

        [Fact]
        public void GetRecyclablePeds_ShouldReturnMarkedForDeletionPeds()
        {
            // Arrange
            var service = CreateService();
            var ped1 = CreateAndAddPed(1);
            var ped2 = CreateAndAddPed(2);
            ped2.MarkForDeletion();

            // Act
            var result = service.GetRecyclablePeds().ToList();

            // Assert
            Assert.Single(result);
            Assert.Equal(2, result.First().Handle);
        }

        [Fact]
        public void GetRecyclablePeds_ShouldReturnEmptyWhenNoneRecyclable()
        {
            // Arrange
            var service = CreateService();
            CreateAndAddPed(1, isDead: false);
            CreateAndAddPed(2, isDead: false);

            // Act
            var result = service.GetRecyclablePeds().ToList();

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void GetRecyclablePeds_ShouldNotIncludeAlreadyRecycledPeds()
        {
            // Arrange
            var service = CreateService();
            var ped = CreateAndAddPed(1, isDead: true);
            ped.MarkAsRecycled();

            // Act
            var result = service.GetRecyclablePeds().ToList();

            // Assert
            Assert.Empty(result);
        }

        #endregion

        #region GetRecyclableCount

        [Fact]
        public void GetRecyclableCount_ShouldReturnCorrectCount()
        {
            // Arrange
            var service = CreateService();
            CreateAndAddPed(1, isDead: false);
            CreateAndAddPed(2, isDead: true);
            CreateAndAddPed(3, isDead: true);
            var ped4 = CreateAndAddPed(4, isDead: false);
            ped4.MarkForDeletion();

            // Act
            var count = service.GetRecyclableCount();

            // Assert
            Assert.Equal(3, count);
        }

        [Fact]
        public void GetRecyclableCount_ShouldReturnZeroWhenEmpty()
        {
            // Arrange
            var service = CreateService();

            // Act
            var count = service.GetRecyclableCount();

            // Assert
            Assert.Equal(0, count);
        }

        #endregion

        #region HasRecyclablePeds

        [Fact]
        public void HasRecyclablePeds_ShouldReturnTrueWhenDeadPedsExist()
        {
            // Arrange
            var service = CreateService();
            CreateAndAddPed(1, isDead: true);

            // Act & Assert
            Assert.True(service.HasRecyclablePeds());
        }

        [Fact]
        public void HasRecyclablePeds_ShouldReturnTrueWhenMarkedPedsExist()
        {
            // Arrange
            var service = CreateService();
            var ped = CreateAndAddPed(1);
            ped.MarkForDeletion();

            // Act & Assert
            Assert.True(service.HasRecyclablePeds());
        }

        [Fact]
        public void HasRecyclablePeds_ShouldReturnFalseWhenNoneRecyclable()
        {
            // Arrange
            var service = CreateService();
            CreateAndAddPed(1, isDead: false);

            // Act & Assert
            Assert.False(service.HasRecyclablePeds());
        }

        #endregion

        #region GetNextRecyclableCandidate

        [Fact]
        public void GetNextRecyclableCandidate_ShouldReturnDeadPedFirst()
        {
            // Arrange
            var service = CreateService();
            var ped1 = CreateAndAddPed(1, isDead: false);
            ped1.MarkForDeletion();
            CreateAndAddPed(2, isDead: true);

            // Act
            var result = service.GetNextRecyclableCandidate();

            // Assert - dead peds should be preferred over marked
            Assert.NotNull(result);
            Assert.Equal(2, result!.Handle);
        }

        [Fact]
        public void GetNextRecyclableCandidate_ShouldReturnNullWhenNoneAvailable()
        {
            // Arrange
            var service = CreateService();
            CreateAndAddPed(1, isDead: false);

            // Act
            var result = service.GetNextRecyclableCandidate();

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetNextRecyclableCandidate_ShouldReturnMarkedIfNoDeadPeds()
        {
            // Arrange
            var service = CreateService();
            var ped = CreateAndAddPed(1, isDead: false);
            ped.MarkForDeletion();

            // Act
            var result = service.GetNextRecyclableCandidate();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result!.Handle);
        }

        #endregion

        #region RecyclePed (by PedHandle)

        [Fact]
        public void RecyclePed_ShouldReviveDeadPed()
        {
            // Arrange
            var service = CreateService();
            var ped = CreateAndAddPed(1, isDead: true);

            // Act
            var result = service.RecyclePed(ped, "faction_trevor", new Vector3(100, 100, 0), "zone2");

            // Assert
            Assert.NotNull(result);
            Assert.True(_gameBridge.IsPedAlive(1));
        }

        [Fact]
        public void RecyclePed_ShouldMoveToNewPosition()
        {
            // Arrange
            var service = CreateService();
            var ped = CreateAndAddPed(1, isDead: true, position: new Vector3(0, 0, 0));
            var newPosition = new Vector3(100, 200, 50);

            // Act
            var result = service.RecyclePed(ped, "faction_trevor", newPosition, "zone2");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(newPosition, _gameBridge.GetPedPosition(1));
        }

        [Fact]
        public void RecyclePed_ShouldChangeRelationshipGroup()
        {
            // Arrange
            var service = CreateService();
            var ped = CreateAndAddPed(1, factionId: "faction_michael", isDead: true);
            _gameBridge.SetPedRelationshipGroup(1, "FACTION_MICHAEL");

            // Act
            var result = service.RecyclePed(ped, "faction_trevor", new Vector3(0, 0, 0), "zone2");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("FACTION_TREVOR", _gameBridge.GetPedRelationshipGroup(1));
        }

        [Fact]
        public void RecyclePed_ShouldReturnNewPedHandleWithUpdatedMetadata()
        {
            // Arrange
            var service = CreateService();
            var ped = CreateAndAddPed(1, factionId: "faction_michael", zoneId: "zone1", isDead: true);
            var newPosition = new Vector3(100, 100, 0);

            // Act
            var result = service.RecyclePed(ped, "faction_trevor", newPosition, "zone2");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result!.Handle); // Same underlying handle
            Assert.Equal("faction_trevor", result.FactionId);
            Assert.Equal("zone2", result.ZoneId);
            Assert.Equal(newPosition, result.SpawnPosition);
        }

        [Fact]
        public void RecyclePed_ShouldUpdatePoolWithNewPedHandle()
        {
            // Arrange
            var service = CreateService();
            var ped = CreateAndAddPed(1, factionId: "faction_michael", isDead: true);

            // Act
            service.RecyclePed(ped, "faction_trevor", new Vector3(0, 0, 0), "zone2");

            // Assert
            var poolPed = _pedPool.GetByHandle(1);
            Assert.NotNull(poolPed);
            Assert.Equal("faction_trevor", poolPed!.FactionId);
        }

        [Fact]
        public void RecyclePed_ShouldReturnNullForAlivePed()
        {
            // Arrange
            var service = CreateService();
            var ped = CreateAndAddPed(1, isDead: false);

            // Act
            var result = service.RecyclePed(ped, "faction_trevor", new Vector3(0, 0, 0), "zone2");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void RecyclePed_ShouldWorkForMarkedPed()
        {
            // Arrange
            var service = CreateService();
            var ped = CreateAndAddPed(1, isDead: false);
            ped.MarkForDeletion();

            // Act
            var result = service.RecyclePed(ped, "faction_trevor", new Vector3(0, 0, 0), "zone2");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("faction_trevor", result!.FactionId);
        }

        [Fact]
        public void RecyclePed_ShouldThrowForNullPed()
        {
            // Arrange
            var service = CreateService();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                service.RecyclePed((PedHandle)null!, "faction_trevor", new Vector3(0, 0, 0), "zone2"));
        }

        [Fact]
        public void RecyclePed_ShouldThrowForNullFactionId()
        {
            // Arrange
            var service = CreateService();
            var ped = CreateAndAddPed(1, isDead: true);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                service.RecyclePed(ped, null!, new Vector3(0, 0, 0), "zone2"));
        }

        [Fact]
        public void RecyclePed_ShouldThrowForEmptyFactionId()
        {
            // Arrange
            var service = CreateService();
            var ped = CreateAndAddPed(1, isDead: true);

            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                service.RecyclePed(ped, "", new Vector3(0, 0, 0), "zone2"));
        }

        [Fact]
        public void RecyclePed_ShouldReturnNullForInvalidPed()
        {
            // Arrange
            var service = CreateService();

            // Act
            var result = service.RecyclePed(PedHandle.Invalid, "faction_trevor", new Vector3(0, 0, 0), "zone2");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void RecyclePed_ShouldReturnNullForPedNotInPool()
        {
            // Arrange
            var service = CreateService();
            var ped = new PedHandle(999); // Not in pool

            // Act
            var result = service.RecyclePed(ped, "faction_trevor", new Vector3(0, 0, 0), "zone2");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void RecyclePed_ShouldAllowNullZoneId()
        {
            // Arrange
            var service = CreateService();
            var ped = CreateAndAddPed(1, isDead: true);

            // Act
            var result = service.RecyclePed(ped, "faction_trevor", new Vector3(0, 0, 0), null);

            // Assert
            Assert.NotNull(result);
            Assert.Null(result!.ZoneId);
        }

        #endregion

        #region RecyclePed (by handle)

        [Fact]
        public void RecyclePedByHandle_ShouldRecyclePed()
        {
            // Arrange
            var service = CreateService();
            CreateAndAddPed(1, isDead: true);

            // Act
            var result = service.RecyclePed(1, "faction_trevor", new Vector3(0, 0, 0), "zone2");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result!.Handle);
            Assert.Equal("faction_trevor", result.FactionId);
        }

        [Fact]
        public void RecyclePedByHandle_ShouldReturnNullForNonexistentHandle()
        {
            // Arrange
            var service = CreateService();

            // Act
            var result = service.RecyclePed(999, "faction_trevor", new Vector3(0, 0, 0), "zone2");

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region RecycleDeadPeds

        [Fact]
        public void RecycleDeadPeds_ShouldRecycleAllDeadPeds()
        {
            // Arrange
            var service = CreateService();
            CreateAndAddPed(1, isDead: true);
            CreateAndAddPed(2, isDead: true);
            CreateAndAddPed(3, isDead: false);

            // Act
            var result = service.RecycleDeadPeds("faction_trevor", new Vector3(100, 100, 0), "zone2", 10);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.All(result, p => Assert.Equal("faction_trevor", p.FactionId));
        }

        [Fact]
        public void RecycleDeadPeds_ShouldRespectMaxCount()
        {
            // Arrange
            var service = CreateService();
            CreateAndAddPed(1, isDead: true);
            CreateAndAddPed(2, isDead: true);
            CreateAndAddPed(3, isDead: true);

            // Act
            var result = service.RecycleDeadPeds("faction_trevor", new Vector3(100, 100, 0), "zone2", 2);

            // Assert
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public void RecycleDeadPeds_ShouldThrowForNegativeMaxCount()
        {
            // Arrange
            var service = CreateService();

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                service.RecycleDeadPeds("faction_trevor", new Vector3(0, 0, 0), "zone2", -1));
        }

        [Fact]
        public void RecycleDeadPeds_ShouldReturnEmptyWhenNoDeadPeds()
        {
            // Arrange
            var service = CreateService();
            CreateAndAddPed(1, isDead: false);

            // Act
            var result = service.RecycleDeadPeds("faction_trevor", new Vector3(0, 0, 0), "zone2", 10);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void RecycleDeadPeds_ShouldThrowForNullFactionId()
        {
            // Arrange
            var service = CreateService();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                service.RecycleDeadPeds(null!, new Vector3(0, 0, 0), "zone2", 10));
        }

        [Fact]
        public void RecycleDeadPeds_ShouldReviveAllRecycledPeds()
        {
            // Arrange
            var service = CreateService();
            CreateAndAddPed(1, isDead: true);
            CreateAndAddPed(2, isDead: true);

            // Act
            service.RecycleDeadPeds("faction_trevor", new Vector3(0, 0, 0), "zone2", 10);

            // Assert
            Assert.True(_gameBridge.IsPedAlive(1));
            Assert.True(_gameBridge.IsPedAlive(2));
        }

        #endregion

        #region MarkAsRecycled

        [Fact]
        public void MarkAsRecycled_ShouldMarkPed()
        {
            // Arrange
            var service = CreateService();
            var ped = CreateAndAddPed(1);

            // Act
            var result = service.MarkAsRecycled(ped);

            // Assert
            Assert.True(result);
            Assert.True(ped.IsRecycled);
        }

        [Fact]
        public void MarkAsRecycled_ShouldReturnFalseForNullPed()
        {
            // Arrange
            var service = CreateService();

            // Act
            var result = service.MarkAsRecycled(null!);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void MarkAsRecycled_ShouldReturnFalseForInvalidPed()
        {
            // Arrange
            var service = CreateService();

            // Act
            var result = service.MarkAsRecycled(PedHandle.Invalid);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region Integration Tests

        [Fact]
        public void RecycledPed_ShouldBeCountedInNewFaction()
        {
            // Arrange
            var service = CreateService();
            CreateAndAddPed(1, factionId: "faction_michael", isDead: true);

            // Act
            service.RecyclePed(1, "faction_trevor", new Vector3(0, 0, 0), "zone2");

            // Assert
            Assert.Equal(0, _pedPool.GetFactionCount("faction_michael"));
            Assert.Equal(1, _pedPool.GetFactionCount("faction_trevor"));
        }

        [Fact]
        public void RecycledPed_ShouldBeCountedInNewZone()
        {
            // Arrange
            var service = CreateService();
            CreateAndAddPed(1, zoneId: "zone1", isDead: true);

            // Act
            service.RecyclePed(1, "faction_trevor", new Vector3(0, 0, 0), "zone2");

            // Assert
            Assert.Equal(0, _pedPool.GetZoneCount("zone1"));
            Assert.Equal(1, _pedPool.GetZoneCount("zone2"));
        }

        [Fact]
        public void RecycledPed_ShouldNotIncreaseTotalPoolCount()
        {
            // Arrange
            var service = CreateService();
            CreateAndAddPed(1, isDead: true);
            CreateAndAddPed(2, isDead: false);
            var initialCount = _pedPool.Count;

            // Act
            service.RecyclePed(1, "faction_trevor", new Vector3(0, 0, 0), "zone2");

            // Assert
            Assert.Equal(initialCount, _pedPool.Count);
        }

        [Fact]
        public void RecycledPed_ShouldHaveNewCreatedAtTime()
        {
            // Arrange
            var service = CreateService();
            var ped = CreateAndAddPed(1, isDead: true);
            var originalCreatedAt = ped.CreatedAt;

            // Small delay to ensure time difference
            System.Threading.Thread.Sleep(10);

            // Act
            var result = service.RecyclePed(ped, "faction_trevor", new Vector3(0, 0, 0), "zone2");

            // Assert
            Assert.NotNull(result);
            Assert.True(result!.CreatedAt >= originalCreatedAt);
        }

        #endregion
    }
}
