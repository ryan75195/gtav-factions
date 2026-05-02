# Zone Defender Leash Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Keep friendly and enemy zone defenders inside their assigned zone via a bounded native wander task plus a 2-second periodic leash sweep.

**Architecture:** A new bounded-wander native (`TASK_WANDER_IN_AREA`) replaces the unbounded one in friendly defender spawn. A new pure-logic helper (`ZoneLeashEnforcer`) holds the threshold/return-point math. Each defender manager (friendly + enemy) gains a 2s timer that, when fired, scans its tracked peds and pulls strays back via `ClearPedTasks` + a new `TaskGoToCoord` native. Battle attackers are intentionally unaffected.

**Tech Stack:** C# .NET Framework 4.8, ScriptHookVDotNet3, xUnit, Moq.

---

## File Structure

| File | Responsibility |
|---|---|
| `src/FactionWars/Core/Interfaces/IGameBridge.cs` | Add `TaskPedWanderInBoundedArea` and `TaskGoToCoord` interface methods |
| `src/FactionWars/ScriptHookV/GameBridge.cs` | Implement both new natives against SHVDN3 |
| `src/FactionWars/Core/Utils/MockGameBridge.cs` | Record both new calls so tests can assert on them |
| `src/FactionWars/ScriptHookV/Services/ZoneLeashEnforcer.cs` | NEW — pure-logic `ShouldLeash` + `PickReturnPoint` + constants |
| `src/FactionWars/ScriptHookV/Managers/FriendlyDefenderManager.cs` | Switch spawn-time wander to bounded variant; add leash sweep |
| `src/FactionWars/ScriptHookV/Managers/EnemyDefenderManager.cs` | Add leash sweep (no wander change — enemies use combat tasks) |
| `tests/FactionWars.Tests/Unit/Core/Utils/MockGameBridgeBoundedWanderTests.cs` | NEW — verifies mock records bounded-wander calls |
| `tests/FactionWars.Tests/Unit/Core/Utils/MockGameBridgeGoToCoordTests.cs` | NEW — verifies mock records go-to-coord calls |
| `tests/FactionWars.Tests/Unit/ScriptHookV/Services/ZoneLeashEnforcerTests.cs` | NEW — pure-logic tests for the helper |
| `tests/FactionWars.Tests/Unit/ScriptHookV/FriendlyDefenderManagerLeashTests.cs` | NEW — leash sweep wired correctly into Friendly Update |
| `tests/FactionWars.Tests/Unit/ScriptHookV/EnemyDefenderManagerLeashTests.cs` | NEW — leash sweep wired correctly into Enemy Update |
| `tests/FactionWars.Tests/Unit/ScriptHookV/FriendlyDefenderManagerTests.cs` | Add a test asserting spawn uses the bounded-wander native |

`BattleAttackerManager.cs` is intentionally **not** modified.

---

## Conventions

- **Domain `Vector3`:** `FactionWars.Core.Models.Vector3`. Throughout this plan the bare token `Vector3` refers to that type. In `GameBridge.cs` it's aliased as `DomainVector3` so it doesn't collide with `GTA.Math.Vector3`; mirror that aliasing if you touch GameBridge.
- **Time:** `_gameBridge.GetGameTime()` returns `int` milliseconds. Pattern for periodic work: `if (currentMs - _lastFooMs >= IntervalMs) { ...; _lastFooMs = currentMs; }`. Initialize `_lastFooMs = 0` so the first tick fires immediately — that's fine for our case (defenders may already be out of bounds when Update first runs).
- **Test mocks:** Project uses concrete `MockGameBridge` (not Moq) for game-bridge interactions, and Moq for everything else. Follow that pattern.
- **Build / test commands** (Windows bash, run from repo root):
  - Build: `dotnet build src/FactionWars/FactionWars.csproj -c Debug -v minimal`
  - Run a single test: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj -c Debug --nologo --filter "FullyQualifiedName~<TestClassName>"`
  - Full suite: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj -c Debug --nologo -v minimal`

---

## Task 1: Add `TaskPedWanderInBoundedArea` native (interface + mock + real)

**Files:**
- Modify: `src/FactionWars/Core/Interfaces/IGameBridge.cs` (around line 398, after existing `TaskPedWanderInArea` method)
- Modify: `src/FactionWars/Core/Utils/MockGameBridge.cs` (around line 568, after existing `TaskPedWanderInAreaSprinting`)
- Modify: `src/FactionWars/ScriptHookV/GameBridge.cs` (after existing `TaskPedWanderInArea` implementation around line 1343)
- Test: `tests/FactionWars.Tests/Unit/Core/Utils/MockGameBridgeBoundedWanderTests.cs` (NEW)

- [ ] **Step 1.1: Write the failing test**

Create `tests/FactionWars.Tests/Unit/Core/Utils/MockGameBridgeBoundedWanderTests.cs`:

```csharp
using FactionWars.Core.Models;
using FactionWars.Core.Utils;
using Xunit;

namespace FactionWars.Tests.Unit.Core.Utils
{
    public class MockGameBridgeBoundedWanderTests
    {
        [Fact]
        public void TaskPedWanderInBoundedArea_RecordsCallDistinctFromUnbounded()
        {
            var bridge = new MockGameBridge();
            int handle = bridge.SpawnPed("test_model", new Vector3(0f, 0f, 0f));

            bridge.TaskPedWanderInBoundedArea(handle, new Vector3(10f, 20f, 0f), 50f);

            Assert.True(bridge.IsPedBoundedWandering(handle));
            Assert.False(bridge.IsPedWandering(handle), "Bounded wander should NOT be reported as plain wander");
            Assert.Equal(new Vector3(10f, 20f, 0f), bridge.GetBoundedWanderCenter(handle));
            Assert.Equal(50f, bridge.GetBoundedWanderRadius(handle));
        }

        [Fact]
        public void TaskPedWanderInBoundedArea_OverwritesPreviousTasks()
        {
            var bridge = new MockGameBridge();
            int handle = bridge.SpawnPed("test_model", new Vector3(0f, 0f, 0f));

            bridge.TaskPedWanderInArea(handle, new Vector3(0f, 0f, 0f), 100f);
            bridge.TaskPedWanderInBoundedArea(handle, new Vector3(10f, 20f, 0f), 50f);

            Assert.False(bridge.IsPedWandering(handle));
            Assert.True(bridge.IsPedBoundedWandering(handle));
        }
    }
}
```

- [ ] **Step 1.2: Run test to verify it fails**

```bash
dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj -c Debug --nologo --filter "FullyQualifiedName~MockGameBridgeBoundedWanderTests"
```

Expected: build error or test fail referencing `TaskPedWanderInBoundedArea`/`IsPedBoundedWandering`/`GetBoundedWanderCenter`/`GetBoundedWanderRadius` not defined.

- [ ] **Step 1.3: Add interface method**

In `src/FactionWars/Core/Interfaces/IGameBridge.cs`, add after the existing `TaskPedWanderInArea` declaration (around line 398):

