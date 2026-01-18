using System;
using System.Collections.Generic;
using System.Linq;
using FactionWars.Lieutenants.Interfaces;
using FactionWars.Lieutenants.Models;

namespace FactionWars.Lieutenants.Services
{
    /// <summary>
    /// Service for procedurally generating lieutenant traits.
    /// </summary>
    public class TraitGenerator : ITraitGenerator
    {
        private readonly IRandomProvider _randomProvider;

        private static readonly Dictionary<LieutenantTrait, HashSet<LieutenantTrait>> MutuallyExclusivePairs =
            new Dictionary<LieutenantTrait, HashSet<LieutenantTrait>>
            {
                { LieutenantTrait.Loyal, new HashSet<LieutenantTrait> { LieutenantTrait.Ambitious } },
                { LieutenantTrait.Ambitious, new HashSet<LieutenantTrait> { LieutenantTrait.Loyal } },
                { LieutenantTrait.Aggressive, new HashSet<LieutenantTrait> { LieutenantTrait.Defensive } },
                { LieutenantTrait.Defensive, new HashSet<LieutenantTrait> { LieutenantTrait.Aggressive } }
            };

        public TraitGenerator(IRandomProvider randomProvider)
        {
            _randomProvider = randomProvider ?? throw new ArgumentNullException(nameof(randomProvider));
        }

        public IReadOnlyCollection<LieutenantTrait> GenerateTraits(int count)
        {
            if (count <= 0)
            {
                return new List<LieutenantTrait>();
            }

            return GenerateTraitsWithWeights(count, GetDefaultTraitWeights().ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
        }

        public IReadOnlyCollection<LieutenantTrait> GenerateTraitsWithWeights(int count, IDictionary<LieutenantTrait, double> weights)
        {
            if (weights == null)
            {
                throw new ArgumentNullException(nameof(weights));
            }

            if (count <= 0)
            {
                return new List<LieutenantTrait>();
            }

            // Use default weights if the provided dictionary is empty
            var effectiveWeights = weights.Count > 0
                ? new Dictionary<LieutenantTrait, double>(weights)
                : GetDefaultTraitWeights().ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            // When custom weights are provided, only those traits are available for selection
            // If the dictionary is empty, all traits are available with default weights
            var allTraits = weights.Count > 0
                ? effectiveWeights.Keys.ToList()
                : Enum.GetValues(typeof(LieutenantTrait)).Cast<LieutenantTrait>().ToList();

            var selectedTraits = new List<LieutenantTrait>();
            var excludedTraits = new HashSet<LieutenantTrait>();

            // Limit count to max possible traits
            var maxTraits = allTraits.Count;
            var targetCount = Math.Min(count, maxTraits);

            while (selectedTraits.Count < targetCount)
            {
                // Build available traits list (not selected, not excluded, weight > 0)
                // Sort by weight descending so higher-weighted traits accumulate first in the cumulative sum
                var availableTraits = allTraits
                    .Where(t => !selectedTraits.Contains(t) && !excludedTraits.Contains(t))
                    .Where(t => effectiveWeights[t] > 0)
                    .OrderByDescending(t => effectiveWeights[t])
                    .ToList();

                if (availableTraits.Count == 0)
                {
                    break;
                }

                // Calculate total weight
                var totalWeight = availableTraits.Sum(t => effectiveWeights[t]);

                // Select a trait based on weighted random
                var randomValue = _randomProvider.NextDouble() * totalWeight;
                var cumulativeWeight = 0.0;
                LieutenantTrait selectedTrait = availableTraits[0];

                foreach (var trait in availableTraits)
                {
                    var weight = effectiveWeights[trait];
                    cumulativeWeight += weight;
                    if (randomValue < cumulativeWeight)
                    {
                        selectedTrait = trait;
                        break;
                    }
                }

                selectedTraits.Add(selectedTrait);

                // Add mutually exclusive traits to excluded set
                if (MutuallyExclusivePairs.TryGetValue(selectedTrait, out var exclusives))
                {
                    foreach (var exclusive in exclusives)
                    {
                        excludedTraits.Add(exclusive);
                    }
                }
            }

            return selectedTraits;
        }

        public IReadOnlyCollection<LieutenantTrait> GenerateTraitsForFaction(int count, string factionId)
        {
            if (factionId == null)
            {
                throw new ArgumentNullException(nameof(factionId));
            }

            if (string.IsNullOrWhiteSpace(factionId))
            {
                throw new ArgumentException("Faction ID cannot be empty.", nameof(factionId));
            }

            var weights = GetFactionTraitWeights(factionId);
            return GenerateTraitsWithWeights(count, weights.ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
        }

        public IReadOnlyCollection<LieutenantTrait> GetMutuallyExclusiveTraits(LieutenantTrait trait)
        {
            if (MutuallyExclusivePairs.TryGetValue(trait, out var exclusives))
            {
                return exclusives.ToList();
            }

            return new List<LieutenantTrait>();
        }

        public IReadOnlyDictionary<LieutenantTrait, double> GetDefaultTraitWeights()
        {
            var allTraits = Enum.GetValues(typeof(LieutenantTrait)).Cast<LieutenantTrait>();
            return allTraits.ToDictionary(t => t, t => 1.0);
        }

        public IReadOnlyDictionary<LieutenantTrait, double> GetFactionTraitWeights(string factionId)
        {
            if (factionId == null)
            {
                throw new ArgumentNullException(nameof(factionId));
            }

            var weights = GetDefaultTraitWeights().ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            var normalizedFactionId = factionId.ToLowerInvariant();

            switch (normalizedFactionId)
            {
                case "michael":
                    // Michael: strategic, calculated - favors Cunning, Resourceful, Connected
                    weights[LieutenantTrait.Cunning] = 3.0;
                    weights[LieutenantTrait.Resourceful] = 3.0;
                    weights[LieutenantTrait.Connected] = 3.0;
                    break;

                case "trevor":
                    // Trevor: aggressive, combat-focused - favors Aggressive, Ruthless, Intimidating
                    weights[LieutenantTrait.Aggressive] = 3.0;
                    weights[LieutenantTrait.Ruthless] = 3.0;
                    weights[LieutenantTrait.Intimidating] = 3.0;
                    break;

                case "franklin":
                    // Franklin: loyal, opportunistic - favors Charismatic, Loyal, Veteran
                    weights[LieutenantTrait.Charismatic] = 3.0;
                    weights[LieutenantTrait.Loyal] = 3.0;
                    weights[LieutenantTrait.Veteran] = 3.0;
                    break;
            }

            return weights;
        }
    }
}
