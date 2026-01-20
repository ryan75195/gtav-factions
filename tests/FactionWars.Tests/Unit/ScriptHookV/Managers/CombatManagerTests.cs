using System.Collections.Generic;
using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Models;
using FactionWars.Combat.Services;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.ScriptHookV.Managers;
using FactionWars.Territory.Models;
using Moq;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV.Managers
{
    public class CombatManagerTests
    {
        private readonly Mock<IGameBridge> _gameBridgeMock;
        private readonly Mock<IPedPool> _pedPoolMock;
        private readonly Mock<IPedSpawningService> _pedSpawningServiceMock;
        private readonly Mock<ISpawnPositionCalculator> _spawnPositionCalculatorMock;
        private readonly Mock<IControlPercentageCalculator> _controlCalculatorMock;
        private readonly Mock<ITakeoverDetector> _takeoverDetectorMock;
        private readonly Mock<ICombatResultHandler> _combatResultHandlerMock;
        private readonly IWaveSpawnerService _waveSpawnerService;
        private readonly Mock<IFollowerService> _followerServiceMock;
        private readonly CombatManager _manager;

        public CombatManagerTests()
        {
            _gameBridgeMock = new Mock<IGameBridge>();
            _pedPoolMock = new Mock<IPedPool>();
            _pedSpawningServiceMock = new Mock<IPedSpawningService>();
            _spawnPositionCalculatorMock = new Mock<ISpawnPositionCalculator>();
            _controlCalculatorMock = new Mock<IControlPercentageCalculator>();
            _takeoverDetectorMock = new Mock<ITakeoverDetector>();
            _combatResultHandlerMock = new Mock<ICombatResultHandler>();
            _waveSpawnerService = new WaveSpawnerService();
            _followerServiceMock = new Mock<IFollowerService>();
            _followerServiceMock.Setup(f => f.GetFollowerCount(It.IsAny<string>())).Returns(0);
            _manager = new CombatManager(
                _gameBridgeMock.Object,
                _pedPoolMock.Object,
                _pedSpawningServiceMock.Object,
                _spawnPositionCalculatorMock.Object,
                _controlCalculatorMock.Object,
                _takeoverDetectorMock.Object,
                _combatResultHandlerMock.Object,
                _waveSpawnerService,
                _followerServiceMock.Object);
        }

        [Fact]
        public void CurrentEncounter_Initially_ShouldBeNull()
        {
            // Assert
            Assert.Null(_manager.CurrentEncounter);
        }

        [Fact]
        public void IsInCombat_WhenNoEncounter_ShouldReturnFalse()
        {
            // Assert
            Assert.False(_manager.IsInCombat);
        }

        [Fact]
        public void StartCombat_WithValidZone_ShouldCreateEncounter()
        {
            // Arrange
            var zone = new Zone("zone1", "Downtown", new Vector3(100f, 200f, 30f), 100f, 10);
            zone.OwnerFactionId = "defender";

            // Act
            var encounter = _manager.StartCombat(zone, "attacker");

            // Assert
            Assert.NotNull(encounter);
            Assert.Equal("zone1", encounter.ZoneId);
            Assert.Equal("attacker", encounter.AttackingFactionId);
            Assert.Equal("defender", encounter.DefendingFactionId);
            Assert.Same(encounter, _manager.CurrentEncounter);
            Assert.True(_manager.IsInCombat);
        }

        [Fact]
        public void StartCombat_WhenAlreadyInCombat_ShouldReturnExistingEncounter()
        {
            // Arrange
            var zone = new Zone("zone1", "Downtown", new Vector3(100f, 200f, 30f), 100f, 10);
            zone.OwnerFactionId = "defender";
            var existingEncounter = _manager.StartCombat(zone, "attacker");

            // Act - try to start combat again
            var newZone = new Zone("zone2", "Industrial", new Vector3(500f, 600f, 30f), 150f, 8);
            newZone.OwnerFactionId = "defender2";
            var result = _manager.StartCombat(newZone, "attacker");

            // Assert - should return the existing encounter, not create a new one
            Assert.Same(existingEncounter, result);
        }

        [Fact]
        public void StartCombat_WithNullZone_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<System.ArgumentNullException>(() => _manager.StartCombat(null!, "attacker"));
        }

        [Fact]
        public void StartCombat_WithNullAttacker_ShouldThrowArgumentNullException()
        {
            // Arrange
            var zone = new Zone("zone1", "Downtown", new Vector3(100f, 200f, 30f), 100f, 10);
            zone.OwnerFactionId = "defender";

            // Act & Assert
            Assert.Throws<System.ArgumentNullException>(() => _manager.StartCombat(zone, null!));
        }

        [Fact]
        public void StartCombat_WithNoOwner_ShouldThrowArgumentException()
        {
            // Arrange - neutral zone with no owner
            var zone = new Zone("zone1", "Downtown", new Vector3(100f, 200f, 30f), 100f, 10);

            // Act & Assert
            Assert.Throws<System.ArgumentException>(() => _manager.StartCombat(zone, "attacker"));
        }

        [Fact]
        public void StartCombat_WithAttackerSameAsDefender_ShouldThrowArgumentException()
        {
            // Arrange
            var zone = new Zone("zone1", "Downtown", new Vector3(100f, 200f, 30f), 100f, 10);
            zone.OwnerFactionId = "faction1";

            // Act & Assert
            Assert.Throws<System.ArgumentException>(() => _manager.StartCombat(zone, "faction1"));
        }

        [Fact]
        public void StartCombat_ShouldRaiseCombatStartedEvent()
        {
            // Arrange
            var zone = new Zone("zone1", "Downtown", new Vector3(100f, 200f, 30f), 100f, 10);
            zone.OwnerFactionId = "defender";
            CombatEncounter? raisedEncounter = null;
            _manager.CombatStarted += (sender, e) => raisedEncounter = e;

            // Act
            var encounter = _manager.StartCombat(zone, "attacker");

            // Assert
            Assert.Same(encounter, raisedEncounter);
        }

        [Fact]
        public void EndCombat_WithAttackerVictory_ShouldEndEncounterAndProcessResult()
        {
            // Arrange
            var zone = new Zone("zone1", "Downtown", new Vector3(100f, 200f, 30f), 100f, 10);
            zone.OwnerFactionId = "defender";
            _manager.StartCombat(zone, "attacker");

            _combatResultHandlerMock.Setup(h => h.ProcessCombatResult(It.IsAny<CombatEncounter>()))
                .Returns(CombatProcessingResult.Success(CombatResultOutcome.ZoneCaptured, "zone1", "attacker", "defender"));

            // Act
            _manager.EndCombat(CombatStatus.AttackerVictory);

            // Assert
            Assert.Null(_manager.CurrentEncounter);
            Assert.False(_manager.IsInCombat);
            _combatResultHandlerMock.Verify(h => h.ProcessCombatResult(It.IsAny<CombatEncounter>()), Times.Once);
        }

        [Fact]
        public void EndCombat_WhenNotInCombat_ShouldDoNothing()
        {
            // Act - should not throw
            _manager.EndCombat(CombatStatus.Aborted);

            // Assert
            Assert.Null(_manager.CurrentEncounter);
            _combatResultHandlerMock.Verify(h => h.ProcessCombatResult(It.IsAny<CombatEncounter>()), Times.Never);
        }

        [Fact]
        public void EndCombat_ShouldRaiseCombatEndedEvent()
        {
            // Arrange
            var zone = new Zone("zone1", "Downtown", new Vector3(100f, 200f, 30f), 100f, 10);
            zone.OwnerFactionId = "defender";
            var encounter = _manager.StartCombat(zone, "attacker");

            CombatEncounter? raisedEncounter = null;
            _manager.CombatEnded += (sender, e) => raisedEncounter = e;

            _combatResultHandlerMock.Setup(h => h.ProcessCombatResult(It.IsAny<CombatEncounter>()))
                .Returns(CombatProcessingResult.Success(CombatResultOutcome.ZoneDefended, "zone1", "defender", "attacker"));

            // Act
            _manager.EndCombat(CombatStatus.DefenderVictory);

            // Assert
            Assert.NotNull(raisedEncounter);
            Assert.Equal(encounter.Id, raisedEncounter.Id);
        }

        [Fact]
        public void Update_WhenNotInCombat_ShouldDoNothing()
        {
            // Act - should not throw
            _manager.Update();

            // Assert
            _pedPoolMock.Verify(p => p.GetByFactionAndZone(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void Update_WhenInCombat_ShouldUpdatePedCounts()
        {
            // Arrange
            var zone = new Zone("zone1", "Downtown", new Vector3(100f, 200f, 30f), 100f, 10);
            zone.OwnerFactionId = "defender";
            var encounter = _manager.StartCombat(zone, "attacker");

            var attackerPeds = new[] { new PedHandle(1, "attacker", default, "model", "zone1") };
            var defenderPeds = new[] { new PedHandle(2, "defender", default, "model", "zone1"), new PedHandle(3, "defender", default, "model", "zone1") };

            _pedPoolMock.Setup(p => p.GetByFactionAndZone("attacker", "zone1")).Returns(attackerPeds);
            _pedPoolMock.Setup(p => p.GetByFactionAndZone("defender", "zone1")).Returns(defenderPeds);

            _controlCalculatorMock.Setup(c => c.Calculate(1, 2))
                .Returns(new ControlPercentageResult(33.3f, 66.7f, 3));

            _takeoverDetectorMock.Setup(t => t.CheckTakeover(33.3f, 66.7f, "attacker", "defender"))
                .Returns(TakeoverResult.InProgress(33.3f, 66.7f));

            // Act
            _manager.Update();

            // Assert
            Assert.Equal(1, encounter.AttackerPedCount);
            Assert.Equal(2, encounter.DefenderPedCount);
        }

        [Fact]
        public void Update_WhenInCombat_ShouldUpdateControlPercentages()
        {
            // Arrange
            var zone = new Zone("zone1", "Downtown", new Vector3(100f, 200f, 30f), 100f, 10);
            zone.OwnerFactionId = "defender";
            var encounter = _manager.StartCombat(zone, "attacker");

            var attackerPeds = new[] { new PedHandle(1, "attacker", default, "model", "zone1"), new PedHandle(2, "attacker", default, "model", "zone1") };
            var defenderPeds = new[] { new PedHandle(3, "defender", default, "model", "zone1") };

            _pedPoolMock.Setup(p => p.GetByFactionAndZone("attacker", "zone1")).Returns(attackerPeds);
            _pedPoolMock.Setup(p => p.GetByFactionAndZone("defender", "zone1")).Returns(defenderPeds);

            _controlCalculatorMock.Setup(c => c.Calculate(2, 1))
                .Returns(new ControlPercentageResult(66.7f, 33.3f, 3));

            _takeoverDetectorMock.Setup(t => t.CheckTakeover(66.7f, 33.3f, "attacker", "defender"))
                .Returns(TakeoverResult.InProgress(66.7f, 33.3f));

            // Act
            _manager.Update();

            // Assert
            Assert.Equal(66.7f, encounter.AttackerControlPercentage);
            Assert.Equal(33.3f, encounter.DefenderControlPercentage);
        }

        [Fact]
        public void Update_WhenAttackerVictoryDetected_ShouldEndCombat()
        {
            // Arrange
            var zone = new Zone("zone1", "Downtown", new Vector3(100f, 200f, 30f), 100f, 10);
            zone.OwnerFactionId = "defender";
            _manager.StartCombat(zone, "attacker");

            var attackerPeds = new[] { new PedHandle(1, "attacker", default, "model", "zone1") };
            var defenderPeds = System.Array.Empty<PedHandle>();

            _pedPoolMock.Setup(p => p.GetByFactionAndZone("attacker", "zone1")).Returns(attackerPeds);
            _pedPoolMock.Setup(p => p.GetByFactionAndZone("defender", "zone1")).Returns(defenderPeds);

            _controlCalculatorMock.Setup(c => c.Calculate(1, 0))
                .Returns(new ControlPercentageResult(100f, 0f, 1));

            _takeoverDetectorMock.Setup(t => t.CheckTakeover(100f, 0f, "attacker", "defender"))
                .Returns(TakeoverResult.AttackerVictory("attacker", 100f, 0f));

            _combatResultHandlerMock.Setup(h => h.ProcessCombatResult(It.IsAny<CombatEncounter>()))
                .Returns(CombatProcessingResult.Success(CombatResultOutcome.ZoneCaptured, "zone1", "attacker", "defender"));

            // Act
            _manager.Update();

            // Assert
            Assert.False(_manager.IsInCombat);
            _combatResultHandlerMock.Verify(h => h.ProcessCombatResult(It.Is<CombatEncounter>(
                e => e.Status == CombatStatus.AttackerVictory)), Times.Once);
        }

        [Fact]
        public void Update_WhenDefenderVictoryDetected_ShouldEndCombat()
        {
            // Arrange
            var zone = new Zone("zone1", "Downtown", new Vector3(100f, 200f, 30f), 100f, 10);
            zone.OwnerFactionId = "defender";
            _manager.StartCombat(zone, "attacker");

            var attackerPeds = System.Array.Empty<PedHandle>();
            var defenderPeds = new[] { new PedHandle(1, "defender", default, "model", "zone1") };

            _pedPoolMock.Setup(p => p.GetByFactionAndZone("attacker", "zone1")).Returns(attackerPeds);
            _pedPoolMock.Setup(p => p.GetByFactionAndZone("defender", "zone1")).Returns(defenderPeds);

            _controlCalculatorMock.Setup(c => c.Calculate(0, 1))
                .Returns(new ControlPercentageResult(0f, 100f, 1));

            _takeoverDetectorMock.Setup(t => t.CheckTakeover(0f, 100f, "attacker", "defender"))
                .Returns(TakeoverResult.DefenderVictory("defender", 0f, 100f));

            _combatResultHandlerMock.Setup(h => h.ProcessCombatResult(It.IsAny<CombatEncounter>()))
                .Returns(CombatProcessingResult.Success(CombatResultOutcome.ZoneDefended, "zone1", "defender", "attacker"));

            // Act
            _manager.Update();

            // Assert
            Assert.False(_manager.IsInCombat);
            _combatResultHandlerMock.Verify(h => h.ProcessCombatResult(It.Is<CombatEncounter>(
                e => e.Status == CombatStatus.DefenderVictory)), Times.Once);
        }

        [Fact]
        public void AbortCombat_WhenInCombat_ShouldEndWithAbortedStatus()
        {
            // Arrange
            var zone = new Zone("zone1", "Downtown", new Vector3(100f, 200f, 30f), 100f, 10);
            zone.OwnerFactionId = "defender";
            var encounter = _manager.StartCombat(zone, "attacker");

            // Act
            _manager.AbortCombat();

            // Assert
            Assert.Null(_manager.CurrentEncounter);
            Assert.False(_manager.IsInCombat);
            Assert.Equal(CombatStatus.Aborted, encounter.Status);
        }

        [Fact]
        public void AbortCombat_ShouldNotProcessCombatResult()
        {
            // Arrange
            var zone = new Zone("zone1", "Downtown", new Vector3(100f, 200f, 30f), 100f, 10);
            zone.OwnerFactionId = "defender";
            _manager.StartCombat(zone, "attacker");

            // Act
            _manager.AbortCombat();

            // Assert - combat result handler should NOT be called for aborted combat
            _combatResultHandlerMock.Verify(h => h.ProcessCombatResult(It.IsAny<CombatEncounter>()), Times.Never);
        }

        [Fact]
        public void AbortCombat_WhenNotInCombat_ShouldDoNothing()
        {
            // Act - should not throw
            _manager.AbortCombat();

            // Assert
            Assert.False(_manager.IsInCombat);
        }

        [Fact]
        public void SpawnDefenders_WhenNotInCombat_ShouldThrowInvalidOperationException()
        {
            // Act & Assert
            Assert.Throws<System.InvalidOperationException>(
                () => _manager.SpawnDefenders("model", "faction", 3));
        }

        [Fact]
        public void SpawnDefenders_WhenInCombat_ShouldSpawnPedsAtNaturalPositions()
        {
            // Arrange
            var zone = new Zone("zone1", "Downtown", new Vector3(100f, 200f, 30f), 100f, 10);
            zone.OwnerFactionId = "defender";
            _manager.StartCombat(zone, "attacker");

            var spawnPositions = new List<Vector3>
            {
                new Vector3(80f, 180f, 30f),
                new Vector3(82f, 178f, 30f),
                new Vector3(84f, 176f, 30f)
            };

            _spawnPositionCalculatorMock.Setup(c => c.CalculateNaturalSpawnPositions(3))
                .Returns(spawnPositions);
            _pedSpawningServiceMock.Setup(s => s.CanSpawn()).Returns(true);
            _pedSpawningServiceMock.Setup(s => s.SpawnPed(It.IsAny<string>(), It.IsAny<Vector3>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns<string, Vector3, string, string>((model, pos, faction, zoneId) =>
                    new PedHandle(1, faction, pos, model, zoneId));

            // Act
            var result = _manager.SpawnDefenders("model", "defender", 3);

            // Assert
            Assert.Equal(3, result.Count);
            _spawnPositionCalculatorMock.Verify(c => c.CalculateNaturalSpawnPositions(3), Times.Once);
            _pedSpawningServiceMock.Verify(s => s.SpawnPed("model", It.IsAny<Vector3>(), "defender", "zone1"), Times.Exactly(3));
        }

        [Fact]
        public void SpawnDefenders_WhenPoolCannotSpawn_ShouldReturnEmptyList()
        {
            // Arrange
            var zone = new Zone("zone1", "Downtown", new Vector3(100f, 200f, 30f), 100f, 10);
            zone.OwnerFactionId = "defender";
            _manager.StartCombat(zone, "attacker");
            _pedSpawningServiceMock.Setup(s => s.CanSpawn()).Returns(false);

            // Act
            var result = _manager.SpawnDefenders("model", "defender", 3);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void SpawnDefender_WhenNotInCombat_ShouldThrowInvalidOperationException()
        {
            // Act & Assert
            Assert.Throws<System.InvalidOperationException>(
                () => _manager.SpawnDefender("model", "faction"));
        }

        [Fact]
        public void SpawnDefender_WhenInCombat_ShouldSpawnSinglePedAtNaturalPosition()
        {
            // Arrange
            var zone = new Zone("zone1", "Downtown", new Vector3(100f, 200f, 30f), 100f, 10);
            zone.OwnerFactionId = "defender";
            _manager.StartCombat(zone, "attacker");

            var spawnPosition = new Vector3(80f, 180f, 30f);
            _spawnPositionCalculatorMock.Setup(c => c.CalculateNaturalSpawnPosition())
                .Returns(spawnPosition);
            _pedSpawningServiceMock.Setup(s => s.CanSpawn()).Returns(true);
            _pedSpawningServiceMock.Setup(s => s.SpawnPed("model", spawnPosition, "defender", "zone1"))
                .Returns(new PedHandle(1, "defender", spawnPosition, "model", "zone1"));

            // Act
            var result = _manager.SpawnDefender("model", "defender");

            // Assert
            Assert.True(result.IsValid);
            _spawnPositionCalculatorMock.Verify(c => c.CalculateNaturalSpawnPosition(), Times.Once);
            _pedSpawningServiceMock.Verify(s => s.SpawnPed("model", spawnPosition, "defender", "zone1"), Times.Once);
        }

        [Fact]
        public void CanSpawnDefenders_ShouldDelegateToSpawningService()
        {
            // Arrange
            _pedSpawningServiceMock.Setup(s => s.CanSpawn()).Returns(true);

            // Act
            var result = _manager.CanSpawnDefenders();

            // Assert
            Assert.True(result);
            _pedSpawningServiceMock.Verify(s => s.CanSpawn(), Times.Once);
        }

        [Fact]
        public void CanSpawnDefendersCount_ShouldDelegateToSpawningService()
        {
            // Arrange
            _pedSpawningServiceMock.Setup(s => s.CanSpawnCount()).Returns(10);

            // Act
            var result = _manager.CanSpawnDefendersCount();

            // Assert
            Assert.Equal(10, result);
            _pedSpawningServiceMock.Verify(s => s.CanSpawnCount(), Times.Once);
        }

        #region Wave-Based Spawning Tests

        [Fact]
        public void CurrentWaveState_Initially_ShouldBeNull()
        {
            // Assert
            Assert.Null(_manager.CurrentWaveState);
        }

        [Fact]
        public void InitializeWaveSpawning_WhenNotInCombat_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var plan = new DefenderSpawnPlan(basicPeds: 3, mediumPeds: 2, heavyPeds: 1);

            // Act & Assert
            Assert.Throws<System.InvalidOperationException>(() => _manager.InitializeWaveSpawning(plan));
        }

        [Fact]
        public void InitializeWaveSpawning_WithNullPlan_ShouldThrowArgumentNullException()
        {
            // Arrange
            var zone = new Zone("zone1", "Downtown", new Vector3(100f, 200f, 30f), 100f, 10);
            zone.OwnerFactionId = "defender";
            _manager.StartCombat(zone, "attacker");

            // Act & Assert
            Assert.Throws<System.ArgumentNullException>(() => _manager.InitializeWaveSpawning(null!));
        }

        [Fact]
        public void InitializeWaveSpawning_WithValidPlan_ShouldCreateWaveState()
        {
            // Arrange
            var zone = new Zone("zone1", "Downtown", new Vector3(100f, 200f, 30f), 100f, 10);
            zone.OwnerFactionId = "defender";
            _manager.StartCombat(zone, "attacker");
            var plan = new DefenderSpawnPlan(basicPeds: 3, mediumPeds: 2, heavyPeds: 1);

            // Act
            _manager.InitializeWaveSpawning(plan);

            // Assert
            Assert.NotNull(_manager.CurrentWaveState);
            Assert.Equal(6, _manager.CurrentWaveState.TotalRemaining);
        }

        [Fact]
        public void GetNextWaveTier_WhenNotInitialized_ShouldReturnNull()
        {
            // Arrange
            var zone = new Zone("zone1", "Downtown", new Vector3(100f, 200f, 30f), 100f, 10);
            zone.OwnerFactionId = "defender";
            _manager.StartCombat(zone, "attacker");

            // Act
            var tier = _manager.GetNextWaveTier();

            // Assert
            Assert.Null(tier);
        }

        [Fact]
        public void GetNextWaveTier_ShouldReturnHeavyFirst()
        {
            // Arrange
            var zone = new Zone("zone1", "Downtown", new Vector3(100f, 200f, 30f), 100f, 10);
            zone.OwnerFactionId = "defender";
            _manager.StartCombat(zone, "attacker");
            var plan = new DefenderSpawnPlan(basicPeds: 3, mediumPeds: 2, heavyPeds: 1);
            _manager.InitializeWaveSpawning(plan);

            // Act
            var tier = _manager.GetNextWaveTier();

            // Assert
            Assert.Equal(DefenderTier.Heavy, tier);
        }

        [Fact]
        public void SpawnNextWave_WhenNotInCombat_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var models = new Dictionary<DefenderTier, string>
            {
                { DefenderTier.Heavy, "heavy_model" },
                { DefenderTier.Medium, "medium_model" },
                { DefenderTier.Basic, "basic_model" }
            };

            // Act & Assert
            Assert.Throws<System.InvalidOperationException>(() => _manager.SpawnNextWave(models, "defender", 5));
        }

        [Fact]
        public void SpawnNextWave_WhenNotInitialized_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var zone = new Zone("zone1", "Downtown", new Vector3(100f, 200f, 30f), 100f, 10);
            zone.OwnerFactionId = "defender";
            _manager.StartCombat(zone, "attacker");
            var models = new Dictionary<DefenderTier, string>
            {
                { DefenderTier.Heavy, "heavy_model" }
            };

            // Act & Assert
            Assert.Throws<System.InvalidOperationException>(() => _manager.SpawnNextWave(models, "defender", 5));
        }

        [Fact]
        public void SpawnNextWave_ShouldSpawnHeavyTierFirst()
        {
            // Arrange
            var zone = new Zone("zone1", "Downtown", new Vector3(100f, 200f, 30f), 100f, 10);
            zone.OwnerFactionId = "defender";
            _manager.StartCombat(zone, "attacker");

            var plan = new DefenderSpawnPlan(basicPeds: 3, mediumPeds: 2, heavyPeds: 2);
            _manager.InitializeWaveSpawning(plan);

            var models = new Dictionary<DefenderTier, string>
            {
                { DefenderTier.Heavy, "heavy_model" },
                { DefenderTier.Medium, "medium_model" },
                { DefenderTier.Basic, "basic_model" }
            };

            var spawnPositions = new List<Vector3>
            {
                new Vector3(80f, 180f, 30f),
                new Vector3(82f, 178f, 30f)
            };

            _spawnPositionCalculatorMock.Setup(c => c.CalculateNaturalSpawnPositions(2))
                .Returns(spawnPositions);
            _pedSpawningServiceMock.Setup(s => s.CanSpawn()).Returns(true);
            _pedSpawningServiceMock.Setup(s => s.SpawnPed("heavy_model", It.IsAny<Vector3>(), "defender", "zone1"))
                .Returns<string, Vector3, string, string>((model, pos, faction, zoneId) =>
                    new PedHandle(1, faction, pos, model, zoneId));

            // Act
            var result = _manager.SpawnNextWave(models, "defender", 10);

            // Assert
            Assert.Equal(2, result.Count);
            _pedSpawningServiceMock.Verify(s => s.SpawnPed("heavy_model", It.IsAny<Vector3>(), "defender", "zone1"), Times.Exactly(2));
        }

        [Fact]
        public void SpawnNextWave_AfterHeavyComplete_ShouldSpawnMedium()
        {
            // Arrange
            var zone = new Zone("zone1", "Downtown", new Vector3(100f, 200f, 30f), 100f, 10);
            zone.OwnerFactionId = "defender";
            _manager.StartCombat(zone, "attacker");

            var plan = new DefenderSpawnPlan(basicPeds: 3, mediumPeds: 2, heavyPeds: 1);
            _manager.InitializeWaveSpawning(plan);

            var models = new Dictionary<DefenderTier, string>
            {
                { DefenderTier.Heavy, "heavy_model" },
                { DefenderTier.Medium, "medium_model" },
                { DefenderTier.Basic, "basic_model" }
            };

            // Setup for heavy spawn
            _spawnPositionCalculatorMock.Setup(c => c.CalculateNaturalSpawnPositions(1))
                .Returns(new List<Vector3> { new Vector3(80f, 180f, 30f) });
            _pedSpawningServiceMock.Setup(s => s.CanSpawn()).Returns(true);
            _pedSpawningServiceMock.Setup(s => s.SpawnPed(It.IsAny<string>(), It.IsAny<Vector3>(), "defender", "zone1"))
                .Returns<string, Vector3, string, string>((model, pos, faction, zoneId) =>
                    new PedHandle(1, faction, pos, model, zoneId));

            // Spawn heavy wave
            _manager.SpawnNextWave(models, "defender", 10);

            // Verify heavy is complete and medium is next
            Assert.Equal(DefenderTier.Medium, _manager.GetNextWaveTier());

            // Setup for medium spawn
            _spawnPositionCalculatorMock.Setup(c => c.CalculateNaturalSpawnPositions(2))
                .Returns(new List<Vector3> { new Vector3(80f, 180f, 30f), new Vector3(82f, 178f, 30f) });

            // Act - spawn next wave (should be Medium)
            var result = _manager.SpawnNextWave(models, "defender", 10);

            // Assert - medium peds spawned
            Assert.Equal(2, result.Count);
            _pedSpawningServiceMock.Verify(s => s.SpawnPed("medium_model", It.IsAny<Vector3>(), "defender", "zone1"), Times.Exactly(2));
            // After spawning medium, next should be Basic
            Assert.Equal(DefenderTier.Basic, _manager.GetNextWaveTier());
        }

        [Fact]
        public void IsWaveSpawningComplete_WhenNotInitialized_ShouldReturnTrue()
        {
            // Arrange
            var zone = new Zone("zone1", "Downtown", new Vector3(100f, 200f, 30f), 100f, 10);
            zone.OwnerFactionId = "defender";
            _manager.StartCombat(zone, "attacker");

            // Act
            var complete = _manager.IsWaveSpawningComplete();

            // Assert
            Assert.True(complete);
        }

        [Fact]
        public void IsWaveSpawningComplete_WhenInitialized_ShouldReturnFalse()
        {
            // Arrange
            var zone = new Zone("zone1", "Downtown", new Vector3(100f, 200f, 30f), 100f, 10);
            zone.OwnerFactionId = "defender";
            _manager.StartCombat(zone, "attacker");
            var plan = new DefenderSpawnPlan(basicPeds: 1, mediumPeds: 1, heavyPeds: 1);
            _manager.InitializeWaveSpawning(plan);

            // Act
            var complete = _manager.IsWaveSpawningComplete();

            // Assert
            Assert.False(complete);
        }

        [Fact]
        public void GetRemainingDefendersToSpawn_ShouldReturnTotalRemaining()
        {
            // Arrange
            var zone = new Zone("zone1", "Downtown", new Vector3(100f, 200f, 30f), 100f, 10);
            zone.OwnerFactionId = "defender";
            _manager.StartCombat(zone, "attacker");
            var plan = new DefenderSpawnPlan(basicPeds: 5, mediumPeds: 3, heavyPeds: 2);
            _manager.InitializeWaveSpawning(plan);

            // Act
            var remaining = _manager.GetRemainingDefendersToSpawn();

            // Assert
            Assert.Equal(10, remaining);
        }

        [Fact]
        public void GetWaveSpawnOrder_ShouldReturnHeavyMediumBasic()
        {
            // Act
            var order = _manager.GetWaveSpawnOrder();

            // Assert
            Assert.Equal(3, order.Count);
            Assert.Equal(DefenderTier.Heavy, order[0]);
            Assert.Equal(DefenderTier.Medium, order[1]);
            Assert.Equal(DefenderTier.Basic, order[2]);
        }

        [Fact]
        public void EndCombat_ShouldClearWaveState()
        {
            // Arrange
            var zone = new Zone("zone1", "Downtown", new Vector3(100f, 200f, 30f), 100f, 10);
            zone.OwnerFactionId = "defender";
            _manager.StartCombat(zone, "attacker");
            var plan = new DefenderSpawnPlan(basicPeds: 3, mediumPeds: 2, heavyPeds: 1);
            _manager.InitializeWaveSpawning(plan);

            _combatResultHandlerMock.Setup(h => h.ProcessCombatResult(It.IsAny<CombatEncounter>()))
                .Returns(CombatProcessingResult.Success(CombatResultOutcome.ZoneCaptured, "zone1", "attacker", "defender"));

            // Act
            _manager.EndCombat(CombatStatus.AttackerVictory);

            // Assert
            Assert.Null(_manager.CurrentWaveState);
        }

        [Fact]
        public void AbortCombat_ShouldClearWaveState()
        {
            // Arrange
            var zone = new Zone("zone1", "Downtown", new Vector3(100f, 200f, 30f), 100f, 10);
            zone.OwnerFactionId = "defender";
            _manager.StartCombat(zone, "attacker");
            var plan = new DefenderSpawnPlan(basicPeds: 3, mediumPeds: 2, heavyPeds: 1);
            _manager.InitializeWaveSpawning(plan);

            // Act
            _manager.AbortCombat();

            // Assert
            Assert.Null(_manager.CurrentWaveState);
        }

        #endregion

        #region Player Death Handling Tests

        [Fact]
        public void Update_WhenPlayerDies_ShouldEndCombatWithPlayerRetreat()
        {
            // Arrange
            var zone = new Zone("zone1", "Downtown", new Vector3(100f, 200f, 30f), 100f, 10);
            zone.OwnerFactionId = "defender";
            _manager.StartCombat(zone, "attacker");

            // Simulate player is dead
            _gameBridgeMock.Setup(g => g.IsPlayerDead()).Returns(true);

            // Act
            _manager.Update();

            // Assert - combat should be ended with PlayerRetreat status
            Assert.False(_manager.IsInCombat);
        }

        [Fact]
        public void Update_WhenPlayerDies_ShouldNotProcessCombatResult()
        {
            // Arrange
            var zone = new Zone("zone1", "Downtown", new Vector3(100f, 200f, 30f), 100f, 10);
            zone.OwnerFactionId = "defender";
            _manager.StartCombat(zone, "attacker");

            _gameBridgeMock.Setup(g => g.IsPlayerDead()).Returns(true);

            // Act
            _manager.Update();

            // Assert - combat result should NOT be processed on player retreat
            _combatResultHandlerMock.Verify(h => h.ProcessCombatResult(It.IsAny<CombatEncounter>()), Times.Never);
        }

        [Fact]
        public void Update_WhenPlayerDies_ShouldSetEncounterStatusToPlayerRetreat()
        {
            // Arrange
            var zone = new Zone("zone1", "Downtown", new Vector3(100f, 200f, 30f), 100f, 10);
            zone.OwnerFactionId = "defender";
            var encounter = _manager.StartCombat(zone, "attacker");

            CombatEncounter? endedEncounter = null;
            _manager.CombatEnded += (sender, e) => endedEncounter = e;

            _gameBridgeMock.Setup(g => g.IsPlayerDead()).Returns(true);

            // Act
            _manager.Update();

            // Assert
            Assert.NotNull(endedEncounter);
            Assert.Equal(CombatStatus.PlayerRetreat, endedEncounter.Status);
        }

        [Fact]
        public void Update_WhenPlayerDies_ShouldClearWaveState()
        {
            // Arrange
            var zone = new Zone("zone1", "Downtown", new Vector3(100f, 200f, 30f), 100f, 10);
            zone.OwnerFactionId = "defender";
            _manager.StartCombat(zone, "attacker");
            var plan = new DefenderSpawnPlan(basicPeds: 3, mediumPeds: 2, heavyPeds: 1);
            _manager.InitializeWaveSpawning(plan);

            _gameBridgeMock.Setup(g => g.IsPlayerDead()).Returns(true);

            // Act
            _manager.Update();

            // Assert
            Assert.Null(_manager.CurrentWaveState);
        }

        [Fact]
        public void Update_WhenPlayerAlive_ShouldContinueNormalCombatUpdate()
        {
            // Arrange
            var zone = new Zone("zone1", "Downtown", new Vector3(100f, 200f, 30f), 100f, 10);
            zone.OwnerFactionId = "defender";
            var encounter = _manager.StartCombat(zone, "attacker");

            _gameBridgeMock.Setup(g => g.IsPlayerDead()).Returns(false);

            var attackerPeds = new[] { new PedHandle(1, "attacker", default, "model", "zone1") };
            var defenderPeds = new[] { new PedHandle(2, "defender", default, "model", "zone1") };

            _pedPoolMock.Setup(p => p.GetByFactionAndZone("attacker", "zone1")).Returns(attackerPeds);
            _pedPoolMock.Setup(p => p.GetByFactionAndZone("defender", "zone1")).Returns(defenderPeds);

            _controlCalculatorMock.Setup(c => c.Calculate(1, 1))
                .Returns(new ControlPercentageResult(50f, 50f, 2));

            _takeoverDetectorMock.Setup(t => t.CheckTakeover(50f, 50f, "attacker", "defender"))
                .Returns(TakeoverResult.InProgress(50f, 50f));

            // Act
            _manager.Update();

            // Assert - combat should continue
            Assert.True(_manager.IsInCombat);
            Assert.Equal(50f, encounter.AttackerControlPercentage);
            Assert.Equal(50f, encounter.DefenderControlPercentage);
        }

        [Fact]
        public void Retreat_WhenInCombat_ShouldEndCombatWithPlayerRetreatStatus()
        {
            // Arrange
            var zone = new Zone("zone1", "Downtown", new Vector3(100f, 200f, 30f), 100f, 10);
            zone.OwnerFactionId = "defender";
            var encounter = _manager.StartCombat(zone, "attacker");

            // Act
            _manager.Retreat();

            // Assert
            Assert.Null(_manager.CurrentEncounter);
            Assert.False(_manager.IsInCombat);
            Assert.Equal(CombatStatus.PlayerRetreat, encounter.Status);
        }

        [Fact]
        public void Retreat_ShouldNotProcessCombatResult()
        {
            // Arrange
            var zone = new Zone("zone1", "Downtown", new Vector3(100f, 200f, 30f), 100f, 10);
            zone.OwnerFactionId = "defender";
            _manager.StartCombat(zone, "attacker");

            // Act
            _manager.Retreat();

            // Assert - combat result handler should NOT be called for retreat
            _combatResultHandlerMock.Verify(h => h.ProcessCombatResult(It.IsAny<CombatEncounter>()), Times.Never);
        }

        [Fact]
        public void Retreat_WhenNotInCombat_ShouldDoNothing()
        {
            // Act - should not throw
            _manager.Retreat();

            // Assert
            Assert.False(_manager.IsInCombat);
        }

        [Fact]
        public void Retreat_ShouldRaiseCombatEndedEvent()
        {
            // Arrange
            var zone = new Zone("zone1", "Downtown", new Vector3(100f, 200f, 30f), 100f, 10);
            zone.OwnerFactionId = "defender";
            var encounter = _manager.StartCombat(zone, "attacker");

            CombatEncounter? raisedEncounter = null;
            _manager.CombatEnded += (sender, e) => raisedEncounter = e;

            // Act
            _manager.Retreat();

            // Assert
            Assert.NotNull(raisedEncounter);
            Assert.Equal(encounter.Id, raisedEncounter.Id);
            Assert.Equal(CombatStatus.PlayerRetreat, raisedEncounter.Status);
        }

        #endregion
    }
}
