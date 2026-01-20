namespace FactionWars.AI.Interfaces
{
    public interface IAIBudgetService
    {
        int CalculateAttackCost(int troopsToCommit);
        bool CanAffordAttack(int factionCash, int troopsToCommit);
        int CalculateRecruitmentCost(int troopsToRecruit);
        int CostPerTroop { get; }
        int RecruitCostPerTroop { get; }
    }
}
