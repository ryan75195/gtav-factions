using FactionWars.Factions.Interfaces;
using FactionWars.Factions.Models;
using FactionWars.UI.Handlers;
using FactionWars.UI.Interfaces;
using FactionWars.UI.Models;
using Moq;
using System;
using System.Linq;
using Xunit;

namespace FactionWars.Tests.Unit.UI
{
    /// <summary>
    /// Tests for FactionStatusCommandHandler.
    /// Following TDD - defines expected behavior for faction status command handling.
    /// </summary>
    public class FactionStatusCommandHandlerTests
    {
        #region Test Setup

        private readonly Mock<IFactionService> _factionServiceMock;
        private readonly Mock<IStatusDisplayService> _statusDisplayMock;

        public FactionStatusCommandHandlerTests()
        {
            _factionServiceMock = new Mock<IFactionService>();
            _statusDisplayMock = new Mock<IStatusDisplayService>();
        }

        private FactionStatusCommandHandler CreateHandler()
        {
            return new FactionStatusCommandHandler(
                _factionServiceMock.Object,
                _statusDisplayMock.Object);
        }

        private void SetupPlayerFaction(string factionId = "michael_faction")
        {
            var faction = new Faction(factionId, "Michael's Crew", "Michael De Santa");
            var state = new FactionState(factionId, 50000, 25);
            state.AddZone("zone_1");
            state.AddZone("zone_2");
            state.Weapons = 10;
            state.RecruitmentPoints = 100;

            _statusDisplayMock.Setup(s => s.GetPlayerFactionId()).Returns(factionId);
            _factionServiceMock.Setup(s => s.GetFaction(factionId)).Returns(faction);
            _factionServiceMock.Setup(s => s.GetFactionState(factionId)).Returns(state);
        }

        #endregion

        #region Constructor Tests

