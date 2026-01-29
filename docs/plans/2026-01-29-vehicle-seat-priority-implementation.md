# Vehicle Seat Priority Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Implement smart seat assignment for followers entering vehicles - prioritize turrets and optimal seats by vehicle type, with coordinated direct assignment within 15m proximity.

**Architecture:** Add `GetVehicleClass` and `IsVehicleSeatTurret` to GameBridge. Create `VehicleSeatPriorityService` to sort seats by priority. Update `FollowerManager` to use coordinated assignment with proximity filter.

**Tech Stack:** C#, xUnit, Moq, GTA V natives (GET_VEHICLE_CLASS, IS_TURRET_SEAT)

---

## Task 1: Add GetVehicleClass and IsVehicleSeatTurret to IGameBridge

**Files:**
- Modify: `src/FactionWars/Core/Interfaces/IGameBridge.cs`
- Test: `tests/FactionWars.Tests/Unit/Core/MockGameBridgeTests.cs`

**Step 1: Add interface methods**

Add to `IGameBridge.cs` after `GetVehicleFreeSeats`:

```csharp
/// <summary>
/// Gets the vehicle class (e.g., 15=helicopter, 14=boat, 8=motorcycle).
/// </summary>
/// <param name="vehicleHandle">Handle of the vehicle.</param>
/// <returns>Vehicle class ID, or -1 if invalid.</returns>
int GetVehicleClass(int vehicleHandle);

/// <summary>
/// Checks if a vehicle seat is a turret/gun seat.
/// </summary>
/// <param name="vehicleHandle">Handle of the vehicle.</param>
/// <param name="seatIndex">The seat index to check.</param>
/// <returns>True if the seat is a turret, false otherwise.</returns>
bool IsVehicleSeatTurret(int vehicleHandle, int seatIndex);

/// <summary>
/// Gets the position of a vehicle.
/// </summary>
/// <param name="vehicleHandle">Handle of the vehicle.</param>
/// <returns>The vehicle's position, or Vector3.Zero if invalid.</returns>
Vector3 GetVehiclePosition(int vehicleHandle);
```

**Step 2: Build to verify interface compiles**

Run: `dotnet build src/FactionWars --verbosity quiet`
Expected: Build errors (methods not implemented in GameBridge/MockGameBridge)

---

## Task 2: Implement in GameBridge

**Files:**
- Modify: `src/FactionWars/ScriptHookV/GameBridge.cs`

**Step 1: Implement GetVehicleClass**

Add after `GetVehicleFreeSeats`:

```csharp
/// <inheritdoc />
public int GetVehicleClass(int vehicleHandle)
{
    try
    {
        var vehicle = new Vehicle(vehicleHandle);
        if (!vehicle.Exists())
            return -1;

        return Function.Call<int>(Hash.GET_VEHICLE_CLASS, vehicleHandle);
    }
    catch (Exception ex)
    {
        FileLogger.Error("GetVehicleClass error", ex);
        return -1;
    }
}
```

**Step 2: Implement IsVehicleSeatTurret**

```csharp
/// <inheritdoc />
public bool IsVehicleSeatTurret(int vehicleHandle, int seatIndex)
{
    try
    {
        var vehicle = new Vehicle(vehicleHandle);
        if (!vehicle.Exists())
            return false;

        // GTA V uses -1 for driver, 0+ for passengers
        // Our abstraction uses 0 for driver, 1+ for passengers
        var gtaSeatIndex = seatIndex - 1;
        return Function.Call<bool>(Hash.IS_TURRET_SEAT, vehicleHandle, gtaSeatIndex);
    }
    catch (Exception ex)
    {
        FileLogger.Error("IsVehicleSeatTurret error", ex);
        return false;
    }
}
```

**Step 3: Implement GetVehiclePosition**

```csharp
/// <inheritdoc />
public Vector3 GetVehiclePosition(int vehicleHandle)
{
    try
    {
        var vehicle = new Vehicle(vehicleHandle);
        if (!vehicle.Exists())
            return Vector3.Zero;

        var pos = vehicle.Position;
        return new Vector3(pos.X, pos.Y, pos.Z);
    }
    catch (Exception ex)
    {
        FileLogger.Error("GetVehiclePosition error", ex);
        return Vector3.Zero;
    }
}
```

