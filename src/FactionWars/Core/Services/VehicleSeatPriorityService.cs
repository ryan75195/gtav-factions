using FactionWars.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

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
                var distance = pedPos.DistanceTo(vehiclePos);

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
