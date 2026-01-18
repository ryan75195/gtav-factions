using FactionWars.UI.Models;
using System;
using Xunit;

namespace FactionWars.Tests.Unit.UI
{
    /// <summary>
    /// Tests for the PhoneCommand model.
    /// Following TDD - these tests define the expected behavior.
    /// </summary>
    public class PhoneCommandTests
    {
        #region Constructor Tests

        [Fact]
        public void Constructor_WithValidParameters_CreatesInstance()
        {
            // Act
            var command = new PhoneCommand("test_id", "Test Name", "Test Description");

            // Assert
            Assert.NotNull(command);
            Assert.Equal("test_id", command.Id);
            Assert.Equal("Test Name", command.Name);
            Assert.Equal("Test Description", command.Description);
        }

        [Fact]
        public void Constructor_WithNullId_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new PhoneCommand(null!, "Name", "Description"));
        }

        [Fact]
        public void Constructor_WithEmptyId_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => new PhoneCommand("", "Name", "Description"));
        }

        [Fact]
        public void Constructor_WithWhitespaceId_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => new PhoneCommand("   ", "Name", "Description"));
        }

        [Fact]
        public void Constructor_WithNullName_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new PhoneCommand("id", null!, "Description"));
        }

        [Fact]
        public void Constructor_WithEmptyName_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => new PhoneCommand("id", "", "Description"));
        }

        [Fact]
        public void Constructor_WithWhitespaceName_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => new PhoneCommand("id", "   ", "Description"));
        }

        [Fact]
        public void Constructor_WithNullDescription_SetsEmptyString()
        {
            // Act
            var command = new PhoneCommand("id", "Name", null!);

            // Assert
            Assert.Equal(string.Empty, command.Description);
        }

        [Fact]
        public void Constructor_WithoutDescription_SetsEmptyString()
        {
            // Act
            var command = new PhoneCommand("id", "Name");

            // Assert
            Assert.Equal(string.Empty, command.Description);
        }

        #endregion

        #region IsEnabled Tests

        [Fact]
        public void IsEnabled_DefaultValue_IsTrue()
        {
            // Act
            var command = new PhoneCommand("id", "Name", "Description");

            // Assert
            Assert.True(command.IsEnabled);
        }

        [Fact]
        public void IsEnabled_CanBeSetToFalse()
        {
            // Arrange
            var command = new PhoneCommand("id", "Name", "Description");

            // Act
            command.IsEnabled = false;

            // Assert
            Assert.False(command.IsEnabled);
        }

        [Fact]
        public void IsEnabled_CanBeSetToTrue()
        {
            // Arrange
            var command = new PhoneCommand("id", "Name", "Description") { IsEnabled = false };

            // Act
            command.IsEnabled = true;

            // Assert
            Assert.True(command.IsEnabled);
        }

        #endregion

        #region Category Tests

        [Fact]
        public void Category_DefaultValue_IsEmpty()
        {
            // Act
            var command = new PhoneCommand("id", "Name", "Description");

            // Assert
            Assert.Equal(string.Empty, command.Category);
        }

        [Fact]
        public void Category_CanBeSet()
        {
            // Arrange
            var command = new PhoneCommand("id", "Name", "Description");

            // Act
            command.Category = "Faction";

            // Assert
            Assert.Equal("Faction", command.Category);
        }

        [Fact]
        public void Category_SetToNull_BecomesEmpty()
        {
            // Arrange
            var command = new PhoneCommand("id", "Name", "Description") { Category = "Test" };

            // Act
            command.Category = null!;

            // Assert
            Assert.Equal(string.Empty, command.Category);
        }

        #endregion

        #region Equality Tests

        [Fact]
        public void Equals_SameId_ReturnsTrue()
        {
            // Arrange
            var command1 = new PhoneCommand("test_id", "Name 1", "Desc 1");
            var command2 = new PhoneCommand("test_id", "Name 2", "Desc 2");

            // Act & Assert
            Assert.True(command1.Equals(command2));
        }

        [Fact]
        public void Equals_DifferentId_ReturnsFalse()
        {
            // Arrange
            var command1 = new PhoneCommand("id_1", "Name", "Desc");
            var command2 = new PhoneCommand("id_2", "Name", "Desc");

            // Act & Assert
            Assert.False(command1.Equals(command2));
        }

        [Fact]
        public void Equals_Null_ReturnsFalse()
        {
            // Arrange
            var command = new PhoneCommand("id", "Name", "Desc");

            // Act & Assert
            Assert.False(command.Equals(null));
        }

        [Fact]
        public void GetHashCode_SameId_ReturnsSameHash()
        {
            // Arrange
            var command1 = new PhoneCommand("test_id", "Name 1", "Desc 1");
            var command2 = new PhoneCommand("test_id", "Name 2", "Desc 2");

            // Act & Assert
            Assert.Equal(command1.GetHashCode(), command2.GetHashCode());
        }

        #endregion
    }
}