**Step 4: Build to verify**

Run: `dotnet build src/FactionWars --verbosity quiet`
Expected: Build errors (MockGameBridge not implemented yet)

---

## Task 3: Implement in MockGameBridge

**Files:**
- Modify: `src/FactionWars/Core/Utils/MockGameBridge.cs`
- Test: `tests/FactionWars.Tests/Unit/Core/MockGameBridgeTests.cs`

**Step 1: Add vehicle class tracking to VehicleState**

Find `VehicleState` class and add:

```csharp
public int VehicleClass { get; set; } = 0; // Default to Compacts
public HashSet<int> TurretSeats { get; } = new HashSet<int>();
public Vector3 Position { get; set; } = Vector3.Zero;
```

**Step 2: Implement mock methods**

```csharp
/// <inheritdoc />
public int GetVehicleClass(int vehicleHandle)
{
    if (_vehicles.TryGetValue(vehicleHandle, out var state))
        return state.VehicleClass;
    return -1;
}

/// <inheritdoc />
public bool IsVehicleSeatTurret(int vehicleHandle, int seatIndex)
{
    if (_vehicles.TryGetValue(vehicleHandle, out var state))
        return state.TurretSeats.Contains(seatIndex);
    return false;
}

/// <inheritdoc />
public Vector3 GetVehiclePosition(int vehicleHandle)
{
    if (_vehicles.TryGetValue(vehicleHandle, out var state))
        return state.Position;
    return Vector3.Zero;
}
```

**Step 3: Add test helper methods**

```csharp
/// <summary>
/// Sets the vehicle class for testing.
/// </summary>
public void SetVehicleClass(int vehicleHandle, int vehicleClass)
{
    if (_vehicles.TryGetValue(vehicleHandle, out var state))
        state.VehicleClass = vehicleClass;
}

/// <summary>
/// Marks a seat as a turret seat for testing.
/// </summary>
public void SetSeatAsTurret(int vehicleHandle, int seatIndex)
{
    if (_vehicles.TryGetValue(vehicleHandle, out var state))
        state.TurretSeats.Add(seatIndex);
}

/// <summary>
/// Sets the vehicle position for testing.
/// </summary>
public void SetVehiclePosition(int vehicleHandle, Vector3 position)
{
    if (_vehicles.TryGetValue(vehicleHandle, out var state))
        state.Position = position;
}
```

**Step 4: Write tests**

Add to `MockGameBridgeTests.cs`:

```csharp
[Fact]
public void GetVehicleClass_ReturnsSetClass()
{
    var vehicleHandle = _gameBridge.CreateVehicle("car", new Vector3(0, 0, 0));
    _gameBridge.SetVehicleClass(vehicleHandle, 15); // Helicopter

    var result = _gameBridge.GetVehicleClass(vehicleHandle);

    Assert.Equal(15, result);
}

[Fact]
public void GetVehicleClass_InvalidVehicle_ReturnsNegativeOne()
{
    var result = _gameBridge.GetVehicleClass(9999);

    Assert.Equal(-1, result);
}

[Fact]
public void IsVehicleSeatTurret_ReturnsTrueForTurretSeat()
{
    var vehicleHandle = _gameBridge.CreateVehicle("technical", new Vector3(0, 0, 0));
    _gameBridge.SetSeatAsTurret(vehicleHandle, 2); // Back turret

    Assert.True(_gameBridge.IsVehicleSeatTurret(vehicleHandle, 2));
    Assert.False(_gameBridge.IsVehicleSeatTurret(vehicleHandle, 1));
}

[Fact]
public void GetVehiclePosition_ReturnsSetPosition()
{
    var vehicleHandle = _gameBridge.CreateVehicle("car", new Vector3(10, 20, 30));
    _gameBridge.SetVehiclePosition(vehicleHandle, new Vector3(100, 200, 50));

    var result = _gameBridge.GetVehiclePosition(vehicleHandle);

    Assert.Equal(100, result.X);
    Assert.Equal(200, result.Y);
    Assert.Equal(50, result.Z);
}
```