```csharp
        /// <summary>
        /// Tasks a ped to wander within a bounded circular area. Unlike
        /// <see cref="TaskPedWanderInArea"/> (which calls TASK_WANDER_STANDARD
        /// and ignores the radius), this wraps TASK_WANDER_IN_AREA which
        /// natively keeps the ped inside (center, radius). Used for zone
        /// defenders so idle wandering respects the zone boundary.
        /// </summary>
        /// <param name="pedHandle">The ped's entity handle.</param>
        /// <param name="center">Center point of the wander area.</param>
        /// <param name="radius">Radius of the wander area in meters.</param>
        void TaskPedWanderInBoundedArea(int pedHandle, Vector3 center, float radius);
```

- [ ] **Step 1.4: Implement on MockGameBridge**

In `src/FactionWars/Core/Utils/MockGameBridge.cs`, add after the existing `TaskPedWanderInAreaSprinting` method (around line 568):

```csharp
        private readonly Dictionary<int, WanderState> _boundedWanderingPeds = new Dictionary<int, WanderState>();

        public void TaskPedWanderInBoundedArea(int pedHandle, Vector3 center, float radius)
        {
            if (_peds.ContainsKey(pedHandle))
            {
                // Clear other task states (mirror plain wander's behavior)
                _pedsFacingPosition.Remove(pedHandle);
                _combatTargetingPeds.Remove(pedHandle);
                _goToEntityPeds.Remove(pedHandle);
                _followEntityPeds.Remove(pedHandle);
                _wanderingPeds.Remove(pedHandle);
                _boundedWanderingPeds[pedHandle] = new WanderState
                {
                    Center = center,
                    Radius = radius,
                    IsSprinting = false
                };
            }
        }

        /// <summary>
        /// Gets whether a ped is currently bounded-wandering (TASK_WANDER_IN_AREA).
        /// Distinct from <see cref="IsPedWandering"/> which reports
        /// TASK_WANDER_STANDARD.
        /// </summary>
        public bool IsPedBoundedWandering(int pedHandle) => _boundedWanderingPeds.ContainsKey(pedHandle);

        public Vector3? GetBoundedWanderCenter(int pedHandle)
        {
            return _boundedWanderingPeds.TryGetValue(pedHandle, out var state) ? state.Center : (Vector3?)null;
        }

        public float? GetBoundedWanderRadius(int pedHandle)
        {
            return _boundedWanderingPeds.TryGetValue(pedHandle, out var state) ? state.Radius : (float?)null;
        }
```

Also update the existing `TaskPedWanderInArea` implementation to remove from the new bounded set (so the symmetric "switching back" works). Find the existing `TaskPedWanderInArea` method (around line 534) and add `_boundedWanderingPeds.Remove(pedHandle);` to its task-state-clearing block.

- [ ] **Step 1.5: Implement on real `GameBridge`**

In `src/FactionWars/ScriptHookV/GameBridge.cs`, add after the existing `TaskPedWanderInArea` implementation (around line 1343):

```csharp
        /// <inheritdoc />
        public void TaskPedWanderInBoundedArea(int pedHandle, DomainVector3 center, float radius)
        {
            FileLogger.AI($"TaskPedWanderInBoundedArea: CALLED for ped {pedHandle} at center ({center.X:F1}, {center.Y:F1}, {center.Z:F1}) radius {radius:F1}");

            try
            {
                var ped = Entity.FromHandle(pedHandle) as Ped;
                if (ped == null || !ped.Exists())
                {
                    FileLogger.Warn($"TaskPedWanderInBoundedArea: Ped {pedHandle} is null or doesn't exist, aborting");
                    return;
                }

                ped.Task.ClearAllImmediately();

                // TASK_WANDER_IN_AREA params: ped, x, y, z, radius, minLength, timeBetweenWalks
                // minLength=10.0, timeBetweenWalks=10.0 are reasonable defaults that produce
                // varied paths within the area without rapid re-pathing.
                Function.Call(
                    Hash.TASK_WANDER_IN_AREA,
                    ped.Handle,
                    center.X, center.Y, center.Z,
                    radius,
                    10.0f, 10.0f);
                FileLogger.AI($"TaskPedWanderInBoundedArea: TASK_WANDER_IN_AREA called for ped {pedHandle}");

                // Walk pace (1.0 = walk, matches existing TaskPedWanderInArea)
                Function.Call(Hash.SET_PED_DESIRED_MOVE_BLEND_RATIO, ped.Handle, 1.0f);

                FileLogger.AI($"TaskPedWanderInBoundedArea: COMPLETED successfully for ped {pedHandle}");
            }
            catch (Exception ex)
            {
                FileLogger.Error($"TaskPedWanderInBoundedArea exception for ped {pedHandle}", ex);
            }
        }
```

If `Hash.TASK_WANDER_IN_AREA` is not in your SHVDN3 enum (the enum is sometimes incomplete), replace `Hash.TASK_WANDER_IN_AREA` with the explicit cast `(Hash)0xE054346CA3A0F315uL` — the raw native id.

- [ ] **Step 1.6: Run tests to verify they pass**

```bash
dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj -c Debug --nologo --filter "FullyQualifiedName~MockGameBridgeBoundedWanderTests"
```

Expected: 2 passed.

Also run the full mock test suite to make sure the `_boundedWanderingPeds.Remove` addition didn't break anything:

```bash
dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj -c Debug --nologo --filter "FullyQualifiedName~MockGameBridge"
```

Expected: all green.

- [ ] **Step 1.7: Commit**

```bash
git add src/FactionWars/Core/Interfaces/IGameBridge.cs src/FactionWars/Core/Utils/MockGameBridge.cs src/FactionWars/ScriptHookV/GameBridge.cs tests/FactionWars.Tests/Unit/Core/Utils/MockGameBridgeBoundedWanderTests.cs
git commit -m "feat: add TaskPedWanderInBoundedArea native"
```

---

## Task 2: Add `TaskGoToCoord` native (interface + mock + real)

**Files:**
- Modify: `src/FactionWars/Core/Interfaces/IGameBridge.cs` (after existing `TaskGoToEntity`, around line 425)
- Modify: `src/FactionWars/Core/Utils/MockGameBridge.cs` (with the other task-recording dictionaries)
- Modify: `src/FactionWars/ScriptHookV/GameBridge.cs` (near existing TaskGoToEntity implementation)
- Test: `tests/FactionWars.Tests/Unit/Core/Utils/MockGameBridgeGoToCoordTests.cs` (NEW)

- [ ] **Step 2.1: Write the failing test**

Create `tests/FactionWars.Tests/Unit/Core/Utils/MockGameBridgeGoToCoordTests.cs`:

