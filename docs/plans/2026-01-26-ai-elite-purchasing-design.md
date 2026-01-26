# AI Elite Troop Purchasing Heuristics Design

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Enable AI factions to intelligently purchase and deploy Elite/RPG troops based on wealth and vehicle threats.

**Architecture:** Zone-driven purchasing with reactive deployment. AI assesses threats during battles and responds with appropriate anti-vehicle units. Wealthy factions proactively stock Elite reserves.

**Tech Stack:** C# .NET Framework 4.8, existing FactionWars AI and combat systems.

---

## 1. Vehicle Threat Classification

The AI identifies vehicles in combat zones and classifies their threat level.

### VehicleThreatLevel Enum

```csharp
public enum VehicleThreatLevel
{
    None,   // Bati, civilian cars - no RPG response
    Light,  // Technical, Zentorno - 1 RPG
    Heavy   // Insurgent, APC, Buzzard, Khanjali - 2 RPGs
}
```

### Vehicle Classification Table

| Model | Threat Level | RPG Response |
|-------|-------------|--------------|
| bati | None | 0 |
| technical | Light | 1 |
| zentorno | Light | 1 |
| insurgent | Heavy | 2 |
| apc | Heavy | 2 |
| buzzard | Heavy | 2 |
| khanjali | Heavy | 2 |

### Service Interface

```csharp
public interface IVehicleThreatService
{
    VehicleThreatLevel GetThreatLevel(string vehicleModelName);
    int GetRequiredRpgCount(VehicleThreatLevel threatLevel);
}
```

---

## 2. Reactive Elite Deployment During Battles

When enemy vehicles enter a zone during battle, the defending AI faction responds with RPG units.

### Detection Trigger

During active battles, scan for enemy vehicles in the combat zone. Integrate with existing `EnemyDefenderManager` or `BattleAttackerManager`.

### Response Flow

```
1. Vehicle detected in battle zone
2. Classify threat level → determine RPG count needed
3. Check faction's Elite reserve pool
4. If reserve has Elite units:
   → Deploy from reserve immediately
5. If reserve empty but faction has $2000+:
   → Emergency purchase Elite unit
   → Deploy immediately
6. If can't afford:
   → No RPG response (defenders use regular weapons)
```

### Service Interface

```csharp
public interface IAntiVehicleResponseService
{
    /// <summary>
    /// Called when vehicles are detected in a battle zone.
    /// Returns the number of Elite units deployed as response.
    /// </summary>
    int RespondToVehicleThreat(string factionId, string zoneId, VehicleThreatLevel threatLevel);
}
```

### Integration Point

Hook into existing battle detection - when attackers with vehicles are detected, notify the defending faction via `IAntiVehicleResponseService`.

---

## 3. Proactive Elite Stocking (Recruitment Cycle)

Outside of battles, wealthy AI factions build up Elite reserves during regular recruitment cycles.

### Elite Purchase Thresholds

| Cash Level | Elite Purchased Per Cycle |
|------------|--------------------------|
| Below $15k | 0 (focus on Basic/Medium/Heavy) |
| $15k - $30k | 1 Elite ($2000) |
| Above $30k | 2 Elite ($4000) |

### Wealth-Scaled Tier Distribution

| Cash Level | Basic | Medium | Heavy | Elite |
|------------|-------|--------|-------|-------|
| Below $5k | 100% | 0% | 0% | 0 |
| $5k - $15k | 60% | 30% | 10% | 0 |
| $15k - $30k | 40% | 30% | 20% | 1/cycle |
| Above $30k | 20% | 30% | 40% | 2/cycle |

### Recruitment Logic

1. First, check wealth and buy Elite units if thresholds met
2. Then, buy standard tiers (Basic/Medium/Heavy) with remaining budget
3. Distribution based on wealth level
4. Maintain existing max 10 troops per cycle limit

### Example at $35k Cash

1. Buy 2 Elite ($4000) → $31k remaining
2. Recruit standard troops with remaining budget
3. Distribution: 20% Basic, 30% Medium, 40% Heavy
4. If buying 5 troops: ~1 Basic, ~1-2 Medium, ~2 Heavy

---

## 4. Required Infrastructure Updates

### 4.1 ZoneDefenderAllocation - Add Elite Tier

Update constructor to include Elite:

```csharp
_troops = new Dictionary<DefenderTier, int>
{
    { DefenderTier.Basic, 0 },
    { DefenderTier.Medium, 0 },
    { DefenderTier.Heavy, 0 },
    { DefenderTier.Elite, 0 }  // Add this
};
```

Update `ToString()` to include Elite count.

### 4.2 FactionState Reserve Pool

Already supports Elite - uses `Dictionary<DefenderTier, int>` that dynamically handles any tier.

### 4.3 DefenderSpawnPlan

Verify spawn planning for battles can handle Elite tier defenders with RPG weapons.

### 4.4 Persistence

Update `ZoneDefenderAllocationData` to serialize Elite counts in save/load.

---

## 5. Implementation Order

1. **Infrastructure updates** - Elite in ZoneDefenderAllocation, persistence
2. **VehicleThreatService** - classify vehicle models
3. **Update AIRecruitmentService** - multi-tier recruitment with wealth scaling
4. **AntiVehicleResponseService** - reactive deployment logic
5. **Integration with battle managers** - hook vehicle detection

---

## 6. New Files

- `src/FactionWars/AI/Models/VehicleThreatLevel.cs`
- `src/FactionWars/AI/Services/VehicleThreatService.cs`
- `src/FactionWars/AI/Interfaces/IVehicleThreatService.cs`
- `src/FactionWars/AI/Services/AntiVehicleResponseService.cs`
- `src/FactionWars/AI/Interfaces/IAntiVehicleResponseService.cs`

## 7. Modified Files

- `src/FactionWars/Core/Models/ZoneDefenderAllocation.cs` - Add Elite tier
- `src/FactionWars/AI/Services/AIRecruitmentService.cs` - Multi-tier purchasing
- `src/FactionWars/AI/Services/AIBudgetService.cs` - Tier costs
- `src/FactionWars/Persistence/Models/ZoneDefenderAllocationData.cs` - Elite serialization
- Battle managers (TBD) - Hook vehicle detection

---

## 8. Testing Strategy

### Unit Tests

- `VehicleThreatServiceTests` - verify classification for all vehicle models
- `AntiVehicleResponseServiceTests` - verify reserve deployment, emergency purchase, can't afford scenarios
- `AIRecruitmentServiceTests` - verify wealth-scaled tier distribution

### Integration Tests

- AI faction builds Elite reserves when wealthy
- AI deploys RPG units when player attacks with vehicles
- AI emergency purchases Elite when reserves empty but has cash

---

## 9. Success Criteria

- AI factions naturally build diverse armies based on wealth
- Player bringing a tank triggers visible RPG defender response
- Rich factions have Elite reserves; poor factions rely on Basic troops
- No infinite RPG spam - limited by economy
