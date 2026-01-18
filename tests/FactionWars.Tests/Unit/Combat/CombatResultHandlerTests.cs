using System;
using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Models;
using FactionWars.Combat.Services;
using FactionWars.Territory.Interfaces;
using FactionWars.Territory.Models;
using FactionWars.Core.Interfaces;
using Moq;
using Xunit;

namespace FactionWars.Tests.Unit.Combat
{
    /// <summary>
    /// Tests for the CombatResultHandler service which processes completed
    /// combat encounters and updates zone state accordingly.
    /// </summary>
    public class CombatResultHandlerTests
    {
        private readonly Mock<IZoneService> _mockZoneService;
        private readonly ICombatResultHandler _handler;

        public CombatResultHandlerTests()
        {
            _mockZoneService = new Mock<IZoneService>();

            // By default, zone service operations succeed
            _mockZoneService
                .Setup(s => s.TransferZoneOwnership(It.IsAny<string>(), It.IsAny<string?>()))
                .Returns(true);
            _mockZoneService
                .Setup(s => s.UpdateZoneControl(It.IsAny<string>(), It.IsAny<float>()))
                .Returns(true);
            _mockZoneService
                .Setup(s => s.SetZoneContested(It.IsAny<string>(), It.IsAny<bool>()))
                .Returns(true);

            _handler = new CombatResultHandler(_mockZoneService.Object);
        }

        private CombatEncounter CreateEncounter(string id = "enc-1", string zoneId = "zone-1",
            string attackerId = "faction-A", string defenderId = "faction-B")
        {
            return new CombatEncounter(id, zoneId, attackerId, defenderId);
        }

        private Zone CreateZone(string id = "zone-1", string? ownerId = "faction-B")
        {
            var zone = new Zone(id, "Test Zone", new Vector3(0, 0, 0));
            zone.OwnerFactionId = ownerId;
            zone.ControlPercentage = 100f;
            return zone;
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithNullZoneService_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new CombatResultHandler(null!));
        }

        [Fact]
        public void Constructor_WithValidZoneService_CreatesInstance()
        {
            var handler = new CombatResultHandler(_mockZoneService.Object);

            Assert.NotNull(handler);
        }

        #endregion

        #region ProcessCombatResult - Parameter Validation

