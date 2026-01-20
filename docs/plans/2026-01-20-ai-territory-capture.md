# AI Territory Capture Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Enable AI factions to autonomously capture territory within budget constraints, using probability-weighted target selection favoring unguarded zones.

**Architecture:** Modify AIManager to use 2-3 minute decision cycles. Add budget enforcement and auto-recruitment to AI decision flow. Implement probability-weighted target selection in BaseAIStrategy. Wire BackgroundBattleSimulator to properly execute AI attacks.

**Tech Stack:** C#, .NET Framework 4.8, ScriptHookVDotNet

---

## Task 1: Increase AI Decision Interval to 2-3 Minutes

**Files:**
- Modify: `src/FactionWars/ScriptHookV/Managers/AIManager.cs:56`
- Modify: `src/FactionWars/ScriptHookV/GameLoopController.cs:493-495`

**Step 1: Update default decision interval in AIManager**

Change line 56 in `AIManager.cs`:
```csharp
// Before:
private const float DefaultDecisionInterval = 5.0f;

// After:
private const float DefaultDecisionInterval = 150.0f; // 2.5 minutes
```

**Step 2: Verify GameLoopController doesn't override the interval**

Check `GameLoopController.cs` around line 493-495 - it should just call `_aiManager.Start()` without setting a custom interval. No change needed if it doesn't override.

**Step 3: Build and verify**

Run: `dotnet build src/FactionWars/FactionWars.csproj -c Release`
Expected: Build succeeds

**Step 4: Commit**

```bash
git add src/FactionWars/ScriptHookV/Managers/AIManager.cs
git commit -m "feat(ai): increase decision interval to 2.5 minutes for strategic pacing"
```

---

## Task 2: Add Attack Cost Calculation

**Files:**
- Create: `src/FactionWars/AI/Services/AIBudgetService.cs`
- Create: `src/FactionWars/AI/Interfaces/IAIBudgetService.cs`
- Test: `tests/FactionWars.Tests/Unit/AI/AIBudgetServiceTests.cs`

**Step 1: Write the failing test**

Create `tests/FactionWars.Tests/Unit/AI/AIBudgetServiceTests.cs`:
```csharp
using FactionWars.AI.Interfaces;
using FactionWars.AI.Services;
using Xunit;

namespace FactionWars.Tests.Unit.AI
{
    public class AIBudgetServiceTests
    {
        [Fact]
        public void CalculateAttackCost_ReturnsCorrectCost()
        {
            // Arrange
            var service = new AIBudgetService(costPerTroop: 50);

            // Act
            var cost = service.CalculateAttackCost(troopsToCommit: 10);

            // Assert
            Assert.Equal(500, cost);
        }

        [Fact]
        public void CanAffordAttack_ReturnsTrueWhenSufficientFunds()
        {
            var service = new AIBudgetService(costPerTroop: 50);

            var canAfford = service.CanAffordAttack(factionCash: 1000, troopsToCommit: 10);

            Assert.True(canAfford);
        }

        [Fact]
        public void CanAffordAttack_ReturnsFalseWhenInsufficientFunds()
        {
            var service = new AIBudgetService(costPerTroop: 50);

            var canAfford = service.CanAffordAttack(factionCash: 400, troopsToCommit: 10);

            Assert.False(canAfford);
        }

        [Fact]
        public void CalculateRecruitmentCost_ReturnsCorrectCost()
        {
            var service = new AIBudgetService(costPerTroop: 50, recruitCostPerTroop: 100);

            var cost = service.CalculateRecruitmentCost(troopsToRecruit: 5);

            Assert.Equal(500, cost);
        }
    }
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~AIBudgetServiceTests"`
Expected: FAIL - types not found

**Step 3: Create the interface**

Create `src/FactionWars/AI/Interfaces/IAIBudgetService.cs`:
```csharp
namespace FactionWars.AI.Interfaces
{
    /// <summary>
    /// Service for calculating AI faction budget constraints.
    /// </summary>
    public interface IAIBudgetService
    {
        /// <summary>
        /// Calculates the cost of an attack based on troops committed.
        /// </summary>
        int CalculateAttackCost(int troopsToCommit);

        /// <summary>
        /// Checks if a faction can afford an attack.
        /// </summary>
        bool CanAffordAttack(int factionCash, int troopsToCommit);

        /// <summary>
        /// Calculates the cost to recruit troops.
        /// </summary>
        int CalculateRecruitmentCost(int troopsToRecruit);

        /// <summary>
        /// Gets the cost per troop for attacks.
        /// </summary>
        int CostPerTroop { get; }

        /// <summary>
        /// Gets the cost per troop for recruitment.
        /// </summary>
        int RecruitCostPerTroop { get; }
    }
}
```

