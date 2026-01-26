using FactionWars.AI.Interfaces;
using FactionWars.AI.Models;
using FactionWars.Core.Interfaces;
using FactionWars.Factions.Models;
using FactionWars.Territory.Models;
using System.Collections.Generic;
using Xunit;
using Moq;

namespace FactionWars.Tests.Unit.AI.Services
{
    /// <summary>
    /// Tests for the ICapitalDeploymentService interface contract.
    /// Verifies the expected behavior that implementations must fulfill.
    /// </summary>
    public class ICapitalDeploymentServiceTests
    {
        #region Interface Existence Tests

        [Fact]
        public void ICapitalDeploymentService_InterfaceExists_CanBeReferenced()
        {
            // Verify the interface exists and can be mocked
            var mock = new Mock<ICapitalDeploymentService>();
            Assert.NotNull(mock.Object);
        }

        [Fact]
        public void ICapitalDeploymentService_GetDefensePriority_MethodExists()
        {
            // Verify GetDefensePriority method signature
            var mock = new Mock<ICapitalDeploymentService>();
            var zone = CreateTestZone("test-zone");
            var context = CreateTestContext();

            mock.Setup(s => s.GetDefensePriority(zone, context)).Returns(0.5f);

            var result = mock.Object.GetDefensePriority(zone, context);
            Assert.Equal(0.5f, result);
        }

        [Fact]
        public void ICapitalDeploymentService_GetAttackOpportunity_MethodExists()
        {
            // Verify GetAttackOpportunity method signature
            var mock = new Mock<ICapitalDeploymentService>();
            var zone = CreateTestZone("target-zone");
            var context = CreateTestContext();

            mock.Setup(s => s.GetAttackOpportunity(zone, context)).Returns(0.7f);

            var result = mock.Object.GetAttackOpportunity(zone, context);
            Assert.Equal(0.7f, result);
        }

        [Fact]
        public void ICapitalDeploymentService_GetScaledRecruitmentMax_MethodExists()
        {
            // Verify GetScaledRecruitmentMax method signature
            var mock = new Mock<ICapitalDeploymentService>();

            mock.Setup(s => s.GetScaledRecruitmentMax(50000)).Returns(15);

            var result = mock.Object.GetScaledRecruitmentMax(50000);
            Assert.Equal(15, result);
        }

        [Fact]
        public void ICapitalDeploymentService_GetOverwhelmingAttackForce_MethodExists()
        {
            // Verify GetOverwhelmingAttackForce method signature
            var mock = new Mock<ICapitalDeploymentService>();

            mock.Setup(s => s.GetOverwhelmingAttackForce(100, 20)).Returns(60);

            var result = mock.Object.GetOverwhelmingAttackForce(100, 20);
            Assert.Equal(60, result);
        }

        [Fact]
        public void ICapitalDeploymentService_GetBestDecision_MethodExists()
        {
            // Verify GetBestDecision method signature - returns nullable AIDecision
            var mock = new Mock<ICapitalDeploymentService>();
            var context = CreateTestContext();
            var decision = new AIDecision(AIDecisionType.Attack, "zone-1", 0.8f, 30);

            mock.Setup(s => s.GetBestDecision(context)).Returns(decision);

            var result = mock.Object.GetBestDecision(context);
            Assert.NotNull(result);
            Assert.Equal(AIDecisionType.Attack, result!.DecisionType);
        }

        [Fact]
        public void ICapitalDeploymentService_GetBestDecision_CanReturnNull()
        {
            // GetBestDecision returns null for Hold (no action needed)
            var mock = new Mock<ICapitalDeploymentService>();
            var context = CreateTestContext();

            mock.Setup(s => s.GetBestDecision(context)).Returns((AIDecision?)null);

            var result = mock.Object.GetBestDecision(context);
            Assert.Null(result);
        }

        #endregion

        #region Helper Methods

        private Faction CreateTestFaction(FactionType type, string? id = null)
        {
            var factionId = id ?? $"faction-{type.ToString().ToLower()}";
            var info = FactionTypeInfo.GetInfo(type);
            return new Faction(
                id: factionId,
                name: info.FactionName,
                leader: info.LeaderName,
                description: info.Description,
                color: info.Color);
        }

        private FactionState CreateTestFactionState(string factionId = "faction-michael")
        {
            return new FactionState(
                factionId: factionId,
                initialCash: 10000,
                initialTroopCount: 50);
        }

        private Zone CreateTestZone(string id, string? ownerFactionId = null)
        {
            var zone = new Zone(
                id: id,
                name: $"Test Zone {id}",
                center: new Vector3(0, 0, 0),
                radius: 150f,
                strategicValue: 5);
            zone.OwnerFactionId = ownerFactionId;
            return zone;
        }

        private AIContext CreateTestContext()
        {
            var faction = CreateTestFaction(FactionType.Michael);
            var factionState = CreateTestFactionState();
            var ownedZones = new List<Zone> { CreateTestZone("zone-1", ownerFactionId: faction.Id) };
            var allZones = new List<Zone>
            {
                CreateTestZone("zone-1", ownerFactionId: faction.Id),
                CreateTestZone("zone-2", ownerFactionId: "enemy-faction"),
                CreateTestZone("zone-3")
            };
            var enemyFactions = new List<Faction> { CreateTestFaction(FactionType.Trevor, "enemy-faction") };

            return new AIContext(faction, factionState, ownedZones, allZones, enemyFactions);
        }

        #endregion
    }
}
