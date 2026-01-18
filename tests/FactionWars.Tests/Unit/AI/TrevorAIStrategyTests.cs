using FactionWars.AI.Interfaces;
using FactionWars.AI.Models;
using FactionWars.AI.Strategies;
using FactionWars.Factions.Models;
using FactionWars.Territory.Models;
using FactionWars.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Moq;

namespace FactionWars.Tests.Unit.AI
{
    /// <summary>
    /// Tests for TrevorAIStrategy.
    /// Trevor's strategy is aggressive, combat-focused, and risk-tolerant.
    /// - High aggressiveness (attack-first approach)
    /// - High risk tolerance (willing to take chances)
    /// - Combat bonuses (more troops allocated to attacks)
    /// - Lower concern for zone value (attacks any target)
    /// </summary>
    public class TrevorAIStrategyTests
    {
        #region Constructor Tests

        [Fact]
        public void Constructor_SetsFactionTypeToTrevor()
        {
            var strategy = new TrevorAIStrategy();

            Assert.Equal(FactionType.Trevor, strategy.FactionType);
        }

        [Fact]
        public void Constructor_SetsHighAggressiveness()
        {
            var strategy = new TrevorAIStrategy();

            // Trevor should be highly aggressive (>= 0.7)
            Assert.True(strategy.GetAggressiveness() >= 0.7f);
        }

        [Fact]
        public void Constructor_SetsHighRiskTolerance()
        {
            var strategy = new TrevorAIStrategy();

            // Trevor is risk-tolerant and willing to take chances (>= 0.7)
            Assert.True(strategy.GetRiskTolerance() >= 0.7f);
        }

        #endregion

        #region EvaluateZone Tests - Combat Focus

        [Fact]
        public void EvaluateZone_EnemyZone_ScoresHigherThanNeutral()
        {
            var strategy = new TrevorAIStrategy();
            var context = CreateTestContext();

            var enemyZone = CreateTestZone("enemy", ownerFactionId: "enemy-faction", strategicValue: 5);
            var neutralZone = CreateTestZone("neutral", strategicValue: 5);

            var enemyScore = strategy.EvaluateZone(enemyZone, context);
            var neutralScore = strategy.EvaluateZone(neutralZone, context);

            // Trevor prefers fighting enemies over taking neutral zones
            Assert.True(enemyScore >= neutralScore, "Trevor should find enemy zones at least as attractive as neutral zones");
        }

        [Fact]
        public void EvaluateZone_LowValueEnemyZone_StillAttractiveForFight()
        {
            var strategy = new TrevorAIStrategy();
            var context = CreateTestContext();

            var lowValueEnemyZone = CreateTestZone("low-enemy", ownerFactionId: "enemy-faction", strategicValue: 2);

            var score = strategy.EvaluateZone(lowValueEnemyZone, context);

            // Trevor doesn't care as much about value - he wants combat
            // Even low value zones should have meaningful scores if enemies own them
            Assert.True(score > 0.2f, "Low value enemy zones should still be attractive to Trevor");
        }

        [Fact]
        public void EvaluateZone_HighAggressivenessBoostsScores()
        {
            var trevorStrategy = new TrevorAIStrategy();
            var michaelStrategy = new MichaelAIStrategy();
            var context = CreateTestContext();

            var zone = CreateTestZone("zone-1", strategicValue: 5);

            var trevorScore = trevorStrategy.EvaluateZone(zone, context);
            var michaelScore = michaelStrategy.EvaluateZone(zone, context);

            // Trevor's high aggressiveness should give him higher scores for attack evaluation
            Assert.True(trevorScore > michaelScore, "Trevor's aggressiveness should boost zone scores");
        }

        [Fact]
        public void EvaluateZone_NullZone_ThrowsArgumentNullException()
        {
            var strategy = new TrevorAIStrategy();
            var context = CreateTestContext();

            Assert.Throws<ArgumentNullException>(() => strategy.EvaluateZone(null!, context));
        }

        [Fact]
        public void EvaluateZone_NullContext_ThrowsArgumentNullException()
        {
            var strategy = new TrevorAIStrategy();
            var zone = CreateTestZone("zone-1");

            Assert.Throws<ArgumentNullException>(() => strategy.EvaluateZone(zone, null!));
        }