**Step 4: Create the implementation**

Create `src/FactionWars/AI/Services/AIBudgetService.cs`:
```csharp
using System;
using FactionWars.AI.Interfaces;

namespace FactionWars.AI.Services
{
    /// <summary>
    /// Default implementation of AI budget service.
    /// </summary>
    public class AIBudgetService : IAIBudgetService
    {
        private readonly int _costPerTroop;
        private readonly int _recruitCostPerTroop;

        /// <summary>
        /// Creates a new AIBudgetService with specified costs.
        /// </summary>
        /// <param name="costPerTroop">Cost per troop for attacks (default: 50).</param>
        /// <param name="recruitCostPerTroop">Cost per troop for recruitment (default: 100).</param>
        public AIBudgetService(int costPerTroop = 50, int recruitCostPerTroop = 100)
        {
            _costPerTroop = Math.Max(1, costPerTroop);
            _recruitCostPerTroop = Math.Max(1, recruitCostPerTroop);
        }

        /// <inheritdoc />
        public int CostPerTroop => _costPerTroop;

        /// <inheritdoc />
        public int RecruitCostPerTroop => _recruitCostPerTroop;

        /// <inheritdoc />
        public int CalculateAttackCost(int troopsToCommit)
        {
            return Math.Max(0, troopsToCommit) * _costPerTroop;
        }

        /// <inheritdoc />
        public bool CanAffordAttack(int factionCash, int troopsToCommit)
        {
            var cost = CalculateAttackCost(troopsToCommit);
            return factionCash >= cost;
        }

        /// <inheritdoc />
        public int CalculateRecruitmentCost(int troopsToRecruit)
        {
            return Math.Max(0, troopsToRecruit) * _recruitCostPerTroop;
        }
    }
}
```

**Step 5: Run tests to verify they pass**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~AIBudgetServiceTests"`
Expected: All 4 tests PASS

**Step 6: Commit**

```bash
git add src/FactionWars/AI/Interfaces/IAIBudgetService.cs src/FactionWars/AI/Services/AIBudgetService.cs tests/FactionWars.Tests/Unit/AI/AIBudgetServiceTests.cs
git commit -m "feat(ai): add AIBudgetService for attack and recruitment cost calculation"
```

---

## Task 3: Add Probability-Weighted Target Selection

**Files:**
- Modify: `src/FactionWars/AI/Strategies/BaseAIStrategy.cs`
- Modify: `src/FactionWars/AI/Models/AIContext.cs` (add defender count method)
- Test: `tests/FactionWars.Tests/Unit/AI/ProbabilityTargetSelectionTests.cs`

**Step 1: Write the failing test**

