using FactionWars.Loyalty.Models;
using System.Collections.Generic;

namespace FactionWars.Loyalty.Interfaces
{
    /// <summary>
    /// Repository for managing zone integration states.
    /// </summary>
    public interface IZoneIntegrationRepository
    {
        /// <summary>
        /// Adds a new zone integration state to the repository.
        /// </summary>
        /// <param name="state">The integration state to add.</param>
        void Add(ZoneIntegrationState state);

        /// <summary>
        /// Gets a zone integration state by zone ID.
        /// </summary>
        /// <param name="zoneId">The zone ID.</param>
        /// <returns>The integration state, or null if not found.</returns>
        ZoneIntegrationState? GetByZoneId(string zoneId);

        /// <summary>
        /// Removes a zone integration state from the repository.
        /// </summary>
        /// <param name="zoneId">The zone ID to remove.</param>
        /// <returns>True if removed, false if not found.</returns>
        bool Remove(string zoneId);

        /// <summary>
        /// Gets all zone integration states.
        /// </summary>
        /// <returns>All integration states.</returns>
        IEnumerable<ZoneIntegrationState> GetAll();

        /// <summary>
        /// Gets all zone integration states for a specific faction.
        /// </summary>
        /// <param name="factionId">The faction ID.</param>
        /// <returns>Integration states for the faction.</returns>
        IEnumerable<ZoneIntegrationState> GetByFaction(string factionId);

        /// <summary>
        /// Gets all integration states that are not yet complete.
        /// </summary>
        /// <returns>Pending integration states.</returns>
        IEnumerable<ZoneIntegrationState> GetPendingIntegration();

        /// <summary>
        /// Updates an existing zone integration state.
        /// </summary>
        /// <param name="state">The state to update.</param>
        void Update(ZoneIntegrationState state);
    }
}
