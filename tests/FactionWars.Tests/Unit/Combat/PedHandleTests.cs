using FactionWars.Combat.Models;
using FactionWars.Core.Interfaces;
using System;
using Xunit;

namespace FactionWars.Tests.Unit.Combat
{
    public class PedHandleTests
    {
        #region Constructor and Properties

        [Fact]
        public void PedHandle_ShouldStoreHandle()
        {
            // Arrange & Act
            var pedHandle = new PedHandle(100);

            // Assert
            Assert.Equal(100, pedHandle.Handle);
        }

        [Fact]
        public void PedHandle_ShouldStoreFactionId()
        {
            // Arrange & Act
            var pedHandle = new PedHandle(100, factionId: "faction_michael");

            // Assert
            Assert.Equal("faction_michael", pedHandle.FactionId);
        }

        [Fact]
        public void PedHandle_ShouldHaveNullFactionIdByDefault()
        {
            // Arrange & Act
            var pedHandle = new PedHandle(100);

            // Assert
            Assert.Null(pedHandle.FactionId);
        }

        [Fact]
        public void PedHandle_ShouldStoreSpawnPosition()
        {
            // Arrange
            var position = new Vector3(100.5f, 200.5f, 50.0f);

            // Act
            var pedHandle = new PedHandle(100, spawnPosition: position);

            // Assert
            Assert.Equal(position, pedHandle.SpawnPosition);
        }

        [Fact]
        public void PedHandle_ShouldHaveZeroSpawnPositionByDefault()
        {
            // Arrange & Act
            var pedHandle = new PedHandle(100);

            // Assert
            Assert.Equal(Vector3.Zero, pedHandle.SpawnPosition);
        }

        [Fact]
        public void PedHandle_ShouldStoreModelName()
        {
            // Arrange & Act
            var pedHandle = new PedHandle(100, modelName: "a_m_y_hipster_01");

            // Assert
            Assert.Equal("a_m_y_hipster_01", pedHandle.ModelName);
        }

        [Fact]
        public void PedHandle_ShouldHaveNullModelNameByDefault()
        {
            // Arrange & Act
            var pedHandle = new PedHandle(100);

            // Assert
            Assert.Null(pedHandle.ModelName);
        }

        [Fact]
        public void PedHandle_ShouldStoreZoneId()
        {
            // Arrange & Act
            var pedHandle = new PedHandle(100, zoneId: "zone_vinewood");

            // Assert
            Assert.Equal("zone_vinewood", pedHandle.ZoneId);
        }

        [Fact]
        public void PedHandle_ShouldHaveNullZoneIdByDefault()
        {
            // Arrange & Act
            var pedHandle = new PedHandle(100);

            // Assert
            Assert.Null(pedHandle.ZoneId);
        }

        [Fact]
        public void PedHandle_ShouldRecordCreationTime()
        {
            // Arrange
            var before = DateTime.UtcNow;

            // Act
            var pedHandle = new PedHandle(100);
            var after = DateTime.UtcNow;

            // Assert
            Assert.InRange(pedHandle.CreatedAt, before, after);
        }

        #endregion

        #region Invalid Handle Detection

        [Fact]
        public void PedHandle_Invalid_ShouldHaveNegativeHandle()
        {
            // Arrange & Act
            var invalid = PedHandle.Invalid;

            // Assert
            Assert.Equal(-1, invalid.Handle);
        }

        [Fact]
        public void PedHandle_IsValid_ShouldBeTrueForPositiveHandle()
        {
            // Arrange
            var pedHandle = new PedHandle(100);

            // Act & Assert
            Assert.True(pedHandle.IsValid);
        }

        [Fact]
        public void PedHandle_IsValid_ShouldBeTrueForZeroHandle()
        {
            // Arrange - Handle 0 can be valid in GTA V
            var pedHandle = new PedHandle(0);

            // Act & Assert
            Assert.True(pedHandle.IsValid);
        }

        [Fact]
        public void PedHandle_IsValid_ShouldBeFalseForNegativeHandle()
        {
            // Arrange
            var pedHandle = new PedHandle(-1);

            // Act & Assert
            Assert.False(pedHandle.IsValid);
        }

        [Fact]
        public void PedHandle_IsValid_ShouldBeFalseForInvalidConstant()
        {
            // Arrange
            var invalid = PedHandle.Invalid;

            // Act & Assert
            Assert.False(invalid.IsValid);
        }

        #endregion

        #region State Tracking

        [Fact]
        public void PedHandle_IsMarkedForDeletion_ShouldBeFalseByDefault()
        {
            // Arrange
            var pedHandle = new PedHandle(100);

            // Act & Assert
            Assert.False(pedHandle.IsMarkedForDeletion);
        }

        [Fact]
        public void PedHandle_MarkForDeletion_ShouldSetFlag()
        {
            // Arrange
            var pedHandle = new PedHandle(100);

            // Act
            pedHandle.MarkForDeletion();

            // Assert
            Assert.True(pedHandle.IsMarkedForDeletion);
        }

        [Fact]
        public void PedHandle_MarkForDeletion_ShouldBeIdempotent()
        {
            // Arrange
            var pedHandle = new PedHandle(100);

            // Act
            pedHandle.MarkForDeletion();
            pedHandle.MarkForDeletion();

            // Assert
            Assert.True(pedHandle.IsMarkedForDeletion);
        }

