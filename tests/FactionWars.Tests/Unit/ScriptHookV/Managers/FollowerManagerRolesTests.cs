using System.Collections.Generic;
using FactionWars.Combat.Interfaces;
using FactionWars.Configuration;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.ScriptHookV.Managers;
using FactionWars.UI.Interfaces;
using Moq;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV.Managers
{
    public class FollowerManagerRolesTests
    {
        [Fact]
        public void OnFootBodyguardRoles_AfterUpdate_MapsAliveFollowersToRole()
        {
            var gameBridge = new Mock<IGameBridge>();
            gameBridge.Setup(g => g.IsPlayerInVehicle()).Returns(false);
            gameBridge.Setup(g => g.IsPedAlive(10)).Returns(true);
            gameBridge.Setup(g => g.IsPedAlive(11)).Returns(true);
            var followerService = new Mock<IFollowerService>();
            followerService.Setup(s => s.GetFollowers("player")).Returns(new List<Follower>
            {
                new Follower("player", DefenderRole.Sniper, 10),
                new Follower("player", DefenderRole.Grunt, 11)
            });

            var manager = new FollowerManager(
                gameBridge.Object, followerService.Object,
                new Mock<IPedSpawningService>().Object, new Mock<IDefenderRoleService>().Object,
                new Mock<IPedBlipService>().Object, new Mock<IVehicleSeatPriorityService>().Object,
                CombatantStatsProviderFactory.Create(new CombatantsConfig()));

            manager.Update("player", boardPlayerVehicle: false);

            Assert.Equal(DefenderRole.Sniper, manager.OnFootBodyguardRoles[10]);
            Assert.Equal(DefenderRole.Grunt, manager.OnFootBodyguardRoles[11]);
        }

        [Fact]
        public void OnFootBodyguardRoles_EmptyFaction_IsEmpty()
        {
            var gameBridge = new Mock<IGameBridge>();
            var manager = new FollowerManager(
                gameBridge.Object, new Mock<IFollowerService>().Object,
                new Mock<IPedSpawningService>().Object, new Mock<IDefenderRoleService>().Object,
                new Mock<IPedBlipService>().Object, new Mock<IVehicleSeatPriorityService>().Object,
                CombatantStatsProviderFactory.Create(new CombatantsConfig()));

            manager.Update("", boardPlayerVehicle: false);

            Assert.Empty(manager.OnFootBodyguardRoles);
        }
    }
}
