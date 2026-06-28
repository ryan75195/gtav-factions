# Squad Aggressive Engagement (Search & Destroy) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make Search & Destroy bodyguards aggressively advance to each assigned enemy and open fire at role-appropriate range, instead of holding position.

**Architecture:** Replace the single `TASK_COMBAT_PED` in `ApplySearchAndDestroy` with a per-follower Advance/Engage state machine. A portable `SquadEngagementResolver` decides the phase from `{distToTarget, hasLineOfSight, role, priorPhase, priorLosMisses}` with hysteresis; the ScriptHookV layer probes LOS/range via `IGameBridge` and issues `TaskGoToEntity` (advance) or `TaskCombatPed` (engage) through the existing `PedIntentReconciler`. Target pool becomes all live zone hostiles.

**Tech Stack:** C# / .NET Framework 4.8, ScriptHookVDotNet3, xUnit + Moq, GTA.Native.

## Global Constraints

- `.cs` files MUST be CRLF + UTF-8-no-BOM. The Write tool emits LF — after writing any `.cs` file run `sed -i 's/\r$//; s/$/\r/' <file>`. Edit tool preserves existing endings.
- Analyzer ERRORS block the build: no tuple return types; ≤250 lines/class per file (CI0017); ≤40 effective lines/method (CI0007); one public top-level type per file; ≤5 constructor parameters; ≤10 public methods/class (CI0004); a class in a `.Services` namespace must implement a first-party interface (CI0015).
- All new `GameBridge` native methods MUST include `FileLogger` debug logging (project rule).
- Branch is `feat/83-aggressive-search-and-destroy` (issue #83). Do NOT commit on master.
- Pre-commit hook runs `dotnet build FactionWars.sln --no-incremental` + `dotnet test --filter FactionWars.Tests.Unit`. Every commit must pass both.
- Commit message footer:
  ```
  Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>
  Claude-Session: https://claude.ai/code/session_01MGyei1N5nCAgmp5DmiECej
  ```
- `DefenderRole` (namespace `FactionWars.Core.Models`) values: `Grunt, Gunner, Rifleman, Rocketeer, Sniper`.
- Domain `Vector3` is `FactionWars.Core.Interfaces.Vector3` and has `float DistanceTo(Vector3 other)`.

---

## File Structure

- Create `src/FactionWars/Combat/Interfaces/IEngageRangeProvider.cs` — role → engage range contract.
- Create `src/FactionWars/Combat/Services/EngageRangeProvider.cs` — the range table.
- Create `src/FactionWars/Combat/Models/EngagePhase.cs` — `Advance | Engage` enum.
- Create `src/FactionWars/Combat/Models/EngageDecision.cs` — resolver output struct.
- Create `src/FactionWars/Combat/Interfaces/ISquadEngagementResolver.cs` — phase-brain contract.
- Create `src/FactionWars/Combat/Services/SquadEngagementResolver.cs` — phase brain + hysteresis.
- Modify `src/FactionWars/Combat/Models/PedIntentKind.cs` — add `AdvanceOnTarget`.
- Modify `src/FactionWars/Combat/Models/PedIntent.cs` — add `AdvanceOnTarget` factory.
- Modify `src/FactionWars/Combat/Services/PedIntentReconciler.cs` — add `AdvanceOnTarget` case.
- Modify `src/FactionWars/Core/Interfaces/IGameBridge.cs` — add `HasClearLineOfSight`.
- Create `src/FactionWars/ScriptHookV/GameBridge.LineOfSight.cs` — native impl.
- Modify `src/FactionWars/Core/Utils/MockGameBridge.cs` — LOS hook.
- Modify `src/FactionWars/ScriptHookV/Combat/EnemyTargetCollector.cs` + `Interfaces/IEnemyTargetCollector.cs` — `CollectAll`.
- Modify `src/FactionWars/ScriptHookV/Managers/FollowerManager.cs` + `FollowerManager.Telemetry.cs` — expose `OnFootBodyguardRoles`.
- Modify `src/FactionWars/ScriptHookV/Managers/SquadStanceController.cs` + `.Stances.cs` — engagement resolver, phase state, S&D rewrite.
- Modify `src/FactionWars/ScriptHookV/GameLoopController.SystemUpdates.cs` + `GameLoopController.Initialization.cs` — all-zone pool, role threading, resolver construction.

---

## Task 1: `HasClearLineOfSight` native + mock

**Files:**
- Modify: `src/FactionWars/Core/Interfaces/IGameBridge.cs`
- Create: `src/FactionWars/ScriptHookV/GameBridge.LineOfSight.cs`
- Modify: `src/FactionWars/Core/Utils/MockGameBridge.cs`
- Test: `tests/FactionWars.Tests/Unit/Core/Utils/MockGameBridgeLineOfSightTests.cs`

**Interfaces:**
- Produces: `bool IGameBridge.HasClearLineOfSight(int fromPedHandle, int toPedHandle)`; mock hook `MockGameBridge.SetLineOfSight(int from, int to, bool clear)`.

- [ ] **Step 1: Write the failing test** (`tests/FactionWars.Tests/Unit/Core/Utils/MockGameBridgeLineOfSightTests.cs`)

```csharp
using FactionWars.Core.Utils;
using Xunit;

namespace FactionWars.Tests.Unit.Core.Utils
{
    public class MockGameBridgeLineOfSightTests
    {
        [Fact]
        public void HasClearLineOfSight_DefaultsFalse()
        {
            var bridge = new MockGameBridge();
            Assert.False(bridge.HasClearLineOfSight(1, 2));
        }

        [Fact]
        public void HasClearLineOfSight_ReturnsWhatWasSet()
        {
            var bridge = new MockGameBridge();
            bridge.SetLineOfSight(1, 2, true);
            Assert.True(bridge.HasClearLineOfSight(1, 2));
            Assert.False(bridge.HasClearLineOfSight(2, 1)); // direction-specific unless also set
        }
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~MockGameBridgeLineOfSight"`
Expected: FAIL — `IGameBridge` / `MockGameBridge` has no `HasClearLineOfSight` / `SetLineOfSight` (compile error).

- [ ] **Step 3: Add the interface method**

In `src/FactionWars/Core/Interfaces/IGameBridge.cs`, add near the other read-only ped observers:

```csharp
        /// <summary>
        /// True if <paramref name="fromPedHandle"/> has a clear line of sight to
        /// <paramref name="toPedHandle"/> (wraps HAS_ENTITY_CLEAR_LOS_TO_ENTITY). False for
        /// invalid/dead handles.
        /// </summary>
        bool HasClearLineOfSight(int fromPedHandle, int toPedHandle);
```

- [ ] **Step 4: Add the real native impl** (`src/FactionWars/ScriptHookV/GameBridge.LineOfSight.cs`)

```csharp
using System;
using FactionWars.ScriptHookV.Logging;
using GTA;
using GTA.Native;

namespace FactionWars.ScriptHookV
{
    public partial class GameBridge
    {
        /// <inheritdoc />
        public bool HasClearLineOfSight(int fromPedHandle, int toPedHandle)
        {
            try
            {
                var from = Entity.FromHandle(fromPedHandle) as Ped;
                var to = Entity.FromHandle(toPedHandle) as Ped;
                if (from == null || !from.Exists() || to == null || !to.Exists())
                {
                    return false;
                }

                // HAS_ENTITY_CLEAR_LOS_TO_ENTITY: trace flag 17 = world geometry + vehicles + objects.
                return Function.Call<bool>(
                    Hash.HAS_ENTITY_CLEAR_LOS_TO_ENTITY, from.Handle, to.Handle, 17);
            }
            catch (Exception ex)
            {
                FileLogger.Error($"HasClearLineOfSight exception for {fromPedHandle}->{toPedHandle}", ex);
                return false;
            }
        }
    }
}
```

- [ ] **Step 5: Add the mock hook** (in `src/FactionWars/Core/Utils/MockGameBridge.cs`, near the other ped-state hooks)

```csharp
        private readonly HashSet<(int, int)> _clearLos = new HashSet<(int, int)>();

        /// <summary>Test hook: sets whether `from` has clear LOS to `to`.</summary>
        public void SetLineOfSight(int from, int to, bool clear)
        {
            if (clear) _clearLos.Add((from, to));
            else _clearLos.Remove((from, to));
        }

        /// <inheritdoc />
        public bool HasClearLineOfSight(int fromPedHandle, int toPedHandle)
            => _clearLos.Contains((fromPedHandle, toPedHandle));
```

(`System.Collections.Generic` is already imported in `MockGameBridge.cs`.)

- [ ] **Step 6: CRLF-convert new file and run the test**

```bash
sed -i 's/\r$//; s/$/\r/' src/FactionWars/ScriptHookV/GameBridge.LineOfSight.cs tests/FactionWars.Tests/Unit/Core/Utils/MockGameBridgeLineOfSightTests.cs
dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~MockGameBridgeLineOfSight"
```
Expected: PASS (2 tests).

- [ ] **Step 7: Commit**

```bash
git add -A && git commit -m "feat: add HasClearLineOfSight native + mock hook"
```

---

## Task 2: `EngageRangeProvider` (role → range)

**Files:**
- Create: `src/FactionWars/Combat/Interfaces/IEngageRangeProvider.cs`
- Create: `src/FactionWars/Combat/Services/EngageRangeProvider.cs`
- Test: `tests/FactionWars.Tests/Unit/Combat/EngageRangeProviderTests.cs`

**Interfaces:**
- Produces: `float IEngageRangeProvider.For(DefenderRole role)`.

- [ ] **Step 1: Write the failing test**

```csharp
using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Services;
using FactionWars.Core.Models;
using Xunit;

namespace FactionWars.Tests.Unit.Combat
{
    public class EngageRangeProviderTests
    {
        private readonly IEngageRangeProvider _provider = new EngageRangeProvider();

        [Theory]
        [InlineData(DefenderRole.Sniper, 80f)]
        [InlineData(DefenderRole.Rocketeer, 45f)]
        [InlineData(DefenderRole.Rifleman, 45f)]
        [InlineData(DefenderRole.Gunner, 35f)]
        [InlineData(DefenderRole.Grunt, 18f)]
        public void For_KnownRole_ReturnsTableValue(DefenderRole role, float expected)
        {
            Assert.Equal(expected, _provider.For(role));
        }
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~EngageRangeProvider"`
Expected: FAIL — types don't exist (compile error).

- [ ] **Step 3: Create the interface** (`src/FactionWars/Combat/Interfaces/IEngageRangeProvider.cs`)

```csharp
using FactionWars.Core.Models;

namespace FactionWars.Combat.Interfaces
{
    /// <summary>Maps a defender role to the distance (metres) at which it should stop advancing
    /// and engage. Tunable in one place.</summary>
    public interface IEngageRangeProvider
    {
        float For(DefenderRole role);
    }
}
```

- [ ] **Step 4: Create the provider** (`src/FactionWars/Combat/Services/EngageRangeProvider.cs`)

```csharp
using FactionWars.Combat.Interfaces;
using FactionWars.Core.Models;

namespace FactionWars.Combat.Services
{
    /// <summary>Role → engage range table. Values are starting points, tunable here.</summary>
    public sealed class EngageRangeProvider : IEngageRangeProvider
    {
        private const float Fallback = 30f;

        public float For(DefenderRole role)
        {
            switch (role)
            {
                case DefenderRole.Sniper: return 80f;
                case DefenderRole.Rocketeer: return 45f;
                case DefenderRole.Rifleman: return 45f;
                case DefenderRole.Gunner: return 35f;
                case DefenderRole.Grunt: return 18f;
                default: return Fallback;
            }
        }
    }
}
```

- [ ] **Step 5: CRLF-convert and run**

```bash
sed -i 's/\r$//; s/$/\r/' src/FactionWars/Combat/Interfaces/IEngageRangeProvider.cs src/FactionWars/Combat/Services/EngageRangeProvider.cs tests/FactionWars.Tests/Unit/Combat/EngageRangeProviderTests.cs
dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~EngageRangeProvider"
```
Expected: PASS (5 cases).

- [ ] **Step 6: Commit**

```bash
git add -A && git commit -m "feat: add EngageRangeProvider role->range table"
```

---

## Task 3: `SquadEngagementResolver` (phase brain + hysteresis)

**Files:**
- Create: `src/FactionWars/Combat/Models/EngagePhase.cs`
- Create: `src/FactionWars/Combat/Models/EngageDecision.cs`
- Create: `src/FactionWars/Combat/Interfaces/ISquadEngagementResolver.cs`
- Create: `src/FactionWars/Combat/Services/SquadEngagementResolver.cs`
- Test: `tests/FactionWars.Tests/Unit/Combat/SquadEngagementResolverTests.cs`

**Interfaces:**
- Consumes: `IEngageRangeProvider.For(DefenderRole)` (Task 2).
- Produces: `EngageDecision ISquadEngagementResolver.Resolve(float distToTarget, bool hasLineOfSight, DefenderRole role, EngagePhase currentPhase, int consecutiveLosMisses)`. `EngageDecision { EngagePhase Phase; float EngageRange; int ConsecutiveLosMisses; }`. Enum `EngagePhase { Advance, Engage }`.

- [ ] **Step 1: Write the failing test**

```csharp
using FactionWars.Combat.Models;
using FactionWars.Combat.Services;
using FactionWars.Core.Models;
using Xunit;

namespace FactionWars.Tests.Unit.Combat
{
    public class SquadEngagementResolverTests
    {
        // Grunt range = 18; hysteresis band = 18 * 1.3 = 23.4; LOS grace = 2.
        private readonly SquadEngagementResolver _resolver =
            new SquadEngagementResolver(new EngageRangeProvider());

        private EngageDecision Resolve(float dist, bool los, EngagePhase phase, int losMisses)
            => _resolver.Resolve(dist, los, DefenderRole.Grunt, phase, losMisses);

        [Fact]
        public void OutOfRange_Advances()
        {
            var d = Resolve(40f, true, EngagePhase.Advance, 0);
            Assert.Equal(EngagePhase.Advance, d.Phase);
            Assert.Equal(18f, d.EngageRange);
        }

        [Fact]
        public void InRangeWithLos_Engages()
        {
            var d = Resolve(15f, true, EngagePhase.Advance, 0);
            Assert.Equal(EngagePhase.Engage, d.Phase);
            Assert.Equal(0, d.ConsecutiveLosMisses);
        }

        [Fact]
        public void InRangeNoLos_StaysAdvance()
        {
            var d = Resolve(15f, false, EngagePhase.Advance, 0);
            Assert.Equal(EngagePhase.Advance, d.Phase);
        }

        [Fact]
        public void Engaging_WithinHysteresisBand_StaysEngaged()
        {
            // 20m is > range(18) but <= 18*1.3 (23.4): keep engaging.
            var d = Resolve(20f, true, EngagePhase.Engage, 0);
            Assert.Equal(EngagePhase.Engage, d.Phase);
        }

        [Fact]
        public void Engaging_PastHysteresisBand_DropsToAdvance()
        {
            var d = Resolve(30f, true, EngagePhase.Engage, 0);
            Assert.Equal(EngagePhase.Advance, d.Phase);
        }

        [Fact]
        public void Engaging_FirstLosMiss_StaysEngaged_CounterIncrements()
        {
            var d = Resolve(15f, false, EngagePhase.Engage, 0);
            Assert.Equal(EngagePhase.Engage, d.Phase);
            Assert.Equal(1, d.ConsecutiveLosMisses);
        }

        [Fact]
        public void Engaging_SecondLosMiss_DropsToAdvance()
        {
            var d = Resolve(15f, false, EngagePhase.Engage, 1);
            Assert.Equal(EngagePhase.Advance, d.Phase);
            Assert.Equal(2, d.ConsecutiveLosMisses);
        }

        [Fact]
        public void Engaging_LosRegained_ResetsMissCounter()
        {
            var d = Resolve(15f, true, EngagePhase.Engage, 1);
            Assert.Equal(EngagePhase.Engage, d.Phase);
            Assert.Equal(0, d.ConsecutiveLosMisses);
        }
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~SquadEngagementResolver"`
Expected: FAIL — types don't exist.

- [ ] **Step 3: Create the enum** (`src/FactionWars/Combat/Models/EngagePhase.cs`)

```csharp
namespace FactionWars.Combat.Models
{
    /// <summary>Per-follower combat phase in Search &amp; Destroy.</summary>
    public enum EngagePhase
    {
        /// <summary>Running toward the assigned enemy; not yet in range/LOS.</summary>
        Advance,

        /// <summary>In weapon range with line of sight; fighting the assigned enemy.</summary>
        Engage
    }
}
```

- [ ] **Step 4: Create the decision struct** (`src/FactionWars/Combat/Models/EngageDecision.cs`)

```csharp
namespace FactionWars.Combat.Models
{
    /// <summary>Output of <c>ISquadEngagementResolver.Resolve</c>: the new phase, the role's engage
    /// range (used as the advance stopping range), and the updated consecutive-LOS-miss counter.</summary>
    public readonly struct EngageDecision
    {
        public EngageDecision(EngagePhase phase, float engageRange, int consecutiveLosMisses)
        {
            Phase = phase;
            EngageRange = engageRange;
            ConsecutiveLosMisses = consecutiveLosMisses;
        }

        public EngagePhase Phase { get; }

        public float EngageRange { get; }

        public int ConsecutiveLosMisses { get; }
    }
}
```

- [ ] **Step 5: Create the interface** (`src/FactionWars/Combat/Interfaces/ISquadEngagementResolver.cs`)

```csharp
using FactionWars.Combat.Models;
using FactionWars.Core.Models;

namespace FactionWars.Combat.Interfaces
{
    /// <summary>Decides whether a follower should advance toward or engage its assigned enemy,
    /// with hysteresis so the phase cannot flip every tick.</summary>
    public interface ISquadEngagementResolver
    {
        EngageDecision Resolve(
            float distToTarget,
            bool hasLineOfSight,
            DefenderRole role,
            EngagePhase currentPhase,
            int consecutiveLosMisses);
    }
}
```

- [ ] **Step 6: Create the resolver** (`src/FactionWars/Combat/Services/SquadEngagementResolver.cs`)

```csharp
using System;
using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Models;
using FactionWars.Core.Models;

namespace FactionWars.Combat.Services
{
    /// <summary>Default <see cref="ISquadEngagementResolver"/>. Advance until in range + LOS, then
    /// engage; drop back to advance only past the hysteresis band or after sustained LOS loss.</summary>
    public sealed class SquadEngagementResolver : ISquadEngagementResolver
    {
        private const float HysteresisFactor = 1.3f;
        private const int LosGraceMisses = 2;

        private readonly IEngageRangeProvider _rangeProvider;

        public SquadEngagementResolver(IEngageRangeProvider rangeProvider)
        {
            _rangeProvider = rangeProvider ?? throw new ArgumentNullException(nameof(rangeProvider));
        }

        public EngageDecision Resolve(
            float distToTarget,
            bool hasLineOfSight,
            DefenderRole role,
            EngagePhase currentPhase,
            int consecutiveLosMisses)
        {
            float range = _rangeProvider.For(role);
            int losMisses = hasLineOfSight ? 0 : consecutiveLosMisses + 1;

            if (currentPhase == EngagePhase.Engage)
            {
                bool rangeBroken = distToTarget > range * HysteresisFactor;
                bool losBroken = losMisses >= LosGraceMisses;
                var phase = (rangeBroken || losBroken) ? EngagePhase.Advance : EngagePhase.Engage;
                return new EngageDecision(phase, range, losMisses);
            }

            bool canEngage = distToTarget <= range && hasLineOfSight;
            return new EngageDecision(canEngage ? EngagePhase.Engage : EngagePhase.Advance, range, losMisses);
        }
    }
}
```

- [ ] **Step 7: CRLF-convert and run**

```bash
sed -i 's/\r$//; s/$/\r/' src/FactionWars/Combat/Models/EngagePhase.cs src/FactionWars/Combat/Models/EngageDecision.cs src/FactionWars/Combat/Interfaces/ISquadEngagementResolver.cs src/FactionWars/Combat/Services/SquadEngagementResolver.cs tests/FactionWars.Tests/Unit/Combat/SquadEngagementResolverTests.cs
dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~SquadEngagementResolver"
```
Expected: PASS (8 tests).

- [ ] **Step 8: Commit**

```bash
git add -A && git commit -m "feat: add SquadEngagementResolver phase brain with hysteresis"
```

---

## Task 4: `AdvanceOnTarget` PedIntent + reconciler case

**Files:**
- Modify: `src/FactionWars/Combat/Models/PedIntentKind.cs`
- Modify: `src/FactionWars/Combat/Models/PedIntent.cs`
- Modify: `src/FactionWars/Combat/Services/PedIntentReconciler.cs`
- Test: `tests/FactionWars.Tests/Unit/Combat/PedIntentReconcilerAdvanceTests.cs`

**Interfaces:**
- Produces: `PedIntent.AdvanceOnTarget(int targetHandle, float stoppingRange)` → `Kind = AdvanceOnTarget`, `Discriminator = targetHandle`, `Radius = stoppingRange`. Reconciler applies `RemovePedFromFollowerGroup` + `TaskGoToEntity(ped, targetHandle, stoppingRange)`.

- [ ] **Step 1: Write the failing test** (uses `MockGameBridge`, which records native calls)

```csharp
using FactionWars.Combat.Models;
using FactionWars.Combat.Services;
using FactionWars.Core.Utils;
using Xunit;

namespace FactionWars.Tests.Unit.Combat
{
    public class PedIntentReconcilerAdvanceTests
    {
        [Fact]
        public void AdvanceOnTarget_TasksGoToEntity_AtStoppingRange()
        {
            var bridge = new MockGameBridge();
            var reconciler = new PedIntentReconciler(bridge);

            reconciler.Submit(50, PedIntent.AdvanceOnTarget(99, 18f));

            Assert.Equal(99, bridge.GetGoToEntityTarget(50)!.Value);
            Assert.Equal(18f, bridge.GetGoToEntityStoppingRange(50)!.Value);
        }

        [Fact]
        public void AdvanceOnTarget_SameTarget_NotReissued()
        {
            var bridge = new MockGameBridge();
            var reconciler = new PedIntentReconciler(bridge);

            reconciler.Submit(50, PedIntent.AdvanceOnTarget(99, 18f));
            int callsAfterFirst = bridge.GetGoToEntityCallCount(50);
            reconciler.Submit(50, PedIntent.AdvanceOnTarget(99, 18f)); // identical -> deduped

            Assert.Equal(callsAfterFirst, bridge.GetGoToEntityCallCount(50));
        }

        [Fact]
        public void AdvanceOnTarget_NewTarget_Reissued()
        {
            var bridge = new MockGameBridge();
            var reconciler = new PedIntentReconciler(bridge);

            reconciler.Submit(50, PedIntent.AdvanceOnTarget(99, 18f));
            reconciler.Submit(50, PedIntent.AdvanceOnTarget(123, 18f)); // different discriminator

            Assert.Equal(123, bridge.GetGoToEntityTarget(50));
        }
    }
}
```

- [ ] **Step 2: Add a call counter to MockGameBridge**

The mock already records go-to-entity state and exposes `int? GetGoToEntityTarget(int)` and `float? GetGoToEntityStoppingRange(int)` (do NOT re-add these). It has **no call counter**, which the dedup test needs. Near the existing `_goToEntityPeds` field add:

```csharp
        private readonly Dictionary<int, int> _goToEntityCalls = new Dictionary<int, int>();

        public int GetGoToEntityCallCount(int pedHandle)
            => _goToEntityCalls.TryGetValue(pedHandle, out var c) ? c : 0;
```

Then add this one line inside the existing `TaskGoToEntity(int pedHandle, ...)` body, inside the `if (_peds.ContainsKey(pedHandle))` block (right after it sets `_goToEntityPeds[pedHandle] = ...`):

```csharp
                _goToEntityCalls[pedHandle] = (_goToEntityCalls.TryGetValue(pedHandle, out var goToCount) ? goToCount : 0) + 1;
```

- [ ] **Step 3: Run test to verify it fails**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~PedIntentReconcilerAdvance"`
Expected: FAIL — `PedIntent.AdvanceOnTarget` does not exist.

- [ ] **Step 4: Add the enum value** (`src/FactionWars/Combat/Models/PedIntentKind.cs`)

Add `AdvanceOnTarget,` to the enum after `CombatTarget,`:

```csharp
        Idle,
        FollowPlayer,
        GuardArea,
        CombatTarget,
        AdvanceOnTarget,
        SeekHatedTargets,
        WanderArea,
        GoToCoord,
        LeaveVehicle
```

- [ ] **Step 5: Add the factory** (`src/FactionWars/Combat/Models/PedIntent.cs`, after the `CombatTarget` factory)

```csharp
        /// <summary>Run toward a target ped, stopping at <paramref name="stoppingRange"/> metres,
        /// without engaging yet. Tracks the moving target (TASK_GO_TO_ENTITY).</summary>
        public static PedIntent AdvanceOnTarget(int targetHandle, float stoppingRange)
            => new PedIntent(PedIntentKind.AdvanceOnTarget, targetHandle, default, stoppingRange);
```

- [ ] **Step 6: Add the reconciler case** (`src/FactionWars/Combat/Services/PedIntentReconciler.cs`, in the `Apply` switch, after the `CombatTarget` case)

```csharp
                case PedIntentKind.AdvanceOnTarget:
                    _gameBridge.RemovePedFromFollowerGroup(pedHandle);
                    _gameBridge.TaskGoToEntity(pedHandle, intent.Discriminator, intent.Radius);
                    break;
```

- [ ] **Step 7: CRLF-convert the new test and run**

```bash
sed -i 's/\r$//; s/$/\r/' tests/FactionWars.Tests/Unit/Combat/PedIntentReconcilerAdvanceTests.cs
dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~PedIntentReconcilerAdvance"
```
Expected: PASS (3 tests).

- [ ] **Step 8: Commit**

```bash
git add -A && git commit -m "feat: add AdvanceOnTarget ped intent + reconciler case"
```

---

## Task 5: Expose follower roles for engage-range lookup

**Files:**
- Modify: `src/FactionWars/ScriptHookV/Managers/FollowerManager.Telemetry.cs` (add the role map field + accessor — it already imports the telemetry/role types)
- Modify: `src/FactionWars/ScriptHookV/Managers/FollowerManager.cs` (populate the map in `Update`)
- Test: `tests/FactionWars.Tests/Unit/ScriptHookV/Managers/FollowerManagerRolesTests.cs`

**Interfaces:**
- Produces: `IReadOnlyDictionary<int, DefenderRole> FollowerManager.OnFootBodyguardRoles` — handle→role for the alive on-foot followers from the most recent `Update`. Empty when player in vehicle / no faction.

> Note: `FollowerManager` already builds `_trackedFollowers` (a `TrackedCombatant` list with `Handle` + `Role`) each `Update` (see `FollowerManager.Telemetry.cs`). Reuse it to build the role map so there is a single source.

- [ ] **Step 1: Write the failing test**

```csharp
using System.Collections.Generic;
using FactionWars.Combat.Interfaces;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.ScriptHookV.Managers;
using FactionWars.UI.Interfaces;
using Moq;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV.Managers
{
    public class FollowerManagerRolesTests
    {
        [Fact]
        public void OnFootBodyguardRoles_AfterUpdate_MapsAliveFollowersToRole()
        {
            var gameBridge = new Mock<IGameBridge>();
            gameBridge.Setup(g => g.IsPlayerInVehicle()).Returns(false);
            gameBridge.Setup(g => g.IsPedAlive(10)).Returns(true);
            gameBridge.Setup(g => g.IsPedAlive(11)).Returns(true);
            var followerService = new Mock<IFollowerService>();
            followerService.Setup(s => s.GetFollowers("player")).Returns(new List<Follower>
            {
                new Follower("player", DefenderRole.Sniper, 10),
                new Follower("player", DefenderRole.Grunt, 11)
            });

            var manager = new FollowerManager(
                gameBridge.Object, followerService.Object,
                new Mock<IPedSpawningService>().Object, new Mock<IDefenderRoleService>().Object,
                new Mock<IPedBlipService>().Object, new Mock<IVehicleSeatPriorityService>().Object);

            manager.Update("player", boardPlayerVehicle: false);

            Assert.Equal(DefenderRole.Sniper, manager.OnFootBodyguardRoles[10]);
            Assert.Equal(DefenderRole.Grunt, manager.OnFootBodyguardRoles[11]);
        }

        [Fact]
        public void OnFootBodyguardRoles_EmptyFaction_IsEmpty()
        {
            var gameBridge = new Mock<IGameBridge>();
            var manager = new FollowerManager(
                gameBridge.Object, new Mock<IFollowerService>().Object,
                new Mock<IPedSpawningService>().Object, new Mock<IDefenderRoleService>().Object,
                new Mock<IPedBlipService>().Object, new Mock<IVehicleSeatPriorityService>().Object);

            manager.Update("", boardPlayerVehicle: false);

            Assert.Empty(manager.OnFootBodyguardRoles);
        }
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~FollowerManagerRoles"`
Expected: FAIL — `OnFootBodyguardRoles` does not exist.

- [ ] **Step 3: Add the property + builder** (in `src/FactionWars/ScriptHookV/Managers/FollowerManager.Telemetry.cs`, inside the partial class)

```csharp
        private IReadOnlyDictionary<int, DefenderRole> _onFootBodyguardRoles =
            new Dictionary<int, DefenderRole>();

        /// <summary>Handle → role for the alive on-foot followers from the most recent Update.
        /// Empty when the player is in a vehicle or has no faction.</summary>
        public IReadOnlyDictionary<int, DefenderRole> OnFootBodyguardRoles => _onFootBodyguardRoles;

        private void CaptureFollowerRoles(IReadOnlyList<TrackedCombatant> tracked)
        {
            var map = new Dictionary<int, DefenderRole>(tracked.Count);
            foreach (var t in tracked) map[t.Handle] = t.Role;
            _onFootBodyguardRoles = map;
        }
```

(The file already imports `System.Collections.Generic`, `FactionWars.Core.Models`, and `FactionWars.Telemetry.Models`.)

- [ ] **Step 4: Populate it in `Update`** (`src/FactionWars/ScriptHookV/Managers/FollowerManager.cs`)

In the early-return block, after `_trackedFollowers = Array.Empty<TrackedCombatant>();` add:

```csharp
                _onFootBodyguardRoles = new Dictionary<int, DefenderRole>();
```

After the existing `_trackedFollowers = BuildTrackedFollowers(followers, aliveFollowerHandles);` line add:

```csharp
            CaptureFollowerRoles(_trackedFollowers);
```

(`System.Collections.Generic` is already imported in `FollowerManager.cs`.)

- [ ] **Step 5: Run test**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~FollowerManagerRoles"` then CRLF-convert the new test file first:
```bash
sed -i 's/\r$//; s/$/\r/' tests/FactionWars.Tests/Unit/ScriptHookV/Managers/FollowerManagerRolesTests.cs
dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~FollowerManagerRoles"
```
Expected: PASS (2 tests).

- [ ] **Step 6: Commit**

```bash
git add -A && git commit -m "feat: expose FollowerManager.OnFootBodyguardRoles for engage-range lookup"
```

---

## Task 6: `CollectAll` on the enemy target collector

**Files:**
- Modify: `src/FactionWars/ScriptHookV/Combat/Interfaces/IEnemyTargetCollector.cs`
- Modify: `src/FactionWars/ScriptHookV/Combat/EnemyTargetCollector.cs`
- Test: `tests/FactionWars.Tests/Unit/ScriptHookV/Combat/EnemyTargetCollectorTests.cs` (add to existing if present; else create)

**Interfaces:**
- Produces: `IReadOnlyList<EnemyTarget> IEnemyTargetCollector.CollectAll(IReadOnlyList<int> hostileHandles)` — every hostile with its live position, no radius gate.

- [ ] **Step 1: Write the failing test**

```csharp
using System.Collections.Generic;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Utils;
using FactionWars.ScriptHookV.Combat;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV.Combat
{
    public class EnemyTargetCollectorAllTests
    {
        [Fact]
        public void CollectAll_IncludesEveryHostile_RegardlessOfDistance()
        {
            var bridge = new MockGameBridge();
            int near = bridge.CreatePed("a", new Vector3(0f, 0f, 0f));
            int far = bridge.CreatePed("b", new Vector3(5000f, 0f, 0f));
            var collector = new EnemyTargetCollector(bridge);

            var result = collector.CollectAll(new List<int> { near, far });

            Assert.Equal(2, result.Count);
            Assert.Contains(result, t => t.Handle == far);
        }

        [Fact]
        public void CollectAll_NullInput_ReturnsEmpty()
        {
            var collector = new EnemyTargetCollector(new MockGameBridge());
            Assert.Empty(collector.CollectAll(null));
        }
    }
}
```

- [ ] **Step 2: Run to verify it fails**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~EnemyTargetCollectorAll"`
Expected: FAIL — `CollectAll` not defined.

- [ ] **Step 3: Add to the interface** (`src/FactionWars/ScriptHookV/Combat/Interfaces/IEnemyTargetCollector.cs`)

```csharp
        /// <summary>Every hostile with its live position, no radius filter (whole-zone target pool).</summary>
        IReadOnlyList<EnemyTarget> CollectAll(IReadOnlyList<int> hostileHandles);
```

- [ ] **Step 4: Implement it** (`src/FactionWars/ScriptHookV/Combat/EnemyTargetCollector.cs`)

```csharp
        public IReadOnlyList<EnemyTarget> CollectAll(IReadOnlyList<int> hostileHandles)
        {
            var result = new List<EnemyTarget>();
            if (hostileHandles == null) return result;

            foreach (var handle in hostileHandles)
            {
                result.Add(new EnemyTarget(handle, _gameBridge.GetPedPosition(handle)));
            }

            return result;
        }
```

- [ ] **Step 5: CRLF-convert and run**

```bash
sed -i 's/\r$//; s/$/\r/' tests/FactionWars.Tests/Unit/ScriptHookV/Combat/EnemyTargetCollectorAllTests.cs
dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~EnemyTargetCollectorAll"
```
Expected: PASS (2 tests).

- [ ] **Step 6: Commit**

```bash
git add -A && git commit -m "feat: add EnemyTargetCollector.CollectAll (whole-zone pool)"
```

---

## Task 7: Rewrite `ApplySearchAndDestroy` to advance-then-engage

**Files:**
- Modify: `src/FactionWars/ScriptHookV/Managers/SquadStanceController.cs` (ctor gains `ISquadEngagementResolver`; add phase/LOS-miss state dicts; prune them; `Update` gains a `rolesByHandle` parameter)
- Modify: `src/FactionWars/ScriptHookV/Managers/SquadStanceController.Stances.cs` (rewrite `ApplySearchAndDestroy`)
- Test: `tests/FactionWars.Tests/Unit/ScriptHookV/Managers/SquadStanceControllerSearchDestroyTests.cs` (new file; existing SquadStance tests must be updated for the new ctor + `Update` signature)

**Interfaces:**
- Consumes: `ISquadEngagementResolver.Resolve(...)` (Task 3), `IGameBridge.HasClearLineOfSight` (Task 1), `PedIntent.AdvanceOnTarget` / `PedIntent.CombatTarget` (Task 4), `EngageDecision` (Task 3).
- Produces: `SquadStanceController(IGameBridge, ISquadStanceResolver, ITargetAssignmentResolver, IPedIntentReconciler, ISquadEngagementResolver)`; `Update(Vector3 anchorCenter, float anchorRadius, IReadOnlyList<int> onFootBodyguardHandles, IReadOnlyList<EnemyTarget> enemiesInZone, IReadOnlyDictionary<int, DefenderRole> rolesByHandle)`.

- [ ] **Step 1: Write the failing test** (new file)

```csharp
using System.Collections.Generic;
using FactionWars.Combat.Models;
using FactionWars.Combat.Services;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.Core.Utils;
using FactionWars.ScriptHookV.Managers;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV.Managers
{
    public class SquadStanceControllerSearchDestroyTests
    {
        private readonly MockGameBridge _bridge = new MockGameBridge();
        private readonly SquadStanceController _controller;

        public SquadStanceControllerSearchDestroyTests()
        {
            _controller = new SquadStanceController(
                _bridge,
                new SquadStanceResolver(),
                new TargetAssignmentResolver(),
                new PedIntentReconciler(_bridge),
                new SquadEngagementResolver(new EngageRangeProvider()));
            // Put the controller into Search & Destroy (Escort -> HoldArea -> SearchAndDestroy).
            var handles = new List<int> { 0 };
            _controller.CycleStance(handles);
            _controller.CycleStance(handles);
        }

        private static IReadOnlyDictionary<int, DefenderRole> Roles(int handle, DefenderRole role)
            => new Dictionary<int, DefenderRole> { [handle] = role };

        [Fact]
        public void OutOfRange_IssuesAdvance_GoToEntity()
        {
            int follower = _bridge.CreatePed("f", new Vector3(0f, 0f, 0f));
            int enemy = _bridge.CreatePed("e", new Vector3(60f, 0f, 0f)); // 60m > Grunt range 18
            _bridge.SetLineOfSight(follower, enemy, true);
            var enemies = new List<EnemyTarget> { new EnemyTarget(enemy, _bridge.GetPedPosition(enemy)) };

            _controller.Update(new Vector3(0f, 0f, 0f), 250f,
                new List<int> { follower }, enemies, Roles(follower, DefenderRole.Grunt));

            Assert.Equal(enemy, _bridge.GetGoToEntityTarget(follower)!.Value); // advancing
        }

        [Fact]
        public void InRangeWithLos_IssuesCombat()
        {
            int follower = _bridge.CreatePed("f", new Vector3(0f, 0f, 0f));
            int enemy = _bridge.CreatePed("e", new Vector3(10f, 0f, 0f)); // 10m <= Grunt range 18
            _bridge.SetLineOfSight(follower, enemy, true);
            var enemies = new List<EnemyTarget> { new EnemyTarget(enemy, _bridge.GetPedPosition(enemy)) };

            _controller.Update(new Vector3(0f, 0f, 0f), 250f,
                new List<int> { follower }, enemies, Roles(follower, DefenderRole.Grunt));

            Assert.True(_bridge.IsPedInCombat(follower)); // TaskCombatPed marks in-combat in the mock
        }
    }
}
```

- [ ] **Step 2: Run to verify it fails**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~SquadStanceControllerSearchDestroy"`
Expected: FAIL — ctor / `Update` signature mismatch (compile error).

- [ ] **Step 3: Update the controller core** (`src/FactionWars/ScriptHookV/Managers/SquadStanceController.cs`)

Add the new field (the class already declares `_reconciler`; add only the resolver) and replace the ctor (keeps it at 5 params):

```csharp
        private readonly ISquadEngagementResolver _engagementResolver;
```

```csharp
        public SquadStanceController(IGameBridge gameBridge, ISquadStanceResolver stanceResolver, ITargetAssignmentResolver assignmentResolver, IPedIntentReconciler reconciler, ISquadEngagementResolver engagementResolver)
        {
            _gameBridge = gameBridge ?? throw new ArgumentNullException(nameof(gameBridge));
            _stanceResolver = stanceResolver ?? throw new ArgumentNullException(nameof(stanceResolver));
            _assignmentResolver = assignmentResolver ?? throw new ArgumentNullException(nameof(assignmentResolver));
            _reconciler = reconciler ?? throw new ArgumentNullException(nameof(reconciler));
            _engagementResolver = engagementResolver ?? throw new ArgumentNullException(nameof(engagementResolver));
        }
```

Add per-ped phase state near `_lastApplied`:

```csharp
        private readonly Dictionary<int, EngagePhase> _enginePhase = new Dictionary<int, EngagePhase>();
        private readonly Dictionary<int, int> _losMisses = new Dictionary<int, int>();
        private IReadOnlyDictionary<int, DefenderRole> _rolesByHandle = new Dictionary<int, DefenderRole>();
```

Change the `Update` signature and stash the roles (the existing body keeps calling `ApplySearchAndDestroy(...)`):

```csharp
        public void Update(Vector3 anchorCenter, float anchorRadius, IReadOnlyList<int> onFootBodyguardHandles, IReadOnlyList<EnemyTarget> enemiesInZone, IReadOnlyDictionary<int, DefenderRole> rolesByHandle)
        {
            _rolesByHandle = rolesByHandle ?? new Dictionary<int, DefenderRole>();
            PruneStale(onFootBodyguardHandles);
            if (onFootBodyguardHandles == null || onFootBodyguardHandles.Count == 0)
            {
                return;
            }

            switch (_currentStance)
            {
                case SquadStance.HoldArea:
                    ApplyHoldArea(onFootBodyguardHandles);
                    break;
                case SquadStance.SearchAndDestroy:
                    ApplySearchAndDestroy(anchorCenter, anchorRadius, onFootBodyguardHandles, enemiesInZone);
                    break;
                default:
                    ApplyEscort(onFootBodyguardHandles);
                    break;
            }
        }
```

Add the needed `using FactionWars.Core.Models;` import (for `DefenderRole`) at the top of the file if not present.

In `PruneStale`, drop the per-ped engagement state too — add inside the existing prune loop body after `_reconciler.Forget(handle);`:

```csharp
                    _enginePhase.Remove(handle);
                    _losMisses.Remove(handle);
```

Also clear them in `CycleStance` after `_reconciler.Clear();`:

```csharp
            _enginePhase.Clear();
            _losMisses.Clear();
```

- [ ] **Step 4: Rewrite `ApplySearchAndDestroy`** (`src/FactionWars/ScriptHookV/Managers/SquadStanceController.Stances.cs`)

Replace the whole `ApplySearchAndDestroy` method with:

```csharp
        private void ApplySearchAndDestroy(Vector3 anchorCenter, float anchorRadius, IReadOnlyList<int> handles, IReadOnlyList<EnemyTarget> enemies)
        {
            if (enemies == null || enemies.Count == 0)
            {
                SeekFallback(anchorRadius, handles);
                return;
            }

            var bodyguards = new List<BodyguardPosition>();
            foreach (var pedHandle in handles)
            {
                bodyguards.Add(new BodyguardPosition(pedHandle, _gameBridge.GetPedPosition(pedHandle)));
            }

            var enemyPositions = new Dictionary<int, Vector3>();
            foreach (var enemy in enemies) enemyPositions[enemy.Handle] = enemy.Position;

            var assignment = _assignmentResolver.Assign(bodyguards, enemies, BuildPreviousAssignment());
            foreach (var pedHandle in handles)
            {
                if (DisembarkedThisTick(pedHandle)) continue;
                if (!assignment.TryGetValue(pedHandle, out var targetHandle)) continue;
                if (!enemyPositions.TryGetValue(targetHandle, out var targetPos)) continue;

                ApplyEngagement(pedHandle, targetHandle, targetPos);
            }
        }

        private void ApplyEngagement(int pedHandle, int targetHandle, Vector3 targetPos)
        {
            float dist = _gameBridge.GetPedPosition(pedHandle).DistanceTo(targetPos);
            bool los = _gameBridge.HasClearLineOfSight(pedHandle, targetHandle);
            var role = _rolesByHandle.TryGetValue(pedHandle, out var r) ? r : DefenderRole.Grunt;
            var phase = _enginePhase.TryGetValue(pedHandle, out var p) ? p : EngagePhase.Advance;
            int misses = _losMisses.TryGetValue(pedHandle, out var m) ? m : 0;

            var decision = _engagementResolver.Resolve(dist, los, role, phase, misses);
            _enginePhase[pedHandle] = decision.Phase;
            _losMisses[pedHandle] = decision.ConsecutiveLosMisses;

            if (decision.Phase == EngagePhase.Engage)
            {
                if (AlreadyApplied(pedHandle, SquadStance.SearchAndDestroy, BodyguardOrderKind.AttackTarget, targetHandle)) return;
                _reconciler.Submit(pedHandle, PedIntent.CombatTarget(targetHandle));
                Remember(pedHandle, SquadStance.SearchAndDestroy, BodyguardOrderKind.AttackTarget, targetHandle);
                FileLogger.AI($"SquadStance S&D: ped {pedHandle} engage {targetHandle} dist={dist:F0} los={los}");
            }
            else
            {
                if (AlreadyApplied(pedHandle, SquadStance.SearchAndDestroy, BodyguardOrderKind.AdvanceOnTarget, targetHandle)) return;
                _reconciler.Submit(pedHandle, PedIntent.AdvanceOnTarget(targetHandle, decision.EngageRange));
                Remember(pedHandle, SquadStance.SearchAndDestroy, BodyguardOrderKind.AdvanceOnTarget, targetHandle);
                FileLogger.AI($"SquadStance S&D: ped {pedHandle} advance {targetHandle} dist={dist:F0} stop={decision.EngageRange:F0}");
            }
        }
```

Add the `using FactionWars.Core.Models;` import to `SquadStanceController.Stances.cs` if not present.

> The `AlreadyApplied`/`Remember` `discriminator` is the `targetHandle`. Because `AttackTarget` and `AdvanceOnTarget` are distinct `BodyguardOrderKind` values, a phase flip on the same target is treated as a change and re-issued; a steady phase on the same target is deduped. This requires a new `BodyguardOrderKind` value — see Step 5.

- [ ] **Step 5: Add the `AdvanceOnTarget` order kind**

In `src/FactionWars/Combat/Models/BodyguardOrderKind.cs`, add `AdvanceOnTarget` to the enum (existing members are `FollowPlayer, HoldAtPoint, SeekInRadius, AttackTarget`):

```csharp
    public enum BodyguardOrderKind
    {
        FollowPlayer,
        HoldAtPoint,
        SeekInRadius,
        AttackTarget,
        AdvanceOnTarget
    }
```

- [ ] **Step 6: Update existing SquadStance tests for the new ctor + Update signature**

Run `git grep -ln "new SquadStanceController(" tests` and `git grep -ln "\.Update(" tests/FactionWars.Tests/Unit/ScriptHookV/Managers` to find existing SquadStance tests. In each:
- add `new SquadEngagementResolver(new EngageRangeProvider())` as the 5th ctor argument;
- add a final `Update` argument — an empty `new Dictionary<int, DefenderRole>()` for non-S&D cases (Escort/HoldArea don't read roles).
Add `using FactionWars.Combat.Services;` and `using FactionWars.Core.Models;` to those test files as needed.

- [ ] **Step 7: CRLF-convert the new test and run the SquadStance tests**

```bash
sed -i 's/\r$//; s/$/\r/' tests/FactionWars.Tests/Unit/ScriptHookV/Managers/SquadStanceControllerSearchDestroyTests.cs
dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~SquadStance"
```
Expected: PASS (new S&D tests + all updated existing SquadStance tests).

- [ ] **Step 8: Commit**

```bash
git add -A && git commit -m "feat: advance-then-engage Search & Destroy in SquadStanceController"
```

---

## Task 8: Wire the whole-zone pool, roles, and resolver into the game loop

**Files:**
- Modify: `src/FactionWars/ScriptHookV/GameLoopController.Initialization.cs:54` (construct controller with the engagement resolver)
- Modify: `src/FactionWars/ScriptHookV/GameLoopController.SystemUpdates.cs` (`UpdateSquadStance`: all-zone enemies, pass roles)
- Test: none new (integration glue; covered by build + existing suite). Manual in-game verification follows.

**Interfaces:**
- Consumes: `SquadStanceController(... , ISquadEngagementResolver)`, `IEnemyTargetCollector.CollectAll`, `FollowerManager.OnFootBodyguardRoles`.

- [ ] **Step 1: Construct the controller with the engagement resolver** (`GameLoopController.Initialization.cs`, the `_squadStanceController = new SquadStanceController(...)` line)

```csharp
            _squadStanceController = new SquadStanceController(_gameBridge, new SquadStanceResolver(), new TargetAssignmentResolver(), new PedIntentReconciler(_gameBridge), new SquadEngagementResolver(new EngageRangeProvider()));
```

Add imports to that file if missing: `using FactionWars.Combat.Services;`.

- [ ] **Step 2: Switch `UpdateSquadStance` to the whole-zone pool + roles** (`GameLoopController.SystemUpdates.cs`)

Replace the enemy-collection + `Update` call in `UpdateSquadStance` with:

```csharp
            IReadOnlyList<EnemyTarget> enemies = System.Array.Empty<EnemyTarget>();
            int hostileCount = 0;
            if (_squadStanceController.CurrentStance == SquadStance.SearchAndDestroy && handles.Count > 0)
            {
                var hostileHandles = GatherHostileHandles();
                hostileCount = hostileHandles.Count;
                enemies = _enemyTargetCollector!.CollectAll(hostileHandles);
            }

            var rolesByHandle = _followerManager?.OnFootBodyguardRoles
                ?? (IReadOnlyDictionary<int, DefenderRole>)new Dictionary<int, DefenderRole>();
```

And update the `_squadStanceController.Update(...)` call to pass both new arguments:

```csharp
            _squadStanceController.Update(anchor.Center, anchor.Radius, handles, enemies, rolesByHandle);
```

Add imports to that file if missing: `using System.Collections.Generic;` (already present), `using FactionWars.Core.Models;` (for `DefenderRole`).

> Note: the old code passed `anchor.Center, anchor.Radius` to `Collect` for radius filtering. With `CollectAll` the anchor is no longer used for enemy filtering, but `anchor.Center`/`anchor.Radius` are still passed to `Update` (used by `SeekFallback` and HoldArea). Keep the `ResolveSquadAnchor()` call.

- [ ] **Step 3: Build the whole solution and run the full unit suite**

```bash
dotnet build FactionWars.sln --no-incremental
dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --no-build --filter "FullyQualifiedName~FactionWars.Tests.Unit"
```
Expected: Build succeeds (0 warnings, 0 errors). All unit tests pass.

- [ ] **Step 4: Commit**

```bash
git add -A && git commit -m "feat: wire whole-zone target pool + roles + engagement resolver into game loop"
```

---

## Task 9: Verify, push, PR

- [ ] **Step 1: Final full build + test**

```bash
dotnet build FactionWars.sln --no-incremental
dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --no-build --filter "FullyQualifiedName~FactionWars.Tests.Unit"
```
Expected: 0 warnings, 0 errors; all unit tests green.

- [ ] **Step 2: Push and open the PR**

```bash
git push -u origin feat/83-aggressive-search-and-destroy
gh pr create --title "Aggressive Search & Destroy: advance-and-engage squad algorithm (#83)" --body "<summary of the spec + behavior change; closes #83>"
```

- [ ] **Step 3: Manual in-game verification (post-merge deploy)**

After merge the post-merge hook deploys the DLL. In-game: recruit bodyguards, enter an enemy zone, cycle to Search & Destroy. Confirm via `behavior_trace.csv` / logs:
- followers advance toward assigned enemies (`SquadStance S&D: ped X advance`) and switch to engage near role range (`... engage`);
- the Advance/Engage phase does not flip every second (hysteresis);
- enemies are drawn from the whole zone, not just nearby.

---

## Notes for the implementer

- **Out of scope:** the Escort "2-of-6 follow" desync is a separate fix; do not touch `ApplyEscort` here.
- **No behavior change** to HoldArea or Escort — only the S&D branch, the new `AdvanceOnTarget` intent/order kinds, and the `Update`/ctor signatures change.
- If adding a public method to `SquadStanceController` or `FollowerManager` trips CI0004 (≤10 public methods), prefer extending an existing partial / using a property rather than a new public method; `OnFootBodyguardRoles` is a property (getter), which does not count against the method cap.
