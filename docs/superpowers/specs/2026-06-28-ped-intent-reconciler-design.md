# Ped-Control Consolidation: Intent Reconciler Design

**Status:** Approved direction (roadmap Stage 3). Designed autonomously per the owner's
"do it all, don't ask" directive. Implemented across several PRs.

## Problem

Followers, enemy defenders, battle attackers, and friendly defenders each call
`IGameBridge.TaskXxx` / combat-config primitives directly, every tick, with no single owner
of a ped's desired state. Two failure modes result:

1. **Per-frame task thrash.** Without consistent dedup, a ped can be re-tasked every tick
   (clear → re-issue), which the engine renders as "flicker-aim / run-in-place" and which drives
   GTA's combat-AI pathfinding to re-evaluate every frame — the suspected trigger of the >5s
   blocking-script freeze.
2. **Conflicting owners.** Combat config is applied in two places (`FollowerManager.ConfigureFollowerCombat`
   and the static `GameBridge.ConfigureFollowerCombat`), the latter slapping a blanket
   `ABILITY=2 / RANGE=2 / MOVEMENT=2` on every follower on every `SetPedAsFollower`, overriding the
   per-role profile ("the similar bug").

`SquadStanceController` already solves (1) for followers with `AlreadyApplied`/`Remember`. The fix
is to **generalize that pattern into one reconciler** every controller submits to.

## Design

### PedIntent (value type)

A `PedIntent` describes what a ped should be doing this tick. Dedup is by `(Kind, Discriminator)`
— exactly mirroring the proven `SquadStance` discriminator approach (target handle for combat, ring
index for hold, 0 otherwise). Position/radius ride along for application but do **not** trigger
re-tasking on their own (matches current behavior: a guard point that drifts sub-threshold isn't
re-issued every tick).

```
enum PedIntentKind { Idle, FollowPlayer, GuardArea, CombatTarget, SeekHatedTargets, WanderArea, GoToCoord, LeaveVehicle }

readonly struct PedIntent : IEquatable<PedIntent>
{
    PedIntentKind Kind;
    int Discriminator;   // target handle / ring index / 0
    Vector3 Position;    // center or destination (area/goto intents)
    float Radius;
    // factories: FollowPlayer(), GuardArea(center, radius, ringIndex), CombatTarget(targetHandle),
    //            SeekHatedTargets(center, radius), WanderArea(center, radius), GoToCoord(dest), LeaveVehicle(), Idle()
    // Equals/GetHashCode over (Kind, Discriminator) only.
}
```

### IPedIntentReconciler

```
void Submit(int pedHandle, PedIntent intent);  // apply via IGameBridge iff changed from last-applied
void Forget(int pedHandle);                     // drop one ped's last-applied state (despawn)
void Clear();                                   // drop all (stance cycle / re-init)
```

`PedIntentReconciler(IGameBridge)` keeps a `Dictionary<int, PedIntent> _lastApplied`. `Submit`
returns early when the new intent equals the last-applied for that ped; otherwise it applies the
intent and records it. Application centralizes the existing "detach-from-group before combat" and
"clear-tasks before go-to" sequencing:

| Kind | IGameBridge calls |
|------|-------------------|
| FollowPlayer | `SetPedAsFollower` |
| GuardArea | `RemovePedFromFollowerGroup`, `TaskGuardArea(pos, radius)` |
| CombatTarget | `RemovePedFromFollowerGroup`, `TaskCombatPed(discriminator)` |
| SeekHatedTargets | `RemovePedFromFollowerGroup`, `TaskCombatHatedTargetsAroundPed(radius)` |
| WanderArea | `TaskPedWanderInBoundedArea(pos, radius)` |
| GoToCoord | `ClearPedTasks`, `TaskGoToCoord(pos)` |
| LeaveVehicle | `TaskPedLeaveVehicle` |
| Idle | (none) |

### Combat profile (separate from per-tick intent)

Per-tick **task** intent (above) is distinct from one-time **combat config** (ability/range/movement,
weapon, accuracy, armor, health). The consolidation removes the blanket profile from the static
`GameBridge.ConfigureFollowerCombat` so the per-role `FollowerManager.ConfigureFollowerCombat` is the
single source of a follower's combat profile, applied once at recruit. `SetPedAsFollower` becomes a
dumb primitive (group membership + clear tasks only; no combat-attribute side effects).

## Rollout (one PR each)

1. **Reconciler core** (this spec + `PedIntent`/`IPedIntentReconciler`/`PedIntentReconciler`, TDD). No wiring — zero behavior change.
2. **Followers** — `SquadStanceController` submits intents instead of calling `_gameBridge.TaskXxx`; collapse the two `ConfigureFollowerCombat`; demote `SetPedAsFollower`; remove the blanket profile.
3. **Enemy defenders** — `EnemyDefenderManager` spawn/leash/close-defense tasking via the reconciler.
4. **Battle attackers** — `BattleAttackerManager` tasking via the reconciler.
5. **Friendly defenders** — `FriendlyDefenderManager` wander/seek/leash tasking via the reconciler.

Each migration PR is behavior-preserving (same native calls, now deduped through one owner) and
verified by the Stage 1 tick instrumentation in the final in-game test.

## Testing

Reconciler core is fully unit-tested against `Mock<IGameBridge>` (Moq): each kind maps to the right
primitive(s); identical re-submits are deduped (primitive called once); changed discriminator
re-applies; `Forget`/`Clear` drop state so the next submit re-applies. Each migration PR keeps the
existing controller tests green and adds intent-submission assertions where behavior is observable.

## Constraints

Standard repo rules (CRLF/UTF-8-no-BOM; no tuples; ≤250 lines/class; ≤40 lines/method; one public
type/file; ≤5 ctor params; TDD through the pre-commit hook). `PedIntent`/reconciler are portable
(`Combat` or `ScriptHookV.Managers` namespace, no GTA refs in the model).
