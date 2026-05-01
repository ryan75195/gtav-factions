using System;
using System.Collections.Generic;
using FactionWars.AI.Interfaces;
using FactionWars.AI.Models;
using FactionWars.AI.Services;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using Vector3 = FactionWars.Core.Interfaces.Vector3;
using FactionWars.Factions.Models;
using FactionWars.Territory.Models;
using Moq;
using Xunit;

namespace FactionWars.Tests.Unit.AI.Services
{
    /// <summary>
    /// Unit tests for CapitalDeploymentService implementation.
    /// </summary>
    public class CapitalDeploymentServiceTests
    {
        private readonly Mock<IAIBudgetService> _mockBudgetService;
        private readonly Mock<IZoneDefenderAllocationService> _mockAllocationService;
        private readonly CapitalDeploymentService _service;

        public CapitalDeploymentServiceTests()
        {
            _mockBudgetService = new Mock<IAIBudgetService>();
            _mockAllocationService = new Mock<IZoneDefenderAllocationService>();
            _service = new CapitalDeploymentService(_mockBudgetService.Object, _mockAllocationService.Object);

            // Default budget service behavior
            _mockBudgetService.Setup(b => b.CanAffordAttack(It.IsAny<int>(), It.IsAny<int>())).Returns(true);
        }

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

        private FactionState CreateTestFactionState(string factionId = "faction-michael", int cash = 10000, int troops = 50)
        {
            return new FactionState(
                factionId: factionId,
                initialCash: cash,
                initialTroopCount: troops);
        }

        private Zone CreateTestZone(string id, string? ownerFactionId = null, int strategicValue = 5, bool isContested = false)
        {
            var zone = new Zone(
                id: id,
                name: $"Test Zone {id}",
                center: new Vector3(0, 0, 0),
                radius: 150f,
                strategicValue: strategicValue);
            zone.OwnerFactionId = ownerFactionId;
            zone.IsContested = isContested;
            return zone;
        }

        private AIContext CreateTestContext(
            Faction? faction = null,
            FactionState? factionState = null,
            List<Zone>? ownedZones = null,
            List<Zone>? allZones = null,
            List<Faction>? enemyFactions = null)
        {
            faction ??= CreateTestFaction(FactionType.Michael);
            factionState ??= CreateTestFactionState(faction.Id);
            ownedZones ??= new List<Zone> { CreateTestZone("zone-1", ownerFactionId: faction.Id) };
            allZones ??= new List<Zone>
            {
                CreateTestZone("zone-1", ownerFactionId: faction.Id),
                CreateTestZone("zone-2", ownerFactionId: "enemy-faction"),
                CreateTestZone("zone-3")
            };
            enemyFactions ??= new List<Faction> { CreateTestFaction(FactionType.Trevor, "enemy-faction") };

            return new AIContext(faction, factionState, ownedZones, allZones, enemyFactions);
        }

        private void SetupZoneAllocation(string factionId, string zoneId, int totalTroops)
        {
            var allocation = new ZoneDefenderAllocation(factionId, zoneId);
            allocation.AddTroops(DefenderTier.Basic, totalTroops);
            _mockAllocationService
                .Setup(a => a.GetAllocation(factionId, zoneId))
                .Returns(allocation);
        }

        #endregion

        #region GetScaledRecruitmentMax Tests