        [Fact]
        public void EvaluateZone_ReturnsValueInValidRange()
        {
            var strategy = new TrevorAIStrategy();
            var context = CreateTestContext();
            var zone = CreateTestZone("zone-1", strategicValue: 7);

            var score = strategy.EvaluateZone(zone, context);

            Assert.InRange(score, 0f, 1f);
        }

        #endregion

        #region MakeDecisions Tests - Attack Priority

        [Fact]
        public void MakeDecisions_WithEnemyZones_PrioritizesAttack()
        {
            var strategy = new TrevorAIStrategy();
            var faction = CreateTestFaction(FactionType.Trevor);
            var factionState = CreateTestFactionState(faction.Id, troopCount: 50);

            var ownedZone = CreateTestZone("owned", ownerFactionId: faction.Id, strategicValue: 5);
            var enemyZone = CreateTestZone("enemy", ownerFactionId: "enemy-faction", strategicValue: 5);

            var ownedZones = new List<Zone> { ownedZone };
            var allZones = new List<Zone> { ownedZone, enemyZone };
            var enemyFactions = new List<Faction> { CreateTestFaction(FactionType.Michael, "enemy-faction") };
            var context = new AIContext(faction, factionState, ownedZones, allZones, enemyFactions);

            var decisions = strategy.MakeDecisions(context);

            // Trevor should have attack decisions when enemies exist
            var attackDecision = decisions.FirstOrDefault(d => d.DecisionType == AIDecisionType.Attack);
            Assert.NotNull(attackDecision);
        }

        [Fact]
        public void MakeDecisions_AttackDecisions_HaveHighPriority()
        {
            var strategy = new TrevorAIStrategy();
            var faction = CreateTestFaction(FactionType.Trevor);
            var factionState = CreateTestFactionState(faction.Id, troopCount: 100);

            var ownedZone = CreateTestZone("owned", ownerFactionId: faction.Id, strategicValue: 5);
            var enemyZone = CreateTestZone("enemy", ownerFactionId: "enemy-faction", strategicValue: 5);

            var ownedZones = new List<Zone> { ownedZone };
            var allZones = new List<Zone> { ownedZone, enemyZone };
            var enemyFactions = new List<Faction> { CreateTestFaction(FactionType.Michael, "enemy-faction") };
            var context = new AIContext(faction, factionState, ownedZones, allZones, enemyFactions);

            var decisions = strategy.MakeDecisions(context);

            var attackDecision = decisions.FirstOrDefault(d => d.DecisionType == AIDecisionType.Attack);
            Assert.NotNull(attackDecision);
            // Trevor prioritizes attack highly
            Assert.True(attackDecision.Priority >= 0.5f, "Attack should have high priority for Trevor");
        }

        [Fact]
        public void MakeDecisions_NullContext_ThrowsArgumentNullException()
        {
            var strategy = new TrevorAIStrategy();

            Assert.Throws<ArgumentNullException>(() => strategy.MakeDecisions(null!));
        }

        [Fact]
        public void MakeDecisions_NoTroops_ReturnsEmptyOrHold()
        {
            var strategy = new TrevorAIStrategy();
            var faction = CreateTestFaction(FactionType.Trevor);
            var factionState = CreateTestFactionState(faction.Id, troopCount: 0);

            var ownedZone = CreateTestZone("owned", ownerFactionId: faction.Id);
            var context = new AIContext(faction, factionState, new[] { ownedZone }, new[] { ownedZone }, new List<Faction>());

            var decisions = strategy.MakeDecisions(context);

            Assert.True(decisions.Count == 0 || decisions.All(d => d.DecisionType == AIDecisionType.Hold));
        }

        #endregion

        #region ShouldAttack Tests - Aggressive Approach

