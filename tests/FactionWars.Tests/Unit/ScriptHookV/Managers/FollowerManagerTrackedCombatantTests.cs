using System.Collections.Generic;
using System.Linq;
using FactionWars.Combat.Interfaces;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.ScriptHookV.Managers;
using FactionWars.Telemetry.Interfaces;
using FactionWars.Telemetry.Models;
using FactionWars.UI.Interfaces;
using Moq;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV.Managers
{
    public class FollowerManagerTrackedCombatantTests
    {
        private readonly Mock<IGameBridge> _gameBridge = new Mock<IGameBridge>();
        private readonly Mock<IFollowerService> _followerService = new Mock<IFollowerService>();
        private readonly FollowerManager _manager;

        public FollowerManagerTrackedCombatantTests()
        {
            _gameBridge.Setup(g => g.IsPlayerInVehicle()).Returns(false);
            _manager = new FollowerManager(
                _gameBridge.Object,
                _followerService.Object,
                new Mock<IPedSpawningService>().Object,
                new Mock<IDefenderRoleService>().Object,
                new Mock<IPedBlipService>().Object,
                new Mock<IVehicleSeatPriorityService>().Object);
        }

        private ITrackedCombatantSource Source => _manager;

        [Fact]
        public void GetTrackedCombatants_BeforeUpdate_ReturnsEmpty()
        {
            Assert.Empty(Source.GetTrackedCombatants());
        }

        [Fact]
        public void GetTrackedCombatants_AfterUpdate_ReturnsAliveFollowersWithRole()
        {
            var followers = new List<Follower>
            {
                new Follower("player", DefenderRole.Sniper, 10),
                new Follower("player", DefenderRole.Grunt, 11)
            };
            _followerService.Setup(s => s.GetFollowers("player")).Returns(followers);
            _gameBridge.Setup(g => g.IsPedAlive(10)).Returns(true);
            _gameBridge.Setup(g => g.IsPedAlive(11)).Returns(true);

            _manager.Update("player", boardPlayerVehicle: false);

            var tracked = Source.GetTrackedCombatants();
            Assert.Equal(2, tracked.Count);
            Assert.All(tracked, c => Assert.Equal(CombatantKind.Follower, c.Kind));
            Assert.Contains(tracked, c => c.Handle == 10 && c.Role == DefenderRole.Sniper);
            Assert.Contains(tracked, c => c.Handle == 11 && c.Role == DefenderRole.Grunt);
        }

        [Fact]
        public void GetTrackedCombatants_ExcludesDeadFollowers()
        {
            var followers = new List<Follower>
            {
                new Follower("player", DefenderRole.Grunt, 20),
                new Follower("player", DefenderRole.Grunt, 21)
            };
            _followerService.Setup(s => s.GetFollowers("player")).Returns(followers);
            _gameBridge.Setup(g => g.IsPedAlive(20)).Returns(true);
            _gameBridge.Setup(g => g.IsPedAlive(21)).Returns(false);

            _manager.Update("player", boardPlayerVehicle: false);

            var tracked = Source.GetTrackedCombatants();
            Assert.Single(tracked);
            Assert.Equal(20, tracked[0].Handle);
        }

        [Fact]
        public void GetTrackedCombatants_EmptyFactionId_ReturnsEmpty()
        {
            _manager.Update("", boardPlayerVehicle: false);
            Assert.Empty(Source.GetTrackedCombatants());
        }
    }
}
