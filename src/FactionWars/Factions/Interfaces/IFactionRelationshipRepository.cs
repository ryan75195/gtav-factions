using FactionWars.Factions.Models;
using System.Collections.Generic;

namespace FactionWars.Factions.Interfaces
{
    /// <summary>
    /// Repository interface for faction relationship data access.
    /// Provides CRUD operations for relationships between factions.
    /// </summary>
    public interface IFactionRelationshipRepository
    {
        /// <summary>
        /// Gets the total number of relationships in the repository.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Adds a new relationship to the repository.
        /// </summary>
        /// <param name="relationship">The relationship to add.</param>
        /// <exception cref="System.ArgumentNullException">Thrown if relationship is null.</exception>
        /// <exception cref="System.InvalidOperationException">Thrown if a relationship between these factions already exists.</exception>
        void Add(FactionRelationship relationship);

        /// <summary>
        /// Gets a relationship between two factions.
        /// The order of faction IDs doesn't matter.
        /// </summary>
        /// <param name="factionId1">The first faction ID.</param>
        /// <param name="factionId2">The second faction ID.</param>
        /// <returns>The relationship if found, null otherwise.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if either faction ID is null.</exception>
        FactionRelationship? Get(string factionId1, string factionId2);

        /// <summary>
        /// Gets all relationships involving a specific faction.
        /// </summary>
        /// <param name="factionId">The faction ID to find relationships for.</param>
        /// <returns>An enumerable of all relationships involving the faction.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if factionId is null.</exception>
        IEnumerable<FactionRelationship> GetByFaction(string factionId);

        /// <summary>
        /// Gets all relationships in the repository.
        /// </summary>
        /// <returns>An enumerable of all relationships.</returns>
        IEnumerable<FactionRelationship> GetAll();

        /// <summary>
        /// Updates an existing relationship in the repository.
        /// </summary>
        /// <param name="relationship">The relationship with updated data.</param>
        /// <exception cref="System.ArgumentNullException">Thrown if relationship is null.</exception>
        /// <exception cref="System.InvalidOperationException">Thrown if the relationship doesn't exist.</exception>
        void Update(FactionRelationship relationship);

        /// <summary>
        /// Removes a relationship between two factions.
        /// The order of faction IDs doesn't matter.
        /// </summary>
        /// <param name="factionId1">The first faction ID.</param>
        /// <param name="factionId2">The second faction ID.</param>
        /// <returns>True if the relationship was removed, false if it didn't exist.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if either faction ID is null.</exception>
        bool Remove(string factionId1, string factionId2);

        /// <summary>
        /// Checks if a relationship exists between two factions.
        /// The order of faction IDs doesn't matter.
        /// </summary>
        /// <param name="factionId1">The first faction ID.</param>
        /// <param name="factionId2">The second faction ID.</param>
        /// <returns>True if a relationship exists, false otherwise.</returns>
        bool Contains(string factionId1, string factionId2);

        /// <summary>
        /// Removes all relationships from the repository.
        /// </summary>
        void Clear();

        /// <summary>
        /// Gets or creates a relationship between two factions.
        /// If the relationship doesn't exist, creates one with the specified default value.
        /// </summary>
        /// <param name="factionId1">The first faction ID.</param>
        /// <param name="factionId2">The second faction ID.</param>
        /// <param name="defaultValue">The default value for new relationships (default: 0).</param>
        /// <returns>The existing or newly created relationship.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if either faction ID is null.</exception>
        FactionRelationship GetOrCreate(string factionId1, string factionId2, int defaultValue = 0);
    }
}
