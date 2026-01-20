using System;
using FactionWars.AI.Interfaces;
using FactionWars.Factions.Interfaces;

namespace FactionWars.AI.Services
{
    public class AIRecruitmentService : IAIRecruitmentService
    {
        private readonly IFactionService _factionService;
        private readonly IAIBudgetService _budgetService;

        public AIRecruitmentService(IFactionService factionService, IAIBudgetService budgetService)
        {
            _factionService = factionService ?? throw new ArgumentNullException(nameof(factionService));
            _budgetService = budgetService ?? throw new ArgumentNullException(nameof(budgetService));
        }

        public int TryAutoRecruit(string factionId, int maxTroopsToRecruit = 10)
        {
            if (string.IsNullOrEmpty(factionId))
                return 0;

            var state = _factionService.GetFactionState(factionId);
            if (state == null)
                return 0;

            int affordableTroops = state.Cash / _budgetService.RecruitCostPerTroop;
            int troopsToRecruit = Math.Min(affordableTroops, maxTroopsToRecruit);

            if (troopsToRecruit <= 0)
                return 0;

            int cost = _budgetService.CalculateRecruitmentCost(troopsToRecruit);

            _factionService.RecruitTroops(factionId, troopsToRecruit);
            _factionService.AddCash(factionId, -cost);

            return troopsToRecruit;
        }
    }
}
