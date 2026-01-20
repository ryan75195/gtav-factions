# RALPH_PLAN.md - FactionWars ScriptHookVDotNet Integration

## Project Overview

Integrate the existing FactionWars domain layer (64 interfaces, comprehensive tests) into a fully playable ScriptHookVDotNet mod. The domain layer is complete but "orphaned" - services exist but are not wired to the actual game.

**Goal:** Create a playable GTA V territory control mod where three factions battle for Los Santos and Blaine County.

## Architecture Decisions

1. **Service Container Pattern** - Simple DI container for wiring services
2. **Manager Pattern** - Manager classes coordinate domain services with game loop
3. **Bridge Pattern** - `GameBridge` implements `IGameBridge` with ScriptHookV natives
4. **Event-Driven Updates** - Use GTA V's tick system to drive periodic updates

## File Structure Plan

```
src/FactionWars/
├── ScriptHookV/
│   ├── FactionWarsScript.cs        # Main entry point (GTA.Script)
│   ├── GameBridge.cs               # IGameBridge implementation
│   ├── ServiceContainer.cs         # DI container
│   ├── ServiceContainerFactory.cs  # Service wiring
│   ├── TimeProvider.cs             # ITimeProvider implementation
│   ├── Managers/
│   │   ├── CombatManager.cs        # Combat encounters
│   │   ├── AIManager.cs            # AI decisions & simulation
│   │   ├── EconomyManager.cs       # Resource ticks
│   │   ├── TerritoryManager.cs     # Zone detection & management
│   │   ├── FollowerManager.cs      # Bodyguard system
│   │   └── VictoryManager.cs       # Victory detection
│   ├── UI/
│   │   ├── NativeUIMenuProvider.cs # Menu system
│   │   ├── CombatHudRenderer.cs    # Combat HUD
│   │   ├── TerritoryIndicator.cs   # Zone status display
│   │   ├── EventFeedRenderer.cs    # World events feed
│   │   └── NotificationRenderer.cs # Notifications
│   ├── Data/
│   │   └── ZoneDataLoader.cs       # Load zone definitions
│   └── Persistence/
│       └── GameStateManager.cs     # Save/load coordination
├── Core/
│   ├── Interfaces/
│   │   ├── IVictoryConditionService.cs
│   │   ├── IDefenderTierService.cs
│   │   ├── IBattleSimulationService.cs
│   │   ├── IEventFeedService.cs
│   │   ├── ITerritoryIndicatorService.cs
│   │   └── IFollowerService.cs
│   └── Services/
│       ├── VictoryConditionService.cs
│       ├── DefenderTierService.cs
│       ├── BattleSimulationService.cs
│       ├── EventFeedService.cs
│       ├── TerritoryIndicatorService.cs
│       └── FollowerService.cs
```

---

## Task Breakdown

### Phase 1: Foundation (Core Infrastructure)

- [x] **1.1** Create `ITimeProvider` implementation using GTA V game time
- [x] **1.2** Create `GameBridge` class implementing `IGameBridge` with ScriptHookV natives
- [x] **1.3** Create `ServiceContainer` for dependency injection
- [x] **1.4** Create `ServiceContainerFactory` to wire all services together
- [x] **1.5** Create `FactionWarsScript` main entry point extending `GTA.Script`
- [x] **1.6** Verify mod loads in-game without errors

### Phase 2: Territory & Factions

- [x] **2.1** Create `ZoneDataLoader` to load 31 zones from configuration
- [x] **2.2** Wire `IZoneRepository` and `IZoneService` in container
- [x] **2.3** Wire `IFactionRepository` and `IFactionService` in container
- [x] **2.4** Create `TerritoryManager` for zone detection based on player position
- [x] **2.5** Wire `IFactionRelationshipService` in container
- [x] **2.6** Create `MapBlipManager` to display zone blips on map
- [x] **2.7** Implement player faction detection based on character model (Michael=Blue, Trevor=Orange, Franklin=Green)
- [x] **2.8** Implement character switching detection - changing character changes player faction
- [x] **2.9** Initialize starting conditions per faction (Michael: 8 zones, 50 troops, $10k; Trevor: 10 zones, 60 troops, $8k; Franklin: 5 zones, 30 troops, $5k)
- [x] **2.10** Verify zones appear on map with correct faction colors

### Phase 3: Defender Tiers & Troop Reserve

- [x] **3.1** Create `IDefenderTierService` interface
- [x] **3.2** Implement `DefenderTierService` with Basic/Medium/Heavy tier configs
- [x] **3.3** Extend faction state model to track reserve pool by tier
- [x] **3.4** Implement troop allocation from reserve to zones
- [x] **3.5** Implement troop withdrawal from zones back to reserve
- [x] **3.6** Write tests for defender tier service
- [x] **3.7** Write tests for reserve pool management

### Phase 4: Combat System

- [x] **4.1** Create `CombatManager` to handle combat encounters
- [x] **4.2** Wire `IPedSpawningService` and configure natural spawning (behind player)
- [x] **4.3** Wire `IPedDespawnService` and `IPedRecyclingService`
- [x] **4.4** Implement defender scaling based on allocated troops
- [x] **4.5** Implement wave-based spawning (Heavy → Medium → Basic)
- [x] **4.6** Wire `IControlPercentageCalculator` and `ITakeoverDetector`
- [x] **4.7** Wire `ICombatResultHandler` for zone capture
- [x] **4.8** Implement troop deduction when defenders die
- [x] **4.9** Implement player death handling - death in contested zone = retreat (combat ends, zone ownership unchanged)
- [x] **4.10** Write integration tests for combat flow
- [x] **4.11** Verify entering enemy zone triggers combat and capture works

