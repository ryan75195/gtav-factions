using FactionWars.UI.Interfaces;
using FactionWars.UI.Models;
using FactionWars.UI.Services;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace FactionWars.Tests.Unit.UI
{
    /// <summary>
    /// Tests for the PhoneCommandService.
    /// Following TDD - these tests define the expected behavior for phone command registration.
    /// </summary>
    public class PhoneCommandServiceTests
    {
        #region Test Setup

        private readonly Mock<IPhoneCommandHandler> _phoneCommandHandlerMock;

        public PhoneCommandServiceTests()
        {
            _phoneCommandHandlerMock = new Mock<IPhoneCommandHandler>();
        }

        private IPhoneCommandService CreateService()
        {
            return new PhoneCommandService(_phoneCommandHandlerMock.Object);
        }

        #endregion

        #region Constructor Tests

        [Fact]
        public void Constructor_WithNullHandler_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new PhoneCommandService(null!));
        }

        [Fact]
        public void Constructor_WithValidHandler_CreatesInstance()
        {
            // Act
            var service = CreateService();

            // Assert
            Assert.NotNull(service);
        }

        #endregion

        #region RegisterCommand Tests

        [Fact]
        public void RegisterCommand_WithValidCommand_AddsToRegisteredCommands()
        {
            // Arrange
            var service = CreateService();
            var command = new PhoneCommand("faction_status", "Status", "View faction status");

            // Act
            service.RegisterCommand(command);

            // Assert
            var registeredCommands = service.GetRegisteredCommands();
            Assert.Single(registeredCommands);
            Assert.Contains(registeredCommands, c => c.Id == "faction_status");
        }

        [Fact]
        public void RegisterCommand_WithNullCommand_ThrowsArgumentNullException()
        {
            // Arrange
            var service = CreateService();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => service.RegisterCommand(null!));
        }

        [Fact]
        public void RegisterCommand_WithDuplicateId_ThrowsInvalidOperationException()
        {
            // Arrange
            var service = CreateService();
            var command1 = new PhoneCommand("test_cmd", "Test 1", "Description 1");
            var command2 = new PhoneCommand("test_cmd", "Test 2", "Description 2");

            // Act
            service.RegisterCommand(command1);

            // Assert
            Assert.Throws<InvalidOperationException>(() => service.RegisterCommand(command2));
        }

        [Fact]
        public void RegisterCommand_MultipleCommands_AllRegistered()
        {
            // Arrange
            var service = CreateService();
            var command1 = new PhoneCommand("cmd_1", "Command 1", "Desc 1");
            var command2 = new PhoneCommand("cmd_2", "Command 2", "Desc 2");
            var command3 = new PhoneCommand("cmd_3", "Command 3", "Desc 3");

            // Act
            service.RegisterCommand(command1);
            service.RegisterCommand(command2);
            service.RegisterCommand(command3);

            // Assert
            var registeredCommands = service.GetRegisteredCommands();
            Assert.Equal(3, registeredCommands.Count());
        }

        #endregion

        #region UnregisterCommand Tests

        [Fact]
        public void UnregisterCommand_ExistingCommand_RemovesFromRegisteredCommands()
        {
            // Arrange
            var service = CreateService();
            var command = new PhoneCommand("test_cmd", "Test", "Description");
            service.RegisterCommand(command);

            // Act
            var result = service.UnregisterCommand("test_cmd");

            // Assert
            Assert.True(result);
            var registeredCommands = service.GetRegisteredCommands();
            Assert.Empty(registeredCommands);
        }

        [Fact]
        public void UnregisterCommand_NonExistingCommand_ReturnsFalse()
        {
            // Arrange
            var service = CreateService();

            // Act
            var result = service.UnregisterCommand("nonexistent");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void UnregisterCommand_NullId_ThrowsArgumentNullException()
        {
            // Arrange
            var service = CreateService();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => service.UnregisterCommand(null!));
        }

        [Fact]
        public void UnregisterCommand_EmptyId_ThrowsArgumentException()
        {
            // Arrange
            var service = CreateService();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => service.UnregisterCommand(""));
        }

        #endregion

        #region GetCommand Tests

        [Fact]
        public void GetCommand_ExistingCommand_ReturnsCommand()
        {
            // Arrange
            var service = CreateService();
            var command = new PhoneCommand("test_cmd", "Test", "Description");
            service.RegisterCommand(command);

            // Act
            var result = service.GetCommand("test_cmd");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("test_cmd", result!.Id);
            Assert.Equal("Test", result.Name);
        }

        [Fact]
        public void GetCommand_NonExistingCommand_ReturnsNull()
        {
            // Arrange
            var service = CreateService();

            // Act
            var result = service.GetCommand("nonexistent");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetCommand_NullId_ReturnsNull()
        {
            // Arrange
            var service = CreateService();

            // Act
            var result = service.GetCommand(null!);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region ExecuteCommand Tests

        [Fact]
        public void ExecuteCommand_ExistingCommand_CallsHandler()
        {
            // Arrange
            var service = CreateService();
            var command = new PhoneCommand("test_cmd", "Test", "Description");
            service.RegisterCommand(command);

            // Act
            service.ExecuteCommand("test_cmd");

            // Assert
            _phoneCommandHandlerMock.Verify(h => h.HandleCommand(command), Times.Once);
        }

        [Fact]
        public void ExecuteCommand_NonExistingCommand_DoesNotCallHandler()
        {
            // Arrange
            var service = CreateService();

            // Act
            service.ExecuteCommand("nonexistent");

            // Assert
            _phoneCommandHandlerMock.Verify(h => h.HandleCommand(It.IsAny<PhoneCommand>()), Times.Never);
        }

        [Fact]
        public void ExecuteCommand_NullId_DoesNotCallHandler()
        {
            // Arrange
            var service = CreateService();

            // Act
            service.ExecuteCommand(null!);

            // Assert
            _phoneCommandHandlerMock.Verify(h => h.HandleCommand(It.IsAny<PhoneCommand>()), Times.Never);
        }

        [Fact]
        public void ExecuteCommand_DisabledCommand_DoesNotCallHandler()
        {
            // Arrange
            var service = CreateService();
            var command = new PhoneCommand("test_cmd", "Test", "Description") { IsEnabled = false };
            service.RegisterCommand(command);

            // Act
            service.ExecuteCommand("test_cmd");

            // Assert
            _phoneCommandHandlerMock.Verify(h => h.HandleCommand(It.IsAny<PhoneCommand>()), Times.Never);
        }

        #endregion

        #region RegisterAllFactionCommands Tests

        [Fact]
        public void RegisterAllFactionCommands_RegistersStatusCommand()
        {
            // Arrange
            var service = CreateService();

            // Act
            service.RegisterAllFactionCommands();

            // Assert
            var command = service.GetCommand("faction_status");
            Assert.NotNull(command);
            Assert.Equal("Faction Status", command!.Name);
        }

        [Fact]
        public void RegisterAllFactionCommands_RegistersTerritoryCommand()
        {
            // Arrange
            var service = CreateService();

            // Act
            service.RegisterAllFactionCommands();

            // Assert
            var command = service.GetCommand("faction_territory");
            Assert.NotNull(command);
            Assert.Equal("Territory Info", command!.Name);
        }

        [Fact]
        public void RegisterAllFactionCommands_RegistersResourcesCommand()
        {
            // Arrange
            var service = CreateService();

            // Act
            service.RegisterAllFactionCommands();

            // Assert
            var command = service.GetCommand("faction_resources");
            Assert.NotNull(command);
            Assert.Equal("Resources", command!.Name);
        }

        [Fact]
        public void RegisterAllFactionCommands_RegistersOrdersCommand()
        {
            // Arrange
            var service = CreateService();

            // Act
            service.RegisterAllFactionCommands();

            // Assert
            var command = service.GetCommand("faction_orders");
            Assert.NotNull(command);
            Assert.Equal("Issue Orders", command!.Name);
        }

        [Fact]
        public void RegisterAllFactionCommands_AllCommandsEnabled()
        {
            // Arrange
            var service = CreateService();

            // Act
            service.RegisterAllFactionCommands();

            // Assert
            var commands = service.GetRegisteredCommands();
            Assert.All(commands, c => Assert.True(c.IsEnabled));
        }

        #endregion

        #region IsCommandRegistered Tests

        [Fact]
        public void IsCommandRegistered_ExistingCommand_ReturnsTrue()
        {
            // Arrange
            var service = CreateService();
            var command = new PhoneCommand("test_cmd", "Test", "Description");
            service.RegisterCommand(command);

            // Act
            var result = service.IsCommandRegistered("test_cmd");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsCommandRegistered_NonExistingCommand_ReturnsFalse()
        {
            // Arrange
            var service = CreateService();

            // Act
            var result = service.IsCommandRegistered("nonexistent");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsCommandRegistered_NullId_ReturnsFalse()
        {
            // Arrange
            var service = CreateService();

            // Act
            var result = service.IsCommandRegistered(null!);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region GetEnabledCommands Tests

        [Fact]
        public void GetEnabledCommands_ReturnsOnlyEnabledCommands()
        {
            // Arrange
            var service = CreateService();
            var enabledCmd = new PhoneCommand("enabled", "Enabled", "Desc") { IsEnabled = true };
            var disabledCmd = new PhoneCommand("disabled", "Disabled", "Desc") { IsEnabled = false };
            service.RegisterCommand(enabledCmd);
            service.RegisterCommand(disabledCmd);

            // Act
            var result = service.GetEnabledCommands();

            // Assert
            Assert.Single(result);
            Assert.Equal("enabled", result.First().Id);
        }

        [Fact]
        public void GetEnabledCommands_NoEnabledCommands_ReturnsEmpty()
        {
            // Arrange
            var service = CreateService();
            var disabledCmd = new PhoneCommand("disabled", "Disabled", "Desc") { IsEnabled = false };
            service.RegisterCommand(disabledCmd);

            // Act
            var result = service.GetEnabledCommands();

            // Assert
            Assert.Empty(result);
        }

        #endregion

        #region SetCommandEnabled Tests

        [Fact]
        public void SetCommandEnabled_ExistingCommand_UpdatesEnabledState()
        {
            // Arrange
            var service = CreateService();
            var command = new PhoneCommand("test_cmd", "Test", "Description") { IsEnabled = true };
            service.RegisterCommand(command);

            // Act
            service.SetCommandEnabled("test_cmd", false);

            // Assert
            var result = service.GetCommand("test_cmd");
            Assert.False(result!.IsEnabled);
        }

        [Fact]
        public void SetCommandEnabled_NonExistingCommand_DoesNotThrow()
        {
            // Arrange
            var service = CreateService();

            // Act & Assert (should not throw)
            service.SetCommandEnabled("nonexistent", true);
        }

        #endregion
    }
}
