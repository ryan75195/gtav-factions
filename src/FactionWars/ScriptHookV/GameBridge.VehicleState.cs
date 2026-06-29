using System;
using System.Collections.Generic;
using FactionWars.Core.Interfaces;
using FactionWars.ScriptHookV.Logging;
using GTA;
using GTA.Native;

namespace FactionWars.ScriptHookV
{
    public partial class GameBridge
    {
        public void SetPlayerIntoVehicle(int vehicleHandle, int seatIndex)
        {
            try
            {
                var player = Game.Player.Character;
                if (player == null || !player.Exists())
                    return;

                SetPedIntoVehicle(player.Handle, vehicleHandle, seatIndex);
            }
            catch (Exception ex)
            {
                FileLogger.Error("SetPlayerIntoVehicle exception", ex);
            }
        }

        public int GetPedVehicle(int pedHandle)
        {
            try
            {
                var ped = Entity.FromHandle(pedHandle) as Ped;
                if (ped == null || !ped.Exists() || !ped.IsInVehicle())
                    return -1;

                return ped.CurrentVehicle?.Handle ?? -1;
            }
            catch
            {
                return -1;
            }
        }

        public int GetPedVehicleSeat(int pedHandle)
        {
            try
            {
                var ped = Entity.FromHandle(pedHandle) as Ped;
                var vehicle = ped?.CurrentVehicle;
                if (ped == null || !ped.Exists() || vehicle == null || !vehicle.Exists())
                    return -1;

                for (var seat = -1; seat < vehicle.PassengerCapacity; seat++)
                {
                    if (vehicle.GetPedOnSeat((VehicleSeat)seat)?.Handle == pedHandle)
                    {
                        return seat + 1;
                    }
                }

                return -1;
            }
            catch
            {
                return -1;
            }
        }

        public void SetPedIntoVehicle(int pedHandle, int vehicleHandle, int seatIndex)
        {
            try
            {
                var ped = Entity.FromHandle(pedHandle) as Ped;
                var vehicle = Entity.FromHandle(vehicleHandle) as Vehicle;
                if (ped == null || !ped.Exists() || vehicle == null || !vehicle.Exists())
                    return;

                var gtaSeatIndex = seatIndex - 1;
                Function.Call(Hash.SET_PED_INTO_VEHICLE, ped.Handle, vehicle.Handle, gtaSeatIndex);
            }
            catch (Exception ex)
            {
                FileLogger.Error($"SetPedIntoVehicle exception for ped {pedHandle}", ex);
            }
        }

        public float GetVehicleHeading(int vehicleHandle)
        {
            try
            {
                var vehicle = Entity.FromHandle(vehicleHandle) as Vehicle;
                return vehicle != null && vehicle.Exists() ? vehicle.Heading : 0f;
            }
            catch
            {
                return 0f;
            }
        }

        public void SetVehicleHeading(int vehicleHandle, float heading)
        {
            try
            {
                var vehicle = Entity.FromHandle(vehicleHandle) as Vehicle;
                if (vehicle != null && vehicle.Exists())
                    vehicle.Heading = heading;
            }
            catch (Exception ex)
            {
                FileLogger.Error($"SetVehicleHeading exception for vehicle {vehicleHandle}", ex);
            }
        }

        // SHVDN's World.GetNearbyVehicles enumerates the entity pool by pattern-scanning game memory
        // for the script-GUID pool. That scan breaks after GTA updates and throws a
        // NullReferenceException deep inside SHVDN (FwScriptGuidPoolTask.Run), so every scan returned
        // an empty list and no ambient cars were ever evicted. Bypass the broken pool entirely with
        // the GET_CLOSEST_VEHICLE native, which returns real game handles. It only yields the ONE
        // nearest vehicle per query, so a sparse sample collapses to a handful of cars and the rest
        // keep driving. Instead, grid the scan disc and query a small LOCAL radius at each node, so
        // each road segment surfaces its own nearest vehicle. Flag 70 with model 0 selects non-police
        // cars and motorbikes only (no police/helis/boats), matching the "just pedestrian cars" intent.
        public int[] GetNearbyVehicles(Vector3 center, float radius)
        {
            var found = new HashSet<int>();
            try
            {
                // Node spacing; the local query radius overlaps neighbours so no car between nodes is
                // missed (any point in the disc is within GridStep of some node).
                const float gridStep = 18f;
                float r2 = radius * radius;
                for (float dx = -radius; dx <= radius; dx += gridStep)
                {
                    for (float dy = -radius; dy <= radius; dy += gridStep)
                    {
                        if (dx * dx + dy * dy > r2) continue; // keep the sample area a disc, not a square
                        AddClosestVehicle(found, center.X + dx, center.Y + dy, center.Z, gridStep);
                    }
                }
            }
            catch (Exception ex)
            {
                FileLogger.Error($"GetNearbyVehicles exception at ({center.X:F0},{center.Y:F0})", ex);
            }

            var result = new int[found.Count];
            found.CopyTo(result);
            return result;
        }

        private static void AddClosestVehicle(HashSet<int> set, float x, float y, float z, float radius)
        {
            // GET_CLOSEST_VEHICLE(x, y, z, radius, modelHash, flags); modelHash 0 = any model that
            // matches the flags. Returns 0 when none is found.
            int handle = Function.Call<int>(Hash.GET_CLOSEST_VEHICLE, x, y, z, radius, 0, 70);
            if (handle != 0)
            {
                set.Add(handle);
            }
        }

        public int GetVehicleDriver(int vehicleHandle)
        {
            try
            {
                var vehicle = Entity.FromHandle(vehicleHandle) as Vehicle;
                if (vehicle == null || !vehicle.Exists())
                    return -1;

                var driver = vehicle.Driver;
                return driver != null && driver.Exists() && !driver.IsDead ? driver.Handle : -1;
            }
            catch
            {
                return -1;
            }
        }

        public bool IsVehiclePersistent(int vehicleHandle)
        {
            try
            {
                var vehicle = Entity.FromHandle(vehicleHandle) as Vehicle;
                return vehicle != null && vehicle.Exists() && vehicle.IsPersistent;
            }
            catch
            {
                return false;
            }
        }

        public void SetVehicleHandbrake(int vehicleHandle, bool engaged)
        {
            try
            {
                Function.Call(Hash.SET_VEHICLE_HANDBRAKE, vehicleHandle, engaged);
            }
            catch (Exception ex)
            {
                FileLogger.Error($"SetVehicleHandbrake exception for vehicle {vehicleHandle}", ex);
            }
        }
    }
}
