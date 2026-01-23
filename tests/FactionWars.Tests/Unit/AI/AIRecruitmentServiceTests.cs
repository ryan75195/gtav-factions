using FactionWars.AI.Interfaces;
using FactionWars.AI.Services;
using FactionWars.Factions.Interfaces;
using FactionWars.Factions.Models;
using Moq;
using Xunit;

namespace FactionWars.Tests.Unit.AI
{
    public class AIRecruitmentServiceTests
    {
        [Fact]
        public void TryAutoRecruit_WithSufficientFunds_RecruitsTroops()
        {
            var mockFactionService = new Mock<IFactionService>();
            var factionState = new FactionState("test", initialCash: 1000, initialTroopCount: 5);
            mockFactionService.Setup(f => f.GetFactionState("test")).Returns(factionState);
            mockFactionService.Setup(f => f.RecruitTroops("test", It.IsAny<int>())).Returns(true);
            mockFactionService.Setup(f => f.SpendCash("test", It.IsAny<int>())).Returns(true);

            var budgetService = new AIBudgetService(costPerTroop: 50, recruitCostPerTroop: 100);
            var service = new AIRecruitmentService(mockFactionService.Object, budgetService);

            var recruited = service.TryAutoRecruit("test", maxTroopsToRecruit: 5);

            Assert.Equal(5, recruited);
            mockFactionService.Verify(f => f.RecruitTroops("test", 5), Times.Once);
            mockFactionService.Verify(f => f.SpendCash("test", 500), Times.Once);
        }

        [Fact]
        public void TryAutoRecruit_WithInsufficientFunds_RecruitsAffordableAmount()
        {
            var mockFactionService = new Mock<IFactionService>();
            var factionState = new FactionState("test", initialCash: 250, initialTroopCount: 5);
            mockFactionService.Setup(f => f.GetFactionState("test")).Returns(factionState);
            mockFactionService.Setup(f => f.RecruitTroops("test", It.IsAny<int>())).Returns(true);
            mockFactionService.Setup(f => f.AddCash("test", It.IsAny<int>())).Returns(true);

            var budgetService = new AIBudgetService(costPerTroop: 50, recruitCostPerTroop: 100);
            var service = new AIRecruitmentService(mockFactionService.Object, budgetService);

            var recruited = service.TryAutoRecruit("test", maxTroopsToRecruit: 10);

            Assert.Equal(2, recruited);
            mockFactionService.Verify(f => f.RecruitTroops("test", 2), Times.Once);
        }

        [Fact]
        public void TryAutoRecruit_WithNoFunds_ReturnsZero()
        {
            var mockFactionService = new Mock<IFactionService>();
            var factionState = new FactionState("test", initialCash: 50, initialTroopCount: 5);
            mockFactionService.Setup(f => f.GetFactionState("test")).Returns(factionState);

            var budgetService = new AIBudgetService(costPerTroop: 50, recruitCostPerTroop: 100);
            var service = new AIRecruitmentService(mockFactionService.Object, budgetService);

            var recruited = service.TryAutoRecruit("test", maxTroopsToRecruit: 10);

            Assert.Equal(0, recruited);
            mockFactionService.Verify(f => f.RecruitTroops(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        }
    }
}