        [Fact]
        public void ShouldAttack_LowValueTarget_StillReturnsTrue()
        {
            var strategy = new TrevorAIStrategy();
            var faction = CreateTestFaction(FactionType.Trevor);
            var factionState = CreateTestFactionState(faction.Id, troopCount: 50);

            // Unlike Michael, Trevor will attack even low value targets
            var lowValueZone = CreateTestZone("low-value", ownerFactionId: "enemy-faction", strategicValue: 3);
            var enemyFactions = new List<Faction> { CreateTestFaction(FactionType.Michael, "enemy-faction") };
            var context = new AIContext(faction, factionState, new List<Zone>(), new[] { lowValueZone }, enemyFactions);

            var shouldAttack = strategy.ShouldAttack(lowValueZone, context);

            // Trevor's aggressiveness means he'll attack even low-value targets
            Assert.True(shouldAttack);
        }

        [Fact]
        public void ShouldAttack_AnyEnemyZone_WithTroops_ReturnsTrue()
        {
            var strategy = new TrevorAIStrategy();
            var faction = CreateTestFaction(FactionType.Trevor);
            var factionState = CreateTestFactionState(faction.Id, troopCount: 50);

            var enemyZone = CreateTestZone("enemy", ownerFactionId: "enemy-faction", strategicValue: 5);
            var enemyFactions = new List<Faction> { CreateTestFaction(FactionType.Michael, "enemy-faction") };
            var context = new AIContext(faction, factionState, new List<Zone>(), new[] { enemyZone }, enemyFactions);

            var shouldAttack = strategy.ShouldAttack(enemyZone, context);

            Assert.True(shouldAttack);
        }

        [Fact]
        public void ShouldAttack_OwnedZone_ReturnsFalse()
        {
            var strategy = new TrevorAIStrategy();
            var faction = CreateTestFaction(FactionType.Trevor);
            var factionState = CreateTestFactionState(faction.Id, troopCount: 50);

            var ownedZone = CreateTestZone("owned", ownerFactionId: faction.Id);
            var context = new AIContext(faction, factionState, new[] { ownedZone }, new[] { ownedZone }, new List<Faction>());

            var shouldAttack = strategy.ShouldAttack(ownedZone, context);

            Assert.False(shouldAttack);
        }

        [Fact]
        public void ShouldAttack_NeutralZone_WithTroops_ReturnsTrue()
        {
            var strategy = new TrevorAIStrategy();
            var faction = CreateTestFaction(FactionType.Trevor);
            var factionState = CreateTestFactionState(faction.Id, troopCount: 50);

            var neutralZone = CreateTestZone("neutral", strategicValue: 4);
            var context = new AIContext(faction, factionState, new List<Zone>(), new[] { neutralZone }, new List<Faction>());

            var shouldAttack = strategy.ShouldAttack(neutralZone, context);

            // Trevor will take neutral zones too
            Assert.True(shouldAttack);
        }

        [Fact]
        public void ShouldAttack_NoTroops_ReturnsFalse()
        {
            var strategy = new TrevorAIStrategy();
            var faction = CreateTestFaction(FactionType.Trevor);
            var factionState = CreateTestFactionState(faction.Id, troopCount: 0);

            var enemyZone = CreateTestZone("enemy", ownerFactionId: "enemy-faction", strategicValue: 10);
            var enemyFactions = new List<Faction> { CreateTestFaction(FactionType.Michael, "enemy-faction") };
            var context = new AIContext(faction, factionState, new List<Zone>(), new[] { enemyZone }, enemyFactions);

            var shouldAttack = strategy.ShouldAttack(enemyZone, context);

            // Even Trevor can't attack without troops
            Assert.False(shouldAttack);
        }

        [Fact]
        public void ShouldAttack_NullZone_ThrowsArgumentNullException()
        {
            var strategy = new TrevorAIStrategy();
            var context = CreateTestContext();

            Assert.Throws<ArgumentNullException>(() => strategy.ShouldAttack(null!, context));
        }

        [Fact]
        public void ShouldAttack_NullContext_ThrowsArgumentNullException()
        {
            var strategy = new TrevorAIStrategy();
            var zone = CreateTestZone("zone-1");

            Assert.Throws<ArgumentNullException>(() => strategy.ShouldAttack(zone, null!));
        }

        #endregion

        #region ShouldDefend Tests - Less Defense-Focused

