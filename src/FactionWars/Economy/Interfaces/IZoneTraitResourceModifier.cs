using System.Collections.Generic;
using FactionWars.Economy.Models;
using FactionWars.Territory.Models;

namespace FactionWars.Economy.Interfaces
{
    /// <summary>
    /// Provides resource generation modifiers based on zone traits.
    /// Enables calculation of trait-based bonuses for resource generation.
    /// </summary>
    public interface IZoneTraitResourceModifier
    {
        /// <summary>
        /// Gets the resource generation modifier for a specific resource type based on zone traits.
        /// </summary>
        /// <param name="traits">The combined zone traits.</param>
        /// <param name="resourceType">The type of resource to get the modifier for.</param>
        /// <returns>
        /// A multiplier for resource generation (1.0 = baseline, 1.5 = +50%, 2.0 = +100%).
        /// </returns>
        /// <exception cref="System.ArgumentException">Thrown if resourceType is invalid.</exception>
        float GetModifier(ZoneTrait traits, ResourceType resourceType);

        /// <summary>
        /// Gets the resource generation modifiers for all resource types based on zone traits.
        /// </summary>
        /// <param name="traits">The combined zone traits.</param>
        /// <returns>A dictionary mapping each resource type to its modifier.</returns>
        Dictionary<ResourceType, float> GetTotalModifier(ZoneTrait traits);

        /// <summary>
        /// Checks if the given traits provide any bonus for the specified resource type.
        /// </summary>
        /// <param name="traits">The combined zone traits.</param>
        /// <param name="resourceType">The type of resource to check.</param>
        /// <returns>True if the traits provide a bonus (modifier > 1.0) for this resource.</returns>
        bool HasResourceBonus(ZoneTrait traits, ResourceType resourceType);
    }
}
