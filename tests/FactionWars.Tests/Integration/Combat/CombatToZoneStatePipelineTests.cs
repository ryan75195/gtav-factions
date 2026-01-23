using System.Linq;
using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Models;
using FactionWars.Combat.Services;
using FactionWars.Core.Interfaces;
using FactionWars.Territory.Interfaces;
using FactionWars.Territory.Models;
using FactionWars.Territory.Repositories;
using FactionWars.Territory.Services;
using Xunit;

namespace FactionWars.Tests.Integration.Combat
{
    /// <summary>
    /// Integration tests for the full combat-to-zone-state pipeline.
    /// Tests the interaction between CombatEncounter, CombatResultHandler, ZoneService, and ZoneRepository.
    /// Uses real implementations (not mocks) to verify end-to-end behavior.
    /// </summary>
    public class CombatToZoneStatePipelineTests
    {
        private readonly InMemoryZoneRepository _zoneRepository;
        private readonly IZoneService _zoneService;
        private readonly ICombatResultHandler _combatResultHandler;

        private const string MichaelFactionId = "faction-michael";
        private const string TrevorFactionId = "faction-trevor";
        private const string FranklinFactionId = "faction-franklin";

        public CombatToZoneStatePipelineTests()
        {
            _zoneRepository = new InMemoryZoneRepository();
            _zoneService = new ZoneService(_zoneRepository);
            _combatResultHandler = new CombatResultHandler(_zoneService);
        }

        #region Attacker Victory Pipeline Tests

        [Fact]
        public void AttackerVictory_NeutralizesZone_WhenDefenderLosesAllControl()
        {
            // Arrange: Set up a zone owned by Trevor
            var zone = CreateAndAddZone("zone-vinewood", "Vinewood", TrevorFactionId, 100f);

            // Create combat encounter where Michael attacks Trevor's zone
            var encounter = new CombatEncounter(
                "combat-1",
                zone.Id,
                MichaelFactionId,
                TrevorFactionId);

            // Simulate combat: Michael wins
            encounter.AttackerControlPercentage = 100f;
            encounter.DefenderControlPercentage = 0f;
            encounter.End(CombatStatus.AttackerVictory);

            // Act: Process the combat result
            var result = _combatResultHandler.ProcessCombatResult(encounter);

            // Assert: Zone is now neutral (attacker must claim it separately)
            Assert.True(result.IsSuccess);
            Assert.Equal(CombatResultOutcome.ZoneNeutralized, result.Outcome);
            Assert.Null(result.NewOwnerFactionId); // No owner until claimed
            Assert.Equal(TrevorFactionId, result.PreviousOwnerFactionId);

            // Verify zone state was actually updated in repository
            var updatedZone = _zoneRepository.GetById(zone.Id);
            Assert.NotNull(updatedZone);
            Assert.Null(updatedZone!.OwnerFactionId); // Neutral
            Assert.Equal(0f, updatedZone.ControlPercentage);
            Assert.False(updatedZone.IsContested);
        }

        [Fact]
        public void AttackerVictory_NeutralizesZone_ReducesDefenderCount()
        {
            // Arrange: Create multiple zones with different owners
            CreateAndAddZone("zone-1", "Downtown", TrevorFactionId, 100f);
            CreateAndAddZone("zone-2", "Airport", TrevorFactionId, 100f);
            CreateAndAddZone("zone-3", "Beach", MichaelFactionId, 100f);

            // Initial counts
            Assert.Equal(2, _zoneService.GetZoneCount(TrevorFactionId));
            Assert.Equal(1, _zoneService.GetZoneCount(MichaelFactionId));

            // Create and end combat with Michael victory
            var encounter = new CombatEncounter(
                "combat-1",
                "zone-1",
                MichaelFactionId,
                TrevorFactionId);
            encounter.End(CombatStatus.AttackerVictory);

            // Act
            _combatResultHandler.ProcessCombatResult(encounter);

            // Assert: Zone becomes neutral (defender loses zone, attacker doesn't gain until claim)
            Assert.Equal(1, _zoneService.GetZoneCount(TrevorFactionId));
            Assert.Equal(1, _zoneService.GetZoneCount(MichaelFactionId)); // No change until claimed
        }

