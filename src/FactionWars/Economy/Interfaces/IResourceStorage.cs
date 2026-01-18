using FactionWars.Economy.Models;

namespace FactionWars.Economy.Interfaces
{
    /// <summary>
    /// Interface for managing resource storage with capacity limits (caps).
    /// Provides methods to add, remove, and query resources while respecting storage caps.
    /// </summary>
    public interface IResourceStorage
    {
        /// <summary>
        /// Gets the current amount of a specific resource.
        /// </summary>
        /// <param name="resourceType">The type of resource to query.</param>
        /// <returns>The current amount of the resource.</returns>
        int GetAmount(ResourceType resourceType);

        /// <summary>
        /// Gets the maximum storage capacity for a specific resource.
        /// </summary>
        /// <param name="resourceType">The type of resource to query.</param>
        /// <returns>The maximum capacity for the resource.</returns>
        int GetCap(ResourceType resourceType);

        /// <summary>
        /// Gets the remaining capacity for a specific resource.
        /// </summary>
        /// <param name="resourceType">The type of resource to query.</param>
        /// <returns>The remaining capacity (cap - current amount).</returns>
        int GetRemainingCapacity(ResourceType resourceType);

        /// <summary>
        /// Gets the fill percentage for a specific resource (0-100).
        /// </summary>
        /// <param name="resourceType">The type of resource to query.</param>
        /// <returns>The fill percentage from 0 to 100.</returns>
        float GetFillPercentage(ResourceType resourceType);

        /// <summary>
        /// Checks if storage for a specific resource is at capacity.
        /// </summary>
        /// <param name="resourceType">The type of resource to check.</param>
        /// <returns>True if the resource is at its cap, false otherwise.</returns>
        bool IsAtCap(ResourceType resourceType);

        /// <summary>
        /// Checks if there is at least the specified amount of a resource.
        /// </summary>
        /// <param name="resourceType">The type of resource to check.</param>
        /// <param name="amount">The amount to check for.</param>
        /// <returns>True if the resource amount is at least the specified amount.</returns>
        /// <exception cref="System.ArgumentOutOfRangeException">Thrown if amount is negative.</exception>
        bool HasAmount(ResourceType resourceType, int amount);

        /// <summary>
        /// Adds an amount to a specific resource, respecting the cap.
        /// </summary>
        /// <param name="resourceType">The type of resource to add to.</param>
        /// <param name="amount">The amount to add (must be non-negative).</param>
        /// <returns>The actual amount added (may be less if capped).</returns>
        /// <exception cref="System.ArgumentOutOfRangeException">Thrown if amount is negative.</exception>
        int Add(ResourceType resourceType, int amount);

        /// <summary>
        /// Removes an amount from a specific resource.
        /// </summary>
        /// <param name="resourceType">The type of resource to remove from.</param>
        /// <param name="amount">The amount to remove (must be non-negative).</param>
        /// <returns>True if the full amount was removed, false if insufficient resources.</returns>
        /// <exception cref="System.ArgumentOutOfRangeException">Thrown if amount is negative.</exception>
        bool Remove(ResourceType resourceType, int amount);

        /// <summary>
        /// Sets the amount of a specific resource, respecting the cap.
        /// </summary>
        /// <param name="resourceType">The type of resource to set.</param>
        /// <param name="amount">The amount to set (will be clamped to cap).</param>
        /// <exception cref="System.ArgumentOutOfRangeException">Thrown if amount is negative.</exception>
        void Set(ResourceType resourceType, int amount);

        /// <summary>
        /// Sets the cap for a specific resource.
        /// If current amount exceeds new cap, amount will be clamped.
        /// </summary>
        /// <param name="resourceType">The type of resource to set the cap for.</param>
        /// <param name="cap">The new cap value (must be positive).</param>
        /// <exception cref="System.ArgumentOutOfRangeException">Thrown if cap is not positive.</exception>
        void SetCap(ResourceType resourceType, int cap);

        /// <summary>
        /// Modifies the cap by a multiplier.
        /// If current amount exceeds new cap, amount will be clamped.
        /// </summary>
        /// <param name="resourceType">The type of resource to modify the cap for.</param>
        /// <param name="multiplier">The multiplier to apply (must be positive).</param>
        /// <exception cref="System.ArgumentOutOfRangeException">Thrown if multiplier is not positive or result is less than 1.</exception>
        void ModifyCap(ResourceType resourceType, float multiplier);

        /// <summary>
        /// Clears (sets to zero) a specific resource.
        /// </summary>
        /// <param name="resourceType">The type of resource to clear.</param>
        void Clear(ResourceType resourceType);

        /// <summary>
        /// Clears all resources to zero without affecting caps.
        /// </summary>
        void ClearAll();
    }
}
