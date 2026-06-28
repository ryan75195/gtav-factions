# Stage 1: Crash Instrumentation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans (inline) to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax.

**Goal:** Add per-subsystem tick profiling so the next >5s blocking-script freeze names the exact subsystem, plus a SLOW TICK summary for before/after measurement.

**Architecture:** A portable `TickProfiler` (Performance namespace) times each named phase of a tick using the existing `ITimeProvider` clock and emits to an `ITickDiagnosticsSink`. The ScriptHookV `FileTickDiagnosticsSink` writes a flushed single-line breadcrumb file (independent of `FileLogger`) and routes SLOW TICK summaries to `FileLogger.Warn`. `GameLoopController.OnTick` brackets the tick with `BeginTick`/`EndTick`; `UpdateCoreSystems`/`UpdateWorldSystems` wrap each subsystem call in `Measure(name, body)`.

**Tech Stack:** C# .NET Framework 4.8, xUnit + Moq (existing test project).

## Global Constraints

- `.cs` files: CRLF, UTF-8 no BOM. Edit in place.
- Analyzer ERRORS: no tuple returns; ≤250 lines/class; ≤40 effective lines/method; one public top-level type per file; ≤5 ctor params; no `#pragma` for CA*/CI*.
- `Core`/`Performance` stay portable (no GTA refs). GTA/file-logging integration stays in `ScriptHookV`.
- TDD: failing test first. Pre-commit hook runs full build + `--filter FactionWars.Tests.Unit`. Never bypass.
- Reuse existing `FactionWars.Core.Interfaces.ITimeProvider` (`UtcNow`, `Now`) as the clock — do NOT add a new clock type.
- Commit footer: `Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>` + `Claude-Session: https://claude.ai/code/session_01MGyei1N5nCAgmp5DmiECej`.

---

## File Structure

- Create `src/FactionWars/Performance/Interfaces/ITickDiagnosticsSink.cs` — sink seam: `WriteBreadcrumb(string)`, `ReportSlowTick(string)`.
- Create `src/FactionWars/Performance/Models/TickProfilerOptions.cs` — thresholds (`BreadcrumbAfterMs`, `SlowTickMs`).
- Create `src/FactionWars/Performance/Services/TickProfiler.cs` — the profiler (`BeginTick`, `Measure`, `EndTick`).
- Create `src/FactionWars/ScriptHookV/Diagnostics/FileTickDiagnosticsSink.cs` — file/log sink.
- Modify `src/FactionWars/ScriptHookV/GameLoopController.Runtime.cs` — construct profiler in ctor; bracket `OnTick`.
- Modify `src/FactionWars/ScriptHookV/GameLoopController.SystemUpdates.cs` — wrap subsystem calls in `Measure`.
- Create tests under `tests/FactionWars.Tests/Unit/Performance/` and `tests/FactionWars.Tests/Unit/ScriptHookV/Diagnostics/`.

---

## Task 1: ITickDiagnosticsSink seam + TickProfilerOptions

**Files:**
- Create: `src/FactionWars/Performance/Interfaces/ITickDiagnosticsSink.cs`
- Create: `src/FactionWars/Performance/Models/TickProfilerOptions.cs`

No tests (pure declarations); covered via Task 2.

- [ ] **Step 1: Create the sink interface**

```csharp
namespace FactionWars.Performance.Interfaces
{
    /// <summary>
    /// Destination for tick-profiling diagnostics. The breadcrumb is a single
    /// last-write-wins record of the phase currently executing during a slow tick;
    /// the slow-tick report is a one-line per-phase breakdown emitted after the tick.
    /// </summary>
    public interface ITickDiagnosticsSink
    {
        /// <summary>Records the phase currently executing (overwrites the previous breadcrumb).</summary>
        void WriteBreadcrumb(string content);

        /// <summary>Reports a completed tick whose total exceeded the slow-tick threshold.</summary>
        void ReportSlowTick(string summary);
    }
}
```

- [ ] **Step 2: Create the options type**