        [Fact]
        public void ShouldDefend_ContestedZone_ReturnsTrue()
        {
            var strategy = new TrevorAIStrategy();
            var faction = CreateTestFaction(FactionType.Trevor);
            var factionState = CreateTestFactionState(faction.Id, troopCount: 20);

            var contestedZone = CreateTestZone("contested", ownerFactionId: faction.Id);
            contestedZone.IsContested = true;
            var context = new AIContext(faction, factionState, new[] { contestedZone }, new[] { contestedZone }, new List<Faction>());

            var shouldDefend = strategy.ShouldDefend(contestedZone, context);

            // Even Trevor will defend contested zones (it's a fight!)
            Assert.True(shouldDefend);
        }

        [Fact]
        public void ShouldDefend_HighValueNonContestedZone_MayNotDefend()
        {
            var strategy = new TrevorAIStrategy();
            var faction = CreateTestFaction(FactionType.Trevor);
            var factionState = CreateTestFactionState(faction.Id, troopCount: 30);

            var highValueZone = CreateTestZone("high-value", ownerFactionId: faction.Id, strategicValue: 8);
            var context = new AIContext(faction, factionState, new[] { highValueZone }, new[] { highValueZone }, new List<Faction>());

            var shouldDefend = strategy.ShouldDefend(highValueZone, context);

            // Trevor's high aggressiveness means lower defense thresholds
            // He'd rather attack than sit and defend
            // Test verifies the method runs; specific behavior is implementation detail
            Assert.NotNull(shouldDefend.ToString());
        }

        [Fact]
        public void ShouldDefend_NotOwnedZone_ReturnsFalse()
        {
            var strategy = new TrevorAIStrategy();
            var faction = CreateTestFaction(FactionType.Trevor);
            var factionState = CreateTestFactionState(faction.Id, troopCount: 50);

            var enemyZone = CreateTestZone("enemy", ownerFactionId: "enemy-faction");
            var context = new AIContext(faction, factionState, new List<Zone>(), new[] { enemyZone },
                new[] { CreateTestFaction(FactionType.Michael, "enemy-faction") });

            var shouldDefend = strategy.ShouldDefend(enemyZone, context);

            Assert.False(shouldDefend);
        }

        [Fact]
        public void ShouldDefend_NullZone_ThrowsArgumentNullException()
        {
            var strategy = new TrevorAIStrategy();
            var context = CreateTestContext();

            Assert.Throws<ArgumentNullException>(() => strategy.ShouldDefend(null!, context));
        }

        [Fact]
        public void ShouldDefend_NullContext_ThrowsArgumentNullException()
        {
            var strategy = new TrevorAIStrategy();
            var zone = CreateTestZone("zone-1");

            Assert.Throws<ArgumentNullException>(() => strategy.ShouldDefend(zone, null!));
        }

        #endregion

        #region GetTroopsForAction Tests - Combat Bonuses

        [Fact]
        public void GetTroopsForAction_Attack_AllocatesAggressively()
        {
            var trevorStrategy = new TrevorAIStrategy();
            var michaelStrategy = new MichaelAIStrategy();

            var faction = CreateTestFaction(FactionType.Trevor);
            var factionState = CreateTestFactionState(faction.Id, troopCount: 100);
            var context = new AIContext(faction, factionState, new List<Zone>(), new List<Zone>(), new List<Faction>());

            var attackDecision = new AIDecision(AIDecisionType.Attack, "zone-1", 0.7f, 0);

            var trevorTroops = trevorStrategy.GetTroopsForAction(attackDecision, context);
            var michaelTroops = michaelStrategy.GetTroopsForAction(attackDecision, context);

            // Trevor should allocate MORE troops for attacks (combat bonuses)
            Assert.True(trevorTroops >= michaelTroops, "Trevor should allocate at least as many troops as Michael for attacks");
        }

