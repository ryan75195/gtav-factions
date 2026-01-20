using System.Collections.Generic;
using FactionWars.Core.Models;

namespace FactionWars.Core.Interfaces
{
    /// <summary>
    /// Service for managing defender tier configurations and calculations.
    /// Provides access to tier costs, stats, and combat modifiers.
    /// </summary>
    public interface IDefenderTierService
    {
        /// <summary>
        /// Gets the configuration for a specific defender tier.
        /// </summary>
        /// <param name="tier">The tier to get configuration for.</param>
        /// <returns>The configuration for the specified tier.</returns>
        DefenderTierConfig GetTierConfig(DefenderTier tier);

        /// <summary>
        /// Gets the configurations for all defender tiers.
        /// </summary>
        /// <returns>A list of all tier configurations.</returns>
        IReadOnlyList<DefenderTierConfig> GetAllTierConfigs();

        /// <summary>
        /// Gets the cost to purchase one defender of the specified tier.
        /// </summary>
        /// <param name="tier">The tier to get cost for.</param>
        /// <returns>The cost in dollars.</returns>
        int GetCost(DefenderTier tier);

        /// <summary>
        /// Gets the combat strength modifier for the specified tier.
        /// Used in battle simulation calculations.
        /// Basic=1.0, Medium=1.5, Heavy=2.0
        /// </summary>
        /// <param name="tier">The tier to get modifier for.</param>
        /// <returns>The combat modifier value.</returns>
        float GetCombatModifier(DefenderTier tier);

        /// <summary>
        /// Calculates the total cost for a collection of troops by tier.
        /// </summary>
        /// <param name="troopsByTier">Dictionary mapping tiers to troop counts.</param>
        /// <returns>The total cost in dollars.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if troopsByTier is null.</exception>
        int CalculateTotalCost(Dictionary<DefenderTier, int> troopsByTier);

        /// <summary>
        /// Calculates the total combat strength for a collection of troops by tier.
        /// Applies combat modifiers to each tier's count.
        /// </summary>
        /// <param name="troopsByTier">Dictionary mapping tiers to troop counts.</param>
        /// <returns>The total combat strength value.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if troopsByTier is null.</exception>
        float CalculateTotalStrength(Dictionary<DefenderTier, int> troopsByTier);
    }
}
