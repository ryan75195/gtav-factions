# Wire Unused MVP Services Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Wire the AI background battle simulation and event feed display systems that are registered but not actively called in the game loop.

**Architecture:** BackgroundBattleSimulator handles AI vs AI combat when AIManager makes attack decisions. EventFeedRenderer displays world events on screen. IAggressionResponseService tracks player attacks to inform AI retaliation. All services exist - they just need to be instantiated, connected via events, and rendered.

**Tech Stack:** C#, ScriptHookVDotNet, existing service layer

---

## Background: Service Status

| Service | Registered | Instantiated | Called | Gap |
|---------|------------|--------------|--------|-----|
| `IZoneEvaluationService` | ✓ | ✓ | ✗ | Used by AI strategies internally |
| `IResourceAllocationService` | ✓ | ✓ | ✗ | Used by AI strategies internally |
| `IAggressionResponseService` | ✓ | ✓ | ✗ | Not recording player attacks |
| `IAIDifficultyService` | ✓ | ✓ | ✗ | Could wire to settings menu |
| `IBattleSimulationService` | ✓ | ✓ | ✗ | BackgroundBattleSimulator not created |
| `IEventFeedService` | ✓ | ✓ | ✗ | No renderer on screen |
| `IEventAlertService` | ✓ | ✓ | ✗ | Not called from battle results |
| `BackgroundBattleSimulator` | ✗ | ✗ | ✗ | **Primary gap** |
| `EventFeedRenderer` | ✗ | ✗ | ✗ | **Primary gap** |

---

## Task 1: Wire BackgroundBattleSimulator in ServiceContainerFactory

**Files:**
- Modify: `src/FactionWars/ScriptHookV/ServiceContainerFactory.cs`

**Step 1: Add BackgroundBattleSimulator registration**

In `RegisterAIServices()` method, add after the existing AI service registrations:

```csharp
// Background battle simulator for AI vs AI combat
container.RegisterSingleton<BackgroundBattleSimulator>(() =>
    new BackgroundBattleSimulator(
        container.Resolve<IBattleSimulationService>(),
        container.Resolve<IFactionService>(),
        container.Resolve<IZoneService>(),
        container.Resolve<IZoneDefenderAllocationService>()));
```

**Step 2: Build and verify**

```bash
dotnet build src/FactionWars/FactionWars.csproj
```

**Step 3: Commit**

```bash
git add src/FactionWars/ScriptHookV/ServiceContainerFactory.cs
git commit -m "feat: register BackgroundBattleSimulator in service container"
```

---

## Task 2: Instantiate BackgroundBattleSimulator in GameLoopController

**Files:**
- Modify: `src/FactionWars/ScriptHookV/GameLoopController.cs`

**Step 1: Add field declaration**

Near other manager fields (around line 50):

```csharp
private BackgroundBattleSimulator? _backgroundBattleSimulator;
```

**Step 2: Resolve and wire in InitializeGameData**

After AIManager is created (find `_aiManager = new AIManager`), add:

```csharp
// Wire background battle simulator for AI vs AI combat
_backgroundBattleSimulator = _container.Resolve<BackgroundBattleSimulator>();
_aiManager.OnAIDecision += _backgroundBattleSimulator.HandleAIDecision;
```

**Step 3: Update player zone tracking**

In the `OnZoneEntered` handler, add:

```csharp
// Tell battle simulator to skip battles in player's current zone
_backgroundBattleSimulator?.SetPlayerZone(zone.Id);
```

In the `OnZoneExited` handler, add:

```csharp
// Clear player zone tracking
_backgroundBattleSimulator?.SetPlayerZone(null);
```

**Step 4: Cleanup in OnAbort**

In `OnAbort()`, add cleanup:

```csharp
if (_aiManager != null && _backgroundBattleSimulator != null)
{
    _aiManager.OnAIDecision -= _backgroundBattleSimulator.HandleAIDecision;
}
_backgroundBattleSimulator = null;
```

**Step 5: Build and verify**

```bash
dotnet build src/FactionWars/FactionWars.csproj
```

**Step 6: Commit**

