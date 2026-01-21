# AI Controller Consolidation Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Consolidate scattered AI logic (AIManager, AIDecisionExecutor, BackgroundBattleSimulator, recruitment) into a single `AIController` class with an `IAIController` interface, enabling swappable AI implementations.

**Architecture:** Create `AIController` that owns all AI timing (decision cycles, recruitment), strategy execution, budget enforcement, and battle simulation. GameLoopController only calls `Update()`. Old classes remain but are deprecated.

**Tech Stack:** C#, .NET Framework 4.8, xUnit for testing

---

### Task 1: Add SetPlayerZone to IAIController Interface

The interface needs a method to tell the AI controller where the player is (so it doesn't simulate battles there).

**Files:**
- Modify: `src/FactionWars/AI/Interfaces/IAIController.cs`

**Step 1: Add the method to the interface**

Add after `PlayerFactionId` property:

```csharp
/// <summary>
/// Sets the current zone where the player is located.
/// Battles will not be simulated in this zone.
/// </summary>
/// <param name="zoneId">The zone ID, or null if not in any zone.</param>
void SetPlayerZone(string? zoneId);

/// <summary>
/// Gets the current zone where the player is located.
/// </summary>
string? PlayerZoneId { get; }
```

**Step 2: Build to verify compilation**

Run: `dotnet build src/FactionWars/FactionWars.csproj --verbosity quiet`
Expected: Build succeeded (warnings OK)

**Step 3: Commit**

```bash
git add src/FactionWars/AI/Interfaces/IAIController.cs
git commit -m "feat: Add SetPlayerZone to IAIController interface"
```

---

### Task 2: Create AIController Implementation - Core Structure

**Files:**
- Create: `src/FactionWars/AI/Controllers/AIController.cs`
- Create: `tests/FactionWars.Tests/Unit/AI/AIControllerTests.cs`

**Step 1: Write the failing test**

```csharp
using FactionWars.AI.Controllers;
using FactionWars.AI.Interfaces;
using FactionWars.Core.Interfaces;
using FactionWars.Factions.Interfaces;
using FactionWars.Territory.Interfaces;
using FactionWars.UI.Interfaces;
using Moq;
using System.Collections.Generic;
using Xunit;

namespace FactionWars.Tests.Unit.AI
{
    public class AIControllerTests
    {
        private readonly Mock<IFactionService> _factionServiceMock;
        private readonly Mock<IZoneService> _zoneServiceMock;
        private readonly Mock<IBattleSimulationService> _battleSimulationServiceMock;
        private readonly Mock<IZoneDefenderAllocationService> _allocationServiceMock;
        private readonly Mock<IEventFeedService> _eventFeedServiceMock;
        private readonly Dictionary<string, IAIStrategy> _strategies;

        public AIControllerTests()
        {
            _factionServiceMock = new Mock<IFactionService>();
            _zoneServiceMock = new Mock<IZoneService>();
            _battleSimulationServiceMock = new Mock<IBattleSimulationService>();
            _allocationServiceMock = new Mock<IZoneDefenderAllocationService>();
            _eventFeedServiceMock = new Mock<IEventFeedService>();
            _strategies = new Dictionary<string, IAIStrategy>();
        }

        [Fact]
        public void Constructor_ShouldInitializeWithIsRunningFalse()
        {
            var controller = CreateController();

            Assert.False(controller.IsRunning);
        }

        [Fact]
        public void Start_ShouldSetIsRunningTrue()
        {
            var controller = CreateController();

            controller.Start();

            Assert.True(controller.IsRunning);
        }

        [Fact]
        public void Stop_ShouldSetIsRunningFalse()
        {
            var controller = CreateController();
            controller.Start();

            controller.Stop();

            Assert.False(controller.IsRunning);
        }

        [Fact]
        public void SetPlayerFactionId_ShouldStoreValue()
        {
            var controller = CreateController();

            controller.SetPlayerFactionId("michael");

            Assert.Equal("michael", controller.PlayerFactionId);
        }

        [Fact]
        public void SetPlayerZone_ShouldStoreValue()
        {
            var controller = CreateController();

            controller.SetPlayerZone("vinewood");

            Assert.Equal("vinewood", controller.PlayerZoneId);
        }

        private AIController CreateController()
        {
            return new AIController(
                _factionServiceMock.Object,
                _zoneServiceMock.Object,
                _battleSimulationServiceMock.Object,
                _allocationServiceMock.Object,
                _eventFeedServiceMock.Object,
                _strategies);
        }
    }
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/FactionWars.Tests --filter "FullyQualifiedName~AIControllerTests" --verbosity quiet`
Expected: FAIL - AIController class not found

**Step 3: Create the AIController class**

Create directory if needed, then create file:

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using FactionWars.AI.Interfaces;
using FactionWars.AI.Models;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.Factions.Interfaces;
using FactionWars.Factions.Models;
using FactionWars.Territory.Interfaces;
using FactionWars.UI.Interfaces;

namespace FactionWars.AI.Controllers
{
    /// <summary>
    /// Consolidated AI controller that manages all AI faction behavior.
    /// Handles decision-making, recruitment, budget enforcement, and battle simulation.
    /// </summary>
    public class AIController : IAIController
    {
        // Dependencies
        private readonly IFactionService _factionService;
        private readonly IZoneService _zoneService;
        private readonly IBattleSimulationService _battleSimulationService;
        private readonly IZoneDefenderAllocationService _allocationService;
        private readonly IEventFeedService _eventFeedService;
        private readonly IDictionary<string, IAIStrategy> _strategies;

        // Configuration
        private const float DefaultDecisionIntervalSeconds = 30f;
        private const float DefaultRecruitmentIntervalSeconds = 60f;
        private const int RecruitCostPerTroop = 100;
        private const int AttackCostPerTroop = 50;
        private const int MaxRecruitPerCycle = 5;

        // State
        private bool _isRunning;
        private string? _playerFactionId;
        private string? _playerZoneId;
        private float _decisionTimer;
        private float _recruitmentTimer;

        /// <inheritdoc />
        public bool IsRunning => _isRunning;

        /// <inheritdoc />
        public string? PlayerFactionId => _playerFactionId;

        /// <inheritdoc />
        public string? PlayerZoneId => _playerZoneId;

        /// <inheritdoc />
        public event EventHandler<AIAttackEventArgs>? OnAttackStarted;

        /// <inheritdoc />
        public event EventHandler<AIBattleResultEventArgs>? OnBattleResolved;

        /// <summary>
        /// Creates a new AIController.
        /// </summary>
        public AIController(
            IFactionService factionService,
            IZoneService zoneService,
            IBattleSimulationService battleSimulationService,
            IZoneDefenderAllocationService allocationService,
            IEventFeedService eventFeedService,
            IDictionary<string, IAIStrategy> strategies)
        {
            _factionService = factionService ?? throw new ArgumentNullException(nameof(factionService));
            _zoneService = zoneService ?? throw new ArgumentNullException(nameof(zoneService));
            _battleSimulationService = battleSimulationService ?? throw new ArgumentNullException(nameof(battleSimulationService));
            _allocationService = allocationService ?? throw new ArgumentNullException(nameof(allocationService));
            _eventFeedService = eventFeedService ?? throw new ArgumentNullException(nameof(eventFeedService));
            _strategies = strategies ?? throw new ArgumentNullException(nameof(strategies));

            _isRunning = false;
            _decisionTimer = 0f;
            _recruitmentTimer = 0f;
        }

        /// <inheritdoc />
        public void Start()
        {
            _isRunning = true;
            _decisionTimer = 0f;
            _recruitmentTimer = 0f;
        }

        /// <inheritdoc />
        public void Stop()
        {
            _isRunning = false;
        }

        /// <inheritdoc />
        public void SetPlayerFactionId(string? factionId)
        {
            _playerFactionId = factionId;
        }

        /// <inheritdoc />
        public void SetPlayerZone(string? zoneId)
        {
            _playerZoneId = zoneId;
        }

        /// <inheritdoc />
        public void Update(float deltaTimeSeconds)
        {
            if (!_isRunning)
                return;

            // Update recruitment timer
            _recruitmentTimer += deltaTimeSeconds;
            if (_recruitmentTimer >= DefaultRecruitmentIntervalSeconds)
            {
                _recruitmentTimer = 0f;
                RecruitForAllAIFactions();
            }

            // Update decision timer
            _decisionTimer += deltaTimeSeconds;
            if (_decisionTimer >= DefaultDecisionIntervalSeconds)
            {
                _decisionTimer = 0f;
                MakeDecisionsForAllAIFactions();
            }
        }

        private void RecruitForAllAIFactions()
        {
            var factions = _factionService.GetActiveFactions();

            foreach (var faction in factions)
            {
                if (faction.Id == _playerFactionId)
                    continue;

                TryRecruitTroops(faction.Id);
            }
        }

        private void TryRecruitTroops(string factionId)
        {
            var state = _factionService.GetFactionState(factionId);
            if (state == null)
                return;

            int affordableTroops = state.Cash / RecruitCostPerTroop;
            int troopsToRecruit = Math.Min(affordableTroops, MaxRecruitPerCycle);

            if (troopsToRecruit <= 0)
                return;

            int cost = troopsToRecruit * RecruitCostPerTroop;
            _factionService.RecruitTroops(factionId, troopsToRecruit);
            _factionService.SpendCash(factionId, cost);
        }

        private void MakeDecisionsForAllAIFactions()
        {
            var factions = _factionService.GetActiveFactions();

            foreach (var faction in factions)
            {
                if (faction.Id == _playerFactionId)
                    continue;

                MakeDecisionForFaction(faction.Id);
            }
        }

        private void MakeDecisionForFaction(string factionId)
        {
            if (!_strategies.TryGetValue(factionId, out var strategy))
                return;

            var faction = _factionService.GetFaction(factionId);
            if (faction == null)
                return;

            var factionState = _factionService.GetFactionState(factionId);
            if (factionState == null)
                return;

            var context = BuildAIContext(faction, factionState);
            var decisions = strategy.MakeDecisions(context);

            foreach (var decision in decisions)
            {
                if (decision.DecisionType == AIDecisionType.Attack)
                {
                    ExecuteAttackDecision(factionId, decision);
                }
            }
        }

        private AIContext BuildAIContext(Faction faction, FactionState factionState)
        {
            var allZones = _zoneService.GetAllZones().ToList();
            var ownedZones = _zoneService.GetZonesByOwner(faction.Id);
            var allFactions = _factionService.GetAllFactions();
            var enemyFactions = allFactions.Where(f => f.Id != faction.Id);

            return new AIContext(
                faction,
                factionState,
                ownedZones,
                allZones,
                enemyFactions);
        }

        private void ExecuteAttackDecision(string attackerFactionId, AIDecision decision)
        {
            if (decision.TargetZoneId == null)
                return;

            // Check budget
            var state = _factionService.GetFactionState(attackerFactionId);
            if (state == null)
                return;

            int cost = decision.TroopsToCommit * AttackCostPerTroop;
            if (state.Cash < cost)
                return;

            // Spend cash
            _factionService.SpendCash(attackerFactionId, cost);

            // Raise attack started event
            OnAttackStarted?.Invoke(this, new AIAttackEventArgs(
                attackerFactionId,
                decision.TargetZoneId,
                decision.TroopsToCommit));

            // Don't simulate if player is in the zone
            if (_playerZoneId == decision.TargetZoneId)
                return;

            // Simulate the battle
            SimulateBattle(attackerFactionId, decision);
        }

        private void SimulateBattle(string attackerFactionId, AIDecision decision)
        {
            var zone = _zoneService.GetZone(decision.TargetZoneId!);
            if (zone == null)
                return;

            // Can't attack own zone
            if (zone.OwnerFactionId == attackerFactionId)
                return;

            var attackerFaction = _factionService.GetFaction(attackerFactionId);
            var attackerFactionName = attackerFaction?.Name ?? attackerFactionId;

            // Handle neutral zone capture
            if (zone.OwnerFactionId == null)
            {
                _zoneService.TransferZoneOwnership(decision.TargetZoneId!, attackerFactionId);
                _eventFeedService.AddZoneCaptured(zone.Name, attackerFactionName);

                OnBattleResolved?.Invoke(this, new AIBattleResultEventArgs(
                    attackerFactionId, "neutral", decision.TargetZoneId!, true, 0, 0));
                return;
            }

            var defenderFactionId = zone.OwnerFactionId;

            // Build troop compositions
            var attackerTroops = new TroopComposition(decision.TroopsToCommit, 0, 0);
            var defenderTroops = BuildDefenderTroops(defenderFactionId, decision.TargetZoneId!);

            // Simulate battle
            var result = _battleSimulationService.SimulateBattle(
                attackerFactionId,
                defenderFactionId,
                decision.TargetZoneId!,
                attackerTroops,
                defenderTroops);

            // Apply results
            ApplyBattleResult(result);

            // Notify
            var defenderFactionName = _factionService.GetFaction(defenderFactionId)?.Name ?? defenderFactionId;
            if (result.AttackerWon)
            {
                _eventFeedService.AddZoneCaptured(zone.Name, attackerFactionName);
            }
            else
            {
                _eventFeedService.AddCombatEnded(zone.Name, defenderFactionName, defenderWon: true);
            }

            OnBattleResolved?.Invoke(this, new AIBattleResultEventArgs(
                attackerFactionId,
                defenderFactionId,
                decision.TargetZoneId!,
                result.AttackerWon,
                result.AttackerCasualties.TotalCount,
                result.DefenderCasualties.TotalCount));
        }

        private TroopComposition BuildDefenderTroops(string defenderFactionId, string zoneId)
        {
            var allocation = _allocationService.GetAllocation(defenderFactionId, zoneId);
            if (allocation == null)
                return TroopComposition.Empty;

            return new TroopComposition(
                allocation.GetTroopCount(DefenderTier.Basic),
                allocation.GetTroopCount(DefenderTier.Medium),
                allocation.GetTroopCount(DefenderTier.Heavy));
        }

        private void ApplyBattleResult(BattleSimulationResult result)
        {
            int attackerCasualties = result.AttackerCasualties.TotalCount;
            if (attackerCasualties > 0)
            {
                _factionService.LoseTroops(result.AttackerFactionId, attackerCasualties);
            }

            int defenderCasualties = result.DefenderCasualties.TotalCount;
            if (defenderCasualties > 0)
            {
                _factionService.LoseTroops(result.DefenderFactionId, defenderCasualties);
            }

            if (result.AttackerWon)
            {
                _zoneService.TransferZoneOwnership(result.ZoneId, result.AttackerFactionId);
            }
        }
    }
}
```

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/FactionWars.Tests --filter "FullyQualifiedName~AIControllerTests" --verbosity quiet`
Expected: PASS (5 tests)

**Step 5: Commit**

```bash
git add src/FactionWars/AI/Controllers/AIController.cs tests/FactionWars.Tests/Unit/AI/AIControllerTests.cs
git commit -m "feat: Create AIController with core structure"
```

---

### Task 3: Add Update Behavior Tests

**Files:**
- Modify: `tests/FactionWars.Tests/Unit/AI/AIControllerTests.cs`

**Step 1: Add recruitment timer test**

```csharp
[Fact]
public void Update_After60Seconds_ShouldTriggerRecruitment()
{
    // Arrange
    var faction = new Faction("trevor", "Trevor", new FactionColor(255, 150, 0));
    var factionState = new FactionState("trevor", 1000, 5);

    _factionServiceMock.Setup(f => f.GetActiveFactions())
        .Returns(new[] { faction });
    _factionServiceMock.Setup(f => f.GetFactionState("trevor"))
        .Returns(factionState);

    var controller = CreateController();
    controller.Start();

    // Act - simulate 60 seconds
    controller.Update(60f);

    // Assert - should have recruited (1000 cash / 100 per troop = 10, capped at 5)
    _factionServiceMock.Verify(f => f.RecruitTroops("trevor", 5), Times.Once);
    _factionServiceMock.Verify(f => f.SpendCash("trevor", 500), Times.Once);
}

[Fact]
public void Update_WhenNotRunning_ShouldNotProcess()
{
    var controller = CreateController();
    // Don't call Start()

    controller.Update(100f);

    _factionServiceMock.Verify(f => f.GetActiveFactions(), Times.Never);
}

[Fact]
public void Update_ShouldSkipPlayerFaction()
{
    var playerFaction = new Faction("michael", "Michael", new FactionColor(0, 100, 255));
    var aiFaction = new Faction("trevor", "Trevor", new FactionColor(255, 150, 0));

    _factionServiceMock.Setup(f => f.GetActiveFactions())
        .Returns(new[] { playerFaction, aiFaction });
    _factionServiceMock.Setup(f => f.GetFactionState("trevor"))
        .Returns(new FactionState("trevor", 1000, 5));

    var controller = CreateController();
    controller.SetPlayerFactionId("michael");
    controller.Start();

    controller.Update(60f);

    // Should recruit for trevor but not michael
    _factionServiceMock.Verify(f => f.RecruitTroops("trevor", It.IsAny<int>()), Times.Once);
    _factionServiceMock.Verify(f => f.RecruitTroops("michael", It.IsAny<int>()), Times.Never);
}
```

**Step 2: Run tests**

Run: `dotnet test tests/FactionWars.Tests --filter "FullyQualifiedName~AIControllerTests" --verbosity quiet`
Expected: PASS (8 tests)

**Step 3: Commit**

```bash
git add tests/FactionWars.Tests/Unit/AI/AIControllerTests.cs
git commit -m "test: Add AIController update behavior tests"
```

---

### Task 4: Register AIController in ServiceContainerFactory

**Files:**
- Modify: `src/FactionWars/ScriptHookV/ServiceContainerFactory.cs`

**Step 1: Add using statement**

Add at top of file:
```csharp
using FactionWars.AI.Controllers;
```

**Step 2: Register AIController in RegisterAIServices method**

Find the `RegisterAIServices` method and add at the end:

```csharp
// Register consolidated AI controller
container.RegisterSingleton<IAIController>(() => new AIController(
    container.Resolve<IFactionService>(),
    container.Resolve<IZoneService>(),
    container.Resolve<IBattleSimulationService>(),
    container.Resolve<IZoneDefenderAllocationService>(),
    container.Resolve<IEventFeedService>(),
    container.Resolve<IDictionary<string, IAIStrategy>>()));
```

**Step 3: Build to verify**

Run: `dotnet build src/FactionWars/FactionWars.csproj --verbosity quiet`
Expected: Build succeeded

**Step 4: Commit**

```bash
git add src/FactionWars/ScriptHookV/ServiceContainerFactory.cs
git commit -m "feat: Register AIController in ServiceContainerFactory"
```

---

### Task 5: Update GameLoopController to Use IAIController

**Files:**
- Modify: `src/FactionWars/ScriptHookV/GameLoopController.cs`

**Step 1: Add field for IAIController**

Find the field declarations and add:
```csharp
private IAIController? _aiController;
```

**Step 2: Replace AI wiring in InitializeManagers**

Find the section that initializes `_aiManager`, `_backgroundBattleSimulator`, and `_aiDecisionExecutor`. Replace with:

```csharp
// Initialize consolidated AI controller
_aiController = _container.Resolve<IAIController>();
_aiController.SetPlayerFactionId(CurrentPlayerFactionId);
_aiController.Start();
```

**Step 3: Update the Update method**

Find and remove these lines:
- `_aiManager?.Update(deltaTime);`
- The `_aiRecruitmentTimer` block
- The `RecruitForAllAIFactions()` call

Replace with:
```csharp
// Update AI controller (handles decisions, recruitment, battles)
_aiController?.Update(deltaTime);
```

**Step 4: Update SetPlayerZone calls**

Find any place that calls `_backgroundBattleSimulator?.SetPlayerZone()` and add:
```csharp
_aiController?.SetPlayerZone(zoneId);
```

**Step 5: Update Cleanup**

In the cleanup method, add:
```csharp
_aiController?.Stop();
_aiController = null;
```

**Step 6: Remove old HandleAIDecision method and RecruitForAllAIFactions**

Delete the `HandleAIDecision` and `RecruitForAllAIFactions` methods as they're now internal to AIController.

**Step 7: Build to verify**

Run: `dotnet build src/FactionWars/FactionWars.csproj --verbosity quiet`
Expected: Build succeeded

**Step 8: Commit**

```bash
git add src/FactionWars/ScriptHookV/GameLoopController.cs
git commit -m "refactor: Use IAIController in GameLoopController"
```

---

### Task 6: Run Full Test Suite and Deploy

**Step 1: Run all tests**

Run: `dotnet test tests/FactionWars.Tests --verbosity quiet`
Expected: Most tests pass (some pre-existing failures OK)

**Step 2: Build Release**

Run: `dotnet build src/FactionWars/FactionWars.csproj -c Release --verbosity quiet`
Expected: Build succeeded

**Step 3: Deploy**

Run: `cp src/FactionWars/bin/Release/net48/FactionWars.dll "E:/SteamLibrary/steamapps/common/Grand Theft Auto V/scripts/"`

**Step 4: Commit all changes**

```bash
git add -A
git commit -m "feat: Complete AI controller consolidation"
```

---

## Summary

After completing this plan:

1. **Single interface** - `IAIController` defines the contract
2. **Single implementation** - `AIController` owns all AI logic
3. **Swappable** - Create alternative implementations (e.g., `AggressiveAIController`, `PassiveAIController`)
4. **Clean GameLoopController** - Just calls `_aiController.Update(deltaTime)`
5. **Old classes remain** - `AIManager`, `AIDecisionExecutor`, `BackgroundBattleSimulator` still exist but are no longer used by GameLoopController

## Files Changed

| File | Action |
|------|--------|
| `src/FactionWars/AI/Interfaces/IAIController.cs` | Already created, add SetPlayerZone |
| `src/FactionWars/AI/Controllers/AIController.cs` | Create |
| `tests/FactionWars.Tests/Unit/AI/AIControllerTests.cs` | Create |
| `src/FactionWars/ScriptHookV/ServiceContainerFactory.cs` | Register AIController |
| `src/FactionWars/ScriptHookV/GameLoopController.cs` | Use IAIController |
