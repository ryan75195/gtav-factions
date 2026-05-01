# Savegame Integration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Tie FactionWars mod state to GTA V's native single-player savegame via a sidecar JSON file approach, so the player saves/loads through GTA's pause menu and the mod mirrors automatically.

**Architecture:** A `FileSystemWatcher` (`NativeSaveWatcher`) on Rockstar's profile dir detects native saves and triggers sidecar writes. A `LoadDetector` watches loading-screen edges and uses a state fingerprint (primary key: total-playing-time stat; tiebreakers: money, completed-mission count, in-game clock) to identify which sidecar to hydrate. The mod's standalone save/load UI is removed.

**Tech Stack:** .NET Framework 4.8, C# (xUnit + Moq for tests), ScriptHookVDotNet for in-game natives. Project layout follows existing conventions: `src/FactionWars/` with `tests/FactionWars.Tests/` mirroring the structure.

**Spec:** `docs/superpowers/specs/2026-05-01-savegame-integration-design.md`

---

## File structure

**New files:**

| File | Responsibility |
|---|---|
| `src/FactionWars/Persistence/Models/SaveFingerprint.cs` | Value object identifying a save by stable in-game fields; matching primitives. |
| `src/FactionWars/Persistence/Models/Sidecar.cs` | Sidecar payload DTO matching JSON schema. |
| `src/FactionWars/Persistence/Models/PlayerPosition.cs` | Position+heading record stored in sidecar (recorded for future use, not consumed in v1). |
| `src/FactionWars/Persistence/ISidecarStore.cs` | Sidecar read/write interface. |
| `src/FactionWars/Persistence/SidecarStore.cs` | Filesystem-backed sidecar store. |
| `src/FactionWars/ScriptHookV/Persistence/NativeSaveWatcher.cs` | `FileSystemWatcher` on Rockstar profile dir with debouncing. |
| `src/FactionWars/ScriptHookV/Persistence/LoadDetector.cs` | Loading-screen edge detection, fires sidecar lookup. |
| `src/FactionWars/ScriptHookV/Persistence/LegacyBackupTask.cs` | One-shot first-launch migration. |
| `tests/FactionWars.Tests/Unit/Persistence/SaveFingerprintTests.cs` | |
| `tests/FactionWars.Tests/Unit/Persistence/SidecarStoreTests.cs` | |
| `tests/FactionWars.Tests/Unit/ScriptHookV/Persistence/NativeSaveWatcherTests.cs` | Integration test (real temp filesystem). |
| `tests/FactionWars.Tests/Unit/ScriptHookV/Persistence/LoadDetectorTests.cs` | |
| `tests/FactionWars.Tests/Unit/ScriptHookV/Persistence/LegacyBackupTaskTests.cs` | |

**Files to modify:**

| File | What changes |
|---|---|
| `src/FactionWars/Persistence/Models/GameState.cs` | Drop `PlayerMoney`, `PlayerWeapons` fields. |
| `src/FactionWars/Core/Interfaces/IGameBridge.cs` | Add `GetTotalPlayTimeSeconds`, `GetCompletedMissionCount`, `GetInGameClockMinutes`. |
| `src/FactionWars/ScriptHookV/GameBridge.cs` | Implement the three new methods using SHVDN natives. |
| `src/FactionWars/ScriptHookV/Mocks/MockGameBridge.cs` | Implement new methods returning seedable values. |
| `src/FactionWars/ScriptHookV/Persistence/GameStateManager.cs` | Remove `SaveToSlot`/`LoadFromSlot`/money-weapons mirroring; add `WriteCurrentSidecar`/`HydrateFromSidecar`. |
| `src/FactionWars/ScriptHookV/Persistence/IGameStateManager.cs` | Mirror the `GameStateManager` API change. |
| `src/FactionWars/ScriptHookV/UI/MainMenuController.cs` | Remove save/load buttons. |
| `src/FactionWars/ScriptHookV/UI/SettingsMenuController.cs` | Remove save/load entries. |
| `src/FactionWars/ScriptHookV/ServiceContainerFactory.cs` | Register new components; deregister `AutoSaveService` and `SaveSlotManager`. |
| `src/FactionWars/ScriptHookV/FactionWarsScript.cs` | Subscribe `NativeSaveWatcher` and `LoadDetector` events. |
| `tests/FactionWars.Tests/Unit/ScriptHookV/Persistence/GameStateManagerTests.cs` | Adapt to new API; drop money/weapons assertions. |
| `tests/FactionWars.Tests/Unit/ScriptHookV/Mocks/MockGameBridgeTests.cs` | Test new mock methods. |

**Files to delete:**

| File | Reason |
|---|---|
| `src/FactionWars/Persistence/SaveSlotManager.cs` | Replaced by `SidecarStore`. |
| `src/FactionWars/Persistence/Models/SaveSlotInfo.cs` | Mod no longer enumerates slots. |
| `src/FactionWars/Core/Interfaces/ISaveSlotManager.cs` | Replaced by `ISidecarStore`. |
| `src/FactionWars/Core/Services/StubSaveSlotManager.cs` | Stub no longer needed. |
| `src/FactionWars/Persistence/AutoSaveService.cs` | GTA's own saves drive sidecar writes; mod-side timer is obsolete. |
| `src/FactionWars/Core/Interfaces/IAutoSaveService.cs` | Same. |
| Corresponding tests for the above | Same. |

---

## Conventions used in tasks

- **Test framework:** xUnit (`[Fact]` / `[Theory]`).
- **Mocking:** Moq.
- **Existing test path:** `tests/FactionWars.Tests/Unit/<area>/<TypeName>Tests.cs`.
- **Build/test commands:**
  - `dotnet build src/FactionWars/FactionWars.csproj` — compile mod
  - `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~<TypeName>"` — run a single test class
  - `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj` — full suite
- **Logging:** `FactionWars.ScriptHookV.Logging.FileLogger` (existing). New code uses appropriate level (`Info` / `Error` / `Debug`).
- **Commit style** matches existing history: `feat:`/`refactor:`/`test:`/`chore:` prefix, lowercase first word.

---

## Task 1: `SaveFingerprint` value object

**Files:**
- Create: `src/FactionWars/Persistence/Models/SaveFingerprint.cs`
- Test: `tests/FactionWars.Tests/Unit/Persistence/SaveFingerprintTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
// tests/FactionWars.Tests/Unit/Persistence/SaveFingerprintTests.cs
using FactionWars.Persistence.Models;
using Xunit;

namespace FactionWars.Tests.Unit.Persistence
{
    public class SaveFingerprintTests
    {
        private static SaveFingerprint Make(long playTime = 12340, int money = 50000, int missions = 23, int clockMinutes = 854)
            => new SaveFingerprint
            {
                TotalPlayTimeSeconds = playTime,
                Money = money,
                CompletedMissionCount = missions,
                InGameClockMinutes = clockMinutes,
            };

        [Fact]
        public void ExactMatch_AllFieldsEqual_ReturnsTrue()
        {
            var a = Make();
            var b = Make();
            Assert.True(a.ExactMatch(b));
        }

        [Fact]
        public void ExactMatch_TotalPlayTimeDiffers_ReturnsFalse()
        {
            var a = Make(playTime: 12340);
            var b = Make(playTime: 12341);
            Assert.False(a.ExactMatch(b));
        }

        [Fact]
        public void ExactMatch_MoneyDiffers_ReturnsFalse()
        {
            var a = Make(money: 50000);
            var b = Make(money: 50001);
            Assert.False(a.ExactMatch(b));
        }

        [Fact]
        public void ExactMatch_MissionCountDiffers_ReturnsFalse()
        {
            var a = Make(missions: 23);
            var b = Make(missions: 24);
            Assert.False(a.ExactMatch(b));
        }

        [Fact]
        public void ExactMatch_ClockDiffers_ReturnsFalse()
        {
            var a = Make(clockMinutes: 854);
            var b = Make(clockMinutes: 855);
            Assert.False(a.ExactMatch(b));
        }

        [Fact]
        public void PrimaryMatch_OnlyTotalPlayTime_OtherFieldsDiffer_ReturnsTrue()
        {
            var a = Make(playTime: 12340, money: 50000, missions: 23, clockMinutes: 854);
            var b = Make(playTime: 12340, money: 99999, missions: 99, clockMinutes: 0);
            Assert.True(a.PrimaryMatch(b));
        }

        [Fact]
        public void PrimaryMatch_TotalPlayTimeDiffers_ReturnsFalse()
        {
            var a = Make(playTime: 12340);
            var b = Make(playTime: 12341);
            Assert.False(a.PrimaryMatch(b));
        }
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~SaveFingerprintTests"`
Expected: COMPILE FAIL (`SaveFingerprint` does not exist).

- [ ] **Step 3: Write minimal implementation**

```csharp
// src/FactionWars/Persistence/Models/SaveFingerprint.cs
namespace FactionWars.Persistence.Models
{
    /// <summary>
    /// Stable identification of a GTA V save based on in-game state values that
    /// are preserved by the savegame and restored exactly on load.
    /// </summary>
    public sealed class SaveFingerprint
    {
        /// <summary>Primary key. Monotonically increasing seconds-played, persisted in the save.</summary>
        public long TotalPlayTimeSeconds { get; set; }

        /// <summary>Tiebreaker. Player's GTA money at save time.</summary>
        public int Money { get; set; }

        /// <summary>Tiebreaker. Number of completed story missions.</summary>
        public int CompletedMissionCount { get; set; }

        /// <summary>Tiebreaker. In-game clock as minutes-of-day (HH*60+MM).</summary>
        public int InGameClockMinutes { get; set; }

        /// <summary>True if all four fields are equal.</summary>
        public bool ExactMatch(SaveFingerprint other)
        {
            if (other == null) return false;
            return TotalPlayTimeSeconds == other.TotalPlayTimeSeconds
                && Money == other.Money
                && CompletedMissionCount == other.CompletedMissionCount
                && InGameClockMinutes == other.InGameClockMinutes;
        }

        /// <summary>True if only TotalPlayTimeSeconds matches (used for O(1) lookup).</summary>
        public bool PrimaryMatch(SaveFingerprint other)
        {
            if (other == null) return false;
            return TotalPlayTimeSeconds == other.TotalPlayTimeSeconds;
        }
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~SaveFingerprintTests"`
Expected: All 7 tests PASS.

