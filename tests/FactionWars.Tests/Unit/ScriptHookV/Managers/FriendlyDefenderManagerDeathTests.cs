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

namespace FactionWars.Tests.Unit.ScriptHookV.Managers
{
    /// <summary>
    /// Tests for FriendlyDefenderManager death detection, replacement spawning,
    /// and territory loss functionality.
    /// </summary>
    public class FriendlyDefenderManagerDeathTests
    {
        private MockGameBridge _gameBridge = null!;
        private Mock<IZoneDefenderAllocationService> _allocationServiceMock = null!;
        private Mock<IPedSpawningService> _pedSpawningServiceMock = null!;
        private Mock<IDefenderTierService> _defenderTierServiceMock = null!;
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

        private ZoneDefenderAllocation CreateAllocationWithDefenders(int basic, int medium = 0, int heavy = 0)
        {
            var allocation = new ZoneDefenderAllocation(PlayerFactionId, TestZoneId);
            if (basic > 0) allocation.AddTroops(DefenderTier.Basic, basic);
            if (medium > 0) allocation.AddTroops(DefenderTier.Medium, medium);
            if (heavy > 0) allocation.AddTroops(DefenderTier.Heavy, heavy);
            return allocation;
        }

        [Fact]
        public void GetSpawnedDefenderCount_WhenNoDefenders_ShouldReturnZero()
        {
            // Arrange
            SetupManager();

            // Act
            var count = _manager.GetSpawnedDefenderCount("zone1");

            // Assert
            Assert.Equal(0, count);
        }

        [Fact]
        public void Update_WhenDefenderDies_ShouldRemoveBlip()
        {
            // Arrange
            SetupManager();
            var zone = CreateFriendlyZone();
            var allocation = CreateAllocationWithDefenders(basic: 1);

            _allocationServiceMock.Setup(a => a.GetAllocation(PlayerFactionId, TestZoneId))
                .Returns(allocation);

            // Spawn a defender
            _manager.OnZoneEntered(zone);
            var spawnedCount = _manager.GetSpawnedDefenderCount(TestZoneId);
            Assert.Equal(1, spawnedCount);

            // Get the ped handle that was spawned
            var spawnedPedHandle = _gameBridge.GetSpawnedPeds()[0];

            // Now kill the defender
            _gameBridge.SetPedDead(spawnedPedHandle);

            // Act
            _manager.Update();

            // Assert
            _pedBlipServiceMock.Verify(b => b.RemoveBlipForPed(spawnedPedHandle), Times.AtLeastOnce);
        }

        [Fact]
        public void Update_WhenDefenderDies_ShouldRaiseDefenderDiedEvent()
        {
            // Arrange
            SetupManager();
            var zone = CreateFriendlyZone();
            var allocation = CreateAllocationWithDefenders(basic: 1);
            DefenderDiedEventArgs? receivedArgs = null;

            _allocationServiceMock.Setup(a => a.GetAllocation(PlayerFactionId, TestZoneId))
                .Returns(allocation);

            _manager.OnZoneEntered(zone);
            _manager.DefenderDied += (sender, args) => receivedArgs = args;

            // Get the ped handle and kill it
            var spawnedPedHandle = _gameBridge.GetSpawnedPeds()[0];
            _gameBridge.SetPedDead(spawnedPedHandle);

            // Act
            _manager.Update();

            // Assert
            Assert.NotNull(receivedArgs);
            Assert.Equal(TestZoneId, receivedArgs!.ZoneId);
            Assert.Equal(spawnedPedHandle, receivedArgs.PedHandle);
            Assert.Equal(DefenderTier.Basic, receivedArgs.Tier);
        }

        [Fact]
        public void Update_WhenDefenderDies_ShouldDecrementAllocation()
        {
            // Arrange
            SetupManager();
            var zone = CreateFriendlyZone();
            var allocation = CreateAllocationWithDefenders(basic: 2);

            _allocationServiceMock.Setup(a => a.GetAllocation(PlayerFactionId, TestZoneId))
                .Returns(allocation);

            _manager.OnZoneEntered(zone);

            // Get the first ped handle and kill it
            var spawnedPedHandle = _gameBridge.GetSpawnedPeds()[0];
            _gameBridge.SetPedDead(spawnedPedHandle);

            // Act
            _manager.Update();

            // Assert - allocation should now be 1
            Assert.Equal(1, allocation.GetTroopCount(DefenderTier.Basic));
        }

        [Fact]
        public void Update_WhenDefenderDiesWithReserve_ShouldSpawnReplacement()
        {
            // Arrange
            SetupManager();
            var zone = CreateFriendlyZone();
            // Allocate 14 troops - 12 will spawn, 2 in reserve
            var allocation = CreateAllocationWithDefenders(basic: 14);

            _allocationServiceMock.Setup(a => a.GetAllocation(PlayerFactionId, TestZoneId))
                .Returns(allocation);
            _zoneServiceMock.Setup(z => z.GetZone(TestZoneId)).Returns(zone);

            _manager.OnZoneEntered(zone);

            // Verify max 12 spawned
            Assert.Equal(FriendlyDefenderManager.MaxSpawnedDefenders, _manager.GetSpawnedDefenderCount(TestZoneId));

            // Get the first ped handle and kill it
            var spawnedPedHandle = _gameBridge.GetSpawnedPeds()[0];
            _gameBridge.SetPedDead(spawnedPedHandle);

            // Act
            _manager.Update();

            // Assert - should still have 12 spawned (replacement from reserve)
            Assert.Equal(FriendlyDefenderManager.MaxSpawnedDefenders, _manager.GetSpawnedDefenderCount(TestZoneId));
        }

