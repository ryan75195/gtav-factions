using FactionWars.Core.Models;
using FactionWars.Factions.Models;
using System.Collections.Generic;

namespace FactionWars.Factions.Interfaces
{
    /// <summary>
    /// Service interface for faction-related business logic and operations.
    /// Provides higher-level operations beyond basic CRUD.
    /// </summary>
    public interface IFactionService
    {
        /// <summary>
        /// Gets a faction by its unique identifier.
        /// </summary>
        /// <param name="id">The faction ID to find.</param>
        /// <returns>The faction if found, null otherwise.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if id is null.</exception>
        /// <exception cref="System.ArgumentException">Thrown if id is empty or whitespace.</exception>
        Faction? GetFaction(string id);

        /// <summary>
        /// Gets all factions.
        /// </summary>
        /// <returns>An enumerable of all factions.</returns>
        IEnumerable<Faction> GetAllFactions();

        /// <summary>
        /// Gets all active factions.
        /// </summary>
        /// <returns>An enumerable of active factions.</returns>
        IEnumerable<Faction> GetActiveFactions();

        /// <summary>
        /// Gets the state for a specific faction.
        /// </summary>
        /// <param name="factionId">The faction ID to get state for.</param>
        /// <returns>The faction state if found, null otherwise.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if factionId is null.</exception>
        FactionState? GetFactionState(string factionId);

        /// <summary>
        /// Activates a faction, allowing it to participate in combat and resource generation.
        /// </summary>
        /// <param name="factionId">The faction ID to activate.</param>
        /// <returns>True if activation succeeded, false if faction not found.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if factionId is null.</exception>
        bool ActivateFaction(string factionId);

        /// <summary>
        /// Deactivates a faction, preventing it from participating in combat and resource generation.
        /// </summary>
        /// <param name="factionId">The faction ID to deactivate.</param>
        /// <returns>True if deactivation succeeded, false if faction not found.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if factionId is null.</exception>
        bool DeactivateFaction(string factionId);

        /// <summary>
        /// Initializes the state for a faction with starting values.
        /// </summary>
        /// <param name="factionId">The faction ID to initialize state for.</param>
        /// <param name="initialCash">Starting cash amount.</param>
        /// <param name="initialTroops">Starting troop count.</param>
        /// <returns>True if initialization succeeded, false if faction not found.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if factionId is null.</exception>
        bool InitializeFactionState(string factionId, int initialCash, int initialTroops);

        /// <summary>
        /// Adds a zone to a faction's ownership.
        /// </summary>
        /// <param name="factionId">The faction ID to add the zone to.</param>
        /// <param name="zoneId">The zone ID to add.</param>
        /// <returns>True if zone was added, false if faction or state not found.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if factionId or zoneId is null.</exception>
        bool AddZoneToFaction(string factionId, string zoneId);

        /// <summary>
        /// Removes a zone from a faction's ownership.
        /// </summary>
        /// <param name="factionId">The faction ID to remove the zone from.</param>
        /// <param name="zoneId">The zone ID to remove.</param>
        /// <returns>True if zone was removed, false if not owned or faction/state not found.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if factionId or zoneId is null.</exception>
        bool RemoveZoneFromFaction(string factionId, string zoneId);

        /// <summary>
        /// Adds cash to a faction's resources.
        /// </summary>
        /// <param name="factionId">The faction ID to add cash to.</param>
        /// <param name="amount">The amount of cash to add (must be non-negative).</param>
        /// <returns>True if cash was added, false if faction or state not found.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if factionId is null.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">Thrown if amount is negative.</exception>
        bool AddCash(string factionId, int amount);

        /// <summary>
        /// Attempts to spend cash from a faction's resources.
        /// </summary>
        /// <param name="factionId">The faction ID to spend cash from.</param>
        /// <param name="amount">The amount of cash to spend (must be non-negative).</param>
        /// <returns>True if purchase was successful, false if insufficient funds or faction/state not found.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if factionId is null.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">Thrown if amount is negative.</exception>
        bool SpendCash(string factionId, int amount);

        /// <summary>
        /// Recruits additional troops for a faction.
        /// </summary>
        /// <param name="factionId">The faction ID to recruit troops for.</param>
        /// <param name="count">The number of troops to recruit (must be non-negative).</param>
        /// <returns>True if troops were recruited, false if faction or state not found.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if factionId is null.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">Thrown if count is negative.</exception>
        bool RecruitTroops(string factionId, int count);

        /// <summary>
        /// Records troop losses for a faction.
        /// </summary>
        /// <param name="factionId">The faction ID to record losses for.</param>
        /// <param name="count">The number of troops lost (must be non-negative).</param>
        /// <returns>True if loss was recorded, false if faction or state not found.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if factionId is null.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">Thrown if count is negative.</exception>
        bool LoseTroops(string factionId, int count);

        /// <summary>
        /// Gets the total military strength of a faction.
        /// </summary>
        /// <param name="factionId">The faction ID to calculate strength for.</param>
        /// <returns>The total military strength, or 0 if faction/state not found.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if factionId is null.</exception>
        int GetMilitaryStrength(string factionId);

        /// <summary>
        /// Gets the count of zones owned by a faction.
        /// </summary>
        /// <param name="factionId">The faction ID to count zones for.</param>
        /// <returns>The number of zones owned, or 0 if faction/state not found.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if factionId is null.</exception>
        int GetZoneCount(string factionId);

        /// <summary>
        /// Checks if a faction can afford a specified amount.
        /// </summary>
        /// <param name="factionId">The faction ID to check.</param>
        /// <param name="amount">The amount to check.</param>
        /// <returns>True if faction has sufficient funds, false otherwise.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if factionId is null.</exception>
        bool CanAfford(string factionId, int amount);

        /// <summary>
        /// Transfers a zone between two factions.
        /// </summary>
        /// <param name="zoneId">The zone ID to transfer.</param>
        /// <param name="sourceFactionId">The faction currently owning the zone.</param>
        /// <param name="targetFactionId">The faction to receive the zone.</param>
        /// <returns>True if transfer succeeded, false if source doesn't own zone or factions not found.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if any parameter is null.</exception>
        bool TransferZoneBetweenFactions(string zoneId, string sourceFactionId, string targetFactionId);

        /// <summary>
        /// Adds weapons to a faction's arsenal.
        /// </summary>
        /// <param name="factionId">The faction ID to add weapons to.</param>
        /// <param name="count">The number of weapons to add (must be non-negative).</param>
        /// <returns>True if weapons were added, false if faction or state not found.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if factionId is null.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">Thrown if count is negative.</exception>
        bool AddWeapons(string factionId, int count);

        /// <summary>
        /// Adds recruitment points to a faction's resources.
        /// </summary>
        /// <param name="factionId">The faction ID to add recruitment points to.</param>
        /// <param name="amount">The amount of recruitment points to add (must be non-negative).</param>
        /// <returns>True if recruitment points were added, false if faction or state not found.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if factionId is null.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">Thrown if amount is negative.</exception>
        bool AddRecruitmentPoints(string factionId, int amount);

        /// <summary>
        /// Adds troops to a faction's reserve pool for a specific tier.
        /// </summary>
        /// <param name="factionId">The faction ID to add reserve troops to.</param>
        /// <param name="tier">The defender tier to add troops to.</param>
        /// <param name="count">The number of troops to add (must be non-negative).</param>
        /// <returns>True if troops were added, false if faction or state not found.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if factionId is null.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">Thrown if count is negative.</exception>
        bool AddReserveTroops(string factionId, DefenderRole tier, int count);
    }
}
