# Faction Telemetry Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a per-save CSV telemetry pipeline that records 9 categories of match data (snapshots, zone events, battles, decisions, recruitments, allocations, resource ticks, match meta, player events) by subscribing to existing C# domain events.

**Architecture:** Single `TelemetryService` orchestrator subscribes to existing domain events plus three new ones (`ZoneOwnershipChanged`, `OnTroopsRecruited`, `AttackerKilled`), transforms them into immutable DTOs, and routes them to a swappable `ITelemetrySink`. The default `CsvTelemetrySink` writes one CSV file per category into `Documents\FactionWars\Telemetry\<saveFilename>\`, buffering events in memory until a save filename is known (via `LoadDetector` or `NativeSaveWatcher`).

**Tech Stack:** .NET Framework 4.8, C# 11, xUnit, Moq, Newtonsoft.Json. ScriptHookVDotNet3 mod target. Existing codebase uses class-based DTOs (no `record` types found).

**Spec:** `docs/superpowers/specs/2026-05-03-faction-telemetry-design.md`

**Build & test commands:**
- Build: `dotnet build FactionWars.sln`
- Test all: `dotnet test FactionWars.sln`
- Test single: `dotnet test FactionWars.sln --filter "FullyQualifiedName~<TestClassName>"`

---

## File Structure

### New files

```
src/FactionWars/Telemetry/
├── Models/
│   ├── TelemetryEnums.cs              (ZoneEventType, BattleEventType, BattleOutcome,
│   │                                    AIDecisionTypeMeta, MatchMetaEventType,
│   │                                    PlayerEventType, AllocationSource)
│   ├── FactionSnapshot.cs             (snapshot DTO)
│   ├── ZoneEventRow.cs                (zone event DTO)
│   ├── BattleEventRow.cs              (battle event DTO)
│   ├── DecisionEventRow.cs            (AI decision DTO)
│   ├── RecruitmentEventRow.cs         (recruitment DTO)
│   ├── AllocationEventRow.cs          (allocation DTO)
│   ├── ResourceTickEventRow.cs        (resource tick DTO)
│   ├── MatchMetaEventRow.cs           (match meta DTO)
│   └── PlayerEventRow.cs              (player event DTO)
├── Interfaces/
│   └── ITelemetrySink.cs              (10 Write methods + SetSaveFile + Dispose)
├── Sinks/
│   ├── CsvFieldEscaper.cs             (pure CSV escaping helper)
│   ├── CsvTelemetrySink.cs            (file-backed sink with buffer+flush)
│   └── NullTelemetrySink.cs           (no-op sink for tests/opt-out)
└── Services/
    ├── FactionSnapshotBuilder.cs      (pure: builds snapshot rows from services)
    ├── PlayerKillResolver.cs          (pure: AttackerKilled → PlayerEvent or null)
    └── TelemetryService.cs            (orchestrator: timer + subscriptions)

src/FactionWars/Territory/Events/
└── ZoneOwnershipChangedEventArgs.cs   (new event args)

src/FactionWars/AI/Events/
└── TroopsRecruitedEventArgs.cs        (new event args)

src/FactionWars/Combat/Events/
└── AttackerKilledEventArgs.cs         (new event args)

tests/FactionWars.Tests/Unit/Telemetry/
├── CsvFieldEscaperTests.cs
├── CsvTelemetrySinkTests.cs
├── FactionSnapshotBuilderTests.cs
├── PlayerKillResolverTests.cs
└── TelemetryServiceTests.cs

tests/FactionWars.Tests/Integration/Telemetry/
└── TelemetryEndToEndTests.cs
```

### Modified files

- `src/FactionWars/Territory/Interfaces/IZoneService.cs` (add event)
- `src/FactionWars/Territory/Services/ZoneService.cs` (raise event)
- `src/FactionWars/AI/Controllers/AIController.cs` (add+raise OnTroopsRecruited)
- `src/FactionWars/ScriptHookV/Managers/BattleAttackerManager.cs` (add+raise AttackerKilled)
- `src/FactionWars/ScriptHookV/ServiceContainerFactory.cs` (register telemetry services)
- `src/FactionWars/ScriptHookV/FactionWarsScript.cs` (instantiate + dispose TelemetryService)

---

## Task 1: Telemetry enums and DTOs

**Files:**
- Create: `src/FactionWars/Telemetry/Models/TelemetryEnums.cs`
- Create: `src/FactionWars/Telemetry/Models/FactionSnapshot.cs`
- Create: `src/FactionWars/Telemetry/Models/ZoneEventRow.cs`
- Create: `src/FactionWars/Telemetry/Models/BattleEventRow.cs`
- Create: `src/FactionWars/Telemetry/Models/DecisionEventRow.cs`
- Create: `src/FactionWars/Telemetry/Models/RecruitmentEventRow.cs`
- Create: `src/FactionWars/Telemetry/Models/AllocationEventRow.cs`
- Create: `src/FactionWars/Telemetry/Models/ResourceTickEventRow.cs`
- Create: `src/FactionWars/Telemetry/Models/MatchMetaEventRow.cs`
- Create: `src/FactionWars/Telemetry/Models/PlayerEventRow.cs`

These are immutable POCOs with public getters; no logic, no test needed.

- [ ] **Step 1: Create TelemetryEnums.cs**

```csharp
namespace FactionWars.Telemetry.Models
{
    public enum ZoneEventType { Captured, Lost, Neutralized }
    public enum BattleEventType { Started, Ended }
    public enum BattleOutcome { AttackerWon, DefenderWon, AttackerRetreated, Stalemate }
    public enum AIDecisionTypeMeta { Attack, Defend, Reinforce, Idle, Other }
    public enum MatchMetaEventType
    {
        MatchStart, Victory, Defeat, DifficultyChanged,
        ModSessionStart, ModSessionEnd
    }
    public enum PlayerEventType
    {
        Kill, Death, FollowerRecruited, FollowerDied,
        ZoneEntered, ZoneExited, RespawnAtHospital
    }
    public enum AllocationSource { Player, AI, Initial }
}
```

- [ ] **Step 2: Create FactionSnapshot.cs**

```csharp
using System;

namespace FactionWars.Telemetry.Models
{
    public sealed class FactionSnapshot
    {
        public DateTime Timestamp { get; }
        public long PlayTimeSeconds { get; }
        public string FactionId { get; }
        public int Cash { get; }
        public int TotalTroops { get; }
        public int ZonesOwned { get; }
        public int Basic { get; }
        public int Medium { get; }
        public int Heavy { get; }
        public int Elite { get; }
        public int ReserveTroops { get; }
        public int DeployedTroops { get; }

        public FactionSnapshot(DateTime timestamp, long playTimeSeconds, string factionId,
            int cash, int totalTroops, int zonesOwned,
            int basic, int medium, int heavy, int elite,
            int reserveTroops, int deployedTroops)
        {
            Timestamp = timestamp;
            PlayTimeSeconds = playTimeSeconds;
            FactionId = factionId ?? throw new ArgumentNullException(nameof(factionId));
            Cash = cash;
            TotalTroops = totalTroops;
            ZonesOwned = zonesOwned;
            Basic = basic;
            Medium = medium;
            Heavy = heavy;
            Elite = elite;
            ReserveTroops = reserveTroops;
            DeployedTroops = deployedTroops;
        }
    }
}
```

- [ ] **Step 3: Create ZoneEventRow.cs**

```csharp
using System;

namespace FactionWars.Telemetry.Models
{
    public sealed class ZoneEventRow
    {
        public DateTime Timestamp { get; }
        public long PlayTimeSeconds { get; }
        public ZoneEventType Type { get; }
        public string ZoneId { get; }
        public string? PreviousOwner { get; }
        public string? NewOwner { get; }

        public ZoneEventRow(DateTime timestamp, long playTimeSeconds,
            ZoneEventType type, string zoneId, string? previousOwner, string? newOwner)
        {
            Timestamp = timestamp;
            PlayTimeSeconds = playTimeSeconds;
            Type = type;
            ZoneId = zoneId ?? throw new ArgumentNullException(nameof(zoneId));
            PreviousOwner = previousOwner;
            NewOwner = newOwner;
        }
    }
}
```

- [ ] **Step 4: Create BattleEventRow.cs**

```csharp
using System;

namespace FactionWars.Telemetry.Models
{
    public sealed class BattleEventRow
    {
        public DateTime Timestamp { get; }
        public long PlayTimeSeconds { get; }
        public BattleEventType Type { get; }
        public string ZoneId { get; }
        public string AttackerFactionId { get; }
        public string DefenderFactionId { get; }
        public int AttackerTroops { get; }
        public int DefenderTroops { get; }
        public BattleOutcome? Outcome { get; }
        public int AttackerCasualties { get; }
        public int DefenderCasualties { get; }

        public BattleEventRow(DateTime timestamp, long playTimeSeconds,
            BattleEventType type, string zoneId,
            string attackerFactionId, string defenderFactionId,
            int attackerTroops, int defenderTroops,
            BattleOutcome? outcome, int attackerCasualties, int defenderCasualties)
        {
            Timestamp = timestamp;
            PlayTimeSeconds = playTimeSeconds;
            Type = type;
            ZoneId = zoneId ?? throw new ArgumentNullException(nameof(zoneId));
            AttackerFactionId = attackerFactionId ?? throw new ArgumentNullException(nameof(attackerFactionId));
            DefenderFactionId = defenderFactionId ?? throw new ArgumentNullException(nameof(defenderFactionId));
            AttackerTroops = attackerTroops;
            DefenderTroops = defenderTroops;
            Outcome = outcome;
            AttackerCasualties = attackerCasualties;
            DefenderCasualties = defenderCasualties;
        }
    }
}
```

- [ ] **Step 5: Create DecisionEventRow.cs**

```csharp
using System;

namespace FactionWars.Telemetry.Models
{
    public sealed class DecisionEventRow
    {
        public DateTime Timestamp { get; }
        public long PlayTimeSeconds { get; }
        public string FactionId { get; }
        public AIDecisionTypeMeta Type { get; }
        public string? TargetZoneId { get; }
        public int Troops { get; }
        public double Priority { get; }
        public bool Executed { get; }

        public DecisionEventRow(DateTime timestamp, long playTimeSeconds, string factionId,
            AIDecisionTypeMeta type, string? targetZoneId, int troops, double priority, bool executed)
        {
            Timestamp = timestamp;
            PlayTimeSeconds = playTimeSeconds;
            FactionId = factionId ?? throw new ArgumentNullException(nameof(factionId));
            Type = type;
            TargetZoneId = targetZoneId;
            Troops = troops;
            Priority = priority;
            Executed = executed;
        }
    }
}
```

- [ ] **Step 6: Create RecruitmentEventRow.cs**

```csharp
using System;

namespace FactionWars.Telemetry.Models
{
    public sealed class RecruitmentEventRow
    {
        public DateTime Timestamp { get; }
        public long PlayTimeSeconds { get; }
        public string FactionId { get; }
        public int TroopsRecruited { get; }
        public int Cost { get; }
        public int CashBefore { get; }
        public int CashAfter { get; }

        public RecruitmentEventRow(DateTime timestamp, long playTimeSeconds, string factionId,
            int troopsRecruited, int cost, int cashBefore, int cashAfter)
        {
            Timestamp = timestamp;
            PlayTimeSeconds = playTimeSeconds;
            FactionId = factionId ?? throw new ArgumentNullException(nameof(factionId));
            TroopsRecruited = troopsRecruited;
            Cost = cost;
            CashBefore = cashBefore;
            CashAfter = cashAfter;
        }
    }
}
```

- [ ] **Step 7: Create AllocationEventRow.cs**

```csharp
using System;
using FactionWars.Core.Models;

namespace FactionWars.Telemetry.Models
{
    public sealed class AllocationEventRow
    {
        public DateTime Timestamp { get; }
        public long PlayTimeSeconds { get; }
        public string FactionId { get; }
        public string ZoneId { get; }
        public DefenderTier Tier { get; }
        public int Count { get; }
        public AllocationSource Source { get; }

