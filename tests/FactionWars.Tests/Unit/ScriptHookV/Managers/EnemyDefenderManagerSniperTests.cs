using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Models;
using FactionWars.Combat.Services;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.Core.Utils;
using FactionWars.ScriptHookV.Combat;
using FactionWars.ScriptHookV.Managers;
using FactionWars.ScriptHookV.Models;
using FactionWars.Territory.Interfaces;
using FactionWars.Territory.Models;
using FactionWars.UI.Interfaces;
using Moq;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV.Managers
{
    public class EnemyDefenderManagerSniperTests
    {
        private const string EnemyFactionId = "ballas";
        private const string TestZoneId = "zone_sniper";

        private EnemyDefenderManager BuildManager(MockGameBridge bridge)
        {
            var allocationServiceMock = new Mock<IZoneDefenderAllocationService>();
            var pedSpawningServiceMock = new Mock<IPedSpawningService>();
            var pedDespawnServiceMock = new Mock<IPedDespawnService>();
            var defenderRoleServiceMock = new Mock<IDefenderRoleService>();
            var pedBlipServiceMock = new Mock<IPedBlipService>();
            var zoneServiceMock = new Mock<IZoneService>();

            pedSpawningServiceMock.Setup(p => p.CanSpawn()).Returns(true);
            pedSpawningServiceMock
                .Setup(p => p.SpawnPed(
                    It.IsAny<string>(), It.IsAny<Vector3>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns<string, Vector3, string, string?>((model, pos, faction, zone) =>
                {
                    var h = bridge.CreatePed(model, pos);
                    bridge.SetPedRelationshipGroup(h, faction.ToUpperInvariant());
                    return new PedHandle(h, faction, pos, model, zone);
                });

            defenderRoleServiceMock
                .Setup(d => d.GetRoleConfig(It.IsAny<DefenderRole>()))
                .Returns<DefenderRole>(role => role == DefenderRole.Sniper
                    ? new DefenderRoleConfig(DefenderRole.Sniper, 1500, 275, 50, "WEAPON_SNIPERRIFLE", 0.8f, 2.2f, false)
                    : new DefenderRoleConfig(DefenderRole.Grunt, 200, 100, 0, "weapon_pistol", 0.5f, 1.0f));

            pedBlipServiceMock
                .Setup(p => p.CreateBlipForPed(It.IsAny<int>(), It.IsAny<BlipColor>()))
                .Returns(1);

            var allocation = new ZoneDefenderAllocation(EnemyFactionId, TestZoneId);
            allocation.AddTroops(DefenderRole.Sniper, 1);
            allocationServiceMock
                .Setup(a => a.GetAllocation(EnemyFactionId, TestZoneId))
                .Returns(allocation);

            return new EnemyDefenderManager(new EnemyDefenderManagerDependencies
            {
                GameBridge = bridge,
                AllocationService = allocationServiceMock.Object,
                PedSpawningService = pedSpawningServiceMock.Object,
                PedDespawnService = pedDespawnServiceMock.Object,
                DefenderRoleService = defenderRoleServiceMock.Object,
                PedBlipService = pedBlipServiceMock.Object,
                ZoneService = zoneServiceMock.Object,
                SniperDeployment = new SniperDeploymentService(new PerchResolver(), bridge)
            });
        }

        [Fact]
        public void Spawn_SniperAllocation_PerchesAndGuards()
        {
            // Arrange
            var bridge = new MockGameBridge();
            var manager = BuildManager(bridge);
            var zone = new Zone(TestZoneId, "Sniper Zone", new Vector3(100f, 100f, 0f), 150f, 1);
            zone.OwnerFactionId = EnemyFactionId;

            // Act
            manager.OnEnemyZoneEntered(zone, EnemyFactionId);

            // Assert
            Assert.Equal(1, manager.GetSpawnedDefenderCount(TestZoneId));
            Assert.True(bridge.IsPedGuardingArea(1));
        }
    }
}
