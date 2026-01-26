using FactionWars.AI.Models;

namespace FactionWars.AI.Interfaces
{
    /// <summary>
    /// Service for responding to vehicle threats by deploying Elite/RPG units.
    /// When enemy vehicles are detected in a battle zone, this service coordinates
    /// the deployment of anti-vehicle troops from the faction's reserve pool or
    /// through emergency purchases.
    /// </summary>
    public interface IAntiVehicleResponseService
    {
        /// <summary>
        /// Called when vehicles are detected in a battle zone.
        /// Returns the number of Elite units deployed as response.
        /// </summary>
        /// <param name="factionId">The faction responding to the vehicle threat.</param>
        /// <param name="zoneId">The zone where the threat was detected.</param>
        /// <param name="threatLevel">The level of vehicle threat detected.</param>
        /// <returns>The number of Elite units deployed (0 if no response possible).</returns>
        int RespondToVehicleThreat(string factionId, string zoneId, VehicleThreatLevel threatLevel);
    }
}
