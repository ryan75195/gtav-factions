using System;
using FactionWars.ScriptHookV.Logging;
using GTA;
using GTA.Native;
using DomainVector3 = FactionWars.Core.Interfaces.Vector3;

namespace FactionWars.ScriptHookV
{
    public partial class GameBridge
    {
        /// <inheritdoc />
        public void TaskVehicleDriveToCoord(int vehicleHandle, DomainVector3 dest, float speed, float stopRange)
        {
            try
            {
                var vehicle = Entity.FromHandle(vehicleHandle) as Vehicle;
                if (vehicle == null || !vehicle.Exists()) return;

                // Driver is the ped in seat -1 (driver). TASK_VEHICLE_DRIVE_TO_COORD_LONGRANGE is built
                // for driving across long distances toward a point and stopping near it.
                var driver = Function.Call<int>(Hash.GET_PED_IN_VEHICLE_SEAT, vehicle.Handle, -1);
                if (driver == 0) return;

                const int drivingStyle = 786603; // normal + avoid obstacles/vehicles, stop at lights
                Function.Call(Hash.TASK_VEHICLE_DRIVE_TO_COORD_LONGRANGE,
                    driver, vehicle.Handle, dest.X, dest.Y, dest.Z, speed, drivingStyle, stopRange);

                FileLogger.AI($"TaskVehicleDriveToCoord: vehicle {vehicleHandle} -> ({dest.X:F0},{dest.Y:F0},{dest.Z:F0}) speed {speed:F0} stop {stopRange:F0}");
            }
            catch (Exception ex)
            {
                FileLogger.Error($"TaskVehicleDriveToCoord exception for vehicle {vehicleHandle}", ex);
            }
        }
    }
}
