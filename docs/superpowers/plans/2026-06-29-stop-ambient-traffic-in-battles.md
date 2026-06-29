# Stop Ambient Traffic During Zone Battles — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** While a faction battle is active in the player's current zone, evict drivers from nearby ambient cars (they leave + flee) and handbrake the emptied cars, without touching the player's vehicle or any mod/scripted vehicle.

**Architecture:** Native vehicle access goes behind four new `IGameBridge` primitives. A pure `AmbientTrafficSuppressor` (Combat domain) decides which drivers to evict. An `AmbientTrafficController` (ScriptHookV manager) mirrors `PoliceSuppressionController`: gated on an injected `Func<bool>`, throttled, it scans → selects → evicts. `GameLoopController` wires the gate and ticks it.

**Tech Stack:** C# .NET Framework 4.8, ScriptHookVDotNet3, xUnit, MockGameBridge fake.

## Global Constraints

- `.cs` files MUST be CRLF + UTF-8-no-BOM. After Write/Edit run: `sed -i 's/\r$//; s/$/\r/' <files>`.
- Analyzer ERRORS block build: ≤250 lines/file, ≤40 effective lines/method, ≤10 public methods/class, ≤5 ctor params, one public top-level type per file, `<Class>Tests` name-match must cover each public method, a `.Services` class needs a first-party interface.
- `Vector3` lives in `FactionWars.Core.Interfaces`.
- New `IGameBridge` methods MUST be implemented in real `GameBridge.*` AND `MockGameBridge`, with `FileLogger` debug logging in the real impl.
- Pre-commit hook builds the solution and runs unit tests; do not bypass.
- Eligibility rule (verbatim): evict a vehicle's driver iff `!IsPersistent && Driver != -1 && Handle != playerVehicleHandle`.
- Scan radius ≈ 80 m; scan throttle ≈ 750 ms game time.

---

### Task 1: `IGameBridge` vehicle primitives + MockGameBridge

**Files:**
- Modify: `src/FactionWars/Core/Interfaces/IGameBridge.cs` (add 4 method signatures)
- Modify: `src/FactionWars/ScriptHookV/GameBridge.VehicleState.cs` (real impls)
- Modify: `src/FactionWars/Core/Utils/MockGameBridge.cs` (fake impls + `VehicleState.IsPersistent`, driver accessor; `CreateVehicle` sets `IsPersistent = true`; test setters)
- Test: `tests/FactionWars.Tests/Unit/Core/MockGameBridgeTests.cs`

**Interfaces produced:**
- `int[] GetNearbyVehicles(Vector3 center, float radius)`
- `int GetVehicleDriver(int vehicleHandle)` → driver ped handle or `-1`
- `bool IsVehiclePersistent(int vehicleHandle)`
- `void SetVehicleHandbrake(int vehicleHandle, bool engaged)`

- [ ] **Step 1: Failing MockGameBridge tests.** Add to `MockGameBridgeTests.cs`:

```csharp
[Fact]
public void GetNearbyVehicles_ReturnsVehiclesWithinRadius()
{
    var bridge = new MockGameBridge();
    int near = bridge.CreateVehicle("adder", new Vector3(0f, 0f, 0f));
    int far = bridge.CreateVehicle("adder", new Vector3(500f, 0f, 0f));
    var found = bridge.GetNearbyVehicles(new Vector3(0f, 0f, 0f), 80f);
    Assert.Contains(near, found);
    Assert.DoesNotContain(far, found);
}

[Fact]
public void CreateVehicle_MarksVehiclePersistent_AmbientDefaultsNotPersistent()
{
    var bridge = new MockGameBridge();
    int mod = bridge.CreateVehicle("adder", new Vector3(0f, 0f, 0f));
    int ambient = bridge.CreateAmbientVehicle("adder", new Vector3(1f, 0f, 0f));
    Assert.True(bridge.IsVehiclePersistent(mod));
    Assert.False(bridge.IsVehiclePersistent(ambient));
}

[Fact]
public void GetVehicleDriver_ReturnsDriver_OrMinusOneWhenEmpty()
{
    var bridge = new MockGameBridge();
    int veh = bridge.CreateAmbientVehicle("adder", new Vector3(0f, 0f, 0f));
    Assert.Equal(-1, bridge.GetVehicleDriver(veh));
    int driver = bridge.CreatePed("d", new Vector3(0f, 0f, 0f));
    bridge.SetVehicleDriver(veh, driver);
    Assert.Equal(driver, bridge.GetVehicleDriver(veh));
}

[Fact]
public void SetVehicleHandbrake_IsObservable()
{
    var bridge = new MockGameBridge();
    int veh = bridge.CreateAmbientVehicle("adder", new Vector3(0f, 0f, 0f));
    bridge.SetVehicleHandbrake(veh, true);
    Assert.True(bridge.GetVehicleHandbrakeForTest(veh));
}
```

