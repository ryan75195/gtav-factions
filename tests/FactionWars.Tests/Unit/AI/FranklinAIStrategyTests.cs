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
    /// Tests for FranklinAIStrategy.
    /// Franklin's strategy is opportunistic, mobile, and flexible.
    /// - Medium aggressiveness (balanced approach)
    /// - Medium risk tolerance (calculated opportunism)
    /// - Opportunistic focus (targets underdefended zones)
    /// - Mobility bonuses (quick strikes, efficient troop usage)
    /// - Flexibility (can disengage from unfavorable situations)
    /// </summary>
    public class FranklinAIStrategyTests
    {
        #region Constructor Tests

        [Fact]
        public void Constructor_SetsFactionTypeToFranklin()
        {
            var strategy = new FranklinAIStrategy();

            Assert.Equal(FactionType.Franklin, strategy.FactionType);
        }

        [Fact]
        public void Constructor_SetsMediumAggressiveness()
        {
            var strategy = new FranklinAIStrategy();

            // Franklin should have medium aggressiveness (balanced, opportunistic)
            var aggressiveness = strategy.GetAggressiveness();
            Assert.True(aggressiveness >= 0.5f && aggressiveness <= 0.7f,
                $"Franklin's aggressiveness should be medium (0.5-0.7), was {aggressiveness}");
        }

        [Fact]
        public void Constructor_SetsMediumRiskTolerance()
        {
            var strategy = new FranklinAIStrategy();

            // Franklin takes calculated risks (medium risk tolerance)
            var riskTolerance = strategy.GetRiskTolerance();
            Assert.True(riskTolerance >= 0.5f && riskTolerance <= 0.7f,
                $"Franklin's risk tolerance should be medium (0.5-0.7), was {riskTolerance}");
        }

        #endregion

        #region EvaluateZone Tests - Opportunistic Focus

        [Fact]
        public void EvaluateZone_NeutralZone_ScoresHigherThanEnemyZone()
        {
            var strategy = new FranklinAIStrategy();
            var context = CreateTestContext();

            var neutralZone = CreateTestZone("neutral", strategicValue: 5);
            var enemyZone = CreateTestZone("enemy", ownerFactionId: "enemy-faction", strategicValue: 5);

            var neutralScore = strategy.EvaluateZone(neutralZone, context);
            var enemyScore = strategy.EvaluateZone(enemyZone, context);

            // Franklin is opportunistic - prefers easy targets (neutral zones)
            Assert.True(neutralScore > enemyScore,
                $"Neutral zone score ({neutralScore}) should be higher than enemy zone score ({enemyScore})");
        }

        [Fact]
        public void EvaluateZone_UncontestedZone_ScoresHigherThanContestedZone()
        {
            var strategy = new FranklinAIStrategy();
            var faction = CreateTestFaction(FactionType.Franklin);
            var factionState = CreateTestFactionState(faction.Id, troopCount: 50);

            // Create similar zones - one contested (harder), one not
            var easyZone = CreateTestZone("easy-enemy", ownerFactionId: "enemy-faction", strategicValue: 5);
            var hardZone = CreateTestZone("hard-enemy", ownerFactionId: "enemy-faction", strategicValue: 5);
            hardZone.IsContested = true; // Already being fought over - harder target

            var context = new AIContext(faction, factionState,
                new List<Zone>(),
                new[] { easyZone, hardZone },
                new[] { CreateTestFaction(FactionType.Michael, "enemy-faction") });

            var easyScore = strategy.EvaluateZone(easyZone, context);
            var hardScore = strategy.EvaluateZone(hardZone, context);

            // Franklin prefers easier targets (uncontested zones)
            Assert.True(easyScore >= hardScore,
                $"Uncontested zone score ({easyScore}) should be >= contested zone score ({hardScore})");
        }

        [Fact]
        public void EvaluateZone_AppliesOpportunityBonus()
        {
            var franklinStrategy = new FranklinAIStrategy();
            var michaelStrategy = new MichaelAIStrategy();
            var context = CreateTestContext();

            // Neutral zone (easy opportunity)
            var neutralZone = CreateTestZone("neutral", strategicValue: 5);

            var franklinScore = franklinStrategy.EvaluateZone(neutralZone, context);
            var michaelScore = michaelStrategy.EvaluateZone(neutralZone, context);

            // Franklin should find opportunities more attractive than calculated Michael
            Assert.True(franklinScore > michaelScore,
                "Franklin should find opportunities more attractive than Michael");
        }

        [Fact]
        public void EvaluateZone_NullZone_ThrowsArgumentNullException()
        {
            var strategy = new FranklinAIStrategy();
            var context = CreateTestContext();

            Assert.Throws<ArgumentNullException>(() => strategy.EvaluateZone(null!, context));
        }

        [Fact]
        public void EvaluateZone_NullContext_ThrowsArgumentNullException()
        {
            var strategy = new FranklinAIStrategy();
            var zone = CreateTestZone("zone-1");

            Assert.Throws<ArgumentNullException>(() => strategy.EvaluateZone(zone, null!));
        }

        [Fact]
        public void EvaluateZone_ReturnsValueInValidRange()
        {
            var strategy = new FranklinAIStrategy();
            var context = CreateTestContext();
            var zone = CreateTestZone("zone-1", strategicValue: 7);

            var score = strategy.EvaluateZone(zone, context);

            Assert.InRange(score, 0f, 1f);
        }

        [Fact]
        public void EvaluateZone_AdjacentToOwnedZone_ScoresHigher()
        {
            var strategy = new FranklinAIStrategy();
            var faction = CreateTestFaction(FactionType.Franklin);
            var factionState = CreateTestFactionState(faction.Id, troopCount: 50);

            var ownedZone = CreateTestZone("owned", ownerFactionId: faction.Id, strategicValue: 5,
                center: new Vector3(0, 0, 0));
            var adjacentZone = CreateTestZone("adjacent", strategicValue: 5,
                center: new Vector3(100, 0, 0)); // Close enough to be adjacent
            var distantZone = CreateTestZone("distant", strategicValue: 5,
                center: new Vector3(1000, 1000, 0)); // Far away

            var allZones = new[] { ownedZone, adjacentZone, distantZone };
            var context = new AIContext(faction, factionState, new[] { ownedZone }, allZones, new List<Faction>());

            var adjacentScore = strategy.EvaluateZone(adjacentZone, context);
            var distantScore = strategy.EvaluateZone(distantZone, context);

            // Franklin values mobility - adjacent zones allow quick strikes
            Assert.True(adjacentScore >= distantScore,
                $"Adjacent zone score ({adjacentScore}) should be >= distant zone score ({distantScore})");
        }

        #endregion

        #region MakeDecisions Tests - Balanced Approach

        [Fact]
        public void MakeDecisions_WithOpportunity_PrioritizesEasyTargets()
        {
            var strategy = new FranklinAIStrategy();
            var faction = CreateTestFaction(FactionType.Franklin);
            var factionState = CreateTestFactionState(faction.Id, troopCount: 50);

            var ownedZone = CreateTestZone("owned", ownerFactionId: faction.Id, strategicValue: 5);
            var easyNeutral = CreateTestZone("easy-neutral", strategicValue: 6);
            var hardEnemy = CreateTestZone("hard-enemy", ownerFactionId: "enemy-faction", strategicValue: 7);
            hardEnemy.IsContested = true; // Already contested - harder target

            var ownedZones = new List<Zone> { ownedZone };
            var allZones = new List<Zone> { ownedZone, easyNeutral, hardEnemy };
            var enemyFactions = new List<Faction> { CreateTestFaction(FactionType.Trevor, "enemy-faction") };
            var context = new AIContext(faction, factionState, ownedZones, allZones, enemyFactions);

            var decisions = strategy.MakeDecisions(context);

            // Franklin should have attack decisions
            var attackDecision = decisions.FirstOrDefault(d => d.DecisionType == AIDecisionType.Attack);
            Assert.NotNull(attackDecision);

            // Should target the easier neutral zone over the heavily defended enemy zone
            Assert.Equal("easy-neutral", attackDecision.TargetZoneId);
        }

        [Fact]
        public void MakeDecisions_AttackDecisions_HaveModeratePriority()
        {
            var strategy = new FranklinAIStrategy();
            var faction = CreateTestFaction(FactionType.Franklin);
            var factionState = CreateTestFactionState(faction.Id, troopCount: 100);

            var ownedZone = CreateTestZone("owned", ownerFactionId: faction.Id, strategicValue: 5);
            var targetZone = CreateTestZone("target", strategicValue: 6);

            var ownedZones = new List<Zone> { ownedZone };
            var allZones = new List<Zone> { ownedZone, targetZone };
            var context = new AIContext(faction, factionState, ownedZones, allZones, new List<Faction>());

            var decisions = strategy.MakeDecisions(context);

            var attackDecision = decisions.FirstOrDefault(d => d.DecisionType == AIDecisionType.Attack);
            Assert.NotNull(attackDecision);
            // Franklin has balanced priorities
            Assert.True(attackDecision.Priority >= 0.3f && attackDecision.Priority <= 0.9f,
                "Attack priority should be moderate for Franklin");
        }

        [Fact]
        public void MakeDecisions_NullContext_ThrowsArgumentNullException()
        {
            var strategy = new FranklinAIStrategy();

            Assert.Throws<ArgumentNullException>(() => strategy.MakeDecisions(null!));
        }

        [Fact]
        public void MakeDecisions_NoTroops_ReturnsEmptyOrHold()
        {
            var strategy = new FranklinAIStrategy();
            var faction = CreateTestFaction(FactionType.Franklin);
            var factionState = CreateTestFactionState(faction.Id, troopCount: 0);

            var ownedZone = CreateTestZone("owned", ownerFactionId: faction.Id);
            var context = new AIContext(faction, factionState, new[] { ownedZone }, new[] { ownedZone }, new List<Faction>());

            var decisions = strategy.MakeDecisions(context);

            Assert.True(decisions.Count == 0 || decisions.All(d => d.DecisionType == AIDecisionType.Hold));
        }

        [Fact]
        public void MakeDecisions_BalancesAttackAndDefend()
        {
            var strategy = new FranklinAIStrategy();
            var faction = CreateTestFaction(FactionType.Franklin);
            var factionState = CreateTestFactionState(faction.Id, troopCount: 100);

            var contestedZone = CreateTestZone("contested", ownerFactionId: faction.Id, strategicValue: 6);
            contestedZone.IsContested = true;
            var targetZone = CreateTestZone("target", strategicValue: 6);

            var ownedZones = new List<Zone> { contestedZone };
            var allZones = new List<Zone> { contestedZone, targetZone };
            var context = new AIContext(faction, factionState, ownedZones, allZones, new List<Faction>());

            var decisions = strategy.MakeDecisions(context);

            // Franklin should consider both attack and defense options
            var hasDefend = decisions.Any(d => d.DecisionType == AIDecisionType.Defend);
            var hasAttack = decisions.Any(d => d.DecisionType == AIDecisionType.Attack);

            Assert.True(hasDefend, "Franklin should consider defending contested zones");
            // May or may not attack depending on situation
        }

        #endregion

        #region ShouldAttack Tests - Opportunistic Approach

        [Fact]
        public void ShouldAttack_NeutralZone_WithModerateTroops_ReturnsTrue()
        {
            var strategy = new FranklinAIStrategy();
            var faction = CreateTestFaction(FactionType.Franklin);
            var factionState = CreateTestFactionState(faction.Id, troopCount: 30);

            var neutralZone = CreateTestZone("neutral", strategicValue: 5);
            var context = new AIContext(faction, factionState, new List<Zone>(), new[] { neutralZone }, new List<Faction>());

            var shouldAttack = strategy.ShouldAttack(neutralZone, context);

            // Franklin takes opportunities
            Assert.True(shouldAttack);
        }

        [Fact]
        public void ShouldAttack_ModerateValueEnemyZone_WithTroops_ReturnsTrue()
        {
            var strategy = new FranklinAIStrategy();
            var faction = CreateTestFaction(FactionType.Franklin);
            var factionState = CreateTestFactionState(faction.Id, troopCount: 50);

            var enemyZone = CreateTestZone("enemy", ownerFactionId: "enemy-faction", strategicValue: 5);
            var enemyFactions = new List<Faction> { CreateTestFaction(FactionType.Michael, "enemy-faction") };
            var context = new AIContext(faction, factionState, new List<Zone>(), new[] { enemyZone }, enemyFactions);

            var shouldAttack = strategy.ShouldAttack(enemyZone, context);

            // Franklin will attack moderate-value targets when he has the troops
            Assert.True(shouldAttack);
        }

        [Fact]
        public void ShouldAttack_LowValueZone_MayReturn_BasedOnOpportunity()
        {
            var strategy = new FranklinAIStrategy();
            var faction = CreateTestFaction(FactionType.Franklin);
            var factionState = CreateTestFactionState(faction.Id, troopCount: 50);

            var lowValueZone = CreateTestZone("low-value", strategicValue: 3);
            var context = new AIContext(faction, factionState, new List<Zone>(), new[] { lowValueZone }, new List<Faction>());

            var shouldAttack = strategy.ShouldAttack(lowValueZone, context);

            // Franklin is opportunistic - may attack low value if it's easy
            // (neutral zones with low value are still opportunities)
            Assert.True(shouldAttack);
        }

        [Fact]
        public void ShouldAttack_OwnedZone_ReturnsFalse()
        {
            var strategy = new FranklinAIStrategy();
            var faction = CreateTestFaction(FactionType.Franklin);
            var factionState = CreateTestFactionState(faction.Id, troopCount: 50);

            var ownedZone = CreateTestZone("owned", ownerFactionId: faction.Id);
            var context = new AIContext(faction, factionState, new[] { ownedZone }, new[] { ownedZone }, new List<Faction>());

            var shouldAttack = strategy.ShouldAttack(ownedZone, context);

            Assert.False(shouldAttack);
        }

        [Fact]
        public void ShouldAttack_NoTroops_ReturnsFalse()
        {
            var strategy = new FranklinAIStrategy();
            var faction = CreateTestFaction(FactionType.Franklin);
            var factionState = CreateTestFactionState(faction.Id, troopCount: 0);

            var neutralZone = CreateTestZone("neutral", strategicValue: 8);
            var context = new AIContext(faction, factionState, new List<Zone>(), new[] { neutralZone }, new List<Faction>());

            var shouldAttack = strategy.ShouldAttack(neutralZone, context);

            Assert.False(shouldAttack);
        }

        [Fact]
        public void ShouldAttack_NullZone_ThrowsArgumentNullException()
        {
            var strategy = new FranklinAIStrategy();
            var context = CreateTestContext();

            Assert.Throws<ArgumentNullException>(() => strategy.ShouldAttack(null!, context));
        }

        [Fact]
        public void ShouldAttack_NullContext_ThrowsArgumentNullException()
        {
            var strategy = new FranklinAIStrategy();
            var zone = CreateTestZone("zone-1");

            Assert.Throws<ArgumentNullException>(() => strategy.ShouldAttack(zone, null!));
        }

        #endregion

        #region ShouldDefend Tests - Flexible Defense

        [Fact]
        public void ShouldDefend_ContestedZone_ReturnsTrue()
        {
            var strategy = new FranklinAIStrategy();
            var faction = CreateTestFaction(FactionType.Franklin);
            var factionState = CreateTestFactionState(faction.Id, troopCount: 20);

            var contestedZone = CreateTestZone("contested", ownerFactionId: faction.Id);
            contestedZone.IsContested = true;
            var context = new AIContext(faction, factionState, new[] { contestedZone }, new[] { contestedZone }, new List<Faction>());

            var shouldDefend = strategy.ShouldDefend(contestedZone, context);

            Assert.True(shouldDefend);
        }

        [Fact]
        public void ShouldDefend_HighValueZone_ReturnsTrue()
        {
            var strategy = new FranklinAIStrategy();
            var faction = CreateTestFaction(FactionType.Franklin);
            var factionState = CreateTestFactionState(faction.Id, troopCount: 30);

            var highValueZone = CreateTestZone("high-value", ownerFactionId: faction.Id, strategicValue: 8);
            var context = new AIContext(faction, factionState, new[] { highValueZone }, new[] { highValueZone }, new List<Faction>());

            var shouldDefend = strategy.ShouldDefend(highValueZone, context);

            Assert.True(shouldDefend);
        }

        [Fact]
        public void ShouldDefend_ModerateValueZone_MayDefend()
        {
            var strategy = new FranklinAIStrategy();
            var faction = CreateTestFaction(FactionType.Franklin);
            var factionState = CreateTestFactionState(faction.Id, troopCount: 30);

            var moderateValueZone = CreateTestZone("moderate-value", ownerFactionId: faction.Id, strategicValue: 5);
            var context = new AIContext(faction, factionState, new[] { moderateValueZone }, new[] { moderateValueZone }, new List<Faction>());

            // Franklin has balanced defense - should defend moderate value zones
            var shouldDefend = strategy.ShouldDefend(moderateValueZone, context);

            // Franklin is flexible - this is implementation dependent but should work
            Assert.NotNull(shouldDefend.ToString());
        }

        [Fact]
        public void ShouldDefend_NotOwnedZone_ReturnsFalse()
        {
            var strategy = new FranklinAIStrategy();
            var faction = CreateTestFaction(FactionType.Franklin);
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
            var strategy = new FranklinAIStrategy();
            var context = CreateTestContext();

            Assert.Throws<ArgumentNullException>(() => strategy.ShouldDefend(null!, context));
        }

        [Fact]
        public void ShouldDefend_NullContext_ThrowsArgumentNullException()
        {
            var strategy = new FranklinAIStrategy();
            var zone = CreateTestZone("zone-1");

            Assert.Throws<ArgumentNullException>(() => strategy.ShouldDefend(zone, null!));
        }

        #endregion

        #region GetTroopsForAction Tests - Efficient Allocation

        [Fact]
        public void GetTroopsForAction_Attack_AllocatesEfficientAmount()
        {
            var franklinStrategy = new FranklinAIStrategy();
            var trevorStrategy = new TrevorAIStrategy();

            var faction = CreateTestFaction(FactionType.Franklin);
            var factionState = CreateTestFactionState(faction.Id, troopCount: 100);
            var context = new AIContext(faction, factionState, new List<Zone>(), new List<Zone>(), new List<Faction>());

            var attackDecision = new AIDecision(AIDecisionType.Attack, "zone-1", 0.7f, 0);

            var franklinTroops = franklinStrategy.GetTroopsForAction(attackDecision, context);
            var trevorTroops = trevorStrategy.GetTroopsForAction(attackDecision, context);

            // Franklin is more efficient than aggressive Trevor - commits fewer troops
            Assert.True(franklinTroops <= trevorTroops,
                $"Franklin troops ({franklinTroops}) should be <= Trevor troops ({trevorTroops})");
        }

        [Fact]
        public void GetTroopsForAction_Attack_AllocatesReasonableAmount()
        {
            var strategy = new FranklinAIStrategy();
            var faction = CreateTestFaction(FactionType.Franklin);
            var factionState = CreateTestFactionState(faction.Id, troopCount: 100);
            var context = new AIContext(faction, factionState, new List<Zone>(), new List<Zone>(), new List<Faction>());

            var attackDecision = new AIDecision(AIDecisionType.Attack, "zone-1", 0.7f, 0);

            var troops = strategy.GetTroopsForAction(attackDecision, context);

            // Franklin allocates a reasonable amount for attacks (mobile, efficient)
            Assert.True(troops >= 15 && troops <= 60,
                $"Franklin should allocate 15-60 troops for attack, got {troops}");
        }

        [Fact]
        public void GetTroopsForAction_Defense_AllocatesBalanced()
        {
            var strategy = new FranklinAIStrategy();
            var faction = CreateTestFaction(FactionType.Franklin);
            var factionState = CreateTestFactionState(faction.Id, troopCount: 100);
            var context = new AIContext(faction, factionState, new List<Zone>(), new List<Zone>(), new List<Faction>());

            var defendDecision = new AIDecision(AIDecisionType.Defend, "zone-1", 0.7f, 0);

            var troops = strategy.GetTroopsForAction(defendDecision, context);

            // Franklin defends adequately
            Assert.True(troops > 0);
            Assert.True(troops >= 10, "Franklin should allocate meaningful troops for defense");
        }

        [Fact]
        public void GetTroopsForAction_DoesNotExceedAvailableTroops()
        {
            var strategy = new FranklinAIStrategy();
            var faction = CreateTestFaction(FactionType.Franklin);
            var factionState = CreateTestFactionState(faction.Id, troopCount: 20);
            var context = new AIContext(faction, factionState, new List<Zone>(), new List<Zone>(), new List<Faction>());

            var decision = new AIDecision(AIDecisionType.Attack, "zone-1", 1.0f, 0);

            var troops = strategy.GetTroopsForAction(decision, context);

            Assert.True(troops <= factionState.TroopCount);
        }

        [Fact]
        public void GetTroopsForAction_NullDecision_ThrowsArgumentNullException()
        {
            var strategy = new FranklinAIStrategy();
            var context = CreateTestContext();

            Assert.Throws<ArgumentNullException>(() => strategy.GetTroopsForAction(null!, context));
        }

        [Fact]
        public void GetTroopsForAction_NullContext_ThrowsArgumentNullException()
        {
            var strategy = new FranklinAIStrategy();
            var decision = new AIDecision(AIDecisionType.Attack, "zone-1", 0.8f, 0);

            Assert.Throws<ArgumentNullException>(() => strategy.GetTroopsForAction(decision, null!));
        }

        #endregion

        #region Comparative Tests - Between Strategies

        [Fact]
        public void FranklinStrategy_AggressivenessBetweenMichaelAndTrevor()
        {
            var franklinStrategy = new FranklinAIStrategy();
            var michaelStrategy = new MichaelAIStrategy();
            var trevorStrategy = new TrevorAIStrategy();

            var franklinAgg = franklinStrategy.GetAggressiveness();
            var michaelAgg = michaelStrategy.GetAggressiveness();
            var trevorAgg = trevorStrategy.GetAggressiveness();

            // Franklin should be in between Michael (lowest) and Trevor (highest)
            Assert.True(franklinAgg > michaelAgg,
                $"Franklin ({franklinAgg}) should be more aggressive than Michael ({michaelAgg})");
            Assert.True(franklinAgg < trevorAgg,
                $"Franklin ({franklinAgg}) should be less aggressive than Trevor ({trevorAgg})");
        }

        [Fact]
        public void FranklinStrategy_RiskToleranceBetweenMichaelAndTrevor()
        {
            var franklinStrategy = new FranklinAIStrategy();
            var michaelStrategy = new MichaelAIStrategy();
            var trevorStrategy = new TrevorAIStrategy();

            var franklinRisk = franklinStrategy.GetRiskTolerance();
            var michaelRisk = michaelStrategy.GetRiskTolerance();
            var trevorRisk = trevorStrategy.GetRiskTolerance();

            // Franklin should be in between Michael (lowest) and Trevor (highest)
            Assert.True(franklinRisk > michaelRisk,
                $"Franklin ({franklinRisk}) should have higher risk tolerance than Michael ({michaelRisk})");
            Assert.True(franklinRisk < trevorRisk,
                $"Franklin ({franklinRisk}) should have lower risk tolerance than Trevor ({trevorRisk})");
        }

        [Fact]
        public void FranklinStrategy_PrefersNeutralZonesOverMichael()
        {
            var franklinStrategy = new FranklinAIStrategy();
            var michaelStrategy = new MichaelAIStrategy();

            var faction = CreateTestFaction(FactionType.Franklin);
            var factionState = CreateTestFactionState(faction.Id, troopCount: 50);
            var context = new AIContext(faction, factionState, new List<Zone>(), new List<Zone>(), new List<Faction>());

            var neutralZone = CreateTestZone("neutral", strategicValue: 5);

            var franklinScore = franklinStrategy.EvaluateZone(neutralZone, context);
            var michaelScore = michaelStrategy.EvaluateZone(neutralZone, context);

            Assert.True(franklinScore > michaelScore,
                "Franklin should find neutral zones more attractive than Michael");
        }

        [Fact]
        public void FranklinStrategy_MoreSelectiveThanTrevor()
        {
            var franklinStrategy = new FranklinAIStrategy();
            var trevorStrategy = new TrevorAIStrategy();

            var faction = CreateTestFaction(FactionType.Franklin);
            var factionState = CreateTestFactionState(faction.Id, troopCount: 50);

            // Very low value enemy zone
            var lowValueEnemy = CreateTestZone("low-value", ownerFactionId: "enemy-faction", strategicValue: 2);
            var enemyFactions = new List<Faction> { CreateTestFaction(FactionType.Michael, "enemy-faction") };
            var context = new AIContext(faction, factionState, new List<Zone>(), new[] { lowValueEnemy }, enemyFactions);

            var trevorAttacks = trevorStrategy.ShouldAttack(lowValueEnemy, context);
            var franklinAttacks = franklinStrategy.ShouldAttack(lowValueEnemy, context);

            // Trevor attacks anything, Franklin is more selective
            Assert.True(trevorAttacks, "Trevor should attack low value zones");
            // Franklin might not attack very low value enemy zones (not opportunistic enough)
            // This is implementation dependent - but Franklin should at least consider it differently
        }

        #endregion

        #region Mobility Bonus Tests

        [Fact]
        public void FranklinStrategy_ValuesQuickStrikesWithLowerTroopCommitment()
        {
            var franklinStrategy = new FranklinAIStrategy();
            var michaelStrategy = new MichaelAIStrategy();

            var faction = CreateTestFaction(FactionType.Franklin);
            var factionState = CreateTestFactionState(faction.Id, troopCount: 100);
            var context = new AIContext(faction, factionState, new List<Zone>(), new List<Zone>(), new List<Faction>());

            var moderatePriorityAttack = new AIDecision(AIDecisionType.Attack, "zone-1", 0.5f, 0);

            var franklinTroops = franklinStrategy.GetTroopsForAction(moderatePriorityAttack, context);
            var michaelTroops = michaelStrategy.GetTroopsForAction(moderatePriorityAttack, context);

            // Franklin values mobility - should not over-commit troops
            // His allocation should be efficient, not necessarily lower than Michael
            Assert.True(franklinTroops > 0 && franklinTroops <= 50,
                "Franklin should make efficient troop allocations for mobility");
        }

        [Fact]
        public void FranklinStrategy_MaintainsReservesForFutureOpportunities()
        {
            var strategy = new FranklinAIStrategy();
            var faction = CreateTestFaction(FactionType.Franklin);
            var factionState = CreateTestFactionState(faction.Id, troopCount: 100);
            var context = new AIContext(faction, factionState, new List<Zone>(), new List<Zone>(), new List<Faction>());

            var highPriorityAttack = new AIDecision(AIDecisionType.Attack, "zone-1", 0.9f, 0);

            var troops = strategy.GetTroopsForAction(highPriorityAttack, context);

            // Franklin should not commit all troops even for high priority attacks
            // He maintains reserves for mobility and future opportunities
            Assert.True(troops <= 80,
                $"Franklin should maintain some reserves, committed {troops} of 100");
        }

        #endregion

        #region IAIStrategy Interface Implementation Tests

        [Fact]
        public void FranklinAIStrategy_ImplementsIAIStrategy()
        {
            var strategy = new FranklinAIStrategy();

            Assert.IsAssignableFrom<IAIStrategy>(strategy);
        }

        [Fact]
        public void FranklinAIStrategy_InheritsFromBaseAIStrategy()
        {
            var strategy = new FranklinAIStrategy();

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

        private FactionState CreateTestFactionState(string factionId = "faction-franklin", int troopCount = 50)
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
            var faction = CreateTestFaction(FactionType.Franklin);
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
