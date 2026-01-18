using System.Collections.Generic;
using FactionWars.Escalation.Models;

namespace FactionWars.Escalation.Interfaces
{
    /// <summary>
    /// Repository interface for managing weapon unlock definitions.
    /// Provides CRUD operations and queries for weapons tied to escalation tiers.
    /// </summary>
    public interface IWeaponUnlockRepository
    {
        /// <summary>
        /// Gets the total number of registered weapons.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Adds a weapon unlock to the repository.
        /// </summary>
        /// <param name="weapon">The weapon to add.</param>
        /// <returns>True if added, false if a weapon with the same hash already exists.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if weapon is null.</exception>
        bool Add(WeaponUnlock weapon);

        /// <summary>
        /// Removes a weapon unlock from the repository.
        /// </summary>
        /// <param name="weaponHash">The weapon hash to remove.</param>
        /// <returns>True if removed, false if not found.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if weaponHash is null.</exception>
        /// <exception cref="System.ArgumentException">Thrown if weaponHash is empty or whitespace.</exception>
        bool Remove(string weaponHash);

        /// <summary>
        /// Gets a weapon by its hash.
        /// </summary>
        /// <param name="weaponHash">The weapon hash to look up.</param>
        /// <returns>The weapon if found, null otherwise.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if weaponHash is null.</exception>
        /// <exception cref="System.ArgumentException">Thrown if weaponHash is empty or whitespace.</exception>
        WeaponUnlock? GetByHash(string weaponHash);

        /// <summary>
        /// Gets all weapons that require a specific tier (exact match).
        /// </summary>
        /// <param name="tier">The tier to filter by.</param>
        /// <returns>Weapons that require exactly that tier.</returns>
        IEnumerable<WeaponUnlock> GetByTier(EscalationTier tier);

        /// <summary>
        /// Gets all weapons in a specific category.
        /// </summary>
        /// <param name="category">The category to filter by.</param>
        /// <returns>Weapons in that category.</returns>
        IEnumerable<WeaponUnlock> GetByCategory(WeaponCategory category);

        /// <summary>
        /// Gets all registered weapons.
        /// </summary>
        /// <returns>All weapons in the repository.</returns>
        IEnumerable<WeaponUnlock> GetAll();

        /// <summary>
        /// Gets all weapons unlocked at or below a specific tier.
        /// </summary>
        /// <param name="tier">The maximum tier to include.</param>
        /// <returns>All weapons unlocked up to and including that tier.</returns>
        IEnumerable<WeaponUnlock> GetUnlockedAtTier(EscalationTier tier);

        /// <summary>
        /// Gets all weapons unlocked at or below a specific tier in a specific category.
        /// </summary>
        /// <param name="tier">The maximum tier to include.</param>
        /// <param name="category">The category to filter by.</param>
        /// <returns>Weapons in that category unlocked up to and including that tier.</returns>
        IEnumerable<WeaponUnlock> GetUnlockedAtTierByCategory(EscalationTier tier, WeaponCategory category);

        /// <summary>
        /// Checks if a weapon exists in the repository.
        /// </summary>
        /// <param name="weaponHash">The weapon hash to check.</param>
        /// <returns>True if the weapon exists.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if weaponHash is null.</exception>
        bool Exists(string weaponHash);

        /// <summary>
        /// Clears all weapons from the repository.
        /// </summary>
        void Clear();
    }
}
