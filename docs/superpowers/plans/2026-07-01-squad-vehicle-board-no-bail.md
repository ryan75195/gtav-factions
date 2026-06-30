# Squad Boards Cleanly and Doesn't Bail — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make squad bodyguards board the player's vehicle straight (no aim-stutter) and stay seated doing drive-bys instead of bailing out to fight.

**Architecture:** A new `IGameBridge.SetPedCanLeaveVehicle` wraps `SET_PED_COMBAT_ATTRIBUTES` index 3 (and enables drive-bys, index 2, when locking a ped in). `FollowerManager.VehicleAssignment` blocks threat events while a follower runs to board (so the enter task isn't interrupted), then locks each seated follower into the vehicle (can't-leave + unblock) exactly once, and restores can-leave + unblock when the follower is no longer riding.

**Tech Stack:** C#/.NET 4.8, ScriptHookVDotNet3 (`SET_PED_COMBAT_ATTRIBUTES`, `SET_BLOCKING_OF_NON_TEMPORARY_EVENTS`), xUnit + Moq. Custom Roslyn analyzers.

## Global Constraints

- Strict TDD: failing test first, watch it fail, then implement.
- Build must stay **0 warnings / 0 errors**. Analyzers: CI0007 method ≤40 lines; CI0017 file ≤250 lines; CI0004 ≤10 public methods/class; CI0014 ctor concrete-type rule; CI0016 one public top-level type per file; ENDOFLINE = CRLF on every file.
- No `#pragma warning disable CI*/CA*`, no skipped tests, no git-hook bypass.
- New `IGameBridge` behavior MUST include a `FileLogger` line (native calls can't be unit-tested).
- Combat-attribute indices (GTA): **3 = BF_CanLeaveVehicle**, **2 = BF_CanDoDrivebys** (the codebase already uses index 2 in `GameBridge.HostilePeds.cs` and 0/5/46 in `GameBridge.PedCombatConfig.cs`).
- Scope: only the squad's player-vehicle boarding path. Do NOT change native group membership, the stance system, the escort follow, or which enemies bodyguards target.
- Branch: `fix/140-squad-vehicle-board-no-bail` (already created). Commit per task; do not push until reviewed.
- Build: `dotnet build FactionWars.sln --no-incremental`. Unit tests: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~FactionWars.Tests.Unit"`. Pre-commit hook runs build + unit tests (allow up to 10 min).

---

### Task 1: `IGameBridge.SetPedCanLeaveVehicle`

**Files:**
- Modify: `src/FactionWars/Core/Interfaces/IGameBridge.cs` (declare after `SetPedCombatAttributes`, ~line 592)
- Modify: `src/FactionWars/ScriptHookV/GameBridge.PedCombatConfig.cs` (implement near `SetPedCombatAttributes`)
- Modify: `src/FactionWars/Core/Utils/MockGameBridge.cs` (track + getter + Reset)
- Test: `tests/FactionWars.Tests/Unit/Core/MockGameBridgeCanLeaveVehicleTests.cs` (new)

**Interfaces:**
- Produces: `IGameBridge.SetPedCanLeaveVehicle(int pedHandle, bool canLeave)`; `MockGameBridge.GetPedCanLeaveVehicleForTest(int) -> bool` (default `true`).

- [ ] **Step 1: Write the failing test**

`tests/FactionWars.Tests/Unit/Core/MockGameBridgeCanLeaveVehicleTests.cs`:
```csharp
using FactionWars.Core.Utils;
using Xunit;

namespace FactionWars.Tests.Unit.Core
{
    public class MockGameBridgeCanLeaveVehicleTests
    {
        [Fact]
        public void GetPedCanLeaveVehicleForTest_DefaultsToTrue()
        {
            var bridge = new MockGameBridge();
            Assert.True(bridge.GetPedCanLeaveVehicleForTest(42));
        }

        [Fact]
        public void SetPedCanLeaveVehicle_TracksValue()
        {
            var bridge = new MockGameBridge();
            bridge.SetPedCanLeaveVehicle(42, false);
            Assert.False(bridge.GetPedCanLeaveVehicleForTest(42));

            bridge.SetPedCanLeaveVehicle(42, true);
            Assert.True(bridge.GetPedCanLeaveVehicleForTest(42));
        }

        [Fact]
        public void Reset_RestoresCanLeaveVehicleDefault()
        {
            var bridge = new MockGameBridge();
            bridge.SetPedCanLeaveVehicle(42, false);
            bridge.Reset();
            Assert.True(bridge.GetPedCanLeaveVehicleForTest(42));
        }
    }
}
```

- [ ] **Step 2: Run to verify it fails**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~MockGameBridgeCanLeaveVehicleTests"`
Expected: FAIL to compile — `SetPedCanLeaveVehicle` / `GetPedCanLeaveVehicleForTest` missing.

- [ ] **Step 3: Declare on `IGameBridge`** after the `SetPedCombatAttributes` declaration (~line 592):
```csharp
        /// <summary>
        /// Controls whether a ped's combat AI may exit its vehicle to fight
        /// (SET_PED_COMBAT_ATTRIBUTES index 3, BF_CanLeaveVehicle). Set false to keep a
        /// boarded bodyguard seated; doing so also enables drive-bys (index 2) so the
        /// kept-in ped shoots from the vehicle instead of bailing out to engage on foot.
        /// </summary>
        void SetPedCanLeaveVehicle(int pedHandle, bool canLeave);
```

- [ ] **Step 4: Implement in `GameBridge.PedCombatConfig.cs`** (next to `SetPedCombatAttributes`):
```csharp
        /// <inheritdoc />
        public void SetPedCanLeaveVehicle(int pedHandle, bool canLeave)
        {
            try
            {
                var ped = Entity.FromHandle(pedHandle) as Ped;
                if (ped == null || !ped.Exists()) return;

                // 3 = BF_CanLeaveVehicle. False keeps the ped seated; pair it with drive-bys
                // (2 = BF_CanDoDrivebys) so a kept-in bodyguard still shoots from the vehicle.
                Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped.Handle, 3, canLeave);
                if (!canLeave)
                {
                    Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped.Handle, 2, true);
                }

                FileLogger.AI($"SetPedCanLeaveVehicle: ped {pedHandle} canLeave={canLeave}");
            }
            catch (Exception ex)
            {
                FileLogger.Error($"SetPedCanLeaveVehicle exception for ped {pedHandle}", ex);
            }
        }
```
Confirm `using System;`, `GTA`, `GTA.Native`, and the `FactionWars.ScriptHookV.Logging` using are present in the file (they are — `SetPedCombatAttributes` uses them).

- [ ] **Step 5: Implement in `MockGameBridge.cs`.** Add a tracking set near the other ped-state collections and the method + getter (mirror the `_blockPermanentEventsPeds` / `GetPedBlockPermanentEventsForTest` pattern at ~line 494-501). Because the default is `true` (a ped CAN leave by default), track only peds explicitly set to `false`:
```csharp
        private readonly HashSet<int> _cannotLeaveVehiclePeds = new HashSet<int>();

        public void SetPedCanLeaveVehicle(int pedHandle, bool canLeave)
        {
            if (canLeave) _cannotLeaveVehiclePeds.Remove(pedHandle);
            else _cannotLeaveVehiclePeds.Add(pedHandle);
        }

        public bool GetPedCanLeaveVehicleForTest(int pedHandle) => !_cannotLeaveVehiclePeds.Contains(pedHandle);
```
In `Reset()`, add: `_cannotLeaveVehiclePeds.Clear();`

- [ ] **Step 6: Run to verify it passes**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~MockGameBridgeCanLeaveVehicleTests"`
Expected: PASS (3/3).

- [ ] **Step 7: Build + full unit suite**

Run: `dotnet build FactionWars.sln --no-incremental` then the full `FactionWars.Tests.Unit` filter.
Expected: clean build (0/0), all PASS.

- [ ] **Step 8: Commit**

```bash
git add src/FactionWars tests/FactionWars.Tests
git commit -m "feat: add SetPedCanLeaveVehicle bridge method (#140)"
```

---

### Task 2: Block events on board, lock seated followers in, restore on exit

**Files:**
- Modify: `src/FactionWars/ScriptHookV/Managers/FollowerManager.VehicleAssignment.cs` (board-order event-block; seated lock; locked-set state; a `RestoreLockedFollowers` helper)
- Modify: `src/FactionWars/ScriptHookV/Managers/FollowerManager.cs` (call `RestoreLockedFollowers` in the `Update` not-boarding/else branch, ~lines 254-259)
- Test: `tests/FactionWars.Tests/Unit/ScriptHookV/Managers/FollowerManagerVehicleLockTests.cs` (new) — or extend `FollowerManagerTests` if its fixture is reused

**Interfaces:**
- Consumes: `IGameBridge.SetPedCanLeaveVehicle` (Task 1), existing `SetPedBlockPermanentEvents`, `IsPedInVehicle`, `IsPedTryingToEnterVehicle`, `TaskPedEnterVehicle`, `GetPlayerVehicle`, `IsPlayerInVehicle`.

- [ ] **Step 1: Write the failing tests.** Create `tests/FactionWars.Tests/Unit/ScriptHookV/Managers/FollowerManagerVehicleLockTests.cs`. Mirror how `FollowerManagerTests` builds the manager and sets the mock (look at `FollowerManagerTests.cs` constructor + the `Update_WhenPlayerInVehicle_*` tests for the exact `_gameBridgeMock`/`MockGameBridge` setup, the `_followerServiceMock` follower list, and how a follower is marked in-vehicle). Use the SAME mock type the fixture uses. Three behaviors:

```csharp
        // (1) A follower being boarded (on foot, gets a board order) has threat events blocked
        //     so the run-to-seat isn't interrupted.
        [Fact]
        public void Update_BoardingFollower_BlocksPermanentEvents() { /* arrange player-in-vehicle + a free seat + follower on foot; act Update; assert GetPedBlockPermanentEventsForTest(follower) == true */ }

        // (2) A seated follower (IsPedInVehicle true) is locked in: can't leave + events unblocked.
        [Fact]
        public void Update_SeatedFollower_LockedInAndUnblocked() { /* arrange player-in-vehicle + follower IsPedInVehicle true; act Update; assert GetPedCanLeaveVehicleForTest(follower) == false && GetPedBlockPermanentEventsForTest(follower) == false */ }

        // (3) When the player is no longer in a vehicle, a previously-locked follower is restored.
        [Fact]
        public void Update_PlayerExitsVehicle_RestoresCanLeave() { /* arrange: tick 1 player-in-vehicle + seated follower (locks it); tick 2 player NOT in vehicle; assert GetPedCanLeaveVehicleForTest(follower) == true */ }
```
Fill each test body using the fixture's established arrange pattern (player-in-vehicle via `IsPlayerInVehicle`/`GetPlayerVehicle`, a follower handle from the follower service, in-vehicle marking via the mock's in-vehicle setter, a free seat via the seat-priority service or the mock). If the FollowerManager fixture is Moq-based and cannot easily express `IsPedInVehicle == true` for a specific ped, use the concrete `MockGameBridge` (it has `IsPedInVehicle`, `GetPedBlockPermanentEventsForTest`, and the new `GetPedCanLeaveVehicleForTest`) and whatever in-vehicle setter it exposes — grep `MockGameBridge` for the method that marks a ped in a vehicle.

