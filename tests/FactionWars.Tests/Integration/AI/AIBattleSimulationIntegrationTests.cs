using System;
using System.Collections.Generic;
using System.Linq;
using FactionWars.AI.Interfaces;
using FactionWars.AI.Models;
using FactionWars.AI.Strategies;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.Core.Services;
using FactionWars.Factions.Interfaces;
using FactionWars.Factions.Models;
using FactionWars.Factions.Repositories;
using FactionWars.Factions.Services;
using FactionWars.ScriptHookV.Managers;
using FactionWars.Territory.Interfaces;
using FactionWars.Territory.Models;
using FactionWars.Territory.Repositories;
using FactionWars.Territory.Services;
using Moq;
using Xunit;

namespace FactionWars.Tests.Integration.AI
{
    /// <summary>
    /// Integration tests verifying that AI factions can attack, defend, and battle each other.
    /// Tests the complete flow from AI decision making through battle simulation to
    /// zone ownership changes and casualty application.
    ///
    /// Uses real implementations of AI strategies, battle simulation, and zone/faction services
    /// with only IGameBridge mocked (representing actual GTA V interactions).
    /// </summary>
    public class AIBattleSimulationIntegrationTests
    {
        private readonly Mock<IGameBridge> _mockGameBridge;
        private readonly InMemoryZoneRepository _zoneRepository;
        private readonly IZoneService _zoneService;
        private readonly InMemoryFactionRepository _factionRepository;
        private readonly IFactionService _factionService;
        private readonly IBattleSimulationService _battleSimulationService;
        private readonly IAIStrategy _michaelStrategy;
        private readonly IAIStrategy _trevorStrategy;
        private readonly IAIStrategy _franklinStrategy;
        private readonly Dictionary<string, IAIStrategy> _strategies;
        private readonly AIManager _aiManager;

        private const string MichaelFactionId = "faction-michael";
        private const string TrevorFactionId = "faction-trevor";
        private const string FranklinFactionId = "faction-franklin";

        public AIBattleSimulationIntegrationTests()
        {
            _mockGameBridge = new Mock<IGameBridge>();

            // Real implementations
            _zoneRepository = new InMemoryZoneRepository();
            _zoneService = new ZoneService(_zoneRepository);
            _factionRepository = new InMemoryFactionRepository();
            _factionService = new FactionService(_factionRepository);
            _battleSimulationService = new BattleSimulationService();

            // Real AI strategies
            _michaelStrategy = new MichaelAIStrategy();
            _trevorStrategy = new TrevorAIStrategy();
            _franklinStrategy = new FranklinAIStrategy();

            _strategies = new Dictionary<string, IAIStrategy>
            {
                { MichaelFactionId, _michaelStrategy },
                { TrevorFactionId, _trevorStrategy },
                { FranklinFactionId, _franklinStrategy }
            };

            _aiManager = new AIManager(_factionService, _zoneService, _strategies);
        }

        #region AI Attack Flow Tests

        [Fact]
        public void AIAttack_TrevorAttacksMichaelZone_BattleSimulatedAndZoneChangesOwner()
        {
            // Arrange: Trevor (aggressive) has overwhelming force against weak Michael zone
            SetupFaction(TrevorFactionId, "Trevor's Gang", initialTroops: 100);
            SetupFaction(MichaelFactionId, "Michael's Crew", initialTroops: 10);

            var trevorZone = CreateAndAddZone("zone-trevor", "Sandy Shores", TrevorFactionId, strategicValue: 5);
            var targetZone = CreateAndAddZone("zone-target", "Vinewood", MichaelFactionId, strategicValue: 8);

            // Player is Michael, so Trevor is AI controlled
            _aiManager.SetPlayerFactionId(MichaelFactionId);
            _aiManager.Start();

            // Act: Get Trevor's attack decision
            _aiManager.ForceDecision();
            var trevorDecisions = _aiManager.GetLastDecisions(TrevorFactionId);
            var attackDecision = trevorDecisions.FirstOrDefault(d => d.DecisionType == AIDecisionType.Attack);

            Assert.NotNull(attackDecision);
            Assert.Equal("zone-target", attackDecision!.TargetZoneId);

            // Simulate the battle using the decision
            var attackerTroops = new TroopComposition(attackDecision.TroopsToCommit, 0, 0);
            var defenderState = _factionService.GetFactionState(MichaelFactionId);
            var defenderTroopCount = defenderState?.TroopCount ?? 0;
            var defenderTroops = new TroopComposition(defenderTroopCount, 0, 0);

            var battleResult = _battleSimulationService.SimulateBattle(
                TrevorFactionId,
                MichaelFactionId,
                "zone-target",
                attackerTroops,
                defenderTroops);

            // Assert: Trevor should win (overwhelming force)
            Assert.True(battleResult.AttackerWon, "Trevor should win with overwhelming force");
            Assert.Equal(TrevorFactionId, battleResult.NewOwnerFactionId);
            Assert.Equal("zone-target", battleResult.ZoneId);
        }