- [ ] **Step 2: Run, verify fail** (compile errors — methods/helpers absent).
  Run: `dotnet build FactionWars.sln --no-incremental`. Expected: CS errors on the new members.

- [ ] **Step 3: Add interface signatures** to `IGameBridge.cs` near the other vehicle methods:

```csharp
int[] GetNearbyVehicles(Vector3 center, float radius);
int GetVehicleDriver(int vehicleHandle);
bool IsVehiclePersistent(int vehicleHandle);
void SetVehicleHandbrake(int vehicleHandle, bool engaged);
```

- [ ] **Step 4: Real impls** in `GameBridge.VehicleState.cs` (SHVDN), each with `FileLogger` logging:

```csharp
public int[] GetNearbyVehicles(Vector3 center, float radius)
{
    var pos = new GTA.Math.Vector3(center.X, center.Y, center.Z);
    var vehicles = World.GetNearbyVehicles(pos, radius);
    var handles = new int[vehicles.Length];
    for (int i = 0; i < vehicles.Length; i++) handles[i] = vehicles[i].Handle;
    return handles;
}

public int GetVehicleDriver(int vehicleHandle)
{
    var vehicle = (Vehicle)Entity.FromHandle(vehicleHandle);
    if (vehicle == null) return -1;
    var driver = vehicle.Driver;
    return driver != null && driver.Exists() && !driver.IsDead ? driver.Handle : -1;
}

public bool IsVehiclePersistent(int vehicleHandle)
{
    var vehicle = (Vehicle)Entity.FromHandle(vehicleHandle);
    return vehicle != null && vehicle.IsPersistent;
}

public void SetVehicleHandbrake(int vehicleHandle, bool engaged)
{
    Function.Call(Hash.SET_VEHICLE_HANDBRAKE, vehicleHandle, engaged);
}
```

- [ ] **Step 5: Mock impls** in `MockGameBridge.cs`. Add `public bool IsPersistent;` and
  `public int Driver = -1;` to the nested `VehicleState`; in `CreateVehicle` set the new
  vehicle's `IsPersistent = true`. Add a `CreateAmbientVehicle(model, pos)` that creates a
  vehicle with `IsPersistent = false`. Add `SetVehicleDriver(int veh, int ped)`,
  `GetVehicleHandbrakeForTest(int veh)`, a `Dictionary<int,bool> _handbrakes`, and:

```csharp
public int[] GetNearbyVehicles(Vector3 center, float radius)
{
    var result = new List<int>();
    foreach (var kvp in _vehicles)
        if (center.DistanceTo(kvp.Value.Position) <= radius) result.Add(kvp.Key);
    return result.ToArray();
}

public int GetVehicleDriver(int vehicleHandle)
    => _vehicles.TryGetValue(vehicleHandle, out var v) ? v.Driver : -1;

public bool IsVehiclePersistent(int vehicleHandle)
    => _vehicles.TryGetValue(vehicleHandle, out var v) && v.IsPersistent;

public void SetVehicleHandbrake(int vehicleHandle, bool engaged)
    => _handbrakes[vehicleHandle] = engaged;

public bool GetVehicleHandbrakeForTest(int vehicleHandle)
    => _handbrakes.TryGetValue(vehicleHandle, out var on) && on;
```

