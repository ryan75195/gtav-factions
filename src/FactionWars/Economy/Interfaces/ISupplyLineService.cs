using System.Collections.Generic;
using FactionWars.Territory.Models;

namespace FactionWars.Economy.Interfaces
{
    /// <summary>
    /// Service interface for managing supply line connectivity between zones.
    /// Supply lines determine resource efficiency based on territorial connectivity to headquarters.
    /// </summary>
    public interface ISupplyLineService
    {
        /// <summary>
        /// Sets the headquarters zone for a faction.
        /// The headquarters is the central point from which supply lines are calculated.
        /// </summary>
        /// <param name="factionId">The faction ID to set headquarters for.</param>
        /// <param name="zoneId">The zone ID to designate as headquarters.</param>
        /// <returns>True if headquarters was set, false if zone doesn't exist or isn't owned by faction.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if factionId or zoneId is null.</exception>
        /// <exception cref="System.ArgumentException">Thrown if factionId or zoneId is empty or whitespace.</exception>
        bool SetHeadquarters(string factionId, string zoneId);

        /// <summary>
        /// Gets the headquarters zone ID for a faction.
        /// </summary>
        /// <param name="factionId">The faction ID to get headquarters for.</param>
        /// <returns>The zone ID of the headquarters, or null if no headquarters is set.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if factionId is null.</exception>
        /// <exception cref="System.ArgumentException">Thrown if factionId is empty or whitespace.</exception>
        string? GetHeadquarters(string factionId);

        /// <summary>
        /// Clears the headquarters for a faction.
        /// </summary>
        /// <param name="factionId">The faction ID to clear headquarters for.</param>
        /// <exception cref="System.ArgumentNullException">Thrown if factionId is null.</exception>
        void ClearHeadquarters(string factionId);

        /// <summary>
        /// Checks if a zone is connected to the faction's headquarters via owned territory.
        /// </summary>
        /// <param name="factionId">The faction ID to check for.</param>
        /// <param name="zoneId">The zone ID to check connectivity for.</param>
        /// <returns>True if zone is connected to HQ or if no HQ is set, false if disconnected.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if factionId or zoneId is null.</exception>
        bool IsConnectedToHeadquarters(string factionId, string zoneId);

        /// <summary>
        /// Gets the supply line efficiency multiplier for a zone.
        /// Connected zones get full efficiency (1.0), disconnected zones get reduced efficiency.
        /// </summary>
        /// <param name="factionId">The faction ID to calculate efficiency for.</param>
        /// <param name="zoneId">The zone ID to calculate efficiency for.</param>
        /// <returns>Efficiency multiplier between 0 and 1.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if factionId or zoneId is null.</exception>
        float GetSupplyLineEfficiency(string factionId, string zoneId);

        /// <summary>
        /// Gets all zones that are connected to headquarters for a faction.
        /// </summary>
        /// <param name="factionId">The faction ID to get connected zones for.</param>
        /// <returns>Collection of connected zones including HQ. Empty if no HQ set.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if factionId is null.</exception>
        IEnumerable<Zone> GetConnectedZones(string factionId);

        /// <summary>
        /// Gets all zones that are disconnected from headquarters for a faction.
        /// </summary>
        /// <param name="factionId">The faction ID to get disconnected zones for.</param>
        /// <returns>Collection of disconnected zones. Empty if no HQ set or all connected.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if factionId is null.</exception>
        IEnumerable<Zone> GetDisconnectedZones(string factionId);

        /// <summary>
        /// Checks if a zone has an active supply line (connected to HQ or no HQ requirement).
        /// </summary>
        /// <param name="factionId">The faction ID to check for.</param>
        /// <param name="zoneId">The zone ID to check.</param>
        /// <returns>True if zone has supply line, false otherwise.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if factionId or zoneId is null.</exception>
        bool HasSupplyLine(string factionId, string zoneId);
    }
}
