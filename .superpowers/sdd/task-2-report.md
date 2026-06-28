# Task 2 Report: Legacy-save migration + value-stability guard

## Status: DONE

## Commit
- SHA: `42a5d0a`
- Subject: `Migrate legacy tier keys on save load (#39)`

---

## TDD Evidence

### RED phase

Wrote both test files, then attempted build:

```
dotnet build tests/FactionWars.Tests/FactionWars.Tests.csproj
```

Output (last 8 lines):
```
C:\...\LegacyRoleDictionaryConverterTests.cs(3,31): error CS0234: The type or namespace name 'Converters' does not exist in the namespace 'FactionWars.Persistence' (are you missing an assembly reference?)

Build FAILED.
    0 Warning(s)
    1 Error(s)
```

Confirms RED: converter class not yet implemented.

### GREEN phase

After implementing `LegacyRoleDictionaryConverter` and annotating the two persisted dictionaries:

```
dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj \
  --filter "FullyQualifiedName~LegacyRoleDictionaryConverterTests|FullyQualifiedName~DefenderRoleValuesTests"
```

Output:
```
Passed!  - Failed: 0, Passed: 7, Skipped: 0, Total: 7, Duration: 506 ms
```

Full unit suite:
```
dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj \
  --filter "FullyQualifiedName~FactionWars.Tests.Unit" --no-build
```

Output:
```
Passed!  - Failed: 0, Passed: 3543, Skipped: 0, Total: 3543, Duration: 16 s
```

---

## Files Changed

| Path | Action |
|---|---|
| `src/FactionWars/Persistence/Converters/LegacyRoleDictionaryConverter.cs` | Created |
| `src/FactionWars/Persistence/Models/ZoneDefenderAllocationData.cs` | Modified (added usings + `[JsonConverter]`) |
| `src/FactionWars/Persistence/Models/FactionStateData.cs` | Modified (added usings + `[JsonConverter]`) |
| `tests/FactionWars.Tests/Unit/Persistence/LegacyRoleDictionaryConverterTests.cs` | Created |
| `tests/FactionWars.Tests/Unit/Core/DefenderRoleValuesTests.cs` | Created |

---

## Self-Review Checklist

- [x] Converter handles legacy names (Basic→Grunt, Medium→Gunner, Heavy→Rifleman, Elite→Rocketeer)
- [x] Converter handles new canonical role names (Grunt, Gunner, Rifleman, Rocketeer)
- [x] Converter handles integer-string keys (e.g., "0", "1", "2", "3")
- [x] Converter throws `JsonSerializationException` for unknown keys
- [x] `WriteJson` emits canonical new names, never legacy names
- [x] `ZoneDefenderAllocationData.Troops` annotated with `[JsonConverter(typeof(LegacyRoleDictionaryConverter))]`
- [x] `FactionStateData.ReservePool` annotated with `[JsonConverter(typeof(LegacyRoleDictionaryConverter))]`
- [x] `Read_LegacyTierNames_MapsToRoles` test: all 4 legacy names map correctly
- [x] `Read_NewRoleNames_RoundTrips` test: uses `DefenderRole.Rifleman` (not Sniper, which is Task 3)
- [x] `Write_EmitsNewRoleNames` test: "Grunt" present, "Basic" absent
- [x] `Role_HasStablePersistedValue` Theory: Grunt=0, Gunner=1, Rifleman=2, Rocketeer=3
- [x] Build: 0 warnings, 0 errors
- [x] All 3543 unit tests green
- [x] All files: CRLF, UTF-8 without BOM, trailing newline (FINALNEWLINE check passed)
- [x] Pre-commit gate passed on first re-attempt after fixing trailing newlines

### Note on `WriteJson` nullable fix

The brief's code had `Dictionary<DefenderRole, int> value` in `WriteJson`, which produced CS8765 (nullability mismatch with the base abstract method). Changed to `Dictionary<DefenderRole, int>? value` with a null-check guard, eliminating the warning.

### Note on `Read_NewRoleNames_RoundTrips`

Per the brief, `DefenderRole.Sniper` does not yet exist. The test uses `DefenderRole.Rifleman` as the second key instead. Task 3 will update this to `DefenderRole.Sniper` once the member is added.

---

## Fix wave

### Changes made

| Change | Detail |
|---|---|
| Renamed `Read_NewRoleNames_RoundTrips` | → `Read_NewRoleNames_MapsToRoles` (method-only rename; body unchanged) |
| Added `Read_IntegerStringKeys_MapsToRoles` | Deserializes `{"0":1,"3":2}` and asserts `result[DefenderRole.Grunt] == 1`, `result[DefenderRole.Rocketeer] == 2` |
| Added `Read_UnknownKey_Throws` | Asserts `Assert.Throws<JsonSerializationException>` for `{"Unknown":1}` |

### Covering-test run

Command:
```
dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~LegacyRoleDictionaryConverterTests" --nologo
```

Output:
```
Passed!  - Failed: 0, Passed: 5, Skipped: 0, Total: 5, Duration: 491 ms
```