- [ ] **Step 6: Normalize + build + test.**
  `sed -i 's/\r$//; s/$/\r/'` the four files, then
  `dotnet build FactionWars.sln --no-incremental` (Build succeeded) and
  `dotnet test ... --filter "FullyQualifiedName~MockGameBridgeTests"` (Passed).

- [ ] **Step 7: Commit** `feat: add ambient-vehicle bridge primitives (#112)`.

---

### Task 2: `AmbientTrafficSuppressor` domain selection

**Files:**
- Create: `src/FactionWars/Combat/Models/VehicleSnapshot.cs`
- Create: `src/FactionWars/Combat/Interfaces/IAmbientTrafficSuppressor.cs`
- Create: `src/FactionWars/Combat/Services/AmbientTrafficSuppressor.cs`
- Test: `tests/FactionWars.Tests/Unit/Combat/AmbientTrafficSuppressorTests.cs`

**Interfaces produced:**
- `VehicleSnapshot(int Handle, bool IsPersistent, int Driver)` with read-only props.
- `IReadOnlyList<int> SelectDriversToEvict(IReadOnlyList<VehicleSnapshot> vehicles, int playerVehicleHandle)`

- [ ] **Step 1: Failing tests** `AmbientTrafficSuppressorTests.cs`:

```csharp
using System.Collections.Generic;
using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Models;
using FactionWars.Combat.Services;
using Xunit;

namespace FactionWars.Tests.Unit.Combat
{
    public class AmbientTrafficSuppressorTests
    {
        private readonly IAmbientTrafficSuppressor _suppressor = new AmbientTrafficSuppressor();

        [Fact]
        public void EvictsAmbientCarWithDriver()
        {
            var v = new List<VehicleSnapshot> { new VehicleSnapshot(10, false, 99) };
            Assert.Equal(new[] { 99 }, _suppressor.SelectDriversToEvict(v, playerVehicleHandle: -1));
        }

        [Fact]
        public void SkipsPersistentCar()
        {
            var v = new List<VehicleSnapshot> { new VehicleSnapshot(10, true, 99) };
            Assert.Empty(_suppressor.SelectDriversToEvict(v, -1));
        }

        [Fact]
        public void SkipsDriverlessCar()
        {
            var v = new List<VehicleSnapshot> { new VehicleSnapshot(10, false, -1) };
            Assert.Empty(_suppressor.SelectDriversToEvict(v, -1));
        }

        [Fact]
        public void SkipsPlayerVehicle()
        {
            var v = new List<VehicleSnapshot> { new VehicleSnapshot(10, false, 99) };
            Assert.Empty(_suppressor.SelectDriversToEvict(v, playerVehicleHandle: 10));
        }

        [Fact]
        public void ReturnsAllQualifyingDrivers()
        {
            var v = new List<VehicleSnapshot>
            {
                new VehicleSnapshot(10, false, 1),
                new VehicleSnapshot(11, true, 2),   // persistent, skip
                new VehicleSnapshot(12, false, 3),
            };
            Assert.Equal(new[] { 1, 3 }, _suppressor.SelectDriversToEvict(v, -1));
        }
    }
}
```

- [ ] **Step 2: Run, verify fail** (types absent). Build → CS errors.

- [ ] **Step 3: Model** `VehicleSnapshot.cs`:

```csharp
namespace FactionWars.Combat.Models
{
    /// <summary>Minimal per-tick view of a vehicle the ambient-traffic suppressor reasons about.</summary>
    public readonly struct VehicleSnapshot
    {
        public VehicleSnapshot(int handle, bool isPersistent, int driver)
        {
            Handle = handle;
            IsPersistent = isPersistent;
            Driver = driver;
        }

        public int Handle { get; }
        public bool IsPersistent { get; }
        public int Driver { get; } // ped handle, or -1 when empty
    }
}
```

- [ ] **Step 4: Interface** `IAmbientTrafficSuppressor.cs`:

