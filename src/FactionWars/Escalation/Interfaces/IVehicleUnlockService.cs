using System.Collections.Generic;
using FactionWars.Escalation.Models;

namespace FactionWars.Escalation.Interfaces
{
    /// <summary>
    /// Service interface for vehicle unlock operations.
    /// Provides business logic for querying available vehicles based on faction escalation.
    /// </summary>
    public interface IVehicleUnlockService
    {
        /// <summary>
        /// Gets all vehicles available to a faction based on their current escalation tier.
        /// </summary>
        /// <param name="factionId">The faction ID.</param>
        /// <returns>All vehicles the faction has unlocked.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if factionId is null.</exception>
        /// <exception cref="System.ArgumentException">Thrown if factionId is empty or whitespace.</exception>
        IEnumerable<VehicleUnlock> GetAvailableVehicles(string factionId);

        /// <summary>
        /// Gets available vehicles for a faction filtered by category.
        /// </summary>
        /// <param name="factionId">The faction ID.</param>
        /// <param name="category">The vehicle category to filter by.</param>
        /// <returns>Available vehicles in the specified category.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if factionId is null.</exception>
        IEnumerable<VehicleUnlock> GetAvailableVehiclesByCategory(string factionId, VehicleCategory category);

        /// <summary>
        /// Checks if a specific vehicle is available to a faction.
        /// </summary>
        /// <param name="factionId">The faction ID.</param>
        /// <param name="vehicleModel">The vehicle model to check.</param>
        /// <returns>True if the vehicle is available to the faction.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if factionId or vehicleModel is null.</exception>
        bool IsVehicleAvailable(string factionId, string vehicleModel);

        /// <summary>
        /// Gets vehicles that were newly unlocked when transitioning between tiers.
        /// </summary>
        /// <param name="previousTier">The previous escalation tier.</param>
        /// <param name="newTier">The new escalation tier.</param>
        /// <returns>Vehicles unlocked between the two tiers.</returns>
        IEnumerable<VehicleUnlock> GetNewlyUnlockedVehicles(EscalationTier previousTier, EscalationTier newTier);

        /// <summary>
        /// Gets a random vehicle available at a specific tier.
        /// </summary>
        /// <param name="tier">The tier to select from.</param>
        /// <returns>A random available vehicle, or null if none available.</returns>
        VehicleUnlock? GetRandomVehicleForTier(EscalationTier tier);

        /// <summary>
        /// Gets a random vehicle available to a faction.
        /// </summary>
        /// <param name="factionId">The faction ID.</param>
        /// <returns>A random available vehicle, or null if none available.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if factionId is null.</exception>
        VehicleUnlock? GetRandomVehicleForFaction(string factionId);

        /// <summary>
        /// Gets a random vehicle available to a faction in a specific category.
        /// </summary>
        /// <param name="factionId">The faction ID.</param>
        /// <param name="category">The vehicle category.</param>
        /// <returns>A random available vehicle in that category, or null if none available.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if factionId is null.</exception>
        VehicleUnlock? GetRandomVehicleForFactionByCategory(string factionId, VehicleCategory category);

        /// <summary>
        /// Registers a new vehicle unlock definition.
        /// </summary>
        /// <param name="vehicle">The vehicle to register.</param>
        /// <returns>True if registered, false if already exists.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if vehicle is null.</exception>
        bool RegisterVehicle(VehicleUnlock vehicle);

        /// <summary>
        /// Unregisters a vehicle unlock definition.
        /// </summary>
        /// <param name="vehicleModel">The vehicle model to unregister.</param>
        /// <returns>True if unregistered, false if not found.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if vehicleModel is null.</exception>
        bool UnregisterVehicle(string vehicleModel);

        /// <summary>
        /// Gets all registered vehicle unlocks.
        /// </summary>
        /// <returns>All registered vehicles.</returns>
        IEnumerable<VehicleUnlock> GetAllRegisteredVehicles();

        /// <summary>
        /// Gets information about a specific vehicle.
        /// </summary>
        /// <param name="vehicleModel">The vehicle model.</param>
        /// <returns>The vehicle info, or null if not found.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if vehicleModel is null.</exception>
        VehicleUnlock? GetVehicleInfo(string vehicleModel);

        /// <summary>
        /// Gets vehicles newly unlocked for a faction based on their previous tier.
        /// Uses the faction's stored previous tier.
        /// </summary>
        /// <param name="factionId">The faction ID.</param>
        /// <returns>Vehicles the faction just unlocked.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if factionId is null.</exception>
        IEnumerable<VehicleUnlock> GetVehiclesNewlyUnlockedForFaction(string factionId);
    }
}
