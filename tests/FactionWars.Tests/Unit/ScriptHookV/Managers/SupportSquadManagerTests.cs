using System;
using System.Collections.Generic;
using System.Linq;
using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Models;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.Core.Utils;
using FactionWars.ScriptHookV.Combat.Interfaces;
using FactionWars.ScriptHookV.Managers;
using FactionWars.ScriptHookV.Models;
using FactionWars.Territory.Interfaces;
using FactionWars.Territory.Models;
using FactionWars.UI.Interfaces;
using Moq;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV.Managers
{
    public class SupportSquadManagerTests
    {
        private const string PlayerFactionId = "player";

        private readonly MockGameBridge _bridge = new MockGameBridge();
        private readonly Mock<IZoneCombatantSpawner> _spawnerMock = new Mock<IZoneCombatantSpawner>();
        private readonly Mock<ICombatantStatsProvider> _statsProviderMock = new Mock<ICombatantStatsProvider>();
        private readonly Mock<IZoneService> _zoneServiceMock = new Mock<IZoneService>();
        private readonly Mock<IPedDespawnService> _pedDespawnMock = new Mock<IPedDespawnService>();
        private readonly Mock<IPedBlipService> _pedBlipMock = new Mock<IPedBlipService>();

        public SupportSquadManagerTests()
        {
            // Backs each Spawn call with a real MockGameBridge ped so IsPedAlive/DoesPedExist
            // (used by pruning) reflect a genuinely spawned combatant, mirroring how the real
            // ZoneCombatantSpawner ultimately calls IGameBridge.CreatePed under the hood.
            _spawnerMock
                .Setup(s => s.Spawn(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Vector3>(), It.IsAny<string>()))
                .Returns((string factionId, string playerFactionId, string model, Vector3 pos, string zoneId)
                    => new PedHandle(_bridge.CreatePed(model, pos)));

            _statsProviderMock
                .Setup(s => s.GetRoleStats(It.IsAny<CombatantCategory>(), It.IsAny<DefenderRole>()))
                .Returns(new RoleStats(200, 50, 0.6f, "WEAPON_CARBINERIFLE", 1.0f));
        }

        private SupportSquadManager CreateManager()
        {
            return new SupportSquadManager(
                new SupportSquadManagerDependencies
                {
                    GameBridge = _bridge,
                    Spawner = _spawnerMock.Object,
                    StatsProvider = _statsProviderMock.Object,
                    ZoneService = _zoneServiceMock.Object,
                    PedDespawn = _pedDespawnMock.Object,
                    PedBlip = _pedBlipMock.Object
                },
                PlayerFactionId);
        }

        private static Zone CreateZone() =>
            new Zone("downtown", "Downtown", new Vector3(0f, 0f, 0f), 100f) { OwnerFactionId = PlayerFactionId };

        [Fact]
        public void Constructor_ThrowsOnNullGameBridge()
        {
            Assert.Throws<ArgumentNullException>(() => new SupportSquadManager(
                null,
                _spawnerMock.Object,
                _statsProviderMock.Object,
                _zoneServiceMock.Object,
                _pedDespawnMock.Object,
                _pedBlipMock.Object,
                PlayerFactionId));
        }

        [Fact]
        public void Constructor_ThrowsOnNullSpawner()
        {
            Assert.Throws<ArgumentNullException>(() => new SupportSquadManager(
                _bridge,
                null,
                _statsProviderMock.Object,
                _zoneServiceMock.Object,
                _pedDespawnMock.Object,
                _pedBlipMock.Object,
                PlayerFactionId));
        }

        [Fact]
        public void Constructor_ThrowsOnNullStatsProvider()
        {
            Assert.Throws<ArgumentNullException>(() => new SupportSquadManager(
                _bridge,
                _spawnerMock.Object,
                null,
                _zoneServiceMock.Object,
                _pedDespawnMock.Object,
                _pedBlipMock.Object,
                PlayerFactionId));
        }

        [Fact]
        public void Constructor_ThrowsOnNullZoneService()
        {
            Assert.Throws<ArgumentNullException>(() => new SupportSquadManager(
                _bridge,
                _spawnerMock.Object,
                _statsProviderMock.Object,
                null,
                _pedDespawnMock.Object,
                _pedBlipMock.Object,
                PlayerFactionId));
        }

        [Fact]
        public void Constructor_ThrowsOnNullPedDespawn()
        {
            Assert.Throws<ArgumentNullException>(() => new SupportSquadManager(
                _bridge,
                _spawnerMock.Object,
                _statsProviderMock.Object,
                _zoneServiceMock.Object,
                null,
                _pedBlipMock.Object,
                PlayerFactionId));
        }

        [Fact]
        public void Constructor_ThrowsOnNullPedBlip()
        {
            Assert.Throws<ArgumentNullException>(() => new SupportSquadManager(
                _bridge,
                _spawnerMock.Object,
                _statsProviderMock.Object,
                _zoneServiceMock.Object,
                _pedDespawnMock.Object,
                null,
                PlayerFactionId));
        }

        [Fact]
        public void Constructor_ThrowsOnNullPlayerFactionId()
        {
            Assert.Throws<ArgumentNullException>(() => new SupportSquadManager(
                _bridge,
                _spawnerMock.Object,
                _statsProviderMock.Object,
                _zoneServiceMock.Object,
                _pedDespawnMock.Object,
                _pedBlipMock.Object,
                null!));
        }

        [Fact]
        public void CallSupportSquad_SpawnsVehicleAndEightSeatedNonFollowerAllies()
        {
            var zone = CreateZone();
            var manager = CreateManager();
            var vehiclesBefore = _bridge.GetSpawnedVehicleCount();

            manager.CallSupportSquad(zone);

            // A vehicle was created.
            Assert.Equal(vehiclesBefore + 1, _bridge.GetSpawnedVehicleCount());

            // 8 friendly allies spawned, all in the player's own faction (companion-not-follower).
            _spawnerMock.Verify(
                s => s.Spawn(PlayerFactionId, PlayerFactionId, It.IsAny<string>(), It.IsAny<Vector3>(), zone.Id),
                Times.Exactly(8));

            var spawnedPeds = _bridge.GetSpawnedPeds();
            Assert.Equal(8, spawnedPeds.Count);

            // Every ally was seated (SetPedIntoVehicle for indices 0..7) and none joined the
            // player's follower group - these are temporary support, not crew.
            int? suv = null;
            foreach (var ped in spawnedPeds)
            {
                Assert.True(_bridge.IsPedInVehicle(ped));
                var vehicle = _bridge.GetPedVehicle(ped);
                suv ??= vehicle;
                Assert.Equal(suv, vehicle);
                Assert.Equal(0, _bridge.GetSetAsFollowerCallCount(ped));
            }
            Assert.False(_bridge.IsPedFollowingPlayer(spawnedPeds[0]));

            // A drive task was issued for the SUV.
            Assert.NotNull(_bridge.GetVehicleDriveTargetForTest(suv!.Value));

            Assert.True(manager.HasActiveSquad);
        }

        [Fact]
        public void CallSupportSquad_WhileActive_IsNoOp()
        {
            var zone = CreateZone();
            var manager = CreateManager();

            manager.CallSupportSquad(zone);
            var vehicleCountAfterFirstCall = _bridge.GetSpawnedVehicleCount();

            manager.CallSupportSquad(zone);

            Assert.Equal(vehicleCountAfterFirstCall, _bridge.GetSpawnedVehicleCount());
            _spawnerMock.Verify(
                s => s.Spawn(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Vector3>(), It.IsAny<string>()),
                Times.Exactly(8));
        }

        [Fact]
        public void Update_InboundWithinDismountRange_DismountsAllies()
        {
            var zone = CreateZone();
            var manager = CreateManager();

            manager.CallSupportSquad(zone);
            var spawnedPeds = _bridge.GetSpawnedPeds();
            var suv = _bridge.GetPedVehicle(spawnedPeds[0]);

            // Put the player right on top of the SUV - well within DismountRange.
            _bridge.PlayerPosition = _bridge.GetVehiclePosition(suv);

            manager.Update(new List<EnemyTarget>());

            foreach (var ped in spawnedPeds)
            {
                Assert.False(_bridge.IsPedInVehicle(ped));
            }
        }

        [Fact]
        public void Update_InboundFarFromPlayer_DoesNotDismount()
        {
            var zone = CreateZone();
            var manager = CreateManager();

            manager.CallSupportSquad(zone);
            var spawnedPeds = _bridge.GetSpawnedPeds();

            // Player is nowhere near the SUV's spawn point.
            _bridge.PlayerPosition = new Vector3(5000f, 5000f, 0f);

            manager.Update(new List<EnemyTarget>());

            foreach (var ped in spawnedPeds)
            {
                Assert.True(_bridge.IsPedInVehicle(ped));
            }
        }

        [Fact]
        public void Update_InboundStalledSuv_ReassertsDriveTaskTowardPlayer()
        {
            var zone = CreateZone();
            var manager = CreateManager();

            _bridge.PlayerPosition = new Vector3(0f, 0f, 0f);
            manager.CallSupportSquad(zone);
            var spawnedPeds = _bridge.GetSpawnedPeds();
            var suv = _bridge.GetPedVehicle(spawnedPeds[0]);

            // Player has moved; the SUV has not (mock vehicles never move), so after the stall
            // interval the manager re-issues the drive task toward the player's CURRENT position.
            var newPlayerPos = new Vector3(500f, 500f, 0f);
            _bridge.PlayerPosition = newPlayerPos;
            _bridge.AdvanceGameTime(3000);

            manager.Update(new List<EnemyTarget>());

            Assert.Equal(newPlayerPos, _bridge.GetVehicleDriveTargetForTest(suv));
        }

        [Fact]
        public void Update_InboundBeforeStallInterval_DoesNotReassertDriveTask()
        {
            var zone = CreateZone();
            var manager = CreateManager();

            var originalPlayerPos = new Vector3(0f, 0f, 0f);
            _bridge.PlayerPosition = originalPlayerPos;
            manager.CallSupportSquad(zone);
            var spawnedPeds = _bridge.GetSpawnedPeds();
            var suv = _bridge.GetPedVehicle(spawnedPeds[0]);

            // No game time has passed since the call: the original drive target must stand.
            _bridge.PlayerPosition = new Vector3(500f, 500f, 0f);

            manager.Update(new List<EnemyTarget>());

            Assert.Equal(originalPlayerPos, _bridge.GetVehicleDriveTargetForTest(suv));
        }

        [Fact]
        public void Update_AllAlliesDead_ClearsActiveSquad()
        {
            var zone = CreateZone();
            var manager = CreateManager();

            manager.CallSupportSquad(zone);
            var spawnedPeds = _bridge.GetSpawnedPeds();
            foreach (var ped in spawnedPeds)
            {
                _bridge.KillPed(ped);
            }

            manager.Update(new List<EnemyTarget>());

            Assert.False(manager.HasActiveSquad);
        }

        [Fact]
        public void Update_SomeAlliesDead_KeepsActiveSquad()
        {
            var zone = CreateZone();
            var manager = CreateManager();

            manager.CallSupportSquad(zone);
            var spawnedPeds = _bridge.GetSpawnedPeds();
            _bridge.KillPed(spawnedPeds[0]);

            manager.Update(new List<EnemyTarget>());

            Assert.True(manager.HasActiveSquad);
        }

        [Fact]
        public void Update_AllAlliesDead_FreesPoolSlotsAndBlips()
        {
            var zone = CreateZone();
            var manager = CreateManager();

            manager.CallSupportSquad(zone);
            var spawnedPeds = _bridge.GetSpawnedPeds();
            foreach (var ped in spawnedPeds)
            {
                _bridge.KillPed(ped);
            }

            manager.Update(new List<EnemyTarget>());

            // Each dead ally is a corpse still present in the world (KillPed only flips
            // IsAlive), so pruning must free the pool slot via DespawnPed, not UntrackPed.
            foreach (var ped in spawnedPeds)
            {
                _pedBlipMock.Verify(b => b.RemoveBlipForPed(ped), Times.Once);
                _pedDespawnMock.Verify(d => d.DespawnPed(ped), Times.Once);
            }
            _pedDespawnMock.Verify(d => d.UntrackPed(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public void Update_AllAlliesDead_DeletesSuv()
        {
            var zone = CreateZone();
            var manager = CreateManager();
            var vehiclesBefore = _bridge.GetSpawnedVehicleCount();

            manager.CallSupportSquad(zone);
            var spawnedPeds = _bridge.GetSpawnedPeds();
            foreach (var ped in spawnedPeds)
            {
                _bridge.KillPed(ped);
            }

            manager.Update(new List<EnemyTarget>());

            Assert.Equal(vehiclesBefore, _bridge.GetSpawnedVehicleCount());
        }

        [Fact]
        public void DespawnSquad_DeletesSuvAndRemainingAllies()
        {
            var zone = CreateZone();
            var manager = CreateManager();
            var vehiclesBefore = _bridge.GetSpawnedVehicleCount();

            manager.CallSupportSquad(zone);
            var spawnedPeds = _bridge.GetSpawnedPeds();

            manager.DespawnSquad();

            Assert.Equal(vehiclesBefore, _bridge.GetSpawnedVehicleCount());
            foreach (var ped in spawnedPeds)
            {
                _pedBlipMock.Verify(b => b.RemoveBlipForPed(ped), Times.Once);
                _pedDespawnMock.Verify(d => d.DespawnPed(ped), Times.Once);
            }
            Assert.False(manager.HasActiveSquad);
        }

        [Fact]
        public void CallSupportSquad_ReturnsTrue_OnSuccessfulSpawn()
        {
            var zone = CreateZone();
            var manager = CreateManager();

            var result = manager.CallSupportSquad(zone);

            Assert.True(result);
        }

        [Fact]
        public void DespawnSquad_NoActiveSquad_DoesNothing()
        {
            var manager = CreateManager();

            manager.DespawnSquad();

            Assert.False(manager.HasActiveSquad);
            _pedBlipMock.Verify(b => b.RemoveBlipForPed(It.IsAny<int>()), Times.Never);
            _pedDespawnMock.Verify(d => d.DespawnPed(It.IsAny<int>()), Times.Never);
            _pedDespawnMock.Verify(d => d.UntrackPed(It.IsAny<int>()), Times.Never);
        }
    }
}