        [Theory]
        [InlineData(0, 10)]       // Zero cash -> base rate of 10
        [InlineData(5000, 10)]    // $5k -> 10 + 0 = 10
        [InlineData(10000, 11)]   // $10k -> 10 + 1 = 11
        [InlineData(50000, 15)]   // $50k -> 10 + 5 = 15
        [InlineData(100000, 20)]  // $100k -> 10 + 10 = 20
        [InlineData(400000, 50)]  // $400k -> 10 + 40 = 50 (max)
        [InlineData(1000000, 50)] // $1M -> capped at 50
        public void GetScaledRecruitmentMax_ReturnsCorrectValue(int cash, int expected)
        {
            var result = _service.GetScaledRecruitmentMax(cash);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void GetScaledRecruitmentMax_NegativeCash_TreatedAsZero()
        {
            // Negative cash shouldn't happen, but service should handle gracefully
            var result = _service.GetScaledRecruitmentMax(-1000);

            Assert.Equal(10, result); // Base rate
        }

        #endregion

        #region GetOverwhelmingAttackForce Tests

        [Theory]
        [InlineData(100, 10, 50)]  // 10 * 3 = 30, 100 * 0.5 = 50 -> max is 50
        [InlineData(100, 20, 60)]  // 20 * 3 = 60, 100 * 0.5 = 50 -> max is 60
        [InlineData(50, 10, 30)]   // 10 * 3 = 30, 50 * 0.5 = 25 -> max is 30
        [InlineData(20, 5, 15)]    // 5 * 3 = 15, 20 * 0.5 = 10 -> max is 15
        [InlineData(10, 0, 5)]     // 0 * 3 = 0, 10 * 0.5 = 5 -> max is 5
        public void GetOverwhelmingAttackForce_ReturnsCorrectValue(int availableTroops, int enemyDefenders, int expected)
        {
            var result = _service.GetOverwhelmingAttackForce(availableTroops, enemyDefenders);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void GetOverwhelmingAttackForce_ZeroTroops_ReturnsOverwhelmingForce()
        {
            // Even with 0 available troops, the formula calculates what would be needed
            // to overwhelm the enemy. This is the theoretical force needed.
            // 10 * 3 = 30, 0 * 0.5 = 0, max(30, 0) = 30
            var result = _service.GetOverwhelmingAttackForce(0, 10);

            Assert.Equal(30, result);
        }

        #endregion

        #region GetDefensePriority Tests

        [Fact]
        public void GetDefensePriority_ContestedZone_ReturnsHighPriority()
        {
            // Contested zones have threat level 2.0
            var zone = CreateTestZone("zone-1", ownerFactionId: "faction-michael", strategicValue: 10, isContested: true);
            var context = CreateTestContext(ownedZones: new List<Zone> { zone });

            // Setup: no defenders allocated
            _mockAllocationService.Setup(a => a.GetAllocation(It.IsAny<string>(), It.IsAny<string>())).Returns((ZoneDefenderAllocation?)null);

            var result = _service.GetDefensePriority(zone, context);

            // threatLevel(2.0) * zoneValue(1.0) * (10 / 1) = 20.0 -> but clamped or very high
            Assert.True(result > 0.5f, $"Contested zone should have high priority, got {result}");
        }

        [Fact]
        public void GetDefensePriority_SafeZone_ReturnsLowPriority()
        {
            // Safe zone with no adjacent enemies and not contested
            var ownedZone = CreateTestZone("zone-1", ownerFactionId: "faction-michael", strategicValue: 5);
            var context = CreateTestContext(
                ownedZones: new List<Zone> { ownedZone },
                allZones: new List<Zone> { ownedZone }); // No enemy zones

            _mockAllocationService.Setup(a => a.GetAllocation(It.IsAny<string>(), It.IsAny<string>())).Returns((ZoneDefenderAllocation?)null);

            var result = _service.GetDefensePriority(ownedZone, context);

            Assert.Equal(0f, result); // No threat = 0 priority
        }

        [Fact]
        public void GetDefensePriority_ZoneWithDefenders_ReducesPriority()
        {
            // Zone with defenders should have lower priority than without
            var zone = CreateTestZone("zone-1", ownerFactionId: "faction-michael", strategicValue: 10, isContested: true);
            var context = CreateTestContext(ownedZones: new List<Zone> { zone });

            // Setup: 10 defenders allocated
            SetupZoneAllocation("faction-michael", "zone-1", 10);

            var result = _service.GetDefensePriority(zone, context);

            // With defenders, priority should be lower
            // threatLevel(2.0) * zoneValue(1.0) * (10 / 10) = 2.0
            Assert.True(result < 5f, $"Zone with defenders should have reduced priority, got {result}");
        }

        [Fact]
        public void GetDefensePriority_AdjacentToEnemy_ReturnsModeratePriority()
        {
            // Zone adjacent to enemy territory but not contested
            var ownedZone = CreateTestZone("zone-1", ownerFactionId: "faction-michael", strategicValue: 8);
            var enemyZone = CreateTestZone("zone-2", ownerFactionId: "enemy-faction");
            ownedZone.AdjacentZoneIds.Add("zone-2");

            var context = CreateTestContext(
                ownedZones: new List<Zone> { ownedZone },
                allZones: new List<Zone> { ownedZone, enemyZone });

            _mockAllocationService.Setup(a => a.GetAllocation(It.IsAny<string>(), It.IsAny<string>())).Returns((ZoneDefenderAllocation?)null);

            var result = _service.GetDefensePriority(ownedZone, context);

            // threatLevel(0.5) * zoneValue(0.8) * (10 / 1) = 4.0
            Assert.True(result > 0f, "Zone adjacent to enemy should have some priority");
            Assert.True(result < 10f, "Should not be as high as contested zone");
        }

        [Fact]
        public void GetDefensePriority_HighStrategicValue_IncreasesScore()
        {
            // Higher strategic value should increase defense priority
            var lowValueZone = CreateTestZone("zone-low", ownerFactionId: "faction-michael", strategicValue: 1, isContested: true);
            var highValueZone = CreateTestZone("zone-high", ownerFactionId: "faction-michael", strategicValue: 10, isContested: true);

            var context = CreateTestContext(ownedZones: new List<Zone> { lowValueZone, highValueZone });
            _mockAllocationService.Setup(a => a.GetAllocation(It.IsAny<string>(), It.IsAny<string>())).Returns((ZoneDefenderAllocation?)null);

            var lowResult = _service.GetDefensePriority(lowValueZone, context);
            var highResult = _service.GetDefensePriority(highValueZone, context);

            Assert.True(highResult > lowResult, $"High value zone ({highResult}) should have higher priority than low value ({lowResult})");
        }

        #endregion

        #region GetAttackOpportunity Tests

        [Fact]
        public void GetAttackOpportunity_WeakEnemy_ReturnsHighOpportunity()
        {
            // Many troops vs few defenders = high opportunity
            var faction = CreateTestFaction(FactionType.Michael);
            var factionState = CreateTestFactionState(faction.Id, troops: 100);
            var targetZone = CreateTestZone("target", ownerFactionId: "enemy-faction", strategicValue: 10);

            var context = CreateTestContext(faction: faction, factionState: factionState);

            // Enemy has only 5 defenders
            SetupZoneAllocation("enemy-faction", "target", 5);

            var result = _service.GetAttackOpportunity(targetZone, context);

            // winProbability = 100 / (5 * 2 + 1) = 100 / 11 = ~9.09 -> capped at 1.0
            // zoneValue = 10 / 10 = 1.0
            // affordability = 1.0
            // result = min(1, 9.09) * 1.0 * 1.0 = 1.0
            Assert.True(result >= 0.7f, $"Should be high opportunity against weak enemy, got {result}");
        }

        [Fact]
        public void GetAttackOpportunity_StrongEnemy_ReturnsLowOpportunity()
        {
            // Few troops vs many defenders = low opportunity
            var faction = CreateTestFaction(FactionType.Michael);
            var factionState = CreateTestFactionState(faction.Id, troops: 20);
            var targetZone = CreateTestZone("target", ownerFactionId: "enemy-faction", strategicValue: 10);

            var context = CreateTestContext(faction: faction, factionState: factionState);

            // Enemy has 50 defenders
            SetupZoneAllocation("enemy-faction", "target", 50);

            var result = _service.GetAttackOpportunity(targetZone, context);

            // winProbability = 20 / (50 * 2 + 1) = 20 / 101 = ~0.198
            // zoneValue = 10 / 10 = 1.0
            // result = 0.198 * 1.0 * 1.0 = 0.198
            Assert.True(result < 0.5f, $"Should be low opportunity against strong enemy, got {result}");
        }

        [Fact]
        public void GetAttackOpportunity_ZeroCash_StillReturnsOpportunity_DeploymentIsFree()
        {
            // After deployment cost removal: factions with troops can attack regardless of cash
            var faction = CreateTestFaction(FactionType.Michael);
            var factionState = CreateTestFactionState(faction.Id, troops: 100, cash: 0);
            var targetZone = CreateTestZone("target", ownerFactionId: "enemy-faction", strategicValue: 10);

            var context = CreateTestContext(faction: faction, factionState: factionState);

            // CostPerTroop = 0 means deployment is free
            _mockBudgetService.Setup(b => b.CostPerTroop).Returns(0);

            // Enemy has no defenders
            _mockAllocationService.Setup(a => a.GetAllocation("enemy-faction", "target")).Returns((ZoneDefenderAllocation?)null);

            var result = _service.GetAttackOpportunity(targetZone, context);

            // With 100 troops vs 0 defenders, opportunity should be high
            Assert.True(result > 0f, $"Faction with troops but no cash should still have attack opportunity, got {result}");
        }

        [Fact]
        public void GetAttackOpportunity_LargeTroopCount_UsesAllTroops_DeploymentIsFree()
        {
            // After deployment cost removal: troops are not cash-limited
            // Trevor scenario - 445 troops can all be used regardless of cash
            var faction = CreateTestFaction(FactionType.Trevor, "trevor");
            var factionState = CreateTestFactionState(faction.Id, troops: 445, cash: 0); // Zero cash
            var targetZone = CreateTestZone("target", ownerFactionId: "enemy-faction", strategicValue: 5);

            var context = CreateTestContext(faction: faction, factionState: factionState);

            // Enemy has 10 defenders
            SetupZoneAllocation("enemy-faction", "target", 10);

            // CostPerTroop = 0 means deployment is free
            _mockBudgetService.Setup(b => b.CostPerTroop).Returns(0);

            var result = _service.GetAttackOpportunity(targetZone, context);

            // 445 troops vs 10 defenders = overwhelming advantage
            Assert.True(result > 0.4f, $"Should have high opportunity with 445 troops vs 10 defenders, got {result}");
        }

        [Fact]
        public void GetAttackOpportunity_LowStrategicValue_ReducesOpportunity()
        {
            var faction = CreateTestFaction(FactionType.Michael);
            var factionState = CreateTestFactionState(faction.Id, troops: 100);
            var lowValueTarget = CreateTestZone("target-low", ownerFactionId: "enemy-faction", strategicValue: 1);
            var highValueTarget = CreateTestZone("target-high", ownerFactionId: "enemy-faction", strategicValue: 10);

            var context = CreateTestContext(faction: faction, factionState: factionState);

            // Both have same defenders
            SetupZoneAllocation("enemy-faction", "target-low", 10);
            SetupZoneAllocation("enemy-faction", "target-high", 10);

            var lowResult = _service.GetAttackOpportunity(lowValueTarget, context);
            var highResult = _service.GetAttackOpportunity(highValueTarget, context);

            Assert.True(highResult > lowResult, $"High value target ({highResult}) should be better opportunity than low value ({lowResult})");
        }

        [Fact]
        public void GetAttackOpportunity_UndefendedZone_ReturnsHighOpportunity()
        {
            var faction = CreateTestFaction(FactionType.Michael);
            var factionState = CreateTestFactionState(faction.Id, troops: 50);
            var targetZone = CreateTestZone("target", ownerFactionId: "enemy-faction", strategicValue: 10);

            var context = CreateTestContext(faction: faction, factionState: factionState);

            // No defenders
            _mockAllocationService.Setup(a => a.GetAllocation("enemy-faction", "target")).Returns((ZoneDefenderAllocation?)null);

            var result = _service.GetAttackOpportunity(targetZone, context);

            // winProbability = 50 / (0 * 2 + 1) = 50 / 1 = 50 -> capped at 1.0
            Assert.Equal(1f, result);
        }

        [Fact]
        public void GetAttackOpportunity_NeutralZone_ReturnsOpportunityBasedOnValue()
        {
            var faction = CreateTestFaction(FactionType.Michael);
            var factionState = CreateTestFactionState(faction.Id, troops: 50);
            var neutralZone = CreateTestZone("neutral", ownerFactionId: null, strategicValue: 8);

            var context = CreateTestContext(faction: faction, factionState: factionState);

            // Neutral zones have no defenders
            _mockAllocationService.Setup(a => a.GetAllocation(It.IsAny<string>(), "neutral")).Returns((ZoneDefenderAllocation?)null);

            var result = _service.GetAttackOpportunity(neutralZone, context);

            // winProbability = 50 / 1 = 50 -> capped at 1.0
            // zoneValue = 8 / 10 = 0.8
            Assert.True(result > 0.7f, $"Undefended neutral zone should be good opportunity, got {result}");
        }

        #endregion

        #region GetBestDecision Tests

        [Fact]
        public void GetBestDecision_HighDefenseNeed_ReturnsDefendDecision()
        {
            // Setup: Owned zone is contested, no adjacent attack targets
            var faction = CreateTestFaction(FactionType.Michael);
            var ownedZone = CreateTestZone("zone-1", ownerFactionId: faction.Id, strategicValue: 10, isContested: true);

            var context = CreateTestContext(
                faction: faction,
                ownedZones: new List<Zone> { ownedZone },
                allZones: new List<Zone> { ownedZone }); // No enemy zones to attack

            _mockAllocationService.Setup(a => a.GetAllocation(It.IsAny<string>(), It.IsAny<string>())).Returns((ZoneDefenderAllocation?)null);

            var result = _service.GetBestDecision(context);

            Assert.NotNull(result);
            Assert.Equal(AIDecisionType.Defend, result!.DecisionType);
            Assert.Equal("zone-1", result.TargetZoneId);
        }

        [Fact]
        public void GetBestDecision_HighAttackOpportunity_ReturnsAttackDecision()
        {
            // Setup: Safe owned zone (not adjacent to enemy from owned perspective),
            // but enemy zone IS adjacent to owned (making it attackable)
            var faction = CreateTestFaction(FactionType.Michael);
            var factionState = CreateTestFactionState(faction.Id, troops: 100);
            var ownedZone = CreateTestZone("owned", ownerFactionId: faction.Id, strategicValue: 5);
            var enemyZone = CreateTestZone("enemy", ownerFactionId: "enemy-faction", strategicValue: 10);

            // Only enemy zone lists owned as adjacent - this makes owned safe (no adjacent enemy territory)
            // but enemy zone is still attackable (because it lists owned as adjacent)
            enemyZone.AdjacentZoneIds.Add("owned");

            var context = CreateTestContext(
                faction: faction,
                factionState: factionState,
                ownedZones: new List<Zone> { ownedZone },
                allZones: new List<Zone> { ownedZone, enemyZone });

            // Enemy zone has few defenders
            SetupZoneAllocation("enemy-faction", "enemy", 5);
            _mockAllocationService.Setup(a => a.GetAllocation(faction.Id, It.IsAny<string>())).Returns((ZoneDefenderAllocation?)null);

            var result = _service.GetBestDecision(context);

            Assert.NotNull(result);
            Assert.Equal(AIDecisionType.Attack, result!.DecisionType);
            Assert.Equal("enemy", result.TargetZoneId);
        }

        [Fact]
        public void GetBestDecision_LowOpportunity_ReturnsNull()
        {
            // Setup: Safe owned zone, strong enemies (low attack opportunity)
            var faction = CreateTestFaction(FactionType.Michael);
            var factionState = CreateTestFactionState(faction.Id, troops: 10); // Few troops
            var ownedZone = CreateTestZone("owned", ownerFactionId: faction.Id, strategicValue: 5);
            var enemyZone = CreateTestZone("enemy", ownerFactionId: "enemy-faction", strategicValue: 5);

            // Enemy zone lists owned as adjacent (making enemy attackable)
            // but owned doesn't list enemy (keeping owned safe from defense priority)
            enemyZone.AdjacentZoneIds.Add("owned");

            var context = CreateTestContext(
                faction: faction,
                factionState: factionState,
                ownedZones: new List<Zone> { ownedZone },
                allZones: new List<Zone> { ownedZone, enemyZone });

            // Enemy has many defenders
            SetupZoneAllocation("enemy-faction", "enemy", 50);
            _mockAllocationService.Setup(a => a.GetAllocation(faction.Id, It.IsAny<string>())).Returns((ZoneDefenderAllocation?)null);

            var result = _service.GetBestDecision(context);

            // Attack opportunity too low (<0.7), no defense need -> Hold (null)
            Assert.Null(result);
        }

        [Fact]
        public void GetBestDecision_DefenseHigherThanAttack_PrefersDefense()
        {
            // Setup: Contested zone AND weak enemy, defense should win
            var faction = CreateTestFaction(FactionType.Michael);
            var factionState = CreateTestFactionState(faction.Id, troops: 50);
            var contestedZone = CreateTestZone("contested", ownerFactionId: faction.Id, strategicValue: 10, isContested: true);
            var enemyZone = CreateTestZone("enemy", ownerFactionId: "enemy-faction", strategicValue: 3);
            contestedZone.AdjacentZoneIds.Add("enemy");

            var context = CreateTestContext(
                faction: faction,
                factionState: factionState,
                ownedZones: new List<Zone> { contestedZone },
                allZones: new List<Zone> { contestedZone, enemyZone });

            // Both have some defenders
            SetupZoneAllocation(faction.Id, "contested", 5);
            SetupZoneAllocation("enemy-faction", "enemy", 10);

            var result = _service.GetBestDecision(context);

            Assert.NotNull(result);
            Assert.Equal(AIDecisionType.Defend, result!.DecisionType);
        }

        [Fact]
        public void GetBestDecision_NoOwnedZones_ReturnsNull()
        {
            var faction = CreateTestFaction(FactionType.Michael);
            var context = CreateTestContext(
                faction: faction,
                ownedZones: new List<Zone>(),
                allZones: new List<Zone> { CreateTestZone("enemy", ownerFactionId: "enemy-faction") });

            var result = _service.GetBestDecision(context);

            Assert.Null(result);
        }

        [Fact]
        public void GetBestDecision_AttackDecision_IncludesTroopCommitment()
        {
            // Verify attack decision includes troop count
            var faction = CreateTestFaction(FactionType.Michael);
            var factionState = CreateTestFactionState(faction.Id, troops: 100);
            var ownedZone = CreateTestZone("owned", ownerFactionId: faction.Id);
            var enemyZone = CreateTestZone("enemy", ownerFactionId: "enemy-faction", strategicValue: 10);

            // Enemy zone lists owned as adjacent (making enemy attackable)
            // but owned doesn't list enemy (keeping owned safe from defense priority)
            enemyZone.AdjacentZoneIds.Add("owned");

            var context = CreateTestContext(
                faction: faction,
                factionState: factionState,
                ownedZones: new List<Zone> { ownedZone },
                allZones: new List<Zone> { ownedZone, enemyZone });

            SetupZoneAllocation("enemy-faction", "enemy", 10);
            _mockAllocationService.Setup(a => a.GetAllocation(faction.Id, It.IsAny<string>())).Returns((ZoneDefenderAllocation?)null);

            var result = _service.GetBestDecision(context);

            Assert.NotNull(result);
            Assert.Equal(AIDecisionType.Attack, result!.DecisionType);
            // Overwhelming force: max(10 * 3, 100 * 0.5) = max(30, 50) = 50
            Assert.Equal(50, result.TroopsToCommit);
        }

        #endregion

        #region CalculateThreatLevel Tests (via GetDefensePriority)

        [Fact]
        public void CalculateThreatLevel_ContestedZone_Returns2()
        {
            // Contested zones have highest threat
            var zone = CreateTestZone("zone-1", ownerFactionId: "faction-michael", strategicValue: 10, isContested: true);
            var context = CreateTestContext(ownedZones: new List<Zone> { zone });

            // With max strategic value and no defenders, contested threat shows through
            _mockAllocationService.Setup(a => a.GetAllocation(It.IsAny<string>(), It.IsAny<string>())).Returns((ZoneDefenderAllocation?)null);

            var priority = _service.GetDefensePriority(zone, context);

            // threatLevel(2.0) * zoneValue(1.0) * (10 / 1) = 20.0
            Assert.Equal(20f, priority);
        }

        [Fact]
        public void CalculateThreatLevel_AdjacentEnemy_Returns05()
        {
            var ownedZone = CreateTestZone("owned", ownerFactionId: "faction-michael", strategicValue: 10);
            var enemyZone = CreateTestZone("enemy", ownerFactionId: "enemy-faction");
            ownedZone.AdjacentZoneIds.Add("enemy");

            var context = CreateTestContext(
                ownedZones: new List<Zone> { ownedZone },
                allZones: new List<Zone> { ownedZone, enemyZone });

            _mockAllocationService.Setup(a => a.GetAllocation(It.IsAny<string>(), It.IsAny<string>())).Returns((ZoneDefenderAllocation?)null);

            var priority = _service.GetDefensePriority(ownedZone, context);

            // threatLevel(0.5) * zoneValue(1.0) * (10 / 1) = 5.0
            Assert.Equal(5f, priority);
        }

        [Fact]
        public void CalculateThreatLevel_SafeZone_Returns0()
        {
            var safeZone = CreateTestZone("safe", ownerFactionId: "faction-michael", strategicValue: 10);
            // No adjacent enemy zones
            var context = CreateTestContext(
                ownedZones: new List<Zone> { safeZone },
                allZones: new List<Zone> { safeZone });

            _mockAllocationService.Setup(a => a.GetAllocation(It.IsAny<string>(), It.IsAny<string>())).Returns((ZoneDefenderAllocation?)null);

            var priority = _service.GetDefensePriority(safeZone, context);

            Assert.Equal(0f, priority);
        }

        #endregion

        #region Edge Cases

        [Fact]
        public void GetDefensePriority_NullZone_ThrowsArgumentNullException()
        {
            var context = CreateTestContext();

            Assert.Throws<ArgumentNullException>(() => _service.GetDefensePriority(null!, context));
        }

        [Fact]
        public void GetDefensePriority_NullContext_ThrowsArgumentNullException()
        {
            var zone = CreateTestZone("zone-1");

            Assert.Throws<ArgumentNullException>(() => _service.GetDefensePriority(zone, null!));
        }

        [Fact]
        public void GetAttackOpportunity_NullTarget_ThrowsArgumentNullException()
        {
            var context = CreateTestContext();

            Assert.Throws<ArgumentNullException>(() => _service.GetAttackOpportunity(null!, context));
        }

        [Fact]
        public void GetAttackOpportunity_NullContext_ThrowsArgumentNullException()
        {
            var zone = CreateTestZone("zone-1");

            Assert.Throws<ArgumentNullException>(() => _service.GetAttackOpportunity(zone, null!));
        }

        [Fact]
        public void GetBestDecision_NullContext_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _service.GetBestDecision(null!));
        }

        [Fact]
        public void Constructor_NullBudgetService_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new CapitalDeploymentService(null!, _mockAllocationService.Object));
        }

        [Fact]
        public void Constructor_NullAllocationService_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new CapitalDeploymentService(_mockBudgetService.Object, null!));
        }

        #endregion
    }
}
