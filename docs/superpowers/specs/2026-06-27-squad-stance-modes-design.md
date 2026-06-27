# Squad Stance Modes — Design

**Issue:** #35
**Status:** Approved design, pending implementation plan

## Goal

Let the player cycle their bodyguard party through three combat stances with a single
controller button, so bodyguards can escort, hold ground, or sweep an area on demand.

## User-facing behaviour

- **D-pad Up** cycles the squad stance: **Escort → Hold Area → Search & Destroy → Escort**.
  (The battle-HUD cycle stays on D-pad Down, unchanged.)
- Each press shows a brief on-screen notification naming the new stance
  (e.g. `~b~Bodyguards:~w~ Hold Area`).
- The cycle only does something when the player has at least one living bodyguard;
  with an empty party the press is ignored (no notification).

### The three stances

1. **Escort** (default) — bodyguards follow and defend the player. This is byte-for-byte
   today's on-foot follow behaviour, relocated into the stance system.
2. **Hold Area** — bodyguards fan out to distinct points around the area anchor, take cover,
   and hold position, engaging hostiles that come to them rather than chasing far.
3. **Search & Destroy** — bodyguards actively seek and engage hostiles within the area anchor's
   radius (the same "seek hated targets in radius" native the enemy garrison uses).

### Area anchor

Modes 2 and 3 operate relative to an **area anchor** `(center, radius)`, resolved every tick:

- If the player is standing inside a zone (their **own territory or an enemy zone**), the anchor
  is that zone's `Center` and `Radius`.
- If the player is not in any zone, the anchor is `(playerPosition, DefaultLooseRadius)`.

Because the anchor re-resolves each tick and follows the player's current zone/position, the squad
naturally regroups near the player when the player leaves a zone — they are never stranded.

### Vehicles

When the player is in a vehicle, stance is irrelevant: bodyguards embark/disembark exactly as they
do today. Stance tasking applies only to on-foot bodyguards.

### Persistence & defaults

- Default stance is **Escort** at spawn and after a save is loaded.
- Stance is a **runtime session value** — it is **not** written to the save (YAGNI). Bodyguards
  themselves persist as they do today; their stance simply resets to Escort on load.

## Architecture (Approach A: resolver + thin applier)

```
GameLoopController (input + per-tick wiring)
        │  D-pad Up -> CycleStance()
        ▼
SquadStanceController  (ScriptHookV) ── owns current stance, issues IGameBridge tasks
        │  uses
        ▼
SquadStanceResolver    (Combat, portable, pure) ── (stance, anchor, index, count) -> BodyguardOrder
```

### Components

- **`SquadStance`** — enum in `Combat.Models`: `Escort`, `HoldArea`, `SearchAndDestroy`.
  A `Next()` helper (or the controller) defines the cycle order.

- **`BodyguardOrder`** — value object in `Combat.Models`. Describes one bodyguard's intent without
  any native dependency. Shape:
  - `BodyguardOrderKind Kind` — `FollowPlayer | HoldAtPoint | SeekInRadius`.
  - `Vector3 Point` — destination for `HoldAtPoint`; anchor centre for `SeekInRadius` (unused for `FollowPlayer`).
  - `float Radius` — radius for `SeekInRadius` (unused otherwise).

- **`ISquadStanceResolver` / `SquadStanceResolver`** — in `Combat.Interfaces` / `Combat.Services`.
  Pure logic, fully unit-tested. Method:
  `BodyguardOrder Resolve(SquadStance stance, Vector3 anchorCenter, float anchorRadius, int bodyguardIndex, int bodyguardCount)`.
  - `Escort` → `FollowPlayer`.
  - `HoldArea` → `HoldAtPoint(p)` where `p` is a point on a ring around `anchorCenter`, distributed
    by `bodyguardIndex / bodyguardCount` (even angular spread) at a fraction of `anchorRadius`, so
    bodyguards fan out instead of stacking.
  - `SearchAndDestroy` → `SeekInRadius(anchorCenter, anchorRadius)`.

