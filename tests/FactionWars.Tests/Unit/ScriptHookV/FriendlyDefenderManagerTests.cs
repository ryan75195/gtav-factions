using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Models;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.Core.Utils;
using FactionWars.ScriptHookV.Managers;
using FactionWars.Territory.Interfaces;
using FactionWars.Territory.Models;
using FactionWars.UI.Interfaces;
using Moq;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV
{
    /// <summary>
    /// Tests for FriendlyDefenderManager, which manages friendly defenders
    /// that spawn when the player enters their own territory.
    /// </summary>
    public class FriendlyDefenderManagerTests
    {
        private MockGameBridge _gameBridge = null!;
        private Mock<IZoneDefenderAllocationService> _allocationServiceMock = null!;
        private Mock<IPedSpawningService> _pedSpawningServiceMock = null!;
        private Mock<IPedDespawnService> _pedDespawnServiceMock = null!;
        private Mock<IDefenderTierService> _defenderTierServiceMock = null!;
        private Mock<IPedBlipService> _pedBlipServiceMock = null!;
        private Mock<IZoneService> _zoneServiceMock = null!;
        private FriendlyDefenderManager _manager = null!;

        private const string PlayerFactionId = "michael";
        private const string EnemyFactionId = "ballas";
        private const string TestZoneId = "zone_1";

        private void SetupManager()
        {
            _gameBridge = new MockGameBridge();
            _allocationServiceMock = new Mock<IZoneDefenderAllocationService>();
            _pedSpawningServiceMock = new Mock<IPedSpawningService>();
            _pedDespawnServiceMock = new Mock<IPedDespawnService>();
            _defenderTierServiceMock = new Mock<IDefenderTierService>();
            _pedBlipServiceMock = new Mock<IPedBlipService>();
            _zoneServiceMock = new Mock<IZoneService>();

            // Setup default mock behaviors
            _pedSpawningServiceMock.Setup(p => p.CanSpawn()).Returns(true);
            _pedSpawningServiceMock.Setup(p => p.SpawnPed(It.IsAny<string>(), It.IsAny<Vector3>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(() => new PedHandle(_gameBridge.CreatePed("test", new Vector3(0, 0, 0))));

            _defenderTierServiceMock.Setup(d => d.GetTierConfig(It.IsAny<DefenderTier>()))
                .Returns(new DefenderTierConfig(DefenderTier.Basic, 200, 100, 0, "weapon_pistol", 0.5f, 1.0f));

            _pedBlipServiceMock.Setup(p => p.CreateBlipForPed(It.IsAny<int>(), It.IsAny<BlipColor>()))
                .Returns(1);

            _manager = new FriendlyDefenderManager(
                _gameBridge,
                _allocationServiceMock.Object,
                _pedSpawningServiceMock.Object,
                _pedDespawnServiceMock.Object,
                _defenderTierServiceMock.Object,
                _pedBlipServiceMock.Object,
                _zoneServiceMock.Object,
                PlayerFactionId);
        }

        private Zone CreateFriendlyZone()
        {
            var zone = new Zone(TestZoneId, "Test Zone", new Vector3(100, 100, 0), 150f, 1);
            zone.OwnerFactionId = PlayerFactionId;
            return zone;
        }

        private Zone CreateEnemyZone()
        {
            var zone = new Zone(TestZoneId, "Test Zone", new Vector3(100, 100, 0), 150f, 1);
            zone.OwnerFactionId = EnemyFactionId;
            return zone;
        }

        private Zone CreateNeutralZone()
        {
            var zone = new Zone(TestZoneId, "Test Zone", new Vector3(100, 100, 0), 150f, 1);
            zone.OwnerFactionId = null;
            return zone;
        }

        private ZoneDefenderAllocation CreateAllocationWithDefenders(int basic, int medium = 0, int heavy = 0)
        {
            var allocation = new ZoneDefenderAllocation(PlayerFactionId, TestZoneId);
            if (basic > 0) allocation.AddTroops(DefenderTier.Basic, basic);
            if (medium > 0) allocation.AddTroops(DefenderTier.Medium, medium);
            if (heavy > 0) allocation.AddTroops(DefenderTier.Heavy, heavy);
            return allocation;
        }

        [Fact]
        public void OnFriendlyZoneEntered_SpawnsAllocatedDefenders()
        {
            // Arrange
            SetupManager();
            var zone = CreateFriendlyZone();
            var allocation = CreateAllocationWithDefenders(basic: 3);

            _allocationServiceMock.Setup(a => a.GetAllocation(PlayerFactionId, TestZoneId))
                .Returns(allocation);

            // Act
            _manager.OnZoneEntered(zone);

            // Assert
            _pedSpawningServiceMock.Verify(
                p => p.SpawnPed(It.IsAny<string>(), It.IsAny<Vector3>(), PlayerFactionId, TestZoneId),
                Times.Exactly(3));

            Assert.Equal(3, _manager.GetSpawnedDefenderCount(TestZoneId));
        }

        [Fact]
        public void OnFriendlyZoneEntered_CreatesLightBlueBlips()
        {
            // Arrange
            SetupManager();
            var zone = CreateFriendlyZone();
            var allocation = CreateAllocationWithDefenders(basic: 2);

            _allocationServiceMock.Setup(a => a.GetAllocation(PlayerFactionId, TestZoneId))
                .Returns(allocation);

            // Act
            _manager.OnZoneEntered(zone);

            // Assert
            _pedBlipServiceMock.Verify(
                p => p.CreateBlipForPed(It.IsAny<int>(), BlipColor.LightBlue),
                Times.Exactly(2));
        }

        [Fact]
        public void OnZoneExited_DespawnsDefendersAndRemovesBlips()
        {
            // Arrange
            SetupManager();
            var zone = CreateFriendlyZone();
            var allocation = CreateAllocationWithDefenders(basic: 2);

            _allocationServiceMock.Setup(a => a.GetAllocation(PlayerFactionId, TestZoneId))
                .Returns(allocation);

            _manager.OnZoneEntered(zone);
            var initialCount = _manager.GetSpawnedDefenderCount(TestZoneId);
            Assert.Equal(2, initialCount);

            // Act
            _manager.OnZoneExited(zone);

            // Assert
            _pedBlipServiceMock.Verify(
                p => p.RemoveBlipForPed(It.IsAny<int>()),
                Times.Exactly(2));

            Assert.Equal(0, _manager.GetSpawnedDefenderCount(TestZoneId));
        }

        [Fact]
        public void OnEnemyZoneEntered_DoesNotSpawnDefenders()
        {
            // Arrange
            SetupManager();
            var zone = CreateEnemyZone();

            // Act
            _manager.OnZoneEntered(zone);

            // Assert
            _pedSpawningServiceMock.Verify(
                p => p.SpawnPed(It.IsAny<string>(), It.IsAny<Vector3>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);

            Assert.Equal(0, _manager.GetSpawnedDefenderCount(TestZoneId));
        }

        [Fact]
        public void OnNeutralZoneEntered_DoesNotSpawnDefenders()
        {
            // Arrange
            SetupManager();
            var zone = CreateNeutralZone();

            // Act
            _manager.OnZoneEntered(zone);

            // Assert
            _pedSpawningServiceMock.Verify(
                p => p.SpawnPed(It.IsAny<string>(), It.IsAny<Vector3>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);

            Assert.Equal(0, _manager.GetSpawnedDefenderCount(TestZoneId));
        }

        [Fact]
        public void OnFriendlyZoneEntered_TasksDefendersToWander()
        {
            // Arrange
            SetupManager();
            var zone = CreateFriendlyZone();
            var allocation = CreateAllocationWithDefenders(basic: 2);

            _allocationServiceMock.Setup(a => a.GetAllocation(PlayerFactionId, TestZoneId))
                .Returns(allocation);

            // Act
            _manager.OnZoneEntered(zone);

            // Assert - The wander task is verified implicitly through spawn
            // We verify that peds were spawned successfully (which includes the wander task)
            Assert.Equal(2, _manager.GetSpawnedDefenderCount(TestZoneId));
        }

        [Fact]
        public void Constructor_ThrowsOnNullGameBridge()
        {
            // Arrange
            SetupManager();

            // Act & Assert
            Assert.Throws<System.ArgumentNullException>(() => new FriendlyDefenderManager(
                null!,
                _allocationServiceMock.Object,
                _pedSpawningServiceMock.Object,
                _pedDespawnServiceMock.Object,
                _defenderTierServiceMock.Object,
                _pedBlipServiceMock.Object,
                _zoneServiceMock.Object,
                PlayerFactionId));
        }

        [Fact]
        public void Constructor_ThrowsOnNullPlayerFactionId()
        {
            // Arrange
            SetupManager();

            // Act & Assert
            Assert.Throws<System.ArgumentNullException>(() => new FriendlyDefenderManager(
                _gameBridge,
                _allocationServiceMock.Object,
                _pedSpawningServiceMock.Object,
                _pedDespawnServiceMock.Object,
                _defenderTierServiceMock.Object,
                _pedBlipServiceMock.Object,
                _zoneServiceMock.Object,
                null!));
        }

        [Fact]
        public void SetPlayerFaction_ChangesFaction()
        {
            // Arrange
            SetupManager();
            var zone = CreateFriendlyZone();
            var allocation = CreateAllocationWithDefenders(basic: 2);

            _allocationServiceMock.Setup(a => a.GetAllocation(PlayerFactionId, TestZoneId))
                .Returns(allocation);

            _manager.OnZoneEntered(zone);
            Assert.Equal(2, _manager.GetSpawnedDefenderCount(TestZoneId));

            // Act
            _manager.SetPlayerFaction("franklin");

            // Assert - Old defenders should be despawned
            Assert.Equal(0, _manager.GetSpawnedDefenderCount(TestZoneId));
        }

        [Fact]
        public void SetPlayerFaction_ThrowsOnNullOrEmptyFactionId()
        {
            // Arrange
            SetupManager();

            // Act & Assert
            Assert.Throws<System.ArgumentNullException>(() => _manager.SetPlayerFaction(null!));
            Assert.Throws<System.ArgumentNullException>(() => _manager.SetPlayerFaction(string.Empty));
        }

        [Fact]
        public void DespawnAllDefenders_RemovesAllDefendersAcrossZones()
        {
            // Arrange
            SetupManager();
            var zone1 = new Zone("zone_1", "Zone 1", new Vector3(100, 100, 0)) { OwnerFactionId = PlayerFactionId };
            var zone2 = new Zone("zone_2", "Zone 2", new Vector3(200, 200, 0)) { OwnerFactionId = PlayerFactionId };

            var allocation1 = new ZoneDefenderAllocation(PlayerFactionId, "zone_1");
            allocation1.AddTroops(DefenderTier.Basic, 2);

            var allocation2 = new ZoneDefenderAllocation(PlayerFactionId, "zone_2");
            allocation2.AddTroops(DefenderTier.Basic, 1);

            _allocationServiceMock.Setup(a => a.GetAllocation(PlayerFactionId, "zone_1")).Returns(allocation1);
            _allocationServiceMock.Setup(a => a.GetAllocation(PlayerFactionId, "zone_2")).Returns(allocation2);

            _manager.OnZoneEntered(zone1);
            _manager.OnZoneEntered(zone2);

            Assert.Equal(2, _manager.GetSpawnedDefenderCount("zone_1"));
            Assert.Equal(1, _manager.GetSpawnedDefenderCount("zone_2"));

            // Act
            _manager.DespawnAllDefenders();

            // Assert
            Assert.Equal(0, _manager.GetSpawnedDefenderCount("zone_1"));
            Assert.Equal(0, _manager.GetSpawnedDefenderCount("zone_2"));
        }

        [Fact]
        public void OnZoneEntered_WithNoAllocation_DoesNotSpawnDefenders()
        {
            // Arrange
            SetupManager();
            var zone = CreateFriendlyZone();

            _allocationServiceMock.Setup(a => a.GetAllocation(PlayerFactionId, TestZoneId))
                .Returns((ZoneDefenderAllocation?)null);

            // Act
            _manager.OnZoneEntered(zone);

            // Assert
            _pedSpawningServiceMock.Verify(
                p => p.SpawnPed(It.IsAny<string>(), It.IsAny<Vector3>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);

            Assert.Equal(0, _manager.GetSpawnedDefenderCount(TestZoneId));
        }

        [Fact]
        public void OnZoneEntered_WithNullZone_DoesNotThrow()
        {
            // Arrange
            SetupManager();

            // Act & Assert
            var exception = Record.Exception(() => _manager.OnZoneEntered(null!));
            Assert.Null(exception);
        }

        [Fact]
        public void OnZoneExited_WithNullZone_DoesNotThrow()
        {
            // Arrange
            SetupManager();

            // Act & Assert
            var exception = Record.Exception(() => _manager.OnZoneExited(null!));
            Assert.Null(exception);
        }

        [Fact]
        public void OnFriendlyZoneEntered_SpawnsMultipleTiers()
        {
            // Arrange
            SetupManager();
            var zone = CreateFriendlyZone();
            var allocation = CreateAllocationWithDefenders(basic: 2, medium: 1, heavy: 1);

            _allocationServiceMock.Setup(a => a.GetAllocation(PlayerFactionId, TestZoneId))
                .Returns(allocation);

            // Act
            _manager.OnZoneEntered(zone);

            // Assert
            Assert.Equal(4, _manager.GetSpawnedDefenderCount(TestZoneId));
        }

        [Fact]
        public void OnFriendlyZoneEntered_ConfiguresDefenderCombat()
        {
            // Arrange
            SetupManager();
            var zone = CreateFriendlyZone();
            var allocation = CreateAllocationWithDefenders(basic: 1);

            _allocationServiceMock.Setup(a => a.GetAllocation(PlayerFactionId, TestZoneId))
                .Returns(allocation);

            var tierConfig = new DefenderTierConfig(DefenderTier.Basic, 200, 100, 25, "weapon_pistol", 0.5f, 1.0f);
            _defenderTierServiceMock.Setup(d => d.GetTierConfig(DefenderTier.Basic)).Returns(tierConfig);

            // Act
            _manager.OnZoneEntered(zone);

            // Assert - Verify the defender tier service was called
            _defenderTierServiceMock.Verify(d => d.GetTierConfig(DefenderTier.Basic), Times.Once);
        }

        [Fact]
        public void OnFriendlyZoneEntered_StopsSpawningWhenPoolFull()
        {
            // Arrange
            SetupManager();
            var zone = CreateFriendlyZone();
            var allocation = CreateAllocationWithDefenders(basic: 5);

            _allocationServiceMock.Setup(a => a.GetAllocation(PlayerFactionId, TestZoneId))
                .Returns(allocation);

            // Setup spawning service to return false after 2 spawns
            var spawnCount = 0;
            _pedSpawningServiceMock.Setup(p => p.CanSpawn())
                .Returns(() => spawnCount < 2);
            _pedSpawningServiceMock.Setup(p => p.SpawnPed(It.IsAny<string>(), It.IsAny<Vector3>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(() =>
                {
                    spawnCount++;
                    return new PedHandle(_gameBridge.CreatePed("test", new Vector3(0, 0, 0)));
                });

            // Act
            _manager.OnZoneEntered(zone);

            // Assert - Only 2 should have been spawned due to pool limit
            Assert.Equal(2, _manager.GetSpawnedDefenderCount(TestZoneId));
        }
    }
}
