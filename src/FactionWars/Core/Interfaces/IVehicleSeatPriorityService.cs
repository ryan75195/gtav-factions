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