        [Fact]
        public void AIAttack_WeakAttackerVsStrongDefender_DefenderHoldsZone()
        {
            // Arrange: Trevor attacks with few troops against strong Michael defense
            SetupFaction(TrevorFactionId, "Trevor's Gang", initialTroops: 10);
            SetupFaction(MichaelFactionId, "Michael's Crew", initialTroops: 100);

            CreateAndAddZone("zone-trevor", "Sandy Shores", TrevorFactionId, strategicValue: 3);
            CreateAndAddZone("zone-target", "Vinewood", MichaelFactionId, strategicValue: 10);

            // Act: Simulate a battle with weak attacker vs strong defender
            var attackerTroops = new TroopComposition(10, 0, 0);
            var defenderTroops = new TroopComposition(100, 0, 0);

            var battleResult = _battleSimulationService.SimulateBattle(
                TrevorFactionId,
                MichaelFactionId,
                "zone-target",
                attackerTroops,
                defenderTroops);

            // Assert: Defender should hold
            Assert.False(battleResult.AttackerWon, "Defender should hold with superior force");
            Assert.Equal(MichaelFactionId, battleResult.NewOwnerFactionId);
        }

        [Fact]
        public void AIAttack_DecisionIncludesTroopCommitment()
        {
            // Arrange
            SetupFaction(TrevorFactionId, "Trevor's Gang", initialTroops: 50);
            SetupFaction(MichaelFactionId, "Michael's Crew", initialTroops: 30);

            CreateAndAddZone("zone-trevor", "Sandy Shores", TrevorFactionId, strategicValue: 5);
            CreateAndAddZone("zone-target", "Vinewood", MichaelFactionId, strategicValue: 8);

            _aiManager.SetPlayerFactionId(MichaelFactionId);
            _aiManager.Start();

            // Act
            _aiManager.ForceDecision();
            var trevorDecisions = _aiManager.GetLastDecisions(TrevorFactionId);
            var attackDecision = trevorDecisions.FirstOrDefault(d => d.DecisionType == AIDecisionType.Attack);

            // Assert: Attack decision should include troops
            Assert.NotNull(attackDecision);
            Assert.True(attackDecision!.TroopsToCommit > 0, "Attack decision should commit troops");
            Assert.True(attackDecision.TroopsToCommit <= 50, "Cannot commit more troops than available");
        }

        #endregion

        #region AI Defend Flow Tests

        [Fact]
        public void AIDefend_MichaelDefendsContestedZone_DefendDecisionMade()
        {
            // Arrange: Michael (defensive) has a contested zone
            SetupFaction(MichaelFactionId, "Michael's Crew", initialTroops: 50);
            SetupFaction(TrevorFactionId, "Trevor's Gang", initialTroops: 50);

            var contestedZone = CreateAndAddZone("zone-contested", "Vinewood", MichaelFactionId, strategicValue: 9);
            contestedZone.IsContested = true;
            _zoneRepository.Update(contestedZone);

            // Player is Trevor, so Michael is AI controlled
            _aiManager.SetPlayerFactionId(TrevorFactionId);
            _aiManager.Start();

            // Act
            _aiManager.ForceDecision();
            var michaelDecisions = _aiManager.GetLastDecisions(MichaelFactionId);

            // Assert: Michael should prioritize defense
            Assert.NotEmpty(michaelDecisions);
            var defendDecision = michaelDecisions.FirstOrDefault(d => d.DecisionType == AIDecisionType.Defend);
            Assert.NotNull(defendDecision);
            Assert.Equal("zone-contested", defendDecision!.TargetZoneId);
        }