### Phase 5: Economy System

- [x] **5.1** Create `EconomyManager` to coordinate resource ticks
- [x] **5.2** Wire `IResourceTickService` to game loop
- [x] **5.3** Implement real GTA V cash integration (add income to player money)
- [x] **5.4** Wire `ISupplyLineService` for connected zone bonuses
- [x] **5.5** Implement troop purchasing (deduct from player's real cash)
- [x] **5.6** Write tests for economy flow
- [x] **5.7** Verify income adds to player's GTA V money

### Phase 6: AI System

- [x] **6.1** Create `AIManager` to coordinate AI faction decisions
- [x] **6.2** Wire all three `IAIStrategy` implementations (Michael, Trevor, Franklin)
- [x] **6.3** Wire `IZoneEvaluationService` for zone scoring
- [x] **6.4** Wire `IResourceAllocationService` for troop distribution
- [x] **6.5** Create `IBattleSimulationService` interface
- [x] **6.6** Implement `BattleSimulationService` for AI vs AI combat
- [x] **6.7** Implement background simulation (AI battles when player absent)
- [x] **6.8** Wire `IAggressionResponseService` for response decisions
- [x] **6.9** Write tests for AI decision making
- [x] **6.10** Write tests for battle simulation
- [x] **6.11** Verify AI factions attack, defend, and battle each other

### Phase 7: Follower/Bodyguard System

- [x] **7.1** Create `IFollowerService` interface
- [x] **7.2** Implement `FollowerService` for follower management
- [x] **7.3** Create `FollowerManager` for in-game follower coordination
- [x] **7.4** Implement follower recruitment (spawn and follow player, costs real GTA V cash)
- [x] **7.5** Implement follower vehicle behavior (enter/exit with player)
- [x] **7.6** Implement follower combat behavior (shoot, take cover, tier-appropriate weapons)
- [x] **7.7** Implement follower dismissal and death handling (permanent loss on death)
- [x] **7.8** Implement follower dismissal on character switch (auto-dismiss all followers)
- [x] **7.9** Implement maximum follower limit (suggested: 4-6)
- [x] **7.10** Write tests for follower service
- [x] **7.11** Verify followers follow, fight, and enter vehicles

### Phase 8: UI System

- [x] **8.1** Create `NativeUIMenuProvider` implementing `IMenuProvider`
- [x] **8.2** Implement main menu structure (F7 to open)
- [x] **8.3** Implement Overview submenu (faction stats, victory progress)
- [x] **8.4** Implement Zone Management submenu (view zones, allocate troops by tier, withdraw troops)
- [x] **8.5** Implement Army submenu (purchase troops to reserve, view reserves, recruit followers)
- [x] **8.6** Implement Resources submenu (income breakdown)
- [x] **8.7** Implement Settings submenu (save, load, debug options)
- [x] **8.8** Create `TerritoryIndicator` for zone status HUD (top of screen: zone name, owner, control %)
- [x] **8.9** Create `CombatHudRenderer` for combat progress display (defender count, capture progress bar)
- [x] **8.10** Create `IEventFeedService` interface and implementation
- [x] **8.11** Create `EventFeedRenderer` for world events (bottom left, color-coded, last 4-6 events)
- [x] **8.12** Wire `INotificationService` and `INotificationRenderer`
- [x] **8.13** Implement zone under attack notification with waypoint option (no fast travel)
- [x] **8.14** Wire `IMapBlipService` for zone blip updates
- [x] **8.15** Verify F7 opens menu and all submenus work

### Phase 9: Persistence & Victory

- [x] **9.1** Create `GameStateManager` for save/load coordination
- [x] **9.2** Wire `IPersistenceService` for JSON save/load
- [x] **9.3** Wire `ISaveSlotManager` for multiple save slots
- [x] **9.4** Wire `IAutoSaveService` to game loop
- [x] **9.5** Create `IVictoryConditionService` interface
- [x] **9.6** Implement `VictoryConditionService` (100% control detection)
- [x] **9.7** Create `VictoryManager` for victory screen display
- [x] **9.8** Write tests for save/load integrity
- [x] **9.9** Write tests for victory condition detection
- [x] **9.10** Verify save/load works and victory triggers at 100%

### Phase 10: Polish & Integration Testing

- [x] **10.1** Run full test suite and fix any failures
- [x] **10.2** Verify build succeeds without errors
- [x] **10.3** End-to-end test: full combat flow (enter zone → fight → capture)
- [x] **10.4** End-to-end test: player death in combat (death → retreat → zone unchanged)
- [x] **10.5** End-to-end test: economy flow (income → purchase troops → allocate from reserve)
- [x] **10.6** End-to-end test: AI simulation (AI attacks, defends, battles AI)
- [x] **10.7** End-to-end test: follower system (recruit → follow → fight → die)
- [x] **10.8** End-to-end test: character switching (switch character → faction changes, followers dismissed)
- [x] **10.9** End-to-end test: persistence (save → load → state preserved)
- [x] **10.10** End-to-end test: victory condition (capture all → victory screen)
- [x] **10.11** End-to-end test: zone attack notification (AI attacks player zone → notification → waypoint)
- [x] **10.12** Balance tuning: combat difficulty, economy pacing, AI aggression
- [x] **10.13** Create HANDOVER.md documentation per RFP Section 11 requirements

---

## Current Progress

**Status:** COMPLETE - All tasks finished

**Last Completed Task:** 10.13 - Create HANDOVER.md documentation per RFP Section 11 requirements

**All 10 phases complete. Project ready for delivery.**
