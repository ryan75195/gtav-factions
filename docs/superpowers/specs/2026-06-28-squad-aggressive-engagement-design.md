# Squad Aggressive Engagement (Search & Destroy) — Design

**Date:** 2026-06-28
**Status:** Approved (pending implementation plan)

## Problem

In the Search & Destroy squad stance, bodyguards "just take the hold position"
instead of aggressively hunting the enemy. The current code assigns each
bodyguard a distinct enemy and issues `TASK_COMBAT_PED`, then lets GTA's combat
AI choose movement. With cautious/default combat movement the engine holds at
range and takes cover rather than advancing, so the squad appears to hold.

The desired behaviour (player's words): *"for each player in the squad assign a
known enemy in the enemy faction in the zone, run to that enemy's coordinates,
and as soon as they have targeting capability engage with that enemy, then pick
a new one."* Search & Destroy is meant to be **aggressive**.

A secondary symptom seen in `behavior_trace.csv` is the squad "flickering in and
out of aiming" — `in_combat` toggling roughly every second.

### What the code does today (baseline)

- `GameLoopController.UpdateSquadStance` gathers hostile handles from
  `EnemyDefenderManager` + `BattleAttackerManager`, then
  `EnemyTargetCollector.Collect(hostiles, anchorCenter, anchorRadius)` — which
  **filters enemies to within the squad anchor radius**.
- `SquadStanceController.ApplySearchAndDestroy` assigns one enemy per bodyguard
  via `TargetAssignmentResolver` (load-balanced, dispersed, sticky to the prior
  target while alive), then submits `PedIntent.CombatTarget(target)` →
  `RemovePedFromFollowerGroup` + `TaskCombatPed`.
- If no enemies are in range → `SeekFallback` → `TaskCombatHatedTargetsAroundPed`
  (wander/seek), which also looks like holding.

So the *assignment* and *reassign-on-death* parts already match the desired
algorithm. The divergences are: (1) it issues "fight this ped" rather than
"advance to the ped then engage", and (2) it only considers enemies within the
anchor radius rather than the whole zone.

## Decisions (from brainstorming)

- **Engage trigger:** advance until the follower has **line of sight AND is
  within weapon range**, then engage; fall back to advancing if either is lost.
- **Target pool:** **all live enemies in the zone**, no travel cap (aggressive).
- **Count mismatch (more followers than enemies):** **double up** — spare
  followers pile onto remaining enemies (load-balanced/dispersed) so everyone
  always has a target. This is already `TargetAssignmentResolver`'s behaviour.
- **Implementation approach:** explicit per-follower Advance/Engage state machine
  with the decision logic in a portable, unit-tested resolver; only LOS/range
  probing and task issuing touch natives. (Chosen over "aggressive combat
  attributes", which is the exact lever S3 found unreliable, and over "pure
  pursuit", which never settles to engage.)

## Out of scope

- **Escort "2-of-6 follow" desync** — a separate native group-follow bug. Will be
  fixed next, in its own spec/PR. Not touched here.
- **HoldArea** and **Escort** stances are unchanged. Only the Search & Destroy
  branch and the new intent kind are modified.

## Architecture & components

### 1. `IGameBridge.HasClearLineOfSight(int fromPedHandle, int toPedHandle)`

New read-only native. Wraps `HAS_ENTITY_CLEAR_LOS_TO_ENTITY` (trace flag 17:
map + vehicles + objects). Returns `false` for invalid/dead handles rather than
throwing.

- Real implementation in `GameBridge` (a `GameBridge.*.cs` partial), with debug
  logging per the project's GameBridge logging guideline.
- `MockGameBridge` gains `SetLineOfSight(from, to, bool)` test hook and an
  `HasClearLineOfSight` that reads it (default `false`).

### 2. `EngageRangeProvider` (portable, `FactionWars.Combat`)

Maps `DefenderRole` → effective engage range in metres. Pure lookup behind a
first-party interface `IEngageRangeProvider` with method `float For(DefenderRole
role)`.

| Role | Range (m) |
|---|---|
| Sniper | 80 |
| Rocketeer | 45 |
| Rifleman | 45 |
| Gunner | 35 |
| Grunt | 18 |
| fallback (unmapped) | 30 |

Values are starting points, tunable in one place.

### 3. `SquadEngagementResolver` (portable, `FactionWars.Combat.Services`)

The phase brain. Pure and unit-tested.

```
enum EngagePhase { Advance, Engage }

EngagePhase Resolve(EngageInput input)

EngageInput { float DistToTarget; bool HasLineOfSight; float EngageRange;
              EngagePhase CurrentPhase; int ConsecutiveLosMisses; }
```

Rules (hysteresis prevents per-tick flicker):

- From `Advance`: → `Engage` when `DistToTarget <= EngageRange` **and**
  `HasLineOfSight`. Otherwise stay `Advance`.
- From `Engage`: → `Advance` only when `DistToTarget > EngageRange * 1.3`
  **or** LOS has been false for **2 consecutive resolves**
  (`ConsecutiveLosMisses >= 2`). Otherwise stay `Engage`.

Constants: `HysteresisFactor = 1.3f`, `LosGraceMisses = 2`.

The caller (`SquadStanceController`) owns the per-ped `CurrentPhase` and
`ConsecutiveLosMisses` counters and feeds them back in each tick. The resolver
itself is stateless.

### 4. New `PedIntent` kind `AdvanceOnTarget`

- `PedIntentKind.AdvanceOnTarget` added to the enum.
- `PedIntent.AdvanceOnTarget(int targetHandle, float stoppingRange)` factory:
  `Discriminator = targetHandle`, `Radius = stoppingRange`.
- Equality stays `(Kind, Discriminator)` — i.e. keyed on the target handle, so a
  follower advancing on enemy 42 is tasked once and not re-issued every tick;
  `TaskGoToEntity` tracks the moving enemy itself.
- `PedIntentReconciler.Apply` case: `RemovePedFromFollowerGroup(ped)` +
  `TaskGoToEntity(ped, intent.Discriminator, intent.Radius)`.
- `Engage` reuses the existing `CombatTarget` → `TaskCombatPed`.

### 5. `ApplySearchAndDestroy` rewrite

- **Target pool:** drop the anchor-radius filter. Build `EnemyTarget{handle,
  position}` for **every** live hostile handle (enemy defenders + battle
  attackers in the zone). The seek fallback still applies only when there are
  zero hostiles at all.
- Assignment via the existing `TargetAssignmentResolver` (unchanged).
- For each assigned `follower → target`:
  - `dist = |followerPos − targetPos|`
  - `los = gameBridge.HasClearLineOfSight(follower, target)`
  - `range = engageRangeProvider.For(role)`  — follower role looked up from the
    follower→role mapping the squad already has access to (followers expose role
    via the tracked-combatant snapshot / FollowerManager).
  - `phase = squadEngagementResolver.Resolve(...)` using the stored per-ped phase
    and LOS-miss counter; update both.
  - `phase == Engage` → `reconciler.Submit(PedIntent.CombatTarget(target))`
  - `phase == Advance` → `reconciler.Submit(PedIntent.AdvanceOnTarget(target,
    range))`
- The reconciler dedups, so steady phases don't re-task; a phase flip or target
  change re-issues.

> Role lookup note: `SquadStanceController.Update` currently receives only handle
> lists. The implementation plan must thread each follower's `DefenderRole`
> into the S&D path (e.g. pass the followers' roles alongside their handles, or
> resolve via `FollowerManager`). This is a known interface change to settle in
> the plan.

## Data flow (one tick)

```
GameLoopController.UpdateSquadStance
  hostiles = enemyDefenders.GetHostilePedHandles() + battleAttackers.GetHostilePedHandles()
  enemies  = hostiles.map(h => EnemyTarget{ h, gameBridge.GetPedPosition(h) })   // no radius gate
  squadStanceController.Update(anchor, onFootHandles, enemies)
    ApplySearchAndDestroy:
      if enemies empty -> SeekFallback (unchanged)
      assignment = TargetAssignmentResolver.Assign(followers, enemies, previous)
      for each follower -> target:
        dist  = |followerPos - targetPos|
        los   = gameBridge.HasClearLineOfSight(follower, target)
        range = engageRangeProvider.For(role[follower])
        phase = squadEngagementResolver.Resolve(dist, los, range, lastPhase[follower], losMisses[follower])
        update lastPhase[follower], losMisses[follower]
        phase==Engage  -> reconciler.Submit(CombatTarget(target))
        phase==Advance -> reconciler.Submit(AdvanceOnTarget(target, range))
```

## Testing

**Portable unit tests:**
- `SquadEngagementResolverTests`: out-of-range → Advance; in-range+LOS → Engage;
  in-range no-LOS → Advance; hysteresis holds Engage between `range` and
  `range*1.3`; drops to Advance past `range*1.3`; LOS-lost requires 2 consecutive
  misses before dropping.
- `EngageRangeProviderTests`: each role → its range; unmapped → fallback 30.
- `TargetAssignmentResolver`: existing tests stay green (now fed the full zone
  pool).

**Seam / integration tests (`Mock<IGameBridge>` + fakes, MockGameBridge):**
- `MockGameBridge` LOS hook + `HasClearLineOfSight` mock test.
- `PedIntentReconciler`: `AdvanceOnTarget` applies `RemovePedFromFollowerGroup` +
  `TaskGoToEntity(ped, target, range)`; dedup holds across identical resubmits.
- `SquadStanceController` S&D: out-of-range follower → `AdvanceOnTarget`;
  in-range+LOS follower → `CombatTarget`; phase flip re-issues, steady phase does
  not.

## Error handling / edge cases

- **Target dies / streams out mid-tick:** next `Assign` drops it (no position);
  follower reassigns. Zero hostiles → existing `SeekFallback`.
- **Player dead:** S&D already no-ops via the stance guard; followers not
  re-tasked.
- **LOS probe on invalid/dead handle:** native returns `false` → treated as
  `Advance` (safe default); never throws.
- **No behaviour change** to HoldArea / Escort.

## Success criteria

- In a live S&D fight every follower advances to its assigned enemy and opens
  fire at role-appropriate range.
- The Advance/Engage phase no longer flips every second (visible as stable
  `in_combat` once engaged in `behavior_trace.csv`).
- Enemies are pulled from the whole zone, not just those near the squad.