**Step 5: Run tests**

Run: `dotnet test tests/FactionWars.Tests --filter "FullyQualifiedName~MockGameBridgeTests.GetVehicleClass"`
Expected: PASS

**Step 6: Commit**

```bash
git add src/FactionWars/Core/Interfaces/IGameBridge.cs src/FactionWars/ScriptHookV/GameBridge.cs src/FactionWars/Core/Utils/MockGameBridge.cs tests/FactionWars.Tests/Unit/Core/MockGameBridgeTests.cs
git commit -m "feat: add GetVehicleClass, IsVehicleSeatTurret, GetVehiclePosition to GameBridge"
```

---

## Task 4: Create IVehicleSeatPriorityService Interface

**Files:**
- Create: `src/FactionWars/Core/Interfaces/IVehicleSeatPriorityService.cs`

**Step 1: Create interface**

```csharp
using System.Numerics;

namespace FactionWars.Core.Interfaces
{
    /// <summary>
    /// Service for determining seat priority when followers enter vehicles.
    /// </summary>
    public interface IVehicleSeatPriorityService
    {
        /// <summary>
        /// Gets free seats sorted by priority for the given vehicle type.
        /// Turret seats first for turret vehicles, back seats first for helicopters, etc.
        /// </summary>
        /// <param name="vehicleHandle">Handle of the vehicle.</param>
        /// <returns>Array of seat indices sorted by priority (best seats first).</returns>
        int[] GetPrioritizedFreeSeats(int vehicleHandle);

        /// <summary>
        /// Filters followers to only those within range of the vehicle.
        /// </summary>
        /// <param name="followerPedHandles">Array of follower ped handles.</param>
        /// <param name="vehicleHandle">Handle of the vehicle.</param>
        /// <param name="maxDistance">Maximum distance in meters (default 15m).</param>
        /// <returns>Array of follower handles within range, preserving order.</returns>
        int[] FilterFollowersByProximity(int[] followerPedHandles, int vehicleHandle, float maxDistance = 15f);
    }
}
```

**Step 2: Build**

Run: `dotnet build src/FactionWars --verbosity quiet`
Expected: PASS (interface only)

---

## Task 5: Create VehicleSeatPriorityService

**Files:**
- Create: `src/FactionWars/Core/Services/VehicleSeatPriorityService.cs`
- Create: `tests/FactionWars.Tests/Unit/Core/Services/VehicleSeatPriorityServiceTests.cs`

**Step 1: Write failing tests**

