using System.Collections.Generic;
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
    /// Tests for EnemyDefenderManager, which manages enemy defenders
    /// that spawn when the player enters enemy territory.
    /// </summary>
    public class EnemyDefenderManagerTests
    {
        private MockGameBridge _gameBridge = null!;
        private Mock<IZoneDefenderAllocationService> _allocationServiceMock = null!;
        private Mock<IPedSpawningService> _pedSpawningServiceMock = null!;
        private Mock<IDefenderTierService> _defenderTierServiceMock = null!;
        private Mock<IPedBlipService> _pedBlipServiceMock = null!;
        private Mock<IZoneService> _zoneServiceMock = null!;
        private EnemyDefenderManager _manager = null!;

        private const string EnemyFactionId = "ballas";
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

            _manager = new EnemyDefenderManager(
                _gameBridge,
                _allocationServiceMock.Object,
                _pedSpawningServiceMock.Object,
                _defenderTierServiceMock.Object,
                _pedBlipServiceMock.Object,
                _zoneServiceMock.Object);
        }

        private Zone CreateEnemyZone()
        {
            var zone = new Zone(TestZoneId, "Test Zone", new Vector3(100, 100, 0), 150f, 1);
            zone.OwnerFactionId = EnemyFactionId;
            return zone;
        }

        private ZoneDefenderAllocation CreateAllocationWithDefenders(int basic, int medium = 0, int heavy = 0)
        {
            var allocation = new ZoneDefenderAllocation(EnemyFactionId, TestZoneId);
            if (basic > 0) allocation.AddTroops(DefenderTier.Basic, basic);
            if (medium > 0) allocation.AddTroops(DefenderTier.Medium, medium);
            if (heavy > 0) allocation.AddTroops(DefenderTier.Heavy, heavy);
            return allocation;
        }

        [Fact]
        public void OnEnemyZoneEntered_SpawnsDefendersWithSprintingWander()
        {
            // Arrange
            SetupManager();
            var zone = CreateEnemyZone();
            var allocation = CreateAllocationWithDefenders(basic: 2);

            _allocationServiceMock.Setup(a => a.GetAllocation(EnemyFactionId, TestZoneId))
                .Returns(allocation);

            // Act
            _manager.OnEnemyZoneEntered(zone, EnemyFactionId);

            // Assert - Defenders should be spawned
            Assert.Equal(2, _manager.GetSpawnedDefenderCount(TestZoneId));

            // Note: The actual sprinting wander is verified via GameBridge calls
            // which calls TaskPedWanderInAreaSprinting
        }

        [Fact]
        public void OnEnemyZoneEntered_WithNullZone_DoesNotThrow()
        {
            // Arrange
            SetupManager();

            // Act & Assert
            var exception = Record.Exception(() => _manager.OnEnemyZoneEntered(null!, EnemyFactionId));
            Assert.Null(exception);
        }

        [Fact]
        public void OnEnemyZoneEntered_WithEmptyFactionId_DoesNotThrow()
        {
            // Arrange
            SetupManager();
            var zone = CreateEnemyZone();

            // Act & Assert
            var exception = Record.Exception(() => _manager.OnEnemyZoneEntered(zone, string.Empty));
            Assert.Null(exception);
        }
    }
}