```csharp
namespace FactionWars.Performance.Models
{
    /// <summary>
    /// Thresholds for <c>TickProfiler</c>. Defaults chosen against SHVDN's
    /// ScriptTimeoutThreshold=5000ms: breadcrumbs start well before the kill,
    /// and any tick over a second is worth a log line.
    /// </summary>
    public class TickProfilerOptions
    {
        /// <summary>Once a tick's elapsed time reaches this, write a breadcrumb before each remaining phase.</summary>
        public long BreadcrumbAfterMs { get; set; } = 1000;

        /// <summary>Report a SLOW TICK summary when the whole tick meets or exceeds this.</summary>
        public long SlowTickMs { get; set; } = 1000;
    }
}
```

- [ ] **Step 3: Build to confirm it compiles**

Run: `dotnet build FactionWars.sln --no-incremental`
Expected: Build succeeded, 0 errors.

- [ ] **Step 4: Commit**

```bash
git add src/FactionWars/Performance/Interfaces/ITickDiagnosticsSink.cs src/FactionWars/Performance/Models/TickProfilerOptions.cs
git commit -m "feat: add tick-diagnostics sink seam and profiler options (#69)"
```

---

## Task 2: TickProfiler core (TDD)

**Files:**
- Create: `src/FactionWars/Performance/Services/TickProfiler.cs`
- Test: `tests/FactionWars.Tests/Unit/Performance/TickProfilerTests.cs`

**Interfaces:**
- Consumes: `FactionWars.Core.Interfaces.ITimeProvider` (`DateTime UtcNow`), `ITickDiagnosticsSink`, `TickProfilerOptions`.
- Produces: `TickProfiler(ITimeProvider, ITickDiagnosticsSink, TickProfilerOptions)` with `void BeginTick()`, `void Measure(string phaseName, System.Action body)`, `void EndTick()`.

- [ ] **Step 1: Write the failing tests**

Create `tests/FactionWars.Tests/Unit/Performance/TickProfilerTests.cs`:

```csharp
using System;
using System.Collections.Generic;
using FactionWars.Core.Interfaces;
using FactionWars.Performance.Interfaces;
using FactionWars.Performance.Models;
using FactionWars.Performance.Services;
using Xunit;

namespace FactionWars.Tests.Unit.Performance
{
    public class TickProfilerTests
    {
        private sealed class FakeClock : ITimeProvider
        {
            private DateTime _now = new DateTime(2026, 6, 28, 0, 0, 0, DateTimeKind.Utc);
            public DateTime UtcNow => _now;
            public DateTime Now => _now;
            public void AdvanceMs(long ms) => _now = _now.AddMilliseconds(ms);
        }

        private sealed class FakeSink : ITickDiagnosticsSink
        {
            public List<string> Breadcrumbs { get; } = new List<string>();
            public List<string> SlowTicks { get; } = new List<string>();
            public void WriteBreadcrumb(string content) => Breadcrumbs.Add(content);
            public void ReportSlowTick(string summary) => SlowTicks.Add(summary);
        }

        private static TickProfiler Build(FakeClock clock, FakeSink sink, long breadcrumbAfterMs = 1000, long slowTickMs = 1000)
            => new TickProfiler(clock, sink, new TickProfilerOptions { BreadcrumbAfterMs = breadcrumbAfterMs, SlowTickMs = slowTickMs });

        [Fact]
        public void Measure_InvokesBody()
        {
            var clock = new FakeClock();
            var profiler = Build(clock, new FakeSink());
            var ran = false;
            profiler.BeginTick();
            profiler.Measure("phase", () => ran = true);
            Assert.True(ran);
        }

        [Fact]
        public void EndTick_ReportsSlowTick_WhenTotalMeetsThreshold()
        {
            var clock = new FakeClock();
            var sink = new FakeSink();
            var profiler = Build(clock, sink, slowTickMs: 1000);
            profiler.BeginTick();
            profiler.Measure("slow", () => clock.AdvanceMs(1200));
            profiler.EndTick();
            Assert.Single(sink.SlowTicks);
            Assert.Contains("slow", sink.SlowTicks[0]);
            Assert.Contains("1200", sink.SlowTicks[0]);
        }

        [Fact]
        public void EndTick_DoesNotReport_WhenTickFast()
        {
            var clock = new FakeClock();
            var sink = new FakeSink();
            var profiler = Build(clock, sink, slowTickMs: 1000);
            profiler.BeginTick();
            profiler.Measure("fast", () => clock.AdvanceMs(50));
            profiler.EndTick();
            Assert.Empty(sink.SlowTicks);
        }

        [Fact]
        public void Measure_WritesBreadcrumb_OnlyAfterCumulativeThreshold()
        {
            var clock = new FakeClock();
            var sink = new FakeSink();
            var profiler = Build(clock, sink, breadcrumbAfterMs: 1000);
            profiler.BeginTick();
            profiler.Measure("first", () => clock.AdvanceMs(1100)); // cumulative 0 entering -> no breadcrumb
            profiler.Measure("second", () => clock.AdvanceMs(10));  // cumulative 1100 entering -> breadcrumb
            Assert.Single(sink.Breadcrumbs);
            Assert.Contains("second", sink.Breadcrumbs[0]);
        }

        [Fact]
        public void Measure_RecordsPhase_EvenWhenBodyThrows()
        {
            var clock = new FakeClock();
            var sink = new FakeSink();
            var profiler = Build(clock, sink, slowTickMs: 1);
            profiler.BeginTick();
            Assert.Throws<InvalidOperationException>(() =>
                profiler.Measure("boom", () => { clock.AdvanceMs(5); throw new InvalidOperationException(); }));
            profiler.EndTick();
            Assert.Single(sink.SlowTicks);
            Assert.Contains("boom", sink.SlowTicks[0]);
        }
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~TickProfilerTests"`
Expected: FAIL — `TickProfiler` does not exist.

