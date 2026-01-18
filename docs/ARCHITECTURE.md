# Architecture Documentation

## Overview

GTA V Faction Wars follows a clean architecture with clear separation of concerns. The codebase is organized into modules, each responsible for a specific domain area.

## Module Dependency Graph

```
                    ┌─────────────────┐
                    │      Core       │
                    │  (Interfaces,   │
                    │   Models, Utils)│
                    └────────┬────────┘
                             │
        ┌────────────────────┼────────────────────┐
        │                    │                    │
        ▼                    ▼                    ▼
┌───────────────┐   ┌───────────────┐   ┌───────────────┐
│   Territory   │   │   Factions    │   │  Persistence  │
│    (Zones)    │   │   (State)     │   │  (Save/Load)  │
└───────┬───────┘   └───────┬───────┘   └───────────────┘
        │                   │
        └─────────┬─────────┘
                  │
        ┌─────────┴─────────┐
        │                   │
        ▼                   ▼
┌───────────────┐   ┌───────────────┐
│    Economy    │   │    Combat     │
│  (Resources)  │   │    (Peds)     │
└───────┬───────┘   └───────┬───────┘
        │                   │
        └─────────┬─────────┘
                  │
                  ▼
          ┌───────────────┐
          │      AI       │
          │  (Strategy)   │
          └───────┬───────┘
                  │
                  ▼
          ┌───────────────┐
          │      UI       │
          │ (Menus, HUD)  │
          └───────────────┘
```

## Core Module

The foundation providing shared abstractions and utilities.

### IGameBridge
Central abstraction for all GTA V native function calls:

```csharp
public interface IGameBridge
{
    Vector3 GetPlayerPosition();
    int CreatePed(string modelName, Vector3 position);
    void DeletePed(int pedHandle);
    bool IsPedAlive(int pedHandle);
    void SetPedRelationshipGroup(int pedHandle, string groupName);
    int CreateBlip(Vector3 position);
    void DeleteBlip(int blipHandle);
    void SetBlipColor(int blipHandle, BlipColor color);
    void ShowNotification(string message);
    int GetGameTime();
    bool RevivePed(int pedHandle);
    void SetPedPosition(int pedHandle, Vector3 position);
    bool SetPedModel(int pedHandle, string modelName);
}
```

This interface enables:
- **Unit Testing** - MockGameBridge simulates GTA V behavior
- **Decoupling** - Business logic doesn't depend on GTA V directly
- **Portability** - Could theoretically support other game engines

### Vector3
Custom 3D vector implementation for coordinate handling:

```csharp
public struct Vector3
{
    public float X { get; }
    public float Y { get; }
    public float Z { get; }

    public float DistanceTo(Vector3 other);
    public float DistanceTo2D(Vector3 other);  // Ignores Z for map calculations
}
```

## Territory Module

Manages the zone system - the fundamental building blocks of territory control.

### Zone Model
```csharp
public class Zone
{
    public string Id { get; }
    public string Name { get; }
    public ZoneBoundary Boundary { get; }
    public string? OwnerFactionId { get; set; }
    public bool IsContested { get; set; }
    public float ControlPercentage { get; set; }
    public int StrategicValue { get; }
    public ZoneTrait Traits { get; }
}
```

### Zone Boundary Types
Zones support two boundary geometries:

1. **Circular** - Defined by center point and radius
2. **Polygon** - Defined by list of vertices

Point-in-polygon testing uses the ray casting algorithm for polygon boundaries.

### Zone Traits (Flags Enum)
```csharp
[Flags]
public enum ZoneTrait
{
    None = 0,
    Commercial = 1,      // Bonus cash generation
    Industrial = 2,      // Bonus weapons production
    Residential = 4,     // Bonus recruitment
    Port = 8,            // Smuggling bonuses
    Airport = 16,        // Transport bonuses
    Government = 32,     // Special objectives
    Entertainment = 64,  // Mixed bonuses
    Underground = 128    // Covert operation bonuses
}
```

### Adjacency System
Zone connectivity is calculated based on boundary proximity:
- Zones are adjacent if their boundaries intersect or are within a threshold distance
- Adjacency affects supply lines and reinforcement routing
- Graph-based algorithms for pathfinding between zones