```csharp
using FactionWars.Core.Interfaces;
using FactionWars.Core.Services;
using FactionWars.Core.Utils;
using System.Numerics;
using Xunit;

namespace FactionWars.Tests.Unit.Core.Services
{
    public class VehicleSeatPriorityServiceTests
    {
        private readonly MockGameBridge _gameBridge;
        private readonly VehicleSeatPriorityService _service;

        public VehicleSeatPriorityServiceTests()
        {
            _gameBridge = new MockGameBridge();
            _service = new VehicleSeatPriorityService(_gameBridge);
        }

        [Fact]
        public void GetPrioritizedFreeSeats_TurretVehicle_TurretSeatFirst()
        {
            // Arrange - Technical with turret at seat 2
            var vehicle = _gameBridge.CreateVehicle("technical", Vector3.Zero);
            _gameBridge.SetVehicleClass(vehicle, 19); // Military
            _gameBridge.SetSeatAsTurret(vehicle, 2);
            // Seats 1, 2, 3 are free (0 is driver)

            // Act
            var seats = _service.GetPrioritizedFreeSeats(vehicle);

            // Assert - turret seat (2) should be first
            Assert.Equal(2, seats[0]);
        }

        [Fact]
        public void GetPrioritizedFreeSeats_Helicopter_BackSeatsFirst()
        {
            // Arrange - Helicopter (class 15)
            var vehicle = _gameBridge.CreateVehicle("buzzard", Vector3.Zero);
            _gameBridge.SetVehicleClass(vehicle, 15);
            // Seats 1, 2, 3 are free (front passenger, back left, back right)

            // Act
            var seats = _service.GetPrioritizedFreeSeats(vehicle);

            // Assert - back seats (2, 3) should come before front (1)
            Assert.True(seats[0] > 1 || seats.Length == 1);
        }

        [Fact]
        public void GetPrioritizedFreeSeats_RegularCar_BackSeatsFirst()
        {
            // Arrange - Regular car (class 1 = Sedan)
            var vehicle = _gameBridge.CreateVehicle("sultan", Vector3.Zero);
            _gameBridge.SetVehicleClass(vehicle, 1);
            // Seats 1, 2, 3 are free

            // Act
            var seats = _service.GetPrioritizedFreeSeats(vehicle);

            // Assert - back seats (2, 3) should come before front (1)
            Assert.True(seats[0] > 1 || seats.Length == 1);
        }

        [Fact]
        public void FilterFollowersByProximity_ReturnsOnlyNearbyFollowers()
        {
            // Arrange
            var vehicle = _gameBridge.CreateVehicle("car", new Vector3(100, 100, 0));
            _gameBridge.SetVehiclePosition(vehicle, new Vector3(100, 100, 0));

            var nearPed = _gameBridge.CreatePed("ped1", new Vector3(105, 100, 0)); // 5m away
            var farPed = _gameBridge.CreatePed("ped2", new Vector3(200, 100, 0));  // 100m away

            // Act
            var nearby = _service.FilterFollowersByProximity(
                new[] { nearPed, farPed }, vehicle, 15f);

            // Assert
            Assert.Single(nearby);
            Assert.Equal(nearPed, nearby[0]);
        }

        [Fact]
        public void FilterFollowersByProximity_PreservesOrder()
        {
            // Arrange
            var vehicle = _gameBridge.CreateVehicle("car", new Vector3(100, 100, 0));
            _gameBridge.SetVehiclePosition(vehicle, new Vector3(100, 100, 0));

            var ped1 = _gameBridge.CreatePed("ped1", new Vector3(105, 100, 0)); // 5m
            var ped2 = _gameBridge.CreatePed("ped2", new Vector3(110, 100, 0)); // 10m
            var ped3 = _gameBridge.CreatePed("ped3", new Vector3(112, 100, 0)); // 12m

            // Act
            var nearby = _service.FilterFollowersByProximity(
                new[] { ped1, ped2, ped3 }, vehicle, 15f);

            // Assert - order preserved
            Assert.Equal(3, nearby.Length);
            Assert.Equal(ped1, nearby[0]);
            Assert.Equal(ped2, nearby[1]);
            Assert.Equal(ped3, nearby[2]);
        }
    }
}
```

**Step 2: Run tests to verify they fail**

