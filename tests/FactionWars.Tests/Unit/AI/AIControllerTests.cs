using FactionWars.AI.Controllers;
using FactionWars.AI.Interfaces;
using FactionWars.Combat.Interfaces;
using FactionWars.Core.Interfaces;
using FactionWars.Factions.Interfaces;
using FactionWars.Factions.Models;
using FactionWars.Territory.Interfaces;
using FactionWars.Territory.Models;
using Moq;
using System.Collections.Generic;
using Xunit;

namespace FactionWars.Tests.Unit.AI
{
    public class AIControllerTests
    {
        private readonly Mock<IFactionService> _factionServiceMock;
        private readonly Mock<IZoneService> _zoneServiceMock;
        private readonly Mock<IBattleSimulationService> _battleSimulationServiceMock;
        private readonly Mock<IZoneDefenderAllocationService> _allocationServiceMock;
        private readonly Mock<IGameBridge> _gameBridgeMock;
        private readonly Mock<IZoneBattleManager> _zoneBattleManagerMock;
        private readonly Dictionary<string, IAIStrategy> _strategies;

        public AIControllerTests()
        {
            _factionServiceMock = new Mock<IFactionService>();
            _zoneServiceMock = new Mock<IZoneService>();
            _battleSimulationServiceMock = new Mock<IBattleSimulationService>();
            _allocationServiceMock = new Mock<IZoneDefenderAllocationService>();
            _gameBridgeMock = new Mock<IGameBridge>();
            _zoneBattleManagerMock = new Mock<IZoneBattleManager>();
            _strategies = new Dictionary<string, IAIStrategy>();
        }

        [Fact]
        public void Constructor_ShouldInitializeWithIsRunningFalse()
        {
            var controller = CreateController();
            Assert.False(controller.IsRunning);
        }

        [Fact]
        public void Start_ShouldSetIsRunningTrue()
        {
            var controller = CreateController();
            controller.Start();
            Assert.True(controller.IsRunning);
        }

        [Fact]
        public void Stop_ShouldSetIsRunningFalse()
        {
            var controller = CreateController();
            controller.Start();
            controller.Stop();
            Assert.False(controller.IsRunning);
        }

        [Fact]
        public void SetPlayerFactionId_ShouldStoreValue()
        {
            var controller = CreateController();
            controller.SetPlayerFactionId("michael");
            Assert.Equal("michael", controller.PlayerFactionId);
        }

        [Fact]
        public void SetPlayerZone_ShouldStoreValue()
        {
            var controller = CreateController();
            controller.SetPlayerZone("vinewood");
            Assert.Equal("vinewood", controller.PlayerZoneId);
        }

        [Fact]
        public void Update_After60Seconds_ShouldTriggerRecruitment()
        {
            // Arrange
            var faction = new Faction("trevor", "Trevor", color: new FactionColor(255, 150, 0));
            var factionState = new FactionState("trevor", 1000, 5);

            _factionServiceMock.Setup(f => f.GetActiveFactions())
                .Returns(new[] { faction });
            _factionServiceMock.Setup(f => f.GetFactionState("trevor"))
                .Returns(factionState);

            var controller = CreateController();
            controller.Start();

            // Act - simulate 60 seconds
            controller.Update(60f);

            // Assert - should have recruited (1000 cash / 200 per troop = 5, capped at 5)
            _factionServiceMock.Verify(f => f.RecruitTroops("trevor", 5), Times.Once);
            _factionServiceMock.Verify(f => f.SpendCash("trevor", 1000), Times.Once);
        }

        [Fact]
        public void Update_WhenNotRunning_ShouldNotProcess()
        {
            var controller = CreateController();
            // Don't call Start()

            controller.Update(100f);

            _factionServiceMock.Verify(f => f.GetActiveFactions(), Times.Never);
        }

