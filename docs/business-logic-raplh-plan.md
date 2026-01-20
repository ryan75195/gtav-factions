# RALPH_PLAN.md - GTA V Faction Wars Territory Control Mod

## Project Overview

A territory control modification for Grand Theft Auto V that introduces strategic faction warfare. The map is divided into controllable regions governed by three factions led by the game's protagonists: Michael, Trevor, and Franklin.

**Target Platform:** GTA V on PC
**Tech Stack:** Script Hook V .NET, C# (.NET Framework), NativeUI, JSON persistence

## Architecture Decisions

### Project Structure
```
gtav-factions/
├── src/
│   └── FactionWars/
│       ├── Core/
│       │   ├── Interfaces/         # Abstractions for DI
│       │   ├── Models/             # Domain models
│       │   ├── Services/           # Business logic
│       │   └── Utils/              # Utilities
│       ├── Territory/              # Zone management
│       ├── Factions/               # Faction state and logic
│       ├── Economy/                # Resource system
│       ├── Combat/                 # Combat and ped management
│       ├── AI/                     # AI decision making
│       ├── Persistence/            # Save/Load system
│       └── UI/                     # NativeUI integration
├── tests/
│   └── FactionWars.Tests/
│       ├── Unit/
│       │   ├── Territory/
│       │   ├── Factions/
│       │   ├── Economy/
│       │   ├── Combat/
│       │   ├── AI/
│       │   └── Persistence/
│       └── Integration/
├── FactionWars.sln
└── RALPH_PLAN.md
```

### Design Principles
- **SOLID Principles:** Strict adherence throughout
- **TDD:** All code written test-first (red-green-refactor)
- **Dependency Injection:** All dependencies injected via interfaces
- **Repository Pattern:** For persistence layer
- **Strategy Pattern:** For AI behaviors and faction characteristics

### Key Interfaces (to be created)
- `IZoneRepository` - Zone data access
- `IFactionRepository` - Faction state management
- `IResourceManager` - Resource generation and management
- `IPedPool` - Combat ped pooling
- `IAIStrategy` - Faction-specific AI strategies
- `IGameBridge` - Abstraction over GTA V native calls (for testing)

---

## Milestone 1: Foundation (Week 1-2)

### Phase 1A: Project Setup
- [x] Create solution and project structure with proper folders
- [x] Setup test project with xUnit and Moq
- [x] Create IGameBridge interface for mocking GTA V native calls
- [x] Create mock implementations for testing

### Phase 1B: Zone System (TDD)
- [x] Write tests for Zone model (properties, validation)
- [x] Implement Zone model class
- [x] Write tests for ZoneBoundary (coordinate-based containment)
- [x] Implement ZoneBoundary with vector math
- [x] Write tests for ZoneTrait enum and effects
- [x] Implement ZoneTrait system
- [x] Write tests for IZoneRepository interface behavior
- [x] Implement ZoneRepository
- [x] Write tests for ZoneService (zone querying, updates)
- [x] Implement ZoneService
- [x] Write tests for zone connectivity/adjacency
- [x] Implement zone adjacency calculation

### Phase 1C: Faction System (TDD)
- [x] Write tests for Faction model (properties, validation)
- [x] Implement Faction model class
- [x] Write tests for FactionType enum (Michael/Trevor/Franklin characteristics)
- [x] Implement FactionType with bonuses
- [x] Write tests for FactionState (zones, resources, army)
- [x] Implement FactionState class
- [x] Write tests for IFactionRepository
- [x] Implement FactionRepository
- [x] Write tests for FactionService
- [x] Implement FactionService
- [x] Write tests for faction relationship tracking
- [x] Implement faction relationships

### Phase 1D: Persistence Layer (TDD)
- [x] Write tests for GameState serialization
- [x] Implement GameState class
- [x] Write tests for JSON save/load cycle
- [x] Implement JsonPersistenceService
- [x] Write tests for save file validation
- [x] Implement save file validation
- [x] Write tests for multiple save slots
- [x] Implement save slot management
- [x] Write tests for auto-save functionality
- [x] Implement auto-save service

---

## Milestone 2: Combat Core (Week 3-4)

### Phase 2A: Ped Pool System (TDD)
- [x] Write tests for PedHandle wrapper
- [x] Implement PedHandle class
- [x] Write tests for IPedPool interface
- [x] Implement PedPool with configurable limits
- [x] Write tests for ped spawning with relationship groups
- [x] Implement ped spawning service
- [x] Write tests for ped despawn management
- [x] Implement despawn logic
- [x] Write tests for ped recycling
- [x] Implement ped recycling

### Phase 2B: Combat System (TDD)
- [x] Write tests for CombatEncounter model
- [x] Implement CombatEncounter class
- [x] Write tests for zone control percentage calculation
- [x] Implement control percentage logic
- [x] Write tests for zone takeover threshold
- [x] Implement takeover detection
- [x] Write tests for reinforcement mechanics
- [x] Implement reinforcement spawning
- [x] Write tests for combat outcome affecting zone state
- [x] Implement combat result handler
- [x] Write integration tests for combat-to-zone-state pipeline
- [x] Verify combat integration

---

## Milestone 3: Economy & AI (Week 5-7)

