# Zone Held While the Player Is Present — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** When a player-owned zone's last defender dies while the player is in that zone and alive, defer the loss into a grace state; the zone is lost only when the player dies or leaves it undefended, and redeploying a defender saves it.

**Architecture:** `FriendlyDefenderManager.HandleTerritoryLost` defers the ownership transfer (adding the zone to an in-memory grace set) when the player holds the zone; a new per-tick `MonitorGraceZones()` (called from `Update()`) resolves each grace zone to saved (defender respawned), lost (player died), or lost (player left undefended), and drops stale entries when ownership changed elsewhere.

**Tech Stack:** C#/.NET 4.8, ScriptHookVDotNet3, xUnit + Moq. Custom Roslyn analyzers.

**Battle-path note (resolved during planning):** `ZoneBattleManager` only auto-collapses/transfers a defeated defender's zone when `battle.IsPlayerPresent == false`; with the player present it skips tick combat, and `ApplyBattleOutcome` transfers only on a player *win*. So a player-held zone's loss goes through `HandleTerritoryLost` — the path this plan covers. No battle-path change is needed.

## Global Constraints

- Strict TDD: failing test first, watch it fail, then implement.
- Build must stay **0 warnings / 0 errors**. Analyzers: CI0007 method ≤40 lines; CI0017 file ≤250 lines; CI0004 ≤10 public methods/class; CI0016 one public top-level type per file; ENDOFLINE = CRLF.
- No `#pragma warning disable CI*/CA*`, no skipped tests, no git-hook bypass.
- Grace ends on player **death** OR **leaving the zone while undefended**; redeploying a defender **saves** it. Check order per tick: **saved → died → left**.
- No new interfaces or bridge methods — `FriendlyDefenderManager` already holds `_gameBridge`, `_zoneService`, `_allocationService`, `_playerFactionId`, `GetSpawnedDefenderCount(zoneId)`, and a per-tick `Update()`. `IGameBridge.GetPlayerPosition()`, `IsPlayerDead()`, `ShowNotification(string)`, and `IZoneService.GetZoneAtPosition(Vector3)`/`GetZone(string)` all exist.
- Branch: `feat/141-zone-grace-on-player-present` (already created). Commit per task; do not push until reviewed.
- Build: `dotnet build FactionWars.sln --no-incremental`. Unit tests: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~FactionWars.Tests.Unit"`. Pre-commit hook runs build + unit tests (allow up to 10 min).

---

### Task 1: Grace-state deferral + per-tick monitor in `FriendlyDefenderManager`

**Files:**
- Modify: `src/FactionWars/ScriptHookV/Managers/FriendlyDefenderManager.UpdateAndSpawning.cs` (grace set field; rewrite `HandleTerritoryLost`; add `MonitorGraceZones`, `FinalizeZoneLoss`, `PlayerIsInZoneAndAlive`, `PlayerCurrentZoneId`; call `MonitorGraceZones()` from `Update()`)
- Test: `tests/FactionWars.Tests/Unit/ScriptHookV/Managers/FriendlyDefenderManagerGraceTests.cs` (new)

**Interfaces:**
- Consumes: `_gameBridge.GetPlayerPosition()`, `_gameBridge.IsPlayerDead()`, `_gameBridge.ShowNotification(string)`, `_zoneService.GetZoneAtPosition(Vector3)`, `_zoneService.GetZone(string)`, `_zoneService.TransferZoneOwnership(string, string?)`, `GetSpawnedDefenderCount(string)`, `_playerFactionId`.

- [ ] **Step 1: Write the failing tests.** Create `tests/FactionWars.Tests/Unit/ScriptHookV/Managers/FriendlyDefenderManagerGraceTests.cs`. Read `FriendlyDefenderManagerDeathTests.cs` first to copy the exact fixture setup (how it builds `FriendlyDefenderManager` via `FriendlyDefenderManagerDependencies`, the `Mock<IGameBridge>`/`Mock<IZoneService>` setup, how it drives a defender to death so `HandleTerritoryLost` is reached, and how `GetSpawnedDefenderCount` is made to return 0). Use the SAME mock types that fixture uses (Moq). Cover these behaviors; the exact way you trigger "all defenders dead" must mirror the death-tests fixture:

