using FactionWars.AI.Controllers;
using FactionWars.AI.Interfaces;
using FactionWars.Core.Interfaces;
using FactionWars.Factions.Interfaces;
using FactionWars.Territory.Interfaces;
using FactionWars.UI.Interfaces;
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
        private readonly Mock<IEventFeedService> _eventFeedServiceMock;
        private readonly Dictionary<string, IAIStrategy> _strategies;

        public AIControllerTests()
        {
            _factionServiceMock = new Mock<IFactionService>();
            _zoneServiceMock = new Mock<IZoneService>();
            _battleSimulationServiceMock = new Mock<IBattleSimulationService>();
            _allocationServiceMock = new Mock<IZoneDefenderAllocationService>();
            _eventFeedServiceMock = new Mock<IEventFeedService>();
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

        private AIController CreateController()
        {
            return new AIController(
                _factionServiceMock.Object,
                _zoneServiceMock.Object,
                _battleSimulationServiceMock.Object,
                _allocationServiceMock.Object,
                _eventFeedServiceMock.Object,
                _strategies);
        }
    }
}
