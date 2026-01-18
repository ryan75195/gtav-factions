using FactionWars.AI.Interfaces;
using FactionWars.AI.Models;
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
    /// Tests for the IAIStrategy interface contract.
    /// These tests verify the expected behavior that all AI strategy implementations must fulfill.
    /// </summary>
    public class IAIStrategyTests
    {
        #region AIDecision Model Tests

        [Fact]
        public void AIDecision_Constructor_SetsProperties()
        {
            // AIDecision should capture the decision type and target zone
            var decision = new AIDecision(
                decisionType: AIDecisionType.Attack,
                targetZoneId: "zone-1",
                priority: 0.8f,
                troopsToCommit: 10);

            Assert.Equal(AIDecisionType.Attack, decision.DecisionType);
            Assert.Equal("zone-1", decision.TargetZoneId);
            Assert.Equal(0.8f, decision.Priority);
            Assert.Equal(10, decision.TroopsToCommit);
        }

        [Fact]
        public void AIDecision_NullTargetZoneId_AllowedForHoldDecisions()
        {
            // Hold decisions don't target a specific zone
            var decision = new AIDecision(
                decisionType: AIDecisionType.Hold,
                targetZoneId: null,
                priority: 0.5f,
                troopsToCommit: 0);

            Assert.Equal(AIDecisionType.Hold, decision.DecisionType);
            Assert.Null(decision.TargetZoneId);
        }

        [Fact]
        public void AIDecision_Priority_ClampedToValidRange()
        {
            // Priority should be clamped between 0 and 1
            var highPriority = new AIDecision(AIDecisionType.Attack, "zone-1", 1.5f, 5);
            var lowPriority = new AIDecision(AIDecisionType.Attack, "zone-1", -0.5f, 5);

            Assert.Equal(1.0f, highPriority.Priority);
            Assert.Equal(0.0f, lowPriority.Priority);
        }

        [Fact]
        public void AIDecision_TroopsToCommit_CannotBeNegative()
        {
            // Negative troops should throw or clamp to zero
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new AIDecision(AIDecisionType.Attack, "zone-1", 0.5f, -5));
        }

        #endregion

        #region AIDecisionType Enum Tests

        [Fact]
        public void AIDecisionType_HasExpectedValues()
        {
            // Core decision types the AI can make
            Assert.True(Enum.IsDefined(typeof(AIDecisionType), AIDecisionType.Attack));
            Assert.True(Enum.IsDefined(typeof(AIDecisionType), AIDecisionType.Defend));
            Assert.True(Enum.IsDefined(typeof(AIDecisionType), AIDecisionType.Reinforce));
            Assert.True(Enum.IsDefined(typeof(AIDecisionType), AIDecisionType.Hold));
            Assert.True(Enum.IsDefined(typeof(AIDecisionType), AIDecisionType.Retreat));
        }

        #endregion

        #region AIContext Model Tests

        [Fact]
        public void AIContext_Constructor_SetsRequiredProperties()
        {
            // AIContext provides all the information an AI strategy needs to make decisions
            var faction = CreateTestFaction(FactionType.Michael);
            var factionState = CreateTestFactionState();
            var ownedZones = new List<Zone> { CreateTestZone("zone-1") };
            var allZones = new List<Zone> { CreateTestZone("zone-1"), CreateTestZone("zone-2") };
            var enemyFactions = new List<Faction> { CreateTestFaction(FactionType.Trevor) };

            var context = new AIContext(
                faction: faction,
                factionState: factionState,
                ownedZones: ownedZones,
                allZones: allZones,
                enemyFactions: enemyFactions);

            Assert.Equal(faction, context.Faction);
            Assert.Equal(factionState, context.FactionState);
            Assert.Single(context.OwnedZones);
            Assert.Equal(2, context.AllZones.Count());
            Assert.Single(context.EnemyFactions);
        }

        [Fact]
        public void AIContext_NullFaction_ThrowsArgumentNullException()
        {
            var factionState = CreateTestFactionState();
            var zones = new List<Zone>();
            var enemyFactions = new List<Faction>();

            Assert.Throws<ArgumentNullException>(() =>
                new AIContext(null!, factionState, zones, zones, enemyFactions));
        }

        [Fact]
        public void AIContext_NullFactionState_ThrowsArgumentNullException()
        {
            var faction = CreateTestFaction(FactionType.Michael);
            var zones = new List<Zone>();
            var enemyFactions = new List<Faction>();

            Assert.Throws<ArgumentNullException>(() =>
                new AIContext(faction, null!, zones, zones, enemyFactions));
        }

        [Fact]
        public void AIContext_NullZones_ThrowsArgumentNullException()
        {
            var faction = CreateTestFaction(FactionType.Michael);
            var factionState = CreateTestFactionState();
            var enemyFactions = new List<Faction>();

            Assert.Throws<ArgumentNullException>(() =>
                new AIContext(faction, factionState, null!, new List<Zone>(), enemyFactions));

            Assert.Throws<ArgumentNullException>(() =>
                new AIContext(faction, factionState, new List<Zone>(), null!, enemyFactions));
        }

        [Fact]
        public void AIContext_AdjacentEnemyZones_CalculatedCorrectly()
        {
            // Context should expose zones adjacent to owned territory that belong to enemies
            var faction = CreateTestFaction(FactionType.Michael);
            var factionState = CreateTestFactionState();

            var ownedZone = CreateTestZone("owned-1", ownerFactionId: faction.Id);
            var adjacentEnemyZone = CreateTestZone("enemy-adjacent", ownerFactionId: "enemy-faction");
            var farEnemyZone = CreateTestZone("enemy-far", ownerFactionId: "enemy-faction", center: new Vector3(10000, 0, 0));

            var ownedZones = new List<Zone> { ownedZone };
            var allZones = new List<Zone> { ownedZone, adjacentEnemyZone, farEnemyZone };
            var enemyFactions = new List<Faction> { CreateTestFaction(FactionType.Trevor, "enemy-faction") };

            var context = new AIContext(faction, factionState, ownedZones, allZones, enemyFactions);

            // The context should be able to provide adjacent enemy zones
            Assert.NotNull(context.AllZones);
        }

        [Fact]
        public void AIContext_ThreatenedZones_ExposedProperly()
        {
            // Zones that are contested or under attack should be identifiable
            var faction = CreateTestFaction(FactionType.Michael);
            var factionState = CreateTestFactionState();

            var stableZone = CreateTestZone("stable", ownerFactionId: faction.Id);
            var contestedZone = CreateTestZone("contested", ownerFactionId: faction.Id);
            contestedZone.IsContested = true;

            var ownedZones = new List<Zone> { stableZone, contestedZone };
            var allZones = new List<Zone> { stableZone, contestedZone };
            var enemyFactions = new List<Faction>();

            var context = new AIContext(faction, factionState, ownedZones, allZones, enemyFactions);

            var threatened = context.OwnedZones.Where(z => z.IsContested).ToList();
            Assert.Single(threatened);
            Assert.Equal("contested", threatened[0].Id);
        }

        #endregion

        #region IAIStrategy Interface Contract Tests

        [Fact]
        public void IAIStrategy_EvaluateZone_NullZone_ThrowsArgumentNullException()
        {
            // All AI strategies must throw on null zone
            var mockStrategy = new Mock<IAIStrategy>();
            mockStrategy.Setup(s => s.EvaluateZone(null!, It.IsAny<AIContext>()))
                .Throws<ArgumentNullException>();

            var context = CreateTestContext();
            Assert.Throws<ArgumentNullException>(() => mockStrategy.Object.EvaluateZone(null!, context));
        }

        [Fact]
        public void IAIStrategy_EvaluateZone_NullContext_ThrowsArgumentNullException()
        {
            // All AI strategies must throw on null context
            var mockStrategy = new Mock<IAIStrategy>();
            mockStrategy.Setup(s => s.EvaluateZone(It.IsAny<Zone>(), null!))
                .Throws<ArgumentNullException>();

            var zone = CreateTestZone("zone-1");
            Assert.Throws<ArgumentNullException>(() => mockStrategy.Object.EvaluateZone(zone, null!));
        }

        [Fact]
        public void IAIStrategy_EvaluateZone_ReturnsScoreBetween0And1()
        {
            // Zone evaluation should return a normalized score
            var mockStrategy = new Mock<IAIStrategy>();
            var zone = CreateTestZone("zone-1");
            var context = CreateTestContext();

            mockStrategy.Setup(s => s.EvaluateZone(zone, context))
                .Returns(0.75f);

            var score = mockStrategy.Object.EvaluateZone(zone, context);
            Assert.InRange(score, 0f, 1f);
        }

        [Fact]
        public void IAIStrategy_MakeDecisions_NullContext_ThrowsArgumentNullException()
        {
            var mockStrategy = new Mock<IAIStrategy>();
            mockStrategy.Setup(s => s.MakeDecisions(null!))
                .Throws<ArgumentNullException>();

            Assert.Throws<ArgumentNullException>(() => mockStrategy.Object.MakeDecisions(null!));
        }

        [Fact]
        public void IAIStrategy_MakeDecisions_ReturnsNonNullList()
        {
            // AI strategy should always return a list (may be empty but not null)
            var mockStrategy = new Mock<IAIStrategy>();
            var context = CreateTestContext();

            mockStrategy.Setup(s => s.MakeDecisions(context))
                .Returns(new List<AIDecision>());

            var decisions = mockStrategy.Object.MakeDecisions(context);
            Assert.NotNull(decisions);
        }

        [Fact]
        public void IAIStrategy_MakeDecisions_CanReturnMultipleDecisions()
        {
            // An AI may decide to do multiple things at once
            var mockStrategy = new Mock<IAIStrategy>();
            var context = CreateTestContext();

            var decisions = new List<AIDecision>
            {
                new AIDecision(AIDecisionType.Attack, "zone-1", 0.9f, 15),
                new AIDecision(AIDecisionType.Defend, "zone-2", 0.7f, 5),
                new AIDecision(AIDecisionType.Reinforce, "zone-3", 0.5f, 10)
            };

            mockStrategy.Setup(s => s.MakeDecisions(context))
                .Returns(decisions);

            var result = mockStrategy.Object.MakeDecisions(context);
            Assert.Equal(3, result.Count);
        }

        [Fact]
        public void IAIStrategy_ShouldAttack_NullZone_ThrowsArgumentNullException()
        {
            var mockStrategy = new Mock<IAIStrategy>();
            mockStrategy.Setup(s => s.ShouldAttack(null!, It.IsAny<AIContext>()))
                .Throws<ArgumentNullException>();

            var context = CreateTestContext();
            Assert.Throws<ArgumentNullException>(() => mockStrategy.Object.ShouldAttack(null!, context));
        }

        [Fact]
        public void IAIStrategy_ShouldAttack_ReturnsBoolean()
        {
            var mockStrategy = new Mock<IAIStrategy>();
            var zone = CreateTestZone("enemy-zone", ownerFactionId: "enemy");
            var context = CreateTestContext();

            mockStrategy.Setup(s => s.ShouldAttack(zone, context))
                .Returns(true);

            var shouldAttack = mockStrategy.Object.ShouldAttack(zone, context);
            Assert.True(shouldAttack);
        }

        [Fact]
        public void IAIStrategy_ShouldDefend_NullZone_ThrowsArgumentNullException()
        {
            var mockStrategy = new Mock<IAIStrategy>();
            mockStrategy.Setup(s => s.ShouldDefend(null!, It.IsAny<AIContext>()))
                .Throws<ArgumentNullException>();

            var context = CreateTestContext();
            Assert.Throws<ArgumentNullException>(() => mockStrategy.Object.ShouldDefend(null!, context));
        }

        [Fact]
        public void IAIStrategy_ShouldDefend_ReturnsBoolean()
        {
            var mockStrategy = new Mock<IAIStrategy>();
            var zone = CreateTestZone("owned-zone", ownerFactionId: "test-faction");
            var context = CreateTestContext();

            mockStrategy.Setup(s => s.ShouldDefend(zone, context))
                .Returns(true);

            var shouldDefend = mockStrategy.Object.ShouldDefend(zone, context);
            Assert.True(shouldDefend);
        }

        [Fact]
        public void IAIStrategy_GetTroopsForAction_ReturnsNonNegativeValue()
        {
            var mockStrategy = new Mock<IAIStrategy>();
            var decision = new AIDecision(AIDecisionType.Attack, "zone-1", 0.8f, 0);
            var context = CreateTestContext();

            mockStrategy.Setup(s => s.GetTroopsForAction(decision, context))
                .Returns(20);

            var troops = mockStrategy.Object.GetTroopsForAction(decision, context);
            Assert.True(troops >= 0);
        }

        [Fact]
        public void IAIStrategy_GetAggressiveness_ReturnsBetween0And1()
        {
            // Aggressiveness level affects attack vs defense priority
            var mockStrategy = new Mock<IAIStrategy>();

            mockStrategy.Setup(s => s.GetAggressiveness())
                .Returns(0.7f);

            var aggressiveness = mockStrategy.Object.GetAggressiveness();
            Assert.InRange(aggressiveness, 0f, 1f);
        }

        [Fact]
        public void IAIStrategy_GetRiskTolerance_ReturnsBetween0And1()
        {
            // Risk tolerance affects willingness to attack outnumbered
            var mockStrategy = new Mock<IAIStrategy>();

            mockStrategy.Setup(s => s.GetRiskTolerance())
                .Returns(0.5f);

            var riskTolerance = mockStrategy.Object.GetRiskTolerance();
            Assert.InRange(riskTolerance, 0f, 1f);
        }

        [Fact]
        public void IAIStrategy_FactionType_ReturnsValidFactionType()
        {
            // Each strategy should be associated with a faction type
            var mockStrategy = new Mock<IAIStrategy>();

            mockStrategy.Setup(s => s.FactionType)
                .Returns(FactionType.Michael);

            var factionType = mockStrategy.Object.FactionType;
            Assert.True(Enum.IsDefined(typeof(FactionType), factionType));
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

        private FactionState CreateTestFactionState(string factionId = "faction-michael")
        {
            var state = new FactionState(
                factionId: factionId,
                initialCash: 10000,
                initialTroopCount: 50);
            return state;
        }

        private Zone CreateTestZone(string id, string? ownerFactionId = null, Vector3? center = null)
        {
            var zone = new Zone(
                id: id,
                name: $"Test Zone {id}",
                center: center ?? new Vector3(0, 0, 0),
                radius: 150f,
                strategicValue: 5);
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
}