- [ ] **Step 3: Implement TickProfiler**

Create `src/FactionWars/Performance/Services/TickProfiler.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Text;
using FactionWars.Core.Interfaces;
using FactionWars.Performance.Interfaces;
using FactionWars.Performance.Models;

namespace FactionWars.Performance.Services
{
    /// <summary>
    /// Times each named phase of a game tick. Once a tick's elapsed time crosses
    /// the breadcrumb threshold it records the phase about to run (so a freeze names
    /// the culprit); on tick end it reports a per-phase breakdown if the tick was slow.
    /// Single-threaded: driven from the script thread only.
    /// </summary>
    public class TickProfiler
    {
        private readonly ITimeProvider _time;
        private readonly ITickDiagnosticsSink _sink;
        private readonly TickProfilerOptions _options;
        private readonly List<PhaseTiming> _phases = new List<PhaseTiming>();
        private DateTime _tickStart;

        public TickProfiler(ITimeProvider time, ITickDiagnosticsSink sink, TickProfilerOptions options)
        {
            _time = time ?? throw new ArgumentNullException(nameof(time));
            _sink = sink ?? throw new ArgumentNullException(nameof(sink));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public void BeginTick()
        {
            _phases.Clear();
            _tickStart = _time.UtcNow;
        }

        public void Measure(string phaseName, Action body)
        {
            if (body == null) throw new ArgumentNullException(nameof(body));

            long elapsedIntoTick = (long)(_time.UtcNow - _tickStart).TotalMilliseconds;
            if (elapsedIntoTick >= _options.BreadcrumbAfterMs)
                _sink.WriteBreadcrumb($"{phaseName} (tick+{elapsedIntoTick}ms)");

            var phaseStart = _time.UtcNow;
            try
            {
                body();
            }
            finally
            {
                long durationMs = (long)(_time.UtcNow - phaseStart).TotalMilliseconds;
                _phases.Add(new PhaseTiming(phaseName, durationMs));
            }
        }

        public void EndTick()
        {
            long totalMs = (long)(_time.UtcNow - _tickStart).TotalMilliseconds;
            if (totalMs >= _options.SlowTickMs)
                _sink.ReportSlowTick(FormatSummary(totalMs));
        }

        private string FormatSummary(long totalMs)
        {
            var sb = new StringBuilder();
            sb.Append("SLOW TICK total=").Append(totalMs).Append("ms |");
            foreach (var phase in _phases)
                sb.Append(' ').Append(phase.Name).Append('=').Append(phase.DurationMs).Append("ms");
            return sb.ToString();
        }

        private struct PhaseTiming
        {
            public PhaseTiming(string name, long durationMs)
            {
                Name = name;
                DurationMs = durationMs;
            }

            public string Name { get; }
            public long DurationMs { get; }
        }
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~TickProfilerTests"`
Expected: PASS, 5 tests.

