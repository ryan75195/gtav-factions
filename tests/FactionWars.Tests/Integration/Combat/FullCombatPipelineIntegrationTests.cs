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
    /// Full pipeline integration tests that verify the entire combat flow:
    /// PedCounts -> ControlPercentageCalculator -> TakeoverDetector -> CombatResultHandler -> ZoneState
    /// Uses real implementations (not mocks) to verify end-to-end behavior.
    /// </summary>
    public class FullCombatPipelineIntegrationTests
    {
        private readonly InMemoryZoneRepository _zoneRepository;
        private readonly IZoneService _zoneService;
        private readonly IControlPercentageCalculator _controlCalculator;
        private readonly ITakeoverDetector _takeoverDetector;
        private readonly ICombatResultHandler _combatResultHandler;

        private const string MichaelFactionId = "faction-michael";
        private const string TrevorFactionId = "faction-trevor";
        private const string FranklinFactionId = "faction-franklin";

        public FullCombatPipelineIntegrationTests()
        {
            _zoneRepository = new InMemoryZoneRepository();
            _zoneService = new ZoneService(_zoneRepository);
            _controlCalculator = new ControlPercentageCalculator();
            _takeoverDetector = new TakeoverDetector();
            _combatResultHandler = new CombatResultHandler(_zoneService);
        }

        #region Full Pipeline: Attacker Victory

        [Fact]
        public void FullPipeline_AttackerWins_WhenAttackerDominatesWithPedCount()
        {
            // Arrange: Set up zone and combat
            var zone = CreateAndAddZone("zone-1", "Downtown", TrevorFactionId, 100f);
            zone.IsContested = true;
            _zoneRepository.Update(zone);

            var encounter = new CombatEncounter("combat-1", zone.Id, MichaelFactionId, TrevorFactionId);

            // Simulate: 20 attackers, 0 defenders (attacker eliminates all defenders)
            encounter.AttackerPedCount = 20;
            encounter.DefenderPedCount = 0;

            // Step 1: Calculate control percentages
            _controlCalculator.ApplyToEncounter(encounter);

            // Assert intermediate state
            Assert.Equal(100f, encounter.AttackerControlPercentage);
            Assert.Equal(0f, encounter.DefenderControlPercentage);

            // Step 2: Check takeover status
            var takeoverResult = _takeoverDetector.CheckTakeover(encounter);

            Assert.Equal(TakeoverStatus.AttackerVictory, takeoverResult.Status);
            Assert.Equal(MichaelFactionId, takeoverResult.WinnerFactionId);

            // Step 3: End combat and process result
            encounter.End(CombatStatus.AttackerVictory);
            var combatResult = _combatResultHandler.ProcessCombatResult(encounter);

            // Assert final state - zone is neutralized, not captured
            Assert.True(combatResult.IsSuccess);
            Assert.Equal(CombatResultOutcome.ZoneNeutralized, combatResult.Outcome);
            Assert.Null(combatResult.NewOwnerFactionId); // Neutral until claimed

            // Verify zone was actually updated
            var updatedZone = _zoneRepository.GetById(zone.Id);
            Assert.Null(updatedZone!.OwnerFactionId); // Neutral
            Assert.Equal(0f, updatedZone.ControlPercentage);
            Assert.False(updatedZone.IsContested);
        }

        [Fact]
        public void FullPipeline_AttackerWins_WhenReachingVictoryThreshold()
        {
            // Arrange
            var zone = CreateAndAddZone("zone-1", "Downtown", TrevorFactionId, 100f);
            zone.IsContested = true;
            _zoneRepository.Update(zone);

            var encounter = new CombatEncounter("combat-1", zone.Id, MichaelFactionId, TrevorFactionId);

            // Simulate: Full attacker control (default threshold is 100%)
            encounter.AttackerPedCount = 20;
            encounter.DefenderPedCount = 0;

            // Full pipeline
            _controlCalculator.ApplyToEncounter(encounter);
            var takeoverResult = _takeoverDetector.CheckTakeover(encounter);

            Assert.Equal(100f, encounter.AttackerControlPercentage);
            Assert.Equal(TakeoverStatus.AttackerVictory, takeoverResult.Status);

            encounter.End(CombatStatus.AttackerVictory);
            var combatResult = _combatResultHandler.ProcessCombatResult(encounter);

            Assert.True(combatResult.IsSuccess);
            Assert.Equal(CombatResultOutcome.ZoneNeutralized, combatResult.Outcome);
            Assert.Null(_zoneRepository.GetById(zone.Id)!.OwnerFactionId); // Zone is neutral
        }

        #endregion

        #region Full Pipeline: Defender Victory

        [Fact]
        public void FullPipeline_DefenderWins_WhenAttackerIsEliminated()
        {
            // Arrange
            var zone = CreateAndAddZone("zone-1", "Downtown", TrevorFactionId, 100f);
            zone.IsContested = true;
            _zoneRepository.Update(zone);

            var encounter = new CombatEncounter("combat-1", zone.Id, MichaelFactionId, TrevorFactionId);

            // Simulate: 0 attackers, 10 defenders (defender eliminated all attackers)
            encounter.AttackerPedCount = 0;
            encounter.DefenderPedCount = 10;

            // Full pipeline
            _controlCalculator.ApplyToEncounter(encounter);

            Assert.Equal(0f, encounter.AttackerControlPercentage);
            Assert.Equal(100f, encounter.DefenderControlPercentage);

            var takeoverResult = _takeoverDetector.CheckTakeover(encounter);
            Assert.Equal(TakeoverStatus.DefenderVictory, takeoverResult.Status);
            Assert.Equal(TrevorFactionId, takeoverResult.WinnerFactionId);

            encounter.End(CombatStatus.DefenderVictory);
            var combatResult = _combatResultHandler.ProcessCombatResult(encounter);

            Assert.True(combatResult.IsSuccess);
            Assert.Equal(CombatResultOutcome.ZoneDefended, combatResult.Outcome);
            Assert.Equal(TrevorFactionId, _zoneRepository.GetById(zone.Id)!.OwnerFactionId);
            Assert.Equal(100f, _zoneRepository.GetById(zone.Id)!.ControlPercentage);
        }

        [Fact]
        public void FullPipeline_DefenderWins_WhenAttackerBelowThreshold()
        {
            // Arrange
            var zone = CreateAndAddZone("zone-1", "Downtown", TrevorFactionId, 100f);
            zone.IsContested = true;
            _zoneRepository.Update(zone);

            var encounter = new CombatEncounter("combat-1", zone.Id, MichaelFactionId, TrevorFactionId);

            // Simulate: 0 attackers = 0% attacker control (at 0% defender victory threshold)
            encounter.AttackerPedCount = 0;
            encounter.DefenderPedCount = 19;

            // Full pipeline
            _controlCalculator.ApplyToEncounter(encounter);

            Assert.Equal(0f, encounter.AttackerControlPercentage);
            Assert.Equal(100f, encounter.DefenderControlPercentage);

            var takeoverResult = _takeoverDetector.CheckTakeover(encounter);
            Assert.Equal(TakeoverStatus.DefenderVictory, takeoverResult.Status);

            encounter.End(CombatStatus.DefenderVictory);
            var combatResult = _combatResultHandler.ProcessCombatResult(encounter);

            Assert.True(combatResult.IsSuccess);
            Assert.Equal(CombatResultOutcome.ZoneDefended, combatResult.Outcome);
        }

        #endregion

        #region Full Pipeline: Contested State (No Victory)

        [Fact]
        public void FullPipeline_CombatContinues_WhenNoVictoryThresholdReached()
        {
            // Arrange
            var zone = CreateAndAddZone("zone-1", "Downtown", TrevorFactionId, 100f);
            zone.IsContested = true;
            _zoneRepository.Update(zone);

            var encounter = new CombatEncounter("combat-1", zone.Id, MichaelFactionId, TrevorFactionId);

            // Simulate: 5 attackers, 5 defenders = 50% each (in contested range)
            encounter.AttackerPedCount = 5;
            encounter.DefenderPedCount = 5;

            // Full pipeline
            _controlCalculator.ApplyToEncounter(encounter);

            Assert.Equal(50f, encounter.AttackerControlPercentage);
            Assert.Equal(50f, encounter.DefenderControlPercentage);

            var takeoverResult = _takeoverDetector.CheckTakeover(encounter);
            Assert.Equal(TakeoverStatus.InProgress, takeoverResult.Status);
            Assert.Null(takeoverResult.WinnerFactionId);

            // Combat is still in progress - no resolution yet
            Assert.True(encounter.IsActive);
        }

        [Fact]
        public void FullPipeline_Stalemate_WhenCombatEndsWithNoVictor()
        {
            // Arrange
            var zone = CreateAndAddZone("zone-1", "Downtown", TrevorFactionId, 100f);

            var encounter = new CombatEncounter("combat-1", zone.Id, MichaelFactionId, TrevorFactionId);

            // Simulate contested state at combat end
            encounter.AttackerPedCount = 5;
            encounter.DefenderPedCount = 5;
            _controlCalculator.ApplyToEncounter(encounter);

            // Force end as stalemate (e.g., time limit reached)
            encounter.End(CombatStatus.Stalemate);
            var combatResult = _combatResultHandler.ProcessCombatResult(encounter);

            Assert.True(combatResult.IsSuccess);
            Assert.Equal(CombatResultOutcome.Stalemate, combatResult.Outcome);

            // Defender keeps zone with current control
            var updatedZone = _zoneRepository.GetById(zone.Id);
            Assert.Equal(TrevorFactionId, updatedZone!.OwnerFactionId);
            Assert.Equal(50f, updatedZone.ControlPercentage);
        }

        #endregion

        #region Full Pipeline: Dynamic Ped Count Changes

        [Fact]
        public void FullPipeline_ControlShifts_AsPedCountsChange()
        {
            // Arrange
            var zone = CreateAndAddZone("zone-1", "Downtown", TrevorFactionId, 100f);

            var encounter = new CombatEncounter("combat-1", zone.Id, MichaelFactionId, TrevorFactionId);

            // Phase 1: Combat starts - equal forces
            encounter.AttackerPedCount = 10;
            encounter.DefenderPedCount = 10;
            _controlCalculator.ApplyToEncounter(encounter);

            Assert.Equal(50f, encounter.AttackerControlPercentage);
            var result1 = _takeoverDetector.CheckTakeover(encounter);
            Assert.Equal(TakeoverStatus.InProgress, result1.Status);

            // Phase 2: Reinforcements arrive for attacker
            encounter.AttackerPedCount = 15;
            _controlCalculator.ApplyToEncounter(encounter);

            // 15 / 25 = 60%
            Assert.True(encounter.AttackerControlPercentage > 59.9f && encounter.AttackerControlPercentage < 60.1f);
            var result2 = _takeoverDetector.CheckTakeover(encounter);
            Assert.Equal(TakeoverStatus.InProgress, result2.Status);

            // Phase 3: Defender takes losses
            encounter.DefenderPedCount = 5;
            _controlCalculator.ApplyToEncounter(encounter);

            Assert.Equal(75f, encounter.AttackerControlPercentage);
            var result3 = _takeoverDetector.CheckTakeover(encounter);
            Assert.Equal(TakeoverStatus.InProgress, result3.Status);

            // Phase 4: Attacker pushes toward victory
            encounter.DefenderPedCount = 2;
            _controlCalculator.ApplyToEncounter(encounter);

            // 15 / (15 + 2) = ~88.2% - still in progress (threshold is 100%)
            Assert.True(encounter.AttackerControlPercentage > 85f);
            Assert.True(encounter.AttackerControlPercentage < 90f);

            var result4 = _takeoverDetector.CheckTakeover(encounter);
            Assert.Equal(TakeoverStatus.InProgress, result4.Status);

            // Final push - eliminate all defenders
            encounter.DefenderPedCount = 0;
            _controlCalculator.ApplyToEncounter(encounter);

            // 100% attacker control - attacker victory!
            var finalResult = _takeoverDetector.CheckTakeover(encounter);
            Assert.Equal(TakeoverStatus.AttackerVictory, finalResult.Status);

            // End and process
            encounter.End(CombatStatus.AttackerVictory);
            var combatResult = _combatResultHandler.ProcessCombatResult(encounter);

            Assert.True(combatResult.IsSuccess);
            Assert.Null(_zoneRepository.GetById(zone.Id)!.OwnerFactionId); // Zone is neutral
        }

        [Fact]
        public void FullPipeline_DefenderRepelsAttack_WithReinforcements()
        {
            // Arrange
            var zone = CreateAndAddZone("zone-1", "Downtown", TrevorFactionId, 100f);

            var encounter = new CombatEncounter("combat-1", zone.Id, MichaelFactionId, TrevorFactionId);

            // Phase 1: Attacker starts strong
            encounter.AttackerPedCount = 15;
            encounter.DefenderPedCount = 5;
            _controlCalculator.ApplyToEncounter(encounter);

            Assert.Equal(75f, encounter.AttackerControlPercentage);
            var result1 = _takeoverDetector.CheckTakeover(encounter);
            Assert.Equal(TakeoverStatus.InProgress, result1.Status);

            // Phase 2: Defender reinforcements arrive
            encounter.DefenderPedCount = 15;
            _controlCalculator.ApplyToEncounter(encounter);

            Assert.Equal(50f, encounter.AttackerControlPercentage);

            // Phase 3: Defenders push back, attackers take heavy losses
            encounter.AttackerPedCount = 3;
            encounter.DefenderPedCount = 20;
            _controlCalculator.ApplyToEncounter(encounter);

            // 3 / 23 ≈ 13% - still in progress (threshold is 0%)
            Assert.True(encounter.AttackerControlPercentage < 15f);
            var result2 = _takeoverDetector.CheckTakeover(encounter);
            Assert.Equal(TakeoverStatus.InProgress, result2.Status);

            // Phase 4: Attackers completely eliminated
            encounter.AttackerPedCount = 0;
            _controlCalculator.ApplyToEncounter(encounter);

            // 0% attacker control - defender victory!
            var finalResult = _takeoverDetector.CheckTakeover(encounter);
            Assert.Equal(TakeoverStatus.DefenderVictory, finalResult.Status);

            // End and process
            encounter.End(CombatStatus.DefenderVictory);
            var combatResult = _combatResultHandler.ProcessCombatResult(encounter);

            Assert.True(combatResult.IsSuccess);
            Assert.Equal(CombatResultOutcome.ZoneDefended, combatResult.Outcome);
            Assert.Equal(TrevorFactionId, _zoneRepository.GetById(zone.Id)!.OwnerFactionId);
        }

        #endregion

        #region Full Pipeline: Multi-Zone Warfare

        [Fact]
        public void FullPipeline_SimultaneousBattles_IndependentlyResolved()
        {
            // Arrange: Three zones, two battles
            var zone1 = CreateAndAddZone("zone-1", "Downtown", TrevorFactionId, 100f);
            var zone2 = CreateAndAddZone("zone-2", "Midtown", TrevorFactionId, 100f);
            var zone3 = CreateAndAddZone("zone-3", "Uptown", MichaelFactionId, 100f);

            // Battle 1: Michael attacks zone-1 and wins (100% control)
            var encounter1 = new CombatEncounter("combat-1", zone1.Id, MichaelFactionId, TrevorFactionId);
            encounter1.AttackerPedCount = 20;
            encounter1.DefenderPedCount = 0; // All defenders eliminated
            _controlCalculator.ApplyToEncounter(encounter1);

            // Battle 2: Franklin attacks zone-2 but fails (0% control)
            var encounter2 = new CombatEncounter("combat-2", zone2.Id, FranklinFactionId, TrevorFactionId);
            encounter2.AttackerPedCount = 0; // All attackers eliminated
            encounter2.DefenderPedCount = 15;
            _controlCalculator.ApplyToEncounter(encounter2);

            // Check takeover status for both
            var result1 = _takeoverDetector.CheckTakeover(encounter1);
            var result2 = _takeoverDetector.CheckTakeover(encounter2);

            Assert.Equal(TakeoverStatus.AttackerVictory, result1.Status);
            Assert.Equal(TakeoverStatus.DefenderVictory, result2.Status);

            // Process both results
            encounter1.End(CombatStatus.AttackerVictory);
            encounter2.End(CombatStatus.DefenderVictory);

            _combatResultHandler.ProcessCombatResult(encounter1);
            _combatResultHandler.ProcessCombatResult(encounter2);

            // Verify final state - zone1 is neutral after attacker victory
            Assert.Null(_zoneRepository.GetById(zone1.Id)!.OwnerFactionId); // Neutral
            Assert.Equal(TrevorFactionId, _zoneRepository.GetById(zone2.Id)!.OwnerFactionId);
            Assert.Equal(MichaelFactionId, _zoneRepository.GetById(zone3.Id)!.OwnerFactionId);

            // Verify territory counts - zone1 is neutral, not captured
            Assert.Equal(1, _zoneService.GetZoneCount(MichaelFactionId)); // Only original zone3
            Assert.Equal(1, _zoneService.GetZoneCount(TrevorFactionId)); // Kept zone2
            Assert.Equal(0, _zoneService.GetZoneCount(FranklinFactionId));
        }

        #endregion

        #region Full Pipeline: Custom Threshold Configuration

        [Fact]
        public void FullPipeline_WorksWithCustomThresholds()
        {
            // Arrange: Custom thresholds (80% for attacker, 20% for defender)
            var customConfig = new TakeoverThresholdConfig
            {
                AttackerVictoryThreshold = 80f,
                DefenderVictoryThreshold = 20f
            };
            var customDetector = new TakeoverDetector(customConfig);

            var zone = CreateAndAddZone("zone-1", "Downtown", TrevorFactionId, 100f);
            var encounter = new CombatEncounter("combat-1", zone.Id, MichaelFactionId, TrevorFactionId);

            // At 85% attacker control (above 80% custom threshold)
            encounter.AttackerPedCount = 17;
            encounter.DefenderPedCount = 3;
            _controlCalculator.ApplyToEncounter(encounter);

            Assert.Equal(85f, encounter.AttackerControlPercentage);

            var result = customDetector.CheckTakeover(encounter);
            Assert.Equal(TakeoverStatus.AttackerVictory, result.Status);

            // End and process
            encounter.End(CombatStatus.AttackerVictory);
            var combatResult = _combatResultHandler.ProcessCombatResult(encounter);

            Assert.Equal(CombatResultOutcome.ZoneNeutralized, combatResult.Outcome);
        }

        #endregion

        #region Full Pipeline: Edge Cases

        [Fact]
        public void FullPipeline_HandlesZeroPeds_Gracefully()
        {
            // Arrange
            var zone = CreateAndAddZone("zone-1", "Downtown", TrevorFactionId, 100f);
            var encounter = new CombatEncounter("combat-1", zone.Id, MichaelFactionId, TrevorFactionId);

            // Both sides have zero peds
            encounter.AttackerPedCount = 0;
            encounter.DefenderPedCount = 0;

            _controlCalculator.ApplyToEncounter(encounter);

            // When both are zero, percentages are 50/50 (neutral state to prevent premature victory)
            Assert.Equal(50f, encounter.AttackerControlPercentage);
            Assert.Equal(50f, encounter.DefenderControlPercentage);

            // This is in-progress - neither side has won (both at 50%)
            var result = _takeoverDetector.CheckTakeover(encounter);
            Assert.Equal(TakeoverStatus.InProgress, result.Status);
        }

        [Fact]
        public void FullPipeline_TerritoryValueCorrectlyUpdated()
        {
            // Arrange: High-value zone
            var highValueZone = new Zone("zone-hv", "Diamond Casino", new Vector3(0, 0, 0), 200f, 10);
            highValueZone.OwnerFactionId = TrevorFactionId;
            highValueZone.ControlPercentage = 100f;
            _zoneRepository.Add(highValueZone);

            // Initial territory value
            Assert.Equal(10, _zoneService.GetFactionTerritoryValue(TrevorFactionId));
            Assert.Equal(0, _zoneService.GetFactionTerritoryValue(MichaelFactionId));

            // Full combat pipeline
            var encounter = new CombatEncounter("combat-1", highValueZone.Id, MichaelFactionId, TrevorFactionId);
            encounter.AttackerPedCount = 30;
            encounter.DefenderPedCount = 0;

            _controlCalculator.ApplyToEncounter(encounter);
            var takeoverResult = _takeoverDetector.CheckTakeover(encounter);
            Assert.Equal(TakeoverStatus.AttackerVictory, takeoverResult.Status);

            encounter.End(CombatStatus.AttackerVictory);
            _combatResultHandler.ProcessCombatResult(encounter);

            // Verify territory value: Trevor loses, Michael doesn't gain until claiming
            Assert.Equal(0, _zoneService.GetFactionTerritoryValue(TrevorFactionId));
            Assert.Equal(0, _zoneService.GetFactionTerritoryValue(MichaelFactionId)); // Zone is neutral
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
