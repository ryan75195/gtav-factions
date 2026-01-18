using System.Collections.Generic;
using FactionWars.Escalation.Models;

namespace FactionWars.Escalation.Interfaces
{
    /// <summary>
    /// Repository interface for managing faction escalation data.
    /// </summary>
    public interface IEscalationRepository
    {
        /// <summary>
        /// Adds a new escalation record.
        /// </summary>
        /// <param name="escalation">The escalation to add.</param>
        /// <returns>True if added successfully, false if already exists.</returns>
        bool Add(FactionEscalation escalation);

        /// <summary>
        /// Gets the escalation for a faction.
        /// </summary>
        /// <param name="factionId">The faction ID.</param>
        /// <returns>The escalation record, or null if not found.</returns>
        FactionEscalation? GetByFactionId(string factionId);

        /// <summary>
        /// Gets all escalation records.
        /// </summary>
        /// <returns>All escalation records.</returns>
        IEnumerable<FactionEscalation> GetAll();

        /// <summary>
        /// Updates an existing escalation record.
        /// </summary>
        /// <param name="escalation">The escalation to update.</param>
        /// <returns>True if updated successfully, false if not found.</returns>
        bool Update(FactionEscalation escalation);

        /// <summary>
        /// Removes an escalation record.
        /// </summary>
        /// <param name="factionId">The faction ID.</param>
        /// <returns>True if removed successfully, false if not found.</returns>
        bool Remove(string factionId);

        /// <summary>
        /// Checks if an escalation exists for a faction.
        /// </summary>
        /// <param name="factionId">The faction ID.</param>
        /// <returns>True if exists, false otherwise.</returns>
        bool Exists(string factionId);

        /// <summary>
        /// Gets or creates an escalation for a faction.
        /// </summary>
        /// <param name="factionId">The faction ID.</param>
        /// <returns>The existing or newly created escalation.</returns>
        FactionEscalation GetOrCreate(string factionId);

        /// <summary>
        /// Gets all factions at a specific escalation tier.
        /// </summary>
        /// <param name="tier">The tier to filter by.</param>
        /// <returns>Escalation records at the specified tier.</returns>
        IEnumerable<FactionEscalation> GetByTier(EscalationTier tier);

        /// <summary>
        /// Clears all escalation records.
        /// </summary>
        void Clear();
    }
}
