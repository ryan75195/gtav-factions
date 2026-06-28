# Task 7 Report: SniperDeploymentService (perch + sidearm)

## Status
COMPLETE. Commit `ecbc7ed` on `feat/39-sniper-unit-and-roles`.

## TDD Evidence

### RED Phase
```
dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~SniperDeploymentServiceTests"
```
Result: Build failed — `error CS0246: The type or namespace name 'SniperDeploymentService' could not be found`.

### GREEN Phase
After creating ISniperDeploymentService.cs and SniperDeploymentService.cs:
```
Passed!  - Failed: 0, Passed: 4, Skipped: 0, Total: 4
```

### Full Unit Suite
```
Passed!  - Failed: 0, Passed: 3563, Skipped: 0, Total: 3563
```

### Build (analyzer + pre-commit hook)
```
Build succeeded. 0 Warning(s), 0 Error(s)
=== All checks passed ===
```

## Files Changed

| File | Change |
|------|--------|
| `src/FactionWars/ScriptHookV/Combat/Interfaces/ISniperDeploymentService.cs` | Created |
| `src/FactionWars/ScriptHookV/Combat/SniperDeploymentService.cs` | Created |
| `tests/FactionWars.Tests/Unit/ScriptHookV/Combat/SniperDeploymentServiceTests.cs` | Created |

## Signature Adjustments

**MockGameBridge.CreatePed** takes only 2 args `(string modelName, Vector3 position)`, not 4 as shown in the brief. All 4 test `CreatePed` calls had `"michael", "zone1"` removed.

Everything else matched the brief exactly:
- `GroundZResolver` is `Func<float, float, float, float>` (x, y, z → result) — lambda `(x, y, z) => ...` compiles correctly.
- `IPerchResolver.Resolve` takes `Func<float, float, float>` (x, y → z), correctly bridged via `(x, y) => _gameBridge.GetGroundZ(x, y, zoneCenter.Z + PerchSampling.ProbeHeight)`.

## Concerns
None. Class is 70 lines (well within 250). Constructor takes interfaces only. CRLF UTF-8 no-BOM applied to all 3 files.

## Fix wave

### Changes

1. **Null-guard `UpdateCloseDefense`** (`src/FactionWars/ScriptHookV/Combat/SniperDeploymentService.cs`): Added `if (threatPositions == null) threatPositions = System.Array.Empty<Vector3>();` as the first line of the method body, before `GetPedPosition`. Null is now treated as no threats → rifle selected.

2. **Per-handle call counter in MockGameBridge** (`src/FactionWars/Core/Utils/MockGameBridge.cs`): Added `_activeWeaponSetCount` dictionary; `SetPedActiveWeapon` now increments the counter per handle; new public getter `GetActiveWeaponSetCount(int pedHandle)` returns the count (0 default).

3. **Two new tests** (`tests/FactionWars.Tests/Unit/ScriptHookV/Combat/SniperDeploymentServiceTests.cs`):
   - `UpdateCloseDefense_SameFarThreat_SetActiveWeaponCalledOnlyOnce` — calls twice with identical far threat; asserts `GetActiveWeaponSetCount == 1`.
   - `UpdateCloseDefense_NullThreats_SetsRifleAndDoesNotThrow` — calls with `null!`; asserts rifle is set and no exception is thrown.

### Covering-test command and output

```
dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~SniperDeploymentServiceTests" --nologo
```

```
Passed!  - Failed: 0, Passed: 6, Skipped: 0, Total: 6, Duration: 359 ms - FactionWars.Tests.dll (net48)
```

(4 pre-existing tests + 2 new = 6 total, all green.)