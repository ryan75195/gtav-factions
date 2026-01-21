using System;
using System.Collections.Generic;
using FactionWars.AI.Models;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.Factions.Interfaces;
using FactionWars.Factions.Models;
using FactionWars.ScriptHookV.Managers;
using FactionWars.Territory.Interfaces;
using FactionWars.Territory.Models;
using FactionWars.UI.Interfaces;
using Moq;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV.Managers
{
    /// <summary>
    /// Tests for BackgroundBattleSimulator which handles AI vs AI combat
    /// when the player is not present in the zone.
    /// </summary>
    public class BackgroundBattleSimulatorTests
    {
        private readonly Mock<IBattleSimulationService> _battleSimulationServiceMock;
        private readonly Mock<IFactionService> _factionServiceMock;
        private readonly Mock<IZoneService> _zoneServiceMock;
        private readonly Mock<IZoneDefenderAllocationService> _allocationServiceMock;
        private readonly Mock<IEventAlertService> _eventAlertServiceMock;
        private readonly Mock<IEventFeedService> _eventFeedServiceMock;
        private readonly BackgroundBattleSimulator _simulator;

        public BackgroundBattleSimulatorTests()
        {
            _battleSimulationServiceMock = new Mock<IBattleSimulationService>();
            _factionServiceMock = new Mock<IFactionService>();
            _zoneServiceMock = new Mock<IZoneService>();
            _allocationServiceMock = new Mock<IZoneDefenderAllocationService>();
            _eventAlertServiceMock = new Mock<IEventAlertService>();
            _eventFeedServiceMock = new Mock<IEventFeedService>();

            _simulator = new BackgroundBattleSimulator(
                _battleSimulationServiceMock.Object,
                _factionServiceMock.Object,
                _zoneServiceMock.Object,
                _allocationServiceMock.Object,
                _eventAlertServiceMock.Object,
                _eventFeedServiceMock.Object);
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithNullBattleSimulationService_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new BackgroundBattleSimulator(
                null!,
                _factionServiceMock.Object,
                _zoneServiceMock.Object,
                _allocationServiceMock.Object,
                _eventAlertServiceMock.Object,
                _eventFeedServiceMock.Object));
        }

        [Fact]
        public void Constructor_WithNullFactionService_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new BackgroundBattleSimulator(
                _battleSimulationServiceMock.Object,
                null!,
                _zoneServiceMock.Object,
                _allocationServiceMock.Object,
                _eventAlertServiceMock.Object,
                _eventFeedServiceMock.Object));
        }

        [Fact]
        public void Constructor_WithNullZoneService_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new BackgroundBattleSimulator(
                _battleSimulationServiceMock.Object,
                _factionServiceMock.Object,
                null!,
                _allocationServiceMock.Object,
                _eventAlertServiceMock.Object,
                _eventFeedServiceMock.Object));
        }

        [Fact]
        public void Constructor_WithNullAllocationService_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new BackgroundBattleSimulator(
                _battleSimulationServiceMock.Object,
                _factionServiceMock.Object,
                _zoneServiceMock.Object,
                null!,
                _eventAlertServiceMock.Object,
                _eventFeedServiceMock.Object));
        }

        [Fact]
        public void Constructor_WithValidParameters_Succeeds()
        {
            var simulator = new BackgroundBattleSimulator(
                _battleSimulationServiceMock.Object,
                _factionServiceMock.Object,
                _zoneServiceMock.Object,
                _allocationServiceMock.Object,
                _eventAlertServiceMock.Object,
                _eventFeedServiceMock.Object);

            Assert.NotNull(simulator);
        }

        #endregion

        #region ProcessAttackDecision Tests

        [Fact]
        public void ProcessAttackDecision_WithValidAttackDecision_SimulatesBattle()
        {
            // Arrange
            var decision = new AIDecision(AIDecisionType.Attack, "zone_downtown", 0.8f, 20);
            SetupFactionData("trevor", "michael", "zone_downtown");
            SetupDefenderAllocation("michael", "zone_downtown", 10, 5, 2);
            SetupBattleSimulationResult(true);

            // Act
            var result = _simulator.ProcessAttackDecision("trevor", decision);

            // Assert
            Assert.NotNull(result);
            _battleSimulationServiceMock.Verify(
                s => s.SimulateBattle(
                    "trevor",
                    "michael",
                    "zone_downtown",
                    It.IsAny<TroopComposition>(),
                    It.IsAny<TroopComposition>()),
                Times.Once);
        }

        [Fact]
        public void ProcessAttackDecision_WithNonAttackDecision_ReturnsNull()
        {
            // Arrange
            var decision = new AIDecision(AIDecisionType.Defend, "zone_downtown", 0.8f, 20);

            // Act
            var result = _simulator.ProcessAttackDecision("trevor", decision);

            // Assert
            Assert.Null(result);
            _battleSimulationServiceMock.Verify(
                s => s.SimulateBattle(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<TroopComposition>(),
                    It.IsAny<TroopComposition>()),
                Times.Never);
        }

        [Fact]
        public void ProcessAttackDecision_WithNullTargetZone_ReturnsNull()
        {
            // Arrange
            var decision = new AIDecision(AIDecisionType.Attack, null, 0.8f, 20);

            // Act
            var result = _simulator.ProcessAttackDecision("trevor", decision);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void ProcessAttackDecision_ZoneNotFound_ReturnsNull()
        {
            // Arrange
            var decision = new AIDecision(AIDecisionType.Attack, "zone_unknown", 0.8f, 20);
            _zoneServiceMock.Setup(z => z.GetZone("zone_unknown")).Returns((Zone?)null);

            // Act
            var result = _simulator.ProcessAttackDecision("trevor", decision);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void ProcessAttackDecision_ZoneHasNoOwner_CapturesZone()
        {
            // Arrange
            var decision = new AIDecision(AIDecisionType.Attack, "zone_neutral", 0.8f, 20);
            var zone = new Zone("zone_neutral", "Neutral Zone", new Vector3(0, 0, 0), 100f, 5);
            zone.OwnerFactionId = null;
            _zoneServiceMock.Setup(z => z.GetZone("zone_neutral")).Returns(zone);

            var trevorFaction = new Faction("trevor", "Trevor's Gang", "Trevor Philips");
            _factionServiceMock.Setup(f => f.GetFaction("trevor")).Returns(trevorFaction);

            // Act
            var result = _simulator.ProcessAttackDecision("trevor", decision);

            // Assert - neutral zone should be captured automatically
            Assert.NotNull(result);
            Assert.True(result!.AttackerWon);
            Assert.Equal("trevor", result.AttackerFactionId);
            Assert.Equal("neutral", result.DefenderFactionId);
            Assert.Equal("zone_neutral", result.ZoneId);
            Assert.Equal(0, result.AttackerCasualties.TotalCount);
            Assert.Equal(0, result.DefenderCasualties.TotalCount);

            // Verify zone ownership transferred
            _zoneServiceMock.Verify(z => z.TransferZoneOwnership("zone_neutral", "trevor"), Times.Once);

            // Verify UI notifications
            _eventFeedServiceMock.Verify(e => e.AddZoneCaptured("Neutral Zone", "Trevor's Gang"), Times.Once);
            _eventAlertServiceMock.Verify(e => e.RaiseZoneCaptured("Neutral Zone", "Trevor's Gang"), Times.Once);
        }

        [Fact]
        public void ProcessAttackDecision_AttackerOwnsZone_ReturnsNull()
        {
            // Arrange - attacker trying to attack own zone
            var decision = new AIDecision(AIDecisionType.Attack, "zone_owned", 0.8f, 20);
            var zone = new Zone("zone_owned", "Owned Zone", new Vector3(0, 0, 0), 100f, 5);
            zone.OwnerFactionId = "trevor"; // Same as attacker
            _zoneServiceMock.Setup(z => z.GetZone("zone_owned")).Returns(zone);

            // Act
            var result = _simulator.ProcessAttackDecision("trevor", decision);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void ProcessAttackDecision_AttackerWins_TransfersZoneOwnership()
        {
            // Arrange
            var decision = new AIDecision(AIDecisionType.Attack, "zone_downtown", 0.8f, 20);
            SetupFactionData("trevor", "michael", "zone_downtown");
            SetupDefenderAllocation("michael", "zone_downtown", 10, 5, 2);
            SetupBattleSimulationResult(attackerWon: true);

            // Act
            var result = _simulator.ProcessAttackDecision("trevor", decision);

            // Assert
            Assert.True(result?.AttackerWon);
            _zoneServiceMock.Verify(z => z.TransferZoneOwnership("zone_downtown", "trevor"), Times.Once);
        }

        [Fact]
        public void ProcessAttackDecision_DefenderWins_DoesNotTransferZoneOwnership()
        {
            // Arrange
            var decision = new AIDecision(AIDecisionType.Attack, "zone_downtown", 0.8f, 20);
            SetupFactionData("trevor", "michael", "zone_downtown");
            SetupDefenderAllocation("michael", "zone_downtown", 10, 5, 2);
            SetupBattleSimulationResult(attackerWon: false);

            // Act
            var result = _simulator.ProcessAttackDecision("trevor", decision);

            // Assert
            Assert.False(result?.AttackerWon);
            _zoneServiceMock.Verify(z => z.TransferZoneOwnership(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void ProcessAttackDecision_AttackerCasualties_DeductedFromAttackerTroops()
        {
            // Arrange
            var decision = new AIDecision(AIDecisionType.Attack, "zone_downtown", 0.8f, 20);
            SetupFactionData("trevor", "michael", "zone_downtown");
            SetupDefenderAllocation("michael", "zone_downtown", 10, 5, 2);

            var attackerCasualties = new TroopComposition(3, 2, 1);
            var defenderCasualties = new TroopComposition(5, 3, 1);
            var simulationResult = BattleSimulationResult.AttackerVictory(
                "trevor", "michael", "zone_downtown",
                attackerCasualties, defenderCasualties);

            _battleSimulationServiceMock.Setup(s => s.SimulateBattle(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<TroopComposition>(),
                    It.IsAny<TroopComposition>()))
                .Returns(simulationResult);

            // Act
            _simulator.ProcessAttackDecision("trevor", decision);

            // Assert - should lose 6 troops total (3+2+1)
            _factionServiceMock.Verify(f => f.LoseTroops("trevor", 6), Times.Once);
        }

        [Fact]
        public void ProcessAttackDecision_DefenderCasualties_DeductedFromDefenderTroops()
        {
            // Arrange
            var decision = new AIDecision(AIDecisionType.Attack, "zone_downtown", 0.8f, 20);
            SetupFactionData("trevor", "michael", "zone_downtown");
            SetupDefenderAllocation("michael", "zone_downtown", 10, 5, 2);

            var attackerCasualties = new TroopComposition(3, 2, 1);
            var defenderCasualties = new TroopComposition(5, 3, 1);
            var simulationResult = BattleSimulationResult.AttackerVictory(
                "trevor", "michael", "zone_downtown",
                attackerCasualties, defenderCasualties);

            _battleSimulationServiceMock.Setup(s => s.SimulateBattle(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<TroopComposition>(),
                    It.IsAny<TroopComposition>()))
                .Returns(simulationResult);

            // Act
            _simulator.ProcessAttackDecision("trevor", decision);

            // Assert - should lose 9 troops total (5+3+1)
            _factionServiceMock.Verify(f => f.LoseTroops("michael", 9), Times.Once);
        }

        #endregion

        #region OnBattleCompleted Event Tests

        [Fact]
        public void ProcessAttackDecision_RaisesOnBattleCompletedEvent()
        {
            // Arrange
            var decision = new AIDecision(AIDecisionType.Attack, "zone_downtown", 0.8f, 20);
            SetupFactionData("trevor", "michael", "zone_downtown");
            SetupDefenderAllocation("michael", "zone_downtown", 10, 5, 2);
            SetupBattleSimulationResult(true);

            BattleSimulationResult? eventResult = null;
            _simulator.OnBattleCompleted += (sender, result) => eventResult = result;

            // Act
            _simulator.ProcessAttackDecision("trevor", decision);

            // Assert
            Assert.NotNull(eventResult);
            Assert.Equal("trevor", eventResult!.AttackerFactionId);
            Assert.Equal("michael", eventResult.DefenderFactionId);
            Assert.Equal("zone_downtown", eventResult.ZoneId);
        }

        #endregion

        #region Integration with AIManager Tests

        [Fact]
        public void HandleAIDecision_WhenConnectedToAIManager_ProcessesAttackDecisions()
        {
            // Arrange
            SetupFactionData("trevor", "michael", "zone_downtown");
            SetupDefenderAllocation("michael", "zone_downtown", 10, 5, 2);
            SetupBattleSimulationResult(true);

            var decision = new AIDecision(AIDecisionType.Attack, "zone_downtown", 0.8f, 20);
            var eventArgs = new AIDecisionEventArgs("trevor", decision);

            // Act - simulate AIManager raising the event
            _simulator.HandleAIDecision(this, eventArgs);

            // Assert
            _battleSimulationServiceMock.Verify(
                s => s.SimulateBattle(
                    "trevor",
                    "michael",
                    "zone_downtown",
                    It.IsAny<TroopComposition>(),
                    It.IsAny<TroopComposition>()),
                Times.Once);
        }

        [Fact]
        public void HandleAIDecision_WithNonAttackDecision_DoesNotProcessBattle()
        {
            // Arrange
            var decision = new AIDecision(AIDecisionType.Defend, "zone_downtown", 0.8f, 20);
            var eventArgs = new AIDecisionEventArgs("trevor", decision);

            // Act
            _simulator.HandleAIDecision(this, eventArgs);

            // Assert
            _battleSimulationServiceMock.Verify(
                s => s.SimulateBattle(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<TroopComposition>(),
                    It.IsAny<TroopComposition>()),
                Times.Never);
        }

        #endregion

        #region AttackerTroopComposition Tests

        [Fact]
        public void ProcessAttackDecision_ConvertsDecisionTroopsToComposition()
        {
            // Arrange - decision has 20 troops to commit
            var decision = new AIDecision(AIDecisionType.Attack, "zone_downtown", 0.8f, 20);
            SetupFactionData("trevor", "michael", "zone_downtown");
            SetupDefenderAllocation("michael", "zone_downtown", 10, 5, 2);
            SetupBattleSimulationResult(true);

            TroopComposition? capturedAttackerTroops = null;
            _battleSimulationServiceMock.Setup(s => s.SimulateBattle(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<TroopComposition>(),
                    It.IsAny<TroopComposition>()))
                .Callback<string, string, string, TroopComposition, TroopComposition>(
                    (a, d, z, attacker, defender) => capturedAttackerTroops = attacker)
                .Returns(BattleSimulationResult.AttackerVictory(
                    "trevor", "michael", "zone_downtown",
                    TroopComposition.Empty, TroopComposition.Empty));

            // Act
            _simulator.ProcessAttackDecision("trevor", decision);

            // Assert - 20 troops committed, default distribution is all basic
            Assert.NotNull(capturedAttackerTroops);
            Assert.Equal(20, capturedAttackerTroops!.TotalCount);
        }

        #endregion

        #region DefenderTroopComposition Tests

        [Fact]
        public void ProcessAttackDecision_UsesDefenderAllocationAsDefenderTroops()
        {
            // Arrange
            var decision = new AIDecision(AIDecisionType.Attack, "zone_downtown", 0.8f, 20);
            SetupFactionData("trevor", "michael", "zone_downtown");
            SetupDefenderAllocation("michael", "zone_downtown", 10, 5, 2); // 10 basic, 5 medium, 2 heavy
            SetupBattleSimulationResult(true);

            TroopComposition? capturedDefenderTroops = null;
            _battleSimulationServiceMock.Setup(s => s.SimulateBattle(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<TroopComposition>(),
                    It.IsAny<TroopComposition>()))
                .Callback<string, string, string, TroopComposition, TroopComposition>(
                    (a, d, z, attacker, defender) => capturedDefenderTroops = defender)
                .Returns(BattleSimulationResult.AttackerVictory(
                    "trevor", "michael", "zone_downtown",
                    TroopComposition.Empty, TroopComposition.Empty));

            // Act
            _simulator.ProcessAttackDecision("trevor", decision);

            // Assert
            Assert.NotNull(capturedDefenderTroops);
            Assert.Equal(10, capturedDefenderTroops!.Basic);
            Assert.Equal(5, capturedDefenderTroops.Medium);
            Assert.Equal(2, capturedDefenderTroops.Heavy);
        }

        [Fact]
        public void ProcessAttackDecision_NoDefenderAllocation_UsesEmptyDefenderTroops()
        {
            // Arrange
            var decision = new AIDecision(AIDecisionType.Attack, "zone_downtown", 0.8f, 20);
            SetupFactionData("trevor", "michael", "zone_downtown");
            // No defender allocation setup - returns null
            _allocationServiceMock.Setup(a => a.GetAllocation("michael", "zone_downtown"))
                .Returns((ZoneDefenderAllocation?)null);
            SetupBattleSimulationResult(true);

            TroopComposition? capturedDefenderTroops = null;
            _battleSimulationServiceMock.Setup(s => s.SimulateBattle(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<TroopComposition>(),
                    It.IsAny<TroopComposition>()))
                .Callback<string, string, string, TroopComposition, TroopComposition>(
                    (a, d, z, attacker, defender) => capturedDefenderTroops = defender)
                .Returns(BattleSimulationResult.AttackerVictory(
                    "trevor", "michael", "zone_downtown",
                    TroopComposition.Empty, TroopComposition.Empty));

            // Act
            _simulator.ProcessAttackDecision("trevor", decision);

            // Assert - empty defenders means easy capture
            Assert.NotNull(capturedDefenderTroops);
            Assert.True(capturedDefenderTroops!.IsEmpty);
        }

        #endregion

        #region SetPlayerZone Tests

        [Fact]
        public void SetPlayerZone_UpdatesCurrentPlayerZone()
        {
            // Arrange & Act
            _simulator.SetPlayerZone("zone_player_location");

            // Assert
            Assert.Equal("zone_player_location", _simulator.CurrentPlayerZone);
        }

        [Fact]
        public void ProcessAttackDecision_PlayerInTargetZone_ReturnsNull()
        {
            // Arrange - player is in the zone being attacked
            var decision = new AIDecision(AIDecisionType.Attack, "zone_downtown", 0.8f, 20);
            _simulator.SetPlayerZone("zone_downtown");

            // Act
            var result = _simulator.ProcessAttackDecision("trevor", decision);

            // Assert - should not process because player is present
            Assert.Null(result);
            _battleSimulationServiceMock.Verify(
                s => s.SimulateBattle(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<TroopComposition>(),
                    It.IsAny<TroopComposition>()),
                Times.Never);
        }

        [Fact]
        public void ProcessAttackDecision_PlayerNotInTargetZone_ProcessesBattle()
        {
            // Arrange - player is in different zone
            var decision = new AIDecision(AIDecisionType.Attack, "zone_downtown", 0.8f, 20);
            _simulator.SetPlayerZone("zone_airport"); // Different zone
            SetupFactionData("trevor", "michael", "zone_downtown");
            SetupDefenderAllocation("michael", "zone_downtown", 10, 5, 2);
            SetupBattleSimulationResult(true);

            // Act
            var result = _simulator.ProcessAttackDecision("trevor", decision);

            // Assert
            Assert.NotNull(result);
        }

        #endregion

        #region Helper Methods

        private void SetupFactionData(string attackerFactionId, string defenderFactionId, string zoneId)
        {
            var attackerFaction = new Faction(attackerFactionId, $"{attackerFactionId} Crew", "Leader");
            var defenderFaction = new Faction(defenderFactionId, $"{defenderFactionId} Crew", "Leader");

            var attackerState = new FactionState(attackerFactionId, 10000, 100);
            var defenderState = new FactionState(defenderFactionId, 10000, 100);

            var zone = new Zone(zoneId, "Test Zone", new Vector3(0, 0, 0), 100f, 10);
            zone.OwnerFactionId = defenderFactionId;

            _factionServiceMock.Setup(f => f.GetFaction(attackerFactionId)).Returns(attackerFaction);
            _factionServiceMock.Setup(f => f.GetFaction(defenderFactionId)).Returns(defenderFaction);
            _factionServiceMock.Setup(f => f.GetFactionState(attackerFactionId)).Returns(attackerState);
            _factionServiceMock.Setup(f => f.GetFactionState(defenderFactionId)).Returns(defenderState);
            _zoneServiceMock.Setup(z => z.GetZone(zoneId)).Returns(zone);
        }

        private void SetupDefenderAllocation(string factionId, string zoneId, int basic, int medium, int heavy)
        {
            var allocation = new ZoneDefenderAllocation(factionId, zoneId);
            allocation.AddTroops(DefenderTier.Basic, basic);
            allocation.AddTroops(DefenderTier.Medium, medium);
            allocation.AddTroops(DefenderTier.Heavy, heavy);

            _allocationServiceMock.Setup(a => a.GetAllocation(factionId, zoneId))
                .Returns(allocation);
        }

        private void SetupBattleSimulationResult(bool attackerWon)
        {
            var attackerCasualties = new TroopComposition(2, 1, 0);
            var defenderCasualties = new TroopComposition(5, 3, 1);

            var result = attackerWon
                ? BattleSimulationResult.AttackerVictory(
                    "trevor", "michael", "zone_downtown",
                    attackerCasualties, defenderCasualties)
                : BattleSimulationResult.DefenderVictory(
                    "trevor", "michael", "zone_downtown",
                    attackerCasualties, defenderCasualties);

            _battleSimulationServiceMock.Setup(s => s.SimulateBattle(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<TroopComposition>(),
                    It.IsAny<TroopComposition>()))
                .Returns(result);
        }

        #endregion
    }
}