```bash
git add src/FactionWars/ScriptHookV/GameLoopController.cs
git commit -m "feat: wire BackgroundBattleSimulator to AIManager events"
```

---

## Task 3: Add Event Alerts for AI Battle Results

**Files:**
- Modify: `src/FactionWars/ScriptHookV/Managers/BackgroundBattleSimulator.cs`

**Step 1: Add IEventAlertService and IEventFeedService dependencies**

Update constructor to accept alert/feed services:

```csharp
private readonly IEventAlertService _eventAlertService;
private readonly IEventFeedService _eventFeedService;

public BackgroundBattleSimulator(
    IBattleSimulationService battleSimulationService,
    IFactionService factionService,
    IZoneService zoneService,
    IZoneDefenderAllocationService allocationService,
    IEventAlertService eventAlertService,
    IEventFeedService eventFeedService)
{
    _battleSimulationService = battleSimulationService ?? throw new ArgumentNullException(nameof(battleSimulationService));
    _factionService = factionService ?? throw new ArgumentNullException(nameof(factionService));
    _zoneService = zoneService ?? throw new ArgumentNullException(nameof(zoneService));
    _allocationService = allocationService ?? throw new ArgumentNullException(nameof(allocationService));
    _eventAlertService = eventAlertService ?? throw new ArgumentNullException(nameof(eventAlertService));
    _eventFeedService = eventFeedService ?? throw new ArgumentNullException(nameof(eventFeedService));
}
```

**Step 2: Add event notifications in ProcessAttackDecision**

After battle resolution (where zone ownership changes), add:

```csharp
// Notify UI of battle result
var attackerFaction = _factionService.GetFaction(attackerFactionId);
var defenderFaction = _factionService.GetFaction(zone.OwnerFactionId);

if (result.AttackerWon)
{
    _eventFeedService.AddZoneCaptured(zone.Name, attackerFaction?.Name ?? attackerFactionId);
    _eventAlertService.RaiseZoneCaptured(zone.Name, attackerFaction?.Name ?? attackerFactionId);
}
else
{
    _eventFeedService.AddCombatEnded(zone.Name, defenderFaction?.Name ?? "Defender", defenderWon: true);
}
```

**Step 3: Update ServiceContainerFactory registration**

Update the BackgroundBattleSimulator registration to include new dependencies:

```csharp
container.RegisterSingleton<BackgroundBattleSimulator>(() =>
    new BackgroundBattleSimulator(
        container.Resolve<IBattleSimulationService>(),
        container.Resolve<IFactionService>(),
        container.Resolve<IZoneService>(),
        container.Resolve<IZoneDefenderAllocationService>(),
        container.Resolve<IEventAlertService>(),
        container.Resolve<IEventFeedService>()));
```

**Step 4: Build and verify**

```bash
dotnet build src/FactionWars/FactionWars.csproj
```

**Step 5: Commit**

```bash
git add src/FactionWars/ScriptHookV/Managers/BackgroundBattleSimulator.cs src/FactionWars/ScriptHookV/ServiceContainerFactory.cs
git commit -m "feat: add event alerts for AI battle results"
```

---

## Task 4: Wire EventFeedRenderer to Display on Screen

**Files:**
- Modify: `src/FactionWars/ScriptHookV/GameLoopController.cs`

**Step 1: Add field and resolve EventFeedRenderer**

Add field:

```csharp
private EventFeedRenderer? _eventFeedRenderer;
```

In `InitializeGameData`, after resolving other services:

```csharp
// Event feed renderer for displaying world events
var eventFeedService = _container.Resolve<IEventFeedService>();
_eventFeedRenderer = new EventFeedRenderer(_container.Resolve<IFactionRepository>());
```

**Step 2: Render event feed in OnTick**

In `OnTick()`, after other HUD rendering (find where `_territoryIndicatorRenderer?.Render` is called), add:

```csharp
// Render event feed
var eventFeedService = _container.Resolve<IEventFeedService>();
_eventFeedRenderer?.Render(eventFeedService.Entries);
```

**Step 3: Cleanup in OnAbort**

