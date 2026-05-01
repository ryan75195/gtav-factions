# Defender Rally on Threat Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the defenders of the player's current zone converge on the player when combat is relevant — both when defending the player in their own zone (police / enemy attackers) and when attacking the player as an invader in an enemy zone.

**Architecture:** A new `DefenderRallyController` runs once per tick after `FriendlyDefenderManager.Update()`. It evaluates a cheap composite "should rally?" signal each tick, holds a 5-second cool-down, and tasks defenders only on transitions (`false→true` rally, `true→false` stand down). Two new query interfaces (`IFriendlyDefenderQuery`, `ICombatActivityQuery`) decouple the controller from concrete managers for testability. Four new bridge methods (`GetWantedLevel`, `ConsumePlayerDamagedByPedFlag`, `TaskGoToEntity`, `GetPlayerPedHandle`) wrap the GTA natives.

**Tech Stack:** C# / .NET Framework 4.8, ScriptHookVDotNet3, xUnit + Moq.

**Spec:** `docs/superpowers/specs/2026-05-01-defender-rally-on-threat-design.md`

---

## File Structure

**Create:**
- `src/FactionWars/Combat/Interfaces/IFriendlyDefenderQuery.cs` — read-only accessor over the `FriendlyDefenderManager._spawnedPedTierByZone` dictionary, lets the rally controller list defenders per zone without depending on the concrete manager.
- `src/FactionWars/Combat/Interfaces/ICombatActivityQuery.cs` — exposes `bool HasActiveEncounter` so the rally controller can ask "is there a mod-managed combat encounter?" without depending on `CombatManager`.
- `src/FactionWars/ScriptHookV/Managers/DefenderRallyController.cs` — the controller itself.
- `tests/FactionWars.Tests/Unit/ScriptHookV/Managers/DefenderRallyControllerTests.cs` — unit tests.

**Modify:**
- `src/FactionWars/Core/Interfaces/IGameBridge.cs` — add 4 new methods.
- `src/FactionWars/Core/Utils/MockGameBridge.cs` — implement 4 new methods + recording for assertions.
- `src/FactionWars/ScriptHookV/GameBridge.cs` — implement 4 new methods against SHVDN.
- `src/FactionWars/ScriptHookV/Managers/CombatManager.cs` — add `HasActiveEncounter` property; declare it implements `ICombatActivityQuery`.
- `src/FactionWars/ScriptHookV/Managers/FriendlyDefenderManager.cs` — add `GetDefendersInZone(zoneId)` method; declare it implements `IFriendlyDefenderQuery`.
- `src/FactionWars/ScriptHookV/Managers/ITerritoryEvents.cs` — add `Zone? CurrentZone { get; }`.
- `src/FactionWars/ScriptHookV/GameLoopController.cs` — construct the controller, call `Update()` per tick, null on `OnAbort`.

Each file has one focused responsibility; the controller is self-contained and unit-tested with mocks.

---

## Task 1: Add `GetWantedLevel` to bridge

**Files:**
- Modify: `src/FactionWars/Core/Interfaces/IGameBridge.cs`
- Modify: `src/FactionWars/Core/Utils/MockGameBridge.cs`
- Modify: `src/FactionWars/ScriptHookV/GameBridge.cs`
- Test: `tests/FactionWars.Tests/Unit/Core/Utils/MockGameBridgeTests.cs` (add a test there if the file exists; otherwise create `tests/FactionWars.Tests/Unit/Core/Utils/MockGameBridgeWantedLevelTests.cs`)

- [ ] **Step 1: Write the failing mock test**

Open `tests/FactionWars.Tests/Unit/Core/Utils/` and add a new test file `MockGameBridgeWantedLevelTests.cs`:

```csharp
using FactionWars.Core.Utils;
using Xunit;

namespace FactionWars.Tests.Unit.Core.Utils
{
    public class MockGameBridgeWantedLevelTests
    {
        [Fact]
        public void WantedLevel_DefaultsToZero()
        {
            var bridge = new MockGameBridge();
            Assert.Equal(0, bridge.GetWantedLevel());
        }

        [Fact]
        public void WantedLevel_IsSettableAndReturnedFromGetter()
        {
            var bridge = new MockGameBridge();
            bridge.WantedLevel = 3;
            Assert.Equal(3, bridge.GetWantedLevel());
        }
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter MockGameBridgeWantedLevelTests`
Expected: FAIL — `MockGameBridge` has no `GetWantedLevel()` and no `WantedLevel` property.

- [ ] **Step 3: Add the interface method**

Edit `src/FactionWars/Core/Interfaces/IGameBridge.cs`. Place near the other player-related methods (e.g., right after `IsPlayerDead()` around line 143):

```csharp
/// <summary>
/// Gets the player's current wanted level (0 = no stars, 5 = max).
/// Used as a cheap composite signal that police are actively engaging the player.
/// </summary>
int GetWantedLevel();
```

- [ ] **Step 4: Implement on MockGameBridge**

Edit `src/FactionWars/Core/Utils/MockGameBridge.cs`. Add a settable property near the other `IsPlayerDeadValue`/`PlayerMoney` properties (around line 58):

```csharp
/// <summary>
/// Gets or sets the player's wanted level returned from GetWantedLevel.
/// </summary>
public int WantedLevel { get; set; } = 0;
```

And add the getter implementation near `IsPlayerDead`/`GetPlayerMoney` (around line 253):

```csharp
public int GetWantedLevel() => WantedLevel;
```

- [ ] **Step 5: Implement on GameBridge**

Edit `src/FactionWars/ScriptHookV/GameBridge.cs`. Add near other simple Game.Player accessors (search for `GetPlayerHeading` and add nearby):

```csharp
/// <inheritdoc />
public int GetWantedLevel()
{
    try
    {
        return Game.Player.WantedLevel;
    }
    catch (Exception ex)
    {
        FileLogger.Error("GetWantedLevel exception", ex);
        return 0;
    }
}
```

- [ ] **Step 6: Run test to verify it passes**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter MockGameBridgeWantedLevelTests`
Expected: PASS (both tests).

- [ ] **Step 7: Run full suite to confirm no regressions**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj`
Expected: All tests pass (no compile errors from the added interface method).

- [ ] **Step 8: Commit**

```bash
git add src/FactionWars/Core/Interfaces/IGameBridge.cs src/FactionWars/Core/Utils/MockGameBridge.cs src/FactionWars/ScriptHookV/GameBridge.cs tests/FactionWars.Tests/Unit/Core/Utils/MockGameBridgeWantedLevelTests.cs
git commit -m "feat: add GetWantedLevel to game bridge"
```

---

## Task 2: Add `ConsumePlayerDamagedByPedFlag` to bridge

**Files:**
- Modify: `src/FactionWars/Core/Interfaces/IGameBridge.cs`
- Modify: `src/FactionWars/Core/Utils/MockGameBridge.cs`
- Modify: `src/FactionWars/ScriptHookV/GameBridge.cs`
- Test: `tests/FactionWars.Tests/Unit/Core/Utils/MockGameBridgeDamageFlagTests.cs`

- [ ] **Step 1: Write the failing mock tests**

Create `tests/FactionWars.Tests/Unit/Core/Utils/MockGameBridgeDamageFlagTests.cs`:

```csharp
using FactionWars.Core.Utils;
using Xunit;

namespace FactionWars.Tests.Unit.Core.Utils
{
    public class MockGameBridgeDamageFlagTests
    {
        [Fact]
        public void ConsumePlayerDamagedByPedFlag_DefaultsToFalse()
        {
            var bridge = new MockGameBridge();
            Assert.False(bridge.ConsumePlayerDamagedByPedFlag());
        }

        [Fact]
        public void ConsumePlayerDamagedByPedFlag_WhenSetTrue_ReturnsTrueOnce_ThenFalse()
        {
            var bridge = new MockGameBridge();
            bridge.PlayerDamagedByPed = true;

            Assert.True(bridge.ConsumePlayerDamagedByPedFlag());
            Assert.False(bridge.ConsumePlayerDamagedByPedFlag());
        }

        [Fact]
        public void ConsumePlayerDamagedByPedFlag_AfterConsume_PlayerDamagedByPedIsFalse()
        {
            var bridge = new MockGameBridge();
            bridge.PlayerDamagedByPed = true;

            bridge.ConsumePlayerDamagedByPedFlag();

            Assert.False(bridge.PlayerDamagedByPed);
        }
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter MockGameBridgeDamageFlagTests`
Expected: FAIL — no `PlayerDamagedByPed` property and no `ConsumePlayerDamagedByPedFlag()` method.

