using System.Collections.Generic;
using FactionWars.Escalation.Models;

namespace FactionWars.Escalation.Interfaces
{
    /// <summary>
    /// Service interface for weapon unlock operations.
    /// Provides business logic for querying available weapons based on faction escalation.
    /// </summary>
    public interface IWeaponUnlockService
    {
        /// <summary>
        /// Gets all weapons available to a faction based on their current escalation tier.
        /// </summary>
        /// <param name="factionId">The faction ID.</param>
        /// <returns>All weapons the faction has unlocked.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if factionId is null.</exception>
        /// <exception cref="System.ArgumentException">Thrown if factionId is empty or whitespace.</exception>
        IEnumerable<WeaponUnlock> GetAvailableWeapons(string factionId);

        /// <summary>
        /// Gets available weapons for a faction filtered by category.
        /// </summary>
        /// <param name="factionId">The faction ID.</param>
        /// <param name="category">The weapon category to filter by.</param>
        /// <returns>Available weapons in the specified category.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if factionId is null.</exception>
        IEnumerable<WeaponUnlock> GetAvailableWeaponsByCategory(string factionId, WeaponCategory category);

        /// <summary>
        /// Checks if a specific weapon is available to a faction.
        /// </summary>
        /// <param name="factionId">The faction ID.</param>
        /// <param name="weaponHash">The weapon hash to check.</param>
        /// <returns>True if the weapon is available to the faction.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if factionId or weaponHash is null.</exception>
        bool IsWeaponAvailable(string factionId, string weaponHash);

        /// <summary>
        /// Gets weapons that were newly unlocked when transitioning between tiers.
        /// </summary>
        /// <param name="previousTier">The previous escalation tier.</param>
        /// <param name="newTier">The new escalation tier.</param>
        /// <returns>Weapons unlocked between the two tiers.</returns>
        IEnumerable<WeaponUnlock> GetNewlyUnlockedWeapons(EscalationTier previousTier, EscalationTier newTier);

        /// <summary>
        /// Gets a random weapon available at a specific tier.
        /// </summary>
        /// <param name="tier">The tier to select from.</param>
        /// <returns>A random available weapon, or null if none available.</returns>
        WeaponUnlock? GetRandomWeaponForTier(EscalationTier tier);

        /// <summary>
        /// Gets a random weapon available to a faction.
        /// </summary>
        /// <param name="factionId">The faction ID.</param>
        /// <returns>A random available weapon, or null if none available.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if factionId is null.</exception>
        WeaponUnlock? GetRandomWeaponForFaction(string factionId);

        /// <summary>
        /// Gets a random weapon available to a faction in a specific category.
        /// </summary>
        /// <param name="factionId">The faction ID.</param>
        /// <param name="category">The weapon category.</param>
        /// <returns>A random available weapon in that category, or null if none available.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if factionId is null.</exception>
        WeaponUnlock? GetRandomWeaponForFactionByCategory(string factionId, WeaponCategory category);

        /// <summary>
        /// Registers a new weapon unlock definition.
        /// </summary>
        /// <param name="weapon">The weapon to register.</param>
        /// <returns>True if registered, false if already exists.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if weapon is null.</exception>
        bool RegisterWeapon(WeaponUnlock weapon);

        /// <summary>
        /// Unregisters a weapon unlock definition.
        /// </summary>
        /// <param name="weaponHash">The weapon hash to unregister.</param>
        /// <returns>True if unregistered, false if not found.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if weaponHash is null.</exception>
        bool UnregisterWeapon(string weaponHash);

        /// <summary>
        /// Gets all registered weapon unlocks.
        /// </summary>
        /// <returns>All registered weapons.</returns>
        IEnumerable<WeaponUnlock> GetAllRegisteredWeapons();

        /// <summary>
        /// Gets information about a specific weapon.
        /// </summary>
        /// <param name="weaponHash">The weapon hash.</param>
        /// <returns>The weapon info, or null if not found.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if weaponHash is null.</exception>
        WeaponUnlock? GetWeaponInfo(string weaponHash);

        /// <summary>
        /// Gets weapons newly unlocked for a faction based on their previous tier.
        /// Uses the faction's stored previous tier.
        /// </summary>
        /// <param name="factionId">The faction ID.</param>
        /// <returns>Weapons the faction just unlocked.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if factionId is null.</exception>
        IEnumerable<WeaponUnlock> GetWeaponsNewlyUnlockedForFaction(string factionId);
    }
}