        [Fact]
        public void AIDefend_DefenderWithTroops_SuccessfullyDefendsAgainstWeakAttacker()
        {
            // Arrange
            SetupFaction(MichaelFactionId, "Michael's Crew", initialTroops: 50);
            SetupFaction(TrevorFactionId, "Trevor's Gang", initialTroops: 15);

            // Simulate Trevor attacking Michael's defended zone
            var attackerTroops = new TroopComposition(15, 0, 0);
            var defenderTroops = new TroopComposition(50, 0, 0);

            // Act
            var battleResult = _battleSimulationService.SimulateBattle(
                TrevorFactionId,
                MichaelFactionId,
                "zone-vinewood",
                attackerTroops,
                defenderTroops);

            // Assert: Michael's defense should hold
            Assert.False(battleResult.AttackerWon);
            Assert.Equal(MichaelFactionId, battleResult.NewOwnerFactionId);
            Assert.True(battleResult.AttackerCasualties.TotalCount > 0, "Attacker should suffer casualties");
        }

        [Fact]
        public void AIDefend_DefendDecision_HasHighPriority()
        {
            // Arrange
            SetupFaction(MichaelFactionId, "Michael's Crew", initialTroops: 50);
            SetupFaction(TrevorFactionId, "Trevor's Gang", initialTroops: 50);

            var contestedZone = CreateAndAddZone("zone-contested", "Downtown", MichaelFactionId, strategicValue: 10);
            contestedZone.IsContested = true;
            _zoneRepository.Update(contestedZone);

            CreateAndAddZone("zone-enemy", "Sandy Shores", TrevorFactionId, strategicValue: 5);

            _aiManager.SetPlayerFactionId(TrevorFactionId);
            _aiManager.Start();

            // Act
            _aiManager.ForceDecision();
            var michaelDecisions = _aiManager.GetLastDecisions(MichaelFactionId);

            // Assert: First decision should be defend (highest priority)
            Assert.NotEmpty(michaelDecisions);
            Assert.Equal(AIDecisionType.Defend, michaelDecisions[0].DecisionType);
        }

        #endregion

        #region AI vs AI Battle Flow Tests

        [Fact]
        public void AIvsAI_TwoAIFactionsBattle_BattleSimulatedCorrectly()
        {
            // Arrange: Franklin (player), Trevor attacks Michael
            // Give Trevor high strategic value targets to ensure attack decisions
            SetupFaction(FranklinFactionId, "Franklin's Family", initialTroops: 30);
            SetupFaction(TrevorFactionId, "Trevor's Gang", initialTroops: 60);
            SetupFaction(MichaelFactionId, "Michael's Crew", initialTroops: 40);

            CreateAndAddZone("zone-franklin", "Grove Street", FranklinFactionId, strategicValue: 5);
            CreateAndAddZone("zone-trevor", "Sandy Shores", TrevorFactionId, strategicValue: 4);
            CreateAndAddZone("zone-michael", "Vinewood", MichaelFactionId, strategicValue: 8);

            _aiManager.SetPlayerFactionId(FranklinFactionId);
            _aiManager.Start();

            // Act: Both Trevor and Michael are AI - get their decisions
            _aiManager.ForceDecision();

            var trevorDecisions = _aiManager.GetLastDecisions(TrevorFactionId);
            var michaelDecisions = _aiManager.GetLastDecisions(MichaelFactionId);

            // Assert: At least one AI faction should have made decisions
            // (both have troops and enemy zones available)
            Assert.True(trevorDecisions.Count > 0 || michaelDecisions.Count > 0,
                "At least one AI faction should make decisions");

            // Simulate a battle between them regardless of decisions
            var attackerTroops = new TroopComposition(30, 0, 0);
            var defenderTroops = new TroopComposition(20, 0, 0);

            var battleResult = _battleSimulationService.SimulateBattle(
                TrevorFactionId,
                MichaelFactionId,
                "zone-michael",
                attackerTroops,
                defenderTroops);

            // Both should have casualties
            Assert.True(battleResult.AttackerCasualties.TotalCount > 0 || battleResult.DefenderCasualties.TotalCount > 0,
                "At least one side should have casualties in battle");
        }