Create `tests/FactionWars.Tests/Unit/AI/ProbabilityTargetSelectionTests.cs`:
```csharp
using System.Collections.Generic;
using System.Linq;
using FactionWars.AI.Models;
using FactionWars.AI.Strategies;
using FactionWars.Factions.Models;
using FactionWars.Territory.Models;
using Xunit;

namespace FactionWars.Tests.Unit.AI
{
    public class ProbabilityTargetSelectionTests
    {
        [Fact]
        public void CalculateTargetScore_UnguardedZone_HasHigherScore()
        {
            // Arrange - zone with 0 defenders should score higher than zone with 5
            var unguardedZone = new Zone("zone1", "Unguarded", new Vector3(0, 0, 0), 100f)
            {
                StrategicValue = 5,
                OwnerFactionId = "enemy"
            };
            var guardedZone = new Zone("zone2", "Guarded", new Vector3(0, 0, 0), 100f)
            {
                StrategicValue = 5,
                OwnerFactionId = "enemy"
            };

            var defenderCounts = new Dictionary<string, int>
            {
                { "zone1", 0 },  // Unguarded
                { "zone2", 5 }   // Guarded
            };

            // Act
            var unguardedScore = TestableStrategy.CalculateTargetScorePublic(unguardedZone, defenderCounts);
            var guardedScore = TestableStrategy.CalculateTargetScorePublic(guardedZone, defenderCounts);

            // Assert - unguarded should have 3x multiplier
            Assert.True(unguardedScore > guardedScore);
        }

        [Fact]
        public void CalculateTargetScore_NeutralZone_HasBonus()
        {
            var neutralZone = new Zone("zone1", "Neutral", new Vector3(0, 0, 0), 100f)
            {
                StrategicValue = 5,
                OwnerFactionId = null  // Neutral
            };
            var enemyZone = new Zone("zone2", "Enemy", new Vector3(0, 0, 0), 100f)
            {
                StrategicValue = 5,
                OwnerFactionId = "enemy"
            };

            var defenderCounts = new Dictionary<string, int>
            {
                { "zone1", 0 },
                { "zone2", 0 }
            };

            var neutralScore = TestableStrategy.CalculateTargetScorePublic(neutralZone, defenderCounts);
            var enemyScore = TestableStrategy.CalculateTargetScorePublic(enemyZone, defenderCounts);

            // Neutral should have 1.5x bonus
            Assert.True(neutralScore > enemyScore);
        }

        // Test helper to expose protected method
        private class TestableStrategy : BaseAIStrategy
        {
            public TestableStrategy() : base(FactionType.Balanced, 0.5f, 0.5f) { }

            public static float CalculateTargetScorePublic(Zone zone, IDictionary<string, int> defenderCounts)
            {
                return CalculateTargetScore(zone, defenderCounts);
            }
        }
    }
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~ProbabilityTargetSelectionTests"`
Expected: FAIL - CalculateTargetScore method doesn't exist

**Step 3: Add CalculateTargetScore to BaseAIStrategy**

Add to `src/FactionWars/AI/Strategies/BaseAIStrategy.cs` after the `GetTroopsForAttack` method (around line 327):

```csharp
/// <summary>
/// Calculates a weighted score for a target zone based on defense and ownership.
/// Used for probability-weighted target selection.
/// </summary>
/// <param name="zone">The zone to score.</param>
/// <param name="defenderCounts">Dictionary of zone ID to defender count.</param>
/// <returns>Weighted score (higher = more attractive target).</returns>
protected static float CalculateTargetScore(Zone zone, IDictionary<string, int> defenderCounts)
{
    // Base score from strategic value
    float score = zone.StrategicValue;

    // Defense multiplier based on defender count
    int defenders = defenderCounts.TryGetValue(zone.Id, out var count) ? count : 0;
    float defenseMultiplier;
    if (defenders == 0)
        defenseMultiplier = 3.0f;      // Unguarded
    else if (defenders <= 3)
        defenseMultiplier = 2.0f;      // Lightly guarded
    else if (defenders <= 7)
        defenseMultiplier = 1.0f;      // Moderately guarded
    else
        defenseMultiplier = 0.3f;      // Heavily guarded

    // Ownership multiplier
    float ownershipMultiplier = zone.OwnerFactionId == null ? 1.5f : 1.0f;  // Neutral bonus

    return score * defenseMultiplier * ownershipMultiplier;
}

/// <summary>
/// Selects a target zone using probability-weighted random selection.
/// </summary>
/// <param name="zones">List of potential target zones.</param>
/// <param name="defenderCounts">Dictionary of zone ID to defender count.</param>
/// <param name="random">Random number generator (optional, for testing).</param>
/// <returns>Selected zone, or null if no valid targets.</returns>
protected Zone? SelectTargetByProbability(
    IList<Zone> zones,
    IDictionary<string, int> defenderCounts,
    Random? random = null)
{
    if (zones == null || zones.Count == 0)
        return null;

    random ??= new Random();

    // Calculate scores for all zones
    var scores = zones.Select(z => new { Zone = z, Score = CalculateTargetScore(z, defenderCounts) }).ToList();
    var totalScore = scores.Sum(s => s.Score);

    if (totalScore <= 0)
        return zones[0]; // Fallback to first zone

    // Select by probability
    var roll = random.NextDouble() * totalScore;
    float cumulative = 0;

    foreach (var item in scores)
    {
        cumulative += item.Score;
        if (roll <= cumulative)
            return item.Zone;
    }

    return scores.Last().Zone;
}
```

Also add this using statement at the top if not present:
```csharp
using System.Collections.Generic;
```

**Step 4: Run tests to verify they pass**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~ProbabilityTargetSelectionTests"`
Expected: All tests PASS

**Step 5: Commit**

