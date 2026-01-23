using FactionWars.Core.Interfaces;
using FactionWars.Factions.Interfaces;
using FactionWars.Factions.Models;
using FactionWars.Territory.Interfaces;
using FactionWars.Territory.Models;
using FactionWars.UI.Handlers;
using FactionWars.UI.Interfaces;
using FactionWars.UI.Models;
using Moq;
using System;
using System.Collections.Generic;
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
        private readonly Mock<IZoneService> _zoneServiceMock;
        private readonly Mock<IZoneDefenderAllocationService> _allocationServiceMock;
        private readonly Mock<IStatusDisplayService> _statusDisplayMock;

        public FactionStatusCommandHandlerTests()
        {
            _factionServiceMock = new Mock<IFactionService>();
            _zoneServiceMock = new Mock<IZoneService>();
            _allocationServiceMock = new Mock<IZoneDefenderAllocationService>();
            _statusDisplayMock = new Mock<IStatusDisplayService>();
        }

        private FactionStatusCommandHandler CreateHandler()
        {
            return new FactionStatusCommandHandler(
                _factionServiceMock.Object,
                _zoneServiceMock.Object,
                _allocationServiceMock.Object,
                _statusDisplayMock.Object);
        }

        private void SetupPlayerFaction(string factionId = "michael_faction")
        {
            var faction = new Faction(factionId, "Michael's Crew", "Michael De Santa");
            // After consolidation, initialTroopCount goes to Basic tier, so we just use reserve pool
            var state = new FactionState(factionId, 50000);
            state.AddZone("zone_1");
            state.AddZone("zone_2");
            state.Weapons = 10;
            state.RecruitmentPoints = 100;
            // Add reserve troops for the player (15 basic, 10 medium) = 25 total reserve
            state.AddReserveTroops(FactionWars.Core.Models.DefenderTier.Basic, 15);
            state.AddReserveTroops(FactionWars.Core.Models.DefenderTier.Medium, 10);

            // Create zone objects for the zone service mock
            var center = new Vector3(0, 0, 0);
            var zone1 = new Zone("zone_1", "Zone 1", center);
            zone1.OwnerFactionId = factionId;
            var zone2 = new Zone("zone_2", "Zone 2", center);
            zone2.OwnerFactionId = factionId;
            var zones = new List<Zone> { zone1, zone2 };

            _statusDisplayMock.Setup(s => s.GetPlayerFactionId()).Returns(factionId);
            _factionServiceMock.Setup(s => s.GetFaction(factionId)).Returns(faction);
            _factionServiceMock.Setup(s => s.GetFactionState(factionId)).Returns(state);
            // Zone service returns accurate zone count and zone list
            _zoneServiceMock.Setup(s => s.GetZoneCount(factionId)).Returns(2);
            _zoneServiceMock.Setup(s => s.GetZonesByOwner(factionId)).Returns(zones);
            // Allocation service returns allocated troops (5 deployed to zones)
            _allocationServiceMock.Setup(s => s.GetTotalAllocatedTroops(factionId)).Returns(5);
        }

        #endregion

        #region Constructor Tests

        [Fact]
        public void Constructor_WithNullFactionService_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new FactionStatusCommandHandler(null!, _zoneServiceMock.Object, _allocationServiceMock.Object, _statusDisplayMock.Object));
        }

        [Fact]
        public void Constructor_WithNullZoneService_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new FactionStatusCommandHandler(_factionServiceMock.Object, null!, _allocationServiceMock.Object, _statusDisplayMock.Object));
        }

        [Fact]
        public void Constructor_WithNullAllocationService_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new FactionStatusCommandHandler(_factionServiceMock.Object, _zoneServiceMock.Object, null!, _statusDisplayMock.Object));
        }

        [Fact]
        public void Constructor_WithNullStatusDisplay_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new FactionStatusCommandHandler(_factionServiceMock.Object, _zoneServiceMock.Object, _allocationServiceMock.Object, null!));
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
            // TroopCount = ReserveTroops (15+10=25) + AllocatedTroops (5) = 30
            // MilitaryStrength = TotalTroops (30) + Weapons (10) * 2 = 50
            _statusDisplayMock.Verify(s => s.ShowFactionStatus(
                It.Is<FactionStatusInfo>(info =>
                    info.FactionName == "Michael's Crew" &&
                    info.LeaderName == "Michael De Santa" &&
                    info.Cash == 50000 &&
                    info.TroopCount == 30 &&
                    info.ZoneCount == 2 &&
                    info.Weapons == 10 &&
                    info.RecruitmentPoints == 100 &&
                    info.MilitaryStrength == 50)), // 30 + (10 * 2)
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
            // TroopCount = ReserveTroops (25) + AllocatedTroops (5) = 30
            _statusDisplayMock.Verify(s => s.ShowResourceStatus(
                It.Is<ResourceStatusInfo>(info =>
                    info.Cash == 50000 &&
                    info.Weapons == 10 &&
                    info.RecruitmentPoints == 100 &&
                    info.TroopCount == 30)),
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