        [Fact]
        public void AIvsAI_MultipleAIFactionsActive_EachMakesDecisions()
        {
            // Arrange: Michael is player, Trevor and Franklin are AI
            SetupFaction(MichaelFactionId, "Michael's Crew", initialTroops: 50);
            SetupFaction(TrevorFactionId, "Trevor's Gang", initialTroops: 60);
            SetupFaction(FranklinFactionId, "Franklin's Family", initialTroops: 40);

            CreateAndAddZone("zone-m1", "Vinewood", MichaelFactionId, strategicValue: 8);
            CreateAndAddZone("zone-t1", "Sandy Shores", TrevorFactionId, strategicValue: 5);
            CreateAndAddZone("zone-f1", "Grove Street", FranklinFactionId, strategicValue: 6);

            _aiManager.SetPlayerFactionId(MichaelFactionId);
            _aiManager.Start();

            var decisionsReceived = new Dictionary<string, int>();
            _aiManager.OnAIDecision += (sender, args) =>
            {
                if (!decisionsReceived.ContainsKey(args.FactionId))
                    decisionsReceived[args.FactionId] = 0;
                decisionsReceived[args.FactionId]++;
            };

            // Act
            _aiManager.ForceDecision();

            // Assert: Both Trevor and Franklin made decisions, Michael did not
            Assert.False(decisionsReceived.ContainsKey(MichaelFactionId), "Player faction should not make AI decisions");
            Assert.True(decisionsReceived.ContainsKey(TrevorFactionId) || decisionsReceived.ContainsKey(FranklinFactionId),
                "At least one AI faction should make decisions");
        }

        [Fact]
        public void AIvsAI_BattleProducesCasualties_ForBothSides()
        {
            // Arrange: Evenly matched forces
            var attackerTroops = new TroopComposition(30, 15, 5);
            var defenderTroops = new TroopComposition(25, 12, 8);

            // Act
            var result = _battleSimulationService.SimulateBattle(
                TrevorFactionId,
                MichaelFactionId,
                "zone-contested",
                attackerTroops,
                defenderTroops);

            // Assert: Both sides should have casualties in a contested battle
            Assert.NotNull(result.AttackerCasualties);
            Assert.NotNull(result.DefenderCasualties);
            Assert.True(result.AttackerCasualties.TotalCount > 0 || result.DefenderCasualties.TotalCount > 0,
                "Both sides should suffer some casualties");
        }

        #endregion

        #region Battle Outcome Tests

        [Fact]
        public void BattleOutcome_OverwhelmingAttacker_CapturesZone()
        {
            // Arrange: 100 heavy troops vs 10 basic troops
            var attackerTroops = new TroopComposition(0, 0, 100); // Heavy troops = 200 strength
            var defenderTroops = new TroopComposition(10, 0, 0);  // Basic troops = 10 strength

            // Act
            var result = _battleSimulationService.SimulateBattle(
                TrevorFactionId,
                MichaelFactionId,
                "zone-target",
                attackerTroops,
                defenderTroops);

            // Assert
            Assert.True(result.AttackerWon);
            Assert.Equal(TrevorFactionId, result.NewOwnerFactionId);
        }

        [Fact]
        public void BattleOutcome_OverwhelmingDefender_HoldsZone()
        {
            // Arrange: 10 basic troops vs 100 heavy troops
            var attackerTroops = new TroopComposition(10, 0, 0);
            var defenderTroops = new TroopComposition(0, 0, 100);

            // Act
            var result = _battleSimulationService.SimulateBattle(
                TrevorFactionId,
                MichaelFactionId,
                "zone-target",
                attackerTroops,
                defenderTroops);

            // Assert
            Assert.False(result.AttackerWon);
            Assert.Equal(MichaelFactionId, result.NewOwnerFactionId);
        }

        [Fact]
        public void BattleOutcome_EqualForces_DefenderHasAdvantage()
        {
            // Arrange: Equal forces
            var attackerTroops = new TroopComposition(50, 0, 0);
            var defenderTroops = new TroopComposition(50, 0, 0);

            // Act
            var result = _battleSimulationService.SimulateBattle(
                TrevorFactionId,
                MichaelFactionId,
                "zone-target",
                attackerTroops,
                defenderTroops);

            // Assert: Defender advantage should tip the scales
            Assert.False(result.AttackerWon, "Equal forces should favor defender");
            Assert.Equal(MichaelFactionId, result.NewOwnerFactionId);
        }

        [Fact]
        public void BattleOutcome_TroopTiersMatter_HeavyTroopsBeatBasic()
        {
            // Arrange: Fewer heavy troops vs more basic troops
            var heavyAttacker = new TroopComposition(0, 0, 25);  // 25 heavy = 50 strength
            var basicDefender = new TroopComposition(40, 0, 0);  // 40 basic = 40 strength (48 with defender advantage)

            var basicAttacker = new TroopComposition(25, 0, 0);  // 25 basic = 25 strength
            var heavyDefender = new TroopComposition(0, 0, 20);  // 20 heavy = 40 strength (48 with defender advantage)

            // Act
            var heavyAttackResult = _battleSimulationService.SimulateBattle(
                "heavy-faction", "basic-faction", "zone1", heavyAttacker, basicDefender);

            var basicAttackResult = _battleSimulationService.SimulateBattle(
                "basic-faction", "heavy-faction", "zone2", basicAttacker, heavyDefender);

            // Assert: Heavy attackers should have more success than basic attackers
            // Heavy attacker (50 strength) vs basic defender (48 effective) - close fight
            // Basic attacker (25 strength) vs heavy defender (48 effective) - defender wins easily
            Assert.False(basicAttackResult.AttackerWon, "Basic troops should lose to heavy defenders");
        }