```csharp
using System.Collections.Generic;
using FactionWars.Combat.Models;

namespace FactionWars.Combat.Interfaces
{
    /// <summary>Selects the drivers of ambient (non-persistent) vehicles to evict so they stop
    /// driving through an active battle, leaving the player's and mod-spawned vehicles alone.</summary>
    public interface IAmbientTrafficSuppressor
    {
        IReadOnlyList<int> SelectDriversToEvict(IReadOnlyList<VehicleSnapshot> vehicles, int playerVehicleHandle);
    }
}
```

- [ ] **Step 5: Service** `AmbientTrafficSuppressor.cs`:

```csharp
using System.Collections.Generic;
using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Models;

namespace FactionWars.Combat.Services
{
    /// <summary>Evicts the driver of every non-persistent, occupied vehicle except the player's:
    /// ambient traffic stops driving while the mod's and the player's own vehicles are untouched.</summary>
    public sealed class AmbientTrafficSuppressor : IAmbientTrafficSuppressor
    {
        public IReadOnlyList<int> SelectDriversToEvict(IReadOnlyList<VehicleSnapshot> vehicles, int playerVehicleHandle)
        {
            var drivers = new List<int>();
            foreach (var v in vehicles)
            {
                if (v.IsPersistent || v.Driver == -1 || v.Handle == playerVehicleHandle) continue;
                drivers.Add(v.Driver);
            }
            return drivers;
        }
    }
}
```

- [ ] **Step 6: Normalize + build + test** `--filter "FullyQualifiedName~AmbientTrafficSuppressorTests"` (Passed).

- [ ] **Step 7: Commit** `feat: add ambient-traffic driver-eviction selection (#112)`.

---

### Task 3: `AmbientTrafficController`

**Files:**
- Create: `src/FactionWars/ScriptHookV/Managers/AmbientTrafficController.cs`
- Test: `tests/FactionWars.Tests/Unit/ScriptHookV/Managers/AmbientTrafficControllerTests.cs`
- Reference (read first, do not modify): `src/FactionWars/ScriptHookV/Managers/PoliceSuppressionController.cs`

**Interfaces consumed:** `IGameBridge` (Task 1) — `GetPlayerPosition`, `GetNearbyVehicles`,
`IsVehiclePersistent`, `GetVehicleDriver`, `GetPlayerVehicle`, `TaskPedLeaveVehicle`,
`SetVehicleHandbrake`, `GetGameTime`; `IAmbientTrafficSuppressor` (Task 2).

**Interfaces produced:** `AmbientTrafficController(IGameBridge, IAmbientTrafficSuppressor, Func<bool> isBattleActiveInPlayerZone)`; `void Update()`.

- [ ] **Step 1: Failing tests** `AmbientTrafficControllerTests.cs`:

