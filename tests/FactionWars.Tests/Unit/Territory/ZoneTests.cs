using FactionWars.Core.Interfaces;
using FactionWars.Territory.Models;
using System;
using Xunit;

namespace FactionWars.Tests.Unit.Territory
{
    public class ZoneTests
    {
        #region Constructor and Required Properties

        [Fact]
        public void Zone_ShouldRequireId()
        {
            // Arrange & Act
            var zone = new Zone("zone_1", "Downtown", new Vector3(0, 0, 0));

            // Assert
            Assert.Equal("zone_1", zone.Id);
        }

        [Fact]
        public void Zone_ShouldRequireName()
        {
            // Arrange & Act
            var zone = new Zone("zone_1", "Downtown", new Vector3(0, 0, 0));

            // Assert
            Assert.Equal("Downtown", zone.Name);
        }

        [Fact]
        public void Zone_ShouldRequireCenterPosition()
        {
            // Arrange
            var center = new Vector3(100f, 200f, 50f);

            // Act
            var zone = new Zone("zone_1", "Downtown", center);

            // Assert
            Assert.Equal(center, zone.Center);
        }

        [Fact]
        public void Zone_ShouldThrowOnNullId()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentNullException>(() => new Zone(null!, "Downtown", new Vector3(0, 0, 0)));
        }