        [Fact]
        public void AttackerVictory_ClearsContestedState_AfterOwnershipTransfer()
        {
            // Arrange: Zone is contested during combat
            var zone = CreateAndAddZone("zone-vinewood", "Vinewood", TrevorFactionId, 50f);
            zone.IsContested = true;
            _zoneRepository.Update(zone);

            Assert.True(_zoneRepository.GetById(zone.Id)!.IsContested);

            var encounter = new CombatEncounter(
                "combat-1",
                zone.Id,
                MichaelFactionId,
                TrevorFactionId);
            encounter.End(CombatStatus.AttackerVictory);

            // Act
            _combatResultHandler.ProcessCombatResult(encounter);

            // Assert: Contested state is cleared
            var updatedZone = _zoneRepository.GetById(zone.Id);
            Assert.False(updatedZone!.IsContested);
        }

        [Fact]
        public void AttackerVictory_SetsControlTo0Percent_ForNeutralZone()
        {
            // Arrange: Zone at partial control
            var zone = CreateAndAddZone("zone-vinewood", "Vinewood", TrevorFactionId, 30f);

            var encounter = new CombatEncounter(
                "combat-1",
                zone.Id,
                MichaelFactionId,
                TrevorFactionId);
            encounter.End(CombatStatus.AttackerVictory);

            // Act
            _combatResultHandler.ProcessCombatResult(encounter);

            // Assert: Control is now 0% (neutral zone, awaiting claim)
            var updatedZone = _zoneRepository.GetById(zone.Id);
            Assert.Equal(0f, updatedZone!.ControlPercentage);
        }

        #endregion

        #region Defender Victory Pipeline Tests

        [Fact]
        public void DefenderVictory_MaintainsZoneOwnership_WhenAttackerRepelled()
        {
            // Arrange
            var zone = CreateAndAddZone("zone-vinewood", "Vinewood", TrevorFactionId, 100f);

            var encounter = new CombatEncounter(
                "combat-1",
                zone.Id,
                MichaelFactionId,
                TrevorFactionId);

            // Simulate combat: Trevor defends successfully
            encounter.AttackerControlPercentage = 20f;
            encounter.DefenderControlPercentage = 80f;
            encounter.End(CombatStatus.DefenderVictory);

            // Act
            var result = _combatResultHandler.ProcessCombatResult(encounter);

            // Assert: Zone still owned by Trevor
            Assert.True(result.IsSuccess);
            Assert.Equal(CombatResultOutcome.ZoneDefended, result.Outcome);
            Assert.Equal(TrevorFactionId, result.NewOwnerFactionId);

            var updatedZone = _zoneRepository.GetById(zone.Id);
            Assert.Equal(TrevorFactionId, updatedZone!.OwnerFactionId);
        }

        [Fact]
        public void DefenderVictory_RestoresControlTo100Percent_AfterSuccessfulDefense()
        {
            // Arrange: Zone was losing control during battle
            var zone = CreateAndAddZone("zone-vinewood", "Vinewood", TrevorFactionId, 45f);
            zone.IsContested = true;
            _zoneRepository.Update(zone);

            var encounter = new CombatEncounter(
                "combat-1",
                zone.Id,
                MichaelFactionId,
                TrevorFactionId);
            encounter.End(CombatStatus.DefenderVictory);

            // Act
            _combatResultHandler.ProcessCombatResult(encounter);

            // Assert: Control restored to 100%
            var updatedZone = _zoneRepository.GetById(zone.Id);
            Assert.Equal(100f, updatedZone!.ControlPercentage);
            Assert.False(updatedZone.IsContested);
        }

