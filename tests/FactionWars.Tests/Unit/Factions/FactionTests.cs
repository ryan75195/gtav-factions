using FactionWars.Factions.Models;
using System;
using Xunit;

namespace FactionWars.Tests.Unit.Factions
{
    public class FactionTests
    {
        #region Constructor and Required Properties

        [Fact]
        public void Faction_ShouldRequireId()
        {
            // Arrange & Act
            var faction = new Faction("faction_michael", "Michael's Organization");

            // Assert
            Assert.Equal("faction_michael", faction.Id);
        }

        [Fact]
        public void Faction_ShouldRequireName()
        {
            // Arrange & Act
            var faction = new Faction("faction_michael", "Michael's Organization");

            // Assert
            Assert.Equal("Michael's Organization", faction.Name);
        }

        [Fact]
        public void Faction_ShouldThrowOnNullId()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentNullException>(() => new Faction(null!, "Michael's Organization"));
        }

        [Fact]
        public void Faction_ShouldThrowOnEmptyId()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentException>(() => new Faction("", "Michael's Organization"));
        }

        [Fact]
        public void Faction_ShouldThrowOnWhitespaceId()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentException>(() => new Faction("   ", "Michael's Organization"));
        }

        [Fact]
        public void Faction_ShouldThrowOnNullName()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentNullException>(() => new Faction("faction_michael", null!));
        }

        [Fact]
        public void Faction_ShouldThrowOnEmptyName()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentException>(() => new Faction("faction_michael", ""));
        }

        [Fact]
        public void Faction_ShouldThrowOnWhitespaceName()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentException>(() => new Faction("faction_michael", "   "));
        }

        #endregion

        #region Leader Property

        [Fact]
        public void Faction_ShouldHaveNullLeaderByDefault()
        {
            // Arrange & Act
            var faction = new Faction("faction_michael", "Michael's Organization");

            // Assert
            Assert.Null(faction.Leader);
        }

        [Fact]
        public void Faction_ShouldAllowSettingLeader()
        {
            // Arrange & Act
            var faction = new Faction("faction_michael", "Michael's Organization", leader: "Michael De Santa");

            // Assert
            Assert.Equal("Michael De Santa", faction.Leader);
        }

        #endregion

        #region Color Property

        [Fact]
        public void Faction_ShouldHaveDefaultColor()
        {
            // Arrange & Act
            var faction = new Faction("faction_michael", "Michael's Organization");

            // Assert - Default color should be white (255, 255, 255)
            Assert.Equal(new FactionColor(255, 255, 255), faction.Color);
        }

        [Fact]
        public void Faction_ShouldAllowCustomColor()
        {
            // Arrange
            var blueColor = new FactionColor(0, 100, 255);

            // Act
            var faction = new Faction("faction_michael", "Michael's Organization", color: blueColor);

            // Assert
            Assert.Equal(blueColor, faction.Color);
        }

        #endregion

        #region IsActive Property

        [Fact]
        public void Faction_ShouldBeActiveByDefault()
        {
            // Arrange & Act
            var faction = new Faction("faction_michael", "Michael's Organization");

            // Assert
            Assert.True(faction.IsActive);
        }

        [Fact]
        public void Faction_ShouldAllowDeactivation()
        {
            // Arrange
            var faction = new Faction("faction_michael", "Michael's Organization");

            // Act
            faction.IsActive = false;

            // Assert
            Assert.False(faction.IsActive);
        }

        #endregion

        #region Description Property

        [Fact]
        public void Faction_ShouldHaveEmptyDescriptionByDefault()
        {
            // Arrange & Act
            var faction = new Faction("faction_michael", "Michael's Organization");

            // Assert
            Assert.Equal(string.Empty, faction.Description);
        }

        [Fact]
        public void Faction_ShouldAllowSettingDescription()
        {
            // Arrange & Act
            var faction = new Faction(
                "faction_michael",
                "Michael's Organization",
                description: "A sophisticated criminal empire focused on high-value heists.");

            // Assert
            Assert.Equal("A sophisticated criminal empire focused on high-value heists.", faction.Description);
        }

        #endregion

        #region Equality

        [Fact]
        public void Faction_ShouldBeEqualById()
        {
            // Arrange
            var faction1 = new Faction("faction_michael", "Michael's Organization");
            var faction2 = new Faction("faction_michael", "Different Name");

            // Act & Assert - Factions are equal if they have the same ID
            Assert.Equal(faction1, faction2);
        }

        [Fact]
        public void Faction_ShouldNotBeEqualWithDifferentId()
        {
            // Arrange
            var faction1 = new Faction("faction_michael", "Michael's Organization");
            var faction2 = new Faction("faction_trevor", "Michael's Organization");

            // Act & Assert
            Assert.NotEqual(faction1, faction2);
        }

        [Fact]
        public void Faction_GetHashCode_ShouldBeConsistentWithEquals()
        {
            // Arrange
            var faction1 = new Faction("faction_michael", "Michael's Organization");
            var faction2 = new Faction("faction_michael", "Different Name");

            // Act & Assert - Equal objects must have equal hash codes
            Assert.Equal(faction1.GetHashCode(), faction2.GetHashCode());
        }

        [Fact]
        public void Faction_ShouldNotBeEqualToNull()
        {
            // Arrange
            var faction = new Faction("faction_michael", "Michael's Organization");

            // Act & Assert
            Assert.False(faction.Equals(null));
        }

        [Fact]
        public void Faction_EqualityOperator_ShouldWork()
        {
            // Arrange
            var faction1 = new Faction("faction_michael", "Michael's Organization");
            var faction2 = new Faction("faction_michael", "Different Name");

            // Act & Assert
            Assert.True(faction1 == faction2);
        }

        [Fact]
        public void Faction_InequalityOperator_ShouldWork()
        {
            // Arrange
            var faction1 = new Faction("faction_michael", "Michael's Organization");
            var faction2 = new Faction("faction_trevor", "Trevor's Enterprise");

            // Act & Assert
            Assert.True(faction1 != faction2);
        }

        [Fact]
        public void Faction_NullEquality_ShouldHandleNullLeft()
        {
            // Arrange
            Faction? faction1 = null;
            var faction2 = new Faction("faction_michael", "Michael's Organization");

            // Act & Assert
            Assert.True(faction1 != faction2);
            Assert.False(faction1 == faction2);
        }

        [Fact]
        public void Faction_NullEquality_ShouldHandleBothNull()
        {
            // Arrange
            Faction? faction1 = null;
            Faction? faction2 = null;

            // Act & Assert
            Assert.True(faction1 == faction2);
        }

        #endregion

        #region ToString

        [Fact]
        public void Faction_ToString_ShouldReturnReadableFormat()
        {
            // Arrange
            var faction = new Faction("faction_michael", "Michael's Organization");

            // Act
            var result = faction.ToString();

            // Assert
            Assert.Contains("faction_michael", result);
            Assert.Contains("Michael's Organization", result);
        }

        #endregion
    }
}