        [Fact]
        public void Constructor_WithNullFactionService_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new FactionStatusCommandHandler(null!, _statusDisplayMock.Object));
        }

        [Fact]
        public void Constructor_WithNullStatusDisplay_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new FactionStatusCommandHandler(_factionServiceMock.Object, null!));
        }

        [Fact]
        public void Constructor_WithValidDependencies_CreatesInstance()
        {
            // Act
            var handler = CreateHandler();

            // Assert
            Assert.NotNull(handler);
        }

        #endregion

        #region HandleCommand Tests - faction_status

        [Fact]
        public void HandleCommand_FactionStatus_DisplaysFactionOverview()
        {
            // Arrange
            var handler = CreateHandler();
            SetupPlayerFaction();
            var command = new PhoneCommand("faction_status", "Status");

            // Act
            handler.HandleCommand(command);

            // Assert
            _statusDisplayMock.Verify(s => s.ShowFactionStatus(
                It.Is<FactionStatusInfo>(info =>
                    info.FactionName == "Michael's Crew" &&
                    info.LeaderName == "Michael De Santa" &&
                    info.Cash == 50000 &&
                    info.TroopCount == 25 &&
                    info.ZoneCount == 2 &&
                    info.Weapons == 10 &&
                    info.RecruitmentPoints == 100 &&
                    info.MilitaryStrength == 45)), // 25 + (10 * 2)
                Times.Once);
        }

        [Fact]
        public void HandleCommand_FactionStatus_NoPlayerFaction_ShowsError()
        {
            // Arrange
            var handler = CreateHandler();
            _statusDisplayMock.Setup(s => s.GetPlayerFactionId()).Returns((string?)null);
            var command = new PhoneCommand("faction_status", "Status");

            // Act
            handler.HandleCommand(command);

            // Assert
            _statusDisplayMock.Verify(s => s.ShowError("No faction assigned"), Times.Once);
        }

        [Fact]
        public void HandleCommand_FactionStatus_FactionNotFound_ShowsError()
        {
            // Arrange
            var handler = CreateHandler();
            _statusDisplayMock.Setup(s => s.GetPlayerFactionId()).Returns("unknown_faction");
            _factionServiceMock.Setup(s => s.GetFaction("unknown_faction")).Returns((Faction?)null);
            var command = new PhoneCommand("faction_status", "Status");

            // Act
            handler.HandleCommand(command);

            // Assert
            _statusDisplayMock.Verify(s => s.ShowError("Faction not found"), Times.Once);
        }

        #endregion

        #region HandleCommand Tests - faction_resources

        [Fact]
        public void HandleCommand_FactionResources_DisplaysResourceDetails()
        {
            // Arrange
            var handler = CreateHandler();
            SetupPlayerFaction();
            var command = new PhoneCommand("faction_resources", "Resources");

            // Act
            handler.HandleCommand(command);

            // Assert
            _statusDisplayMock.Verify(s => s.ShowResourceStatus(
                It.Is<ResourceStatusInfo>(info =>
                    info.Cash == 50000 &&
                    info.Weapons == 10 &&
                    info.RecruitmentPoints == 100 &&
                    info.TroopCount == 25)),
                Times.Once);
        }

        [Fact]
        public void HandleCommand_FactionResources_NoPlayerFaction_ShowsError()
        {
            // Arrange
            var handler = CreateHandler();
            _statusDisplayMock.Setup(s => s.GetPlayerFactionId()).Returns((string?)null);
            var command = new PhoneCommand("faction_resources", "Resources");

            // Act
            handler.HandleCommand(command);

            // Assert
            _statusDisplayMock.Verify(s => s.ShowError("No faction assigned"), Times.Once);
        }

        #endregion

        #region HandleCommand Tests - faction_territory

        [Fact]
        public void HandleCommand_FactionTerritory_DisplaysTerritoryDetails()
        {
            // Arrange
            var handler = CreateHandler();
            SetupPlayerFaction();
            var command = new PhoneCommand("faction_territory", "Territory");

            // Act
            handler.HandleCommand(command);

            // Assert
            _statusDisplayMock.Verify(s => s.ShowTerritoryStatus(
                It.Is<TerritoryStatusInfo>(info =>
                    info.ZoneCount == 2 &&
                    info.ZoneIds.Contains("zone_1") &&
                    info.ZoneIds.Contains("zone_2"))),
                Times.Once);
        }

        [Fact]
        public void HandleCommand_FactionTerritory_NoPlayerFaction_ShowsError()
        {
            // Arrange
            var handler = CreateHandler();
            _statusDisplayMock.Setup(s => s.GetPlayerFactionId()).Returns((string?)null);
            var command = new PhoneCommand("faction_territory", "Territory");

            // Act
            handler.HandleCommand(command);

            // Assert
            _statusDisplayMock.Verify(s => s.ShowError("No faction assigned"), Times.Once);
        }

        #endregion

        #region HandleCommand Tests - Unknown Commands

        [Fact]
        public void HandleCommand_UnknownCommand_DoesNotThrow()
        {
            // Arrange
            var handler = CreateHandler();
            SetupPlayerFaction();
            var command = new PhoneCommand("unknown_cmd", "Unknown");

            // Act & Assert (should not throw)
            handler.HandleCommand(command);
        }

        [Fact]
        public void HandleCommand_NullCommand_DoesNotThrow()
        {
            // Arrange
            var handler = CreateHandler();

            // Act & Assert (should not throw)
            handler.HandleCommand(null!);
        }

        #endregion

        #region CanHandle Tests

        [Fact]
        public void CanHandle_FactionStatusCommand_ReturnsTrue()
        {
            // Arrange
            var handler = CreateHandler();

            // Act
            var result = handler.CanHandle("faction_status");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void CanHandle_FactionResourcesCommand_ReturnsTrue()
        {
            // Arrange
            var handler = CreateHandler();

            // Act
            var result = handler.CanHandle("faction_resources");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void CanHandle_FactionTerritoryCommand_ReturnsTrue()
        {
            // Arrange
            var handler = CreateHandler();

            // Act
            var result = handler.CanHandle("faction_territory");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void CanHandle_UnknownCommand_ReturnsFalse()
        {
            // Arrange
            var handler = CreateHandler();

            // Act
            var result = handler.CanHandle("unknown_command");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void CanHandle_NullCommandId_ReturnsFalse()
        {
            // Arrange
            var handler = CreateHandler();

            // Act
            var result = handler.CanHandle(null!);

            // Assert
            Assert.False(result);
        }

        #endregion
    }
}