        [Fact]
        public void DefenderVictory_DoesNotAffectOtherZones_OnlyTargetZone()
        {
            // Arrange: Multiple zones, combat in one
            var targetZone = CreateAndAddZone("zone-1", "Downtown", TrevorFactionId, 100f);
            var otherZone = CreateAndAddZone("zone-2", "Airport", TrevorFactionId, 100f);
            var thirdZone = CreateAndAddZone("zone-3", "Beach", MichaelFactionId, 100f);

            var encounter = new CombatEncounter(
                "combat-1",
                targetZone.Id,
                MichaelFactionId,
                TrevorFactionId);
            encounter.End(CombatStatus.DefenderVictory);

            // Act
            _combatResultHandler.ProcessCombatResult(encounter);

            // Assert: Other zones unchanged
            Assert.Equal(TrevorFactionId, _zoneRepository.GetById(otherZone.Id)!.OwnerFactionId);
            Assert.Equal(MichaelFactionId, _zoneRepository.GetById(thirdZone.Id)!.OwnerFactionId);
        }

        #endregion

        #region Stalemate Pipeline Tests

        [Fact]
        public void Stalemate_MaintainsDefenderOwnership_WhenNoClearVictor()
        {
            // Arrange
            var zone = CreateAndAddZone("zone-vinewood", "Vinewood", TrevorFactionId, 100f);
            zone.IsContested = true;
            _zoneRepository.Update(zone);

            var encounter = new CombatEncounter(
                "combat-1",
                zone.Id,
                MichaelFactionId,
                TrevorFactionId);

            // Stalemate - roughly equal control
            encounter.AttackerControlPercentage = 48f;
            encounter.DefenderControlPercentage = 52f;
            encounter.End(CombatStatus.Stalemate);

            // Act
            var result = _combatResultHandler.ProcessCombatResult(encounter);

            // Assert: Defender keeps zone
            Assert.True(result.IsSuccess);
            Assert.Equal(CombatResultOutcome.Stalemate, result.Outcome);
            Assert.Equal(TrevorFactionId, _zoneRepository.GetById(zone.Id)!.OwnerFactionId);
        }

        [Fact]
        public void Stalemate_PreservesDefenderControlPercentage_FromEncounter()
        {
            // Arrange
            var zone = CreateAndAddZone("zone-vinewood", "Vinewood", TrevorFactionId, 100f);

            var encounter = new CombatEncounter(
                "combat-1",
                zone.Id,
                MichaelFactionId,
                TrevorFactionId);

            encounter.DefenderControlPercentage = 55f;
            encounter.End(CombatStatus.Stalemate);

            // Act
            _combatResultHandler.ProcessCombatResult(encounter);

            // Assert: Control percentage matches encounter's defender percentage
            var updatedZone = _zoneRepository.GetById(zone.Id);
            Assert.Equal(55f, updatedZone!.ControlPercentage);
            Assert.False(updatedZone.IsContested);
        }

        #endregion

        #region Aborted Combat Pipeline Tests

        [Fact]
        public void Aborted_MaintainsDefenderOwnership_WhenCombatAborted()
        {
            // Arrange
            var zone = CreateAndAddZone("zone-vinewood", "Vinewood", TrevorFactionId, 100f);

            var encounter = new CombatEncounter(
                "combat-1",
                zone.Id,
                MichaelFactionId,
                TrevorFactionId);

            encounter.DefenderControlPercentage = 70f;
            encounter.End(CombatStatus.Aborted);

            // Act
            var result = _combatResultHandler.ProcessCombatResult(encounter);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(CombatResultOutcome.Aborted, result.Outcome);
            Assert.Equal(TrevorFactionId, _zoneRepository.GetById(zone.Id)!.OwnerFactionId);
        }

        [Fact]
        public void Aborted_PreservesDefenderControlPercentage_WhenPlayerLeavesArea()
        {
            // Arrange: Combat was at 60% defender control when player left
            var zone = CreateAndAddZone("zone-vinewood", "Vinewood", TrevorFactionId, 100f);
            zone.IsContested = true;
            _zoneRepository.Update(zone);

            var encounter = new CombatEncounter(
                "combat-1",
                zone.Id,
                MichaelFactionId,
                TrevorFactionId);

            encounter.DefenderControlPercentage = 60f;
            encounter.End(CombatStatus.Aborted);

            // Act
            _combatResultHandler.ProcessCombatResult(encounter);

            // Assert: Control preserved, contested cleared
            var updatedZone = _zoneRepository.GetById(zone.Id);
            Assert.Equal(60f, updatedZone!.ControlPercentage);
            Assert.False(updatedZone.IsContested);
        }

