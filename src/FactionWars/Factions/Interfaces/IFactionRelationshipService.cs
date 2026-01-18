using FactionWars.Factions.Models;
using System.Collections.Generic;

namespace FactionWars.Factions.Interfaces
{
    /// <summary>
    /// Service interface for faction relationship business logic.
    /// Provides higher-level operations for managing relationships between factions.
    /// </summary>
    public interface IFactionRelationshipService
    {
        /// <summary>
        /// Gets the relationship between two factions.
        /// </summary>
        /// <param name="factionId1">The first faction ID.</param>
        /// <param name="factionId2">The second faction ID.</param>
        /// <returns>The relationship if found, null otherwise.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if either faction ID is null.</exception>
        FactionRelationship? GetRelationship(string factionId1, string factionId2);

        /// <summary>
        /// Gets the relationship value between two factions.
        /// </summary>
        /// <param name="factionId1">The first faction ID.</param>
        /// <param name="factionId2">The second faction ID.</param>
        /// <returns>The relationship value, or 0 (neutral) if no relationship exists.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if either faction ID is null.</exception>
        int GetRelationshipValue(string factionId1, string factionId2);

        /// <summary>
        /// Gets the relationship status between two factions.
        /// </summary>
        /// <param name="factionId1">The first faction ID.</param>
        /// <param name="factionId2">The second faction ID.</param>
        /// <returns>The relationship status, or Neutral if no relationship exists.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if either faction ID is null.</exception>
        RelationshipStatus GetRelationshipStatus(string factionId1, string factionId2);

        /// <summary>
        /// Sets the relationship value between two factions.
        /// Creates a new relationship if one doesn't exist.
        /// </summary>
        /// <param name="factionId1">The first faction ID.</param>
        /// <param name="factionId2">The second faction ID.</param>
        /// <param name="value">The new relationship value.</param>
        /// <returns>True if successful, false if either faction doesn't exist.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if either faction ID is null.</exception>
        bool SetRelationshipValue(string factionId1, string factionId2, int value);

        /// <summary>
        /// Adjusts the relationship value between two factions by a specified amount.
        /// Creates a new relationship if one doesn't exist.
        /// </summary>
        /// <param name="factionId1">The first faction ID.</param>
        /// <param name="factionId2">The second faction ID.</param>
        /// <param name="amount">The amount to adjust (positive or negative).</param>
        /// <returns>True if successful, false if either faction doesn't exist.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if either faction ID is null.</exception>
        bool AdjustRelationship(string factionId1, string factionId2, int amount);

        /// <summary>
        /// Gets all relationships for a specific faction.
        /// </summary>
        /// <param name="factionId">The faction ID to get relationships for.</param>
        /// <returns>An enumerable of all relationships involving the faction.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if factionId is null.</exception>
        IEnumerable<FactionRelationship> GetAllRelationshipsForFaction(string factionId);

        /// <summary>
        /// Gets all factions that are enemies (hostile or at war) with the specified faction.
        /// </summary>
        /// <param name="factionId">The faction ID to find enemies for.</param>
        /// <returns>An enumerable of enemy faction IDs.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if factionId is null.</exception>
        IEnumerable<string> GetEnemies(string factionId);

        /// <summary>
        /// Gets all factions that are allies (friendly or allied) with the specified faction.
        /// </summary>
        /// <param name="factionId">The faction ID to find allies for.</param>
        /// <returns>An enumerable of ally faction IDs.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if factionId is null.</exception>
        IEnumerable<string> GetAllies(string factionId);

        /// <summary>
        /// Checks if two factions are at war.
        /// </summary>
        /// <param name="factionId1">The first faction ID.</param>
        /// <param name="factionId2">The second faction ID.</param>
        /// <returns>True if the factions are at war, false otherwise.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if either faction ID is null.</exception>
        bool AreAtWar(string factionId1, string factionId2);

        /// <summary>
        /// Checks if two factions are allied.
        /// </summary>
        /// <param name="factionId1">The first faction ID.</param>
        /// <param name="factionId2">The second faction ID.</param>
        /// <returns>True if the factions are allied, false otherwise.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if either faction ID is null.</exception>
        bool AreAllied(string factionId1, string factionId2);

        /// <summary>
        /// Initializes relationships between all factions with the specified default value.
        /// Does not overwrite existing relationships.
        /// </summary>
        /// <param name="defaultValue">The default relationship value (default: 0 for neutral).</param>
        void InitializeAllRelationships(int defaultValue = 0);

        /// <summary>
        /// Declares war between two factions, setting their relationship to minimum.
        /// </summary>
        /// <param name="factionId1">The first faction ID.</param>
        /// <param name="factionId2">The second faction ID.</param>
        /// <returns>True if successful, false if either faction doesn't exist.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if either faction ID is null.</exception>
        bool DeclareWar(string factionId1, string factionId2);

        /// <summary>
        /// Forms an alliance between two factions, setting their relationship to maximum.
        /// </summary>
        /// <param name="factionId1">The first faction ID.</param>
        /// <param name="factionId2">The second faction ID.</param>
        /// <returns>True if successful, false if either faction doesn't exist.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if either faction ID is null.</exception>
        bool FormAlliance(string factionId1, string factionId2);

        /// <summary>
        /// Makes peace between two factions, setting their relationship to neutral.
        /// </summary>
        /// <param name="factionId1">The first faction ID.</param>
        /// <param name="factionId2">The second faction ID.</param>
        /// <returns>True if successful, false if either faction doesn't exist.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if either faction ID is null.</exception>
        bool MakePeace(string factionId1, string factionId2);
    }
}
