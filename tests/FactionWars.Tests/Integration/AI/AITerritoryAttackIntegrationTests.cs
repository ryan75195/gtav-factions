using FactionWars.AI.Interfaces;
using FactionWars.AI.Models;
using FactionWars.AI.Services;
using FactionWars.Factions.Interfaces;
using FactionWars.Factions.Models;
using FactionWars.Factions.Repositories;
using FactionWars.Factions.Services;
using FactionWars.ScriptHookV.Managers;
using Xunit;

namespace FactionWars.Tests.Integration.AI
{
    /// <summary>
    /// Integration tests for AI territory attack cycles.
    /// Tests the full flow from AIDecisionExecutor through budget checking
    /// using real service implementations without mocks.
    /// </summary>
    public class AITerritoryAttackIntegrationTests
    {
        [Fact]
        public void AIFaction_WithBudget_CanCaptureUnguardedTerritory()
        {
            // Arrange - Set up full integration scenario
            var factionRepository = new InMemoryFactionRepository();
            var factionService = new FactionService(factionRepository);
            var budgetService = new AIBudgetService(costPerTroop: 50, recruitCostPerTroop: 100);
            var recruitmentService = new AIRecruitmentService(factionService, budgetService);

            // Create faction
            var trevor = new Faction("trevor", "Trevor's Crew");
            factionRepository.Add(trevor);

            // Initialize state with cash and troops
            factionService.InitializeFactionState("trevor", initialCash: 5000, initialTroops: 20);

            // Create decision executor
            var executor = new AIDecisionExecutor(factionService, budgetService, recruitmentService);

            // Create an attack decision
            var decision = new AIDecision(AIDecisionType.Attack, "zone1", 0.8f, 10);

            // Act - Execute the attack
            var result = executor.TryExecuteAttack("trevor", decision);

            // Assert
            Assert.True(result);

            // Verify cash was deducted
            var trevorState = factionService.GetFactionState("trevor");
            Assert.NotNull(trevorState);
            Assert.Equal(4500, trevorState.Cash); // 5000 - (10 * 50)
        }

        [Fact]
        public void AIFaction_WithoutBudget_CannotAttack()
        {
            // Arrange
            var factionRepository = new InMemoryFactionRepository();
            var factionService = new FactionService(factionRepository);
            var budgetService = new AIBudgetService(costPerTroop: 50);

            // Create faction
            var trevor = new Faction("trevor", "Trevor's Crew");
            factionRepository.Add(trevor);

            // Initialize state with only $100
            factionService.InitializeFactionState("trevor", initialCash: 100, initialTroops: 20);

            var executor = new AIDecisionExecutor(factionService, budgetService, null);
            var decision = new AIDecision(AIDecisionType.Attack, "zone1", 0.8f, 10); // Cost: $500

            // Act
            var result = executor.TryExecuteAttack("trevor", decision);

            // Assert
            Assert.False(result);

            // Verify no cash was deducted
            var trevorState = factionService.GetFactionState("trevor");
            Assert.NotNull(trevorState);
            Assert.Equal(100, trevorState.Cash);
        }
    }
}
