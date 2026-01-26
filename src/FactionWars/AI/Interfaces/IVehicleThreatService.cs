using FactionWars.AI.Models;

namespace FactionWars.AI.Interfaces
{
    /// <summary>
    /// Service for classifying vehicle threats and determining RPG response requirements.
    /// Used by AI troop purchasing to respond appropriately to vehicle threats.
    /// </summary>
    public interface IVehicleThreatService
    {
        /// <summary>
        /// Gets the threat level for a given vehicle model.
        /// </summary>
        /// <param name="vehicleModelName">The GTA V vehicle model name (case-insensitive).</param>
        /// <returns>The threat level classification for the vehicle.</returns>
        VehicleThreatLevel GetThreatLevel(string vehicleModelName);

        /// <summary>
        /// Gets the required number of RPG units to respond to a threat level.
        /// </summary>
        /// <param name="threatLevel">The vehicle threat level.</param>
        /// <returns>The number of RPG units required (0 for None, 1 for Light, 2 for Heavy).</returns>
        int GetRequiredRpgCount(VehicleThreatLevel threatLevel);
    }
}
