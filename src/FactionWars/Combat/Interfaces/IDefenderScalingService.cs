using System.Collections.Generic;
using FactionWars.Combat.Models;
using FactionWars.Core.Models;

namespace FactionWars.Combat.Interfaces
{
    /// <summary>
    /// Service for scaling zone troop allocations to actual spawnable defender peds.
    /// Since zones can have many troops allocated (e.g., 30-50), this service scales
    /// them down to a reasonable number of peds that can be spawned in-game.
    /// </summary>
    public interface IDefenderScalingService
    {
        /// <summary>
        /// Calculates a spawn plan based on zone troop allocation.
        /// Scales down troops to peds while maintaining tier proportions.
        /// </summary>
        /// <param name="troopsByTier">The allocated troops by tier for the zone.</param>
        /// <param name="maxPeds">The maximum number of peds that can be spawned.</param>
        /// <returns>A spawn plan indicating how many peds of each tier to spawn.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if troopsByTier is null.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">Thrown if maxPeds is negative.</exception>
        DefenderSpawnPlan CalculateSpawnPlan(Dictionary<DefenderTier, int> troopsByTier, int maxPeds);

        /// <summary>
        /// Calculates the scaled number of defenders for a given troop count.
        /// Uses a scale factor to convert troops to peds.
        /// </summary>
        /// <param name="troopCount">The number of allocated troops.</param>
        /// <param name="scaleFactor">The number of troops per spawned ped.</param>
        /// <returns>The number of peds to spawn (minimum 1 if troops > 0, 0 otherwise).</returns>
        int CalculateScaledDefenderCount(int troopCount, int scaleFactor);

        /// <summary>
        /// Gets the default scale factor for troop-to-ped conversion.
        /// </summary>
        /// <returns>The default scale factor (troops per ped).</returns>
        int GetDefaultScaleFactor();
    }
}