```csharp
using FactionWars.Core.Models;
using FactionWars.Core.Utils;
using Xunit;

namespace FactionWars.Tests.Unit.Core.Utils
{
    public class MockGameBridgeGoToCoordTests
    {
        [Fact]
        public void TaskGoToCoord_RecordsDestination()
        {
            var bridge = new MockGameBridge();
            int handle = bridge.SpawnPed("test_model", new Vector3(0f, 0f, 0f));

            bridge.TaskGoToCoord(handle, new Vector3(50f, 75f, 0f));

            Assert.True(bridge.IsPedGoingToCoord(handle));
            Assert.Equal(new Vector3(50f, 75f, 0f), bridge.GetPedGoToCoordDestination(handle));
        }

        [Fact]
        public void TaskGoToCoord_OverwritesPreviousDestination()
        {
            var bridge = new MockGameBridge();
            int handle = bridge.SpawnPed("test_model", new Vector3(0f, 0f, 0f));

            bridge.TaskGoToCoord(handle, new Vector3(50f, 75f, 0f));
            bridge.TaskGoToCoord(handle, new Vector3(100f, 0f, 0f));

            Assert.Equal(new Vector3(100f, 0f, 0f), bridge.GetPedGoToCoordDestination(handle));
        }

        [Fact]
        public void ClearPedTasks_ClearsGoToCoordRecording()
        {
            var bridge = new MockGameBridge();
            int handle = bridge.SpawnPed("test_model", new Vector3(0f, 0f, 0f));

            bridge.TaskGoToCoord(handle, new Vector3(50f, 75f, 0f));
            bridge.ClearPedTasks(handle);

            Assert.False(bridge.IsPedGoingToCoord(handle));
        }
    }
}
```

- [ ] **Step 2.2: Run test to verify it fails**

```bash
dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj -c Debug --nologo --filter "FullyQualifiedName~MockGameBridgeGoToCoordTests"
```

Expected: build error referencing `TaskGoToCoord`/`IsPedGoingToCoord`/`GetPedGoToCoordDestination` not defined.

- [ ] **Step 2.3: Add interface method**

In `src/FactionWars/Core/Interfaces/IGameBridge.cs`, add after the existing `TaskGoToEntity` declaration (around line 425):

```csharp
        /// <summary>
        /// Tasks a ped to walk to a fixed world coordinate. Wraps
        /// TASK_GO_TO_COORD_ANY_MEANS so the ped will navigate around
        /// obstacles. Used by the zone leash to pull strays back inside
        /// their zone.
        /// </summary>
        /// <param name="pedHandle">The ped's entity handle.</param>
        /// <param name="destination">World position to walk to.</param>
        void TaskGoToCoord(int pedHandle, Vector3 destination);
```

- [ ] **Step 2.4: Implement on MockGameBridge**

In `src/FactionWars/Core/Utils/MockGameBridge.cs`, add a new private dictionary near the other task-recording dictionaries (e.g. just above `_clearedPeds` around line 795) and a method block after the existing `ClearPedTasks` (around line 798):

```csharp
        private readonly Dictionary<int, Vector3> _goToCoordPeds = new Dictionary<int, Vector3>();

        public void TaskGoToCoord(int pedHandle, Vector3 destination)
        {
            if (_peds.ContainsKey(pedHandle))
            {
                // Clear other task states (mirror plain wander's behavior)
                _wanderingPeds.Remove(pedHandle);
                _boundedWanderingPeds.Remove(pedHandle);
                _pedsFacingPosition.Remove(pedHandle);
                _combatTargetingPeds.Remove(pedHandle);
                _goToEntityPeds.Remove(pedHandle);
                _followEntityPeds.Remove(pedHandle);
                _goToCoordPeds[pedHandle] = destination;
            }
        }

        public bool IsPedGoingToCoord(int pedHandle) => _goToCoordPeds.ContainsKey(pedHandle);

        public Vector3? GetPedGoToCoordDestination(int pedHandle)
        {
            return _goToCoordPeds.TryGetValue(pedHandle, out var dest) ? dest : (Vector3?)null;
        }
```

Then update the existing `ClearPedTasks` implementation (around line 798) to also clear `_goToCoordPeds`:

```csharp
        public void ClearPedTasks(int pedHandle)
        {
            if (_peds.ContainsKey(pedHandle))
            {
                _wanderingPeds.Remove(pedHandle);
                _boundedWanderingPeds.Remove(pedHandle);
                _pedsFacingPosition.Remove(pedHandle);
                _combatTargetingPeds.Remove(pedHandle);
                _goToEntityPeds.Remove(pedHandle);
                _followEntityPeds.Remove(pedHandle);
                _goToCoordPeds.Remove(pedHandle);
                _clearedPeds.Add(pedHandle);
            }
        }
```

(Read the existing `ClearPedTasks` body first; add the bounded + goToCoord lines without removing any existing clearing logic.)

- [ ] **Step 2.5: Implement on real `GameBridge`**

In `src/FactionWars/ScriptHookV/GameBridge.cs`, add this method (place it next to the existing `TaskGoToEntity` implementation):

```csharp
        /// <inheritdoc />
        public void TaskGoToCoord(int pedHandle, DomainVector3 destination)
        {
            FileLogger.AI($"TaskGoToCoord: CALLED for ped {pedHandle} to ({destination.X:F1}, {destination.Y:F1}, {destination.Z:F1})");

            try
            {
                var ped = Entity.FromHandle(pedHandle) as Ped;
                if (ped == null || !ped.Exists())
                {
                    FileLogger.Warn($"TaskGoToCoord: Ped {pedHandle} is null or doesn't exist, aborting");
                    return;
                }

                // TASK_GO_TO_COORD_ANY_MEANS params:
                //   ped, x, y, z, moveSpeed, vehicle, useLongRangePath, drivingFlags, finalHeading
                // moveSpeed = 2.0 (run), vehicle = 0, useLongRangePath = false,
                // drivingFlags = 786603 (default driving), finalHeading = 0
                Function.Call(
                    Hash.TASK_GO_TO_COORD_ANY_MEANS,
                    ped.Handle,
                    destination.X, destination.Y, destination.Z,
                    2.0f, 0, false, 786603, 0.0f);
                FileLogger.AI($"TaskGoToCoord: TASK_GO_TO_COORD_ANY_MEANS issued for ped {pedHandle}");
            }
            catch (Exception ex)
            {
                FileLogger.Error($"TaskGoToCoord exception for ped {pedHandle}", ex);
            }
        }
```

- [ ] **Step 2.6: Run tests to verify they pass**

```bash
dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj -c Debug --nologo --filter "FullyQualifiedName~MockGameBridgeGoToCoordTests"
```

Expected: 3 passed.

- [ ] **Step 2.7: Commit**

```bash
git add src/FactionWars/Core/Interfaces/IGameBridge.cs src/FactionWars/Core/Utils/MockGameBridge.cs src/FactionWars/ScriptHookV/GameBridge.cs tests/FactionWars.Tests/Unit/Core/Utils/MockGameBridgeGoToCoordTests.cs
git commit -m "feat: add TaskGoToCoord native"
```

---

## Task 3: `ZoneLeashEnforcer.ShouldLeash`

**Files:**
- Create: `src/FactionWars/ScriptHookV/Services/ZoneLeashEnforcer.cs`
- Test: `tests/FactionWars.Tests/Unit/ScriptHookV/Services/ZoneLeashEnforcerTests.cs` (NEW)

- [ ] **Step 3.1: Write the failing test**

