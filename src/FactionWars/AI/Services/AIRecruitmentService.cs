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
    public partial class AIRecruitmentService : IAIRecruitmentService
    {
        private readonly IFactionService _factionService;
        private readonly IAIBudgetService _budgetService;
        private readonly IDefenderRoleService? _tierService;
        private readonly ICapitalDeploymentService? _capitalDeploymentService;
        private IDefenderRoleService TierService =>
            _tierService ?? throw new InvalidOperationException("Tier service is required for multi-tier recruitment.");

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
        public AIRecruitmentService(IFactionService factionService, IAIBudgetService budgetService, IDefenderRoleService tierService)
        {
            _factionService = factionService ?? throw new ArgumentNullException(nameof(factionService));
            _budgetService = budgetService ?? throw new ArgumentNullException(nameof(budgetService));
            _tierService = tierService ?? throw new ArgumentNullException(nameof(tierService));
        }

        /// <summary>
        /// Creates a new AIRecruitmentService with multi-tier wealth-scaled recruitment
        /// and dynamic scaled recruitment limits from ICapitalDeploymentService.
        /// </summary>
        public AIRecruitmentService(
            IFactionService factionService,
            IAIBudgetService budgetService,
            IDefenderRoleService tierService,
            ICapitalDeploymentService capitalDeploymentService)
            : this(factionService, budgetService, tierService)
        {
            _capitalDeploymentService = capitalDeploymentService ?? throw new ArgumentNullException(nameof(capitalDeploymentService));
        }

        /// <summary>
        /// Attempts to auto-recruit troops for a faction based on their wealth level.
        /// Uses wealth-scaled tier distribution when IDefenderRoleService is available.
        /// When ICapitalDeploymentService is available, uses scaled recruitment max based on cash.
        /// </summary>
        /// <param name="factionId">The faction to recruit for.</param>
        /// <param name="maxTroopsToRecruit">Maximum troops to recruit per cycle (default 10). Ignored when ICapitalDeploymentService is available.</param>
        /// <returns>The total number of troops recruited.</returns>
        public int TryAutoRecruit(string factionId, int maxTroopsToRecruit = 10)
        {
            if (string.IsNullOrEmpty(factionId))
                return 0;

            var state = _factionService.GetFactionState(factionId);
            if (state == null)
                return 0;

            // Use scaled max if capital deployment service is available
            int effectiveMax = maxTroopsToRecruit;
            if (_capitalDeploymentService != null)
            {
                effectiveMax = _capitalDeploymentService.GetScaledRecruitmentMax(state.Cash);
            }

            // Use multi-tier recruitment if tier service is available
            if (_tierService != null)
            {
                return TryAutoRecruitMultiTier(factionId, state.Cash, effectiveMax);
            }

            // Legacy basic-only recruitment
            return TryAutoRecruitBasicOnly(factionId, state.Cash, effectiveMax);
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

            var recruited = new Dictionary<DefenderRole, int>
            {
                { DefenderRole.Grunt, 0 },
                { DefenderRole.Gunner, 0 },
                { DefenderRole.Rifleman, 0 },
                { DefenderRole.Rocketeer, 0 },
                { DefenderRole.Sniper, 0 }
            };

            int remainingBudget = BuySnipers(cash, maxTroops, recruited, out int remainingSlots);
            remainingBudget = BuyEliteTroops(cash, remainingBudget, remainingSlots, recruited, out remainingSlots);
            BuyStandardTroops(remainingBudget, remainingSlots, recruited);
            return ApplyRecruitment(factionId, recruited);
        }

    }
}
