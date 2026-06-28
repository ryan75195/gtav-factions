using System;
using FactionWars.Core.Interfaces;
using FactionWars.ScriptHookV.Logging;
using GTA;
using GTA.Native;
using DomainVector3 = FactionWars.Core.Interfaces.Vector3;
using DomainBlipColor = FactionWars.Core.Interfaces.BlipColor;

namespace FactionWars.ScriptHookV
{
    public partial class GameBridge
    {
        public void SetPedAsFollower(int pedHandle)
        {
            try
            {
                var ped = Entity.FromHandle(pedHandle) as Ped;
                if (ped == null || !ped.Exists())
                {
                    FileLogger.Error($"SetPedAsFollower: Ped {pedHandle} doesn't exist");
                    return;
                }

                var player = Game.Player.Character;
                if (player == null || !player.Exists())
                {
                    FileLogger.Error("SetPedAsFollower: Player doesn't exist");
                    return;
                }

                // Clear any existing tasks first
                ped.Task.ClearAllImmediately();

                // Set relationship to be friendly with the player
                var playerGroup = player.RelationshipGroup;
                ped.RelationshipGroup = playerGroup;

                // Ensure followers hate the defender enemies group (used by hostile zone defenders)
                var defenderEnemyGroup = World.AddRelationshipGroup("DEFENDER_ENEMIES");
                playerGroup.SetRelationshipBetweenGroups(defenderEnemyGroup, Relationship.Hate, true);

                AddPedToPlayerGroup(player, ped, pedHandle);

                ConfigureFollowerCombat(ped);
            }
            catch (Exception ex)
            {
                FileLogger.Error($"SetPedAsFollower exception", ex);
            }
        }

        /// <inheritdoc />
        public bool IsPlayerInVehicle()
        {
            try
            {
                var player = Game.Player.Character;
                return player != null && player.Exists() && player.IsInVehicle();
            }
            catch
            {
                return false;
            }
        }

        /// <inheritdoc />
        public int GetPlayerVehicle()
        {
            try
            {
                var player = Game.Player.Character;
                if (player == null || !player.Exists() || !player.IsInVehicle())
                    return -1;

                var vehicle = player.CurrentVehicle;
                return vehicle?.Handle ?? -1;
            }
            catch
            {
                return -1;
            }
        }

        /// <inheritdoc />
        public bool IsPedInVehicle(int pedHandle)
        {
            try
            {
                var ped = Entity.FromHandle(pedHandle) as Ped;
                return ped != null && ped.Exists() && ped.IsInVehicle();
            }
            catch
            {
                return false;
            }
        }

        /// <inheritdoc />
        public bool IsPedTryingToEnterVehicle(int pedHandle)
        {
            try
            {
                var ped = Entity.FromHandle(pedHandle) as Ped;
                if (ped == null || !ped.Exists()) return false;

                // Check if ped is getting into a vehicle using native
                return Function.Call<bool>(Hash.IS_PED_GETTING_INTO_A_VEHICLE, ped.Handle);
            }
            catch
            {
                return false;
            }
        }

        /// <inheritdoc />
        public int[] GetVehicleFreeSeats(int vehicleHandle)
        {
            try
            {
                var vehicle = Entity.FromHandle(vehicleHandle) as Vehicle;
                if (vehicle == null || !vehicle.Exists())
                    return Array.Empty<int>();

                var freeSeats = new System.Collections.Generic.List<int>();
                var passengerCapacity = vehicle.PassengerCapacity;

                // Check passenger seats (indices 0-N, but index -1 is driver in GTA V native calls)
                // In our abstraction, 0 = driver, 1+ = passengers
                // Note: driver seat index in natives is -1, passenger seats are 0-based
                for (int i = 0; i < passengerCapacity; i++)
                {
                    if (vehicle.IsSeatFree((VehicleSeat)i))
                    {
                        // Map GTA V seat index (0-based passengers) to our abstraction (1-based for passengers)
                        freeSeats.Add(i + 1);
                    }
                }

                return freeSeats.ToArray();
            }
            catch
            {
                return Array.Empty<int>();
            }
        }

        /// <inheritdoc />
        public int GetVehicleClass(int vehicleHandle)
        {
            try
            {
                var vehicle = Entity.FromHandle(vehicleHandle) as Vehicle;
                if (vehicle == null || !vehicle.Exists())
                    return -1;

                return Function.Call<int>(Hash.GET_VEHICLE_CLASS, vehicleHandle);
            }
            catch (Exception ex)
            {
                FileLogger.Error("GetVehicleClass error", ex);
                return -1;
            }
        }

        /// <inheritdoc />
        public bool IsVehicleSeatTurret(int vehicleHandle, int seatIndex)
        {
            try
            {
                var vehicle = Entity.FromHandle(vehicleHandle) as Vehicle;
                if (vehicle == null || !vehicle.Exists())
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

        /// <inheritdoc />
        public DomainVector3 GetVehiclePosition(int vehicleHandle)
        {
            try
            {
                var vehicle = Entity.FromHandle(vehicleHandle) as Vehicle;
                if (vehicle == null || !vehicle.Exists())
                    return DomainVector3.Zero;

                var pos = vehicle.Position;
                return new DomainVector3(pos.X, pos.Y, pos.Z);
            }
            catch (Exception ex)
            {
                FileLogger.Error("GetVehiclePosition error", ex);
                return DomainVector3.Zero;
            }
        }

        /// <inheritdoc />
        public void TaskPedEnterVehicle(int pedHandle, int vehicleHandle, int seatIndex)
        {
            try
            {
                var ped = Entity.FromHandle(pedHandle) as Ped;
                var vehicle = Entity.FromHandle(vehicleHandle) as Vehicle;

                if (ped == null || !ped.Exists() || vehicle == null || !vehicle.Exists())
                    return;

                // Convert our seat index (1-based for passengers) to GTA V seat index (0-based)
                // Our 0 = driver (-1 in GTA), our 1+ = passengers (0+ in GTA)
                var gtaSeatIndex = seatIndex - 1;

                // Use native for better control - TASK_ENTER_VEHICLE with flags
                // Flags: 1 = warp if failed, 8 = don't wait for door to open, 16 = warp in
                // Timeout -1 = no timeout
                // Speed: 1.0 = walk, 2.0 = run, 3.0 = sprint. Followers should hustle to the car.
                const float SprintSpeed = 3.0f;
                Function.Call(Hash.TASK_ENTER_VEHICLE,
                    ped.Handle,
                    vehicle.Handle,
                    -1,           // timeout - no timeout
                    gtaSeatIndex, // seat
                    SprintSpeed,  // speed (sprint)
                    1,            // flag: 1 = normal entry
                    0);           // unknown

                FileLogger.AI($"TaskPedEnterVehicle: ped {pedHandle} -> vehicle {vehicleHandle} seat {gtaSeatIndex} speed {SprintSpeed:F1} (sprint)");
            }
            catch (Exception ex)
            {
                FileLogger.Error($"TaskPedEnterVehicle exception for ped {pedHandle}", ex);
            }
        }

        /// <inheritdoc />
    }
}