        [Fact]
        public void PedHandle_IsRecycled_ShouldBeFalseByDefault()
        {
            // Arrange
            var pedHandle = new PedHandle(100);

            // Act & Assert
            Assert.False(pedHandle.IsRecycled);
        }

        [Fact]
        public void PedHandle_MarkAsRecycled_ShouldSetFlag()
        {
            // Arrange
            var pedHandle = new PedHandle(100);

            // Act
            pedHandle.MarkAsRecycled();

            // Assert
            Assert.True(pedHandle.IsRecycled);
        }

        #endregion

        #region Equality

        [Fact]
        public void PedHandle_ShouldBeEqualByHandle()
        {
            // Arrange
            var handle1 = new PedHandle(100);
            var handle2 = new PedHandle(100);

            // Act & Assert
            Assert.Equal(handle1, handle2);
        }

        [Fact]
        public void PedHandle_ShouldNotBeEqualWithDifferentHandle()
        {
            // Arrange
            var handle1 = new PedHandle(100);
            var handle2 = new PedHandle(200);

            // Act & Assert
            Assert.NotEqual(handle1, handle2);
        }

        [Fact]
        public void PedHandle_ShouldBeEqualRegardlessOfMetadata()
        {
            // Arrange - Same handle but different metadata
            var handle1 = new PedHandle(100, factionId: "faction_michael");
            var handle2 = new PedHandle(100, factionId: "faction_trevor");

            // Act & Assert - Equality is based on handle only
            Assert.Equal(handle1, handle2);
        }

        [Fact]
        public void PedHandle_GetHashCode_ShouldBeConsistentWithEquals()
        {
            // Arrange
            var handle1 = new PedHandle(100);
            var handle2 = new PedHandle(100);

            // Act & Assert
            Assert.Equal(handle1.GetHashCode(), handle2.GetHashCode());
        }

        [Fact]
        public void PedHandle_ShouldNotBeEqualToNull()
        {
            // Arrange
            var handle = new PedHandle(100);

            // Act & Assert
            Assert.False(handle.Equals(null));
        }

        [Fact]
        public void PedHandle_EqualityOperator_ShouldWork()
        {
            // Arrange
            var handle1 = new PedHandle(100);
            var handle2 = new PedHandle(100);

            // Act & Assert
            Assert.True(handle1 == handle2);
        }

        [Fact]
        public void PedHandle_InequalityOperator_ShouldWork()
        {
            // Arrange
            var handle1 = new PedHandle(100);
            var handle2 = new PedHandle(200);

            // Act & Assert
            Assert.True(handle1 != handle2);
        }

        [Fact]
        public void PedHandle_NullEquality_ShouldHandleNullLeft()
        {
            // Arrange
            PedHandle? handle1 = null;
            var handle2 = new PedHandle(100);

            // Act & Assert
            Assert.True(handle1 != handle2);
            Assert.False(handle1 == handle2);
        }

        [Fact]
        public void PedHandle_NullEquality_ShouldHandleBothNull()
        {
            // Arrange
            PedHandle? handle1 = null;
            PedHandle? handle2 = null;

            // Act & Assert
            Assert.True(handle1 == handle2);
        }

        #endregion

        #region ToString

        [Fact]
        public void PedHandle_ToString_ShouldContainHandle()
        {
            // Arrange
            var pedHandle = new PedHandle(12345);

            // Act
            var result = pedHandle.ToString();

            // Assert
            Assert.Contains("12345", result);
        }

        [Fact]
        public void PedHandle_ToString_ShouldContainFactionIdWhenSet()
        {
            // Arrange
            var pedHandle = new PedHandle(100, factionId: "faction_michael");

            // Act
            var result = pedHandle.ToString();

            // Assert
            Assert.Contains("faction_michael", result);
        }

        [Fact]
        public void PedHandle_ToString_ShouldIndicateInvalidHandle()
        {
            // Arrange
            var invalid = PedHandle.Invalid;

            // Act
            var result = invalid.ToString();

            // Assert
            Assert.Contains("Invalid", result);
        }

        #endregion

        #region Implicit Conversion

        [Fact]
        public void PedHandle_ShouldImplicitlyConvertToInt()
        {
            // Arrange
            var pedHandle = new PedHandle(100);

            // Act
            int handle = pedHandle;

            // Assert
            Assert.Equal(100, handle);
        }

        [Fact]
        public void PedHandle_ShouldExplicitlyConvertFromInt()
        {
            // Arrange
            int rawHandle = 100;

            // Act
            var pedHandle = (PedHandle)rawHandle;

            // Assert
            Assert.Equal(100, pedHandle.Handle);
        }

        #endregion

        #region Age Calculation

        [Fact]
        public void PedHandle_GetAge_ShouldReturnTimeSinceCreation()
        {
            // Arrange
            var pedHandle = new PedHandle(100);
            var creationTime = pedHandle.CreatedAt;

            // Act - Small delay to ensure measurable age
            System.Threading.Thread.Sleep(10);
            var age = pedHandle.GetAge();

            // Assert
            Assert.True(age >= TimeSpan.FromMilliseconds(10));
        }

        #endregion
    }
}
