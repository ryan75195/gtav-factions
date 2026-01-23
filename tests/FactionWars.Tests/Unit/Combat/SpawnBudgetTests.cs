using FactionWars.Combat.Models;
using Xunit;

namespace FactionWars.Tests.Unit.Combat
{
    /// <summary>
    /// Tests for SpawnBudget model - manages the shared ped pool allocation.
    /// </summary>
    public class SpawnBudgetTests
    {
        #region Construction and Defaults

        [Fact]
        public void SpawnBudget_Constructor_ShouldUseDefaultMaxPedsOf30()
        {
            // Arrange & Act
            var budget = new SpawnBudget();

            // Assert
            Assert.Equal(30, budget.MaxTotalPeds);
        }

        [Fact]
        public void SpawnBudget_Constructor_ShouldUseDefaultMaxPerSideOf12()
        {
            // Arrange & Act
            var budget = new SpawnBudget();

            // Assert
            Assert.Equal(12, budget.MaxPerSide);
        }

        [Fact]
        public void SpawnBudget_Constructor_ShouldAllowCustomMaxPeds()
        {
            // Arrange & Act
            var budget = new SpawnBudget(maxTotalPeds: 50, maxPerSide: 15);

            // Assert
            Assert.Equal(50, budget.MaxTotalPeds);
            Assert.Equal(15, budget.MaxPerSide);
        }

        [Fact]
        public void SpawnBudget_Constructor_ShouldStartWithZeroAllocations()
        {
            // Arrange & Act
            var budget = new SpawnBudget();

            // Assert
            Assert.Equal(0, budget.AllocatedAttackers);
            Assert.Equal(0, budget.AllocatedDefenders);
        }

        #endregion

        #region Available Calculation

        [Fact]
        public void SpawnBudget_Available_ShouldReturnMaxWhenNothingAllocated()
        {
            // Arrange
            var budget = new SpawnBudget();

            // Act & Assert
            Assert.Equal(30, budget.Available);
        }

        [Fact]
        public void SpawnBudget_Available_ShouldDecrementWhenAttackersAllocated()
        {
            // Arrange
            var budget = new SpawnBudget();
            budget.AllocateAttacker();
            budget.AllocateAttacker();

            // Act & Assert
            Assert.Equal(28, budget.Available);
        }

        [Fact]
        public void SpawnBudget_Available_ShouldDecrementWhenDefendersAllocated()
        {
            // Arrange
            var budget = new SpawnBudget();
            budget.AllocateDefender();
            budget.AllocateDefender();
            budget.AllocateDefender();

            // Act & Assert
            Assert.Equal(27, budget.Available);
        }

        [Fact]
        public void SpawnBudget_Available_ShouldAccountForBothSides()
        {
            // Arrange
            var budget = new SpawnBudget();
            budget.AllocateAttacker();
            budget.AllocateAttacker();
            budget.AllocateDefender();
            budget.AllocateDefender();
            budget.AllocateDefender();

            // Act & Assert
            Assert.Equal(25, budget.Available);
        }

        #endregion

        #region CanSpawnAttacker

        [Fact]
        public void SpawnBudget_CanSpawnAttacker_ShouldBeTrueWhenUnderLimits()
        {
            // Arrange
            var budget = new SpawnBudget();

            // Act & Assert
            Assert.True(budget.CanSpawnAttacker());
        }

        [Fact]
        public void SpawnBudget_CanSpawnAttacker_ShouldBeFalseWhenAtMaxPerSide()
        {
            // Arrange
            var budget = new SpawnBudget();
            for (int i = 0; i < 12; i++)
            {
                budget.AllocateAttacker();
            }

            // Act & Assert
            Assert.False(budget.CanSpawnAttacker());
        }

