# Squad Stance Modes Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Let the player cycle their bodyguard party through three combat stances (Escort → Hold Area → Search & Destroy) with D-pad Up.

**Architecture:** A pure `SquadStanceResolver` and `TargetAssignmentResolver` (portable `Combat`) decide what each bodyguard should do; a thin `SquadStanceController` (`ScriptHookV.Managers`) owns the current stance and issues `IGameBridge` tasks. On-foot follow tasking moves out of `FollowerManager` into the controller's Escort branch so a single system owns on-foot tasking. `GameLoopController` wires input (D-pad Up), per-tick updates, and composition.

**Tech Stack:** C# .NET Framework 4.8, ScriptHookVDotNet3, xUnit, Moq, custom Roslyn analyzers.

## Global Constraints

- All source files MUST use CRLF line endings (analyzer `ENDOFLINE`).
- Max 10 public methods per class, **excluding interface-implementing methods** (analyzer `CI0004`). Properties are not counted.
- Max 40 effective lines per method (analyzer `CI0007`); max 250 lines per class (analyzer `CI0017`).
- Max 5 constructor parameters (analyzer `CI0005`).
- Constructors take interfaces, not concrete production types (analyzer `CI0014`).
- Exactly one public top-level type per file (analyzer: multiple-public-types).
- No tuple **return** types (analyzer). Tuple locals/fields are fine, but prefer named structs.
- Every new interface MUST live in a namespace whose segment is `Interfaces` (architecture test `Interfaces_ShouldLiveInInterfacesNamespace`).
- `Vector3` is `FactionWars.Core.Interfaces.Vector3` (readonly struct: `X`, `Y`, `Z` floats; `Vector3.Zero`; `float DistanceTo(Vector3)`).
- All new `GameBridge` native methods MUST include `FileLogger.AI` debug logging (project rule).
- `GTA`/NativeUI references stay in the `ScriptHookV` namespace only; `Core`/`Combat` stay portable.
- Pre-commit hook runs `dotnet build FactionWars.sln --no-incremental` (must be 0 warnings/0 errors) + `dotnet test ... --filter "FullyQualifiedName~FactionWars.Tests.Unit"`. Work on branch `feat/35-squad-stance-modes` (commits blocked on master).

---

## File Structure

**Portable domain (`Combat`):**
- `src/FactionWars/Combat/Models/SquadStance.cs` — stance enum.
- `src/FactionWars/Combat/Models/SquadStanceExtensions.cs` — `Next()` cycle helper.
- `src/FactionWars/Combat/Models/BodyguardOrderKind.cs` — order-kind enum.
- `src/FactionWars/Combat/Models/BodyguardOrder.cs` — one bodyguard's intent (no native deps).
- `src/FactionWars/Combat/Models/BodyguardPosition.cs` — (handle, position) input to assignment.
- `src/FactionWars/Combat/Models/EnemyTarget.cs` — (handle, position) enemy.
- `src/FactionWars/Combat/Models/AreaAnchor.cs` — (center, radius).
- `src/FactionWars/Combat/Interfaces/ISquadStanceResolver.cs` + `Services/SquadStanceResolver.cs` — geometry stances.
- `src/FactionWars/Combat/Interfaces/ITargetAssignmentResolver.cs` + `Services/TargetAssignmentResolver.cs` — bodyguard→enemy assignment.
- `src/FactionWars/Combat/Interfaces/IAreaAnchorResolver.cs` + `Services/AreaAnchorResolver.cs` — anchor resolution.

**Native integration (`ScriptHookV` / `Core`):**
- `src/FactionWars/Core/Interfaces/IGameBridge.cs` — add `TaskGuardArea`, `TaskCombatPed`.
- `src/FactionWars/ScriptHookV/GameBridge.SquadTasks.cs` — real native impls.
- `src/FactionWars/Core/Utils/MockGameBridge.cs` — mock impls + recording.
- `src/FactionWars/ScriptHookV/Managers/Interfaces/IHostilePedHandleSource.cs` — handle query.
- `src/FactionWars/ScriptHookV/Managers/EnemyDefenderManager.cs` + `BattleAttackerManager.cs` — implement it.
- `src/FactionWars/ScriptHookV/Combat/Interfaces/IEnemyTargetCollector.cs` + `Combat/EnemyTargetCollector.cs` — handles→targets within radius.
- `src/FactionWars/ScriptHookV/Managers/SquadStanceController.cs` + `SquadStanceController.Stances.cs` — owns stance, issues tasks.
- `src/FactionWars/ScriptHookV/Managers/FollowerManager.cs` + `FollowerManager.OnFoot.cs` — expose on-foot handles, stop tasking.
- `src/FactionWars/ScriptHookV/GameLoopController*.cs` — input, tick wiring, composition.

---

### Task 1: SquadStance enum + Next() cycle

**Files:**
- Create: `src/FactionWars/Combat/Models/SquadStance.cs`
- Create: `src/FactionWars/Combat/Models/SquadStanceExtensions.cs`
- Test: `tests/FactionWars.Tests/Unit/Combat/SquadStanceNextTests.cs`

**Interfaces:**
- Produces: `enum SquadStance { Escort, HoldArea, SearchAndDestroy }`; `static SquadStance Next(this SquadStance stance)` in `SquadStanceExtensions`.

- [ ] **Step 1: Write the failing test**

```csharp
using FactionWars.Combat.Models;
using Xunit;

namespace FactionWars.Tests.Unit.Combat
{
    public class SquadStanceNextTests
    {
        [Fact]
        public void Next_CyclesEscortToHoldAreaToSearchAndDestroyToEscort()
        {
            Assert.Equal(SquadStance.HoldArea, SquadStance.Escort.Next());
            Assert.Equal(SquadStance.SearchAndDestroy, SquadStance.HoldArea.Next());
            Assert.Equal(SquadStance.Escort, SquadStance.SearchAndDestroy.Next());
        }
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~SquadStanceNextTests"`
Expected: FAIL — `SquadStance` / `Next` do not exist (compile error).

- [ ] **Step 3: Write the enum**

`src/FactionWars/Combat/Models/SquadStance.cs`:
```csharp
namespace FactionWars.Combat.Models
{
    /// <summary>
    /// The party-wide combat stance for the player's bodyguards.
    /// </summary>
    public enum SquadStance
    {
        Escort,
        HoldArea,
        SearchAndDestroy
    }
}
```

- [ ] **Step 4: Write the Next() helper**

`src/FactionWars/Combat/Models/SquadStanceExtensions.cs`:
```csharp
namespace FactionWars.Combat.Models
{
    /// <summary>
    /// Cycle helper for <see cref="SquadStance"/>.
    /// </summary>
    public static class SquadStanceExtensions
    {
        public static SquadStance Next(this SquadStance stance)
        {
            switch (stance)
            {
                case SquadStance.Escort: return SquadStance.HoldArea;
                case SquadStance.HoldArea: return SquadStance.SearchAndDestroy;
                default: return SquadStance.Escort;
            }
        }
    }
}
```

- [ ] **Step 5: Run test to verify it passes**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~SquadStanceNextTests"`
Expected: PASS

- [ ] **Step 6: Commit**

```bash
git add src/FactionWars/Combat/Models/SquadStance.cs src/FactionWars/Combat/Models/SquadStanceExtensions.cs tests/FactionWars.Tests/Unit/Combat/SquadStanceNextTests.cs
git commit -m "feat: add SquadStance enum with cycle helper (#35)"
```

---

### Task 2: BodyguardOrder value object

**Files:**
- Create: `src/FactionWars/Combat/Models/BodyguardOrderKind.cs`
- Create: `src/FactionWars/Combat/Models/BodyguardOrder.cs`
- Test: `tests/FactionWars.Tests/Unit/Combat/BodyguardOrderTests.cs`

**Interfaces:**
- Produces: `enum BodyguardOrderKind { FollowPlayer, HoldAtPoint, SeekInRadius, AttackTarget }`; `struct BodyguardOrder` with `BodyguardOrderKind Kind`, `Vector3 Point`, `float Radius`, `int TargetHandle`, and static factories `FollowPlayer()`, `HoldAtPoint(Vector3)`, `SeekInRadius(Vector3 center, float radius)`, `AttackTarget(int handle)`.

- [ ] **Step 1: Write the failing test**

```csharp
using FactionWars.Combat.Models;
using FactionWars.Core.Interfaces;
using Xunit;

namespace FactionWars.Tests.Unit.Combat
{
    public class BodyguardOrderTests
    {
        [Fact]
        public void FollowPlayer_HasFollowKind()
        {
            var order = BodyguardOrder.FollowPlayer();
            Assert.Equal(BodyguardOrderKind.FollowPlayer, order.Kind);
        }

        [Fact]
        public void HoldAtPoint_CarriesPoint()
        {
            var p = new Vector3(1f, 2f, 3f);
            var order = BodyguardOrder.HoldAtPoint(p);
            Assert.Equal(BodyguardOrderKind.HoldAtPoint, order.Kind);
            Assert.Equal(p, order.Point);
        }

        [Fact]
        public void SeekInRadius_CarriesCentreAndRadius()
        {
            var c = new Vector3(5f, 6f, 7f);
            var order = BodyguardOrder.SeekInRadius(c, 40f);
            Assert.Equal(BodyguardOrderKind.SeekInRadius, order.Kind);
            Assert.Equal(c, order.Point);
            Assert.Equal(40f, order.Radius);
        }

        [Fact]
        public void AttackTarget_CarriesTargetHandle()
        {
            var order = BodyguardOrder.AttackTarget(99);
            Assert.Equal(BodyguardOrderKind.AttackTarget, order.Kind);
            Assert.Equal(99, order.TargetHandle);
        }
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~BodyguardOrderTests"`
Expected: FAIL — types not defined (compile error).

- [ ] **Step 3: Write the enum**

`src/FactionWars/Combat/Models/BodyguardOrderKind.cs`:
```csharp
namespace FactionWars.Combat.Models
{
    /// <summary>
    /// What a single bodyguard should do this tick, independent of any native call.
    /// </summary>
    public enum BodyguardOrderKind
    {
        FollowPlayer,
        HoldAtPoint,
        SeekInRadius,
        AttackTarget
    }
}
```