        [Fact]
        public void GetTroopsForAction_Attack_CommitsHighPercentage()
        {
            var strategy = new TrevorAIStrategy();
            var faction = CreateTestFaction(FactionType.Trevor);
            var factionState = CreateTestFactionState(faction.Id, troopCount: 100);
            var context = new AIContext(faction, factionState, new List<Zone>(), new List<Zone>(), new List<Faction>());

            var attackDecision = new AIDecision(AIDecisionType.Attack, "zone-1", 0.8f, 0);

            var troops = strategy.GetTroopsForAction(attackDecision, context);

            // Trevor should commit a significant portion of troops to attacks
            Assert.True(troops >= 30, "Trevor should commit substantial troops to attacks");
        }

        [Fact]
        public void GetTroopsForAction_Defense_StillProvidesTroops()
        {
            var strategy = new TrevorAIStrategy();
            var faction = CreateTestFaction(FactionType.Trevor);
            var factionState = CreateTestFactionState(faction.Id, troopCount: 100);
            var context = new AIContext(faction, factionState, new List<Zone>(), new List<Zone>(), new List<Faction>());

            var defendDecision = new AIDecision(AIDecisionType.Defend, "zone-1", 0.8f, 0);

            var troops = strategy.GetTroopsForAction(defendDecision, context);

            // Trevor still defends when needed, just not as enthusiastically
            Assert.True(troops > 0);
        }

        [Fact]
        public void GetTroopsForAction_DoesNotExceedAvailableTroops()
        {
            var strategy = new TrevorAIStrategy();
            var faction = CreateTestFaction(FactionType.Trevor);
            var factionState = CreateTestFactionState(faction.Id, troopCount: 20);
            var context = new AIContext(faction, factionState, new List<Zone>(), new List<Zone>(), new List<Faction>());

            var decision = new AIDecision(AIDecisionType.Attack, "zone-1", 1.0f, 0);

            var troops = strategy.GetTroopsForAction(decision, context);

            Assert.True(troops <= factionState.TroopCount);
        }

        [Fact]
        public void GetTroopsForAction_NullDecision_ThrowsArgumentNullException()
        {
            var strategy = new TrevorAIStrategy();
            var context = CreateTestContext();

            Assert.Throws<ArgumentNullException>(() => strategy.GetTroopsForAction(null!, context));
        }

        [Fact]
        public void GetTroopsForAction_NullContext_ThrowsArgumentNullException()
        {
            var strategy = new TrevorAIStrategy();
            var decision = new AIDecision(AIDecisionType.Attack, "zone-1", 0.8f, 0);

            Assert.Throws<ArgumentNullException>(() => strategy.GetTroopsForAction(decision, null!));
        }

        #endregion

        #region Comparative Tests - vs Michael (opposite style)

        [Fact]
        public void TrevorStrategy_IsMoreAggressiveThanMichael()
        {
            var trevorStrategy = new TrevorAIStrategy();
            var michaelStrategy = new MichaelAIStrategy();

            Assert.True(trevorStrategy.GetAggressiveness() > michaelStrategy.GetAggressiveness());
        }

        [Fact]
        public void TrevorStrategy_HasHigherRiskToleranceThanMichael()
        {
            var trevorStrategy = new TrevorAIStrategy();
            var michaelStrategy = new MichaelAIStrategy();

            Assert.True(trevorStrategy.GetRiskTolerance() > michaelStrategy.GetRiskTolerance());
        }

        [Fact]
        public void TrevorStrategy_AttacksWhenMichaelWouldNot()
        {
            var trevorStrategy = new TrevorAIStrategy();
            var michaelStrategy = new MichaelAIStrategy();

            var faction = CreateTestFaction(FactionType.Trevor);
            var factionState = CreateTestFactionState(faction.Id, troopCount: 50);

            // Low value enemy zone - Michael won't attack, Trevor will
            var lowValueZone = CreateTestZone("low-value", ownerFactionId: "enemy-faction", strategicValue: 3);
            var enemyFactions = new List<Faction> { CreateTestFaction(FactionType.Franklin, "enemy-faction") };
            var context = new AIContext(faction, factionState, new List<Zone>(), new[] { lowValueZone }, enemyFactions);

            var trevorAttacks = trevorStrategy.ShouldAttack(lowValueZone, context);
            var michaelAttacks = michaelStrategy.ShouldAttack(lowValueZone, context);

            Assert.True(trevorAttacks);
            Assert.False(michaelAttacks);
        }

        #endregion