        [Fact]
        public void Update_WhenAllDefendersDie_ShouldRaiseTerritoryLostEvent()
        {
            // Arrange
            SetupManager();
            var zone = CreateFriendlyZone();
            var allocation = CreateAllocationWithDefenders(basic: 1);
            TerritoryLostEventArgs? receivedArgs = null;

            _allocationServiceMock.Setup(a => a.GetAllocation(PlayerFactionId, TestZoneId))
                .Returns(allocation);

            _manager.OnZoneEntered(zone);
            _manager.TerritoryLost += (sender, args) => receivedArgs = args;

            // Kill the only defender
            var spawnedPedHandle = _gameBridge.GetSpawnedPeds()[0];
            _gameBridge.SetPedDead(spawnedPedHandle);

            // Act
            _manager.Update();

            // Assert
            Assert.NotNull(receivedArgs);
            Assert.Equal(TestZoneId, receivedArgs!.ZoneId);
        }

        [Fact]
        public void Update_WhenAllDefendersDie_ShouldTransferZoneToNeutral()
        {
            // Arrange
            SetupManager();
            var zone = CreateFriendlyZone();
            var allocation = CreateAllocationWithDefenders(basic: 1);

            _allocationServiceMock.Setup(a => a.GetAllocation(PlayerFactionId, TestZoneId))
                .Returns(allocation);

            _manager.OnZoneEntered(zone);

            // Kill the only defender
            var spawnedPedHandle = _gameBridge.GetSpawnedPeds()[0];
            _gameBridge.SetPedDead(spawnedPedHandle);

            // Act
            _manager.Update();

            // Assert
            _zoneServiceMock.Verify(z => z.TransferZoneOwnership(TestZoneId, null), Times.Once);
        }

        [Fact]
        public void OnZoneEntered_WithMoreThan12Allocated_ShouldSpawnOnly12()
        {
            // Arrange
            SetupManager();
            var zone = CreateFriendlyZone();
            var allocation = CreateAllocationWithDefenders(basic: 20);

            _allocationServiceMock.Setup(a => a.GetAllocation(PlayerFactionId, TestZoneId))
                .Returns(allocation);

            // Act
            _manager.OnZoneEntered(zone);

            // Assert - only 12 should spawn
            Assert.Equal(FriendlyDefenderManager.MaxSpawnedDefenders, _manager.GetSpawnedDefenderCount(TestZoneId));
        }

        [Fact]
        public void OnZoneEntered_WithLessThan12Allocated_ShouldSpawnAll()
        {
            // Arrange
            SetupManager();
            var zone = CreateFriendlyZone();
            var allocation = CreateAllocationWithDefenders(basic: 5);

            _allocationServiceMock.Setup(a => a.GetAllocation(PlayerFactionId, TestZoneId))
                .Returns(allocation);

            // Act
            _manager.OnZoneEntered(zone);

            // Assert - all 5 should spawn
            Assert.Equal(5, _manager.GetSpawnedDefenderCount(TestZoneId));
        }

        [Fact]
        public void Update_WhenNoDeadDefenders_ShouldNotRaiseEvents()
        {
            // Arrange
            SetupManager();
            var zone = CreateFriendlyZone();
            var allocation = CreateAllocationWithDefenders(basic: 2);
            var defenderDiedRaised = false;
            var territoryLostRaised = false;

            _allocationServiceMock.Setup(a => a.GetAllocation(PlayerFactionId, TestZoneId))
                .Returns(allocation);

            _manager.OnZoneEntered(zone);
            _manager.DefenderDied += (sender, args) => defenderDiedRaised = true;
            _manager.TerritoryLost += (sender, args) => territoryLostRaised = true;

            // All defenders stay alive

            // Act
            _manager.Update();

            // Assert
            Assert.False(defenderDiedRaised);
            Assert.False(territoryLostRaised);
            Assert.Equal(2, _manager.GetSpawnedDefenderCount(TestZoneId));
        }

        [Fact]
        public void Update_WhenDefenderDiesButReserveAvailable_ShouldNotRaiseTerritoryLost()
        {
            // Arrange
            SetupManager();
            var zone = CreateFriendlyZone();
            var allocation = CreateAllocationWithDefenders(basic: 3); // 3 allocated, all spawn, 1 dies = 2 remaining
            var territoryLostRaised = false;

            _allocationServiceMock.Setup(a => a.GetAllocation(PlayerFactionId, TestZoneId))
                .Returns(allocation);
            _zoneServiceMock.Setup(z => z.GetZone(TestZoneId)).Returns(zone);

            _manager.OnZoneEntered(zone);
            _manager.TerritoryLost += (sender, args) => territoryLostRaised = true;

            // Kill one defender
            var spawnedPedHandle = _gameBridge.GetSpawnedPeds()[0];
            _gameBridge.SetPedDead(spawnedPedHandle);

            // Act
            _manager.Update();

            // Assert - territory should NOT be lost since there are still defenders
            Assert.False(territoryLostRaised);
        }

        [Fact]
        public void GetSpawnedCountByTier_ReturnsCorrectCountPerTier()
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
            Assert.Equal(2, _manager.GetSpawnedCountByTier(TestZoneId, DefenderTier.Basic));
            Assert.Equal(1, _manager.GetSpawnedCountByTier(TestZoneId, DefenderTier.Medium));
            Assert.Equal(1, _manager.GetSpawnedCountByTier(TestZoneId, DefenderTier.Heavy));
        }
    }
}