- **`SquadStanceController`** — in `ScriptHookV.Managers`. Owns `_currentStance`. Public surface:
  - `void CycleStance()` — advances the stance and (when the party is non-empty) shows the
    notification. Returns silently with no party.
  - `SquadStance CurrentStance { get; }`
  - `void Update(Vector3 anchorCenter, float anchorRadius, IReadOnlyList<int> onFootBodyguardHandles)`
    — each tick, for each handle, calls the resolver and issues the matching `IGameBridge` task.
    To avoid task spam, re-issues a ped's task only when its stance assignment changed (track last
    applied `(stance, orderKind)` per handle, clear on stance change).

- **Area anchor resolution** — a small helper (in `SquadStanceController` or a tiny collaborator)
  that returns `(center, radius)` from `ITerritoryEvents.CurrentZone` (any owner) when present,
  else `(playerPosition, DefaultLooseRadius)`.

### Boundary change to FollowerManager

Today `FollowerManager.Update` performs the on-foot follow tasking (`UpdateOnFootFollowers`).
That on-foot tasking moves into the controller's **Escort** branch so a single system owns on-foot
tasking and the two never fight over the same peds. `FollowerManager` keeps:

- roster (recruit / dismiss / restore),
- death detection and cleanup,
- vehicle embark/disembark,
- and exposes the alive on-foot bodyguard handles for the current faction.

`GameLoopController.SystemUpdates` calls `FollowerManager.Update` (roster/death/vehicle) and then
`SquadStanceController.Update(anchorCenter, anchorRadius, onFootHandles)` when the player is on foot.

### New native

One new `IGameBridge` method plus its `MockGameBridge` implementation:

- `void TaskGuardArea(int pedHandle, Vector3 center, float radius)` — used by **Hold Area**. Real
  implementation sets a defensive area and a stand-guard task (GTA `TASK_SET_DEFENSIVE_AREA` +
  `TASK_STAND_GUARD`, or `TASK_GUARD_SPHERE_DEFENSIVE_AREA`), plus `canUseCover` combat attributes.
  Mock records the call for assertions. Per project rule, the real method carries `FileLogger.AI`
  debug logging.

Existing natives reused: `TaskFollowToOffsetFromEntity` (Escort), `TaskCombatHatedTargetsAroundPed`
(Search & Destroy), `GetPedPosition`, `ClearPedTasks`.

## Input wiring

- Add `ControlDpadUp = 172` (INPUT_PHONE_UP) constant.
- In the controller-input poll (`GameLoopController.Lifecycle`), `IsControlJustPressed(ControlDpadUp)`
  → `SquadStanceController.CycleStance()`.
- Notification via the existing `IGameBridge.ShowNotification`.

## Error handling

- No party / no living bodyguards → cycle is a no-op (no notification, no tasking).
- Dead/invalid handles are filtered by `FollowerManager` before the controller sees them.
- Anchor resolution always yields a valid `(center, radius)` (falls back to player position), so the
  controller never operates on a null zone.

## Testing

- **`SquadStanceResolverTests`** (pure): Escort→FollowPlayer; SearchAndDestroy→SeekInRadius with the
  anchor centre/radius; HoldArea→HoldAtPoint with distinct points per index (two indices yield
  different points; points lie within `anchorRadius` of centre).
- **`SquadStanceControllerTests`** (mock `IGameBridge`): cycle order Escort→HoldArea→SearchAndDestroy→
  Escort; `CycleStance` with empty party shows no notification and changes nothing; `Update` issues
  `TaskFollowToOffsetFromEntity` in Escort, `TaskGuardArea` in HoldArea, `TaskCombatHatedTargetsAroundPed`
  in SearchAndDestroy; task is not re-issued for an unchanged stance on consecutive ticks.
- **Anchor resolution**: in-zone uses zone centre/radius; out-of-zone uses player position + default radius.
- **`MockGameBridgeTests`**: `TaskGuardArea` records its arguments.
- Full suite stays green; analyzers (CRLF, ≤10 public methods, ≤250 lines, one public type per file,
  service interfaces, ≤5 ctor params) satisfied.

## Out of scope (YAGNI)

- Per-bodyguard individual stances (the cycle is party-wide).
- Persisting stance across saves.
- A behaviour-tree / per-ped state machine (GTA native combat AI handles per-ped fighting).
- Any change to the battle-HUD cycle or other existing D-pad bindings.