```bash
git add src/FactionWars/AI/Strategies/BaseAIStrategy.cs tests/FactionWars.Tests/Unit/AI/ProbabilityTargetSelectionTests.cs
git commit -m "feat(ai): add probability-weighted target selection favoring unguarded zones"
```

---

## Task 4: Add Auto-Recruitment Service

**Files:**
- Create: `src/FactionWars/AI/Services/AIRecruitmentService.cs`
- Create: `src/FactionWars/AI/Interfaces/IAIRecruitmentService.cs`
- Test: `tests/FactionWars.Tests/Unit/AI/AIRecruitmentServiceTests.cs`

**Step 1: Write the failing test**

Create `tests/FactionWars.Tests/Unit/AI/AIRecruitmentServiceTests.cs`:
```csharp
using FactionWars.AI.Interfaces;
using FactionWars.AI.Services;
using FactionWars.Factions.Interfaces;
using FactionWars.Factions.Models;
using Moq;
using Xunit;

namespace FactionWars.Tests.Unit.AI
{
    public class AIRecruitmentServiceTests
    {
        [Fact]
        public void TryAutoRecruit_WithSufficientFunds_RecruitsTroops()
        {
            // Arrange
            var mockFactionService = new Mock<IFactionService>();
            var factionState = new FactionState("test", initialCash: 1000, initialTroopCount: 5);
            mockFactionService.Setup(f => f.GetFactionState("test")).Returns(factionState);
            mockFactionService.Setup(f => f.RecruitTroops("test", It.IsAny<int>())).Returns(true);
            mockFactionService.Setup(f => f.AddCash("test", It.IsAny<int>())).Returns(true);

            var budgetService = new AIBudgetService(costPerTroop: 50, recruitCostPerTroop: 100);
            var service = new AIRecruitmentService(mockFactionService.Object, budgetService);

            // Act
            var recruited = service.TryAutoRecruit("test", maxTroopsToRecruit: 5);

            // Assert - should recruit as many as affordable (1000 / 100 = 10, but max is 5)
            Assert.Equal(5, recruited);
            mockFactionService.Verify(f => f.RecruitTroops("test", 5), Times.Once);
            mockFactionService.Verify(f => f.AddCash("test", -500), Times.Once); // 5 * 100
        }

        [Fact]
        public void TryAutoRecruit_WithInsufficientFunds_RecruitsAffordableAmount()
        {
            var mockFactionService = new Mock<IFactionService>();
            var factionState = new FactionState("test", initialCash: 250, initialTroopCount: 5);
            mockFactionService.Setup(f => f.GetFactionState("test")).Returns(factionState);
            mockFactionService.Setup(f => f.RecruitTroops("test", It.IsAny<int>())).Returns(true);
            mockFactionService.Setup(f => f.AddCash("test", It.IsAny<int>())).Returns(true);

            var budgetService = new AIBudgetService(costPerTroop: 50, recruitCostPerTroop: 100);
            var service = new AIRecruitmentService(mockFactionService.Object, budgetService);

            var recruited = service.TryAutoRecruit("test", maxTroopsToRecruit: 10);

            // Can only afford 2 troops (250 / 100 = 2)
            Assert.Equal(2, recruited);
            mockFactionService.Verify(f => f.RecruitTroops("test", 2), Times.Once);
        }

        [Fact]
        public void TryAutoRecruit_WithNoFunds_ReturnsZero()
        {
            var mockFactionService = new Mock<IFactionService>();
            var factionState = new FactionState("test", initialCash: 50, initialTroopCount: 5);
            mockFactionService.Setup(f => f.GetFactionState("test")).Returns(factionState);

            var budgetService = new AIBudgetService(costPerTroop: 50, recruitCostPerTroop: 100);
            var service = new AIRecruitmentService(mockFactionService.Object, budgetService);

            var recruited = service.TryAutoRecruit("test", maxTroopsToRecruit: 10);

            Assert.Equal(0, recruited);
            mockFactionService.Verify(f => f.RecruitTroops(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        }
    }
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~AIRecruitmentServiceTests"`
Expected: FAIL - types not found

**Step 3: Create the interface**

