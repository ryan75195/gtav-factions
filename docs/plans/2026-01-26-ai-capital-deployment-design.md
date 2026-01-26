# AI Capital Deployment Design

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Enable AI factions to intelligently deploy their cash and troop reserves based on contextual threat/opportunity assessment rather than rigid thresholds.

**Architecture:** Context-driven decision system that evaluates defense priorities and attack opportunities each cycle. Wealthy factions recruit faster and commit overwhelming force when attacking.

**Tech Stack:** C# .NET Framework 4.8, existing FactionWars AI and strategy systems.

---

## 1. Core Problem

AI factions accumulate massive cash reserves ($1.6M for Michael, $1M for Trevor) but:
- Only recruit 10 troops/cycle regardless of wealth
- Use rigid thresholds (Michael: 50% neutral, 70% enemy) that block expansion
- Commit conservative troop numbers to attacks (20-60% with reductions)
- Don't reinforce zones proportionally to threats

---

## 2. Threat & Opportunity Decision System

Each AI cycle, the faction evaluates whether to defend, attack, or hold.

### Defense Priority Score (per owned zone)

```csharp
float GetDefensePriority(Zone zone, AIContext context)
{
    float threatLevel = CalculateThreatLevel(zone, context);
    float zoneValue = zone.StrategicValue / MaxStrategicValue;
    int currentDefenders = GetDefenderCount(zone);

    // Higher priority when: high threat, high value, few defenders
    return threatLevel * zoneValue * (10f / Math.Max(1, currentDefenders));
}

float CalculateThreatLevel(Zone zone, AIContext context)
{
    if (zone.IsContested) return 2.0f;
    if (HasNearbyEnemyTroops(zone)) return 1.0f;
    if (HasAdjacentEnemyTerritory(zone)) return 0.5f;
    return 0.0f;
}
```

### Attack Opportunity Score (per target zone)

```csharp
float GetAttackOpportunity(Zone target, AIContext context)
{
    int ourTroops = context.FactionState.TroopCount;
    int enemyDefenders = GetDefenderCount(target);

    float winProbability = ourTroops / (float)(enemyDefenders * 2 + 1);
    float zoneValue = target.StrategicValue / MaxStrategicValue;
    float affordability = CanAffordAttack(ourTroops) ? 1.0f : 0.0f;

    return Math.Min(1f, winProbability) * zoneValue * affordability;
}
```

### Decision Flow

```
1. Calculate DefensePriority for each owned zone
2. Calculate AttackOpportunity for each adjacent target
3. If max(DefensePriority) > max(AttackOpportunity):
   → Reinforce highest priority zone
4. Else if max(AttackOpportunity) >= 0.7:
   → Attack best opportunity
5. Else:
   → Hold (build reserves)
```

---

## 3. Scaled Recruitment

Wealthy factions recruit faster to deploy their capital.

### Formula

```
MaxTroopsPerCycle = BaseRate + (Cash / RecruitScaleFactor)

Where:
  BaseRate = 10
  RecruitScaleFactor = $10,000 per additional troop slot

Capped at 50 troops/cycle
```

### Examples

| Cash | Calculation | Troops/Cycle |
|------|-------------|--------------|
| $10k | 10 + (10,000 / 10,000) = 11 | 11 |
| $100k | 10 + (100,000 / 10,000) = 20 | 20 |
| $500k | 10 + 50 → capped | 50 |
| $1.6M | 10 + 160 → capped | 50 |

---

## 4. Overwhelming Force Attacks

When attacking, commit enough troops to ensure victory.

### Attack Troop Calculation

```
AttackTroops = Max(
  EnemyDefenders × OverwhelmMultiplier,
  AvailableTroops × MinCommitPercent
)

Where:
  OverwhelmMultiplier = 3.0 (commit 3x enemy defenders)
  MinCommitPercent = 0.5 (commit at least 50% of available troops)
```

### Examples

| Our Troops | Enemy Defenders | Calculation | Attack With |
|------------|-----------------|-------------|-------------|
| 400 | 10 | Max(30, 200) | 200 |
| 400 | 100 | Max(300, 200) | 300 |
| 50 | 5 | Max(15, 25) | 25 |

### Win Probability Check

```
WinProbability = AttackTroops / (EnemyDefenders × 2)

Only attack if WinProbability >= 0.7 (70% confidence)
```

---

## 5. Removed: Rigid Attack Thresholds

The following thresholds in `MichaelAIStrategy.ShouldAttack()` are **removed**:

```csharp
// REMOVE: These block expansion when adjacent zones are low-value
if (zone.OwnerFactionId == null)
    return normalizedValue >= 0.5f;  // REMOVE
return normalizedValue >= 0.7f;      // REMOVE
```

**Replaced with:** Attack opportunity scoring that considers win probability, zone value, and affordability. High-value zones are still preferred (higher opportunity score) but low-value zones aren't blocked.

---

## 6. Implementation

### New Interface

```csharp
public interface ICapitalDeploymentService
{
    float GetDefensePriority(Zone zone, AIContext context);
    float GetAttackOpportunity(Zone target, AIContext context);
    int GetScaledRecruitmentMax(int cash);
    int GetOverwhelmingAttackForce(int availableTroops, int enemyDefenders);
    AIDecision? GetBestDecision(AIContext context);
}
```

### Modified Files

| File | Changes |
|------|---------|
| `BaseAIStrategy.cs` | Integrate CapitalDeploymentService for decisions |
| `MichaelAIStrategy.cs` | Remove rigid thresholds |
| `TrevorAIStrategy.cs` | Remove rigid thresholds |
| `FranklinAIStrategy.cs` | Remove rigid thresholds (if applicable) |
| `AIRecruitmentService.cs` | Add wealth-scaled max troops formula |
| `AIController.cs` | Use scaled recruitment max |

### New Files

| File | Purpose |
|------|---------|
| `src/FactionWars/AI/Interfaces/ICapitalDeploymentService.cs` | Interface |
| `src/FactionWars/AI/Services/CapitalDeploymentService.cs` | Implementation |
| `tests/.../CapitalDeploymentServiceTests.cs` | Unit tests |

---

## 7. Testing Strategy

### Unit Tests

- `GetDefensePriority`: contested zone > adjacent enemy > safe zone
- `GetAttackOpportunity`: scores correctly with troop ratios
- `GetScaledRecruitmentMax`: formula produces expected values, respects cap
- `GetOverwhelmingAttackForce`: 3x multiplier and 50% minimum work correctly
- `GetBestDecision`: chooses defend when defense priority > attack opportunity

### Integration Tests

- Wealthy AI with threatened zone → reinforces first
- Wealthy AI with safe territory → attacks aggressively
- Wealthy AI recruits at scaled rate (50/cycle at $1M+)
- AI commits overwhelming force (3x defenders)

---

## 8. Success Criteria

- Michael with $1.6M actively expands instead of hoarding
- AI reinforces threatened zones before attacking
- Wealthy factions recruit 50 troops/cycle
- Attacks use 3x defender count or 50% of army (whichever is larger)
- No rigid "must have X strategic value" thresholds blocking expansion
