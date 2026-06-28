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
        private readonly Dictionary<DefenderRole, int> _casualtiesByTier;

        /// <summary>
        /// The total number of casualties processed.
        /// </summary>
        public int TotalCasualties { get; }

        /// <summary>
        /// Casualties broken down by defender tier.
        /// </summary>
        public IReadOnlyDictionary<DefenderRole, int> CasualtiesByTier => _casualtiesByTier;

        /// <summary>
        /// Creates a new casualty result.
        /// </summary>
        /// <param name="casualtiesByTier">The casualties by tier.</param>
        public CasualtyResult(Dictionary<DefenderRole, int> casualtiesByTier)
        {
            _casualtiesByTier = casualtiesByTier ?? new Dictionary<DefenderRole, int>();
            TotalCasualties = _casualtiesByTier.Values.Sum();
        }

        /// <summary>
        /// Creates an empty casualty result with no casualties.
        /// </summary>
        public static CasualtyResult Empty => new CasualtyResult(new Dictionary<DefenderRole, int>());

        /// <summary>
        /// Gets the number of casualties for a specific tier.
        /// </summary>
        /// <param name="tier">The defender tier.</param>
        /// <returns>The number of casualties for that tier.</returns>
        public int GetCasualties(DefenderRole tier)
        {
            return _casualtiesByTier.TryGetValue(tier, out var count) ? count : 0;
        }

        public override string ToString()
        {
            var basic = GetCasualties(DefenderRole.Grunt);
            var medium = GetCasualties(DefenderRole.Gunner);
            var heavy = GetCasualties(DefenderRole.Rifleman);
            return $"CasualtyResult[Total={TotalCasualties}, Basic={basic}, Medium={medium}, Heavy={heavy}]";
        }
    }
}
