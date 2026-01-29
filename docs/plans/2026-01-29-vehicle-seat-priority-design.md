# Vehicle Seat Priority Design

**Goal:** Smart seat assignment for followers entering vehicles - prioritize turrets and optimal seats by vehicle type, with coordinated direct assignment.

**Architecture:** A `VehicleSeatPriorityService` determines seat priority based on vehicle class. `FollowerManager` assigns all nearby followers to specific seats simultaneously, eliminating seat competition.

---

## Seat Priority Rules

| Vehicle Class | Seat Priority Order | Reason |
|---------------|---------------------|--------|
| **Turret vehicles** (Technical, Insurgent Pickup) | Turret → Back → Front passenger | Gun seat is most valuable |
| **Helicopters** | Back seats → Front passenger | Better firing angles, no rotor obstruction |
| **Regular cars/SUVs** | Back seats → Front passenger | Can shoot out rear windows |
| **Boats with guns** | Gun seat → Other seats | Mounted weapon priority |
| **Motorcycles** | Passenger seat only | Only 1 passenger spot |
| **Default fallback** | Any available seat | For unknown/edge cases |

---

## Detection Approach

- Use `GET_VEHICLE_CLASS` native (returns int: 8=motorcycle, 14=boat, 15=helicopter, etc.)
- Use `IS_TURRET_SEAT` to detect gun seats within any class
- Combine both: check class first, then check for turrets within that class

---

## Coordinated Direct Assignment

**Current behavior (problem):**
- Loop through followers, each grabs "next free seat"
- Can cause seat-fighting if multiple followers try same seat

**New behavior:**
1. Player enters vehicle
2. Get all free seats, sorted by priority for this vehicle class
3. Filter to followers within 15 meters of vehicle
4. Calculate assignments: Follower[0]→Seat[0], Follower[1]→Seat[1], etc.
5. Task ALL followers to their assigned seats simultaneously
6. Each follower goes directly to their seat, no competition

**Followers outside 15m range:** Stay on foot, continue their current task.

---

## Code Structure

### New Types

```csharp
public enum VehicleSeatType
{
    Turret,      // Mounted gun
    BackSeat,    // Rear passenger
    FrontSeat,   // Front passenger
    Other        // Fallback
}
```

### IGameBridge Additions

```csharp
// Get vehicle class (car, helicopter, boat, etc.)
int GetVehicleClass(int vehicleHandle);

// Check if seat is a turret
bool IsVehicleSeatTurret(int vehicleHandle, int seatIndex);
```

### New Service: VehicleSeatPriorityService

```csharp
public class VehicleSeatPriorityService
{
    public int[] GetPrioritizedSeats(int vehicleHandle, int[] freeSeats)
    {
        var vehicleClass = _gameBridge.GetVehicleClass(vehicleHandle);

        return vehicleClass switch
        {
            15 => SortForHelicopter(freeSeats),           // Helicopter
            14 => SortForBoat(freeSeats, vehicleHandle),  // Boat
            _  => SortDefault(freeSeats, vehicleHandle)   // Cars, etc.
        };
    }
}
```

---

## Files to Modify

- `src/FactionWars/Core/Interfaces/IGameBridge.cs` - Add new methods
- `src/FactionWars/ScriptHookV/GameBridge.cs` - Implement natives
- `src/FactionWars/Core/Utils/MockGameBridge.cs` - Mock implementation
- `src/FactionWars/ScriptHookV/Managers/FollowerManager.cs` - Use coordinated assignment

## Files to Create

- `src/FactionWars/Core/Services/VehicleSeatPriorityService.cs` - Seat sorting logic
- `src/FactionWars/Core/Interfaces/IVehicleSeatPriorityService.cs` - Interface

---

## GTA V Vehicle Class Reference

| Class ID | Type |
|----------|------|
| 0 | Compacts |
| 1 | Sedans |
| 2 | SUVs |
| 3 | Coupes |
| 4 | Muscle |
| 5 | Sports Classics |
| 6 | Sports |
| 7 | Super |
| 8 | Motorcycles |
| 9 | Off-road |
| 10 | Industrial |
| 11 | Utility |
| 12 | Vans |
| 13 | Cycles |
| 14 | Boats |
| 15 | Helicopters |
| 16 | Planes |
| 17 | Service |
| 18 | Emergency |
| 19 | Military |
| 20 | Commercial |
| 21 | Trains |