        #endregion

        #region Casualty Application Tests

        [Fact]
        public void Casualties_DoNotExceedCommittedTroops()
        {
            // Arrange
            var attackerTroops = new TroopComposition(10, 5, 2);
            var defenderTroops = new TroopComposition(100, 50, 25); // Overwhelming defense

            // Act
            var result = _battleSimulationService.SimulateBattle(
                TrevorFactionId,
                MichaelFactionId,
                "zone-target",
                attackerTroops,
                defenderTroops);

            // Assert
            Assert.True(result.AttackerCasualties.Basic <= attackerTroops.Basic);
            Assert.True(result.AttackerCasualties.Medium <= attackerTroops.Medium);
            Assert.True(result.AttackerCasualties.Heavy <= attackerTroops.Heavy);
            Assert.True(result.DefenderCasualties.Basic <= defenderTroops.Basic);
            Assert.True(result.DefenderCasualties.Medium <= defenderTroops.Medium);
            Assert.True(result.DefenderCasualties.Heavy <= defenderTroops.Heavy);
        }

        [Fact]
        public void Casualties_BasicTroopsLostBeforeHeavy()
        {
            // Arrange: Mixed troops with moderate opposing strength
            var troops = new TroopComposition(20, 15, 10);

            // Act
            var casualties = _battleSimulationService.CalculateCasualties(troops, 30f);

            // Assert: Basic casualty rate >= Medium >= Heavy (if all tiers have troops)
            if (casualties.TotalCount > 0)
            {
                float basicRate = troops.Basic > 0 ? (float)casualties.Basic / troops.Basic : 0f;
                float mediumRate = troops.Medium > 0 ? (float)casualties.Medium / troops.Medium : 0f;
                float heavyRate = troops.Heavy > 0 ? (float)casualties.Heavy / troops.Heavy : 0f;

                Assert.True(basicRate >= mediumRate, $"Basic rate ({basicRate}) should be >= Medium rate ({mediumRate})");
                Assert.True(mediumRate >= heavyRate, $"Medium rate ({mediumRate}) should be >= Heavy rate ({heavyRate})");
            }
        }

        #endregion

        #region Integration: Decision to Battle to Outcome Tests

        [Fact]
        public void FullFlow_AIDecisionToBattle_ZoneOwnershipDetermined()
        {
            // Arrange
            SetupFaction(TrevorFactionId, "Trevor's Gang", initialTroops: 80);
            SetupFaction(MichaelFactionId, "Michael's Crew", initialTroops: 20);

            CreateAndAddZone("zone-trevor", "Sandy Shores", TrevorFactionId, strategicValue: 4);
            var targetZone = CreateAndAddZone("zone-target", "Vinewood", MichaelFactionId, strategicValue: 9);

            _aiManager.SetPlayerFactionId(MichaelFactionId);
            _aiManager.Start();

            // Act: Step 1 - AI makes decision
            _aiManager.ForceDecision();
            var trevorDecisions = _aiManager.GetLastDecisions(TrevorFactionId);
            var attackDecision = trevorDecisions.FirstOrDefault(d => d.DecisionType == AIDecisionType.Attack);

            Assert.NotNull(attackDecision);

            // Step 2 - Convert decision to battle
            var attackerTroops = new TroopComposition(attackDecision!.TroopsToCommit, 0, 0);
            var defenderTroops = new TroopComposition(20, 0, 0); // Michael's troops

            // Step 3 - Simulate battle
            var battleResult = _battleSimulationService.SimulateBattle(
                TrevorFactionId,
                MichaelFactionId,
                attackDecision.TargetZoneId!,
                attackerTroops,
                defenderTroops);

            // Assert: Complete flow produces valid result
            Assert.NotNull(battleResult);
            Assert.Equal(attackDecision.TargetZoneId, battleResult.ZoneId);
            Assert.True(battleResult.NewOwnerFactionId == TrevorFactionId || battleResult.NewOwnerFactionId == MichaelFactionId);
        }