```csharp
_eventFeedRenderer = null;
```

**Step 4: Build and verify**

```bash
dotnet build src/FactionWars/FactionWars.csproj
```

**Step 5: Commit**

```bash
git add src/FactionWars/ScriptHookV/GameLoopController.cs
git commit -m "feat: render event feed on screen"
```

---

## Task 5: Record Player Aggression for AI Retaliation

**Files:**
- Modify: `src/FactionWars/ScriptHookV/Managers/CombatManager.cs`

**Step 1: Add IAggressionResponseService dependency**

Add field and constructor parameter:

```csharp
private readonly IAggressionResponseService _aggressionResponseService;

// In constructor, add parameter and assignment:
IAggressionResponseService aggressionResponseService,
// ...
_aggressionResponseService = aggressionResponseService ?? throw new ArgumentNullException(nameof(aggressionResponseService));
```

**Step 2: Record aggression when combat starts**

In `StartCombat` method, after creating the encounter:

```csharp
// Record player aggression for AI retaliation tracking
_aggressionResponseService.RecordAggression(
    attackingFactionId,
    zone.Id,
    damage: 1);  // Base damage value, could scale with troop count
```

**Step 3: Update ServiceContainerFactory**

Find where CombatManager is created (likely in GameLoopController) and add the dependency.

**Step 4: Update GameLoopController to pass dependency**

In `InitializeGameData` where CombatManager is created:

```csharp
var aggressionResponseService = _container.Resolve<IAggressionResponseService>();
_combatManager = new CombatManager(
    // ... existing parameters ...
    aggressionResponseService);
```

**Step 5: Build and verify**

```bash
dotnet build src/FactionWars/FactionWars.csproj
```

**Step 6: Commit**

```bash
git add src/FactionWars/ScriptHookV/Managers/CombatManager.cs src/FactionWars/ScriptHookV/GameLoopController.cs
git commit -m "feat: record player aggression for AI retaliation"
```

---

## Task 6: Add Periodic Threat Decay

**Files:**
- Modify: `src/FactionWars/ScriptHookV/GameLoopController.cs`

**Step 1: Add threat decay timer**

Add fields:

```csharp
private float _threatDecayTimer = 0f;
private const float ThreatDecayInterval = 60f;  // Decay every 60 seconds
private const float ThreatDecayRate = 0.1f;     // 10% decay per interval
```

**Step 2: Call decay in Update loop**

In `OnTick()`, in the update section:

```csharp
// Decay AI threat levels over time
_threatDecayTimer += deltaTime;
if (_threatDecayTimer >= ThreatDecayInterval)
{
    _threatDecayTimer = 0f;
    var aggressionService = _container.Resolve<IAggressionResponseService>();
    aggressionService.DecayThreatLevels(ThreatDecayRate);
}
```

**Step 3: Build and verify**

```bash
dotnet build src/FactionWars/FactionWars.csproj
```

**Step 4: Commit**

```bash
git add src/FactionWars/ScriptHookV/GameLoopController.cs
git commit -m "feat: add periodic threat level decay for AI"
```

---

## Task 7: Add Combat Events to Event Feed

**Files:**
- Modify: `src/FactionWars/ScriptHookV/GameLoopController.cs`

**Step 1: Add event feed entries for player combat**

In `OnZoneEntered`, when combat starts:

```csharp
var eventFeedService = _container.Resolve<IEventFeedService>();
var attackerFaction = _container.Resolve<IFactionService>().GetFaction(CurrentPlayerFactionId);
var defenderFaction = _container.Resolve<IFactionService>().GetFaction(zone.OwnerFactionId);
eventFeedService.AddCombatStarted(
    zone.Name,
    attackerFaction?.Name ?? "Player",
    defenderFaction?.Name ?? "Defender");
```

**Step 2: Add combat end event**

In `OnCombatEnded` handler:

```csharp
var eventFeedService = _container.Resolve<IEventFeedService>();
if (encounter.Status == CombatStatus.AttackerVictory)
{
    eventFeedService.AddZoneCaptured(zone?.Name ?? "Unknown", "You");
}
else if (encounter.Status == CombatStatus.DefenderVictory)
{
    eventFeedService.AddCombatEnded(zone?.Name ?? "Unknown", "Defender", defenderWon: true);
}
```