        #endregion

        #region Multi-Zone Combat Scenarios

        [Fact]
        public void SequentialCombats_NeutralizesZones_WhenAttackerWins()
        {
            // Arrange: Trevor owns 3 zones, Michael attacks them sequentially
            var zone1 = CreateAndAddZone("zone-1", "Downtown", TrevorFactionId, 100f);
            var zone2 = CreateAndAddZone("zone-2", "Midtown", TrevorFactionId, 100f);
            var zone3 = CreateAndAddZone("zone-3", "Uptown", TrevorFactionId, 100f);

            // Initial state: Trevor has all 3
            Assert.Equal(3, _zoneService.GetZoneCount(TrevorFactionId));
            Assert.Equal(0, _zoneService.GetZoneCount(MichaelFactionId));

            // First combat: Michael wins zone-1 (zone becomes neutral)
            var combat1 = new CombatEncounter("combat-1", zone1.Id, MichaelFactionId, TrevorFactionId);
            combat1.End(CombatStatus.AttackerVictory);
            _combatResultHandler.ProcessCombatResult(combat1);

            Assert.Equal(2, _zoneService.GetZoneCount(TrevorFactionId));
            Assert.Equal(0, _zoneService.GetZoneCount(MichaelFactionId)); // Zone is neutral, not captured

            // Second combat: Trevor defends zone-2
            var combat2 = new CombatEncounter("combat-2", zone2.Id, MichaelFactionId, TrevorFactionId);
            combat2.End(CombatStatus.DefenderVictory);
            _combatResultHandler.ProcessCombatResult(combat2);

            Assert.Equal(2, _zoneService.GetZoneCount(TrevorFactionId));
            Assert.Equal(0, _zoneService.GetZoneCount(MichaelFactionId));

            // Third combat: Michael wins zone-3 (zone becomes neutral)
            var combat3 = new CombatEncounter("combat-3", zone3.Id, MichaelFactionId, TrevorFactionId);
            combat3.End(CombatStatus.AttackerVictory);
            _combatResultHandler.ProcessCombatResult(combat3);

            // Assert: Final state - zones are neutralized, not captured
            Assert.Equal(1, _zoneService.GetZoneCount(TrevorFactionId));
            Assert.Equal(0, _zoneService.GetZoneCount(MichaelFactionId)); // Zones are neutral
            Assert.Null(_zoneRepository.GetById(zone1.Id)!.OwnerFactionId); // Neutral
            Assert.Equal(TrevorFactionId, _zoneRepository.GetById(zone2.Id)!.OwnerFactionId);
            Assert.Null(_zoneRepository.GetById(zone3.Id)!.OwnerFactionId); // Neutral
        }

