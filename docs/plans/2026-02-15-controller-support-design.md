# Controller (Gamepad) Support Design

**Goal:** Add Xbox/PlayStation controller support so all mod functions work with a gamepad plugged in.

## Button Mapping

| Action | Keyboard | Controller |
|--------|----------|------------|
| Open/close menu | F7 | LB + D-pad Right |
| Claim zone | E | D-pad Right (no LB held) |
| Cycle battle HUD | B | D-pad Down |
| Menu select + hold-repeat | Enter | A button |
| Menu navigation | Arrow keys | D-pad Up/Down (NativeUI built-in) |
| Menu back | Backspace | B button (NativeUI built-in) |

## Architecture

NativeUI already handles controller input inside menus (D-pad navigation, A to select, B to go back). We only need to add controller polling for the mod's custom keybinds.

### Approach

Poll controller buttons in `OnTick` using ScriptHookVDotNet3's `Game.IsControlJustPressed` / `Game.IsControlPressed`. Runs alongside existing keyboard handling - both work simultaneously.

### GTA V Control IDs

| Control | ID | Notes |
|---------|----|-------|
| D-pad Up | 27 | INPUT_PHONE_UP |
| D-pad Down | 173 | INPUT_PHONE_DOWN |
| D-pad Left | 174 | INPUT_PHONE_LEFT |
| D-pad Right | 175 | INPUT_PHONE_RIGHT |
| A / Cross | 201 | INPUT_FRONTEND_ACCEPT |
| B / Circle | 202 | INPUT_FRONTEND_CANCEL |
| LB / L1 | 37 | INPUT_AIM |

## Files to Change

| Action | File |
|--------|------|
| Modify | `src/FactionWars/Core/Interfaces/IGameBridge.cs` - Add `IsControlPressed`, `IsControlJustPressed` |
| Modify | `src/FactionWars/ScriptHookV/GameBridge.cs` - Implement via GTA native calls |
| Modify | `src/FactionWars/Core/Utils/MockGameBridge.cs` - Mock implementations |
| Modify | `src/FactionWars/ScriptHookV/GameLoopController.cs` - Poll controller in OnTick |

## Edge Cases

- **LB + D-pad Right combo:** Check LB is held before D-pad Right triggers menu. Bare D-pad Right (no LB) triggers claim zone instead.
- **Menu open:** When menu is open, don't process game actions (claim zone, cycle HUD) - let NativeUI handle all input.
- **Simultaneous keyboard + controller:** Both work independently, no conflicts.