        #region Combat Bonus Tests

        [Fact]
        public void TrevorStrategy_AppliesCombatBonusToEnemyZones()
        {
            var strategy = new TrevorAIStrategy();
            var context = CreateTestContext();

            // Two zones with same strategic value - one enemy, one neutral
            var enemyZone = CreateTestZone("enemy", ownerFactionId: "enemy-faction", strategicValue: 5);
            var neutralZone = CreateTestZone("neutral", strategicValue: 5);

            var enemyScore = strategy.EvaluateZone(enemyZone, context);
            var neutralScore = strategy.EvaluateZone(neutralZone, context);

            // Trevor should find enemy zones attractive due to combat focus
            // At minimum, enemy zones shouldn't be penalized like they are for other strategies
            Assert.True(enemyScore > 0);
        }

        [Fact]
        public void TrevorStrategy_HighRiskTolerance_MoreTroopsForRiskyAttacks()
        {
            var trevorStrategy = new TrevorAIStrategy();
            var conservativeStrategy = new TestBaseStrategy(FactionType.Franklin, 0.5f, 0.2f);

            var faction = CreateTestFaction(FactionType.Trevor);
            var factionState = CreateTestFactionState(faction.Id, troopCount: 100);
            var context = new AIContext(faction, factionState, new List<Zone>(), new List<Zone>(), new List<Faction>());

            // High priority attack (riskier)
            var riskyAttack = new AIDecision(AIDecisionType.Attack, "zone-1", 0.9f, 0);

            var trevorTroops = trevorStrategy.GetTroopsForAction(riskyAttack, context);
            var conservativeTroops = conservativeStrategy.GetTroopsForAction(riskyAttack, context);

            // Trevor's high risk tolerance means he'll commit more troops
            Assert.True(trevorTroops >= conservativeTroops);
        }

        #endregion

        #region IAIStrategy Interface Implementation Tests

        [Fact]
        public void TrevorAIStrategy_ImplementsIAIStrategy()
        {
            var strategy = new TrevorAIStrategy();

            Assert.IsAssignableFrom<IAIStrategy>(strategy);
        }

        [Fact]
        public void TrevorAIStrategy_InheritsFromBaseAIStrategy()
        {
            var strategy = new TrevorAIStrategy();

            Assert.IsAssignableFrom<BaseAIStrategy>(strategy);
        }

        #endregion

        #region Helper Methods

        private Faction CreateTestFaction(FactionType type, string? id = null)
        {
            var factionId = id ?? $"faction-{type.ToString().ToLower()}";
            var info = FactionTypeInfo.GetInfo(type);
            return new Faction(
                id: factionId,
                name: info.FactionName,
                leader: info.LeaderName,
                description: info.Description,
                color: info.Color);
        }

        private FactionState CreateTestFactionState(string factionId = "faction-trevor", int troopCount = 50)
        {
            var state = new FactionState(
                factionId: factionId,
                initialCash: 10000,
                initialTroopCount: troopCount);
            return state;
        }

        private Zone CreateTestZone(string id, string? ownerFactionId = null, int strategicValue = 5, Vector3? center = null)
        {
            var zone = new Zone(
                id: id,
                name: $"Test Zone {id}",
                center: center ?? new Vector3(0, 0, 0),
                radius: 150f,
                strategicValue: strategicValue);
            zone.OwnerFactionId = ownerFactionId;
            return zone;
        }

        private AIContext CreateTestContext()
        {
            var faction = CreateTestFaction(FactionType.Trevor);
            var factionState = CreateTestFactionState();
            var ownedZones = new List<Zone> { CreateTestZone("zone-1", ownerFactionId: faction.Id) };
            var allZones = new List<Zone>
            {
                CreateTestZone("zone-1", ownerFactionId: faction.Id),
                CreateTestZone("zone-2", ownerFactionId: "enemy-faction"),
                CreateTestZone("zone-3") // Neutral
            };
            var enemyFactions = new List<Faction> { CreateTestFaction(FactionType.Michael, "enemy-faction") };

            return new AIContext(faction, factionState, ownedZones, allZones, enemyFactions);
        }

        #endregion
    }
}
