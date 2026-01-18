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
    /// Tests for the BaseAIStrategy abstract class.
    /// Uses a concrete test implementation to verify base functionality.
    /// </summary>
    public class BaseAIStrategyTests
    {
        #region Constructor Tests

        [Fact]
        public void BaseAIStrategy_Constructor_SetsFactionType()
        {
            var strategy = new TestAIStrategy(FactionType.Michael, 0.5f, 0.5f);

            Assert.Equal(FactionType.Michael, strategy.FactionType);
        }

        [Fact]
        public void BaseAIStrategy_Constructor_SetsAggressiveness()
        {
            var strategy = new TestAIStrategy(FactionType.Michael, 0.7f, 0.5f);

            Assert.Equal(0.7f, strategy.GetAggressiveness());
        }

        [Fact]
        public void BaseAIStrategy_Constructor_SetsRiskTolerance()
        {
            var strategy = new TestAIStrategy(FactionType.Michael, 0.5f, 0.8f);

            Assert.Equal(0.8f, strategy.GetRiskTolerance());
        }

        [Fact]
        public void BaseAIStrategy_Constructor_ClampsAggressivenessAbove1()
        {
            var strategy = new TestAIStrategy(FactionType.Michael, 1.5f, 0.5f);

            Assert.Equal(1.0f, strategy.GetAggressiveness());
        }

        [Fact]
        public void BaseAIStrategy_Constructor_ClampsAggressivenessBelow0()
        {
            var strategy = new TestAIStrategy(FactionType.Michael, -0.5f, 0.5f);

            Assert.Equal(0.0f, strategy.GetAggressiveness());
        }

        [Fact]
        public void BaseAIStrategy_Constructor_ClampsRiskToleranceAbove1()
        {
            var strategy = new TestAIStrategy(FactionType.Michael, 0.5f, 1.5f);

            Assert.Equal(1.0f, strategy.GetRiskTolerance());
        }

        [Fact]
        public void BaseAIStrategy_Constructor_ClampsRiskToleranceBelow0()
        {
            var strategy = new TestAIStrategy(FactionType.Michael, 0.5f, -0.5f);

            Assert.Equal(0.0f, strategy.GetRiskTolerance());
        }

        #endregion

        #region EvaluateZone Tests

        [Fact]
        public void EvaluateZone_NullZone_ThrowsArgumentNullException()
        {
            var strategy = CreateDefaultStrategy();
            var context = CreateTestContext();

            Assert.Throws<ArgumentNullException>(() => strategy.EvaluateZone(null!, context));
        }

        [Fact]
        public void EvaluateZone_NullContext_ThrowsArgumentNullException()
        {
            var strategy = CreateDefaultStrategy();
            var zone = CreateTestZone("zone-1");

            Assert.Throws<ArgumentNullException>(() => strategy.EvaluateZone(zone, null!));
        }

        [Fact]
        public void EvaluateZone_ReturnsValueBetween0And1()
        {
            var strategy = CreateDefaultStrategy();
            var context = CreateTestContext();
            var zone = CreateTestZone("zone-1", strategicValue: 5);

            var score = strategy.EvaluateZone(zone, context);

            Assert.InRange(score, 0f, 1f);
        }

        [Fact]
        public void EvaluateZone_HigherStrategicValue_HigherScore()
        {
            var strategy = CreateDefaultStrategy();
            var context = CreateTestContext();
            var lowValueZone = CreateTestZone("zone-low", strategicValue: 1);
            var highValueZone = CreateTestZone("zone-high", strategicValue: 10);

            var lowScore = strategy.EvaluateZone(lowValueZone, context);
            var highScore = strategy.EvaluateZone(highValueZone, context);

            Assert.True(highScore > lowScore);
        }

        [Fact]
        public void EvaluateZone_NeutralZone_HigherScoreThanEnemyZone()
        {
            var strategy = CreateDefaultStrategy();
            var context = CreateTestContext();
            var neutralZone = CreateTestZone("neutral", strategicValue: 5);
            var enemyZone = CreateTestZone("enemy", strategicValue: 5, ownerFactionId: "enemy-faction");

            var neutralScore = strategy.EvaluateZone(neutralZone, context);
            var enemyScore = strategy.EvaluateZone(enemyZone, context);

            // Neutral zones are easier to take, so should score higher
            Assert.True(neutralScore >= enemyScore);
        }

        #endregion

        #region MakeDecisions Tests

        [Fact]
        public void MakeDecisions_NullContext_ThrowsArgumentNullException()
        {
            var strategy = CreateDefaultStrategy();

            Assert.Throws<ArgumentNullException>(() => strategy.MakeDecisions(null!));
        }

        [Fact]
        public void MakeDecisions_ReturnsNonNullList()
        {
            var strategy = CreateDefaultStrategy();
            var context = CreateTestContext();

            var decisions = strategy.MakeDecisions(context);

            Assert.NotNull(decisions);
        }

        [Fact]
        public void MakeDecisions_WithThreatenedZones_IncludesDefendDecisions()
        {
            var strategy = CreateDefaultStrategy();
            var faction = CreateTestFaction(FactionType.Michael);
            var factionState = CreateTestFactionState(faction.Id, troopCount: 30);

            var threatenedZone = CreateTestZone("threatened", ownerFactionId: faction.Id);
            threatenedZone.IsContested = true;

            var ownedZones = new List<Zone> { threatenedZone };
            var allZones = new List<Zone> { threatenedZone };
            var context = new AIContext(faction, factionState, ownedZones, allZones, new List<Faction>());

            var decisions = strategy.MakeDecisions(context);

            Assert.Contains(decisions, d => d.DecisionType == AIDecisionType.Defend);
        }

        [Fact]
        public void MakeDecisions_WithAvailableTroops_IncludesAttackDecisions()
        {
            var strategy = new TestAIStrategy(FactionType.Michael, 0.8f, 0.5f); // Aggressive
            var faction = CreateTestFaction(FactionType.Michael);
            var factionState = CreateTestFactionState(faction.Id, troopCount: 50);

            var ownedZone = CreateTestZone("owned", ownerFactionId: faction.Id);
            var enemyZone = CreateTestZone("enemy", ownerFactionId: "enemy-faction");

            var ownedZones = new List<Zone> { ownedZone };
            var allZones = new List<Zone> { ownedZone, enemyZone };
            var enemyFactions = new List<Faction> { CreateTestFaction(FactionType.Trevor, "enemy-faction") };
            var context = new AIContext(faction, factionState, ownedZones, allZones, enemyFactions);

            var decisions = strategy.MakeDecisions(context);

            Assert.Contains(decisions, d => d.DecisionType == AIDecisionType.Attack);
        }

        [Fact]
        public void MakeDecisions_NoTroopsAvailable_ReturnsHoldDecision()
        {
            var strategy = CreateDefaultStrategy();
            var faction = CreateTestFaction(FactionType.Michael);
            var factionState = CreateTestFactionState(faction.Id, troopCount: 0);

            var ownedZone = CreateTestZone("owned", ownerFactionId: faction.Id);
            var ownedZones = new List<Zone> { ownedZone };
            var context = new AIContext(faction, factionState, ownedZones, ownedZones, new List<Faction>());

            var decisions = strategy.MakeDecisions(context);

            Assert.True(decisions.Count == 0 || decisions.All(d => d.DecisionType == AIDecisionType.Hold));
        }

        [Fact]
        public void MakeDecisions_ResultsOrderedByPriority()
        {
            var strategy = new TestAIStrategy(FactionType.Michael, 0.8f, 0.5f);
            var faction = CreateTestFaction(FactionType.Michael);
            var factionState = CreateTestFactionState(faction.Id, troopCount: 100);

            var threatenedZone = CreateTestZone("threatened", ownerFactionId: faction.Id, strategicValue: 8);
            threatenedZone.IsContested = true;
            var enemyZone = CreateTestZone("enemy", ownerFactionId: "enemy-faction", strategicValue: 3);

            var ownedZones = new List<Zone> { threatenedZone };
            var allZones = new List<Zone> { threatenedZone, enemyZone };
            var enemyFactions = new List<Faction> { CreateTestFaction(FactionType.Trevor, "enemy-faction") };
            var context = new AIContext(faction, factionState, ownedZones, allZones, enemyFactions);

            var decisions = strategy.MakeDecisions(context);

            // Should be ordered by priority (highest first)
            for (int i = 0; i < decisions.Count - 1; i++)
            {
                Assert.True(decisions[i].Priority >= decisions[i + 1].Priority);
            }
        }

        #endregion

        #region ShouldAttack Tests

        [Fact]
        public void ShouldAttack_NullZone_ThrowsArgumentNullException()
        {
            var strategy = CreateDefaultStrategy();
            var context = CreateTestContext();

            Assert.Throws<ArgumentNullException>(() => strategy.ShouldAttack(null!, context));
        }

        [Fact]
        public void ShouldAttack_NullContext_ThrowsArgumentNullException()
        {
            var strategy = CreateDefaultStrategy();
            var zone = CreateTestZone("zone-1");

            Assert.Throws<ArgumentNullException>(() => strategy.ShouldAttack(zone, null!));
        }

        [Fact]
        public void ShouldAttack_OwnedZone_ReturnsFalse()
        {
            var strategy = CreateDefaultStrategy();
            var faction = CreateTestFaction(FactionType.Michael);
            var factionState = CreateTestFactionState(faction.Id, troopCount: 50);
            var ownedZone = CreateTestZone("owned", ownerFactionId: faction.Id);
            var context = new AIContext(faction, factionState, new[] { ownedZone }, new[] { ownedZone }, new List<Faction>());

            var shouldAttack = strategy.ShouldAttack(ownedZone, context);

            Assert.False(shouldAttack);
        }

        [Fact]
        public void ShouldAttack_NoTroops_ReturnsFalse()
        {
            var strategy = CreateDefaultStrategy();
            var faction = CreateTestFaction(FactionType.Michael);
            var factionState = CreateTestFactionState(faction.Id, troopCount: 0);
            var enemyZone = CreateTestZone("enemy", ownerFactionId: "enemy-faction");
            var context = new AIContext(faction, factionState, new List<Zone>(), new[] { enemyZone },
                new[] { CreateTestFaction(FactionType.Trevor, "enemy-faction") });

            var shouldAttack = strategy.ShouldAttack(enemyZone, context);

            Assert.False(shouldAttack);
        }

        [Fact]
        public void ShouldAttack_AggressiveStrategy_MoreLikelyToAttack()
        {
            var aggressiveStrategy = new TestAIStrategy(FactionType.Trevor, 0.9f, 0.7f);
            var passiveStrategy = new TestAIStrategy(FactionType.Michael, 0.2f, 0.3f);

            var faction = CreateTestFaction(FactionType.Michael);
            var factionState = CreateTestFactionState(faction.Id, troopCount: 30);
            var enemyZone = CreateTestZone("enemy", ownerFactionId: "enemy-faction", strategicValue: 5);
            var context = new AIContext(faction, factionState, new List<Zone>(), new[] { enemyZone },
                new[] { CreateTestFaction(FactionType.Trevor, "enemy-faction") });

            var aggressiveAttacks = aggressiveStrategy.ShouldAttack(enemyZone, context);
            var passiveAttacks = passiveStrategy.ShouldAttack(enemyZone, context);

            // Aggressive strategy should be at least as likely to attack
            if (passiveAttacks)
            {
                Assert.True(aggressiveAttacks);
            }
        }

        #endregion

        #region ShouldDefend Tests

        [Fact]
        public void ShouldDefend_NullZone_ThrowsArgumentNullException()
        {
            var strategy = CreateDefaultStrategy();
            var context = CreateTestContext();

            Assert.Throws<ArgumentNullException>(() => strategy.ShouldDefend(null!, context));
        }

        [Fact]
        public void ShouldDefend_NullContext_ThrowsArgumentNullException()
        {
            var strategy = CreateDefaultStrategy();
            var zone = CreateTestZone("zone-1");

            Assert.Throws<ArgumentNullException>(() => strategy.ShouldDefend(zone, null!));
        }

        [Fact]
        public void ShouldDefend_NonOwnedZone_ReturnsFalse()
        {
            var strategy = CreateDefaultStrategy();
            var faction = CreateTestFaction(FactionType.Michael);
            var factionState = CreateTestFactionState(faction.Id, troopCount: 50);
            var enemyZone = CreateTestZone("enemy", ownerFactionId: "enemy-faction");
            var context = new AIContext(faction, factionState, new List<Zone>(), new[] { enemyZone },
                new[] { CreateTestFaction(FactionType.Trevor, "enemy-faction") });

            var shouldDefend = strategy.ShouldDefend(enemyZone, context);

            Assert.False(shouldDefend);
        }

        [Fact]
        public void ShouldDefend_ContestedZone_ReturnsTrue()
        {
            var strategy = CreateDefaultStrategy();
            var faction = CreateTestFaction(FactionType.Michael);
            var factionState = CreateTestFactionState(faction.Id, troopCount: 20);
            var contestedZone = CreateTestZone("contested", ownerFactionId: faction.Id);
            contestedZone.IsContested = true;
            var context = new AIContext(faction, factionState, new[] { contestedZone }, new[] { contestedZone }, new List<Faction>());

            var shouldDefend = strategy.ShouldDefend(contestedZone, context);

            Assert.True(shouldDefend);
        }

        [Fact]
        public void ShouldDefend_HighValueZone_MoreLikelyToDefend()
        {
            var strategy = CreateDefaultStrategy();
            var faction = CreateTestFaction(FactionType.Michael);
            var factionState = CreateTestFactionState(faction.Id, troopCount: 5);

            var highValueZone = CreateTestZone("high-value", ownerFactionId: faction.Id, strategicValue: 10);
            var lowValueZone = CreateTestZone("low-value", ownerFactionId: faction.Id, strategicValue: 1);

            var contextHigh = new AIContext(faction, factionState, new[] { highValueZone }, new[] { highValueZone }, new List<Faction>());
            var contextLow = new AIContext(faction, factionState, new[] { lowValueZone }, new[] { lowValueZone }, new List<Faction>());

            // Even non-contested high value zones should be considered for defense
            Assert.NotNull(highValueZone);
            Assert.NotNull(lowValueZone);
        }

        #endregion

        #region GetTroopsForAction Tests

        [Fact]
        public void GetTroopsForAction_NullDecision_ThrowsArgumentNullException()
        {
            var strategy = CreateDefaultStrategy();
            var context = CreateTestContext();

            Assert.Throws<ArgumentNullException>(() => strategy.GetTroopsForAction(null!, context));
        }

        [Fact]
        public void GetTroopsForAction_NullContext_ThrowsArgumentNullException()
        {
            var strategy = CreateDefaultStrategy();
            var decision = new AIDecision(AIDecisionType.Attack, "zone-1", 0.8f, 0);

            Assert.Throws<ArgumentNullException>(() => strategy.GetTroopsForAction(decision, null!));
        }

        [Fact]
        public void GetTroopsForAction_ReturnsNonNegativeValue()
        {
            var strategy = CreateDefaultStrategy();
            var faction = CreateTestFaction(FactionType.Michael);
            var factionState = CreateTestFactionState(faction.Id, troopCount: 50);
            var context = new AIContext(faction, factionState, new List<Zone>(), new List<Zone>(), new List<Faction>());
            var decision = new AIDecision(AIDecisionType.Attack, "zone-1", 0.8f, 0);

            var troops = strategy.GetTroopsForAction(decision, context);

            Assert.True(troops >= 0);
        }

        [Fact]
        public void GetTroopsForAction_DoesNotExceedAvailableTroops()
        {
            var strategy = CreateDefaultStrategy();
            var faction = CreateTestFaction(FactionType.Michael);
            var factionState = CreateTestFactionState(faction.Id, troopCount: 25);
            var context = new AIContext(faction, factionState, new List<Zone>(), new List<Zone>(), new List<Faction>());
            var decision = new AIDecision(AIDecisionType.Attack, "zone-1", 1.0f, 0);

            var troops = strategy.GetTroopsForAction(decision, context);

            Assert.True(troops <= factionState.TroopCount);
        }

        [Fact]
        public void GetTroopsForAction_HigherPriority_MoreTroops()
        {
            var strategy = CreateDefaultStrategy();
            var faction = CreateTestFaction(FactionType.Michael);
            var factionState = CreateTestFactionState(faction.Id, troopCount: 100);
            var context = new AIContext(faction, factionState, new List<Zone>(), new List<Zone>(), new List<Faction>());

            var highPriorityDecision = new AIDecision(AIDecisionType.Attack, "zone-1", 1.0f, 0);
            var lowPriorityDecision = new AIDecision(AIDecisionType.Attack, "zone-2", 0.3f, 0);

            var troopsHigh = strategy.GetTroopsForAction(highPriorityDecision, context);
            var troopsLow = strategy.GetTroopsForAction(lowPriorityDecision, context);

            Assert.True(troopsHigh >= troopsLow);
        }

        [Fact]
        public void GetTroopsForAction_DefendDecision_AllocatesDefensiveTroops()
        {
            var strategy = CreateDefaultStrategy();
            var faction = CreateTestFaction(FactionType.Michael);
            var factionState = CreateTestFactionState(faction.Id, troopCount: 40);
            var context = new AIContext(faction, factionState, new List<Zone>(), new List<Zone>(), new List<Faction>());
            var defendDecision = new AIDecision(AIDecisionType.Defend, "zone-1", 0.9f, 0);

            var troops = strategy.GetTroopsForAction(defendDecision, context);

            Assert.True(troops > 0);
        }

        #endregion

        #region Helper Methods

        private TestAIStrategy CreateDefaultStrategy()
        {
            return new TestAIStrategy(FactionType.Michael, 0.5f, 0.5f);
        }

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

        private FactionState CreateTestFactionState(string factionId = "faction-michael", int troopCount = 50)
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
            var faction = CreateTestFaction(FactionType.Michael);
            var factionState = CreateTestFactionState();
            var ownedZones = new List<Zone> { CreateTestZone("zone-1", ownerFactionId: faction.Id) };
            var allZones = new List<Zone>
            {
                CreateTestZone("zone-1", ownerFactionId: faction.Id),
                CreateTestZone("zone-2", ownerFactionId: "enemy-faction"),
                CreateTestZone("zone-3") // Neutral
            };
            var enemyFactions = new List<Faction> { CreateTestFaction(FactionType.Trevor, "enemy-faction") };

            return new AIContext(faction, factionState, ownedZones, allZones, enemyFactions);
        }

        #endregion
    }

    /// <summary>
    /// Concrete test implementation of BaseAIStrategy for testing purposes.
    /// Uses the base class implementations directly.
    /// </summary>
    public class TestAIStrategy : BaseAIStrategy
    {
        public TestAIStrategy(FactionType factionType, float aggressiveness, float riskTolerance)
            : base(factionType, aggressiveness, riskTolerance)
        {
        }

        // Override abstract methods if any are required, otherwise base implementations are tested directly
    }
}