- [ ] **Step 5: Commit**

```bash
git add src/FactionWars/Performance/Services/TickProfiler.cs tests/FactionWars.Tests/Unit/Performance/TickProfilerTests.cs
git commit -m "feat: add TickProfiler with per-phase timing and breadcrumb (#69)"
```

---

## Task 3: FileTickDiagnosticsSink (TDD)

**Files:**
- Create: `src/FactionWars/ScriptHookV/Diagnostics/FileTickDiagnosticsSink.cs`
- Test: `tests/FactionWars.Tests/Unit/ScriptHookV/Diagnostics/FileTickDiagnosticsSinkTests.cs`

**Interfaces:**
- Consumes: `ITickDiagnosticsSink`.
- Produces: `FileTickDiagnosticsSink(string breadcrumbFilePath, System.Action<string> slowTickLogger)`.

- [ ] **Step 1: Write the failing tests**

Create `tests/FactionWars.Tests/Unit/ScriptHookV/Diagnostics/FileTickDiagnosticsSinkTests.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using FactionWars.ScriptHookV.Diagnostics;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV.Diagnostics
{
    public class FileTickDiagnosticsSinkTests : IDisposable
    {
        private readonly string _path = Path.Combine(Path.GetTempPath(), "fw_breadcrumb_" + Guid.NewGuid().ToString("N") + ".txt");

        public void Dispose()
        {
            if (File.Exists(_path)) File.Delete(_path);
        }

        [Fact]
        public void WriteBreadcrumb_WritesContentToFile()
        {
            var sink = new FileTickDiagnosticsSink(_path, _ => { });
            sink.WriteBreadcrumb("phaseA (tick+1200ms)");
            Assert.Equal("phaseA (tick+1200ms)", File.ReadAllText(_path));
        }

        [Fact]
        public void WriteBreadcrumb_OverwritesPreviousContent()
        {
            var sink = new FileTickDiagnosticsSink(_path, _ => { });
            sink.WriteBreadcrumb("first");
            sink.WriteBreadcrumb("second");
            Assert.Equal("second", File.ReadAllText(_path));
        }

        [Fact]
        public void ReportSlowTick_InvokesLogger()
        {
            var captured = new List<string>();
            var sink = new FileTickDiagnosticsSink(_path, captured.Add);
            sink.ReportSlowTick("SLOW TICK total=1200ms");
            Assert.Single(captured);
            Assert.Equal("SLOW TICK total=1200ms", captured[0]);
        }
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~FileTickDiagnosticsSinkTests"`
Expected: FAIL — `FileTickDiagnosticsSink` does not exist.

- [ ] **Step 3: Implement the sink**

Create `src/FactionWars/ScriptHookV/Diagnostics/FileTickDiagnosticsSink.cs`:

```csharp
using System;
using System.IO;
using FactionWars.Performance.Interfaces;

namespace FactionWars.ScriptHookV.Diagnostics
{
    /// <summary>
    /// File-backed tick diagnostics. The breadcrumb is written last-write-wins to a
    /// dedicated file (independent of FileLogger) and flushed immediately, so it survives
    /// a SHVDN blocking-script abort (the game process keeps running). Slow-tick summaries
    /// are routed to an injected logger (wired to FileLogger.Warn in production).
    /// </summary>
    public class FileTickDiagnosticsSink : ITickDiagnosticsSink
    {
        private readonly string _breadcrumbFilePath;
        private readonly Action<string> _slowTickLogger;

        public FileTickDiagnosticsSink(string breadcrumbFilePath, Action<string> slowTickLogger)
        {
            _breadcrumbFilePath = breadcrumbFilePath ?? throw new ArgumentNullException(nameof(breadcrumbFilePath));
            _slowTickLogger = slowTickLogger ?? throw new ArgumentNullException(nameof(slowTickLogger));
        }

        /// <inheritdoc />
        public void WriteBreadcrumb(string content)
        {
            try
            {
                File.WriteAllText(_breadcrumbFilePath, content);
            }
            catch
            {
                // Diagnostics must never break the tick.
            }
        }

        /// <inheritdoc />
        public void ReportSlowTick(string summary)
        {
            _slowTickLogger(summary);
        }
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~FileTickDiagnosticsSinkTests"`
Expected: PASS, 3 tests.

