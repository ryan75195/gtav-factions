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
| `BackgroundBattleSimulator` | Task 1-3 | Wired |
| `IEventFeedService` + Renderer | Task 4, 7 | Wired |
| `IEventAlertService` | Task 3 | Wired |
| `IAggressionResponseService` | Task 5-6 | Wired |

## Service Integration Checklist

- [x] IBattleSimulationService - used by BackgroundBattleSimulator
- [x] IZoneEvaluationService - used internally by AI strategies
- [x] IResourceAllocationService - used internally by AI strategies
- [x] IAggressionResponseService - records player attacks, decays over time
- [x] IAIDifficultyService - wired, could add UI settings
- [x] IEventFeedService - rendered on screen
- [x] IEventAlertService - triggered on AI battles