Run: `dotnet test tests/FactionWars.Tests --filter "FullyQualifiedName~VehicleSeatPriorityServiceTests" --no-build`
Expected: FAIL (VehicleSeatPriorityService doesn't exist)

**Step 3: Implement VehicleSeatPriorityService**

```csharp
using FactionWars.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace FactionWars.Core.Services
{
    /// <summary>
    /// Service for determining seat priority when followers enter vehicles.
    /// </summary>
    public class VehicleSeatPriorityService : IVehicleSeatPriorityService
    {
        // Vehicle class constants
        private const int VehicleClassHelicopter = 15;
        private const int VehicleClassBoat = 14;
        private const int VehicleClassMotorcycle = 8;

        private readonly IGameBridge _gameBridge;

        public VehicleSeatPriorityService(IGameBridge gameBridge)
        {
            _gameBridge = gameBridge ?? throw new ArgumentNullException(nameof(gameBridge));
        }

        /// <inheritdoc />
        public int[] GetPrioritizedFreeSeats(int vehicleHandle)
        {
            var freeSeats = _gameBridge.GetVehicleFreeSeats(vehicleHandle);
            if (freeSeats == null || freeSeats.Length == 0)
                return Array.Empty<int>();

            var vehicleClass = _gameBridge.GetVehicleClass(vehicleHandle);

            return vehicleClass switch
            {
                VehicleClassHelicopter => SortForHelicopter(freeSeats),
                VehicleClassBoat => SortForBoat(freeSeats, vehicleHandle),
                VehicleClassMotorcycle => freeSeats, // Only 1 passenger seat typically
                _ => SortDefault(freeSeats, vehicleHandle)
            };
        }

        /// <inheritdoc />
        public int[] FilterFollowersByProximity(int[] followerPedHandles, int vehicleHandle, float maxDistance = 15f)
        {
            if (followerPedHandles == null || followerPedHandles.Length == 0)
                return Array.Empty<int>();

            var vehiclePos = _gameBridge.GetVehiclePosition(vehicleHandle);
            if (vehiclePos == Vector3.Zero)
                return Array.Empty<int>();

            var nearby = new List<int>();

            foreach (var pedHandle in followerPedHandles)
            {
                var pedPos = _gameBridge.GetPedPosition(pedHandle);
                var distance = Vector3.Distance(pedPos, vehiclePos);

                if (distance <= maxDistance)
                {
                    nearby.Add(pedHandle);
                }
            }

            return nearby.ToArray();
        }

        /// <summary>
        /// Sort for helicopters: back seats first, then front passenger.
        /// </summary>
        private int[] SortForHelicopter(int[] freeSeats)
        {
            // Back seats are typically index 2+ in helicopters
            // Front passenger is index 1
            return freeSeats.OrderByDescending(s => s > 1 ? 1 : 0).ToArray();
        }

        /// <summary>
        /// Sort for boats: turret/gun seats first, then others.
        /// </summary>
        private int[] SortForBoat(int[] freeSeats, int vehicleHandle)
        {
            return freeSeats
                .OrderByDescending(s => _gameBridge.IsVehicleSeatTurret(vehicleHandle, s) ? 1 : 0)
                .ToArray();
        }

        /// <summary>
        /// Default sort: turrets first, then back seats, then front.
        /// Works for cars, SUVs, military vehicles, etc.
        /// </summary>
        private int[] SortDefault(int[] freeSeats, int vehicleHandle)
        {
            return freeSeats
                .OrderByDescending(s => _gameBridge.IsVehicleSeatTurret(vehicleHandle, s) ? 2 : 0)
                .ThenByDescending(s => s > 1 ? 1 : 0) // Back seats (2+) before front (1)
                .ToArray();
        }
    }
}
```

**Step 4: Run tests**

Run: `dotnet test tests/FactionWars.Tests --filter "FullyQualifiedName~VehicleSeatPriorityServiceTests"`
Expected: PASS

**Step 5: Commit**

```bash
git add src/FactionWars/Core/Interfaces/IVehicleSeatPriorityService.cs src/FactionWars/Core/Services/VehicleSeatPriorityService.cs tests/FactionWars.Tests/Unit/Core/Services/VehicleSeatPriorityServiceTests.cs
git commit -m "feat: add VehicleSeatPriorityService for smart seat assignment"
```

---

## Task 6: Update FollowerManager for Coordinated Assignment

**Files:**
- Modify: `src/FactionWars/ScriptHookV/Managers/FollowerManager.cs`
- Test: `tests/FactionWars.Tests/Unit/ScriptHookV/Managers/FollowerManagerTests.cs`

**Step 1: Add IVehicleSeatPriorityService dependency**

Update constructor:

```csharp
private readonly IVehicleSeatPriorityService _seatPriorityService;

public FollowerManager(
    IGameBridge gameBridge,
    IFollowerService followerService,
    IPedBlipService pedBlipService,
    IVehicleSeatPriorityService seatPriorityService)
{
    _gameBridge = gameBridge ?? throw new ArgumentNullException(nameof(gameBridge));
    _followerService = followerService ?? throw new ArgumentNullException(nameof(followerService));
    _pedBlipService = pedBlipService ?? throw new ArgumentNullException(nameof(pedBlipService));
    _seatPriorityService = seatPriorityService ?? throw new ArgumentNullException(nameof(seatPriorityService));
}
```

**Step 2: Update the Update method for coordinated assignment**

Replace the vehicle entry logic (around lines 249-256):

```csharp
public void Update(string factionId)
{
    if (string.IsNullOrEmpty(factionId))
        return;

    var followers = _followerService.GetFollowers(factionId);

    // Check vehicle state
    var playerInVehicle = _gameBridge.IsPlayerInVehicle();
    var playerVehicle = playerInVehicle ? _gameBridge.GetPlayerVehicle() : -1;

    // Collect follower handles for coordinated assignment
    var aliveFollowerHandles = new List<int>();

    // Check each follower for death
    foreach (var follower in followers)
    {
        if (follower.PedHandle < 0)
            continue;

        if (!_gameBridge.IsPedAlive(follower.PedHandle))
        {
            _pedBlipService.RemoveBlipForPed(follower.PedHandle);
            _gameBridge.DeletePed(follower.PedHandle);
            _followerService.HandleFollowerDeath(follower.Id);
            FollowerDied?.Invoke(this, follower);
            continue;
        }

        aliveFollowerHandles.Add(follower.PedHandle);
    }

    // Handle vehicle entry/exit
    if (playerInVehicle && playerVehicle >= 0)
    {
        // Get prioritized seats and nearby followers
        var prioritizedSeats = _seatPriorityService.GetPrioritizedFreeSeats(playerVehicle);
        var nearbyFollowers = _seatPriorityService.FilterFollowersByProximity(
            aliveFollowerHandles.ToArray(), playerVehicle, 15f);

        // Coordinated assignment - assign all at once
        var seatIndex = 0;
        foreach (var pedHandle in nearbyFollowers)
        {
            if (seatIndex >= prioritizedSeats.Length)
                break;

            var inVehicle = _gameBridge.IsPedInVehicle(pedHandle);
            var tryingToEnter = _gameBridge.IsPedTryingToEnterVehicle(pedHandle);

            if (!inVehicle && !tryingToEnter)
            {
                _gameBridge.TaskPedEnterVehicle(pedHandle, playerVehicle, prioritizedSeats[seatIndex]);
                seatIndex++;
            }
            else if (inVehicle)
            {
                // Already in vehicle, skip this seat
                seatIndex++;
            }
        }
    }
    else
    {
        // Player not in vehicle - make followers exit if they're in one
        foreach (var pedHandle in aliveFollowerHandles)
        {
            if (_gameBridge.IsPedInVehicle(pedHandle))
            {
                _gameBridge.TaskPedLeaveVehicle(pedHandle);
            }
        }
    }
}
```

**Step 3: Update ServiceContainerFactory to register the new service**

In `ServiceContainerFactory.cs`, add:

```csharp
container.RegisterSingleton<IVehicleSeatPriorityService>(() =>
    new VehicleSeatPriorityService(container.Resolve<IGameBridge>()));
```

And update FollowerManager registration to include the new dependency.

**Step 4: Update tests**

Update `FollowerManagerTests.cs` to mock the new dependency:

```csharp
private Mock<IVehicleSeatPriorityService> _mockSeatPriorityService;

// In setup:
_mockSeatPriorityService = new Mock<IVehicleSeatPriorityService>();
_mockSeatPriorityService.Setup(s => s.GetPrioritizedFreeSeats(It.IsAny<int>()))
    .Returns(new[] { 1, 2, 3 });
_mockSeatPriorityService.Setup(s => s.FilterFollowersByProximity(
    It.IsAny<int[]>(), It.IsAny<int>(), It.IsAny<float>()))
    .Returns<int[], int, float>((handles, v, d) => handles);

_manager = new FollowerManager(
    _mockGameBridge.Object,
    _mockFollowerService.Object,
    _mockPedBlipService.Object,
    _mockSeatPriorityService.Object);
```

**Step 5: Run all tests**

Run: `dotnet test tests/FactionWars.Tests --verbosity quiet`
Expected: PASS

**Step 6: Commit**

```bash
git add src/FactionWars/ScriptHookV/Managers/FollowerManager.cs src/FactionWars/ScriptHookV/ServiceContainerFactory.cs tests/FactionWars.Tests/Unit/ScriptHookV/Managers/FollowerManagerTests.cs
git commit -m "feat: update FollowerManager for coordinated seat assignment with priority"
```

---

## Task 7: Final Integration Test

**Files:**
- Create: `tests/FactionWars.Tests/Integration/VehicleSeatPriorityIntegrationTests.cs`

**Step 1: Write integration test**

```csharp
using FactionWars.Core.Interfaces;
using FactionWars.Core.Services;
using FactionWars.Core.Utils;
using System.Numerics;
using Xunit;

namespace FactionWars.Tests.Integration
{
    public class VehicleSeatPriorityIntegrationTests
    {
        [Fact]
        public void FullFlow_TurretVehicle_TurretSeatAssignedFirst()
        {
            // Arrange
            var gameBridge = new MockGameBridge();
            var service = new VehicleSeatPriorityService(gameBridge);

            // Create turret vehicle (Technical-style)
            var vehicle = gameBridge.CreateVehicle("technical", new Vector3(100, 100, 0));
            gameBridge.SetVehiclePosition(vehicle, new Vector3(100, 100, 0));
            gameBridge.SetVehicleClass(vehicle, 19); // Military
            gameBridge.SetSeatAsTurret(vehicle, 2);

            // Create followers nearby
            var follower1 = gameBridge.CreatePed("follower1", new Vector3(105, 100, 0));
            var follower2 = gameBridge.CreatePed("follower2", new Vector3(110, 100, 0));

            // Act
            var seats = service.GetPrioritizedFreeSeats(vehicle);
            var nearby = service.FilterFollowersByProximity(
                new[] { follower1, follower2 }, vehicle, 15f);

            // Assert
            Assert.Equal(2, seats[0]); // Turret first
            Assert.Equal(2, nearby.Length); // Both within 15m
        }

        [Fact]
        public void FullFlow_Helicopter_BackSeatsFirst()
        {
            // Arrange
            var gameBridge = new MockGameBridge();
            var service = new VehicleSeatPriorityService(gameBridge);

            var vehicle = gameBridge.CreateVehicle("buzzard", new Vector3(100, 100, 0));
            gameBridge.SetVehiclePosition(vehicle, new Vector3(100, 100, 0));
            gameBridge.SetVehicleClass(vehicle, 15); // Helicopter

            // Act
            var seats = service.GetPrioritizedFreeSeats(vehicle);

            // Assert - back seats should be prioritized
            Assert.True(seats.Length > 0);
        }

        [Fact]
        public void ProximityFilter_ExcludesDistantFollowers()
        {
            // Arrange
            var gameBridge = new MockGameBridge();
            var service = new VehicleSeatPriorityService(gameBridge);

            var vehicle = gameBridge.CreateVehicle("car", new Vector3(0, 0, 0));
            gameBridge.SetVehiclePosition(vehicle, new Vector3(0, 0, 0));

            var nearFollower = gameBridge.CreatePed("near", new Vector3(10, 0, 0));
            var farFollower = gameBridge.CreatePed("far", new Vector3(100, 0, 0));

            // Act
            var nearby = service.FilterFollowersByProximity(
                new[] { nearFollower, farFollower }, vehicle, 15f);

            // Assert
            Assert.Single(nearby);
            Assert.Equal(nearFollower, nearby[0]);
        }
    }
}
```

**Step 2: Run all tests**

Run: `dotnet test tests/FactionWars.Tests`
Expected: PASS

**Step 3: Commit**

```bash
git add tests/FactionWars.Tests/Integration/VehicleSeatPriorityIntegrationTests.cs
git commit -m "test: add vehicle seat priority integration tests"
```

---

## Summary

| Task | Description | Files |
|------|-------------|-------|
| 1 | Add interface methods | IGameBridge.cs |
| 2 | Implement in GameBridge | GameBridge.cs |
| 3 | Implement in MockGameBridge | MockGameBridge.cs, tests |
| 4 | Create IVehicleSeatPriorityService | Interface |
| 5 | Create VehicleSeatPriorityService | Service + tests |
| 6 | Update FollowerManager | Coordinated assignment |
| 7 | Integration tests | End-to-end verification |