## Factions Module

Manages the three competing factions and their states.

### Faction Types
```csharp
public enum FactionType
{
    Michael,   // Calculated, defense-focused
    Trevor,    // Aggressive, combat bonuses
    Franklin   // Opportunistic, balanced
}
```

Each faction type has associated bonuses:
- **Michael**: +20% defense, +15% income from commercial zones
- **Trevor**: +25% combat damage, +30% intimidation
- **Franklin**: +15% mobility, +20% opportunity detection

### Faction State
Tracks runtime state per faction:
```csharp
public class FactionState
{
    public string FactionId { get; }
    public int TroopCount { get; set; }
    public Dictionary<ResourceType, int> Resources { get; }
    public HashSet<string> OwnedZoneIds { get; }
    public float TotalControlPercentage { get; }
}
```

### Relationships
Factions have dynamic relationships:
```csharp
public enum RelationshipStatus
{
    Hostile,    // Active warfare
    Tense,      // Border skirmishes
    Neutral,    // No aggression
    Allied      // Cooperation (rare)
}
```

## Economy Module

Handles resource generation, storage, and supply lines.

### Resource Types
| Type | Primary Sources | Uses |
|------|-----------------|------|
| Cash | Commercial zones | Operations, bribes, purchases |
| Recruitment | Residential zones | Hiring troops |
| Weapons | Industrial zones, ports | Combat effectiveness |

### Generation Formula
```
BaseGeneration × ZoneTraitMultiplier × SupplyLineBonus × FactionBonus
```

### Supply Lines
Connected zones provide bonus resources:
- 100% base if connected to faction HQ
- 75% if connected through one intermediate zone
- 50% if connected through two+ intermediate zones
- 25% if disconnected (minimal production)

### Resource Tick Service
Resources generate on configurable intervals (default: 5 minutes real-time):
```csharp
public interface IResourceTickService
{
    void ProcessTick();
    TimeSpan TickInterval { get; set; }
    event EventHandler<ResourceTickEventArgs> OnTick;
}
```

## Combat Module

Manages ped spawning, combat encounters, and zone control transitions.

### Ped Pool System
Efficient ped management within GTA V's limits:

```csharp
public interface IPedPool
{
    PedHandle Acquire(string factionId);
    void Release(PedHandle ped);
    int ActiveCount { get; }
    int MaxCapacity { get; }  // ~30 for GTA V stability
}
```

Features:
- **Object pooling** - Reuse peds instead of create/destroy
- **Relationship groups** - Peds fight based on faction
- **Health tracking** - Dead peds returned to pool

### Combat Encounter
Tracks an active battle:
```csharp
public class CombatEncounter
{
    public string Id { get; }
    public string ZoneId { get; }
    public string AttackingFactionId { get; }
    public string DefendingFactionId { get; }
    public List<PedHandle> Attackers { get; }
    public List<PedHandle> Defenders { get; }
    public CombatStatus Status { get; }
    public DateTime StartTime { get; }
}
```

### Control Percentage
Real-time calculation based on:
- Surviving attacker/defender ratio
- Territory position (defenders have advantage)
- Reinforcement timing

### Takeover Detection
Zone ownership changes when:
1. Control percentage crosses threshold (default: 80%)
2. Sustained for minimum duration (default: 30 seconds)
3. Attacking faction maintains troop presence

## AI Module

Implements faction-specific decision making using the Strategy pattern.

### Strategy Interface
```csharp
public interface IAIStrategy
{
    FactionType FactionType { get; }
    AIDecision MakeDecision(AIContext context);
    float EvaluateZone(Zone zone, AIContext context);
    bool ShouldAttack(Zone zone, AIContext context);
    bool ShouldDefend(Zone zone, AIContext context);
}
```

### AI Context
Information provided to AI for decisions:
```csharp
public class AIContext
{
    public Faction Faction { get; }
    public FactionState FactionState { get; }
    public IReadOnlyList<Zone> AllZones { get; }
    public IReadOnlyList<Faction> AllFactions { get; }
    public IReadOnlyDictionary<string, float> ThreatLevels { get; }
}
```

