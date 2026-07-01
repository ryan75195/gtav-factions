# Support Squad (Step 1) — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development. Steps use checkbox (`- [ ]`) syntax.

**Goal:** Buy Support Squad packages from the commander; call one into your current zone — an FBI SUV of 8 friendly allies that drives in, dismounts, and hunts enemies in Search & Destroy.

**Architecture:** Reuse-first (see the design spec `docs/superpowers/specs/2026-07-01-support-squad-design.md`). New engine primitive: a `TaskVehicleDriveToCoord` bridge native. New Economy service for buy/inventory. Owned count persists on `FactionState`/`FactionStateData`. New `SupportSquadManager` runs spawn→drive→dismount→S&D via a private `SquadStanceController`. Menus mirror `RecruitmentMenuController` (parent) + `ShopMenuController` (purchase).

**Tech Stack:** C#/.NET 4.8, ScriptHookVDotNet3, xUnit + Moq.

## Global Constraints

- Strict TDD; failing test first. Build **0 warnings / 0 errors**.
- Analyzers (errors in this repo): ≤5 ctor params (CI0005 → use a `*Dependencies` bundle in `ScriptHookV/Models/`); ≤250 lines per partial/file (CI0017 → split into partials); ≤40 lines/method (CI0007); ≤10 public methods/class (CI0004); service classes need a first-party interface; one public top-level type per file (CI0016); no tuple returns (use a `Models` result type); CRLF everywhere; each new public type gets a `*Tests` fixture.
- New `IGameBridge` behavior includes a `FileLogger` line; native effects are in-game/log-verified.
- `DefenderRole` integer values are a persistence contract — never reorder.
- Branch `feat/146-support-squad` (created). Commit per task; do not push until reviewed.
- Build: `dotnet build FactionWars.sln --no-incremental`. Tests: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~FactionWars.Tests.Unit"`. Pre-commit hook runs build + unit tests (allow up to 10 min).

---

### Task 1: Bridge native `TaskVehicleDriveToCoord`
**Files:** `src/FactionWars/Core/Interfaces/IGameBridge.cs`; new `src/FactionWars/ScriptHookV/GameBridge.VehicleDrive.cs`; `src/FactionWars/Core/Utils/MockGameBridge.cs`; test `tests/FactionWars.Tests/Unit/Core/MockGameBridgeVehicleDriveTests.cs`.
- Declare `void TaskVehicleDriveToCoord(int vehicleHandle, Vector3 dest, float speed, float stopRange)`.
- Real impl (new partial): drive the vehicle's driver to `dest` via `Function.Call(Hash.TASK_VEHICLE_DRIVE_TO_COORD, driverHandle, vehicleHandle, dest.X, dest.Y, dest.Z, speed, 0, vehicleModelHash, drivingStyle, stopRange, -1f)` (use a sane driving style constant, e.g. `786603` = normal+avoid; confirm arg order against SHVDN `Hash.TASK_VEHICLE_DRIVE_TO_COORD`). Guard null/exists; `FileLogger.AI` line.
- `MockGameBridge`: track `(dest, speed, stopRange)` per vehicle + `GetVehicleDriveTargetForTest(int)` getter; clear in `Reset()`.
- Tests: mock records the call and getter returns it.

### Task 2: Persist owned support-package count
**Files:** `src/FactionWars/Factions/Models/FactionState.cs`; `src/FactionWars/Persistence/Models/FactionStateData.cs`; tests in `tests/FactionWars.Tests/Unit/...` mirroring existing `FactionState`/`FactionStateData` tests (find them first).
- `FactionState`: add `int SupportSquadPackages` (private field, public getter) + `AddSupportSquadPackage(int count=1)` and `bool TryConsumeSupportSquadPackage()` (decrement if >0, guard ≥0).
- `FactionStateData`: serialize the field in `FromFactionState`/`ToFactionState`, defaulting to 0 for legacy saves (mirror the existing legacy-field handling in `ToFactionState`).
- Tests: round-trip N packages `FactionState`→`FactionStateData`→`FactionState`; consume/guard behavior.

