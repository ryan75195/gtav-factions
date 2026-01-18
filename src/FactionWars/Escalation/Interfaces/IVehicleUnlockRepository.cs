using System.Collections.Generic;
using FactionWars.Escalation.Models;

namespace FactionWars.Escalation.Interfaces
{
    /// <summary>
    /// Repository interface for managing vehicle unlock definitions.
    /// Provides CRUD operations and queries for vehicles tied to escalation tiers.
    /// </summary>
    public interface IVehicleUnlockRepository
    {
        /// <summary>
        /// Gets the total number of registered vehicles.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Adds a vehicle unlock to the repository.
        /// </summary>
        /// <param name="vehicle">The vehicle to add.</param>
        /// <returns>True if added, false if a vehicle with the same model already exists.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if vehicle is null.</exception>
        bool Add(VehicleUnlock vehicle);

        /// <summary>
        /// Removes a vehicle unlock from the repository.
        /// </summary>
        /// <param name="vehicleModel">The vehicle model to remove.</param>
        /// <returns>True if removed, false if not found.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if vehicleModel is null.</exception>
        /// <exception cref="System.ArgumentException">Thrown if vehicleModel is empty or whitespace.</exception>
        bool Remove(string vehicleModel);

        /// <summary>
        /// Gets a vehicle by its model name.
        /// </summary>
        /// <param name="vehicleModel">The vehicle model to look up.</param>
        /// <returns>The vehicle if found, null otherwise.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if vehicleModel is null.</exception>
        /// <exception cref="System.ArgumentException">Thrown if vehicleModel is empty or whitespace.</exception>
        VehicleUnlock? GetByModel(string vehicleModel);

        /// <summary>
        /// Gets all vehicles that require a specific tier (exact match).
        /// </summary>
        /// <param name="tier">The tier to filter by.</param>
        /// <returns>Vehicles that require exactly that tier.</returns>
        IEnumerable<VehicleUnlock> GetByTier(EscalationTier tier);

        /// <summary>
        /// Gets all vehicles in a specific category.
        /// </summary>
        /// <param name="category">The category to filter by.</param>
        /// <returns>Vehicles in that category.</returns>
        IEnumerable<VehicleUnlock> GetByCategory(VehicleCategory category);

        /// <summary>
        /// Gets all registered vehicles.
        /// </summary>
        /// <returns>All vehicles in the repository.</returns>
        IEnumerable<VehicleUnlock> GetAll();

        /// <summary>
        /// Gets all vehicles unlocked at or below a specific tier.
        /// </summary>
        /// <param name="tier">The maximum tier to include.</param>
        /// <returns>All vehicles unlocked up to and including that tier.</returns>
        IEnumerable<VehicleUnlock> GetUnlockedAtTier(EscalationTier tier);

        /// <summary>
        /// Gets all vehicles unlocked at or below a specific tier in a specific category.
        /// </summary>
        /// <param name="tier">The maximum tier to include.</param>
        /// <param name="category">The category to filter by.</param>
        /// <returns>Vehicles in that category unlocked up to and including that tier.</returns>
        IEnumerable<VehicleUnlock> GetUnlockedAtTierByCategory(EscalationTier tier, VehicleCategory category);

        /// <summary>
        /// Checks if a vehicle exists in the repository.
        /// </summary>
        /// <param name="vehicleModel">The vehicle model to check.</param>
        /// <returns>True if the vehicle exists.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if vehicleModel is null.</exception>
        bool Exists(string vehicleModel);

        /// <summary>
        /// Clears all vehicles from the repository.
        /// </summary>
        void Clear();
    }
}