### Phase 3A: Resource System (TDD)
- [x] Write tests for ResourceType enum
- [x] Implement ResourceType (cash, recruitment, weapons)
- [x] Write tests for resource generation calculation
- [x] Implement resource generation logic
- [x] Write tests for zone trait effects on resources
- [x] Implement trait-based resource modifiers
- [x] Write tests for resource tick timer
- [x] Implement ResourceTickService
- [x] Write tests for resource caps and storage
- [x] Implement resource cap logic
- [x] Write tests for supply line connectivity
- [x] Implement supply line calculation
- [x] Write integration tests for economy-to-recruitment
- [x] Verify economy integration

### Phase 3B: AI System (TDD)
- [x] Write tests for IAIStrategy interface
- [x] Implement base AIStrategy class
- [x] Write tests for MichaelAIStrategy (calculated, high-value)
- [x] Implement MichaelAIStrategy
- [x] Write tests for TrevorAIStrategy (aggressive, combat bonuses)
- [x] Implement TrevorAIStrategy
- [x] Write tests for FranklinAIStrategy (opportunistic, mobile)
- [x] Implement FranklinAIStrategy
- [x] Write tests for zone evaluation scoring
- [x] Implement zone scoring algorithm
- [x] Write tests for attack/defense resource allocation
- [x] Implement resource allocation logic
- [x] Write tests for AI response to player aggression
- [x] Implement reactive AI behavior
- [x] Write tests for configurable AI difficulty
- [x] Implement difficulty scaling

---

## Milestone 4: Polish & UI (Week 8-10)

### Phase 4A: Map Integration
- [x] Write tests for map blip management
- [x] Implement MapBlipService
- [x] Write tests for zone visual boundaries
- [x] Implement zone boundary rendering
- [x] Write tests for faction color assignments
- [x] Implement faction color system

### Phase 4B: NativeUI Integration
- [x] Implement main faction menu
- [x] Implement zone detail view
- [x] Implement resource overview panel
- [x] Implement attack/defend order menus
- [x] Implement settings menu

### Phase 4C: Phone Integration
- [x] Implement phone command registration
- [x] Implement faction status commands
- [x] Implement quick action commands

### Phase 4D: HUD Elements
- [x] Implement combat HUD (control %, reinforcement timer)
- [x] Implement notification system
- [x] Implement event alerts (zone captured, attack incoming)

### Phase 4E: Final Integration
- [x] Write end-to-end scenario tests
- [x] Full save/load integration tests
- [x] Balance tuning
- [x] Performance optimization
- [x] Documentation

---

## Milestone 5: Enhanced Features (Week 11+)

### Phase 5A: Tension System (TDD)
- [x] Write tests for tension tracking between factions
- [x] Implement tension model
- [x] Write tests for tension escalation triggers
- [x] Implement tension escalation
- [x] Write tests for warfare state transitions
- [x] Implement warfare states
- [x] Write tests for covert operations
- [x] Implement sabotage/assassination/bribery
- [x] Write tests for diplomatic actions
- [x] Implement diplomacy mechanics

### Phase 5B: Lieutenant System (TDD)
- [x] Write tests for Lieutenant model
- [x] Implement Lieutenant class
- [x] Write tests for procedural trait generation
- [x] Implement trait generator
- [x] Write tests for lieutenant zone effects
- [x] Implement zone commander bonuses
- [x] Write tests for defection mechanics
- [x] Implement defection logic
- [x] Write tests for flip missions
- [x] Implement lieutenant flip system

### Phase 5C: Population Loyalty (TDD)
- [x] Write tests for loyalty tracking
- [x] Implement zone loyalty model
- [x] Write tests for loyalty modifiers
- [x] Implement loyalty change mechanics
- [x] Write tests for insurgency risk
- [x] Implement insurgency system
- [x] Write tests for integration difficulty
- [x] Implement captured zone integration

### Phase 5D: Escalation Tiers (TDD)
- [x] Write tests for escalation level tracking
- [x] Implement escalation tiers
- [x] Write tests for weapon unlocks
- [x] Implement tiered weapons
- [x] Write tests for vehicle unlocks
- [x] Implement tiered vehicles

---

## Current Progress

**Current Task:** ALL TASKS COMPLETE
**Status:** Phase 5D complete - Tiered vehicles implementation finished.

Implemented files:
- VehicleCategory.cs - 10 vehicle categories (Compact, Sedan, SUV, Coupe, Muscle, Sports, Motorcycle, Van, Armored, Military)
- VehicleUnlock.cs - Model class with VehicleModel, DisplayName, Category, RequiredTier, Description, MaxSpeed properties
- IVehicleUnlockRepository.cs - Repository interface with Add, Remove, GetByModel, GetByTier, GetByCategory, GetAll, GetUnlockedAtTier, GetUnlockedAtTierByCategory, Exists, Count, Clear
- InMemoryVehicleUnlockRepository.cs - In-memory implementation of the repository
- IVehicleUnlockService.cs - Service interface for vehicle unlock operations
- VehicleUnlockService.cs - Service implementation with faction tier-based vehicle availability

All 126 vehicle unlock tests pass (108 new + 18 related escalation tests).

**Next Task:** RALPH_COMPLETE - All tasks in the plan are now checked

---

## Notes

- All GTA V native calls must go through IGameBridge for testability
- Ped spawning limited to ~30 active combat peds per engine limits
- Resource tick default: 5 minutes real-time
- Minimum 20 zones covering Los Santos metropolitan area
- 80% code coverage target for non-UI code
