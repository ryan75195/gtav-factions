# LOS-Reposition Telemetry Instrumentation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the squad LOS-reposition behavior (PR #99 / issue #98) falsifiably verifiable from telemetry — both a per-sample state snapshot in `behavior_trace.csv` and an exact, full-tick-fidelity transition event log in a new `engagement_events.csv`.

**Architecture:** The `SquadEngagementResolver` already decides each engaged ped's phase per tick. We surface *why* it chose (a reason enum on `EngageDecision`). The `SquadStanceController` records, per engaged ped, the current engagement snapshot (phase, has-LOS, ms-since-LOS) and appends a transition event whenever the phase changes — exposed through a new explicit-interface `ISquadEngagementStateSource` partial (the same pattern the managers use for `ITrackedCombatantSource`, so no new constructor parameter — `SquadStanceController` is already at the 5-param analyzer cap). The behavior sampler pulls the snapshot to enrich its CSV row; a tiny `EngagementEventRecorder` drains the transition buffer each tick into a CSV event sink.

**Tech Stack:** C# / .NET Framework 4.8, ScriptHookVDotNet3, xUnit + Moq.

## Global Constraints

- All `.cs` files MUST be CRLF + UTF-8-no-BOM. The Write tool emits LF; after writing/editing any `.cs` file run: `sed -i 's/\r$//; s/$/\r/' <files>`.
- Work on a numbered feature branch (hook blocks commits on `master`/`main`; branch name MUST contain a numeric issue number). This work uses `feat/100-los-reposition-telemetry` (issue #100).
- Pre-commit hook runs `dotnet build FactionWars.sln --no-incremental` + unit tests; both MUST pass before each commit.
- Analyzer ERRORS block the build: ≤250 lines/file (CI0017), ≤40 effective lines/method (CI0007), ≤10 public methods/class (CI0004), ≤5 constructor parameters (CI0005), `<Class>Tests` must cover every public method by name-match (CI0002), one public top-level type per file, no tuple returns. Explicit interface implementations are NOT public methods (exempt from CI0004/CI0002).
- GTA/native references stay in `ScriptHookV`; `Core`/`Combat`/`Telemetry` stay portable.
- Commit footer (both lines):
  `Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>`
  `Claude-Session: https://claude.ai/code/session_01MGyei1N5nCAgmp5DmiECej`
- Build command: `dotnet build FactionWars.sln --no-incremental`
- Unit-test command: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~FactionWars.Tests.Unit" --no-build`

---

## File Structure

**Create:**
- `src/FactionWars/Combat/Models/EngagePhaseChangeReason.cs` — enum: why the resolver chose its phase.
- `src/FactionWars/Telemetry/Models/SquadEngagementState.cs` — per-ped snapshot struct.
- `src/FactionWars/Telemetry/Models/EngagementTransition.cs` — one phase-change event struct.
- `src/FactionWars/Telemetry/Interfaces/ISquadEngagementStateSource.cs` — pull snapshot + drain transitions.
- `src/FactionWars/Telemetry/Interfaces/IEngagementEventSink.cs` — event sink contract.
- `src/FactionWars/Telemetry/Sinks/CsvEngagementEventSink.cs` — writes `engagement_events.csv`.
- `src/FactionWars/ScriptHookV/Telemetry/EngagementEventRecorder.cs` — drains the buffer into the sink each tick.
- `src/FactionWars/ScriptHookV/Managers/SquadStanceController.Telemetry.cs` — explicit `ISquadEngagementStateSource` impl + recording helpers.
- Test files mirroring each.

**Modify:**
- `src/FactionWars/Combat/Models/EngageDecision.cs` — add `Reason`.
- `src/FactionWars/Combat/Services/SquadEngagementResolver.cs` — set `Reason` per branch.
- `src/FactionWars/ScriptHookV/Managers/SquadStanceController.cs` — prune/clear new state dicts.
- `src/FactionWars/ScriptHookV/Managers/SquadStanceController.Stances.cs` — capture `now`, call recording helper.
- `src/FactionWars/Telemetry/Models/BehaviorSampleRow.cs` — 3 new fields.
- `src/FactionWars/Telemetry/Sinks/CsvBehaviorTraceSink.cs` — 3 new columns.
- `src/FactionWars/ScriptHookV/Telemetry/CombatBehaviorSampler.cs` — optional state source enriches row.
- `src/FactionWars/ScriptHookV/GameLoopController.cs`, `.InitializationTelemetry.cs`, `.Lifecycle.cs`, `.SystemUpdates.cs`, `.AbortCleanup.cs` — wire the event sink + recorder.

---

## Task 1: Resolver returns a phase-change reason

**Files:**
- Create: `src/FactionWars/Combat/Models/EngagePhaseChangeReason.cs`
- Modify: `src/FactionWars/Combat/Models/EngageDecision.cs`
- Modify: `src/FactionWars/Combat/Services/SquadEngagementResolver.cs`
- Test: `tests/FactionWars.Tests/Unit/Combat/SquadEngagementResolverTests.cs`

**Interfaces:**
- Consumes: existing `EngagePhase` enum, `IEngageRangeProvider.For(DefenderRole)`.
- Produces: `enum EngagePhaseChangeReason { None, EngageAcquired, RangeBroken, LosReposition }`; `EngageDecision(EngagePhase phase, float advanceStopRange, EngagePhaseChangeReason reason)` with added `EngagePhaseChangeReason Reason { get; }`. Invariant: `Reason != None` **iff** the phase changed this tick.

- [ ] **Step 1: Add the reason assertions to existing resolver tests (RED)**

In `tests/FactionWars.Tests/Unit/Combat/SquadEngagementResolverTests.cs`, add a `Reason` assertion to these existing tests (append the line shown after each test's existing asserts):

```csharp
// in OutOfRange_Advances:
Assert.Equal(EngagePhaseChangeReason.None, d.Reason);

// in InRangeWithLos_Engages:
Assert.Equal(EngagePhaseChangeReason.EngageAcquired, d.Reason);

// in InRangeNoLos_AdvancesToReposition:
Assert.Equal(EngagePhaseChangeReason.None, d.Reason);

// in Engaging_WithinHysteresisBand_StaysEngaged:
Assert.Equal(EngagePhaseChangeReason.None, d.Reason);

// in Engaging_PastHysteresisBand_DropsToAdvance:
Assert.Equal(EngagePhaseChangeReason.RangeBroken, d.Reason);

// in Engaging_BriefLosLoss_StaysEngaged:
Assert.Equal(EngagePhaseChangeReason.None, d.Reason);

// in Engaging_SustainedLosLoss_RepositionsToEdge:
Assert.Equal(EngagePhaseChangeReason.LosReposition, d.Reason);

// in Engaging_LosRegained_StaysEngaged:
Assert.Equal(EngagePhaseChangeReason.None, d.Reason);
```

`using FactionWars.Combat.Models;` is already present. The file won't compile yet (no `Reason`, no enum) — that's the RED state.

- [ ] **Step 2: Verify it fails to compile**

Run: `dotnet build FactionWars.sln --no-incremental`
Expected: build FAILS — `'EngageDecision' does not contain a definition for 'Reason'` and `EngagePhaseChangeReason` not found.

- [ ] **Step 3: Create the enum**

Create `src/FactionWars/Combat/Models/EngagePhaseChangeReason.cs`:

```csharp
namespace FactionWars.Combat.Models
{
    /// <summary>Why <see cref="FactionWars.Combat.Services.SquadEngagementResolver"/> chose its phase
    /// this tick. A non-<see cref="None"/> value occurs exactly when the phase changed, so it doubles
    /// as the trigger and label for an engagement transition event.</summary>
    public enum EngagePhaseChangeReason
    {
        /// <summary>Phase unchanged this tick (held Advance or held Engage).</summary>
        None = 0,

        /// <summary>Advance -> Engage: in range with line of sight.</summary>
        EngageAcquired,

        /// <summary>Engage -> Advance: target left the hysteresis band.</summary>
        RangeBroken,

        /// <summary>Engage -> Advance: line of sight stayed broken; push for a new vantage.</summary>
        LosReposition
    }
}
```

- [ ] **Step 4: Add `Reason` to `EngageDecision`**

Replace the body of `src/FactionWars/Combat/Models/EngageDecision.cs`:

```csharp
namespace FactionWars.Combat.Models
{
    /// <summary>Output of <c>ISquadEngagementResolver.Resolve</c>: the new phase, the stopping range
    /// to use when advancing (the role's engage range when closing for a shot, or a short reposition
    /// range when pushing for line of sight), and why the phase was chosen.</summary>
    public readonly struct EngageDecision
    {
        public EngageDecision(EngagePhase phase, float advanceStopRange, EngagePhaseChangeReason reason)
        {
            Phase = phase;
            AdvanceStopRange = advanceStopRange;
            Reason = reason;
        }

        public EngagePhase Phase { get; }

        public float AdvanceStopRange { get; }

        public EngagePhaseChangeReason Reason { get; }
    }
}
```

- [ ] **Step 5: Set the reason in the resolver**

In `src/FactionWars/Combat/Services/SquadEngagementResolver.cs`, replace the body of `Resolve` (everything after `float range = _rangeProvider.For(role);`) with:

```csharp
            if (currentPhase == EngagePhase.Engage)
            {
                bool rangeBroken = distToTarget > range * HysteresisFactor;
                bool losLostSustained = !hasLineOfSight && msSinceLastLos >= SustainedLosLossMs;

                if (rangeBroken)
                {
                    return new EngageDecision(EngagePhase.Advance, range, EngagePhaseChangeReason.RangeBroken);
                }

                if (losLostSustained)
                {
                    // Push almost onto the target to break the occlusion and regain a firing line.
                    return new EngageDecision(EngagePhase.Advance, LosRepositionStopRange, EngagePhaseChangeReason.LosReposition);
                }

                return new EngageDecision(EngagePhase.Engage, range, EngagePhaseChangeReason.None);
            }

            if (distToTarget <= range && hasLineOfSight)
            {
                return new EngageDecision(EngagePhase.Engage, range, EngagePhaseChangeReason.EngageAcquired);
            }

            // Advancing: close to engage range when we can see the target, but push right up to it
            // when we can't — the blocked sight line means we need a different vantage point.
            float stopRange = hasLineOfSight ? range : LosRepositionStopRange;
            return new EngageDecision(EngagePhase.Advance, stopRange, EngagePhaseChangeReason.None);
```

- [ ] **Step 6: Normalize line endings, build, test (GREEN)**

```bash
sed -i 's/\r$//; s/$/\r/' \
  src/FactionWars/Combat/Models/EngagePhaseChangeReason.cs \
  src/FactionWars/Combat/Models/EngageDecision.cs \
  src/FactionWars/Combat/Services/SquadEngagementResolver.cs \
  tests/FactionWars.Tests/Unit/Combat/SquadEngagementResolverTests.cs
dotnet build FactionWars.sln --no-incremental
dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~SquadEngagementResolver" --no-build
```
Expected: build clean (0 warnings/errors); all `SquadEngagementResolver` tests pass.

- [ ] **Step 7: Commit**

```bash
git add -A && git commit -m "feat: resolver reports phase-change reason for telemetry"
```

---

## Task 2: Controller records engagement state + transitions

**Files:**
- Create: `src/FactionWars/Telemetry/Models/SquadEngagementState.cs`
- Create: `src/FactionWars/Telemetry/Models/EngagementTransition.cs`
- Create: `src/FactionWars/Telemetry/Interfaces/ISquadEngagementStateSource.cs`
- Create: `src/FactionWars/ScriptHookV/Managers/SquadStanceController.Telemetry.cs`
- Modify: `src/FactionWars/ScriptHookV/Managers/SquadStanceController.cs` (prune/clear)
- Modify: `src/FactionWars/ScriptHookV/Managers/SquadStanceController.Stances.cs` (capture `now`, call recorder)
- Test: `tests/FactionWars.Tests/Unit/ScriptHookV/Managers/SquadStanceControllerEngagementTelemetryTests.cs`

**Interfaces:**
- Consumes: `EngageDecision` (now with `Reason`), `EngagePhase`, `EngagePhaseChangeReason` (Task 1).
- Produces:
  - `readonly struct SquadEngagementState(EngagePhase phase, bool hasLineOfSight, int msSinceLos)` with `Phase`, `HasLineOfSight`, `MsSinceLos`.
  - `readonly struct EngagementTransition(int handle, int atMs, EngagePhase fromPhase, EngagePhase toPhase, EngagePhaseChangeReason reason, float distToTarget, bool hasLineOfSight, int msSinceLos)` with matching get-only properties.
  - `interface ISquadEngagementStateSource { bool TryGetEngagementState(int handle, out SquadEngagementState state); IReadOnlyList<EngagementTransition> DrainEngagementTransitions(); }`.
  - `SquadStanceController` implements `ISquadEngagementStateSource` explicitly.

- [ ] **Step 1: Create the snapshot model**

Create `src/FactionWars/Telemetry/Models/SquadEngagementState.cs`:

```csharp
using FactionWars.Combat.Models;

namespace FactionWars.Telemetry.Models
{
    /// <summary>A squad member's engagement snapshot for the most recent tick it was tasked: its
    /// phase, whether it held line of sight to its target, and how long sight has been broken (ms).</summary>
    public readonly struct SquadEngagementState
    {
        public SquadEngagementState(EngagePhase phase, bool hasLineOfSight, int msSinceLos)
        {
            Phase = phase;
            HasLineOfSight = hasLineOfSight;
            MsSinceLos = msSinceLos;
        }

        public EngagePhase Phase { get; }

        public bool HasLineOfSight { get; }

        public int MsSinceLos { get; }
    }
}
```

- [ ] **Step 2: Create the transition model**

Create `src/FactionWars/Telemetry/Models/EngagementTransition.cs`:

```csharp
using FactionWars.Combat.Models;

namespace FactionWars.Telemetry.Models
{
    /// <summary>One engagement phase change for a squad member, captured at the game time it occurred
    /// so the event log keeps full tick fidelity even when drained on a slower cadence.</summary>
    public readonly struct EngagementTransition
    {
        public EngagementTransition(
            int handle,
            int atMs,
            EngagePhase fromPhase,
            EngagePhase toPhase,
            EngagePhaseChangeReason reason,
            float distToTarget,
            bool hasLineOfSight,
            int msSinceLos)
        {
            Handle = handle;
            AtMs = atMs;
            FromPhase = fromPhase;
            ToPhase = toPhase;
            Reason = reason;
            DistToTarget = distToTarget;
            HasLineOfSight = hasLineOfSight;
            MsSinceLos = msSinceLos;
        }

        public int Handle { get; }

        public int AtMs { get; }

        public EngagePhase FromPhase { get; }

        public EngagePhase ToPhase { get; }

        public EngagePhaseChangeReason Reason { get; }

        public float DistToTarget { get; }

        public bool HasLineOfSight { get; }

        public int MsSinceLos { get; }
    }
}
```

- [ ] **Step 3: Create the source interface**

Create `src/FactionWars/Telemetry/Interfaces/ISquadEngagementStateSource.cs`:

```csharp
using System.Collections.Generic;
using FactionWars.Telemetry.Models;

namespace FactionWars.Telemetry.Interfaces
{
    /// <summary>Exposes the squad controller's per-ped engagement telemetry without coupling consumers
    /// to its internals: a pull snapshot for periodic sampling and a drain of phase-change events.</summary>
    public interface ISquadEngagementStateSource
    {
        /// <summary>Latest engagement snapshot for <paramref name="handle"/>; false if none recorded.</summary>
        bool TryGetEngagementState(int handle, out SquadEngagementState state);

        /// <summary>Returns and clears the buffered phase-change events since the last drain.</summary>
        IReadOnlyList<EngagementTransition> DrainEngagementTransitions();
    }
}
```

- [ ] **Step 4: Write the failing controller-telemetry test (RED)**

Create `tests/FactionWars.Tests/Unit/ScriptHookV/Managers/SquadStanceControllerEngagementTelemetryTests.cs`:

```csharp
using System.Collections.Generic;
using FactionWars.Combat.Models;
using FactionWars.Combat.Services;
using FactionWars.Core.Models;
using FactionWars.Core.Utils;
using FactionWars.ScriptHookV.Managers;
using FactionWars.Telemetry.Interfaces;
using FactionWars.Telemetry.Models;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV.Managers
{
    public class SquadStanceControllerEngagementTelemetryTests
    {
        private readonly MockGameBridge _bridge = new MockGameBridge();
        private readonly SquadStanceController _controller;

        public SquadStanceControllerEngagementTelemetryTests()
        {
            _controller = new SquadStanceController(
                _bridge,
                new SquadStanceResolver(),
                new TargetAssignmentResolver(),
                new PedIntentReconciler(_bridge),
                new SquadEngagementResolver(new EngageRangeProvider()));
            var handles = new List<int> { 0 };
            _controller.CycleStance(handles); // Escort -> HoldArea
            _controller.CycleStance(handles); // HoldArea -> SearchAndDestroy
        }

        private ISquadEngagementStateSource Source => _controller;

        private static IReadOnlyDictionary<int, DefenderRole> Roles(int handle, DefenderRole role)
            => new Dictionary<int, DefenderRole> { [handle] = role };

        [Fact]
        public void TryGetEngagementState_AfterEngaging_ReportsEngagePhaseAndLos()
        {
            int follower = _bridge.CreatePed("f", new Vector3(0f, 0f, 0f));
            int enemy = _bridge.CreatePed("e", new Vector3(10f, 0f, 0f)); // in Grunt range 18
            _bridge.SetLineOfSight(follower, enemy, true);
            var enemies = new List<EnemyTarget> { new EnemyTarget(enemy, _bridge.GetPedPosition(enemy)) };

            _controller.Update(new Vector3(0f, 0f, 0f), 250f,
                new List<int> { follower }, enemies, Roles(follower, DefenderRole.Grunt));

            Assert.True(Source.TryGetEngagementState(follower, out var state));
            Assert.Equal(EngagePhase.Engage, state.Phase);
            Assert.True(state.HasLineOfSight);
            Assert.Equal(0, state.MsSinceLos);
        }

        [Fact]
        public void DrainEngagementTransitions_OnAcquisition_EmitsEngageAcquiredOnce()
        {
            int follower = _bridge.CreatePed("f", new Vector3(0f, 0f, 0f));
            int enemy = _bridge.CreatePed("e", new Vector3(10f, 0f, 0f));
            _bridge.SetLineOfSight(follower, enemy, true);
            var enemies = new List<EnemyTarget> { new EnemyTarget(enemy, _bridge.GetPedPosition(enemy)) };

            _controller.Update(new Vector3(0f, 0f, 0f), 250f,
                new List<int> { follower }, enemies, Roles(follower, DefenderRole.Grunt));

            var first = Source.DrainEngagementTransitions();
            Assert.Single(first);
            Assert.Equal(follower, first[0].Handle);
            Assert.Equal(EngagePhase.Advance, first[0].FromPhase);
            Assert.Equal(EngagePhase.Engage, first[0].ToPhase);
            Assert.Equal(EngagePhaseChangeReason.EngageAcquired, first[0].Reason);

            // Drain is destructive: a second drain with no new transition is empty.
            Assert.Empty(Source.DrainEngagementTransitions());
        }
    }
}
```

> Implementer note: confirm the helper names on `MockGameBridge` (`CreatePed`, `SetLineOfSight`, `GetPedPosition`) match the existing `SquadStanceControllerSearchDestroyTests.cs` — that file uses exactly these. Reuse its conventions.

- [ ] **Step 5: Verify it fails**

Run: `dotnet build FactionWars.sln --no-incremental`
Expected: build FAILS — `SquadStanceController` cannot be cast to `ISquadEngagementStateSource` / member not found.

- [ ] **Step 6: Implement the controller telemetry partial**

Create `src/FactionWars/ScriptHookV/Managers/SquadStanceController.Telemetry.cs`:

```csharp
using System;
using System.Collections.Generic;
using FactionWars.Combat.Models;
using FactionWars.Telemetry.Interfaces;
using FactionWars.Telemetry.Models;

namespace FactionWars.ScriptHookV.Managers
{
    public partial class SquadStanceController : ISquadEngagementStateSource
    {
        private readonly Dictionary<int, SquadEngagementState> _engagementState = new Dictionary<int, SquadEngagementState>();
        private readonly List<EngagementTransition> _transitions = new List<EngagementTransition>();

        bool ISquadEngagementStateSource.TryGetEngagementState(int handle, out SquadEngagementState state)
            => _engagementState.TryGetValue(handle, out state);

        IReadOnlyList<EngagementTransition> ISquadEngagementStateSource.DrainEngagementTransitions()
        {
            if (_transitions.Count == 0)
            {
                return Array.Empty<EngagementTransition>();
            }

            var drained = _transitions.ToArray();
            _transitions.Clear();
            return drained;
        }

        // Stores the current engagement snapshot and, on a phase change (Reason != None), appends a
        // transition event stamped with the game time it occurred. Called once per engaged ped/tick.
        private void RecordEngagementTelemetry(int pedHandle, EngagePhase priorPhase, EngageDecision decision, bool los, int msSinceLos, int nowMs, float dist)
        {
            _engagementState[pedHandle] = new SquadEngagementState(decision.Phase, los, msSinceLos);
            if (decision.Reason == EngagePhaseChangeReason.None)
            {
                return;
            }

            _transitions.Add(new EngagementTransition(pedHandle, nowMs, priorPhase, decision.Phase, decision.Reason, dist, los, msSinceLos));
        }
    }
}
```

- [ ] **Step 7: Wire the recorder into `ApplyEngagement` and `TrackLineOfSight`**

In `src/FactionWars/ScriptHookV/Managers/SquadStanceController.Stances.cs`, replace the top of `ApplyEngagement` (from its opening brace through the `_enginePhase[pedHandle] = decision.Phase;` line) with:

```csharp
        private void ApplyEngagement(int pedHandle, int targetHandle, Vector3 targetPos)
        {
            int now = _gameBridge.GetGameTime();
            float dist = _gameBridge.GetPedPosition(pedHandle).DistanceTo(targetPos);
            bool los = _gameBridge.HasClearLineOfSight(pedHandle, targetHandle);
            var role = _rolesByHandle.TryGetValue(pedHandle, out var r) ? r : DefenderRole.Grunt;
            var phase = _enginePhase.TryGetValue(pedHandle, out var p) ? p : EngagePhase.Advance;
            int msSinceLos = TrackLineOfSight(pedHandle, los, now);

            var decision = _engagementResolver.Resolve(dist, los, role, phase, msSinceLos);
            _enginePhase[pedHandle] = decision.Phase;
            RecordEngagementTelemetry(pedHandle, phase, decision, los, msSinceLos, now, dist);
```

Then replace the `TrackLineOfSight` method with the `now`-parameterized version:

```csharp
        // Maintains the per-ped line-of-sight clock and returns how long LOS has stayed broken (ms).
        // While LOS holds the clock resets to now; the first no-LOS tick baselines off now so a ped
        // is never treated as having "just lost" sight for longer than it actually has.
        private int TrackLineOfSight(int pedHandle, bool los, int now)
        {
            if (los || !_lastLosMs.ContainsKey(pedHandle))
            {
                _lastLosMs[pedHandle] = now;
                return 0;
            }

            return now - _lastLosMs[pedHandle];
        }
```

- [ ] **Step 8: Prune and clear the new state in `SquadStanceController.cs`**

In `src/FactionWars/ScriptHookV/Managers/SquadStanceController.cs`, in `CycleStance` after `_lastLosMs.Clear();` add:

```csharp
            _engagementState.Clear();
            _transitions.Clear();
```

And in `PruneStale`, in the loop body after `_lastLosMs.Remove(handle);` add:

```csharp
                    _engagementState.Remove(handle);
```

(Buffered transitions for a removed handle are harmless — they carry their own handle and are drained regardless — so they are not individually pruned.)

- [ ] **Step 9: Normalize, build, test (GREEN)**

```bash
sed -i 's/\r$//; s/$/\r/' \
  src/FactionWars/Telemetry/Models/SquadEngagementState.cs \
  src/FactionWars/Telemetry/Models/EngagementTransition.cs \
  src/FactionWars/Telemetry/Interfaces/ISquadEngagementStateSource.cs \
  src/FactionWars/ScriptHookV/Managers/SquadStanceController.Telemetry.cs \
  src/FactionWars/ScriptHookV/Managers/SquadStanceController.cs \
  src/FactionWars/ScriptHookV/Managers/SquadStanceController.Stances.cs \
  tests/FactionWars.Tests/Unit/ScriptHookV/Managers/SquadStanceControllerEngagementTelemetryTests.cs
dotnet build FactionWars.sln --no-incremental
dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~SquadStanceController" --no-build
```
Expected: build clean; all `SquadStanceController*` tests pass (including the existing S&D tests, which still hit `GetGameTime` at default time 0).

- [ ] **Step 10: Commit**

```bash
git add -A && git commit -m "feat: record squad engagement state and phase transitions"
```

---

## Task 3: Enrich `behavior_trace.csv` with engagement state

**Files:**
- Modify: `src/FactionWars/Telemetry/Models/BehaviorSampleRow.cs`
- Modify: `src/FactionWars/Telemetry/Sinks/CsvBehaviorTraceSink.cs`
- Modify: `src/FactionWars/ScriptHookV/Telemetry/CombatBehaviorSampler.cs`
- Test: `tests/FactionWars.Tests/Unit/Telemetry/CsvBehaviorTraceSinkTests.cs`
- Test: `tests/FactionWars.Tests/Unit/ScriptHookV/Telemetry/CombatBehaviorSamplerTests.cs`

**Interfaces:**
- Consumes: `ISquadEngagementStateSource.TryGetEngagementState` (Task 2), `SquadEngagementState`.
- Produces: `BehaviorSampleRow` gains `bool HasLineOfSight`, `string EnginePhase` (`""` when N/A), `int MsSinceLos` (`-1` when N/A). `CombatBehaviorSampler` ctor gains optional 4th param `ISquadEngagementStateSource? engagementState = null` (before `sampleIntervalMs`). CSV header gains `has_los,engine_phase,ms_since_los`.

- [ ] **Step 1: Update the sink test for the new columns (RED)**

First read `tests/FactionWars.Tests/Unit/Telemetry/CsvBehaviorTraceSinkTests.cs` to see how `Write_AfterSetSaveFile_WritesHeaderAndRow` asserts the header/row. Then:

- Set on the row before `sink.Write(row)`:

```csharp
            row.HasLineOfSight = true;
            row.EnginePhase = "Engage";
            row.MsSinceLos = 250;
```

- Update the header assertion so it expects the trailing columns. If the test asserts the full header string, change its tail `...,health,combat_ability` to `...,health,combat_ability,has_los,engine_phase,ms_since_los`. If it uses a substring/`Contains`, assert:

```csharp
            Assert.Contains("combat_ability,has_los,engine_phase,ms_since_los", lines[0]);
```

- Assert the data row contains the new fields:

```csharp
            Assert.Contains(",true,Engage,250", lines[1]);
```

- [ ] **Step 2: Verify it fails**

Run: `dotnet build FactionWars.sln --no-incremental`
Expected: FAILS — `BehaviorSampleRow` has no `HasLineOfSight`/`EnginePhase`/`MsSinceLos`.

- [ ] **Step 3: Add the row fields**

In `src/FactionWars/Telemetry/Models/BehaviorSampleRow.cs`, add after the `CombatAbility` property:

```csharp
        /// <summary>Squad engagement: true if the ped held line of sight to its target this sample.
        /// Only meaningful for followers in Search &amp; Destroy; false when no engagement state.</summary>
        public bool HasLineOfSight { get; set; }

        /// <summary>Engagement phase name ("Advance"/"Engage"); empty when no engagement state.</summary>
        public string EnginePhase { get; set; } = string.Empty;

        /// <summary>Milliseconds since the ped last held line of sight; -1 when no engagement state.</summary>
        public int MsSinceLos { get; set; } = -1;
```

- [ ] **Step 4: Add the CSV columns**

In `src/FactionWars/Telemetry/Sinks/CsvBehaviorTraceSink.cs`:

Change the `Header` constant to end with the new columns:

```csharp
            "session_id,timestamp_utc,sample_ms,handle,kind,role,weapon,is_shooting,in_combat,target_handle,dist_to_target,dist_to_player,pos_x,pos_y,pos_z,in_vehicle,is_following_player,health,combat_ability,has_los,engine_phase,ms_since_los";
```

In `Serialize`, change the final argument `I(r.CombatAbility));` to:

```csharp
            I(r.CombatAbility),
            B(r.HasLineOfSight),
            Esc(r.EnginePhase),
            I(r.MsSinceLos));
```

- [ ] **Step 5: Normalize, build, run sink test (partial GREEN)**

```bash
sed -i 's/\r$//; s/$/\r/' \
  src/FactionWars/Telemetry/Models/BehaviorSampleRow.cs \
  src/FactionWars/Telemetry/Sinks/CsvBehaviorTraceSink.cs \
  tests/FactionWars.Tests/Unit/Telemetry/CsvBehaviorTraceSinkTests.cs
dotnet build FactionWars.sln --no-incremental
dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~CsvBehaviorTraceSink" --no-build
```
Expected: build clean; `CsvBehaviorTraceSink` tests pass.

- [ ] **Step 6: Write the failing sampler-enrichment test (RED)**

First read `tests/FactionWars.Tests/Unit/ScriptHookV/Telemetry/CombatBehaviorSamplerTests.cs` to learn how it constructs a bridge with a live ped, a `FakeSource`, and a `FakeSink`, and how it forces an immediate sample. Add a fake engagement source near the existing fakes:

```csharp
        private sealed class FakeEngagementSource : ISquadEngagementStateSource
        {
            private readonly Dictionary<int, SquadEngagementState> _states;
            public FakeEngagementSource(Dictionary<int, SquadEngagementState> states) => _states = states;
            public bool TryGetEngagementState(int handle, out SquadEngagementState state)
                => _states.TryGetValue(handle, out state);
            public IReadOnlyList<EngagementTransition> DrainEngagementTransitions()
                => System.Array.Empty<EngagementTransition>();
        }
```

Add usings if absent: `using FactionWars.Telemetry.Interfaces;`, `using FactionWars.Telemetry.Models;`, `using FactionWars.Combat.Models;`. Then add two tests, reusing the file's existing setup pattern to build a sampler over a single live follower handle and capture the written row (call the existing helper or inline construction the other tests use; the only new argument is the engagement source passed as the 4th constructor parameter):

```csharp
        [Fact]
        public void Update_WithEngagementSource_EnrichesRowWithLosAndPhase()
        {
            const int handle = 7;
            var states = new Dictionary<int, SquadEngagementState>
            {
                [handle] = new SquadEngagementState(EngagePhase.Engage, true, 0)
            };
            // Build a sampler over one alive follower `handle`, with the engagement source as the
            // 4th ctor arg, mirroring this file's existing single-ped sample setup. Force one sample.
            var (sampler, sink) = BuildFollowerSampler(handle, new FakeEngagementSource(states));

            sampler.Update();

            var row = sink.LastRow();
            Assert.Equal("Engage", row.EnginePhase);
            Assert.True(row.HasLineOfSight);
            Assert.Equal(0, row.MsSinceLos);
        }

        [Fact]
        public void Update_WithoutEngagementSource_LeavesEngagementFieldsDefault()
        {
            const int handle = 7;
            var (sampler, sink) = BuildFollowerSampler(handle, null);

            sampler.Update();

            var row = sink.LastRow();
            Assert.Equal(string.Empty, row.EnginePhase);
            Assert.Equal(-1, row.MsSinceLos);
        }
```

If the file lacks a reusable builder/last-row accessor, add `BuildFollowerSampler(int handle, ISquadEngagementStateSource? src)` (returns the sampler and the `FakeSink`) and a `LastRow()` on the `FakeSink` by lifting the construction the existing tests already perform — the only delta is passing `src` as the 4th `CombatBehaviorSampler` arg and `sampleIntervalMs` last.

- [ ] **Step 7: Verify it fails**

Run: `dotnet build FactionWars.sln --no-incremental`
Expected: FAILS — `CombatBehaviorSampler` has no constructor accepting `ISquadEngagementStateSource`.

- [ ] **Step 8: Add the optional source to the sampler**

In `src/FactionWars/ScriptHookV/Telemetry/CombatBehaviorSampler.cs`:

Add a field after `_sink`:

```csharp
        private readonly ISquadEngagementStateSource? _engagementState;
```

Change the constructor to accept the optional source (insert before `sampleIntervalMs`):

```csharp
        public CombatBehaviorSampler(
            IGameBridge gameBridge,
            IReadOnlyList<ITrackedCombatantSource> sources,
            IBehaviorTraceSink sink,
            ISquadEngagementStateSource? engagementState = null,
            int sampleIntervalMs = 1000)
        {
            _gameBridge = gameBridge ?? throw new ArgumentNullException(nameof(gameBridge));
            _sources = sources ?? throw new ArgumentNullException(nameof(sources));
            _sink = sink ?? throw new ArgumentNullException(nameof(sink));
            _engagementState = engagementState;
            _sampleIntervalMs = sampleIntervalMs;
            _lastSampleMs = _gameBridge.GetGameTime();
        }
```

In `BuildRow`, change `return new BehaviorSampleRow { ... };` to assign to a local, enrich, then return:

```csharp
            var row = new BehaviorSampleRow
            {
                SampleMs = sampleMs,
                Handle = handle,
                Kind = self.Combatant.Kind,
                Role = self.Combatant.Role,
                Weapon = _gameBridge.GetSelectedWeapon(handle),
                IsShooting = _gameBridge.IsPedShooting(handle),
                InCombat = _gameBridge.IsPedInCombat(handle),
                TargetHandle = nearest.HasValue ? nearest.Value.Combatant.Handle : -1,
                DistToTarget = nearest.HasValue ? Distance(pos, nearest.Value.Position) : -1f,
                DistToPlayer = Distance(pos, playerPos),
                PosX = pos.X,
                PosY = pos.Y,
                PosZ = pos.Z,
                InVehicle = _gameBridge.IsPedInVehicle(handle),
                IsFollowingPlayer = _gameBridge.IsPedFollowingPlayer(handle),
                Health = _gameBridge.GetPedHealth(handle),
                CombatAbility = _gameBridge.GetPedCombatAbilityValue(handle)
            };

            if (_engagementState != null && _engagementState.TryGetEngagementState(handle, out var es))
            {
                row.HasLineOfSight = es.HasLineOfSight;
                row.EnginePhase = es.Phase.ToString();
                row.MsSinceLos = es.MsSinceLos;
            }

            return row;
```

Add `using FactionWars.Telemetry.Interfaces;` to the file (it already imports `FactionWars.Telemetry.Models`).

- [ ] **Step 9: Normalize, build, test (GREEN)**

```bash
sed -i 's/\r$//; s/$/\r/' \
  src/FactionWars/ScriptHookV/Telemetry/CombatBehaviorSampler.cs \
  tests/FactionWars.Tests/Unit/ScriptHookV/Telemetry/CombatBehaviorSamplerTests.cs
dotnet build FactionWars.sln --no-incremental
dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~CombatBehaviorSampler" --no-build
```
Expected: build clean; all `CombatBehaviorSampler` tests pass.

- [ ] **Step 10: Commit**

```bash
git add -A && git commit -m "feat: add engagement state columns to behavior_trace.csv"
```

---

## Task 4: `engagement_events.csv` sink

**Files:**
- Create: `src/FactionWars/Telemetry/Interfaces/IEngagementEventSink.cs`
- Create: `src/FactionWars/Telemetry/Sinks/CsvEngagementEventSink.cs`
- Test: `tests/FactionWars.Tests/Unit/Telemetry/CsvEngagementEventSinkTests.cs`

**Interfaces:**
- Consumes: `EngagementTransition` (Task 2).
- Produces: `interface IEngagementEventSink : IDisposable { void Write(EngagementTransition e); void SetSaveFile(string saveFilename); }`. `CsvEngagementEventSink(string baseDirectory)` writes `engagement_events.csv` with header `session_id,timestamp_utc,handle,at_ms,from_phase,to_phase,reason,dist_to_target,has_los,ms_since_los`, buffering until `SetSaveFile`.

- [ ] **Step 1: Write the failing sink test (RED)**

First read `tests/FactionWars.Tests/Unit/Telemetry/CsvBehaviorTraceSinkTests.cs` to copy its temp-directory pattern and `CsvFieldEscaper`/`using` conventions. Create `tests/FactionWars.Tests/Unit/Telemetry/CsvEngagementEventSinkTests.cs`:

```csharp
using System.IO;
using FactionWars.Combat.Models;
using FactionWars.Telemetry.Models;
using FactionWars.Telemetry.Sinks;
using Xunit;

namespace FactionWars.Tests.Unit.Telemetry
{
    public class CsvEngagementEventSinkTests
    {
        private static EngagementTransition Sample(int handle) => new EngagementTransition(
            handle, 1234, EngagePhase.Engage, EngagePhase.Advance,
            EngagePhaseChangeReason.LosReposition, 14.5f, false, 1800);

        [Fact]
        public void Write_AfterSetSaveFile_WritesHeaderAndRow()
        {
            var baseDir = Path.Combine(Path.GetTempPath(), "fw_ee_" + Path.GetRandomFileName());
            try
            {
                using var sink = new CsvEngagementEventSink(baseDir);
                sink.SetSaveFile("SGTA0001");
                sink.Write(Sample(42));

                var path = Path.Combine(baseDir, "SGTA0001", "engagement_events.csv");
                var lines = File.ReadAllLines(path);
                Assert.Equal("session_id,timestamp_utc,handle,at_ms,from_phase,to_phase,reason,dist_to_target,has_los,ms_since_los", lines[0]);
                Assert.Contains(",42,1234,Engage,Advance,LosReposition,", lines[1]);
                Assert.Contains(",false,1800", lines[1]);
            }
            finally
            {
                if (Directory.Exists(baseDir)) Directory.Delete(baseDir, true);
            }
        }

        [Fact]
        public void Write_BeforeSetSaveFile_BuffersAndFlushesOnSet()
        {
            var baseDir = Path.Combine(Path.GetTempPath(), "fw_ee_" + Path.GetRandomFileName());
            try
            {
                using var sink = new CsvEngagementEventSink(baseDir);
                sink.Write(Sample(7));
                sink.SetSaveFile("SGTA0002");

                var path = Path.Combine(baseDir, "SGTA0002", "engagement_events.csv");
                Assert.True(File.Exists(path));
                var lines = File.ReadAllLines(path);
                Assert.Equal(2, lines.Length); // header + buffered row
            }
            finally
            {
                if (Directory.Exists(baseDir)) Directory.Delete(baseDir, true);
            }
        }
    }
}
```

- [ ] **Step 2: Verify it fails**

Run: `dotnet build FactionWars.sln --no-incremental`
Expected: FAILS — `CsvEngagementEventSink` / `IEngagementEventSink` not found.

- [ ] **Step 3: Create the interface**

Create `src/FactionWars/Telemetry/Interfaces/IEngagementEventSink.cs`:

```csharp
using System;
using FactionWars.Telemetry.Models;

namespace FactionWars.Telemetry.Interfaces
{
    /// <summary>Persists squad engagement phase-change events. Writes MUST be safe before
    /// <see cref="SetSaveFile"/> is called — implementations buffer until the save folder is known.</summary>
    public interface IEngagementEventSink : IDisposable
    {
        void Write(EngagementTransition e);

        void SetSaveFile(string saveFilename);
    }
}
```

- [ ] **Step 4: Create the CSV sink**

Create `src/FactionWars/Telemetry/Sinks/CsvEngagementEventSink.cs` (read `CsvBehaviorTraceSink.cs` first to confirm `CsvFieldEscaper.Escape` and `FileLogger` namespaces match):

```csharp
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using FactionWars.ScriptHookV.Logging;
using FactionWars.Telemetry.Interfaces;
using FactionWars.Telemetry.Models;

namespace FactionWars.Telemetry.Sinks
{
    /// <summary>
    /// Writes squad engagement phase-change events to a per-save <c>engagement_events.csv</c>. Buffers
    /// rows until <see cref="SetSaveFile"/> is known, then flushes and appends. Mirrors
    /// <see cref="CsvBehaviorTraceSink"/>'s lifecycle and thread-safety.
    /// </summary>
    public sealed class CsvEngagementEventSink : IEngagementEventSink
    {
        private const int BufferCap = 20000;
        private const string FileName = "engagement_events.csv";
        private static readonly string Header =
            "session_id,timestamp_utc,handle,at_ms,from_phase,to_phase,reason,dist_to_target,has_los,ms_since_los";

        private readonly object _lock = new object();
        private readonly string _baseDir;
        private readonly string _sessionId;
        private readonly List<EngagementTransition> _buffer = new List<EngagementTransition>();
        private string? _saveDir;
        private bool _disposed;
        private bool _errored;

        public CsvEngagementEventSink(string baseDirectory)
        {
            _baseDir = baseDirectory ?? throw new ArgumentNullException(nameof(baseDirectory));
            _sessionId = CreateSessionId();
        }

        public void Write(EngagementTransition e)
        {
            lock (_lock)
            {
                if (_disposed) return;
                if (_saveDir == null)
                {
                    if (_buffer.Count >= BufferCap) _buffer.RemoveAt(0);
                    _buffer.Add(e);
                    return;
                }

                AppendLocked(new[] { Serialize(e) });
            }
        }

        public void SetSaveFile(string saveFilename)
        {
            if (string.IsNullOrWhiteSpace(saveFilename))
                throw new ArgumentException("saveFilename cannot be empty", nameof(saveFilename));

            lock (_lock)
            {
                if (_disposed || _saveDir != null) return;

                var dir = Path.Combine(_baseDir, saveFilename);
                try
                {
                    Directory.CreateDirectory(dir);
                }
                catch (Exception ex)
                {
                    FileLogger.Error($"CsvEngagementEventSink: failed to create {dir}", ex);
                    return;
                }

                _saveDir = dir;
                if (_buffer.Count > 0)
                {
                    var rows = new List<string>(_buffer.Count);
                    foreach (var e in _buffer) rows.Add(Serialize(e));
                    AppendLocked(rows);
                    _buffer.Clear();
                }
            }
        }

        public void Dispose()
        {
            lock (_lock)
            {
                if (_disposed) return;
                _disposed = true;
                _buffer.Clear();
            }
        }

        private void AppendLocked(IReadOnlyCollection<string> rows)
        {
            if (_saveDir == null || rows.Count == 0) return;
            var path = Path.Combine(_saveDir, FileName);
            try
            {
                var sb = new StringBuilder();
                if (!File.Exists(path)) sb.AppendLine(Header);
                foreach (var row in rows) sb.AppendLine(row);
                File.AppendAllText(path, sb.ToString());
            }
            catch (Exception ex)
            {
                if (!_errored)
                {
                    _errored = true;
                    FileLogger.Error($"CsvEngagementEventSink: failed to append to {path}", ex);
                }
            }
        }

        private string Serialize(EngagementTransition e) => string.Join(",",
            CsvFieldEscaper.Escape(_sessionId),
            DateTime.UtcNow.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
            e.Handle.ToString(CultureInfo.InvariantCulture),
            e.AtMs.ToString(CultureInfo.InvariantCulture),
            e.FromPhase.ToString(),
            e.ToPhase.ToString(),
            e.Reason.ToString(),
            e.DistToTarget.ToString("G", CultureInfo.InvariantCulture),
            e.HasLineOfSight ? "true" : "false",
            e.MsSinceLos.ToString(CultureInfo.InvariantCulture));

        private static string CreateSessionId()
            => DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffZ", CultureInfo.InvariantCulture)
                + "-"
                + Guid.NewGuid().ToString("N").Substring(0, 8);
    }
}
```

- [ ] **Step 5: Normalize, build, test (GREEN)**

```bash
sed -i 's/\r$//; s/$/\r/' \
  src/FactionWars/Telemetry/Interfaces/IEngagementEventSink.cs \
  src/FactionWars/Telemetry/Sinks/CsvEngagementEventSink.cs \
  tests/FactionWars.Tests/Unit/Telemetry/CsvEngagementEventSinkTests.cs
dotnet build FactionWars.sln --no-incremental
dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~CsvEngagementEventSink" --no-build
```
Expected: build clean; both sink tests pass.

- [ ] **Step 6: Commit**

```bash
git add -A && git commit -m "feat: add CsvEngagementEventSink for engagement_events.csv"
```

---

## Task 5: `EngagementEventRecorder` (drain → sink)

**Files:**
- Create: `src/FactionWars/ScriptHookV/Telemetry/EngagementEventRecorder.cs`
- Test: `tests/FactionWars.Tests/Unit/ScriptHookV/Telemetry/EngagementEventRecorderTests.cs`

**Interfaces:**
- Consumes: `ISquadEngagementStateSource.DrainEngagementTransitions` (Task 2), `IEngagementEventSink.Write` (Task 4).
- Produces: `EngagementEventRecorder(ISquadEngagementStateSource source, IEngagementEventSink sink)` with `void Update()` that drains and writes each transition; never throws.

- [ ] **Step 1: Write the failing test (RED)**

Create `tests/FactionWars.Tests/Unit/ScriptHookV/Telemetry/EngagementEventRecorderTests.cs`:

```csharp
using System;
using System.Collections.Generic;
using FactionWars.Combat.Models;
using FactionWars.ScriptHookV.Telemetry;
using FactionWars.Telemetry.Interfaces;
using FactionWars.Telemetry.Models;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV.Telemetry
{
    public class EngagementEventRecorderTests
    {
        private sealed class FakeSource : ISquadEngagementStateSource
        {
            private readonly Queue<IReadOnlyList<EngagementTransition>> _drains;
            public FakeSource(params IReadOnlyList<EngagementTransition>[] drains)
                => _drains = new Queue<IReadOnlyList<EngagementTransition>>(drains);
            public bool TryGetEngagementState(int handle, out SquadEngagementState state)
            {
                state = default;
                return false;
            }
            public IReadOnlyList<EngagementTransition> DrainEngagementTransitions()
                => _drains.Count > 0 ? _drains.Dequeue() : Array.Empty<EngagementTransition>();
        }

        private sealed class FakeSink : IEngagementEventSink
        {
            public readonly List<EngagementTransition> Written = new List<EngagementTransition>();
            public bool Throw;
            public void Write(EngagementTransition e)
            {
                if (Throw) throw new InvalidOperationException("boom");
                Written.Add(e);
            }
            public void SetSaveFile(string saveFilename) { }
            public void Dispose() { }
        }

        private static EngagementTransition T(int handle) => new EngagementTransition(
            handle, 100, EngagePhase.Advance, EngagePhase.Engage,
            EngagePhaseChangeReason.EngageAcquired, 10f, true, 0);

        [Fact]
        public void Update_WritesEachDrainedTransition()
        {
            var source = new FakeSource(new[] { T(1), T(2) });
            var sink = new FakeSink();
            var recorder = new EngagementEventRecorder(source, sink);

            recorder.Update();

            Assert.Equal(2, sink.Written.Count);
            Assert.Equal(1, sink.Written[0].Handle);
            Assert.Equal(2, sink.Written[1].Handle);
        }

        [Fact]
        public void Update_WhenSinkThrows_DoesNotPropagate()
        {
            var source = new FakeSource(new[] { T(1) });
            var sink = new FakeSink { Throw = true };
            var recorder = new EngagementEventRecorder(source, sink);

            var ex = Record.Exception(() => recorder.Update());

            Assert.Null(ex); // sampling must never crash the game tick
        }
    }
}
```

- [ ] **Step 2: Verify it fails**

Run: `dotnet build FactionWars.sln --no-incremental`
Expected: FAILS — `EngagementEventRecorder` not found.

- [ ] **Step 3: Implement the recorder**

Create `src/FactionWars/ScriptHookV/Telemetry/EngagementEventRecorder.cs`:

```csharp
using System;
using FactionWars.ScriptHookV.Logging;
using FactionWars.Telemetry.Interfaces;

namespace FactionWars.ScriptHookV.Telemetry
{
    /// <summary>
    /// Each tick, drains the squad controller's buffered engagement phase-change events and forwards
    /// them to the engagement event sink. Wrapped so a bad frame logs and is swallowed — telemetry
    /// must never crash the game tick. Draining on a slower cadence than the controller's decisions
    /// loses no fidelity: each transition is timestamped when it occurred.
    /// </summary>
    public sealed class EngagementEventRecorder
    {
        private readonly ISquadEngagementStateSource _source;
        private readonly IEngagementEventSink _sink;

        public EngagementEventRecorder(ISquadEngagementStateSource source, IEngagementEventSink sink)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _sink = sink ?? throw new ArgumentNullException(nameof(sink));
        }

        public void Update()
        {
            try
            {
                var transitions = _source.DrainEngagementTransitions();
                for (int i = 0; i < transitions.Count; i++)
                {
                    _sink.Write(transitions[i]);
                }
            }
            catch (Exception ex)
            {
                FileLogger.Error("EngagementEventRecorder.Update failed", ex);
            }
        }
    }
}
```

- [ ] **Step 4: Normalize, build, test (GREEN)**

```bash
sed -i 's/\r$//; s/$/\r/' \
  src/FactionWars/ScriptHookV/Telemetry/EngagementEventRecorder.cs \
  tests/FactionWars.Tests/Unit/ScriptHookV/Telemetry/EngagementEventRecorderTests.cs
dotnet build FactionWars.sln --no-incremental
dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~EngagementEventRecorder" --no-build
```
Expected: build clean; both recorder tests pass.

- [ ] **Step 5: Commit**

```bash
git add -A && git commit -m "feat: add EngagementEventRecorder draining transitions to sink"
```

---

## Task 6: Wire the sink + recorder into the game loop

**Files:**
- Modify: `src/FactionWars/ScriptHookV/GameLoopController.cs` (fields)
- Modify: `src/FactionWars/ScriptHookV/GameLoopController.InitializationTelemetry.cs` (build + pass state source)
- Modify: `src/FactionWars/ScriptHookV/GameLoopController.Lifecycle.cs` (SetSaveFile)
- Modify: `src/FactionWars/ScriptHookV/GameLoopController.SystemUpdates.cs` (tick recorder)
- Modify: `src/FactionWars/ScriptHookV/GameLoopController.AbortCleanup.cs` (dispose)

**Interfaces:**
- Consumes: `CsvEngagementEventSink` (Task 4), `EngagementEventRecorder` (Task 5), `_squadStanceController` (an `ISquadEngagementStateSource`), the optional sampler param (Task 3).

> **Note:** `GameLoopController` is the composition root and is not unit-tested by project convention (the architecture guardrails keep native integration out of unit tests). This task is verified by a clean build, the full unit suite staying green, and the in-game CSV check in the Verification section. No new unit test.

- [ ] **Step 1: Add the fields**

In `src/FactionWars/ScriptHookV/GameLoopController.cs`, after:

```csharp
        private CombatBehaviorSampler? _behaviorSampler;
        private IBehaviorTraceSink? _behaviorTraceSink;
```

add:

```csharp
        private EngagementEventRecorder? _engagementEventRecorder;
        private IEngagementEventSink? _engagementEventSink;
```

Ensure `using FactionWars.ScriptHookV.Telemetry;` and `using FactionWars.Telemetry.Interfaces;` are present (add if the build complains).

- [ ] **Step 2: Build the sink + recorder and pass the state source to the sampler**

In `src/FactionWars/ScriptHookV/GameLoopController.InitializationTelemetry.cs`, inside `InitializeBehaviorSampler`, replace:

```csharp
            _behaviorSampler = new CombatBehaviorSampler(_gameBridge, sources, sink);
            FileLogger.Info($"Behavior sampler initialized with {sources.Count} source(s); trace root {telemetryRoot}");
```

with:

```csharp
            var engagementSink = new CsvEngagementEventSink(telemetryRoot);
            _engagementEventSink = engagementSink;
            _engagementEventRecorder = _squadStanceController != null
                ? new EngagementEventRecorder(_squadStanceController, engagementSink)
                : null;

            _behaviorSampler = new CombatBehaviorSampler(_gameBridge, sources, sink, _squadStanceController);
            FileLogger.Info($"Behavior sampler initialized with {sources.Count} source(s); trace root {telemetryRoot}; engagement events {(_engagementEventRecorder != null ? "on" : "off")}");
```

`_squadStanceController` is an `ISquadEngagementStateSource` (Task 2) and is non-null here (built earlier in `InitializeGameData`); the null guard is defensive. Confirm `using FactionWars.Telemetry.Sinks;` is present (it already is).

- [ ] **Step 3: Forward `SetSaveFile`**

In `src/FactionWars/ScriptHookV/GameLoopController.Lifecycle.cs`, find:

```csharp
                _behaviorTraceSink?.SetSaveFile(e.SaveName!);
```

and add immediately after it:

```csharp
                _engagementEventSink?.SetSaveFile(e.SaveName!);
```

- [ ] **Step 4: Tick the recorder**

In `src/FactionWars/ScriptHookV/GameLoopController.SystemUpdates.cs`, find:

```csharp
            _tickProfiler.Measure("behaviorSampler", () => _behaviorSampler?.Update());
```

and add immediately after it:

```csharp
            _tickProfiler.Measure("engagementEvents", () => _engagementEventRecorder?.Update());
```

- [ ] **Step 5: Dispose on cleanup**

In `src/FactionWars/ScriptHookV/GameLoopController.AbortCleanup.cs`, find:

```csharp
            _behaviorSampler = null;
            _behaviorTraceSink?.Dispose();
            _behaviorTraceSink = null;
```

and add immediately after it:

```csharp
            _engagementEventRecorder = null;
            _engagementEventSink?.Dispose();
            _engagementEventSink = null;
```

- [ ] **Step 6: Normalize, build, full unit suite**

```bash
sed -i 's/\r$//; s/$/\r/' \
  src/FactionWars/ScriptHookV/GameLoopController.cs \
  src/FactionWars/ScriptHookV/GameLoopController.InitializationTelemetry.cs \
  src/FactionWars/ScriptHookV/GameLoopController.Lifecycle.cs \
  src/FactionWars/ScriptHookV/GameLoopController.SystemUpdates.cs \
  src/FactionWars/ScriptHookV/GameLoopController.AbortCleanup.cs
dotnet build FactionWars.sln --no-incremental
dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~FactionWars.Tests.Unit" --no-build
```
Expected: build clean (0 warnings/errors); full unit suite passes (3701 baseline + the new tests).

- [ ] **Step 7: Commit**

```bash
git add -A && git commit -m "feat: wire engagement event sink and recorder into the game loop"
```

---

## Verification (after Task 6, in-game)

These telemetry signals make the #98 behavior falsifiable. After deploying and playing a Search & Destroy engagement where an enemy breaks line of sight (drops below a parapet):

1. **`engagement_events.csv`** should contain, for a follower handle, a `LosReposition` row (from_phase `Engage`, to_phase `Advance`) when sight stays broken ≥1500ms, followed by an `EngageAcquired` row once it reaches the edge and regains sight.
2. **`behavior_trace.csv`** for that handle, across the surrounding samples, should show `has_los` flip `true→false`, `ms_since_los` climb past ~1500, `engine_phase` flip `Engage→Advance`, `dist_to_target` decrease (it moved toward the target/edge), then `has_los` return `true`, `engine_phase` return `Engage`, and `is_shooting` become `true`.

If the events fire but `dist_to_target` does not decrease, the resolver decision is correct but the native task isn't moving the ped — a separate (native-tasking) bug, now distinguishable from the decision logic.

---

## Self-Review

**Spec coverage:** Both requested deliverables are covered — CSV fields (Task 3) and structured event log (Tasks 4–5), wired (Task 6), with the resolver reason (Task 1) and controller recording (Task 2) feeding them. ✅

**Type consistency:** `EngagePhaseChangeReason` (Task 1) is used by `EngageDecision`, `EngagementTransition`, and the resolver. `SquadEngagementState`/`EngagementTransition` (Task 2) flow to the sampler (Task 3) and sink (Task 4). `ISquadEngagementStateSource` (both methods) is implemented in Task 2 and consumed in Tasks 3/5/6. Sampler ctor 4th param `ISquadEngagementStateSource?` matches the `_squadStanceController` passed in Task 6. CSV header order (`...,has_los,engine_phase,ms_since_los`) matches the serialize order in Task 3. ✅

**Analyzer compliance:** No constructor exceeds 5 params (`CombatBehaviorSampler` reaches 5 including optional `sampleIntervalMs`; `SquadStanceController` unchanged at 5 — new capability added via explicit interface partial, not ctor). New public-method counts stay well under 10; explicit interface methods are exempt. Each new type is one public top-level type per file. ✅

**Placeholder scan:** Every code step contains complete code; the one prose-only step (Task 6, composition root) is explicitly justified as untestable-by-convention with an in-game verification path. The sampler test helpers reference the existing test file's setup, which the implementer adapts — noted explicitly rather than left vague. ✅
