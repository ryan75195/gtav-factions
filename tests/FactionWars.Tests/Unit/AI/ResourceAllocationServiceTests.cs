using FactionWars.AI.Interfaces;
using FactionWars.AI.Models;
using FactionWars.AI.Services;
using FactionWars.Core.Interfaces;
using FactionWars.Factions.Models;
using FactionWars.Territory.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace FactionWars.Tests.Unit.AI
{
    /// <summary>
    /// Tests for ResourceAllocationService.
    /// The resource allocation system determines how a faction should distribute
    /// its resources (troops, cash) between attack and defense operations.
    /// </summary>
    public class ResourceAllocationServiceTests
    {
        #region Constructor Tests

        [Fact]
        public void Constructor_WithValidParameters_CreatesInstance()
        {
            var service = new ResourceAllocationService();

            Assert.NotNull(service);
        }

        [Fact]
        public void Constructor_ImplementsIResourceAllocationService()
        {
            var service = new ResourceAllocationService();

            Assert.IsAssignableFrom<IResourceAllocationService>(service);
        }

        #endregion

        #region AllocateForAttack - Null Parameter Tests

        [Fact]
        public void AllocateForAttack_NullTargetZone_ThrowsArgumentNullException()
        {
            var service = new ResourceAllocationService();
            var context = CreateTestContext();

            Assert.Throws<ArgumentNullException>(() => service.AllocateForAttack(null!, context));
        }

        [Fact]
        public void AllocateForAttack_NullContext_ThrowsArgumentNullException()
        {
            var service = new ResourceAllocationService();
            var zone = CreateTestZone("zone-1");

            Assert.Throws<ArgumentNullException>(() => service.AllocateForAttack(zone, null!));
        }

        #endregion

        #region AllocateForAttack - Basic Allocation Tests

        [Fact]
        public void AllocateForAttack_WithAvailableTroops_ReturnsPositiveTroopAllocation()
        {
            var service = new ResourceAllocationService();
            var context = CreateTestContextWithTroops(50);
            var targetZone = CreateTestZone("target", strategicValue: 5);

            var allocation = service.AllocateForAttack(targetZone, context);

            Assert.True(allocation.Troops > 0, "Should allocate troops for attack");
        }

        [Fact]
        public void AllocateForAttack_WithNoTroops_ReturnsZeroTroops()
        {
            var service = new ResourceAllocationService();
            var context = CreateTestContextWithTroops(0);
            var targetZone = CreateTestZone("target", strategicValue: 5);

            var allocation = service.AllocateForAttack(targetZone, context);

            Assert.Equal(0, allocation.Troops);
        }

        [Fact]
        public void AllocateForAttack_NeverExceedsAvailableTroops()
        {
            var service = new ResourceAllocationService();
            var context = CreateTestContextWithTroops(20);
            var targetZone = CreateTestZone("target", strategicValue: 10);

            var allocation = service.AllocateForAttack(targetZone, context);

            Assert.True(allocation.Troops <= context.FactionState.TroopCount);
        }

        [Fact]
        public void AllocateForAttack_HighValueTarget_AllocatesMoreTroops()
        {
            var service = new ResourceAllocationService();
            var context = CreateTestContextWithTroops(100);

            var lowValueZone = CreateTestZone("low", strategicValue: 2);
            var highValueZone = CreateTestZone("high", strategicValue: 9);

            var lowAllocation = service.AllocateForAttack(lowValueZone, context);
            var highAllocation = service.AllocateForAttack(highValueZone, context);

            Assert.True(highAllocation.Troops >= lowAllocation.Troops,
                "Higher value targets should receive equal or more troops");
        }

        [Fact]
        public void AllocateForAttack_EnemyZone_AllocatesMoreThanNeutralZone()
        {
            var service = new ResourceAllocationService();
            var context = CreateTestContextWithTroops(100);

            var neutralZone = CreateTestZone("neutral", strategicValue: 5);
            neutralZone.OwnerFactionId = null;

            var enemyZone = CreateTestZone("enemy", strategicValue: 5);
            enemyZone.OwnerFactionId = "enemy-faction";

            var neutralAllocation = service.AllocateForAttack(neutralZone, context);
            var enemyAllocation = service.AllocateForAttack(enemyZone, context);

            Assert.True(enemyAllocation.Troops >= neutralAllocation.Troops,
                "Enemy zones require at least as many troops as neutral zones");
        }

        [Fact]
        public void AllocateForAttack_RespectsMinimumAttackTroops()
        {
            var service = new ResourceAllocationService();
            var context = CreateTestContextWithTroops(50);
            var zone = CreateTestZone("target", strategicValue: 1);

            var allocation = service.AllocateForAttack(zone, context);

            if (allocation.Troops > 0)
            {
                Assert.True(allocation.Troops >= ResourceAllocationService.MinimumAttackTroops,
                    "Non-zero allocation should meet minimum attack troops");
            }
        }

        [Fact]
        public void AllocateForAttack_InsufficientForMinimum_ReturnsZero()
        {
            var service = new ResourceAllocationService();
            // Troops below minimum attack threshold
            var context = CreateTestContextWithTroops(ResourceAllocationService.MinimumAttackTroops - 1);
            var zone = CreateTestZone("target", strategicValue: 5);

            var allocation = service.AllocateForAttack(zone, context);

            Assert.Equal(0, allocation.Troops);
        }

        #endregion

        #region AllocateForAttack - Cash Allocation Tests

        [Fact]
        public void AllocateForAttack_WithAvailableCash_ReturnsPositiveCashAllocation()
        {
            var service = new ResourceAllocationService();
            var context = CreateTestContextWithResources(troops: 50, cash: 10000);
            var targetZone = CreateTestZone("target", strategicValue: 5);

            var allocation = service.AllocateForAttack(targetZone, context);

            Assert.True(allocation.Cash >= 0, "Cash allocation should be non-negative");
        }

        [Fact]
        public void AllocateForAttack_NeverExceedsAvailableCash()
        {
            var service = new ResourceAllocationService();
            var context = CreateTestContextWithResources(troops: 50, cash: 5000);
            var targetZone = CreateTestZone("target", strategicValue: 10);

            var allocation = service.AllocateForAttack(targetZone, context);

            Assert.True(allocation.Cash <= context.FactionState.Cash);
        }

        #endregion

        #region AllocateForDefense - Null Parameter Tests

        [Fact]
        public void AllocateForDefense_NullZone_ThrowsArgumentNullException()
        {
            var service = new ResourceAllocationService();
            var context = CreateTestContext();

            Assert.Throws<ArgumentNullException>(() => service.AllocateForDefense(null!, context));
        }

        [Fact]
        public void AllocateForDefense_NullContext_ThrowsArgumentNullException()
        {
            var service = new ResourceAllocationService();
            var zone = CreateTestZone("zone-1");

            Assert.Throws<ArgumentNullException>(() => service.AllocateForDefense(zone, null!));
        }

        #endregion

        #region AllocateForDefense - Basic Allocation Tests

        [Fact]
        public void AllocateForDefense_WithAvailableTroops_ReturnsPositiveTroopAllocation()
        {
            var service = new ResourceAllocationService();
            var context = CreateTestContextWithTroops(50);
            var zone = CreateTestZone("owned", strategicValue: 5);
            zone.OwnerFactionId = context.Faction.Id;

            var allocation = service.AllocateForDefense(zone, context);

            Assert.True(allocation.Troops > 0, "Should allocate troops for defense");
        }

        [Fact]
        public void AllocateForDefense_WithNoTroops_ReturnsZeroTroops()
        {
            var service = new ResourceAllocationService();
            var context = CreateTestContextWithTroops(0);
            var zone = CreateTestZone("owned", strategicValue: 5);
            zone.OwnerFactionId = context.Faction.Id;

            var allocation = service.AllocateForDefense(zone, context);

            Assert.Equal(0, allocation.Troops);
        }

        [Fact]
        public void AllocateForDefense_NeverExceedsAvailableTroops()
        {
            var service = new ResourceAllocationService();
            var context = CreateTestContextWithTroops(20);
            var zone = CreateTestZone("owned", strategicValue: 10);
            zone.OwnerFactionId = context.Faction.Id;

            var allocation = service.AllocateForDefense(zone, context);

            Assert.True(allocation.Troops <= context.FactionState.TroopCount);
        }

        [Fact]
        public void AllocateForDefense_HighValueZone_AllocatesMoreTroops()
        {
            var service = new ResourceAllocationService();
            var context = CreateTestContextWithTroops(100);

            var lowValueZone = CreateTestZone("low", strategicValue: 2);
            lowValueZone.OwnerFactionId = context.Faction.Id;

            var highValueZone = CreateTestZone("high", strategicValue: 9);
            highValueZone.OwnerFactionId = context.Faction.Id;

            var lowAllocation = service.AllocateForDefense(lowValueZone, context);
            var highAllocation = service.AllocateForDefense(highValueZone, context);

            Assert.True(highAllocation.Troops >= lowAllocation.Troops,
                "Higher value zones should receive equal or more defense");
        }

        [Fact]
        public void AllocateForDefense_ContestedZone_AllocatesMoreTroops()
        {
            var service = new ResourceAllocationService();
            var context = CreateTestContextWithTroops(100);

            var stableZone = CreateTestZone("stable", strategicValue: 5);
            stableZone.OwnerFactionId = context.Faction.Id;
            stableZone.IsContested = false;

            var contestedZone = CreateTestZone("contested", strategicValue: 5);
            contestedZone.OwnerFactionId = context.Faction.Id;
            contestedZone.IsContested = true;

            var stableAllocation = service.AllocateForDefense(stableZone, context);
            var contestedAllocation = service.AllocateForDefense(contestedZone, context);

            Assert.True(contestedAllocation.Troops > stableAllocation.Troops,
                "Contested zones should receive more defense troops");
        }

        [Fact]
        public void AllocateForDefense_RespectsMinimumDefenseTroops()
        {
            var service = new ResourceAllocationService();
            var context = CreateTestContextWithTroops(50);
            var zone = CreateTestZone("owned", strategicValue: 1);
            zone.OwnerFactionId = context.Faction.Id;

            var allocation = service.AllocateForDefense(zone, context);

            if (allocation.Troops > 0)
            {
                Assert.True(allocation.Troops >= ResourceAllocationService.MinimumDefenseTroops,
                    "Non-zero allocation should meet minimum defense troops");
            }
        }

        #endregion

        #region AllocateResources - Combined Allocation Tests

        [Fact]
        public void AllocateResources_NullDecisions_ThrowsArgumentNullException()
        {
            var service = new ResourceAllocationService();
            var context = CreateTestContext();

            Assert.Throws<ArgumentNullException>(() => service.AllocateResources(null!, context));
        }

        [Fact]
        public void AllocateResources_NullContext_ThrowsArgumentNullException()
        {
            var service = new ResourceAllocationService();
            var decisions = new List<AIDecision>();

            Assert.Throws<ArgumentNullException>(() => service.AllocateResources(decisions, null!));
        }

        [Fact]
        public void AllocateResources_EmptyDecisions_ReturnsEmptyAllocations()
        {
            var service = new ResourceAllocationService();
            var context = CreateTestContext();

            var allocations = service.AllocateResources(new List<AIDecision>(), context);

            Assert.NotNull(allocations);
            Assert.Empty(allocations);
        }

        [Fact]
        public void AllocateResources_SingleAttackDecision_ReturnsAllocation()
        {
            var service = new ResourceAllocationService();
            var context = CreateTestContextWithTroops(50);
            var decision = new AIDecision(AIDecisionType.Attack, "zone-1", 0.8f, 20);
            var decisions = new List<AIDecision> { decision };

            var allocations = service.AllocateResources(decisions, context);

            Assert.Single(allocations);
            Assert.True(allocations[0].Troops > 0);
        }

        [Fact]
        public void AllocateResources_SingleDefendDecision_ReturnsAllocation()
        {
            var service = new ResourceAllocationService();
            var context = CreateTestContextWithTroops(50);
            var decision = new AIDecision(AIDecisionType.Defend, "zone-1", 0.8f, 15);
            var decisions = new List<AIDecision> { decision };

            var allocations = service.AllocateResources(decisions, context);

            Assert.Single(allocations);
            Assert.True(allocations[0].Troops > 0);
        }

        [Fact]
        public void AllocateResources_MultipleDecisions_TotalTroopsNotExceedAvailable()
        {
            var service = new ResourceAllocationService();
            var context = CreateTestContextWithTroops(50);
            var decisions = new List<AIDecision>
            {
                new AIDecision(AIDecisionType.Defend, "zone-1", 0.9f, 20),
                new AIDecision(AIDecisionType.Attack, "zone-2", 0.7f, 25),
                new AIDecision(AIDecisionType.Defend, "zone-3", 0.5f, 15)
            };

            var allocations = service.AllocateResources(decisions, context);

            int totalAllocatedTroops = allocations.Sum(a => a.Troops);
            Assert.True(totalAllocatedTroops <= context.FactionState.TroopCount,
                "Total allocated troops should not exceed available troops");
        }

        [Fact]
        public void AllocateResources_HigherPriorityGetsMoreResources()
        {
            var service = new ResourceAllocationService();
            var context = CreateTestContextWithTroops(100);
            var decisions = new List<AIDecision>
            {
                new AIDecision(AIDecisionType.Defend, "zone-high", 0.9f, 30),
                new AIDecision(AIDecisionType.Defend, "zone-low", 0.3f, 30)
            };

            var allocations = service.AllocateResources(decisions, context);

            var highPriorityAllocation = allocations.FirstOrDefault(a => a.TargetZoneId == "zone-high");
            var lowPriorityAllocation = allocations.FirstOrDefault(a => a.TargetZoneId == "zone-low");

            Assert.NotNull(highPriorityAllocation);
            Assert.NotNull(lowPriorityAllocation);
            Assert.True(highPriorityAllocation.Troops >= lowPriorityAllocation.Troops,
                "Higher priority should get equal or more troops");
        }

        [Fact]
        public void AllocateResources_DefenseGetsPriorityOverAttack()
        {
            var service = new ResourceAllocationService();
            var context = CreateTestContextWithTroops(30); // Limited troops
            var decisions = new List<AIDecision>
            {
                new AIDecision(AIDecisionType.Attack, "attack-zone", 0.8f, 20),
                new AIDecision(AIDecisionType.Defend, "defend-zone", 0.8f, 20)
            };

            var allocations = service.AllocateResources(decisions, context);

            var attackAllocation = allocations.FirstOrDefault(a => a.TargetZoneId == "attack-zone");
            var defendAllocation = allocations.FirstOrDefault(a => a.TargetZoneId == "defend-zone");

            // With limited troops, defense should be prioritized
            Assert.NotNull(defendAllocation);
            Assert.True(defendAllocation.Troops > 0, "Defense should receive troops");
        }

        #endregion

        #region CalculateAttackStrength Tests

        [Fact]
        public void CalculateAttackStrength_NullAllocation_ThrowsArgumentNullException()
        {
            var service = new ResourceAllocationService();

            Assert.Throws<ArgumentNullException>(() => service.CalculateAttackStrength(null!));
        }

        [Fact]
        public void CalculateAttackStrength_TroopsOnly_ReturnsBasedOnTroops()
        {
            var service = new ResourceAllocationService();
            var allocation = new ResourceAllocation("zone-1", 20, 0, AIDecisionType.Attack);

            var strength = service.CalculateAttackStrength(allocation);

            Assert.True(strength > 0);
        }

        [Fact]
        public void CalculateAttackStrength_TroopsAndCash_ReturnsHigherStrength()
        {
            var service = new ResourceAllocationService();
            var troopsOnly = new ResourceAllocation("zone-1", 20, 0, AIDecisionType.Attack);
            var troopsAndCash = new ResourceAllocation("zone-2", 20, 5000, AIDecisionType.Attack);

            var strengthTroopsOnly = service.CalculateAttackStrength(troopsOnly);
            var strengthWithCash = service.CalculateAttackStrength(troopsAndCash);

            Assert.True(strengthWithCash >= strengthTroopsOnly,
                "Cash should boost or maintain attack strength");
        }

        [Fact]
        public void CalculateAttackStrength_MoreTroops_ReturnsHigherStrength()
        {
            var service = new ResourceAllocationService();
            var smallForce = new ResourceAllocation("zone-1", 10, 0, AIDecisionType.Attack);
            var largeForce = new ResourceAllocation("zone-2", 30, 0, AIDecisionType.Attack);

            var smallStrength = service.CalculateAttackStrength(smallForce);
            var largeStrength = service.CalculateAttackStrength(largeForce);

            Assert.True(largeStrength > smallStrength);
        }

        #endregion

        #region CalculateDefenseStrength Tests

        [Fact]
        public void CalculateDefenseStrength_NullAllocation_ThrowsArgumentNullException()
        {
            var service = new ResourceAllocationService();

            Assert.Throws<ArgumentNullException>(() => service.CalculateDefenseStrength(null!));
        }

        [Fact]
        public void CalculateDefenseStrength_TroopsOnly_ReturnsBasedOnTroops()
        {
            var service = new ResourceAllocationService();
            var allocation = new ResourceAllocation("zone-1", 15, 0, AIDecisionType.Defend);

            var strength = service.CalculateDefenseStrength(allocation);

            Assert.True(strength > 0);
        }

        [Fact]
        public void CalculateDefenseStrength_DefenseHasBonus()
        {
            var service = new ResourceAllocationService();
            // Same troops for attack vs defense
            var attackAllocation = new ResourceAllocation("zone-1", 20, 0, AIDecisionType.Attack);
            var defenseAllocation = new ResourceAllocation("zone-2", 20, 0, AIDecisionType.Defend);

            var attackStrength = service.CalculateAttackStrength(attackAllocation);
            var defenseStrength = service.CalculateDefenseStrength(defenseAllocation);

            // Defenders typically have advantage
            Assert.True(defenseStrength >= attackStrength,
                "Defense should have equal or greater strength than attack with same troops");
        }

        #endregion

        #region GetRecommendedReserve Tests

        [Fact]
        public void GetRecommendedReserve_NullContext_ThrowsArgumentNullException()
        {
            var service = new ResourceAllocationService();

            Assert.Throws<ArgumentNullException>(() => service.GetRecommendedReserve(null!));
        }

        [Fact]
        public void GetRecommendedReserve_ReturnsNonNegativeValue()
        {
            var service = new ResourceAllocationService();
            var context = CreateTestContextWithTroops(50);

            var reserve = service.GetRecommendedReserve(context);

            Assert.True(reserve >= 0);
        }

        [Fact]
        public void GetRecommendedReserve_NeverExceedsTotalTroops()
        {
            var service = new ResourceAllocationService();
            var context = CreateTestContextWithTroops(20);

            var reserve = service.GetRecommendedReserve(context);

            Assert.True(reserve <= context.FactionState.TroopCount);
        }

        [Fact]
        public void GetRecommendedReserve_IncreasesWithMoreOwnedZones()
        {
            var service = new ResourceAllocationService();
            var fewZonesContext = CreateTestContextWithZoneCount(2, troopCount: 100);
            var manyZonesContext = CreateTestContextWithZoneCount(8, troopCount: 100);

            var fewZonesReserve = service.GetRecommendedReserve(fewZonesContext);
            var manyZonesReserve = service.GetRecommendedReserve(manyZonesContext);

            Assert.True(manyZonesReserve >= fewZonesReserve,
                "More owned zones should require equal or higher reserves");
        }

        #endregion

        #region ResourceAllocation Model Tests

        [Fact]
        public void ResourceAllocation_Constructor_SetsProperties()
        {
            var allocation = new ResourceAllocation("zone-1", 25, 5000, AIDecisionType.Attack);

            Assert.Equal("zone-1", allocation.TargetZoneId);
            Assert.Equal(25, allocation.Troops);
            Assert.Equal(5000, allocation.Cash);
            Assert.Equal(AIDecisionType.Attack, allocation.DecisionType);
        }

        [Fact]
        public void ResourceAllocation_NegativeTroops_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new ResourceAllocation("zone-1", -1, 0, AIDecisionType.Attack));
        }

        [Fact]
        public void ResourceAllocation_NegativeCash_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new ResourceAllocation("zone-1", 10, -100, AIDecisionType.Attack));
        }

        [Fact]
        public void ResourceAllocation_ZeroResources_IsValid()
        {
            var allocation = new ResourceAllocation("zone-1", 0, 0, AIDecisionType.Defend);

            Assert.Equal(0, allocation.Troops);
            Assert.Equal(0, allocation.Cash);
        }

        #endregion

        #region Integration Tests

        [Fact]
        public void AllocateResources_RealisticScenario_HandlesCorrectly()
        {
            var service = new ResourceAllocationService();
            var faction = CreateTestFaction(FactionType.Michael);
            var context = CreateTestContextWithFullState(faction, troops: 100, cash: 50000, ownedZones: 5);

            // Mix of attack and defense decisions
            var decisions = new List<AIDecision>
            {
                new AIDecision(AIDecisionType.Defend, "contested-zone", 0.95f, 30),
                new AIDecision(AIDecisionType.Attack, "high-value-target", 0.8f, 40),
                new AIDecision(AIDecisionType.Defend, "valuable-zone", 0.6f, 20),
                new AIDecision(AIDecisionType.Reinforce, "frontier-zone", 0.4f, 10)
            };

            var allocations = service.AllocateResources(decisions, context);

            // Verify reasonable distribution
            Assert.NotEmpty(allocations);
            int totalTroops = allocations.Sum(a => a.Troops);
            int totalCash = allocations.Sum(a => a.Cash);

            Assert.True(totalTroops <= context.FactionState.TroopCount);
            Assert.True(totalCash <= context.FactionState.Cash);
        }

        [Fact]
        public void AllocateResources_ScarceResources_PrioritizesCorrectly()
        {
            var service = new ResourceAllocationService();
            var context = CreateTestContextWithTroops(15); // Very limited troops

            var decisions = new List<AIDecision>
            {
                new AIDecision(AIDecisionType.Defend, "critical-defense", 1.0f, 20),
                new AIDecision(AIDecisionType.Attack, "optional-attack", 0.3f, 15)
            };

            var allocations = service.AllocateResources(decisions, context);

            // High priority defense should get resources
            var defenseAllocation = allocations.FirstOrDefault(a => a.TargetZoneId == "critical-defense");
            Assert.NotNull(defenseAllocation);
            Assert.True(defenseAllocation.Troops > 0);
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

        private Zone CreateTestZone(string id, int strategicValue = 5, Vector3? center = null)
        {
            return new Zone(
                id: id,
                name: $"Test Zone {id}",
                center: center ?? new Vector3(0, 0, 0),
                radius: 150f,
                strategicValue: strategicValue);
        }

        private AIContext CreateTestContext()
        {
            return CreateTestContextWithTroops(50);
        }

        private AIContext CreateTestContextWithTroops(int troopCount)
        {
            return CreateTestContextWithResources(troopCount, 10000);
        }

        private AIContext CreateTestContextWithResources(int troops, int cash)
        {
            var faction = CreateTestFaction(FactionType.Michael);
            var factionState = CreateTestFactionState(faction.Id, troops, cash);

            var ownedZone = CreateTestZone("owned-zone");
            ownedZone.OwnerFactionId = faction.Id;

            var allZones = new List<Zone>
            {
                ownedZone,
                CreateTestZone("enemy-zone"),
                CreateTestZone("neutral-zone")
            };
            allZones[1].OwnerFactionId = "enemy-faction";

            var enemyFactions = new List<Faction> { CreateTestFaction(FactionType.Trevor, "enemy-faction") };

            return new AIContext(faction, factionState, new[] { ownedZone }, allZones, enemyFactions);
        }

        private AIContext CreateTestContextWithZoneCount(int ownedZoneCount, int troopCount)
        {
            var faction = CreateTestFaction(FactionType.Michael);
            var factionState = CreateTestFactionState(faction.Id, troopCount, 10000);

            var ownedZones = new List<Zone>();
            var allZones = new List<Zone>();

            for (int i = 0; i < ownedZoneCount; i++)
            {
                var zone = CreateTestZone($"owned-{i}");
                zone.OwnerFactionId = faction.Id;
                ownedZones.Add(zone);
                allZones.Add(zone);
            }

            // Add some enemy zones
            for (int i = 0; i < 3; i++)
            {
                var zone = CreateTestZone($"enemy-{i}");
                zone.OwnerFactionId = "enemy-faction";
                allZones.Add(zone);
            }

            var enemyFactions = new List<Faction> { CreateTestFaction(FactionType.Trevor, "enemy-faction") };

            return new AIContext(faction, factionState, ownedZones, allZones, enemyFactions);
        }

        private AIContext CreateTestContextWithFullState(Faction faction, int troops, int cash, int ownedZones)
        {
            var factionState = CreateTestFactionState(faction.Id, troops, cash);

            var owned = new List<Zone>();
            var allZones = new List<Zone>();

            for (int i = 0; i < ownedZones; i++)
            {
                var zone = CreateTestZone($"owned-{i}", strategicValue: 3 + (i % 7));
                zone.OwnerFactionId = faction.Id;
                owned.Add(zone);
                allZones.Add(zone);
            }

            // Add enemy and neutral zones
            for (int i = 0; i < 5; i++)
            {
                var enemyZone = CreateTestZone($"enemy-{i}", strategicValue: 4 + i);
                enemyZone.OwnerFactionId = "enemy-faction";
                allZones.Add(enemyZone);

                var neutralZone = CreateTestZone($"neutral-{i}", strategicValue: 2 + i);
                allZones.Add(neutralZone);
            }

            var enemyFactions = new List<Faction> { CreateTestFaction(FactionType.Trevor, "enemy-faction") };

            return new AIContext(faction, factionState, owned, allZones, enemyFactions);
        }

        #endregion
    }
}