### Task 3: Economy `ISupportPackageService`
**Files:** new `src/FactionWars/Economy/Interfaces/ISupportPackageService.cs` + `src/FactionWars/Economy/Services/SupportPackageService.cs`; register in `src/FactionWars/ScriptHookV/ServiceContainerFactory.PersistenceEconomy.cs` (`RegisterEconomyServices`); test `tests/FactionWars.Tests/Unit/Economy/SupportPackageServiceTests.cs`.
- Interface: `int GetSupportSquadCost()` (25000 const), `bool CanAfford()`, `bool PurchaseSupportSquad(string factionId)`, `int GetOwnedCount(string factionId)`, `bool TryConsume(string factionId)`.
- Impl deps (≤5, all interfaces): `IGameBridge`, `IFactionService`. `PurchaseSupportSquad`: `CanAfford` → `AddPlayerMoney(-cost)` → `factionState.AddSupportSquadPackage()`. `TryConsume` → `factionState.TryConsumeSupportSquadPackage()`. Mirror `TroopPurchaseService`/`DefenderDeploymentService`.
- Tests: buy deducts + increments; unaffordable no-ops; owned count; consume decrements / fails at 0.

### Task 4: `SupportSquadManager` (spawn → drive → dismount → S&D)
**Files:** new partials `src/FactionWars/ScriptHookV/Managers/SupportSquadManager*.cs`; `src/FactionWars/ScriptHookV/Models/SupportSquadManagerDependencies.cs`; wiring in `GameLoopController.cs` (field), a `GameLoopController.Initialization*.cs` partial (construct), `GameLoopController.SystemUpdates.cs` (tick), `GameLoopController.AbortCleanup.cs` (null-out); test `tests/FactionWars.Tests/Unit/ScriptHookV/Managers/SupportSquadManagerTests.cs`.
- Deps bundle: `IGameBridge`, `IZoneCombatantSpawner`, `ICombatantStatsProvider`, `IZoneService`, plus the spawn-service bundle other managers use (`ResolveSpawnServices()`); `playerFactionId` ctor arg. Own a private `SquadStanceController` (construct its 5 resolvers with `new`, as `GameLoopController.Initialization.cs:53`).
- Composition constant: `{ Sniper:2, Gunner:2, Rifleman:4 }` (8). Model constant `"fbi2"`; `DismountRange` (~30m); `SpawnMargin` beyond `Zone.Radius`.
- `void CallSupportSquad(Zone zone)`: outside-edge point (`zone.Center + dir*(zone.Radius+margin)`, snap `GetNearestRoadPosition`); `CreateVehicle("fbi2", pt)`; spawn 8 friendly allies (spawner with `playerFactionId==playerFactionId`, stats via `CombatantStatsProvider` `Friendlies`, `ConfigureAttacker`-style weapon/stat block, NO `SetPedAsFollower`); `SetPedIntoVehicle(ped, suv, i)` for i in 0..7 (rails = the flagged risk); `TaskVehicleDriveToCoord(suv, playerPos, speed, stopRange)`; store active-squad state, phase=`Inbound`.
- `bool HasActiveSquad` (for menu gating).
- `void Update()`: `Inbound` → when within `DismountRange` of the player, `TaskPedLeaveVehicle` each ally + phase=`Engaging`. `Engaging` → private `SquadStanceController.Update(zone.Center, zone.Radius, aliveHandles, enemyTargets, rolesByHandle)` (enemy list injected via a delegate/dep the GameLoop provides — or pass a `Func` in the deps that returns hostiles). Prune dead handles (copy `BattleAttackerManager.Update`); when empty, clear active squad.
- Tests (MockGameBridge + Moq spawner/stats): `CallSupportSquad` creates the vehicle, spawns 8 friendly non-follower peds seated, issues the drive task, sets `HasActiveSquad`; `Update` dismounts within range; death-pruning clears `HasActiveSquad`.