- [ ] **Step 2: Run to verify they fail**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~FollowerManagerVehicleLockTests"`
Expected: FAIL — no event-block on board; no can-leave lock when seated.

- [ ] **Step 3: Add the locked-set + edit `AssignFollowersToVehicle`** in `FollowerManager.VehicleAssignment.cs`. Add the field near `_lastBoardOrderMs`:
```csharp
        // Followers currently locked into the player's vehicle (can't-leave applied). Used to apply
        // the lock once on seating and restore can-leave exactly once when they stop riding.
        private readonly HashSet<int> _lockedInPlayerVehicle = new HashSet<int>();
```
In the loop, replace the seated branch and the board-order block:
```csharp
            foreach (var pedHandle in nearbyFollowers)
            {
                if (_gameBridge.IsPedInVehicle(pedHandle))
                {
                    _lastBoardOrderMs.Remove(pedHandle);
                    // Seated: keep it in (drive-by, no bailing) and let it perceive/shoot again.
                    if (_lockedInPlayerVehicle.Add(pedHandle))
                    {
                        _gameBridge.SetPedBlockPermanentEvents(pedHandle, false);
                        _gameBridge.SetPedCanLeaveVehicle(pedHandle, false);
                    }
                    continue;
                }
                if (!_seatAssigner.TryGetSeat(pedHandle, out var seat))
                    continue;

                if (_gameBridge.IsPedTryingToEnterVehicle(pedHandle))
                    continue;
                if (_lastBoardOrderMs.TryGetValue(pedHandle, out var last) && now - last < BoardReissueIntervalMs)
                    continue;

                if (_gameBridge.IsPedInCombat(pedHandle))
                    _gameBridge.ClearPedTasks(pedHandle);

                // Block threat reactions so the sprint-to-seat runs straight instead of stopping to
                // aim at nearby enemies (same trick Escort uses for the on-foot sprint).
                _gameBridge.SetPedBlockPermanentEvents(pedHandle, true);
                _gameBridge.TaskPedEnterVehicle(pedHandle, playerVehicle, seat);
                _lastBoardOrderMs[pedHandle] = now;
            }
```
Keep `nearbyFollowers`/`prioritizedSeats`/`_seatAssigner.Sync(...)` exactly as they are above this loop. If `AssignFollowersToVehicle` now exceeds 40 lines (CI0007), extract the per-ped body into a small private method `BoardOrLockFollower(int pedHandle, int playerVehicle, int now)`; keep it ≤40 lines.