Create `tests/FactionWars.Tests/Unit/ScriptHookV/Services/ZoneLeashEnforcerTests.cs`:

```csharp
using FactionWars.Core.Models;
using FactionWars.ScriptHookV.Services;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV.Services
{
    public class ZoneLeashEnforcerTests
    {
        private static readonly Vector3 ZoneCenter = new Vector3(100f, 200f, 0f);
        private const float ZoneRadius = 150f;

        [Fact]
        public void ShouldLeash_AtZoneCenter_ReturnsFalse()
        {
            Assert.False(ZoneLeashEnforcer.ShouldLeash(ZoneCenter, ZoneCenter, ZoneRadius));
        }

        [Fact]
        public void ShouldLeash_AtBoundary_ReturnsFalse()
        {
            // ped on the boundary (distance == radius) — within the 1.2x hysteresis band
            var pedPos = ZoneCenter + new Vector3(ZoneRadius, 0f, 0f);

            Assert.False(ZoneLeashEnforcer.ShouldLeash(pedPos, ZoneCenter, ZoneRadius));
        }

        [Fact]
        public void ShouldLeash_JustInsideHysteresisBand_ReturnsFalse()
        {
            // distance = radius * 1.19 — still inside the threshold
            var pedPos = ZoneCenter + new Vector3(ZoneRadius * 1.19f, 0f, 0f);

            Assert.False(ZoneLeashEnforcer.ShouldLeash(pedPos, ZoneCenter, ZoneRadius));
        }

        [Fact]
        public void ShouldLeash_JustPastHysteresisBand_ReturnsTrue()
        {
            // distance = radius * 1.21 — past the threshold, leash fires
            var pedPos = ZoneCenter + new Vector3(ZoneRadius * 1.21f, 0f, 0f);

            Assert.True(ZoneLeashEnforcer.ShouldLeash(pedPos, ZoneCenter, ZoneRadius));
        }

        [Fact]
        public void ShouldLeash_FarOutside_ReturnsTrue()
        {
            var pedPos = ZoneCenter + new Vector3(ZoneRadius * 5f, 0f, 0f);

            Assert.True(ZoneLeashEnforcer.ShouldLeash(pedPos, ZoneCenter, ZoneRadius));
        }
    }
}
```

- [ ] **Step 3.2: Run test to verify it fails**

```bash
dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj -c Debug --nologo --filter "FullyQualifiedName~ZoneLeashEnforcerTests"
```

Expected: build error referencing `ZoneLeashEnforcer` not defined.

- [ ] **Step 3.3: Create the helper with `ShouldLeash`**

Create `src/FactionWars/ScriptHookV/Services/ZoneLeashEnforcer.cs`:

```csharp
using System;
using FactionWars.Core.Models;

namespace FactionWars.ScriptHookV.Services
{
    /// <summary>
    /// Pure-logic helper for the zone defender leash. Decides whether a ped
    /// has strayed too far from its zone and picks a sensible point to send
    /// it back to. Stateless — callers pass current positions in.
    /// </summary>
    public static class ZoneLeashEnforcer
    {
        /// <summary>
        /// Defenders past zoneRadius * this multiplier are leashed back.
        /// Above 1.0 to give hysteresis: peds legitimately walking near the
        /// boundary don't yo-yo back and forth.
        /// </summary>
        public const float LeashThresholdMultiplier = 1.2f;

        /// <summary>
        /// Leashed defenders are sent to a random point inside
        /// zoneRadius * this multiplier (the inner half of the zone) so
        /// multiple yanked peds don't pile up on the exact center.
        /// </summary>
        public const float LeashReturnRadiusMultiplier = 0.5f;

        /// <summary>
        /// How often each manager runs the leash sweep, in milliseconds.
        /// </summary>
        public const int LeashCheckIntervalMs = 2000;

        /// <summary>
        /// Returns true if the ped is far enough outside the zone that it
        /// should be retasked back inside.
        /// </summary>
        public static bool ShouldLeash(Vector3 pedPos, Vector3 zoneCenter, float zoneRadius)
        {
            float threshold = zoneRadius * LeashThresholdMultiplier;
            float dx = pedPos.X - zoneCenter.X;
            float dy = pedPos.Y - zoneCenter.Y;
            float dz = pedPos.Z - zoneCenter.Z;
            float distSq = dx * dx + dy * dy + dz * dz;
            return distSq > threshold * threshold;
        }
    }
}
```

- [ ] **Step 3.4: Run tests to verify they pass**

```bash
dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj -c Debug --nologo --filter "FullyQualifiedName~ZoneLeashEnforcerTests"
```

Expected: 5 passed.

- [ ] **Step 3.5: Commit**

```bash
git add src/FactionWars/ScriptHookV/Services/ZoneLeashEnforcer.cs tests/FactionWars.Tests/Unit/ScriptHookV/Services/ZoneLeashEnforcerTests.cs
git commit -m "feat: add ZoneLeashEnforcer.ShouldLeash"
```

---

## Task 4: `ZoneLeashEnforcer.PickReturnPoint`

**Files:**
- Modify: `src/FactionWars/ScriptHookV/Services/ZoneLeashEnforcer.cs`
- Modify: `tests/FactionWars.Tests/Unit/ScriptHookV/Services/ZoneLeashEnforcerTests.cs`

- [ ] **Step 4.1: Write the failing test**

Append to `tests/FactionWars.Tests/Unit/ScriptHookV/Services/ZoneLeashEnforcerTests.cs` (inside the existing class):

```csharp
        [Fact]
        public void PickReturnPoint_AlwaysWithinReturnRadius()
        {
            var rng = new Random(seed: 12345);

            for (int i = 0; i < 200; i++)
            {
                var point = ZoneLeashEnforcer.PickReturnPoint(ZoneCenter, ZoneRadius, rng);
                float dx = point.X - ZoneCenter.X;
                float dy = point.Y - ZoneCenter.Y;
                float dist = (float)Math.Sqrt(dx * dx + dy * dy);

                // Allow a small float tolerance.
                float maxDist = ZoneRadius * ZoneLeashEnforcer.LeashReturnRadiusMultiplier;
                Assert.True(dist <= maxDist + 0.01f,
                    $"iter {i}: dist={dist:F2} exceeded {maxDist:F2}");
            }
        }

        [Fact]
        public void PickReturnPoint_PreservesCenterZ()
        {
            // Z is altitude in GTA — the return point should stay at zone elevation,
            // not get randomized along Z (which would put peds underground or in the air).
            var rng = new Random(seed: 99);
            var center = new Vector3(0f, 0f, 50f);

            var point = ZoneLeashEnforcer.PickReturnPoint(center, 100f, rng);

            Assert.Equal(50f, point.Z);
        }

        [Fact]
        public void PickReturnPoint_DeterministicGivenSameRng()
        {
            var rng1 = new Random(seed: 42);
            var rng2 = new Random(seed: 42);

            var p1 = ZoneLeashEnforcer.PickReturnPoint(ZoneCenter, ZoneRadius, rng1);
            var p2 = ZoneLeashEnforcer.PickReturnPoint(ZoneCenter, ZoneRadius, rng2);

            Assert.Equal(p1, p2);
        }
```