        [Fact]
        public void ThreeFactionWar_NeutralizesZones_WhenAttackerWins()
        {
            // Arrange: Each faction starts with one zone
            var michaelZone = CreateAndAddZone("zone-michael", "Rockford Hills", MichaelFactionId, 100f);
            var trevorZone = CreateAndAddZone("zone-trevor", "Sandy Shores", TrevorFactionId, 100f);
            var franklinZone = CreateAndAddZone("zone-franklin", "Strawberry", FranklinFactionId, 100f);

            // Franklin attacks Trevor's zone and wins (zone becomes neutral)
            var combat1 = new CombatEncounter("combat-1", trevorZone.Id, FranklinFactionId, TrevorFactionId);
            combat1.End(CombatStatus.AttackerVictory);
            _combatResultHandler.ProcessCombatResult(combat1);

            Assert.Equal(0, _zoneService.GetZoneCount(TrevorFactionId));
            Assert.Equal(1, _zoneService.GetZoneCount(FranklinFactionId)); // Still only original zone

            // Michael attacks Franklin's original zone and wins (zone becomes neutral)
            var combat2 = new CombatEncounter("combat-2", franklinZone.Id, MichaelFactionId, FranklinFactionId);
            combat2.End(CombatStatus.AttackerVictory);
            _combatResultHandler.ProcessCombatResult(combat2);

            // Assert: Final state - conquered zones are neutral
            Assert.Equal(1, _zoneService.GetZoneCount(MichaelFactionId)); // Only original zone
            Assert.Equal(0, _zoneService.GetZoneCount(FranklinFactionId)); // Lost original zone
            Assert.Equal(0, _zoneService.GetZoneCount(TrevorFactionId)); // Lost all

            Assert.Equal(MichaelFactionId, _zoneRepository.GetById(michaelZone.Id)!.OwnerFactionId);
            Assert.Null(_zoneRepository.GetById(trevorZone.Id)!.OwnerFactionId); // Neutral
            Assert.Null(_zoneRepository.GetById(franklinZone.Id)!.OwnerFactionId); // Neutral
        }

        #endregion

        #region Neutral Zone Combat Tests

        [Fact]
        public void AttackerVictory_LeavesNeutralZoneNeutral_WhenAlreadyNeutral()
        {
            // Arrange: Neutral zone (no owner)
            var zone = CreateAndAddZone("zone-neutral", "Neutral Territory", null, 0f);

            var encounter = new CombatEncounter(
                "combat-1",
                zone.Id,
                MichaelFactionId,
                "neutral"); // Defending "neutral" for mechanical purposes

            encounter.End(CombatStatus.AttackerVictory);

            // Note: This test assumes the handler can work with any defender ID
            // Victory neutralizes the zone - but it was already neutral
            var result = _combatResultHandler.ProcessCombatResult(encounter);

            // Assert: Zone remains neutral (attacker must claim it separately)
            Assert.True(result.IsSuccess);
            var updatedZone = _zoneRepository.GetById(zone.Id);
            Assert.Null(updatedZone!.OwnerFactionId); // Still neutral
            Assert.Equal(0f, updatedZone.ControlPercentage);
        }

        #endregion

        #region Territory Value Integration Tests

        [Fact]
        public void AttackerVictory_NeutralizesZone_DefenderLosesTerritoryValue()
        {
            // Arrange: High-value zone owned by Trevor
            var highValueZone = new Zone("zone-hv", "Diamond Casino", new Vector3(0, 0, 0), 200f, 10);
            highValueZone.OwnerFactionId = TrevorFactionId;
            highValueZone.ControlPercentage = 100f;
            _zoneRepository.Add(highValueZone);

            var lowValueZone = new Zone("zone-lv", "Alley", new Vector3(500, 0, 0), 100f, 1);
            lowValueZone.OwnerFactionId = MichaelFactionId;
            lowValueZone.ControlPercentage = 100f;
            _zoneRepository.Add(lowValueZone);

            // Initial territory values
            Assert.Equal(10, _zoneService.GetFactionTerritoryValue(TrevorFactionId));
            Assert.Equal(1, _zoneService.GetFactionTerritoryValue(MichaelFactionId));

            // Michael wins combat (zone becomes neutral, not captured)
            var encounter = new CombatEncounter("combat-1", highValueZone.Id, MichaelFactionId, TrevorFactionId);
            encounter.End(CombatStatus.AttackerVictory);
            _combatResultHandler.ProcessCombatResult(encounter);

            // Assert: Defender loses territory value, attacker doesn't gain until claim
            Assert.Equal(0, _zoneService.GetFactionTerritoryValue(TrevorFactionId));
            Assert.Equal(1, _zoneService.GetFactionTerritoryValue(MichaelFactionId)); // No change until claimed
        }

        #endregion

        #region Contested Zone State Tracking Tests

