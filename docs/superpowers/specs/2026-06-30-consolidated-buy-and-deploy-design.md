# Consolidated Buy-and-Deploy for Zone Defenders — Design

**Issue:** #134
**Status:** Approved (design), pending spec review

## Goal

Collapse the two-step player flow for zone defenders — *buy troops into the faction
reserve pool* (Defenders menu) then *allocate reserve troops to a zone* (Zone menu) —
into a single action. In the zone menu, selecting a defender tier now **buys and deploys**
it to that zone in one step. The reserve pool remains in the domain but becomes an
internal/AI-only concept the player never interacts with directly.

## Background / Current State

Two player-facing flows exist for zone defenders:

- **Buy → reserve:** `DefendersMenuController` calls `ITroopPurchaseService.PurchaseTroops(factionId, tier, 1)`,
  which deducts the player's GTA cash and adds the troop to the faction's **reserve pool**
  (`FactionState` reserve operations).
- **Allocate reserve → zone:** `ZoneManagementMenuController` (zone detail menu) calls
  `IZoneDefenderAllocationService.AllocateTroops(factionState, zoneId, tier, 1)`, which moves
  a troop from the reserve pool into a per-zone allocation (`IZoneDefenderAllocationRepository`).
  It also offers `WithdrawTroops` (zone → reserve).

Accounting: `PurchaseTroops` = cash↓, reserve↑. `AllocateTroops` = reserve↓, zone-allocation↑.
Running both back-to-back nets cash↓ + zone-allocation↑ with the reserve unchanged. Zone
allocations live in their own repository, independent of reserve once placed.

The reserve pool is **also** used internally by AI factions (`BackgroundBattleSimulator`,
recruitment, simulated battles via `SetAllocation`). It therefore stays in the domain
unchanged — only the player's UI flow changes.

### Current default tiers in each menu

- Defenders (buy) menu: 5 tiers — Grunt, Gunner, Rifleman, Rocketeer, Sniper.
- Zone menu (allocate): 3 tiers — Grunt, Gunner, Rifleman — plus 3 matching withdraw items.

## Design Decisions (locked)

1. **Remove the buy-to-reserve menu** (`DefendersMenuController`) entirely. The player never
   sees a reserve pool for zone defenders. Reserve stays internal for AI.
2. **Remove withdraw** entirely from the player UI — once deployed, a troop is committed
   (no refund, no return-to-reserve). The domain `WithdrawTroops` method is kept (still tested;
   a coherent reserve↔zone operation AI/future features may use) but is no longer player-facing.
3. **All 5 tiers** are deployable from the zone menu (preserving the access the removed buy
   menu had for Rocketeer/Sniper).
4. **Unaffordable tiers are disabled with their cost shown** (matching the old buy menu's
   `CanAfford`-based disabling).
5. Implementation uses **Approach A**: a new domain service composing the two existing,
   already-tested services.

## Component 1 — `IDefenderDeploymentService` (Economy layer)

New service that orchestrates buy-and-deploy. It is the single facade the zone menu talks to
for all deployment concerns (so the controller gains exactly one new dependency, not three).

```csharp
namespace FactionWars.Economy.Interfaces
{
    public interface IDefenderDeploymentService
    {
        // Buys `count` troops of `tier` and deploys them directly to `zoneId`.
        // Validates affordability first; on failure makes no state change.
        DeploymentResult BuyAndDeploy(FactionState factionState, string zoneId, DefenderRole tier, int count);

        // Cost of a single troop of `tier` (for menu labels). Forwards to ITroopPurchaseService.
        int GetTroopCost(DefenderRole tier);

        // Whether the player can afford `count` of `tier` (for disabling menu items).
        bool CanAfford(DefenderRole tier, int count);
    }
}
```

`DeploymentResult` is a small immutable result type (mirrors `TroopPurchaseResult`):
`Status` (`Success` | `InsufficientFunds`), `TotalCost`, and a human-readable `Message`.

**Implementation (`DefenderDeploymentService`)** depends on `ITroopPurchaseService` and
`IZoneDefenderAllocationService` (both interfaces — CI0014-safe). `BuyAndDeploy`:

1. Validate args (non-null `factionState`/`zoneId`, `count > 0`, `tier` valid).
2. If `!_purchaseService.CanAfford(tier, count)` → return `InsufficientFunds`, no state change.
3. `_purchaseService.PurchaseTroops(factionState.FactionId, tier, count)` (cash↓, reserve↑).
4. `_allocationService.AllocateTroops(factionState, zoneId, tier, count)` (reserve↓, zone↑).
   This cannot fail for insufficient reserve because step 3 just added `count`.
