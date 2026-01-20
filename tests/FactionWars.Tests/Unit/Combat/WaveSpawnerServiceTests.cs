using System.Collections.Generic;
using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Models;
using FactionWars.Combat.Services;
using FactionWars.Core.Models;
using Xunit;

namespace FactionWars.Tests.Unit.Combat
{
    public class WaveSpawnerServiceTests
    {
        [Fact]
        public void CreateWaveState_WithEmptyPlan_ShouldReturnEmptyState()
        {
            // Arrange
            var service = new WaveSpawnerService();
            var plan = new DefenderSpawnPlan(basicPeds: 0, mediumPeds: 0, heavyPeds: 0);

            // Act
            var state = service.CreateWaveState(plan);

            // Assert
            Assert.NotNull(state);
            Assert.Equal(0, state.TotalRemaining);
            Assert.True(state.IsComplete);
        }

        [Fact]
        public void CreateWaveState_WithPlan_ShouldTrackAllTiers()
        {
            // Arrange
            var service = new WaveSpawnerService();
            var plan = new DefenderSpawnPlan(basicPeds: 3, mediumPeds: 2, heavyPeds: 1);

            // Act
            var state = service.CreateWaveState(plan);

            // Assert
            Assert.Equal(6, state.TotalRemaining);
            Assert.Equal(3, state.GetRemaining(DefenderTier.Basic));
            Assert.Equal(2, state.GetRemaining(DefenderTier.Medium));
            Assert.Equal(1, state.GetRemaining(DefenderTier.Heavy));
            Assert.False(state.IsComplete);
        }

        [Fact]
        public void GetNextWaveTier_WithAllTiers_ShouldReturnHeavyFirst()
        {
            // Arrange
            var service = new WaveSpawnerService();
            var plan = new DefenderSpawnPlan(basicPeds: 3, mediumPeds: 2, heavyPeds: 1);
            var state = service.CreateWaveState(plan);

            // Act
            var nextTier = service.GetNextWaveTier(state);

            // Assert - Heavy spawns first (highest priority)
            Assert.Equal(DefenderTier.Heavy, nextTier);
        }

        [Fact]
        public void GetNextWaveTier_AfterHeavyComplete_ShouldReturnMedium()
        {
            // Arrange
            var service = new WaveSpawnerService();
            var plan = new DefenderSpawnPlan(basicPeds: 3, mediumPeds: 2, heavyPeds: 1);
            var state = service.CreateWaveState(plan);

            // Simulate Heavy wave complete
            state.RecordSpawned(DefenderTier.Heavy, 1);

            // Act
            var nextTier = service.GetNextWaveTier(state);

            // Assert - Medium spawns after Heavy
            Assert.Equal(DefenderTier.Medium, nextTier);
        }

        [Fact]
        public void GetNextWaveTier_AfterHeavyAndMediumComplete_ShouldReturnBasic()
        {
            // Arrange
            var service = new WaveSpawnerService();
            var plan = new DefenderSpawnPlan(basicPeds: 3, mediumPeds: 2, heavyPeds: 1);
            var state = service.CreateWaveState(plan);

            // Simulate Heavy and Medium waves complete
            state.RecordSpawned(DefenderTier.Heavy, 1);
            state.RecordSpawned(DefenderTier.Medium, 2);

            // Act
            var nextTier = service.GetNextWaveTier(state);

            // Assert - Basic spawns last
            Assert.Equal(DefenderTier.Basic, nextTier);
        }

        [Fact]
        public void GetNextWaveTier_WhenAllComplete_ShouldReturnNull()
        {
            // Arrange
            var service = new WaveSpawnerService();
            var plan = new DefenderSpawnPlan(basicPeds: 1, mediumPeds: 1, heavyPeds: 1);
            var state = service.CreateWaveState(plan);

            // Simulate all waves complete
            state.RecordSpawned(DefenderTier.Heavy, 1);
            state.RecordSpawned(DefenderTier.Medium, 1);
            state.RecordSpawned(DefenderTier.Basic, 1);

            // Act
            var nextTier = service.GetNextWaveTier(state);

            // Assert - No more tiers to spawn
            Assert.Null(nextTier);
        }

        [Fact]
        public void GetNextWaveTier_WithNoHeavy_ShouldSkipToMedium()
        {
            // Arrange
            var service = new WaveSpawnerService();
            var plan = new DefenderSpawnPlan(basicPeds: 3, mediumPeds: 2, heavyPeds: 0);
            var state = service.CreateWaveState(plan);

            // Act
            var nextTier = service.GetNextWaveTier(state);

            // Assert - Should skip Heavy since there are none
            Assert.Equal(DefenderTier.Medium, nextTier);
        }

        [Fact]
        public void GetNextWaveTier_WithOnlyBasic_ShouldReturnBasic()
        {
            // Arrange
            var service = new WaveSpawnerService();
            var plan = new DefenderSpawnPlan(basicPeds: 5, mediumPeds: 0, heavyPeds: 0);
            var state = service.CreateWaveState(plan);

            // Act
            var nextTier = service.GetNextWaveTier(state);

            // Assert - Should skip to Basic directly
            Assert.Equal(DefenderTier.Basic, nextTier);
        }

        [Fact]
        public void GetSpawnCountForWave_ShouldReturnRemainingForTier()
        {
            // Arrange
            var service = new WaveSpawnerService();
            var plan = new DefenderSpawnPlan(basicPeds: 3, mediumPeds: 2, heavyPeds: 1);
            var state = service.CreateWaveState(plan);

            // Act
            var heavyCount = service.GetSpawnCountForWave(state, DefenderTier.Heavy, maxToSpawn: 10);
            var mediumCount = service.GetSpawnCountForWave(state, DefenderTier.Medium, maxToSpawn: 10);
            var basicCount = service.GetSpawnCountForWave(state, DefenderTier.Basic, maxToSpawn: 10);

            // Assert
            Assert.Equal(1, heavyCount);
            Assert.Equal(2, mediumCount);
            Assert.Equal(3, basicCount);
        }

