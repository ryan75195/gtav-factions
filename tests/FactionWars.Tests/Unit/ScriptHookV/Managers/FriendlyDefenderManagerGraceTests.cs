using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Models;
using FactionWars.Configuration;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.Core.Utils;
using FactionWars.ScriptHookV.Managers;
using FactionWars.Territory.Interfaces;
using FactionWars.Territory.Models;
using FactionWars.UI.Interfaces;
using Moq;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV.Managers
{
    /// <summary>
    /// Tests for the grace-state deferral: when a player-owned zone's last defender dies
    /// while the player is in that zone and alive, ownership transfer is deferred (not lost)
    /// until the player dies, leaves the zone undefended, or a defender is redeployed.
    /// </summary>
    public class FriendlyDefenderManagerGraceTests
    {
        private MockGameBridge _gameBridge = null!;
        private Mock<IZoneDefenderAllocationService> _allocationServiceMock = null!;
        private Mock<IPedSpawningService> _pedSpawningServiceMock = null!;
        private Mock<IPedDespawnService> _pedDespawnServiceMock = null!;
        private Mock<IDefenderRoleService> _defenderRoleServiceMock = null!;
        private Mock<IPedBlipService> _pedBlipServiceMock = null!;
        private Mock<IZoneService> _zoneServiceMock = null!;
        private FriendlyDefenderManager _manager = null!;

        private const string PlayerFactionId = "michael";
        private const string TestZoneId = "zone_1";

        private void SetupManager()
        {
            _gameBridge = new MockGameBridge();
            _allocationServiceMock = new Mock<IZoneDefenderAllocationService>();
            _pedSpawningServiceMock = new Mock<IPedSpawningService>();
            _pedDespawnServiceMock = new Mock<IPedDespawnService>();
            _defenderRoleServiceMock = new Mock<IDefenderRoleService>();
            _pedBlipServiceMock = new Mock<IPedBlipService>();
            _zoneServiceMock = new Mock<IZoneService>();

            _pedSpawningServiceMock.Setup(p => p.CanSpawn()).Returns(true);
            _pedSpawningServiceMock.Setup(p => p.SpawnPed(It.IsAny<string>(), It.IsAny<Vector3>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(() => new PedHandle(_gameBridge.CreatePed("test", new Vector3(0, 0, 0))));

            _defenderRoleServiceMock.Setup(d => d.GetRoleConfig(It.IsAny<DefenderRole>()))
                .Returns(new DefenderRoleConfig(DefenderRole.Grunt, 200, 100, 0, "weapon_pistol", 0.5f, 1.0f));

            _pedBlipServiceMock.Setup(p => p.CreateBlipForPed(It.IsAny<int>(), It.IsAny<BlipColor>()))
                .Returns(1);

            _manager = new FriendlyDefenderManager(
                _gameBridge,
                _allocationServiceMock.Object,
                _pedSpawningServiceMock.Object,
                _pedDespawnServiceMock.Object,
                _defenderRoleServiceMock.Object,
                _pedBlipServiceMock.Object,
                _zoneServiceMock.Object,
                PlayerFactionId,
                CombatantStatsProviderFactory.Create(new CombatantsConfig()));
        }

        private Zone CreateFriendlyZone()
        {
            var zone = new Zone(TestZoneId, "Test Zone", new Vector3(100, 100, 0), 150f, 1);
            zone.OwnerFactionId = PlayerFactionId;
            return zone;
        }

        private ZoneDefenderAllocation CreateAllocationWithDefenders(int basic)
        {
            var allocation = new ZoneDefenderAllocation(PlayerFactionId, TestZoneId);
            if (basic > 0) allocation.AddTroops(DefenderRole.Grunt, basic);
            return allocation;
        }

        /// <summary>
        /// Drives the zone into grace: player alive and present in the zone when the
        /// last defender dies. Leaves the zone/allocation mocks wired up so callers can
        /// mutate them further (redeploy, move the player, kill the player, etc).
        /// </summary>
        private ZoneDefenderAllocation EnterGraceZone()
        {
            SetupManager();
            var zone = CreateFriendlyZone();
            var allocation = CreateAllocationWithDefenders(basic: 1);

            _allocationServiceMock.Setup(a => a.GetAllocation(PlayerFactionId, TestZoneId)).Returns(allocation);
            _zoneServiceMock.Setup(z => z.GetZone(TestZoneId)).Returns(zone);
            _zoneServiceMock.Setup(z => z.GetZoneAtPosition(It.IsAny<Vector3>())).Returns(zone);
            _gameBridge.IsPlayerDeadValue = false;

            _manager.OnZoneEntered(zone);
            var spawnedPedHandle = _gameBridge.GetSpawnedPeds()[0];
            _gameBridge.SetPedDead(spawnedPedHandle);

            _manager.Update();

            return allocation;
        }

        // A) Player in the zone + alive when the last defender dies -> loss deferred (no transfer), grace entered.
        [Fact]
        public void LastDefenderDies_PlayerInZoneAndAlive_DoesNotTransferOwnership()
        {
            EnterGraceZone();

            _zoneServiceMock.Verify(z => z.TransferZoneOwnership(TestZoneId, It.IsAny<string?>()), Times.Never());
        }

        // B) Player NOT in the zone when the last defender dies -> immediate loss (unchanged behavior).
        [Fact]
        public void LastDefenderDies_PlayerNotInZone_TransfersToNeutralImmediately()
        {
            SetupManager();
            var zone = CreateFriendlyZone();
            var allocation = CreateAllocationWithDefenders(basic: 1);

            _allocationServiceMock.Setup(a => a.GetAllocation(PlayerFactionId, TestZoneId)).Returns(allocation);
            // GetZoneAtPosition is left unconfigured (returns null) -> player is not in any zone.

            _manager.OnZoneEntered(zone);
            var spawnedPedHandle = _gameBridge.GetSpawnedPeds()[0];
            _gameBridge.SetPedDead(spawnedPedHandle);

            _manager.Update();

            _zoneServiceMock.Verify(z => z.TransferZoneOwnership(TestZoneId, null), Times.Once());
        }

        // C) Zone in grace, then a defender is present again -> saved (still no transfer).
        [Fact]
        public void GraceZone_DefenderRedeployed_IsSavedNotLost()
        {
            var allocation = EnterGraceZone();
            var zone = CreateFriendlyZone();

            allocation.AddTroops(DefenderRole.Grunt, 1);
            _manager.OnZoneEntered(zone);
            Assert.True(_manager.GetSpawnedDefenderCount(TestZoneId) > 0);

            _manager.Update();

            _zoneServiceMock.Verify(z => z.TransferZoneOwnership(TestZoneId, It.IsAny<string?>()), Times.Never());
        }

        // D) Zone in grace, player dies -> lost.
        [Fact]
        public void GraceZone_PlayerDies_TransfersToNeutral()
        {
            EnterGraceZone();

            _gameBridge.IsPlayerDeadValue = true;
            _manager.Update();

            _zoneServiceMock.Verify(z => z.TransferZoneOwnership(TestZoneId, null), Times.Once());
        }

        // E) Zone in grace, player leaves while still undefended -> lost.
        [Fact]
        public void GraceZone_PlayerLeavesUndefended_TransfersToNeutral()
        {
            EnterGraceZone();

            _zoneServiceMock.Setup(z => z.GetZoneAtPosition(It.IsAny<Vector3>())).Returns((Zone?)null);
            _manager.Update();

            _zoneServiceMock.Verify(z => z.TransferZoneOwnership(TestZoneId, null), Times.Once());
        }

        // F) Zone in grace but ownership already changed elsewhere -> grace dropped, no extra transfer.
        [Fact]
        public void GraceZone_OwnershipChangedElsewhere_DroppedWithoutTransfer()
        {
            EnterGraceZone();

            var rivalOwnedZone = new Zone(TestZoneId, "Test Zone", new Vector3(100, 100, 0), 150f, 1)
            {
                OwnerFactionId = "rival"
            };
            _zoneServiceMock.Setup(z => z.GetZone(TestZoneId)).Returns(rivalOwnedZone);

            _manager.Update();

            _zoneServiceMock.Verify(z => z.TransferZoneOwnership(TestZoneId, It.IsAny<string?>()), Times.Never());
        }
    }
}
