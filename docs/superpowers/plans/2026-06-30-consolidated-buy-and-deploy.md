# Consolidated Buy-and-Deploy Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the zone menu's troop action buy-and-deploy a defender directly to that zone in one step, and remove the separate buy-to-reserve menu and the withdraw action.

**Architecture:** A new Economy-layer `IDefenderDeploymentService.BuyAndDeploy` composes the existing `ITroopPurchaseService.PurchaseTroops` (cash↓, reserve↑) and `IZoneDefenderAllocationService.AllocateTroops` (reserve↓, zone↑), validating affordability first. The zone menu (`ZoneManagementMenuController`) calls this one service. The buy-to-reserve `DefendersMenuController` and the Recruitment→Defenders entry are deleted. The reserve pool stays in the domain for AI use.

**Tech Stack:** C#/.NET Framework 4.8, ScriptHookVDotNet3, xUnit + Moq. Custom Roslyn analyzers enforce architecture.

## Global Constraints

- Strict TDD: write the failing test first, watch it fail, then implement. No production code without a failing test.
- Build must stay **0 warnings / 0 errors**. Analyzers (errors unless noted): CI0005 = ctor with **>5 parameters** (warning, but repo holds zero); CI0007 = method ≤40 lines; CI0017 = file ≤250 lines; CI0004 = ≤10 public methods/class; CI0014 = non-static ctor must not take a concrete `FactionWars.*` production type (interfaces/value-types/`*Options`/`.Models`/`.Events` exempt); ENDOFLINE = CRLF line endings on every file.
- Do not add `#pragma warning disable CI*/CA*`, do not skip tests, do not bypass git hooks.
- Reserve-pool domain operations (`AllocateTroops`, `WithdrawTroops`, `PurchaseTroops`, `SetAllocation`, `FactionState` reserve methods) keep their current signatures and behavior — this plan only changes the player UI flow and adds one orchestration service.
- Branch: `feat/134-consolidate-buy-and-deploy` (already created). Commit per task; do not push until the whole plan is reviewed.
- Build: `dotnet build FactionWars.sln --no-incremental`. Unit tests: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~FactionWars.Tests.Unit"`. The pre-commit hook runs build + unit tests on every commit (allow up to 10 min).

---

### Task 1: `DeploymentResult` + `IDefenderDeploymentService` + `DefenderDeploymentService`

**Files:**
- Create: `src/FactionWars/Economy/Models/DeploymentResult.cs`
- Create: `src/FactionWars/Economy/Interfaces/IDefenderDeploymentService.cs`
- Create: `src/FactionWars/Economy/Services/DefenderDeploymentService.cs`
- Test: `tests/FactionWars.Tests/Unit/Economy/DefenderDeploymentServiceTests.cs`

**Interfaces:**
- Consumes: `ITroopPurchaseService` (`CanAfford(DefenderRole, int)`, `CalculateTotalCost(DefenderRole, int)`, `GetTroopCost(DefenderRole)`, `PurchaseTroops(string factionId, DefenderRole, int)`), `IZoneDefenderAllocationService.AllocateTroops(FactionState, string zoneId, DefenderRole, int)`, `FactionState.FactionId`.
- Produces: `IDefenderDeploymentService.BuyAndDeploy(FactionState, string zoneId, DefenderRole tier, int count) -> DeploymentResult`, `.GetTroopCost(DefenderRole) -> int`, `.CanAfford(DefenderRole, int) -> bool`. `DeploymentResult` with `bool Success`, `DeploymentStatus Status`, `int TotalCost`, `DefenderRole Tier`, `int Count`, `string Message`, and static factories `Deployed(tier, count, cost)` / `Unaffordable(tier, count, cost)`.

- [ ] **Step 1: Write the failing test**

