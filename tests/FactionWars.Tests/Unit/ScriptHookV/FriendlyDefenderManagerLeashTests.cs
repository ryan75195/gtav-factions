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
    public class FriendlyDefenderManagerLeashTests
    {
        private const string PlayerFactionId = "michael";
        private const string TestZoneId = "zone_1";
        private static readonly Vector3 ZoneCenter = new Vector3(100f, 100f, 0f);
        private const float ZoneRadius = 150f;

        [Fact]
        public void Update_DefenderInsideZone_DoesNotRetask()
        {
            var (manager, bridge, defenderHandle) = SpawnSingleDefender();

            bridge.SetPedPosition(defenderHandle, new Vector3(ZoneCenter.X + 10f, ZoneCenter.Y + 10f, 0f));

            bridge.AdvanceGameTime(ZoneLeashEnforcer.LeashCheckIntervalMs + 100);
            manager.Update();

            Assert.False(bridge.IsPedGoingToCoord(defenderHandle));
        }

        [Fact]
        public void Update_DefenderPastHysteresisThreshold_RetasksTowardZone()
        {
            var (manager, bridge, defenderHandle) = SpawnSingleDefender();

            bridge.SetPedPosition(defenderHandle, new Vector3(ZoneCenter.X + ZoneRadius * 1.5f, ZoneCenter.Y, 0f));

            bridge.AdvanceGameTime(ZoneLeashEnforcer.LeashCheckIntervalMs + 100);
            manager.Update();

            Assert.True(bridge.IsPedGoingToCoord(defenderHandle));

            var dest = bridge.GetPedGoToCoordDestination(defenderHandle);
            Assert.NotNull(dest);
            float dx = dest!.Value.X - ZoneCenter.X;
            float dy = dest.Value.Y - ZoneCenter.Y;
            float dist = (float)System.Math.Sqrt(dx * dx + dy * dy);
            Assert.True(dist <= ZoneRadius * ZoneLeashEnforcer.LeashReturnRadiusMultiplier + 0.01f);
        }

        [Fact]
        public void OnZoneEntered_TasksDefendersWithBoundedWander()
        {
            var (_, bridge, defenderHandle) = SpawnSingleDefender();

            Assert.True(bridge.IsPedBoundedWandering(defenderHandle),
                "Friendly defenders must use TaskPedWanderInBoundedArea so GTA's native enforces the zone radius for idle wander.");
            Assert.False(bridge.IsPedWandering(defenderHandle),
                "Friendly defenders must NOT use the unbounded TaskPedWanderInArea anymore.");
        }

        [Fact]
        public void Update_StrayDefenderInCombat_NotLeashed()
        {
            var (manager, bridge, defenderHandle) = SpawnSingleDefender();

            bridge.SetPedPosition(defenderHandle, new Vector3(ZoneCenter.X + ZoneRadius * 1.5f, ZoneCenter.Y, 0f));
            bridge.SetPedInCombat(defenderHandle, true);

            bridge.AdvanceGameTime(ZoneLeashEnforcer.LeashCheckIntervalMs + 100);
            manager.Update();

            Assert.False(bridge.IsPedGoingToCoord(defenderHandle));
        }

        [Fact]
        public void Update_StrayDefender_NoRetaskBeforeIntervalElapsed()
        {
            var (manager, bridge, defenderHandle) = SpawnSingleDefender();

            bridge.SetPedPosition(defenderHandle, new Vector3(ZoneCenter.X + ZoneRadius * 1.5f, ZoneCenter.Y, 0f));

            bridge.AdvanceGameTime(ZoneLeashEnforcer.LeashCheckIntervalMs - 100);
            manager.Update();

            Assert.False(bridge.IsPedGoingToCoord(defenderHandle));

            bridge.AdvanceGameTime(200);
            manager.Update();

            Assert.True(bridge.IsPedGoingToCoord(defenderHandle));
        }

        private static SpawnedFriendlyDefender SpawnSingleDefender()
        {
            var bridge = new MockGameBridge();
            var allocationServiceMock = new Mock<IZoneDefenderAllocationService>();
            var pedSpawningServiceMock = new Mock<IPedSpawningService>();
            var pedDespawnServiceMock = new Mock<IPedDespawnService>();
            var defenderTierServiceMock = new Mock<IDefenderTierService>();
            var pedBlipServiceMock = new Mock<IPedBlipService>();
            var zoneServiceMock = new Mock<IZoneService>();

            pedSpawningServiceMock.Setup(p => p.CanSpawn()).Returns(true);
            pedSpawningServiceMock
                .Setup(p => p.SpawnPed(It.IsAny<string>(), It.IsAny<Vector3>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(() => new PedHandle(bridge.CreatePed("test", new Vector3(0f, 0f, 0f))));

            defenderTierServiceMock
                .Setup(d => d.GetTierConfig(It.IsAny<DefenderTier>()))
                .Returns(new DefenderTierConfig(DefenderTier.Basic, 200, 100, 0, "weapon_pistol", 0.5f, 1.0f));

            pedBlipServiceMock
                .Setup(p => p.CreateBlipForPed(It.IsAny<int>(), It.IsAny<BlipColor>()))
                .Returns(1);

            var zone = new Zone(TestZoneId, "Test Zone", ZoneCenter, ZoneRadius, 1)
            {
                OwnerFactionId = PlayerFactionId
            };
            zoneServiceMock.Setup(z => z.GetZone(TestZoneId)).Returns(zone);

            var allocation = new ZoneDefenderAllocation(PlayerFactionId, TestZoneId);
            allocation.AddTroops(DefenderTier.Basic, 1);
            allocationServiceMock.Setup(a => a.GetAllocation(PlayerFactionId, TestZoneId)).Returns(allocation);

            var manager = new FriendlyDefenderManager(
                bridge,
                allocationServiceMock.Object,
                pedSpawningServiceMock.Object,
                pedDespawnServiceMock.Object,
                defenderTierServiceMock.Object,
                pedBlipServiceMock.Object,
                zoneServiceMock.Object,
                PlayerFactionId);

            manager.OnZoneEntered(zone);
            int handle = bridge.GetSpawnedPeds()[0];
            return new SpawnedFriendlyDefender(manager, bridge, handle);
        }

        private sealed class SpawnedFriendlyDefender
        {
            public SpawnedFriendlyDefender(FriendlyDefenderManager manager, MockGameBridge bridge, int defenderHandle)
            {
                Manager = manager;
                Bridge = bridge;
                DefenderHandle = defenderHandle;
            }

            public FriendlyDefenderManager Manager { get; }
            public MockGameBridge Bridge { get; }
            public int DefenderHandle { get; }

            public void Deconstruct(out FriendlyDefenderManager manager, out MockGameBridge bridge, out int defenderHandle)
            {
                manager = Manager;
                bridge = Bridge;
                defenderHandle = DefenderHandle;
            }
        }
    }
}