        public AllocationEventRow(DateTime timestamp, long playTimeSeconds,
            string factionId, string zoneId, DefenderTier tier, int count, AllocationSource source)
        {
            Timestamp = timestamp;
            PlayTimeSeconds = playTimeSeconds;
            FactionId = factionId ?? throw new ArgumentNullException(nameof(factionId));
            ZoneId = zoneId ?? throw new ArgumentNullException(nameof(zoneId));
            Tier = tier;
            Count = count;
            Source = source;
        }
    }
}
```

- [ ] **Step 8: Create ResourceTickEventRow.cs**

```csharp
using System;

namespace FactionWars.Telemetry.Models
{
    public sealed class ResourceTickEventRow
    {
        public DateTime Timestamp { get; }
        public long PlayTimeSeconds { get; }
        public string FactionId { get; }
        public int Income { get; }
        public int ZonesContributing { get; }

        public ResourceTickEventRow(DateTime timestamp, long playTimeSeconds, string factionId,
            int income, int zonesContributing)
        {
            Timestamp = timestamp;
            PlayTimeSeconds = playTimeSeconds;
            FactionId = factionId ?? throw new ArgumentNullException(nameof(factionId));
            Income = income;
            ZonesContributing = zonesContributing;
        }
    }
}
```

- [ ] **Step 9: Create MatchMetaEventRow.cs**

```csharp
using System;

namespace FactionWars.Telemetry.Models
{
    public sealed class MatchMetaEventRow
    {
        public DateTime Timestamp { get; }
        public long PlayTimeSeconds { get; }
        public MatchMetaEventType Type { get; }
        public string Details { get; }

        public MatchMetaEventRow(DateTime timestamp, long playTimeSeconds,
            MatchMetaEventType type, string details)
        {
            Timestamp = timestamp;
            PlayTimeSeconds = playTimeSeconds;
            Type = type;
            Details = details ?? string.Empty;
        }
    }
}
```

- [ ] **Step 10: Create PlayerEventRow.cs**

```csharp
using System;
using FactionWars.Core.Models;

namespace FactionWars.Telemetry.Models
{
    public sealed class PlayerEventRow
    {
        public DateTime Timestamp { get; }
        public long PlayTimeSeconds { get; }
        public PlayerEventType Type { get; }
        public string? ZoneId { get; }
        public string? TargetFaction { get; }
        public DefenderTier? TargetTier { get; }
        public string Details { get; }

        public PlayerEventRow(DateTime timestamp, long playTimeSeconds, PlayerEventType type,
            string? zoneId, string? targetFaction, DefenderTier? targetTier, string details)
        {
            Timestamp = timestamp;
            PlayTimeSeconds = playTimeSeconds;
            Type = type;
            ZoneId = zoneId;
            TargetFaction = targetFaction;
            TargetTier = targetTier;
            Details = details ?? string.Empty;
        }
    }
}
```

- [ ] **Step 11: Build and verify all files compile**

Run: `dotnet build FactionWars.sln`
Expected: Build succeeded, 0 errors, 0 warnings.

- [ ] **Step 12: Commit**

```bash
git add src/FactionWars/Telemetry/Models
git commit -m "feat(telemetry): add DTOs and enums for telemetry pipeline

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 2: ITelemetrySink interface and NullTelemetrySink

**Files:**
- Create: `src/FactionWars/Telemetry/Interfaces/ITelemetrySink.cs`
- Create: `src/FactionWars/Telemetry/Sinks/NullTelemetrySink.cs`

- [ ] **Step 1: Create ITelemetrySink.cs**

```csharp
using System;
using System.Collections.Generic;
using FactionWars.Telemetry.Models;

namespace FactionWars.Telemetry.Interfaces
{
    /// <summary>
    /// Sink for telemetry events. Implementations decide where rows go (CSV, no-op, etc.).
    /// All methods MUST be safe to call before SetSaveFile is called — implementations
    /// buffer in memory until a save filename is known.
    /// </summary>
    public interface ITelemetrySink : IDisposable
    {
        void SetSaveFile(string saveFilename);
        void WriteSnapshot(IReadOnlyList<FactionSnapshot> rows);
        void WriteZoneEvent(ZoneEventRow row);
        void WriteBattle(BattleEventRow row);
        void WriteDecision(DecisionEventRow row);
        void WriteRecruitment(RecruitmentEventRow row);
        void WriteAllocation(AllocationEventRow row);
        void WriteResourceTick(ResourceTickEventRow row);
        void WriteMatchMeta(MatchMetaEventRow row);
        void WritePlayerEvent(PlayerEventRow row);
    }
}
```

- [ ] **Step 2: Create NullTelemetrySink.cs**

```csharp
using System.Collections.Generic;
using FactionWars.Telemetry.Interfaces;
using FactionWars.Telemetry.Models;

namespace FactionWars.Telemetry.Sinks
{
    /// <summary>
    /// No-op telemetry sink. Used in tests and as an opt-out via DI override.
    /// Every method is safe and discards its input.
    /// </summary>
    public sealed class NullTelemetrySink : ITelemetrySink
    {
        public void SetSaveFile(string saveFilename) { }
        public void WriteSnapshot(IReadOnlyList<FactionSnapshot> rows) { }
        public void WriteZoneEvent(ZoneEventRow row) { }
        public void WriteBattle(BattleEventRow row) { }
        public void WriteDecision(DecisionEventRow row) { }
        public void WriteRecruitment(RecruitmentEventRow row) { }
        public void WriteAllocation(AllocationEventRow row) { }
        public void WriteResourceTick(ResourceTickEventRow row) { }
        public void WriteMatchMeta(MatchMetaEventRow row) { }
        public void WritePlayerEvent(PlayerEventRow row) { }
        public void Dispose() { }
    }
}
```

- [ ] **Step 3: Build**

Run: `dotnet build FactionWars.sln`
Expected: Build succeeded, 0 errors.

- [ ] **Step 4: Commit**

```bash
git add src/FactionWars/Telemetry/Interfaces src/FactionWars/Telemetry/Sinks/NullTelemetrySink.cs
git commit -m "feat(telemetry): add ITelemetrySink interface and NullTelemetrySink

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 3: CsvFieldEscaper

A pure helper for escaping CSV fields per RFC 4180: wrap in quotes if the value contains comma, quote, or newline; double internal quotes.

**Files:**
- Create: `src/FactionWars/Telemetry/Sinks/CsvFieldEscaper.cs`
- Test: `tests/FactionWars.Tests/Unit/Telemetry/CsvFieldEscaperTests.cs`

- [ ] **Step 1: Write failing test**

```csharp
using FactionWars.Telemetry.Sinks;
using Xunit;

namespace FactionWars.Tests.Unit.Telemetry
{
    public class CsvFieldEscaperTests
    {
        [Fact]
        public void Escape_PlainString_ReturnsUnchanged()
        {
            Assert.Equal("hello", CsvFieldEscaper.Escape("hello"));
        }

        [Fact]
        public void Escape_Null_ReturnsEmptyString()
        {
            Assert.Equal(string.Empty, CsvFieldEscaper.Escape(null));
        }

        [Fact]
        public void Escape_ContainsComma_WrapsInQuotes()
        {
            Assert.Equal("\"hello, world\"", CsvFieldEscaper.Escape("hello, world"));
        }

        [Fact]
        public void Escape_ContainsQuote_DoublesQuoteAndWraps()
        {
            Assert.Equal("\"say \"\"hi\"\"\"", CsvFieldEscaper.Escape("say \"hi\""));
        }