- [ ] **Step 5: Commit**

```bash
git add src/FactionWars/ScriptHookV/Diagnostics/FileTickDiagnosticsSink.cs tests/FactionWars.Tests/Unit/ScriptHookV/Diagnostics/FileTickDiagnosticsSinkTests.cs
git commit -m "feat: add file-backed tick diagnostics sink (#69)"
```

---

## Task 4: Wire profiler into GameLoopController (build-verified)

Wiring touches GTA-integrated code (no unit test; verified by build + in-game logs per CLAUDE.md).

**Files:**
- Modify: `src/FactionWars/ScriptHookV/GameLoopController.Runtime.cs`
- Modify: `src/FactionWars/ScriptHookV/GameLoopController.SystemUpdates.cs`

**Interfaces:**
- Consumes: `TickProfiler`, `FileTickDiagnosticsSink`, `TickProfilerOptions`, `FactionWars.Core.Services.SystemTimeProvider`, `FactionWars.ScriptHookV.Logging.FileLogger`.

- [ ] **Step 1: Add the profiler field + construction in the ctor**

In `GameLoopController.Runtime.cs`, add usings at top:
```csharp
using System.IO;
using FactionWars.Core.Services;
using FactionWars.Performance.Models;
using FactionWars.Performance.Services;
using FactionWars.ScriptHookV.Diagnostics;
```

Add a field (place near the other private fields — declared in the partial that owns them; if not visible here, declare it in this partial):
```csharp
private readonly TickProfiler _tickProfiler;
```

At the end of the constructor body (after `_lastTickTime = DateTime.UtcNow;`), add:
```csharp
var breadcrumbDir = Path.GetDirectoryName(FileLogger.LogPath) ?? ".";
var diagnosticsSink = new FileTickDiagnosticsSink(
    Path.Combine(breadcrumbDir, "tick_breadcrumb.txt"),
    FileLogger.Warn);
_tickProfiler = new TickProfiler(new SystemTimeProvider(), diagnosticsSink, new TickProfilerOptions());
```

- [ ] **Step 2: Bracket OnTick with BeginTick/EndTick**

In `OnTick`, immediately after `_lastTickTime = now;` add:
```csharp
_tickProfiler.BeginTick();
```
Wrap the remaining body so `EndTick` always runs. Replace the block from `UpdateCoreSystems(deltaTime);` through `ProcessPendingOwnedTerritoryPlacement();` with:
```csharp
try
{
    UpdateCoreSystems(deltaTime);
    _tickProfiler.Measure("respawnPlacement", () => UpdatePlayerRespawnPlacement());
    _tickProfiler.Measure("controllerInput", () => PollControllerInput());
    _menuProvider?.SetSelectKeyHeld(IsSelectKeyHeld());
    ThrottleMenuNavigation();
    _tickProfiler.Measure("mainMenu", () => _mainMenuController?.Update());
    UpdateWorldSystems(deltaTime);

    try
    {
        _tickProfiler.Measure("hud", () => UpdateAndDrawHud());
    }
    catch (Exception ex)
    {
        FileLogger.Error("UpdateAndDrawHud failed", ex);
    }

    ProcessPendingOwnedTerritoryPlacement();
}
finally
{
    _tickProfiler.EndTick();
}
```

- [ ] **Step 3: Wrap each subsystem call in UpdateCoreSystems / UpdateWorldSystems**

In `GameLoopController.SystemUpdates.cs`, replace `UpdateCoreSystems` body:
```csharp
private void UpdateCoreSystems(float deltaTime)
{
    _tickProfiler.Measure("economy", () => _economyManager?.Update(deltaTime));
    _tickProfiler.Measure("playTime", () => _gameStateManager?.UpdatePlayTime(deltaTime));
    _tickProfiler.Measure("telemetry", () => _telemetryService?.Update(deltaTime));
}
```