`tests/FactionWars.Tests/Unit/Economy/DefenderDeploymentServiceTests.cs`:
```csharp
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.Economy.Interfaces;
using FactionWars.Economy.Models;
using FactionWars.Economy.Services;
using FactionWars.Factions.Models;
using Moq;
using Xunit;

namespace FactionWars.Tests.Unit.Economy
{
    public class DefenderDeploymentServiceTests
    {
        private readonly Mock<ITroopPurchaseService> _purchase = new Mock<ITroopPurchaseService>();
        private readonly Mock<IZoneDefenderAllocationService> _alloc = new Mock<IZoneDefenderAllocationService>();
        private readonly FactionState _faction = new FactionState("michael", 10000);
        private readonly IDefenderDeploymentService _service;

        public DefenderDeploymentServiceTests()
        {
            _service = new DefenderDeploymentService(_purchase.Object, _alloc.Object);
        }

        [Fact]
        public void BuyAndDeploy_WhenAffordable_PurchasesThenAllocatesAndReturnsSuccess()
        {
            _purchase.Setup(p => p.CanAfford(DefenderRole.Rifleman, 1)).Returns(true);
            _purchase.Setup(p => p.CalculateTotalCost(DefenderRole.Rifleman, 1)).Returns(1000);

            var result = _service.BuyAndDeploy(_faction, "zone_downtown", DefenderRole.Rifleman, 1);

            Assert.True(result.Success);
            Assert.Equal(1000, result.TotalCost);
            _purchase.Verify(p => p.PurchaseTroops("michael", DefenderRole.Rifleman, 1), Times.Once);
            _alloc.Verify(a => a.AllocateTroops(_faction, "zone_downtown", DefenderRole.Rifleman, 1), Times.Once);
        }

        [Fact]
        public void BuyAndDeploy_WhenUnaffordable_DoesNothingAndReturnsInsufficientFunds()
        {
            _purchase.Setup(p => p.CanAfford(DefenderRole.Rocketeer, 1)).Returns(false);
            _purchase.Setup(p => p.CalculateTotalCost(DefenderRole.Rocketeer, 1)).Returns(2000);

            var result = _service.BuyAndDeploy(_faction, "zone_downtown", DefenderRole.Rocketeer, 1);

            Assert.False(result.Success);
            Assert.Equal(DeploymentStatus.InsufficientFunds, result.Status);
            _purchase.Verify(p => p.PurchaseTroops(It.IsAny<string>(), It.IsAny<DefenderRole>(), It.IsAny<int>()), Times.Never);
            _alloc.Verify(a => a.AllocateTroops(It.IsAny<FactionState>(), It.IsAny<string>(), It.IsAny<DefenderRole>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public void GetTroopCost_ForwardsToPurchaseService()
        {
            _purchase.Setup(p => p.GetTroopCost(DefenderRole.Sniper)).Returns(1500);
            Assert.Equal(1500, _service.GetTroopCost(DefenderRole.Sniper));
        }
    }
}
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~DefenderDeploymentServiceTests"`
Expected: FAIL to compile — `DefenderDeploymentService` / `IDefenderDeploymentService` / `DeploymentResult` missing.

- [ ] **Step 3: Create `DeploymentResult.cs`**

```csharp
using FactionWars.Core.Models;

namespace FactionWars.Economy.Models
{
    /// <summary>Outcome status of a buy-and-deploy operation.</summary>
    public enum DeploymentStatus
    {
        Success,
        InsufficientFunds
    }

    /// <summary>Result of buying and deploying defenders directly to a zone.</summary>
    public class DeploymentResult
    {
        public DeploymentStatus Status { get; }
        public bool Success => Status == DeploymentStatus.Success;
        public DefenderRole Tier { get; }
        public int Count { get; }
        public int TotalCost { get; }
        public string Message { get; }

        public static DeploymentResult Deployed(DefenderRole tier, int count, int cost) =>
            new DeploymentResult(DeploymentStatus.Success, tier, count, cost, $"Deployed {count} {tier}");

        public static DeploymentResult Unaffordable(DefenderRole tier, int count, int cost) =>
            new DeploymentResult(DeploymentStatus.InsufficientFunds, tier, count, cost, "Not enough cash");

        private DeploymentResult(DeploymentStatus status, DefenderRole tier, int count, int totalCost, string message)
        {
            Status = status;
            Tier = tier;
            Count = count;
            TotalCost = totalCost;
            Message = message;
        }
    }
}
```

- [ ] **Step 4: Create `IDefenderDeploymentService.cs`**

```csharp
using FactionWars.Core.Models;
using FactionWars.Economy.Models;
using FactionWars.Factions.Models;

namespace FactionWars.Economy.Interfaces
{
    /// <summary>
    /// Orchestrates buying defenders and deploying them directly to a zone in one step.
    /// Composes <see cref="ITroopPurchaseService"/> and the zone allocation service so the
    /// player never manages a reserve pool directly.
    /// </summary>
    public interface IDefenderDeploymentService
    {
        /// <summary>
        /// Buys <paramref name="count"/> troops of <paramref name="tier"/> and deploys them to
        /// <paramref name="zoneId"/>. Validates affordability first; on insufficient funds it
        /// makes no state change.
        /// </summary>
        DeploymentResult BuyAndDeploy(FactionState factionState, string zoneId, DefenderRole tier, int count);

        /// <summary>Cost of a single troop of <paramref name="tier"/> (for menu labels).</summary>
        int GetTroopCost(DefenderRole tier);

        /// <summary>Whether the player can afford <paramref name="count"/> of <paramref name="tier"/>.</summary>
        bool CanAfford(DefenderRole tier, int count);
    }
}
```

- [ ] **Step 5: Create `DefenderDeploymentService.cs`**

