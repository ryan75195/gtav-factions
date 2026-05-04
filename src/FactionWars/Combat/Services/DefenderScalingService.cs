using System;
using System.Collections.Generic;
using System.Linq;
using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Models;
using FactionWars.Core.Models;

namespace FactionWars.Combat.Services
{
    /// <summary>
    /// Service for scaling zone troop allocations to actual spawnable defender peds.
    /// Converts allocated troops (which can be many) to a reasonable number of peds
    /// while maintaining the proportional distribution across tiers.
    /// </summary>
    public class DefenderScalingService : IDefenderScalingService
    {
        /// <summary>
        /// Default number of troops represented by a single spawned ped.
        /// For example, with a factor of 5, 15 troops would spawn 3 peds.
        /// </summary>
        private const int DefaultScaleFactor = 5;

        /// <inheritdoc />
        public DefenderSpawnPlan CalculateSpawnPlan(Dictionary<DefenderTier, int> troopsByTier, int maxPeds)
        {
            if (troopsByTier == null)
                throw new ArgumentNullException(nameof(troopsByTier));
            if (maxPeds < 0)
                throw new ArgumentOutOfRangeException(nameof(maxPeds), "Max peds cannot be negative.");

            // Handle edge case: no max peds allowed
            if (maxPeds == 0)
                return new DefenderSpawnPlan();

            // Calculate total troops
            int totalTroops = troopsByTier.Values.Sum();
            if (totalTroops == 0)
                return new DefenderSpawnPlan();

            // Calculate proportional ped counts for each tier
            var plan = new DefenderSpawnPlan();
            var tiers = new[] { DefenderTier.Heavy, DefenderTier.Medium, DefenderTier.Basic };
            var rawCounts = CalculateRawCounts(troopsByTier, tiers, totalTroops, maxPeds);
            var wholeNumbers = AssignWholeNumbers(troopsByTier, tiers, rawCounts, out var remainders, out int totalAssigned);
            DistributeRemainingPeds(troopsByTier, tiers, wholeNumbers, remainders, maxPeds - totalAssigned);
            EnsureRepresentedTiers(troopsByTier, tiers, wholeNumbers, totalAssigned, maxPeds);

            foreach (var tier in tiers)
            {
                plan.SetPedCount(tier, wholeNumbers.TryGetValue(tier, out var c) ? c : 0);
            }

            return plan;
        }

        private static Dictionary<DefenderTier, double> CalculateRawCounts(
            Dictionary<DefenderTier, int> troopsByTier,
            DefenderTier[] tiers,
            int totalTroops,
            int maxPeds)
        {
            var rawCounts = new Dictionary<DefenderTier, double>();
            foreach (var tier in tiers)
            {
                int troops = troopsByTier.TryGetValue(tier, out var t) ? t : 0;
                double proportion = (double)troops / totalTroops;
                rawCounts[tier] = proportion * maxPeds;
            }

            return rawCounts;
        }

        private static Dictionary<DefenderTier, int> AssignWholeNumbers(
            Dictionary<DefenderTier, int> troopsByTier,
            DefenderTier[] tiers,
            Dictionary<DefenderTier, double> rawCounts,
            out Dictionary<DefenderTier, double> remainders,
            out int totalAssigned)
        {
            var wholeNumbers = new Dictionary<DefenderTier, int>();
            remainders = new Dictionary<DefenderTier, double>();
            totalAssigned = 0;

            foreach (var tier in tiers)
            {
                int troops = troopsByTier.TryGetValue(tier, out var t) ? t : 0;
                if (troops == 0)
                {
                    wholeNumbers[tier] = 0;
                    remainders[tier] = 0;
                    continue;
                }

                int whole = (int)rawCounts[tier];
                wholeNumbers[tier] = whole;
                remainders[tier] = rawCounts[tier] - whole;
                totalAssigned += whole;
            }

            return wholeNumbers;
        }

        private static void DistributeRemainingPeds(
            Dictionary<DefenderTier, int> troopsByTier,
            DefenderTier[] tiers,
            Dictionary<DefenderTier, int> wholeNumbers,
            Dictionary<DefenderTier, double> remainders,
            int pedsToDistribute)
        {
            var orderedByRemainder = tiers
                .Where(t => troopsByTier.TryGetValue(t, out var c) && c > 0)
                .OrderByDescending(t => remainders[t])
                .ToList();

            int idx = 0;
            while (pedsToDistribute > 0 && orderedByRemainder.Count > 0)
            {
                var tier = orderedByRemainder[idx % orderedByRemainder.Count];
                wholeNumbers[tier]++;
                pedsToDistribute--;
                idx++;
            }
        }

        private static void EnsureRepresentedTiers(
            Dictionary<DefenderTier, int> troopsByTier,
            DefenderTier[] tiers,
            Dictionary<DefenderTier, int> wholeNumbers,
            int totalAssigned,
            int maxPeds)
        {
            foreach (var tier in tiers)
            {
                int troops = troopsByTier.TryGetValue(tier, out var t) ? t : 0;
                if (troops > 0 && wholeNumbers[tier] == 0 && totalAssigned < maxPeds)
                {
                    // Find a tier with more than its share to borrow from
                    var tierToBorrowFrom = tiers
                        .Where(tt => wholeNumbers[tt] > 1)
                        .OrderByDescending(tt => wholeNumbers[tt])
                        .FirstOrDefault();

                    if (tierToBorrowFrom != default)
                    {
                        wholeNumbers[tierToBorrowFrom]--;
                        wholeNumbers[tier] = 1;
                    }
                }
            }
        }

        /// <inheritdoc />
        public int CalculateScaledDefenderCount(int troopCount, int scaleFactor)
        {
            if (troopCount <= 0)
                return 0;

            if (scaleFactor <= 0)
                scaleFactor = DefaultScaleFactor;

            // Calculate scaled count, with minimum of 1 for any non-zero troop count
            int scaled = (troopCount + scaleFactor - 1) / scaleFactor; // Ceiling division
            return scaled > 0 ? scaled : 1;
        }

        /// <inheritdoc />
        public int GetDefaultScaleFactor()
        {
            return DefaultScaleFactor;
        }
    }
}