- [ ] **Step 5: Commit**

```bash
git add src/FactionWars/Persistence/Models/SaveFingerprint.cs tests/FactionWars.Tests/Unit/Persistence/SaveFingerprintTests.cs
git commit -m "feat: add SaveFingerprint value object for save identification"
```

---

## Task 2: `PlayerPosition` model

**Files:**
- Create: `src/FactionWars/Persistence/Models/PlayerPosition.cs`

No tests — trivial DTO, no behavior.

- [ ] **Step 1: Write the file**

```csharp
// src/FactionWars/Persistence/Models/PlayerPosition.cs
namespace FactionWars.Persistence.Models
{
    /// <summary>
    /// Player position and heading at save time. Recorded in the sidecar but
    /// not consumed by load logic in v1 — reserved for a future "restore position" feature.
    /// </summary>
    public sealed class PlayerPosition
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float Heading { get; set; }
    }
}
```

- [ ] **Step 2: Compile**

Run: `dotnet build src/FactionWars/FactionWars.csproj`
Expected: SUCCESS.

- [ ] **Step 3: Commit**

```bash
git add src/FactionWars/Persistence/Models/PlayerPosition.cs
git commit -m "feat: add PlayerPosition DTO for sidecar payload"
```

---

## Task 3: `Sidecar` payload model

**Files:**
- Create: `src/FactionWars/Persistence/Models/Sidecar.cs`

- [ ] **Step 1: Write the file**

```csharp
// src/FactionWars/Persistence/Models/Sidecar.cs
using System;

namespace FactionWars.Persistence.Models
{
    /// <summary>
    /// Sidecar JSON payload — mirrors a single GTA V save. Keyed on disk by
    /// the fingerprint's TotalPlayTimeSeconds.
    /// </summary>
    public sealed class Sidecar
    {
        public int Version { get; set; } = 1;

        public SaveFingerprint Fingerprint { get; set; } = new SaveFingerprint();

        public DateTime WrittenAtUtc { get; set; }

        /// <summary>Best-effort: filename of the GTA save this sidecar was written for. Not authoritative.</summary>
        public string NativeSaveFilename { get; set; } = string.Empty;

        /// <summary>Recorded for a future restore-position feature; not consumed in v1.</summary>
        public PlayerPosition PlayerPosition { get; set; } = new PlayerPosition();

        public GameState GameState { get; set; } = new GameState();
    }
}
```

- [ ] **Step 2: Compile**

Run: `dotnet build src/FactionWars/FactionWars.csproj`
Expected: SUCCESS.

- [ ] **Step 3: Commit**

```bash
git add src/FactionWars/Persistence/Models/Sidecar.cs
git commit -m "feat: add Sidecar payload model"
```

---

## Task 4: `GameState` surgery — drop money/weapons

**Files:**
- Modify: `src/FactionWars/Persistence/Models/GameState.cs`

- [ ] **Step 1: Remove fields**

Delete lines 70-79 of `GameState.cs` (the `PlayerMoney` and `PlayerWeapons` properties and their docstrings).

After edit, the file should NOT contain:
```csharp
public int PlayerMoney { get; set; }
public Dictionary<string, int> PlayerWeapons { get; set; } = new Dictionary<string, int>();
```

Also remove the `using System.Collections.Generic;` import if unused after the edit (check via build).

- [ ] **Step 2: Compile and observe expected breakage**

Run: `dotnet build src/FactionWars/FactionWars.csproj`
Expected: FAIL — `GameStateManager.cs` still references `gameState.PlayerMoney` and `gameState.PlayerWeapons`. This is intentional and will be fixed in Task 11 (`GameStateManager` surgery). Note the failing references for the next task.

Do NOT commit yet — leaves repo in broken state. Continue to Task 5; we'll bundle the commit at the end of `GameStateManager` surgery (Task 11).

> **Note for executor:** Tasks 4–11 form a single coherent refactor. Build will fail in between. That's expected — they commit together at the end of Task 11.

---

## Task 5: `IGameBridge` — add fingerprint primitive accessors

**Files:**
- Modify: `src/FactionWars/Core/Interfaces/IGameBridge.cs`

- [ ] **Step 1: Add three new method signatures to the interface**

Add these methods to `IGameBridge` (anywhere in the interface body — placement near the existing `GetPlayerMoney()` is natural):

```csharp
/// <summary>
/// Gets the player's total play time in seconds, as tracked by GTA V's stats system
/// (e.g., MP0_TOTAL_PLAYING_TIME or its single-player equivalent). Persisted in the
/// savegame and restored exactly on load — primary key for save identification.
/// </summary>
/// <returns>Total seconds played, monotonically increasing across the campaign.</returns>
long GetTotalPlayTimeSeconds();

/// <summary>
/// Gets the count of completed story missions for the active character.
/// Used as a tiebreaker for save fingerprint matching.
/// </summary>
/// <returns>Number of completed missions.</returns>
int GetCompletedMissionCount();

/// <summary>
/// Gets the in-game wall clock as minutes-of-day (HH*60+MM, 0-1439).
/// Used as a tiebreaker for save fingerprint matching.
/// </summary>
/// <returns>Minutes-of-day in [0, 1440).</returns>
int GetInGameClockMinutes();
```

- [ ] **Step 2: Compile and observe expected breakage**

Run: `dotnet build src/FactionWars/FactionWars.csproj`
Expected: FAIL — `GameBridge` and `MockGameBridge` don't implement the new methods. Fixed in Tasks 6–7.

---

## Task 6: `MockGameBridge` — implement new fingerprint methods

**Files:**
- Modify: `src/FactionWars/ScriptHookV/Mocks/MockGameBridge.cs`
- Modify: `tests/FactionWars.Tests/Unit/ScriptHookV/Mocks/MockGameBridgeTests.cs`

- [ ] **Step 1: Write failing test**

Append to `MockGameBridgeTests.cs`:

```csharp
[Fact]
public void GetTotalPlayTimeSeconds_DefaultsToZero()
{
    var bridge = new MockGameBridge();
    Assert.Equal(0L, bridge.GetTotalPlayTimeSeconds());
}

[Fact]
public void GetTotalPlayTimeSeconds_ReturnsSeededValue()
{
    var bridge = new MockGameBridge { TotalPlayTimeSeconds = 12340 };
    Assert.Equal(12340L, bridge.GetTotalPlayTimeSeconds());
}

[Fact]
public void GetCompletedMissionCount_ReturnsSeededValue()
{
    var bridge = new MockGameBridge { CompletedMissionCount = 23 };
    Assert.Equal(23, bridge.GetCompletedMissionCount());
}

[Fact]
public void GetInGameClockMinutes_ReturnsSeededValue()
{
    var bridge = new MockGameBridge { InGameClockMinutes = 854 };
    Assert.Equal(854, bridge.GetInGameClockMinutes());
}
```

- [ ] **Step 2: Run tests — expect compile failure**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~MockGameBridgeTests"`
Expected: COMPILE FAIL — `TotalPlayTimeSeconds`/etc. properties don't exist on `MockGameBridge` yet.

- [ ] **Step 3: Add implementation**

In `MockGameBridge.cs`, add public properties and method implementations:

```csharp
public long TotalPlayTimeSeconds { get; set; }
public int CompletedMissionCount { get; set; }
public int InGameClockMinutes { get; set; }

public long GetTotalPlayTimeSeconds() => TotalPlayTimeSeconds;
public int GetCompletedMissionCount() => CompletedMissionCount;
public int GetInGameClockMinutes() => InGameClockMinutes;
```