        [Fact]
        public void SpawnBudget_CanSpawnAttacker_ShouldBeFalseWhenTotalAtMax()
        {
            // Arrange
            var budget = new SpawnBudget(maxTotalPeds: 10, maxPerSide: 12);
            for (int i = 0; i < 10; i++)
            {
                budget.AllocateDefender();
            }

            // Act & Assert
            Assert.Equal(0, budget.Available);
            Assert.False(budget.CanSpawnAttacker());
        }

        #endregion

        #region CanSpawnDefender

        [Fact]
        public void SpawnBudget_CanSpawnDefender_ShouldBeTrueWhenUnderLimits()
        {
            // Arrange
            var budget = new SpawnBudget();

            // Act & Assert
            Assert.True(budget.CanSpawnDefender());
        }

        [Fact]
        public void SpawnBudget_CanSpawnDefender_ShouldBeFalseWhenAtMaxPerSide()
        {
            // Arrange
            var budget = new SpawnBudget();
            for (int i = 0; i < 12; i++)
            {
                budget.AllocateDefender();
            }

            // Act & Assert
            Assert.False(budget.CanSpawnDefender());
        }

        [Fact]
        public void SpawnBudget_CanSpawnDefender_ShouldBeFalseWhenTotalAtMax()
        {
            // Arrange
            var budget = new SpawnBudget(maxTotalPeds: 10, maxPerSide: 12);
            for (int i = 0; i < 10; i++)
            {
                budget.AllocateAttacker();
            }

            // Act & Assert
            Assert.Equal(0, budget.Available);
            Assert.False(budget.CanSpawnDefender());
        }

        #endregion

        #region AllocateAttacker

        [Fact]
        public void SpawnBudget_AllocateAttacker_ShouldIncrementCount()
        {
            // Arrange
            var budget = new SpawnBudget();

            // Act
            var result = budget.AllocateAttacker();

            // Assert
            Assert.True(result);
            Assert.Equal(1, budget.AllocatedAttackers);
        }

        [Fact]
        public void SpawnBudget_AllocateAttacker_ShouldReturnFalseWhenAtMaxPerSide()
        {
            // Arrange
            var budget = new SpawnBudget();
            for (int i = 0; i < 12; i++)
            {
                budget.AllocateAttacker();
            }

            // Act
            var result = budget.AllocateAttacker();

            // Assert
            Assert.False(result);
            Assert.Equal(12, budget.AllocatedAttackers);
        }

        [Fact]
        public void SpawnBudget_AllocateAttacker_ShouldReturnFalseWhenNoAvailableSlots()
        {
            // Arrange
            var budget = new SpawnBudget(maxTotalPeds: 5, maxPerSide: 12);
            for (int i = 0; i < 5; i++)
            {
                budget.AllocateDefender();
            }

            // Act
            var result = budget.AllocateAttacker();

            // Assert
            Assert.False(result);
            Assert.Equal(0, budget.AllocatedAttackers);
        }

        #endregion

        #region AllocateDefender

        [Fact]
        public void SpawnBudget_AllocateDefender_ShouldIncrementCount()
        {
            // Arrange
            var budget = new SpawnBudget();

            // Act
            var result = budget.AllocateDefender();

            // Assert
            Assert.True(result);
            Assert.Equal(1, budget.AllocatedDefenders);
        }

        [Fact]
        public void SpawnBudget_AllocateDefender_ShouldReturnFalseWhenAtMaxPerSide()
        {
            // Arrange
            var budget = new SpawnBudget();
            for (int i = 0; i < 12; i++)
            {
                budget.AllocateDefender();
            }

            // Act
            var result = budget.AllocateDefender();

            // Assert
            Assert.False(result);
            Assert.Equal(12, budget.AllocatedDefenders);
        }

        [Fact]
        public void SpawnBudget_AllocateDefender_ShouldReturnFalseWhenNoAvailableSlots()
        {
            // Arrange
            var budget = new SpawnBudget(maxTotalPeds: 5, maxPerSide: 12);
            for (int i = 0; i < 5; i++)
            {
                budget.AllocateAttacker();
            }

            // Act
            var result = budget.AllocateDefender();

            // Assert
            Assert.False(result);
            Assert.Equal(0, budget.AllocatedDefenders);
        }