        [Fact]
        public void Escape_ContainsNewline_WrapsInQuotes()
        {
            Assert.Equal("\"line1\nline2\"", CsvFieldEscaper.Escape("line1\nline2"));
        }
    }
}
```

- [ ] **Step 2: Run test, verify failure**

Run: `dotnet test FactionWars.sln --filter "FullyQualifiedName~CsvFieldEscaperTests"`
Expected: Compilation failure — `CsvFieldEscaper` does not exist.

- [ ] **Step 3: Implement CsvFieldEscaper.cs**

```csharp
namespace FactionWars.Telemetry.Sinks
{
    /// <summary>
    /// Escapes a single field value for inclusion in a CSV row per RFC 4180.
    /// Wraps in double-quotes when the field contains a comma, quote, or newline;
    /// internal quotes are doubled.
    /// </summary>
    public static class CsvFieldEscaper
    {
        public static string Escape(string? value)
        {
            if (value == null)
                return string.Empty;

            bool needsQuoting = value.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0;
            if (!needsQuoting)
                return value;

            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
    }
}
```

- [ ] **Step 4: Run test, verify pass**

Run: `dotnet test FactionWars.sln --filter "FullyQualifiedName~CsvFieldEscaperTests"`
Expected: 5/5 pass.

- [ ] **Step 5: Commit**

```bash
git add src/FactionWars/Telemetry/Sinks/CsvFieldEscaper.cs tests/FactionWars.Tests/Unit/Telemetry/CsvFieldEscaperTests.cs
git commit -m "feat(telemetry): add CsvFieldEscaper helper

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 4: CsvTelemetrySink

The file-backed sink: lazy-opens a per-save directory under `Documents\FactionWars\Telemetry\<saveFilename>\`, writes one CSV per event type with header-once, buffers events in memory until `SetSaveFile` is called.

**Files:**
- Create: `src/FactionWars/Telemetry/Sinks/CsvTelemetrySink.cs`
- Test: `tests/FactionWars.Tests/Unit/Telemetry/CsvTelemetrySinkTests.cs`

- [ ] **Step 1: Write failing test**

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FactionWars.Telemetry.Models;
using FactionWars.Telemetry.Sinks;
using Xunit;

namespace FactionWars.Tests.Unit.Telemetry
{
    public class CsvTelemetrySinkTests : IDisposable
    {
        private readonly string _tempDir;

        public CsvTelemetrySinkTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "fw_tel_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
        }

        public void Dispose()
        {
            try { Directory.Delete(_tempDir, recursive: true); } catch { }
        }

        private static FactionSnapshot Snap(string id, int cash = 0) =>
            new FactionSnapshot(new DateTime(2026, 1, 1, 12, 0, 0), 100, id,
                cash, 0, 0, 0, 0, 0, 0, 0, 0);

        [Fact]
        public void WriteSnapshot_AfterSetSaveFile_WritesHeaderAndRow()
        {
            using var sink = new CsvTelemetrySink(_tempDir);
            sink.SetSaveFile("SGTA0001");
            sink.WriteSnapshot(new[] { Snap("michael", 500) });

            var path = Path.Combine(_tempDir, "SGTA0001", "snapshots.csv");
            var lines = File.ReadAllLines(path);
            Assert.Equal(2, lines.Length);
            Assert.StartsWith("timestamp,play_time_seconds,faction_id,cash,", lines[0]);
            Assert.Contains("michael", lines[1]);
            Assert.Contains("500", lines[1]);
        }

        [Fact]
        public void WriteSnapshot_BeforeSetSaveFile_BuffersAndFlushesOnSet()
        {
            using var sink = new CsvTelemetrySink(_tempDir);
            sink.WriteSnapshot(new[] { Snap("trevor", 100) });
            sink.WriteSnapshot(new[] { Snap("franklin", 200) });

            // Pre-flush: no files exist
            Assert.False(Directory.Exists(Path.Combine(_tempDir, "SGTA0002")));

            sink.SetSaveFile("SGTA0002");

            var path = Path.Combine(_tempDir, "SGTA0002", "snapshots.csv");
            var lines = File.ReadAllLines(path);
            Assert.Equal(3, lines.Length); // header + 2 rows
            Assert.Contains("trevor", lines[1]);
            Assert.Contains("franklin", lines[2]);
        }

        [Fact]
        public void WriteSnapshot_TwoCalls_WritesHeaderOnce()
        {
            using var sink = new CsvTelemetrySink(_tempDir);
            sink.SetSaveFile("SGTA0003");
            sink.WriteSnapshot(new[] { Snap("a") });
            sink.WriteSnapshot(new[] { Snap("b") });

            var path = Path.Combine(_tempDir, "SGTA0003", "snapshots.csv");
            var lines = File.ReadAllLines(path);
            Assert.Equal(3, lines.Length);
            Assert.StartsWith("timestamp,", lines[0]);
            Assert.DoesNotContain("timestamp,", lines[1]);
            Assert.DoesNotContain("timestamp,", lines[2]);
        }

        [Fact]
        public void WriteZoneEvent_WritesToZoneEventsFile()
        {
            using var sink = new CsvTelemetrySink(_tempDir);
            sink.SetSaveFile("SGTA0004");
            sink.WriteZoneEvent(new ZoneEventRow(
                new DateTime(2026, 1, 1, 12, 0, 0), 200,
                ZoneEventType.Captured, "morningwood", "trevor", "michael"));

            var path = Path.Combine(_tempDir, "SGTA0004", "zone_events.csv");
            Assert.True(File.Exists(path));
            var lines = File.ReadAllLines(path);
            Assert.Equal(2, lines.Length);
            Assert.Contains("Captured", lines[1]);
            Assert.Contains("morningwood", lines[1]);
        }

        [Fact]
        public void Dispose_IsIdempotent()
        {
            var sink = new CsvTelemetrySink(_tempDir);
            sink.Dispose();
            sink.Dispose(); // must not throw
        }

        [Fact]
        public void WriteAfterDispose_DoesNotThrow()
        {
            var sink = new CsvTelemetrySink(_tempDir);
            sink.Dispose();
            sink.WriteSnapshot(new[] { Snap("a") }); // must not throw
        }
    }
}
```

- [ ] **Step 2: Run test, verify failure**

Run: `dotnet test FactionWars.sln --filter "FullyQualifiedName~CsvTelemetrySinkTests"`
Expected: Compilation failure — `CsvTelemetrySink` does not exist.

- [ ] **Step 3: Implement CsvTelemetrySink.cs**

```csharp
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using FactionWars.ScriptHookV.Logging;
using FactionWars.Telemetry.Interfaces;
using FactionWars.Telemetry.Models;

namespace FactionWars.Telemetry.Sinks
{
    /// <summary>
    /// Writes telemetry rows to per-save CSV files under a base directory.
    /// Buffers events in memory until SetSaveFile is called, then flushes and
    /// switches to direct-append mode. Thread-safe via a single lock.
    /// </summary>
    public sealed class CsvTelemetrySink : ITelemetrySink
    {
        private const int BufferCapPerType = 10000;
        private static readonly string SnapshotHeader =
            "timestamp,play_time_seconds,faction_id,cash,total_troops,zones_owned,basic,medium,heavy,elite,reserve_troops,deployed_troops";
        private static readonly string ZoneEventHeader =
            "timestamp,play_time_seconds,event_type,zone_id,previous_owner,new_owner";
        private static readonly string BattleHeader =
            "timestamp,play_time_seconds,event_type,zone_id,attacker_faction,defender_faction,attacker_troops,defender_troops,outcome,attacker_casualties,defender_casualties";
        private static readonly string DecisionHeader =
            "timestamp,play_time_seconds,faction_id,decision_type,target_zone,troops,priority,executed";
        private static readonly string RecruitmentHeader =
            "timestamp,play_time_seconds,faction_id,troops_recruited,cost,cash_before,cash_after";
        private static readonly string AllocationHeader =
            "timestamp,play_time_seconds,faction_id,zone_id,tier,count,source";
        private static readonly string ResourceTickHeader =
            "timestamp,play_time_seconds,faction_id,income,zones_contributing";
        private static readonly string MatchMetaHeader =
            "timestamp,play_time_seconds,event_type,details";
        private static readonly string PlayerEventHeader =
            "timestamp,play_time_seconds,event_type,zone_id,target_faction,target_tier,details";

        private readonly object _lock = new object();
        private readonly string _baseDir;
        private string? _saveDir;
        private bool _disposed;
        private readonly HashSet<string> _erroredFiles = new HashSet<string>();

        // Buffers (used until _saveDir is set)
        private readonly List<FactionSnapshot> _bufSnap = new List<FactionSnapshot>();
        private readonly List<ZoneEventRow> _bufZone = new List<ZoneEventRow>();
        private readonly List<BattleEventRow> _bufBattle = new List<BattleEventRow>();
        private readonly List<DecisionEventRow> _bufDecision = new List<DecisionEventRow>();
        private readonly List<RecruitmentEventRow> _bufRecruit = new List<RecruitmentEventRow>();
        private readonly List<AllocationEventRow> _bufAlloc = new List<AllocationEventRow>();
        private readonly List<ResourceTickEventRow> _bufTick = new List<ResourceTickEventRow>();
        private readonly List<MatchMetaEventRow> _bufMeta = new List<MatchMetaEventRow>();
        private readonly List<PlayerEventRow> _bufPlayer = new List<PlayerEventRow>();

        /// <param name="baseDirectory">Root telemetry directory (e.g., Documents\FactionWars\Telemetry).</param>
        public CsvTelemetrySink(string baseDirectory)
        {
            _baseDir = baseDirectory ?? throw new ArgumentNullException(nameof(baseDirectory));
        }

        public void SetSaveFile(string saveFilename)
        {
            if (string.IsNullOrWhiteSpace(saveFilename))
                throw new ArgumentException("saveFilename cannot be empty", nameof(saveFilename));

            lock (_lock)
            {
                if (_disposed) return;
                if (_saveDir != null) return; // already set; one-shot

                _saveDir = Path.Combine(_baseDir, saveFilename);
                try
                {
                    Directory.CreateDirectory(_saveDir);
                }
                catch (Exception ex)
                {
                    FileLogger.Error($"CsvTelemetrySink: failed to create {_saveDir}", ex);
                    _saveDir = null;
                    return;
                }

                FlushBuffersLocked();
            }
        }

        public void WriteSnapshot(IReadOnlyList<FactionSnapshot> rows)
        {
            if (rows == null || rows.Count == 0) return;
            lock (_lock)
            {
                if (_disposed) return;
                if (_saveDir == null) { BufferLocked(_bufSnap, rows); return; }
                AppendLocked("snapshots.csv", SnapshotHeader, rows.Select(SerializeSnapshot));
            }
        }

        public void WriteZoneEvent(ZoneEventRow row)
        {
            if (row == null) return;
            lock (_lock)
            {
                if (_disposed) return;
                if (_saveDir == null) { BufferLocked(_bufZone, new[] { row }); return; }
                AppendLocked("zone_events.csv", ZoneEventHeader, new[] { SerializeZoneEvent(row) });
            }
        }

        public void WriteBattle(BattleEventRow row)
        {
            if (row == null) return;
            lock (_lock)
            {
                if (_disposed) return;
                if (_saveDir == null) { BufferLocked(_bufBattle, new[] { row }); return; }
                AppendLocked("battles.csv", BattleHeader, new[] { SerializeBattle(row) });
            }
        }

        public void WriteDecision(DecisionEventRow row)
        {
            if (row == null) return;
            lock (_lock)
            {
                if (_disposed) return;
                if (_saveDir == null) { BufferLocked(_bufDecision, new[] { row }); return; }
                AppendLocked("decisions.csv", DecisionHeader, new[] { SerializeDecision(row) });
            }
        }

        public void WriteRecruitment(RecruitmentEventRow row)
        {
            if (row == null) return;
            lock (_lock)
            {
                if (_disposed) return;
                if (_saveDir == null) { BufferLocked(_bufRecruit, new[] { row }); return; }
                AppendLocked("recruitments.csv", RecruitmentHeader, new[] { SerializeRecruitment(row) });
            }
        }

        public void WriteAllocation(AllocationEventRow row)
        {
            if (row == null) return;
            lock (_lock)
            {
                if (_disposed) return;
                if (_saveDir == null) { BufferLocked(_bufAlloc, new[] { row }); return; }
                AppendLocked("allocations.csv", AllocationHeader, new[] { SerializeAllocation(row) });
            }
        }

        public void WriteResourceTick(ResourceTickEventRow row)
        {
            if (row == null) return;
            lock (_lock)
            {
                if (_disposed) return;
                if (_saveDir == null) { BufferLocked(_bufTick, new[] { row }); return; }
                AppendLocked("resource_ticks.csv", ResourceTickHeader, new[] { SerializeResourceTick(row) });
            }
        }

        public void WriteMatchMeta(MatchMetaEventRow row)
        {
            if (row == null) return;
            lock (_lock)
            {
                if (_disposed) return;
                if (_saveDir == null) { BufferLocked(_bufMeta, new[] { row }); return; }
                AppendLocked("match_meta.csv", MatchMetaHeader, new[] { SerializeMatchMeta(row) });
            }
        }

        public void WritePlayerEvent(PlayerEventRow row)
        {
            if (row == null) return;
            lock (_lock)
            {
                if (_disposed) return;
                if (_saveDir == null) { BufferLocked(_bufPlayer, new[] { row }); return; }
                AppendLocked("player_events.csv", PlayerEventHeader, new[] { SerializePlayerEvent(row) });
            }
        }

        public void Dispose()
        {
            lock (_lock)
            {
                if (_disposed) return;
                _disposed = true;
                _bufSnap.Clear(); _bufZone.Clear(); _bufBattle.Clear();
                _bufDecision.Clear(); _bufRecruit.Clear(); _bufAlloc.Clear();
                _bufTick.Clear(); _bufMeta.Clear(); _bufPlayer.Clear();
            }
        }

        private static void BufferLocked<T>(List<T> buffer, IEnumerable<T> rows)
        {
            foreach (var r in rows)
            {
                if (buffer.Count >= BufferCapPerType) buffer.RemoveAt(0);
                buffer.Add(r);
            }
        }

        private void FlushBuffersLocked()
        {
            if (_bufSnap.Count > 0)
            {
                AppendLocked("snapshots.csv", SnapshotHeader, _bufSnap.Select(SerializeSnapshot));
                _bufSnap.Clear();
            }
            if (_bufZone.Count > 0)
            {
                AppendLocked("zone_events.csv", ZoneEventHeader, _bufZone.Select(SerializeZoneEvent));
                _bufZone.Clear();
            }
            if (_bufBattle.Count > 0)
            {
                AppendLocked("battles.csv", BattleHeader, _bufBattle.Select(SerializeBattle));
                _bufBattle.Clear();
            }
            if (_bufDecision.Count > 0)
            {
                AppendLocked("decisions.csv", DecisionHeader, _bufDecision.Select(SerializeDecision));
                _bufDecision.Clear();
            }
            if (_bufRecruit.Count > 0)
            {
                AppendLocked("recruitments.csv", RecruitmentHeader, _bufRecruit.Select(SerializeRecruitment));
                _bufRecruit.Clear();
            }
            if (_bufAlloc.Count > 0)
            {
                AppendLocked("allocations.csv", AllocationHeader, _bufAlloc.Select(SerializeAllocation));
                _bufAlloc.Clear();
            }
            if (_bufTick.Count > 0)
            {
                AppendLocked("resource_ticks.csv", ResourceTickHeader, _bufTick.Select(SerializeResourceTick));
                _bufTick.Clear();
            }
            if (_bufMeta.Count > 0)
            {
                AppendLocked("match_meta.csv", MatchMetaHeader, _bufMeta.Select(SerializeMatchMeta));
                _bufMeta.Clear();
            }
            if (_bufPlayer.Count > 0)
            {
                AppendLocked("player_events.csv", PlayerEventHeader, _bufPlayer.Select(SerializePlayerEvent));
                _bufPlayer.Clear();
            }
        }

        private void AppendLocked(string fileName, string header, IEnumerable<string> rows)
        {
            if (_saveDir == null) return;
            var path = Path.Combine(_saveDir, fileName);
            try
            {
                bool needsHeader = !File.Exists(path);
                var sb = new StringBuilder();
                if (needsHeader) sb.AppendLine(header);
                foreach (var row in rows) sb.AppendLine(row);
                File.AppendAllText(path, sb.ToString());
            }
            catch (Exception ex)
            {
                if (_erroredFiles.Add(path))
                    FileLogger.Error($"CsvTelemetrySink: failed to append to {path}", ex);
            }
        }

        private static string Iso(DateTime dt) => dt.ToString("o", CultureInfo.InvariantCulture);
        private static string Esc(string? v) => CsvFieldEscaper.Escape(v);
        private static string I(int v) => v.ToString(CultureInfo.InvariantCulture);
        private static string L(long v) => v.ToString(CultureInfo.InvariantCulture);
        private static string D(double v) => v.ToString("G", CultureInfo.InvariantCulture);

        private static string SerializeSnapshot(FactionSnapshot r) => string.Join(",",
            Iso(r.Timestamp), L(r.PlayTimeSeconds), Esc(r.FactionId),
            I(r.Cash), I(r.TotalTroops), I(r.ZonesOwned),
            I(r.Basic), I(r.Medium), I(r.Heavy), I(r.Elite),
            I(r.ReserveTroops), I(r.DeployedTroops));

        private static string SerializeZoneEvent(ZoneEventRow r) => string.Join(",",
            Iso(r.Timestamp), L(r.PlayTimeSeconds),
            r.Type.ToString(), Esc(r.ZoneId),
            Esc(r.PreviousOwner), Esc(r.NewOwner));

        private static string SerializeBattle(BattleEventRow r) => string.Join(",",
            Iso(r.Timestamp), L(r.PlayTimeSeconds),
            r.Type.ToString(), Esc(r.ZoneId),
            Esc(r.AttackerFactionId), Esc(r.DefenderFactionId),
            I(r.AttackerTroops), I(r.DefenderTroops),
            r.Outcome.HasValue ? r.Outcome.Value.ToString() : string.Empty,
            I(r.AttackerCasualties), I(r.DefenderCasualties));

        private static string SerializeDecision(DecisionEventRow r) => string.Join(",",
            Iso(r.Timestamp), L(r.PlayTimeSeconds),
            Esc(r.FactionId), r.Type.ToString(),
            Esc(r.TargetZoneId), I(r.Troops), D(r.Priority),
            r.Executed ? "true" : "false");

        private static string SerializeRecruitment(RecruitmentEventRow r) => string.Join(",",
            Iso(r.Timestamp), L(r.PlayTimeSeconds), Esc(r.FactionId),
            I(r.TroopsRecruited), I(r.Cost), I(r.CashBefore), I(r.CashAfter));

        private static string SerializeAllocation(AllocationEventRow r) => string.Join(",",
            Iso(r.Timestamp), L(r.PlayTimeSeconds),
            Esc(r.FactionId), Esc(r.ZoneId),
            r.Tier.ToString(), I(r.Count), r.Source.ToString());

        private static string SerializeResourceTick(ResourceTickEventRow r) => string.Join(",",
            Iso(r.Timestamp), L(r.PlayTimeSeconds), Esc(r.FactionId),
            I(r.Income), I(r.ZonesContributing));

        private static string SerializeMatchMeta(MatchMetaEventRow r) => string.Join(",",
            Iso(r.Timestamp), L(r.PlayTimeSeconds),
            r.Type.ToString(), Esc(r.Details));

        private static string SerializePlayerEvent(PlayerEventRow r) => string.Join(",",
            Iso(r.Timestamp), L(r.PlayTimeSeconds), r.Type.ToString(),
            Esc(r.ZoneId), Esc(r.TargetFaction),
            r.TargetTier.HasValue ? r.TargetTier.Value.ToString() : string.Empty,
            Esc(r.Details));
    }
}
```

- [ ] **Step 4: Run test, verify pass**

Run: `dotnet test FactionWars.sln --filter "FullyQualifiedName~CsvTelemetrySinkTests"`
Expected: 6/6 pass.

- [ ] **Step 5: Run full test suite to verify nothing else broke**

Run: `dotnet test FactionWars.sln`
Expected: All tests pass (existing flake notes from session prior — `NativeSaveWatcher` debounce tests may be flaky; tolerate those specifically).

- [ ] **Step 6: Commit**

```bash
git add src/FactionWars/Telemetry/Sinks/CsvTelemetrySink.cs tests/FactionWars.Tests/Unit/Telemetry/CsvTelemetrySinkTests.cs
git commit -m "feat(telemetry): add CsvTelemetrySink with buffer-then-flush

Writes 9 per-event-type CSV files under a per-save directory. Header
written once per file. Buffers in memory until SetSaveFile is called,
then flushes and switches to direct-append mode.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 5: FactionSnapshotBuilder

Pure builder that reads from `IFactionService` + `IZoneService` and produces one `FactionSnapshot` per active faction.

**Files:**
- Create: `src/FactionWars/Telemetry/Services/FactionSnapshotBuilder.cs`
- Test: `tests/FactionWars.Tests/Unit/Telemetry/FactionSnapshotBuilderTests.cs`

- [ ] **Step 1: Write failing test**

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using FactionWars.Core.Models;
using FactionWars.Factions.Interfaces;
using FactionWars.Factions.Models;
using FactionWars.Telemetry.Services;
using FactionWars.Territory.Interfaces;
using Moq;
using Xunit;

namespace FactionWars.Tests.Unit.Telemetry
{
    public class FactionSnapshotBuilderTests
    {
        private static FactionState MakeState(string id, int cash, int basic, int medium, int heavy, int elite)
        {
            var s = new FactionState(id) { Cash = cash };
            s.AddReserveTroops(DefenderTier.Basic, basic);
            s.AddReserveTroops(DefenderTier.Medium, medium);
            s.AddReserveTroops(DefenderTier.Heavy, heavy);
            s.AddReserveTroops(DefenderTier.Elite, elite);
            return s;
        }

        [Fact]
        public void Build_ProducesOneSnapshotPerActiveFaction()
        {
            var factionService = new Mock<IFactionService>();
            var zoneService = new Mock<IZoneService>();
            var michael = new Faction("michael", "Michael's Crew");
            var trevor = new Faction("trevor", "Trevor's Crew");
            factionService.Setup(s => s.GetAllFactions()).Returns(new[] { michael, trevor });
            factionService.Setup(s => s.GetFactionState("michael")).Returns(MakeState("michael", 500, 4, 2, 1, 0));
            factionService.Setup(s => s.GetFactionState("trevor")).Returns(MakeState("trevor", 0, 70, 0, 0, 0));
            zoneService.Setup(z => z.GetZoneCount("michael")).Returns(8);
            zoneService.Setup(z => z.GetZoneCount("trevor")).Returns(21);

            var builder = new FactionSnapshotBuilder(factionService.Object, zoneService.Object);

            var rows = builder.Build(new DateTime(2026, 1, 1, 12, 0, 0), playTimeSeconds: 100);

            Assert.Equal(2, rows.Count);
            var m = rows.Single(r => r.FactionId == "michael");
            Assert.Equal(500, m.Cash);
            Assert.Equal(7, m.TotalTroops);    // 4+2+1+0
            Assert.Equal(8, m.ZonesOwned);
            Assert.Equal(4, m.Basic);
            Assert.Equal(2, m.Medium);
            Assert.Equal(1, m.Heavy);
            Assert.Equal(0, m.Elite);
            Assert.Equal(7, m.ReserveTroops);  // sum of tiers
            Assert.Equal(0, m.DeployedTroops); // total - reserve = 0 (deployed not tracked here yet)
        }

        [Fact]
        public void Build_PassesTimestampThrough()
        {
            var factionService = new Mock<IFactionService>();
            var zoneService = new Mock<IZoneService>();
            factionService.Setup(s => s.GetAllFactions()).Returns(System.Array.Empty<Faction>());
            var builder = new FactionSnapshotBuilder(factionService.Object, zoneService.Object);

            var ts = new DateTime(2025, 6, 1);
            var rows = builder.Build(ts, 999);
            Assert.Empty(rows);
        }
    }
}
```

- [ ] **Step 2: Run test, verify failure**

Run: `dotnet test FactionWars.sln --filter "FullyQualifiedName~FactionSnapshotBuilderTests"`
Expected: Compilation failure — `FactionSnapshotBuilder` does not exist.

- [ ] **Step 3: Implement FactionSnapshotBuilder.cs**

```csharp
using System;
using System.Collections.Generic;
using FactionWars.Core.Models;
using FactionWars.Factions.Interfaces;
using FactionWars.Telemetry.Models;
using FactionWars.Territory.Interfaces;

namespace FactionWars.Telemetry.Services
{
    /// <summary>
    /// Builds FactionSnapshot rows from current faction and zone state.
    /// Pure: no I/O, no side effects.
    /// </summary>
    public sealed class FactionSnapshotBuilder
    {
        private readonly IFactionService _factionService;
        private readonly IZoneService _zoneService;

        public FactionSnapshotBuilder(IFactionService factionService, IZoneService zoneService)
        {
            _factionService = factionService ?? throw new ArgumentNullException(nameof(factionService));
            _zoneService = zoneService ?? throw new ArgumentNullException(nameof(zoneService));
        }

        public IReadOnlyList<FactionSnapshot> Build(DateTime timestamp, long playTimeSeconds)
        {
            var rows = new List<FactionSnapshot>();
            foreach (var faction in _factionService.GetAllFactions())
            {
                var state = _factionService.GetFactionState(faction.Id);
                if (state == null) continue;

                int basic = state.GetReserveTroops(DefenderTier.Basic);
                int medium = state.GetReserveTroops(DefenderTier.Medium);
                int heavy = state.GetReserveTroops(DefenderTier.Heavy);
                int elite = state.GetReserveTroops(DefenderTier.Elite);
                int reserve = basic + medium + heavy + elite;
                int total = state.TroopCount;
                int deployed = Math.Max(0, total - reserve);

                rows.Add(new FactionSnapshot(
                    timestamp, playTimeSeconds, faction.Id,
                    state.Cash, total,
                    _zoneService.GetZoneCount(faction.Id),
                    basic, medium, heavy, elite,
                    reserve, deployed));
            }
            return rows;
        }
    }
}
```

- [ ] **Step 4: Run test, verify pass**

Run: `dotnet test FactionWars.sln --filter "FullyQualifiedName~FactionSnapshotBuilderTests"`
Expected: 2/2 pass.

- [ ] **Step 5: Commit**

```bash
git add src/FactionWars/Telemetry/Services/FactionSnapshotBuilder.cs tests/FactionWars.Tests/Unit/Telemetry/FactionSnapshotBuilderTests.cs
git commit -m "feat(telemetry): add FactionSnapshotBuilder

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 6: AttackerKilled event on BattleAttackerManager

The `BattleAttackerManager` already tracks per-tier attacker peds and detects deaths. Add an event so telemetry can react when an attacker dies, and pass killer info for player-kill resolution.

**Files:**
- Create: `src/FactionWars/Combat/Events/AttackerKilledEventArgs.cs`
- Modify: `src/FactionWars/ScriptHookV/Managers/BattleAttackerManager.cs` (add event + raise)
- Test: `tests/FactionWars.Tests/Unit/ScriptHookV/BattleAttackerManagerTests.cs` (extend if exists, else create)

- [ ] **Step 1: Find the existing death-detection site in BattleAttackerManager**

Run: `grep -nE "IsPedDead|PedKilled|attacker.*died|attacker.*kill" src/FactionWars/ScriptHookV/Managers/BattleAttackerManager.cs | head -10`

Read the relevant section. The event MUST be raised at the moment the manager classifies an attacker ped as dead (so `KillerPedHandle` is still resolvable from `IGameBridge.GetPedKiller`).

- [ ] **Step 2: Create AttackerKilledEventArgs.cs**

```csharp
using System;
using FactionWars.Core.Models;

namespace FactionWars.Combat.Events
{
    public sealed class AttackerKilledEventArgs : EventArgs
    {
        public string ZoneId { get; }
        public string FactionId { get; }
        public DefenderTier Tier { get; }
        public int PedHandle { get; }
        public int KillerPedHandle { get; }