- [ ] **Step 4: Write the value object**

`src/FactionWars/Combat/Models/BodyguardOrder.cs`:
```csharp
using FactionWars.Core.Interfaces;

namespace FactionWars.Combat.Models
{
    /// <summary>
    /// One bodyguard's intent for the current tick. Pure data — no native dependency.
    /// </summary>
    public readonly struct BodyguardOrder
    {
        public BodyguardOrderKind Kind { get; }
        public Vector3 Point { get; }
        public float Radius { get; }
        public int TargetHandle { get; }

        private BodyguardOrder(BodyguardOrderKind kind, Vector3 point, float radius, int targetHandle)
        {
            Kind = kind;
            Point = point;
            Radius = radius;
            TargetHandle = targetHandle;
        }

        public static BodyguardOrder FollowPlayer()
            => new BodyguardOrder(BodyguardOrderKind.FollowPlayer, Vector3.Zero, 0f, 0);

        public static BodyguardOrder HoldAtPoint(Vector3 point)
            => new BodyguardOrder(BodyguardOrderKind.HoldAtPoint, point, 0f, 0);

        public static BodyguardOrder SeekInRadius(Vector3 center, float radius)
            => new BodyguardOrder(BodyguardOrderKind.SeekInRadius, center, radius, 0);

        public static BodyguardOrder AttackTarget(int targetHandle)
            => new BodyguardOrder(BodyguardOrderKind.AttackTarget, Vector3.Zero, 0f, targetHandle);
    }
}
```

- [ ] **Step 5: Run test to verify it passes**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~BodyguardOrderTests"`
Expected: PASS

- [ ] **Step 6: Commit**

```bash
git add src/FactionWars/Combat/Models/BodyguardOrderKind.cs src/FactionWars/Combat/Models/BodyguardOrder.cs tests/FactionWars.Tests/Unit/Combat/BodyguardOrderTests.cs
git commit -m "feat: add BodyguardOrder value object (#35)"
```

---

### Task 3: Position/target/anchor value objects

**Files:**
- Create: `src/FactionWars/Combat/Models/BodyguardPosition.cs`
- Create: `src/FactionWars/Combat/Models/EnemyTarget.cs`
- Create: `src/FactionWars/Combat/Models/AreaAnchor.cs`
- Test: `tests/FactionWars.Tests/Unit/Combat/SquadValueObjectsTests.cs`

**Interfaces:**
- Produces: `struct BodyguardPosition { int Handle; Vector3 Position; }` ctor `(int, Vector3)`; `struct EnemyTarget { int Handle; Vector3 Position; }` ctor `(int, Vector3)`; `struct AreaAnchor { Vector3 Center; float Radius; }` ctor `(Vector3, float)`.

- [ ] **Step 1: Write the failing test**

```csharp
using FactionWars.Combat.Models;
using FactionWars.Core.Interfaces;
using Xunit;

namespace FactionWars.Tests.Unit.Combat
{
    public class SquadValueObjectsTests
    {
        [Fact]
        public void BodyguardPosition_CarriesHandleAndPosition()
        {
            var bp = new BodyguardPosition(7, new Vector3(1f, 2f, 3f));
            Assert.Equal(7, bp.Handle);
            Assert.Equal(new Vector3(1f, 2f, 3f), bp.Position);
        }

        [Fact]
        public void EnemyTarget_CarriesHandleAndPosition()
        {
            var et = new EnemyTarget(8, new Vector3(4f, 5f, 6f));
            Assert.Equal(8, et.Handle);
            Assert.Equal(new Vector3(4f, 5f, 6f), et.Position);
        }

        [Fact]
        public void AreaAnchor_CarriesCentreAndRadius()
        {
            var a = new AreaAnchor(new Vector3(9f, 9f, 9f), 25f);
            Assert.Equal(new Vector3(9f, 9f, 9f), a.Center);
            Assert.Equal(25f, a.Radius);
        }
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~SquadValueObjectsTests"`
Expected: FAIL — types not defined.

- [ ] **Step 3: Write the three structs**

`src/FactionWars/Combat/Models/BodyguardPosition.cs`:
```csharp
using FactionWars.Core.Interfaces;

namespace FactionWars.Combat.Models
{
    /// <summary>A bodyguard's live world position, input to target assignment.</summary>
    public readonly struct BodyguardPosition
    {
        public int Handle { get; }
        public Vector3 Position { get; }

        public BodyguardPosition(int handle, Vector3 position)
        {
            Handle = handle;
            Position = position;
        }
    }
}
```

`src/FactionWars/Combat/Models/EnemyTarget.cs`:
```csharp
using FactionWars.Core.Interfaces;

namespace FactionWars.Combat.Models
{
    /// <summary>A known hostile ped's live world position, input to target assignment.</summary>
    public readonly struct EnemyTarget
    {
        public int Handle { get; }
        public Vector3 Position { get; }

        public EnemyTarget(int handle, Vector3 position)
        {
            Handle = handle;
            Position = position;
        }
    }
}
```

`src/FactionWars/Combat/Models/AreaAnchor.cs`:
```csharp
using FactionWars.Core.Interfaces;

namespace FactionWars.Combat.Models
{
    /// <summary>The centre and radius that Hold Area / Search &amp; Destroy operate within.</summary>
    public readonly struct AreaAnchor
    {
        public Vector3 Center { get; }
        public float Radius { get; }

        public AreaAnchor(Vector3 center, float radius)
        {
            Center = center;
            Radius = radius;
        }
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~SquadValueObjectsTests"`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/FactionWars/Combat/Models/BodyguardPosition.cs src/FactionWars/Combat/Models/EnemyTarget.cs src/FactionWars/Combat/Models/AreaAnchor.cs tests/FactionWars.Tests/Unit/Combat/SquadValueObjectsTests.cs
git commit -m "feat: add squad position/target/anchor value objects (#35)"
```

---

### Task 4: SquadStanceResolver (geometry stances)

**Files:**
- Create: `src/FactionWars/Combat/Interfaces/ISquadStanceResolver.cs`
- Create: `src/FactionWars/Combat/Services/SquadStanceResolver.cs`
- Test: `tests/FactionWars.Tests/Unit/Combat/SquadStanceResolverTests.cs`

**Interfaces:**
- Consumes: `SquadStance`, `BodyguardOrder`, `BodyguardOrderKind`, `Vector3`.
- Produces: `ISquadStanceResolver.Resolve(SquadStance stance, Vector3 anchorCenter, float anchorRadius, int bodyguardIndex, int bodyguardCount) -> BodyguardOrder`.

- [ ] **Step 1: Write the failing test**

```csharp
using FactionWars.Combat.Models;
using FactionWars.Combat.Services;
using FactionWars.Core.Interfaces;
using Xunit;

namespace FactionWars.Tests.Unit.Combat
{
    public class SquadStanceResolverTests
    {
        private readonly SquadStanceResolver _resolver = new SquadStanceResolver();
        private readonly Vector3 _center = new Vector3(100f, 200f, 30f);

        [Fact]
        public void Escort_ResolvesToFollowPlayer()
        {
            var order = _resolver.Resolve(SquadStance.Escort, _center, 50f, 0, 3);
            Assert.Equal(BodyguardOrderKind.FollowPlayer, order.Kind);
        }

        [Fact]
        public void SearchAndDestroy_ResolvesToSeekInRadiusWithAnchor()
        {
            var order = _resolver.Resolve(SquadStance.SearchAndDestroy, _center, 50f, 0, 3);
            Assert.Equal(BodyguardOrderKind.SeekInRadius, order.Kind);
            Assert.Equal(_center, order.Point);
            Assert.Equal(50f, order.Radius);
        }

