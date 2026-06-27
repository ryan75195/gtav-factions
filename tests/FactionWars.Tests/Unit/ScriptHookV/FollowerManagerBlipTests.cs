using System;
using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Models;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.Core.Utils;
using FactionWars.ScriptHookV.Managers;
using FactionWars.UI.Interfaces;
using Moq;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV
{
    public class FollowerManagerBlipTests
    {
        private readonly MockGameBridge _gameBridge;
        private readonly Mock<IFollowerService> _followerServiceMock;
        private readonly Mock<IPedSpawningService> _pedSpawningServiceMock;
        private readonly Mock<IDefenderRoleService> _defenderRoleServiceMock;
        private readonly Mock<IPedBlipService> _pedBlipServiceMock;
        private readonly Mock<IVehicleSeatPriorityService> _seatPriorityServiceMock;
        private readonly FollowerManager _manager;

        public FollowerManagerBlipTests()
        {
            _gameBridge = new MockGameBridge { PlayerPosition = new Vector3(100, 100, 0) };
            _gameBridge.PlayerMoney = 10000;

            _followerServiceMock = new Mock<IFollowerService>();
            _pedSpawningServiceMock = new Mock<IPedSpawningService>();
            _defenderRoleServiceMock = new Mock<IDefenderRoleService>();
            _pedBlipServiceMock = new Mock<IPedBlipService>();
            _seatPriorityServiceMock = new Mock<IVehicleSeatPriorityService>();

            var roleConfig = new DefenderRoleConfig(DefenderRole.Grunt, 100, 100, 0, "weapon_pistol", 0.5f, 1.0f);
            _defenderRoleServiceMock.Setup(d => d.GetRoleConfig(It.IsAny<DefenderRole>())).Returns(roleConfig);

            _pedSpawningServiceMock.Setup(p => p.CanSpawn()).Returns(true);
            _pedSpawningServiceMock.Setup(p => p.SpawnPed(It.IsAny<string>(), It.IsAny<Vector3>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(new PedHandle(123));

            _followerServiceMock.Setup(f => f.Recruit(It.IsAny<string>(), It.IsAny<DefenderRole>()))
                .Returns(FollowerRecruitResult.Succeeded(new Follower("faction-1", DefenderRole.Grunt)));

            // Default seat priority service setup
            _seatPriorityServiceMock.Setup(s => s.GetPrioritizedFreeSeats(It.IsAny<int>()))
                .Returns(new[] { 1, 2, 3 });
            _seatPriorityServiceMock.Setup(s => s.FilterFollowersByProximity(It.IsAny<int[]>(), It.IsAny<int>(), It.IsAny<float>()))
                .Returns<int[], int, float>((handles, v, d) => handles);

            _manager = new FollowerManager(
                _gameBridge,
                _followerServiceMock.Object,
                _pedSpawningServiceMock.Object,
                _defenderRoleServiceMock.Object,
                _pedBlipServiceMock.Object,
                _seatPriorityServiceMock.Object);
        }

        [Fact]
        public void RecruitFollower_CreatesYellowBlip()
        {
            _manager.RecruitFollower("faction-1", DefenderRole.Grunt);

            _pedBlipServiceMock.Verify(b => b.CreateBlipForPed(123, BlipColor.Yellow), Times.Once);
        }

        [Fact]
        public void DismissFollower_RemovesBlip()
        {
            var follower = new Follower("faction-1", DefenderRole.Grunt, 123);
            var followerId = follower.Id;

            _followerServiceMock.Setup(f => f.GetFollowerById(followerId)).Returns(follower);
            _followerServiceMock.Setup(f => f.DismissFollower(followerId)).Returns(true);

            _manager.DismissFollower(followerId);

            _pedBlipServiceMock.Verify(b => b.RemoveBlipForPed(123), Times.Once);
        }

        [Fact]
        public void DismissAllFollowers_RemovesAllBlips()
        {
            var follower1 = new Follower("faction-1", DefenderRole.Grunt, 100);
            var follower2 = new Follower("faction-1", DefenderRole.Gunner, 101);

            _followerServiceMock.Setup(f => f.GetFollowers("faction-1"))
                .Returns(new[] { follower1, follower2 });

            _manager.DismissAllFollowers("faction-1");

            _pedBlipServiceMock.Verify(b => b.RemoveBlipForPed(100), Times.Once);
            _pedBlipServiceMock.Verify(b => b.RemoveBlipForPed(101), Times.Once);
        }

        [Fact]
        public void Update_WhenFollowerDies_RemovesBlip()
        {
            // First create a ped in the game bridge so it can be killed
            var pedHandle = _gameBridge.CreatePed("test_model", new Vector3(100, 100, 0));
            var follower = new Follower("faction-1", DefenderRole.Grunt, pedHandle);

            _followerServiceMock.Setup(f => f.GetFollowers("faction-1"))
                .Returns(new[] { follower });
            _gameBridge.KillPed(pedHandle);

            _manager.Update("faction-1");

            _pedBlipServiceMock.Verify(b => b.RemoveBlipForPed(pedHandle), Times.Once);
        }

        [Fact]
        public void Constructor_WithNullPedBlipService_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new FollowerManager(
                _gameBridge,
                _followerServiceMock.Object,
                _pedSpawningServiceMock.Object,
                _defenderRoleServiceMock.Object,
                null!,
                _seatPriorityServiceMock.Object));
        }
    }
}