        public AttackerKilledEventArgs(string zoneId, string factionId, DefenderTier tier,
            int pedHandle, int killerPedHandle)
        {
            ZoneId = zoneId ?? throw new ArgumentNullException(nameof(zoneId));
            FactionId = factionId ?? throw new ArgumentNullException(nameof(factionId));
            Tier = tier;
            PedHandle = pedHandle;
            KillerPedHandle = killerPedHandle;
        }
    }
}
```

- [ ] **Step 3: Write failing test for the event**

Find an existing test class for BattleAttackerManager via `find tests -name "BattleAttackerManager*Tests.cs"`. If it exists, add to it. If not, create a new one.

```csharp
// In tests/FactionWars.Tests/Unit/ScriptHookV/BattleAttackerManagerTests.cs
[Fact]
public void OnAttackerDeath_RaisesAttackerKilledEvent()
{
    // Arrange a manager with a tracked attacker ped, simulate it being marked dead.
    // Capture the AttackerKilled event args and assert fields.
    AttackerKilledEventArgs? captured = null;
    _manager.AttackerKilled += (_, args) => captured = args;

    // Trigger death detection (depends on existing manager API — typically by calling
    // the tick method after the mock IGameBridge reports the ped as dead).
    _gameBridge.SetPedDead(pedHandle: 42, isDead: true);
    _gameBridge.SetPedKiller(deadPedHandle: 42, killerPedHandle: 99);
    _manager.Tick(/* zone context */);

    Assert.NotNull(captured);
    Assert.Equal(42, captured!.PedHandle);
    Assert.Equal(99, captured.KillerPedHandle);
    Assert.Equal("morningwood", captured.ZoneId);
    Assert.Equal("trevor", captured.FactionId);
}
```

NOTE: Exact test wiring depends on the existing manager API and mock infrastructure. Adapt to existing patterns in the test class. The key assertion: when an attacker ped dies, the event fires once with correct ped/killer/zone/faction/tier.

- [ ] **Step 4: Run test, verify failure**

Run: `dotnet test FactionWars.sln --filter "FullyQualifiedName~BattleAttackerManagerTests.OnAttackerDeath_RaisesAttackerKilledEvent"`
Expected: Compilation failure — `AttackerKilled` event does not exist on the manager.

- [ ] **Step 5: Add event field and raise it on death detection**

In `BattleAttackerManager.cs`:

1. Add `using FactionWars.Combat.Events;` at the top.
2. Add the event declaration near other public events:
   ```csharp
   public event EventHandler<AttackerKilledEventArgs>? AttackerKilled;
   ```
3. At the death-detection site (where the manager removes a dead attacker from its tracking), add:
   ```csharp
   int killerHandle = _gameBridge.GetPedKiller(pedHandle); // returns -1 if unknown
   AttackerKilled?.Invoke(this, new AttackerKilledEventArgs(
       zoneId: currentZoneId,
       factionId: attackerFactionId,
       tier: attackerTier,
       pedHandle: pedHandle,
       killerPedHandle: killerHandle));
   ```
   The exact variable names will match what's available at the death-detection site. If `GetPedKiller` is not on `IGameBridge` yet, add it (check existing native pinvoke patterns in `IGameBridge`). If it's already there, use it directly.

- [ ] **Step 6: Run test, verify pass**

Run: `dotnet test FactionWars.sln --filter "FullyQualifiedName~BattleAttackerManagerTests"`
Expected: All BattleAttackerManagerTests pass, including the new one.

- [ ] **Step 7: Run full test suite to confirm no regressions**

Run: `dotnet test FactionWars.sln`
Expected: All tests pass.

- [ ] **Step 8: Commit**

```bash
git add src/FactionWars/Combat/Events src/FactionWars/ScriptHookV/Managers/BattleAttackerManager.cs tests/FactionWars.Tests/Unit/ScriptHookV/BattleAttackerManagerTests.cs
git commit -m "feat(combat): add AttackerKilled event on BattleAttackerManager

Raised when a tracked enemy attacker ped is detected dead. Includes the
killer ped handle so consumers can resolve player-attributed kills.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 7: PlayerKillResolver

Pure helper that takes an `AttackerKilledEventArgs` plus the player's ped handle and returns a `PlayerEventRow` if the killer was the player, or `null` otherwise.

**Files:**
- Create: `src/FactionWars/Telemetry/Services/PlayerKillResolver.cs`
- Test: `tests/FactionWars.Tests/Unit/Telemetry/PlayerKillResolverTests.cs`

- [ ] **Step 1: Write failing test**

```csharp
using System;
using FactionWars.Combat.Events;
using FactionWars.Core.Models;
using FactionWars.Telemetry.Models;
using FactionWars.Telemetry.Services;
using Xunit;

namespace FactionWars.Tests.Unit.Telemetry
{
    public class PlayerKillResolverTests
    {
        [Fact]
        public void Resolve_KillerIsPlayer_ReturnsPlayerEvent()
        {
            var args = new AttackerKilledEventArgs("morningwood", "trevor", DefenderTier.Heavy,
                pedHandle: 42, killerPedHandle: 99);

            var result = PlayerKillResolver.Resolve(args, playerPedHandle: 99,
                timestamp: new DateTime(2026, 1, 1), playTimeSeconds: 100);

            Assert.NotNull(result);
            Assert.Equal(PlayerEventType.Kill, result!.Type);
            Assert.Equal("morningwood", result.ZoneId);
            Assert.Equal("trevor", result.TargetFaction);
            Assert.Equal(DefenderTier.Heavy, result.TargetTier);
        }

        [Fact]
        public void Resolve_KillerIsNotPlayer_ReturnsNull()
        {
            var args = new AttackerKilledEventArgs("morningwood", "trevor", DefenderTier.Basic,
                pedHandle: 42, killerPedHandle: 50);

            var result = PlayerKillResolver.Resolve(args, playerPedHandle: 99,
                timestamp: new DateTime(2026, 1, 1), playTimeSeconds: 100);

            Assert.Null(result);
        }

        [Fact]
        public void Resolve_KillerUnknown_ReturnsNull()
        {
            var args = new AttackerKilledEventArgs("morningwood", "trevor", DefenderTier.Basic,
                pedHandle: 42, killerPedHandle: -1);

            var result = PlayerKillResolver.Resolve(args, playerPedHandle: 99,
                timestamp: new DateTime(2026, 1, 1), playTimeSeconds: 100);

            Assert.Null(result);
        }
    }
}
```

- [ ] **Step 2: Run test, verify failure**

Run: `dotnet test FactionWars.sln --filter "FullyQualifiedName~PlayerKillResolverTests"`
Expected: Compilation failure — `PlayerKillResolver` does not exist.

- [ ] **Step 3: Implement PlayerKillResolver.cs**

```csharp
using System;
using FactionWars.Combat.Events;
using FactionWars.Telemetry.Models;

namespace FactionWars.Telemetry.Services
{
    /// <summary>
    /// Resolves whether an AttackerKilled event was caused by the player ped.
    /// Pure: no I/O, no side effects.
    /// </summary>
    public static class PlayerKillResolver
    {
        public static PlayerEventRow? Resolve(AttackerKilledEventArgs args, int playerPedHandle,
            DateTime timestamp, long playTimeSeconds)
        {
            if (args == null) throw new ArgumentNullException(nameof(args));
            if (args.KillerPedHandle <= 0) return null;
            if (args.KillerPedHandle != playerPedHandle) return null;

            return new PlayerEventRow(
                timestamp,
                playTimeSeconds,
                PlayerEventType.Kill,
                zoneId: args.ZoneId,
                targetFaction: args.FactionId,
                targetTier: args.Tier,
                details: string.Empty);
        }
    }
}
```

- [ ] **Step 4: Run test, verify pass**

Run: `dotnet test FactionWars.sln --filter "FullyQualifiedName~PlayerKillResolverTests"`
Expected: 3/3 pass.

- [ ] **Step 5: Commit**

```bash
git add src/FactionWars/Telemetry/Services/PlayerKillResolver.cs tests/FactionWars.Tests/Unit/Telemetry/PlayerKillResolverTests.cs
git commit -m "feat(telemetry): add PlayerKillResolver

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 8: ZoneOwnershipChanged event on IZoneService

**Files:**
- Create: `src/FactionWars/Territory/Events/ZoneOwnershipChangedEventArgs.cs`
- Modify: `src/FactionWars/Territory/Interfaces/IZoneService.cs` (add event)
- Modify: `src/FactionWars/Territory/Services/ZoneService.cs` (raise event)
- Test: `tests/FactionWars.Tests/Unit/Territory/ZoneServiceTests.cs` (extend)

- [ ] **Step 1: Write failing test**

Add to `ZoneServiceTests.cs`:

```csharp
[Fact]
public void TransferZoneOwnership_ChangesOwner_RaisesZoneOwnershipChanged()
{
    // Arrange
    var zone = CreateTestZone("morningwood", "Morningwood");
    zone.OwnerFactionId = "trevor";
    _repository.Add(zone);

    ZoneOwnershipChangedEventArgs? captured = null;
    _service.ZoneOwnershipChanged += (_, args) => captured = args;

    // Act
    var result = _service.TransferZoneOwnership("morningwood", "michael");

    // Assert
    Assert.True(result);
    Assert.NotNull(captured);
    Assert.Equal("morningwood", captured!.ZoneId);
    Assert.Equal("trevor", captured.PreviousOwner);
    Assert.Equal("michael", captured.NewOwner);
}

[Fact]
public void TransferZoneOwnership_SameOwner_DoesNotRaiseEvent()
{
    var zone = CreateTestZone("morningwood", "Morningwood");
    zone.OwnerFactionId = "trevor";
    _repository.Add(zone);

    bool raised = false;
    _service.ZoneOwnershipChanged += (_, _) => raised = true;

    _service.TransferZoneOwnership("morningwood", "trevor");

    Assert.False(raised);
}

[Fact]
public void TransferZoneOwnership_ZoneNotFound_DoesNotRaiseEvent()
{
    bool raised = false;
    _service.ZoneOwnershipChanged += (_, _) => raised = true;

    _service.TransferZoneOwnership("nope", "michael");

    Assert.False(raised);
}
```

Add `using FactionWars.Territory.Events;` to the test file.

- [ ] **Step 2: Run test, verify failure**

Run: `dotnet test FactionWars.sln --filter "FullyQualifiedName~ZoneServiceTests.TransferZoneOwnership_ChangesOwner_RaisesZoneOwnershipChanged"`
Expected: Compilation failure — event/event-args do not exist.

- [ ] **Step 3: Create ZoneOwnershipChangedEventArgs.cs**

```csharp
using System;

namespace FactionWars.Territory.Events
{
    public sealed class ZoneOwnershipChangedEventArgs : EventArgs
    {
        public string ZoneId { get; }
        public string? PreviousOwner { get; }
        public string? NewOwner { get; }

