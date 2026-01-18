# API Reference

This document provides detailed API documentation for the GTA V Faction Wars mod.

## Table of Contents

1. [Core Interfaces](#core-interfaces)
2. [Territory System](#territory-system)
3. [Faction System](#faction-system)
4. [Economy System](#economy-system)
5. [Combat System](#combat-system)
6. [AI System](#ai-system)
7. [Persistence System](#persistence-system)
8. [UI System](#ui-system)
9. [Performance Utilities](#performance-utilities)

---

## Core Interfaces

### IGameBridge

Abstraction over GTA V native function calls. All game interactions go through this interface.

```csharp
namespace FactionWars.Core.Interfaces

public interface IGameBridge
{
    /// <summary>
    /// Gets the current player's position in world coordinates.
    /// </summary>
    Vector3 GetPlayerPosition();

    /// <summary>
    /// Creates a ped (pedestrian/NPC) at the specified position.
    /// </summary>
    /// <param name="modelName">The model name or hash of the ped.</param>
    /// <param name="position">World position to spawn the ped.</param>
    /// <returns>Handle to the created ped, or -1 if creation failed.</returns>
    int CreatePed(string modelName, Vector3 position);

    /// <summary>
    /// Deletes a ped from the world.
    /// </summary>
    void DeletePed(int pedHandle);

    /// <summary>
    /// Checks if a ped is still alive.
    /// </summary>
    bool IsPedAlive(int pedHandle);

    /// <summary>
    /// Sets the relationship group for a ped, affecting combat behavior.
    /// </summary>
    void SetPedRelationshipGroup(int pedHandle, string groupName);

    /// <summary>
    /// Creates a blip on the map at the specified position.
    /// </summary>
    int CreateBlip(Vector3 position);

    /// <summary>
    /// Deletes a blip from the map.
    /// </summary>
    void DeleteBlip(int blipHandle);

    /// <summary>
    /// Sets the color of a blip.
    /// </summary>
    void SetBlipColor(int blipHandle, BlipColor color);

    /// <summary>
    /// Shows a notification message to the player.
    /// </summary>
    void ShowNotification(string message);

    /// <summary>
    /// Gets the current game time in milliseconds.
    /// </summary>
    int GetGameTime();

    /// <summary>
    /// Revives a dead ped, restoring them to full health.
    /// </summary>
    bool RevivePed(int pedHandle);

    /// <summary>
    /// Teleports a ped to a new position.
    /// </summary>
    void SetPedPosition(int pedHandle, Vector3 position);

    /// <summary>
    /// Changes the model/appearance of a ped.
    /// </summary>
    bool SetPedModel(int pedHandle, string modelName);
}
```

### ITimeProvider

Abstraction for time-based operations.

```csharp
namespace FactionWars.Core.Interfaces

public interface ITimeProvider
{
    /// <summary>
    /// Gets the current UTC time.
    /// </summary>
    DateTime UtcNow { get; }

    /// <summary>
    /// Gets the elapsed time since the game started.
    /// </summary>
    TimeSpan ElapsedGameTime { get; }
}
```

---

## Territory System

### Zone Model

```csharp
namespace FactionWars.Territory.Models

public class Zone
{
    // Properties
    string Id { get; }                    // Unique identifier
    string Name { get; }                  // Display name
    ZoneBoundary Boundary { get; }        // Geographic boundary
    string? OwnerFactionId { get; set; }  // Current owner (null = neutral)
    bool IsContested { get; set; }        // Currently in combat
    float ControlPercentage { get; set; } // 0-100 during transitions
    int StrategicValue { get; }           // 0-100 importance rating
    ZoneTrait Traits { get; }             // Zone characteristics
}
```

### ZoneBoundary

```csharp
namespace FactionWars.Territory.Models

public class ZoneBoundary
{
    // Properties
    BoundaryType Type { get; }            // Circular or Polygon
    Vector3 Center { get; }               // Center point
    float BoundingRadius { get; }         // Outer radius
    IReadOnlyList<Vector3> Vertices { get; } // Polygon vertices (empty for circular)

    // Factory Methods
    static ZoneBoundary CreateCircular(Vector3 center, float radius);
    static ZoneBoundary CreatePolygon(IEnumerable<Vector3> vertices);

    // Methods
    bool Contains(Vector3 point);         // Point-in-boundary test (2D)
}
```

### IZoneRepository

```csharp
namespace FactionWars.Territory.Interfaces

public interface IZoneRepository
{
    Zone? GetById(string id);
    IReadOnlyList<Zone> GetAll();
    IReadOnlyList<Zone> GetByFaction(string factionId);
    IReadOnlyList<Zone> GetNeutralZones();
    IReadOnlyList<Zone> GetContestedZones();
    void Add(Zone zone);
    void Update(Zone zone);
    void Remove(string id);
    bool Exists(string id);
}
```

### IZoneService

```csharp
namespace FactionWars.Territory.Interfaces

public interface IZoneService
{
    // Queries
    Zone? GetZone(string id);
    IReadOnlyList<Zone> GetAllZones();
    IReadOnlyList<Zone> GetZonesByFaction(string factionId);
    Zone? GetZoneAtPosition(Vector3 position);
    IReadOnlyList<Zone> GetAdjacentZones(string zoneId);
    bool AreZonesAdjacent(string zoneId1, string zoneId2);

    // Operations
    void UpdateZoneOwner(string zoneId, string? factionId);
    void SetZoneContested(string zoneId, bool contested);
    void UpdateControlPercentage(string zoneId, float percentage);

    // Events
    event EventHandler<ZoneOwnerChangedEventArgs> OnZoneOwnerChanged;
    event EventHandler<ZoneContestedEventArgs> OnZoneContested;
}
```

### ZoneTrait Enum

```csharp
namespace FactionWars.Territory.Models

[Flags]
public enum ZoneTrait
{
    None = 0,
    Commercial = 1,      // +50% cash generation
    Industrial = 2,      // +50% weapons production
    Residential = 4,     // +50% recruitment points
    Port = 8,            // Smuggling operations available
    Airport = 16,        // Fast troop deployment
    Government = 32,     // Special objectives
    Entertainment = 64,  // Mixed resource bonuses
    Underground = 128    // Covert operations
}
```

---

## Faction System

### Faction Model

```csharp
namespace FactionWars.Factions.Models

public class Faction : IEquatable<Faction>
{
    string Id { get; }              // Unique identifier
    string Name { get; }            // Display name
    string? Leader { get; }         // Leader name (e.g., "Michael De Santa")
    string Description { get; }     // Faction description
    FactionColor Color { get; }     // Map/UI color
    bool IsActive { get; set; }     // Participates in gameplay
}
```

### FactionType Enum

```csharp
namespace FactionWars.Factions.Models

public enum FactionType
{
    Michael,    // Calculated, defense-focused
    Trevor,     // Aggressive, combat bonuses
    Franklin    // Opportunistic, balanced
}
```

### FactionState

```csharp
namespace FactionWars.Factions.Models

public class FactionState
{
    string FactionId { get; }
    int TroopCount { get; set; }
    Dictionary<ResourceType, int> Resources { get; }
    HashSet<string> OwnedZoneIds { get; }
    float TotalControlPercentage { get; }  // Calculated

    void AddResource(ResourceType type, int amount);
    void RemoveResource(ResourceType type, int amount);
    bool HasResource(ResourceType type, int amount);
    void AddZone(string zoneId);
    void RemoveZone(string zoneId);
}
```

### IFactionRepository

```csharp
namespace FactionWars.Factions.Interfaces

public interface IFactionRepository
{
    Faction? GetById(string id);
    IReadOnlyList<Faction> GetAll();
    IReadOnlyList<Faction> GetActive();
    void Add(Faction faction);
    void Update(Faction faction);
    void Remove(string id);
    bool Exists(string id);
}
```

### IFactionService

```csharp
namespace FactionWars.Factions.Interfaces

public interface IFactionService
{
    // Queries
    Faction? GetFaction(string factionId);
    IReadOnlyList<Faction> GetAllFactions();
    FactionState GetFactionState(string factionId);
    FactionType GetFactionType(string factionId);

    // Resource operations
    void AddResources(string factionId, ResourceType type, int amount);
    void RemoveResources(string factionId, ResourceType type, int amount);
    bool HasResources(string factionId, ResourceType type, int amount);

    // Troop operations
    void ModifyTroops(string factionId, int delta);
    int GetTroopCount(string factionId);

    // Events
    event EventHandler<ResourceChangedEventArgs> OnResourceChanged;
    event EventHandler<TroopCountChangedEventArgs> OnTroopCountChanged;
}
```

### IFactionRelationshipService

```csharp
namespace FactionWars.Factions.Interfaces

public interface IFactionRelationshipService
{
    RelationshipStatus GetRelationship(string factionId1, string factionId2);
    void SetRelationship(string factionId1, string factionId2, RelationshipStatus status);
    IReadOnlyList<string> GetHostileFactions(string factionId);
    IReadOnlyList<string> GetAlliedFactions(string factionId);
    bool AreHostile(string factionId1, string factionId2);
}
```

---

## Economy System

### ResourceType Enum

```csharp
namespace FactionWars.Economy.Models

public enum ResourceType
{
    Cash = 0,        // Currency for operations
    Recruitment = 1, // Hiring troops
    Weapons = 2      // Military stockpile
}
```

### IResourceStorage

```csharp
namespace FactionWars.Economy.Interfaces

public interface IResourceStorage
{
    int GetAmount(string factionId, ResourceType type);
    void SetAmount(string factionId, ResourceType type, int amount);
    void Add(string factionId, ResourceType type, int amount);
    bool TryRemove(string factionId, ResourceType type, int amount);
    int GetCap(string factionId, ResourceType type);
    void SetCap(string factionId, ResourceType type, int cap);
}
```

### IResourceTickService

```csharp
namespace FactionWars.Economy.Interfaces

public interface IResourceTickService
{
    TimeSpan TickInterval { get; set; }
    bool IsRunning { get; }

    void Start();
    void Stop();
    void ProcessTick();          // Manual tick trigger

    event EventHandler<ResourceTickEventArgs> OnTick;
}
```

### ISupplyLineService

```csharp
namespace FactionWars.Economy.Interfaces

public interface ISupplyLineService
{
    /// <summary>
    /// Calculates the supply line efficiency for a zone (0.0 to 1.0).
    /// </summary>
    float GetSupplyLineEfficiency(string factionId, string zoneId);

    /// <summary>
    /// Gets all zones in the supply chain from HQ to target zone.
    /// </summary>
    IReadOnlyList<string> GetSupplyRoute(string factionId, string targetZoneId);

    /// <summary>
    /// Checks if a zone has a valid supply connection.
    /// </summary>
    bool IsConnected(string factionId, string zoneId);
}
```

### IZoneTraitResourceModifier

```csharp
namespace FactionWars.Economy.Interfaces

public interface IZoneTraitResourceModifier
{
    /// <summary>
    /// Calculates resource generation modifier based on zone traits.
    /// </summary>
    float GetModifier(ZoneTrait traits, ResourceType resourceType);

    /// <summary>
    /// Gets the base generation rate for a resource type.
    /// </summary>
    int GetBaseGenerationRate(ResourceType resourceType);
}
```

---

## Combat System

### PedHandle

```csharp
namespace FactionWars.Combat.Models

public class PedHandle
{
    int Handle { get; }               // GTA V ped handle
    string FactionId { get; }         // Owning faction
    string? ZoneId { get; set; }      // Current zone assignment
    bool IsAlive { get; }             // Alive status (via IGameBridge)
    Vector3 Position { get; }         // Current position
    DateTime SpawnTime { get; }       // When spawned
}
```

### IPedPool

```csharp
namespace FactionWars.Combat.Interfaces

public interface IPedPool
{
    PedHandle? Acquire(string factionId, Vector3 position);
    void Release(PedHandle ped);
    void ReleaseAll();

    int ActiveCount { get; }
    int PooledCount { get; }
    int MaxCapacity { get; }          // Default: 30

    IReadOnlyList<PedHandle> GetActivePeds();
    IReadOnlyList<PedHandle> GetPedsByFaction(string factionId);
}
```

### CombatEncounter

```csharp
namespace FactionWars.Combat.Models

public class CombatEncounter
{
    string Id { get; }
    string ZoneId { get; }
    string AttackingFactionId { get; }
    string DefendingFactionId { get; }
    List<PedHandle> Attackers { get; }
    List<PedHandle> Defenders { get; }
    CombatStatus Status { get; set; }
    DateTime StartTime { get; }
    DateTime? EndTime { get; set; }

    int AttackerCasualties { get; }
    int DefenderCasualties { get; }
}
```

### IControlPercentageCalculator

```csharp
namespace FactionWars.Combat.Interfaces

public interface IControlPercentageCalculator
{
    /// <summary>
    /// Calculates current control percentage for an encounter.
    /// </summary>
    ControlPercentageResult Calculate(CombatEncounter encounter);
}

public class ControlPercentageResult
{
    float AttackerPercentage { get; }   // 0-100
    float DefenderPercentage { get; }   // 0-100
    string DominantFactionId { get; }
}
```

### ITakeoverDetector

```csharp
namespace FactionWars.Combat.Interfaces

public interface ITakeoverDetector
{
    /// <summary>
    /// Checks if a zone takeover condition has been met.
    /// </summary>
    TakeoverResult CheckTakeover(CombatEncounter encounter, float controlPercentage);
}

public class TakeoverResult
{
    TakeoverStatus Status { get; }      // InProgress, Successful, Failed
    string? NewOwnerId { get; }
    string Reason { get; }
}
```

### IReinforcementService

```csharp
namespace FactionWars.Combat.Interfaces

public interface IReinforcementService
{
    /// <summary>
    /// Requests reinforcements for an ongoing encounter.
    /// </summary>
    ReinforcementResult RequestReinforcements(ReinforcementRequest request);

    /// <summary>
    /// Gets cooldown remaining before next reinforcement.
    /// </summary>
    TimeSpan GetCooldownRemaining(string encounterId, string factionId);
}
```

### ICombatResultHandler

```csharp
namespace FactionWars.Combat.Interfaces

public interface ICombatResultHandler
{
    /// <summary>
    /// Processes the outcome of a combat encounter.
    /// </summary>
    CombatProcessingResult ProcessCombatResult(CombatEncounter encounter);

    /// <summary>
    /// Handles zone state changes after combat.
    /// </summary>
    void ApplyZoneStateChanges(CombatProcessingResult result);
}
```

---

## AI System

### IAIStrategy

```csharp
namespace FactionWars.AI.Interfaces

public interface IAIStrategy
{
    FactionType FactionType { get; }
    float Aggressiveness { get; }      // 0.0 to 1.0
    float RiskTolerance { get; }       // 0.0 to 1.0

    /// <summary>
    /// Makes a strategic decision based on current context.
    /// </summary>
    AIDecision MakeDecision(AIContext context);

    /// <summary>
    /// Evaluates a zone's attractiveness as a target.
    /// </summary>
    float EvaluateZone(Zone zone, AIContext context);

    /// <summary>
    /// Determines whether to attack a specific zone.
    /// </summary>
    bool ShouldAttack(Zone zone, AIContext context);

    /// <summary>
    /// Determines whether to defend a specific zone.
    /// </summary>
    bool ShouldDefend(Zone zone, AIContext context);
}
```

### AIContext

```csharp
namespace FactionWars.AI.Models

public class AIContext
{
    Faction Faction { get; }
    FactionState FactionState { get; }
    IReadOnlyList<Zone> AllZones { get; }
    IReadOnlyList<Zone> OwnedZones { get; }
    IReadOnlyList<Zone> AdjacentEnemyZones { get; }
    IReadOnlyList<Faction> AllFactions { get; }
    IReadOnlyDictionary<string, float> ThreatLevels { get; }
    float DifficultyMultiplier { get; }
}
```

### AIDecision

```csharp
namespace FactionWars.AI.Models

public class AIDecision
{
    AIDecisionType Type { get; }       // Attack, Defend, Fortify, Wait
    string? TargetZoneId { get; }
    int TroopCommitment { get; }
    float Priority { get; }            // 0.0 to 1.0
    string Reasoning { get; }          // Debug info
}

public enum AIDecisionType
{
    Wait,       // No action
    Attack,     // Launch attack on target zone
    Defend,     // Reinforce defense
    Fortify,    // Build up zone defenses
    Retreat     // Pull back from zone
}
```

### IZoneEvaluationService

```csharp
namespace FactionWars.AI.Interfaces

public interface IZoneEvaluationService
{
    /// <summary>
    /// Scores a zone for AI targeting (0.0 to 1.0).
    /// </summary>
    float EvaluateZone(Zone zone, FactionState evaluatorState, IAIStrategy strategy);

    /// <summary>
    /// Ranks all potential target zones.
    /// </summary>
    IReadOnlyList<(Zone Zone, float Score)> RankTargets(
        IEnumerable<Zone> zones,
        FactionState evaluatorState,
        IAIStrategy strategy);
}
```

### IResourceAllocationService

```csharp
namespace FactionWars.AI.Interfaces

public interface IResourceAllocationService
{
    /// <summary>
    /// Calculates optimal resource split between attack and defense.
    /// </summary>
    ResourceAllocation CalculateAllocation(AIContext context, IAIStrategy strategy);
}

public class ResourceAllocation
{
    int TroopsForAttack { get; }
    int TroopsForDefense { get; }
    int TroopsInReserve { get; }
    Dictionary<ResourceType, int> ResourcesForAttack { get; }
    Dictionary<ResourceType, int> ResourcesForDefense { get; }
}
```

### IAggressionResponseService

```csharp
namespace FactionWars.AI.Interfaces

public interface IAggressionResponseService
{
    /// <summary>
    /// Records aggression from one faction to another.
    /// </summary>
    void RecordAggression(string aggressorId, string targetId, float severity);

    /// <summary>
    /// Gets the AI's response to recent aggression.
    /// </summary>
    AIDecision GetAggressionResponse(string factionId, AIContext context);

    /// <summary>
    /// Gets accumulated threat level from a faction.
    /// </summary>
    float GetThreatLevel(string targetId, string fromFactionId);
}
```

### IAIDifficultyService

```csharp
namespace FactionWars.AI.Interfaces

public interface IAIDifficultyService
{
    AIDifficulty CurrentDifficulty { get; set; }

    /// <summary>
    /// Gets resource generation multiplier for AI factions.
    /// </summary>
    float GetResourceMultiplier();

    /// <summary>
    /// Gets decision delay (simulates thinking time, lower = harder).
    /// </summary>
    TimeSpan GetDecisionDelay();

    /// <summary>
    /// Gets mistake probability (higher = easier).
    /// </summary>
    float GetMistakeProbability();
}

public enum AIDifficulty
{
    Easy,       // +50% mistake rate, slower decisions
    Normal,     // Balanced
    Hard        // No mistakes, instant decisions, resource bonus
}
```

---

## Persistence System

### GameState

```csharp
namespace FactionWars.Persistence.Models

public class GameState
{
    string Version { get; }
    DateTime SaveTime { get; }
    List<FactionData> Factions { get; }
    List<ZoneData> Zones { get; }
    List<RelationshipData> Relationships { get; }
    Dictionary<string, object> Settings { get; }
}
```

### IPersistenceService

```csharp
namespace FactionWars.Core.Interfaces

public interface IPersistenceService
{
    /// <summary>
    /// Saves game state to a file.
    /// </summary>
    void Save(GameState state, string filePath);

    /// <summary>
    /// Loads game state from a file.
    /// </summary>
    GameState Load(string filePath);

    /// <summary>
    /// Checks if a save file exists.
    /// </summary>
    bool Exists(string filePath);
}
```

### ISaveSlotManager

```csharp
namespace FactionWars.Core.Interfaces

public interface ISaveSlotManager
{
    int MaxSlots { get; }

    IReadOnlyList<SaveSlotInfo> GetSlots();
    SaveSlotInfo? GetSlot(int index);
    SaveSlotInfo CreateSlot(string name);
    void DeleteSlot(int index);

    void SaveToSlot(int index, GameState state);
    GameState? LoadFromSlot(int index);

    bool IsSlotEmpty(int index);
}

public class SaveSlotInfo
{
    int Index { get; }
    string Name { get; }
    string FilePath { get; }
    DateTime? LastSaveTime { get; }
    bool IsEmpty { get; }
}
```

### ISaveFileValidator

```csharp
namespace FactionWars.Core.Interfaces

public interface ISaveFileValidator
{
    /// <summary>
    /// Validates a save file for integrity and compatibility.
    /// </summary>
    SaveFileValidationResult Validate(string filePath);
}

public class SaveFileValidationResult
{
    bool IsValid { get; }
    string? ErrorMessage { get; }
    string? DetectedVersion { get; }
    bool RequiresMigration { get; }
}
```

### IAutoSaveService

```csharp
namespace FactionWars.Core.Interfaces

public interface IAutoSaveService
{
    bool IsEnabled { get; set; }
    TimeSpan Interval { get; set; }
    int MaxBackups { get; set; }

    void Start();
    void Stop();
    void TriggerSave();          // Manual trigger

    event EventHandler<AutoSaveEventArgs> OnAutoSave;
}
```

---

## UI System

### IMapBlipService

```csharp
namespace FactionWars.UI.Interfaces

public interface IMapBlipService
{
    void CreateZoneBlip(Zone zone);
    void UpdateZoneBlip(Zone zone);
    void RemoveZoneBlip(string zoneId);
    void RefreshAllBlips();
    void SetBlipVisibility(string zoneId, bool visible);
}
```

### INotificationService

```csharp
namespace FactionWars.UI.Interfaces

public interface INotificationService
{
    void Show(string message);
    void Show(string title, string message);
    void ShowSuccess(string message);
    void ShowWarning(string message);
    void ShowError(string message);

    void QueueNotification(string message, TimeSpan delay);
    void ClearQueue();
}
```

### ICombatHudService

```csharp
namespace FactionWars.UI.Interfaces

public interface ICombatHudService
{
    void Show(CombatEncounter encounter);
    void Hide();
    void Update(CombatEncounter encounter, float controlPercentage);
    bool IsVisible { get; }
}
```

### IEventAlertService

```csharp
namespace FactionWars.UI.Interfaces

public interface IEventAlertService
{
    void AlertZoneCaptured(Zone zone, Faction newOwner);
    void AlertZoneLost(Zone zone, Faction lostBy);
    void AlertAttackIncoming(Zone zone, Faction attacker);
    void AlertReinforcementsArrived(Zone zone, int count);
    void AlertResourceMilestone(ResourceType type, int amount);
}
```

---

## Performance Utilities

### IObjectPool<T>

```csharp
namespace FactionWars.Performance.Interfaces

public interface IObjectPool<T> where T : class
{
    /// <summary>
    /// Gets an object from the pool or creates a new one.
    /// </summary>
    T Get();

    /// <summary>
    /// Returns an object to the pool.
    /// </summary>
    void Return(T item);

    /// <summary>
    /// Clears all pooled objects.
    /// </summary>
    void Clear();

    int ActiveCount { get; }
    int PooledCount { get; }
    int MaxSize { get; }

    // Statistics
    long HitCount { get; }    // Retrieved from pool
    long MissCount { get; }   // Had to create new
}
```

### ICacheService<TKey, TValue>

```csharp
namespace FactionWars.Performance.Interfaces

public interface ICacheService<TKey, TValue>
{
    void Set(TKey key, TValue value, TimeSpan? expiration = null);
    bool TryGet(TKey key, out TValue value);
    TValue GetOrAdd(TKey key, Func<TValue> factory, TimeSpan? expiration = null);
    void Remove(TKey key);
    void Clear();
    bool Contains(TKey key);

    int Count { get; }
    TimeSpan DefaultExpiration { get; set; }
}
```

### ILazyLoader<T>

```csharp
namespace FactionWars.Performance.Interfaces

public interface ILazyLoader<T>
{
    /// <summary>
    /// Gets the lazily loaded value, initializing if necessary.
    /// </summary>
    T Value { get; }

    /// <summary>
    /// Whether the value has been loaded.
    /// </summary>
    bool IsLoaded { get; }

    /// <summary>
    /// Resets the loader, causing next access to reinitialize.
    /// </summary>
    void Reset();

    /// <summary>
    /// Gets value if loaded, otherwise returns default.
    /// </summary>
    T? GetValueOrDefault();

    /// <summary>
    /// Tries to get the value without triggering initialization.
    /// </summary>
    bool TryGetValue(out T? value);
}
```

### IPerformanceMonitor

```csharp
namespace FactionWars.Performance.Interfaces

public interface IPerformanceMonitor
{
    bool IsEnabled { get; set; }

    /// <summary>
    /// Times an operation. Dispose the result to stop timing.
    /// </summary>
    IDisposable Time(string operationName);

    /// <summary>
    /// Gets metrics for a specific operation.
    /// </summary>
    PerformanceMetric? GetMetric(string operationName);

    /// <summary>
    /// Gets all recorded metrics.
    /// </summary>
    IReadOnlyDictionary<string, PerformanceMetric> GetAllMetrics();

    /// <summary>
    /// Resets all metrics.
    /// </summary>
    void Reset();
}

public class PerformanceMetric
{
    string OperationName { get; }
    long CallCount { get; }
    double TotalMilliseconds { get; }
    double AverageMilliseconds { get; }
    double MinMilliseconds { get; }
    double MaxMilliseconds { get; }
}
```