(Make sure the test file `using` block already has `using System;` for `Random` and `Math` — if not, add it.)

- [ ] **Step 4.2: Run tests to verify they fail**

```bash
dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj -c Debug --nologo --filter "FullyQualifiedName~ZoneLeashEnforcerTests.PickReturnPoint"
```

Expected: build error referencing `ZoneLeashEnforcer.PickReturnPoint` not defined.

- [ ] **Step 4.3: Implement `PickReturnPoint`**

Append to `ZoneLeashEnforcer` in `src/FactionWars/ScriptHookV/Services/ZoneLeashEnforcer.cs`:

```csharp
        /// <summary>
        /// Picks a random point inside zoneRadius * LeashReturnRadiusMultiplier
        /// of the zone center. Z is preserved from the center so peds don't
        /// end up underground or in the air. Uses sqrt-of-uniform for the
        /// radial distance so points are uniformly distributed in the disk
        /// (not biased toward the center).
        /// </summary>
        public static Vector3 PickReturnPoint(Vector3 zoneCenter, float zoneRadius, Random rng)
        {
            if (rng == null)
                throw new ArgumentNullException(nameof(rng));

            float maxRadius = zoneRadius * LeashReturnRadiusMultiplier;
            float angle = (float)(rng.NextDouble() * 2.0 * Math.PI);
            float dist = maxRadius * (float)Math.Sqrt(rng.NextDouble());

            return new Vector3(
                zoneCenter.X + dist * (float)Math.Cos(angle),
                zoneCenter.Y + dist * (float)Math.Sin(angle),
                zoneCenter.Z);
        }
```

- [ ] **Step 4.4: Run tests to verify they pass**

```bash
dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj -c Debug --nologo --filter "FullyQualifiedName~ZoneLeashEnforcerTests"
```

Expected: 8 passed (5 from Task 3 + 3 new).

- [ ] **Step 4.5: Commit**

```bash
git add src/FactionWars/ScriptHookV/Services/ZoneLeashEnforcer.cs tests/FactionWars.Tests/Unit/ScriptHookV/Services/ZoneLeashEnforcerTests.cs
git commit -m "feat: add ZoneLeashEnforcer.PickReturnPoint"
```

---

## Task 5: Wire leash sweep into `FriendlyDefenderManager`

**Files:**
- Modify: `src/FactionWars/ScriptHookV/Managers/FriendlyDefenderManager.cs`
- Test: `tests/FactionWars.Tests/Unit/ScriptHookV/FriendlyDefenderManagerLeashTests.cs` (NEW)

Inspect existing test setup before writing. Read `tests/FactionWars.Tests/Unit/ScriptHookV/Managers/FriendlyDefenderManagerDeathTests.cs` to copy the constructor wiring (it uses `MockGameBridge` + Moq for the other services). The leash test mirrors that setup.

- [ ] **Step 5.1: Write the failing tests**

Create `tests/FactionWars.Tests/Unit/ScriptHookV/FriendlyDefenderManagerLeashTests.cs`. The constructor wiring follows `tests/FactionWars.Tests/Unit/ScriptHookV/Managers/FriendlyDefenderManagerDeathTests.cs:34-64`. The full file:

```csharp
using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Models;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.Core.Utils;
using FactionWars.ScriptHookV.Managers;
using FactionWars.ScriptHookV.Services;
using FactionWars.Territory.Interfaces;
using FactionWars.Territory.Models;
using FactionWars.UI.Interfaces;
using Moq;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV
{
    public class FriendlyDefenderManagerLeashTests
    {
        private const string PlayerFactionId = "michael";
        private const string TestZoneId = "zone_1";
        private static readonly Vector3 ZoneCenter = new Vector3(100f, 100f, 0f);
        private const float ZoneRadius = 150f;

        [Fact]
        public void Update_DefenderInsideZone_DoesNotRetask()
        {
            var (manager, bridge, defenderHandle) = SpawnSingleDefender();

            // Well inside the zone.
            bridge.SetPedPosition(defenderHandle, new Vector3(ZoneCenter.X + 10f, ZoneCenter.Y + 10f, 0f));

            bridge.AdvanceGameTime(ZoneLeashEnforcer.LeashCheckIntervalMs + 100);
            manager.Update();

            Assert.False(bridge.IsPedGoingToCoord(defenderHandle));
        }

        [Fact]
        public void Update_DefenderPastHysteresisThreshold_RetasksTowardZone()
        {
            var (manager, bridge, defenderHandle) = SpawnSingleDefender();

            // 1.5x radius from center — clearly outside the 1.2x hysteresis band.
            bridge.SetPedPosition(defenderHandle, new Vector3(ZoneCenter.X + ZoneRadius * 1.5f, ZoneCenter.Y, 0f));

            bridge.AdvanceGameTime(ZoneLeashEnforcer.LeashCheckIntervalMs + 100);
            manager.Update();

            Assert.True(bridge.IsPedGoingToCoord(defenderHandle));

            var dest = bridge.GetPedGoToCoordDestination(defenderHandle);
            Assert.NotNull(dest);
            float dx = dest!.Value.X - ZoneCenter.X;
            float dy = dest.Value.Y - ZoneCenter.Y;
            float dist = (float)System.Math.Sqrt(dx * dx + dy * dy);
            Assert.True(dist <= ZoneRadius * ZoneLeashEnforcer.LeashReturnRadiusMultiplier + 0.01f);
        }

        [Fact]
        public void Update_StrayDefender_NoRetaskBeforeIntervalElapsed()
        {
            var (manager, bridge, defenderHandle) = SpawnSingleDefender();

            bridge.SetPedPosition(defenderHandle, new Vector3(ZoneCenter.X + ZoneRadius * 1.5f, ZoneCenter.Y, 0f));

            // Less than the interval — sweep should NOT fire.
            bridge.AdvanceGameTime(ZoneLeashEnforcer.LeashCheckIntervalMs - 100);
            manager.Update();

            Assert.False(bridge.IsPedGoingToCoord(defenderHandle));

            // Cross the interval — sweep fires.
            bridge.AdvanceGameTime(200);
            manager.Update();

            Assert.True(bridge.IsPedGoingToCoord(defenderHandle));
        }

        private static (FriendlyDefenderManager manager, MockGameBridge bridge, int defenderHandle) SpawnSingleDefender()
        {
            var bridge = new MockGameBridge();
            var allocationServiceMock = new Mock<IZoneDefenderAllocationService>();
            var pedSpawningServiceMock = new Mock<IPedSpawningService>();
            var pedDespawnServiceMock = new Mock<IPedDespawnService>();
            var defenderTierServiceMock = new Mock<IDefenderTierService>();
            var pedBlipServiceMock = new Mock<IPedBlipService>();
            var zoneServiceMock = new Mock<IZoneService>();

            pedSpawningServiceMock.Setup(p => p.CanSpawn()).Returns(true);
            pedSpawningServiceMock
                .Setup(p => p.SpawnPed(It.IsAny<string>(), It.IsAny<Vector3>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(() => new PedHandle(bridge.CreatePed("test", new Vector3(0f, 0f, 0f))));

            defenderTierServiceMock
                .Setup(d => d.GetTierConfig(It.IsAny<DefenderTier>()))
                .Returns(new DefenderTierConfig(DefenderTier.Basic, 200, 100, 0, "weapon_pistol", 0.5f, 1.0f));

            pedBlipServiceMock
                .Setup(p => p.CreateBlipForPed(It.IsAny<int>(), It.IsAny<BlipColor>()))
                .Returns(1);

            var zone = new Zone(TestZoneId, "Test Zone", ZoneCenter, ZoneRadius, 1)
            {
                OwnerFactionId = PlayerFactionId
            };
            // The leash sweep looks up zones via IZoneService — wire the mock so it returns the test zone.
            zoneServiceMock.Setup(z => z.GetZone(TestZoneId)).Returns(zone);

            var allocation = new ZoneDefenderAllocation(PlayerFactionId, TestZoneId);
            allocation.AddTroops(DefenderTier.Basic, 1);
            allocationServiceMock.Setup(a => a.GetAllocation(PlayerFactionId, TestZoneId)).Returns(allocation);

            var manager = new FriendlyDefenderManager(
                bridge,
                allocationServiceMock.Object,
                pedSpawningServiceMock.Object,
                pedDespawnServiceMock.Object,
                defenderTierServiceMock.Object,
                pedBlipServiceMock.Object,
                zoneServiceMock.Object,
                PlayerFactionId);

            manager.OnZoneEntered(zone);
            int handle = bridge.GetSpawnedPeds()[0];
            return (manager, bridge, handle);
        }
    }
}
```