Create `src/FactionWars/AI/Interfaces/IAIRecruitmentService.cs`:
```csharp
namespace FactionWars.AI.Interfaces
{
    /// <summary>
    /// Service for automatic AI troop recruitment.
    /// </summary>
    public interface IAIRecruitmentService
    {
        /// <summary>
        /// Attempts to auto-recruit troops for a faction using available cash.
        /// </summary>
        /// <param name="factionId">The faction to recruit for.</param>
        /// <param name="maxTroopsToRecruit">Maximum troops to recruit in one cycle.</param>
        /// <returns>Number of troops actually recruited.</returns>
        int TryAutoRecruit(string factionId, int maxTroopsToRecruit = 10);
    }
}
```

**Step 4: Create the implementation**

Create `src/FactionWars/AI/Services/AIRecruitmentService.cs`:
```csharp
using System;
using FactionWars.AI.Interfaces;
using FactionWars.Factions.Interfaces;

namespace FactionWars.AI.Services
{
    /// <summary>
    /// Service that handles automatic troop recruitment for AI factions.
    /// </summary>
    public class AIRecruitmentService : IAIRecruitmentService
    {
        private readonly IFactionService _factionService;
        private readonly IAIBudgetService _budgetService;

        /// <summary>
        /// Creates a new AIRecruitmentService.
        /// </summary>
        public AIRecruitmentService(IFactionService factionService, IAIBudgetService budgetService)
        {
            _factionService = factionService ?? throw new ArgumentNullException(nameof(factionService));
            _budgetService = budgetService ?? throw new ArgumentNullException(nameof(budgetService));
        }

        /// <inheritdoc />
        public int TryAutoRecruit(string factionId, int maxTroopsToRecruit = 10)
        {
            if (string.IsNullOrEmpty(factionId))
                return 0;

            var state = _factionService.GetFactionState(factionId);
            if (state == null)
                return 0;

            // Calculate how many we can afford
            int affordableTroops = state.Cash / _budgetService.RecruitCostPerTroop;
            int troopsToRecruit = Math.Min(affordableTroops, maxTroopsToRecruit);

            if (troopsToRecruit <= 0)
                return 0;

            // Calculate cost and deduct
            int cost = _budgetService.CalculateRecruitmentCost(troopsToRecruit);

            // Recruit and deduct cash
            _factionService.RecruitTroops(factionId, troopsToRecruit);
            _factionService.AddCash(factionId, -cost);

            return troopsToRecruit;
        }
    }
}
```

**Step 5: Run tests to verify they pass**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~AIRecruitmentServiceTests"`
Expected: All 3 tests PASS

**Step 6: Commit**

```bash
git add src/FactionWars/AI/Interfaces/IAIRecruitmentService.cs src/FactionWars/AI/Services/AIRecruitmentService.cs tests/FactionWars.Tests/Unit/AI/AIRecruitmentServiceTests.cs
git commit -m "feat(ai): add AIRecruitmentService for automatic troop recruitment"
```

---

## Task 5: Create AIDecisionExecutor to Coordinate Everything

**Files:**
- Create: `src/FactionWars/ScriptHookV/Managers/AIDecisionExecutor.cs`
- Test: `tests/FactionWars.Tests/Unit/ScriptHookV/AIDecisionExecutorTests.cs`

**Step 1: Write the failing test**

Create `tests/FactionWars.Tests/Unit/ScriptHookV/AIDecisionExecutorTests.cs`:
```csharp
using FactionWars.AI.Interfaces;
using FactionWars.AI.Models;
using FactionWars.AI.Services;
using FactionWars.Factions.Interfaces;
using FactionWars.Factions.Models;
using FactionWars.ScriptHookV.Managers;
using FactionWars.Territory.Interfaces;
using FactionWars.Territory.Models;
using Moq;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV
{
    public class AIDecisionExecutorTests
    {
        [Fact]
        public void ExecuteAttack_WithSufficientBudget_CallsBattleSimulator()
        {
            // Arrange
            var mockFactionService = new Mock<IFactionService>();
            var mockBattleSimulator = new Mock<BackgroundBattleSimulator>(
                null, null, null, null, null, null); // Will need proper mocks
            var budgetService = new AIBudgetService(costPerTroop: 50);

            var factionState = new FactionState("attacker", initialCash: 1000, initialTroopCount: 20);
            mockFactionService.Setup(f => f.GetFactionState("attacker")).Returns(factionState);
            mockFactionService.Setup(f => f.AddCash("attacker", It.IsAny<int>())).Returns(true);

            var executor = new AIDecisionExecutor(
                mockFactionService.Object,
                budgetService,
                null); // Recruitment service not needed for this test

            var decision = new AIDecision(AIDecisionType.Attack, "zone1", 0.8f, 10);

            // Act
            var result = executor.TryExecuteAttack("attacker", decision);

            // Assert
            Assert.True(result);
            mockFactionService.Verify(f => f.AddCash("attacker", -500), Times.Once); // 10 * 50
        }

        [Fact]
        public void ExecuteAttack_WithInsufficientBudget_ReturnsFalse()
        {
            var mockFactionService = new Mock<IFactionService>();
            var budgetService = new AIBudgetService(costPerTroop: 50);

            var factionState = new FactionState("attacker", initialCash: 100, initialTroopCount: 20);
            mockFactionService.Setup(f => f.GetFactionState("attacker")).Returns(factionState);

            var executor = new AIDecisionExecutor(
                mockFactionService.Object,
                budgetService,
                null);

            var decision = new AIDecision(AIDecisionType.Attack, "zone1", 0.8f, 10); // Cost: 500

            var result = executor.TryExecuteAttack("attacker", decision);

            Assert.False(result);
            mockFactionService.Verify(f => f.AddCash(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        }
    }
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~AIDecisionExecutorTests"`
Expected: FAIL - AIDecisionExecutor not found