        [Fact]
        public void ProcessCombatResult_WithNullEncounter_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _handler.ProcessCombatResult(null!));
        }

        [Fact]
        public void ProcessCombatResult_WithActiveEncounter_ThrowsInvalidOperationException()
        {
            var encounter = CreateEncounter();
            // Encounter is InProgress by default

            var exception = Assert.Throws<InvalidOperationException>(
                () => _handler.ProcessCombatResult(encounter));
            Assert.Contains("still in progress", exception.Message);
        }

        #endregion

        #region ProcessCombatResult - Attacker Victory

        [Fact]
        public void ProcessCombatResult_AttackerVictory_TransfersZoneOwnership()
        {
            var encounter = CreateEncounter();
            encounter.End(CombatStatus.AttackerVictory);

            _handler.ProcessCombatResult(encounter);

            _mockZoneService.Verify(
                s => s.TransferZoneOwnership("zone-1", "faction-A"),
                Times.Once);
        }

        [Fact]
        public void ProcessCombatResult_AttackerVictory_SetsControlTo100Percent()
        {
            var encounter = CreateEncounter();
            encounter.End(CombatStatus.AttackerVictory);

            _handler.ProcessCombatResult(encounter);

            _mockZoneService.Verify(
                s => s.UpdateZoneControl("zone-1", 100f),
                Times.Once);
        }

        [Fact]
        public void ProcessCombatResult_AttackerVictory_ClearsContestedState()
        {
            var encounter = CreateEncounter();
            encounter.End(CombatStatus.AttackerVictory);

            _handler.ProcessCombatResult(encounter);

            _mockZoneService.Verify(
                s => s.SetZoneContested("zone-1", false),
                Times.Once);
        }

        [Fact]
        public void ProcessCombatResult_AttackerVictory_ReturnsSuccessResult()
        {
            var encounter = CreateEncounter();
            encounter.End(CombatStatus.AttackerVictory);

            var result = _handler.ProcessCombatResult(encounter);

            Assert.True(result.IsSuccess);
            Assert.Equal(CombatResultOutcome.ZoneCaptured, result.Outcome);
            Assert.Equal("faction-A", result.NewOwnerFactionId);
            Assert.Equal("zone-1", result.ZoneId);
        }

        [Fact]
        public void ProcessCombatResult_AttackerVictory_ReturnsNewOwnerAsPreviousDefender()
        {
            var encounter = CreateEncounter(attackerId: "michael", defenderId: "trevor");
            encounter.End(CombatStatus.AttackerVictory);

            var result = _handler.ProcessCombatResult(encounter);

            Assert.Equal("michael", result.NewOwnerFactionId);
            Assert.Equal("trevor", result.PreviousOwnerFactionId);
        }

        #endregion

        #region ProcessCombatResult - Defender Victory

        [Fact]
        public void ProcessCombatResult_DefenderVictory_DoesNotTransferOwnership()
        {
            var encounter = CreateEncounter();
            encounter.End(CombatStatus.DefenderVictory);

            _handler.ProcessCombatResult(encounter);

            _mockZoneService.Verify(
                s => s.TransferZoneOwnership(It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public void ProcessCombatResult_DefenderVictory_SetsControlTo100Percent()
        {
            var encounter = CreateEncounter();
            encounter.End(CombatStatus.DefenderVictory);

            _handler.ProcessCombatResult(encounter);

            _mockZoneService.Verify(
                s => s.UpdateZoneControl("zone-1", 100f),
                Times.Once);
        }

        [Fact]
        public void ProcessCombatResult_DefenderVictory_ClearsContestedState()
        {
            var encounter = CreateEncounter();
            encounter.End(CombatStatus.DefenderVictory);

            _handler.ProcessCombatResult(encounter);

            _mockZoneService.Verify(
                s => s.SetZoneContested("zone-1", false),
                Times.Once);
        }

        [Fact]
        public void ProcessCombatResult_DefenderVictory_ReturnsZoneDefendedOutcome()
        {
            var encounter = CreateEncounter();
            encounter.End(CombatStatus.DefenderVictory);

            var result = _handler.ProcessCombatResult(encounter);

            Assert.True(result.IsSuccess);
            Assert.Equal(CombatResultOutcome.ZoneDefended, result.Outcome);
            Assert.Equal("faction-B", result.NewOwnerFactionId);
            Assert.Equal("faction-B", result.PreviousOwnerFactionId);
        }

        #endregion

        #region ProcessCombatResult - Stalemate

        [Fact]
        public void ProcessCombatResult_Stalemate_DoesNotTransferOwnership()
        {
            var encounter = CreateEncounter();
            encounter.End(CombatStatus.Stalemate);

            _handler.ProcessCombatResult(encounter);

            _mockZoneService.Verify(
                s => s.TransferZoneOwnership(It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public void ProcessCombatResult_Stalemate_KeepsCurrentControlPercentage()
        {
            var encounter = CreateEncounter();
            encounter.AttackerControlPercentage = 45f;
            encounter.DefenderControlPercentage = 55f;
            encounter.End(CombatStatus.Stalemate);

            _handler.ProcessCombatResult(encounter);

            // Should set control to defender's percentage since they still own it
            _mockZoneService.Verify(
                s => s.UpdateZoneControl("zone-1", 55f),
                Times.Once);
        }

        [Fact]
        public void ProcessCombatResult_Stalemate_ClearsContestedState()
        {
            var encounter = CreateEncounter();
            encounter.End(CombatStatus.Stalemate);

            _handler.ProcessCombatResult(encounter);

            _mockZoneService.Verify(
                s => s.SetZoneContested("zone-1", false),
                Times.Once);
        }

        [Fact]
        public void ProcessCombatResult_Stalemate_ReturnsStalemateOutcome()
        {
            var encounter = CreateEncounter();
            encounter.End(CombatStatus.Stalemate);

            var result = _handler.ProcessCombatResult(encounter);

            Assert.True(result.IsSuccess);
            Assert.Equal(CombatResultOutcome.Stalemate, result.Outcome);
            Assert.Equal("faction-B", result.NewOwnerFactionId);
        }

        #endregion

        #region ProcessCombatResult - Aborted

        [Fact]
        public void ProcessCombatResult_Aborted_DoesNotTransferOwnership()
        {
            var encounter = CreateEncounter();
            encounter.End(CombatStatus.Aborted);

            _handler.ProcessCombatResult(encounter);

            _mockZoneService.Verify(
                s => s.TransferZoneOwnership(It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public void ProcessCombatResult_Aborted_KeepsCurrentControlPercentage()
        {
            var encounter = CreateEncounter();
            encounter.AttackerControlPercentage = 30f;
            encounter.DefenderControlPercentage = 70f;
            encounter.End(CombatStatus.Aborted);

            _handler.ProcessCombatResult(encounter);

            _mockZoneService.Verify(
                s => s.UpdateZoneControl("zone-1", 70f),
                Times.Once);
        }

        [Fact]
        public void ProcessCombatResult_Aborted_ClearsContestedState()
        {
            var encounter = CreateEncounter();
            encounter.End(CombatStatus.Aborted);

            _handler.ProcessCombatResult(encounter);

            _mockZoneService.Verify(
                s => s.SetZoneContested("zone-1", false),
                Times.Once);
        }

        [Fact]
        public void ProcessCombatResult_Aborted_ReturnsAbortedOutcome()
        {
            var encounter = CreateEncounter();
            encounter.End(CombatStatus.Aborted);

            var result = _handler.ProcessCombatResult(encounter);

            Assert.True(result.IsSuccess);
            Assert.Equal(CombatResultOutcome.Aborted, result.Outcome);
        }

        #endregion

        #region ProcessCombatResult - Zone Not Found

        [Fact]
        public void ProcessCombatResult_ZoneNotFound_ReturnsFailure()
        {
            // Create a separate mock that returns false for zone operations
            var failingZoneService = new Mock<IZoneService>();
            failingZoneService
                .Setup(s => s.TransferZoneOwnership(It.IsAny<string>(), It.IsAny<string?>()))
                .Returns(false);
            failingZoneService
                .Setup(s => s.UpdateZoneControl(It.IsAny<string>(), It.IsAny<float>()))
                .Returns(false);
            failingZoneService
                .Setup(s => s.SetZoneContested(It.IsAny<string>(), It.IsAny<bool>()))
                .Returns(false);

            var handler = new CombatResultHandler(failingZoneService.Object);

            var encounter = CreateEncounter(zoneId: "nonexistent-zone");
            encounter.End(CombatStatus.AttackerVictory);

            var result = handler.ProcessCombatResult(encounter);

            Assert.False(result.IsSuccess);
            Assert.Equal(CombatResultOutcome.ZoneNotFound, result.Outcome);
        }

        #endregion

        #region CombatProcessingResult Model Tests

        [Fact]
        public void CombatProcessingResult_Success_HasCorrectProperties()
        {
            var result = CombatProcessingResult.Success(
                CombatResultOutcome.ZoneCaptured,
                "zone-1",
                "faction-A",
                "faction-B");

            Assert.True(result.IsSuccess);
            Assert.Equal(CombatResultOutcome.ZoneCaptured, result.Outcome);
            Assert.Equal("zone-1", result.ZoneId);
            Assert.Equal("faction-A", result.NewOwnerFactionId);
            Assert.Equal("faction-B", result.PreviousOwnerFactionId);
        }

        [Fact]
        public void CombatProcessingResult_Failure_HasCorrectProperties()
        {
            var result = CombatProcessingResult.Failure(
                CombatResultOutcome.ZoneNotFound,
                "zone-1");

            Assert.False(result.IsSuccess);
            Assert.Equal(CombatResultOutcome.ZoneNotFound, result.Outcome);
            Assert.Equal("zone-1", result.ZoneId);
            Assert.Null(result.NewOwnerFactionId);
            Assert.Null(result.PreviousOwnerFactionId);
        }

        #endregion

        #region CombatResultOutcome Enum Tests

        [Fact]
        public void CombatResultOutcome_HasExpectedValues()
        {
            Assert.True(Enum.IsDefined(typeof(CombatResultOutcome), CombatResultOutcome.ZoneCaptured));
            Assert.True(Enum.IsDefined(typeof(CombatResultOutcome), CombatResultOutcome.ZoneDefended));
            Assert.True(Enum.IsDefined(typeof(CombatResultOutcome), CombatResultOutcome.Stalemate));
            Assert.True(Enum.IsDefined(typeof(CombatResultOutcome), CombatResultOutcome.Aborted));
            Assert.True(Enum.IsDefined(typeof(CombatResultOutcome), CombatResultOutcome.ZoneNotFound));
        }

        #endregion
    }
}
