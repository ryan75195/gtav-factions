using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Models;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.Core.Utils;
using FactionWars.ScriptHookV.Managers;
using FactionWars.ScriptHookV.Services;
using FactionWars.Territory.Interfaces;
using FactionWars.Territory.Models;
using FactionWars.UI.Interfaces;
using Moq;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV
{
    public class EnemyDefenderManagerLeashTests
    {
        private const string EnemyFactionId = "ballas";
        private const string TestZoneId = "zone_1";
        private static readonly Vector3 ZoneCenter = new Vector3(100f, 100f, 0f);
        private const float ZoneRadius = 150f;

        [Fact]
        public void Update_DefenderInsideZone_DoesNotRetask()
        {
            var (manager, bridge, defenderHandle) = SpawnSingleEnemyDefender();

            bridge.SetPedPosition(defenderHandle, new Vector3(ZoneCenter.X + 10f, ZoneCenter.Y + 10f, 0f));

            bridge.AdvanceGameTime(ZoneLeashEnforcer.LeashCheckIntervalMs + 100);
            manager.Update(EnemyFactionId);

            Assert.False(bridge.IsPedGoingToCoord(defenderHandle));
        }

        [Fact]
        public void Update_DefenderPastHysteresisThreshold_RetasksTowardZone()
        {
            var (manager, bridge, defenderHandle) = SpawnSingleEnemyDefender();

            bridge.SetPedPosition(defenderHandle, new Vector3(ZoneCenter.X + ZoneRadius * 1.5f, ZoneCenter.Y, 0f));
            // Strayed but not actively fighting — the leash only retasks non-combat peds, and the
            // spawn combat task now (correctly) leaves the defender in combat until it disengages.
            bridge.SetPedInCombat(defenderHandle, false);

            bridge.AdvanceGameTime(ZoneLeashEnforcer.LeashCheckIntervalMs + 100);
            manager.Update(EnemyFactionId);

            Assert.True(bridge.IsPedGoingToCoord(defenderHandle));

            var dest = bridge.GetPedGoToCoordDestination(defenderHandle);
            Assert.NotNull(dest);
            float dx = dest!.Value.X - ZoneCenter.X;
            float dy = dest.Value.Y - ZoneCenter.Y;
            float dist = (float)System.Math.Sqrt(dx * dx + dy * dy);
            Assert.True(dist <= ZoneRadius * ZoneLeashEnforcer.LeashReturnRadiusMultiplier + 0.01f);
        }

        [Fact]
        public void Update_StrayDefenderInCombat_NotLeashed()
        {
            var (manager, bridge, defenderHandle) = SpawnSingleEnemyDefender();

            // Strayed well outside the zone, but actively fighting.
            bridge.SetPedPosition(defenderHandle, new Vector3(ZoneCenter.X + ZoneRadius * 1.5f, ZoneCenter.Y, 0f));
            bridge.SetPedInCombat(defenderHandle, true);

            bridge.AdvanceGameTime(ZoneLeashEnforcer.LeashCheckIntervalMs + 100);
            manager.Update(EnemyFactionId);

            // Leashing an in-combat ped clears its combat task; skip it instead.
            Assert.False(bridge.IsPedGoingToCoord(defenderHandle));
        }

        [Fact]
        public void Update_StrayDefender_NoRetaskBeforeIntervalElapsed()
        {
            var (manager, bridge, defenderHandle) = SpawnSingleEnemyDefender();

            bridge.SetPedPosition(defenderHandle, new Vector3(ZoneCenter.X + ZoneRadius * 1.5f, ZoneCenter.Y, 0f));
            // Strayed but not actively fighting — the leash only retasks non-combat peds.
            bridge.SetPedInCombat(defenderHandle, false);

            bridge.AdvanceGameTime(ZoneLeashEnforcer.LeashCheckIntervalMs - 100);
            manager.Update(EnemyFactionId);
            Assert.False(bridge.IsPedGoingToCoord(defenderHandle));

            bridge.AdvanceGameTime(200);
            manager.Update(EnemyFactionId);
            Assert.True(bridge.IsPedGoingToCoord(defenderHandle));
        }

        private static SpawnedEnemyDefender SpawnSingleEnemyDefender()
        {
            var bridge = new MockGameBridge();
            var allocationServiceMock = new Mock<IZoneDefenderAllocationService>();
            var pedSpawningServiceMock = new Mock<IPedSpawningService>();
            var pedDespawnServiceMock = new Mock<IPedDespawnService>();
            var defenderRoleServiceMock = new Mock<IDefenderRoleService>();
            var pedBlipServiceMock = new Mock<IPedBlipService>();
            var zoneServiceMock = new Mock<IZoneService>();

            pedSpawningServiceMock.Setup(p => p.CanSpawn()).Returns(true);
            pedSpawningServiceMock
                .Setup(p => p.SpawnPed(It.IsAny<string>(), It.IsAny<Vector3>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(() => new PedHandle(bridge.CreatePed("test", new Vector3(0f, 0f, 0f))));

            defenderRoleServiceMock
                .Setup(d => d.GetRoleConfig(It.IsAny<DefenderRole>()))
                .Returns(new DefenderRoleConfig(DefenderRole.Grunt, 200, 100, 0, "weapon_pistol", 0.5f, 1.0f));

            pedBlipServiceMock
                .Setup(p => p.CreateBlipForPed(It.IsAny<int>(), It.IsAny<BlipColor>()))
                .Returns(1);

            var zone = new Zone(TestZoneId, "Test Zone", ZoneCenter, ZoneRadius, 1)
            {
                OwnerFactionId = EnemyFactionId
            };
            zoneServiceMock.Setup(z => z.GetZone(TestZoneId)).Returns(zone);

            var allocation = new ZoneDefenderAllocation(EnemyFactionId, TestZoneId);
            allocation.AddTroops(DefenderRole.Grunt, 1);
            allocationServiceMock.Setup(a => a.GetAllocation(EnemyFactionId, TestZoneId)).Returns(allocation);

            var manager = new EnemyDefenderManager(
                bridge,
                allocationServiceMock.Object,
                pedSpawningServiceMock.Object,
                pedDespawnServiceMock.Object,
                defenderRoleServiceMock.Object,
                pedBlipServiceMock.Object,
                zoneServiceMock.Object);

            manager.OnEnemyZoneEntered(zone, EnemyFactionId);
            int handle = bridge.GetSpawnedPeds()[0];
            return new SpawnedEnemyDefender(manager, bridge, handle);
        }

        private sealed class SpawnedEnemyDefender
        {
            public SpawnedEnemyDefender(EnemyDefenderManager manager, MockGameBridge bridge, int defenderHandle)
            {
                Manager = manager;
                Bridge = bridge;
                DefenderHandle = defenderHandle;
            }

            public EnemyDefenderManager Manager { get; }
            public MockGameBridge Bridge { get; }
            public int DefenderHandle { get; }

            public void Deconstruct(out EnemyDefenderManager manager, out MockGameBridge bridge, out int defenderHandle)
            {
                manager = Manager;
                bridge = Bridge;
                defenderHandle = DefenderHandle;
            }
        }
    }
}