**Step 3: Create AIDecisionExecutor**

Create `src/FactionWars/ScriptHookV/Managers/AIDecisionExecutor.cs`:
```csharp
using System;
using FactionWars.AI.Interfaces;
using FactionWars.AI.Models;
using FactionWars.Factions.Interfaces;

namespace FactionWars.ScriptHookV.Managers
{
    /// <summary>
    /// Coordinates AI decision execution with budget enforcement.
    /// Handles auto-recruitment and attack cost deduction.
    /// </summary>
    public class AIDecisionExecutor
    {
        private readonly IFactionService _factionService;
        private readonly IAIBudgetService _budgetService;
        private readonly IAIRecruitmentService? _recruitmentService;

        /// <summary>
        /// Raised when an AI decision is about to be executed (after budget check passes).
        /// </summary>
        public event EventHandler<AIDecisionEventArgs>? OnDecisionExecuting;

        /// <summary>
        /// Creates a new AIDecisionExecutor.
        /// </summary>
        public AIDecisionExecutor(
            IFactionService factionService,
            IAIBudgetService budgetService,
            IAIRecruitmentService? recruitmentService)
        {
            _factionService = factionService ?? throw new ArgumentNullException(nameof(factionService));
            _budgetService = budgetService ?? throw new ArgumentNullException(nameof(budgetService));
            _recruitmentService = recruitmentService;
        }

        /// <summary>
        /// Processes a full AI decision cycle for a faction.
        /// 1. Auto-recruits troops
        /// 2. Executes attack if affordable
        /// </summary>
        /// <param name="factionId">The faction ID.</param>
        /// <param name="decision">The AI decision to execute.</param>
        public void ProcessDecisionCycle(string factionId, AIDecision decision)
        {
            // Step 1: Auto-recruit troops
            _recruitmentService?.TryAutoRecruit(factionId, maxTroopsToRecruit: 10);

            // Step 2: Execute attack if this is an attack decision
            if (decision.DecisionType == AIDecisionType.Attack)
            {
                TryExecuteAttack(factionId, decision);
            }
        }

        /// <summary>
        /// Attempts to execute an attack decision with budget enforcement.
        /// </summary>
        /// <param name="factionId">The attacking faction ID.</param>
        /// <param name="decision">The attack decision.</param>
        /// <returns>True if attack was executed, false if budget insufficient.</returns>
        public bool TryExecuteAttack(string factionId, AIDecision decision)
        {
            if (decision.DecisionType != AIDecisionType.Attack)
                return false;

            var state = _factionService.GetFactionState(factionId);
            if (state == null)
                return false;

            // Check budget
            if (!_budgetService.CanAffordAttack(state.Cash, decision.TroopsToCommit))
                return false;

            // Deduct attack cost
            var cost = _budgetService.CalculateAttackCost(decision.TroopsToCommit);
            _factionService.AddCash(factionId, -cost);

            // Raise event for BackgroundBattleSimulator to handle
            OnDecisionExecuting?.Invoke(this, new AIDecisionEventArgs(factionId, decision));

            return true;
        }
    }
}
```

**Step 4: Run tests to verify they pass**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~AIDecisionExecutorTests"`
Expected: All tests PASS

**Step 5: Commit**

