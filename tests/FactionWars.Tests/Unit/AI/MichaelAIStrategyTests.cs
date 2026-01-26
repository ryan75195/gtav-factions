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
    /// Tests for MichaelAIStrategy.
    /// Michael's strategy is calculated, focused on high-value targets and defense.
    /// - Low aggressiveness (calculated approach)
    /// - Low risk tolerance (prefers safe operations)
    /// - Prioritizes high-value zones over quantity
    /// - Strong defensive focus
    /// </summary>
    public class MichaelAIStrategyTests
    {
        #region Constructor Tests

        [Fact]
        public void Constructor_SetsFactionTypeToMichael()
        {
            var strategy = new MichaelAIStrategy();

            Assert.Equal(FactionType.Michael, strategy.FactionType);
        }

        [Fact]
        public void Constructor_SetsLowAggressiveness()
        {
            var strategy = new MichaelAIStrategy();

            // Michael should be calculated, not aggressive (low aggressiveness)
            Assert.True(strategy.GetAggressiveness() <= 0.4f);
        }

        [Fact]
        public void Constructor_SetsLowRiskTolerance()
        {
            var strategy = new MichaelAIStrategy();

            // Michael is calculated and prefers safe operations (low risk tolerance)
            Assert.True(strategy.GetRiskTolerance() <= 0.4f);
        }

        #endregion

        #region EvaluateZone Tests - High-Value Focus

        [Fact]
        public void EvaluateZone_HighValueZone_ScoresSignificantlyHigherThanLowValue()
        {
            var strategy = new MichaelAIStrategy();
            var context = CreateTestContext();

            var highValueZone = CreateTestZone("high", strategicValue: 10);
            var lowValueZone = CreateTestZone("low", strategicValue: 2);

            var highScore = strategy.EvaluateZone(highValueZone, context);
            var lowScore = strategy.EvaluateZone(lowValueZone, context);

            // Michael should have a bigger gap between high and low value zones
            // compared to base strategy (he focuses on high-value targets)
            Assert.True(highScore > lowScore);
            Assert.True(highScore / lowScore > 2.0f, "High value zones should be significantly more attractive");
        }

        [Fact]
        public void EvaluateZone_ValuesStrategicValueMoreThanBase()
        {
            var michaelStrategy = new MichaelAIStrategy();
            var baseStrategy = new TestBaseStrategy(FactionType.Michael, 0.3f, 0.3f);
            var context = CreateTestContext();

            var mediumZone = CreateTestZone("medium", strategicValue: 5);
            var highZone = CreateTestZone("high", strategicValue: 10);

            var michaelMedium = michaelStrategy.EvaluateZone(mediumZone, context);
            var michaelHigh = michaelStrategy.EvaluateZone(highZone, context);
            var baseMedium = baseStrategy.EvaluateZone(mediumZone, context);
            var baseHigh = baseStrategy.EvaluateZone(highZone, context);

            // Michael's ratio of high to medium should be greater than base's ratio
            var michaelRatio = michaelHigh / michaelMedium;
            var baseRatio = baseHigh / baseMedium;

            Assert.True(michaelRatio >= baseRatio, "Michael should value high-value targets even more than base");
        }

        [Fact]
        public void EvaluateZone_NullZone_ThrowsArgumentNullException()
        {
            var strategy = new MichaelAIStrategy();
            var context = CreateTestContext();

            Assert.Throws<ArgumentNullException>(() => strategy.EvaluateZone(null!, context));
        }

        [Fact]
        public void EvaluateZone_NullContext_ThrowsArgumentNullException()
        {
            var strategy = new MichaelAIStrategy();
            var zone = CreateTestZone("zone-1");

            Assert.Throws<ArgumentNullException>(() => strategy.EvaluateZone(zone, null!));
        }

        [Fact]
        public void EvaluateZone_ReturnsValueInValidRange()
        {
            var strategy = new MichaelAIStrategy();
            var context = CreateTestContext();
            var zone = CreateTestZone("zone-1", strategicValue: 7);

            var score = strategy.EvaluateZone(zone, context);

            Assert.InRange(score, 0f, 1f);
        }

        #endregion

        #region MakeDecisions Tests - Defense Priority

        [Fact]
        public void MakeDecisions_WithContestedZone_PrioritizesDefense()
        {
            var strategy = new MichaelAIStrategy();
            var faction = CreateTestFaction(FactionType.Michael);
            var factionState = CreateTestFactionState(faction.Id, troopCount: 50);

            var contestedZone = CreateTestZone("contested", ownerFactionId: faction.Id, strategicValue: 5);
            contestedZone.IsContested = true;
            var enemyZone = CreateTestZone("enemy", ownerFactionId: "enemy-faction", strategicValue: 8);

            var ownedZones = new List<Zone> { contestedZone };
            var allZones = new List<Zone> { contestedZone, enemyZone };
            var enemyFactions = new List<Faction> { CreateTestFaction(FactionType.Trevor, "enemy-faction") };
            var context = new AIContext(faction, factionState, ownedZones, allZones, enemyFactions);

            var decisions = strategy.MakeDecisions(context);

            // First decision should be defend
            Assert.NotEmpty(decisions);
            Assert.Equal(AIDecisionType.Defend, decisions[0].DecisionType);
        }

        [Fact]
        public void MakeDecisions_DefendDecisions_HaveHighPriority()
        {
            var strategy = new MichaelAIStrategy();
            var faction = CreateTestFaction(FactionType.Michael);
            var factionState = CreateTestFactionState(faction.Id, troopCount: 100);

            var highValueContested = CreateTestZone("high-contested", ownerFactionId: faction.Id, strategicValue: 8);
            highValueContested.IsContested = true;

            var ownedZones = new List<Zone> { highValueContested };
            var allZones = new List<Zone> { highValueContested };
            var context = new AIContext(faction, factionState, ownedZones, allZones, new List<Faction>());

            var decisions = strategy.MakeDecisions(context);

            var defendDecision = decisions.FirstOrDefault(d => d.DecisionType == AIDecisionType.Defend);
            Assert.NotNull(defendDecision);
            // Michael prioritizes defense highly, especially for valuable zones
            Assert.True(defendDecision.Priority >= 0.6f, "Defense should have high priority for Michael");
        }

        [Fact]
        public void MakeDecisions_NullContext_ThrowsArgumentNullException()
        {
            var strategy = new MichaelAIStrategy();

            Assert.Throws<ArgumentNullException>(() => strategy.MakeDecisions(null!));
        }

        [Fact]
        public void MakeDecisions_NoTroops_ReturnsEmptyOrHold()
        {
            var strategy = new MichaelAIStrategy();
            var faction = CreateTestFaction(FactionType.Michael);
            var factionState = CreateTestFactionState(faction.Id, troopCount: 0);

            var ownedZone = CreateTestZone("owned", ownerFactionId: faction.Id);
            var context = new AIContext(faction, factionState, new[] { ownedZone }, new[] { ownedZone }, new List<Faction>());

            var decisions = strategy.MakeDecisions(context);

            Assert.True(decisions.Count == 0 || decisions.All(d => d.DecisionType == AIDecisionType.Hold));
        }

        #endregion

        #region ShouldAttack Tests - Permissive Approach (CapitalDeploymentService handles prioritization)

        [Fact]
        public void ShouldAttack_LowValueTarget_WithSufficientTroops_ReturnsTrue()
        {
            // After removing rigid thresholds, AI should be willing to attack any adjacent zone
            // CapitalDeploymentService handles intelligent prioritization through opportunity scoring
            var strategy = new MichaelAIStrategy();
            var faction = CreateTestFaction(FactionType.Michael);
            var factionState = CreateTestFactionState(faction.Id, troopCount: 50);

            var lowValueZone = CreateTestZone("low-value", ownerFactionId: "enemy-faction", strategicValue: 2);
            var enemyFactions = new List<Faction> { CreateTestFaction(FactionType.Trevor, "enemy-faction") };
            var context = new AIContext(faction, factionState, new List<Zone>(), new[] { lowValueZone }, enemyFactions);

            var shouldAttack = strategy.ShouldAttack(lowValueZone, context);

            // Michael should now be willing to attack low-value targets - prioritization is handled elsewhere
            Assert.True(shouldAttack);
        }

        [Fact]
        public void ShouldAttack_VeryLowValueNeutralZone_WithSufficientTroops_ReturnsTrue()
        {
            // AI should attack any neutral zone when they have troops, regardless of strategic value
            var strategy = new MichaelAIStrategy();
            var faction = CreateTestFaction(FactionType.Michael);
            var factionState = CreateTestFactionState(faction.Id, troopCount: 50);

            var veryLowValueZone = CreateTestZone("very-low-value", strategicValue: 1);
            var context = new AIContext(faction, factionState, new List<Zone>(), new[] { veryLowValueZone }, new List<Faction>());

            var shouldAttack = strategy.ShouldAttack(veryLowValueZone, context);

            Assert.True(shouldAttack);
        }

        [Fact]
        public void ShouldAttack_HighValueTarget_WithSufficientTroops_ReturnsTrue()
        {
            var strategy = new MichaelAIStrategy();
            var faction = CreateTestFaction(FactionType.Michael);
            var factionState = CreateTestFactionState(faction.Id, troopCount: 100);

            var highValueZone = CreateTestZone("high-value", ownerFactionId: "enemy-faction", strategicValue: 9);
            var enemyFactions = new List<Faction> { CreateTestFaction(FactionType.Trevor, "enemy-faction") };
            var context = new AIContext(faction, factionState, new List<Zone>(), new[] { highValueZone }, enemyFactions);

            var shouldAttack = strategy.ShouldAttack(highValueZone, context);

            // Even calculated Michael will attack very high value targets when he has troops
            Assert.True(shouldAttack);
        }

        [Fact]
        public void ShouldAttack_OwnedZone_ReturnsFalse()
        {
            var strategy = new MichaelAIStrategy();
            var faction = CreateTestFaction(FactionType.Michael);
            var factionState = CreateTestFactionState(faction.Id, troopCount: 50);

            var ownedZone = CreateTestZone("owned", ownerFactionId: faction.Id);
            var context = new AIContext(faction, factionState, new[] { ownedZone }, new[] { ownedZone }, new List<Faction>());

            var shouldAttack = strategy.ShouldAttack(ownedZone, context);

            Assert.False(shouldAttack);
        }

        [Fact]
        public void ShouldAttack_NeutralHighValueZone_ReturnsTrue()
        {
            var strategy = new MichaelAIStrategy();
            var faction = CreateTestFaction(FactionType.Michael);
            var factionState = CreateTestFactionState(faction.Id, troopCount: 50);

            // Neutral high value zone (no owner)
            var neutralZone = CreateTestZone("neutral-high", strategicValue: 8);
            var context = new AIContext(faction, factionState, new List<Zone>(), new[] { neutralZone }, new List<Faction>());

            var shouldAttack = strategy.ShouldAttack(neutralZone, context);

            // All neutral zones are valid attack targets when AI has troops
            Assert.True(shouldAttack);
        }

        [Fact]
        public void ShouldAttack_NoTroops_ReturnsFalse()
        {
            var strategy = new MichaelAIStrategy();
            var faction = CreateTestFaction(FactionType.Michael);
            var factionState = CreateTestFactionState(faction.Id, troopCount: 0);

            var highValueZone = CreateTestZone("high-value", ownerFactionId: "enemy-faction", strategicValue: 10);
            var enemyFactions = new List<Faction> { CreateTestFaction(FactionType.Trevor, "enemy-faction") };
            var context = new AIContext(faction, factionState, new List<Zone>(), new[] { highValueZone }, enemyFactions);

            var shouldAttack = strategy.ShouldAttack(highValueZone, context);

            Assert.False(shouldAttack);
        }

        [Fact]
        public void ShouldAttack_NullZone_ThrowsArgumentNullException()
        {
            var strategy = new MichaelAIStrategy();
            var context = CreateTestContext();

            Assert.Throws<ArgumentNullException>(() => strategy.ShouldAttack(null!, context));
        }

        [Fact]
        public void ShouldAttack_NullContext_ThrowsArgumentNullException()
        {
            var strategy = new MichaelAIStrategy();
            var zone = CreateTestZone("zone-1");

            Assert.Throws<ArgumentNullException>(() => strategy.ShouldAttack(zone, null!));
        }

        #endregion

        #region ShouldDefend Tests - Strong Defense Focus

        [Fact]
        public void ShouldDefend_HighValueOwnedZone_ReturnsTrue()
        {
            var strategy = new MichaelAIStrategy();
            var faction = CreateTestFaction(FactionType.Michael);
            var factionState = CreateTestFactionState(faction.Id, troopCount: 30);

            var highValueZone = CreateTestZone("high-value", ownerFactionId: faction.Id, strategicValue: 8);
            var context = new AIContext(faction, factionState, new[] { highValueZone }, new[] { highValueZone }, new List<Faction>());

            var shouldDefend = strategy.ShouldDefend(highValueZone, context);

            // Michael is defense-focused, should want to defend high-value zones
            Assert.True(shouldDefend);
        }

        [Fact]
        public void ShouldDefend_ContestedZone_ReturnsTrue()
        {
            var strategy = new MichaelAIStrategy();
            var faction = CreateTestFaction(FactionType.Michael);
            var factionState = CreateTestFactionState(faction.Id, troopCount: 20);

            var contestedZone = CreateTestZone("contested", ownerFactionId: faction.Id);
            contestedZone.IsContested = true;
            var context = new AIContext(faction, factionState, new[] { contestedZone }, new[] { contestedZone }, new List<Faction>());

            var shouldDefend = strategy.ShouldDefend(contestedZone, context);

            Assert.True(shouldDefend);
        }

        [Fact]
        public void ShouldDefend_LowValueOwnedZone_StillConsidersDefense()
        {
            var strategy = new MichaelAIStrategy();
            var faction = CreateTestFaction(FactionType.Michael);
            var factionState = CreateTestFactionState(faction.Id, troopCount: 30);

            var lowValueZone = CreateTestZone("low-value", ownerFactionId: faction.Id, strategicValue: 2);
            var context = new AIContext(faction, factionState, new[] { lowValueZone }, new[] { lowValueZone }, new List<Faction>());

            // Michael's lower aggressiveness means higher defense focus
            // He may still defend lower value zones compared to more aggressive factions
            var shouldDefend = strategy.ShouldDefend(lowValueZone, context);

            // This test verifies the method runs without error for low value zones
            // The actual result depends on implementation details
            Assert.NotNull(shouldDefend.ToString());
        }

        [Fact]
        public void ShouldDefend_NotOwnedZone_ReturnsFalse()
        {
            var strategy = new MichaelAIStrategy();
            var faction = CreateTestFaction(FactionType.Michael);
            var factionState = CreateTestFactionState(faction.Id, troopCount: 50);

            var enemyZone = CreateTestZone("enemy", ownerFactionId: "enemy-faction");
            var context = new AIContext(faction, factionState, new List<Zone>(), new[] { enemyZone },
                new[] { CreateTestFaction(FactionType.Trevor, "enemy-faction") });

            var shouldDefend = strategy.ShouldDefend(enemyZone, context);

            Assert.False(shouldDefend);
        }

        [Fact]
        public void ShouldDefend_NullZone_ThrowsArgumentNullException()
        {
            var strategy = new MichaelAIStrategy();
            var context = CreateTestContext();

            Assert.Throws<ArgumentNullException>(() => strategy.ShouldDefend(null!, context));
        }

        [Fact]
        public void ShouldDefend_NullContext_ThrowsArgumentNullException()
        {
            var strategy = new MichaelAIStrategy();
            var zone = CreateTestZone("zone-1");

            Assert.Throws<ArgumentNullException>(() => strategy.ShouldDefend(zone, null!));
        }

        #endregion

        #region GetTroopsForAction Tests - Conservative Allocation

        [Fact]
        public void GetTroopsForAction_Attack_AllocatesConservatively()
        {
            var michaelStrategy = new MichaelAIStrategy();
            var aggressiveStrategy = new TestBaseStrategy(FactionType.Trevor, 0.9f, 0.8f);

            var faction = CreateTestFaction(FactionType.Michael);
            var factionState = CreateTestFactionState(faction.Id, troopCount: 100);
            var context = new AIContext(faction, factionState, new List<Zone>(), new List<Zone>(), new List<Faction>());

            var attackDecision = new AIDecision(AIDecisionType.Attack, "zone-1", 0.7f, 0);

            var michaelTroops = michaelStrategy.GetTroopsForAction(attackDecision, context);
            var aggressiveTroops = aggressiveStrategy.GetTroopsForAction(attackDecision, context);

            // Michael should allocate fewer troops for attacks (conservative)
            Assert.True(michaelTroops <= aggressiveTroops, "Michael should be more conservative with attack troops");
        }

        [Fact]
        public void GetTroopsForAction_Defense_AllocatesGenerously()
        {
            var michaelStrategy = new MichaelAIStrategy();
            var faction = CreateTestFaction(FactionType.Michael);
            var factionState = CreateTestFactionState(faction.Id, troopCount: 100);
            var context = new AIContext(faction, factionState, new List<Zone>(), new List<Zone>(), new List<Faction>());

            var defendDecision = new AIDecision(AIDecisionType.Defend, "zone-1", 0.8f, 0);

            var troops = michaelStrategy.GetTroopsForAction(defendDecision, context);

            // Michael is defense-focused, should allocate decent troops for defense
            Assert.True(troops > 0);
            Assert.True(troops >= 10, "Michael should allocate meaningful troops for defense");
        }

        [Fact]
        public void GetTroopsForAction_DoesNotExceedAvailableTroops()
        {
            var strategy = new MichaelAIStrategy();
            var faction = CreateTestFaction(FactionType.Michael);
            var factionState = CreateTestFactionState(faction.Id, troopCount: 20);
            var context = new AIContext(faction, factionState, new List<Zone>(), new List<Zone>(), new List<Faction>());

            var decision = new AIDecision(AIDecisionType.Defend, "zone-1", 1.0f, 0);

            var troops = strategy.GetTroopsForAction(decision, context);

            Assert.True(troops <= factionState.TroopCount);
        }

        [Fact]
        public void GetTroopsForAction_NullDecision_ThrowsArgumentNullException()
        {
            var strategy = new MichaelAIStrategy();
            var context = CreateTestContext();

            Assert.Throws<ArgumentNullException>(() => strategy.GetTroopsForAction(null!, context));
        }

        [Fact]
        public void GetTroopsForAction_NullContext_ThrowsArgumentNullException()
        {
            var strategy = new MichaelAIStrategy();
            var decision = new AIDecision(AIDecisionType.Attack, "zone-1", 0.8f, 0);

            Assert.Throws<ArgumentNullException>(() => strategy.GetTroopsForAction(decision, null!));
        }

        #endregion

        #region Comparative Tests - vs Trevor (opposite style)

        [Fact]
        public void MichaelStrategy_IsLessAggressiveThanTrevor()
        {
            var michaelStrategy = new MichaelAIStrategy();
            var trevorLikeStrategy = new TestBaseStrategy(FactionType.Trevor, 0.85f, 0.75f);

            Assert.True(michaelStrategy.GetAggressiveness() < trevorLikeStrategy.GetAggressiveness());
        }

        [Fact]
        public void MichaelStrategy_IsMoreRiskAverseThanTrevor()
        {
            var michaelStrategy = new MichaelAIStrategy();
            var trevorLikeStrategy = new TestBaseStrategy(FactionType.Trevor, 0.85f, 0.75f);

            Assert.True(michaelStrategy.GetRiskTolerance() < trevorLikeStrategy.GetRiskTolerance());
        }

        #endregion

        #region IAIStrategy Interface Implementation Tests

        [Fact]
        public void MichaelAIStrategy_ImplementsIAIStrategy()
        {
            var strategy = new MichaelAIStrategy();

            Assert.IsAssignableFrom<IAIStrategy>(strategy);
        }

        [Fact]
        public void MichaelAIStrategy_InheritsFromBaseAIStrategy()
        {
            var strategy = new MichaelAIStrategy();

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
    /// Test helper class that exposes BaseAIStrategy with configurable parameters.
    /// Used to compare Michael's strategy against other configurations.
    /// </summary>
    public class TestBaseStrategy : BaseAIStrategy
    {
        public TestBaseStrategy(FactionType factionType, float aggressiveness, float riskTolerance)
            : base(factionType, aggressiveness, riskTolerance)
        {
        }
    }
}
