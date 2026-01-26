using System;
using System.Collections.Generic;
using FactionWars.AI.Interfaces;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.Factions.Interfaces;

namespace FactionWars.AI.Services
{
    /// <summary>
    /// Service for automatic AI faction troop recruitment with wealth-scaled tier distribution.
    /// </summary>
    public class AIRecruitmentService : IAIRecruitmentService
    {
        private readonly IFactionService _factionService;
        private readonly IAIBudgetService _budgetService;
        private readonly IDefenderTierService? _tierService;

        // Wealth thresholds
        private const int LowWealthThreshold = 5000;
        private const int MidWealthThreshold = 15000;
        private const int HighWealthThreshold = 30000;

        /// <summary>
        /// Creates a new AIRecruitmentService with basic tier support only.
        /// </summary>
        public AIRecruitmentService(IFactionService factionService, IAIBudgetService budgetService)
        {
            _factionService = factionService ?? throw new ArgumentNullException(nameof(factionService));
            _budgetService = budgetService ?? throw new ArgumentNullException(nameof(budgetService));
            _tierService = null;
        }

        /// <summary>
        /// Creates a new AIRecruitmentService with multi-tier wealth-scaled recruitment.
        /// </summary>
        public AIRecruitmentService(IFactionService factionService, IAIBudgetService budgetService, IDefenderTierService tierService)
        {
            _factionService = factionService ?? throw new ArgumentNullException(nameof(factionService));
            _budgetService = budgetService ?? throw new ArgumentNullException(nameof(budgetService));
            _tierService = tierService ?? throw new ArgumentNullException(nameof(tierService));
        }

        /// <summary>
        /// Attempts to auto-recruit troops for a faction based on their wealth level.
        /// Uses wealth-scaled tier distribution when IDefenderTierService is available.
        /// </summary>
        /// <param name="factionId">The faction to recruit for.</param>
        /// <param name="maxTroopsToRecruit">Maximum troops to recruit per cycle (default 10).</param>
        /// <returns>The total number of troops recruited.</returns>
        public int TryAutoRecruit(string factionId, int maxTroopsToRecruit = 10)
        {
            if (string.IsNullOrEmpty(factionId))
                return 0;

            var state = _factionService.GetFactionState(factionId);
            if (state == null)
                return 0;

            // Use multi-tier recruitment if tier service is available
            if (_tierService != null)
            {
                return TryAutoRecruitMultiTier(factionId, state.Cash, maxTroopsToRecruit);
            }

            // Legacy basic-only recruitment
            return TryAutoRecruitBasicOnly(factionId, state.Cash, maxTroopsToRecruit);
        }

        /// <summary>
        /// Legacy recruitment method - only recruits Basic tier troops.
        /// </summary>
        private int TryAutoRecruitBasicOnly(string factionId, int cash, int maxTroops)
        {
            int affordableTroops = cash / _budgetService.RecruitCostPerTroop;
            int troopsToRecruit = Math.Min(affordableTroops, maxTroops);

            if (troopsToRecruit <= 0)
                return 0;

            int cost = _budgetService.CalculateRecruitmentCost(troopsToRecruit);

            _factionService.RecruitTroops(factionId, troopsToRecruit);
            _factionService.SpendCash(factionId, cost);

            return troopsToRecruit;
        }

        /// <summary>
        /// Multi-tier recruitment with wealth-scaled distribution.
        /// Elite Purchase Thresholds:
        /// - Below $15k: 0 Elite
        /// - $15k - $30k: 1 Elite
        /// - Above $30k: 2 Elite
        ///
        /// Wealth-Scaled Tier Distribution (for remaining budget after Elite):
        /// - Below $5k: 100% Basic
        /// - $5k - $15k: 60% Basic, 30% Medium, 10% Heavy
        /// - $15k - $30k: 40% Basic, 30% Medium, 20% Heavy
        /// - Above $30k: 20% Basic, 30% Medium, 40% Heavy
        /// </summary>
        private int TryAutoRecruitMultiTier(string factionId, int cash, int maxTroops)
        {
            if (cash <= 0 || maxTroops <= 0)
                return 0;

            var recruited = new Dictionary<DefenderTier, int>
            {
                { DefenderTier.Basic, 0 },
                { DefenderTier.Medium, 0 },
                { DefenderTier.Heavy, 0 },
                { DefenderTier.Elite, 0 }
            };

            int remainingBudget = cash;
            int remainingSlots = maxTroops;

            // Step 1: Buy Elite based on wealth thresholds
            int eliteToBuy = GetEliteCountForWealth(cash);
            int eliteCost = _tierService!.GetCost(DefenderTier.Elite);

            for (int i = 0; i < eliteToBuy && remainingSlots > 0 && remainingBudget >= eliteCost; i++)
            {
                recruited[DefenderTier.Elite]++;
                remainingBudget -= eliteCost;
                remainingSlots--;
            }

            // Step 2: Get tier distribution percentages based on remaining cash
            var distribution = GetTierDistributionForWealth(remainingBudget);

            // Step 3: Calculate how many of each standard tier to buy
            int standardTroopsToBuy = remainingSlots;

            int basicCount = (int)Math.Round(standardTroopsToBuy * distribution.BasicPercent);
            int mediumCount = (int)Math.Round(standardTroopsToBuy * distribution.MediumPercent);
            int heavyCount = (int)Math.Round(standardTroopsToBuy * distribution.HeavyPercent);

            // Ensure we don't exceed remaining slots due to rounding
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

            // Step 4: Buy troops within budget constraints
            int basicCost = _tierService.GetCost(DefenderTier.Basic);
            int mediumCost = _tierService.GetCost(DefenderTier.Medium);
            int heavyCost = _tierService.GetCost(DefenderTier.Heavy);

            // Buy Heavy first (most expensive, prioritize)
            for (int i = 0; i < heavyCount && remainingBudget >= heavyCost; i++)
            {
                recruited[DefenderTier.Heavy]++;
                remainingBudget -= heavyCost;
            }

            // Buy Medium
            for (int i = 0; i < mediumCount && remainingBudget >= mediumCost; i++)
            {
                recruited[DefenderTier.Medium]++;
                remainingBudget -= mediumCost;
            }

            // Buy Basic with remaining budget
            for (int i = 0; i < basicCount && remainingBudget >= basicCost; i++)
            {
                recruited[DefenderTier.Basic]++;
                remainingBudget -= basicCost;
            }

            // Step 5: Calculate total cost and apply
            int totalRecruited = 0;
            int totalCost = 0;

            foreach (var kvp in recruited)
            {
                if (kvp.Value > 0)
                {
                    _factionService.AddReserveTroops(factionId, kvp.Key, kvp.Value);
                    totalRecruited += kvp.Value;
                    totalCost += _tierService.GetCost(kvp.Key) * kvp.Value;
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
        private (double BasicPercent, double MediumPercent, double HeavyPercent) GetTierDistributionForWealth(int cash)
        {
            if (cash >= HighWealthThreshold)
                return (0.20, 0.30, 0.40); // 90% total, remainder goes to Basic
            if (cash >= MidWealthThreshold)
                return (0.40, 0.30, 0.20); // 90% total, remainder goes to Basic
            if (cash >= LowWealthThreshold)
                return (0.60, 0.30, 0.10);

            // Below $5k - only Basic
            return (1.0, 0.0, 0.0);
        }
    }
}
