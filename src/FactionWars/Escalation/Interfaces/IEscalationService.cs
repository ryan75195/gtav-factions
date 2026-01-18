using System.Collections.Generic;
using FactionWars.Escalation.Models;

namespace FactionWars.Escalation.Interfaces
{
    /// <summary>
    /// Service interface for managing faction escalation levels.
    /// Provides business logic operations for escalation tier management.
    /// </summary>
    public interface IEscalationService
    {
        /// <summary>
        /// Gets the escalation record for a faction.
        /// </summary>
        /// <param name="factionId">The faction ID.</param>
        /// <returns>The escalation record, or null if not found.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if factionId is null.</exception>
        /// <exception cref="System.ArgumentException">Thrown if factionId is empty or whitespace.</exception>
        FactionEscalation? GetEscalation(string factionId);

        /// <summary>
        /// Gets or creates an escalation record for a faction.
        /// </summary>
        /// <param name="factionId">The faction ID.</param>
        /// <returns>The existing or newly created escalation record.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if factionId is null.</exception>
        FactionEscalation GetOrCreateEscalation(string factionId);

        /// <summary>
        /// Gets the current escalation tier for a faction.
        /// </summary>
        /// <param name="factionId">The faction ID.</param>
        /// <returns>The current tier, or Tier1 if faction not found.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if factionId is null.</exception>
        EscalationTier GetCurrentTier(string factionId);

        /// <summary>
        /// Adds escalation points to a faction.
        /// </summary>
        /// <param name="factionId">The faction ID.</param>
        /// <param name="amount">The amount of points to add (must be non-negative).</param>
        /// <returns>A result indicating success/failure and whether the tier changed.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if factionId is null.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">Thrown if amount is negative.</exception>
        EscalationPointsResult AddEscalationPoints(string factionId, int amount);

        /// <summary>
        /// Removes escalation points from a faction.
        /// </summary>
        /// <param name="factionId">The faction ID.</param>
        /// <param name="amount">The amount of points to remove (must be non-negative).</param>
        /// <returns>A result indicating success/failure and whether the tier changed.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if factionId is null.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">Thrown if amount is negative.</exception>
        EscalationPointsResult RemoveEscalationPoints(string factionId, int amount);

        /// <summary>
        /// Sets the escalation points for a faction to a specific value.
        /// </summary>
        /// <param name="factionId">The faction ID.</param>
        /// <param name="points">The new points value.</param>
        /// <returns>True if set successfully, false if faction not found.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if factionId is null.</exception>
        bool SetEscalationPoints(string factionId, int points);

        /// <summary>
        /// Gets all escalation records.
        /// </summary>
        /// <returns>All escalation records.</returns>
        IEnumerable<FactionEscalation> GetAllEscalations();

        /// <summary>
        /// Gets all factions at a specific escalation tier.
        /// </summary>
        /// <param name="tier">The tier to filter by.</param>
        /// <returns>Escalation records for factions at that tier.</returns>
        IEnumerable<FactionEscalation> GetFactionsAtTier(EscalationTier tier);

        /// <summary>
        /// Gets the progress percentage towards the next tier.
        /// </summary>
        /// <param name="factionId">The faction ID.</param>
        /// <returns>Progress percentage (0-100), or 0 if faction not found.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if factionId is null.</exception>
        float GetProgressToNextTier(string factionId);

        /// <summary>
        /// Gets the number of points needed to reach the next tier.
        /// </summary>
        /// <param name="factionId">The faction ID.</param>
        /// <returns>Points needed, 0 if at max tier, or int.MaxValue if faction not found.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if factionId is null.</exception>
        int GetPointsToNextTier(string factionId);

        /// <summary>
        /// Resets a faction's escalation to zero points.
        /// </summary>
        /// <param name="factionId">The faction ID.</param>
        /// <returns>True if reset successfully, false if faction not found.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if factionId is null.</exception>
        bool ResetEscalation(string factionId);

        /// <summary>
        /// Initializes escalation for a new faction.
        /// </summary>
        /// <param name="factionId">The faction ID.</param>
        /// <param name="initialPoints">Optional initial points (default 0).</param>
        /// <returns>True if initialized successfully, false if already exists.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if factionId is null.</exception>
        bool InitializeEscalation(string factionId, int initialPoints = 0);

        /// <summary>
        /// Removes the escalation record for a faction.
        /// </summary>
        /// <param name="factionId">The faction ID.</param>
        /// <returns>True if removed successfully, false if not found.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if factionId is null.</exception>
        bool RemoveEscalation(string factionId);
    }
}