        [Fact]
        public void GetSpawnCountForWave_ShouldRespectMaxToSpawn()
        {
            // Arrange
            var service = new WaveSpawnerService();
            var plan = new DefenderSpawnPlan(basicPeds: 10, mediumPeds: 0, heavyPeds: 0);
            var state = service.CreateWaveState(plan);

            // Act
            var count = service.GetSpawnCountForWave(state, DefenderTier.Basic, maxToSpawn: 3);

            // Assert - Should only spawn up to max
            Assert.Equal(3, count);
        }

        [Fact]
        public void GetSpawnCountForWave_WhenTierComplete_ShouldReturnZero()
        {
            // Arrange
            var service = new WaveSpawnerService();
            var plan = new DefenderSpawnPlan(basicPeds: 3, mediumPeds: 2, heavyPeds: 1);
            var state = service.CreateWaveState(plan);
            state.RecordSpawned(DefenderTier.Heavy, 1);

            // Act
            var count = service.GetSpawnCountForWave(state, DefenderTier.Heavy, maxToSpawn: 10);

            // Assert - Heavy is complete, should be 0
            Assert.Equal(0, count);
        }

        [Fact]
        public void WaveState_RecordSpawned_ShouldDecrementRemaining()
        {
            // Arrange
            var service = new WaveSpawnerService();
            var plan = new DefenderSpawnPlan(basicPeds: 5, mediumPeds: 3, heavyPeds: 2);
            var state = service.CreateWaveState(plan);

            // Act
            state.RecordSpawned(DefenderTier.Heavy, 1);
            state.RecordSpawned(DefenderTier.Medium, 2);

            // Assert
            Assert.Equal(1, state.GetRemaining(DefenderTier.Heavy));
            Assert.Equal(1, state.GetRemaining(DefenderTier.Medium));
            Assert.Equal(5, state.GetRemaining(DefenderTier.Basic));
            Assert.Equal(7, state.TotalRemaining);
        }

        [Fact]
        public void WaveState_RecordSpawned_ShouldNotGoBelowZero()
        {
            // Arrange
            var service = new WaveSpawnerService();
            var plan = new DefenderSpawnPlan(basicPeds: 0, mediumPeds: 0, heavyPeds: 1);
            var state = service.CreateWaveState(plan);

            // Act - Record more than we have
            state.RecordSpawned(DefenderTier.Heavy, 5);

            // Assert - Should not go below 0
            Assert.Equal(0, state.GetRemaining(DefenderTier.Heavy));
        }

        [Fact]
        public void WaveState_IsComplete_ShouldBeTrueWhenAllSpawned()
        {
            // Arrange
            var service = new WaveSpawnerService();
            var plan = new DefenderSpawnPlan(basicPeds: 2, mediumPeds: 1, heavyPeds: 1);
            var state = service.CreateWaveState(plan);

            // Act
            state.RecordSpawned(DefenderTier.Heavy, 1);
            state.RecordSpawned(DefenderTier.Medium, 1);
            state.RecordSpawned(DefenderTier.Basic, 2);

            // Assert
            Assert.True(state.IsComplete);
            Assert.Equal(0, state.TotalRemaining);
        }

        [Fact]
        public void WaveState_GetSpawned_ShouldTrackSpawnedCounts()
        {
            // Arrange
            var service = new WaveSpawnerService();
            var plan = new DefenderSpawnPlan(basicPeds: 5, mediumPeds: 3, heavyPeds: 2);
            var state = service.CreateWaveState(plan);

            // Act
            state.RecordSpawned(DefenderTier.Heavy, 2);
            state.RecordSpawned(DefenderTier.Medium, 1);

            // Assert
            Assert.Equal(2, state.GetSpawned(DefenderTier.Heavy));
            Assert.Equal(1, state.GetSpawned(DefenderTier.Medium));
            Assert.Equal(0, state.GetSpawned(DefenderTier.Basic));
            Assert.Equal(3, state.TotalSpawned);
        }

        [Fact]
        public void GetWaveOrder_ShouldReturnHeavyMediumBasicOrder()
        {
            // Arrange
            var service = new WaveSpawnerService();

            // Act
            var order = service.GetWaveOrder();

            // Assert
            Assert.Equal(3, order.Count);
            Assert.Equal(DefenderTier.Heavy, order[0]);
            Assert.Equal(DefenderTier.Medium, order[1]);
            Assert.Equal(DefenderTier.Basic, order[2]);
        }

        [Fact]
        public void WaveState_IsTierComplete_ShouldReturnTrueWhenTierFullySpawned()
        {
            // Arrange
            var service = new WaveSpawnerService();
            var plan = new DefenderSpawnPlan(basicPeds: 3, mediumPeds: 2, heavyPeds: 1);
            var state = service.CreateWaveState(plan);

            // Act
            state.RecordSpawned(DefenderTier.Heavy, 1);

            // Assert
            Assert.True(state.IsTierComplete(DefenderTier.Heavy));
            Assert.False(state.IsTierComplete(DefenderTier.Medium));
            Assert.False(state.IsTierComplete(DefenderTier.Basic));
        }

        [Fact]
        public void CreateWaveState_WithNullPlan_ShouldThrowArgumentNullException()
        {
            // Arrange
            var service = new WaveSpawnerService();

            // Act & Assert
            Assert.Throws<System.ArgumentNullException>(() => service.CreateWaveState(null!));
        }
    }
}