- [ ] **Step 4: Add `RestoreLockedFollowers` + call it on the not-boarding branch.** In `FollowerManager.VehicleAssignment.cs`:
```csharp
        // Restores can-leave + clears the event-block on any follower that was locked into the
        // player's vehicle but is no longer riding (player got out, or the squad is grounded for
        // HoldArea/S&D). Applied once per follower, then forgotten.
        private void RestoreLockedFollowers()
        {
            if (_lockedInPlayerVehicle.Count == 0) return;
            foreach (var pedHandle in new List<int>(_lockedInPlayerVehicle))
            {
                _gameBridge.SetPedCanLeaveVehicle(pedHandle, true);
                _gameBridge.SetPedBlockPermanentEvents(pedHandle, false);
            }
            _lockedInPlayerVehicle.Clear();
        }
```
In `FollowerManager.cs`, the `Update` else branch (the not-boarding path, ~lines 254-259), call it first:
```csharp
            else
            {
                RestoreLockedFollowers();
                // On foot / holding / hunting: keep bodyguards grounded for the squad stance controller.
                OnFootBodyguardHandles = aliveFollowerHandles;
                SniperBodyguardHandles = FilterSniperHandles(followers, aliveFollowerHandles);
            }
```
(The `if (playerInVehicle && playerVehicle >= 0 && boardPlayerVehicle)` branch keeps calling `AssignFollowersToVehicle`; the else branch — player not in a vehicle, or squad grounded — restores. This guarantees the lock never leaks into on-foot combat.)