### Strategy Implementations

**MichaelAIStrategy**
- Low aggressiveness (0.3)
- Low risk tolerance (0.3)
- High-value focus multiplier (1.5x)
- Defense priority boost (1.2x)

**TrevorAIStrategy**
- High aggressiveness (0.8)
- High risk tolerance (0.7)
- Combat damage bonus
- Attacks frequently, defends rarely

**FranklinAIStrategy**
- Medium aggressiveness (0.5)
- Medium risk tolerance (0.5)
- Opportunity detection bonus
- Balanced attack/defense

### Zone Evaluation
Scoring algorithm considers:
- Strategic value (0-100)
- Resource generation potential
- Defensive position (adjacencies)
- Current owner strength
- Distance from faction territory

## Persistence Module

Handles game state serialization and save management.

### GameState Model
```csharp
public class GameState
{
    public string Version { get; }
    public DateTime SaveTime { get; }
    public List<FactionData> Factions { get; }
    public List<ZoneData> Zones { get; }
    public List<RelationshipData> Relationships { get; }
    public Dictionary<string, object> Settings { get; }
}
```

### JSON Persistence
- Newtonsoft.Json for serialization
- Human-readable save files
- Schema versioning for upgrades

### Save Slots
Multiple save file support:
```csharp
public interface ISaveSlotManager
{
    IReadOnlyList<SaveSlotInfo> GetSlots();
    SaveSlotInfo CreateSlot(string name);
    void DeleteSlot(int slotIndex);
    void SaveToSlot(int slotIndex, GameState state);
    GameState LoadFromSlot(int slotIndex);
}
```

### Auto-Save
Configurable automatic saving:
- Interval-based (default: 10 minutes)
- Event-triggered (zone capture, major events)
- Rotating backup slots

## UI Module

NativeUI integration for menus and HUD elements.

### Menu Structure
```
Faction Wars Menu
├── Overview
│   ├── Territory Status
│   ├── Resource Summary
│   └── Faction Relations
├── Zone Management
│   ├── View Zones
│   ├── Zone Details
│   └── Attack/Defend Orders
├── Army
│   ├── Troop Count
│   ├── Recruitment
│   └── Deployment
└── Settings
    ├── Difficulty
    ├── Auto-Save
    └── Display Options
```

### Combat HUD
Real-time display during battles:
- Control percentage bar
- Reinforcement countdown timer
- Casualty counters
- Zone name/owner

### Notification System
Event-driven alerts:
- Zone captured
- Zone lost
- Attack incoming
- Reinforcements arrived
- Resource milestone

## Performance Module

Optimization utilities for stable gameplay.

### Object Pooling
Generic pooling for frequently created objects:
```csharp
public interface IObjectPool<T>
{
    T Get();
    void Return(T item);
    void Clear();
    int ActiveCount { get; }
    int PooledCount { get; }
}
```

### Caching
Time-based caching for expensive calculations:
```csharp
public interface ICacheService<TKey, TValue>
{
    void Set(TKey key, TValue value, TimeSpan? expiration = null);
    bool TryGet(TKey key, out TValue value);
    TValue GetOrAdd(TKey key, Func<TValue> factory);
    void Remove(TKey key);
}
```

### Lazy Loading
Deferred initialization for heavy resources:
```csharp
public interface ILazyLoader<T>
{
    T Value { get; }
    bool IsLoaded { get; }
    void Reset();
}
```

### Performance Monitoring
Optional metrics collection:
```csharp
public interface IPerformanceMonitor
{
    IDisposable Time(string operationName);
    PerformanceMetric GetMetric(string operationName);
    void Reset();
    bool IsEnabled { get; set; }
}
```

## Testing Strategy

### Unit Tests
- One test class per production class
- Moq for dependency mocking
- xUnit for assertions
- Naming: `MethodName_Scenario_ExpectedResult`

### Integration Tests
- End-to-end scenarios
- Full save/load cycles
- Combat-to-zone-state pipelines
- AI decision sequences

### Test Coverage
Target: 80% code coverage for non-UI code
Current: 2600+ tests passing
