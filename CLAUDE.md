# Project Configuration

## Development Lifecycle

This repo uses local git hooks for basic quality gates. Run this once in each clone:

```bash
git config core.hooksPath .githooks
```

Commits are blocked on `main` and `master`. Normal work should happen on feature branches.

Before every commit, `.githooks/pre-commit` runs:
- `dotnet build FactionWars.sln --no-incremental`
- `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~FactionWars.Tests.Unit"`

Run those commands yourself before risky edits. Do not bypass the hook unless the user explicitly asks for it and understands what is being skipped.

## Architecture Guardrails

The mod has one production assembly, but the code is still layered by namespace:
- `Core`, `Factions`, `Territory`, `Economy`, `Combat`, `AI`, `Telemetry`, `Persistence`, `Performance`, and `UI` contain portable domain logic.
- `ScriptHookV` is the composition root and native integration layer.

Architecture tests live in `tests/FactionWars.Tests/Unit/Architecture`.
Custom analyzers live in `src/FactionWars.Analyzers` and run as warnings, not errors.

Rules currently enforced:
- GTA/NativeUI references stay in `ScriptHookV`.
- `Core` does not reference `ScriptHookV`.
- Production code does not reference test-only dependencies.

Analyzer warnings currently report:
- tuple return types
- public methods without coverage from matching test fixtures
- constructors creating disposable dependencies
- classes with too many public methods
- constructors with too many parameters
- nested public types
- overlong methods
- `#pragma warning disable` for `CA*` or `CI*` diagnostics
- skipped/ignored tests
- chained `?.` plus `??` inside method arguments
- concrete production types in constructors where an interface would keep coupling lower
- service-like classes without first-party interfaces
- multiple public top-level types in one file

When adding native behavior, put it behind `IGameBridge` or a renderer/menu interface first, then test the domain behavior against mocks.

## Deployment

GTA V installation path: `E:\SteamLibrary\steamapps\common\Grand Theft Auto V\`

Scripts folder: `E:\SteamLibrary\steamapps\common\Grand Theft Auto V\scripts\`

To deploy, copy the compiled DLL:
```bash
cp "C:/Users/ryan7/programming/gtav-factions/src/FactionWars/bin/Debug/net48/FactionWars.dll" "E:/SteamLibrary/steamapps/common/Grand Theft Auto V/scripts/"
```

## Debugging

Mod logs are stored in the user's Documents folder:

```
C:\Users\ryan7\Documents\FactionWars\Logs\
```

Each game session creates a new timestamped log file (e.g., `FactionWars_2026-01-22_14-30-00.log`).

When debugging in-game issues, read the most recent log file to see what happened during gameplay. The logger supports levels: INFO, DEBUG, WARN, ERROR, COMBAT, ZONE, SPAWN, and AI.

### Debug Logging Guidelines

**All new GameBridge functionality MUST include debug logging** to verify in-game behavior. Since unit tests cannot verify actual GTA V native calls, logging is essential for debugging.

Example pattern:
```csharp
public void SomeNewMethod(int pedHandle, Vector3 position)
{
    FileLogger.AI($"SomeNewMethod: Starting for ped {pedHandle} at ({position.X:F1}, {position.Y:F1}, {position.Z:F1})");

    // ... do work ...

    FileLogger.AI($"SomeNewMethod: Completed for ped {pedHandle}");
}
```

Use appropriate log levels:
- `FileLogger.AI` - AI behavior (wandering, tasks, combat decisions)
- `FileLogger.Spawn` - Ped creation and positioning
- `FileLogger.Combat` - Combat-related actions
- `FileLogger.Zone` - Zone entry/exit events
- `FileLogger.Info` - General information
- `FileLogger.Debug` - Detailed debugging
- `FileLogger.Warn` - Warnings
- `FileLogger.Error` - Errors with exceptions

### Updating Mocks from In-Game Behavior

When debugging against logs reveals that **in-game behavior differs from mock behavior**, update the mock to match reality:

1. **Identify the discrepancy** - Compare log output with what MockGameBridge assumes
2. **Update MockGameBridge** - Correct the mock to reflect actual GTA V behavior
3. **Update mock tests** - Ensure MockGameBridgeTests verify the corrected behavior
4. **Document the finding** - Add a comment explaining what the real behavior is

Example: If logs show `SetPedAsFriendly` puts peds in `FRIENDLY_DEFENDERS` group but the mock assumed `PLAYER` group, update the mock and tests to use `FRIENDLY_DEFENDERS`.

This keeps our test suite accurate and prevents false confidence from tests that pass but don't reflect reality.