```csharp
        // A) Player in the zone + alive when the last defender dies -> loss deferred (no transfer), grace entered.
        [Fact]
        public void LastDefenderDies_PlayerInZoneAndAlive_DoesNotTransferOwnership()
        { /* arrange: zone owned by player; GetZoneAtPosition(playerPos).Id == zoneId; IsPlayerDead()==false; drive all defenders dead; assert _zoneServiceMock.Verify(z => z.TransferZoneOwnership(zoneId, It.IsAny<string?>()), Times.Never) */ }

        // B) Player NOT in the zone when the last defender dies -> immediate loss (unchanged behavior).
        [Fact]
        public void LastDefenderDies_PlayerNotInZone_TransfersToNeutralImmediately()
        { /* GetZoneAtPosition(playerPos) returns a different/no zone; assert TransferZoneOwnership(zoneId, null) called once */ }

        // C) Zone in grace, then a defender is present again -> saved (still no transfer).
        [Fact]
        public void GraceZone_DefenderRedeployed_IsSavedNotLost()
        { /* enter grace via (A); then GetSpawnedDefenderCount(zoneId) returns > 0; call Update(); assert TransferZoneOwnership never called for zoneId */ }

        // D) Zone in grace, player dies -> lost.
        [Fact]
        public void GraceZone_PlayerDies_TransfersToNeutral()
        { /* enter grace; then IsPlayerDead() returns true; call Update(); assert TransferZoneOwnership(zoneId, null) called once */ }

        // E) Zone in grace, player leaves while still undefended -> lost.
        [Fact]
        public void GraceZone_PlayerLeavesUndefended_TransfersToNeutral()
        { /* enter grace; then GetZoneAtPosition(playerPos).Id != zoneId, GetSpawnedDefenderCount==0, IsPlayerDead()==false; call Update(); assert TransferZoneOwnership(zoneId, null) called once */ }

        // F) Zone in grace but ownership already changed elsewhere -> grace dropped, no extra transfer.
        [Fact]
        public void GraceZone_OwnershipChangedElsewhere_DroppedWithoutTransfer()
        { /* enter grace; then GetZone(zoneId).OwnerFactionId != _playerFactionId; call Update(); assert TransferZoneOwnership(zoneId, ...) NOT called again */ }
```
Fill each body using the death-tests fixture's arrange pattern. If a behavior genuinely can't be expressed against that fixture, report DONE_WITH_CONCERNS rather than weakening it.

- [ ] **Step 2: Run to verify they fail**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~FriendlyDefenderManagerGraceTests"`
Expected: FAIL — today `HandleTerritoryLost` transfers unconditionally (A/C/D/E/F fail; B passes).

