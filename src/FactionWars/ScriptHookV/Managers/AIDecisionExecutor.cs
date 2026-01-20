using System;
using FactionWars.AI.Interfaces;
using FactionWars.AI.Models;
using FactionWars.Factions.Interfaces;

namespace FactionWars.ScriptHookV.Managers
{
    /// <summary>
    /// Coordinates AI decision execution with budget enforcement.
    /// </summary>
    public class AIDecisionExecutor
    {
        private readonly IFactionService _factionService;
        private readonly IAIBudgetService _budgetService;
        private readonly IAIRecruitmentService? _recruitmentService;

        /// <summary>
        /// Raised when an AI decision is about to be executed.
        /// </summary>
        public event EventHandler<AIDecisionEventArgs>? OnDecisionExecuting;

        public AIDecisionExecutor(
            IFactionService factionService,
            IAIBudgetService budgetService,
            IAIRecruitmentService? recruitmentService)
        {
            _factionService = factionService ?? throw new ArgumentNullException(nameof(factionService));
            _budgetService = budgetService ?? throw new ArgumentNullException(nameof(budgetService));
            _recruitmentService = recruitmentService;
        }

        /// <summary>
        /// Processes a full AI decision cycle: auto-recruits then executes attack.
        /// </summary>
        public void ProcessDecisionCycle(string factionId, AIDecision decision)
        {
            _recruitmentService?.TryAutoRecruit(factionId, maxTroopsToRecruit: 10);

            if (decision.DecisionType == AIDecisionType.Attack)
            {
                TryExecuteAttack(factionId, decision);
            }
        }

        /// <summary>
        /// Attempts to execute an attack with budget enforcement.
        /// </summary>
        public bool TryExecuteAttack(string factionId, AIDecision decision)
        {
            if (decision.DecisionType != AIDecisionType.Attack)
                return false;

            var state = _factionService.GetFactionState(factionId);
            if (state == null)
                return false;

            if (!_budgetService.CanAffordAttack(state.Cash, decision.TroopsToCommit))
                return false;

            var cost = _budgetService.CalculateAttackCost(decision.TroopsToCommit);
            _factionService.SpendCash(factionId, cost);

            OnDecisionExecuting?.Invoke(this, new AIDecisionEventArgs(factionId, decision));

            return true;
        }
    }
}