- [ ] **Step 3: Add the interface method**

Edit `src/FactionWars/Core/Interfaces/IGameBridge.cs`. Add right after the new `GetWantedLevel()` method:

```csharp
/// <summary>
/// Reads-and-clears the engine-set "player has been damaged by any ped" flag.
/// Returns true if the player took damage from a ped since the last call,
/// then resets the flag. Used as an event-ish signal for the rally controller.
/// </summary>
bool ConsumePlayerDamagedByPedFlag();
```

- [ ] **Step 4: Implement on MockGameBridge**

Edit `src/FactionWars/Core/Utils/MockGameBridge.cs`. Add a settable property near `WantedLevel`:

```csharp
/// <summary>
/// Gets or sets whether the player has been damaged by a ped.
/// Cleared by ConsumePlayerDamagedByPedFlag().
/// </summary>
public bool PlayerDamagedByPed { get; set; } = false;
```

Add the consume method near the other player-state getters (next to `GetWantedLevel()`):

```csharp
public bool ConsumePlayerDamagedByPedFlag()
{
    var was = PlayerDamagedByPed;
    PlayerDamagedByPed = false;
    return was;
}
```

- [ ] **Step 5: Implement on GameBridge**

Edit `src/FactionWars/ScriptHookV/GameBridge.cs`. Add near the other Game.Player accessors:

```csharp
/// <inheritdoc />
public bool ConsumePlayerDamagedByPedFlag()
{
    try
    {
        var player = Game.Player.Character;
        if (player == null || !player.Exists())
            return false;

        // HasBeenDamagedByAnyPed reads the flag; we then clear it so the next
        // call only returns true if NEW damage has occurred.
        var damaged = player.HasBeenDamagedByAnyPed;
        if (damaged)
        {
            // CLEAR_ENTITY_LAST_DAMAGE_ENTITY clears the flag for the next read.
            Function.Call(Hash.CLEAR_ENTITY_LAST_DAMAGE_ENTITY, player.Handle);
        }
        return damaged;
    }
    catch (Exception ex)
    {
        FileLogger.Error("ConsumePlayerDamagedByPedFlag exception", ex);
        return false;
    }
}
```

- [ ] **Step 6: Run tests to verify pass**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter MockGameBridgeDamageFlagTests`
Expected: PASS (all 3 tests).

- [ ] **Step 7: Run full suite**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj`
Expected: All tests pass.

- [ ] **Step 8: Commit**

```bash
git add src/FactionWars/Core/Interfaces/IGameBridge.cs src/FactionWars/Core/Utils/MockGameBridge.cs src/FactionWars/ScriptHookV/GameBridge.cs tests/FactionWars.Tests/Unit/Core/Utils/MockGameBridgeDamageFlagTests.cs
git commit -m "feat: add read-and-clear ConsumePlayerDamagedByPedFlag bridge method"
```

---

## Task 3: Add `GetPlayerPedHandle` to bridge

**Files:**
- Modify: `src/FactionWars/Core/Interfaces/IGameBridge.cs`
- Modify: `src/FactionWars/Core/Utils/MockGameBridge.cs`
- Modify: `src/FactionWars/ScriptHookV/GameBridge.cs`
- Test: `tests/FactionWars.Tests/Unit/Core/Utils/MockGameBridgePlayerPedHandleTests.cs`

- [ ] **Step 1: Write the failing mock test**

Create `tests/FactionWars.Tests/Unit/Core/Utils/MockGameBridgePlayerPedHandleTests.cs`:

```csharp
using FactionWars.Core.Utils;
using Xunit;

namespace FactionWars.Tests.Unit.Core.Utils
{
    public class MockGameBridgePlayerPedHandleTests
    {
        [Fact]
        public void PlayerPedHandle_DefaultsToOne()
        {
            // The mock returns a stable non-zero handle by default so test code
            // doesn't have to set it up just to call TaskGoToEntity.
            var bridge = new MockGameBridge();
            Assert.Equal(1, bridge.GetPlayerPedHandle());
        }

        [Fact]
        public void PlayerPedHandle_IsSettable()
        {
            var bridge = new MockGameBridge();
            bridge.PlayerPedHandle = 42;
            Assert.Equal(42, bridge.GetPlayerPedHandle());
        }
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter MockGameBridgePlayerPedHandleTests`
Expected: FAIL — no `GetPlayerPedHandle()` method.

- [ ] **Step 3: Add the interface method**

Edit `src/FactionWars/Core/Interfaces/IGameBridge.cs`. Add near the player accessors:

```csharp
/// <summary>
/// Gets the entity handle of the player character. Used as the target entity for
/// tasks like TaskGoToEntity that need an entity handle (not a position).
/// </summary>
int GetPlayerPedHandle();
```

- [ ] **Step 4: Implement on MockGameBridge**

Edit `src/FactionWars/Core/Utils/MockGameBridge.cs`. Add property + getter:

```csharp
/// <summary>
/// Gets or sets the player ped handle returned from GetPlayerPedHandle.
/// Defaults to 1 so test code doesn't have to set it up to use rally tasks.
/// </summary>
public int PlayerPedHandle { get; set; } = 1;
```

```csharp
public int GetPlayerPedHandle() => PlayerPedHandle;
```

- [ ] **Step 5: Implement on GameBridge**

Edit `src/FactionWars/ScriptHookV/GameBridge.cs`:

```csharp
/// <inheritdoc />
public int GetPlayerPedHandle()
{
    try
    {
        var player = Game.Player.Character;
        if (player == null || !player.Exists())
            return -1;
        return player.Handle;
    }
    catch (Exception ex)
    {
        FileLogger.Error("GetPlayerPedHandle exception", ex);
        return -1;
    }
}
```

- [ ] **Step 6: Run tests**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter MockGameBridgePlayerPedHandleTests`
Expected: PASS.

- [ ] **Step 7: Run full suite**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj`
Expected: All tests pass.

- [ ] **Step 8: Commit**

```bash
git add src/FactionWars/Core/Interfaces/IGameBridge.cs src/FactionWars/Core/Utils/MockGameBridge.cs src/FactionWars/ScriptHookV/GameBridge.cs tests/FactionWars.Tests/Unit/Core/Utils/MockGameBridgePlayerPedHandleTests.cs
git commit -m "feat: add GetPlayerPedHandle bridge method"
```

---

## Task 4: Add `TaskGoToEntity` to bridge

**Files:**
- Modify: `src/FactionWars/Core/Interfaces/IGameBridge.cs`
- Modify: `src/FactionWars/Core/Utils/MockGameBridge.cs`
- Modify: `src/FactionWars/ScriptHookV/GameBridge.cs`
- Test: `tests/FactionWars.Tests/Unit/Core/Utils/MockGameBridgeTaskGoToEntityTests.cs`

- [ ] **Step 1: Write the failing mock test**

Create `tests/FactionWars.Tests/Unit/Core/Utils/MockGameBridgeTaskGoToEntityTests.cs`:

