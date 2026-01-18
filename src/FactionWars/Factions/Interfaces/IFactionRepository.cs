using FactionWars.Factions.Models;
using System.Collections.Generic;

namespace FactionWars.Factions.Interfaces
{
    /// <summary>
    /// Repository interface for faction data access.
    /// Provides CRUD operations and query methods for factions and their states.
    /// </summary>
    public interface IFactionRepository
    {
        /// <summary>
        /// Gets the total number of factions in the repository.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Adds a new faction to the repository.
        /// </summary>
        /// <param name="faction">The faction to add.</param>
        /// <exception cref="System.ArgumentNullException">Thrown if faction is null.</exception>
        /// <exception cref="System.InvalidOperationException">Thrown if a faction with the same ID already exists.</exception>
        void Add(Faction faction);

        /// <summary>
        /// Gets a faction by its unique identifier.
        /// </summary>
        /// <param name="id">The faction ID to find.</param>
        /// <returns>The faction if found, null otherwise.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if id is null.</exception>
        /// <exception cref="System.ArgumentException">Thrown if id is empty or whitespace.</exception>
        Faction? GetById(string id);

        /// <summary>
        /// Gets all factions in the repository.
        /// </summary>
        /// <returns>An enumerable of all factions.</returns>
        IEnumerable<Faction> GetAll();

        /// <summary>
        /// Updates an existing faction in the repository.
        /// </summary>
        /// <param name="faction">The faction with updated data.</param>
        /// <exception cref="System.ArgumentNullException">Thrown if faction is null.</exception>
        /// <exception cref="System.InvalidOperationException">Thrown if the faction does not exist in the repository.</exception>
        void Update(Faction faction);

        /// <summary>
        /// Removes a faction from the repository.
        /// </summary>
        /// <param name="id">The ID of the faction to remove.</param>
        /// <returns>True if the faction was removed, false if it didn't exist.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if id is null.</exception>
        bool Remove(string id);

        /// <summary>
        /// Checks if a faction with the specified ID exists in the repository.
        /// </summary>
        /// <param name="id">The faction ID to check.</param>
        /// <returns>True if the faction exists, false otherwise.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if id is null.</exception>
        bool Contains(string id);

        /// <summary>
        /// Removes all factions from the repository.
        /// </summary>
        void Clear();

        /// <summary>
        /// Gets all active factions.
        /// </summary>
        /// <returns>An enumerable of all active factions.</returns>
        IEnumerable<Faction> GetActive();

        /// <summary>
        /// Gets the state for a specific faction.
        /// </summary>
        /// <param name="factionId">The faction ID to get state for.</param>
        /// <returns>The faction state if found, null otherwise.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if factionId is null.</exception>
        /// <exception cref="System.ArgumentException">Thrown if factionId is empty or whitespace.</exception>
        FactionState? GetState(string factionId);

        /// <summary>
        /// Sets or updates the state for a faction.
        /// Creates a new state if one doesn't exist.
        /// </summary>
        /// <param name="state">The faction state to set.</param>
        /// <exception cref="System.ArgumentNullException">Thrown if state is null.</exception>
        /// <exception cref="System.InvalidOperationException">Thrown if the faction doesn't exist in the repository.</exception>
        void SetState(FactionState state);

        /// <summary>
        /// Gets all faction states.
        /// </summary>
        /// <returns>An enumerable of all faction states.</returns>
        IEnumerable<FactionState> GetAllStates();
    }
}
