using System;
using FactionWars.AI.Interfaces;

namespace FactionWars.AI.Services
{
    public class AIBudgetService : IAIBudgetService
    {
        private readonly int _costPerTroop;
        private readonly int _recruitCostPerTroop;

        public AIBudgetService(int costPerTroop = 50, int recruitCostPerTroop = 100)
        {
            _costPerTroop = Math.Max(1, costPerTroop);
            _recruitCostPerTroop = Math.Max(1, recruitCostPerTroop);
        }

        public int CostPerTroop => _costPerTroop;
        public int RecruitCostPerTroop => _recruitCostPerTroop;

        public int CalculateAttackCost(int troopsToCommit)
        {
            return Math.Max(0, troopsToCommit) * _costPerTroop;
        }

        public bool CanAffordAttack(int factionCash, int troopsToCommit)
        {
            var cost = CalculateAttackCost(troopsToCommit);
            return factionCash >= cost;
        }

        public int CalculateRecruitmentCost(int troopsToRecruit)
        {
            return Math.Max(0, troopsToRecruit) * _recruitCostPerTroop;
        }
    }
}
