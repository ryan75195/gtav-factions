# RFP: FactionWars ScriptHookVDotNet Integration

## Executive Summary

**FactionWars** is a GTA V territory control mod where three factions (Michael's Organization, Trevor Philips Enterprises, and Franklin's Crew) battle for control of Los Santos and Blaine County. The player joins their character's faction and must strategically capture zones, manage resources, recruit troops, and outmaneuver enemy AI to dominate the map.

This RFP defines the work required to integrate the existing domain layer (64 interfaces, comprehensive test coverage) into a fully playable ScriptHookVDotNet mod.

---

## Table of Contents

1. [Game Vision](#1-game-vision)
2. [Current State](#2-current-state)
3. [Scope of Work](#3-scope-of-work)
4. [Technical Requirements](#4-technical-requirements)
5. [Feature Specifications](#5-feature-specifications)
6. [Service Integration Matrix](#6-service-integration-matrix)
7. [Zone Definitions](#7-zone-definitions)
8. [Testing & Tuning](#8-testing--tuning)
9. [Implementation Phases](#9-implementation-phases)
10. [Acceptance Criteria](#10-acceptance-criteria)
11. [Handover Document](#11-handover-document)

---

## 1. Game Vision

### Core Gameplay Loop

1. **Territory Control** - Los Santos and Blaine County are divided into 30-35 zones, each providing income and strategic value
2. **Combat** - Enter enemy territory to trigger battles; kill defenders to capture zones
3. **Economy** - Owned zones generate cash (added to player's real GTA V money); spend cash to recruit troops and upgrade defenders
4. **Troop Management** - Allocate troops to zones via menu; troops determine defense strength
5. **AI Opponents** - Enemy factions autonomously attack, defend, and expand their territory
6. **Background Simulation** - AI factions fight each other even when player is not present
7. **Victory** - Game complete when one faction achieves 100% territorial control

### What Makes It Fun

- **Dynamic World** - Zones change hands, AI factions fight each other, the map evolves
- **Strategic Depth** - Balance offense vs defense, manage resources, time your attacks
- **Meaningful Choices** - Invest in cheap basic troops or expensive elite defenders
- **Emergent Gameplay** - AI factions make their own decisions, creating unpredictable situations

### Three Factions

| Faction | Leader | Color | AI Personality |
|---------|--------|-------|----------------|
| Michael's Organization | Michael De Santa | Blue | Defensive, calculated, protects assets |
| Trevor Philips Enterprises | Trevor Philips | Orange | Aggressive, chaotic, overextends |
| Franklin's Crew | Franklin Clinton | Green | Opportunistic, exploits weaknesses |

### Key Rules

- **Single-Player Only** - This mod is designed for single-player GTA V
- **Faction Assignment** - Player's faction is determined by current character (Michael → Blue, Trevor → Orange, Franklin → Green)
- **Character Switching** - Switching characters switches faction; each faction maintains its own state (troops, zones, etc.)
- **Player Death** - Dying while in a contested zone counts as a retreat; zone ownership unchanged, combat ends
- **Zone Under Attack** - When an AI faction attacks your zone, you receive a notification and can set a waypoint (no fast travel)

### Starting Conditions (New Game)

| Faction | Starting Zones | Starting Troops | Starting Cash |
|---------|---------------|-----------------|---------------|
| Michael | 8 zones (Vinewood, Downtown, etc.) | 50 | $10,000 |
| Trevor | 10 zones (Sandy Shores, Port, etc.) | 60 | $8,000 |
| Franklin | 5 zones (Grove Street, Davis, etc.) | 30 | $5,000 |

- Each faction begins with troops distributed across their starting zones
- Neutral zones have no defenders initially (easy early captures)
- Player's starting cash is added to their GTA V money on first load

---

## 2. Current State

### Domain Layer (Complete)

The domain layer has been fully implemented with comprehensive test coverage:

```
Source Files:     277
Interfaces:       64
Test Files:       122
Test Coverage:    High (all services unit tested)
```

### Module Breakdown

| Module | Interfaces | Services | Models | Status |
|--------|------------|----------|--------|--------|
| **AI** | 5 | 4 | 8 | Complete |
| **Balance** | 1 | 1 | 4 | Complete |
| **Combat** | 8 | 6 | 12 | Complete |
| **Core** | 7 | 5 | 3 | Complete |
| **Economy** | 4 | 4 | 5 | Complete |
| **Escalation** | 6 | 4 | 8 | Complete (skip for MVP) |
| **Factions** | 4 | 3 | 6 | Complete |
| **Lieutenants** | 5 | 4 | 7 | Complete (skip for MVP) |
| **Loyalty** | 5 | 4 | 8 | Complete (skip for MVP) |
| **Performance** | 4 | 4 | 2 | Complete |
| **Territory** | 2 | 2 | 5 | Complete |
| **UI** | 14 | 10 | 8 | Complete |

### What's Missing

The domain layer exists but is **not wired to ScriptHookVDotNet**. There is no:
- Main script entry point (`GTA.Script`)
- Game bridge implementation (`IGameBridge`)
- Service container / dependency injection
- Game loop integration
- NativeUI menu implementation
- Ped spawning / management
- Map blip management
- Save/load integration

### Critical Problem: Orphaned Services

**The existing services are registered but never called.** They exist as standalone code with no connection to the actual game. This RFP requires that all services be **actively wired into the game loop** - not just instantiated, but actually invoked during gameplay.

For example:
- `IResourceTickService` must be called every tick interval to generate income
- `IAIStrategy` must be invoked periodically to make AI decisions
- `ICombatResultHandler` must be triggered when combat events occur
- `IZoneService` must respond to player position changes

**The deliverable is a playable mod, not a collection of unused services.**

---

## 3. Scope of Work

### In Scope (MVP)

| Category | Items |
|----------|-------|
| **Core Integration** | ScriptHookV entry point, game bridge, service container, logger |
| **Combat System** | Ped spawning, zone combat, defender scaling, troop deduction |
| **Economy System** | Resource ticks tied to player's real GTA V cash, troop purchasing, defender tier costs |
| **AI System** | Decision making, troop allocation, background simulation |
| **Territory System** | 30-35 zones, capture mechanics, ownership tracking |
| **UI System** | NativeUI menus, combat HUD, map blips, notifications |
| **Persistence** | Save/load game state, auto-save |
| **Victory Condition** | 100% control detection, victory screen |
| **Defender Tiers** | Basic/Medium/Heavy with different costs and stats |
| **Follower System** | Recruit bodyguards to accompany player on raids |

### Out of Scope (Future Versions)

| Feature | Reason |
|---------|--------|
| Escalation System | Weapon/vehicle unlocks - adds complexity |
| Lieutenant System | Named NPCs with traits - adds complexity |
| Loyalty System | Zone integration/insurgency - adds complexity |
| Diplomacy | Truces/alliances between factions |
| Military Vehicles | Helicopters/planes from special locations |

---

## 4. Technical Requirements

### Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| ScriptHookVDotNet3 | 3.6.0+ | GTA V script framework |
| NativeUI | 1.9.1+ | In-game menu system |
| Newtonsoft.Json | 13.0.1+ | Save/load serialization |

### Recommended GTA V Modding Libraries

The implementation team is encouraged to leverage existing, well-tested GTA V modding libraries rather than building everything from scratch:

| Library | Purpose | Notes |
|---------|---------|-------|
| **NativeUI** | Menus, notifications | Widely used, stable, good documentation |
| **LemonUI** | Alternative menu system | Modern, actively maintained |
| **ScriptHookVDotNet Extensions** | Helper utilities | Community extensions for common tasks |
| **GTA V Native Database** | Native function reference | For direct game integration |

**General Guidance:**
- Prefer established libraries over custom implementations
- Check GTA5-Mods.com and GitHub for existing solutions
- Use native GTA V UI elements where possible for consistency
- Leverage community knowledge from GTAForums and modding Discords

> **Flexibility Note:** The libraries above are suggestions. The implementation team should use whatever tools best accomplish the goals - including libraries not listed here.

### Project Structure

**Existing Domain Layer** (under `src/FactionWars/`):
- AI, Balance, Combat, Core, Economy, Factions, Territory, UI, Performance modules
- All interfaces and services already implemented and tested

**New Integration Layer** (suggested: `src/FactionWars/ScriptHookV/`):
- Main script entry point
- Game bridge implementation
- Service container / dependency injection
- Manager classes (Combat, AI, Economy, etc.)
- UI renderers (HUD, menus, notifications)
- Zone data loader
- Game state persistence

### Test Structure

**Existing Unit Tests** - Comprehensive coverage of domain layer

**New Integration Tests** - Cover the ScriptHookV integration:
- Service container wiring
- Combat flow (zone entry → fight → capture)
- AI simulation cycles
- Economy flow
- Victory conditions
- Save/load integrity

> **Flexibility Note:** The file and folder structure above is a suggestion. The implementation team has full flexibility to organize code as they see fit, as long as the functionality is delivered.

---

## 5. Feature Specifications

### 5.1 Victory Condition

**Requirement:** Game ends when one faction controls 100% of all zones.

**Behavior:**
- Check victory condition after every zone ownership change
- Display victory notification with winning faction name
- Option to continue playing or start new game
- Victory screen shows final stats (zones captured, troops lost, time played)

> **Flexibility Note:** The interface above is illustrative. The implementation team may design the victory system however they prefer, as long as it detects and celebrates faction victory.

### 5.2 Troop Reserve Pool

**Requirement:** Purchased troops go into a faction reserve pool before being deployed to zones.

**Purchase Flow:**
1. Player opens Army menu
2. Player buys troops (Basic/Medium/Heavy) using real GTA V cash
3. Troops are added to the **reserve pool** (not directly to a zone)
4. Player then allocates troops from reserve to specific zones

**Reserve Pool Tracking:**
- Each faction has a reserve pool with counts per tier
- Example: Reserve has 20 Basic, 10 Medium, 5 Heavy available to deploy
- Troops in reserve cost nothing to maintain (no upkeep)
- AI factions also have reserve pools they manage

**Why Reserve Pool:**
- Strategic choice: where to deploy limited troops
- Can't instantly reinforce a zone under attack (must have reserves)
- Forces planning ahead

### 5.3 Defender Tiers

**Requirement:** Players can assign different quality defenders to zones at different costs.

| Tier | Cost | Health | Armor | Weapon | Accuracy |
|------|------|--------|-------|--------|----------|
| Basic | $200 | 100 | None | Pistol | 0.3 |
| Medium | $500 | 150 | Light | SMG | 0.5 |
| Heavy | $1000 | 200 | Heavy | Carbine Rifle | 0.7 |

**Zone Allocation:**
- Allocate troops from reserve pool to specific zones
- Each zone tracks how many of each tier are allocated
- Example: Grove Street might have 10 Basic, 5 Medium, 2 Heavy defenders
- Can withdraw troops from zones back to reserve

> **Flexibility Note:** The exact costs, health values, weapons, and tier count are tunable. The implementation team may adjust these values or add/remove tiers based on gameplay testing.

### 5.4 Combat Defender Scaling

**Requirement:** When entering enemy zone, spawn defenders based on allocated troops.

**Spawn Logic:**
- Spawn count scales with total allocated troops (e.g., 30 troops → 6 defenders)
- Cap maximum spawned defenders to avoid overwhelming combat (suggested: 8 max)

**Spawn Distribution:**
- Spawn in waves, not all at once
- Heavy tier spawns first (best defenders)
- Then Medium, then Basic
- Deduct from zone allocation when defenders die

**Example:**
- Zone has: 5 Heavy, 10 Medium, 15 Basic (30 total)
- Spawn count: Min(8, 30/5) = 6
- Wave 1: 2 Heavy
- Wave 2: 2 Medium
- Wave 3: 2 Basic

> **Flexibility Note:** The spawn formula, wave timing, and spawn order are suggestions. The implementation team should tune these for the best gameplay feel.

### 5.5 AI Troop Allocation

**Requirement:** AI factions intelligently distribute troops across their zones.

**Priority Factors:**
- Base strategic value of the zone
- Proximity to enemy territory (frontline zones score higher)
- Zones under attack get priority
- Recently captured zones need reinforcement
- Safe interior zones can have fewer troops

**Allocation Rules:**
- High priority zones (score > 7): 50-70% of available troops
- Medium priority zones (score 4-7): 20-40% of available troops
- Low priority zones (score < 4): 10-20% of available troops
- Reserve 20% of troops for attacks

**AI Tick Cycle (every 30 seconds):**
1. Recalculate zone priorities
2. Reallocate troops based on priorities
3. Decide: Attack, Defend, or Hold
4. Execute decision

> **Flexibility Note:** The priority formula and allocation percentages are starting points. The implementation team should tune AI behavior until each faction feels distinct and intelligent.

### 5.6 Background AI Simulation

**Requirement:** AI factions battle each other even when player is not present.

**Simulation Approach:**
- Calculate attacker strength (troop count × tier modifiers)
- Calculate defender strength (troop count × tier modifiers × terrain bonus)
- Determine win probability based on relative strengths
- Roll for outcome, calculate casualties for both sides

**Modifiers:**
- Tier bonuses: Basic=1.0, Medium=1.5, Heavy=2.0
- Terrain bonus: Fortified zones get +0.3 defense
- Supply line bonus: Connected zones get +0.2

> **Flexibility Note:** The battle simulation formula should create interesting, somewhat unpredictable outcomes. The implementation team may use different formulas or approaches.

### 5.7 Natural Ped Spawning

**Requirement:** Troops spawn believably, not in front of player.

**Spawn Rules:**
- Spawn 120-240 degrees behind player (outside FOV)
- Distance: 40-80m for initial defenders
- Stagger spawns: 2-3 second delays between waves
- Use ground detection for correct Z height
- Never spawn in player's line of sight

> **Flexibility Note:** Spawn angles, distances, and timing should be tuned for immersion. The implementation team should test different approaches to find what feels best.

### 5.8 HUD & Status Display

**Requirement:** Player should always know the current game state at a glance.

#### Territory Indicator (Top of Screen)

**Elements:**
- Zone name
- Owning faction (colored by faction)
- Control percentage (during combat)
- "NEUTRAL" or "CONTESTED" status when applicable

#### Combat HUD (During Encounters)

**Elements:**
- Zone being contested
- Defender count (remaining / total spawned)
- Capture progress bar
- Outcome indicator (who will own if completed)

#### Event Feed (Bottom Left)

**Event Types:**
- Zone captures (by any faction)
- Skirmishes/battles in progress
- Reinforcement movements
- Player actions
- AI faction decisions (attacks launched, retreats)
- Resource milestones (e.g., "Trevor's income: $5,000/tick")

**Feed Behavior:**
- Show last 4-6 events
- New events slide in, old events fade out
- Color-code by faction
- Timestamp each event
- Critical events (player's zones attacked) highlighted

#### Minimap Enhancements
- Zone boundaries visible on minimap
- Faction colors on zone areas
- Flashing indicators for zones under attack
- Icons for ongoing battles

> **Flexibility Note:** The HUD layouts above are conceptual. The implementation team should design UI that's readable, unobtrusive, and fits GTA V's aesthetic. Consider using existing GTA V modding libraries for HUD rendering (e.g., NativeUI, LemonUI, RAGEPluginHook utilities) rather than building from scratch.

### 5.9 Economy & Real Cash Integration

**Requirement:** The faction economy is tied directly to the player's actual GTA V in-game money.

**Income (Money In):**
- When resource ticks occur, income is added to player's real GTA V cash
- Example: Owning 5 zones generating $500/tick → player's bank increases by $500
- Player sees their actual money go up, making territory feel valuable

**Expenses (Money Out):**
- Purchasing troops deducts from player's real GTA V cash
- Allocating defenders to zones costs real money
- All tier costs (Basic $200, Medium $500, Heavy $1000) use real cash

**Benefits:**
- Integrates seamlessly with GTA V's existing economy
- Player can use money earned from other activities (missions, heists) to fund their faction
- Creates real stakes - losing territory means losing income
- No separate "faction currency" to track

**Implementation Notes:**
- Use GTA V native functions to get/set player money
- Handle edge cases: what if player doesn't have enough cash?
- Consider: should AI factions also have visible "war chests"?

**UI Integration:**
- Menu shows player's current cash (pulled from game)
- Purchases show cost and remaining balance
- Income notifications show amount added to player money

> **Flexibility Note:** The exact tick rates and costs should be balanced so the economy feels meaningful but not punishing. Players should feel rewarded for holding territory.

### 5.10 Follower / Bodyguard System

**Requirement:** Player can recruit followers from the menu to accompany them on raids and provide protection.

**Recruitment:**
- Recruit followers via Army menu
- Costs money (deducted from player's real GTA V cash)
- Choose tier: Basic, Medium, or Heavy (same costs as zone defenders)
- Maximum followers at one time (suggested: 4-6)

**Follower Behavior:**
- Follow the player on foot
- Enter vehicles with the player (fill passenger seats)
- Exit vehicles when player exits
- Engage enemies in combat (shoot, take cover)
- Run to keep up with player
- Defend player when attacked

**Combat Capabilities:**
- Weapons based on tier (Basic = Pistol, Medium = SMG, Heavy = Carbine)
- Health/armor based on tier
- Will fight alongside player during zone captures
- Can die in combat (permanently lost)

**Vehicle Integration:**
- Followers automatically enter player's vehicle
- Fill available passenger seats
- Exit and fight when player exits in combat zones
- Can shoot from vehicles if in appropriate seats

**Management:**
- View current followers in menu
- Dismiss followers (they despawn, no refund)
- Followers persist until death or dismissal
- On character switch, followers are dismissed

**Use Cases:**
- Bring backup on difficult zone raids
- Personal protection while exploring
- Assault heavily defended zones with support

> **Flexibility Note:** Follower AI behavior should use GTA V's native ped AI systems where possible. The implementation team should tune follower responsiveness and combat effectiveness for good gameplay feel.

---

## 6. Service Integration Matrix

### Services to Wire (MVP)

| Interface | Implementation | Integration Point |
|-----------|---------------|-------------------|
| **Core** |||
| `ITimeProvider` | TimeProvider | ServiceContainer |
| `IGameBridge` | GameBridge | ServiceContainer |
| `IPersistenceService` | JsonPersistenceService | GameStateManager |
| `ISaveFileValidator` | SaveFileValidator | GameStateManager.Load |
| `ISaveSlotManager` | SaveSlotManager | Menu (Save/Load) |
| `IAutoSaveService` | AutoSaveService | Game loop tick |
| **Territory** |||
| `IZoneRepository` | InMemoryZoneRepository | ServiceContainer |
| `IZoneService` | ZoneService | CombatManager, AIManager, Menu |
| **Factions** |||
| `IFactionRepository` | InMemoryFactionRepository | ServiceContainer |
| `IFactionService` | FactionService | CombatManager, AIManager, Menu |
| `IFactionRelationshipRepository` | InMemoryFactionRelationshipRepository | ServiceContainer |
| `IFactionRelationshipService` | FactionRelationshipService | AIManager |
| **Economy** |||
| `IResourceTickService` | ResourceTickService | Game loop tick |
| `IResourceStorage` | ResourceStorage | FactionService |
| `ISupplyLineService` | SupplyLineService | AI battle simulation |
| `IZoneTraitResourceModifier` | ZoneTraitResourceModifier | ResourceTickService |
| **Combat** |||
| `IPedPool` | InMemoryPedPool | PedManager |
| `IPedSpawningService` | PedSpawningService | CombatManager |
| `IPedDespawnService` | PedDespawnService | CombatManager |
| `IPedRecyclingService` | PedRecyclingService | CombatManager |
| `IReinforcementService` | ReinforcementService | CombatManager |
| `IControlPercentageCalculator` | ControlPercentageCalculator | CombatManager |
| `ITakeoverDetector` | TakeoverDetector | CombatManager |
| `ICombatResultHandler` | CombatResultHandler | CombatManager |
| **AI** |||
| `IAIStrategy` | Michael/Trevor/FranklinAIStrategy | AIManager |
| `IZoneEvaluationService` | ZoneEvaluationService | AIManager |
| `IResourceAllocationService` | ResourceAllocationService | AIManager |
| `IAggressionResponseService` | AggressionResponseService | AIManager |
| `IAIDifficultyService` | AIDifficultyService | AIManager |
| **UI** |||
| `IMenuProvider` | NativeUIMenuProvider | FactionWarsScript |
| `INotificationRenderer` | NotificationRenderer | NotificationService |
| `INotificationService` | NotificationService | CombatManager, AIManager |
| `ICombatHudRenderer` | CombatHudRenderer | CombatManager |
| `ICombatHudService` | CombatHudService | CombatManager |
| `IMapBlipService` | MapBlipService | MapBlipManager |
| `IFactionColorService` | FactionColorService | UI rendering |
| `IEventAlertService` | EventAlertService | AIManager, CombatManager |
| `IZoneBoundaryRenderer` | ZoneBoundaryRenderer | Debug mode |
| **Performance** |||
| `IObjectPool` | ObjectPool | PedManager |
| `ICacheService` | CacheService | ZoneService |

### New Services to Create

| Interface | Purpose |
|-----------|---------|
| `IVictoryConditionService` | Check and announce victory |
| `IDefenderTierService` | Manage defender tier costs and configs |
| `IBattleSimulationService` | Simulate AI vs AI battles |
| `IEventFeedService` | Track and display world events |
| `ITerritoryIndicatorService` | Show current zone status on HUD |

> **Flexibility Note:** Not all services may be needed. The implementation team should wire what's necessary and skip what isn't. The matrix above is a reference, not a mandate.

---

## 7. Zone Definitions

### Zone Coverage Requirements

- **Total Zones:** 30-35
- **Los Santos:** ~20 zones
- **Blaine County:** ~10-15 zones
- **Initial Distribution:** Roughly equal between 3 factions + some neutral

### Zone List

| ID | Name | Region | X | Y | Z | Radius | Value | Traits | Initial Owner |
|----|------|--------|---|---|---|--------|-------|--------|---------------|
| **LOS SANTOS - SOUTH** |||||||||
| grove | Grove Street | South LS | 105 | -1940 | 20 | 150 | 6 | Residential | Franklin |
| davis | Davis | South LS | 100 | -1750 | 30 | 180 | 5 | Residential, Fortified | Franklin |
| strawberry | Strawberry | South LS | 150 | -1350 | 30 | 180 | 5 | Residential | Franklin |
| chamberlain | Chamberlain Hills | South LS | -170 | -1650 | 30 | 180 | 5 | Residential | Franklin |
| rancho | Rancho | South LS | 450 | -1700 | 30 | 180 | 4 | Residential | Franklin |
| **LOS SANTOS - CENTRAL** |||||||||
| downtown | Downtown | Central LS | -227 | -836 | 30 | 200 | 8 | Commercial, HighValue | Michael |
| pillbox | Pillbox Hill | Central LS | 300 | -500 | 30 | 180 | 7 | Commercial | Michael |
| textile | Textile City | Central LS | 450 | -800 | 30 | 180 | 6 | Commercial, Industrial | Neutral |
| littleseoul | Little Seoul | Central LS | -700 | -900 | 20 | 180 | 6 | Commercial | Neutral |
| **LOS SANTOS - WEST** |||||||||
| rockford | Rockford Hills | West LS | -620 | 50 | 40 | 200 | 8 | Residential, HighValue | Michael |
| delperro | Del Perro | West LS | -1720 | -250 | 15 | 180 | 6 | Commercial | Michael |
| vespucci | Vespucci Beach | West LS | -1400 | -1000 | 5 | 200 | 5 | Commercial | Neutral |
| morningwood | Morningwood | West LS | -1300 | -400 | 35 | 150 | 5 | Residential | Michael |
| **LOS SANTOS - NORTH** |||||||||
| vinewood | Vinewood | North LS | 320 | 180 | 70 | 200 | 7 | Commercial, HighValue | Michael |
| mirror | Mirror Park | North LS | 1050 | -650 | 60 | 180 | 5 | Residential | Neutral |
| eclipse | Eclipse Blvd | North LS | -450 | 280 | 80 | 150 | 7 | Residential | Michael |
| **LOS SANTOS - EAST** |||||||||
| lamesa | La Mesa | East LS | 800 | -1900 | 25 | 200 | 5 | Industrial | Trevor |
| elburro | El Burro Heights | East LS | 1600 | -2200 | 60 | 200 | 4 | Industrial | Trevor |
| murrieta | Murrieta Heights | East LS | 1100 | -1400 | 35 | 180 | 5 | Industrial | Neutral |
| **LOS SANTOS - PORT** |||||||||
| port | Port of LS | Port | 150 | -2950 | 5 | 250 | 9 | Port, Industrial | Trevor |
| terminal | Terminal | Port | 850 | -2950 | 5 | 200 | 7 | Port, Industrial | Trevor |
| **LOS SANTOS - AIRPORT** |||||||||
| airport | LS Airport | Airport | -1100 | -2850 | 15 | 350 | 10 | Airfield, HighValue | Neutral |
| **BLAINE COUNTY** |||||||||
| sandy | Sandy Shores | Blaine | 1900 | 3750 | 30 | 300 | 5 | Residential, Fortified | Trevor |
| grapeseed | Grapeseed | Blaine | 2000 | 4900 | 40 | 200 | 3 | Residential | Trevor |
| harmony | Harmony | Blaine | 550 | 2700 | 40 | 200 | 4 | Industrial | Trevor |
| stab | Stab City | Blaine | 70 | 3700 | 40 | 150 | 3 | Residential | Trevor |
| alamo | Alamo Sea | Blaine | 1350 | 4350 | 30 | 250 | 4 | Residential | Neutral |
| chumash | Chumash | Coast | -3200 | 450 | 10 | 200 | 5 | Residential | Neutral |
| paleto | Paleto Bay | North | -350 | 6250 | 30 | 250 | 6 | Residential, Port | Neutral |
| zancudo | Fort Zancudo | Military | -2200 | 3200 | 30 | 300 | 10 | Military, Fortified | Neutral |
| **TOTAL: 31 ZONES** |||||||||

### Initial Territory Distribution

| Faction | Zones | Total Value |
|---------|-------|-------------|
| Michael | 8 | 54 |
| Trevor | 10 | 49 |
| Franklin | 5 | 25 |
| Neutral | 8 | 53 |

> **Flexibility Note:** Zone coordinates, radii, values, and initial ownership can all be adjusted. The implementation team should tune for balanced, fun gameplay and may add/remove zones as needed.

---

## 8. Testing & Tuning

### Testing Philosophy

Thorough test coverage is encouraged to ensure reliability and enable confident iteration on gameplay parameters. Tests should cover critical paths and edge cases.

### Recommended Test Coverage

| Area | Priority | Focus |
|------|----------|-------|
| **Service Container** | High | Dependency resolution, lifecycle |
| **Combat Flow** | High | Zone entry → spawn → fight → capture |
| **AI Decisions** | High | Evaluation, allocation, battle simulation |
| **Economy** | Medium | Resource generation, purchases |
| **Victory Condition** | Medium | Detection, edge cases |
| **Save/Load** | Medium | State preservation, corruption handling |
| **Defender Tiers** | Medium | Cost calculation, stat application |

### Test Approach

- **TDD Encouraged:** Write tests for complex logic before implementation
- **Mocking:** Use `MockGameBridge` for all GTA V API calls
- **In-Memory:** Use in-memory repositories for state
- **No GTA Required:** All tests run without game
- **Iterate Freely:** Tests enable safe parameter tuning

### Gameplay Tuning

The following parameters should be tuned for exciting, balanced gameplay:

#### Combat Balance
| Parameter | Starting Value | Tune For |
|-----------|---------------|----------|
| Defender spawn count divisor | 5 | More/fewer defenders per allocated troop |
| Max defenders per encounter | 8 | Combat intensity |
| Spawn wave delay | 2-3 seconds | Pacing, tension |
| Spawn distance from player | 40-80m | Surprise vs. fairness |

#### Economy Balance
| Parameter | Starting Value | Tune For |
|-----------|---------------|----------|
| Base income per zone | $100/tick | Progression speed |
| Strategic value multiplier | 1x-2x | Zone importance |
| Resource tick interval | 5 minutes | Income pacing |
| Troop costs | $200/$500/$1000 | Investment decisions |

#### AI Balance
| Parameter | Starting Value | Tune For |
|-----------|---------------|----------|
| AI decision interval | 30 seconds | AI responsiveness |
| Attack threshold (troops) | 10 | AI aggression |
| Defense priority bonus | +3 | AI territorial behavior |
| Battle win probability base | 50/50 | AI vs AI outcomes |
| Casualty rates | 20-40% | Attrition, recovery time |

#### Faction Personalities
| Faction | Aggression | Defense Priority | Risk Tolerance |
|---------|------------|------------------|----------------|
| Michael | Low (0.3) | High (0.8) | Low - protects assets |
| Trevor | High (0.8) | Low (0.3) | High - overextends |
| Franklin | Medium (0.5) | Medium (0.5) | Medium - opportunistic |

#### Victory Pacing
| Parameter | Starting Value | Tune For |
|-----------|---------------|----------|
| Zone count | 31 | Game length |
| Initial faction distribution | ~equal | Early game balance |
| Neutral zone count | 8 | Expansion opportunities |

**Tuning Goal:** Games should feel dynamic with momentum shifts. Neither steamroll victories nor endless stalemates. Target: 30-60 minute games with 2-3 major turning points.

> **Flexibility Note:** All parameters above are starting suggestions. The implementation team should tune extensively through playtesting to achieve the best gameplay experience.

---

## 9. Implementation Phases

### Phase 1: Foundation (Core Infrastructure)

**Files to Create:**
- `ScriptHookV/FactionWarsScript.cs` - Main entry point
- `ScriptHookV/GameBridge.cs` - IGameBridge implementation
- `ScriptHookV/ServiceContainer.cs` - DI container
- `ScriptHookV/ServiceContainerFactory.cs` - Service wiring
- `ScriptHookV/Logger.cs` - File logging
- `ScriptHookV/TimeProvider.cs` - ITimeProvider impl

**Deliverable:** Mod loads, services resolve, logging works

> **Flexibility Note:** The implementation team may organize foundation code differently, use alternative DI approaches, or combine/split files as they see fit.

### Phase 2: Territory & Factions

**Files to Create:**
- `ScriptHookV/ZoneDataLoader.cs` - Load 31 zones
- `ScriptHookV/MapBlipManager.cs` - Zone blips

**Integration:**
- Wire IZoneService, IZoneRepository
- Wire IFactionService, IFactionRepository
- Wire IFactionRelationshipService

**Deliverable:** Zones appear on map with faction colors

> **Flexibility Note:** Zone data can come from JSON, hardcoded values, or any source. Blip styling is up to the implementation team.

### Phase 3: Combat System

**Files to Create:**
- `ScriptHookV/PedManager.cs` - Ped lifecycle
- `ScriptHookV/CombatManager.cs` - Combat encounters
- `Core/Services/DefenderTierService.cs` - Tier configs
- `Core/Interfaces/IDefenderTierService.cs` - Interface

**Integration:**
- Wire IPedSpawningService, IPedDespawnService, IPedRecyclingService
- Wire IReinforcementService, ICombatResultHandler
- Wire IControlPercentageCalculator, ITakeoverDetector

**Deliverable:** Enter enemy zone, fight defenders, capture zone

> **Flexibility Note:** Combat pacing, spawn behavior, and defender AI can be tuned freely. The implementation team should experiment to find what's fun.

### Phase 4: Economy System

**Files to Create:**
- `ScriptHookV/EconomyManager.cs` - Resource ticks

**Integration:**
- Wire IResourceTickService
- Wire IResourceStorage
- Wire ISupplyLineService
- Wire IZoneTraitResourceModifier

**Deliverable:** Zones generate income, can buy troops

> **Flexibility Note:** Economic values, tick rates, and costs should be tuned through playtesting.

### Phase 5: AI System

**Files to Create:**
- `ScriptHookV/AIManager.cs` - AI decisions
- `Core/Services/BattleSimulationService.cs` - AI battles
- `Core/Interfaces/IBattleSimulationService.cs` - Interface

**Integration:**
- Wire IAIStrategy (all 3)
- Wire IZoneEvaluationService
- Wire IResourceAllocationService
- Wire IAggressionResponseService
- Wire IAIDifficultyService

**Deliverable:** AI factions make decisions, allocate troops, battle each other

> **Flexibility Note:** AI personality tuning is critical. The implementation team should iterate until Michael feels defensive, Trevor feels aggressive, and Franklin feels opportunistic.

### Phase 6: UI System

**Files to Create:**
- `ScriptHookV/UI/NativeUIMenuProvider.cs` - Menus
- `ScriptHookV/UI/CombatHudRenderer.cs` - Combat HUD
- `ScriptHookV/UI/TerritoryIndicatorRenderer.cs` - Zone status display
- `ScriptHookV/UI/EventFeedRenderer.cs` - World event feed
- `ScriptHookV/UI/NotificationRenderer.cs` - Notifications
- `ScriptHookV/UI/ZoneBoundaryRenderer.cs` - Debug

**Integration:**
- Wire IMenuProvider
- Wire ICombatHudService, ICombatHudRenderer
- Wire IEventFeedService (new)
- Wire ITerritoryIndicatorService (new)
- Wire INotificationService, INotificationRenderer
- Wire IEventAlertService
- Wire IFactionColorService
- Wire IMapBlipService

**HUD Elements:**
- **Top:** Territory indicator (zone name, owner, control %)
- **Center:** Combat HUD when fighting (progress bar, defender count)
- **Bottom Left:** Event feed (last 4-6 world events, color-coded)

**Menu Structure (F7):**
- Overview: Faction stats, victory progress
- Zone Management: View zones, allocate troops by tier, withdraw
- Army: Purchase troops, view distribution
- Resources: Income breakdown
- Relations: Faction standings
- Settings: Save, load, debug options

**Deliverable:** Full menu system, HUD with territory indicator, combat display, and event feed

> **Flexibility Note:** Menu structure, HUD layout, and styling are flexible. The implementation team should make the UI readable, unobtrusive, and fitting for GTA V's aesthetic.

### Phase 7: Persistence & Victory

**Files to Create:**
- `ScriptHookV/GameStateManager.cs` - Save/load
- `ScriptHookV/VictoryManager.cs` - Victory detection
- `Core/Services/VictoryConditionService.cs` - Victory logic
- `Core/Interfaces/IVictoryConditionService.cs` - Interface

**Integration:**
- Wire IPersistenceService
- Wire ISaveFileValidator
- Wire ISaveSlotManager
- Wire IAutoSaveService

**Deliverable:** Save/load works, victory detected at 100%

> **Flexibility Note:** Save format, auto-save frequency, and victory presentation are all flexible.

### Phase 8: Polish & Tuning

**Tasks:**
- Verify build succeeds
- Run test suite, fix any failures
- In-game playtesting
- Tune gameplay parameters for fun factor
- Balance AI aggression and economy pacing
- Bug fixes and edge case handling

> **Flexibility Note:** This phase is ongoing. The implementation team should iterate until the game feels polished and fun.

---

## 10. Acceptance Criteria

### Game Loop Integration (Critical)

**All services must be actively wired into the game loop, not just instantiated.**

- [ ] Main `OnTick()` calls relevant services each frame/interval
- [ ] Resource generation actually adds money to player on tick
- [ ] AI decisions execute and change game state periodically
- [ ] Combat services respond to real player/ped events
- [ ] Zone detection triggers on actual player movement
- [ ] No orphaned services - every registered service has a caller

### Core Functionality

- [ ] Mod loads without errors
- [ ] F7 opens main menu
- [ ] 31 zones appear on map with correct colors
- [ ] Player faction determined by character model
- [ ] Entering enemy zone triggers combat
- [ ] Defenders spawn behind player (not in FOV)
- [ ] Killing defenders captures zone
- [ ] Zone blip color updates on capture
- [ ] Victory screen at 100% control

### HUD & Status Display

- [ ] Territory indicator shows current zone name and owner
- [ ] Territory indicator shows control percentage during combat
- [ ] Combat HUD displays defender count and capture progress
- [ ] Event feed visible in bottom left corner
- [ ] Zone captures appear in event feed
- [ ] AI skirmishes appear in event feed
- [ ] Events color-coded by faction
- [ ] Player's zones under attack highlighted/emphasized

### Troop Management

- [ ] Can buy troops from menu (go to reserve pool)
- [ ] Reserve pool tracks troops by tier
- [ ] Can allocate troops from reserve to zones
- [ ] Can withdraw troops from zones back to reserve
- [ ] Can allocate Basic/Medium/Heavy to zones
- [ ] Different tiers cost different amounts
- [ ] Allocated troops determine defender count
- [ ] Correct tier peds spawn (weapons, health, armor)
- [ ] Troops deducted from zone when defenders die

### Economy

- [ ] Zones generate income over time
- [ ] Income is added to player's real GTA V cash
- [ ] Income scales with strategic value
- [ ] Troop purchases deduct from player's real GTA V cash
- [ ] Player's actual cash balance displayed in menu
- [ ] Cannot purchase if insufficient funds

### AI

- [ ] AI factions make decisions periodically
- [ ] AI allocates troops to zones
- [ ] AI attacks player/other AI zones
- [ ] Background battles resolve (math-based)
- [ ] Zone ownership changes from AI battles
- [ ] Notifications for AI zone captures

### Persistence

- [ ] Can save game from menu
- [ ] Can load saved game
- [ ] Auto-save periodically
- [ ] State preserved: zones, factions, troops, cash

### Follower / Bodyguard System

- [ ] Can recruit followers from Army menu
- [ ] Follower costs deducted from player's real GTA V cash
- [ ] Can choose follower tier (Basic/Medium/Heavy)
- [ ] Maximum follower limit enforced
- [ ] Followers follow player on foot
- [ ] Followers enter vehicles with player
- [ ] Followers exit vehicles when player exits
- [ ] Followers engage enemies in combat
- [ ] Followers use weapons appropriate to their tier
- [ ] Followers can die in combat (permanently lost)
- [ ] Can dismiss followers from menu
- [ ] Followers dismissed on character switch

### Quality

- [ ] Build succeeds without errors
- [ ] Test suite passes
- [ ] No crashes during normal gameplay
- [ ] Gameplay feels balanced and fun

> **Flexibility Note:** The acceptance criteria above define the target functionality. The implementation team has full flexibility in HOW these criteria are achieved - architecture, code organization, specific implementations, and parameter values are all open to creative solutions. What matters is delivering a fun, working game.

---

## 11. Handover Document

**Requirement:** Upon completion, the implementation team must provide a handover document.

### Document Contents

1. **Implementation Summary**
   - Overview of what was built
   - Architecture decisions made
   - Key files and their purposes
   - Any deviations from the RFP and rationale

2. **Installation Guide**
   - Prerequisites (ScriptHookV, etc.)
   - Step-by-step installation instructions
   - Required file locations
   - Troubleshooting common issues

3. **User Guide / How to Play**
   - Controls and keybindings (F7 for menu, etc.)
   - Menu walkthrough
   - How to buy troops and manage reserves
   - How to allocate troops to zones
   - How to recruit followers
   - How combat works
   - How to save/load
   - Tips for new players

4. **Feature Walkthrough**
   - Complete list of implemented features
   - How each feature works in-game
   - Any known limitations or quirks

5. **Technical Reference**
   - Service wiring overview
   - Configuration options and tuning parameters
   - How to modify zone definitions
   - How to adjust balance values

6. **Known Issues & Future Work**
   - Any bugs or limitations
   - Suggestions for future enhancements
   - What was descoped and why (if anything)

### Format

The handover document should be a markdown file (e.g., `HANDOVER.md`) included in the repository root.

---

## Appendix A: GTA V Coordinates Reference

Coordinates sourced from:
- [GTAForums Coordinate List](https://gtaforums.com/topic/792877-list-of-over-100-coordinates-more-comming/)
- [GTA Lens Interactive Map](https://gtalens.com/map)
- [GTAWeb Interactive Map](https://gtaweb.eu/gtao-map/ls/0)

## Appendix B: Existing Interface Documentation

All 64 interfaces are documented in their respective files under `src/FactionWars/*/Interfaces/`.

## Appendix C: Test Coverage Report

Run `dotnet test --collect:"XPlat Code Coverage"` to generate coverage report.

---

*Document Version: 1.3*
*Last Updated: 2026-01-20*