```bash
git add src/FactionWars/ScriptHookV/Managers/AIDecisionExecutor.cs tests/FactionWars.Tests/Unit/ScriptHookV/AIDecisionExecutorTests.cs
git commit -m "feat(ai): add AIDecisionExecutor for budget-constrained attack execution"
```

---

## Task 6: Wire Everything in ServiceContainerFactory

**Files:**
- Modify: `src/FactionWars/ScriptHookV/ServiceContainerFactory.cs`

**Step 1: Add service registrations**

Find the `RegisterAIServices` method (or create it if not exists) and add:

```csharp
// Register AI budget service
container.RegisterSingleton<IAIBudgetService>(() => new AIBudgetService(
    costPerTroop: 50,
    recruitCostPerTroop: 100));

// Register AI recruitment service
container.RegisterSingleton<IAIRecruitmentService>(() => new AIRecruitmentService(
    container.Resolve<IFactionService>(),
    container.Resolve<IAIBudgetService>()));

// Register AI decision executor
container.RegisterSingleton<AIDecisionExecutor>(() => new AIDecisionExecutor(
    container.Resolve<IFactionService>(),
    container.Resolve<IAIBudgetService>(),
    container.Resolve<IAIRecruitmentService>()));
```

Add the using statements at the top:
```csharp
using FactionWars.AI.Interfaces;
using FactionWars.AI.Services;
```

**Step 2: Build and verify**

Run: `dotnet build src/FactionWars/FactionWars.csproj -c Release`
Expected: Build succeeds

**Step 3: Commit**

```bash
git add src/FactionWars/ScriptHookV/ServiceContainerFactory.cs
git commit -m "feat(ai): wire AIBudgetService, AIRecruitmentService, and AIDecisionExecutor"
```

---

## Task 7: Wire AIDecisionExecutor in GameLoopController

**Files:**
- Modify: `src/FactionWars/ScriptHookV/GameLoopController.cs`

**Step 1: Add field and initialization**

Add field near line 45:
```csharp
private AIDecisionExecutor? _aiDecisionExecutor;
```

In the initialization section (around line 500), add after BackgroundBattleSimulator initialization:
```csharp
// Initialize AI decision executor
_aiDecisionExecutor = _container.Resolve<AIDecisionExecutor>();
_aiDecisionExecutor.OnDecisionExecuting += _backgroundBattleSimulator.HandleAIDecision;
```

**Step 2: Replace the stubbed HandleAIDecision**

Find the `HandleAIDecision` method (around line 978) and replace:

```csharp
private void HandleAIDecision(object? sender, AIDecisionEventArgs e)
{
    // Route through decision executor for budget enforcement
    _aiDecisionExecutor?.ProcessDecisionCycle(e.FactionId, e.Decision);
}
```

**Step 3: Cleanup in Cleanup method**

Add to the Cleanup method:
```csharp
if (_aiDecisionExecutor != null && _backgroundBattleSimulator != null)
{
    _aiDecisionExecutor.OnDecisionExecuting -= _backgroundBattleSimulator.HandleAIDecision;
}
_aiDecisionExecutor = null;
```

**Step 4: Build and verify**

Run: `dotnet build src/FactionWars/FactionWars.csproj -c Release`
Expected: Build succeeds

**Step 5: Commit**

```bash
git add src/FactionWars/ScriptHookV/GameLoopController.cs
git commit -m "feat(ai): wire AIDecisionExecutor to GameLoopController for budget-enforced AI attacks"
```

---

## Task 8: Integration Test - Full AI Attack Cycle

**Files:**
- Test: `tests/FactionWars.Tests/Integration/AI/AITerritoryAttackIntegrationTests.cs`

**Step 1: Write the integration test**

