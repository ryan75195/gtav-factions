# Defender Rally on Threat — Design

## Goal

When the player is in a zone owned by their faction and aggressive NPCs are
attacking them, the defenders allocated to that zone should abandon their
wander pattern, run to the player, and stay within a tight radius engaging
threats. When the threat clears, defenders return to wandering.

This makes player-owned territory feel meaningfully defended and turns
faction-owned zones into safe havens during combat.

## Scope

In scope:
- Player-faction defenders in the **player's current zone only**.
- Threats from police, mod-managed combat, or any ped that has damaged the
  player.

Out of scope (future work):
- AI factions defending their own zones (AI factions don't have a "player"
  centerpiece to rally around).
- Defenders in adjacent zones rushing to help (cross-zone reinforcement).
- Vehicle-aware go-to behavior — defenders pathing through traffic relies on
  GTA's built-in pathing.

## Threat-detection signal

A defender is considered "under attack" if any of the following is true on
the current tick:

| Source | Bridge call |
|---|---|
| Player has a wanted level | `GetWantedLevel() > 0` |
| Mod-managed combat encounter is active | `CombatManager.HasActiveEncounter` |
| Any ped has damaged the player since last check | `ConsumePlayerDamagedByPedFlag()` |

Each individual signal is cheap (single GTA native or mod state read), so
the composite signal is evaluated every tick with no per-ped scanning.

`ConsumePlayerDamagedByPedFlag` reads the engine-set
`HasBeenDamagedByAnyPed` boolean and clears it. This is the only "event-ish"
signal — the engine sets the flag asynchronously when damage occurs, we
consume it on tick.

## Cool-down

Once `isUnderAttackNow` is true, the controller holds `isUnderAttack = true`
for 5 seconds (`UnderAttackCoolDownMs = 5000`). This prevents defender
behavior from oscillating between firefight bursts.

```
on tick:
    if (isUnderAttackNow) underAttackUntilTickMs = now + UnderAttackCoolDownMs
    isUnderAttack = now < underAttackUntilTickMs
```

## State machine and tasking

The controller tracks `wasUnderAttack` from the previous tick. Tasks are
issued **on transitions only**, never re-issued every tick — re-tasking each
frame causes GTA peds to reset their animations and looks twitchy.

### `false → true` (rally)

For each defender ped in `player.CurrentZone` whose zone owner =
`CurrentPlayerFactionId`:

1. `ClearPedTasks(handle)` — wipe the wander task.
2. `TaskGoToEntity(handle, playerHandle, stoppingRange = RallyStoppingRangeM)`
   — sprint toward the player, stop at 8m.
3. `TaskCombatHatedTargetsAroundPed(handle, radius = RallyCombatRadiusM)` —
   stays stacked on the previous task; while engaging hated targets, the
   defender remains within ~12m of the player.

Constants:
- `RallyStoppingRangeM = 8.0f`
- `RallyCombatRadiusM = 12.0f`

### `true → false` (stand down)

For each defender currently rallied: re-issue
`TaskPedWanderInArea(handle, zoneCenter, zoneRadius)` to send them back to
patrol.

### `false → false` and `true → true`

No-op. The controller does not re-task during steady state.

## Components

### `DefenderRallyController` (new)

```
public sealed class DefenderRallyController
{
    public DefenderRallyController(
        IGameBridge bridge,
        ITerritoryEvents territory,    // for player current zone
        IFriendlyDefenderQuery defenders, // see below
        ICombatActivityQuery combat,   // see below
        Func<string?> currentPlayerFactionIdAccessor,
        Func<long> nowMs);             // injectable clock for tests

    public void Update();              // called once per tick from GameLoopController
}
```

Owns:
- `long _underAttackUntilTickMs`
- `bool _wasUnderAttack`
- `HashSet<int> _rallyingPeds` — peds we tasked on the most recent rally,
  used so we know who to send back to wander.

### Small interfaces for testability

- `IFriendlyDefenderQuery` — exposes `IReadOnlyDictionary<int, DefenderTier>
  GetDefendersInZone(string zoneId)`. Implemented by `FriendlyDefenderManager`
  (a thin accessor over `_spawnedPedTierByZone`).
- `ICombatActivityQuery` — exposes `bool HasActiveEncounter { get; }`.
  Implemented by `CombatManager` (single-line property).

These keep `DefenderRallyController` decoupled from concrete classes, which
matters more for unit-testability than for runtime architecture.

### Bridge additions (`IGameBridge`)

| Member | Purpose |
|---|---|
| `int GetWantedLevel()` | `Game.Player.WantedLevel` |
| `bool ConsumePlayerDamagedByPedFlag()` | Read-and-clear `HasBeenDamagedByAnyPed` |
| `void TaskGoToEntity(int pedHandle, int targetEntityHandle, float stoppingRange)` | Wraps `TASK_GO_TO_ENTITY` |
| `int GetPlayerPedHandle()` | Returns `Game.Player.Character.Handle` |

`MockGameBridge` gets settable `WantedLevel`, `PlayerDamagedByPed` (bool that
the consume call clears), and a no-op `TaskGoToEntity` (recorded for test
assertions).

## Wiring

In `GameLoopController.InitializeGameData()`:

```
_defenderRallyController = new DefenderRallyController(
    _gameBridge,
    _territoryManager,
    _friendlyDefenderManager,
    _combatManager,
    () => CurrentPlayerFactionId,
    () => Environment.TickCount);
```

In `OnTick()`, called immediately after `_friendlyDefenderManager.Update()`
so defender state is fresh for the rally controller.

In `OnAbort()`: null out the field; no event subscriptions to unwire.

## Testing

Unit tests against `DefenderRallyController` using `MockGameBridge`,
in-memory implementations of `IFriendlyDefenderQuery` and
`ICombatActivityQuery`, and a deterministic clock:

- `Update_NoThreat_DoesNothing`
- `Update_ThreatDetected_IssuesGoToAndCombatTasks`
- `Update_ThreatPersists_DoesNotReissueTasks`
- `Update_ThreatClears_ButCoolDownActive_KeepsRally`
- `Update_ThreatClears_AfterCoolDown_RestoresWander`
- `Update_DefenderInDifferentZone_NotRallied`
- `Update_PlayerNotInOwnedZone_DoesNotRally`
- `Update_PlayerDamagedByPed_RefreshesCoolDown`

In-game verification (manual):
- Stand in player-owned zone, draw police attention → defenders converge.
- Stand in player-owned zone, get attacked by enemy faction → defenders
  converge.
- Threat ends → defenders return to wander after cool-down.
- Stand in non-player-owned zone with police on → defenders in your owned
  zones do NOT rally (they're not in your current zone).

## Risks / open questions

- `TASK_GO_TO_ENTITY` may behave oddly if the player is in a vehicle. May
  need follow-up: detect player-in-vehicle and skip the rally (defenders
  can't catch a moving car), or rally to the player's last on-foot
  position.
- Stacking `TaskCombatHatedTargets` after `TaskGoToEntity` is well-known
  pattern but worth verifying empirically — first stops the defender at the
  stopping range, second engages anything hated within combat radius. If
  GTA collapses these incorrectly, fallback is to issue
  `TaskGoToEntity` first, then on a later tick (when defender is close)
  swap to `TaskCombatHatedTargets`.
- The 5-second cool-down value is a guess. May need tuning.
