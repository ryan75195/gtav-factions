using FactionWars.AI.Controllers;
using FactionWars.AI.Interfaces;
using FactionWars.Core.Interfaces;
using FactionWars.Factions.Interfaces;
using FactionWars.Factions.Models;
using FactionWars.Territory.Interfaces;
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
        private readonly Dictionary<string, IAIStrategy> _strategies;

        public AIControllerTests()
        {
            _factionServiceMock = new Mock<IFactionService>();
            _zoneServiceMock = new Mock<IZoneService>();
            _battleSimulationServiceMock = new Mock<IBattleSimulationService>();
            _allocationServiceMock = new Mock<IZoneDefenderAllocationService>();
            _gameBridgeMock = new Mock<IGameBridge>();
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

            // Assert - should have recruited (1000 cash / 100 per troop = 10, capped at 5)
            _factionServiceMock.Verify(f => f.RecruitTroops("trevor", 5), Times.Once);
            _factionServiceMock.Verify(f => f.SpendCash("trevor", 500), Times.Once);
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

        private AIController CreateController()
        {
            return new AIController(
                _factionServiceMock.Object,
                _zoneServiceMock.Object,
                _battleSimulationServiceMock.Object,
                _allocationServiceMock.Object,
                _gameBridgeMock.Object,
                _strategies);
        }
    }
}