- [ ] **Step 5: Run the lock tests + full unit suite**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~FollowerManagerVehicleLockTests"` then the full `FactionWars.Tests.Unit` filter.
Expected: PASS; build clean (0/0). Existing FollowerManager tests still pass (the board path adds calls but doesn't change the on-foot list semantics).

- [ ] **Step 6: Commit**

```bash
git add src/FactionWars tests/FactionWars.Tests
git commit -m "fix: bodyguards board straight and stay seated in the player's vehicle (#140)"
```

---

## Self-Review

**Spec coverage:** new `SetPedCanLeaveVehicle` bridge method (Task 1) ✓; drive-by enabled when locking in (Task 1 Step 4 sets index 2) ✓; block-events on the board order (Task 2 Step 3) ✓; can't-leave + unblock once seated (Task 2 Step 3) ✓; restore can-leave + unblock when no longer riding (Task 2 Step 4) ✓; applied-once via `_lockedInPlayerVehicle` set (no per-frame native spam) ✓; scoped to the player-vehicle boarding path, heli/boat drive-by preserved ✓; `FileLogger` on new bridge behavior ✓; MockGameBridge tracking + tests ✓.

**Placeholder scan:** Task 2 Step 1 leaves the test bodies as guided fill-ins (the fixture's exact mock-arrange pattern must be read from `FollowerManagerTests`), with explicit assertions named (`GetPedBlockPermanentEventsForTest`, `GetPedCanLeaveVehicleForTest`) and the exact arrange ingredients listed. This is intentional — the arrange boilerplate depends on the existing fixture and must match it — not a vague placeholder.

**Type consistency:** `SetPedCanLeaveVehicle(int, bool)` and `GetPedCanLeaveVehicleForTest(int) -> bool` are defined in Task 1 and consumed identically in Task 2 and its tests. `_lockedInPlayerVehicle` (Task 2) is used consistently across the seated-lock and restore paths. `SetPedBlockPermanentEvents`/`GetPedBlockPermanentEventsForTest` are existing members used as-is.
