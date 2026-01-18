using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Models;
using FactionWars.Combat.Pools;
using FactionWars.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace FactionWars.Tests.Unit.Combat
{
    /// <summary>
    /// Tests for IPedPool interface behavior.
    /// These tests define the contract that any ped pool implementation must satisfy.
    /// </summary>
    public class PedPoolTests
    {
        #region Test Setup

        private IPedPool CreatePool(int maxPeds = 30)
        {
            return new InMemoryPedPool(maxPeds);
        }

        private PedHandle CreateTestPed(
            int handle,
            string? factionId = null,
            string? zoneId = null,
            string? modelName = null)
        {
            return new PedHandle(
                handle,
                factionId: factionId,
                spawnPosition: new Vector3(100, 200, 50),
                modelName: modelName ?? "a_m_y_hipster_01",
                zoneId: zoneId);
        }

        #endregion

        #region Count and Capacity

        [Fact]
        public void PedPool_Count_ShouldBeZeroInitially()
        {
            // Arrange
            var pool = CreatePool();

            // Act & Assert
            Assert.Equal(0, pool.Count);
        }

        [Fact]
        public void PedPool_MaxCapacity_ShouldReturnConfiguredLimit()
        {
            // Arrange
            var pool = CreatePool(maxPeds: 50);

            // Act & Assert
            Assert.Equal(50, pool.MaxCapacity);
        }

        [Fact]
        public void PedPool_IsFull_ShouldBeFalseWhenEmpty()
        {
            // Arrange
            var pool = CreatePool(maxPeds: 10);

            // Act & Assert
            Assert.False(pool.IsFull);
        }

        [Fact]
        public void PedPool_IsFull_ShouldBeTrueWhenAtCapacity()
        {
            // Arrange
            var pool = CreatePool(maxPeds: 3);
            pool.Add(CreateTestPed(1));
            pool.Add(CreateTestPed(2));
            pool.Add(CreateTestPed(3));

            // Act & Assert
            Assert.True(pool.IsFull);
        }

        [Fact]
        public void PedPool_AvailableSlots_ShouldReturnRemainingCapacity()
        {
            // Arrange
            var pool = CreatePool(maxPeds: 10);
            pool.Add(CreateTestPed(1));
            pool.Add(CreateTestPed(2));

            // Act & Assert
            Assert.Equal(8, pool.AvailableSlots);
        }

        #endregion

        #region Add

        [Fact]
        public void PedPool_Add_ShouldIncrementCount()
        {
            // Arrange
            var pool = CreatePool();
            var ped = CreateTestPed(100);

            // Act
            pool.Add(ped);

            // Assert
            Assert.Equal(1, pool.Count);
        }

        [Fact]
        public void PedPool_Add_ShouldReturnTrueOnSuccess()
        {
            // Arrange
            var pool = CreatePool();
            var ped = CreateTestPed(100);

            // Act
            var result = pool.Add(ped);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void PedPool_Add_ShouldReturnFalseWhenPoolIsFull()
        {
            // Arrange
            var pool = CreatePool(maxPeds: 2);
            pool.Add(CreateTestPed(1));
            pool.Add(CreateTestPed(2));

            // Act
            var result = pool.Add(CreateTestPed(3));

            // Assert
            Assert.False(result);
            Assert.Equal(2, pool.Count);
        }

        [Fact]
        public void PedPool_Add_ShouldThrowForNullPed()
        {
            // Arrange
            var pool = CreatePool();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => pool.Add(null!));
        }

        [Fact]
        public void PedPool_Add_ShouldReturnFalseForDuplicateHandle()
        {
            // Arrange
            var pool = CreatePool();
            pool.Add(CreateTestPed(100));

            // Act
            var result = pool.Add(CreateTestPed(100, factionId: "different"));

            // Assert
            Assert.False(result);
            Assert.Equal(1, pool.Count);
        }

        [Fact]
        public void PedPool_Add_ShouldReturnFalseForInvalidHandle()
        {
            // Arrange
            var pool = CreatePool();
            var invalidPed = new PedHandle(-1);

            // Act
            var result = pool.Add(invalidPed);

            // Assert
            Assert.False(result);
            Assert.Equal(0, pool.Count);
        }

        #endregion

        #region Contains

        [Fact]
        public void PedPool_Contains_ShouldReturnTrueForExistingPed()
        {
            // Arrange
            var pool = CreatePool();
            var ped = CreateTestPed(100);
            pool.Add(ped);

            // Act
            var result = pool.Contains(100);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void PedPool_Contains_ShouldReturnFalseForNonExistentPed()
        {
            // Arrange
            var pool = CreatePool();

            // Act
            var result = pool.Contains(100);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void PedPool_ContainsPedHandle_ShouldReturnTrueForExistingPed()
        {
            // Arrange
            var pool = CreatePool();
            var ped = CreateTestPed(100);
            pool.Add(ped);

            // Act
            var result = pool.Contains(ped);

            // Assert
            Assert.True(result);
        }

        #endregion

        #region Get

        [Fact]
        public void PedPool_GetByHandle_ShouldReturnPedIfExists()
        {
            // Arrange
            var pool = CreatePool();
            var ped = CreateTestPed(100, factionId: "faction_michael");
            pool.Add(ped);

            // Act
            var result = pool.GetByHandle(100);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(100, result.Handle);
            Assert.Equal("faction_michael", result.FactionId);
        }

        [Fact]
        public void PedPool_GetByHandle_ShouldReturnNullIfNotExists()
        {
            // Arrange
            var pool = CreatePool();

            // Act
            var result = pool.GetByHandle(100);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void PedPool_GetAll_ShouldReturnAllPeds()
        {
            // Arrange
            var pool = CreatePool();
            pool.Add(CreateTestPed(1));
            pool.Add(CreateTestPed(2));
            pool.Add(CreateTestPed(3));

            // Act
            var result = pool.GetAll().ToList();

            // Assert
            Assert.Equal(3, result.Count);
            Assert.Contains(result, p => p.Handle == 1);
            Assert.Contains(result, p => p.Handle == 2);
            Assert.Contains(result, p => p.Handle == 3);
        }

        [Fact]
        public void PedPool_GetAll_ShouldReturnEmptyWhenPoolIsEmpty()
        {
            // Arrange
            var pool = CreatePool();

            // Act
            var result = pool.GetAll().ToList();

            // Assert
            Assert.Empty(result);
        }

        #endregion

        #region GetByFaction

        [Fact]
        public void PedPool_GetByFaction_ShouldReturnOnlyMatchingFactionPeds()
        {
            // Arrange
            var pool = CreatePool();
            pool.Add(CreateTestPed(1, factionId: "faction_michael"));
            pool.Add(CreateTestPed(2, factionId: "faction_trevor"));
            pool.Add(CreateTestPed(3, factionId: "faction_michael"));

            // Act
            var result = pool.GetByFaction("faction_michael").ToList();

            // Assert
            Assert.Equal(2, result.Count);
            Assert.All(result, p => Assert.Equal("faction_michael", p.FactionId));
        }

        [Fact]
        public void PedPool_GetByFaction_ShouldReturnEmptyWhenNoMatch()
        {
            // Arrange
            var pool = CreatePool();
            pool.Add(CreateTestPed(1, factionId: "faction_michael"));

            // Act
            var result = pool.GetByFaction("faction_trevor").ToList();

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void PedPool_GetByFaction_ShouldThrowForNullFactionId()
        {
            // Arrange
            var pool = CreatePool();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => pool.GetByFaction(null!).ToList());
        }

        [Fact]
        public void PedPool_GetFactionCount_ShouldReturnCorrectCount()
        {
            // Arrange
            var pool = CreatePool();
            pool.Add(CreateTestPed(1, factionId: "faction_michael"));
            pool.Add(CreateTestPed(2, factionId: "faction_trevor"));
            pool.Add(CreateTestPed(3, factionId: "faction_michael"));

            // Act
            var michaelCount = pool.GetFactionCount("faction_michael");
            var trevorCount = pool.GetFactionCount("faction_trevor");
            var franklinCount = pool.GetFactionCount("faction_franklin");

            // Assert
            Assert.Equal(2, michaelCount);
            Assert.Equal(1, trevorCount);
            Assert.Equal(0, franklinCount);
        }

        #endregion

        #region GetByZone

        [Fact]
        public void PedPool_GetByZone_ShouldReturnOnlyMatchingZonePeds()
        {
            // Arrange
            var pool = CreatePool();
            pool.Add(CreateTestPed(1, zoneId: "zone_vinewood"));
            pool.Add(CreateTestPed(2, zoneId: "zone_downtown"));
            pool.Add(CreateTestPed(3, zoneId: "zone_vinewood"));

            // Act
            var result = pool.GetByZone("zone_vinewood").ToList();

            // Assert
            Assert.Equal(2, result.Count);
            Assert.All(result, p => Assert.Equal("zone_vinewood", p.ZoneId));
        }

        [Fact]
        public void PedPool_GetByZone_ShouldReturnEmptyWhenNoMatch()
        {
            // Arrange
            var pool = CreatePool();
            pool.Add(CreateTestPed(1, zoneId: "zone_vinewood"));

            // Act
            var result = pool.GetByZone("zone_downtown").ToList();

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void PedPool_GetByZone_ShouldThrowForNullZoneId()
        {
            // Arrange
            var pool = CreatePool();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => pool.GetByZone(null!).ToList());
        }

        [Fact]
        public void PedPool_GetZoneCount_ShouldReturnCorrectCount()
        {
            // Arrange
            var pool = CreatePool();
            pool.Add(CreateTestPed(1, zoneId: "zone_vinewood"));
            pool.Add(CreateTestPed(2, zoneId: "zone_downtown"));
            pool.Add(CreateTestPed(3, zoneId: "zone_vinewood"));

            // Act
            var vinewoodCount = pool.GetZoneCount("zone_vinewood");
            var downtownCount = pool.GetZoneCount("zone_downtown");
            var airportCount = pool.GetZoneCount("zone_airport");

            // Assert
            Assert.Equal(2, vinewoodCount);
            Assert.Equal(1, downtownCount);
            Assert.Equal(0, airportCount);
        }

        #endregion

        #region GetByFactionAndZone

        [Fact]
        public void PedPool_GetByFactionAndZone_ShouldReturnMatchingPeds()
        {
            // Arrange
            var pool = CreatePool();
            pool.Add(CreateTestPed(1, factionId: "faction_michael", zoneId: "zone_vinewood"));
            pool.Add(CreateTestPed(2, factionId: "faction_michael", zoneId: "zone_downtown"));
            pool.Add(CreateTestPed(3, factionId: "faction_trevor", zoneId: "zone_vinewood"));

            // Act
            var result = pool.GetByFactionAndZone("faction_michael", "zone_vinewood").ToList();

            // Assert
            Assert.Single(result);
            Assert.Equal(1, result[0].Handle);
        }

        #endregion

        #region Remove

        [Fact]
        public void PedPool_Remove_ShouldDecrementCount()
        {
            // Arrange
            var pool = CreatePool();
            pool.Add(CreateTestPed(100));

            // Act
            pool.Remove(100);

            // Assert
            Assert.Equal(0, pool.Count);
        }

        [Fact]
        public void PedPool_Remove_ShouldReturnTrueWhenPedExists()
        {
            // Arrange
            var pool = CreatePool();
            pool.Add(CreateTestPed(100));

            // Act
            var result = pool.Remove(100);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void PedPool_Remove_ShouldReturnFalseWhenPedDoesNotExist()
        {
            // Arrange
            var pool = CreatePool();

            // Act
            var result = pool.Remove(100);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void PedPool_RemovePedHandle_ShouldRemovePed()
        {
            // Arrange
            var pool = CreatePool();
            var ped = CreateTestPed(100);
            pool.Add(ped);

            // Act
            var result = pool.Remove(ped);

            // Assert
            Assert.True(result);
            Assert.Equal(0, pool.Count);
        }

        #endregion

        #region Clear

        [Fact]
        public void PedPool_Clear_ShouldRemoveAllPeds()
        {
            // Arrange
            var pool = CreatePool();
            pool.Add(CreateTestPed(1));
            pool.Add(CreateTestPed(2));
            pool.Add(CreateTestPed(3));

            // Act
            pool.Clear();

            // Assert
            Assert.Equal(0, pool.Count);
        }

        [Fact]
        public void PedPool_Clear_ShouldReturnRemovedPeds()
        {
            // Arrange
            var pool = CreatePool();
            pool.Add(CreateTestPed(1));
            pool.Add(CreateTestPed(2));
            pool.Add(CreateTestPed(3));

            // Act
            var removed = pool.Clear();

            // Assert
            Assert.Equal(3, removed.Count());
        }

        #endregion

        #region GetMarkedForDeletion

        [Fact]
        public void PedPool_GetMarkedForDeletion_ShouldReturnOnlyMarkedPeds()
        {
            // Arrange
            var pool = CreatePool();
            var ped1 = CreateTestPed(1);
            var ped2 = CreateTestPed(2);
            var ped3 = CreateTestPed(3);
            ped1.MarkForDeletion();
            ped3.MarkForDeletion();
            pool.Add(ped1);
            pool.Add(ped2);
            pool.Add(ped3);

            // Act
            var result = pool.GetMarkedForDeletion().ToList();

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Contains(result, p => p.Handle == 1);
            Assert.Contains(result, p => p.Handle == 3);
        }

        [Fact]
        public void PedPool_GetMarkedForDeletion_ShouldReturnEmptyWhenNoMarkedPeds()
        {
            // Arrange
            var pool = CreatePool();
            pool.Add(CreateTestPed(1));
            pool.Add(CreateTestPed(2));

            // Act
            var result = pool.GetMarkedForDeletion().ToList();

            // Assert
            Assert.Empty(result);
        }

        #endregion

        #region RemoveMarkedForDeletion

        [Fact]
        public void PedPool_RemoveMarkedForDeletion_ShouldRemoveOnlyMarkedPeds()
        {
            // Arrange
            var pool = CreatePool();
            var ped1 = CreateTestPed(1);
            var ped2 = CreateTestPed(2);
            var ped3 = CreateTestPed(3);
            ped1.MarkForDeletion();
            ped3.MarkForDeletion();
            pool.Add(ped1);
            pool.Add(ped2);
            pool.Add(ped3);

            // Act
            var removed = pool.RemoveMarkedForDeletion().ToList();

            // Assert
            Assert.Equal(2, removed.Count);
            Assert.Equal(1, pool.Count);
            Assert.True(pool.Contains(2));
        }

        #endregion

        #region GetOldestPeds

        [Fact]
        public void PedPool_GetOldest_ShouldReturnPedsOrderedByAge()
        {
            // Arrange
            var pool = CreatePool();
            pool.Add(CreateTestPed(1));
            System.Threading.Thread.Sleep(5);
            pool.Add(CreateTestPed(2));
            System.Threading.Thread.Sleep(5);
            pool.Add(CreateTestPed(3));

            // Act
            var oldest = pool.GetOldest(2).ToList();

            // Assert
            Assert.Equal(2, oldest.Count);
            Assert.Equal(1, oldest[0].Handle);
            Assert.Equal(2, oldest[1].Handle);
        }

        [Fact]
        public void PedPool_GetOldest_ShouldReturnAllWhenCountExceedsPoolSize()
        {
            // Arrange
            var pool = CreatePool();
            pool.Add(CreateTestPed(1));
            pool.Add(CreateTestPed(2));

            // Act
            var oldest = pool.GetOldest(10).ToList();

            // Assert
            Assert.Equal(2, oldest.Count);
        }

        #endregion

        #region Thread Safety Check

        [Fact]
        public async System.Threading.Tasks.Task PedPool_ShouldNotThrowOnConcurrentRead()
        {
            // Arrange
            var pool = CreatePool();
            for (int i = 0; i < 10; i++)
            {
                pool.Add(CreateTestPed(i));
            }

            // Act & Assert - Should not throw
            var exception = await Record.ExceptionAsync(async () =>
            {
                var tasks = new List<System.Threading.Tasks.Task>();
                for (int i = 0; i < 10; i++)
                {
                    tasks.Add(System.Threading.Tasks.Task.Run(() =>
                    {
                        var _ = pool.GetAll().ToList();
                        var __ = pool.GetByFaction("faction_michael").ToList();
                    }));
                }
                await System.Threading.Tasks.Task.WhenAll(tasks);
            });

            Assert.Null(exception);
        }

        #endregion
    }
}