        [Fact]
        public void HoldArea_GivesDistinctPointsPerIndexWithinRadius()
        {
            var first = _resolver.Resolve(SquadStance.HoldArea, _center, 50f, 0, 3);
            var second = _resolver.Resolve(SquadStance.HoldArea, _center, 50f, 1, 3);

            Assert.Equal(BodyguardOrderKind.HoldAtPoint, first.Kind);
            Assert.NotEqual(first.Point, second.Point);
            Assert.True(_center.DistanceTo2D(first.Point) <= 50f);
            Assert.True(_center.DistanceTo2D(second.Point) <= 50f);
        }
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~SquadStanceResolverTests"`
Expected: FAIL — `ISquadStanceResolver` / `SquadStanceResolver` not defined.

- [ ] **Step 3: Write the interface**

`src/FactionWars/Combat/Interfaces/ISquadStanceResolver.cs`:
```csharp
using FactionWars.Combat.Models;
using FactionWars.Core.Interfaces;

namespace FactionWars.Combat.Interfaces
{
    /// <summary>
    /// Pure geometry logic mapping a stance + anchor + bodyguard slot to a single order.
    /// Live target assignment for Search &amp; Destroy is handled separately by
    /// <see cref="ITargetAssignmentResolver"/>; this resolver returns the seek fallback.
    /// </summary>
    public interface ISquadStanceResolver
    {
        BodyguardOrder Resolve(SquadStance stance, Vector3 anchorCenter, float anchorRadius, int bodyguardIndex, int bodyguardCount);
    }
}
```

- [ ] **Step 4: Write the implementation**

`src/FactionWars/Combat/Services/SquadStanceResolver.cs`:
```csharp
using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Models;
using FactionWars.Core.Interfaces;

namespace FactionWars.Combat.Services
{
    public class SquadStanceResolver : ISquadStanceResolver
    {
        private const float RingFraction = 0.6f;

        public BodyguardOrder Resolve(SquadStance stance, Vector3 anchorCenter, float anchorRadius, int bodyguardIndex, int bodyguardCount)
        {
            switch (stance)
            {
                case SquadStance.HoldArea:
                    return BodyguardOrder.HoldAtPoint(RingPoint(anchorCenter, anchorRadius, bodyguardIndex, bodyguardCount));
                case SquadStance.SearchAndDestroy:
                    return BodyguardOrder.SeekInRadius(anchorCenter, anchorRadius);
                default:
                    return BodyguardOrder.FollowPlayer();
            }
        }

        private static Vector3 RingPoint(Vector3 center, float radius, int index, int count)
        {
            if (count <= 0) count = 1;
            double angle = 2.0 * System.Math.PI * index / count;
            float r = radius * RingFraction;
            float x = center.X + (float)(r * System.Math.Cos(angle));
            float y = center.Y + (float)(r * System.Math.Sin(angle));
            return new Vector3(x, y, center.Z);
        }
    }
}
```

- [ ] **Step 5: Run test to verify it passes**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~SquadStanceResolverTests"`
Expected: PASS

- [ ] **Step 6: Commit**

```bash
git add src/FactionWars/Combat/Interfaces/ISquadStanceResolver.cs src/FactionWars/Combat/Services/SquadStanceResolver.cs tests/FactionWars.Tests/Unit/Combat/SquadStanceResolverTests.cs
git commit -m "feat: add SquadStanceResolver geometry logic (#35)"
```

---

### Task 5: TargetAssignmentResolver

**Files:**
- Create: `src/FactionWars/Combat/Interfaces/ITargetAssignmentResolver.cs`
- Create: `src/FactionWars/Combat/Services/TargetAssignmentResolver.cs`
- Test: `tests/FactionWars.Tests/Unit/Combat/TargetAssignmentResolverTests.cs`

**Interfaces:**
- Consumes: `BodyguardPosition`, `EnemyTarget`, `Vector3`.
- Produces: `ITargetAssignmentResolver.Assign(IReadOnlyList<BodyguardPosition> bodyguards, IReadOnlyList<EnemyTarget> enemies) -> IReadOnlyDictionary<int, int>` (bodyguardHandle → enemyHandle).

- [ ] **Step 1: Write the failing test**

```csharp
using System.Collections.Generic;
using System.Linq;
using FactionWars.Combat.Models;
using FactionWars.Combat.Services;
using FactionWars.Core.Interfaces;
using Xunit;

namespace FactionWars.Tests.Unit.Combat
{
    public class TargetAssignmentResolverTests
    {
        private readonly TargetAssignmentResolver _resolver = new TargetAssignmentResolver();

        private static BodyguardPosition Bg(int h, float x) => new BodyguardPosition(h, new Vector3(x, 0f, 0f));
        private static EnemyTarget En(int h, float x) => new EnemyTarget(h, new Vector3(x, 0f, 0f));

        [Fact]
        public void TwoBodyguardsTwoEnemies_SpreadAcrossDistinctTargets()
        {
            var bodyguards = new List<BodyguardPosition> { Bg(1, 0f), Bg(2, 100f) };
            var enemies = new List<EnemyTarget> { En(10, 5f), En(20, 95f) };

            var map = _resolver.Assign(bodyguards, enemies);

            Assert.Equal(10, map[1]); // nearest to bodyguard 1
            Assert.Equal(20, map[2]); // nearest to bodyguard 2
            Assert.Equal(2, map.Values.Distinct().Count());
        }

        [Fact]
        public void MoreBodyguardsThanEnemies_ExtrasDoubleUpAndAllAssigned()
        {
            var bodyguards = new List<BodyguardPosition> { Bg(1, 0f), Bg(2, 10f), Bg(3, 200f) };
            var enemies = new List<EnemyTarget> { En(10, 0f), En(20, 210f) };

            var map = _resolver.Assign(bodyguards, enemies);

            Assert.Equal(3, map.Count);
            Assert.All(map.Values, v => Assert.Contains(v, new[] { 10, 20 }));
            Assert.Equal(20, map[3]); // bodyguard 3 nearest to enemy 20
        }

        [Fact]
        public void NoEnemies_ReturnsEmptyMap()
        {
            var map = _resolver.Assign(new List<BodyguardPosition> { Bg(1, 0f) }, new List<EnemyTarget>());
            Assert.Empty(map);
        }

        [Fact]
        public void NoBodyguards_ReturnsEmptyMap()
        {
            var map = _resolver.Assign(new List<BodyguardPosition>(), new List<EnemyTarget> { En(10, 0f) });
            Assert.Empty(map);
        }
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~TargetAssignmentResolverTests"`
Expected: FAIL — types not defined.

- [ ] **Step 3: Write the interface**

`src/FactionWars/Combat/Interfaces/ITargetAssignmentResolver.cs`:
```csharp
using System.Collections.Generic;
using FactionWars.Combat.Models;

namespace FactionWars.Combat.Interfaces
{
    /// <summary>
    /// Maps each bodyguard to a known enemy target. Greedy-nearest with balancing so
    /// bodyguards spread across distinct enemies before doubling up.
    /// </summary>
    public interface ITargetAssignmentResolver
    {
        IReadOnlyDictionary<int, int> Assign(IReadOnlyList<BodyguardPosition> bodyguards, IReadOnlyList<EnemyTarget> enemies);
    }
}
```

- [ ] **Step 4: Write the implementation**

`src/FactionWars/Combat/Services/TargetAssignmentResolver.cs`:
```csharp
using System.Collections.Generic;
using System.Linq;
using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Models;

namespace FactionWars.Combat.Services
{
    public class TargetAssignmentResolver : ITargetAssignmentResolver
    {
        public IReadOnlyDictionary<int, int> Assign(IReadOnlyList<BodyguardPosition> bodyguards, IReadOnlyList<EnemyTarget> enemies)
        {
            var result = new Dictionary<int, int>();
            if (bodyguards == null || bodyguards.Count == 0 || enemies == null || enemies.Count == 0)
            {
                return result;
            }

            var load = new Dictionary<int, int>();
            foreach (var enemy in enemies)
            {
                load[enemy.Handle] = 0;
            }

            foreach (var bodyguard in bodyguards)
            {
                int minLoad = load.Values.Min();
                EnemyTarget best = enemies[0];
                float bestDistance = float.MaxValue;
                foreach (var enemy in enemies)
                {
                    if (load[enemy.Handle] != minLoad) continue;
                    float distance = bodyguard.Position.DistanceTo(enemy.Position);
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        best = enemy;
                    }
                }

                result[bodyguard.Handle] = best.Handle;
                load[best.Handle]++;
            }

            return result;
        }
    }
}
```

- [ ] **Step 5: Run test to verify it passes**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~TargetAssignmentResolverTests"`
Expected: PASS

- [ ] **Step 6: Commit**

```bash
git add src/FactionWars/Combat/Interfaces/ITargetAssignmentResolver.cs src/FactionWars/Combat/Services/TargetAssignmentResolver.cs tests/FactionWars.Tests/Unit/Combat/TargetAssignmentResolverTests.cs
git commit -m "feat: add TargetAssignmentResolver greedy-nearest assignment (#35)"
```

---

### Task 6: AreaAnchorResolver

**Files:**
- Create: `src/FactionWars/Combat/Interfaces/IAreaAnchorResolver.cs`
- Create: `src/FactionWars/Combat/Services/AreaAnchorResolver.cs`
- Test: `tests/FactionWars.Tests/Unit/Combat/AreaAnchorResolverTests.cs`

**Interfaces:**
- Consumes: `AreaAnchor`, `Vector3`.
- Produces: `IAreaAnchorResolver.Resolve(Vector3? zoneCenter, float zoneRadius, Vector3 playerPosition, float defaultRadius) -> AreaAnchor`.

- [ ] **Step 1: Write the failing test**

```csharp
using FactionWars.Combat.Models;
using FactionWars.Combat.Services;
using FactionWars.Core.Interfaces;
using Xunit;

namespace FactionWars.Tests.Unit.Combat
{
    public class AreaAnchorResolverTests
    {
        private readonly AreaAnchorResolver _resolver = new AreaAnchorResolver();

        [Fact]
        public void InZone_UsesZoneCentreAndRadius()
        {
            var zoneCenter = new Vector3(10f, 20f, 30f);
            var anchor = _resolver.Resolve(zoneCenter, 150f, new Vector3(0f, 0f, 0f), 30f);
            Assert.Equal(zoneCenter, anchor.Center);
            Assert.Equal(150f, anchor.Radius);
        }

        [Fact]
        public void OutOfZone_UsesPlayerPositionAndDefaultRadius()
        {
            var playerPos = new Vector3(5f, 5f, 5f);
            var anchor = _resolver.Resolve(null, 0f, playerPos, 30f);
            Assert.Equal(playerPos, anchor.Center);
            Assert.Equal(30f, anchor.Radius);
        }
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~AreaAnchorResolverTests"`
Expected: FAIL — types not defined.

- [ ] **Step 3: Write the interface**

`src/FactionWars/Combat/Interfaces/IAreaAnchorResolver.cs`:
```csharp
using FactionWars.Combat.Models;
using FactionWars.Core.Interfaces;

namespace FactionWars.Combat.Interfaces
{
    /// <summary>
    /// Resolves the squad's operating anchor: the current zone when inside one,
    /// otherwise the player's position with a default loose radius.
    /// </summary>
    public interface IAreaAnchorResolver
    {
        AreaAnchor Resolve(Vector3? zoneCenter, float zoneRadius, Vector3 playerPosition, float defaultRadius);
    }
}
```

- [ ] **Step 4: Write the implementation**

`src/FactionWars/Combat/Services/AreaAnchorResolver.cs`:
```csharp
using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Models;
using FactionWars.Core.Interfaces;

namespace FactionWars.Combat.Services
{
    public class AreaAnchorResolver : IAreaAnchorResolver
    {
        public AreaAnchor Resolve(Vector3? zoneCenter, float zoneRadius, Vector3 playerPosition, float defaultRadius)
        {
            if (zoneCenter.HasValue)
            {
                return new AreaAnchor(zoneCenter.Value, zoneRadius);
            }

            return new AreaAnchor(playerPosition, defaultRadius);
        }
    }
}
```

- [ ] **Step 5: Run test to verify it passes**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~AreaAnchorResolverTests"`
Expected: PASS

- [ ] **Step 6: Commit**

```bash
git add src/FactionWars/Combat/Interfaces/IAreaAnchorResolver.cs src/FactionWars/Combat/Services/AreaAnchorResolver.cs tests/FactionWars.Tests/Unit/Combat/AreaAnchorResolverTests.cs
git commit -m "feat: add AreaAnchorResolver (#35)"
```

---

### Task 7: New natives — TaskGuardArea & TaskCombatPed

**Files:**
- Modify: `src/FactionWars/Core/Interfaces/IGameBridge.cs` (add two method declarations)
- Create: `src/FactionWars/ScriptHookV/GameBridge.SquadTasks.cs`
- Modify: `src/FactionWars/Core/Utils/MockGameBridge.cs` (add impls + recording + query helpers)
- Test: `tests/FactionWars.Tests/Unit/Core/MockGameBridgeTests.cs` (add two tests)

**Interfaces:**
- Produces on `IGameBridge`: `void TaskGuardArea(int pedHandle, Vector3 center, float radius)`; `void TaskCombatPed(int pedHandle, int targetPedHandle)`.
- Produces on `MockGameBridge` (recording helpers, not on the interface): `bool IsPedGuardingArea(int)`, `Vector3 GetGuardAreaCenter(int)`, `float GetGuardAreaRadius(int)`, `bool IsPedCombatingPed(int)`, `int GetCombatPedTarget(int)`.

- [ ] **Step 1: Write the failing mock tests**

Add to `tests/FactionWars.Tests/Unit/Core/MockGameBridgeTests.cs` (inside the existing `MockGameBridgeTests` class):
```csharp
        [Fact]
        public void TaskGuardArea_RecordsCentreAndRadius()
        {
            var mock = new MockGameBridge();
            int ped = mock.CreatePed("test", new Vector3(0f, 0f, 0f));

            mock.TaskGuardArea(ped, new Vector3(10f, 20f, 30f), 8f);

            Assert.True(mock.IsPedGuardingArea(ped));
            Assert.Equal(new Vector3(10f, 20f, 30f), mock.GetGuardAreaCenter(ped));
            Assert.Equal(8f, mock.GetGuardAreaRadius(ped));
        }

        [Fact]
        public void TaskCombatPed_RecordsTarget()
        {
            var mock = new MockGameBridge();
            int ped = mock.CreatePed("test", new Vector3(0f, 0f, 0f));

            mock.TaskCombatPed(ped, 555);

            Assert.True(mock.IsPedCombatingPed(ped));
            Assert.Equal(555, mock.GetCombatPedTarget(ped));
        }
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~MockGameBridgeTests"`
Expected: FAIL — `TaskGuardArea` / `TaskCombatPed` / helpers not defined.

- [ ] **Step 3: Declare the natives on IGameBridge**

In `src/FactionWars/Core/Interfaces/IGameBridge.cs`, near the other task declarations (e.g. just after `TaskCombatHatedTargetsAroundPed`), add:
```csharp
        /// <summary>
        /// Tasks the ped to defend a sphere centred on <paramref name="center"/> with the
        /// given radius, taking cover and engaging hostiles that enter the area.
        /// </summary>
        void TaskGuardArea(int pedHandle, Vector3 center, float radius);

        /// <summary>
        /// Tasks the ped to run to and fight a specific target ped.
        /// </summary>
        void TaskCombatPed(int pedHandle, int targetPedHandle);
```

- [ ] **Step 4: Implement the real natives**

`src/FactionWars/ScriptHookV/GameBridge.SquadTasks.cs`:
```csharp
using System;
using GTA;
using GTA.Native;
using FactionWars.ScriptHookV.Logging;
using DomainVector3 = FactionWars.Core.Interfaces.Vector3;

namespace FactionWars.ScriptHookV
{
    public partial class GameBridge
    {
        public void TaskGuardArea(int pedHandle, DomainVector3 center, float radius)
        {
            FileLogger.AI($"TaskGuardArea: CALLED for ped {pedHandle} center ({center.X:F1}, {center.Y:F1}, {center.Z:F1}) radius {radius:F1}");

            try
            {
                var ped = Entity.FromHandle(pedHandle) as Ped;
                if (ped == null || !ped.Exists())
                {
                    FileLogger.Warn($"TaskGuardArea: Ped {pedHandle} is null or doesn't exist, aborting");
                    return;
                }

                // Defend a sphere centred on the hold point. The defensive area keeps the ped
                // anchored near the point; the guard task makes it hold and engage from cover.
                // NOTE: confirm the TASK_GUARD_SPHERE_DEFENSIVE_AREA parameter order against the
                // installed SHVDN Hash enum via in-game logs and adjust if peds wander off.
                Function.Call(Hash.SET_PED_SPHERE_DEFENSIVE_AREA, ped.Handle, center.X, center.Y, center.Z, radius, false, 0);
                Function.Call(
                    Hash.TASK_GUARD_SPHERE_DEFENSIVE_AREA,
                    ped.Handle,
                    center.X, center.Y, center.Z,
                    0.0f,
                    radius,
                    -1,
                    center.X, center.Y, center.Z,
                    radius);
                Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped.Handle, 5, true); // BF_CanUseCover

                FileLogger.AI($"TaskGuardArea: COMPLETED for ped {pedHandle}");
            }
            catch (Exception ex)
            {
                FileLogger.Error($"TaskGuardArea exception for ped {pedHandle}", ex);
            }
        }

        public void TaskCombatPed(int pedHandle, int targetPedHandle)
        {
            FileLogger.AI($"TaskCombatPed: CALLED for ped {pedHandle} -> target {targetPedHandle}");

            try
            {
                var ped = Entity.FromHandle(pedHandle) as Ped;
                var target = Entity.FromHandle(targetPedHandle) as Ped;
                if (ped == null || !ped.Exists())
                {
                    FileLogger.Warn($"TaskCombatPed: Ped {pedHandle} is null or doesn't exist, aborting");
                    return;
                }
                if (target == null || !target.Exists())
                {
                    FileLogger.Warn($"TaskCombatPed: Target {targetPedHandle} is null or doesn't exist, aborting");
                    return;
                }

                Function.Call(Hash.TASK_COMBAT_PED, ped.Handle, target.Handle, 0, 16);
                Function.Call(Hash.SET_PED_DESIRED_MOVE_BLEND_RATIO, ped.Handle, 3.0f);

                FileLogger.AI($"TaskCombatPed: COMPLETED for ped {pedHandle}");
            }
            catch (Exception ex)
            {
                FileLogger.Error($"TaskCombatPed exception for ped {pedHandle}", ex);
            }
        }
    }
}
```

> If `Hash.TASK_GUARD_SPHERE_DEFENSIVE_AREA` is not present in the installed SHVDN `Hash` enum, fall back to `Hash.TASK_GUARD_CURRENT_POSITION(ped, radius, radius, true)` after the `SET_PED_SPHERE_DEFENSIVE_AREA` call. Verify behaviour via the `FileLogger.AI` output described in CLAUDE.md.

- [ ] **Step 5: Implement the mock natives + recording**

In `src/FactionWars/Core/Utils/MockGameBridge.cs`, add fields and methods (place near the other task-recording dictionaries such as `_combatTargetingPeds`):
```csharp
        private readonly Dictionary<int, Vector3> _guardAreaCenter = new Dictionary<int, Vector3>();
        private readonly Dictionary<int, float> _guardAreaRadius = new Dictionary<int, float>();
        private readonly Dictionary<int, int> _combatPedTargets = new Dictionary<int, int>();

        public void TaskGuardArea(int pedHandle, Vector3 center, float radius)
        {
            if (!_peds.ContainsKey(pedHandle)) return;

            // Primary-task replacement: a new TASK_X wipes the previous task.
            _wanderingPeds.Remove(pedHandle);
            _pedsFacingPosition.Remove(pedHandle);
            _combatTargetingPeds.Remove(pedHandle);
            _goToEntityPeds.Remove(pedHandle);
            _followEntityPeds.Remove(pedHandle);
            _combatPedTargets.Remove(pedHandle);

            _guardAreaCenter[pedHandle] = center;
            _guardAreaRadius[pedHandle] = radius;
        }

        public bool IsPedGuardingArea(int pedHandle) => _guardAreaCenter.ContainsKey(pedHandle);

        public Vector3 GetGuardAreaCenter(int pedHandle)
            => _guardAreaCenter.TryGetValue(pedHandle, out var c) ? c : Vector3.Zero;

        public float GetGuardAreaRadius(int pedHandle)
            => _guardAreaRadius.TryGetValue(pedHandle, out var r) ? r : 0f;

        public void TaskCombatPed(int pedHandle, int targetPedHandle)
        {
            if (!_peds.ContainsKey(pedHandle)) return;

            // Primary-task replacement.
            _wanderingPeds.Remove(pedHandle);
            _pedsFacingPosition.Remove(pedHandle);
            _combatTargetingPeds.Remove(pedHandle);
            _goToEntityPeds.Remove(pedHandle);
            _followEntityPeds.Remove(pedHandle);
            _guardAreaCenter.Remove(pedHandle);
            _guardAreaRadius.Remove(pedHandle);

            _combatPedTargets[pedHandle] = targetPedHandle;
        }

        public bool IsPedCombatingPed(int pedHandle) => _combatPedTargets.ContainsKey(pedHandle);

        public int GetCombatPedTarget(int pedHandle)
            => _combatPedTargets.TryGetValue(pedHandle, out var t) ? t : -1;
```

> If any of `_wanderingPeds`, `_pedsFacingPosition`, `_goToEntityPeds`, `_followEntityPeds`, `_combatTargetingPeds` is not present under that exact name in `MockGameBridge`, drop the missing `.Remove(...)` line — they exist only to mirror task replacement and are not asserted here.

- [ ] **Step 6: Run tests to verify they pass**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~MockGameBridgeTests"`
Expected: PASS

- [ ] **Step 7: Build the whole solution (analyzer check)**

Run: `dotnet build FactionWars.sln --no-incremental`
Expected: 0 Warnings, 0 Errors. (Both `GameBridge` and `MockGameBridge` must implement the new `IGameBridge` members or the build fails.)

- [ ] **Step 8: Commit**

```bash
git add src/FactionWars/Core/Interfaces/IGameBridge.cs src/FactionWars/ScriptHookV/GameBridge.SquadTasks.cs src/FactionWars/Core/Utils/MockGameBridge.cs tests/FactionWars.Tests/Unit/Core/MockGameBridgeTests.cs
git commit -m "feat: add TaskGuardArea and TaskCombatPed natives (#35)"
```

---

### Task 8: Expose hostile ped handles from the defender managers

**Files:**
- Create: `src/FactionWars/ScriptHookV/Managers/Interfaces/IHostilePedHandleSource.cs`
- Modify: `src/FactionWars/ScriptHookV/Managers/EnemyDefenderManager.cs` (add interface to declaration + method)
- Modify: `src/FactionWars/ScriptHookV/Managers/BattleAttackerManager.cs` (add interface to declaration + method)
- Test: `tests/FactionWars.Tests/Unit/ScriptHookV/Managers/EnemyDefenderManagerTests.cs` (add a test)
- Test: `tests/FactionWars.Tests/Unit/ScriptHookV/Managers/BattleAttackerManagerTests.cs` (add a test)

**Interfaces:**
- Produces: `IHostilePedHandleSource.GetHostilePedHandles() -> IReadOnlyList<int>`, implemented by both managers (interface members → excluded from `CI0004`).
- Both managers track spawned peds in a field `private readonly Dictionary<string, Dictionary<int, DefenderTier>> _spawnedPedTierByZone;` (zoneId → pedHandle → tier).

- [ ] **Step 1: Write the failing test (EnemyDefenderManager)**

Add to `EnemyDefenderManagerTests` (the existing `SetupManager()` helper builds `_manager` with `_gameBridge`, mocked services; `_pedSpawningServiceMock.SpawnPed` returns a `PedHandle` whose handle comes from `_gameBridge.CreatePed`):
```csharp
        [Fact]
        public void GetHostilePedHandles_ReturnsSpawnedEnemyHandles()
        {
            SetupManager();
            var zone = new Zone("z1", "Zone 1", new Vector3(0f, 0f, 0f), 150f) { OwnerFactionId = "trevor" };
            _allocationServiceMock
                .Setup(a => a.GetAllocation("trevor", "z1"))
                .Returns(new ZoneDefenderAllocation("trevor", "z1", new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 2 } }));

            _manager.OnEnemyZoneEntered(zone, "trevor");

            var handles = _manager.GetHostilePedHandles();
            Assert.NotEmpty(handles);
            Assert.Equal(_manager.GetSpawnedDefenderCount("z1"), handles.Count);
        }
```

> Match the exact `Zone` constructor, `ZoneDefenderAllocation` shape, and allocation-mock setup already used by neighbouring tests in this file (e.g. `DespawnForZone_RemovesSpawnedDefendersAndBlips`). If those helpers differ, copy their arrangement verbatim and only keep the final two assertions.

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~EnemyDefenderManagerTests.GetHostilePedHandles_ReturnsSpawnedEnemyHandles"`
Expected: FAIL — `GetHostilePedHandles` not defined.

- [ ] **Step 3: Write the interface**

`src/FactionWars/ScriptHookV/Managers/Interfaces/IHostilePedHandleSource.cs`:
```csharp
using System.Collections.Generic;

namespace FactionWars.ScriptHookV.Managers.Interfaces
{
    /// <summary>
    /// Exposes the live hostile ped handles a manager currently tracks, so squad
    /// Search &amp; Destroy can target known enemies.
    /// </summary>
    public interface IHostilePedHandleSource
    {
        IReadOnlyList<int> GetHostilePedHandles();
    }
}
```

- [ ] **Step 4: Implement on EnemyDefenderManager**

In `src/FactionWars/ScriptHookV/Managers/EnemyDefenderManager.cs`, add the interface to the partial declaration and add the method. Add `using FactionWars.ScriptHookV.Managers.Interfaces;` and `using System.Collections.Generic;` if not already present. Change the class line to include `IHostilePedHandleSource` (append to the existing base list, or add `: IHostilePedHandleSource` if none):
```csharp
    public partial class EnemyDefenderManager : IHostilePedHandleSource
```
Add the method body:
```csharp
        public IReadOnlyList<int> GetHostilePedHandles()
        {
            var handles = new List<int>();
            foreach (var pedsInZone in _spawnedPedTierByZone.Values)
            {
                handles.AddRange(pedsInZone.Keys);
            }
            return handles;
        }
```

- [ ] **Step 5: Run EnemyDefenderManager test to verify it passes**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~EnemyDefenderManagerTests.GetHostilePedHandles_ReturnsSpawnedEnemyHandles"`
Expected: PASS

- [ ] **Step 6: Write the failing test (BattleAttackerManager)**

Add to `BattleAttackerManagerTests` a test mirroring the existing spawn-setup in that file (it spawns attackers via `OnPlayerZoneEntered`):
```csharp
        [Fact]
        public void GetHostilePedHandles_ReturnsSpawnedAttackerHandles()
        {
            SetupManager(); // or the file's existing arrange helper
            // ... arrange a battle / zone entry exactly as the neighbouring spawn test does ...

            var handles = _manager.GetHostilePedHandles();
            Assert.NotEmpty(handles);
        }
```

> Copy the arrange block (zone/battle/allocation setup that causes attackers to spawn) verbatim from the existing attacker-spawn test in this file; keep only the final `GetHostilePedHandles` assertion.

- [ ] **Step 7: Run test to verify it fails**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~BattleAttackerManagerTests.GetHostilePedHandles_ReturnsSpawnedAttackerHandles"`
Expected: FAIL — `GetHostilePedHandles` not defined.

- [ ] **Step 8: Implement on BattleAttackerManager**

In `src/FactionWars/ScriptHookV/Managers/BattleAttackerManager.cs`, append `IHostilePedHandleSource` to the partial class declaration (add `using` lines as needed) and add:
```csharp
        public IReadOnlyList<int> GetHostilePedHandles()
        {
            var handles = new List<int>();
            foreach (var pedsInZone in _spawnedPedTierByZone.Values)
            {
                handles.AddRange(pedsInZone.Keys);
            }
            return handles;
        }
```

- [ ] **Step 9: Run both manager tests + full build**

Run: `dotnet build FactionWars.sln --no-incremental`
Expected: 0 Warnings, 0 Errors.
Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~GetHostilePedHandles"`
Expected: PASS (both).

- [ ] **Step 10: Commit**

```bash
git add src/FactionWars/ScriptHookV/Managers/Interfaces/IHostilePedHandleSource.cs src/FactionWars/ScriptHookV/Managers/EnemyDefenderManager.cs src/FactionWars/ScriptHookV/Managers/BattleAttackerManager.cs tests/FactionWars.Tests/Unit/ScriptHookV/Managers/EnemyDefenderManagerTests.cs tests/FactionWars.Tests/Unit/ScriptHookV/Managers/BattleAttackerManagerTests.cs
git commit -m "feat: expose hostile ped handles via IHostilePedHandleSource (#35)"
```

---

### Task 9: EnemyTargetCollector (handles → targets within radius)

**Files:**
- Create: `src/FactionWars/ScriptHookV/Combat/Interfaces/IEnemyTargetCollector.cs`
- Create: `src/FactionWars/ScriptHookV/Combat/EnemyTargetCollector.cs`
- Test: `tests/FactionWars.Tests/Unit/ScriptHookV/Combat/EnemyTargetCollectorTests.cs`

**Interfaces:**
- Consumes: `IGameBridge.GetPedPosition(int) -> Vector3`, `EnemyTarget`, `Vector3.DistanceTo`.
- Produces: `IEnemyTargetCollector.Collect(IReadOnlyList<int> hostileHandles, Vector3 center, float radius) -> IReadOnlyList<EnemyTarget>` (only handles within `radius` of `center`).

- [ ] **Step 1: Write the failing test**

```csharp
using System.Collections.Generic;
using System.Linq;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Utils;
using FactionWars.ScriptHookV.Combat;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV.Combat
{
    public class EnemyTargetCollectorTests
    {
        [Fact]
        public void Collect_ReturnsOnlyHandlesWithinRadius()
        {
            var bridge = new MockGameBridge();
            int near = bridge.CreatePed("e1", new Vector3(5f, 0f, 0f));
            int far = bridge.CreatePed("e2", new Vector3(500f, 0f, 0f));
            var collector = new EnemyTargetCollector(bridge);

            var result = collector.Collect(new List<int> { near, far }, new Vector3(0f, 0f, 0f), 50f);

            Assert.Single(result);
            Assert.Equal(near, result[0].Handle);
            Assert.Equal(new Vector3(5f, 0f, 0f), result[0].Position);
        }

        [Fact]
        public void Collect_EmptyInput_ReturnsEmpty()
        {
            var collector = new EnemyTargetCollector(new MockGameBridge());
            Assert.Empty(collector.Collect(new List<int>(), new Vector3(0f, 0f, 0f), 50f));
        }
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~EnemyTargetCollectorTests"`
Expected: FAIL — types not defined.

- [ ] **Step 3: Write the interface**

`src/FactionWars/ScriptHookV/Combat/Interfaces/IEnemyTargetCollector.cs`:
```csharp
using System.Collections.Generic;
using FactionWars.Combat.Models;
using FactionWars.Core.Interfaces;

namespace FactionWars.ScriptHookV.Combat.Interfaces
{
    /// <summary>
    /// Reads live ped positions and returns those hostile handles within a radius of a centre.
    /// </summary>
    public interface IEnemyTargetCollector
    {
        IReadOnlyList<EnemyTarget> Collect(IReadOnlyList<int> hostileHandles, Vector3 center, float radius);
    }
}
```

- [ ] **Step 4: Write the implementation**

`src/FactionWars/ScriptHookV/Combat/EnemyTargetCollector.cs`:
```csharp
using System;
using System.Collections.Generic;
using FactionWars.Combat.Models;
using FactionWars.Core.Interfaces;
using FactionWars.ScriptHookV.Combat.Interfaces;

namespace FactionWars.ScriptHookV.Combat
{
    public class EnemyTargetCollector : IEnemyTargetCollector
    {
        private readonly IGameBridge _gameBridge;

        public EnemyTargetCollector(IGameBridge gameBridge)
        {
            _gameBridge = gameBridge ?? throw new ArgumentNullException(nameof(gameBridge));
        }

        public IReadOnlyList<EnemyTarget> Collect(IReadOnlyList<int> hostileHandles, Vector3 center, float radius)
        {
            var result = new List<EnemyTarget>();
            if (hostileHandles == null) return result;

            foreach (var handle in hostileHandles)
            {
                var position = _gameBridge.GetPedPosition(handle);
                if (center.DistanceTo(position) <= radius)
                {
                    result.Add(new EnemyTarget(handle, position));
                }
            }

            return result;
        }
    }
}
```

- [ ] **Step 5: Run test to verify it passes**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~EnemyTargetCollectorTests"`
Expected: PASS

- [ ] **Step 6: Commit**

```bash
git add src/FactionWars/ScriptHookV/Combat/Interfaces/IEnemyTargetCollector.cs src/FactionWars/ScriptHookV/Combat/EnemyTargetCollector.cs tests/FactionWars.Tests/Unit/ScriptHookV/Combat/EnemyTargetCollectorTests.cs
git commit -m "feat: add EnemyTargetCollector (#35)"
```

---

### Task 10: SquadStanceController

**Files:**
- Create: `src/FactionWars/ScriptHookV/Managers/SquadStanceController.cs`
- Create: `src/FactionWars/ScriptHookV/Managers/SquadStanceController.Stances.cs`
- Test: `tests/FactionWars.Tests/Unit/ScriptHookV/Managers/SquadStanceControllerTests.cs`

**Interfaces:**
- Consumes: `IGameBridge` (`ShowNotification`, `GetPedPosition`, `TaskGuardArea`, `TaskCombatPed`, `TaskCombatHatedTargetsAroundPed`, `IsPlayerDead`, `GetGameTime`, `IsPedInVehicle`, `TaskPedLeaveVehicle`, `IsPedFollowingPlayer`, `IsPedInCombat`, `SetPedAsFollower`), `ISquadStanceResolver`, `ITargetAssignmentResolver`, `SquadStance`, `SquadStanceExtensions.Next`, `BodyguardOrder`, `BodyguardPosition`, `EnemyTarget`.
- Produces: `SquadStanceController` with ctor `(IGameBridge, ISquadStanceResolver, ITargetAssignmentResolver)`; `SquadStance CurrentStance { get; }`; `void CycleStance(IReadOnlyList<int> onFootBodyguardHandles)`; `void Update(Vector3 anchorCenter, float anchorRadius, IReadOnlyList<int> onFootBodyguardHandles, IReadOnlyList<EnemyTarget> enemiesInRange)`.

- [ ] **Step 1: Write the failing tests**

```csharp
using System.Collections.Generic;
using FactionWars.Combat.Models;
using FactionWars.Combat.Services;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Utils;
using FactionWars.ScriptHookV.Managers;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV.Managers
{
    public class SquadStanceControllerTests
    {
        private readonly MockGameBridge _bridge = new MockGameBridge();
        private SquadStanceController _controller;

        private SquadStanceController Build()
            => new SquadStanceController(_bridge, new SquadStanceResolver(), new TargetAssignmentResolver());

        private static readonly Vector3 Anchor = new Vector3(0f, 0f, 0f);

        [Fact]
        public void CycleStance_AdvancesEscortToHoldAreaToSearchAndDestroyToEscort()
        {
            _controller = Build();
            var party = new List<int> { _bridge.CreatePed("bg", new Vector3(1f, 0f, 0f)) };

            Assert.Equal(SquadStance.Escort, _controller.CurrentStance);
            _controller.CycleStance(party);
            Assert.Equal(SquadStance.HoldArea, _controller.CurrentStance);
            _controller.CycleStance(party);
            Assert.Equal(SquadStance.SearchAndDestroy, _controller.CurrentStance);
            _controller.CycleStance(party);
            Assert.Equal(SquadStance.Escort, _controller.CurrentStance);
        }

        [Fact]
        public void CycleStance_EmptyParty_DoesNotChangeStance()
        {
            _controller = Build();
            _controller.CycleStance(new List<int>());
            Assert.Equal(SquadStance.Escort, _controller.CurrentStance);
        }

        [Fact]
        public void Update_HoldArea_IssuesTaskGuardArea()
        {
            _controller = Build();
            int bg = _bridge.CreatePed("bg", new Vector3(1f, 0f, 0f));
            var party = new List<int> { bg };
            _controller.CycleStance(party); // -> HoldArea

            _controller.Update(Anchor, 50f, party, new List<EnemyTarget>());

            Assert.True(_bridge.IsPedGuardingArea(bg));
        }

        [Fact]
        public void Update_SearchAndDestroy_WithEnemy_IssuesTaskCombatPed()
        {
            _controller = Build();
            int bg = _bridge.CreatePed("bg", new Vector3(1f, 0f, 0f));
            var party = new List<int> { bg };
            _controller.CycleStance(party); // HoldArea
            _controller.CycleStance(party); // SearchAndDestroy

            var enemy = new EnemyTarget(777, new Vector3(10f, 0f, 0f));
            _controller.Update(Anchor, 50f, party, new List<EnemyTarget> { enemy });

            Assert.True(_bridge.IsPedCombatingPed(bg));
            Assert.Equal(777, _bridge.GetCombatPedTarget(bg));
        }

        [Fact]
        public void Update_SearchAndDestroy_NoEnemies_FallsBackToSeek()
        {
            _controller = Build();
            int bg = _bridge.CreatePed("bg", new Vector3(1f, 0f, 0f));
            var party = new List<int> { bg };
            _controller.CycleStance(party); // HoldArea
            _controller.CycleStance(party); // SearchAndDestroy

            _controller.Update(Anchor, 50f, party, new List<EnemyTarget>());

            Assert.True(_bridge.IsPedCombatTargeting(bg)); // TaskCombatHatedTargetsAroundPed recorded
        }

        [Fact]
        public void Update_SearchAndDestroy_RetargetsWhenAssignmentChanges()
        {
            _controller = Build();
            int bg = _bridge.CreatePed("bg", new Vector3(1f, 0f, 0f));
            var party = new List<int> { bg };
            _controller.CycleStance(party);
            _controller.CycleStance(party); // SearchAndDestroy

            _controller.Update(Anchor, 50f, party, new List<EnemyTarget> { new EnemyTarget(100, new Vector3(5f, 0f, 0f)) });
            Assert.Equal(100, _bridge.GetCombatPedTarget(bg));

            // Previous target "dies"; a new enemy is the only one left.
            _controller.Update(Anchor, 50f, party, new List<EnemyTarget> { new EnemyTarget(200, new Vector3(5f, 0f, 0f)) });
            Assert.Equal(200, _bridge.GetCombatPedTarget(bg));
        }
    }
}
```

> `IsPedCombatTargeting` is the existing `MockGameBridge` recorder for `TaskCombatHatedTargetsAroundPed`. If its name differs, use the file's actual recorder.

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~SquadStanceControllerTests"`
Expected: FAIL — `SquadStanceController` not defined.

- [ ] **Step 3: Write the controller core**

`src/FactionWars/ScriptHookV/Managers/SquadStanceController.cs`:
```csharp
using System;
using System.Collections.Generic;
using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Models;
using FactionWars.Core.Interfaces;

namespace FactionWars.ScriptHookV.Managers
{
    /// <summary>
    /// Owns the party-wide squad stance and issues the matching per-bodyguard native tasks.
    /// Escort reproduces the on-foot follow repair; HoldArea anchors bodyguards on a ring;
    /// SearchAndDestroy assigns each bodyguard a known enemy (or seeks when none are tracked).
    /// </summary>
    public partial class SquadStanceController
    {
        private readonly IGameBridge _gameBridge;
        private readonly ISquadStanceResolver _stanceResolver;
        private readonly ITargetAssignmentResolver _assignmentResolver;

        private SquadStance _currentStance = SquadStance.Escort;
        private readonly Dictionary<int, AppliedOrder> _lastApplied = new Dictionary<int, AppliedOrder>();
        private readonly Dictionary<int, int> _lastFollowReassertMs = new Dictionary<int, int>();

        private const int FollowerReassertIntervalMs = 2000;
        private const float HoldRadiusPerBodyguard = 8f;

        public SquadStanceController(IGameBridge gameBridge, ISquadStanceResolver stanceResolver, ITargetAssignmentResolver assignmentResolver)
        {
            _gameBridge = gameBridge ?? throw new ArgumentNullException(nameof(gameBridge));
            _stanceResolver = stanceResolver ?? throw new ArgumentNullException(nameof(stanceResolver));
            _assignmentResolver = assignmentResolver ?? throw new ArgumentNullException(nameof(assignmentResolver));
        }

        public SquadStance CurrentStance => _currentStance;

        public void CycleStance(IReadOnlyList<int> onFootBodyguardHandles)
        {
            if (onFootBodyguardHandles == null || onFootBodyguardHandles.Count == 0)
            {
                return;
            }

            _currentStance = _currentStance.Next();
            _lastApplied.Clear();
            _gameBridge.ShowNotification($"~b~Bodyguards:~w~ {StanceLabel(_currentStance)}");
        }

        public void Update(Vector3 anchorCenter, float anchorRadius, IReadOnlyList<int> onFootBodyguardHandles, IReadOnlyList<EnemyTarget> enemiesInRange)
        {
            PruneStale(onFootBodyguardHandles);
            if (onFootBodyguardHandles == null || onFootBodyguardHandles.Count == 0)
            {
                return;
            }

            switch (_currentStance)
            {
                case SquadStance.HoldArea:
                    ApplyHoldArea(anchorCenter, anchorRadius, onFootBodyguardHandles);
                    break;
                case SquadStance.SearchAndDestroy:
                    ApplySearchAndDestroy(anchorCenter, anchorRadius, onFootBodyguardHandles, enemiesInRange);
                    break;
                default:
                    ApplyEscort(onFootBodyguardHandles);
                    break;
            }
        }

        private void PruneStale(IReadOnlyList<int> currentHandles)
        {
            var keep = currentHandles == null ? new HashSet<int>() : new HashSet<int>(currentHandles);
            foreach (var handle in new List<int>(_lastApplied.Keys))
            {
                if (!keep.Contains(handle)) _lastApplied.Remove(handle);
            }
            foreach (var handle in new List<int>(_lastFollowReassertMs.Keys))
            {
                if (!keep.Contains(handle)) _lastFollowReassertMs.Remove(handle);
            }
        }

        private static string StanceLabel(SquadStance stance)
        {
            switch (stance)
            {
                case SquadStance.HoldArea: return "Hold Area";
                case SquadStance.SearchAndDestroy: return "Search & Destroy";
                default: return "Escort";
            }
        }
    }
}
```

- [ ] **Step 4: Write the stance branches**

`src/FactionWars/ScriptHookV/Managers/SquadStanceController.Stances.cs`:
```csharp
using System.Collections.Generic;
using FactionWars.Combat.Models;
using FactionWars.Core.Interfaces;

namespace FactionWars.ScriptHookV.Managers
{
    public partial class SquadStanceController
    {
        private struct AppliedOrder
        {
            public SquadStance Stance;
            public BodyguardOrderKind Kind;
            public int Discriminator; // target handle for AttackTarget, ring index for HoldAtPoint, else 0
        }

        private bool AlreadyApplied(int handle, SquadStance stance, BodyguardOrderKind kind, int discriminator)
        {
            return _lastApplied.TryGetValue(handle, out var last)
                && last.Stance == stance && last.Kind == kind && last.Discriminator == discriminator;
        }

        private void Remember(int handle, SquadStance stance, BodyguardOrderKind kind, int discriminator)
        {
            _lastApplied[handle] = new AppliedOrder { Stance = stance, Kind = kind, Discriminator = discriminator };
        }

        private void ApplyEscort(IReadOnlyList<int> handles)
        {
            if (_gameBridge.IsPlayerDead()) return;
            int now = _gameBridge.GetGameTime();

            foreach (var pedHandle in handles)
            {
                if (_gameBridge.IsPedInVehicle(pedHandle))
                {
                    _gameBridge.TaskPedLeaveVehicle(pedHandle);
                    continue;
                }
                if (_gameBridge.IsPedFollowingPlayer(pedHandle))
                {
                    _lastFollowReassertMs.Remove(pedHandle);
                    continue;
                }
                if (_gameBridge.IsPedInCombat(pedHandle))
                {
                    continue;
                }
                if (_lastFollowReassertMs.TryGetValue(pedHandle, out var last) && now - last < FollowerReassertIntervalMs)
                {
                    continue;
                }

                _gameBridge.SetPedAsFollower(pedHandle);
                _lastFollowReassertMs[pedHandle] = now;
            }
        }

        private void ApplyHoldArea(Vector3 anchorCenter, float anchorRadius, IReadOnlyList<int> handles)
        {
            for (int i = 0; i < handles.Count; i++)
            {
                int pedHandle = handles[i];
                if (AlreadyApplied(pedHandle, SquadStance.HoldArea, BodyguardOrderKind.HoldAtPoint, i)) continue;

                var order = _stanceResolver.Resolve(SquadStance.HoldArea, anchorCenter, anchorRadius, i, handles.Count);
                _gameBridge.TaskGuardArea(pedHandle, order.Point, HoldRadiusPerBodyguard);
                Remember(pedHandle, SquadStance.HoldArea, BodyguardOrderKind.HoldAtPoint, i);
            }
        }

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

            var assignment = _assignmentResolver.Assign(bodyguards, enemies);
            foreach (var pedHandle in handles)
            {
                if (!assignment.TryGetValue(pedHandle, out var targetHandle)) continue;
                if (AlreadyApplied(pedHandle, SquadStance.SearchAndDestroy, BodyguardOrderKind.AttackTarget, targetHandle)) continue;

                _gameBridge.TaskCombatPed(pedHandle, targetHandle);
                Remember(pedHandle, SquadStance.SearchAndDestroy, BodyguardOrderKind.AttackTarget, targetHandle);
            }
        }

        private void SeekFallback(float anchorRadius, IReadOnlyList<int> handles)
        {
            foreach (var pedHandle in handles)
            {
                if (AlreadyApplied(pedHandle, SquadStance.SearchAndDestroy, BodyguardOrderKind.SeekInRadius, 0)) continue;
                _gameBridge.TaskCombatHatedTargetsAroundPed(pedHandle, anchorRadius);
                Remember(pedHandle, SquadStance.SearchAndDestroy, BodyguardOrderKind.SeekInRadius, 0);
            }
        }
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~SquadStanceControllerTests"`
Expected: PASS (all 6).

- [ ] **Step 6: Build the solution (analyzer/line-count check)**

Run: `dotnet build FactionWars.sln --no-incremental`
Expected: 0 Warnings, 0 Errors. (Both partial files stay under 250 lines; every method under 40.)

- [ ] **Step 7: Commit**

```bash
git add src/FactionWars/ScriptHookV/Managers/SquadStanceController.cs src/FactionWars/ScriptHookV/Managers/SquadStanceController.Stances.cs tests/FactionWars.Tests/Unit/ScriptHookV/Managers/SquadStanceControllerTests.cs
git commit -m "feat: add SquadStanceController (#35)"
```

---

### Task 11: Move on-foot tasking out of FollowerManager

**Files:**
- Modify: `src/FactionWars/ScriptHookV/Managers/FollowerManager.cs` (Update method + new property)
- Modify/Delete: `src/FactionWars/ScriptHookV/Managers/FollowerManager.OnFoot.cs` (remove relocated logic)
- Test: existing FollowerManager on-foot test file(s) under `tests/FactionWars.Tests/Unit/ScriptHookV/Managers/`

**Interfaces:**
- Consumes: `GetAliveFollowerHandles(IEnumerable<Follower>) -> List<int>`, `AssignFollowersToVehicle(List<int>, int)`, `IGameBridge.IsPlayerInVehicle()`, `IGameBridge.GetPlayerVehicle()`.
- Produces: `FollowerManager.OnFootBodyguardHandles` (`IReadOnlyList<int>`, auto-property, default empty) — the alive on-foot bodyguard handles after the most recent `Update`. The `SquadStanceController` now owns all on-foot follow tasking; `FollowerManager` keeps roster/death/vehicle only.

The current `UpdateOnFootFollowers` logic (the `SetPedAsFollower` repair with the `_lastFollowReassertMs` throttle) was reproduced in `SquadStanceController.ApplyEscort` in Task 10. This task removes the original so the two systems never both task on-foot bodyguards.

- [ ] **Step 1: Update the failing expectation first — adapt the on-foot test**

Find the FollowerManager test(s) that assert on-foot follow tasking (they call `_manager.Update(faction)` with the player on foot and assert `SetPedAsFollower` / `IsPedFollowingPlayer` behaviour via `MockGameBridge`). Replace each such test's assertion with the new contract: after `Update` with the player on foot, `OnFootBodyguardHandles` contains the alive follower handles and the manager does NOT call `SetPedAsFollower`. Example shape:
```csharp
        [Fact]
        public void Update_PlayerOnFoot_ExposesAliveHandlesWithoutTasking()
        {
            SetupManager();
            // ... arrange: recruit/spawn a follower so it has a valid alive ped handle,
            //     player NOT in vehicle (the MockGameBridge default) ...

            _manager.Update("michael");

            Assert.NotEmpty(_manager.OnFootBodyguardHandles);
            // No follow tasking issued by FollowerManager anymore:
            Assert.False(_gameBridge.IsPedFollowingPlayer(_manager.OnFootBodyguardHandles[0]));
        }
```
Delete tests that asserted the relocated throttle/`SetPedAsFollower` repair behaviour (that behaviour is now covered by `SquadStanceControllerTests`).

> Locate these by grepping the managers test folder for `IsPedFollowingPlayer`, `SetPedAsFollower`, and `UpdateOnFootFollowers` references within FollowerManager tests.

- [ ] **Step 2: Run the adapted test to verify it fails**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~FollowerManager"`
Expected: FAIL — `OnFootBodyguardHandles` not defined.

- [ ] **Step 3: Add the property and rewrite Update**

In `src/FactionWars/ScriptHookV/Managers/FollowerManager.cs`, add the property (near the other fields/properties); add `using System;` for `Array.Empty` if not present:
```csharp
        /// <summary>
        /// Alive on-foot bodyguard handles from the most recent <see cref="Update"/>. Empty when
        /// the player is in a vehicle. The squad stance controller owns their on-foot tasking.
        /// </summary>
        public IReadOnlyList<int> OnFootBodyguardHandles { get; private set; } = Array.Empty<int>();
```
Replace the body of `Update`:
```csharp
        public void Update(string factionId)
        {
            if (string.IsNullOrEmpty(factionId))
            {
                OnFootBodyguardHandles = Array.Empty<int>();
                return;
            }

            var followers = _followerService.GetFollowers(factionId);
            var playerInVehicle = _gameBridge.IsPlayerInVehicle();
            var playerVehicle = playerInVehicle ? _gameBridge.GetPlayerVehicle() : -1;
            var aliveFollowerHandles = GetAliveFollowerHandles(followers);

            if (playerInVehicle && playerVehicle >= 0)
            {
                AssignFollowersToVehicle(aliveFollowerHandles, playerVehicle);
                OnFootBodyguardHandles = Array.Empty<int>();
            }
            else
            {
                OnFootBodyguardHandles = aliveFollowerHandles;
            }
        }
```

- [ ] **Step 4: Remove the relocated on-foot logic**

Delete `src/FactionWars/ScriptHookV/Managers/FollowerManager.OnFoot.cs` entirely (it contained `UpdateOnFootFollowers`, `_lastFollowReassertMs`, and `FollowerReassertIntervalMs`, all now living in `SquadStanceController`). Remove any remaining reference to `UpdateOnFootFollowers` in `FollowerManager.cs`.
```bash
git rm src/FactionWars/ScriptHookV/Managers/FollowerManager.OnFoot.cs
```

- [ ] **Step 5: Build + run FollowerManager tests**

Run: `dotnet build FactionWars.sln --no-incremental`
Expected: 0 Warnings, 0 Errors. (If a compile error references `UpdateOnFootFollowers`, remove that call.)
Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~FollowerManager"`
Expected: PASS

- [ ] **Step 6: Commit**

```bash
git add -A src/FactionWars/ScriptHookV/Managers/FollowerManager.cs tests/FactionWars.Tests/Unit/ScriptHookV/Managers
git commit -m "refactor: FollowerManager exposes on-foot handles, drops on-foot tasking (#35)"
```

---

### Task 12: Wire the controller into GameLoopController

**Files:**
- Modify: `src/FactionWars/ScriptHookV/GameLoopController.cs` (fields + control const)
- Modify: `src/FactionWars/ScriptHookV/GameLoopController.Lifecycle.cs` (input poll)
- Modify: `src/FactionWars/ScriptHookV/GameLoopController.SystemUpdates.cs` (per-tick update + helpers)
- Modify: `src/FactionWars/ScriptHookV/GameLoopController.Initialization.cs` (composition)

**Interfaces:**
- Consumes: `SquadStanceController`, `EnemyTargetCollector`, `AreaAnchorResolver`, `SquadStanceResolver`, `TargetAssignmentResolver`, `_followerManager.OnFootBodyguardHandles`, `_territoryManager.CurrentZone`, `_enemyDefenderManager`/`_battleAttackerManager` as `IHostilePedHandleSource`, `IGameBridge.GetPlayerPosition()`, `IGameBridge.IsControlJustPressed(int)`.

This wiring is not independently unit-tested (`GameLoopController` is the native composition root). The behaviour it composes is fully covered by Tasks 4–11. Verify via build + the manual in-game smoke test at the end.

- [ ] **Step 1: Add fields and the D-pad Up constant**

In `src/FactionWars/ScriptHookV/GameLoopController.cs`, add to the control constants block (alongside `ControlDpadDown = 173`):
```csharp
        private const int ControlDpadUp = 172;       // INPUT_PHONE_UP
        private const float SquadDefaultLooseRadius = 30f;
```
Add fields (near `_followerManager`):
```csharp
        private SquadStanceController? _squadStanceController;
        private IEnemyTargetCollector? _enemyTargetCollector;
        private IAreaAnchorResolver? _areaAnchorResolver;
```
Add `using` directives at the top of the file as needed: `using FactionWars.Combat.Interfaces;`, `using FactionWars.Combat.Models;`, `using FactionWars.Combat.Services;`, `using FactionWars.ScriptHookV.Combat;`, `using FactionWars.ScriptHookV.Combat.Interfaces;`.

- [ ] **Step 2: Compose the collaborators**

In `src/FactionWars/ScriptHookV/GameLoopController.Initialization.cs`, after the followers/enemy/battle managers are constructed, add:
```csharp
            _areaAnchorResolver = new AreaAnchorResolver();
            _enemyTargetCollector = new EnemyTargetCollector(_gameBridge);
            _squadStanceController = new SquadStanceController(
                _gameBridge,
                new SquadStanceResolver(),
                new TargetAssignmentResolver());
```

> Match the exact field name used for the game bridge in `GameLoopController` (`_gameBridge`). If composition happens inside a helper that returns early when `_gameBridge`/managers are null, place these lines after those guards so the collaborators are only built once initialization succeeds.

- [ ] **Step 3: Wire the input — D-pad Up cycles the stance**

In `src/FactionWars/ScriptHookV/GameLoopController.Lifecycle.cs`, inside `PollControllerInput()` (after the D-pad Down battle-HUD block), add:
```csharp
            // D-pad Up = Cycle bodyguard squad stance
            if (_gameBridge.IsControlJustPressed(ControlDpadUp))
            {
                _squadStanceController?.CycleStance(_followerManager?.OnFootBodyguardHandles ?? System.Array.Empty<int>());
            }
```

- [ ] **Step 4: Wire the per-tick update**

In `src/FactionWars/ScriptHookV/GameLoopController.SystemUpdates.cs`, in `UpdateWorldSystems`, immediately after the `_followerManager?.Update(CurrentPlayerFactionId ?? "");` line, add:
```csharp
            UpdateSquadStance();
```
Then add these private helpers in the same partial:
```csharp
        private void UpdateSquadStance()
        {
            if (_squadStanceController == null || _followerManager == null) return;

            var handles = _followerManager.OnFootBodyguardHandles;
            var anchor = ResolveSquadAnchor();
            IReadOnlyList<EnemyTarget> enemies = System.Array.Empty<EnemyTarget>();

            if (_squadStanceController.CurrentStance == SquadStance.SearchAndDestroy && handles.Count > 0)
            {
                enemies = _enemyTargetCollector!.Collect(GatherHostileHandles(), anchor.Center, anchor.Radius);
            }

            _squadStanceController.Update(anchor.Center, anchor.Radius, handles, enemies);
        }

        private AreaAnchor ResolveSquadAnchor()
        {
            var zone = _territoryManager?.CurrentZone;
            Vector3? zoneCenter = zone != null ? zone.Center : (Vector3?)null;
            float zoneRadius = zone?.Radius ?? 0f;
            return _areaAnchorResolver!.Resolve(zoneCenter, zoneRadius, _gameBridge.GetPlayerPosition(), SquadDefaultLooseRadius);
        }

        private IReadOnlyList<int> GatherHostileHandles()
        {
            var handles = new List<int>();
            if (_enemyDefenderManager != null) handles.AddRange(_enemyDefenderManager.GetHostilePedHandles());
            if (_battleAttackerManager != null) handles.AddRange(_battleAttackerManager.GetHostilePedHandles());
            return handles;
        }
```
Add `using System.Collections.Generic;`, `using FactionWars.Combat.Models;`, and `using FactionWars.Core.Interfaces;` to this partial if not present.

- [ ] **Step 5: Build the solution**

Run: `dotnet build FactionWars.sln --no-incremental`
Expected: 0 Warnings, 0 Errors.

- [ ] **Step 6: Run the full unit suite**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~FactionWars.Tests.Unit"`
Expected: PASS (all tests, including the new ones).

- [ ] **Step 7: Commit**

```bash
git add src/FactionWars/ScriptHookV/GameLoopController.cs src/FactionWars/ScriptHookV/GameLoopController.Lifecycle.cs src/FactionWars/ScriptHookV/GameLoopController.SystemUpdates.cs src/FactionWars/ScriptHookV/GameLoopController.Initialization.cs
git commit -m "feat: wire squad stance cycling on D-pad Up (#35)"
```

- [ ] **Step 8: Deploy + manual in-game smoke test**

```bash
cp "src/FactionWars/bin/Debug/net48/FactionWars.dll" "E:/SteamLibrary/steamapps/common/Grand Theft Auto V/scripts/"
```
In game (reload scripts with Insert or relaunch): recruit ≥2 bodyguards, then press **D-pad Up** repeatedly. Confirm notifications cycle Escort → Hold Area → Search & Destroy → Escort, that Hold Area fans bodyguards out and holds, and Search & Destroy sends them at known zone enemies. Read the newest log in `C:\Users\ryan7\Documents\FactionWars\Logs\` and confirm `TaskGuardArea` / `TaskCombatPed` / `TaskCombatHatedTargetsAroundPed` fire as expected; adjust the `TaskGuardArea` native params (Task 7 note) if peds wander off the hold point.

---

## Self-Review

**1. Spec coverage:**
- D-pad Up cycles Escort→HoldArea→S&D→Escort, notification per press, ignored when party empty → Tasks 1, 10 (`CycleStance`), 12 (input). ✓
- Escort = relocated on-foot follow → Tasks 10 (`ApplyEscort`), 11 (removal). ✓
- Hold Area = fan out + hold + take cover → Tasks 4 (ring), 7 (`TaskGuardArea`), 10 (`ApplyHoldArea`). ✓
- Search & Destroy = assignment to known enemies + seek fallback → Tasks 5 (`TargetAssignmentResolver`), 7 (`TaskCombatPed`), 8 (handle sources), 9 (collector), 10 (`ApplySearchAndDestroy`/`SeekFallback`). ✓
- Area anchor (own/enemy zone any-owner, else player+default) → Tasks 6, 12 (`ResolveSquadAnchor`). ✓
- Vehicles: stance irrelevant, embark/disembark unchanged → Task 11 (`OnFootBodyguardHandles` empty in vehicle → controller no-ops). ✓
- Default Escort, not persisted → Task 10 (`_currentStance = Escort` field initializer; never saved). ✓
- Task-spam avoidance via last-applied → Task 10 (`AppliedOrder`, `AlreadyApplied`). ✓
- New natives `TaskGuardArea` + `TaskCombatPed` with logging + mock → Task 7. ✓
- Testing: resolver/assignment/anchor/controller/mock tests → Tasks 4–10. ✓

**2. Placeholder scan:** No "TBD"/"TODO"/"handle edge cases" left; every code step shows complete code. The two soft notes (TASK_GUARD_SPHERE param order; copying exact arrange blocks in manager tests) are explicit verification instructions, not deferred work. ✓

**3. Type consistency:** `SquadStance`, `BodyguardOrder(Kind/Point/Radius/TargetHandle)`, `BodyguardPosition(Handle/Position)`, `EnemyTarget(Handle/Position)`, `AreaAnchor(Center/Radius)`, `ISquadStanceResolver.Resolve(...)`, `ITargetAssignmentResolver.Assign(...)`, `IAreaAnchorResolver.Resolve(...)`, `IEnemyTargetCollector.Collect(...)`, `IHostilePedHandleSource.GetHostilePedHandles()`, `SquadStanceController.CycleStance/Update/CurrentStance`, `FollowerManager.OnFootBodyguardHandles`, `IGameBridge.TaskGuardArea/TaskCombatPed` — names and signatures are identical across producing and consuming tasks. ✓
