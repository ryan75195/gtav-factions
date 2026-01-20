# HANDOVER.md - FactionWars ScriptHookVDotNet Integration

## Table of Contents

1. [Implementation Summary](#1-implementation-summary)
2. [Installation Guide](#2-installation-guide)
3. [User Guide / How to Play](#3-user-guide--how-to-play)
4. [Feature Walkthrough](#4-feature-walkthrough)
5. [Technical Reference](#5-technical-reference)
6. [Known Issues & Future Work](#6-known-issues--future-work)

---

## 1. Implementation Summary

### Overview

FactionWars is a fully playable GTA V territory control mod where three factions battle for control of Los Santos and Blaine County. The player joins their character's faction (Michael → Blue/Organization, Trevor → Orange/TPE, Franklin → Green/Crew) and must strategically capture zones, manage resources, recruit troops, and outmaneuver enemy AI to dominate the map.

The implementation integrates the existing domain layer (64 interfaces, 5,617 passing tests) into ScriptHookVDotNet, creating a complete gameplay experience.

### Architecture Decisions

1. **Service Container Pattern** - Simple dependency injection container (`ServiceContainer.cs`) that resolves services by type. `ServiceContainerFactory.cs` wires all services together at startup.

2. **Manager Pattern** - Manager classes coordinate domain services with the game loop:
   - `CombatManager` - Handles combat encounters and ped spawning
   - `AIManager` - Coordinates AI faction decisions
   - `EconomyManager` - Processes resource ticks
   - `TerritoryManager` - Zone detection based on player position
   - `FollowerManager` - Bodyguard system
   - `VictoryManager` - Victory condition detection

3. **Bridge Pattern** - `GameBridge.cs` implements `IGameBridge` with ScriptHookVDotNet natives, enabling:
   - Unit testing with `MockGameBridge`
   - Decoupling business logic from GTA V APIs
   - All 5,617 tests run without requiring the game

4. **Event-Driven Updates** - `GameLoopController` coordinates tick-based updates to all managers

5. **Thin Entry Point** - `FactionWarsScript` extends `GTA.Script` but delegates all logic to `GameLoopController` for testability

### Key Files and Purposes

| File | Purpose |
|------|---------|
| `ScriptHookV/FactionWarsScript.cs` | Main entry point (GTA.Script) |
| `ScriptHookV/GameLoopController.cs` | Coordinates all game systems each tick |
| `ScriptHookV/ServiceContainer.cs` | Dependency injection container |
| `ScriptHookV/ServiceContainerFactory.cs` | Wires all 50+ services together |
| `ScriptHookV/GameBridge.cs` | IGameBridge implementation with natives |
| `ScriptHookV/PlayerContext.cs` | Current player faction and state |
| `ScriptHookV/Managers/*.cs` | Combat, AI, Economy, Territory, Follower, Victory |
| `ScriptHookV/UI/*.cs` | Menus, HUD, indicators, event feed |
| `ScriptHookV/Data/ZoneDataLoader.cs` | Loads 31 zone definitions |
| `ScriptHookV/Persistence/*.cs` | Save/load coordination |
| `Core/Services/*.cs` | Domain services (DefenderTier, Victory, Battle) |
| `Balance/Models/*.cs` | Balance configuration and presets |

### Deviations from RFP

No significant deviations. All acceptance criteria from Section 10 were met:

- All 31 zones implemented with correct coordinates
- Three factions with distinct AI personalities
- Full combat system with tiered defenders
- Economy tied to real GTA V cash
- Follower/bodyguard system
- Save/load with auto-save
- Victory condition at 100% control
- Complete menu system (F7)
- HUD with territory indicator, combat display, and event feed

---

## 2. Installation Guide

### Prerequisites

1. **GTA V** - Latest version (Steam, Rockstar Launcher, or Epic Games)
2. **ScriptHookV** - Download from [dev-c.com](http://www.dev-c.com/gtav/scripthookv/)
3. **ScriptHookVDotNet** - Version 3.6.0 or higher from [GitHub](https://github.com/scripthookvdotnet/scripthookvdotnet/releases)
4. **.NET Framework 4.8** - Usually pre-installed on Windows 10/11

### Step-by-Step Installation

1. **Install ScriptHookV**
   - Extract `ScriptHookV.dll` and `dinput8.dll` to your GTA V root folder
   - (e.g., `C:\Program Files\Rockstar Games\Grand Theft Auto V\`)

2. **Install ScriptHookVDotNet**
   - Extract `ScriptHookVDotNet3.dll` to your GTA V root folder
   - Create a `scripts` folder if it doesn't exist

3. **Install FactionWars**
   - Copy `FactionWars.dll` to the `scripts` folder
   - Copy `NativeUI.dll` to the `scripts` folder
   - Copy `Newtonsoft.Json.dll` to the `scripts` folder

4. **Verify Installation**
   - Launch GTA V
   - Load into single-player mode
   - You should see "FactionWars loaded successfully!" notification
   - Press F7 to open the menu

### Required File Locations

```
Grand Theft Auto V/
├── ScriptHookV.dll
├── dinput8.dll
├── ScriptHookVDotNet3.dll
└── scripts/
    ├── FactionWars.dll
    ├── NativeUI.dll
    └── Newtonsoft.Json.dll
```

### Troubleshooting

| Issue | Solution |
|-------|----------|
| "FactionWars failed to load" | Check all DLLs are in correct locations |
| No notification on startup | Verify ScriptHookV and ScriptHookVDotNet are installed |
| Menu doesn't open (F7) | Check for keybind conflicts with other mods |
| Game crashes on load | Update ScriptHookV to match your GTA V version |
| Zones don't appear on map | Wait for full game load; check scripts folder |

---

## 3. User Guide / How to Play

### Controls and Keybindings

| Key | Action |
|-----|--------|
| **F7** | Open/Close FactionWars menu |
| **Backspace** | Go back in menu |
| **Enter** | Select menu item |
| **Arrow Keys** | Navigate menu |
| **Escape** | Close menu |

### Menu Walkthrough

#### Overview Submenu
- **Victory Progress** - Shows each faction's zone count and percentage
- **Your Faction** - Current faction name, zones owned, total troops
- **Resources** - Income per tick, current cash balance

#### Zone Management Submenu
- **View Zones** - List of all zones you own
- **Zone Details** - Select a zone to see defenders allocated
- **Allocate Troops** - Move troops from reserve to zone (by tier)
- **Withdraw Troops** - Move troops from zone back to reserve

#### Army Submenu
- **Reserve Pool** - Shows available troops by tier (Basic/Medium/Heavy)
- **Purchase Troops** - Buy troops to add to reserve
  - Basic: $200 (100 HP, Pistol)
  - Medium: $500 (150 HP, 50 Armor, SMG)
  - Heavy: $1,000 (200 HP, 100 Armor, Carbine)
- **Recruit Followers** - Hire bodyguards (max 4-6)
- **View Followers** - See current followers, dismiss if needed

#### Resources Submenu
- **Income Breakdown** - Per-zone income with trait bonuses
- **Supply Lines** - Connected zone bonus information
- **Current Balance** - Your actual GTA V cash

#### Settings Submenu
- **Save Game** - Manual save to slot
- **Load Game** - Load from save slot
- **Auto-Save** - Toggle and configure interval
- **Difficulty** - Easy, Normal, Hard, Veteran
- **Debug Options** - Zone boundaries, AI logging

### How to Buy Troops and Manage Reserves

1. Open menu (F7) → **Army** → **Purchase Troops**
2. Select tier (Basic $200, Medium $500, Heavy $1,000)
3. Enter quantity (deducts from your GTA V cash)
4. Troops go to your **reserve pool** (not directly to zones)
5. Go to **Zone Management** → **Allocate Troops** to deploy

### How to Allocate Troops to Zones

1. Open menu (F7) → **Zone Management**
2. Select a zone you own
3. Choose **Allocate Troops**
4. Select tier and quantity from reserve
5. Confirm allocation

### How to Recruit Followers

1. Open menu (F7) → **Army** → **Recruit Followers**
2. Select tier (determines weapons, health, armor)
3. Confirm (costs cash, spawns follower near you)
4. Followers automatically follow, enter vehicles, and fight

### How Combat Works

1. **Enter enemy zone** - Territory indicator shows zone name and owner
2. **Combat triggers** - Defenders spawn behind you (outside FOV)
3. **Fight defenders** - Kill them to increase control percentage
4. **Capture zone** - Reach 100% control to capture
5. **Zone transfers** - Blip color updates, zone joins your faction

**Combat HUD displays:**
- Zone name and current owner
- Defender count (remaining / total)
- Capture progress bar (0-100%)

**Death in combat:**
- Counts as retreat
- Zone ownership unchanged
- Combat ends

### How to Save/Load

1. Open menu (F7) → **Settings** → **Save Game**
2. Select save slot (Slot 1, 2, or 3)
3. Confirm save

**To load:**
1. Open menu (F7) → **Settings** → **Load Game**
2. Select save slot with timestamp
3. Confirm load (replaces current state)

**Auto-save:**
- Enabled by default (every 10 minutes)
- Configure in Settings → Auto-Save

### Tips for New Players

1. **Start with neutral zones** - No defenders, easy captures
2. **Build your reserve first** - Buy troops before attacking heavily defended zones
3. **Watch the event feed** - See AI attacks and plan responses
4. **Defend your high-value zones** - Allocate more troops to Downtown, Port, Airport
5. **Use followers for hard zones** - Heavy followers can turn the tide
6. **Check supply lines** - Connected zones provide bonuses
7. **Character switching** - Changes your faction! Followers are dismissed
8. **Watch for "Zone Under Attack"** - Set waypoint to defend your territory

---

## 4. Feature Walkthrough

### Territory Control System

**31 controllable zones** covering Los Santos and Blaine County:

| Region | Zones | Notable Locations |
|--------|-------|-------------------|
| South LS | 5 | Grove Street, Davis, Strawberry |
| Central LS | 5 | Downtown, Pillbox Hill, Little Seoul |
| West LS | 4 | Rockford Hills, Del Perro, Vespucci |
| Port/Industrial | 4 | Port of LS, Terminal, Elysian Island |
| Airport | 1 | Los Santos International |
| Blaine County | 12 | Sandy Shores, Paleto Bay, Trevor's Airfield |

**Zone traits provide bonuses:**
- Commercial - Extra cash generation
- Industrial - Weapons production bonus
- Residential - Recruitment bonus
- Port - Smuggling bonuses
- Airfield - Transport bonuses
- HighValue - 2x income multiplier
- Fortified - Defense bonus

### Combat System

**Defender scaling:**
- Spawn count = Total allocated troops ÷ 5 (max 8)
- Spawns in waves: Heavy → Medium → Basic
- Troops deducted from zone when defenders die

**Wave spawning:**
- 120-240 degrees behind player (outside FOV)
- 40-80m spawn distance
- 2-3 second delay between waves

**Zone capture:**
- Kill defenders to increase control %
- 100% control = capture
- 5 second hold time required

### Economy System

**Real GTA V cash integration:**
- Zone income added to player's actual money
- Troop purchases deduct from player's cash
- No separate "faction currency"

**Resource generation:**
- Base: $100 per zone per tick (5 minutes)
- Modified by strategic value (1-10)
- Modified by zone traits
- Modified by supply line bonuses

### AI System

**Three distinct AI personalities:**

| Faction | Aggression | Defense | Risk |
|---------|------------|---------|------|
| Michael | Low (0.3) | High (0.8) | Low |
| Trevor | High (0.8) | Low (0.3) | High |
| Franklin | Medium (0.5) | Medium (0.5) | Medium |

**AI behaviors:**
- Evaluate zones by strategic value and proximity
- Allocate troops to high-priority zones
- Attack weak zones opportunistically
- Defend zones under attack
- Background simulation when player absent

### Follower System

**Recruitment:**
- Maximum 4-6 followers at once
- Costs real GTA V cash
- Choose tier for weapon/health

**Behavior:**
- Follow player on foot
- Enter/exit vehicles automatically
- Engage enemies in combat
- Use cover, shoot accurately

**Management:**
- View in Army → View Followers
- Dismiss (no refund)
- Death is permanent
- All dismissed on character switch

### Persistence System

**Save data includes:**
- Zone ownership
- Faction states (troops, resources)
- Reserve pools by tier
- Zone defender allocations
- Victory progress

**Save slots:**
- 3 manual save slots
- Auto-save (configurable interval)
- JSON format (human-readable)

### Victory Condition

- Achieve **100% territorial control** (all 31 zones)
- Victory screen displays:
  - Winning faction name
  - Total zones captured
  - Time to victory
  - Option to continue or start new game

### HUD Elements

**Territory Indicator (top of screen):**
- Current zone name
- Owning faction (color-coded)
- Control percentage (during combat)
- "NEUTRAL" or "CONTESTED" status

**Combat HUD (during encounters):**
- Zone being contested
- Defender count (remaining/total)
- Capture progress bar
- Outcome indicator

**Event Feed (bottom left):**
- Last 4-6 world events
- Color-coded by faction
- Zone captures, AI attacks, skirmishes
- Player zone attacks highlighted

### Known Limitations

- Follower AI uses native GTA V ped AI (occasionally quirky)
- Maximum ~30 active peds for game stability
- Zone boundaries are circular (not polygon-matched to real streets)
- Background AI battles are math-based (not visually simulated)

---

## 5. Technical Reference

### Service Wiring Overview

The `ServiceContainerFactory` wires 50+ services at startup:

```
Core Services:
├── ITimeProvider → SystemTimeProvider
├── IGameBridge → GameBridge
├── IPlayerContext → PlayerContext
└── IPlayerFactionDetector → CharacterModelFactionDetector

Territory Services:
├── IZoneRepository → InMemoryZoneRepository
├── IZoneService → ZoneService
└── IZoneDefenderAllocationService → ZoneDefenderAllocationService

Faction Services:
├── IFactionRepository → InMemoryFactionRepository
├── IFactionService → FactionService
└── IFactionRelationshipService → FactionRelationshipService

Combat Services:
├── IPedPool → InMemoryPedPool
├── IPedSpawningService → PedSpawningService
├── IControlPercentageCalculator → ControlPercentageCalculator
├── ICombatResultHandler → CombatResultHandler
└── IDefenderTierService → DefenderTierService

Economy Services:
├── IResourceTickService → ResourceTickService
├── ITroopPurchaseService → TroopPurchaseService
└── ISupplyLineService → SupplyLineService

AI Services:
├── IAIStrategy (3 implementations)
├── IZoneEvaluationService → ZoneEvaluationService
├── IBattleSimulationService → BattleSimulationService
└── IAggressionResponseService → AggressionResponseService

UI Services:
├── IMenuProvider → NativeUIMenuProvider
├── INotificationService → NotificationService
├── ICombatHudService → CombatHudService
├── IEventFeedService → EventFeedService
└── ITerritoryIndicatorService → TerritoryIndicatorService

Persistence Services:
├── IPersistenceService → JsonPersistenceService
├── ISaveSlotManager → SaveSlotManager
└── IAutoSaveService → AutoSaveService
```

### Configuration Options

All balance parameters are configurable via `BalanceConfiguration`:

**Economy Settings:**
| Parameter | Default | Description |
|-----------|---------|-------------|
| BaseCashGeneration | 100 | Cash per zone per tick |
| ResourceTickIntervalSeconds | 300 | Time between ticks (5 min) |
| MaxCashStorage | 100,000 | Maximum faction cash |

**Combat Settings:**
| Parameter | Default | Description |
|-----------|---------|-------------|
| AttackerVictoryThreshold | 100% | Control needed to win |
| MinimumHoldTimeSeconds | 5 | Hold time before victory |
| MaxActivePeds | 30 | Max combat peds |

**AI Settings:**
| Parameter | Default | Description |
|-----------|---------|-------------|
| AIDecisionIntervalSeconds | 5 | Time between AI decisions |
| AIAggressionMultiplier | 1.0 | Global aggression modifier |
| AIAttackCooldownSeconds | 30 | Cooldown between attacks |

### Difficulty Presets

Four presets available in `BalancePresets`:

| Preset | Resource Gen | AI Aggression | Player Bonus |
|--------|--------------|---------------|--------------|
| Easy | 4 min tick | 0.7x | +50% resources |
| Normal | 5 min tick | 1.0x | None |
| Hard | 6 min tick | 1.2x | -10% resources |
| Veteran | 7 min tick | 1.5x | -25% resources |

### How to Modify Zone Definitions

Zones are defined in `ZoneDataLoader.cs`:

```csharp
// Example zone creation
yield return CreateZone(
    id: "downtown",
    name: "Downtown",
    x: -250f,           // GTA V X coordinate
    y: -850f,           // GTA V Y coordinate
    radius: 200f,       // Zone radius in meters
    strategicValue: 8,  // 1-10 scale
    traits: ZoneTrait.Commercial | ZoneTrait.HighValue
);
```

**To add a new zone:**
1. Open `ScriptHookV/Data/ZoneDataLoader.cs`
2. Add a new `yield return CreateZone(...)` in `CreateDefaultZones()`
3. Use GTA V map coordinates (X, Y)
4. Set appropriate radius, value, and traits

**Zone traits (flags):**
- `ZoneTrait.None`
- `ZoneTrait.Commercial`
- `ZoneTrait.Industrial`
- `ZoneTrait.Residential`
- `ZoneTrait.Port`
- `ZoneTrait.Airfield`
- `ZoneTrait.HighValue`
- `ZoneTrait.Fortified`

### How to Adjust Balance Values

**Method 1: Modify BalancePresets**

Edit `Balance/Models/BalancePresets.cs`:

```csharp
public static BalanceConfiguration Normal()
{
    return new BalanceConfiguration
    {
        BaseCashGeneration = 150,  // Increase income
        AIAggressionMultiplier = 0.8f,  // Reduce AI aggression
        // ... other values
    };
}
```

**Method 2: Create Custom Preset**

```csharp
public static BalanceConfiguration Custom()
{
    var config = Normal();  // Start with Normal
    config.PlayerResourceMultiplier = 1.25f;  // +25% player resources
    config.AIAttackCooldownSeconds = 45f;  // Slower AI attacks
    config.PresetName = "Custom";
    return config;
}
```

### Defender Tier Configuration

Tiers are defined in `DefenderTierService.cs`:

| Tier | Cost | Health | Armor | Weapon | Accuracy | Modifier |
|------|------|--------|-------|--------|----------|----------|
| Basic | $200 | 100 | 0 | Pistol | 0.3 | 1.0x |
| Medium | $500 | 150 | 50 | SMG | 0.5 | 1.5x |
| Heavy | $1,000 | 200 | 100 | Carbine | 0.7 | 2.0x |

---

## 6. Known Issues & Future Work

### Known Issues

1. **Follower vehicle pathfinding** - Followers occasionally struggle to enter vehicles in tight spaces

2. **Zone boundary precision** - Circular zones don't match exact GTA V street layouts; some overlap

3. **AI battle notifications** - Very rapid AI battles can flood the event feed

4. **Large-scale combat** - 30+ peds can cause frame rate drops on lower-end systems

5. **Save file location** - Currently saves to working directory; may require admin rights in some installations

### Suggestions for Future Enhancements

**From RFP "Out of Scope" items:**

1. **Escalation System** - Weapon/vehicle unlocks based on faction progress
   - Unlock helicopters, military vehicles at milestones
   - Progressive weapon tiers for player and AI

2. **Lieutenant System** - Named NPCs with unique traits
   - Recruit lieutenants with special abilities
   - Send lieutenants to lead attacks
   - Risk of lieutenant death/capture

3. **Loyalty System** - Zone integration/insurgency mechanics
   - Zones have loyalty ratings
   - Low loyalty = insurgent attacks
   - Build loyalty through presence/investment

4. **Diplomacy System** - Faction truces and alliances
   - Temporary ceasefires
   - Joint attacks against common enemy
   - Betrayal mechanics

**Additional enhancement ideas:**

5. **Polygon zone boundaries** - Match actual GTA V neighborhoods more precisely

6. **Visual AI battles** - Option to spectate AI vs AI combat

7. **Faction headquarters** - Special "home base" zones with unique mechanics

8. **Random events** - Police raids, rival gang skirmishes, civilian uprisings

9. **Statistics tracking** - Detailed stats (zones captured, troops lost, income earned)

10. **Multiple campaigns** - Different starting scenarios and win conditions

### What Was Descoped

All RFP MVP features were implemented. The following RFP suggestions were implemented as specified:

- All 31 zones ✓
- Three factions with distinct AI ✓
- Full combat system ✓
- Real GTA V cash integration ✓
- Follower system ✓
- Complete menu (F7) ✓
- HUD elements ✓
- Save/load ✓
- Victory condition ✓

Nothing was descoped from the MVP requirements.

---

*Document Version: 1.0*
*Implementation Complete: 2026-01-20*
*Test Suite: 5,617 tests passing*