        #endregion

        #region ReleaseAttacker

        [Fact]
        public void SpawnBudget_ReleaseAttacker_ShouldDecrementCount()
        {
            // Arrange
            var budget = new SpawnBudget();
            budget.AllocateAttacker();
            budget.AllocateAttacker();

            // Act
            var result = budget.ReleaseAttacker();

            // Assert
            Assert.True(result);
            Assert.Equal(1, budget.AllocatedAttackers);
        }

        [Fact]
        public void SpawnBudget_ReleaseAttacker_ShouldReturnFalseWhenNoneAllocated()
        {
            // Arrange
            var budget = new SpawnBudget();

            // Act
            var result = budget.ReleaseAttacker();

            // Assert
            Assert.False(result);
            Assert.Equal(0, budget.AllocatedAttackers);
        }

        #endregion

        #region ReleaseDefender

        [Fact]
        public void SpawnBudget_ReleaseDefender_ShouldDecrementCount()
        {
            // Arrange
            var budget = new SpawnBudget();
            budget.AllocateDefender();
            budget.AllocateDefender();

            // Act
            var result = budget.ReleaseDefender();

            // Assert
            Assert.True(result);
            Assert.Equal(1, budget.AllocatedDefenders);
        }

        [Fact]
        public void SpawnBudget_ReleaseDefender_ShouldReturnFalseWhenNoneAllocated()
        {
            // Arrange
            var budget = new SpawnBudget();

            // Act
            var result = budget.ReleaseDefender();

            // Assert
            Assert.False(result);
            Assert.Equal(0, budget.AllocatedDefenders);
        }

        #endregion

        #region Reset

        [Fact]
        public void SpawnBudget_Reset_ShouldClearAllAllocations()
        {
            // Arrange
            var budget = new SpawnBudget();
            budget.AllocateAttacker();
            budget.AllocateAttacker();
            budget.AllocateDefender();
            budget.AllocateDefender();
            budget.AllocateDefender();

            // Act
            budget.Reset();

            // Assert
            Assert.Equal(0, budget.AllocatedAttackers);
            Assert.Equal(0, budget.AllocatedDefenders);
            Assert.Equal(30, budget.Available);
        }

        #endregion

        #region Edge Cases

        [Fact]
        public void SpawnBudget_ShouldHandleBothLimitsSimultaneously()
        {
            // Arrange - small budget where both per-side and total limits matter
            var budget = new SpawnBudget(maxTotalPeds: 20, maxPerSide: 12);

            // Allocate 12 attackers (max per side)
            for (int i = 0; i < 12; i++)
            {
                Assert.True(budget.AllocateAttacker());
            }
            Assert.False(budget.AllocateAttacker()); // 13th should fail (per-side limit)

            // Allocate 8 defenders (total reaches 20)
            for (int i = 0; i < 8; i++)
            {
                Assert.True(budget.AllocateDefender());
            }
            Assert.False(budget.AllocateDefender()); // 9th should fail (total limit)

            // Assert
            Assert.Equal(12, budget.AllocatedAttackers);
            Assert.Equal(8, budget.AllocatedDefenders);
            Assert.Equal(0, budget.Available);
        }

        [Fact]
        public void SpawnBudget_ShouldAllowReallocationAfterRelease()
        {
            // Arrange
            var budget = new SpawnBudget(maxTotalPeds: 5, maxPerSide: 12);

            // Fill up
            for (int i = 0; i < 5; i++)
            {
                budget.AllocateAttacker();
            }
            Assert.False(budget.CanSpawnDefender());

            // Release one attacker
            budget.ReleaseAttacker();

            // Now should be able to allocate again
            Assert.True(budget.CanSpawnDefender());
            Assert.True(budget.AllocateDefender());
        }

        #endregion
    }
}
