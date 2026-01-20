using System.Collections.Generic;
using FactionWars.AI.Interfaces;
using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Models;
using FactionWars.Combat.Pools;
using FactionWars.Combat.Services;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.Core.Services;
using FactionWars.Core.Utils;
using FactionWars.ScriptHookV.Managers;
using FactionWars.Territory.Models;
using Moq;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV.Managers
{
    public class CombatManagerImmediateSpawnTests
    {
        [Fact]
        public void SpawnAllDefendersImmediately_SpawnsAllTiersAtOnce()
        {
            // Arrange
            var gameBridge = new MockGameBridge { PlayerPosition = new Vector3(100, 100, 0) };
            var pedPool = new InMemoryPedPool(30);
            var pedSpawningService = new PedSpawningService(gameBridge, pedPool);
            var spawnPositionCalculator = new SpawnPositionCalculator(gameBridge);
            var controlCalculator = new ControlPercentageCalculator();
            var takeoverDetector = new TakeoverDetector();
            var combatResultHandlerMock = new Mock<ICombatResultHandler>();
            var waveSpawnerService = new WaveSpawnerService();
            var followerService = new FollowerService();
            var pedDespawnServiceMock = new Mock<IPedDespawnService>();
            pedDespawnServiceMock.Setup(p => p.DespawnPedsByZone(It.IsAny<string>())).Returns(new List<PedHandle>());
            pedDespawnServiceMock.Setup(p => p.DespawnDeadPeds()).Returns(new List<PedHandle>());
            var aggressionResponseServiceMock = new Mock<IAggressionResponseService>();

            var combatManager = new CombatManager(
                gameBridge,
                pedPool,
                pedSpawningService,
                pedDespawnServiceMock.Object,
                spawnPositionCalculator,
                controlCalculator,
                takeoverDetector,
                combatResultHandlerMock.Object,
                waveSpawnerService,
                followerService,
                aggressionResponseServiceMock.Object);

            var zone = new Zone("zone-1", "Test", new Vector3(100, 100, 0), 200f, 5);
            zone.OwnerFactionId = "faction-trevor";

            combatManager.StartCombat(zone, "faction-michael");

            var spawnPlan = new DefenderSpawnPlan(basicPeds: 5, mediumPeds: 3, heavyPeds: 2);

            var models = new Dictionary<DefenderTier, string>
            {
                { DefenderTier.Heavy, "model_heavy" },
                { DefenderTier.Medium, "model_medium" },
                { DefenderTier.Basic, "model_basic" }
            };

            // Act
            var spawned = combatManager.SpawnAllDefendersImmediately(spawnPlan, models, zone.OwnerFactionId, zone.Center);

            // Assert
            Assert.Equal(10, spawned.Count);
        }

        [Fact]
        public void SpawnAllDefendersImmediately_WhenNotInCombat_ThrowsException()
        {
            // Arrange - create manager but don't start combat
            var gameBridge = new MockGameBridge();
            var pedPool = new InMemoryPedPool(30);
            var pedSpawningService = new PedSpawningService(gameBridge, pedPool);
            var spawnPositionCalculator = new SpawnPositionCalculator(gameBridge);
            var controlCalculator = new ControlPercentageCalculator();
            var takeoverDetector = new TakeoverDetector();
            var combatResultHandlerMock = new Mock<ICombatResultHandler>();
            var waveSpawnerService = new WaveSpawnerService();
            var followerService = new FollowerService();
            var pedDespawnServiceMock = new Mock<IPedDespawnService>();
            pedDespawnServiceMock.Setup(p => p.DespawnPedsByZone(It.IsAny<string>())).Returns(new List<PedHandle>());
            var aggressionResponseServiceMock = new Mock<IAggressionResponseService>();

            var combatManager = new CombatManager(
                gameBridge,
                pedPool,
                pedSpawningService,
                pedDespawnServiceMock.Object,
                spawnPositionCalculator,
                controlCalculator,
                takeoverDetector,
                combatResultHandlerMock.Object,
                waveSpawnerService,
                followerService,
                aggressionResponseServiceMock.Object);

            var spawnPlan = new DefenderSpawnPlan();
            var models = new Dictionary<DefenderTier, string>();

            // Act & Assert
            Assert.Throws<System.InvalidOperationException>(() =>
                combatManager.SpawnAllDefendersImmediately(spawnPlan, models, "faction-1", new Vector3(0, 0, 0)));
        }
    }
}