```csharp
using System;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.Economy.Interfaces;
using FactionWars.Economy.Models;
using FactionWars.Factions.Models;
using FactionWars.ScriptHookV.Logging;

namespace FactionWars.Economy.Services
{
    /// <summary>
    /// Default <see cref="IDefenderDeploymentService"/> that composes purchase + allocation.
    /// </summary>
    public sealed class DefenderDeploymentService : IDefenderDeploymentService
    {
        private readonly ITroopPurchaseService _purchaseService;
        private readonly IZoneDefenderAllocationService _allocationService;

        public DefenderDeploymentService(
            ITroopPurchaseService purchaseService,
            IZoneDefenderAllocationService allocationService)
        {
            _purchaseService = purchaseService ?? throw new ArgumentNullException(nameof(purchaseService));
            _allocationService = allocationService ?? throw new ArgumentNullException(nameof(allocationService));
        }

        public int GetTroopCost(DefenderRole tier) => _purchaseService.GetTroopCost(tier);

        public bool CanAfford(DefenderRole tier, int count) => _purchaseService.CanAfford(tier, count);

        public DeploymentResult BuyAndDeploy(FactionState factionState, string zoneId, DefenderRole tier, int count)
        {
            if (factionState == null) throw new ArgumentNullException(nameof(factionState));
            if (string.IsNullOrWhiteSpace(zoneId)) throw new ArgumentException("Zone id must be provided.", nameof(zoneId));
            if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count));

            var cost = _purchaseService.CalculateTotalCost(tier, count);
            if (!_purchaseService.CanAfford(tier, count))
            {
                FileLogger.Info($"BuyAndDeploy: insufficient funds for {count}x {tier} (${cost}) in zone {zoneId}");
                return DeploymentResult.Unaffordable(tier, count, cost);
            }

            _purchaseService.PurchaseTroops(factionState.FactionId, tier, count);
            _allocationService.AllocateTroops(factionState, zoneId, tier, count);
            FileLogger.Info($"BuyAndDeploy: deployed {count}x {tier} to zone {zoneId} for ${cost}");
            return DeploymentResult.Deployed(tier, count, cost);
        }
    }
}
```
(If `FileLogger` is not resolvable from the Economy layer per architecture rules, drop the log lines — keep the method otherwise identical. Verify by building; do not introduce a layering violation to keep a log.)

- [ ] **Step 6: Run the test to verify it passes**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~DefenderDeploymentServiceTests"`
Expected: PASS (3/3).

- [ ] **Step 7: Commit**

```bash
git add src/FactionWars/Economy tests/FactionWars.Tests/Unit/Economy
git commit -m "feat: add DefenderDeploymentService (buy + deploy in one step) (#134)"
```

---

### Task 2: Register the service in DI + introduce the zone-menu dependencies bundle

This task wires the new service into the container and refactors `ZoneManagementMenuController` to take a dependencies bundle (so adding the deployment service does not exceed the 5-parameter limit). **No menu behavior changes yet** — the deployment service is stored but unused until Task 3.

**Files:**
- Modify: `src/FactionWars/ScriptHookV/ServiceContainerFactory.PersistenceEconomy.cs` (register `IDefenderDeploymentService`)
- Create: `src/FactionWars/ScriptHookV/Models/ZoneManagementMenuControllerDependencies.cs`
- Modify: `src/FactionWars/ScriptHookV/UI/ZoneManagementMenuController.cs` (constructor → bundle; add `_deploymentService` field)
- Modify: `src/FactionWars/ScriptHookV/GameLoopController.InitializationUi.cs:66-67` (construct via bundle)
- Test: `tests/FactionWars.Tests/Unit/ScriptHookV/UI/ZoneManagementMenuControllerTests.cs` (constructor calls → bundle), and add a DI-resolution test in `tests/FactionWars.Tests/Unit/ScriptHookV/ServiceContainerFactoryCombatantStatsTests.cs`-style fixture (new file below).

**Interfaces:**
- Consumes: `IDefenderDeploymentService` (Task 1).
- Produces: `ZoneManagementMenuControllerDependencies` with settable properties `MenuProvider`, `FactionService`, `ZoneService`, `PlayerContext`, `AllocationService`, `DeploymentService`; `ZoneManagementMenuController(ZoneManagementMenuControllerDependencies dependencies)`.

- [ ] **Step 1: Write the failing DI-resolution test**

`tests/FactionWars.Tests/Unit/ScriptHookV/ServiceContainerFactoryDeploymentTests.cs`:
```csharp
using FactionWars.Core.Utils;
using FactionWars.Economy.Interfaces;
using FactionWars.ScriptHookV;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV
{
    public class ServiceContainerFactoryDeploymentTests
    {
        [Fact]
        public void Create_RegistersDefenderDeploymentService()
        {
            var container = ServiceContainerFactory.Create(new MockGameBridge());
            Assert.NotNull(container.Resolve<IDefenderDeploymentService>());
        }
    }
}
```

