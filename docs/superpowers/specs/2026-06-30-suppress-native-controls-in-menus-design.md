# Suppress Conflicting Native Controls While a Mod Menu Is Open — Design

**Issue:** #136
**Status:** Approved (design), pending spec review

## Goal

While any mod menu is open, stop the player from triggering conflicting native GTA
interactions — the weapon wheel, vehicle radio, phone, character-switch, and
attack/aim/melee — so that interacting with the menu does not also fire a native
action. Applies to **both** mod menu systems (the squad/bodyguard radial wheel and the
NativeUI list menus). Movement and all menu-navigation inputs stay enabled so the menus
remain fully operable.

## Background / Current State

Two menu systems exist:

- **`SquadRadialMenuRenderer`** (the bodyguard/squad radial wheel, held on the phone-left
  key). It already suppresses a set of controls each frame while open
  (`SuppressedControls = { 21, 22, 24, 25, 30, 31, 37, 140 }` plus the look axes for a
  camera lock) via the existing `IGameBridge.DisableControlThisFrame(int)`. That set
  omits some conflicts (e.g. the vehicle radio wheel).
- **`NativeUIMenuProvider`** (the LemonUI list menus: recruitment, zone management, shop,
  settings). Its `Update()` only calls `_menuPool.ProcessMenus()` — it disables **no**
  native controls, so the weapon wheel, radio, phone, etc. all still fire while a list
  menu is open. `NativeUIMenuProvider` does not hold an `IGameBridge`.

`GameLoopController.UpdateAndDrawHud()` runs every frame, already holds both `_gameBridge`
and the `_menuProvider` (which exposes `IsMenuVisible`), and already ticks
`_squadRadialMenuRenderer.Update()`. It is the natural central place to apply suppression
for both systems.

The native call already in use is `DISABLE_CONTROL_ACTION` (group 0), wrapped by
`IGameBridge.DisableControlThisFrame(int control)`.

## Design Decisions (locked)

1. **Scope:** all mod menus — the radial wheel AND the NativeUI list menus.
2. **Aggressiveness:** a curated conflict set, NOT "disable everything". The player can
   still move and look; only conflicting action/UI inputs are suppressed.

## Component 1 — `IGameBridge.DisableMenuConflictControlsThisFrame()`

New bridge method (added to `IGameBridge`, real `GameBridge`, and `MockGameBridge`):

```csharp
/// <summary>
/// Disables the native controls that conflict with mod-menu use for this frame
/// (weapon wheel, vehicle radio, phone, character switch, attack/aim/melee, cover).
/// Movement and frontend menu-navigation controls are intentionally left enabled so
/// the mod menus stay operable. Must be called every frame a mod menu is open.
/// </summary>
void DisableMenuConflictControlsThisFrame();
```

Real `GameBridge` implements it by disabling a curated set, expressed with named
`GTA.Control` members (cast to int and routed through the existing `DISABLE_CONTROL_ACTION`
path) so the list is self-documenting. The set covers, by intent:

- Weapon wheel: `SelectWeapon`.
- Vehicle radio: `VehicleRadioWheel`, `VehicleNextRadio`, `VehiclePrevRadio`.
- Phone: `Phone`.
- Character switch: the character-switch input(s) (e.g. `CharacterWheel` / `SelectCharacter*`).
- Attack / aim / melee: `Attack`, `Aim`, `Attack2`, `MeleeAttack1`, `MeleeAttack2`,
  `VehicleAttack`, `VehicleAim`.
- Cover: `Cover`.

The implementation logs once via `FileLogger` for in-game debugging (per repo convention
for new GameBridge behavior). `MockGameBridge` records invocation (a counter / flag exposed
through a test getter, e.g. `MenuConflictSuppressCount`) and `Reset()` clears it.

**Exclusions (must NOT be in the set):** movement (`MoveLeftRight`, `MoveUpDown`,
`VehicleAccelerate`/`Brake`/`MoveLeftRight`), look axes, and every frontend control the
menus use to navigate/accept/cancel/back (`FrontendUp/Down/Left/Right`, `FrontendAccept`,
`FrontendCancel`, `FrontendPause`, etc.). Disabling any of those would soft-lock the menu.

The exact `GTA.Control` member list is finalized in the plan; the binding requirement is:
include the conflict categories above, exclude movement and all frontend-nav controls.

## Component 2 — `SquadRadialMenuRenderer.IsOpen`

The renderer already tracks whether the wheel is currently held/open internally. Surface
that state as a read-only property:

```csharp
public bool IsOpen { get; }
```

No behavior change to the renderer; its existing per-frame `SuppressControls()` (movement
+ camera lock specific to the wheel pointing) stays exactly as-is. The new central
suppression is additive — overlapping a control in both paths is harmless (disabling twice
in one frame is a no-op).

## Component 3 — Central application in `GameLoopController.UpdateAndDrawHud()`

Each frame, after ticking the radial, suppress conflicts if any mod menu is open:

```csharp
if (_menuProvider.IsMenuVisible || (_squadRadialMenuRenderer?.IsOpen ?? false))
{
    _gameBridge.DisableMenuConflictControlsThisFrame();
}
```

`_menuProvider` is the `IMenuProvider` already held by `GameLoopController`
(`IsMenuVisible` is on the interface). This single decision covers both menu systems
consistently.

## Error handling / risk

- **Soft-lock risk:** the only real failure mode is disabling a control the menus need
  to navigate/accept/back, which would make a menu unusable. Mitigated by the exclusion
  rule above (no movement, no frontend-nav controls in the set). Because the controls are
  native, a wrong ID cannot be caught by unit tests — the bridge method logs, and the set
  is verified in-game.
- **No menu open:** when neither condition holds, nothing is disabled — zero behavior
  change from today.
- **Radial already suppressing:** the central call and the radial's own
  `SuppressControls()` can both disable the same control in a frame; `DISABLE_CONTROL_ACTION`
  is idempotent within a frame, so this is safe.

## Testing

- **Bridge method (`MockGameBridge`):** `DisableMenuConflictControlsThisFrame` increments a
  tracked counter; a test asserts the mock records the call and that `Reset()` clears it.
  (The real native control IDs cannot be unit-tested — verified in-game via the log line.)
- **Radial `IsOpen`:** a unit test on `SquadRadialMenuRenderer` (or its existing test
  fixture) asserts `IsOpen` reflects the held state — true while the open key is held,
  false otherwise — using the mock game bridge's control-press simulation the fixture
  already uses.
- **Central wiring** (`GameLoopController.UpdateAndDrawHud`): composition-root /
  integration-and-log-verified per repo convention; no new unit test for the tick itself,
  but the decision is a trivial `IsMenuVisible || IsOpen` guard over the tested bridge
  method.

## Out of scope (YAGNI)

- A configurable / data-driven control list (the curated set is hardcoded; revisit only if
  tuning is needed).
- Disabling movement or making menus pause the game ("disable all but nav" was explicitly
  rejected in favor of the curated set).
- Changing the radial's existing suppression set or camera-lock behavior.
- Any change to which key opens which menu.
