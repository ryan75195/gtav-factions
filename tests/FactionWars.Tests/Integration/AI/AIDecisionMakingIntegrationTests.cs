using System;
using System.Collections.Generic;
using System.Linq;
using FactionWars.AI.Interfaces;
using FactionWars.AI.Models;
using FactionWars.AI.Strategies;
using FactionWars.Core.Interfaces;
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
    /// Integration tests for AI decision making flow.
    /// Tests the complete flow from AIManager coordinating AI strategies through
    /// to making attack, defend, and hold decisions based on game state.
    ///
    /// Uses real implementations of AI strategies and services with only
    /// IGameBridge mocked (representing actual GTA V interactions).
    /// </summary>
    public class AIDecisionMakingIntegrationTests
    {
        private readonly Mock<IGameBridge> _mockGameBridge;
        private readonly InMemoryZoneRepository _zoneRepository;
        private readonly IZoneService _zoneService;
        private readonly InMemoryFactionRepository _factionRepository;
        private readonly IFactionService _factionService;
        private readonly IAIStrategy _michaelStrategy;
        private readonly IAIStrategy _trevorStrategy;
        private readonly IAIStrategy _franklinStrategy;
        private readonly Dictionary<string, IAIStrategy> _strategies;
        private readonly AIManager _aiManager;

        private const string MichaelFactionId = "faction-michael";
        private const string TrevorFactionId = "faction-trevor";
        private const string FranklinFactionId = "faction-franklin";

        public AIDecisionMakingIntegrationTests()
        {
            _mockGameBridge = new Mock<IGameBridge>();

            // Real implementations
            _zoneRepository = new InMemoryZoneRepository();
            _zoneService = new ZoneService(_zoneRepository);
            _factionRepository = new InMemoryFactionRepository();
            _factionService = new FactionService(_factionRepository);

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

        #region Full AI Decision Cycle Tests

        [Fact]
        public void FullCycle_AIManagerMakesDecisionsForNonPlayerFactions()
        {
            // Arrange: Setup three factions, Michael is player
            SetupAllThreeFactions();
            SetupZonesWithOwnership();

            _aiManager.SetPlayerFactionId(MichaelFactionId);
            _aiManager.Start();

            var decisionsReceived = new List<(string FactionId, AIDecision Decision)>();
            _aiManager.OnAIDecision += (sender, args) =>
            {
                decisionsReceived.Add((args.FactionId, args.Decision));
            };

            // Act: Trigger AI decision cycle
            _aiManager.Update(30.0f); // 30 seconds triggers decision (default interval)

            // Assert: Trevor and Franklin made decisions, Michael did not
            Assert.DoesNotContain(decisionsReceived, d => d.FactionId == MichaelFactionId);

            // AI factions with troops should make some decisions
            var trevorDecisions = decisionsReceived.Where(d => d.FactionId == TrevorFactionId).ToList();
            var franklinDecisions = decisionsReceived.Where(d => d.FactionId == FranklinFactionId).ToList();

            // Trevor has troops and aggressive strategy - should make attack decisions
            Assert.NotEmpty(trevorDecisions);
        }

        [Fact]
        public void FullCycle_CharacterSwitchChangesWhichFactionsAreAIControlled()
        {
            // Arrange: Setup factions with zones
            SetupAllThreeFactions();
            SetupZonesWithOwnership();

            _aiManager.Start();

            // Act & Assert: Michael is player - Trevor and Franklin are AI
            _aiManager.SetPlayerFactionId(MichaelFactionId);

            var decisionsAsMichael = new List<string>();
            _aiManager.OnAIDecision += (sender, args) => decisionsAsMichael.Add(args.FactionId);
            _aiManager.ForceDecision();

            Assert.DoesNotContain(MichaelFactionId, decisionsAsMichael);

            // Switch to Trevor as player
            decisionsAsMichael.Clear();
            _aiManager.OnAIDecision += (sender, args) => decisionsAsMichael.Add(args.FactionId);
            _aiManager.SetPlayerFactionId(TrevorFactionId);
            _aiManager.ForceDecision();

            Assert.DoesNotContain(TrevorFactionId, decisionsAsMichael);
            Assert.Contains(MichaelFactionId, decisionsAsMichael);
        }

        [Fact]
        public void FullCycle_AIDecisionsCapturedAndStoredByManager()
        {
            // Arrange
            SetupAllThreeFactions();
            SetupZonesWithOwnership();

            _aiManager.SetPlayerFactionId(MichaelFactionId);
            _aiManager.Start();

            // Act
            _aiManager.Update(30.0f);

            // Assert: Last decisions are stored
            var trevorLastDecisions = _aiManager.GetLastDecisions(TrevorFactionId);
            var franklinLastDecisions = _aiManager.GetLastDecisions(FranklinFactionId);

            // Aggressive Trevor with troops should have attack decisions
            Assert.NotEmpty(trevorLastDecisions);

            // Player faction should have no decisions
            var michaelLastDecisions = _aiManager.GetLastDecisions(MichaelFactionId);
            Assert.Empty(michaelLastDecisions);
        }

        #endregion

        #region Strategy Behavior Tests

        [Fact]
        public void TrevorStrategy_IsAggressive_MakesAttackDecisions()
        {
            // Arrange: Trevor has troops and there are enemy zones
            SetupFaction(TrevorFactionId, "Trevor's Gang", initialTroops: 50);
            SetupFaction(MichaelFactionId, "Michael's Crew", initialTroops: 50);

            // Trevor owns zone 1, Michael owns zone 2 (high value enemy target)
            var trevorZone = CreateAndAddZone("zone-1", "Sandy Shores", TrevorFactionId, strategicValue: 5);
            var michaelZone = CreateAndAddZone("zone-2", "Vinewood", MichaelFactionId, strategicValue: 8);

            _aiManager.SetPlayerFactionId(MichaelFactionId);
            _aiManager.Start();

            // Act
            _aiManager.ForceDecision();

            // Assert: Trevor should decide to attack
            var trevorDecisions = _aiManager.GetLastDecisions(TrevorFactionId);
            Assert.NotEmpty(trevorDecisions);

            var attackDecision = trevorDecisions.FirstOrDefault(d => d.DecisionType == AIDecisionType.Attack);
            Assert.NotNull(attackDecision);
            Assert.Equal("zone-2", attackDecision.TargetZoneId);
        }

        [Fact]
        public void MichaelStrategy_IsDefensive_PrioritizesDefense()
        {
            // Arrange: Michael owns a contested zone
            SetupFaction(MichaelFactionId, "Michael's Crew", initialTroops: 50);
            SetupFaction(TrevorFactionId, "Trevor's Gang", initialTroops: 50);

            var contestedZone = CreateAndAddZone("zone-1", "Vinewood", MichaelFactionId, strategicValue: 8);
            contestedZone.IsContested = true;
            _zoneRepository.Update(contestedZone);

            var enemyZone = CreateAndAddZone("zone-2", "Sandy Shores", TrevorFactionId, strategicValue: 9);

            _aiManager.SetPlayerFactionId(TrevorFactionId);
            _aiManager.Start();

            // Act
            _aiManager.ForceDecision();

            // Assert: Michael should prioritize defense
            var michaelDecisions = _aiManager.GetLastDecisions(MichaelFactionId);
            Assert.NotEmpty(michaelDecisions);

            // First decision should be defend
            Assert.Equal(AIDecisionType.Defend, michaelDecisions[0].DecisionType);
            Assert.Equal("zone-1", michaelDecisions[0].TargetZoneId);
        }

        [Fact]
        public void MichaelStrategy_OnlyAttacksHighValueTargets()
        {
            // Arrange: Michael has troops, enemy has low-value and high-value zones
            SetupFaction(MichaelFactionId, "Michael's Crew", initialTroops: 100);
            SetupFaction(TrevorFactionId, "Trevor's Gang", initialTroops: 50);

            CreateAndAddZone("michael-zone", "Del Perro", MichaelFactionId, strategicValue: 5);
            var lowValueEnemy = CreateAndAddZone("low-value", "Desert", TrevorFactionId, strategicValue: 2);
            var highValueEnemy = CreateAndAddZone("high-value", "Vinewood", TrevorFactionId, strategicValue: 9);

            _aiManager.SetPlayerFactionId(TrevorFactionId);
            _aiManager.Start();

            // Act
            _aiManager.ForceDecision();

            // Assert: Michael should attack high-value, not low-value
            var michaelDecisions = _aiManager.GetLastDecisions(MichaelFactionId);
            var attackDecisions = michaelDecisions.Where(d => d.DecisionType == AIDecisionType.Attack).ToList();

            if (attackDecisions.Any())
            {
                // If Michael attacks, it should be the high-value target
                Assert.Equal("high-value", attackDecisions[0].TargetZoneId);
            }
        }

        [Fact]
        public void TrevorStrategy_WillAttackLowValueTargets()
        {
            // Arrange: Trevor has troops, enemy has low-value zone
            SetupFaction(TrevorFactionId, "Trevor's Gang", initialTroops: 50);
            SetupFaction(MichaelFactionId, "Michael's Crew", initialTroops: 50);

            CreateAndAddZone("trevor-zone", "Sandy Shores", TrevorFactionId, strategicValue: 3);
            var lowValueEnemy = CreateAndAddZone("low-value", "Desert", MichaelFactionId, strategicValue: 3);

            _aiManager.SetPlayerFactionId(MichaelFactionId);
            _aiManager.Start();

            // Act
            _aiManager.ForceDecision();

            // Assert: Trevor should still attack (aggressive, low threshold)
            var trevorDecisions = _aiManager.GetLastDecisions(TrevorFactionId);
            var attackDecision = trevorDecisions.FirstOrDefault(d => d.DecisionType == AIDecisionType.Attack);

            Assert.NotNull(attackDecision);
            Assert.Equal("low-value", attackDecision.TargetZoneId);
        }

        #endregion

        #region Troop Requirements Tests

        [Fact]
        public void AIWithNoTroops_MakesNoDecisions()
        {
            // Arrange: Faction with no troops
            SetupFaction(TrevorFactionId, "Trevor's Gang", initialTroops: 0);
            SetupFaction(MichaelFactionId, "Michael's Crew", initialTroops: 50);

            CreateAndAddZone("zone-1", "Downtown", MichaelFactionId, strategicValue: 10);

            _aiManager.SetPlayerFactionId(MichaelFactionId);
            _aiManager.Start();

            // Act
            _aiManager.ForceDecision();

            // Assert: Trevor has no troops, should have no decisions
            var trevorDecisions = _aiManager.GetLastDecisions(TrevorFactionId);
            Assert.Empty(trevorDecisions);
        }

        [Fact]
        public void AIWithMinimalTroops_CanStillDefend()
        {
            // Arrange: Faction with just enough troops to defend (3 minimum)
            SetupFaction(TrevorFactionId, "Trevor's Gang", initialTroops: 5);
            SetupFaction(MichaelFactionId, "Michael's Crew", initialTroops: 50);

            var contestedZone = CreateAndAddZone("zone-1", "Sandy Shores", TrevorFactionId, strategicValue: 8);
            contestedZone.IsContested = true;
            _zoneRepository.Update(contestedZone);

            _aiManager.SetPlayerFactionId(MichaelFactionId);
            _aiManager.Start();

            // Act
            _aiManager.ForceDecision();

            // Assert: Trevor should still defend contested zone even with few troops
            var trevorDecisions = _aiManager.GetLastDecisions(TrevorFactionId);
            var defendDecision = trevorDecisions.FirstOrDefault(d => d.DecisionType == AIDecisionType.Defend);

            Assert.NotNull(defendDecision);
            Assert.True(defendDecision.TroopsToCommit >= 3, "Should commit at least minimum defense troops");
        }

        [Fact]
        public void AITroopAllocation_DoesNotExceedAvailable()
        {
            // Arrange
            int availableTroops = 20;
            SetupFaction(TrevorFactionId, "Trevor's Gang", initialTroops: availableTroops);
            SetupFaction(MichaelFactionId, "Michael's Crew", initialTroops: 50);

            var contestedZone = CreateAndAddZone("zone-1", "Sandy Shores", TrevorFactionId, strategicValue: 10);
            contestedZone.IsContested = true;
            _zoneRepository.Update(contestedZone);

            var enemyZone = CreateAndAddZone("zone-2", "Vinewood", MichaelFactionId, strategicValue: 10);

            _aiManager.SetPlayerFactionId(MichaelFactionId);
            _aiManager.Start();

            // Act
            _aiManager.ForceDecision();

            // Assert: All decisions combined should not exceed available troops
            var trevorDecisions = _aiManager.GetLastDecisions(TrevorFactionId);
            foreach (var decision in trevorDecisions)
            {
                Assert.True(decision.TroopsToCommit <= availableTroops,
                    $"Decision troops ({decision.TroopsToCommit}) should not exceed available ({availableTroops})");
            }
        }

        #endregion

        #region Decision Priority Tests

        [Fact]
        public void Decisions_AreOrderedByPriority()
        {
            // Arrange: Setup scenario where multiple decisions are possible
            SetupFaction(MichaelFactionId, "Michael's Crew", initialTroops: 100);
            SetupFaction(TrevorFactionId, "Trevor's Gang", initialTroops: 50);

            // Michael has contested zone (defense priority) and enemy zone available (attack)
            var contestedZone = CreateAndAddZone("contested", "Downtown", MichaelFactionId, strategicValue: 8);
            contestedZone.IsContested = true;
            _zoneRepository.Update(contestedZone);

            var enemyZone = CreateAndAddZone("enemy", "Sandy Shores", TrevorFactionId, strategicValue: 5);

            _aiManager.SetPlayerFactionId(TrevorFactionId);
            _aiManager.Start();

            // Act
            _aiManager.ForceDecision();

            // Assert: Decisions should be ordered by priority (highest first)
            var michaelDecisions = _aiManager.GetLastDecisions(MichaelFactionId);

            if (michaelDecisions.Count >= 2)
            {
                for (int i = 0; i < michaelDecisions.Count - 1; i++)
                {
                    Assert.True(michaelDecisions[i].Priority >= michaelDecisions[i + 1].Priority,
                        "Decisions should be ordered by priority (descending)");
                }
            }
        }

        [Fact]
        public void DefendDecision_HasHighPriorityWhenContested()
        {
            // Arrange
            SetupFaction(MichaelFactionId, "Michael's Crew", initialTroops: 100);
            SetupFaction(TrevorFactionId, "Trevor's Gang", initialTroops: 50);

            var contestedZone = CreateAndAddZone("contested", "Vinewood", MichaelFactionId, strategicValue: 8);
            contestedZone.IsContested = true;
            _zoneRepository.Update(contestedZone);

            _aiManager.SetPlayerFactionId(TrevorFactionId);
            _aiManager.Start();

            // Act
            _aiManager.ForceDecision();

            // Assert: Defense of contested zone should have high priority
            var michaelDecisions = _aiManager.GetLastDecisions(MichaelFactionId);
            var defendDecision = michaelDecisions.FirstOrDefault(d => d.DecisionType == AIDecisionType.Defend);

            Assert.NotNull(defendDecision);
            Assert.True(defendDecision.Priority >= 0.5f,
                $"Defend priority ({defendDecision.Priority}) should be high for contested zone");
        }

        #endregion

        #region Decision Event Tests

        [Fact]
        public void OnAIDecision_FiredForEachDecision()
        {
            // Arrange
            SetupAllThreeFactions();
            SetupZonesWithOwnership();

            _aiManager.SetPlayerFactionId(MichaelFactionId);
            _aiManager.Start();

            var decisionCount = 0;
            _aiManager.OnAIDecision += (sender, args) => decisionCount++;

            // Act
            _aiManager.ForceDecision();

            // Assert: Events should have been raised for AI decisions
            var totalDecisions = _aiManager.GetLastDecisions(TrevorFactionId).Count +
                                _aiManager.GetLastDecisions(FranklinFactionId).Count;

            Assert.Equal(totalDecisions, decisionCount);
        }

        [Fact]
        public void OnAIDecision_ContainsCorrectFactionId()
        {
            // Arrange
            SetupFaction(TrevorFactionId, "Trevor's Gang", initialTroops: 50);
            SetupFaction(MichaelFactionId, "Michael's Crew", initialTroops: 50);

            CreateAndAddZone("zone-1", "Sandy Shores", TrevorFactionId, strategicValue: 5);
            CreateAndAddZone("zone-2", "Vinewood", MichaelFactionId, strategicValue: 8);

            _aiManager.SetPlayerFactionId(MichaelFactionId);
            _aiManager.Start();

            string? receivedFactionId = null;
            _aiManager.OnAIDecision += (sender, args) => receivedFactionId = args.FactionId;

            // Act
            _aiManager.ForceDecision();

            // Assert: Should receive Trevor's faction ID (only AI faction)
            Assert.Equal(TrevorFactionId, receivedFactionId);
        }

        #endregion

        #region Context Building Tests

        [Fact]
        public void AIContext_ContainsCorrectOwnedZones()
        {
            // Arrange: Trevor owns 2 zones, Michael owns 1
            SetupFaction(TrevorFactionId, "Trevor's Gang", initialTroops: 50);
            SetupFaction(MichaelFactionId, "Michael's Crew", initialTroops: 50);

            CreateAndAddZone("zone-t1", "Sandy Shores", TrevorFactionId, strategicValue: 5);
            CreateAndAddZone("zone-t2", "Alamo Sea", TrevorFactionId, strategicValue: 4);
            CreateAndAddZone("zone-m1", "Vinewood", MichaelFactionId, strategicValue: 8);

            // Create a custom strategy that captures the context
            AIContext? capturedContext = null;
            var capturingStrategy = new ContextCapturingStrategy(ctx => capturedContext = ctx);

            var customStrategies = new Dictionary<string, IAIStrategy>
            {
                { TrevorFactionId, capturingStrategy },
                { MichaelFactionId, _michaelStrategy }
            };

            var customAiManager = new AIManager(_factionService, _zoneService, customStrategies);
            customAiManager.SetPlayerFactionId(MichaelFactionId);
            customAiManager.Start();

            // Act
            customAiManager.ForceDecision();

            // Assert
            Assert.NotNull(capturedContext);
            Assert.Equal(2, capturedContext!.OwnedZones.Count);
            Assert.Contains(capturedContext.OwnedZones, z => z.Id == "zone-t1");
            Assert.Contains(capturedContext.OwnedZones, z => z.Id == "zone-t2");
        }

        [Fact]
        public void AIContext_ContainsAllZones()
        {
            // Arrange
            SetupFaction(TrevorFactionId, "Trevor's Gang", initialTroops: 50);
            SetupFaction(MichaelFactionId, "Michael's Crew", initialTroops: 50);

            CreateAndAddZone("zone-1", "Sandy Shores", TrevorFactionId, strategicValue: 5);
            CreateAndAddZone("zone-2", "Vinewood", MichaelFactionId, strategicValue: 8);
            CreateAndAddZone("zone-3", "Neutral Zone", null, strategicValue: 3);

            AIContext? capturedContext = null;
            var capturingStrategy = new ContextCapturingStrategy(ctx => capturedContext = ctx);

            var customStrategies = new Dictionary<string, IAIStrategy>
            {
                { TrevorFactionId, capturingStrategy },
                { MichaelFactionId, _michaelStrategy }
            };

            var customAiManager = new AIManager(_factionService, _zoneService, customStrategies);
            customAiManager.SetPlayerFactionId(MichaelFactionId);
            customAiManager.Start();

            // Act
            customAiManager.ForceDecision();

            // Assert
            Assert.NotNull(capturedContext);
            Assert.Equal(3, capturedContext!.AllZones.Count);
        }

        [Fact]
        public void AIContext_ContainsEnemyFactions()
        {
            // Arrange
            SetupAllThreeFactions();
            SetupZonesWithOwnership();

            AIContext? capturedContext = null;
            var capturingStrategy = new ContextCapturingStrategy(ctx => capturedContext = ctx);

            var customStrategies = new Dictionary<string, IAIStrategy>
            {
                { TrevorFactionId, capturingStrategy },
                { MichaelFactionId, _michaelStrategy },
                { FranklinFactionId, _franklinStrategy }
            };

            var customAiManager = new AIManager(_factionService, _zoneService, customStrategies);
            customAiManager.SetPlayerFactionId(MichaelFactionId);
            customAiManager.Start();

            // Act
            customAiManager.ForceDecision();

            // Assert: Trevor's context should have Michael and Franklin as enemies
            Assert.NotNull(capturedContext);
            Assert.Equal(2, capturedContext!.EnemyFactions.Count);
            Assert.Contains(capturedContext.EnemyFactions, f => f.Id == MichaelFactionId);
            Assert.Contains(capturedContext.EnemyFactions, f => f.Id == FranklinFactionId);
        }

        #endregion

        #region Helper Methods

        private void SetupAllThreeFactions()
        {
            SetupFaction(MichaelFactionId, "Michael's Crew", initialTroops: 50);
            SetupFaction(TrevorFactionId, "Trevor's Gang", initialTroops: 60);
            SetupFaction(FranklinFactionId, "Franklin's Family", initialTroops: 30);
        }

        private void SetupZonesWithOwnership()
        {
            // Michael's zones (high value - defensive)
            CreateAndAddZone("zone-m1", "Vinewood", MichaelFactionId, strategicValue: 8);
            CreateAndAddZone("zone-m2", "Del Perro", MichaelFactionId, strategicValue: 7);

            // Trevor's zones (medium value - aggressive)
            CreateAndAddZone("zone-t1", "Sandy Shores", TrevorFactionId, strategicValue: 5);
            CreateAndAddZone("zone-t2", "Alamo Sea", TrevorFactionId, strategicValue: 4);
            CreateAndAddZone("zone-t3", "Grapeseed", TrevorFactionId, strategicValue: 3);

            // Franklin's zones (lower value - balanced)
            CreateAndAddZone("zone-f1", "Grove Street", FranklinFactionId, strategicValue: 5);
            CreateAndAddZone("zone-f2", "Davis", FranklinFactionId, strategicValue: 4);

            // Neutral zone
            CreateAndAddZone("zone-n1", "Mount Chiliad", null, strategicValue: 6);
        }

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

    /// <summary>
    /// Test helper strategy that captures the AIContext passed to it.
    /// </summary>
    public class ContextCapturingStrategy : IAIStrategy
    {
        private readonly Action<AIContext> _onContext;

        public ContextCapturingStrategy(Action<AIContext> onContext)
        {
            _onContext = onContext;
        }

        public FactionType FactionType => FactionType.Trevor;

        public float EvaluateZone(Zone zone, AIContext context) => 0.5f;

        public IList<AIDecision> MakeDecisions(AIContext context)
        {
            _onContext(context);
            return new List<AIDecision>();
        }

        public bool ShouldAttack(Zone zone, AIContext context) => false;

        public bool ShouldDefend(Zone zone, AIContext context) => false;

        public int GetTroopsForAction(AIDecision decision, AIContext context) => 0;

        public float GetAggressiveness() => 0.5f;

        public float GetRiskTolerance() => 0.5f;
    }
}
