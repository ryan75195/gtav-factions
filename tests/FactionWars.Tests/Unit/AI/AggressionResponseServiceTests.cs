using FactionWars.AI.Interfaces;
using FactionWars.AI.Models;
using FactionWars.AI.Services;
using FactionWars.Factions.Models;
using FactionWars.Territory.Models;
using FactionWars.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace FactionWars.Tests.Unit.AI
{
    /// <summary>
    /// Tests for the AggressionResponseService.
    /// The aggression response system determines how AI factions react when the player
    /// attacks their zones, including defense prioritization, counter-attacks, and
    /// faction-specific behavioral responses.
    /// </summary>
    public class AggressionResponseServiceTests
    {
        #region Constructor Tests

        [Fact]
        public void Constructor_WithValidParameters_CreatesInstance()
        {
            var service = new AggressionResponseService();

            Assert.NotNull(service);
        }

        [Fact]
        public void Constructor_ImplementsIAggressionResponseService()
        {
            var service = new AggressionResponseService();

            Assert.IsAssignableFrom<IAggressionResponseService>(service);
        }

        #endregion

        #region RecordAggression - Null Parameter Tests

        [Fact]
        public void RecordAggression_NullAggressorId_ThrowsArgumentNullException()
        {
            var service = new AggressionResponseService();

            Assert.Throws<ArgumentNullException>(() => service.RecordAggression(null!, "target-zone", 10));
        }

        [Fact]
        public void RecordAggression_NullTargetZoneId_ThrowsArgumentNullException()
        {
            var service = new AggressionResponseService();

            Assert.Throws<ArgumentNullException>(() => service.RecordAggression("player", null!, 10));
        }

        [Fact]
        public void RecordAggression_EmptyAggressorId_ThrowsArgumentException()
        {
            var service = new AggressionResponseService();

            Assert.Throws<ArgumentException>(() => service.RecordAggression("", "target-zone", 10));
        }

        [Fact]
        public void RecordAggression_EmptyTargetZoneId_ThrowsArgumentException()
        {
            var service = new AggressionResponseService();

            Assert.Throws<ArgumentException>(() => service.RecordAggression("player", "", 10));
        }

        #endregion

        #region RecordAggression - Basic Recording Tests

        [Fact]
        public void RecordAggression_ValidParameters_RecordsAggression()
        {
            var service = new AggressionResponseService();

            service.RecordAggression("player", "zone-1", 10);

            var threatLevel = service.GetThreatLevel("player", "faction-1");
            Assert.True(threatLevel > 0, "Threat level should increase after aggression");
        }

        [Fact]
        public void RecordAggression_MultipleTimes_AccumulatesThreatLevel()
        {
            var service = new AggressionResponseService();

            service.RecordAggression("player", "zone-1", 10);
            var firstThreatLevel = service.GetThreatLevel("player", "faction-1");

            service.RecordAggression("player", "zone-2", 10);
            var secondThreatLevel = service.GetThreatLevel("player", "faction-1");

            Assert.True(secondThreatLevel > firstThreatLevel, "Threat level should accumulate");
        }

        [Fact]
        public void RecordAggression_DamageAffectsThreatLevel()
        {
            var service = new AggressionResponseService();

            service.RecordAggression("player", "zone-1", 5);
            var lowDamageThreat = service.GetThreatLevel("player", "test-faction");

            var service2 = new AggressionResponseService();
            service2.RecordAggression("player", "zone-1", 50);
            var highDamageThreat = service2.GetThreatLevel("player", "test-faction");

            Assert.True(highDamageThreat > lowDamageThreat, "Higher damage should cause higher threat");
        }

        [Fact]
        public void RecordAggression_NegativeDamage_ThrowsArgumentOutOfRangeException()
        {
            var service = new AggressionResponseService();

            Assert.Throws<ArgumentOutOfRangeException>(() => service.RecordAggression("player", "zone-1", -5));
        }

        #endregion

        #region GetThreatLevel Tests

        [Fact]
        public void GetThreatLevel_NullAggressorId_ThrowsArgumentNullException()
        {
            var service = new AggressionResponseService();

            Assert.Throws<ArgumentNullException>(() => service.GetThreatLevel(null!, "faction-1"));
        }

        [Fact]
        public void GetThreatLevel_NullDefenderId_ThrowsArgumentNullException()
        {
            var service = new AggressionResponseService();

            Assert.Throws<ArgumentNullException>(() => service.GetThreatLevel("player", null!));
        }

        [Fact]
        public void GetThreatLevel_NoAggressionRecorded_ReturnsZero()
        {
            var service = new AggressionResponseService();

            var threatLevel = service.GetThreatLevel("player", "faction-1");

            Assert.Equal(0f, threatLevel);
        }

        [Fact]
        public void GetThreatLevel_ReturnsValueBetweenZeroAndOne()
        {
            var service = new AggressionResponseService();
            service.RecordAggression("player", "zone-1", 100);

            var threatLevel = service.GetThreatLevel("player", "faction-1");

            Assert.InRange(threatLevel, 0f, 1f);
        }

        #endregion

        #region GetAggressionResponse Tests

        [Fact]
        public void GetAggressionResponse_NullContext_ThrowsArgumentNullException()
        {
            var service = new AggressionResponseService();

            Assert.Throws<ArgumentNullException>(() => service.GetAggressionResponse(null!, "player"));
        }

        [Fact]
        public void GetAggressionResponse_NullAggressorId_ThrowsArgumentNullException()
        {
            var service = new AggressionResponseService();
            var context = CreateTestContext();

            Assert.Throws<ArgumentNullException>(() => service.GetAggressionResponse(context, null!));
        }

        [Fact]
        public void GetAggressionResponse_NoAggression_ReturnsNoResponse()
        {
            var service = new AggressionResponseService();
            var context = CreateTestContext();

            var response = service.GetAggressionResponse(context, "player");

            Assert.NotNull(response);
            Assert.Equal(AggressionResponseType.None, response.ResponseType);
        }

        [Fact]
        public void GetAggressionResponse_LowAggression_ReturnsDefensiveResponse()
        {
            var service = new AggressionResponseService();
            service.RecordAggression("player", "owned-zone", 10);

            var context = CreateTestContext();
            var response = service.GetAggressionResponse(context, "player");

            Assert.NotNull(response);
            Assert.True(
                response.ResponseType == AggressionResponseType.Defensive ||
                response.ResponseType == AggressionResponseType.None,
                "Low aggression should trigger defensive or no response");
        }

        [Fact]
        public void GetAggressionResponse_HighAggression_ReturnsRetaliationResponse()
        {
            var service = new AggressionResponseService();
            // Record multiple high-damage aggressions
            for (int i = 0; i < 5; i++)
            {
                service.RecordAggression("player", $"zone-{i}", 50);
            }

            var context = CreateTestContext(troops: 100);
            var response = service.GetAggressionResponse(context, "player");

            Assert.NotNull(response);
            Assert.True(
                response.ResponseType == AggressionResponseType.Retaliation ||
                response.ResponseType == AggressionResponseType.Defensive,
                "High aggression should trigger retaliation or strong defense");
        }

        [Fact]
        public void GetAggressionResponse_ReturnsDecisionsWithTargets()
        {
            var service = new AggressionResponseService();
            service.RecordAggression("player", "owned-zone", 30);

            var context = CreateTestContext(troops: 50);
            var response = service.GetAggressionResponse(context, "player");

            // If there's a response other than None, it should have decisions
            if (response.ResponseType != AggressionResponseType.None)
            {
                Assert.NotEmpty(response.Decisions);
            }
        }

        #endregion

        #region Faction-Specific Response Tests

        [Theory]
        [InlineData(FactionType.Trevor)]
        [InlineData(FactionType.Michael)]
        [InlineData(FactionType.Franklin)]
        public void GetAggressionResponse_AllFactionTypes_ReturnsValidResponse(FactionType factionType)
        {
            var service = new AggressionResponseService();
            service.RecordAggression("player", "owned-zone", 30);

            var context = CreateTestContextForFaction(factionType, troops: 50);
            var response = service.GetAggressionResponse(context, "player");

            Assert.NotNull(response);
            Assert.True(Enum.IsDefined(typeof(AggressionResponseType), response.ResponseType));
        }

        [Fact]
        public void GetAggressionResponse_TrevorFaction_MoreAggressive()
        {
            var service = new AggressionResponseService();
            service.RecordAggression("player", "owned-zone", 30);

            var trevorContext = CreateTestContextForFaction(FactionType.Trevor, troops: 50);
            var trevorResponse = service.GetAggressionResponse(trevorContext, "player");

            var michaelContext = CreateTestContextForFaction(FactionType.Michael, troops: 50);
            var michaelResponse = service.GetAggressionResponse(michaelContext, "player");

            // Trevor should have at least as aggressive a response as Michael
            Assert.True(
                (int)trevorResponse.ResponseType >= (int)michaelResponse.ResponseType ||
                trevorResponse.Decisions.Sum(d => d.TroopsToCommit) >= michaelResponse.Decisions.Sum(d => d.TroopsToCommit),
                "Trevor should respond at least as aggressively as Michael");
        }

        [Fact]
        public void GetAggressionResponse_MichaelFaction_MoreDefensive()
        {
            var service = new AggressionResponseService();
            service.RecordAggression("player", "owned-zone", 30);

            var michaelContext = CreateTestContextForFaction(FactionType.Michael, troops: 50);
            var michaelResponse = service.GetAggressionResponse(michaelContext, "player");

            // Michael's response should prioritize defense
            var defendDecisions = michaelResponse.Decisions.Count(d => d.DecisionType == AIDecisionType.Defend);
            var attackDecisions = michaelResponse.Decisions.Count(d => d.DecisionType == AIDecisionType.Attack);

            // When threatened, Michael should have at least as many defense decisions as attack decisions
            Assert.True(defendDecisions >= attackDecisions,
                "Michael should prioritize defense when under attack");
        }

        [Fact]
        public void GetAggressionResponse_FranklinFaction_Balanced()
        {
            var service = new AggressionResponseService();
            service.RecordAggression("player", "owned-zone", 30);

            var franklinContext = CreateTestContextForFaction(FactionType.Franklin, troops: 50);
            var franklinResponse = service.GetAggressionResponse(franklinContext, "player");

            // Franklin should respond, but not overly aggressively
            Assert.NotNull(franklinResponse);
            Assert.True(
                franklinResponse.ResponseType != AggressionResponseType.None ||
                franklinContext.FactionState.TroopCount < 10,
                "Franklin should respond to aggression when resources allow");
        }

        #endregion

        #region Threat Decay Tests

        [Fact]
        public void DecayThreatLevels_ReducesThreat()
        {
            var service = new AggressionResponseService();
            service.RecordAggression("player", "zone-1", 50);
            var initialThreat = service.GetThreatLevel("player", "faction-1");

            service.DecayThreatLevels(0.5f); // 50% decay
            var decayedThreat = service.GetThreatLevel("player", "faction-1");

            Assert.True(decayedThreat < initialThreat, "Threat should decrease after decay");
        }

        [Fact]
        public void DecayThreatLevels_InvalidDecayRate_ThrowsArgumentOutOfRangeException()
        {
            var service = new AggressionResponseService();

            Assert.Throws<ArgumentOutOfRangeException>(() => service.DecayThreatLevels(-0.1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => service.DecayThreatLevels(1.1f));
        }

        [Fact]
        public void DecayThreatLevels_ZeroDecay_KeepsThreatUnchanged()
        {
            var service = new AggressionResponseService();
            service.RecordAggression("player", "zone-1", 50);
            var initialThreat = service.GetThreatLevel("player", "faction-1");

            service.DecayThreatLevels(0f);
            var decayedThreat = service.GetThreatLevel("player", "faction-1");

            Assert.Equal(initialThreat, decayedThreat);
        }

        [Fact]
        public void DecayThreatLevels_FullDecay_ZerosThreat()
        {
            var service = new AggressionResponseService();
            service.RecordAggression("player", "zone-1", 50);

            service.DecayThreatLevels(1f); // 100% decay
            var decayedThreat = service.GetThreatLevel("player", "faction-1");

            Assert.Equal(0f, decayedThreat);
        }

        #endregion

        #region GetRecentAggressions Tests

        [Fact]
        public void GetRecentAggressions_NullDefenderId_ThrowsArgumentNullException()
        {
            var service = new AggressionResponseService();

            Assert.Throws<ArgumentNullException>(() => service.GetRecentAggressions(null!));
        }

        [Fact]
        public void GetRecentAggressions_NoAggressions_ReturnsEmptyList()
        {
            var service = new AggressionResponseService();

            var aggressions = service.GetRecentAggressions("faction-1");

            Assert.NotNull(aggressions);
            Assert.Empty(aggressions);
        }

        [Fact]
        public void GetRecentAggressions_WithAggressions_ReturnsRecordedAggressions()
        {
            var service = new AggressionResponseService();
            service.RecordAggression("player", "zone-1", 10);
            service.RecordAggression("player", "zone-2", 20);

            var aggressions = service.GetRecentAggressions("test-faction");

            Assert.NotNull(aggressions);
            Assert.Equal(2, aggressions.Count);
        }

        [Fact]
        public void GetRecentAggressions_ReturnsCorrectDetails()
        {
            var service = new AggressionResponseService();
            service.RecordAggression("player", "zone-1", 25);

            var aggressions = service.GetRecentAggressions("test-faction");

            Assert.Single(aggressions);
            var aggression = aggressions[0];
            Assert.Equal("player", aggression.AggressorId);
            Assert.Equal("zone-1", aggression.TargetZoneId);
            Assert.Equal(25, aggression.DamageDealt);
        }

        #endregion

        #region ClearAggressionHistory Tests

        [Fact]
        public void ClearAggressionHistory_NullAggressorId_ThrowsArgumentNullException()
        {
            var service = new AggressionResponseService();

            Assert.Throws<ArgumentNullException>(() => service.ClearAggressionHistory(null!));
        }

        [Fact]
        public void ClearAggressionHistory_ClearsAllRecordsForAggressor()
        {
            var service = new AggressionResponseService();
            service.RecordAggression("player", "zone-1", 10);
            service.RecordAggression("player", "zone-2", 20);

            service.ClearAggressionHistory("player");

            var threatLevel = service.GetThreatLevel("player", "faction-1");
            Assert.Equal(0f, threatLevel);
        }

        [Fact]
        public void ClearAggressionHistory_DoesNotAffectOtherAggressors()
        {
            var service = new AggressionResponseService();
            service.RecordAggression("player", "zone-1", 10);
            service.RecordAggression("other-faction", "zone-2", 20);

            service.ClearAggressionHistory("player");

            var otherThreat = service.GetThreatLevel("other-faction", "faction-1");
            Assert.True(otherThreat > 0, "Other aggressor's threat should remain");
        }

        #endregion

        #region IsUnderAttack Tests

        [Fact]
        public void IsUnderAttack_NullDefenderId_ThrowsArgumentNullException()
        {
            var service = new AggressionResponseService();

            Assert.Throws<ArgumentNullException>(() => service.IsUnderAttack(null!));
        }

        [Fact]
        public void IsUnderAttack_NoAggression_ReturnsFalse()
        {
            var service = new AggressionResponseService();

            var isUnderAttack = service.IsUnderAttack("faction-1");

            Assert.False(isUnderAttack);
        }

        [Fact]
        public void IsUnderAttack_RecentAggression_ReturnsTrue()
        {
            var service = new AggressionResponseService();
            service.RecordAggression("player", "zone-1", 10);

            var isUnderAttack = service.IsUnderAttack("test-faction");

            Assert.True(isUnderAttack);
        }

        [Fact]
        public void IsUnderAttack_AfterDecayToZero_ReturnsFalse()
        {
            var service = new AggressionResponseService();
            service.RecordAggression("player", "zone-1", 10);
            service.DecayThreatLevels(1f); // Full decay

            var isUnderAttack = service.IsUnderAttack("test-faction");

            Assert.False(isUnderAttack);
        }

        #endregion

        #region GetPrimaryThreat Tests

        [Fact]
        public void GetPrimaryThreat_NullDefenderId_ThrowsArgumentNullException()
        {
            var service = new AggressionResponseService();

            Assert.Throws<ArgumentNullException>(() => service.GetPrimaryThreat(null!));
        }

        [Fact]
        public void GetPrimaryThreat_NoThreats_ReturnsNull()
        {
            var service = new AggressionResponseService();

            var threat = service.GetPrimaryThreat("faction-1");

            Assert.Null(threat);
        }

        [Fact]
        public void GetPrimaryThreat_SingleThreat_ReturnsThatThreat()
        {
            var service = new AggressionResponseService();
            service.RecordAggression("player", "zone-1", 10);

            var threat = service.GetPrimaryThreat("test-faction");

            Assert.NotNull(threat);
            Assert.Equal("player", threat);
        }

        [Fact]
        public void GetPrimaryThreat_MultipleThreats_ReturnsHighestThreat()
        {
            var service = new AggressionResponseService();
            service.RecordAggression("player", "zone-1", 50); // High damage
            service.RecordAggression("other-faction", "zone-2", 10); // Low damage

            var threat = service.GetPrimaryThreat("test-faction");

            Assert.NotNull(threat);
            Assert.Equal("player", threat);
        }

        #endregion

        #region Integration Tests

        [Fact]
        public void FullAggressionCycle_TracksAndRespondsCorrectly()
        {
            var service = new AggressionResponseService();
            var context = CreateTestContext(troops: 100);

            // Initially no threats
            Assert.False(service.IsUnderAttack(context.Faction.Id));
            Assert.Null(service.GetPrimaryThreat(context.Faction.Id));

            // Player attacks
            service.RecordAggression("player", "owned-zone", 30);

            // Now under attack
            Assert.True(service.IsUnderAttack(context.Faction.Id));
            Assert.Equal("player", service.GetPrimaryThreat(context.Faction.Id));

            // Get response
            var response = service.GetAggressionResponse(context, "player");
            Assert.NotEqual(AggressionResponseType.None, response.ResponseType);

            // Decay over time
            service.DecayThreatLevels(0.9f);
            var reducedThreat = service.GetThreatLevel("player", context.Faction.Id);
            Assert.True(reducedThreat < 1f);
        }

        [Fact]
        public void AggressionResponse_WithLowTroops_ReturnsDefensiveResponse()
        {
            var service = new AggressionResponseService();
            service.RecordAggression("player", "owned-zone", 30);

            var context = CreateTestContext(troops: 10); // Very low troops
            var response = service.GetAggressionResponse(context, "player");

            // With low troops, should prioritize defense over retaliation
            Assert.True(
                response.ResponseType == AggressionResponseType.Defensive ||
                response.ResponseType == AggressionResponseType.None ||
                response.Decisions.All(d => d.DecisionType == AIDecisionType.Defend),
                "Low troop count should result in defensive response");
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

        private FactionState CreateTestFactionState(string factionId, int troopCount = 50, int cash = 10000)
        {
            var state = new FactionState(factionId, cash, troopCount);
            return state;
        }

        private Zone CreateTestZone(string id, int strategicValue = 5, string? ownerId = null)
        {
            var zone = new Zone(
                id: id,
                name: $"Test Zone {id}",
                center: new Vector3(0, 0, 0),
                radius: 150f,
                strategicValue: strategicValue);
            zone.OwnerFactionId = ownerId;
            return zone;
        }

        private AIContext CreateTestContext(int troops = 50, int cash = 10000)
        {
            return CreateTestContextForFaction(FactionType.Michael, troops, cash);
        }

        private AIContext CreateTestContextForFaction(FactionType factionType, int troops = 50, int cash = 10000)
        {
            var faction = CreateTestFaction(factionType);
            var factionState = CreateTestFactionState(faction.Id, troops, cash);

            var ownedZone = CreateTestZone("owned-zone", strategicValue: 5, ownerId: faction.Id);
            var enemyZone = CreateTestZone("enemy-zone", strategicValue: 5, ownerId: "player");
            var neutralZone = CreateTestZone("neutral-zone", strategicValue: 3);

            var allZones = new List<Zone> { ownedZone, enemyZone, neutralZone };
            var ownedZones = new List<Zone> { ownedZone };

            var enemyFaction = new Faction("player", "Player", leader: "Player", description: "The player faction");
            var enemyFactions = new List<Faction> { enemyFaction };

            return new AIContext(faction, factionState, ownedZones, allZones, enemyFactions);
        }

        #endregion
    }
}
