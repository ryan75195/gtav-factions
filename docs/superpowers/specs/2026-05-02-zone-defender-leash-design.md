# Zone Defender Leash — Design

## Goal

Defenders that are tied to a specific zone (friendly defenders of the
player's faction, enemy defenders of an AI faction) should stay near
that zone. Today they get a `TASK_WANDER_STANDARD` task whose radius
parameter is ignored by the native, so they can drift anywhere on the
map. They can also be pulled outside the zone by combat (for example,
chasing a hostile ped through GTA's relationship-group machinery).

The aim is two-fold:

1. **Idle wandering should respect the zone's radius natively.**
2. **Strays should get pulled back** even when something else (combat,
   panic) caused them to leave.

This makes territory feel like territory: when the player visits a zone
they should find their defenders *in* the zone, not three blocks away.

## Scope

In scope:
- Friendly defenders managed by `FriendlyDefenderManager`.
- Enemy defenders managed by `EnemyDefenderManager`.

Out of scope:
- **Battle attackers** (`BattleAttackerManager`). These are intentionally
  spawned at the perimeter (30%–100% of zone radius) as a "wave"
  pushing into the zone, so a leash would defeat their purpose.
- **Squad followers / commanders / rally targets.** They are tied to the
  player, not a zone.
- Any change to the existing 80% spawn radius (`SpawnRadiusFraction`).
  That is already correct.

## Mechanism

Two complementary parts. Both apply to friendly and enemy defenders;
neither applies to battle attackers.

### Part 1 — Bounded idle wander (native)

This part applies **only to friendly defenders**. Enemy defenders never
get a wander task today — they get `SetPedAsHostileWanderer` (a
relationship-group config) plus `TaskCombatHatedTargetsAroundPed`. So
swapping their wander native is not meaningful; the leash sweep in Part
2 is what keeps them in the zone.

For friendly defenders: replace the unbounded `TASK_WANDER_STANDARD`
call (currently inside `GameBridge.TaskPedWanderInArea`) with the
bounded native `TASK_WANDER_IN_AREA`, which natively keeps the ped
wandering inside `(center, radius)`.

Implementation note: the existing `TaskPedWanderInArea` is also used by
`BattleAttackerManager` (sprinting variant) where unbounded behaviour is
desired. To avoid disturbing that, a **new** native is added rather than
modifying the existing one:

```csharp
// IGameBridge
void TaskPedWanderInBoundedArea(int pedHandle, Vector3 center, float radius);
```

`FriendlyDefenderManager.SpawnDefender` will switch from
`TaskPedWanderInArea` to `TaskPedWanderInBoundedArea`.
`BattleAttackerManager`'s call is unchanged.

### Part 2 — Periodic leash check

Each manager runs a leash sweep every `LeashCheckIntervalSeconds = 2.0f`.
The sweep iterates currently-tracked defenders, asks for each ped's
position, and:

```
if Vector3.Distance(pedPos, zone.Center) > zone.Radius * 1.2:
    ClearPedTasks(ped)
    Vector3 returnPoint = randomPointWithin(zone.Center, zone.Radius * 0.5)
    TaskGoToCoord(ped, returnPoint)
```

The `1.2` multiplier is hysteresis — a ped legitimately walking near
the boundary won't yo-yo back and forth; only peds that are clearly out
get yanked. The return point is randomized inside the inner half of the
zone so multiple yanked peds don't pile up on the exact center.

A small **pure-logic helper** (`ZoneLeashEnforcer`) holds the threshold
math so it can be unit-tested without GameBridge plumbing:

```csharp
public static class ZoneLeashEnforcer
{
    public const float LeashThresholdMultiplier = 1.2f;
    public const float LeashReturnRadiusMultiplier = 0.5f;
    public const float LeashCheckIntervalSeconds = 2.0f;

    public static bool ShouldLeash(
        Vector3 pedPos, Vector3 zoneCenter, float zoneRadius);

    public static Vector3 PickReturnPoint(
        Vector3 zoneCenter, float zoneRadius, Random rng);
}
```

Each manager owns its own `_leashTimer` field and calls into the
enforcer from its existing `Update(float deltaSeconds)` loop:

```csharp
_leashTimer += deltaSeconds;
if (_leashTimer >= ZoneLeashEnforcer.LeashCheckIntervalSeconds) {
    _leashTimer = 0f;
    EnforceLeashOnTrackedDefenders();
}
```

## Files Touched

| File | Change |
|---|---|
| `src/FactionWars/Core/Interfaces/IGameBridge.cs` | Add `TaskPedWanderInBoundedArea` |
| `src/FactionWars/ScriptHookV/GameBridge.cs` | Implement using `Hash.TASK_WANDER_IN_AREA` (or raw native fallback `0xE054346CA3A0F315` if SHVDN3 enum lacks it) |
| `src/FactionWars/Core/Utils/MockGameBridge.cs` | Record the call (parity with existing wander tracking) |
| `src/FactionWars/ScriptHookV/Services/ZoneLeashEnforcer.cs` | NEW — pure-logic helper (constants + 2 static methods) |
| `src/FactionWars/ScriptHookV/Managers/FriendlyDefenderManager.cs` | Switch wander call; add leash timer + sweep |
| `src/FactionWars/ScriptHookV/Managers/EnemyDefenderManager.cs` | Add leash timer + sweep (no wander change since enemies use combat tasks, not wander) |
| `tests/FactionWars.Tests/Unit/ScriptHookV/Services/ZoneLeashEnforcerTests.cs` | NEW — pure-logic tests |
| `tests/FactionWars.Tests/Unit/ScriptHookV/FriendlyDefenderManagerLeashTests.cs` | NEW — leash sweep behaviour |
| `tests/FactionWars.Tests/Unit/ScriptHookV/EnemyDefenderManagerLeashTests.cs` | NEW — mirror for enemy side |
| `tests/FactionWars.Tests/Unit/Core/Utils/MockGameBridgeTests.cs` | Add a test for the new bounded-wander mock recording |

`BattleAttackerManager.cs` is intentionally **not** touched.

## Test Plan (TDD)

Each new test fails first. Watch it fail. Then implement.

**`ZoneLeashEnforcerTests`** — pure-logic, no mocks:
- Ped at `radius * 1.0` (boundary) → `ShouldLeash` returns false (hysteresis)
- Ped at `radius * 1.19` → false
- Ped at `radius * 1.21` → true
- `PickReturnPoint` returns a point whose distance from `zoneCenter` ≤ `zoneRadius * 0.5` (run for many seeds)
- `PickReturnPoint` is deterministic given the same `Random` instance

**`FriendlyDefenderManagerLeashTests`** — uses `MockGameBridge`:
- Spawn defender; place its mock position past `radius * 1.2`.
- Tick `Update(2.1f)` → mock records `ClearPedTasks` and `TaskGoToCoord` for that ped.
- Spawn defender; place position at `radius * 0.5` (well inside).
- Tick `Update(2.1f)` → no `ClearPedTasks` and no `TaskGoToCoord`.
- Place defender past threshold, tick `Update(1.9f)` (under cadence) → no retask. Tick another `Update(0.2f)` → retask fires.

**`EnemyDefenderManagerLeashTests`** — same three scenarios, mirrored.

**`MockGameBridgeTests`** — one new test: calling
`TaskPedWanderInBoundedArea` records the call with the right
`(pedHandle, center, radius)` triple, distinct from the existing
`TaskPedWanderInArea` recording.

## Constants

All three constants live as `public const float` on `ZoneLeashEnforcer`
so future tuning is one-file. No config plumbing in v1.

```csharp
LeashThresholdMultiplier      = 1.2f;
LeashReturnRadiusMultiplier   = 0.5f;
LeashCheckIntervalSeconds     = 2.0f;
```

## Risks

- **`Hash.TASK_WANDER_IN_AREA` may not be in the SHVDN3 enum.** Verify
  before coding; if absent, use `Function.Call(Hash) /* native id
  0xE054346CA3A0F315 */` form. Either way the IGameBridge surface stays
  the same.
- **Combat task overrides the bounded wander.** Expected — that is what
  Part 2 (the periodic leash) is for.
- **Yank-during-combat may feel jarring** if a defender is genuinely
  fighting an enemy that just stepped over the line. We accept this in
  v1; if it shows up in playtest, the v2 step is the "combat-aware"
  policy described as option C during brainstorming.