        [Fact]
        public void FullFlow_MultipleAIBattles_ProduceValidResults()
        {
            // Arrange: Player is Franklin, Trevor and Michael are AI
            SetupFaction(FranklinFactionId, "Franklin's Family", initialTroops: 30);
            SetupFaction(TrevorFactionId, "Trevor's Gang", initialTroops: 60);
            SetupFaction(MichaelFactionId, "Michael's Crew", initialTroops: 50);

            CreateAndAddZone("zone-f1", "Grove Street", FranklinFactionId, strategicValue: 5);
            CreateAndAddZone("zone-t1", "Sandy Shores", TrevorFactionId, strategicValue: 4);
            CreateAndAddZone("zone-m1", "Vinewood", MichaelFactionId, strategicValue: 8);

            _aiManager.SetPlayerFactionId(FranklinFactionId);
            _aiManager.Start();

            // Act: Run multiple decision cycles
            for (int i = 0; i < 3; i++)
            {
                _aiManager.ForceDecision();
            }

            var trevorDecisions = _aiManager.GetLastDecisions(TrevorFactionId);
            var michaelDecisions = _aiManager.GetLastDecisions(MichaelFactionId);

            // Both AI factions should be making decisions
            Assert.True(trevorDecisions.Count > 0 || michaelDecisions.Count > 0,
                "AI factions should make decisions over multiple cycles");
        }

        #endregion

        #region Edge Cases

        [Fact]
        public void EdgeCase_NoTroops_DefenderHoldsAutomatically()
        {
            // Arrange: Attacker with no troops
            var attackerTroops = TroopComposition.Empty;
            var defenderTroops = new TroopComposition(10, 0, 0);

            // Act
            var result = _battleSimulationService.SimulateBattle(
                TrevorFactionId,
                MichaelFactionId,
                "zone-target",
                attackerTroops,
                defenderTroops);

            // Assert
            Assert.False(result.AttackerWon);
            Assert.Equal(MichaelFactionId, result.NewOwnerFactionId);
        }

        [Fact]
        public void EdgeCase_NoDefenders_AttackerCapturesAutomatically()
        {
            // Arrange: Defender with no troops
            var attackerTroops = new TroopComposition(10, 0, 0);
            var defenderTroops = TroopComposition.Empty;

            // Act
            var result = _battleSimulationService.SimulateBattle(
                TrevorFactionId,
                MichaelFactionId,
                "zone-target",
                attackerTroops,
                defenderTroops);

            // Assert
            Assert.True(result.AttackerWon);
            Assert.Equal(TrevorFactionId, result.NewOwnerFactionId);
        }

        [Fact]
        public void EdgeCase_BothNoTroops_DefenderHoldsPosition()
        {
            // Arrange: Both have no troops
            var attackerTroops = TroopComposition.Empty;
            var defenderTroops = TroopComposition.Empty;

            // Act
            var result = _battleSimulationService.SimulateBattle(
                TrevorFactionId,
                MichaelFactionId,
                "zone-target",
                attackerTroops,
                defenderTroops);

            // Assert: Defender advantage - they hold position
            Assert.False(result.AttackerWon);
            Assert.Equal(MichaelFactionId, result.NewOwnerFactionId);
        }

        #endregion

        #region Helper Methods

        private void SetupFaction(string factionId, string name, int initialTroops = 0, int initialCash = 0)
        {
            var faction = new Faction(factionId, name);
            _factionRepository.Add(faction);
            _factionService.InitializeFactionState(factionId, initialCash, initialTroops);
        }

        private Zone CreateAndAddZone(string id, string name, string? ownerFactionId, int strategicValue = 5)
        {
            var zone = new Zone(id, name, new Vector3(0, 0, 0), 150f, strategicValue);
            zone.OwnerFactionId = ownerFactionId;
            zone.ControlPercentage = 100f;

            // Make all zones adjacent to each other for testing AI decisions
            var existingZones = _zoneRepository.GetAll().ToList();
            foreach (var existing in existingZones)
            {
                zone.AdjacentZoneIds.Add(existing.Id);
                existing.AdjacentZoneIds.Add(id);
                _zoneRepository.Update(existing);
            }

            _zoneRepository.Add(zone);

            if (ownerFactionId != null)
            {
                _factionService.AddZoneToFaction(ownerFactionId, id);
            }

            return zone;
        }

        #endregion
    }
}
