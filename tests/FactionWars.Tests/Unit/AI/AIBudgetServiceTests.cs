using FactionWars.AI.Interfaces;
using FactionWars.AI.Services;
using Xunit;

namespace FactionWars.Tests.Unit.AI
{
    public class AIBudgetServiceTests
    {
        [Fact]
        public void CalculateAttackCost_ReturnsCorrectCost()
        {
            var service = new AIBudgetService(costPerTroop: 50);
            var cost = service.CalculateAttackCost(troopsToCommit: 10);
            Assert.Equal(500, cost);
        }

        [Fact]
        public void CanAffordAttack_ReturnsTrueWhenSufficientFunds()
        {
            var service = new AIBudgetService(costPerTroop: 50);
            var canAfford = service.CanAffordAttack(factionCash: 1000, troopsToCommit: 10);
            Assert.True(canAfford);
        }

        [Fact]
        public void CanAffordAttack_ReturnsFalseWhenInsufficientFunds()
        {
            var service = new AIBudgetService(costPerTroop: 50);
            var canAfford = service.CanAffordAttack(factionCash: 400, troopsToCommit: 10);
            Assert.False(canAfford);
        }

        [Fact]
        public void CalculateRecruitmentCost_ReturnsCorrectCost()
        {
            var service = new AIBudgetService(costPerTroop: 50, recruitCostPerTroop: 100);
            var cost = service.CalculateRecruitmentCost(troopsToRecruit: 5);
            Assert.Equal(500, cost);
        }
    }
}