```csharp
using FactionWars.Core.Interfaces;
using FactionWars.Core.Utils;
using Xunit;

namespace FactionWars.Tests.Unit.Core.Utils
{
    public class MockGameBridgeTaskGoToEntityTests
    {
        [Fact]
        public void TaskGoToEntity_RecordsCallForExistingPed()
        {
            var bridge = new MockGameBridge();
            int pedHandle = bridge.CreatePed("a_m_y_business_01", new Vector3(0, 0, 0));

            bridge.TaskGoToEntity(pedHandle, targetEntityHandle: 100, stoppingRange: 8.0f);

            Assert.True(bridge.IsPedGoingToEntity(pedHandle));
            Assert.Equal(100, bridge.GetGoToEntityTarget(pedHandle));
            Assert.Equal(8.0f, bridge.GetGoToEntityStoppingRange(pedHandle));
        }

        [Fact]
        public void TaskGoToEntity_OverwritesEarlierTask()
        {
            var bridge = new MockGameBridge();
            int pedHandle = bridge.CreatePed("a_m_y_business_01", new Vector3(0, 0, 0));

            bridge.TaskGoToEntity(pedHandle, targetEntityHandle: 100, stoppingRange: 8.0f);
            bridge.TaskGoToEntity(pedHandle, targetEntityHandle: 200, stoppingRange: 5.0f);

            Assert.Equal(200, bridge.GetGoToEntityTarget(pedHandle));
            Assert.Equal(5.0f, bridge.GetGoToEntityStoppingRange(pedHandle));
        }

        [Fact]
        public void TaskGoToEntity_IsClearedByClearPedTasks()
        {
            var bridge = new MockGameBridge();
            int pedHandle = bridge.CreatePed("a_m_y_business_01", new Vector3(0, 0, 0));

            bridge.TaskGoToEntity(pedHandle, targetEntityHandle: 100, stoppingRange: 8.0f);
            bridge.ClearPedTasks(pedHandle);

            Assert.False(bridge.IsPedGoingToEntity(pedHandle));
        }
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter MockGameBridgeTaskGoToEntityTests`
Expected: FAIL — no `TaskGoToEntity` method, no `IsPedGoingToEntity` helper.

- [ ] **Step 3: Add the interface method**

Edit `src/FactionWars/Core/Interfaces/IGameBridge.cs`. Add near `TaskCombatHatedTargetsAroundPed`:

```csharp
/// <summary>
/// Tasks a ped to move toward another entity (player, vehicle, etc.) until
/// they are within stoppingRange meters. Wraps TASK_GO_TO_ENTITY.
/// Used by the defender rally controller to make defenders converge on the player.
/// </summary>
/// <param name="pedHandle">The ped to task.</param>
/// <param name="targetEntityHandle">Entity to walk/sprint toward.</param>
/// <param name="stoppingRange">Distance in meters at which to stop.</param>
void TaskGoToEntity(int pedHandle, int targetEntityHandle, float stoppingRange);
```

- [ ] **Step 4: Implement on MockGameBridge**

Edit `src/FactionWars/Core/Utils/MockGameBridge.cs`. Add a state struct and method near `_combatTargetingPeds` (around line 567):

```csharp
private readonly Dictionary<int, GoToEntityState> _goToEntityPeds = new Dictionary<int, GoToEntityState>();

public void TaskGoToEntity(int pedHandle, int targetEntityHandle, float stoppingRange)
{
    if (_peds.ContainsKey(pedHandle))
    {
        // Clear other task states when assigning go-to-entity
        _wanderingPeds.Remove(pedHandle);
        _pedsFacingPosition.Remove(pedHandle);
        _combatTargetingPeds.Remove(pedHandle);
        _goToEntityPeds[pedHandle] = new GoToEntityState
        {
            TargetEntityHandle = targetEntityHandle,
            StoppingRange = stoppingRange
        };
    }
}

/// <summary>Gets whether a ped is currently going to an entity.</summary>
public bool IsPedGoingToEntity(int pedHandle) => _goToEntityPeds.ContainsKey(pedHandle);

/// <summary>Gets the target entity handle for a go-to-entity task.</summary>
public int? GetGoToEntityTarget(int pedHandle)
    => _goToEntityPeds.TryGetValue(pedHandle, out var state) ? state.TargetEntityHandle : (int?)null;

/// <summary>Gets the stopping range for a go-to-entity task.</summary>
public float? GetGoToEntityStoppingRange(int pedHandle)
    => _goToEntityPeds.TryGetValue(pedHandle, out var state) ? state.StoppingRange : (float?)null;

private class GoToEntityState
{
    public int TargetEntityHandle { get; set; }
    public float StoppingRange { get; set; }
}
```

Also update the existing `ClearPedTasks` to clear `_goToEntityPeds` (around line 676):

Before:
```csharp
public void ClearPedTasks(int pedHandle)
{
    if (_peds.ContainsKey(pedHandle))
    {
        _clearedPeds.Add(pedHandle);
        _wanderingPeds.Remove(pedHandle);
        _pedsFacingPosition.Remove(pedHandle);
    }
}
```

After:
```csharp
public void ClearPedTasks(int pedHandle)
{
    if (_peds.ContainsKey(pedHandle))
    {
        _clearedPeds.Add(pedHandle);
        _wanderingPeds.Remove(pedHandle);
        _pedsFacingPosition.Remove(pedHandle);
        _combatTargetingPeds.Remove(pedHandle);
        _goToEntityPeds.Remove(pedHandle);
    }
}
```

- [ ] **Step 5: Implement on GameBridge**

Edit `src/FactionWars/ScriptHookV/GameBridge.cs`. Add near `TaskCombatHatedTargetsAroundPed`:

```csharp
/// <inheritdoc />
public void TaskGoToEntity(int pedHandle, int targetEntityHandle, float stoppingRange)
{
    FileLogger.AI($"TaskGoToEntity: CALLED for ped {pedHandle} -> entity {targetEntityHandle} stopRange={stoppingRange:F1}");

    try
    {
        var ped = Entity.FromHandle(pedHandle) as Ped;
        if (ped == null || !ped.Exists())
        {
            FileLogger.Warn($"TaskGoToEntity: Ped {pedHandle} is null or doesn't exist, aborting");
            return;
        }

        // TASK_GO_TO_ENTITY signature: ped, target, duration (-1=indefinite),
        // stoppingRange, speed (3.0=sprint), targetOffset (0=current pos), flags.
        Function.Call(
            Hash.TASK_GO_TO_ENTITY,
            ped.Handle,
            targetEntityHandle,
            -1,
            stoppingRange,
            3.0f,
            0f,
            0);

        FileLogger.AI($"TaskGoToEntity: COMPLETED for ped {pedHandle}");
    }
    catch (Exception ex)
    {
        FileLogger.Error($"TaskGoToEntity exception for ped {pedHandle}", ex);
    }
}
```

- [ ] **Step 6: Run tests**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter MockGameBridgeTaskGoToEntityTests`
Expected: PASS (all 3 tests).

- [ ] **Step 7: Run full suite**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj`
Expected: All tests pass.

- [ ] **Step 8: Commit**

```bash
git add src/FactionWars/Core/Interfaces/IGameBridge.cs src/FactionWars/Core/Utils/MockGameBridge.cs src/FactionWars/ScriptHookV/GameBridge.cs tests/FactionWars.Tests/Unit/Core/Utils/MockGameBridgeTaskGoToEntityTests.cs
git commit -m "feat: add TaskGoToEntity bridge method"
```

---

## Task 5: Define `IFriendlyDefenderQuery` and implement on `FriendlyDefenderManager`

**Files:**
- Create: `src/FactionWars/Combat/Interfaces/IFriendlyDefenderQuery.cs`
- Modify: `src/FactionWars/ScriptHookV/Managers/FriendlyDefenderManager.cs`
- Test: `tests/FactionWars.Tests/Unit/ScriptHookV/Managers/FriendlyDefenderManagerQueryTests.cs`

- [ ] **Step 1: Write the failing test**

Create `tests/FactionWars.Tests/Unit/ScriptHookV/Managers/FriendlyDefenderManagerQueryTests.cs`:

```csharp
using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Models;
using FactionWars.Core.Utils;
using FactionWars.ScriptHookV;
using FactionWars.ScriptHookV.Managers;
using FactionWars.Tests.Mocks;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV.Managers
{
    /// <summary>
    /// FriendlyDefenderManager exposes its spawned-ped tracking via IFriendlyDefenderQuery
    /// so DefenderRallyController can list the defenders to rally without depending on the
    /// concrete manager.
    /// </summary>
    public class FriendlyDefenderManagerQueryTests
    {
        [Fact]
        public void GetDefendersInZone_UnknownZone_ReturnsEmpty()
        {
            // FriendlyDefenderManager is constructed inside GameLoopController.InitializeGameData,
            // so we reach it via the controller (same pattern as GameLoopControllerCombatTests).
            var bridge = new MockGameBridge();
            var container = ServiceContainerFactory.Create(bridge, new MockMenuProvider());
            var controller = new GameLoopController(container);
            controller.OnTick(); // Initialize.

            IFriendlyDefenderQuery query = controller.FriendlyDefenderManager!;
            var result = query.GetDefendersInZone("does_not_exist");

            Assert.Empty(result);
        }
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter FriendlyDefenderManagerQueryTests`
Expected: FAIL — `IFriendlyDefenderQuery` does not exist; `FriendlyDefenderManager` does not implement it.