Notes:
- `MockGameBridge.AdvanceGameTime(int milliseconds)` already exists (around line 944) — no test-only addition needed.
- `MockGameBridge.GetSpawnedPeds()` returns the list of spawned ped handles in spawn order. Used here to grab the just-spawned defender.
- `Zone` ctor signature is `(id, name, center, radius=150f, strategicValue=1)`; `OwnerFactionId` is a settable property (not a ctor arg).
- The leash sweep must skip dead/streamed-out peds; the mock spawns peds in alive state by default, so no extra setup is needed.

- [ ] **Step 5.2: Run tests to verify they fail**

```bash
dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj -c Debug --nologo --filter "FullyQualifiedName~FriendlyDefenderManagerLeashTests"
```

Expected: tests fail because `Update()` does not currently retask out-of-bounds defenders. The "inside zone" test will pass trivially (we're asserting nothing happens) but the other two should fail.

- [ ] **Step 5.3: Add leash sweep to `FriendlyDefenderManager`**

In `src/FactionWars/ScriptHookV/Managers/FriendlyDefenderManager.cs`:

1. Add a field near the other private fields (around line 65):

```csharp
        private int _lastLeashCheckMs = 0;
        private readonly Random _leashRandom = new Random();
```

2. Add a `using` for the enforcer namespace at the top:

```csharp
using FactionWars.ScriptHookV.Services;
```

3. At the end of the existing `Update()` method body (after `CleanupExpiredCorpses(currentGameTime);` around line 419), add:

```csharp
            EnforceZoneLeash(currentGameTime);
```

4. Add the new private method (place it near `CleanupExpiredCorpses`):

```csharp
        /// <summary>
        /// Every <see cref="ZoneLeashEnforcer.LeashCheckIntervalMs"/>, scan all
        /// tracked defenders. Any whose distance from their zone center exceeds
        /// the hysteresis threshold gets its tasks cleared and a TaskGoToCoord
        /// back to a random point inside the inner half of the zone.
        /// </summary>
        private void EnforceZoneLeash(int currentGameTime)
        {
            if (currentGameTime - _lastLeashCheckMs < ZoneLeashEnforcer.LeashCheckIntervalMs)
                return;
            _lastLeashCheckMs = currentGameTime;

            foreach (var kvp in _spawnedPedTierByZone)
            {
                var zoneId = kvp.Key;
                var pedTiers = kvp.Value;

                var zone = _zoneService.GetZone(zoneId);
                if (zone == null)
                    continue;

                foreach (var pedHandle in pedTiers.Keys)
                {
                    if (_corpseDeathTimes.ContainsKey(pedHandle))
                        continue;
                    if (!_gameBridge.DoesPedExist(pedHandle) || !_gameBridge.IsPedAlive(pedHandle))
                        continue;

                    var pedPos = _gameBridge.GetPedPosition(pedHandle);
                    if (!ZoneLeashEnforcer.ShouldLeash(pedPos, zone.Center, zone.Radius))
                        continue;

                    var returnPoint = ZoneLeashEnforcer.PickReturnPoint(zone.Center, zone.Radius, _leashRandom);
                    _gameBridge.ClearPedTasks(pedHandle);
                    _gameBridge.TaskGoToCoord(pedHandle, returnPoint);
                    FileLogger.AI($"FriendlyDefenderManager: leashed ped {pedHandle} in zone {zoneId} from ({pedPos.X:F1},{pedPos.Y:F1}) back to ({returnPoint.X:F1},{returnPoint.Y:F1})");
                }
            }
        }
```

- [ ] **Step 5.4: Run tests to verify they pass**

```bash
dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj -c Debug --nologo --filter "FullyQualifiedName~FriendlyDefenderManagerLeashTests"
```

Expected: 3 passed.

Then run the full friendly-defender suite to make sure existing tests still pass:

```bash
dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj -c Debug --nologo --filter "FullyQualifiedName~FriendlyDefenderManager"
```

Expected: all green.

- [ ] **Step 5.5: Commit**

```bash
git add src/FactionWars/ScriptHookV/Managers/FriendlyDefenderManager.cs tests/FactionWars.Tests/Unit/ScriptHookV/FriendlyDefenderManagerLeashTests.cs
git commit -m "feat: leash strayed friendly defenders back to zone every 2s"
```

---

## Task 6: Wire leash sweep into `EnemyDefenderManager`

Mirror of Task 5 for the enemy side. The signature difference: `EnemyDefenderManager.Update(string? enemyFactionId)`.

**Files:**
- Modify: `src/FactionWars/ScriptHookV/Managers/EnemyDefenderManager.cs`
- Test: `tests/FactionWars.Tests/Unit/ScriptHookV/EnemyDefenderManagerLeashTests.cs` (NEW)

- [ ] **Step 6.1: Write the failing tests**

Create `tests/FactionWars.Tests/Unit/ScriptHookV/EnemyDefenderManagerLeashTests.cs`:

```csharp
using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Models;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.Core.Utils;
using FactionWars.ScriptHookV.Managers;
using FactionWars.ScriptHookV.Services;
using FactionWars.Territory.Interfaces;
using FactionWars.Territory.Models;
using FactionWars.UI.Interfaces;
using Moq;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV
{
    public class EnemyDefenderManagerLeashTests
    {
        private const string EnemyFactionId = "ballas";
        private const string TestZoneId = "zone_1";
        private static readonly Vector3 ZoneCenter = new Vector3(100f, 100f, 0f);
        private const float ZoneRadius = 150f;

        [Fact]
        public void Update_DefenderInsideZone_DoesNotRetask()
        {
            var (manager, bridge, defenderHandle) = SpawnSingleEnemyDefender();

            bridge.SetPedPosition(defenderHandle, new Vector3(ZoneCenter.X + 10f, ZoneCenter.Y + 10f, 0f));

            bridge.AdvanceGameTime(ZoneLeashEnforcer.LeashCheckIntervalMs + 100);
            manager.Update(EnemyFactionId);

            Assert.False(bridge.IsPedGoingToCoord(defenderHandle));
        }

        [Fact]
        public void Update_DefenderPastHysteresisThreshold_RetasksTowardZone()
        {
            var (manager, bridge, defenderHandle) = SpawnSingleEnemyDefender();

            bridge.SetPedPosition(defenderHandle, new Vector3(ZoneCenter.X + ZoneRadius * 1.5f, ZoneCenter.Y, 0f));

            bridge.AdvanceGameTime(ZoneLeashEnforcer.LeashCheckIntervalMs + 100);
            manager.Update(EnemyFactionId);

            Assert.True(bridge.IsPedGoingToCoord(defenderHandle));

            var dest = bridge.GetPedGoToCoordDestination(defenderHandle);
            Assert.NotNull(dest);
            float dx = dest!.Value.X - ZoneCenter.X;
            float dy = dest.Value.Y - ZoneCenter.Y;
            float dist = (float)System.Math.Sqrt(dx * dx + dy * dy);
            Assert.True(dist <= ZoneRadius * ZoneLeashEnforcer.LeashReturnRadiusMultiplier + 0.01f);
        }

        [Fact]
        public void Update_StrayDefender_NoRetaskBeforeIntervalElapsed()
        {
            var (manager, bridge, defenderHandle) = SpawnSingleEnemyDefender();

            bridge.SetPedPosition(defenderHandle, new Vector3(ZoneCenter.X + ZoneRadius * 1.5f, ZoneCenter.Y, 0f));

            bridge.AdvanceGameTime(ZoneLeashEnforcer.LeashCheckIntervalMs - 100);
            manager.Update(EnemyFactionId);
            Assert.False(bridge.IsPedGoingToCoord(defenderHandle));

            bridge.AdvanceGameTime(200);
            manager.Update(EnemyFactionId);
            Assert.True(bridge.IsPedGoingToCoord(defenderHandle));
        }

        private static (EnemyDefenderManager manager, MockGameBridge bridge, int defenderHandle) SpawnSingleEnemyDefender()
        {
            var bridge = new MockGameBridge();
            var allocationServiceMock = new Mock<IZoneDefenderAllocationService>();
            var pedSpawningServiceMock = new Mock<IPedSpawningService>();
            var pedDespawnServiceMock = new Mock<IPedDespawnService>();
            var defenderTierServiceMock = new Mock<IDefenderTierService>();
            var pedBlipServiceMock = new Mock<IPedBlipService>();
            var zoneServiceMock = new Mock<IZoneService>();

            pedSpawningServiceMock.Setup(p => p.CanSpawn()).Returns(true);
            pedSpawningServiceMock
                .Setup(p => p.SpawnPed(It.IsAny<string>(), It.IsAny<Vector3>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(() => new PedHandle(bridge.CreatePed("test", new Vector3(0f, 0f, 0f))));

            defenderTierServiceMock
                .Setup(d => d.GetTierConfig(It.IsAny<DefenderTier>()))
                .Returns(new DefenderTierConfig(DefenderTier.Basic, 200, 100, 0, "weapon_pistol", 0.5f, 1.0f));

            pedBlipServiceMock
                .Setup(p => p.CreateBlipForPed(It.IsAny<int>(), It.IsAny<BlipColor>()))
                .Returns(1);

            var zone = new Zone(TestZoneId, "Test Zone", ZoneCenter, ZoneRadius, 1)
            {
                OwnerFactionId = EnemyFactionId
            };
            zoneServiceMock.Setup(z => z.GetZone(TestZoneId)).Returns(zone);

            var allocation = new ZoneDefenderAllocation(EnemyFactionId, TestZoneId);
            allocation.AddTroops(DefenderTier.Basic, 1);
            allocationServiceMock.Setup(a => a.GetAllocation(EnemyFactionId, TestZoneId)).Returns(allocation);

            var manager = new EnemyDefenderManager(
                bridge,
                allocationServiceMock.Object,
                pedSpawningServiceMock.Object,
                pedDespawnServiceMock.Object,
                defenderTierServiceMock.Object,
                pedBlipServiceMock.Object,
                zoneServiceMock.Object);

            manager.OnEnemyZoneEntered(zone, EnemyFactionId);
            int handle = bridge.GetSpawnedPeds()[0];
            return (manager, bridge, handle);
        }
    }
}
```

Notes:
- `EnemyDefenderManager` ctor takes 7 args (no `playerFactionId` — enemies aren't tied to the player).
- Spawn entry point is `OnEnemyZoneEntered(zone, factionId)` (vs friendly's `OnZoneEntered(zone)`).

- [ ] **Step 6.2: Run tests to verify they fail**

```bash
dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj -c Debug --nologo --filter "FullyQualifiedName~EnemyDefenderManagerLeashTests"
```

Expected: 2 fail (the inside-zone test passes trivially).

- [ ] **Step 6.3: Add leash sweep to `EnemyDefenderManager`**

In `src/FactionWars/ScriptHookV/Managers/EnemyDefenderManager.cs`:

1. Add fields near the other private fields:

```csharp
        private int _lastLeashCheckMs = 0;
        private readonly Random _leashRandom = new Random();
```

2. Add `using FactionWars.ScriptHookV.Services;` at the top.

3. At the end of `Update(string? enemyFactionId)` body, add:

```csharp
            EnforceZoneLeash(_gameBridge.GetGameTime());
```

(If `Update` already calls `_gameBridge.GetGameTime()` once and stores it in a local, reuse that local instead of calling twice.)

4. Add the private method (mirror of Task 5's, identical body):

```csharp
        private void EnforceZoneLeash(int currentGameTime)
        {
            if (currentGameTime - _lastLeashCheckMs < ZoneLeashEnforcer.LeashCheckIntervalMs)
                return;
            _lastLeashCheckMs = currentGameTime;

            foreach (var kvp in _spawnedPedTierByZone)
            {
                var zoneId = kvp.Key;
                var pedTiers = kvp.Value;

                var zone = _zoneService.GetZone(zoneId);
                if (zone == null)
                    continue;

                foreach (var pedHandle in pedTiers.Keys)
                {
                    if (_corpseDeathTimes.ContainsKey(pedHandle))
                        continue;
                    if (!_gameBridge.DoesPedExist(pedHandle) || !_gameBridge.IsPedAlive(pedHandle))
                        continue;

                    var pedPos = _gameBridge.GetPedPosition(pedHandle);
                    if (!ZoneLeashEnforcer.ShouldLeash(pedPos, zone.Center, zone.Radius))
                        continue;

                    var returnPoint = ZoneLeashEnforcer.PickReturnPoint(zone.Center, zone.Radius, _leashRandom);
                    _gameBridge.ClearPedTasks(pedHandle);
                    _gameBridge.TaskGoToCoord(pedHandle, returnPoint);
                    FileLogger.AI($"EnemyDefenderManager: leashed ped {pedHandle} in zone {zoneId} from ({pedPos.X:F1},{pedPos.Y:F1}) back to ({returnPoint.X:F1},{returnPoint.Y:F1})");
                }
            }
        }
```

The body is intentionally identical to `FriendlyDefenderManager.EnforceZoneLeash`. Resist the temptation to extract a shared helper here — the dictionaries (`_spawnedPedTierByZone`, `_corpseDeathTimes`) are private fields on each manager, and a shared static helper would need the manager to expose them. Keep duplication local until a third manager needs it.

- [ ] **Step 6.4: Run tests to verify they pass**

```bash
dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj -c Debug --nologo --filter "FullyQualifiedName~EnemyDefenderManagerLeashTests"
```

Expected: 3 passed.

Then run the full enemy-defender suite to make sure existing tests still pass:

```bash
dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj -c Debug --nologo --filter "FullyQualifiedName~EnemyDefenderManager"
```

Expected: all green.

- [ ] **Step 6.5: Commit**

```bash
git add src/FactionWars/ScriptHookV/Managers/EnemyDefenderManager.cs tests/FactionWars.Tests/Unit/ScriptHookV/EnemyDefenderManagerLeashTests.cs
git commit -m "feat: leash strayed enemy defenders back to zone every 2s"
```

---

## Task 7: Switch `FriendlyDefenderManager` spawn to bounded wander

**Files:**
- Modify: `src/FactionWars/ScriptHookV/Managers/FriendlyDefenderManager.cs:200`
- Modify: `tests/FactionWars.Tests/Unit/ScriptHookV/FriendlyDefenderManagerLeashTests.cs` (add one test — re-uses Task 5's `SpawnSingleDefender` helper in the same file)

- [ ] **Step 7.1: Write the failing test**

Append this `[Fact]` to the `FriendlyDefenderManagerLeashTests` class (the file created in Task 5):

```csharp
        [Fact]
        public void OnZoneEntered_TasksDefendersWithBoundedWander()
        {
            var (_, bridge, defenderHandle) = SpawnSingleDefender();

            // Defenders should idle-wander only inside their zone, not unbounded.
            Assert.True(bridge.IsPedBoundedWandering(defenderHandle),
                "Friendly defenders must use TaskPedWanderInBoundedArea so GTA's native enforces the zone radius for idle wander.");
            Assert.False(bridge.IsPedWandering(defenderHandle),
                "Friendly defenders must NOT use the unbounded TaskPedWanderInArea anymore.");
        }
```

- [ ] **Step 7.2: Run test to verify it fails**

```bash
dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj -c Debug --nologo --filter "FullyQualifiedName~FriendlyDefenderManagerTests.SpawnDefendersForZone_TasksDefendersWithBoundedWander"
```

Expected: FAIL — `IsPedBoundedWandering` returns false because spawn currently calls `TaskPedWanderInArea` (the unbounded variant).

- [ ] **Step 7.3: Switch the spawn-time wander call**

In `src/FactionWars/ScriptHookV/Managers/FriendlyDefenderManager.cs`, line 200, change:

```csharp
                    // Wander using zone radius for full coverage
                    _gameBridge.TaskPedWanderInArea(pedHandle.Handle, zone.Center, zone.Radius);
```

to:

```csharp
                    // Bounded native wander — keeps idle peds inside the zone
                    // without per-tick checks. The leash sweep handles the
                    // combat-chase case separately.
                    _gameBridge.TaskPedWanderInBoundedArea(pedHandle.Handle, zone.Center, zone.Radius);
```

- [ ] **Step 7.4: Run tests to verify they pass**

```bash
dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj -c Debug --nologo --filter "FullyQualifiedName~FriendlyDefenderManager"
```

Expected: all green, including the new `SpawnDefendersForZone_TasksDefendersWithBoundedWander` test.

- [ ] **Step 7.5: Commit**

```bash
git add src/FactionWars/ScriptHookV/Managers/FriendlyDefenderManager.cs tests/FactionWars.Tests/Unit/ScriptHookV/FriendlyDefenderManagerTests.cs
git commit -m "feat: friendly defenders idle-wander bounded by zone radius"
```

---

## Task 8: Final verification & deploy

- [ ] **Step 8.1: Run the full test suite**

```bash
dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj -c Debug --nologo -v minimal
```

Expected: all green except possibly the known-flaky `NativeSaveWatcherTests.MultipleRapidWritesSamePath_DebouncesToOneEvent` (timing-sensitive file watcher test, unrelated to this work). If it fails, re-run it in isolation to confirm it's flake, not regression:

```bash
dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj -c Debug --nologo --filter "FullyQualifiedName~NativeSaveWatcherTests.MultipleRapidWritesSamePath"
```

Expected: PASS in isolation.

- [ ] **Step 8.2: Build a clean DLL**

```bash
dotnet build src/FactionWars/FactionWars.csproj -c Debug -v minimal
```

Expected: 0 errors.

- [ ] **Step 8.3: Deploy to the GTA V scripts folder**

```bash
cp "C:/Users/ryan7/programming/gtav-factions/src/FactionWars/bin/Debug/net48/FactionWars.dll" "E:/SteamLibrary/steamapps/common/Grand Theft Auto V/scripts/"
```

Expected: file copied.

- [ ] **Step 8.4: Manual in-game verification (out of scope for the agent — for the human)**

After deploying, in-game checks:
1. Enter an owned territory with allocated defenders. Watch the HUD count and observe defenders patrolling within the zone boundary (not drifting toward neighboring blocks).
2. Move outside the zone, watch the radar. Defenders should remain inside the zone outline.
3. Trigger combat that pulls a defender toward the boundary (spawn an enemy near the edge). Within 2 seconds of crossing `radius * 1.2`, the defender should disengage and head back toward zone center. Check the latest `C:\Users\ryan7\Documents\FactionWars\Logs\FactionWars_*.log` for `leashed ped` entries.

---

## Spec Coverage Map

| Spec Section | Implemented By |
|---|---|
| Part 1 — Bounded idle wander (friendly only) | Tasks 1, 7 |
| Part 2 — Periodic leash check | Tasks 2, 3, 4, 5, 6 |
| Constants on `ZoneLeashEnforcer` | Tasks 3, 4 |
| Friendly + Enemy scope | Tasks 5, 6 |
| Battle attackers unchanged | (intentionally not in any task) |
| Risks: Hash fallback | Task 1 (Step 1.5 includes raw-hash fallback) |
| Risks: yank-during-combat acceptable in v1 | (no test asserts otherwise) |