```csharp
using System;
using FactionWars.Combat.Services;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Utils;
using FactionWars.ScriptHookV.Managers;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV.Managers
{
    public class AmbientTrafficControllerTests
    {
        private readonly MockGameBridge _bridge = new MockGameBridge();
        private bool _gate = true;
        private AmbientTrafficController Build()
            => new AmbientTrafficController(_bridge, new AmbientTrafficSuppressor(), () => _gate);

        private int AmbientCarWithDriver(Vector3 pos)
        {
            int veh = _bridge.CreateAmbientVehicle("adder", pos);
            int driver = _bridge.CreatePed("d", pos);
            _bridge.SetVehicleDriver(veh, driver);
            _bridge.PutPedInVehicle(driver, veh, -1); // so TaskPedLeaveVehicle has an effect to observe
            return veh;
        }

        [Fact]
        public void GateClosed_DoesNotEvict()
        {
            _gate = false;
            int veh = AmbientCarWithDriver(new Vector3(0f, 0f, 0f));
            int driver = _bridge.GetVehicleDriver(veh);
            Build().Update();
            Assert.True(_bridge.IsPedInVehicle(driver)); // untouched
        }

        [Fact]
        public void GateOpen_EvictsAmbientDriver_AndHandbrakes()
        {
            int veh = AmbientCarWithDriver(new Vector3(0f, 0f, 0f));
            int driver = _bridge.GetVehicleDriver(veh);
            Build().Update();
            Assert.False(_bridge.IsPedInVehicle(driver));      // tasked out
            Assert.True(_bridge.GetVehicleHandbrakeForTest(veh));
        }

        [Fact]
        public void GateOpen_LeavesPlayerVehicleAndPersistentCar()
        {
            int playerVeh = _bridge.CreateVehicle("adder", new Vector3(0f, 0f, 0f)); // persistent
            _bridge.PlayerVehicleHandle = playerVeh;
            int pDriver = _bridge.CreatePed("p", new Vector3(0f, 0f, 0f));
            _bridge.SetVehicleDriver(playerVeh, pDriver);
            _bridge.PutPedInVehicle(pDriver, playerVeh, -1);
            Build().Update();
            Assert.True(_bridge.IsPedInVehicle(pDriver)); // untouched (persistent + player veh)
        }

        [Fact]
        public void Throttle_SkipsSecondScanWithinWindow()
        {
            var controller = Build();
            controller.Update();
            int veh = AmbientCarWithDriver(new Vector3(0f, 0f, 0f)); // appears after first scan
            int driver = _bridge.GetVehicleDriver(veh);
            _bridge.GameTime = 100; // < 750ms throttle
            controller.Update();
            Assert.True(_bridge.IsPedInVehicle(driver)); // not scanned again yet
        }
    }
}
```

- [ ] **Step 2: Run, verify fail** (`AmbientTrafficController` absent). Build → CS errors.

- [ ] **Step 3: Implement** `AmbientTrafficController.cs` (mirror `PoliceSuppressionController` style):

```csharp
using System;
using System.Collections.Generic;
using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Models;
using FactionWars.Core.Interfaces;
using FactionWars.ScriptHookV.Logging;

namespace FactionWars.ScriptHookV.Managers
{
    /// <summary>While a battle is active in the player's zone, evicts drivers from nearby ambient
    /// cars (they leave and flee) and handbrakes the emptied cars so they stop driving through the
    /// fight. The player's vehicle and any mod/scripted (persistent) vehicle are never touched.</summary>
    public sealed class AmbientTrafficController
    {
        private const float ScanRadius = 80f;
        private const int ScanThrottleMs = 750;

        private readonly IGameBridge _gameBridge;
        private readonly IAmbientTrafficSuppressor _suppressor;
        private readonly Func<bool> _isBattleActiveInPlayerZone;
        private int _lastScanMs = int.MinValue;

        public AmbientTrafficController(IGameBridge gameBridge, IAmbientTrafficSuppressor suppressor, Func<bool> isBattleActiveInPlayerZone)
        {
            _gameBridge = gameBridge ?? throw new ArgumentNullException(nameof(gameBridge));
            _suppressor = suppressor ?? throw new ArgumentNullException(nameof(suppressor));
            _isBattleActiveInPlayerZone = isBattleActiveInPlayerZone ?? throw new ArgumentNullException(nameof(isBattleActiveInPlayerZone));
        }

        public void Update()
        {
            if (!_isBattleActiveInPlayerZone()) return;

            int now = _gameBridge.GetGameTime();
            if (now - _lastScanMs < ScanThrottleMs) return;
            _lastScanMs = now;

            var center = _gameBridge.GetPlayerPosition();
            var handles = _gameBridge.GetNearbyVehicles(center, ScanRadius);
            if (handles == null || handles.Length == 0) return;

            var snapshots = new List<VehicleSnapshot>(handles.Length);
            var byDriver = new Dictionary<int, int>();
            foreach (var handle in handles)
            {
                int driver = _gameBridge.GetVehicleDriver(handle);
                snapshots.Add(new VehicleSnapshot(handle, _gameBridge.IsVehiclePersistent(handle), driver));
                if (driver != -1) byDriver[driver] = handle;
            }

            var drivers = _suppressor.SelectDriversToEvict(snapshots, _gameBridge.GetPlayerVehicle());
            foreach (var driver in drivers)
            {
                _gameBridge.TaskPedLeaveVehicle(driver);
                if (byDriver.TryGetValue(driver, out var veh)) _gameBridge.SetVehicleHandbrake(veh, true);
                FileLogger.AI($"AmbientTraffic: evicted driver {driver} from vehicle {byDriver.GetValueOrDefault(driver, -1)}");
            }
        }
    }
}
```

  Note: if `GetValueOrDefault` is unavailable on this TFM, replace the log's vehicle lookup
  with a local `int veh` resolved above. Verify `GetPlayerVehicle()` returns `-1`/`0` when the
  player is on foot (MockGameBridge default) so it never accidentally matches a real handle.