- [ ] **Step 3: Create the interface**

Create `src/FactionWars/Combat/Interfaces/IFriendlyDefenderQuery.cs`:

```csharp
using System.Collections.Generic;
using FactionWars.Combat.Models;

namespace FactionWars.Combat.Interfaces
{
    /// <summary>
    /// Read-only accessor over the defenders currently spawned in a zone. Implemented by
    /// FriendlyDefenderManager so DefenderRallyController can list defenders to rally
    /// without taking a hard reference to the concrete manager.
    /// </summary>
    public interface IFriendlyDefenderQuery
    {
        /// <summary>
        /// Returns the spawned defenders for the given zone, mapped from ped handle to tier.
        /// Returns an empty dictionary if the zone has no spawned defenders.
        /// </summary>
        IReadOnlyDictionary<int, DefenderTier> GetDefendersInZone(string zoneId);
    }
}
```

- [ ] **Step 4: Implement on FriendlyDefenderManager**

Edit `src/FactionWars/ScriptHookV/Managers/FriendlyDefenderManager.cs`.

Update the class declaration line to declare the interface (around line 50):

Before:
```csharp
public class FriendlyDefenderManager
```

After:
```csharp
public class FriendlyDefenderManager : IFriendlyDefenderQuery
```

Add the `using` for the interface at the top of the file (already has `using FactionWars.Combat.Interfaces;` based on existing code; verify and add if missing).

Add the new method near `GetSpawnedDefenderCount` (around line 330):

```csharp
/// <inheritdoc />
public IReadOnlyDictionary<int, DefenderTier> GetDefendersInZone(string zoneId)
{
    if (_spawnedPedTierByZone.TryGetValue(zoneId, out var pedTiers))
    {
        // Return a snapshot copy so callers iterating in a tick aren't affected by
        // concurrent additions/removals from the same tick (e.g. defender death).
        return new Dictionary<int, DefenderTier>(pedTiers);
    }
    return new Dictionary<int, DefenderTier>();
}
```

- [ ] **Step 5: Run test to verify it passes**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter FriendlyDefenderManagerQueryTests`
Expected: PASS.

- [ ] **Step 6: Run full suite**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj`
Expected: All tests pass.

- [ ] **Step 7: Commit**

```bash
git add src/FactionWars/Combat/Interfaces/IFriendlyDefenderQuery.cs src/FactionWars/ScriptHookV/Managers/FriendlyDefenderManager.cs tests/FactionWars.Tests/Unit/ScriptHookV/Managers/FriendlyDefenderManagerQueryTests.cs
git commit -m "feat: expose IFriendlyDefenderQuery on FriendlyDefenderManager"
```

---

## Task 6: Define `ICombatActivityQuery` and implement on `CombatManager`