Replace `UpdateWorldSystems` body (preserve the existing control logic, wrap only the calls):
```csharp
private void UpdateWorldSystems(float deltaTime)
{
    _tickProfiler.Measure("worldRestore", () => TryRestoreRuntimeWorldState());
    _tickProfiler.Measure("blips", () => _mapBlipManager?.UpdateBlipColors());
    _tickProfiler.Measure("territory", () => _territoryManager?.Update());
    _tickProfiler.Measure("ai", () => _aiController?.Update(deltaTime));
    _tickProfiler.Measure("zoneBattle", () => _zoneBattleManager?.Tick(deltaTime));
    _tickProfiler.Measure("police", () => _policeSuppressionController?.Update());
    _tickProfiler.Measure("victory", () => _victoryManager?.Update(deltaTime));

    var boardPlayerVehicle = _squadStanceController == null
        || _squadStanceController.CurrentStance == SquadStance.Escort;
    _tickProfiler.Measure("followers", () => _followerManager?.Update(CurrentPlayerFactionId ?? "", boardPlayerVehicle));
    _tickProfiler.Measure("squadStance", () => UpdateSquadStance());
    _tickProfiler.Measure("sniperWeapons", () => UpdateFollowerSniperWeapons());
    _tickProfiler.Measure("friendlyDefenders", () => _friendlyDefenderManager?.Update());
    _tickProfiler.Measure("defenderRally", () => _defenderRallyController?.Update());
    _tickProfiler.Measure("commander", () => _commanderManager?.Update());

    var currentZone = _territoryManager?.CurrentZone;
    var enemyFactionId = currentZone?.OwnerFactionId;
    if (enemyFactionId != null && enemyFactionId != CurrentPlayerFactionId)
    {
        _tickProfiler.Measure("enemyDefenders", () => _enemyDefenderManager?.Update(enemyFactionId));
    }

    _tickProfiler.Measure("battleAttackers", () => _battleAttackerManager?.Update());
}
```

Keep the existing `using` for `FactionWars.Combat.Models` (SquadStance) already present in the file.

- [ ] **Step 4: Build to confirm it compiles**

Run: `dotnet build FactionWars.sln --no-incremental`
Expected: Build succeeded, 0 errors. (Analyzer may warn on closures/method length; warnings do not block. If `UpdateWorldSystems` trips the ≤40-line method analyzer, split the body into `UpdateCombatWorldSystems`/`UpdateSupportWorldSystems` helpers, each wrapping a subset.)

- [ ] **Step 5: Run the full unit suite**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~FactionWars.Tests.Unit"`
Expected: PASS (all existing + new tests).

- [ ] **Step 6: Commit**

```bash
git add src/FactionWars/ScriptHookV/GameLoopController.Runtime.cs src/FactionWars/ScriptHookV/GameLoopController.SystemUpdates.cs
git commit -m "feat: wire TickProfiler into the game loop (#69)"
```

---

## Self-Review notes

- **Spec coverage:** breadcrumb (Task 3 + wiring), per-phase timing + SLOW TICK summary (Task 2 + wiring), independence from FileLogger (dedicated file in Task 3), measurement for later stages (SLOW TICK summary). ✓
- **Type consistency:** `TickProfiler(ITimeProvider, ITickDiagnosticsSink, TickProfilerOptions)`; `Measure(string, Action)`; `WriteBreadcrumb(string)`/`ReportSlowTick(string)`; `FileTickDiagnosticsSink(string, Action<string>)` — consistent across tasks. ✓
- **Analyzer risk:** `UpdateWorldSystems` may exceed the 40-line method limit after wrapping — Task 4 Step 4 includes the split fallback. `TickProfiler` and sinks are small, single public type per file, ≤5 ctor params, no tuples. ✓
- **Method-length note:** the rewritten `OnTick` try/finally may approach the limit; if it trips, extract the wrapped body into `RunTickBody(float deltaTime)` and keep `OnTick` as init-guards + BeginTick + `try { RunTickBody(...) } finally { EndTick(); }`.