        public ZoneOwnershipChangedEventArgs(string zoneId, string? previousOwner, string? newOwner)
        {
            ZoneId = zoneId ?? throw new ArgumentNullException(nameof(zoneId));
            PreviousOwner = previousOwner;
            NewOwner = newOwner;
        }
    }
}
```

- [ ] **Step 4: Add event to IZoneService.cs**

Modify `src/FactionWars/Territory/Interfaces/IZoneService.cs`:

1. Add `using FactionWars.Territory.Events;` at top.
2. Add inside the interface (near top, after summary block):
   ```csharp
   /// <summary>
   /// Raised when a zone's owner actually changes (not raised if newOwner == previousOwner).
   /// </summary>
   event EventHandler<ZoneOwnershipChangedEventArgs>? ZoneOwnershipChanged;
   ```

- [ ] **Step 5: Raise the event in ZoneService.TransferZoneOwnership**

Modify `src/FactionWars/Territory/Services/ZoneService.cs`:

1. Add `using FactionWars.Territory.Events;` at top.
2. Add field declaration:
   ```csharp
   public event EventHandler<ZoneOwnershipChangedEventArgs>? ZoneOwnershipChanged;
   ```
3. Inside `TransferZoneOwnership`, after `_repository.Update(zone)` and `SyncFactionStateZones(...)`, before `return true;`, add:
   ```csharp
   if (!string.Equals(previousOwner, newOwnerFactionId, StringComparison.Ordinal))
   {
       ZoneOwnershipChanged?.Invoke(this,
           new ZoneOwnershipChangedEventArgs(zoneId, previousOwner, newOwnerFactionId));
   }
   ```

- [ ] **Step 6: Run tests, verify pass**

Run: `dotnet test FactionWars.sln --filter "FullyQualifiedName~ZoneServiceTests"`
Expected: All ZoneServiceTests pass, including the 3 new ones.

- [ ] **Step 7: Run full test suite to confirm no regressions**

Run: `dotnet test FactionWars.sln`
Expected: All tests pass.

- [ ] **Step 8: Commit**

```bash
git add src/FactionWars/Territory/Events src/FactionWars/Territory/Interfaces/IZoneService.cs src/FactionWars/Territory/Services/ZoneService.cs tests/FactionWars.Tests/Unit/Territory/ZoneServiceTests.cs
git commit -m "feat(territory): add ZoneOwnershipChanged event

Raised by TransferZoneOwnership when the owner actually changes (no-op
transfers do not raise). Telemetry and any other interested party can
subscribe to this single domain event instead of imperatively calling
notifiers from every callsite.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 9: OnTroopsRecruited event on AIController

**Files:**
- Create: `src/FactionWars/AI/Events/TroopsRecruitedEventArgs.cs`
- Modify: `src/FactionWars/AI/Controllers/AIController.cs` (add event + raise)
- Test: `tests/FactionWars.Tests/Unit/AI/AIControllerTests.cs` (extend)

- [ ] **Step 1: Find the recruitment site in AIController**

Run: `grep -nE "Recruitment|recruited|RecruitmentPoints" src/FactionWars/AI/Controllers/AIController.cs | head -20`

Identify the per-faction recruitment block — typically a method like `RunRecruitmentForFaction` or inside `RunAITicks`. The event must be raised at the end of that block, with `cashBefore` captured at the start and `cashAfter` after the cash deduction.

- [ ] **Step 2: Create TroopsRecruitedEventArgs.cs**

```csharp
using System;

namespace FactionWars.AI.Events
{
    public sealed class TroopsRecruitedEventArgs : EventArgs
    {
        public string FactionId { get; }
        public int TroopsRecruited { get; }
        public int Cost { get; }
        public int CashBefore { get; }
        public int CashAfter { get; }

        public TroopsRecruitedEventArgs(string factionId, int troopsRecruited, int cost,
            int cashBefore, int cashAfter)
        {
            FactionId = factionId ?? throw new ArgumentNullException(nameof(factionId));
            TroopsRecruited = troopsRecruited;
            Cost = cost;
            CashBefore = cashBefore;
            CashAfter = cashAfter;
        }
    }
}
```

- [ ] **Step 3: Write failing test**

Find or create `tests/FactionWars.Tests/Unit/AI/AIControllerTests.cs`. Add:

```csharp
[Fact]
public void RunRecruitment_WhenTroopsRecruited_RaisesEvent()
{
    // Arrange: a faction with cash sufficient to recruit some troops.
    // Wire the controller against a faction service mock so RunRecruitment
    // ends up actually recruiting > 0 troops.

    TroopsRecruitedEventArgs? captured = null;
    _controller.OnTroopsRecruited += (_, args) => captured = args;

    _controller.RunAITicks(); // or whichever public entry runs recruitment

    Assert.NotNull(captured);
    Assert.True(captured!.TroopsRecruited > 0);
    Assert.True(captured.Cost > 0);
    Assert.Equal(captured.CashBefore - captured.Cost, captured.CashAfter);
}

[Fact]
public void RunRecruitment_WhenNoTroopsRecruited_DoesNotRaiseEvent()
{
    // Arrange: faction with $0 cash → no recruitment possible.
    bool raised = false;
    _controller.OnTroopsRecruited += (_, _) => raised = true;

    _controller.RunAITicks();

    Assert.False(raised);
}
```

NOTE: Exact wiring depends on existing AIControllerTests setup. Adapt to existing patterns. Add `using FactionWars.AI.Events;`.

- [ ] **Step 4: Run test, verify failure**

Run: `dotnet test FactionWars.sln --filter "FullyQualifiedName~AIControllerTests.RunRecruitment_WhenTroopsRecruited_RaisesEvent"`
Expected: Compilation failure — `OnTroopsRecruited` does not exist.

- [ ] **Step 5: Add event and raise it**

In `AIController.cs`:

1. Add `using FactionWars.AI.Events;` at top.
2. Add event declaration near other public events:
   ```csharp
   public event EventHandler<TroopsRecruitedEventArgs>? OnTroopsRecruited;
   ```
3. At the recruitment site, capture `int cashBefore = state.Cash;` before recruitment and `int cashAfter = state.Cash;` after, plus `int troopsRecruited` and `int cost` already computed in that block. Then raise:
   ```csharp
   if (troopsRecruited > 0)
   {
       OnTroopsRecruited?.Invoke(this, new TroopsRecruitedEventArgs(
           factionId, troopsRecruited, cost, cashBefore, cashAfter));
   }
   ```

- [ ] **Step 6: Run tests, verify pass**

Run: `dotnet test FactionWars.sln --filter "FullyQualifiedName~AIControllerTests"`
Expected: All AIControllerTests pass, including new ones.

- [ ] **Step 7: Run full test suite to confirm no regressions**

Run: `dotnet test FactionWars.sln`
Expected: All tests pass.

- [ ] **Step 8: Commit**

```bash
git add src/FactionWars/AI/Events src/FactionWars/AI/Controllers/AIController.cs tests/FactionWars.Tests/Unit/AI/AIControllerTests.cs
git commit -m "feat(ai): add OnTroopsRecruited event on AIController

Raised after a successful recruitment cycle for a faction. Args expose
cash before/after, troops recruited, and cost so telemetry can record a
complete recruitment row.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 10: TelemetryService — snapshot timer

The first slice of the orchestrator: a 60s timer that builds snapshots and writes them to the sink.

**Files:**
- Create: `src/FactionWars/Telemetry/Services/TelemetryService.cs`
- Test: `tests/FactionWars.Tests/Unit/Telemetry/TelemetryServiceTests.cs`

- [ ] **Step 1: Write failing test**

```csharp
using System;
using System.Collections.Generic;
using FactionWars.Factions.Interfaces;
using FactionWars.Factions.Models;
using FactionWars.ScriptHookV.Persistence;
using FactionWars.Telemetry.Interfaces;
using FactionWars.Telemetry.Models;
using FactionWars.Telemetry.Services;
using FactionWars.Territory.Interfaces;
using Moq;
using Xunit;

namespace FactionWars.Tests.Unit.Telemetry
{
    public class TelemetryServiceTests
    {
        private readonly Mock<ITelemetrySink> _sink = new();
        private readonly Mock<IFactionService> _factionService = new();
        private readonly Mock<IZoneService> _zoneService = new();
        private readonly Mock<IGameStateManager> _gameStateManager = new();

