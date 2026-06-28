using System.Collections.Generic;
using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Models;
using FactionWars.Combat.Services;
using FactionWars.Core.Models;
using Xunit;

namespace FactionWars.Tests.Unit.Combat
{
    public class DefenderScalingServiceTests
    {
        private readonly IDefenderScalingService _service;

        public DefenderScalingServiceTests()
        {
            _service = new DefenderScalingService();
        }

        [Fact]
        public void CalculateSpawnPlan_WithNoAllocation_ShouldReturnEmptyPlan()
        {
            // Arrange
            var allocation = new Dictionary<DefenderRole, int>
            {
                { DefenderRole.Grunt, 0 },
                { DefenderRole.Gunner, 0 },
                { DefenderRole.Rifleman, 0 }
            };

            // Act
            var plan = _service.CalculateSpawnPlan(allocation, maxPeds: 10);

            // Assert
            Assert.Equal(0, plan.TotalPeds);
            Assert.Equal(0, plan.GetPedCount(DefenderRole.Grunt));
            Assert.Equal(0, plan.GetPedCount(DefenderRole.Gunner));
            Assert.Equal(0, plan.GetPedCount(DefenderRole.Rifleman));
        }

        [Fact]
        public void CalculateSpawnPlan_WithSmallAllocation_ShouldReturnMinimumPeds()
        {
            // Arrange - 5 basic troops should spawn at least 1 ped
            var allocation = new Dictionary<DefenderRole, int>
            {
                { DefenderRole.Grunt, 5 },
                { DefenderRole.Gunner, 0 },
                { DefenderRole.Rifleman, 0 }
            };

            // Act
            var plan = _service.CalculateSpawnPlan(allocation, maxPeds: 10);

            // Assert
            Assert.True(plan.TotalPeds >= 1);
            Assert.True(plan.GetPedCount(DefenderRole.Grunt) >= 1);
        }

        [Fact]
        public void CalculateSpawnPlan_WithLargeAllocation_ShouldRespectMaxPeds()
        {
            // Arrange - 100 troops should not exceed max peds
            var allocation = new Dictionary<DefenderRole, int>
            {
                { DefenderRole.Grunt, 50 },
                { DefenderRole.Gunner, 30 },
                { DefenderRole.Rifleman, 20 }
            };

            // Act
            var plan = _service.CalculateSpawnPlan(allocation, maxPeds: 10);

            // Assert
            Assert.True(plan.TotalPeds <= 10);
        }

        [Fact]
        public void CalculateSpawnPlan_ShouldMaintainTierProportions()
        {
            // Arrange - equal allocation should result in roughly equal peds
            var allocation = new Dictionary<DefenderRole, int>
            {
                { DefenderRole.Grunt, 30 },
                { DefenderRole.Gunner, 30 },
                { DefenderRole.Rifleman, 30 }
            };

            // Act
            var plan = _service.CalculateSpawnPlan(allocation, maxPeds: 9);

            // Assert - should have some of each tier
            Assert.Equal(3, plan.GetPedCount(DefenderRole.Grunt));
            Assert.Equal(3, plan.GetPedCount(DefenderRole.Gunner));
            Assert.Equal(3, plan.GetPedCount(DefenderRole.Rifleman));
        }

        [Fact]
        public void CalculateSpawnPlan_WithOnlyHeavyTroops_ShouldOnlySpawnHeavy()
        {
            // Arrange
            var allocation = new Dictionary<DefenderRole, int>
            {
                { DefenderRole.Grunt, 0 },
                { DefenderRole.Gunner, 0 },
                { DefenderRole.Rifleman, 20 }
            };

            // Act
            var plan = _service.CalculateSpawnPlan(allocation, maxPeds: 5);

            // Assert
            Assert.Equal(0, plan.GetPedCount(DefenderRole.Grunt));
            Assert.Equal(0, plan.GetPedCount(DefenderRole.Gunner));
            Assert.True(plan.GetPedCount(DefenderRole.Rifleman) > 0);
        }

        [Fact]
        public void CalculateSpawnPlan_WithMixedTroops_ShouldDistributeProportionally()
        {
            // Arrange - 60 basic, 30 medium, 10 heavy = 6:3:1 ratio
            var allocation = new Dictionary<DefenderRole, int>
            {
                { DefenderRole.Grunt, 60 },
                { DefenderRole.Gunner, 30 },
                { DefenderRole.Rifleman, 10 }
            };

            // Act
            var plan = _service.CalculateSpawnPlan(allocation, maxPeds: 10);

            // Assert - should roughly maintain ratio (6 basic, 3 medium, 1 heavy)
            Assert.Equal(6, plan.GetPedCount(DefenderRole.Grunt));
            Assert.Equal(3, plan.GetPedCount(DefenderRole.Gunner));
            Assert.Equal(1, plan.GetPedCount(DefenderRole.Rifleman));
        }

        [Fact]
        public void CalculateSpawnPlan_WithNullAllocation_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<System.ArgumentNullException>(
                () => _service.CalculateSpawnPlan(null!, maxPeds: 10));
        }

        [Fact]
        public void CalculateSpawnPlan_WithZeroMaxPeds_ShouldReturnEmptyPlan()
        {
            // Arrange
            var allocation = new Dictionary<DefenderRole, int>
            {
                { DefenderRole.Grunt, 10 },
                { DefenderRole.Gunner, 10 },
                { DefenderRole.Rifleman, 10 }
            };

            // Act
            var plan = _service.CalculateSpawnPlan(allocation, maxPeds: 0);

            // Assert
            Assert.Equal(0, plan.TotalPeds);
        }

        [Fact]
        public void CalculateSpawnPlan_WithNegativeMaxPeds_ShouldThrowArgumentOutOfRangeException()
        {
            // Arrange
            var allocation = new Dictionary<DefenderRole, int>
            {
                { DefenderRole.Grunt, 10 }
            };

            // Act & Assert
            Assert.Throws<System.ArgumentOutOfRangeException>(
                () => _service.CalculateSpawnPlan(allocation, maxPeds: -1));
        }

        [Fact]
        public void CalculateScaledDefenderCount_WithZeroTroops_ShouldReturnZero()
        {
            // Act
            var count = _service.CalculateScaledDefenderCount(0, scaleFactor: 5);

            // Assert
            Assert.Equal(0, count);
        }

        [Fact]
        public void CalculateScaledDefenderCount_WithTroopsBelowScaleFactor_ShouldReturnOne()
        {
            // Act - 3 troops with scale factor 5 should still spawn 1
            var count = _service.CalculateScaledDefenderCount(3, scaleFactor: 5);

            // Assert
            Assert.Equal(1, count);
        }

        [Fact]
        public void CalculateScaledDefenderCount_WithTroopsEqualToScaleFactor_ShouldReturnOne()
        {
            // Act - 5 troops with scale factor 5 should spawn 1
            var count = _service.CalculateScaledDefenderCount(5, scaleFactor: 5);

            // Assert
            Assert.Equal(1, count);
        }

        [Fact]
        public void CalculateScaledDefenderCount_WithTroopsAboveScaleFactor_ShouldScaleUp()
        {
            // Act - 15 troops with scale factor 5 should spawn 3
            var count = _service.CalculateScaledDefenderCount(15, scaleFactor: 5);

            // Assert
            Assert.Equal(3, count);
        }

        [Fact]
        public void GetDefaultScaleFactor_ShouldReturnPositiveValue()
        {
            // Act
            var factor = _service.GetDefaultScaleFactor();

            // Assert
            Assert.True(factor > 0);
        }
    }
}