- [ ] **Step 3: Add the grace set + helpers + rewrite in `FriendlyDefenderManager.UpdateAndSpawning.cs`.** Add the field near the other collections at the top of the partial (or with the existing `private` fields in `FriendlyDefenderManager.cs` — put it wherever the class's fields live; if adding to the `.UpdateAndSpawning.cs` partial, place it just inside the class):
```csharp
        // Zones whose last defender died while the player was holding them: ownership transfer is
        // deferred until the player dies, leaves undefended, or (saved) a defender is redeployed.
        private readonly HashSet<string> _undefendedGraceZones = new HashSet<string>();
```
Rewrite `HandleTerritoryLost`:
```csharp
        private void HandleTerritoryLost(string zoneId)
        {
            if (PlayerIsInZoneAndAlive(zoneId))
            {
                if (_undefendedGraceZones.Add(zoneId))
                {
                    var name = _zoneService.GetZone(zoneId)?.Name ?? zoneId;
                    _gameBridge.ShowNotification($"~y~{name} is undefended — hold it or redeploy!");
                    FileLogger.Zone($"HandleTerritoryLost: {zoneId} undefended but player present — grace, not lost.");
                }
                return;
            }

            FinalizeZoneLoss(zoneId);
        }

        private void FinalizeZoneLoss(string zoneId)
        {
            _undefendedGraceZones.Remove(zoneId);
            _zoneService.TransferZoneOwnership(zoneId, null);
            TerritoryLost?.Invoke(this, new TerritoryLostEventArgs(zoneId));
        }

        private bool PlayerIsInZoneAndAlive(string zoneId)
            => !_gameBridge.IsPlayerDead() && PlayerCurrentZoneId() == zoneId;

        private string? PlayerCurrentZoneId()
            => _zoneService.GetZoneAtPosition(_gameBridge.GetPlayerPosition())?.Id;
```
Add `MonitorGraceZones` and call it from `Update()` (put the call at the very top of `Update()`, before the defender-death loop):
```csharp
        private void MonitorGraceZones()
        {
            if (_undefendedGraceZones.Count == 0) return;

            foreach (var zoneId in new List<string>(_undefendedGraceZones))
            {
                // Ownership changed via another path (battle/reclaim): nothing to hold.
                if (_zoneService.GetZone(zoneId)?.OwnerFactionId != _playerFactionId)
                {
                    _undefendedGraceZones.Remove(zoneId);
                    continue;
                }

                // Saved: a defender exists again (player redeployed).
                if (GetSpawnedDefenderCount(zoneId) > 0)
                {
                    _undefendedGraceZones.Remove(zoneId);
                    var name = _zoneService.GetZone(zoneId)?.Name ?? zoneId;
                    _gameBridge.ShowNotification($"~g~{name} secured.");
                    continue;
                }

                // Lost: player died, or left the zone while it is still undefended.
                if (_gameBridge.IsPlayerDead() || PlayerCurrentZoneId() != zoneId)
                {
                    FinalizeZoneLoss(zoneId);
                }
            }
        }
```
In `Update()` (top, after the local var decls, before the spawned-defender loop):
```csharp
            MonitorGraceZones();
```
Keep every method ≤40 lines (CI0007) — `MonitorGraceZones` is ~20. Confirm `using System.Collections.Generic;` is present (it is).

- [ ] **Step 4: Run the grace tests to verify they pass**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~FriendlyDefenderManagerGraceTests"`
Expected: PASS (6/6).

- [ ] **Step 5: Build + full unit suite**

Run: `dotnet build FactionWars.sln --no-incremental` then the full `FactionWars.Tests.Unit` filter.
Expected: clean build (0/0), all PASS. Existing `FriendlyDefenderManagerDeathTests` still pass — check any test that asserted immediate `TransferZoneOwnership` on all-defenders-dead: if such a test does NOT set the player as present in the zone, it still passes (immediate loss path). If a death-test incidentally set up the player inside the zone, it now goes to grace — update that test to reflect the new behavior (deferred), not the old (immediate), since the new behavior is correct.

- [ ] **Step 6: Commit**

```bash
git add src/FactionWars tests/FactionWars.Tests
git commit -m "feat: hold a zone in grace while the player is present instead of losing it on last defender death (#141)"
```

---

## Self-Review

**Spec coverage:** defer in `HandleTerritoryLost` when player in zone + alive (Step 3) ✓; immediate loss when not present (unchanged else path) ✓; `MonitorGraceZones` saved→died→left order (Step 3) ✓; ownership-change safety drop (Step 3) ✓; called from `Update()` ✓; notifications on grace-enter and save ✓; `TerritoryLost` fired only on real loss via `FinalizeZoneLoss` ✓; no new interfaces/bridge methods ✓; battle path confirmed not a bypass when player present (plan header) ✓.

**Placeholder scan:** the test bodies (Step 1) are guided fill-ins keyed to the existing death-tests fixture, with concrete `Times.Never`/`Times.Once` assertions and the exact mock conditions per case — intentional (arrange boilerplate must match the fixture), not vague.

**Type consistency:** `_undefendedGraceZones` (HashSet<string>), `FinalizeZoneLoss(string)`, `PlayerIsInZoneAndAlive(string)`, `PlayerCurrentZoneId() -> string?`, `MonitorGraceZones()` used consistently. `GetSpawnedDefenderCount(string) -> int` and `TransferZoneOwnership(string, string?)` match existing signatures.
