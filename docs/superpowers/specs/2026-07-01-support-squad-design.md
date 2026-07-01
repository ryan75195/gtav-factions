# Support Packages — Support Squad (Step 1) — Design

**Issue:** #146
**Status:** Approved (design), pending spec review

## Goal

Add a support-package system. The player **buys** packages from the commander and **calls**
them into the zone they're fighting in. Step 1 is one package type — a **Support Squad**: an
FBI SUV loaded with 8 friendly allied combatants that spawns outside the current zone, drives
to the player, dismounts within range, then hunts enemies in Search & Destroy. These allies
are friendly to the player and hostile to enemy factions but are **not** members of the
player's crew — temporary support.

The squad menu is restructured into a parent hub so "Support" (call) sits alongside "Manage
Squad".

## Locked decisions

- **Vehicle:** one **FBI SUV** (`fbi2`), fully loaded — **4 seats + 4 side rails = 8** combatants.
- **Composition (8):** 2 Sniper + 2 Gunner (SMG) + 4 Rifleman — a named constant, adjustable.
- **Buy vs call:** the **commander** interaction opens the **purchase** menu; the squad menu's
  new **Support** option **calls** a purchased package into the player's current zone.
- **Persistence:** owned-package count **persists across save/load** (on `FactionState`).
- **Price:** **$25,000** per package.
- **Concurrency (step 1):** one active support squad at a time; "call" is disabled while a
  support squad is inbound or alive.

## Architecture (reuse-first)

The only new engine primitive is a vehicle-drive bridge native. Everything else mirrors
existing managers/services (verified by codebase exploration).

- **Friendly-but-not-a-follower** is free from `RelationshipMatrixInitializer`
  (`src/FactionWars/ScriptHookV/Combat/RelationshipMatrixInitializer.cs`): spawn the peds in the
  player's faction relationship group (`factionId == playerFactionId`) and **skip**
  `SetPedAsFollower`/group membership. That group is already companion to `PLAYER` and hostile to
  enemy factions.
- **Spawn + per-role stats** mirror `BattleAttackerManager.ConfigureAttacker`
  (`src/FactionWars/ScriptHookV/Managers/BattleAttackerManager.Spawning.cs:111`) with
  `IZoneCombatantSpawner.Spawn(playerFactionId, playerFactionId, model, pos, zoneId)` and
  `ICombatantStatsProvider.GetRoleStats(CombatantCategory.Friendlies, role)`.