5. Return `Success` with the total cost.

`GetTroopCost`/`CanAfford` forward to `ITroopPurchaseService`.

Registered as a singleton in `ServiceContainerFactory` alongside the other economy services.

## Component 2 — Zone menu (`ZoneManagementMenuController`)

- Replace the 3 allocate + 3 withdraw items with **5 deploy items** (Grunt, Gunner, Rifleman,
  Rocketeer, Sniper), each labeled with its cost, e.g. `Deploy Rifleman — $1000`.
- Each tier whose cost the player **cannot afford is disabled**, with the cost still shown in
  the label (via `_deploymentService.CanAfford` / `GetTroopCost`).
- Selecting a tier calls `_deploymentService.BuyAndDeploy(factionState, _selectedZoneId, tier, 1)`.
  On `Success` the menu refreshes, showing the updated per-tier zone allocation and the player's
  cash. On `InsufficientFunds` it shows a `~r~Not enough cash` notification (defensive; the item
  is already disabled when unaffordable).
- The menu continues to show the **current per-tier allocation for the selected zone** and the
  player's cash. The reserve summary line is removed (reserve is no longer player-facing).
- Withdraw items and their handlers are removed.

### Constructor dependencies bundle

`ZoneManagementMenuController` currently takes 5 constructor parameters; adding
`IDefenderDeploymentService` would make 6 and trip **CI0005** (>5 params, a warning the repo
keeps at zero). Introduce `ZoneManagementMenuControllerDependencies`
(in `ScriptHookV/Models/`, matching the existing `ArmyMenuControllerDependencies` /
`SquadMenuControllerDependencies` pattern) bundling the existing five dependencies plus the new
`IDefenderDeploymentService`, and refactor the constructor to take the bundle. Update the
construction site in `GameLoopController.InitializationMenus`.

## Component 3 — Removals

- Delete `DefendersMenuController` and its construction + `BackRequested` wiring in
  `GameLoopController.InitializationMenus`.
- Remove the **Defenders** option from `RecruitmentMenuController` (the `DefendersItemId` item,
  the `DefendersRequested` event, and its `case` handler) and the corresponding
  `DefendersRequested` subscription in `GameLoopController.InitializationMenus`. The Recruitment
  menu then offers Squad (+ Back).
- Delete `DefendersMenuController`'s test fixture and update any menu-navigation/integration
  tests that referenced it or the recruitment Defenders entry.
- The domain `WithdrawTroops` method and its tests stay (decision #2).

## Error handling / back-compat

- **Validate-first atomicity:** affordability is checked before any cash moves, so there is no
  partial state. `AllocateTroops` cannot fail post-purchase (reserve was just topped up).
- **Existing reserves:** any reserve troops a player accumulated under the old flow remain in
  reserve, untouched — deploy always buys fresh (it does not auto-consume reserve). Acceptable
  for a dev build; called out as a conscious choice rather than a silent gap.
- **Invalid zone / no faction:** `BuyAndDeploy` is only reached from the zone detail menu with a
  selected zone and current faction; the controller already guards `factionState == null` /
  `_selectedZoneId == null` before calling.

## Testing

- **`DefenderDeploymentServiceTests`** (unit, mocked `ITroopPurchaseService` +
  `IZoneDefenderAllocationService`):
  - Success: affordable → `PurchaseTroops` then `AllocateTroops` called with the right args;
    result `Success` with correct total cost.
  - Insufficient funds: `CanAfford` false → returns `InsufficientFunds`, neither purchase nor
    allocate is called (no state change).
  - `count > 1` multiplies cost and passes `count` through to both calls.
  - Argument validation (null/empty zone, `count <= 0`) throws/returns as specified.
  - `GetTroopCost`/`CanAfford` forward to the purchase service.
- **`ZoneManagementMenuController` tests:** selecting each tier calls `BuyAndDeploy` with that
  tier; unaffordable tiers are disabled and show cost; the menu shows current allocation and no
  withdraw items; a `Success` refreshes the menu.
- **Removals:** delete `DefendersMenuController` tests; update navigation/integration tests that
  referenced the Defenders menu or recruitment entry so the suite stays green.
- Defaults/behavior of AI reserve usage is unchanged — no new tests needed there, but existing
  reserve/allocation tests must still pass.

## Out of scope (YAGNI)

- Auto-consuming pre-existing player reserve troops before charging (always buys fresh).
- Refund/sell-back of deployed troops (withdraw removed by decision).
- Any change to AI reserve/allocation behavior or the simulated-battle paths.
- Bulk "deploy N at once" UI (single-tap deploys one, as today's allocate did).
