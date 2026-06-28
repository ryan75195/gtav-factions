using System;
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
    }
}
