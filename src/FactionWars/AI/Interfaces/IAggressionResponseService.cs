using System.Collections.Generic;
using FactionWars.AI.Models;

namespace FactionWars.AI.Interfaces
{
    /// <summary>
    /// Interface for the aggression response service.
    /// Tracks aggression against AI factions and determines appropriate responses.
    /// This enables AI factions to react dynamically to player attacks.
    /// </summary>
    public interface IAggressionResponseService
    {
        /// <summary>
        /// Records an act of aggression against a faction's territory.
        /// </summary>
        /// <param name="aggressorId">The ID of the attacker (player or faction).</param>
        /// <param name="targetZoneId">The ID of the zone being attacked.</param>
        /// <param name="damage">The amount of damage dealt (troops killed, control lost, etc.).</param>
        /// <exception cref="System.ArgumentNullException">Thrown if aggressorId or targetZoneId is null.</exception>
        /// <exception cref="System.ArgumentException">Thrown if aggressorId or targetZoneId is empty.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">Thrown if damage is negative.</exception>
        void RecordAggression(string aggressorId, string targetZoneId, int damage);

        /// <summary>
        /// Gets the current threat level from a specific aggressor to a defender.
        /// </summary>
        /// <param name="aggressorId">The ID of the potential aggressor.</param>
        /// <param name="defenderId">The ID of the defending faction.</param>
        /// <returns>A threat level between 0 (no threat) and 1 (maximum threat).</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if aggressorId or defenderId is null.</exception>
        float GetThreatLevel(string aggressorId, string defenderId);

        /// <summary>
        /// Gets the AI's response to aggression from a specific attacker.
        /// </summary>
        /// <param name="context">The current AI context for the defending faction.</param>
        /// <param name="aggressorId">The ID of the aggressor to respond to.</param>
        /// <returns>An aggression response with type and decisions.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if context or aggressorId is null.</exception>
        AggressionResponse GetAggressionResponse(AIContext context, string aggressorId);

        /// <summary>
        /// Decays all threat levels over time.
        /// </summary>
        /// <param name="decayRate">The rate of decay (0-1), where 1 means full decay.</param>
        /// <exception cref="System.ArgumentOutOfRangeException">Thrown if decayRate is outside 0-1 range.</exception>
        void DecayThreatLevels(float decayRate);

        /// <summary>
        /// Gets a list of recent aggressions against a faction.
        /// </summary>
        /// <param name="defenderId">The ID of the defending faction.</param>
        /// <returns>A list of recent aggression records.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if defenderId is null.</exception>
        IList<AggressionRecord> GetRecentAggressions(string defenderId);

        /// <summary>
        /// Clears all aggression history for a specific aggressor.
        /// </summary>
        /// <param name="aggressorId">The ID of the aggressor to clear history for.</param>
        /// <exception cref="System.ArgumentNullException">Thrown if aggressorId is null.</exception>
        void ClearAggressionHistory(string aggressorId);

        /// <summary>
        /// Checks if a faction is currently under attack (has recent unresolved aggression).
        /// </summary>
        /// <param name="defenderId">The ID of the faction to check.</param>
        /// <returns>True if the faction is under attack, false otherwise.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if defenderId is null.</exception>
        bool IsUnderAttack(string defenderId);

        /// <summary>
        /// Gets the primary threat to a faction (the aggressor with highest threat level).
        /// </summary>
        /// <param name="defenderId">The ID of the defending faction.</param>
        /// <returns>The ID of the primary threat, or null if no threats exist.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if defenderId is null.</exception>
        string? GetPrimaryThreat(string defenderId);
    }
}
