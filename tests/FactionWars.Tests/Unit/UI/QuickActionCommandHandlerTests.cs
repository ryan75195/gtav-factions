using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Models;
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
using Xunit;

namespace FactionWars.Tests.Unit.UI
{
    /// <summary>
    /// Tests for QuickActionCommandHandler.
    /// Following TDD - defines expected behavior for quick action phone commands.
    /// </summary>
    public class QuickActionCommandHandlerTests
    {
        #region Test Setup

        private readonly Mock<IFactionService> _factionServiceMock;
        private readonly Mock<IZoneService> _zoneServiceMock;
        private readonly Mock<IReinforcementService> _reinforcementServiceMock;
        private readonly Mock<IQuickActionDisplayService> _displayServiceMock;
        private readonly Mock<IGameBridge> _gameBridgeMock;

        public QuickActionCommandHandlerTests()
        {
            _factionServiceMock = new Mock<IFactionService>();
            _zoneServiceMock = new Mock<IZoneService>();
            _reinforcementServiceMock = new Mock<IReinforcementService>();
            _displayServiceMock = new Mock<IQuickActionDisplayService>();
            _gameBridgeMock = new Mock<IGameBridge>();
        }

        private QuickActionCommandHandler CreateHandler()
        {
            return new QuickActionCommandHandler(
                _factionServiceMock.Object,
                _zoneServiceMock.Object,
                _reinforcementServiceMock.Object,
                _displayServiceMock.Object,
                _gameBridgeMock.Object);
        }

        private void SetupPlayerFaction(string factionId = "michael_faction")
        {
            var faction = new Faction(factionId, "Michael's Crew", "Michael De Santa");
            var state = new FactionState(factionId, 50000, 25);
            state.AddZone("zone_1");
            state.AddZone("zone_2");

            _displayServiceMock.Setup(s => s.GetPlayerFactionId()).Returns(factionId);
            _factionServiceMock.Setup(s => s.GetFaction(factionId)).Returns(faction);
            _factionServiceMock.Setup(s => s.GetFactionState(factionId)).Returns(state);
        }

        private Zone CreateTestZone(string id, string? ownerId = "michael_faction")
        {
            var zone = new Zone(id, "Test Zone", new Vector3(0, 0, 0), 100f, 5);
            zone.OwnerFactionId = ownerId;
            return zone;
        }

        #endregion

        #region Constructor Tests