- [ ] **Step 4: Run tests** `--filter "FullyQualifiedName~AmbientTrafficControllerTests"` (Passed).
  If `MockGameBridge.TaskPedLeaveVehicle` does not clear `_pedsInVehicles`, fix the mock to
  remove the ped from its vehicle (matching real leave-vehicle) and add/adjust a
  `MockGameBridgeTests` case documenting it.

- [ ] **Step 5: Normalize + full unit suite** `--filter "FullyQualifiedName~FactionWars.Tests.Unit"` (all pass).

- [ ] **Step 6: Commit** `feat: add AmbientTrafficController (#112)`.

---

### Task 4: Wire into `GameLoopController`

**Files:**
- Modify: `src/FactionWars/ScriptHookV/GameLoopController.SystemUpdates.cs` (tick after `police`)
- Modify: the `GameLoopController` partial that constructs managers (e.g. `Runtime.cs` /
  `Initialization*.cs`) — add `private AmbientTrafficController? _ambientTrafficController;`
  and build it with the gate.
- Reference: how `_policeSuppressionController` is constructed and where `_territoryManager`
  / `_zoneBattleManager` are available.

**Interfaces consumed:** `AmbientTrafficController` (Task 3), `AmbientTrafficSuppressor` (Task 2),
`IZoneBattleManager.GetBattleForZone(string)`, `TerritoryManager.CurrentZone`.

- [ ] **Step 1: Construct** the controller where `_policeSuppressionController` is built:

```csharp
_ambientTrafficController = new AmbientTrafficController(
    _gameBridge,
    new AmbientTrafficSuppressor(),
    () =>
    {
        var zone = _territoryManager?.CurrentZone;
        return zone != null && _zoneBattleManager?.GetBattleForZone(zone.Id) != null;
    });
```

- [ ] **Step 2: Tick** it in `UpdateWorldSystems`, immediately after the `police` measure:

```csharp
_tickProfiler.Measure("ambientTraffic", () => _ambientTrafficController?.Update());
```

- [ ] **Step 3: Normalize + build** `dotnet build FactionWars.sln --no-incremental` (Build succeeded).

- [ ] **Step 4: Full unit suite** `--filter "FullyQualifiedName~FactionWars.Tests.Unit"` (all pass).

- [ ] **Step 5: Commit** `feat: wire AmbientTrafficController into the game loop (#112)`.

---

## Self-Review

**Spec coverage:** Trigger (battle in current zone) → Task 4 gate. Eviction + handbrake →
Task 3. Eligibility rule → Task 2. ~80 m / ~750 ms → Task 3 consts. Bridge primitives →
Task 1. Player/mod exclusion → Task 2 rule + persistence flag (Tasks 1–2). All covered.

**Placeholder scan:** none — every code step is concrete.

**Type consistency:** `VehicleSnapshot(int, bool, int)`, `SelectDriversToEvict(IReadOnlyList<VehicleSnapshot>, int)`,
`GetVehicleDriver→-1`, `IsVehiclePersistent`, `SetVehicleHandbrake(int,bool)`,
`GetVehicleHandbrakeForTest`, `CreateAmbientVehicle`, `SetVehicleDriver` — names match across tasks.

**Risk note:** if driverless cars coast in-game, add a brake-to-halt before eviction (spec risk section).
