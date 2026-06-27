# Task 11 Report: Move On-Foot Tasking Out of FollowerManager

## Status: COMPLETE

## Files Changed

| File | Action |
|------|--------|
| `src/FactionWars/ScriptHookV/Managers/FollowerManager.cs` | Added `OnFootBodyguardHandles` property; rewrote `Update` body |
| `src/FactionWars/ScriptHookV/Managers/FollowerManager.OnFoot.cs` | **Deleted** via `git rm` |
| `src/FactionWars/ScriptHookV/Managers/FollowerManager.Death.cs` | **Created** (partial) — holds `GetAliveFollowerHandles` extracted to keep FollowerManager.cs ≤250 lines |
| `tests/FactionWars.Tests/Unit/ScriptHookV/Managers/FollowerManagerTests.cs` | Deleted 5 on-foot tests; added 1 new contract test |
| `tests/FactionWars.Tests/Unit/ScriptHookV/Managers/SquadStanceControllerTests.cs` | Added 1 carry-forward Escort test |
| `tests/FactionWars.Tests/Integration/ScriptHookV/FollowerManagerIntegrationTests.cs` | Adapted 3 integration tests |

## Tests Deleted and Why

### Unit tests deleted (FollowerManagerTests.cs)
1. **`Update_WhenPlayerExitsVehicle_ShouldOrderFollowersToExitVehicle`** — asserted `TaskPedLeaveVehicle` was called when player on foot but ped still in vehicle. This is `UpdateOnFootFollowers` behavior, now owned by `SquadStanceController.ApplyEscort`.
2. **`Update_WhenOnFootFollowerLostPlayerGroup_ShouldSetPedAsFollowerAgain`** — asserted `SetPedAsFollower` was called when follower lost player group. Relocated behavior.
3. **`Update_WhenPlayerDead_ShouldNotRepairFollowerGroup`** — asserted `SetPedAsFollower` not called when player dead. Tests `UpdateOnFootFollowers` dead-player guard, now in `ApplyEscort`.
4. **`Update_OnFootFollowerStuckNotFollowing_ShouldReassertOncePerInterval`** — tested the `_lastFollowReassertMs` throttle. Throttle and logic now live entirely in `SquadStanceController`.
5. **`Update_OnFootFollowerInCombat_ShouldNotReassertFollow`** — tested the combat-guard in `UpdateOnFootFollowers`. Now in `ApplyEscort`.

### Integration tests deleted/adapted (FollowerManagerIntegrationTests.cs)
- **`Update_OnFootFollowerLostPlayerGroup_ReattachesFollower`** — deleted. Asserted `FollowingPeds` contains ped after `Update`; FollowerManager no longer calls `SetPedAsFollower` in the `Update` path.
- **`Update_PlayerExitsVehicle_FollowersOrderedToExit`** — renamed and adapted to `Update_PlayerExitsVehicle_ExposesFollowerAsOnFootBodyguard`. Now asserts `OnFootBodyguardHandles` contains the ped (SquadStanceController owns the actual exit tasking).
- **`FullFlow_RecruitFollower_FollowsPlayer_EntersVehicle_DiesInCombat`** — Step 3 changed from `Assert.False(IsPedInVehicle)` to `Assert.Contains(pedHandle, OnFootBodyguardHandles)`.

## New Contract Test

```csharp
[Fact]
public void Update_PlayerOnFoot_ExposesAliveHandlesWithoutTasking()
{
    // Arrange: alive follower ped, player not in vehicle
    _followerServiceMock.Setup(s => s.GetFollowers(factionId)).Returns(followers);
    _gameBridgeMock.Setup(g => g.IsPedAlive(42)).Returns(true);
    _gameBridgeMock.Setup(g => g.IsPlayerInVehicle()).Returns(false);

    _manager.Update(factionId);

    Assert.Single(_manager.OnFootBodyguardHandles);
    Assert.Equal(42, _manager.OnFootBodyguardHandles[0]);
    _gameBridgeMock.Verify(g => g.SetPedAsFollower(It.IsAny<int>()), Times.Never);
}
```

## Carry-Forward Escort Controller Test

```csharp
[Fact]
public void Update_EscortStance_RepairsFollowerGroupForBodyguardNotFollowingPlayer()
{
    _controller = Build(); // starts in Escort by default
    int bg = _bridge.CreatePed("bg", new Vector3(1f, 0f, 0f));
    var party = new List<int> { bg };

    Assert.False(_bridge.IsPedFollowingPlayer(bg)); // pre-condition: NOT following

    _controller.Update(Anchor, 50f, party, new List<EnemyTarget>());

    Assert.True(_bridge.IsPedFollowingPlayer(bg)); // ApplyEscort called SetPedAsFollower
}
```

**Non-vacuous confirmation:** `CreatePed` in `MockGameBridge` does NOT add the ped to `_followingPeds`. The pre-condition `Assert.False` guards against a vacuous pass. `IsPedFollowingPlayer` returns `true` only after `SetPedAsFollower` is called (adds to `_followingPeds`). The controller is on Escort (default) so `ApplyEscort` runs. The ped is not in combat, not in a vehicle, player is not dead — all guards in `ApplyEscort` pass, so `SetPedAsFollower` executes. Verified by tracing `MockGameBridge.SetPedAsFollower` → `_followingPeds.Add`.

## TDD Evidence

1. Wrote contract test first (referencing `_manager.OnFootBodyguardHandles` before the property existed).
2. Build failed: `CI0017` (261 lines) → moved `GetAliveFollowerHandles` to `FollowerManager.Death.cs`.
3. Build succeeded (0 warnings, 0 errors).
4. Focused tests: 95/95 pass.
5. Full unit suite: 3534/3534 pass.

## Architecture Notes

- `OnFootBodyguardHandles` is a **property** (not a method), keeping FollowerManager within the ≤10 public method analyzer cap.
- `GetAliveFollowerHandles` moved to `FollowerManager.Death.cs` (new partial) to keep the main file ≤250 lines. Thematically cohesive: the method exists solely to detect and handle follower deaths.
- `FollowerManager.OnFoot.cs` deleted. All on-foot follow logic (throttle, combat guard, dead-player guard, leave-vehicle task) now lives exclusively in `SquadStanceController.ApplyEscort`.

## Concerns

None. The `Update` path is now simpler (no `UpdateOnFootFollowers` call), the dual-tasking race condition is eliminated, and the carry-forward Escort test gives meaningful coverage of the relocated logic.
