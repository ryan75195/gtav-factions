using System.Collections.Generic;
using FactionWars.Lieutenants.Models;

namespace FactionWars.Lieutenants.Interfaces
{
    /// <summary>
    /// Service for procedurally generating lieutenant traits.
    /// </summary>
    public interface ITraitGenerator
    {
        /// <summary>
        /// Generates a random set of traits for a lieutenant.
        /// </summary>
        /// <param name="count">The number of traits to generate.</param>
        /// <returns>A collection of distinct traits.</returns>
        IReadOnlyCollection<LieutenantTrait> GenerateTraits(int count);

        /// <summary>
        /// Generates traits using custom weights for each trait type.
        /// </summary>
        /// <param name="count">The number of traits to generate.</param>
        /// <param name="weights">Custom weights for each trait (higher = more likely).</param>
        /// <returns>A collection of distinct traits.</returns>
        IReadOnlyCollection<LieutenantTrait> GenerateTraitsWithWeights(int count, IDictionary<LieutenantTrait, double> weights);

        /// <summary>
        /// Generates traits with faction-specific preferences.
        /// </summary>
        /// <param name="count">The number of traits to generate.</param>
        /// <param name="factionId">The faction ID to generate traits for.</param>
        /// <returns>A collection of distinct traits.</returns>
        IReadOnlyCollection<LieutenantTrait> GenerateTraitsForFaction(int count, string factionId);

        /// <summary>
        /// Gets the traits that are mutually exclusive with the specified trait.
        /// </summary>
        /// <param name="trait">The trait to check.</param>
        /// <returns>Collection of traits that cannot coexist with the specified trait.</returns>
        IReadOnlyCollection<LieutenantTrait> GetMutuallyExclusiveTraits(LieutenantTrait trait);

        /// <summary>
        /// Gets the default weights for all traits.
        /// </summary>
        /// <returns>Dictionary mapping traits to their default weights.</returns>
        IReadOnlyDictionary<LieutenantTrait, double> GetDefaultTraitWeights();

        /// <summary>
        /// Gets the trait weights for a specific faction.
        /// </summary>
        /// <param name="factionId">The faction ID.</param>
        /// <returns>Dictionary mapping traits to their weights for the faction.</returns>
        IReadOnlyDictionary<LieutenantTrait, double> GetFactionTraitWeights(string factionId);
    }
}