### Task 5: `SupportMenuController` (purchase, from commander)
**Files:** new `src/FactionWars/ScriptHookV/UI/SupportMenuController.cs` (+ `SupportMenuControllerDependencies.cs` if >5 deps); repoint commander callback in `GameLoopController.Initialization.cs` (~line 213) + register back target in `GameLoopController.InitializationMenus.cs`; test `tests/FactionWars.Tests/Unit/ScriptHookV/UI/SupportMenuControllerTests.cs`.
- Menu id `support_menu`. Items: disabled cash, disabled owned-count line, `Support Squad — $25,000` (`IsEnabled = service.CanAfford()`), Back. Select → `ISupportPackageService.PurchaseSupportSquad(playerContext.CurrentFactionId)` → notify → `Show()` refresh. `public event EventHandler? BackRequested`. Mirror `ShopMenuController`.
- Commander: `GameLoopController.Initialization.cs` change `_ => _mainMenuController?.ShowMainMenu()` → `_ => _supportMenuController?.Show()`; `BackTo(SupportMenuController.MenuId, close-to-gameplay)`.
- Tests: item present/priced, disabled when broke, purchase deducts + increments owned count (via the real `SupportPackageService` over `MockGameBridge`, or a mock service).

### Task 6: Squad hub restructure + Support-call screen
**Files:** new `src/FactionWars/ScriptHookV/UI/SquadHubMenuController.cs`, `SupportCallMenuController.cs`; re-wire `GameLoopController.InitializationMenus.cs`; add fields in `GameLoopController.cs`; tests `SquadHubMenuControllerTests`, `SupportCallMenuControllerTests`; update `tests/FactionWars.Tests/Integration/ScriptHookV/MenuSystemIntegrationTests.cs`.
- `SquadHubMenuController` (id `squad_hub_menu`): items **Manage Squad**, **Support**, Back; events `ManageSquadRequested`, `SupportRequested`, `BackRequested` (mirror `RecruitmentMenuController`).
- `SupportCallMenuController` (id `support_call_menu`): owned-count line + **Call Support Squad** (`IsEnabled = ownedCount>0 && !manager.HasActiveSquad && territory.CurrentZone!=null`). Select → `ISupportPackageService.TryConsume(factionId)` then `_supportSquadManager.CallSupportSquad(currentZone)` → notify. `BackRequested`.
- Re-wire: `_recruitmentMenuController.SquadRequested` → `_squadHubMenuController.Show()`; `SquadHub.ManageSquadRequested` → `_squadMenuController.Show()`; `SquadHub.SupportRequested` → `_supportCallMenuController.Show()`; back targets: `_squadMenuController` backs to hub; `_supportCallMenuController` backs to hub; hub backs to Recruitment.
- Tests: hub items + navigation events; call gating (disabled with 0 owned / active squad / no zone); integration navigation Recruitment → hub → {Manage, Support}.

---

## Self-Review
**Spec coverage:** buy (T3,T5) ✓; persist (T2) ✓; call + spawn/drive/dismount/S&D (T1,T4) ✓; friendly-not-follower (T4) ✓; menu hub + support-call (T6) ✓; FBI SUV 8 seats+rails (T4, risk-flagged) ✓; commander purchase entry (T5) ✓; price/persistence/one-active (T2,T3,T4) ✓.
**Dependencies:** T4 needs T1–T3; T5 needs T3; T6 needs T4–T5. Land T1→T2→T3→T4→T5→T6.
**Type consistency:** `ISupportPackageService` methods (T3) used by T5/T6; `SupportSquadManager.CallSupportSquad(Zone)`/`HasActiveSquad` (T4) used by T6; `TaskVehicleDriveToCoord` (T1) used by T4; `FactionState.SupportSquadPackages`/`Add`/`TryConsume` (T2) used by T3.
