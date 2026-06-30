# Squad Boards the Player's Vehicle Cleanly and Doesn't Bail — Design

**Issue:** #140
**Status:** Approved (design), pending spec review

## Goal

Fix two squad-vehicle bugs when the player is in a vehicle with enemies nearby ("in
aggro"):

1. An on-foot bodyguard ordered to board the player's vehicle runs a few steps, stops to
   aim at a nearby enemy, runs again — it never just runs straight in.
2. A boarded bodyguard's native combat AI bails it out of the vehicle to fight, then
   FollowerManager re-orders it back in — a jump-out/jump-in oscillation.

Desired behavior: bodyguards board straight, then **shoot from the vehicle but never exit
it** (drive-by, no bailing).

## Root Cause (from in-game logs)

`ConfigureFollowerCombat` sets each follower as a **native player-group member**
(`SET_PED_AS_GROUP_MEMBER`). So three systems steer a bodyguard at once: GTA's native
group AI, FollowerManager's explicit `TaskPedEnterVehicle` boarding, and — when enemies
are near — **native combat reactions**. They conflict:

- The on-foot bodyguard's native combat reaction interrupts the enter-task to aim → the
  run-stop-aim-run stutter (bug 1).
- A seated bodyguard's native combat AI **leaves the vehicle to fight**, and FollowerManager
  re-orders it in → oscillation (bug 2).

Evidence: in the log, while `inVehicle=True`, `TaskPedEnterVehicle` is re-issued every
~1.5 s to the same peds indefinitely and never sticks, while the mod's `TaskPedLeaveVehicle`
is issued **zero** times — confirming it is native combat (not the mod) pulling them out,
and that the boarding never settles.

The Escort on-foot follow already solved the equivalent "stalled by fire" problem by
setting `BlockPermanentEvents` during the sprint. The vehicle-boarding path does not do
this — that is the gap.

## Fix

### Component 1 — new bridge method `SetPedCanLeaveVehicle`

Added to `IGameBridge`, real `GameBridge`, and `MockGameBridge`:

```csharp
/// <summary>
/// Controls whether a ped's combat AI is allowed to exit its vehicle to fight
/// (BF_CanLeaveVehicle / SET_PED_COMBAT_ATTRIBUTES index 3). Set false to keep a
/// boarded bodyguard seated doing drive-bys instead of bailing out to engage on foot.
/// </summary>
void SetPedCanLeaveVehicle(int pedHandle, bool canLeave);
```

Real implementation: `Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped.Handle, 3, canLeave);`
(index 3 = `BF_CanLeaveVehicle`; the codebase already uses index 2 = `CanDoDrivebys` and
0/5/46 elsewhere). Logs once-style per repo convention. `MockGameBridge` tracks the last
value per ped via a test getter (`GetPedCanLeaveVehicleForTest(int)`, default `true`).

### Component 2 — boarding/riding state in `FollowerManager.VehicleAssignment`

While the player is in a vehicle and a follower is being boarded/seated:

- **On issuing the board order** (follower still on foot):
  `SetPedBlockPermanentEvents(true)` so native combat reactions don't interrupt the
  sprint-to-seat — the follower runs straight in (fixes bug 1). This mirrors the Escort
  sprint's existing event-block.
- **Once the follower is in the vehicle** (`IsPedInVehicle` true):
  `SetPedBlockPermanentEvents(false)` (so it can perceive/shoot) **and**
  `SetPedCanLeaveVehicle(false)` **and** ensure `CanDoDrivebys` is enabled. The follower
  shoots from the vehicle but native AI cannot pull it out (fixes bug 2).
- **When the follower is no longer riding** (player exits the vehicle, or the follower is
  dropped to the ground for HoldArea/S&D): restore `SetPedCanLeaveVehicle(true)` and clear
  any lingering event-block, so on-foot combat behaves normally again.

The existing `AssignFollowersToVehicle` already commits seats, throttles re-orders, and
clears tasks before issuing the enter task — keep all of that; this change adds the
event-block on the board order and the `CanLeaveVehicle`/driveby application once seated,
plus the restore on disembark.

State tracking: a small per-follower set of "currently locked into the player's vehicle"
handles so the lock is applied once on seating and restored once on exit (no per-tick
re-spam, matching the existing `_lastBoardOrderMs` throttle discipline).

### Where "restore on exit" runs

When `FollowerManager.Update` takes the not-boarding branch (player not in a vehicle, or
the squad is grounded for HoldArea/S&D), any follower previously locked into the vehicle
gets `SetPedCanLeaveVehicle(true)` + unblock, and is removed from the locked set. This
guarantees the lock never leaks into on-foot combat.

## Scope / preserved behavior

- Only the squad's player-vehicle boarding path changes. The existing heli/boat passenger
  drive-by setup (followers given a pistol "for drive-by shooting") is preserved — and now
  those passengers also won't bail, matching the desired "shoot from vehicle, just don't
  exit."
- No change to native group membership, the stance system, or the escort follow.

## Error handling

- Idempotent: `CanLeaveVehicle`/block are applied once on the seating transition and once
  on the exit transition, tracked by the locked-handle set, so there is no per-frame
  thrash and no native-call spam.
- A follower that dies or despawns while locked is pruned from the locked set (same pruning
  the manager already does for stale handles), so no stale handle is restored.

## Testing

- **Bridge method:** `MockGameBridge.SetPedCanLeaveVehicle` records the value;
  `GetPedCanLeaveVehicleForTest` returns it (default true); `Reset` restores default. A unit
  test asserts set/track.
- **FollowerManager.VehicleAssignment:** with `MockGameBridge`, a follower being boarded
  (on foot, ordered to enter) gets `BlockPermanentEvents(true)`; a follower reported
  `IsPedInVehicle == true` gets `SetPedCanLeaveVehicle(false)` and `BlockPermanentEvents(false)`;
  a follower no longer riding (player not in vehicle) gets `SetPedCanLeaveVehicle(true)` restored.
- The actual native combat-attribute effect (drive-by vs bail) is in-game/log-verified per
  repo convention (native behavior cannot be unit-tested).

## Out of scope (YAGNI)

- Removing native group membership (kept; the fix works with it in place).
- Changing on-foot combat, the stance system, or which enemies bodyguards target.
- The zone-loss grace rule (#141) — separate change.