- **Search & Destroy** reuses `SquadStanceController` — `SupportSquadManager` owns a **private**
  instance forced to `SearchAndDestroy` (its 5 resolver deps are `new`-able exactly as
  `GameLoopController.Initialization.cs:53` constructs the player's). Do NOT share the player's
  instance (per-squad state). Enemy list from `GameLoopController.GatherHostileHandles()` →
  `IEnemyTargetCollector.CollectAll(...)`.
- **Seat peds** via `IGameBridge.SetPedIntoVehicle(ped, veh, seatIndex)` (1-based: 0=driver).
- **Drive in** needs a NEW `IGameBridge.TaskVehicleDriveToCoord` (wraps
  `TASK_VEHICLE_DRIVE_TO_COORD`); nothing like it exists today.
- **Dismount within range** models on `DefenderRallyController`: per-tick
  `GetPlayerPosition().DistanceTo(GetVehiclePosition(suv)) < DismountRange` → `TaskPedLeaveVehicle`.
- **Menus** mirror `RecruitmentMenuController` (parent; `XRequested`/`BackRequested` + the
  `BackTo(menuId, toParent)` helper in `GameLoopController.InitializationMenus.cs`) and
  `ShopMenuController` (purchase: disable-when-unaffordable, deduct cash, notify, refresh).
- **Persistence** extends `FactionState` (`src/FactionWars/Factions/Models/FactionState.cs`) +
  `FactionStateData` (`src/FactionWars/Persistence/Models/FactionStateData.cs`), which already
  flow through the save/load sidecar (`GameGtateManager`).
- **Roles:** `DefenderRole` = Sniper(4), Gunner(1), Rifleman(2); integer values are a
  persistence contract — never reorder.

## Menu tree (after — revised 2026-07-01 post-playtest)

The first cut replaced the commander's main-menu callback and left the hub buried under
F7 → Recruitment. Revised per user feedback: **additive, not replacing**.

```
Main (F7 or Commander E — same menu)
├─ Zone Management / Recruitment / Shop / Settings   (unchanged)
│  └─ Recruitment → Squad → Squad Hub                (kept for discoverability)
└─ Support (purchase)  → SupportMenuController (NEW item; buy packages. Also reachable
                          via F7 — accepted: buying is just cash, the CALL is zone-gated)

D-pad Left / Left Arrow (control 174)
├─ TAP  (<250 ms)      → Squad Hub (menu)            (NEW)
│                        ├─ Manage Squad  → existing SquadMenuController
│                        └─ Support       → SupportCallMenuController (call)
└─ HOLD (≥250 ms)      → stance radial (unchanged, now opens after the hold threshold)
```

Revision details:

- Commander callback reverts to `ShowMainMenu()`; `MainMenuController` gains a `SupportItemId`
  item dispatched in the existing `Lifecycle.cs` switch. `SupportMenuController`'s back action
  becomes "return to main menu" (its parent from both entry points), replacing the no-op.
- Tap-vs-hold via a new portable `HoldTapDetector` (alongside `SquadRadialMenu` in `UI.Models`):
  fed `(isPressed, nowMs)` per frame with injected `Func<long>` time (`Environment.TickCount`
  in production); release before 250 ms → Tap, held past it → Hold. `SquadRadialMenuRenderer`
  drives the detector and exposes a `Tapped` event; the radial opens on Hold instead of raw press.
- `Tapped` → hub is guarded by `IsMenuVisible == false` (left is also a NativeUI navigation key).
- The hub has two parents, so its back target is set at show time via the existing
  `_menuBackActions` map: from Recruitment → back to Recruitment; from tap → close to gameplay.

## Error handling / edge cases

- **Affordability:** purchase blocked when `GetPlayerMoney() < 25000` (menu item disabled + guard).
- **Call preconditions:** requires an owned package (`count > 0`), the player in a valid zone
  (`TerritoryManager.CurrentZone != null`), and no active support squad — else the call item is
  disabled / no-ops with a notification.
- **Legacy saves:** the new `FactionStateData` field defaults to 0 (follow the existing
  legacy-field migration style so old sidecars load).
- **Squad lifecycle:** when all support allies are dead/streamed out, the active squad is cleared,
  re-enabling "call". Dead-handle pruning copies `BattleAttackerManager.Update`.

## FBI SUV side-rail risk (verify in-game early)

The FBI SUV's 4 side-rail (hang-on) positions must be fillable. In GTA V these are typically
extra seat indices, so `SetPedIntoVehicle(ped, suv, index)` for indices 4–7 should place peds on
the rails. **Verify in-game.** If only 4 seats are exposed: fall back to seating 4 and either use
a native "warp onto vehicle" for the rest, switch to a larger vehicle, or reduce the count —
surface the choice to the user rather than silently changing behavior.

## Testing

Unit-testable against `MockGameBridge` + Moq:
- **Bridge native:** mock records the drive target per vehicle.
- **Persistence:** `FactionState`↔`FactionStateData` round-trip preserves the package count.
- **Economy service:** buy deducts cash + increments; unaffordable no-ops; consume decrements /
  fails at 0.
- **Manager:** `CallSupportSquad` spawns a vehicle + 8 friendly (non-follower) peds seated and
  issues a drive task; `Update` dismounts within range and hands to S&D; death-pruning frees the
  slot.
- **Menus:** `SupportMenuController` (item present/priced, disabled-when-broke, purchase),
  `SquadHubMenuController` + `SupportCallMenuController` (items, call gating), and
  `MenuSystemIntegrationTests` navigation (Recruitment → hub → Manage/Support).
- Native drive/seat/rail and S&D effects are in-game/log-verified per repo convention.

## Out of scope (YAGNI, later steps)

- Additional package types (air support, artillery, etc.) — Step 1 is the Support Squad only.
- Multiple concurrent support squads.
- Rebalancing the player's own squad/follower system.
- In-game hot-config of composition/price (uses code constants for now).
