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
3. **Search & Destroy** — bodyguards are assigned to specific known enemies and charge them.
   Each tick we read the live hostile ped handles we already track (the `EnemyDefenderManager`
   garrison and `BattleAttackerManager` attackers) plus their positions, and a pure
   `TargetAssignmentResolver` maps each bodyguard to a target (greedy-nearest, balanced so they
   spread across distinct enemies rather than dogpiling; extras double up on the nearest when
   bodyguards outnumber enemies; re-target as enemies die). Bodyguards engage their assigned
   target via `TaskCombatPed`.
   - **Fallback:** when there are no tracked enemies in range (e.g. ambient hostiles or police, or
     when standing in open ground with nothing tracked), bodyguards use the area-seek native
     `TaskCombatHatedTargetsAroundPed(anchorRadius)` so they still react to whatever is nearby.

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
  - `BodyguardOrderKind Kind` — `FollowPlayer | HoldAtPoint | SeekInRadius | AttackTarget`.
  - `Vector3 Point` — destination for `HoldAtPoint`; anchor centre for `SeekInRadius` (unused otherwise).
  - `float Radius` — radius for `SeekInRadius` (unused otherwise).
  - `int TargetHandle` — assigned enemy ped for `AttackTarget` (unused otherwise).

- **`ISquadStanceResolver` / `SquadStanceResolver`** — in `Combat.Interfaces` / `Combat.Services`.
  Pure logic for the geometry stances, fully unit-tested. Method:
  `BodyguardOrder Resolve(SquadStance stance, Vector3 anchorCenter, float anchorRadius, int bodyguardIndex, int bodyguardCount)`.
  - `Escort` → `FollowPlayer`.
  - `HoldArea` → `HoldAtPoint(p)` where `p` is a point on a ring around `anchorCenter`, distributed
    by `bodyguardIndex / bodyguardCount` (even angular spread) at a fraction of `anchorRadius`, so
    bodyguards fan out instead of stacking.
  - `SearchAndDestroy` → `SeekInRadius(anchorCenter, anchorRadius)` — this is the **fallback** order
    used only when no tracked enemy targets are available; live target assignment is handled by
    `TargetAssignmentResolver` (below).

- **`ITargetAssignmentResolver` / `TargetAssignmentResolver`** — in `Combat.Interfaces` /
  `Combat.Services`. Pure logic, fully unit-tested. Maps bodyguards to known enemy targets:
  `IReadOnlyDictionary<int, int> Assign(IReadOnlyList<BodyguardPosition> bodyguards, IReadOnlyList<EnemyTarget> enemies)`
  returning `bodyguardHandle → enemyHandle`. `BodyguardPosition` is `(int Handle, Vector3 Position)`;
  `EnemyTarget` is `(int Handle, Vector3 Position)` — both small value objects in `Combat.Models`.
  Greedy-nearest with balancing: assign each bodyguard the nearest enemy not yet at its share of the
  load, so they spread across distinct targets; when bodyguards outnumber enemies, extras pile onto
  the nearest remaining enemy. Returns an empty map when there are no enemies (controller then uses
  the seek fallback).

- **Enemy target source** — read-only handle queries added to `EnemyDefenderManager` and
  `BattleAttackerManager` exposing the currently-tracked hostile ped handles (overall or by zone).
  The controller reads each handle's position via `IGameBridge.GetPedPosition`, filters to those
  within the anchor radius, and feeds them to `TargetAssignmentResolver`. (Watch the ≤10 public
  method cap on those managers — if at the cap, expose the handles via a small read-only query
  interface the manager already implements, or a focused partial, rather than a bare new method.)