- [ ] **Step 4: Run tests — verify pass**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~MockGameBridgeTests"`
Expected: New tests PASS. (Build of full test project may still fail because `GameBridge.cs` doesn't implement new methods yet — fixed in Task 7.)

> **Don't commit yet** — same multi-task commit window started in Task 4.

---

## Task 7: `GameBridge` — implement new fingerprint methods using SHVDN natives

**Files:**
- Modify: `src/FactionWars/ScriptHookV/GameBridge.cs`

This task can't be unit-tested — natives only run in-game. Verification is via the manual checklist (Task 16) plus `FileLogger` output.

- [ ] **Step 1: Add implementations**

Add these methods to `GameBridge` (in the same area as `GetPlayerMoney`, etc.):

```csharp
public long GetTotalPlayTimeSeconds()
{
    // GTA V tracks playing time per-character in stats. SP characters use
    // SP0_TOTAL_PLAYING_TIME / SP1_ / SP2_. We pick the active SP character.
    // Stat values are in milliseconds; convert to seconds.
    int activeChar = GetActiveSpCharacterIndex();
    string statName = $"SP{activeChar}_TOTAL_PLAYING_TIME";
    int hash = GTA.Native.Function.Call<int>(GTA.Native.Hash.GET_HASH_KEY, statName);
    int millisOut = 0;
    GTA.Native.Function.Call(GTA.Native.Hash.STAT_GET_INT, hash, new GTA.Native.OutputArgument(millisOut), -1);
    long seconds = millisOut / 1000L;
    FileLogger.Debug($"GetTotalPlayTimeSeconds: stat={statName} ms={millisOut} s={seconds}");
    return seconds;
}

public int GetCompletedMissionCount()
{
    // Use the SP mission completed counter. The exact stat name is verified
    // against the manual checklist; if SP0_TOTAL_MISSIONS_PASSED proves wrong
    // in-game, swap to MP0_MISSIONS_PASSED and update both this code and the
    // mock alignment per CLAUDE.md guidance.
    int activeChar = GetActiveSpCharacterIndex();
    string statName = $"SP{activeChar}_TOTAL_MISSIONS_PASSED";
    int hash = GTA.Native.Function.Call<int>(GTA.Native.Hash.GET_HASH_KEY, statName);
    int valueOut = 0;
    GTA.Native.Function.Call(GTA.Native.Hash.STAT_GET_INT, hash, new GTA.Native.OutputArgument(valueOut), -1);
    FileLogger.Debug($"GetCompletedMissionCount: stat={statName} value={valueOut}");
    return valueOut;
}

public int GetInGameClockMinutes()
{
    int hours = GTA.Native.Function.Call<int>(GTA.Native.Hash.GET_CLOCK_HOURS);
    int minutes = GTA.Native.Function.Call<int>(GTA.Native.Hash.GET_CLOCK_MINUTES);
    int total = hours * 60 + minutes;
    FileLogger.Debug($"GetInGameClockMinutes: {hours:D2}:{minutes:D2} = {total}");
    return total;
}

private int GetActiveSpCharacterIndex()
{
    // Maps the active player model to an SP character index 0/1/2 (Michael/Franklin/Trevor).
    // Falls back to 0 if model is unrecognized.
    string model = GetPlayerCharacterModel();
    if (string.Equals(model, "player_one", System.StringComparison.OrdinalIgnoreCase)) return 1;   // Trevor
    if (string.Equals(model, "player_two", System.StringComparison.OrdinalIgnoreCase)) return 2;   // Franklin
    return 0; // player_zero / Michael
}
```

> **Note:** The exact stat names (`SP0_TOTAL_PLAYING_TIME`, `SP0_TOTAL_MISSIONS_PASSED`) are validated against in-game logs during manual verification (Task 16). If a name turns out wrong, log lines will show `value=0` despite played time/missions; update the constant and re-deploy. Per CLAUDE.md "Updating Mocks from In-Game Behavior", any divergence found in-game is reflected back to the mock and tests.

- [ ] **Step 2: Compile**

Run: `dotnet build src/FactionWars/FactionWars.csproj`
Expected: SUCCESS (the previously broken IGameBridge implementations are now satisfied).

> **Don't commit yet** — continuing the chain through Task 11.

---

## Task 8: `SidecarStore` interface and tests

**Files:**
- Create: `src/FactionWars/Persistence/ISidecarStore.cs`
- Create: `tests/FactionWars.Tests/Unit/Persistence/SidecarStoreTests.cs`

- [ ] **Step 1: Write the interface**

```csharp
// src/FactionWars/Persistence/ISidecarStore.cs
using FactionWars.Persistence.Models;
using System.Collections.Generic;

namespace FactionWars.Persistence
{
    /// <summary>
    /// Read/write store for sidecar JSON files keyed by the primary fingerprint
    /// (TotalPlayTimeSeconds).
    /// </summary>
    public interface ISidecarStore
    {
        /// <summary>
        /// Writes a sidecar to disk. Overwrites any existing sidecar with the same
        /// primary key. Atomic via tmp+rename. Failures are caught and logged;
        /// this method does not throw on IO errors.
        /// </summary>
        void WriteSidecar(Sidecar sidecar);

        /// <summary>
        /// Looks up a sidecar by fingerprint. Performs an O(1) filename lookup
        /// keyed on TotalPlayTimeSeconds, then validates ExactMatch on tiebreakers.
        /// </summary>
        /// <returns>True if a fully-matching sidecar was found.</returns>
        bool TryFindByFingerprint(SaveFingerprint fingerprint, out Sidecar sidecar);

        /// <summary>
        /// Returns all sidecar files currently on disk (in arbitrary order).
        /// </summary>
        IReadOnlyList<Sidecar> ListAll();
    }
}
```

- [ ] **Step 2: Write failing tests**

```csharp
// tests/FactionWars.Tests/Unit/Persistence/SidecarStoreTests.cs
using FactionWars.Persistence;
using FactionWars.Persistence.Models;
using System;
using System.IO;
using System.Linq;
using Xunit;

namespace FactionWars.Tests.Unit.Persistence
{
    public class SidecarStoreTests : IDisposable
    {
        private readonly string _tempDir;

        public SidecarStoreTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "fw_sidecar_test_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
        }

        public void Dispose()
        {
            try { Directory.Delete(_tempDir, recursive: true); } catch { }
        }

        private static Sidecar Make(long playTime = 12340, int money = 50000, int missions = 23, int clock = 854)
            => new Sidecar
            {
                Fingerprint = new SaveFingerprint
                {
                    TotalPlayTimeSeconds = playTime,
                    Money = money,
                    CompletedMissionCount = missions,
                    InGameClockMinutes = clock,
                },
                NativeSaveFilename = "SGTA00003",
                WrittenAtUtc = new DateTime(2026, 5, 1, 12, 0, 0, DateTimeKind.Utc),
                GameState = new GameState { SaveName = "test" },
            };

        [Fact]
        public void WriteSidecar_ThenTryFind_RoundTrips()
        {
            var store = new SidecarStore(_tempDir);
            var original = Make();
            store.WriteSidecar(original);

            Assert.True(store.TryFindByFingerprint(original.Fingerprint, out var loaded));
            Assert.Equal(12340L, loaded.Fingerprint.TotalPlayTimeSeconds);
            Assert.Equal(50000, loaded.Fingerprint.Money);
            Assert.Equal("test", loaded.GameState.SaveName);
        }

        [Fact]
        public void TryFindByFingerprint_NoSuchSidecar_ReturnsFalse()
        {
            var store = new SidecarStore(_tempDir);
            var fp = new SaveFingerprint { TotalPlayTimeSeconds = 999999 };

            Assert.False(store.TryFindByFingerprint(fp, out _));
        }

        [Fact]
        public void TryFindByFingerprint_PrimaryMatchButTiebreakerMismatch_ReturnsFalse()
        {
            var store = new SidecarStore(_tempDir);
            store.WriteSidecar(Make(playTime: 12340, money: 50000));

            var differentFp = new SaveFingerprint
            {
                TotalPlayTimeSeconds = 12340,
                Money = 99999,                  // diverges
                CompletedMissionCount = 23,
                InGameClockMinutes = 854,
            };

            Assert.False(store.TryFindByFingerprint(differentFp, out _));
        }

        [Fact]
        public void TryFindByFingerprint_CorruptJson_ReturnsFalseAndDoesNotThrow()
        {
            var store = new SidecarStore(_tempDir);
            var corruptPath = Path.Combine(_tempDir, "sidecar_12340.json");
            File.WriteAllText(corruptPath, "{ this is not valid json");

            var fp = new SaveFingerprint { TotalPlayTimeSeconds = 12340 };
            Assert.False(store.TryFindByFingerprint(fp, out _));
            Assert.True(File.Exists(corruptPath)); // bad file left on disk
        }

        [Fact]
        public void WriteSidecar_OverwritesExistingFile()
        {
            var store = new SidecarStore(_tempDir);
            store.WriteSidecar(Make(money: 50000));
            store.WriteSidecar(Make(money: 70000));

            Assert.True(store.TryFindByFingerprint(new SaveFingerprint { TotalPlayTimeSeconds = 12340, Money = 70000, CompletedMissionCount = 23, InGameClockMinutes = 854 }, out var loaded));
            Assert.Equal(70000, loaded.Fingerprint.Money);
        }

        [Fact]
        public void ListAll_ReturnsAllSidecarsOnDisk()
        {
            var store = new SidecarStore(_tempDir);
            store.WriteSidecar(Make(playTime: 100));
            store.WriteSidecar(Make(playTime: 200));
            store.WriteSidecar(Make(playTime: 300));

            var all = store.ListAll();
            Assert.Equal(3, all.Count);
            Assert.Contains(all, s => s.Fingerprint.TotalPlayTimeSeconds == 100);
            Assert.Contains(all, s => s.Fingerprint.TotalPlayTimeSeconds == 200);
            Assert.Contains(all, s => s.Fingerprint.TotalPlayTimeSeconds == 300);
        }

        [Fact]
        public void WriteSidecar_FilenameUsesPrimaryFingerprintKey()
        {
            var store = new SidecarStore(_tempDir);
            store.WriteSidecar(Make(playTime: 12340));

            Assert.True(File.Exists(Path.Combine(_tempDir, "sidecar_12340.json")));
        }
    }
}
```

- [ ] **Step 3: Run tests — expect compile failure**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~SidecarStoreTests"`
Expected: COMPILE FAIL — `SidecarStore` doesn't exist yet.

---

## Task 9: `SidecarStore` implementation

**Files:**
- Create: `src/FactionWars/Persistence/SidecarStore.cs`

- [ ] **Step 1: Implement**

```csharp
// src/FactionWars/Persistence/SidecarStore.cs
using FactionWars.Persistence.Models;
using FactionWars.ScriptHookV.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FactionWars.Persistence
{
    public sealed class SidecarStore : ISidecarStore
    {
        private const string FilePrefix = "sidecar_";
        private const string FileExtension = ".json";

        private readonly string _directory;
        private readonly JsonSerializerSettings _settings;

        public SidecarStore(string directory)
        {
            if (string.IsNullOrEmpty(directory))
            {
                throw new ArgumentException("Directory must be non-empty.", nameof(directory));
            }

            _directory = directory;
            Directory.CreateDirectory(_directory);

            _settings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore,
            };
        }

        public void WriteSidecar(Sidecar sidecar)
        {
            if (sidecar == null) throw new ArgumentNullException(nameof(sidecar));
            if (sidecar.Fingerprint == null) throw new ArgumentException("Sidecar.Fingerprint required.", nameof(sidecar));

            var finalPath = GetPath(sidecar.Fingerprint.TotalPlayTimeSeconds);
            var tmpPath = finalPath + ".tmp";

            try
            {
                var json = JsonConvert.SerializeObject(sidecar, _settings);
                File.WriteAllText(tmpPath, json);

                if (File.Exists(finalPath))
                {
                    File.Delete(finalPath);
                }
                File.Move(tmpPath, finalPath);

                FileLogger.Info($"SidecarStore: wrote {Path.GetFileName(finalPath)} (totalPlayTime={sidecar.Fingerprint.TotalPlayTimeSeconds})");
            }
            catch (Exception ex)
            {
                FileLogger.Error($"SidecarStore: failed to write {Path.GetFileName(finalPath)}", ex);
                try { if (File.Exists(tmpPath)) File.Delete(tmpPath); } catch { /* swallow */ }
            }
        }

        public bool TryFindByFingerprint(SaveFingerprint fingerprint, out Sidecar sidecar)
        {
            sidecar = null!;
            if (fingerprint == null) return false;

            var path = GetPath(fingerprint.TotalPlayTimeSeconds);
            if (!File.Exists(path)) return false;

            try
            {
                var json = File.ReadAllText(path);
                var loaded = JsonConvert.DeserializeObject<Sidecar>(json, _settings);
                if (loaded == null || loaded.Fingerprint == null)
                {
                    FileLogger.Warn($"SidecarStore: {Path.GetFileName(path)} deserialized to null; treating as no-match.");
                    return false;
                }

                if (!fingerprint.ExactMatch(loaded.Fingerprint))
                {
                    FileLogger.Warn($"SidecarStore: {Path.GetFileName(path)} primary key matched but tiebreakers diverged; treating as no-match.");
                    return false;
                }

                sidecar = loaded;
                return true;
            }
            catch (Exception ex)
            {
                FileLogger.Error($"SidecarStore: failed to read {Path.GetFileName(path)}", ex);
                return false;
            }
        }

        public IReadOnlyList<Sidecar> ListAll()
        {
            var results = new List<Sidecar>();
            if (!Directory.Exists(_directory)) return results;

            foreach (var path in Directory.EnumerateFiles(_directory, FilePrefix + "*" + FileExtension))
            {
                try
                {
                    var json = File.ReadAllText(path);
                    var loaded = JsonConvert.DeserializeObject<Sidecar>(json, _settings);
                    if (loaded != null) results.Add(loaded);
                }
                catch (Exception ex)
                {
                    FileLogger.Error($"SidecarStore: skipping unreadable {Path.GetFileName(path)}", ex);
                }
            }

            return results;
        }

        private string GetPath(long totalPlayTimeSeconds)
            => Path.Combine(_directory, $"{FilePrefix}{totalPlayTimeSeconds}{FileExtension}");
    }
}
```

> **Note:** This depends on Newtonsoft.Json being available in the project. The existing `IPersistenceService` implementation uses it, so the package reference is in place. Verify by inspecting `src/FactionWars/FactionWars.csproj` — if Newtonsoft.Json is not referenced, add it.

- [ ] **Step 2: Run tests — expect pass**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~SidecarStoreTests"`
Expected: All 7 tests PASS.

> **Don't commit yet** — chain continues.

---

## Task 10: `SaveFingerprint.Capture` static factory

**Files:**
- Modify: `src/FactionWars/Persistence/Models/SaveFingerprint.cs`
- Modify: `tests/FactionWars.Tests/Unit/Persistence/SaveFingerprintTests.cs`

- [ ] **Step 1: Add failing test**

Append to `SaveFingerprintTests.cs`:

```csharp
[Fact]
public void Capture_FromBridge_BuildsFingerprintFromCurrentState()
{
    var bridge = new MockGameBridge
    {
        TotalPlayTimeSeconds = 12340,
        CompletedMissionCount = 23,
        InGameClockMinutes = 854,
    };
    bridge.SetPlayerMoney(50000);

    var fp = SaveFingerprint.Capture(bridge);

    Assert.Equal(12340L, fp.TotalPlayTimeSeconds);
    Assert.Equal(50000, fp.Money);
    Assert.Equal(23, fp.CompletedMissionCount);
    Assert.Equal(854, fp.InGameClockMinutes);
}
```

You'll need `using FactionWars.ScriptHookV.Mocks;` and `using FactionWars.Core.Interfaces;` at the top.

- [ ] **Step 2: Run test — expect compile fail**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~SaveFingerprintTests.Capture"`
Expected: COMPILE FAIL — `Capture` doesn't exist on `SaveFingerprint`.

- [ ] **Step 3: Add implementation**

Add a static method to `SaveFingerprint`:

```csharp
public static SaveFingerprint Capture(FactionWars.Core.Interfaces.IGameBridge bridge)
{
    if (bridge == null) throw new System.ArgumentNullException(nameof(bridge));
    return new SaveFingerprint
    {
        TotalPlayTimeSeconds = bridge.GetTotalPlayTimeSeconds(),
        Money = bridge.GetPlayerMoney(),
        CompletedMissionCount = bridge.GetCompletedMissionCount(),
        InGameClockMinutes = bridge.GetInGameClockMinutes(),
    };
}
```

- [ ] **Step 4: Run test — expect pass**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~SaveFingerprintTests"`
Expected: All 8 tests PASS.

> **Don't commit yet.**

---

## Task 11: `GameStateManager` surgery

**Files:**
- Modify: `src/FactionWars/ScriptHookV/Persistence/GameStateManager.cs`
- Modify: `src/FactionWars/ScriptHookV/Persistence/IGameStateManager.cs`
- Modify: `tests/FactionWars.Tests/Unit/ScriptHookV/Persistence/GameStateManagerTests.cs`

This is the largest single edit in the plan. We're removing the slot-based API in favor of two new methods.

- [ ] **Step 1: Update `IGameStateManager`**

Open `src/FactionWars/ScriptHookV/Persistence/IGameStateManager.cs`. Remove these members:
- `SaveToSlot`, `SaveToSlotAsync`
- `LoadFromSlot`, `LoadFromSlotAsync`
- `CurrentSaveName` property
- Any `SaveSlotInfo`-returning methods

Add:

```csharp
/// <summary>
/// Captures the current game state into a sidecar tagged with the supplied fingerprint
/// and writes it via the SidecarStore. Failures are logged and swallowed.
/// </summary>
void WriteCurrentSidecar(SaveFingerprint fingerprint, PlayerPosition position, string nativeSaveFilename);

/// <summary>
/// Applies the given sidecar's GameState to the current world.
/// </summary>
void HydrateFromSidecar(Sidecar sidecar);
```

Add `using FactionWars.Persistence.Models;` if not already present.

- [ ] **Step 2: Update `GameStateManager` — remove slot API and money/weapons mirroring**

In `GameStateManager.cs`:

1. Replace the constructor's `ISaveSlotManager _saveSlotManager` field/parameter with `ISidecarStore _sidecarStore`. Update the namespace reference.
2. Delete: `SaveToSlot`, `SaveToSlotAsync`, `LoadFromSlot`, `LoadFromSlotAsync`, `_currentSaveName` field, `CurrentSaveName` property.
3. In `GetCurrentGameState`, delete the entire money/weapons capture block (the `if (_gameBridge != null) { ... }` block around the `gameState.PlayerMoney = ...; gameState.PlayerWeapons = ...;` assignments).
4. In `ApplyGameState`, delete the entire money/weapons restore block (the `if (_gameBridge != null) { ... }` block at the end that calls `_gameBridge.SetPlayerMoney`, `_gameBridge.RemoveAllPlayerWeapons`, `_gameBridge.GivePlayerWeapon`).
5. Add new methods:

```csharp
/// <inheritdoc />
public void WriteCurrentSidecar(SaveFingerprint fingerprint, PlayerPosition position, string nativeSaveFilename)
{
    if (!_hasGameLoaded)
    {
        FileLogger.Debug("WriteCurrentSidecar: no game loaded; skipping.");
        return;
    }

    if (fingerprint == null) throw new ArgumentNullException(nameof(fingerprint));
    if (position == null) throw new ArgumentNullException(nameof(position));
    if (nativeSaveFilename == null) throw new ArgumentNullException(nameof(nativeSaveFilename));

    var gameState = GetCurrentGameState()!;
    var sidecar = new Sidecar
    {
        Fingerprint = fingerprint,
        WrittenAtUtc = DateTime.UtcNow,
        NativeSaveFilename = nativeSaveFilename,
        PlayerPosition = position,
        GameState = gameState,
    };

    _sidecarStore.WriteSidecar(sidecar);
    OnGameSaved?.Invoke(this, new GameStateSavedEventArgs(0, gameState.SaveName, true));
}

/// <inheritdoc />
public void HydrateFromSidecar(Sidecar sidecar)
{
    if (sidecar == null) throw new ArgumentNullException(nameof(sidecar));
    if (sidecar.GameState == null) throw new ArgumentException("Sidecar.GameState required.", nameof(sidecar));

    try
    {
        ApplyGameState(sidecar.GameState);
        _totalPlayTimeSeconds = sidecar.GameState.TotalPlayTimeSeconds;
        _hasGameLoaded = true;

        OnGameLoaded?.Invoke(this, new GameStateLoadedEventArgs(0, sidecar.GameState.SaveName, true));
    }
    catch (Exception ex)
    {
        OnGameLoaded?.Invoke(this, new GameStateLoadedEventArgs(0, sidecar.GameState.SaveName, false, ex));
        throw;
    }
}
```

- [ ] **Step 3: Update existing `GameStateManager` tests**

Open `tests/.../GameStateManagerTests.cs`. The tests for `SaveToSlot`/`LoadFromSlot` no longer apply. Delete those tests entirely. Keep tests for: constructor null-arg validation, `NewGame`, `ApplyGameState`, `UpdatePlayTime`, `GetCurrentGameState` (excluding money/weapons assertions).

Replace `ISaveSlotManager` mock with `ISidecarStore` mock in the test class fixture:

```csharp
private readonly Mock<ISidecarStore> _mockSidecarStore;
// ... in ctor:
_mockSidecarStore = new Mock<ISidecarStore>();
_sut = new GameStateManager(
    _mockSidecarStore.Object,
    _mockZoneRepository.Object,
    _mockFactionRepository.Object,
    _mockAllocationRepository.Object);
```

Remove any test that asserts `gameState.PlayerMoney` or `gameState.PlayerWeapons` — those fields no longer exist.

Add new tests:

```csharp
[Fact]
public void WriteCurrentSidecar_CallsStoreWithExpectedPayload()
{
    _sut.NewGame();
    var fp = new SaveFingerprint { TotalPlayTimeSeconds = 12340, Money = 50000, CompletedMissionCount = 23, InGameClockMinutes = 854 };
    var pos = new PlayerPosition { X = 1, Y = 2, Z = 3, Heading = 90 };

    _sut.WriteCurrentSidecar(fp, pos, "SGTA00003");

    _mockSidecarStore.Verify(
        s => s.WriteSidecar(It.Is<Sidecar>(sc =>
            sc.Fingerprint.TotalPlayTimeSeconds == 12340 &&
            sc.NativeSaveFilename == "SGTA00003" &&
            sc.PlayerPosition.X == 1f)),
        Times.Once);
}

[Fact]
public void WriteCurrentSidecar_NoGameLoaded_DoesNotCallStore()
{
    var fp = new SaveFingerprint { TotalPlayTimeSeconds = 12340 };
    var pos = new PlayerPosition();

    _sut.WriteCurrentSidecar(fp, pos, "SGTA00003");

    _mockSidecarStore.Verify(s => s.WriteSidecar(It.IsAny<Sidecar>()), Times.Never);
}

[Fact]
public void HydrateFromSidecar_AppliesGameStateAndMarksLoaded()
{
    var sidecar = new Sidecar
    {
        Fingerprint = new SaveFingerprint { TotalPlayTimeSeconds = 12340 },
        GameState = new GameState { SaveName = "test", TotalPlayTimeSeconds = 12340 },
    };

    _sut.HydrateFromSidecar(sidecar);

    Assert.True(_sut.HasGameLoaded);
    Assert.Equal(12340, _sut.TotalPlayTimeSeconds);
}
```

Add `using FactionWars.Persistence;` and `using FactionWars.Persistence.Models;` to the test file as needed.

- [ ] **Step 4: Compile and run tests**

Run: `dotnet build src/FactionWars/FactionWars.csproj`
Expected: SUCCESS (chain that started in Task 4 is now resolved).

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~GameStateManagerTests"`
Expected: All adapted tests PASS.

- [ ] **Step 5: Commit the chain (Tasks 4–11)**

```bash
git add src/FactionWars/Persistence/ src/FactionWars/Core/Interfaces/IGameBridge.cs src/FactionWars/ScriptHookV/GameBridge.cs src/FactionWars/ScriptHookV/Mocks/MockGameBridge.cs src/FactionWars/ScriptHookV/Persistence/IGameStateManager.cs src/FactionWars/ScriptHookV/Persistence/GameStateManager.cs tests/FactionWars.Tests/
git commit -m "refactor: replace slot-based saves with sidecar-store API

Drop PlayerMoney/PlayerWeapons mirroring (handled natively by GTA save).
Add SaveFingerprint, Sidecar, PlayerPosition models and SidecarStore.
Add IGameBridge primitives for total play time / mission count / clock.
Replace GameStateManager.SaveToSlot/LoadFromSlot with WriteCurrentSidecar/HydrateFromSidecar."
```

---

## Task 12: `NativeSaveWatcher`

**Files:**
- Create: `src/FactionWars/ScriptHookV/Persistence/NativeSaveWatcher.cs`
- Create: `tests/FactionWars.Tests/Unit/ScriptHookV/Persistence/NativeSaveWatcherTests.cs`

- [ ] **Step 1: Write failing tests (real-filesystem integration tests)**

```csharp
// tests/FactionWars.Tests/Unit/ScriptHookV/Persistence/NativeSaveWatcherTests.cs
using FactionWars.ScriptHookV.Persistence;
using System;
using System.IO;
using System.Threading;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV.Persistence
{
    public class NativeSaveWatcherTests : IDisposable
    {
        private readonly string _tempDir;

        public NativeSaveWatcherTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "fw_watcher_test_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
        }

        public void Dispose()
        {
            try { Directory.Delete(_tempDir, recursive: true); } catch { }
        }

        private void WriteSgta(string name)
        {
            File.WriteAllText(Path.Combine(_tempDir, name), Guid.NewGuid().ToString());
        }

        [Fact]
        public void SingleSave_FiresOneEvent()
        {
            int eventCount = 0;
            using var watcher = new NativeSaveWatcher(_tempDir, debounceMs: 100);
            watcher.OnNativeSaveWritten += (_, _) => Interlocked.Increment(ref eventCount);
            watcher.Start();

            WriteSgta("SGTA00003");
            Thread.Sleep(400); // generously past debounce

            Assert.Equal(1, eventCount);
        }

        [Fact]
        public void MultipleRapidWritesSamePath_DebouncesToOneEvent()
        {
            int eventCount = 0;
            using var watcher = new NativeSaveWatcher(_tempDir, debounceMs: 100);
            watcher.OnNativeSaveWritten += (_, _) => Interlocked.Increment(ref eventCount);
            watcher.Start();

            WriteSgta("SGTA00003");
            Thread.Sleep(20);
            WriteSgta("SGTA00003");
            Thread.Sleep(20);
            WriteSgta("SGTA00003");
            Thread.Sleep(400);

            Assert.Equal(1, eventCount);
        }

        [Fact]
        public void DistinctSaves_FireDistinctEvents()
        {
            int eventCount = 0;
            using var watcher = new NativeSaveWatcher(_tempDir, debounceMs: 100);
            watcher.OnNativeSaveWritten += (_, _) => Interlocked.Increment(ref eventCount);
            watcher.Start();

            WriteSgta("SGTA00003");
            Thread.Sleep(300);
            WriteSgta("SGTA00007");
            Thread.Sleep(400);

            Assert.Equal(2, eventCount);
        }

        [Fact]
        public void NonSgtaFile_IsIgnored()
        {
            int eventCount = 0;
            using var watcher = new NativeSaveWatcher(_tempDir, debounceMs: 100);
            watcher.OnNativeSaveWritten += (_, _) => Interlocked.Increment(ref eventCount);
            watcher.Start();

            File.WriteAllText(Path.Combine(_tempDir, "ignore.txt"), "hello");
            Thread.Sleep(300);

            Assert.Equal(0, eventCount);
        }
    }
}
```

- [ ] **Step 2: Run tests — expect compile failure**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~NativeSaveWatcherTests"`
Expected: COMPILE FAIL.

- [ ] **Step 3: Implement `NativeSaveWatcher`**

```csharp
// src/FactionWars/ScriptHookV/Persistence/NativeSaveWatcher.cs
using FactionWars.ScriptHookV.Logging;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;

namespace FactionWars.ScriptHookV.Persistence
{
    /// <summary>
    /// Watches Rockstar's profile dir for SGTA file writes. Debounces FS event
    /// bursts (saves typically fire 2-3 events per file) and emits one
    /// OnNativeSaveWritten per logical save.
    /// </summary>
    public sealed class NativeSaveWatcher : IDisposable
    {
        public sealed class SaveEvent : EventArgs
        {
            public string Path { get; }
            public DateTime ModifiedAtUtc { get; }
            public SaveEvent(string path, DateTime modifiedAtUtc) { Path = path; ModifiedAtUtc = modifiedAtUtc; }
        }

        public event EventHandler<SaveEvent>? OnNativeSaveWritten;

        private readonly string _directory;
        private readonly int _debounceMs;
        private readonly FileSystemWatcher _fsw;
        private readonly ConcurrentDictionary<string, Timer> _timers = new ConcurrentDictionary<string, Timer>(StringComparer.OrdinalIgnoreCase);
        private bool _disposed;

        public NativeSaveWatcher(string directory, int debounceMs = 200)
        {
            if (string.IsNullOrEmpty(directory)) throw new ArgumentException("directory required", nameof(directory));
            _directory = directory;
            _debounceMs = debounceMs;

            _fsw = new FileSystemWatcher(_directory)
            {
                Filter = "SGTA*",
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
                IncludeSubdirectories = false,
            };

            _fsw.Changed += OnFsEvent;
            _fsw.Created += OnFsEvent;
            _fsw.Renamed += OnFsRenamed;
        }

        public void Start()
        {
            _fsw.EnableRaisingEvents = true;
            FileLogger.Info($"NativeSaveWatcher: started on {_directory}");
        }

        private void OnFsEvent(object sender, FileSystemEventArgs e) => Schedule(e.FullPath);
        private void OnFsRenamed(object sender, RenamedEventArgs e) => Schedule(e.FullPath);

        private void Schedule(string path)
        {
            if (_disposed) return;
            if (!IsSgtaFile(path)) return;

            var timer = _timers.AddOrUpdate(
                path,
                _ => new Timer(state => Fire((string)state!), path, _debounceMs, Timeout.Infinite),
                (_, existing) =>
                {
                    existing.Change(_debounceMs, Timeout.Infinite);
                    return existing;
                });
        }

        private static bool IsSgtaFile(string path)
        {
            var name = Path.GetFileName(path);
            return name != null && name.StartsWith("SGTA", StringComparison.OrdinalIgnoreCase);
        }

        private void Fire(string path)
        {
            if (_disposed) return;
            try
            {
                var info = new FileInfo(path);
                if (!info.Exists) return;

                var args = new SaveEvent(path, info.LastWriteTimeUtc);
                FileLogger.Info($"NativeSaveWatcher: detected save {Path.GetFileName(path)} mtime={info.LastWriteTimeUtc:O}");
                OnNativeSaveWritten?.Invoke(this, args);
            }
            catch (Exception ex)
            {
                FileLogger.Error("NativeSaveWatcher: failed firing save event", ex);
            }
            finally
            {
                if (_timers.TryRemove(path, out var t)) { try { t.Dispose(); } catch { } }
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            try { _fsw.EnableRaisingEvents = false; } catch { }
            try { _fsw.Dispose(); } catch { }
            foreach (var kv in _timers) { try { kv.Value.Dispose(); } catch { } }
            _timers.Clear();
        }
    }
}
```

- [ ] **Step 4: Run tests — verify pass**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~NativeSaveWatcherTests"`
Expected: All 4 tests PASS.

> If any test is flaky on slow CI/disks, raise the `Thread.Sleep` budgets and the `debounceMs` proportionally.

- [ ] **Step 5: Commit**

```bash
git add src/FactionWars/ScriptHookV/Persistence/NativeSaveWatcher.cs tests/FactionWars.Tests/Unit/ScriptHookV/Persistence/NativeSaveWatcherTests.cs
git commit -m "feat: add NativeSaveWatcher for SGTA save detection"
```

---

## Task 13: `LoadDetector`

**Files:**
- Create: `src/FactionWars/ScriptHookV/Persistence/LoadDetector.cs`
- Create: `tests/FactionWars.Tests/Unit/ScriptHookV/Persistence/LoadDetectorTests.cs`

The `LoadDetector` does not own the loading-state polling — it accepts the current `IsLoading` value each tick from `FactionWarsScript`. This decouples the detector from SHVDN APIs and makes it unit-testable.

- [ ] **Step 1: Write failing tests**

```csharp
// tests/FactionWars.Tests/Unit/ScriptHookV/Persistence/LoadDetectorTests.cs
using FactionWars.Core.Interfaces;
using FactionWars.Persistence;
using FactionWars.Persistence.Models;
using FactionWars.ScriptHookV.Mocks;
using FactionWars.ScriptHookV.Persistence;
using Moq;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV.Persistence
{
    public class LoadDetectorTests
    {
        private readonly MockGameBridge _bridge = new MockGameBridge();
        private readonly Mock<ISidecarStore> _store = new Mock<ISidecarStore>();
        private readonly LoadDetector _sut;

        // Captured side-effects.
        private Sidecar? _hydrated;
        private bool _hydrateCalled;
        private bool _newGameCalled;

        public LoadDetectorTests()
        {
            _sut = new LoadDetector(
                _bridge,
                _store.Object,
                onHydrate: s => { _hydrateCalled = true; _hydrated = s; },
                onNewGame: () => _newGameCalled = true);
        }

        [Fact]
        public void NoLoadingTransition_DoesNothing()
        {
            _sut.Tick(isLoading: false);
            _sut.Tick(isLoading: false);

            Assert.False(_hydrateCalled);
            Assert.False(_newGameCalled);
        }

        [Fact]
        public void LoadingFalseToTrue_DoesNothing()
        {
            _sut.Tick(isLoading: false);
            _sut.Tick(isLoading: true);   // entering loading screen

            Assert.False(_hydrateCalled);
            Assert.False(_newGameCalled);
        }

        [Fact]
        public void LoadingTrueToFalse_FingerprintChanged_MatchingSidecar_Hydrates()
        {
            _bridge.TotalPlayTimeSeconds = 0;
            _sut.Tick(isLoading: false);    // baseline
            _bridge.TotalPlayTimeSeconds = 12340;
            _sut.Tick(isLoading: true);     // loading begins
            // Bridge now reads as loaded save's state:
            _bridge.SetPlayerMoney(50000);
            _bridge.CompletedMissionCount = 23;
            _bridge.InGameClockMinutes = 854;

            var matchedSidecar = new Sidecar
            {
                Fingerprint = new SaveFingerprint
                {
                    TotalPlayTimeSeconds = 12340,
                    Money = 50000,
                    CompletedMissionCount = 23,
                    InGameClockMinutes = 854,
                },
            };
            _store.Setup(s => s.TryFindByFingerprint(It.IsAny<SaveFingerprint>(), out matchedSidecar)).Returns(true);

            _sut.Tick(isLoading: false);    // loading ends — fingerprint changed → look up

            Assert.True(_hydrateCalled);
            Assert.Equal(12340L, _hydrated!.Fingerprint.TotalPlayTimeSeconds);
            Assert.False(_newGameCalled);
        }

        [Fact]
        public void LoadingTrueToFalse_FingerprintChanged_NoMatch_TriggersNewGame()
        {
            _sut.Tick(isLoading: false);
            _bridge.TotalPlayTimeSeconds = 999;
            _sut.Tick(isLoading: true);

            Sidecar? notFound = null;
            _store.Setup(s => s.TryFindByFingerprint(It.IsAny<SaveFingerprint>(), out notFound!)).Returns(false);

            _sut.Tick(isLoading: false);

            Assert.False(_hydrateCalled);
            Assert.True(_newGameCalled);
        }

        [Fact]
        public void LoadingTrueToFalse_FingerprintUnchanged_DoesNothing()
        {
            _bridge.TotalPlayTimeSeconds = 5000;
            _sut.Tick(isLoading: false);    // capture baseline
            _sut.Tick(isLoading: true);     // loading begins (mission cutscene, fast travel, etc.)
            // Fingerprint same after the "load" — not a real load.
            _sut.Tick(isLoading: false);

            Assert.False(_hydrateCalled);
            Assert.False(_newGameCalled);
        }
    }
}
```

- [ ] **Step 2: Run tests — expect compile fail**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~LoadDetectorTests"`
Expected: COMPILE FAIL.

- [ ] **Step 3: Implement `LoadDetector`**

```csharp
// src/FactionWars/ScriptHookV/Persistence/LoadDetector.cs
using FactionWars.Core.Interfaces;
using FactionWars.Persistence;
using FactionWars.Persistence.Models;
using FactionWars.ScriptHookV.Logging;
using System;

namespace FactionWars.ScriptHookV.Persistence
{
    /// <summary>
    /// Observes loading-screen edges and reacts when a new save has been loaded.
    /// Pure logic — no SHVDN dependencies; the host script feeds it the IsLoading
    /// flag each tick and supplies callbacks for hydrate / newGame.
    /// </summary>
    public sealed class LoadDetector
    {
        private readonly IGameBridge _bridge;
        private readonly ISidecarStore _store;
        private readonly Action<Sidecar> _onHydrate;
        private readonly Action _onNewGame;

        private bool _wasLoading;
        private long _lastKnownTotalPlayTimeSeconds = -1;

        public LoadDetector(IGameBridge bridge, ISidecarStore store, Action<Sidecar> onHydrate, Action onNewGame)
        {
            _bridge = bridge ?? throw new ArgumentNullException(nameof(bridge));
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _onHydrate = onHydrate ?? throw new ArgumentNullException(nameof(onHydrate));
            _onNewGame = onNewGame ?? throw new ArgumentNullException(nameof(onNewGame));
        }

        /// <summary>
        /// Called once per script tick. Detects the IsLoading edge and reacts.
        /// </summary>
        public void Tick(bool isLoading)
        {
            // Capture baseline whenever we're not loading.
            if (!isLoading && !_wasLoading)
            {
                _lastKnownTotalPlayTimeSeconds = _bridge.GetTotalPlayTimeSeconds();
                return;
            }

            // Loading-edge: true → false means a load just completed.
            if (_wasLoading && !isLoading)
            {
                var currentPlayTime = _bridge.GetTotalPlayTimeSeconds();

                // False-positive guard: if play time didn't change, this wasn't a save load.
                if (currentPlayTime == _lastKnownTotalPlayTimeSeconds)
                {
                    FileLogger.Debug("LoadDetector: loading-end transition with unchanged play time — skipping (likely cutscene/fast-travel).");
                    _wasLoading = isLoading;
                    return;
                }

                var fingerprint = SaveFingerprint.Capture(_bridge);
                if (_store.TryFindByFingerprint(fingerprint, out var sidecar))
                {
                    FileLogger.Info($"LoadDetector: matched sidecar for play time {fingerprint.TotalPlayTimeSeconds}s — hydrating.");
                    _onHydrate(sidecar);
                }
                else
                {
                    FileLogger.Info($"LoadDetector: no sidecar matched play time {fingerprint.TotalPlayTimeSeconds}s — starting new game.");
                    _onNewGame();
                }

                _lastKnownTotalPlayTimeSeconds = currentPlayTime;
            }

            _wasLoading = isLoading;
        }
    }
}
```

- [ ] **Step 4: Run tests — verify pass**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~LoadDetectorTests"`
Expected: All 5 tests PASS.

- [ ] **Step 5: Commit**

```bash
git add src/FactionWars/ScriptHookV/Persistence/LoadDetector.cs tests/FactionWars.Tests/Unit/ScriptHookV/Persistence/LoadDetectorTests.cs
git commit -m "feat: add LoadDetector for save-load identification via fingerprint"
```

---

## Task 14: `LegacyBackupTask`

**Files:**
- Create: `src/FactionWars/ScriptHookV/Persistence/LegacyBackupTask.cs`
- Create: `tests/FactionWars.Tests/Unit/ScriptHookV/Persistence/LegacyBackupTaskTests.cs`

- [ ] **Step 1: Write failing tests**

```csharp
// tests/FactionWars.Tests/Unit/ScriptHookV/Persistence/LegacyBackupTaskTests.cs
using FactionWars.ScriptHookV.Persistence;
using System;
using System.IO;
using System.Linq;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV.Persistence
{
    public class LegacyBackupTaskTests : IDisposable
    {
        private readonly string _tempDir;

        public LegacyBackupTaskTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "fw_legacy_test_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
        }

        public void Dispose()
        {
            try { Directory.Delete(_tempDir, recursive: true); } catch { }
        }

        [Fact]
        public void NoLegacyFiles_NoOp()
        {
            var sut = new LegacyBackupTask(_tempDir);
            sut.Run();

            Assert.False(Directory.GetDirectories(_tempDir, "legacy_backup_*").Any());
        }

        [Fact]
        public void LegacyFilesPresent_MovedToBackupSubfolder()
        {
            File.WriteAllText(Path.Combine(_tempDir, "save_slot_0.json"), "{}");
            File.WriteAllText(Path.Combine(_tempDir, "save_slot_5.json"), "{}");
            File.WriteAllText(Path.Combine(_tempDir, "unrelated.txt"), "keep me");

            var sut = new LegacyBackupTask(_tempDir);
            sut.Run();

            // Originals gone:
            Assert.False(File.Exists(Path.Combine(_tempDir, "save_slot_0.json")));
            Assert.False(File.Exists(Path.Combine(_tempDir, "save_slot_5.json")));
            // Unrelated kept:
            Assert.True(File.Exists(Path.Combine(_tempDir, "unrelated.txt")));

            // Backup folder created and contains both:
            var backups = Directory.GetDirectories(_tempDir, "legacy_backup_*");
            Assert.Single(backups);
            Assert.True(File.Exists(Path.Combine(backups[0], "save_slot_0.json")));
            Assert.True(File.Exists(Path.Combine(backups[0], "save_slot_5.json")));
        }

        [Fact]
        public void Run_CreatesSidecarsSubdirIfMissing()
        {
            var sut = new LegacyBackupTask(_tempDir);
            sut.Run();

            Assert.True(Directory.Exists(Path.Combine(_tempDir, "sidecars")));
        }

        [Fact]
        public void Run_IsIdempotent_WhenNoLegacyFiles()
        {
            var sut = new LegacyBackupTask(_tempDir);
            sut.Run();
            sut.Run(); // second invocation
            sut.Run();

            // Should not create extra empty backup folders
            Assert.False(Directory.GetDirectories(_tempDir, "legacy_backup_*").Any());
        }
    }
}
```

- [ ] **Step 2: Run tests — expect compile fail**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~LegacyBackupTaskTests"`
Expected: COMPILE FAIL.

- [ ] **Step 3: Implement**

```csharp
// src/FactionWars/ScriptHookV/Persistence/LegacyBackupTask.cs
using FactionWars.ScriptHookV.Logging;
using System;
using System.IO;
using System.Linq;

namespace FactionWars.ScriptHookV.Persistence
{
    /// <summary>
    /// One-shot first-launch task: relocates legacy save_slot_*.json files to a
    /// dated backup subfolder and ensures the sidecars/ subdirectory exists.
    /// Safe to run repeatedly — no-ops if no legacy files are present.
    /// </summary>
    public sealed class LegacyBackupTask
    {
        private const string LegacyPattern = "save_slot_*.json";
        private const string SidecarsSubdir = "sidecars";
        private const string BackupPrefix = "legacy_backup_";

        private readonly string _saveDirectory;

        public LegacyBackupTask(string saveDirectory)
        {
            if (string.IsNullOrEmpty(saveDirectory)) throw new ArgumentException("required", nameof(saveDirectory));
            _saveDirectory = saveDirectory;
        }

        public void Run()
        {
            Directory.CreateDirectory(_saveDirectory);
            Directory.CreateDirectory(Path.Combine(_saveDirectory, SidecarsSubdir));

            var legacyFiles = Directory.EnumerateFiles(_saveDirectory, LegacyPattern, SearchOption.TopDirectoryOnly).ToList();
            if (legacyFiles.Count == 0)
            {
                FileLogger.Debug("LegacyBackupTask: no legacy save_slot_*.json files; no-op.");
                return;
            }

            var stamp = DateTime.UtcNow.ToString("yyyyMMddTHHmmssZ");
            var backupDir = Path.Combine(_saveDirectory, BackupPrefix + stamp);
            int suffix = 0;
            while (Directory.Exists(backupDir))
            {
                suffix++;
                backupDir = Path.Combine(_saveDirectory, BackupPrefix + stamp + "_" + suffix);
            }
            Directory.CreateDirectory(backupDir);

            foreach (var file in legacyFiles)
            {
                var dest = Path.Combine(backupDir, Path.GetFileName(file));
                File.Move(file, dest);
            }

            FileLogger.Info($"LegacyBackupTask: moved {legacyFiles.Count} legacy save(s) to {backupDir}");
        }
    }
}
```

- [ ] **Step 4: Run tests — verify pass**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~LegacyBackupTaskTests"`
Expected: All 4 tests PASS.

- [ ] **Step 5: Commit**

```bash
git add src/FactionWars/ScriptHookV/Persistence/LegacyBackupTask.cs tests/FactionWars.Tests/Unit/ScriptHookV/Persistence/LegacyBackupTaskTests.cs
git commit -m "feat: add LegacyBackupTask for first-launch save migration"
```

---

## Task 15: Remove obsolete components

**Files to delete:**
- `src/FactionWars/Persistence/SaveSlotManager.cs`
- `src/FactionWars/Persistence/Models/SaveSlotInfo.cs`
- `src/FactionWars/Core/Interfaces/ISaveSlotManager.cs`
- `src/FactionWars/Core/Services/StubSaveSlotManager.cs`
- `src/FactionWars/Persistence/AutoSaveService.cs`
- `src/FactionWars/Core/Interfaces/IAutoSaveService.cs`
- Their corresponding test files (use `Glob` to find under `tests/FactionWars.Tests/`)

- [ ] **Step 1: Delete the obsolete files**

```bash
git rm src/FactionWars/Persistence/SaveSlotManager.cs
git rm src/FactionWars/Persistence/Models/SaveSlotInfo.cs
git rm src/FactionWars/Core/Interfaces/ISaveSlotManager.cs
git rm src/FactionWars/Core/Services/StubSaveSlotManager.cs
git rm src/FactionWars/Persistence/AutoSaveService.cs
git rm src/FactionWars/Core/Interfaces/IAutoSaveService.cs
```

For tests, find and remove anything that references the deleted types:

```bash
# Find test files that reference deleted types:
git grep -l "SaveSlotManager\|ISaveSlotManager\|StubSaveSlotManager\|AutoSaveService\|IAutoSaveService" tests/
```

For each file in that list, either delete the file (if it's the dedicated test class) or remove the affected tests.

- [ ] **Step 2: Compile — observe expected breakage**

Run: `dotnet build src/FactionWars/FactionWars.csproj`
Expected: FAIL — `ServiceContainerFactory` and `MainMenuController` / `SettingsMenuController` / `FactionWarsScript` still reference these types. Tasks 16–18 fix that.

> **Don't commit yet.**

---

## Task 16: Mod-menu surgery — remove save/load UI

**Files:**
- Modify: `src/FactionWars/ScriptHookV/UI/MainMenuController.cs`
- Modify: `src/FactionWars/ScriptHookV/UI/SettingsMenuController.cs`

The exact menu structure depends on what's there. Approach:

- [ ] **Step 1: Find save/load UI references**

```bash
git grep -n "SaveToSlot\|LoadFromSlot\|SaveSlotInfo\|SaveSlotManager\|ISaveSlotManager" src/FactionWars/ScriptHookV/UI/
```

- [ ] **Step 2: For each match in `MainMenuController.cs` and `SettingsMenuController.cs`:**

   - If it's a button/menu item → delete the button construction + its click handler.
   - If it's a property/field → delete it.
   - If it's a constructor parameter (`ISaveSlotManager`) → remove the parameter and any field it backs. Update DI registrations in Task 18.

   **Do not** add a placeholder "feature removed" message; just delete the UI cleanly.

- [ ] **Step 3: Compile — observe expected status**

Run: `dotnet build src/FactionWars/FactionWars.csproj`
Expected: Either SUCCESS (if all references gone) or FAIL on `ServiceContainerFactory` / `FactionWarsScript`. Either way, don't commit yet — proceed to next task.

---

## Task 17: `ServiceContainerFactory` — re-wire DI

**Files:**
- Modify: `src/FactionWars/ScriptHookV/ServiceContainerFactory.cs`

- [ ] **Step 1: Find all references to the removed services**

```bash
git grep -n "ISaveSlotManager\|SaveSlotManager\|IAutoSaveService\|AutoSaveService\|StubSaveSlotManager" src/FactionWars/ScriptHookV/ServiceContainerFactory.cs
```

- [ ] **Step 2: Remove their registrations**

Delete the `RegisterSingleton<ISaveSlotManager>` block, the `RegisterSingleton<IAutoSaveService>` block, and any usages of `StubSaveSlotManager`.

- [ ] **Step 3: Add new registrations**

Locate the persistence-related DI section (search for `IPersistenceService` or `IGameStateManager`). Add:

```csharp
// Sidecar store — replaces the old slot-based SaveSlotManager.
container.RegisterSingleton<ISidecarStore>(() =>
{
    var saveDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "FactionWars");
    return new SidecarStore(Path.Combine(saveDir, "sidecars"));
});

// Legacy backup — runs once at startup.
container.RegisterSingleton<LegacyBackupTask>(() =>
{
    var saveDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "FactionWars");
    return new LegacyBackupTask(saveDir);
});

// Native save watcher — points at Rockstar's profile directory.
container.RegisterSingleton<NativeSaveWatcher>(() =>
{
    var profileDir = ResolveActiveRockstarProfileDir();
    return new NativeSaveWatcher(profileDir);
});
```

Add a helper at the bottom of the class:

```csharp
private static string ResolveActiveRockstarProfileDir()
{
    var profilesRoot = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "Rockstar Games", "GTA V", "Profiles");

    if (!Directory.Exists(profilesRoot))
    {
        // Defer: no GTA saves yet. Return root anyway; NativeSaveWatcher should
        // handle a missing dir gracefully (see watcher's Start() guard) — if it
        // doesn't, that's a follow-up.
        FactionWars.ScriptHookV.Logging.FileLogger.Warn($"Rockstar profile root not found: {profilesRoot}");
        return profilesRoot;
    }

    var best = Directory.EnumerateDirectories(profilesRoot)
        .Select(dir => new
        {
            Dir = dir,
            MostRecent = Directory.EnumerateFiles(dir, "SGTA*")
                .Select(f => File.GetLastWriteTimeUtc(f))
                .DefaultIfEmpty(DateTime.MinValue)
                .Max(),
        })
        .OrderByDescending(x => x.MostRecent)
        .FirstOrDefault();

    var chosen = best?.Dir ?? profilesRoot;
    FactionWars.ScriptHookV.Logging.FileLogger.Info($"Resolved Rockstar profile dir: {chosen}");
    return chosen;
}
```

Add `using System.IO;`, `using System.Linq;`, `using FactionWars.Persistence;`, `using FactionWars.ScriptHookV.Persistence;` if not already present.

- [ ] **Step 4: Update `GameStateManager` registration**

Find the `RegisterSingleton<IGameStateManager>` registration. Replace its dependency on `ISaveSlotManager` with `ISidecarStore`:

```csharp
container.RegisterSingleton<IGameStateManager>(() =>
    new GameStateManager(
        container.Resolve<ISidecarStore>(),
        container.Resolve<IZoneRepository>(),
        container.Resolve<IFactionRepository>(),
        container.Resolve<IZoneDefenderAllocationRepository>(),
        container.Resolve<IGameBridge>()));
```

The `GameStateManager` constructor signature should already match — Task 11 changed its first parameter type.

- [ ] **Step 5: Compile**

Run: `dotnet build src/FactionWars/FactionWars.csproj`
Expected: SUCCESS — only `FactionWarsScript` wiring remains in Task 18.

> **Don't commit yet.**

---

## Task 18: `FactionWarsScript` — wire watcher and load detector

**Files:**
- Modify: `src/FactionWars/ScriptHookV/FactionWarsScript.cs`

`FactionWarsScript` is the SHVDN script entry point — it has a `Tick` event we hook for per-frame work.

- [ ] **Step 1: In the script's setup/OnStart, run `LegacyBackupTask` and start the watcher**

```csharp
// In the script's setup method (likely OnInit / OnStart / wherever DI is resolved):
var legacy = _container.Resolve<LegacyBackupTask>();
legacy.Run();

var watcher = _container.Resolve<NativeSaveWatcher>();
watcher.OnNativeSaveWritten += HandleNativeSaveWritten;
watcher.Start();

_loadDetector = new LoadDetector(
    _container.Resolve<IGameBridge>(),
    _container.Resolve<ISidecarStore>(),
    onHydrate: sidecar => _container.Resolve<IGameStateManager>().HydrateFromSidecar(sidecar),
    onNewGame: () => _container.Resolve<IGameStateManager>().NewGame());
```

Add `private LoadDetector _loadDetector = null!;` field.

- [ ] **Step 2: In the script's per-tick handler, call the load detector**

```csharp
private void OnTick(object sender, EventArgs e)
{
    // ... existing tick work ...

    // Loading-state input for LoadDetector. Use SHVDN's Game.IsLoading if exposed;
    // otherwise the documented fallback (multi-frame tick gap + position teleport)
    // can be implemented here. For v1, attempt Game.IsLoading first:
    bool isLoading;
    try { isLoading = GTA.Game.IsLoading; }
    catch { isLoading = false; }
    _loadDetector.Tick(isLoading);
}
```

> If `GTA.Game.IsLoading` is not available in this SHVDN version, the implementation should fall back to detecting via consecutive-tick gaps (>1.5s) combined with a player-position teleport (>500m delta). Document the choice in code comments and verify against the manual checklist (Task 19).

- [ ] **Step 3: Implement `HandleNativeSaveWritten`**

```csharp
private void HandleNativeSaveWritten(object? sender, NativeSaveWatcher.SaveEvent e)
{
    try
    {
        var bridge = _container.Resolve<IGameBridge>();
        var manager = _container.Resolve<IGameStateManager>();

        var fingerprint = SaveFingerprint.Capture(bridge);
        var pos = bridge.GetPlayerPosition();
        var heading = bridge.GetPlayerHeading();
        var position = new PlayerPosition { X = pos.X, Y = pos.Y, Z = pos.Z, Heading = heading };
        var nativeFilename = System.IO.Path.GetFileName(e.Path);

        manager.WriteCurrentSidecar(fingerprint, position, nativeFilename);
    }
    catch (Exception ex)
    {
        FileLogger.Error("HandleNativeSaveWritten: failed", ex);
    }
}
```

Add necessary `using` statements: `FactionWars.Persistence`, `FactionWars.Persistence.Models`, `FactionWars.ScriptHookV.Persistence`.

- [ ] **Step 4: Compile and run full test suite**

```bash
dotnet build src/FactionWars/FactionWars.csproj
dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj
```

Expected: SUCCESS for build and all tests.

- [ ] **Step 5: Commit Tasks 15–18**

```bash
git add -A
git commit -m "refactor: remove SaveSlotManager/AutoSaveService and wire sidecar pipeline

Delete obsolete slot-based save UI and AutoSaveService. Register
SidecarStore, NativeSaveWatcher, LegacyBackupTask in DI. Subscribe
FactionWarsScript to watcher events and tick LoadDetector each frame."
```

---

## Task 19: Deploy and manually verify in-game

**Files:** None — this is a runbook.

The unit tests don't cover the actual GTA V native interactions. Each step in this checklist must be performed in-game with a real GTA V install, and log lines confirmed via `C:\Users\ryan7\Documents\FactionWars\Logs\`.

- [ ] **Step 1: Deploy the build**

```bash
cp "C:/Users/ryan7/programming/gtav-factions/src/FactionWars/bin/Debug/net48/FactionWars.dll" "E:/SteamLibrary/steamapps/common/Grand Theft Auto V/scripts/"
```

- [ ] **Step 2: Verify legacy backup runs once**

If `Documents\FactionWars\save_slot_*.json` files exist, launch GTA. After the mod loads, confirm:

- A `Documents\FactionWars\legacy_backup_<timestamp>\` directory exists
- The legacy `save_slot_*.json` files moved into it
- Latest log shows `LegacyBackupTask: moved N legacy save(s)`

- [ ] **Step 3: Verify save → sidecar mirror**

In-game: open pause menu → Save Game → pick slot 5. Wait ~2 seconds.

Confirm:
- `Documents\FactionWars\sidecars\sidecar_<N>.json` was created where `<N>` matches your total play time in seconds.
- Latest log contains `NativeSaveWatcher: detected save SGTA00005` and `SidecarStore: wrote sidecar_<N>.json`.

If `<N>` is `0` despite played time, the stat name in `GameBridge.GetTotalPlayTimeSeconds` is wrong. Per CLAUDE.md "Updating Mocks from In-Game Behavior", fix the constant in `GameBridge.cs`, redeploy, retest, and reflect the correction back into any tests that hardcoded a stat name.

- [ ] **Step 4: Verify load → hydration**

Save in slot 5. Exit GTA. Relaunch. Load slot 5.

Confirm:
- Mod state visible in-game matches what was saved (faction zones, allocations, etc.)
- Latest log contains `LoadDetector: matched sidecar for play time <N>s — hydrating`.

- [ ] **Step 5: Verify load-older-save case**

Save in slot 1. Play and earn money / progress. Save in slot 2. Now load slot 1.

Confirm:
- Mod state matches slot 1's saved state, NOT slot 2's. (This was the failure case for the rejected mtime-heuristic approach.)
- Latest log shows the matched sidecar's TotalPlayTimeSeconds equals slot 1's, not slot 2's.

- [ ] **Step 6: Verify autosave triggers sidecar**

In-game: complete any mission to trigger an autosave. Wait ~2 seconds.

Confirm a new sidecar appears, and log shows watcher event for `SGTA00000` (or whatever the autosave file is).

- [ ] **Step 7: Verify new-game fallback**

Use Rockstar Launcher to start a new GTA campaign. After prologue's first loading screen completes, confirm:

- Latest log contains `LoadDetector: no sidecar matched ... starting new game`.
- Mod state initialized as a fresh game (no zones claimed, factions reset).

- [ ] **Step 8: Verify cutscene false-positive guard**

Trigger a mission that has a cutscene with a loading screen mid-mission (e.g., Heist setup transitions).

Confirm log contains `LoadDetector: loading-end transition with unchanged play time — skipping`. Mod state should NOT have been hydrated mid-mission.

- [ ] **Step 9: Verify orphan tolerance**

Manually delete a GTA save via the pause menu. Confirm the corresponding sidecar in `Documents\FactionWars\sidecars\` is **still present** (no auto-delete, per design).

- [ ] **Step 10: Update mocks if any divergence found**

If any in-game log shows behavior different from what `MockGameBridge` does, update the mock and the affected unit tests per CLAUDE.md guidance. Re-run the full test suite.

```bash
dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj
```

- [ ] **Step 11: Commit any mock/spec updates**

```bash
git add -A
git commit -m "test: align mocks and tests with in-game behavior from manual verification"
```

---

## Self-review

Run through this checklist after completing the plan to make sure it's coherent.

**Spec coverage:**
- [x] Goal & non-goals → Task scope
- [x] Decisions table — every row has a corresponding implementation step:
  - GTA pause menu drives save/load → Tasks 16, 18
  - Fresh state on no-match → Task 13 (LoadDetector → NewGame)
  - Autosaves mirrored → Task 12 (watcher catches all SGTA writes)
  - Legacy backup → Task 14
  - Sidecar location → Task 17 (DI wires `Documents\FactionWars\sidecars\`)
  - Drop money/weapons → Tasks 4, 11
  - Fingerprint matching → Tasks 1, 5, 6, 7, 8, 9, 10, 13
  - No auto-delete orphans → SidecarStore has no Delete API, watcher swallows deletions (Task 12 implementation)
- [x] Architecture diagram → Components in Tasks 8–14
- [x] Data flow scenarios → covered by Task 19 manual verification

**Placeholder scan:** Searched for "TBD", "TODO", "implement later", "appropriate", "fill in" — none present. Hedges around `Game.IsLoading` and `SP0_TOTAL_PLAYING_TIME` are explicitly flagged with fallback behavior and the runbook step that validates them.

**Type consistency:**
- `SaveFingerprint` properties used in Tasks 1, 7, 8, 9, 10, 11, 13 — all reference the same field names (`TotalPlayTimeSeconds`, `Money`, `CompletedMissionCount`, `InGameClockMinutes`).
- `Sidecar` properties used in Tasks 3, 9, 11, 13 — consistent.
- `IGameBridge` new methods (`GetTotalPlayTimeSeconds`, `GetCompletedMissionCount`, `GetInGameClockMinutes`) declared in Task 5 and implemented in Tasks 6 (mock) and 7 (real).
- `LoadDetector` constructor signature in Task 13 matches the wiring in Task 18.
- `GameStateManager.WriteCurrentSidecar(SaveFingerprint, PlayerPosition, string)` declared in Task 11 and called in Task 18 with matching arguments.

No inconsistencies found.
