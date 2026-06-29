# Stop Ambient Traffic From Driving During Active Zone Battles — Design

**Issue:** #112

## Problem

Ambient (random-traffic) cars keep driving through active combat zones. They run
defenders/attackers/the player over and clog the fight. We want the cars to stop
*driving* without removing them from the world, and without touching the player's
personal vehicle or any vehicle the mod spawns/restores.

## Behaviour

While a faction battle is active **in the zone the player is currently standing in**:

- Scan ambient vehicles within **~80 m of the player**, throttled to about once per
  **750 ms** (new cars stream in continuously, so this re-runs while the battle lasts).
- For each eligible vehicle, **evict the driver** (`TaskPedLeaveVehicle`); the driver
  leaves and flees on foot under ambient panic AI. Immediately **engage the handbrake**
  on the emptied car so it does not coast and run people over.
- The car stays where it is. Only the driving stops.

When the battle in the player's zone ends, the controller goes idle and stops touching
traffic. No explicit "resume driving" restore is needed — evicted drivers are already
gone, and we simply stop re-applying.

### Eligibility (what counts as "ambient")

A vehicle is evicted only if **all** hold:
- It is **not persistent** (`IsVehiclePersistent == false`). Mod-spawned, restored, and
  scripted vehicles are marked persistent/mission-entity, so this excludes them.
- It **has a driver** (`GetVehicleDriver != -1`). Driverless parked cars are left alone.
- It is **not the player's current vehicle** (`handle != GetPlayerVehicle()`), a belt-and-
  braces exclusion on top of persistence.

## Architecture

Three layers, matching the existing seam conventions (native behind `IGameBridge`,
domain logic pure and unit-tested, manager mirrors `PoliceSuppressionController`):

### 1. New `IGameBridge` primitives (real `GameBridge.*` + `MockGameBridge`)

- `int[] GetNearbyVehicles(Vector3 center, float radius)` — area scan (SHVDN
  `World.GetNearbyVehicles`).
- `int GetVehicleDriver(int vehicleHandle)` — driver ped handle, or `-1` if none/dead.
- `bool IsVehiclePersistent(int vehicleHandle)` — mission-entity discriminator; `true`
  for mod/player scripted vehicles, `false` for ambient traffic.
- `void SetVehicleHandbrake(int vehicleHandle, bool engaged)` — keep the emptied car put.

Already present and reused: `TaskPedLeaveVehicle(int)`, `GetPlayerVehicle()`,
`GetPlayerPosition()`.

### 2. Domain: `IAmbientTrafficSuppressor` + `AmbientTrafficSuppressor` (Combat)

Pure selection logic, no natives. Model `VehicleSnapshot { int Handle; bool IsPersistent;
int Driver; }` (Combat.Models).

```
IReadOnlyList<int> SelectDriversToEvict(
    IReadOnlyList<VehicleSnapshot> vehicles,
    int playerVehicleHandle)
```

Returns the driver handles of vehicles where `!IsPersistent && Driver != -1 &&
Handle != playerVehicleHandle`. This is the testable core.

### 3. `AmbientTrafficController` (ScriptHookV.Managers)

Mirrors `PoliceSuppressionController`. Constructor takes `IGameBridge`,
`IAmbientTrafficSuppressor`, and a `Func<bool>` gate (`isBattleActiveInPlayerZone`).
`Update()`:

1. If the gate is false → return (idle).
2. Throttle: skip unless ≥ 750 ms (game time) since the last scan.
3. `center = GetPlayerPosition()`; `handles = GetNearbyVehicles(center, 80f)`.
4. Build `VehicleSnapshot`s via `IsVehiclePersistent` + `GetVehicleDriver`.
5. `drivers = suppressor.SelectDriversToEvict(snapshots, GetPlayerVehicle())`.
6. For each driver: `TaskPedLeaveVehicle(driver)` + `SetVehicleHandbrake(vehicle, true)`;
   log via `FileLogger.AI`.

Re-issuing leave-vehicle to a driver mid-exit is harmless; the throttle bounds spam, and
once a driver is out `GetVehicleDriver` returns `-1` so that car is skipped next scan. No
per-handle "already evicted" state is kept (YAGNI).

### Wiring (`GameLoopController`)

Construct `AmbientTrafficController` alongside the other managers. The gate is computed
from the already-available current zone + battle manager:

```
Func<bool> gate = () => {
    var zone = _territoryManager?.CurrentZone;
    return zone != null && _zoneBattleManager?.GetBattleForZone(zone.Id) != null;
};
```

Tick it in `UpdateWorldSystems` right after `police`:
`_tickProfiler.Measure("ambientTraffic", () => _ambientTrafficController?.Update());`

## Testing (TDD)

- **Suppressor** (`AmbientTrafficSuppressorTests`): evicts ambient-with-driver; skips
  persistent; skips driverless (`Driver == -1`); skips the player's vehicle handle;
  returns multiple drivers when several qualify.
- **Controller** (`AmbientTrafficControllerTests`, vs `MockGameBridge`): gate false → no
  eviction; gate true → ambient driver tasked out + car handbraked, while a persistent car
  and the player's car are untouched; throttle prevents a second scan inside the window.
- **MockGameBridge** (`MockGameBridgeTests`): new primitives — nearby query returns
  vehicles in radius, driver accessor, persistence flag (default `false`, `true` from
  `CreateVehicle`), handbrake setter observable.

## Risk

"Bail and flee" on a fast-moving car: GTA's leave-vehicle decelerates the car as the ped
exits and we handbrake immediately, so it should not roll far. If in-game testing shows
driverless cars coasting into people, the fallback is to brake-to-halt before evicting.
Not built unless observed.

## Out of scope

- Reducing vehicle spawn density (cars are allowed in the zone; only driving stops).
- Ambient pedestrians on foot (the user explicitly does not mind those).
- Restoring/resuming evicted drivers after the battle.
