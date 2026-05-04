using FactionWars.AI.Interfaces;
using FactionWars.AI.Models;
using FactionWars.AI.Services;
using FactionWars.Factions.Interfaces;
using FactionWars.Factions.Models;
using FactionWars.ScriptHookV.Managers;
using Moq;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV.Managers
{
    public class AIDecisionExecutorTests
    {
        [Fact]
        public void ExecuteAttack_WithSufficientBudget_DeductsCostAndReturnsTrue()
        {
            var mockFactionService = new Mock<IFactionService>();
            var budgetService = new AIBudgetService(costPerTroop: 50);

            var factionState = new FactionState("attacker", initialCash: 1000, initialTroopCount: 20);
            mockFactionService.Setup(f => f.GetFactionState("attacker")).Returns(factionState);
            mockFactionService.Setup(f => f.SpendCash("attacker", It.IsAny<int>())).Returns(true);

            var executor = new AIDecisionExecutor(
                mockFactionService.Object,
                budgetService,
                null);

            var decision = new AIDecision(AIDecisionType.Attack, "zone1", 0.8f, 10);

            var result = executor.TryExecuteAttack("attacker", decision);

            Assert.True(result);
            mockFactionService.Verify(f => f.SpendCash("attacker", 500), Times.Once);
        }

        [Fact]
        public void ExecuteAttack_WithInsufficientBudget_ReturnsFalse()
        {
            var mockFactionService = new Mock<IFactionService>();
            var budgetService = new AIBudgetService(costPerTroop: 50);

            var factionState = new FactionState("attacker", initialCash: 100, initialTroopCount: 20);
            mockFactionService.Setup(f => f.GetFactionState("attacker")).Returns(factionState);

            var executor = new AIDecisionExecutor(
                mockFactionService.Object,
                budgetService,
                null);

            var decision = new AIDecision(AIDecisionType.Attack, "zone1", 0.8f, 10);

            var result = executor.TryExecuteAttack("attacker", decision);

            Assert.False(result);
            mockFactionService.Verify(f => f.SpendCash(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public void ProcessDecisionCycle_WithAttack_AutoRecruitsAndExecutesDecision()
        {
            var mockFactionService = new Mock<IFactionService>();
            var recruitmentService = new Mock<IAIRecruitmentService>();
            var budgetService = new AIBudgetService(costPerTroop: 50);
            var factionState = new FactionState("attacker", initialCash: 1000, initialTroopCount: 20);
            mockFactionService.Setup(f => f.GetFactionState("attacker")).Returns(factionState);
            mockFactionService.Setup(f => f.SpendCash("attacker", It.IsAny<int>())).Returns(true);
            var executor = new AIDecisionExecutor(mockFactionService.Object, budgetService, recruitmentService.Object);
            var decision = new AIDecision(AIDecisionType.Attack, "zone1", 0.8f, 10);

            executor.ProcessDecisionCycle("attacker", decision);

            recruitmentService.Verify(r => r.TryAutoRecruit("attacker", 10), Times.Once);
            mockFactionService.Verify(f => f.SpendCash("attacker", 500), Times.Once);
        }
    }
}