        [Fact]
        public void Constructor_WithNullFactionService_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new QuickActionCommandHandler(
                    null!,
                    _zoneServiceMock.Object,
                    _reinforcementServiceMock.Object,
                    _displayServiceMock.Object,
                    _gameBridgeMock.Object));
        }

        [Fact]
        public void Constructor_WithNullZoneService_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new QuickActionCommandHandler(
                    _factionServiceMock.Object,
                    null!,
                    _reinforcementServiceMock.Object,
                    _displayServiceMock.Object,
                    _gameBridgeMock.Object));
        }

        [Fact]
        public void Constructor_WithNullReinforcementService_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new QuickActionCommandHandler(
                    _factionServiceMock.Object,
                    _zoneServiceMock.Object,
                    null!,
                    _displayServiceMock.Object,
                    _gameBridgeMock.Object));
        }

        [Fact]
        public void Constructor_WithNullDisplayService_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new QuickActionCommandHandler(
                    _factionServiceMock.Object,
                    _zoneServiceMock.Object,
                    _reinforcementServiceMock.Object,
                    null!,
                    _gameBridgeMock.Object));
        }

        [Fact]
        public void Constructor_WithNullGameBridge_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new QuickActionCommandHandler(
                    _factionServiceMock.Object,
                    _zoneServiceMock.Object,
                    _reinforcementServiceMock.Object,
                    _displayServiceMock.Object,
                    null!));
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

        #region CanHandle Tests

        [Theory]
        [InlineData("quick_reinforcements")]
        [InlineData("quick_rally")]
        [InlineData("quick_attack")]
        [InlineData("quick_defend")]
        public void CanHandle_SupportedCommands_ReturnsTrue(string commandId)
        {
            // Arrange
            var handler = CreateHandler();

            // Act
            var result = handler.CanHandle(commandId);

            // Assert
            Assert.True(result);
        }

        [Theory]
        [InlineData("faction_status")]
        [InlineData("unknown_command")]
        [InlineData("")]
        public void CanHandle_UnsupportedCommands_ReturnsFalse(string commandId)
        {
            // Arrange
            var handler = CreateHandler();

            // Act
            var result = handler.CanHandle(commandId);

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

        #region HandleCommand Tests - Null/Invalid

        [Fact]
        public void HandleCommand_NullCommand_DoesNotThrow()
        {
            // Arrange
            var handler = CreateHandler();

            // Act & Assert (should not throw)
            handler.HandleCommand(null!);
        }

        [Fact]
        public void HandleCommand_NoPlayerFaction_ShowsError()
        {
            // Arrange
            var handler = CreateHandler();
            _displayServiceMock.Setup(s => s.GetPlayerFactionId()).Returns((string?)null);
            var command = new PhoneCommand("quick_reinforcements", "Call Backup");

            // Act
            handler.HandleCommand(command);

            // Assert
            _displayServiceMock.Verify(s => s.ShowError("No faction assigned"), Times.Once);
        }

        [Fact]
        public void HandleCommand_FactionNotFound_ShowsError()
        {
            // Arrange
            var handler = CreateHandler();
            _displayServiceMock.Setup(s => s.GetPlayerFactionId()).Returns("unknown_faction");
            _factionServiceMock.Setup(s => s.GetFaction("unknown_faction")).Returns((Faction?)null);
            var command = new PhoneCommand("quick_reinforcements", "Call Backup");

            // Act
            handler.HandleCommand(command);

            // Assert
            _displayServiceMock.Verify(s => s.ShowError("Faction not found"), Times.Once);
        }

        #endregion

        #region HandleCommand Tests - quick_reinforcements

        [Fact]
        public void HandleCommand_QuickReinforcements_NotInZone_ShowsError()
        {
            // Arrange
            var handler = CreateHandler();
            SetupPlayerFaction();
            var playerPos = new Vector3(100, 100, 0);
            _gameBridgeMock.Setup(g => g.GetPlayerPosition()).Returns(playerPos);
            _zoneServiceMock.Setup(z => z.GetZoneAtPosition(playerPos)).Returns((Zone?)null);
            var command = new PhoneCommand("quick_reinforcements", "Call Backup");

            // Act
            handler.HandleCommand(command);

            // Assert
            _displayServiceMock.Verify(s => s.ShowError("Not in a controlled zone"), Times.Once);
        }

        [Fact]
        public void HandleCommand_QuickReinforcements_InEnemyZone_ShowsError()
        {
            // Arrange
            var handler = CreateHandler();
            SetupPlayerFaction();
            var playerPos = new Vector3(100, 100, 0);
            _gameBridgeMock.Setup(g => g.GetPlayerPosition()).Returns(playerPos);
            var enemyZone = CreateTestZone("enemy_zone", "trevor_faction");
            _zoneServiceMock.Setup(z => z.GetZoneAtPosition(playerPos)).Returns(enemyZone);
            var command = new PhoneCommand("quick_reinforcements", "Call Backup");

            // Act
            handler.HandleCommand(command);

            // Assert
            _displayServiceMock.Verify(s => s.ShowError("Cannot call reinforcements in enemy territory"), Times.Once);
        }

        [Fact]
        public void HandleCommand_QuickReinforcements_InOwnZone_RequestsReinforcements()
        {
            // Arrange
            var handler = CreateHandler();
            SetupPlayerFaction();
            var playerPos = new Vector3(100, 100, 0);
            _gameBridgeMock.Setup(g => g.GetPlayerPosition()).Returns(playerPos);
            var ownZone = CreateTestZone("zone_1", "michael_faction");
            _zoneServiceMock.Setup(z => z.GetZoneAtPosition(playerPos)).Returns(ownZone);
            var spawnedPeds = new List<PedHandle>
            {
                new PedHandle(1),
                new PedHandle(2),
                new PedHandle(3),
                new PedHandle(4),
                new PedHandle(5)
            };
            var successResult = ReinforcementResult.Success(spawnedPeds, 500);
            _reinforcementServiceMock
                .Setup(r => r.RequestReinforcements(It.IsAny<ReinforcementRequest>()))
                .Returns(successResult);
            var command = new PhoneCommand("quick_reinforcements", "Call Backup");

            // Act
            handler.HandleCommand(command);

            // Assert
            _displayServiceMock.Verify(s => s.ShowReinforcementsRequested(5), Times.Once);
        }

        [Fact]
        public void HandleCommand_QuickReinforcements_OnCooldown_ShowsCooldownMessage()
        {
            // Arrange
            var handler = CreateHandler();
            SetupPlayerFaction();
            var playerPos = new Vector3(100, 100, 0);
            _gameBridgeMock.Setup(g => g.GetPlayerPosition()).Returns(playerPos);
            var ownZone = CreateTestZone("zone_1", "michael_faction");
            _zoneServiceMock.Setup(z => z.GetZoneAtPosition(playerPos)).Returns(ownZone);
            var cooldownResult = ReinforcementResult.OnCooldown(30f);
            _reinforcementServiceMock
                .Setup(r => r.RequestReinforcements(It.IsAny<ReinforcementRequest>()))
                .Returns(cooldownResult);
            var command = new PhoneCommand("quick_reinforcements", "Call Backup");

            // Act
            handler.HandleCommand(command);

            // Assert
            _displayServiceMock.Verify(s => s.ShowError("Reinforcements on cooldown"), Times.Once);
        }

        [Fact]
        public void HandleCommand_QuickReinforcements_InsufficientResources_ShowsResourceError()
        {
            // Arrange
            var handler = CreateHandler();
            SetupPlayerFaction();
            var playerPos = new Vector3(100, 100, 0);
            _gameBridgeMock.Setup(g => g.GetPlayerPosition()).Returns(playerPos);
            var ownZone = CreateTestZone("zone_1", "michael_faction");
            _zoneServiceMock.Setup(z => z.GetZoneAtPosition(playerPos)).Returns(ownZone);
            var resourceResult = ReinforcementResult.InsufficientResources(500, 100);
            _reinforcementServiceMock
                .Setup(r => r.RequestReinforcements(It.IsAny<ReinforcementRequest>()))
                .Returns(resourceResult);
            var command = new PhoneCommand("quick_reinforcements", "Call Backup");

            // Act
            handler.HandleCommand(command);

            // Assert
            _displayServiceMock.Verify(s => s.ShowError("Insufficient resources for reinforcements"), Times.Once);
        }

        #endregion

        #region HandleCommand Tests - quick_rally

        [Fact]
        public void HandleCommand_QuickRally_IssuesRallyOrder()
        {
            // Arrange
            var handler = CreateHandler();
            SetupPlayerFaction();
            var playerPos = new Vector3(100, 100, 0);
            _gameBridgeMock.Setup(g => g.GetPlayerPosition()).Returns(playerPos);
            var command = new PhoneCommand("quick_rally", "Rally Troops");

            // Act
            handler.HandleCommand(command);

            // Assert
            _displayServiceMock.Verify(s => s.ShowRallyOrdered(playerPos), Times.Once);
        }

        #endregion

        #region HandleCommand Tests - quick_attack

        [Fact]
        public void HandleCommand_QuickAttack_NotNearTargetZone_ShowsError()
        {
            // Arrange
            var handler = CreateHandler();
            SetupPlayerFaction();
            var playerPos = new Vector3(100, 100, 0);
            _gameBridgeMock.Setup(g => g.GetPlayerPosition()).Returns(playerPos);
            _zoneServiceMock.Setup(z => z.GetZoneAtPosition(playerPos)).Returns((Zone?)null);
            var command = new PhoneCommand("quick_attack", "Attack Zone");

            // Act
            handler.HandleCommand(command);

            // Assert
            _displayServiceMock.Verify(s => s.ShowError("No target zone nearby"), Times.Once);
        }

        [Fact]
        public void HandleCommand_QuickAttack_AlreadyOwned_ShowsError()
        {
            // Arrange
            var handler = CreateHandler();
            SetupPlayerFaction();
            var playerPos = new Vector3(100, 100, 0);
            _gameBridgeMock.Setup(g => g.GetPlayerPosition()).Returns(playerPos);
            var ownZone = CreateTestZone("zone_1", "michael_faction");
            _zoneServiceMock.Setup(z => z.GetZoneAtPosition(playerPos)).Returns(ownZone);
            var command = new PhoneCommand("quick_attack", "Attack Zone");

            // Act
            handler.HandleCommand(command);

            // Assert
            _displayServiceMock.Verify(s => s.ShowError("Already control this zone"), Times.Once);
        }

        [Fact]
        public void HandleCommand_QuickAttack_EnemyZone_InitiatesAttack()
        {
            // Arrange
            var handler = CreateHandler();
            SetupPlayerFaction();
            var playerPos = new Vector3(100, 100, 0);
            _gameBridgeMock.Setup(g => g.GetPlayerPosition()).Returns(playerPos);
            var enemyZone = CreateTestZone("enemy_zone", "trevor_faction");
            _zoneServiceMock.Setup(z => z.GetZoneAtPosition(playerPos)).Returns(enemyZone);
            var command = new PhoneCommand("quick_attack", "Attack Zone");

            // Act
            handler.HandleCommand(command);

            // Assert
            _displayServiceMock.Verify(s => s.ShowAttackInitiated("enemy_zone", "Test Zone"), Times.Once);
        }

        [Fact]
        public void HandleCommand_QuickAttack_NeutralZone_InitiatesAttack()
        {
            // Arrange
            var handler = CreateHandler();
            SetupPlayerFaction();
            var playerPos = new Vector3(100, 100, 0);
            _gameBridgeMock.Setup(g => g.GetPlayerPosition()).Returns(playerPos);
            var neutralZone = CreateTestZone("neutral_zone", null);
            _zoneServiceMock.Setup(z => z.GetZoneAtPosition(playerPos)).Returns(neutralZone);
            var command = new PhoneCommand("quick_attack", "Attack Zone");

            // Act
            handler.HandleCommand(command);

            // Assert
            _displayServiceMock.Verify(s => s.ShowAttackInitiated("neutral_zone", "Test Zone"), Times.Once);
        }

        #endregion

        #region HandleCommand Tests - quick_defend

        [Fact]
        public void HandleCommand_QuickDefend_NotInZone_ShowsError()
        {
            // Arrange
            var handler = CreateHandler();
            SetupPlayerFaction();
            var playerPos = new Vector3(100, 100, 0);
            _gameBridgeMock.Setup(g => g.GetPlayerPosition()).Returns(playerPos);
            _zoneServiceMock.Setup(z => z.GetZoneAtPosition(playerPos)).Returns((Zone?)null);
            var command = new PhoneCommand("quick_defend", "Defend Zone");

            // Act
            handler.HandleCommand(command);

            // Assert
            _displayServiceMock.Verify(s => s.ShowError("Not in a controlled zone"), Times.Once);
        }

        [Fact]
        public void HandleCommand_QuickDefend_InEnemyZone_ShowsError()
        {
            // Arrange
            var handler = CreateHandler();
            SetupPlayerFaction();
            var playerPos = new Vector3(100, 100, 0);
            _gameBridgeMock.Setup(g => g.GetPlayerPosition()).Returns(playerPos);
            var enemyZone = CreateTestZone("enemy_zone", "trevor_faction");
            _zoneServiceMock.Setup(z => z.GetZoneAtPosition(playerPos)).Returns(enemyZone);
            var command = new PhoneCommand("quick_defend", "Defend Zone");

            // Act
            handler.HandleCommand(command);

            // Assert
            _displayServiceMock.Verify(s => s.ShowError("Cannot defend enemy territory"), Times.Once);
        }

        [Fact]
        public void HandleCommand_QuickDefend_InOwnZone_InitiatesDefense()
        {
            // Arrange
            var handler = CreateHandler();
            SetupPlayerFaction();
            var playerPos = new Vector3(100, 100, 0);
            _gameBridgeMock.Setup(g => g.GetPlayerPosition()).Returns(playerPos);
            var ownZone = CreateTestZone("zone_1", "michael_faction");
            _zoneServiceMock.Setup(z => z.GetZoneAtPosition(playerPos)).Returns(ownZone);
            var command = new PhoneCommand("quick_defend", "Defend Zone");

            // Act
            handler.HandleCommand(command);

            // Assert
            _displayServiceMock.Verify(s => s.ShowDefenseInitiated("zone_1", "Test Zone"), Times.Once);
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

        #endregion
    }
}