        public TelemetryServiceTests()
        {
            _factionService.Setup(s => s.GetAllFactions()).Returns(System.Array.Empty<Faction>());
            _gameStateManager.Setup(g => g.TotalPlayTimeSeconds).Returns(0);
        }

        [Fact]
        public void Tick_BuildsSnapshotAndWritesToSink()
        {
            var michael = new Faction("michael", "Michael's Crew");
            _factionService.Setup(s => s.GetAllFactions()).Returns(new[] { michael });
            _factionService.Setup(s => s.GetFactionState("michael"))
                .Returns(new FactionState("michael") { Cash = 500 });
            _zoneService.Setup(z => z.GetZoneCount("michael")).Returns(8);
            _gameStateManager.Setup(g => g.TotalPlayTimeSeconds).Returns(123L);

            using var svc = new TelemetryService(_sink.Object, _factionService.Object,
                _zoneService.Object, _gameStateManager.Object);

            svc.Tick(); // public test entry that bypasses the timer

            _sink.Verify(s => s.WriteSnapshot(It.Is<IReadOnlyList<FactionSnapshot>>(rows =>
                rows.Count == 1 && rows[0].FactionId == "michael" && rows[0].Cash == 500
                && rows[0].PlayTimeSeconds == 123L)), Times.Once);
        }

        [Fact]
        public void Dispose_StopsTimerAndIsIdempotent()
        {
            var svc = new TelemetryService(_sink.Object, _factionService.Object,
                _zoneService.Object, _gameStateManager.Object);
            svc.Dispose();
            svc.Dispose(); // must not throw
        }
    }
}
```

- [ ] **Step 2: Run test, verify failure**

Run: `dotnet test FactionWars.sln --filter "FullyQualifiedName~TelemetryServiceTests"`
Expected: Compilation failure — `TelemetryService` does not exist.

- [ ] **Step 3: Implement TelemetryService.cs (snapshot-only version)**

```csharp
using System;
using System.Threading;
using FactionWars.Factions.Interfaces;
using FactionWars.ScriptHookV.Logging;
using FactionWars.ScriptHookV.Persistence;
using FactionWars.Telemetry.Interfaces;
using FactionWars.Territory.Interfaces;

namespace FactionWars.Telemetry.Services
{
    /// <summary>
    /// Orchestrates telemetry: drives a periodic snapshot timer and (in later tasks)
    /// subscribes to domain events, routing everything to the sink.
    /// </summary>
    public sealed class TelemetryService : IDisposable
    {
        private const int SnapshotIntervalMs = 60_000;

        private readonly ITelemetrySink _sink;
        private readonly IGameStateManager _gameStateManager;
        private readonly FactionSnapshotBuilder _snapshotBuilder;
        private readonly Timer _timer;
        private bool _disposed;

        public TelemetryService(ITelemetrySink sink,
            IFactionService factionService,
            IZoneService zoneService,
            IGameStateManager gameStateManager)
        {
            _sink = sink ?? throw new ArgumentNullException(nameof(sink));
            _gameStateManager = gameStateManager ?? throw new ArgumentNullException(nameof(gameStateManager));
            _snapshotBuilder = new FactionSnapshotBuilder(
                factionService ?? throw new ArgumentNullException(nameof(factionService)),
                zoneService ?? throw new ArgumentNullException(nameof(zoneService)));

            _timer = new Timer(_ => SafeTick(), null, SnapshotIntervalMs, SnapshotIntervalMs);
        }

        /// <summary>Public entry for tests — bypasses the timer.</summary>
        public void Tick() => SafeTick();