        [Fact]
        public void Zone_ShouldThrowOnEmptyId()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentException>(() => new Zone("", "Downtown", new Vector3(0, 0, 0)));
        }

        [Fact]
        public void Zone_ShouldThrowOnWhitespaceId()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentException>(() => new Zone("   ", "Downtown", new Vector3(0, 0, 0)));
        }

        [Fact]
        public void Zone_ShouldThrowOnNullName()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentNullException>(() => new Zone("zone_1", null!, new Vector3(0, 0, 0)));
        }

        [Fact]
        public void Zone_ShouldThrowOnEmptyName()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentException>(() => new Zone("zone_1", "", new Vector3(0, 0, 0)));
        }

        #endregion

        #region Optional Properties with Defaults

        [Fact]
        public void Zone_ShouldHaveDefaultRadius()
        {
            // Arrange & Act
            var zone = new Zone("zone_1", "Downtown", new Vector3(0, 0, 0));

            // Assert - Default radius should be a reasonable value (150 units)
            Assert.Equal(150f, zone.Radius);
        }

        [Fact]
        public void Zone_ShouldAllowCustomRadius()
        {
            // Arrange & Act
            var zone = new Zone("zone_1", "Downtown", new Vector3(0, 0, 0), radius: 250f);

            // Assert
            Assert.Equal(250f, zone.Radius);
        }

        [Fact]
        public void Zone_ShouldThrowOnNegativeRadius()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new Zone("zone_1", "Downtown", new Vector3(0, 0, 0), radius: -10f));
        }

        [Fact]
        public void Zone_ShouldThrowOnZeroRadius()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new Zone("zone_1", "Downtown", new Vector3(0, 0, 0), radius: 0f));
        }

        [Fact]
        public void Zone_ShouldHaveNullOwnerByDefault()
        {
            // Arrange & Act
            var zone = new Zone("zone_1", "Downtown", new Vector3(0, 0, 0));

            // Assert - Neutral zones have no owner
            Assert.Null(zone.OwnerFactionId);
        }

        [Fact]
        public void Zone_ShouldAllowSettingOwner()
        {
            // Arrange
            var zone = new Zone("zone_1", "Downtown", new Vector3(0, 0, 0));

            // Act
            zone.OwnerFactionId = "michael_faction";

            // Assert
            Assert.Equal("michael_faction", zone.OwnerFactionId);
        }

        [Fact]
        public void Zone_ShouldHaveDefaultControlPercentage()
        {
            // Arrange & Act
            var zone = new Zone("zone_1", "Downtown", new Vector3(0, 0, 0));

            // Assert - Default control should be 0 (no one controls it)
            Assert.Equal(0f, zone.ControlPercentage);
        }

        [Fact]
        public void Zone_ShouldAllowSettingControlPercentage()
        {
            // Arrange
            var zone = new Zone("zone_1", "Downtown", new Vector3(0, 0, 0));

            // Act
            zone.ControlPercentage = 75.5f;

            // Assert
            Assert.Equal(75.5f, zone.ControlPercentage);
        }

        [Fact]
        public void Zone_ShouldClampControlPercentageToMax100()
        {
            // Arrange
            var zone = new Zone("zone_1", "Downtown", new Vector3(0, 0, 0));

            // Act
            zone.ControlPercentage = 150f;

            // Assert
            Assert.Equal(100f, zone.ControlPercentage);
        }

        [Fact]
        public void Zone_ShouldClampControlPercentageToMin0()
        {
            // Arrange
            var zone = new Zone("zone_1", "Downtown", new Vector3(0, 0, 0));

            // Act
            zone.ControlPercentage = -25f;

            // Assert
            Assert.Equal(0f, zone.ControlPercentage);
        }

        #endregion

        #region Strategic Value

        [Fact]
        public void Zone_ShouldHaveDefaultStrategicValue()
        {
            // Arrange & Act
            var zone = new Zone("zone_1", "Downtown", new Vector3(0, 0, 0));

            // Assert - Default strategic value of 1 (normal priority)
            Assert.Equal(1, zone.StrategicValue);
        }

        [Fact]
        public void Zone_ShouldAllowCustomStrategicValue()
        {
            // Arrange & Act
            var zone = new Zone("zone_1", "Downtown", new Vector3(0, 0, 0), strategicValue: 5);

            // Assert
            Assert.Equal(5, zone.StrategicValue);
        }

        [Fact]
        public void Zone_ShouldThrowOnNegativeStrategicValue()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new Zone("zone_1", "Downtown", new Vector3(0, 0, 0), strategicValue: -1));
        }

        [Fact]
        public void Zone_ShouldThrowOnZeroStrategicValue()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new Zone("zone_1", "Downtown", new Vector3(0, 0, 0), strategicValue: 0));
        }

        #endregion

        #region Equality

        [Fact]
        public void Zone_ShouldBeEqualById()
        {
            // Arrange
            var zone1 = new Zone("zone_1", "Downtown", new Vector3(0, 0, 0));
            var zone2 = new Zone("zone_1", "Different Name", new Vector3(100, 100, 100));

            // Act & Assert - Zones are equal if they have the same ID
            Assert.Equal(zone1, zone2);
        }

        [Fact]
        public void Zone_ShouldNotBeEqualWithDifferentId()
        {
            // Arrange
            var zone1 = new Zone("zone_1", "Downtown", new Vector3(0, 0, 0));
            var zone2 = new Zone("zone_2", "Downtown", new Vector3(0, 0, 0));

            // Act & Assert
            Assert.NotEqual(zone1, zone2);
        }

        [Fact]
        public void Zone_GetHashCode_ShouldBeConsistentWithEquals()
        {
            // Arrange
            var zone1 = new Zone("zone_1", "Downtown", new Vector3(0, 0, 0));
            var zone2 = new Zone("zone_1", "Different Name", new Vector3(100, 100, 100));

            // Act & Assert - Equal objects must have equal hash codes
            Assert.Equal(zone1.GetHashCode(), zone2.GetHashCode());
        }

        #endregion

        #region IsContested Property

        [Fact]
        public void Zone_ShouldNotBeContestedByDefault()
        {
            // Arrange & Act
            var zone = new Zone("zone_1", "Downtown", new Vector3(0, 0, 0));

            // Assert
            Assert.False(zone.IsContested);
        }

        [Fact]
        public void Zone_ShouldAllowSettingContested()
        {
            // Arrange
            var zone = new Zone("zone_1", "Downtown", new Vector3(0, 0, 0));

            // Act
            zone.IsContested = true;

            // Assert
            Assert.True(zone.IsContested);
        }

        #endregion
    }
}