- [ ] **Step 2: Run it to verify it fails**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~ServiceContainerFactoryDeploymentTests"`
Expected: FAIL — `IDefenderDeploymentService` not registered (resolve throws).

- [ ] **Step 3: Register the service** in `RegisterEconomyServices` (`ServiceContainerFactory.PersistenceEconomy.cs`), immediately after the `ITroopPurchaseService` registration block:
```csharp
            // Defender deployment service composes purchase + allocation (one-step buy & deploy)
            container.RegisterSingleton<IDefenderDeploymentService>(() =>
                new DefenderDeploymentService(
                    container.Resolve<ITroopPurchaseService>(),
                    container.Resolve<IZoneDefenderAllocationService>()));
```
Add `using FactionWars.Economy.Services;` if not already present (the file already uses `FactionWars.Economy.Interfaces`/`Services` for `TroopPurchaseService`; verify and add what's missing).

- [ ] **Step 4: Run the DI test to verify it passes**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~ServiceContainerFactoryDeploymentTests"`
Expected: PASS.

- [ ] **Step 5: Create the dependencies bundle** `src/FactionWars/ScriptHookV/Models/ZoneManagementMenuControllerDependencies.cs`:
```csharp
using FactionWars.Core.Interfaces;
using FactionWars.Economy.Interfaces;
using FactionWars.Factions.Interfaces;
using FactionWars.Territory.Interfaces;
using FactionWars.UI.Interfaces;

namespace FactionWars.ScriptHookV.Models
{
    public sealed class ZoneManagementMenuControllerDependencies
    {
        public IMenuProvider? MenuProvider { get; set; }
        public IFactionService? FactionService { get; set; }
        public IZoneService? ZoneService { get; set; }
        public IPlayerContext? PlayerContext { get; set; }
        public IZoneDefenderAllocationService? AllocationService { get; set; }
        public IDefenderDeploymentService? DeploymentService { get; set; }
    }
}
```

- [ ] **Step 6: Refactor the constructor** in `ZoneManagementMenuController.cs`. Replace the five-parameter constructor (lines ~116-131) with a bundle constructor, add `using FactionWars.ScriptHookV.Models;` and `using FactionWars.Economy.Interfaces;`, and add the `_deploymentService` field next to the others:
```csharp
        private readonly IDefenderDeploymentService _deploymentService;
```
```csharp
        public ZoneManagementMenuController(ZoneManagementMenuControllerDependencies dependencies)
        {
            if (dependencies == null) throw new ArgumentNullException(nameof(dependencies));
            _menuProvider = dependencies.MenuProvider ?? throw new ArgumentNullException(nameof(dependencies.MenuProvider));
            _factionService = dependencies.FactionService ?? throw new ArgumentNullException(nameof(dependencies.FactionService));
            _zoneService = dependencies.ZoneService ?? throw new ArgumentNullException(nameof(dependencies.ZoneService));
            _playerContext = dependencies.PlayerContext ?? throw new ArgumentNullException(nameof(dependencies.PlayerContext));
            _allocationService = dependencies.AllocationService ?? throw new ArgumentNullException(nameof(dependencies.AllocationService));
            _deploymentService = dependencies.DeploymentService ?? throw new ArgumentNullException(nameof(dependencies.DeploymentService));

            _menuProvider.ItemSelected += OnItemSelected;
        }
```

- [ ] **Step 7: Update the construction site** in `GameLoopController.InitializationUi.cs` (lines 66-67). The method `InitializeOverviewMenus` already has `allocationService` in scope; resolve the deployment service from the container:
```csharp
            _zoneManagementMenuController = new ZoneManagementMenuController(
                new ZoneManagementMenuControllerDependencies
                {
                    MenuProvider = menuProvider,
                    FactionService = _factionService,
                    ZoneService = zoneService,
                    PlayerContext = playerContext,
                    AllocationService = allocationService,
                    DeploymentService = _container.Resolve<IDefenderDeploymentService>()
                });
```
Ensure `using FactionWars.Economy.Interfaces;` and `using FactionWars.ScriptHookV.Models;` are present in that file.

- [ ] **Step 8: Update the existing controller tests** in `ZoneManagementMenuControllerTests.cs`. In the constructor, build a `Mock<IDefenderDeploymentService>` and a helper that creates the bundle, then construct via the bundle. Replace the fixture's `_controller = new ZoneManagementMenuController(...)` (lines ~72-77) and **every** `new ZoneManagementMenuController(...)` in the null-check tests. Add this field + helper and a deployment mock:
```csharp
        private readonly Mock<IDefenderDeploymentService> _deploymentServiceMock = new Mock<IDefenderDeploymentService>();

        private ZoneManagementMenuControllerDependencies Deps() => new ZoneManagementMenuControllerDependencies
        {
            MenuProvider = _menuProvider,
            FactionService = _factionServiceMock.Object,
            ZoneService = _zoneServiceMock.Object,
            PlayerContext = _playerContextMock.Object,
            AllocationService = _allocationServiceMock.Object,
            DeploymentService = _deploymentServiceMock.Object
        };
```
Fixture construction becomes `_controller = new ZoneManagementMenuController(Deps());`. For each existing null-parameter constructor test, convert it to set exactly one bundle property to `null!` and assert `ArgumentNullException` — e.g.:
```csharp
        [Fact]
        public void Constructor_WithNullMenuProvider_ShouldThrowArgumentNullException()
        {
            var deps = Deps();
            deps.MenuProvider = null;
            Assert.Throws<ArgumentNullException>(() => new ZoneManagementMenuController(deps));
        }
```
Add an equivalent null-check test for `DeploymentService`. Add `using FactionWars.Economy.Interfaces;` and `using FactionWars.ScriptHookV.Models;`.

- [ ] **Step 9: Build + full unit suite**

Run: `dotnet build FactionWars.sln --no-incremental` then `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~FactionWars.Tests.Unit"`
Expected: clean build (0/0), all PASS. Behavior is unchanged (the menu still allocates from reserve at this point).

- [ ] **Step 10: Commit**

```bash
git add src/FactionWars tests/FactionWars.Tests
git commit -m "refactor: register deployment service + bundle zone-menu deps (#134)"
```

---

### Task 3: Zone menu buys and deploys all 5 tiers; remove withdraw + reserve display

**Files:**
- Modify: `src/FactionWars/ScriptHookV/UI/ZoneManagementMenuController.cs` (item-id constants; remove reserve summary from list)
- Modify: `src/FactionWars/ScriptHookV/UI/ZoneManagementMenuController.Details.cs` (5-tier current allocation + 5 deploy items; remove allocate-from-reserve + withdraw items)
- Modify: `src/FactionWars/ScriptHookV/UI/ZoneManagementMenuController.Selection.cs` (deploy via `BuyAndDeploy`; remove withdraw cases)
- Test: `tests/FactionWars.Tests/Unit/ScriptHookV/UI/ZoneManagementMenuControllerTests.cs`

**Interfaces:**
- Consumes: `_deploymentService.BuyAndDeploy(factionState, zoneId, tier, 1)`, `_deploymentService.GetTroopCost(tier)`, `_deploymentService.CanAfford(tier, 1)`.

- [ ] **Step 1: Write the failing tests** (add to `ZoneManagementMenuControllerTests.cs`). These drive a zone-detail menu and a deploy selection. Use the existing `MockMenuProvider` (it records the last shown `MenuDefinition` and can raise `ItemSelected`). Mirror the file's existing patterns for showing the detail menu and simulating selection.
```csharp
        [Fact]
        public void DeployItem_WhenSelected_CallsBuyAndDeployForThatTier()
        {
            _deploymentServiceMock.Setup(d => d.GetTroopCost(It.IsAny<DefenderRole>())).Returns(1000);
            _deploymentServiceMock.Setup(d => d.CanAfford(It.IsAny<DefenderRole>(), 1)).Returns(true);
            _deploymentServiceMock.Setup(d => d.BuyAndDeploy(It.IsAny<FactionState>(), "zone_downtown", DefenderRole.Rifleman, 1))
                .Returns(DeploymentResult.Deployed(DefenderRole.Rifleman, 1, 1000));

            _controller.Show();
            _menuProvider.RaiseItemSelected(ZoneManagementMenuController.ZoneManagementMenuId, "zone_downtown");
            _menuProvider.RaiseItemSelected(ZoneManagementMenuController.ZoneDetailMenuId, ZoneManagementMenuController.DeployHeavyItemId);

            _deploymentServiceMock.Verify(d => d.BuyAndDeploy(It.IsAny<FactionState>(), "zone_downtown", DefenderRole.Rifleman, 1), Times.Once);
        }

        [Fact]
        public void DeployItem_WhenUnaffordable_IsDisabledAndShowsCost()
        {
            _deploymentServiceMock.Setup(d => d.GetTroopCost(DefenderRole.Rocketeer)).Returns(2000);
            _deploymentServiceMock.Setup(d => d.CanAfford(DefenderRole.Rocketeer, 1)).Returns(false);
            _deploymentServiceMock.Setup(d => d.CanAfford(It.Is<DefenderRole>(t => t != DefenderRole.Rocketeer), 1)).Returns(true);
            _deploymentServiceMock.Setup(d => d.GetTroopCost(It.IsAny<DefenderRole>())).Returns(1000);

            _controller.Show();
            _menuProvider.RaiseItemSelected(ZoneManagementMenuController.ZoneManagementMenuId, "zone_downtown");

            var item = _menuProvider.LastMenu.Items.Single(i => i.Id == ZoneManagementMenuController.DeployEliteItemId);
            Assert.False(item.IsEnabled);
            Assert.Contains("2000", item.Title);
        }

        [Fact]
        public void ZoneDetailMenu_HasNoWithdrawItems()
        {
            _deploymentServiceMock.Setup(d => d.GetTroopCost(It.IsAny<DefenderRole>())).Returns(1000);
            _deploymentServiceMock.Setup(d => d.CanAfford(It.IsAny<DefenderRole>(), 1)).Returns(true);

            _controller.Show();
            _menuProvider.RaiseItemSelected(ZoneManagementMenuController.ZoneManagementMenuId, "zone_downtown");

            Assert.DoesNotContain(_menuProvider.LastMenu.Items, i => i.Id.StartsWith("withdraw_"));
        }
```
(If `MockMenuProvider` exposes `LastMenu`/`RaiseItemSelected` under different names, use the actual members — inspect `tests/FactionWars.Tests/Mocks/MockMenuProvider.cs` and match. Do NOT add new members to the mock unless a needed capability is genuinely absent; if you must, keep it minimal and note it in the report.)

- [ ] **Step 2: Run to verify failure**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~ZoneManagementMenuControllerTests"`
Expected: FAIL — `DeployHeavyItemId`/`DeployEliteItemId` don't exist; deploy items not present; withdraw items still present.

- [ ] **Step 3: Update item-id constants** in `ZoneManagementMenuController.cs`. Remove `AllocateBasic/Medium/HeavyItemId`, `WithdrawBasic/Medium/HeavyItemId`, and `ReserveSummaryItemId`. Add deploy + full current-allocation ids:
```csharp
        public const string CurrentBasicItemId = "current_basic";
        public const string CurrentMediumItemId = "current_medium";
        public const string CurrentHeavyItemId = "current_heavy";
        public const string CurrentEliteItemId = "current_elite";
        public const string CurrentSniperItemId = "current_sniper";

        public const string DeployBasicItemId = "deploy_basic";
        public const string DeployMediumItemId = "deploy_medium";
        public const string DeployHeavyItemId = "deploy_heavy";
        public const string DeployEliteItemId = "deploy_elite";
        public const string DeploySniperItemId = "deploy_sniper";
```
Remove `AddReserveSummary` and its call in `ShowZoneListMenu` (the `menu` then goes straight from title to `AddOwnedZoneItems`). Remove the now-unused `FactionState` parameter threading for the reserve summary if it leaves an unused local (keep `factionState` only if still used; in `ShowZoneListMenu` it is no longer needed — drop the local to avoid an unused-variable warning).

- [ ] **Step 4: Rewrite the detail menu** in `ZoneManagementMenuController.Details.cs`. Replace `ShowZoneDetailMenu`'s reserve reads + `AddAllocateItems`/`AddWithdrawItems` with cost-labelled deploy items for all five tiers, and expand current-allocation display to five tiers:
```csharp
        private void ShowZoneDetailMenu(string zoneId, string? selectedItemId = null)
        {
            _selectedZoneId = zoneId;

            var factionId = _playerContext.CurrentFactionId;
            var zone = _zoneService.GetZone(zoneId);
            var zoneName = zone?.Name ?? "Unknown Zone";

            var menu = new MenuDefinition(ZoneDetailMenuId, zoneName, "Deploy defenders");

            var allocation = factionId != null ? _allocationService.GetAllocation(factionId, zoneId) : null;
            AddCurrentAllocationItems(menu, allocation);
            AddDeployItems(menu);
            AddDetailBackItem(menu);

            _menuProvider.ShowMenu(menu, selectedItemId);
            _menuProvider.HoldToRepeatEnabled = true;
        }

        private static void AddCurrentAllocationItems(MenuDefinition menu, Core.Models.ZoneDefenderAllocation? allocation)
        {
            AddCurrentItem(menu, CurrentBasicItemId, "Basic", allocation, DefenderRole.Grunt);
            AddCurrentItem(menu, CurrentMediumItemId, "Medium", allocation, DefenderRole.Gunner);
            AddCurrentItem(menu, CurrentHeavyItemId, "Heavy", allocation, DefenderRole.Rifleman);
            AddCurrentItem(menu, CurrentEliteItemId, "Elite", allocation, DefenderRole.Rocketeer);
            AddCurrentItem(menu, CurrentSniperItemId, "Sniper", allocation, DefenderRole.Sniper);
        }

        private static void AddCurrentItem(MenuDefinition menu, string itemId, string label, Core.Models.ZoneDefenderAllocation? allocation, DefenderRole tier)
        {
            var count = allocation?.GetTroopCount(tier) ?? 0;
            var item = new MenuItem(itemId, $"{label}: {count}", $"Currently deployed {label} tier defenders");
            item.IsEnabled = false;
            menu.AddItem(item);
        }

        private void AddDeployItems(MenuDefinition menu)
        {
            AddDeployItem(menu, DeployBasicItemId, "Basic", DefenderRole.Grunt);
            AddDeployItem(menu, DeployMediumItemId, "Medium", DefenderRole.Gunner);
            AddDeployItem(menu, DeployHeavyItemId, "Heavy", DefenderRole.Rifleman);
            AddDeployItem(menu, DeployEliteItemId, "Elite", DefenderRole.Rocketeer);
            AddDeployItem(menu, DeploySniperItemId, "Sniper", DefenderRole.Sniper);
        }

        private void AddDeployItem(MenuDefinition menu, string itemId, string label, DefenderRole tier)
        {
            var cost = _deploymentService.GetTroopCost(tier);
            var item = new MenuItem(itemId, $"Deploy {label} — ${cost}", $"Buy and deploy one {label} defender to this zone");
            item.IsEnabled = _deploymentService.CanAfford(tier, 1);
            menu.AddItem(item);
        }
```
Note `AddCurrentAllocationItems` / `AddDeployItem` are now instance methods (they use `_deploymentService`); ensure they are not `static`. Add `using FactionWars.Core.Models;` if needed for `DefenderRole`/`ZoneDefenderAllocation` (or fully-qualify as shown). Keep each method ≤40 lines (CI0007) — the helpers above are small.

- [ ] **Step 5: Rewrite the selection handler** in `ZoneManagementMenuController.Selection.cs`. Replace the `switch` in `HandleZoneDetailSelection` with deploy handling and remove withdraw cases:
```csharp
        private void HandleZoneDetailSelection(string itemId)
        {
            if (itemId == DetailBackItemId)
            {
                ShowZoneListMenu();
                return;
            }

            var factionId = _playerContext.CurrentFactionId;
            var factionState = factionId != null ? _factionService.GetFactionState(factionId) : null;

            if (factionState == null || _selectedZoneId == null)
                return;

            var tier = DeployTierFor(itemId);
            if (tier == null) return;

            var result = _deploymentService.BuyAndDeploy(factionState, _selectedZoneId, tier.Value, 1);
            if (!result.Success)
            {
                _menuProvider.ShowNotification("~r~Not enough cash");
            }
            ShowZoneDetailMenu(_selectedZoneId, itemId);
        }

        private static DefenderRole? DeployTierFor(string itemId)
        {
            switch (itemId)
            {
                case DeployBasicItemId: return DefenderRole.Grunt;
                case DeployMediumItemId: return DefenderRole.Gunner;
                case DeployHeavyItemId: return DefenderRole.Rifleman;
                case DeployEliteItemId: return DefenderRole.Rocketeer;
                case DeploySniperItemId: return DefenderRole.Sniper;
                default: return null;
            }
        }
```
Check the actual notification API on `IMenuProvider`/`_gameBridge`. If `IMenuProvider` has no `ShowNotification`, use the same notification mechanism the menu layer already uses (inspect `MockMenuProvider`/`IMenuProvider`); if notifications require `IGameBridge`, the controller does not currently hold one — in that case drop the notification (the item is already disabled when unaffordable, so this is an unreachable defensive branch) rather than adding a new dependency. State the choice in the report.

- [ ] **Step 6: Update the zone-list item display** (optional but keep consistent): in `AddOwnedZoneItems` (`ZoneManagementMenuController.cs`), the per-zone summary currently shows `B/M/H`. Extend the `total` to include all five tiers so the count is accurate:
```csharp
                    var allocation = factionId != null ? _allocationService.GetAllocation(factionId, zone.Id) : null;
                    var total = allocation == null ? 0 :
                        allocation.GetTroopCount(DefenderRole.Grunt)
                        + allocation.GetTroopCount(DefenderRole.Gunner)
                        + allocation.GetTroopCount(DefenderRole.Rifleman)
                        + allocation.GetTroopCount(DefenderRole.Rocketeer)
                        + allocation.GetTroopCount(DefenderRole.Sniper);

                    var zoneItem = new MenuItem(
                        zone.Id,
                        $"{zone.Name} ({total} troops)",
                        $"Value: {zone.StrategicValue}");
                    menu.AddItem(zoneItem);
```

- [ ] **Step 7: Run the controller tests + full unit suite**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~ZoneManagementMenuControllerTests"` then the full `FactionWars.Tests.Unit` filter.
Expected: PASS. Remove/repair any old tests that asserted allocate-from-reserve, withdraw, or the reserve summary line (those behaviors are gone) — update them to the new deploy behavior rather than deleting coverage wholesale.

- [ ] **Step 8: Commit**

```bash
git add src/FactionWars tests/FactionWars.Tests
git commit -m "feat: zone menu buys & deploys all 5 tiers; drop withdraw/reserve UI (#134)"
```

---

### Task 4: Remove the buy-to-reserve Defenders menu + Recruitment entry

**Files:**
- Delete: `src/FactionWars/ScriptHookV/UI/DefendersMenuController.cs`
- Delete: `tests/FactionWars.Tests/Unit/ScriptHookV/UI/DefendersMenuControllerTests.cs`
- Modify: `src/FactionWars/ScriptHookV/GameLoopController.cs:75` (remove `_defendersMenuController` field)
- Modify: `src/FactionWars/ScriptHookV/GameLoopController.InitializationMenus.cs:41-42,54` (remove construction, back-wiring, and `DefendersRequested` subscription)
- Modify: `src/FactionWars/ScriptHookV/UI/RecruitmentMenuController.cs` (remove `DefendersItemId`, `DefendersRequested`, the menu item, and the `case`)
- Test: `tests/FactionWars.Tests/Unit/ScriptHookV/UI/RecruitmentMenuControllerTests.cs` (remove Defenders-option assertions)

**Interfaces:** none produced; this is removal only. The Recruitment menu now offers Squad + Back.

- [ ] **Step 1: Update the Recruitment test first (red).** In `RecruitmentMenuControllerTests.cs`, delete any test asserting a Defenders item or `DefendersRequested` firing, and add/adjust a test asserting the menu contains Squad + Back and **no** `defenders` item:
```csharp
        [Fact]
        public void Show_DoesNotIncludeDefendersOption()
        {
            _controller.Show();
            Assert.DoesNotContain(_menuProvider.LastMenu.Items, i => i.Id == "defenders");
        }
```
(Match the fixture's existing mock/menu-inspection members.)

- [ ] **Step 2: Run it (red).**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~RecruitmentMenuControllerTests"`
Expected: FAIL — the Defenders item is still present.

- [ ] **Step 3: Edit `RecruitmentMenuController.cs`.** Remove the `DefendersItemId` constant, the `DefendersRequested` event, the `defendersItem` block in `Show()`, and the `case DefendersItemId:` in `OnItemSelected`. The `Show()` menu now adds cash display, Squad, Back.

- [ ] **Step 4: Remove wiring in `GameLoopController.InitializationMenus.cs`.** Delete lines constructing `_defendersMenuController` and its `BackRequested` (41-42) and the `_recruitmentMenuController.DefendersRequested += ...` line (54). Remove the `_defendersMenuController` field in `GameLoopController.cs:75`.

- [ ] **Step 5: Delete the controller + its test.**
```bash
git rm src/FactionWars/ScriptHookV/UI/DefendersMenuController.cs
git rm tests/FactionWars.Tests/Unit/ScriptHookV/UI/DefendersMenuControllerTests.cs
```
Search for any other reference: `grep -rn "DefendersMenuController\|DefendersRequested" src tests` must return nothing after this step. Fix any stragglers (e.g. integration/navigation tests referencing the Defenders menu).

- [ ] **Step 6: Build + full unit suite.**

Run: `dotnet build FactionWars.sln --no-incremental` then the full `FactionWars.Tests.Unit` filter.
Expected: clean build (0/0), all PASS.

- [ ] **Step 7: Commit.**
```bash
git add -A
git commit -m "refactor: remove buy-to-reserve Defenders menu + Recruitment entry (#134)"
```

---

## Self-Review

**Spec coverage:** Component 1 service + `DeploymentResult` (Task 1) ✓; DI registration (Task 2) ✓; dependencies bundle for the 5-param limit (Task 2) ✓; zone menu 5-tier deploy with cost labels + disable-if-unaffordable (Task 3) ✓; remove withdraw + reserve display (Task 3) ✓; remove DefendersMenu + Recruitment entry (Task 4) ✓; reserve pool/AI behavior untouched (no task changes domain reserve ops) ✓; validate-first atomicity (Task 1 `BuyAndDeploy`) ✓; existing reserves left untouched (deploy always buys fresh — Task 1 never reads existing reserve) ✓; domain `WithdrawTroops` kept (no task deletes it) ✓.

**Type consistency:** `IDefenderDeploymentService.BuyAndDeploy(FactionState, string, DefenderRole, int)` and `GetTroopCost`/`CanAfford` are used identically across Tasks 1–3. `DeploymentResult.Success`/`Status`/`TotalCost` consistent. `ZoneManagementMenuControllerDependencies` property names (`MenuProvider`/`FactionService`/`ZoneService`/`PlayerContext`/`AllocationService`/`DeploymentService`) match between Task 2's bundle, the ctor, the construction site, and the tests. Deploy item-id constants (`DeployBasic/Medium/Heavy/Elite/SniperItemId`) defined in Task 3 Step 3 and used in Steps 4–5 and the Task 3 tests.

**Verification dependencies the implementer must confirm against live code (called out inline in the steps):** `MockMenuProvider`'s inspection/raise members; whether `IMenuProvider` exposes a notification method (else drop the unaffordable notification); whether `FileLogger` is reachable from the Economy layer (else drop the log lines). None of these change the design; each step states the fallback.