Create `tests/FactionWars.Tests/Integration/AI/AITerritoryAttackIntegrationTests.cs`:
```csharp
using System.Collections.Generic;
using FactionWars.AI.Interfaces;
using FactionWars.AI.Models;
using FactionWars.AI.Services;
using FactionWars.AI.Strategies;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Services;
using FactionWars.Factions.Interfaces;
using FactionWars.Factions.Models;
using FactionWars.Factions.Services;
using FactionWars.ScriptHookV.Managers;
using FactionWars.Territory.Interfaces;
using FactionWars.Territory.Models;
using FactionWars.Territory.Services;
using Moq;
using Xunit;

namespace FactionWars.Tests.Integration.AI
{
    public class AITerritoryAttackIntegrationTests
    {
        [Fact]
        public void AIFaction_WithBudget_CanCaptureUnguardedTerritory()
        {
            // Arrange - Set up full integration scenario
            var factionService = new FactionService();
            var zoneService = new ZoneService();
            var budgetService = new AIBudgetService(costPerTroop: 50, recruitCostPerTroop: 100);
            var recruitmentService = new AIRecruitmentService(factionService, budgetService);

            // Create factions
            var trevor = new Faction("trevor", "Trevor's Crew", FactionType.Aggressive);
            var enemy = new Faction("enemy", "Enemy Gang", FactionType.Defensive);
            factionService.RegisterFaction(trevor);
            factionService.RegisterFaction(enemy);

            // Give Trevor money and troops
            factionService.AddCash("trevor", 5000);
            factionService.RecruitTroops("trevor", 20);

            // Create an unguarded enemy zone
            var zone = new Zone("zone1", "Unguarded Zone", new Vector3(0, 0, 0), 100f)
            {
                StrategicValue = 5,
                OwnerFactionId = "enemy"
            };
            zoneService.AddZone(zone);

            // Create decision executor
            var executor = new AIDecisionExecutor(factionService, budgetService, recruitmentService);

            // Create an attack decision
            var decision = new AIDecision(AIDecisionType.Attack, "zone1", 0.8f, 10);

            // Act - Execute the attack
            var result = executor.TryExecuteAttack("trevor", decision);

            // Assert
            Assert.True(result);

            // Verify cash was deducted
            var trevorState = factionService.GetFactionState("trevor");
            Assert.Equal(4500, trevorState?.Cash); // 5000 - (10 * 50)
        }

        [Fact]
        public void AIFaction_WithoutBudget_CannotAttack()
        {
            var factionService = new FactionService();
            var budgetService = new AIBudgetService(costPerTroop: 50);

            var trevor = new Faction("trevor", "Trevor's Crew", FactionType.Aggressive);
            factionService.RegisterFaction(trevor);
            factionService.AddCash("trevor", 100); // Only $100
            factionService.RecruitTroops("trevor", 20);

            var executor = new AIDecisionExecutor(factionService, budgetService, null);
            var decision = new AIDecision(AIDecisionType.Attack, "zone1", 0.8f, 10); // Cost: $500

            var result = executor.TryExecuteAttack("trevor", decision);

            Assert.False(result);

            // Verify no cash was deducted
            var trevorState = factionService.GetFactionState("trevor");
            Assert.Equal(100, trevorState?.Cash);
        }
    }
}
```

**Step 2: Run integration tests**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~AITerritoryAttackIntegrationTests"`
Expected: All tests PASS

**Step 3: Commit**

```bash
git add tests/FactionWars.Tests/Integration/AI/AITerritoryAttackIntegrationTests.cs
git commit -m "test(ai): add integration tests for budget-constrained AI territory attacks"
```

---

## Task 9: Final Build, Deploy, and Test

**Step 1: Run all tests**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj`
Expected: All tests PASS

**Step 2: Build release**

Run: `dotnet build src/FactionWars/FactionWars.csproj -c Release`
Expected: Build succeeds

**Step 3: Deploy to GTA V**

```bash
cp "src/FactionWars/bin/Release/net48/FactionWars.dll" "E:/SteamLibrary/steamapps/common/Grand Theft Auto V/scripts/"
```

**Step 4: Final commit**

```bash
git add -A
git commit -m "feat(ai): complete AI territory capture system with budget constraints and probability targeting"
```

---

## Summary

After implementing this plan, AI factions will:

1. **Make decisions every 2.5 minutes** - Strategic pacing for big pushes
2. **Auto-recruit troops** - Spend cash to build army before attacking
3. **Check budget before attacking** - Can't attack if can't afford
4. **Prefer unguarded targets** - 3x score multiplier for 0 defenders
5. **Use probability selection** - Usually picks good targets but occasionally varies
6. **Deduct attack costs** - Cash spent on each attack

**Cost structure:**
- Attack cost: 50 per troop committed
- Recruitment cost: 100 per troop

**Scoring multipliers:**
- Unguarded (0 defenders): 3.0x
- Lightly guarded (1-3): 2.0x
- Moderately guarded (4-7): 1.0x
- Heavily guarded (8+): 0.3x
- Neutral zone: 1.5x bonus
