using System;
using System.Collections.Generic;
using FactionWars.Core.Models;
using FactionWars.Factions.Models;

namespace FactionWars.AI.Services
{
    public partial class AIRecruitmentService
    {
        private int BuyEliteTroops(
            int cash,
            int maxTroops,
            Dictionary<DefenderRole, int> recruited,
            out int remainingSlots)
        {
            int remainingBudget = cash;
            remainingSlots = maxTroops;
            int eliteToBuy = GetEliteCountForWealth(cash);
            int eliteCost = TierService.GetCost(DefenderRole.Rocketeer);

            for (int i = 0; i < eliteToBuy && remainingSlots > 0 && remainingBudget >= eliteCost; i++)
            {
                recruited[DefenderRole.Rocketeer]++;
                remainingBudget -= eliteCost;
                remainingSlots--;
            }

            return remainingBudget;
        }

        private void BuyStandardTroops(
            int remainingBudget,
            int remainingSlots,
            Dictionary<DefenderRole, int> recruited)
        {
            var distribution = GetTierDistributionForWealth(remainingBudget);
            int standardTroopsToBuy = remainingSlots;
            int basicCount = (int)Math.Round(standardTroopsToBuy * distribution.BasicPercent);
            int mediumCount = (int)Math.Round(standardTroopsToBuy * distribution.MediumPercent);
            int heavyCount = (int)Math.Round(standardTroopsToBuy * distribution.HeavyPercent);
            NormalizeStandardCounts(remainingSlots, ref basicCount, ref mediumCount, ref heavyCount);

            remainingBudget = BuyTier(recruited, DefenderRole.Rifleman, heavyCount, remainingBudget);
            remainingBudget = BuyTier(recruited, DefenderRole.Gunner, mediumCount, remainingBudget);
            BuyTier(recruited, DefenderRole.Grunt, basicCount, remainingBudget);
        }

        private static void NormalizeStandardCounts(
            int remainingSlots,
            ref int basicCount,
            ref int mediumCount,
            ref int heavyCount)
        {
            int totalStandard = basicCount + mediumCount + heavyCount;
            if (totalStandard > remainingSlots)
            {
                // Reduce from largest tier first
                int excess = totalStandard - remainingSlots;
                if (heavyCount >= excess)
                    heavyCount -= excess;
                else
                {
                    excess -= heavyCount;
                    heavyCount = 0;
                    if (mediumCount >= excess)
                        mediumCount -= excess;
                    else
                    {
                        excess -= mediumCount;
                        mediumCount = 0;
                        basicCount -= excess;
                    }
                }
            }
            else if (totalStandard < remainingSlots)
            {
                // Add remainder to basic
                basicCount += (remainingSlots - totalStandard);
            }
        }

        private int BuyTier(
            Dictionary<DefenderRole, int> recruited,
            DefenderRole tier,
            int count,
            int remainingBudget)
        {
            int cost = TierService.GetCost(tier);
            for (int i = 0; i < count && remainingBudget >= cost; i++)
            {
                recruited[tier]++;
                remainingBudget -= cost;
            }

            return remainingBudget;
        }

        private int ApplyRecruitment(string factionId, Dictionary<DefenderRole, int> recruited)
        {
            int totalRecruited = 0;
            int totalCost = 0;

            foreach (var kvp in recruited)
            {
                if (kvp.Value > 0)
                {
                    _factionService.AddReserveTroops(factionId, kvp.Key, kvp.Value);
                    totalRecruited += kvp.Value;
                    totalCost += TierService.GetCost(kvp.Key) * kvp.Value;
                }
            }

            if (totalCost > 0)
            {
                _factionService.SpendCash(factionId, totalCost);
            }

            return totalRecruited;
        }

        /// <summary>
        /// Determines how many Elite troops to buy based on faction wealth.
        /// </summary>
        private int GetEliteCountForWealth(int cash)
        {
            if (cash >= HighWealthThreshold)
                return 2;
            if (cash >= MidWealthThreshold)
                return 1;
            return 0;
        }

        /// <summary>
        /// Gets the tier distribution percentages based on wealth level.
        /// </summary>
        private TierDistribution GetTierDistributionForWealth(int cash)
        {
            if (cash >= HighWealthThreshold)
                return new TierDistribution(0.20, 0.30, 0.40); // 90% total, remainder goes to Basic
            if (cash >= MidWealthThreshold)
                return new TierDistribution(0.40, 0.30, 0.20); // 90% total, remainder goes to Basic
            if (cash >= LowWealthThreshold)
                return new TierDistribution(0.60, 0.30, 0.10);

            // Below $5k - only Basic
            return new TierDistribution(1.0, 0.0, 0.0);
        }

        private readonly struct TierDistribution
        {
            public TierDistribution(double basicPercent, double mediumPercent, double heavyPercent)
            {
                BasicPercent = basicPercent;
                MediumPercent = mediumPercent;
                HeavyPercent = heavyPercent;
            }

            public double BasicPercent { get; }
            public double MediumPercent { get; }
            public double HeavyPercent { get; }
        }
    }
}