- **`SquadStanceController`** — in `ScriptHookV.Managers`. Owns `_currentStance`. Public surface:
  - `void CycleStance()` — advances the stance and (when the party is non-empty) shows the
    notification. Returns silently with no party.
  - `SquadStance CurrentStance { get; }`
  - `void Update(Vector3 anchorCenter, float anchorRadius, IReadOnlyList<int> onFootBodyguardHandles, IReadOnlyList<EnemyTarget> enemiesInRange)`
    — each tick: for `Escort`/`HoldArea`, calls `SquadStanceResolver` per bodyguard and issues the
    matching task. For `SearchAndDestroy`, if `enemiesInRange` is non-empty it calls
    `TargetAssignmentResolver` and issues `TaskCombatPed(bodyguard, target)` per assignment; if empty
    it falls back to `TaskCombatHatedTargetsAroundPed(anchorRadius)`. To avoid task spam, a ped's task
    is re-issued only when its assignment changed — tracked as last-applied `(stance, orderKind,
    targetHandle)` per handle, cleared on stance change and when an assigned target dies.

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
`SquadStanceController.Update(anchorCenter, anchorRadius, onFootHandles, enemiesInRange)` when the
player is on foot (gathering `enemiesInRange` only when the stance is SearchAndDestroy).

### New natives

Two new `IGameBridge` methods plus their `MockGameBridge` implementations (each real method carries
`FileLogger.AI` debug logging per project rule; each mock records calls for assertions):

- `void TaskGuardArea(int pedHandle, Vector3 center, float radius)` — used by **Hold Area**. Real
  implementation sets a defensive area and a stand-guard task (GTA `TASK_SET_DEFENSIVE_AREA` +
  `TASK_STAND_GUARD`, or `TASK_GUARD_SPHERE_DEFENSIVE_AREA`), plus `canUseCover` combat attributes.
- `void TaskCombatPed(int pedHandle, int targetPedHandle)` — used by **Search & Destroy** assignment.
  Wraps GTA `TASK_COMBAT_PED` so the bodyguard runs to and fights the specific assigned enemy.

Existing natives reused: `TaskFollowToOffsetFromEntity` (Escort), `TaskCombatHatedTargetsAroundPed`
(Search & Destroy fallback), `GetPedPosition` (enemy/bodyguard positions), `ClearPedTasks`.

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
- **`TargetAssignmentResolverTests`** (pure): with more enemies than bodyguards, each bodyguard gets a
  distinct nearest enemy (no dogpiling); with more bodyguards than enemies, extras double up on the
  nearest remaining enemy and every bodyguard is assigned; nearest-first ordering (a bodyguard is
  assigned its closest available enemy); empty enemy list yields an empty map; empty bodyguard list
  yields an empty map.
- **`SquadStanceControllerTests`** (mock `IGameBridge`): cycle order Escort→HoldArea→SearchAndDestroy→
  Escort; `CycleStance` with empty party shows no notification and changes nothing; `Update` issues
  `TaskFollowToOffsetFromEntity` in Escort, `TaskGuardArea` in HoldArea; in SearchAndDestroy with
  tracked enemies in range it issues `TaskCombatPed(bodyguard, assignedTarget)` per assignment, and
  with no tracked enemies it falls back to `TaskCombatHatedTargetsAroundPed`; task is not re-issued for
  an unchanged stance/assignment on consecutive ticks, and IS re-issued when a ped's assigned target
  changes (e.g. its previous target died).
- **Anchor resolution**: in-zone uses zone centre/radius; out-of-zone uses player position + default radius.
- **`MockGameBridgeTests`**: `TaskGuardArea` records its arguments; `TaskCombatPed` records the
  (ped, target) pair for assertions.
- Full suite stays green; analyzers (CRLF, ≤10 public methods, ≤250 lines, one public type per file,
  service interfaces, ≤5 ctor params) satisfied.

## Out of scope (YAGNI)

- Per-bodyguard individual stances (the cycle is party-wide).
- Persisting stance across saves.
- A behaviour-tree / per-ped state machine (GTA native combat AI handles per-ped fighting).
- Any change to the battle-HUD cycle or other existing D-pad bindings.