**Step 3: Build and verify**

```bash
dotnet build src/FactionWars/FactionWars.csproj
```

**Step 4: Commit**

```bash
git add src/FactionWars/ScriptHookV/GameLoopController.cs
git commit -m "feat: add player combat events to event feed"
```

---

## Task 8: Document Unused Services

**Files:**
- Create: `docs/UNUSED_SERVICES.md`

**Step 1: Create documentation file**

```markdown
# Unused Services Documentation

This document tracks services that are registered but not actively wired into the game loop.
These are candidates for future implementation or removal.

## Status: Registered but Unused

### Over-Engineering (Consider Removal)

| Service | Reason | Alternative |
|---------|--------|-------------|
| `IPedRecyclingService` | Premature optimization | Direct spawn/delete via GameBridge |
| `IDefenderScalingService` | Over-abstracted | Direct calculation in CombatManager |
| `IDefenderCasualtyService` | Not needed for MVP | Troops simply deducted on death |
| `IMapBlipService` | Bypassed | MapBlipManager handles directly |

### Coordinator Classes (Duplicated Logic)

| Class | Reason | Current Implementation |
|-------|--------|----------------------|
| `CombatTriggerCoordinator` | Logic in GameLoopController | OnZoneEntered/OnZoneExited handlers |

### Future Features (Out of Scope)

| Service | RFP Section | Notes |
|---------|-------------|-------|
| `IFactionRelationshipService` | Out of scope | Diplomacy system not in MVP |

## Recently Wired (This Sprint)

| Service | Task | Status |
|---------|------|--------|
| `BackgroundBattleSimulator` | Task 1-3 | ✓ Wired |
| `IEventFeedService` + Renderer | Task 4, 7 | ✓ Wired |
| `IEventAlertService` | Task 3 | ✓ Wired |
| `IAggressionResponseService` | Task 5-6 | ✓ Wired |

## Service Integration Checklist

- [x] IBattleSimulationService - used by BackgroundBattleSimulator
- [x] IZoneEvaluationService - used internally by AI strategies
- [x] IResourceAllocationService - used internally by AI strategies
- [x] IAggressionResponseService - records player attacks, decays over time
- [x] IAIDifficultyService - wired, could add UI settings
- [x] IEventFeedService - rendered on screen
- [x] IEventAlertService - triggered on AI battles
```

**Step 2: Commit**

```bash
git add docs/UNUSED_SERVICES.md
git commit -m "docs: document unused services status"
```

---

## Task 9: Build, Deploy, and Test

**Step 1: Full build**

```bash
dotnet build src/FactionWars/FactionWars.csproj -c Release
```

**Step 2: Run tests**

```bash
dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj
```

**Step 3: Deploy**

```bash
cp src/FactionWars/bin/Release/net48/FactionWars.dll "E:/SteamLibrary/steamapps/common/Grand Theft Auto V/scripts/"
```

**Step 4: Manual test checklist**

- [ ] Start game, wait for AI decision cycle (5 seconds)
- [ ] Event feed appears in bottom-left corner
- [ ] AI factions attack each other (check event feed)
- [ ] Zone captures appear in feed with faction colors
- [ ] Enter enemy zone - combat started event appears
- [ ] Win combat - zone captured event appears
- [ ] AI retaliates after player attacks (threat tracking)

**Step 5: Final commit**

```bash
git add -A
git commit -m "feat: complete MVP service wiring for AI and event feed"
```

---

## Summary of Changes

| File | Change |
|------|--------|
| `ServiceContainerFactory.cs` | Register BackgroundBattleSimulator |
| `GameLoopController.cs` | Wire battle simulator, event feed renderer, threat decay |
| `BackgroundBattleSimulator.cs` | Add event alert/feed dependencies |
| `CombatManager.cs` | Add aggression recording |
| `docs/UNUSED_SERVICES.md` | Document service status |