        [Fact]
        public void ContestedZones_ReflectOngoingCombat_UntilResolution()
        {
            // Arrange
            var zone = CreateAndAddZone("zone-1", "Downtown", TrevorFactionId, 100f);

            // No contested zones initially
            Assert.Empty(_zoneService.GetContestedZones());

            // Simulate combat starting - zone becomes contested
            zone.IsContested = true;
            _zoneRepository.Update(zone);

            Assert.Single(_zoneService.GetContestedZones());

            // Combat ends with defender victory
            var encounter = new CombatEncounter("combat-1", zone.Id, MichaelFactionId, TrevorFactionId);
            encounter.End(CombatStatus.DefenderVictory);
            _combatResultHandler.ProcessCombatResult(encounter);

            // Assert: No longer contested
            Assert.Empty(_zoneService.GetContestedZones());
        }

        [Fact]
        public void MultipleContestedZones_AreIndependentlyResolved()
        {
            // Arrange: Two zones under attack
            var zone1 = CreateAndAddZone("zone-1", "Downtown", TrevorFactionId, 100f);
            var zone2 = CreateAndAddZone("zone-2", "Midtown", TrevorFactionId, 100f);

            zone1.IsContested = true;
            zone2.IsContested = true;
            _zoneRepository.Update(zone1);
            _zoneRepository.Update(zone2);

            Assert.Equal(2, _zoneService.GetContestedZones().Count());

            // Resolve first combat
            var combat1 = new CombatEncounter("combat-1", zone1.Id, MichaelFactionId, TrevorFactionId);
            combat1.End(CombatStatus.AttackerVictory);
            _combatResultHandler.ProcessCombatResult(combat1);

            // Assert: Only zone2 still contested
            Assert.Single(_zoneService.GetContestedZones());
            Assert.False(_zoneRepository.GetById(zone1.Id)!.IsContested);
            Assert.True(_zoneRepository.GetById(zone2.Id)!.IsContested);

            // Resolve second combat
            var combat2 = new CombatEncounter("combat-2", zone2.Id, MichaelFactionId, TrevorFactionId);
            combat2.End(CombatStatus.DefenderVictory);
            _combatResultHandler.ProcessCombatResult(combat2);

            // Assert: No more contested zones
            Assert.Empty(_zoneService.GetContestedZones());
        }

        #endregion

        #region Error Handling Integration Tests

        [Fact]
        public void ProcessCombatResult_ReturnsFailure_WhenZoneDoesNotExist()
        {
            // Arrange: Create encounter for non-existent zone
            var encounter = new CombatEncounter(
                "combat-1",
                "zone-does-not-exist",
                MichaelFactionId,
                TrevorFactionId);
            encounter.End(CombatStatus.AttackerVictory);

            // Act
            var result = _combatResultHandler.ProcessCombatResult(encounter);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(CombatResultOutcome.ZoneNotFound, result.Outcome);
        }

        [Fact]
        public void ProcessCombatResult_DoesNotModifyZones_WhenZoneNotFound()
        {
            // Arrange: Create a valid zone
            var validZone = CreateAndAddZone("zone-valid", "Valid Zone", TrevorFactionId, 100f);

            // Create encounter for non-existent zone
            var encounter = new CombatEncounter(
                "combat-1",
                "zone-invalid",
                MichaelFactionId,
                TrevorFactionId);
            encounter.End(CombatStatus.AttackerVictory);

            // Act
            _combatResultHandler.ProcessCombatResult(encounter);

            // Assert: Valid zone unchanged
            var zone = _zoneRepository.GetById(validZone.Id);
            Assert.Equal(TrevorFactionId, zone!.OwnerFactionId);
            Assert.Equal(100f, zone.ControlPercentage);
        }

        #endregion

        #region Helper Methods

        private Zone CreateAndAddZone(string id, string name, string? ownerFactionId, float controlPercentage)
        {
            var zone = new Zone(id, name, new Vector3(0, 0, 0), 150f, 5);
            zone.OwnerFactionId = ownerFactionId;
            zone.ControlPercentage = controlPercentage;
            _zoneRepository.Add(zone);
            return zone;
        }

        #endregion
    }
}
