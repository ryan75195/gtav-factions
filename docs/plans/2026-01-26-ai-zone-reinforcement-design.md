# AI Zone Reinforcement Design

**Goal:** Enable AI factions to deploy reserve troops to defend owned zones, using desperation scaling based on territorial control.

**Architecture:** Add `ExecuteDefendDecision()` to AIController that allocates reserves to zones when CapitalDeploymentService returns Defend decisions.

**Tech Stack:** C# .NET Framework 4.8, existing FactionWars AI and allocation systems.

---

## 1. Core Problem

AI factions recruit troops into reserves but never deploy them to defend zones:
- `AIRecruitmentService` adds troops to `FactionState.ReserveTroops`
- `CapitalDeploymentService.GetBestDecision()` returns Defend decisions with `Troops=0`
- `AIController` logs Defend decisions but takes no action
- Result: AI with 500 reserves loses zones defended by only 5 troops

---

## 2. Solution: Execute Defend Decisions

### When to Reinforce

On every Defend decision from `CapitalDeploymentService` (triggered when a zone has high defense priority due to being contested or adjacent to enemies).

### How Many Troops (Desperation Scaling)

Deploy percentage of reserves based on zones owned:

| Zones Owned | Deploy % | Rationale |
|-------------|----------|-----------|
| 1 zone | 80% | Last stand - survival mode |
| 2 zones | 50% | Significant threat |
| 3+ zones | 30% | Conservative - spread reserves |

### Which Tiers

All tiers proportionally. If reserves are:
- 100 Basic, 50 Medium, 20 Heavy

Then at 50% deployment:
- 50 Basic, 25 Medium, 10 Heavy

This preserves the faction's wealth-based tier distribution.

### Where

The zone specified in the Defend decision (highest defense priority from CapitalDeploymentService).

---

## 3. Implementation

### New Method: `AIController.ExecuteDefendDecision()`

```csharp
private void ExecuteDefendDecision(string factionId, AIDecision decision)
{
    if (decision.TargetZoneId == null) return;

    var state = _factionService.GetFactionState(factionId);
    if (state == null) return;

    // Desperation scaling
    float deployPercent = state.ZoneCount switch
    {
        1 => 0.80f,
        2 => 0.50f,
        _ => 0.30f
    };

    FileLogger.AI($"ExecuteDefend: {factionId} reinforcing {decision.TargetZoneId} ({state.ZoneCount} zones, {deployPercent:P0} deploy)");

    int totalDeployed = 0;
    foreach (var tier in new[] { DefenderTier.Basic, DefenderTier.Medium, DefenderTier.Heavy, DefenderTier.Elite })
    {
        int reserves = state.GetReserveTroops(tier);
        int toDeploy = (int)(reserves * deployPercent);

        if (toDeploy > 0 && _allocationService.AllocateTroops(state, decision.TargetZoneId, tier, toDeploy))
        {
            totalDeployed += toDeploy;
        }
    }

    FileLogger.AI($"ExecuteDefend: Allocated {totalDeployed} troops to {decision.TargetZoneId}");
}
```

### Modify `MakeDecisionForFaction()`

```csharp
foreach (var decision in decisions)
{
    if (decision.DecisionType == AIDecisionType.Attack)
    {
        ExecuteAttackDecision(factionId, decision);
    }
    else if (decision.DecisionType == AIDecisionType.Defend)
    {
        ExecuteDefendDecision(factionId, decision);
    }
}
```

---

## 4. Test Cases

| Test | Scenario | Expected |
|------|----------|----------|
| `ExecuteDefendDecision_WithOneZone_Deploys80Percent` | 1 zone, 100 Basic reserves | 80 Basic allocated |
| `ExecuteDefendDecision_WithTwoZones_Deploys50Percent` | 2 zones, 100 Basic reserves | 50 Basic allocated |
| `ExecuteDefendDecision_WithThreeZones_Deploys30Percent` | 3 zones, 100 Basic reserves | 30 Basic allocated |
| `ExecuteDefendDecision_DeploysAllTiersProportionally` | 100B/50M/20H, 50% | 50B/25M/10H allocated |
| `ExecuteDefendDecision_WithNoReserves_DoesNothing` | 0 reserves | No allocation calls |
| `ExecuteDefendDecision_WithNullZone_DoesNothing` | Null target zone | Early return |

---

## 5. Files Modified

| File | Changes |
|------|---------|
| `AIController.cs` | Add `ExecuteDefendDecision()`, call it for Defend decisions |
| `AIControllerTests.cs` | Add test cases above |

---

## 6. Success Criteria

- Michael with 1 zone and 500 reserves deploys 400 troops to defend
- AI zones have defenders proportional to faction's reserve strength
- Logs show reinforcement activity with troop counts
- All existing tests continue to pass
