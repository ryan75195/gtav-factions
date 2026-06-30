# Suppress Native Controls While a Mod Menu Is Open — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** While any mod menu is open (the squad radial wheel or a NativeUI list menu), disable a curated set of conflicting native GTA controls each frame so menu use doesn't also fire weapon wheel / radio / phone / attack.

**Architecture:** A new `IGameBridge.DisableMenuConflictControlsThisFrame()` disables the curated set via the existing `DISABLE_CONTROL_ACTION` path. `SquadRadialMenuRenderer` exposes `IsOpen`. `GameLoopController.UpdateAndDrawHud()` calls the suppressor each frame when `_menuProvider.IsMenuVisible || radial.IsOpen`.

**Tech Stack:** C#/.NET Framework 4.8, ScriptHookVDotNet3 (`GTA.Control` enum, `GTA.Native`), xUnit + Moq. Custom Roslyn analyzers enforce architecture.

## Global Constraints

- Strict TDD: failing test first, watch it fail, then implement.
- Build must stay **0 warnings / 0 errors**. Analyzers (errors unless noted): CI0007 method ≤40 lines; CI0017 file ≤250 lines; CI0004 ≤10 public methods/class; CI0014 ctor concrete-type rule; CI0016 one public top-level type per file; ENDOFLINE = CRLF on every file.
- No `#pragma warning disable CI*/CA*`, no skipped tests, no git-hook bypass.
- New `IGameBridge` behavior MUST include a `FileLogger` line (repo convention — native calls can't be unit-tested).
- The disabled control set MUST include the conflict categories (weapon wheel, vehicle radio, phone, character switch, attack/aim/melee, cover) and MUST exclude movement and all frontend menu-navigation controls (`FrontendUp/Down/Left/Right/Accept/Cancel/Pause`), or the menus soft-lock.
- Branch: `feat/136-suppress-native-controls-in-menus` (already created). Commit per task; do not push until the whole plan is reviewed.
- Build: `dotnet build FactionWars.sln --no-incremental`. Unit tests: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~FactionWars.Tests.Unit"`. Pre-commit hook runs build + unit tests (allow up to 10 min).

---

### Task 1: `IGameBridge.DisableMenuConflictControlsThisFrame()` + GameBridge + MockGameBridge

**Files:**
- Modify: `src/FactionWars/Core/Interfaces/IGameBridge.cs` (add method after `DisableControlThisFrame`, ~line 856)
- Modify: `src/FactionWars/ScriptHookV/GameBridge.PlayerWeapons.cs` (implement near `DisableControlThisFrame`, ~line 93)
- Modify: `src/FactionWars/Core/Utils/MockGameBridge.cs` (counter + Reset)
- Test: `tests/FactionWars.Tests/Unit/Core/MockGameBridgeMenuControlsTests.cs` (new)

**Interfaces:**
- Produces: `IGameBridge.DisableMenuConflictControlsThisFrame()`; `MockGameBridge.MenuConflictSuppressCallCount` (int).

- [ ] **Step 1: Write the failing test**

`tests/FactionWars.Tests/Unit/Core/MockGameBridgeMenuControlsTests.cs`:
```csharp
using FactionWars.Core.Utils;
using Xunit;

namespace FactionWars.Tests.Unit.Core
{
    public class MockGameBridgeMenuControlsTests
    {
        [Fact]
        public void DisableMenuConflictControlsThisFrame_IncrementsCallCount()
        {
            var bridge = new MockGameBridge();
            Assert.Equal(0, bridge.MenuConflictSuppressCallCount);

            bridge.DisableMenuConflictControlsThisFrame();
            bridge.DisableMenuConflictControlsThisFrame();

            Assert.Equal(2, bridge.MenuConflictSuppressCallCount);
        }

        [Fact]
        public void Reset_ClearsMenuConflictSuppressCallCount()
        {
            var bridge = new MockGameBridge();
            bridge.DisableMenuConflictControlsThisFrame();

            bridge.Reset();

            Assert.Equal(0, bridge.MenuConflictSuppressCallCount);
        }
    }
}
```

- [ ] **Step 2: Run to verify it fails**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~MockGameBridgeMenuControlsTests"`
Expected: FAIL to compile — `DisableMenuConflictControlsThisFrame` / `MenuConflictSuppressCallCount` missing.

- [ ] **Step 3: Add the interface method** in `IGameBridge.cs` immediately after the existing `DisableControlThisFrame(int control)` declaration:
```csharp
        /// <summary>
        /// Disables the native controls that conflict with mod-menu use for this frame
        /// (weapon wheel, vehicle radio, phone, character switch, attack/aim/melee, cover).
        /// Movement and frontend menu-navigation controls are intentionally left enabled so
        /// the mod menus stay operable. Must be called every frame a mod menu is open.
        /// </summary>
        void DisableMenuConflictControlsThisFrame();
```

- [ ] **Step 4: Implement in `GameBridge.PlayerWeapons.cs`** after `DisableControlThisFrame`:
```csharp
        // Native controls that conflict with mod-menu use. Expressed as GTA.Control members
        // (compile-checked) so the list is self-documenting. Movement + every frontend
        // navigation control are deliberately excluded so menus stay operable.
        private static readonly GTA.Control[] MenuConflictControls =
        {
            GTA.Control.SelectWeapon,
            GTA.Control.VehicleRadioWheel,
            GTA.Control.VehicleNextRadio,
            GTA.Control.VehiclePrevRadio,
            GTA.Control.Phone,
            GTA.Control.CharacterWheel,
            GTA.Control.Attack,
            GTA.Control.Attack2,
            GTA.Control.Aim,
            GTA.Control.MeleeAttack1,
            GTA.Control.MeleeAttack2,
            GTA.Control.VehicleAttack,
            GTA.Control.Cover,
        };

        private bool _loggedMenuConflictSuppress;

        /// <inheritdoc />
        public void DisableMenuConflictControlsThisFrame()
        {
            foreach (var control in MenuConflictControls)
            {
                Function.Call(Hash.DISABLE_CONTROL_ACTION, 0, (int)control, true);
            }

            if (!_loggedMenuConflictSuppress)
            {
                FileLogger.Info($"DisableMenuConflictControlsThisFrame: suppressing {MenuConflictControls.Length} menu-conflict controls while a mod menu is open");
                _loggedMenuConflictSuppress = true;
            }
        }
```
**Compile-verify the `GTA.Control` member names against THIS SHVDN build.** Each identifier above is checked by the compiler. If any name does not exist in this build's `GTA.Control` enum, the build errors — replace it with the correct member for that intent (e.g. an alternate radio/character-switch member) or drop it, but keep every conflict category represented (weapon wheel, vehicle radio, phone, character switch, attack/aim/melee, cover). Do NOT add movement (`MoveLeftRight`/`MoveUpDown`/`VehicleAccelerate`/`VehicleBrake`) or any `Frontend*` control. Ensure `using FactionWars.ScriptHookV.Logging;` is present in the file for `FileLogger` (add if missing). The method must stay ≤40 lines (CI0007).

- [ ] **Step 5: Implement in `MockGameBridge.cs`.** Add the counter near the other call-count fields (e.g. by `ClearWantedLevelCallCount`):
```csharp
        public int MenuConflictSuppressCallCount { get; private set; }
```
Add the method (place it near `DisableControlThisFrame` if the mock has one, else with the other control methods):
```csharp
        public void DisableMenuConflictControlsThisFrame()
        {
            MenuConflictSuppressCallCount++;
        }
```
In `Reset()`, add:
```csharp
            MenuConflictSuppressCallCount = 0;
```
If `MockGameBridge` does not already implement `DisableControlThisFrame`, confirm it implements the full `IGameBridge` — it must, since the suite builds today; just add the new method so the interface stays satisfied.

- [ ] **Step 6: Run to verify it passes**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~MockGameBridgeMenuControlsTests"`
Expected: PASS (2/2).

- [ ] **Step 7: Build + full unit suite**

Run: `dotnet build FactionWars.sln --no-incremental` then the full `FactionWars.Tests.Unit` filter.
Expected: clean build (0/0), all PASS.

- [ ] **Step 8: Commit**

```bash
git add src/FactionWars tests/FactionWars.Tests
git commit -m "feat: add DisableMenuConflictControlsThisFrame bridge method (#136)"
```

---

### Task 2: `SquadRadialMenuRenderer.IsOpen` + central suppression in the HUD tick

**Files:**
- Modify: `src/FactionWars/ScriptHookV/UI/SquadRadialMenuRenderer.cs` (add `IsOpen`)
- Modify: `src/FactionWars/ScriptHookV/GameLoopController.HudUpdate.cs` (call the suppressor)
- Test: the existing `SquadRadialMenuRenderer` test fixture under `tests/FactionWars.Tests/` (extend; if none exists, create `tests/FactionWars.Tests/Unit/ScriptHookV/UI/SquadRadialMenuRendererIsOpenTests.cs`)

**Interfaces:**
- Consumes: `IGameBridge.DisableMenuConflictControlsThisFrame()` (Task 1), `IMenuProvider.IsMenuVisible`, `SquadRadialMenuRenderer.Update()`.
- Produces: `SquadRadialMenuRenderer.IsOpen` (bool).

- [ ] **Step 1: Write the failing test for `IsOpen`.** First locate any existing renderer test: `ls tests/FactionWars.Tests/Unit/ScriptHookV/UI/ | grep -i radial`. If a fixture exists, add to it (mirror its construction of `SquadRadialMenuRenderer` with a `MockGameBridge` + the three delegates). Otherwise create `SquadRadialMenuRendererIsOpenTests.cs`:
```csharp
using System.Collections.Generic;
using FactionWars.Combat.Models;
using FactionWars.Core.Utils;
using FactionWars.ScriptHookV.UI;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV.UI
{
    public class SquadRadialMenuRendererIsOpenTests
    {
        private const int OpenControl = 174; // INPUT_PHONE_LEFT

        private static SquadRadialMenuRenderer Build(MockGameBridge bridge) =>
            new SquadRadialMenuRenderer(
                bridge,
                () => SquadStance.Defensive,
                (stance, handles) => { },
                () => new List<int>());

        [Fact]
        public void IsOpen_IsFalseInitially()
        {
            var bridge = new MockGameBridge();
            var renderer = Build(bridge);
            Assert.False(renderer.IsOpen);
        }

        [Fact]
        public void IsOpen_IsTrueWhileOpenKeyHeld()
        {
            var bridge = new MockGameBridge();
            bridge.SetControlPressed(OpenControl, true);
            var renderer = Build(bridge);

            renderer.Update();

            Assert.True(renderer.IsOpen);
        }
    }
}
```
**Verify the mock's control-press API.** `MockGameBridge.IsControlPressed(int)` is read by the renderer's `Update()`. Find how the mock lets a test mark a control as pressed (it may be `SetControlPressed(int, bool)`, a settable dictionary, or similar — grep `IsControlPressed` in `src/FactionWars/Core/Utils/MockGameBridge.cs`). Use the ACTUAL API; if the mock has no way to force a pressed control, add a minimal `SetControlPressed(int control, bool pressed)` helper to the mock (and have `IsControlPressed` read it). Keep that addition minimal and note it in the report. Note `Update()` also calls `Draw()` and native time-scale through the bridge when held — if `Draw()`/native calls make `Update()` unsafe under the mock, instead test `IsOpen` by the narrowest path the fixture supports; report any deviation.

- [ ] **Step 2: Run to verify it fails**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~SquadRadialMenuRenderer"`
Expected: FAIL to compile — `IsOpen` missing.

- [ ] **Step 3: Add `IsOpen`** to `SquadRadialMenuRenderer.cs` (the renderer already has a private `SquadRadialMenu _menu` whose `IsOpen` tracks the open state):
```csharp
        /// <summary>True while the radial wheel is currently open (the squad key is held).</summary>
        public bool IsOpen => _menu.IsOpen;
```

- [ ] **Step 4: Run to verify it passes**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~SquadRadialMenuRenderer"`
Expected: PASS.

- [ ] **Step 5: Wire the central suppression** in `GameLoopController.HudUpdate.cs`. In `UpdateAndDrawHud()`, immediately after the existing `_squadRadialMenuRenderer?.Update();` line, add:
```csharp
            // Disable native controls that conflict with menu use (weapon wheel, radio, phone,
            // attack, etc.) while any mod menu is open, so menu interaction can't also trigger
            // a native action. Re-applied every frame because DISABLE_CONTROL_ACTION is per-frame.
            if (_menuProvider.IsMenuVisible || (_squadRadialMenuRenderer?.IsOpen ?? false))
            {
                _gameBridge.DisableMenuConflictControlsThisFrame();
            }
```
`_menuProvider`, `_squadRadialMenuRenderer`, and `_gameBridge` are all existing fields on `GameLoopController`. If `_menuProvider` can be null before init (it is assigned in `InitializeMenuControllers`), guard it: `if ((_menuProvider?.IsMenuVisible ?? false) || (_squadRadialMenuRenderer?.IsOpen ?? false))`. Confirm whether `_menuProvider` is nullable in `GameLoopController.cs` and match its null-safety (use `?.` if the field type is nullable or could be unset when this runs).

- [ ] **Step 6: Build + full unit suite**

Run: `dotnet build FactionWars.sln --no-incremental` then the full `FactionWars.Tests.Unit` filter.
Expected: clean build (0/0), all PASS.

- [ ] **Step 7: Commit**

```bash
git add src/FactionWars tests/FactionWars.Tests
git commit -m "feat: suppress menu-conflict controls while any mod menu is open (#136)"
```

---

## Self-Review

**Spec coverage:** bridge method with curated set + exclusions (Task 1) ✓; `FileLogger` line (Task 1 Step 4) ✓; `MockGameBridge` tracking + Reset (Task 1) ✓; `SquadRadialMenuRenderer.IsOpen` (Task 2) ✓; central application keyed on `IsMenuVisible || radial.IsOpen` covering both menu systems (Task 2 Step 5) ✓; radial existing suppression untouched (no edit to `SuppressControls`) ✓; movement/frontend-nav excluded (Task 1 Step 4 instruction + Global Constraints) ✓.

**Placeholder scan:** the `GTA.Control` member list is concrete; the one deliberate flex point (a member name that may not exist in this SHVDN build) is compile-checked with an explicit fallback instruction, not a TBD.

**Type consistency:** `DisableMenuConflictControlsThisFrame()` (no args, void) is defined in Task 1 and consumed in Task 2 Step 5 identically. `MenuConflictSuppressCallCount` (int) defined and asserted in Task 1. `IsOpen` (bool) defined in Task 2 Step 3 and consumed in Step 5 and the Task 2 tests. `_menuProvider`/`_squadRadialMenuRenderer`/`_gameBridge` are existing `GameLoopController` fields.