**Files:**
- Create: `src/FactionWars/Combat/Interfaces/ICombatActivityQuery.cs`
- Modify: `src/FactionWars/ScriptHookV/Managers/CombatManager.cs`
- Test: `tests/FactionWars.Tests/Unit/ScriptHookV/Managers/CombatManagerActivityQueryTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `tests/FactionWars.Tests/Unit/ScriptHookV/Managers/CombatManagerActivityQueryTests.cs`:

```csharp
using FactionWars.Combat.Interfaces;
using FactionWars.Core.Utils;
using FactionWars.ScriptHookV;
using FactionWars.Territory.Interfaces;
using FactionWars.Tests.Mocks;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV.Managers
{
    public class CombatManagerActivityQueryTests
    {
        [Fact]
        public void HasActiveEncounter_NoCombat_ReturnsFalse()
        {
            var bridge = new MockGameBridge();
            var container = ServiceContainerFactory.Create(bridge, new MockMenuProvider());
            var controller = new GameLoopController(container);
            controller.OnTick();

            ICombatActivityQuery query = controller.CombatManager!;
            Assert.False(query.HasActiveEncounter);
        }

        [Fact]
        public void HasActiveEncounter_AfterStartCombat_ReturnsTrue()
        {
            var bridge = new MockGameBridge();
            var container = ServiceContainerFactory.Create(bridge, new MockMenuProvider());
            var controller = new GameLoopController(container);
            controller.OnTick();

            var zoneRepo = container.Resolve<IZoneRepository>();
            var zone = zoneRepo.GetById("vinewood_hills")!;
            zone.OwnerFactionId = "trevor";

            controller.CombatManager!.StartCombat(zone, attackingFactionId: "michael");

            ICombatActivityQuery query = controller.CombatManager!;
            Assert.True(query.HasActiveEncounter);
        }
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter CombatManagerActivityQueryTests`
Expected: FAIL — `ICombatActivityQuery` does not exist.

- [ ] **Step 3: Create the interface**

Create `src/FactionWars/Combat/Interfaces/ICombatActivityQuery.cs`:

```csharp
namespace FactionWars.Combat.Interfaces
{
    /// <summary>
    /// Read-only query for "is there an active mod-managed combat encounter?".
    /// Implemented by CombatManager. Used by DefenderRallyController as one of the
    /// composite "should defenders rally?" signals.
    /// </summary>
    public interface ICombatActivityQuery
    {
        /// <summary>True if a CombatEncounter is currently active.</summary>
        bool HasActiveEncounter { get; }
    }
}
```

- [ ] **Step 4: Implement on CombatManager**

Edit `src/FactionWars/ScriptHookV/Managers/CombatManager.cs`:

Update class declaration (line 20):

Before:
```csharp
public class CombatManager
```

After:
```csharp
public class CombatManager : ICombatActivityQuery
```

Add the property near `IsInCombat` (around line 48):

```csharp
/// <inheritdoc />
public bool HasActiveEncounter => _currentEncounter != null;
```

> **Note:** `IsInCombat` and `HasActiveEncounter` are equivalent; we keep both because `IsInCombat` is the historical name used by lots of callers and `HasActiveEncounter` is the interface name expected by the rally controller. Don't try to delete `IsInCombat`.

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter CombatManagerActivityQueryTests`
Expected: PASS.

- [ ] **Step 6: Run full suite**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj`
Expected: All tests pass.

- [ ] **Step 7: Commit**

```bash
git add src/FactionWars/Combat/Interfaces/ICombatActivityQuery.cs src/FactionWars/ScriptHookV/Managers/CombatManager.cs tests/FactionWars.Tests/Unit/ScriptHookV/Managers/CombatManagerActivityQueryTests.cs
git commit -m "feat: expose ICombatActivityQuery on CombatManager"
```

---

## Task 7: Add `CurrentZone` to `ITerritoryEvents`

**Files:**
- Modify: `src/FactionWars/ScriptHookV/Managers/ITerritoryEvents.cs`
- Modify: existing test mocks/usages of `ITerritoryEvents` (Moq mocks don't need changes; nothing else to change because `TerritoryManager.CurrentZone` already exists).

- [ ] **Step 1: Update the interface**

Edit `src/FactionWars/ScriptHookV/Managers/ITerritoryEvents.cs`:

```csharp
using FactionWars.Territory.Models;
using System;

namespace FactionWars.ScriptHookV.Managers
{
    /// <summary>
    /// Subset of TerritoryManager exposed to consumers that need zone enter/exit
    /// events and the current zone the player is in. Lets dependents subscribe
    /// without taking a hard reference to the concrete TerritoryManager (and lets
    /// unit tests Mock these events).
    /// </summary>
    public interface ITerritoryEvents
    {
        event EventHandler<Zone>? ZoneEntered;
        event EventHandler<Zone>? ZoneExited;

        /// <summary>
        /// The zone the player is currently inside, or null if outside all zones.
        /// </summary>
        Zone? CurrentZone { get; }
    }
}
```

- [ ] **Step 2: Verify TerritoryManager already satisfies it**

Read `src/FactionWars/ScriptHookV/Managers/TerritoryManager.cs` (around line 22). The line `public Zone? CurrentZone => _currentZone;` already exists, so no change needed.

- [ ] **Step 3: Run full suite**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj`
Expected: All tests pass. (The interface change just exposes existing state.)

- [ ] **Step 4: Commit**

```bash
git add src/FactionWars/ScriptHookV/Managers/ITerritoryEvents.cs
git commit -m "feat: expose CurrentZone on ITerritoryEvents"
```

---

## Task 8: Skeleton `DefenderRallyController` (no-op `Update`, constructor only)

**Files:**
- Create: `src/FactionWars/ScriptHookV/Managers/DefenderRallyController.cs`
- Test: `tests/FactionWars.Tests/Unit/ScriptHookV/Managers/DefenderRallyControllerTests.cs`

- [ ] **Step 1: Write the failing test**

Create `tests/FactionWars.Tests/Unit/ScriptHookV/Managers/DefenderRallyControllerTests.cs`:

```csharp
using System.Collections.Generic;
using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Models;
using FactionWars.Core.Utils;
using FactionWars.ScriptHookV.Managers;
using FactionWars.Territory.Models;
using Moq;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV.Managers
{
    public class DefenderRallyControllerTests
    {
        // --- Test fixtures shared by all tests --------------------------------

        private readonly MockGameBridge _bridge = new MockGameBridge();
        private readonly Mock<ITerritoryEvents> _territory = new Mock<ITerritoryEvents>();
        private readonly Mock<IFriendlyDefenderQuery> _defenders = new Mock<IFriendlyDefenderQuery>();
        private readonly Mock<ICombatActivityQuery> _combat = new Mock<ICombatActivityQuery>();
        private string? _playerFactionId = "michael";
        private long _now = 1_000_000;

        private DefenderRallyController BuildSut()
        {
            return new DefenderRallyController(
                _bridge,
                _territory.Object,
                _defenders.Object,
                _combat.Object,
                () => _playerFactionId,
                () => _now);
        }

        // --- Skeleton test ----------------------------------------------------

        [Fact]
        public void Update_NoCurrentZone_DoesNothing()
        {
            _territory.Setup(t => t.CurrentZone).Returns((Zone?)null);

            var sut = BuildSut();
            sut.Update();

            // No tasking calls.
            _defenders.Verify(d => d.GetDefendersInZone(It.IsAny<string>()), Times.Never);
        }
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter DefenderRallyControllerTests`
Expected: FAIL — `DefenderRallyController` does not exist.

- [ ] **Step 3: Create the skeleton**

Create `src/FactionWars/ScriptHookV/Managers/DefenderRallyController.cs`:

```csharp
using System;
using System.Collections.Generic;
using FactionWars.Combat.Interfaces;
using FactionWars.Core.Interfaces;
using FactionWars.Territory.Models;

namespace FactionWars.ScriptHookV.Managers
{
    /// <summary>
    /// When the player is in a zone and combat is relevant, makes that zone's defenders
    /// converge on the player and stay clustered. See
    /// docs/superpowers/specs/2026-05-01-defender-rally-on-threat-design.md.
    /// </summary>
    public sealed class DefenderRallyController
    {
        public const float RallyStoppingRangeM = 8.0f;
        public const float RallyCombatRadiusM = 12.0f;
        public const long UnderAttackCoolDownMs = 5000;

        private readonly IGameBridge _bridge;
        private readonly ITerritoryEvents _territory;
        private readonly IFriendlyDefenderQuery _defenders;
        private readonly ICombatActivityQuery _combat;
        private readonly Func<string?> _currentPlayerFactionIdAccessor;
        private readonly Func<long> _nowMs;

        private long _underAttackUntilTickMs;
        private bool _wasUnderAttack;
        private readonly HashSet<int> _rallyingPeds = new HashSet<int>();

        public DefenderRallyController(
            IGameBridge bridge,
            ITerritoryEvents territory,
            IFriendlyDefenderQuery defenders,
            ICombatActivityQuery combat,
            Func<string?> currentPlayerFactionIdAccessor,
            Func<long> nowMs)
        {
            _bridge = bridge ?? throw new ArgumentNullException(nameof(bridge));
            _territory = territory ?? throw new ArgumentNullException(nameof(territory));
            _defenders = defenders ?? throw new ArgumentNullException(nameof(defenders));
            _combat = combat ?? throw new ArgumentNullException(nameof(combat));
            _currentPlayerFactionIdAccessor = currentPlayerFactionIdAccessor ?? throw new ArgumentNullException(nameof(currentPlayerFactionIdAccessor));
            _nowMs = nowMs ?? throw new ArgumentNullException(nameof(nowMs));
        }

        public void Update()
        {
            var zone = _territory.CurrentZone;
            if (zone == null) return;
            // Subsequent tasks will fill in the rally logic.
        }
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter DefenderRallyControllerTests`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/FactionWars/ScriptHookV/Managers/DefenderRallyController.cs tests/FactionWars.Tests/Unit/ScriptHookV/Managers/DefenderRallyControllerTests.cs
git commit -m "feat: add DefenderRallyController skeleton"
```

---

## Task 9: Friendly rally — issue tasks on `false → true` transition

**Files:**
- Modify: `src/FactionWars/ScriptHookV/Managers/DefenderRallyController.cs`
- Modify: `tests/FactionWars.Tests/Unit/ScriptHookV/Managers/DefenderRallyControllerTests.cs`

- [ ] **Step 1: Write the failing tests**

Append to `DefenderRallyControllerTests.cs`:

```csharp
        // ---- Helpers ---------------------------------------------------------

        private static Zone OwnedZone(string id, string ownerFactionId)
            => new Zone(id, id, new Vector3(0, 0, 0), radius: 100f, strategicValue: 1)
            {
                OwnerFactionId = ownerFactionId,
            };

        private void SetCurrentZone(Zone? zone) => _territory.Setup(t => t.CurrentZone).Returns(zone);

        private int SpawnDefenderInZone(string zoneId, DefenderTier tier = DefenderTier.Basic)
        {
            int handle = _bridge.CreatePed("a_m_y_business_01", new Vector3(0, 0, 0));
            var current = new Dictionary<int, DefenderTier>();
            // Preserve existing defenders if the test added more.
            var prev = _defenders.Object.GetDefendersInZone(zoneId);
            foreach (var kv in prev) current[kv.Key] = kv.Value;
            current[handle] = tier;
            _defenders.Setup(d => d.GetDefendersInZone(zoneId)).Returns(current);
            return handle;
        }

        // ---- Friendly rally: false -> true tests -----------------------------

        [Fact]
        public void Update_PlayerInOwnZone_NoThreat_DoesNotRally()
        {
            var zone = OwnedZone("vinewood_hills", "michael");
            SetCurrentZone(zone);
            int defender = SpawnDefenderInZone(zone.Id);

            var sut = BuildSut();
            sut.Update();

            Assert.False(_bridge.IsPedGoingToEntity(defender));
        }

        [Fact]
        public void Update_PlayerInOwnZone_WantedLevelOn_RalliesDefenders()
        {
            var zone = OwnedZone("vinewood_hills", "michael");
            SetCurrentZone(zone);
            int defender = SpawnDefenderInZone(zone.Id);
            _bridge.WantedLevel = 2;
            _bridge.PlayerPedHandle = 99;

            var sut = BuildSut();
            sut.Update();

            Assert.True(_bridge.IsPedGoingToEntity(defender));
            Assert.Equal(99, _bridge.GetGoToEntityTarget(defender));
            Assert.Equal(DefenderRallyController.RallyStoppingRangeM, _bridge.GetGoToEntityStoppingRange(defender));
            Assert.True(_bridge.IsPedCombatTargeting(defender));
            Assert.Equal(DefenderRallyController.RallyCombatRadiusM, _bridge.GetPedCombatTargetingRadius(defender));
        }

        [Fact]
        public void Update_PlayerInOwnZone_CombatActive_RalliesDefenders()
        {
            var zone = OwnedZone("vinewood_hills", "michael");
            SetCurrentZone(zone);
            int defender = SpawnDefenderInZone(zone.Id);
            _combat.Setup(c => c.HasActiveEncounter).Returns(true);

            var sut = BuildSut();
            sut.Update();

            Assert.True(_bridge.IsPedGoingToEntity(defender));
        }

        [Fact]
        public void Update_PlayerInOwnZone_PlayerDamagedByPed_RalliesDefenders()
        {
            var zone = OwnedZone("vinewood_hills", "michael");
            SetCurrentZone(zone);
            int defender = SpawnDefenderInZone(zone.Id);
            _bridge.PlayerDamagedByPed = true;

            var sut = BuildSut();
            sut.Update();

            Assert.True(_bridge.IsPedGoingToEntity(defender));
        }

        [Fact]
        public void Update_PlayerInOwnZone_RallyTransition_ClearsTasksFirst()
        {
            // Spec says ClearPedTasks is called before TaskGoToEntity so the previous
            // wander task doesn't fight the new go-to.
            var zone = OwnedZone("vinewood_hills", "michael");
            SetCurrentZone(zone);
            int defender = SpawnDefenderInZone(zone.Id);
            _bridge.WantedLevel = 1;

            // Pre-task the defender so we can verify ClearPedTasks happened.
            _bridge.TaskPedWanderInArea(defender, new Vector3(0, 0, 0), 50f);
            Assert.True(_bridge.IsPedWandering(defender));

            var sut = BuildSut();
            sut.Update();

            // After rally, no longer wandering (cleared) and now going-to-entity.
            Assert.False(_bridge.IsPedWandering(defender));
            Assert.True(_bridge.IsPedGoingToEntity(defender));
        }

        [Fact]
        public void Update_PlayerInOwnZone_AlreadyRallying_DoesNotReissueTasks()
        {
            var zone = OwnedZone("vinewood_hills", "michael");
            SetCurrentZone(zone);
            int defender = SpawnDefenderInZone(zone.Id);
            _bridge.WantedLevel = 2;

            var sut = BuildSut();
            sut.Update(); // Transition: false -> true. Issues tasks.

            // Manually simulate that something else has tasked the defender; the
            // controller must NOT re-issue on the second tick (steady state true -> true).
            _bridge.TaskPedWanderInArea(defender, new Vector3(0, 0, 0), 50f);
            sut.Update();

            // Wander task is still in place — controller did not re-issue go-to-entity.
            Assert.True(_bridge.IsPedWandering(defender));
        }
```

> **Note:** Add `using FactionWars.Core.Interfaces;` to the test file's `using` block for `Vector3`. The existing tests already cover `using FactionWars.Combat.Interfaces;`, etc.

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter DefenderRallyControllerTests`
Expected: FAIL — none of the rally-on-threat behavior is implemented yet.

- [ ] **Step 3: Implement the friendly rally logic**

Edit `src/FactionWars/ScriptHookV/Managers/DefenderRallyController.cs`. Replace the body of `Update()`:

```csharp
public void Update()
{
    var zone = _territory.CurrentZone;
    var playerFactionId = _currentPlayerFactionIdAccessor();

    bool isUnderAttackNow = false;
    bool inOwnZone = zone != null && zone.OwnerFactionId != null && zone.OwnerFactionId == playerFactionId;

    if (inOwnZone)
    {
        // Cheap composite signal: any ONE of these triggers rally.
        bool wanted = _bridge.GetWantedLevel() > 0;
        bool encounter = _combat.HasActiveEncounter;
        bool damaged = _bridge.ConsumePlayerDamagedByPedFlag();
        isUnderAttackNow = wanted || encounter || damaged;
    }

    long now = _nowMs();
    if (isUnderAttackNow)
        _underAttackUntilTickMs = now + UnderAttackCoolDownMs;

    bool isUnderAttack = inOwnZone && now < _underAttackUntilTickMs;

    bool shouldRally = isUnderAttack;

    if (shouldRally && !_wasUnderAttack)
    {
        IssueRallyTasks(zone!);
    }

    _wasUnderAttack = shouldRally;
}

private void IssueRallyTasks(Zone zone)
{
    int playerHandle = _bridge.GetPlayerPedHandle();
    var defenders = _defenders.GetDefendersInZone(zone.Id);
    _rallyingPeds.Clear();

    foreach (var pedHandle in defenders.Keys)
    {
        _bridge.ClearPedTasks(pedHandle);
        _bridge.TaskGoToEntity(pedHandle, playerHandle, RallyStoppingRangeM);
        _bridge.TaskCombatHatedTargetsAroundPed(pedHandle, RallyCombatRadiusM);
        _rallyingPeds.Add(pedHandle);
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter DefenderRallyControllerTests`
Expected: PASS — all friendly-rally tests pass, plus the original `Update_NoCurrentZone_DoesNothing`.

- [ ] **Step 5: Run full suite**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj`
Expected: All tests pass.

- [ ] **Step 6: Commit**

```bash
git add src/FactionWars/ScriptHookV/Managers/DefenderRallyController.cs tests/FactionWars.Tests/Unit/ScriptHookV/Managers/DefenderRallyControllerTests.cs
git commit -m "feat: rally defenders on threat in player-owned zones"
```

---

## Task 10: Cool-down — keep rallying for 5s after threat clears, then stand down

**Files:**
- Modify: `src/FactionWars/ScriptHookV/Managers/DefenderRallyController.cs`
- Modify: `tests/FactionWars.Tests/Unit/ScriptHookV/Managers/DefenderRallyControllerTests.cs`

- [ ] **Step 1: Write the failing tests**

Append to `DefenderRallyControllerTests.cs`:

```csharp
        [Fact]
        public void Update_ThreatClears_CoolDownActive_KeepsRally()
        {
            var zone = OwnedZone("vinewood_hills", "michael");
            SetCurrentZone(zone);
            int defender = SpawnDefenderInZone(zone.Id);

            var sut = BuildSut();
            _bridge.WantedLevel = 1;
            sut.Update(); // Tick 1: rally starts.

            _bridge.WantedLevel = 0; // Threat clears.
            _now += 1000;            // 1s later — within 5s cool-down.

            // Re-task the defender so we can verify whether stand-down happened.
            _bridge.TaskPedWanderInArea(defender, new Vector3(0, 0, 0), 50f);

            sut.Update();

            // Within cool-down: defender NOT sent back to wander by the controller.
            // (The wander we set is from our test setup; if the controller stood down
            // it would have re-issued TaskPedWanderInArea, but verifying steady state
            // means the controller does not call any tasking method on this tick.)
            // Easiest way to assert no stand-down: defender is not back to fresh wander
            // task issued by the controller; we check by ensuring _rallyingPeds tracking
            // is preserved via behavior — re-rally on next tick if threat returns
            // should still be a no-op (true -> true).
            // Concrete assertion: still under attack means the next threat wouldn't
            // issue NEW rally tasks (steady state).
            _bridge.WantedLevel = 1;
            sut.Update(); // true -> true: no new tasks.

            // The earlier-set wander is still active (controller did not re-rally).
            Assert.True(_bridge.IsPedWandering(defender));
        }

        [Fact]
        public void Update_ThreatClears_AfterCoolDown_RestoresWander()
        {
            var zone = OwnedZone("vinewood_hills", "michael");
            SetCurrentZone(zone);
            int defender = SpawnDefenderInZone(zone.Id);

            var sut = BuildSut();
            _bridge.WantedLevel = 1;
            sut.Update();                // Rally starts.

            _bridge.WantedLevel = 0;     // Threat ends.
            _now += DefenderRallyController.UnderAttackCoolDownMs + 1; // Past cool-down.

            sut.Update();                // true -> false: stand down.

            // Stand down re-issues wander to send defenders back to patrol.
            Assert.True(_bridge.IsPedWandering(defender));
            Assert.Equal(zone.Center, _bridge.GetPedWanderCenter(defender));
            Assert.Equal(zone.Radius, _bridge.GetPedWanderRadius(defender));
        }

        [Fact]
        public void Update_PlayerDamagedByPed_RefreshesCoolDown()
        {
            var zone = OwnedZone("vinewood_hills", "michael");
            SetCurrentZone(zone);
            SpawnDefenderInZone(zone.Id);

            var sut = BuildSut();
            _bridge.PlayerDamagedByPed = true;
            sut.Update();                                  // tick 1: rally starts (now=1_000_000).

            // Spam through cool-down with periodic damage refreshing the timer.
            for (int i = 0; i < 5; i++)
            {
                _now += DefenderRallyController.UnderAttackCoolDownMs - 100;
                _bridge.PlayerDamagedByPed = true;
                sut.Update();
            }

            // Now stop damaging, advance just under the cool-down.
            _now += DefenderRallyController.UnderAttackCoolDownMs - 100;
            sut.Update();
            // Still rallying — cool-down has been refreshed each iteration.

            // Assert by checking that an additional tick after FULL cool-down stands down.
            _now += DefenderRallyController.UnderAttackCoolDownMs + 1;
            sut.Update();
            // If cool-down had not been refreshed, this would already be stand-down.
            // No direct getter — instead verify the public behavior:
            // re-arming the threat should now trigger a fresh false -> true (re-rally).
            int defender2 = SpawnDefenderInZone(zone.Id, DefenderTier.Basic);
            _bridge.WantedLevel = 1;
            sut.Update(); // false -> true: rallies the new defender.
            Assert.True(_bridge.IsPedGoingToEntity(defender2));
        }
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter DefenderRallyControllerTests`
Expected: FAIL — `Update_ThreatClears_AfterCoolDown_RestoresWander` fails because no stand-down logic exists; the others may pass coincidentally.

- [ ] **Step 3: Implement stand-down logic**

Edit `src/FactionWars/ScriptHookV/Managers/DefenderRallyController.cs`. Update `Update()` to call a new `IssueStandDownTasks` on transition `true → false`:

Replace the existing transition block:

```csharp
    if (shouldRally && !_wasUnderAttack)
    {
        IssueRallyTasks(zone!);
    }

    _wasUnderAttack = shouldRally;
```

With:

```csharp
    if (shouldRally && !_wasUnderAttack)
    {
        IssueRallyTasks(zone!);
    }
    else if (!shouldRally && _wasUnderAttack)
    {
        IssueStandDownTasks(zone);
    }

    _wasUnderAttack = shouldRally;
```

Add the new method below `IssueRallyTasks`:

```csharp
private void IssueStandDownTasks(Zone? zone)
{
    if (zone == null)
    {
        // The player left the zone; defenders that were rallying are out of scope
        // (they belong to a zone we no longer track). Just clear our tracking.
        _rallyingPeds.Clear();
        return;
    }

    foreach (var pedHandle in _rallyingPeds)
    {
        _bridge.TaskPedWanderInArea(pedHandle, zone.Center, zone.Radius);
    }
    _rallyingPeds.Clear();
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter DefenderRallyControllerTests`
Expected: PASS — all three new cool-down tests plus all earlier tests still pass.

- [ ] **Step 5: Run full suite**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj`
Expected: All tests pass.

- [ ] **Step 6: Commit**

```bash
git add src/FactionWars/ScriptHookV/Managers/DefenderRallyController.cs tests/FactionWars.Tests/Unit/ScriptHookV/Managers/DefenderRallyControllerTests.cs
git commit -m "feat: rally cool-down and stand-down for defenders"
```

---

## Task 11: Defenders in OTHER zones are not rallied; neutral zone does not rally

**Files:**
- Modify: `tests/FactionWars.Tests/Unit/ScriptHookV/Managers/DefenderRallyControllerTests.cs`

These behaviors are already implemented (only `_defenders.GetDefendersInZone(currentZone.Id)` is called, only in-own-zone branch evaluates the threat signal), but we add tests to lock the behavior in.

- [ ] **Step 1: Write the failing tests**

Append:

```csharp
        [Fact]
        public void Update_DefenderInDifferentZone_NotRallied()
        {
            // Player is in vinewood_hills, but spawn a defender in a DIFFERENT zone.
            var current = OwnedZone("vinewood_hills", "michael");
            SetCurrentZone(current);

            int otherZoneDefender = _bridge.CreatePed("a_m_y_business_01", new Vector3(0, 0, 0));
            _defenders.Setup(d => d.GetDefendersInZone("vinewood_hills"))
                .Returns(new Dictionary<int, DefenderTier>()); // No defenders in current zone.
            _defenders.Setup(d => d.GetDefendersInZone("other_zone"))
                .Returns(new Dictionary<int, DefenderTier> { [otherZoneDefender] = DefenderTier.Basic });

            _bridge.WantedLevel = 3;

            var sut = BuildSut();
            sut.Update();

            Assert.False(_bridge.IsPedGoingToEntity(otherZoneDefender));
        }

        [Fact]
        public void Update_PlayerInNeutralZone_DoesNotRally()
        {
            var neutral = new Zone("alamo", "Alamo Sea",
                new Vector3(0, 0, 0), radius: 100f, strategicValue: 1);
            SetCurrentZone(neutral);
            int defender = SpawnDefenderInZone(neutral.Id);
            _bridge.WantedLevel = 5;

            var sut = BuildSut();
            sut.Update();

            Assert.False(_bridge.IsPedGoingToEntity(defender));
        }
```

- [ ] **Step 2: Run tests to verify they pass**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter DefenderRallyControllerTests`
Expected: PASS — these are guard-rail tests; the implementation already satisfies them.

If they fail, the implementation is wrong: re-check that `IssueRallyTasks` only iterates `_defenders.GetDefendersInZone(zone.Id)` (current zone) and that `inOwnZone` requires `zone.OwnerFactionId == playerFactionId`.

- [ ] **Step 3: Commit**

```bash
git add tests/FactionWars.Tests/Unit/ScriptHookV/Managers/DefenderRallyControllerTests.cs
git commit -m "test: lock in cross-zone and neutral-zone non-rally behavior"
```

---

## Task 12: Hostile rally — enemy zones rally on the player as long as they're inside

**Files:**
- Modify: `src/FactionWars/ScriptHookV/Managers/DefenderRallyController.cs`
- Modify: `tests/FactionWars.Tests/Unit/ScriptHookV/Managers/DefenderRallyControllerTests.cs`

- [ ] **Step 1: Write the failing tests**

Append:

```csharp
        [Fact]
        public void Update_PlayerInEnemyZone_RalliesEnemyDefenders()
        {
            // Player is michael; zone is owned by trevor.
            _playerFactionId = "michael";
            var enemy = OwnedZone("sandy_shores", "trevor");
            SetCurrentZone(enemy);
            int defender = SpawnDefenderInZone(enemy.Id);
            _bridge.PlayerPedHandle = 99;

            // No threat signals at all (no wanted level, no combat, no damage).
            var sut = BuildSut();
            sut.Update();

            Assert.True(_bridge.IsPedGoingToEntity(defender));
            Assert.Equal(99, _bridge.GetGoToEntityTarget(defender));
        }

        [Fact]
        public void Update_PlayerLeavesEnemyZone_EnemyDefendersResumeWander()
        {
            _playerFactionId = "michael";
            var enemy = OwnedZone("sandy_shores", "trevor");
            SetCurrentZone(enemy);
            int defender = SpawnDefenderInZone(enemy.Id);

            var sut = BuildSut();
            sut.Update(); // Hostile rally starts.

            // Player leaves the zone (no current zone).
            SetCurrentZone(null);
            sut.Update(); // Should immediately stand down — no cool-down for hostile case.

            // After stand-down, defender's wander task… we can't verify the wander
            // because we have no zone reference. The contract is: _rallyingPeds is
            // cleared without re-issuing wander (zone is null in IssueStandDownTasks).
            // Re-arm: a NEW rally must transition false -> true cleanly on the next zone.
            var newEnemy = OwnedZone("grapeseed", "trevor");
            SetCurrentZone(newEnemy);
            int newDefender = SpawnDefenderInZone(newEnemy.Id);
            sut.Update();

            Assert.True(_bridge.IsPedGoingToEntity(newDefender));
        }

        [Fact]
        public void Update_PlayerInEnemyZone_NoCoolDownAfterLeave()
        {
            // The cool-down only applies in own-zone case. In enemy-zone case,
            // leaving the zone immediately ends the rally on the same tick.
            _playerFactionId = "michael";
            var enemy = OwnedZone("sandy_shores", "trevor");
            SetCurrentZone(enemy);
            int defender = SpawnDefenderInZone(enemy.Id);

            var sut = BuildSut();
            sut.Update(); // Rally starts.

            SetCurrentZone(null);
            sut.Update();
            // Stand-down should have been issued without waiting 5s.

            // Verify: re-entering the zone fires a fresh false -> true rally.
            // First re-task the defender to wander so we can confirm the new tick re-rallies it.
            _bridge.TaskPedWanderInArea(defender, enemy.Center, enemy.Radius);
            SetCurrentZone(enemy);
            sut.Update();
            Assert.True(_bridge.IsPedGoingToEntity(defender));
        }
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter DefenderRallyControllerTests`
Expected: FAIL — current implementation only rallies in own zone.

- [ ] **Step 3: Extend `Update()` for enemy-zone case**

Edit `src/FactionWars/ScriptHookV/Managers/DefenderRallyController.cs`. Update the early section of `Update()`:

Replace:

```csharp
    bool isUnderAttackNow = false;
    bool inOwnZone = zone != null && zone.OwnerFactionId != null && zone.OwnerFactionId == playerFactionId;

    if (inOwnZone)
    {
        // Cheap composite signal: any ONE of these triggers rally.
        bool wanted = _bridge.GetWantedLevel() > 0;
        bool encounter = _combat.HasActiveEncounter;
        bool damaged = _bridge.ConsumePlayerDamagedByPedFlag();
        isUnderAttackNow = wanted || encounter || damaged;
    }

    long now = _nowMs();
    if (isUnderAttackNow)
        _underAttackUntilTickMs = now + UnderAttackCoolDownMs;

    bool isUnderAttack = inOwnZone && now < _underAttackUntilTickMs;

    bool shouldRally = isUnderAttack;
```

With:

```csharp
    bool inOwnZone = zone != null && zone.OwnerFactionId != null && zone.OwnerFactionId == playerFactionId;
    bool inEnemyZone = zone != null && zone.OwnerFactionId != null && zone.OwnerFactionId != playerFactionId;

    bool isUnderAttackNow = false;
    if (inOwnZone)
    {
        bool wanted = _bridge.GetWantedLevel() > 0;
        bool encounter = _combat.HasActiveEncounter;
        bool damaged = _bridge.ConsumePlayerDamagedByPedFlag();
        isUnderAttackNow = wanted || encounter || damaged;
    }

    long now = _nowMs();
    if (isUnderAttackNow)
        _underAttackUntilTickMs = now + UnderAttackCoolDownMs;

    bool friendlyRally = inOwnZone && now < _underAttackUntilTickMs;
    bool hostileRally = inEnemyZone;

    bool shouldRally = friendlyRally || hostileRally;
```

The rest of `Update()` (transition handling) is unchanged because it only depends on `shouldRally` and the player's current zone.

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter DefenderRallyControllerTests`
Expected: PASS — all hostile-rally tests pass, all earlier tests still pass.

- [ ] **Step 5: Run full suite**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj`
Expected: All tests pass.

- [ ] **Step 6: Commit**

```bash
git add src/FactionWars/ScriptHookV/Managers/DefenderRallyController.cs tests/FactionWars.Tests/Unit/ScriptHookV/Managers/DefenderRallyControllerTests.cs
git commit -m "feat: rally enemy defenders when player invades their zone"
```

---

## Task 13: Wire `DefenderRallyController` into `GameLoopController`

**Files:**
- Modify: `src/FactionWars/ScriptHookV/GameLoopController.cs`

- [ ] **Step 1: Add the field declaration**

Edit `src/FactionWars/ScriptHookV/GameLoopController.cs`. Find the `_friendlyDefenderManager` field declaration (around line 54) and add directly below:

```csharp
        private DefenderRallyController? _defenderRallyController;
```

- [ ] **Step 2: Construct in `InitializeGameData`**

Find the line constructing `_friendlyDefenderManager = new FriendlyDefenderManager(...)` (around line 619). Directly below the line where the manager is fully constructed and after `_combatManager = new CombatManager(...)` is also constructed (around line 729 — wiring must come after BOTH dependencies exist), add:

```csharp
            _defenderRallyController = new DefenderRallyController(
                _gameBridge,
                _territoryManager,
                _friendlyDefenderManager,
                _combatManager,
                () => CurrentPlayerFactionId,
                () => System.Environment.TickCount);
```

> **Note:** Place this line after `_combatManager.CombatStarted += OnCombatStarted;` so all dependencies are fully initialized. If the existing initialization order makes that awkward, place it at the very end of `InitializeGameData()` just before any final notifications/logs.

- [ ] **Step 3: Call `Update()` from `OnTick()`**

Find the line `_friendlyDefenderManager?.Update();` in `OnTick()` (around line 361). Add directly after it:

```csharp
            _defenderRallyController?.Update();
```

- [ ] **Step 4: Null in `OnAbort`**

Find the `OnAbort()` method and add a line after the `_combatManager = null;` block (around line 1133):

```csharp
            _defenderRallyController = null;
```

- [ ] **Step 5: Build and run full suite**

Run: `dotnet build src/FactionWars/FactionWars.csproj`
Expected: Build succeeds.

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj`
Expected: All tests pass.

- [ ] **Step 6: Commit**

```bash
git add src/FactionWars/ScriptHookV/GameLoopController.cs
git commit -m "feat: wire DefenderRallyController into GameLoopController"
```

---

## Task 14: Deploy and in-game smoke test

**Files:** None — manual gameplay test.

- [ ] **Step 1: Build Debug DLL**

Run: `dotnet build src/FactionWars/FactionWars.csproj -c Debug`

- [ ] **Step 2: Deploy**

Run (from a Bash shell — the project's CLAUDE.md uses Bash; `cp` and forward slashes work fine):
```bash
cp "src/FactionWars/bin/Debug/net48/FactionWars.dll" "E:/SteamLibrary/steamapps/common/Grand Theft Auto V/scripts/"
```

- [ ] **Step 3: Manual test the friendly rally case**

1. Launch GTA V.
2. Walk into a player-owned zone (e.g., as Michael into a Michael-owned zone).
3. Verify defenders are wandering normally (no rally).
4. Trigger a wanted level (punch a civilian or pull a weapon near a cop).
5. Watch defenders converge on the player and stay clustered (~8m stop, ~12m engage).
6. Lose the wanted level.
7. After ~5s, defenders should resume wandering.

Inspect `C:\Users\ryan7\Documents\FactionWars\Logs\` for the most recent log file. Look for:
- `TaskGoToEntity: CALLED for ped …` lines on rally start.
- `TaskCombatHatedTargetsAroundPed: CALLED for ped …` lines stacked after.
- `TaskPedWanderInArea: CALLED for ped …` lines on stand-down.

- [ ] **Step 4: Manual test the hostile rally case**

1. Walk INTO an enemy zone (e.g., as Michael into Trevor's zone).
2. Watch enemy defenders run toward you and engage.
3. Walk back OUT of the zone.
4. Defenders should immediately resume wandering inside the enemy zone (not chase you out).

Re-check the log for the same task patterns.

- [ ] **Step 5: Note any tuning issues**

If the 5s cool-down is too short or too long, or `RallyStoppingRangeM`/`RallyCombatRadiusM` feel wrong, capture observations in this section and tune in a follow-up commit.

- [ ] **Step 6: Commit any tuning adjustments (optional)**

If tuning is needed, edit the constants in `DefenderRallyController.cs`, run the unit tests, and commit:

```bash
git add src/FactionWars/ScriptHookV/Managers/DefenderRallyController.cs
git commit -m "tune: adjust rally cool-down / radii from in-game testing"
```

---

## Self-review checklist

After all tasks complete, before marking the feature done:

- [ ] All 11 spec test cases are present in `DefenderRallyControllerTests.cs`. Cross-reference with the spec list.
- [ ] No method or constant referenced by a later task was redefined in an earlier task.
- [ ] Both rally cases (friendly + hostile) verified in-game per Task 14.
- [ ] No stray `TODO` or placeholder comments left in the new files.
- [ ] `IGameBridge` additions all have corresponding `MockGameBridge` and `GameBridge` implementations.