        private void SafeTick()
        {
            if (_disposed) return;
            try
            {
                var rows = _snapshotBuilder.Build(DateTime.Now, _gameStateManager.TotalPlayTimeSeconds);
                if (rows.Count > 0) _sink.WriteSnapshot(rows);
            }
            catch (Exception ex)
            {
                FileLogger.Error("TelemetryService: snapshot tick failed", ex);
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            try { _timer.Dispose(); } catch { }
        }
    }
}
```

- [ ] **Step 4: Run tests, verify pass**

Run: `dotnet test FactionWars.sln --filter "FullyQualifiedName~TelemetryServiceTests"`
Expected: 2/2 pass.

- [ ] **Step 5: Commit**

```bash
git add src/FactionWars/Telemetry/Services/TelemetryService.cs tests/FactionWars.Tests/Unit/Telemetry/TelemetryServiceTests.cs
git commit -m "feat(telemetry): add TelemetryService with snapshot timer

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 11: TelemetryService — domain event subscriptions

Add subscriptions to existing domain events: zone ownership, battles, AI decisions, troop allocations, recruitment, resource ticks, attacker kills, save events, victory, difficulty.

**Files:**
- Modify: `src/FactionWars/Telemetry/Services/TelemetryService.cs`
- Modify: `tests/FactionWars.Tests/Unit/Telemetry/TelemetryServiceTests.cs`

- [ ] **Step 1: Write failing tests for the new subscriptions**

Add to `TelemetryServiceTests.cs`:

```csharp
[Fact]
public void ZoneOwnershipChanged_ToOwner_WritesCapturedAndLost()
{
    var zoneService = new Mock<IZoneService>();
    using var svc = new TelemetryService(_sink.Object, _factionService.Object,
        zoneService.Object, _gameStateManager.Object);

    zoneService.Raise(z => z.ZoneOwnershipChanged += null,
        new ZoneOwnershipChangedEventArgs("morningwood", "trevor", "michael"));

    _sink.Verify(s => s.WriteZoneEvent(It.Is<ZoneEventRow>(r =>
        r.Type == ZoneEventType.Captured && r.ZoneId == "morningwood"
        && r.PreviousOwner == "trevor" && r.NewOwner == "michael")), Times.Once);
    _sink.Verify(s => s.WriteZoneEvent(It.Is<ZoneEventRow>(r =>
        r.Type == ZoneEventType.Lost && r.ZoneId == "morningwood"
        && r.PreviousOwner == "trevor" && r.NewOwner == "michael")), Times.Once);
}

[Fact]
public void ZoneOwnershipChanged_ToNeutral_WritesNeutralized()
{
    var zoneService = new Mock<IZoneService>();
    using var svc = new TelemetryService(_sink.Object, _factionService.Object,
        zoneService.Object, _gameStateManager.Object);

    zoneService.Raise(z => z.ZoneOwnershipChanged += null,
        new ZoneOwnershipChangedEventArgs("morningwood", "trevor", null));

    _sink.Verify(s => s.WriteZoneEvent(It.Is<ZoneEventRow>(r =>
        r.Type == ZoneEventType.Neutralized && r.NewOwner == null)), Times.Once);
}

[Fact]
public void OnGameLoaded_ForwardsSetSaveFile()
{
    var gsm = new Mock<IGameStateManager>();
    gsm.Setup(g => g.TotalPlayTimeSeconds).Returns(0);
    using var svc = new TelemetryService(_sink.Object, _factionService.Object,
        _zoneService.Object, gsm.Object);

    gsm.Raise(g => g.OnGameLoaded += null,
        new GameStateLoadedEventArgs(0, "SGTA0004", true)); // adapt to actual ctor

    _sink.Verify(s => s.SetSaveFile("SGTA0004"), Times.Once);
}
```

NOTE: The exact constructor of `GameStateLoadedEventArgs` should match the existing one in `IGameStateManager.cs`. Adapt the test arg order accordingly.

Add corresponding tests for `OnZoneBattleStarted`, `OnZoneBattleEnded`, `OnAIDecision`, `TroopsAllocated`, `OnTroopsRecruited`, `OnResourceTick`, `AttackerKilled` (player kill), `OnVictory`, `DifficultyChanged`. Each follows the same Mock-and-Raise pattern. For `AttackerKilled`, the player ped handle must be obtainable — pass a player-kills-resolver or `Func<int>` for the player ped through the ctor (see Step 3 for service signature).

- [ ] **Step 2: Run tests, verify failure**

Run: `dotnet test FactionWars.sln --filter "FullyQualifiedName~TelemetryServiceTests"`
Expected: Compilation failure — TelemetryService does not accept the new dependencies / subscriptions.

- [ ] **Step 3: Update TelemetryService.cs to accept and subscribe to event sources**

Expand the constructor:

```csharp
public TelemetryService(ITelemetrySink sink,
    IFactionService factionService,
    IZoneService zoneService,
    IGameStateManager gameStateManager,
    Func<int> getPlayerPedHandle,
    IZoneBattleManager? zoneBattleManager = null,
    IAIManager? aiManager = null,
    IAIController? aiController = null,
    IZoneDefenderAllocationService? allocationService = null,
    IResourceTickService? resourceTickService = null,
    IBattleAttackerManager? battleAttackerManager = null,
    IVictoryManager? victoryManager = null,
    IDifficultyService? difficultyService = null,
    INativeSaveWatcher? nativeSaveWatcher = null)
```

Each non-null dependency triggers a subscription in the constructor body. Each handler maps the event args to a telemetry DTO and forwards to `_sink`. Example handler bodies:

```csharp
zoneService.ZoneOwnershipChanged += OnZoneOwnershipChanged;

private void OnZoneOwnershipChanged(object? sender, ZoneOwnershipChangedEventArgs e)
{
    var ts = DateTime.Now;
    var pt = _gameStateManager.TotalPlayTimeSeconds;

    if (e.NewOwner == null)
    {
        _sink.WriteZoneEvent(new ZoneEventRow(ts, pt, ZoneEventType.Neutralized,
            e.ZoneId, e.PreviousOwner, null));
    }
    else
    {
        _sink.WriteZoneEvent(new ZoneEventRow(ts, pt, ZoneEventType.Captured,
            e.ZoneId, e.PreviousOwner, e.NewOwner));
        if (e.PreviousOwner != null)
        {
            _sink.WriteZoneEvent(new ZoneEventRow(ts, pt, ZoneEventType.Lost,
                e.ZoneId, e.PreviousOwner, e.NewOwner));
        }
    }
}
```

Repeat for all event sources. For `AttackerKilled`:

```csharp
private void OnAttackerKilled(object? sender, AttackerKilledEventArgs e)
{
    var row = PlayerKillResolver.Resolve(e, _getPlayerPedHandle(),
        DateTime.Now, _gameStateManager.TotalPlayTimeSeconds);
    if (row != null) _sink.WritePlayerEvent(row);
}
```

For battle started/ended map to `BattleEventRow`. For AI decision map `AIDecision.Type` enum to `AIDecisionTypeMeta`. For troop allocations map `TroopsAllocatedEventArgs` to `AllocationEventRow` with `AllocationSource.AI` if from AI executor or `Player` if from manual UI flow — use the `EventArgs.Source` field if present, else default `AI`.

In `Dispose`, unsubscribe from each event handler that was wired.

NOTE: If any dependency interface (e.g., `IZoneBattleManager`, `IBattleAttackerManager`) doesn't exist yet, use the concrete type. The point is that non-null dependencies get subscribed; the point is NOT to add new interfaces.

- [ ] **Step 4: Run tests, verify pass**

Run: `dotnet test FactionWars.sln --filter "FullyQualifiedName~TelemetryServiceTests"`
Expected: All TelemetryServiceTests pass.

- [ ] **Step 5: Run full test suite to confirm no regressions**

Run: `dotnet test FactionWars.sln`
Expected: All tests pass.

- [ ] **Step 6: Commit**

```bash
git add src/FactionWars/Telemetry/Services/TelemetryService.cs tests/FactionWars.Tests/Unit/Telemetry/TelemetryServiceTests.cs
git commit -m "feat(telemetry): subscribe to domain events in TelemetryService

Wires zone ownership, battles, AI decisions, troop allocations,
recruitment, resource ticks, attacker kills, save events, victory, and
difficulty changes to their respective telemetry sink methods.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 12: TelemetryService — match meta and mod-session lifecycle

Emit `ModSessionStart` on construction, `ModSessionEnd` on dispose, and `MatchStart` on the first `OnGameLoaded` for a never-before-seen save filename. `Victory`/`Defeat`/`DifficultyChanged` already wired in Task 11.

**Files:**
- Modify: `src/FactionWars/Telemetry/Services/TelemetryService.cs`
- Modify: `tests/FactionWars.Tests/Unit/Telemetry/TelemetryServiceTests.cs`

- [ ] **Step 1: Write failing test**

```csharp
[Fact]
public void Constructor_EmitsModSessionStart()
{
    using var svc = new TelemetryService(/* ... */);
    _sink.Verify(s => s.WriteMatchMeta(It.Is<MatchMetaEventRow>(r =>
        r.Type == MatchMetaEventType.ModSessionStart)), Times.Once);
}

[Fact]
public void Dispose_EmitsModSessionEnd()
{
    var svc = new TelemetryService(/* ... */);
    svc.Dispose();
    _sink.Verify(s => s.WriteMatchMeta(It.Is<MatchMetaEventRow>(r =>
        r.Type == MatchMetaEventType.ModSessionEnd)), Times.Once);
}

[Fact]
public void FirstOnGameLoadedForNewSave_EmitsMatchStart()
{
    var gsm = new Mock<IGameStateManager>();
    gsm.Setup(g => g.TotalPlayTimeSeconds).Returns(0);
    using var svc = new TelemetryService(/* ... gsm ... */);

    // Simulate the per-save Telemetry folder NOT existing yet by injecting a
    // tracker — see implementation note in Step 3. For unit test, we ask the
    // service to consult an injected predicate Func<string,bool> isNewSave.

    gsm.Raise(g => g.OnGameLoaded += null, new GameStateLoadedEventArgs(0, "SGTA0009", true));

    _sink.Verify(s => s.WriteMatchMeta(It.Is<MatchMetaEventRow>(r =>
        r.Type == MatchMetaEventType.MatchStart && r.Details.Contains("SGTA0009"))), Times.Once);
}
```

- [ ] **Step 2: Run tests, verify failure**

Run: `dotnet test FactionWars.sln --filter "FullyQualifiedName~TelemetryServiceTests"`
Expected: failures (events not yet emitted).

- [ ] **Step 3: Implement match meta emission**

In `TelemetryService` constructor, after sink wiring but before timer start:

```csharp
_sink.WriteMatchMeta(new MatchMetaEventRow(
    DateTime.Now, _gameStateManager.TotalPlayTimeSeconds,
    MatchMetaEventType.ModSessionStart, string.Empty));
```

In `Dispose`, before timer disposal:

```csharp
try
{
    _sink.WriteMatchMeta(new MatchMetaEventRow(
        DateTime.Now, _gameStateManager.TotalPlayTimeSeconds,
        MatchMetaEventType.ModSessionEnd, string.Empty));
}
catch (Exception ex) { FileLogger.Error("TelemetryService: ModSessionEnd write failed", ex); }
```

For `MatchStart` detection, accept a `Func<string, bool> isFirstTimeSeenSave` ctor parameter (default = checks if `Documents\FactionWars\Telemetry\<saveFilename>\` directory exists; first-time iff it does NOT exist). In `OnGameLoaded`:

```csharp
private void OnGameLoaded(object? sender, GameStateLoadedEventArgs e)
{
    var save = e.SaveName; // adapt to actual property name
    _sink.SetSaveFile(save);
    if (_isFirstTimeSeenSave(save))
    {
        _sink.WriteMatchMeta(new MatchMetaEventRow(
            DateTime.Now, _gameStateManager.TotalPlayTimeSeconds,
            MatchMetaEventType.MatchStart, $"save={save}"));
    }
}
```

The `isFirstTimeSeenSave` predicate is a real predicate provided by `FactionWarsScript` (real impl checks directory existence under `Documents\FactionWars\Telemetry`). Tests pass a stub.

- [ ] **Step 4: Run tests, verify pass**

Run: `dotnet test FactionWars.sln --filter "FullyQualifiedName~TelemetryServiceTests"`
Expected: All TelemetryServiceTests pass.

- [ ] **Step 5: Commit**

```bash
git add src/FactionWars/Telemetry/Services/TelemetryService.cs tests/FactionWars.Tests/Unit/Telemetry/TelemetryServiceTests.cs
git commit -m "feat(telemetry): emit ModSessionStart/End and MatchStart events

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 13: ServiceContainerFactory + FactionWarsScript wiring

Register `ITelemetrySink` (default: `CsvTelemetrySink`) and `TelemetryService` in the DI container, and instantiate/dispose `TelemetryService` from `FactionWarsScript` alongside other services.

**Files:**
- Modify: `src/FactionWars/ScriptHookV/ServiceContainerFactory.cs`
- Modify: `src/FactionWars/ScriptHookV/FactionWarsScript.cs`

This task has no new unit tests — it's pure wiring. The integration test in Task 14 will exercise it end-to-end.

- [ ] **Step 1: Register the sink in ServiceContainerFactory**

In `ServiceContainerFactory.cs`, find `RegisterPersistenceServices` (or the most appropriate `Register...Services` method) and add at the end:

```csharp
private static void RegisterTelemetryServices(ServiceContainer container)
{
    var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    var telemetryRoot = Path.Combine(documentsPath, "FactionWars", "Telemetry");

    var sink = new CsvTelemetrySink(telemetryRoot);
    container.Register<ITelemetrySink>(sink);
}
```

Add the call in `Create(...)`:

```csharp
RegisterTelemetryServices(container);
```

Add usings at top: `using FactionWars.Telemetry.Interfaces; using FactionWars.Telemetry.Sinks;`.

- [ ] **Step 2: Instantiate and dispose TelemetryService in FactionWarsScript**

In `FactionWarsScript.cs`:

1. Add field: `private TelemetryService? _telemetryService;`
2. In the constructor (after other services are resolved), construct the TelemetryService with all available dependencies. The factory pattern is: for each optional dependency, pass either the resolved instance from the container or null. Provide `getPlayerPedHandle` as a `Func<int>` that reads the current player ped via `_gameBridge`. Provide `isFirstTimeSeenSave` as a predicate that checks `Path.Combine(telemetryRoot, saveName)` does not exist on disk.
3. In `OnAborted` (or wherever existing services are disposed), call `_telemetryService?.Dispose();`.

```csharp
// Construction (inside ctor or Init):
var telemetryRoot = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
    "FactionWars", "Telemetry");
var sink = _container.Get<ITelemetrySink>();
_telemetryService = new TelemetryService(
    sink,
    _factionService,
    _zoneService,
    _gameStateManager,
    getPlayerPedHandle: () => _gameBridge.GetPlayerPedHandle(),
    isFirstTimeSeenSave: save => !Directory.Exists(Path.Combine(telemetryRoot, save)),
    zoneBattleManager: _zoneBattleManager,
    aiManager: _aiManager,
    aiController: _aiController,
    allocationService: _zoneDefenderAllocationService,
    resourceTickService: _resourceTickService,
    battleAttackerManager: _battleAttackerManager,
    victoryManager: _victoryManager,
    difficultyService: _difficultyService,
    nativeSaveWatcher: _nativeSaveWatcher);
```

Adapt parameter names exactly to the resolved field names in `FactionWarsScript`. If `IGameBridge.GetPlayerPedHandle()` does not yet exist, use the existing equivalent (e.g., `Game.Player.Character.Handle`) or extend the bridge interface.

- [ ] **Step 3: Build**

Run: `dotnet build FactionWars.sln`
Expected: Build succeeded, 0 errors.

- [ ] **Step 4: Run full test suite to confirm no regressions**

Run: `dotnet test FactionWars.sln`
Expected: All tests pass.

- [ ] **Step 5: Commit**

```bash
git add src/FactionWars/ScriptHookV/ServiceContainerFactory.cs src/FactionWars/ScriptHookV/FactionWarsScript.cs
git commit -m "feat(telemetry): wire TelemetryService into mod lifecycle

Registers CsvTelemetrySink in the DI container and instantiates
TelemetryService alongside other domain services in FactionWarsScript.
Telemetry now starts emitting on mod load and stops on abort.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 14: End-to-end integration test

Wire real `TelemetryService` + real `CsvTelemetrySink` (writing to a temp dir) + mocked domain services. Drive a scripted scenario and verify CSV files on disk.

**Files:**
- Create: `tests/FactionWars.Tests/Integration/Telemetry/TelemetryEndToEndTests.cs`

- [ ] **Step 1: Write the integration test**

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FactionWars.Combat.Events;
using FactionWars.Core.Models;
using FactionWars.Factions.Interfaces;
using FactionWars.Factions.Models;
using FactionWars.ScriptHookV.Persistence;
using FactionWars.Telemetry.Models;
using FactionWars.Telemetry.Services;
using FactionWars.Telemetry.Sinks;
using FactionWars.Territory.Events;
using FactionWars.Territory.Interfaces;
using Moq;
using Xunit;

namespace FactionWars.Tests.Integration.Telemetry
{
    public class TelemetryEndToEndTests : IDisposable
    {
        private readonly string _tempDir;
        private readonly CsvTelemetrySink _sink;

        public TelemetryEndToEndTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "fw_tel_e2e_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
            _sink = new CsvTelemetrySink(_tempDir);
        }

        public void Dispose()
        {
            _sink.Dispose();
            try { Directory.Delete(_tempDir, recursive: true); } catch { }
        }

        [Fact]
        public void FullScenario_WritesAllExpectedCsvFiles()
        {
            // Arrange: real TelemetryService with mocked event sources.
            var factionService = new Mock<IFactionService>();
            var zoneService = new Mock<IZoneService>();
            var gameStateManager = new Mock<IGameStateManager>();

            var michael = new Faction("michael", "Michael's Crew");
            factionService.Setup(s => s.GetAllFactions()).Returns(new[] { michael });
            factionService.Setup(s => s.GetFactionState("michael"))
                .Returns(new FactionState("michael") { Cash = 500 });
            zoneService.Setup(z => z.GetZoneCount("michael")).Returns(8);
            gameStateManager.Setup(g => g.TotalPlayTimeSeconds).Returns(100L);

            using var svc = new TelemetryService(_sink, factionService.Object, zoneService.Object,
                gameStateManager.Object,
                getPlayerPedHandle: () => 99,
                isFirstTimeSeenSave: _ => true,
                zoneBattleManager: null, aiManager: null, aiController: null,
                allocationService: null, resourceTickService: null,
                battleAttackerManager: null, victoryManager: null,
                difficultyService: null, nativeSaveWatcher: null);

            // Act: drive a scenario.
            svc.Tick(); // emits a snapshot before save filename is known (buffered)
            gameStateManager.Raise(g => g.OnGameLoaded += null,
                new GameStateLoadedEventArgs(0, "SGTA0099", true));
            zoneService.Raise(z => z.ZoneOwnershipChanged += null,
                new ZoneOwnershipChangedEventArgs("morningwood", "trevor", "michael"));

            // Assert: files exist in the per-save directory and contain expected rows.
            var saveDir = Path.Combine(_tempDir, "SGTA0099");
            Assert.True(Directory.Exists(saveDir));

            var snapPath = Path.Combine(saveDir, "snapshots.csv");
            Assert.True(File.Exists(snapPath));
            var snapLines = File.ReadAllLines(snapPath);
            Assert.True(snapLines.Length >= 2); // header + at least one row
            Assert.Contains(snapLines.Skip(1), l => l.Contains("michael") && l.Contains("500"));

            var zonePath = Path.Combine(saveDir, "zone_events.csv");
            Assert.True(File.Exists(zonePath));
            var zoneLines = File.ReadAllLines(zonePath);
            Assert.True(zoneLines.Length >= 3); // header + Captured + Lost
            Assert.Contains(zoneLines.Skip(1), l => l.Contains("Captured") && l.Contains("morningwood"));
            Assert.Contains(zoneLines.Skip(1), l => l.Contains("Lost") && l.Contains("morningwood"));

            var metaPath = Path.Combine(saveDir, "match_meta.csv");
            Assert.True(File.Exists(metaPath));
            var metaLines = File.ReadAllLines(metaPath);
            Assert.Contains(metaLines.Skip(1), l => l.Contains("ModSessionStart"));
            Assert.Contains(metaLines.Skip(1), l => l.Contains("MatchStart"));
        }
    }
}
```

- [ ] **Step 2: Run the integration test**

Run: `dotnet test FactionWars.sln --filter "FullyQualifiedName~TelemetryEndToEndTests"`
Expected: 1/1 pass.

- [ ] **Step 3: Run full test suite to confirm no regressions**

Run: `dotnet test FactionWars.sln`
Expected: All tests pass.

- [ ] **Step 4: Commit**

```bash
git add tests/FactionWars.Tests/Integration/Telemetry/TelemetryEndToEndTests.cs
git commit -m "test(telemetry): add end-to-end integration test

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 15: Manual in-game verification

Build, deploy to GTA V, play a session, inspect the CSV files.

- [ ] **Step 1: Deploy the DLL**

```bash
dotnet build FactionWars.sln -c Debug
cp "src/FactionWars/bin/Debug/net48/FactionWars.dll" "/e/SteamLibrary/steamapps/common/Grand Theft Auto V/scripts/"
```

Expected: copy succeeds.

- [ ] **Step 2: Launch GTA V and load a save**

The user runs the game and loads a save. Play for a few minutes — engage a battle, capture a zone, let the AI take a turn.

- [ ] **Step 3: Inspect the per-save telemetry directory**

```bash
ls "/c/Users/ryan7/Documents/FactionWars/Telemetry/"
ls "/c/Users/ryan7/Documents/FactionWars/Telemetry/SGTA000X/"
head -5 "/c/Users/ryan7/Documents/FactionWars/Telemetry/SGTA000X/snapshots.csv"
head -5 "/c/Users/ryan7/Documents/FactionWars/Telemetry/SGTA000X/zone_events.csv"
head -5 "/c/Users/ryan7/Documents/FactionWars/Telemetry/SGTA000X/battles.csv"
head -5 "/c/Users/ryan7/Documents/FactionWars/Telemetry/SGTA000X/match_meta.csv"
```

Expected:
- A directory exists named after the loaded save (e.g., `SGTA0004`)
- `snapshots.csv` has rows for **all** active factions including `michael`
- `zone_events.csv` has rows matching captures observed in-game
- `battles.csv` has Started + Ended rows for each battle that occurred
- `match_meta.csv` has at least one `ModSessionStart` row

- [ ] **Step 4: If anything is missing or wrong, file a follow-up issue**

Open `Documents\FactionWars\Logs\FactionWars_<latest>.log` and search for `TelemetryService:` or `CsvTelemetrySink:` for any error lines. Address the underlying cause.

- [ ] **Step 5: Commit any final fixes (if needed)**

```bash
git add <files>
git commit -m "fix(telemetry): <specific fix>

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Self-Review Checklist (already done by author)

**Spec coverage:**
- ✅ 9 CSVs (Tasks 1, 4 define structure; Task 11 + 12 wire all event sources to them)
- ✅ ITelemetrySink + Csv + Null impls (Tasks 2, 4)
- ✅ TelemetryService orchestrator (Tasks 10, 11, 12)
- ✅ 60s snapshot timer (Task 10)
- ✅ 3 new domain events: ZoneOwnershipChanged, OnTroopsRecruited, AttackerKilled (Tasks 6, 8, 9)
- ✅ Buffer-then-flush save filename handoff (Task 4 implementation)
- ✅ Service container registration (Task 13)
- ✅ Match meta emission (Task 12)
- ✅ Player kill detection (Tasks 6, 7, 11)
- ✅ Tests for all logic (per-task)
- ✅ Integration test (Task 14)
- ✅ Manual verification (Task 15)

**Type consistency:** All DTOs declared in Task 1 are referenced consistently in Tasks 4 (sink), 5 (builder), 7 (kill resolver), 10–12 (service). Method names: `WriteSnapshot`, `WriteZoneEvent`, `WriteBattle`, `WriteDecision`, `WriteRecruitment`, `WriteAllocation`, `WriteResourceTick`, `WriteMatchMeta`, `WritePlayerEvent` consistent across interface (Task 2) and impl (Task 4). Enum values `ZoneEventType.{Captured, Lost, Neutralized}` referenced in Tasks 1, 4, 11.

**Placeholder scan:** No "TBD"/"TODO" left. The "adapt to existing patterns" notes in Tasks 6, 9, 11 are unavoidable references to existing test setup that varies per file — concrete code is shown for the new behavior.