        [Fact]
        public void Update_ShouldSkipPlayerFaction()
        {
            var playerFaction = new Faction("michael", "Michael", color: new FactionColor(0, 100, 255));
            var aiFaction = new Faction("trevor", "Trevor", color: new FactionColor(255, 150, 0));

            _factionServiceMock.Setup(f => f.GetActiveFactions())
                .Returns(new[] { playerFaction, aiFaction });
            _factionServiceMock.Setup(f => f.GetFactionState("trevor"))
                .Returns(new FactionState("trevor", 1000, 5));

            var controller = CreateController();
            controller.SetPlayerFactionId("michael");
            controller.Start();

            controller.Update(60f);

            // Should recruit for trevor but not michael
            _factionServiceMock.Verify(f => f.RecruitTroops("trevor", It.IsAny<int>()), Times.Once);
            _factionServiceMock.Verify(f => f.RecruitTroops("michael", It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public void ExecuteAttack_WhenBattleAlreadyExistsInZone_ShouldSkipWithoutException()
        {
            // Arrange
            var attackerFaction = new Faction("trevor", "Trevor", color: new FactionColor(255, 150, 0));
            var defenderFaction = new Faction("michael", "Michael", color: new FactionColor(0, 100, 255));
            var targetZone = new Zone("vinewood", "Vinewood", new FactionWars.Core.Interfaces.Vector3(100f, 100f, 30f), 100f, 5);
            targetZone.OwnerFactionId = "michael";

            var attackerOwnedZone = new Zone("davis", "Davis", new FactionWars.Core.Interfaces.Vector3(200f, 200f, 30f), 100f, 3);
            attackerOwnedZone.OwnerFactionId = "trevor";
            attackerOwnedZone.AdjacentZoneIds.Add("vinewood");

            // Setup faction service
            _factionServiceMock.Setup(f => f.GetActiveFactions())
                .Returns(new[] { attackerFaction, defenderFaction });
            _factionServiceMock.Setup(f => f.GetFaction("trevor"))
                .Returns(attackerFaction);
            _factionServiceMock.Setup(f => f.GetFactionState("trevor"))
                .Returns(new FactionState("trevor", 10000, 20)); // Enough cash and troops
            _factionServiceMock.Setup(f => f.GetAllFactions())
                .Returns(new[] { attackerFaction, defenderFaction });

            // Setup zone service
            _zoneServiceMock.Setup(z => z.GetAllZones())
                .Returns(new[] { targetZone, attackerOwnedZone });
            _zoneServiceMock.Setup(z => z.GetZonesByOwner("trevor"))
                .Returns(new[] { attackerOwnedZone });
            _zoneServiceMock.Setup(z => z.GetZone("vinewood"))
                .Returns(targetZone);

            // Setup strategy that will attack vinewood
            var strategyMock = new Mock<IAIStrategy>();
            strategyMock.Setup(s => s.MakeDecisions(It.IsAny<FactionWars.AI.Models.AIContext>()))
                .Returns(new List<FactionWars.AI.Models.AIDecision>
                {
                    new FactionWars.AI.Models.AIDecision(
                        FactionWars.AI.Models.AIDecisionType.Attack,
                        "vinewood",
                        0.8f,
                        10)
                });
            _strategies["trevor"] = strategyMock.Object;

            // KEY: A battle already exists in the target zone
            var existingBattle = new FactionWars.Combat.Models.ZoneBattle(
                attackerFactionId: "franklin",
                defenderFactionId: "michael",
                zoneId: "vinewood",
                attackerTroops: new Dictionary<FactionWars.Core.Models.DefenderTier, int>
                {
                    { FactionWars.Core.Models.DefenderTier.Basic, 5 }
                },
                defenderTroops: new Dictionary<FactionWars.Core.Models.DefenderTier, int>
                {
                    { FactionWars.Core.Models.DefenderTier.Basic, 3 }
                },
                playerFactionId: null);

            _zoneBattleManagerMock.Setup(m => m.GetBattleForZone("vinewood"))
                .Returns(existingBattle);

            // Simulate real ZoneBattleManager behavior: throws if battle already exists
            _zoneBattleManagerMock.Setup(m => m.StartBattle(
                    "vinewood",
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<Dictionary<FactionWars.Core.Models.DefenderTier, int>>(),
                    It.IsAny<Dictionary<FactionWars.Core.Models.DefenderTier, int>>()))
                .Throws(new System.InvalidOperationException("A battle already exists in zone 'vinewood'."));

            var controller = CreateController();
            controller.SetPlayerFactionId("michael"); // Player is michael, so trevor is AI
            controller.Start();

            // Act & Assert - should NOT throw, should skip the attack gracefully
            var exception = Record.Exception(() => controller.Update(60f));
            Assert.Null(exception);

            // StartBattle should NOT be called since a battle already exists
            _zoneBattleManagerMock.Verify(
                m => m.StartBattle(
                    "vinewood",
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<Dictionary<FactionWars.Core.Models.DefenderTier, int>>(),
                    It.IsAny<Dictionary<FactionWars.Core.Models.DefenderTier, int>>()),
                Times.Never);
        }

        private AIController CreateController()
        {
            return new AIController(
                _factionServiceMock.Object,
                _zoneServiceMock.Object,
                _battleSimulationServiceMock.Object,
                _allocationServiceMock.Object,
                _gameBridgeMock.Object,
                _strategies,
                _zoneBattleManagerMock.Object);
        }
    }
}
