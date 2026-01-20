using System.Collections.Generic;
using System.Linq;
using FactionWars.Core.Models;

namespace FactionWars.Combat.Models
{
    /// <summary>
    /// Represents the result of processing defender casualties.
    /// Contains totals and breakdowns by defender tier.
    /// </summary>
    public class CasualtyResult
    {
        private readonly Dictionary<DefenderTier, int> _casualtiesByTier;

        /// <summary>
        /// The total number of casualties processed.
        /// </summary>
        public int TotalCasualties { get; }

        /// <summary>
        /// Casualties broken down by defender tier.
        /// </summary>
        public IReadOnlyDictionary<DefenderTier, int> CasualtiesByTier => _casualtiesByTier;

        /// <summary>
        /// Creates a new casualty result.
        /// </summary>
        /// <param name="casualtiesByTier">The casualties by tier.</param>
        public CasualtyResult(Dictionary<DefenderTier, int> casualtiesByTier)
        {
            _casualtiesByTier = casualtiesByTier ?? new Dictionary<DefenderTier, int>();
            TotalCasualties = _casualtiesByTier.Values.Sum();
        }

        /// <summary>
        /// Creates an empty casualty result with no casualties.
        /// </summary>
        public static CasualtyResult Empty => new CasualtyResult(new Dictionary<DefenderTier, int>());

        /// <summary>
        /// Gets the number of casualties for a specific tier.
        /// </summary>
        /// <param name="tier">The defender tier.</param>
        /// <returns>The number of casualties for that tier.</returns>
        public int GetCasualties(DefenderTier tier)
        {
            return _casualtiesByTier.TryGetValue(tier, out var count) ? count : 0;
        }

        public override string ToString()
        {
            var basic = GetCasualties(DefenderTier.Basic);
            var medium = GetCasualties(DefenderTier.Medium);
            var heavy = GetCasualties(DefenderTier.Heavy);
            return $"CasualtyResult[Total={TotalCasualties}, Basic={basic}, Medium={medium}, Heavy={heavy}]";
        }
    }
}
