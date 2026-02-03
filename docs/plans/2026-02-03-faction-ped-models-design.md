# Faction-Specific Ped Models Design

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Give each faction (Franklin, Trevor, Michael) distinct ped models that reflect their faction theme, with tier-based variations to denote rank.

**Architecture:** Create a centralized `FactionPedModels` class that maps faction IDs and defender tiers to GTA V ped model names. Managers query this at spawn time instead of using hardcoded model dictionaries.

**Tech Stack:** C#, existing DefenderTier enum, existing faction ID system

---

## Faction Themes

| Faction | Theme | Visual Style |
|---------|-------|--------------|
| **Franklin** | Street gangs (Families, Ballas) | Gangsters with casual street clothes |
| **Trevor** | Rural/Biker (Lost MC, rednecks) | Bikers, meth dealers, hillbillies |
| **Michael** | Professional (Merryweather, FIB) | Suited criminals, professional muscle |

## Tier Progression

| Tier | Franklin (Street) | Trevor (Rural/Biker) | Michael (Professional) |
|------|-------------------|---------------------|------------------------|
| **Basic** | Young street thug | Hillbilly | Armenian thug |
| **Medium** | Established gang member | Lost MC member | Blackops soldier |
| **Heavy** | OG/veteran gangster | Lost MC veteran | Blackops veteran |
| **Elite** | Ballas enforcer | Lost MC leader | High security |

## Data Model

New class `FactionPedModels` in `src/FactionWars/Combat/Models/`:

```csharp
public static class FactionPedModels
{
    private static readonly Dictionary<string, Dictionary<DefenderTier, string>> Models = new()
    {
        ["franklin"] = new()
        {
            [DefenderTier.Basic] = "g_m_y_famca_01",      // Families casual
            [DefenderTier.Medium] = "g_m_y_famdnf_01",   // Families DNF
            [DefenderTier.Heavy] = "g_m_y_famfor_01",    // Families OG
            [DefenderTier.Elite] = "g_m_y_ballasout_01"  // Ballas enforcer
        },
        ["trevor"] = new()
        {
            [DefenderTier.Basic] = "a_m_m_hillbilly_01", // Hillbilly
            [DefenderTier.Medium] = "g_m_y_lost_01",     // Lost MC member
            [DefenderTier.Heavy] = "g_m_y_lost_02",      // Lost MC veteran
            [DefenderTier.Elite] = "g_m_y_lost_03"       // Lost MC leader
        },
        ["michael"] = new()
        {
            [DefenderTier.Basic] = "g_m_m_armboss_01",   // Armenian thug
            [DefenderTier.Medium] = "s_m_y_blackops_01", // Blackops soldier
            [DefenderTier.Heavy] = "s_m_y_blackops_02", // Blackops veteran
            [DefenderTier.Elite] = "s_m_m_highsec_01"   // High security
        }
    };

    private const string FallbackModel = "a_m_m_business_01";

    public static string GetModel(string factionId, DefenderTier tier)
    {
        if (string.IsNullOrEmpty(factionId))
            return FallbackModel;

        if (!Models.TryGetValue(factionId.ToLowerInvariant(), out var tierModels))
            return FallbackModel;

        if (!tierModels.TryGetValue(tier, out var model))
            return FallbackModel;

        return model;
    }
}
```

## Manager Integration

### FriendlyDefenderManager

Remove hardcoded `_modelsByTier` dictionary. At spawn time:

```csharp
var playerFactionId = _playerContext.GetCurrentFactionId();
var model = FactionPedModels.GetModel(playerFactionId, tier);
```

### EnemyDefenderManager

Remove hardcoded `_modelsByTier` dictionary. At spawn time:

```csharp
var zoneOwnerFactionId = zone.OwningFactionId;
var model = FactionPedModels.GetModel(zoneOwnerFactionId, tier);
```

## Edge Cases

1. **Unknown faction ID** - Returns fallback generic model (`a_m_m_business_01`)
2. **Player switches character mid-battle** - Existing troops keep their models; new spawns use new faction's models
3. **AI vs AI battles** - No visible peds spawned, no impact

## Files to Change

| Action | File |
|--------|------|
| Create | `src/FactionWars/Combat/Models/FactionPedModels.cs` |
| Modify | `src/FactionWars/ScriptHookV/Managers/FriendlyDefenderManager.cs` |
| Modify | `src/FactionWars/ScriptHookV/Managers/EnemyDefenderManager.cs` |
| Create | `tests/FactionWars.Tests/Unit/Combat/FactionPedModelsTests.cs` |

## Testing

- Unit tests for `FactionPedModels.GetModel()` verifying correct models for each faction/tier
- Unit tests for fallback behavior with unknown faction
- Update existing manager tests to verify faction-aware model selection
