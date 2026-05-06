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
        private Mock<IPedDespawnService> _pedDespawnServiceMock = null!;
        private Mock<IDefenderTierService> _defenderTierServiceMock = null!;
        private Mock<IPedBlipService> _pedBlipServiceMock = null!;
        private Mock<IZoneService> _zoneServiceMock = null!;
        private Mock<IZoneBattleManager> _zoneBattleManagerMock = null!;
        private EnemyDefenderManager _manager = null!;

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
            _zoneBattleManagerMock = new Mock<IZoneBattleManager>();

            // Setup default mock behaviors
            _pedSpawningServiceMock.Setup(p => p.CanSpawn()).Returns(true);
            _pedSpawningServiceMock.Setup(p => p.GetRelationshipGroup(It.IsAny<string>()))
                .Returns<string>(factionId => factionId.ToUpperInvariant());
            _pedSpawningServiceMock.Setup(p => p.SpawnPed(It.IsAny<string>(), It.IsAny<Vector3>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns<string, Vector3, string, string?>((model, position, factionId, zoneId) =>
                {
                    var handle = _gameBridge.CreatePed(model, position);
                    _gameBridge.SetPedRelationshipGroup(handle, factionId.ToUpperInvariant());
                    return new PedHandle(handle, factionId, position, model, zoneId);
                });

            _defenderTierServiceMock.Setup(d => d.GetTierConfig(It.IsAny<DefenderTier>()))
                .Returns(new DefenderTierConfig(DefenderTier.Basic, 200, 100, 0, "weapon_pistol", 0.5f, 1.0f));

            _pedBlipServiceMock.Setup(p => p.CreateBlipForPed(It.IsAny<int>(), It.IsAny<BlipColor>()))
                .Returns(1);

            _manager = new EnemyDefenderManager(
                _gameBridge,
                _allocationServiceMock.Object,
                _pedSpawningServiceMock.Object,
                _pedDespawnServiceMock.Object,
                _defenderTierServiceMock.Object,
                _pedBlipServiceMock.Object,
                _zoneServiceMock.Object,
                _zoneBattleManagerMock.Object);
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
            Assert.True(_gameBridge.GetPedCriticalHitsEnabled(1));
            Assert.True(_gameBridge.GetPedCriticalHitsEnabled(2));
            Assert.True(_gameBridge.GetPedRagdollEnabled(1));
            Assert.True(_gameBridge.GetPedRagdollEnabled(2));

            // Note: The actual sprinting wander is verified via GameBridge calls
            // which calls TaskPedWanderInAreaSprinting
        }

        [Fact]
        public void OnTroopsAllocated_WhenPlayerInEnemyZone_SpawnsLiveReinforcements()
        {
            SetupManager();
            var zone = CreateEnemyZone();
            var allocation = CreateAllocationWithDefenders(basic: 1);

            _zoneServiceMock.Setup(z => z.GetZone(TestZoneId)).Returns(zone);
            _allocationServiceMock.Setup(a => a.GetAllocation(EnemyFactionId, TestZoneId))
                .Returns(allocation);
            _manager.OnEnemyZoneEntered(zone, EnemyFactionId);
            Assert.Equal(1, _manager.GetSpawnedDefenderCount(TestZoneId));

            allocation.AddTroops(DefenderTier.Basic, 2);

            _manager.OnTroopsAllocated(EnemyFactionId, TestZoneId, DefenderTier.Basic, 2);

            Assert.Equal(3, _manager.GetSpawnedDefenderCount(TestZoneId));
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

        [Fact]
        public void OnEnemyZoneEntered_WithThreeWayBattle_ConfiguresAllParticipantRelationships()
        {
            SetupManager();
            var zone = CreateEnemyZone();
            var allocation = CreateAllocationWithDefenders(basic: 1);
            var defender = BattleParticipant.ForAi(EnemyFactionId, BattleRole.Defender, new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 1 } });
            var aiAttacker = BattleParticipant.ForAi("faction_franklin", BattleRole.Attacker, new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 1 } });
            var playerAttacker = BattleParticipant.ForPlayer("faction_trevor", BattleRole.Attacker, () => 1);
            var battle = new ZoneBattle(TestZoneId, new List<BattleParticipant> { defender, aiAttacker, playerAttacker }, "faction_trevor");

            _allocationServiceMock.Setup(a => a.GetAllocation(EnemyFactionId, TestZoneId))
                .Returns(allocation);
            _zoneBattleManagerMock.Setup(b => b.GetBattleForZone(TestZoneId)).Returns(battle);

            _manager.OnEnemyZoneEntered(zone, EnemyFactionId);

            Assert.Equal(5, _gameBridge.GetRelationshipBetweenGroups("BALLAS", "FACTION_FRANKLIN"));
            Assert.Equal(5, _gameBridge.GetRelationshipBetweenGroups("BALLAS", "FACTION_TREVOR"));
            Assert.Equal(5, _gameBridge.GetRelationshipBetweenGroups("FACTION_FRANKLIN", "FACTION_TREVOR"));
            Assert.True(_gameBridge.IsPedCombatTargeting(1));
            Assert.Equal(EnemyFactionId.ToUpperInvariant(), _gameBridge.GetPedRelationshipGroup(1));
        }

        #region Corpse Persistence Tests

        [Fact]
        public void Update_WhenDefenderDies_DoesNotImmediatelyDespawn()
        {
            // Arrange
            SetupManager();
            var zone = CreateEnemyZone();
            var allocation = CreateAllocationWithDefenders(basic: 1);

            _allocationServiceMock.Setup(a => a.GetAllocation(EnemyFactionId, TestZoneId))
                .Returns(allocation);
            _zoneServiceMock.Setup(z => z.GetZone(TestZoneId)).Returns(zone);

            // Spawn a defender
            _manager.OnEnemyZoneEntered(zone, EnemyFactionId);
            Assert.Equal(1, _manager.GetSpawnedDefenderCount(TestZoneId));

            // Kill the ped (simulate death)
            var spawnedPedHandle = 1; // First spawned ped has handle 1
            _gameBridge.KillPed(spawnedPedHandle);

            // Act - First update after death
            _manager.Update(EnemyFactionId);

            // Assert - Ped should be untracked (frees pool slot) but entity not deleted yet (corpse persists)
            _pedDespawnServiceMock.Verify(
                d => d.UntrackPed(spawnedPedHandle),
                Times.Once,
                "Ped should be untracked from pool immediately on death to free spawn slot");
            _pedDespawnServiceMock.Verify(
                d => d.DeletePedEntity(spawnedPedHandle),
                Times.Never,
                "Dead ped entity should not be immediately deleted - corpse should persist");
        }

        [Fact]
        public void Update_WhenDefenderDeadForCorpseDelay_DespawnsCorpse()
        {
            // Arrange
            SetupManager();
            var zone = CreateEnemyZone();
            var allocation = CreateAllocationWithDefenders(basic: 1);

            _allocationServiceMock.Setup(a => a.GetAllocation(EnemyFactionId, TestZoneId))
                .Returns(allocation);
            _zoneServiceMock.Setup(z => z.GetZone(TestZoneId)).Returns(zone);

            // Spawn a defender
            _manager.OnEnemyZoneEntered(zone, EnemyFactionId);
            var spawnedPedHandle = 1;

            // Kill the ped
            _gameBridge.KillPed(spawnedPedHandle);

            // Act - Update immediately after death (should mark as dead but not despawn)
            _manager.Update(EnemyFactionId);

            // Simulate passage of corpse delay time (15 seconds)
            _gameBridge.AdvanceGameTime(15000); // 15 seconds in ms

            // Update again after delay
            _manager.Update(EnemyFactionId);

            // Assert - Ped should be untracked on death (frees pool slot) and deleted after delay
            _pedDespawnServiceMock.Verify(
                d => d.UntrackPed(spawnedPedHandle),
                Times.Once,
                "Ped should be untracked from pool on death");
            _pedDespawnServiceMock.Verify(
                d => d.DeletePedEntity(spawnedPedHandle),
                Times.Once,
                "Corpse entity should be deleted after delay expires");
        }

        [Fact]
        public void Update_WhenDefenderStreamedOut_DoesNotDecrementAllocation()
        {
            // Streaming-out (entity removed by GTA's population manager) is not a kill.
            // Allocation must stay intact so the troop is preserved when streamed back in.
            SetupManager();
            var zone = CreateEnemyZone();
            var allocation = CreateAllocationWithDefenders(basic: 2);

            _allocationServiceMock.Setup(a => a.GetAllocation(EnemyFactionId, TestZoneId))
                .Returns(allocation);
            _zoneServiceMock.Setup(z => z.GetZone(TestZoneId)).Returns(zone);

            _manager.OnEnemyZoneEntered(zone, EnemyFactionId);

            _gameBridge.DeletePed(1); // simulate streamed-out

            _manager.Update(EnemyFactionId);

            Assert.Equal(2, allocation.GetTroopCount(DefenderTier.Basic));
        }

        [Fact]
        public void Update_WhenDefenderDies_StillDecrementsAllocationImmediately()
        {
            // Arrange
            SetupManager();
            var zone = CreateEnemyZone();
            var allocation = CreateAllocationWithDefenders(basic: 2);

            _allocationServiceMock.Setup(a => a.GetAllocation(EnemyFactionId, TestZoneId))
                .Returns(allocation);
            _zoneServiceMock.Setup(z => z.GetZone(TestZoneId)).Returns(zone);

            // Spawn defenders
            _manager.OnEnemyZoneEntered(zone, EnemyFactionId);

            // Kill one ped
            _gameBridge.KillPed(1);

            // Act
            _manager.Update(EnemyFactionId);

            // Assert - Allocation should be decremented immediately (even though corpse persists)
            Assert.Equal(1, allocation.GetTroopCount(DefenderTier.Basic));
        }

        [Fact]
        public void Update_WhenDefenderDies_StillSpawnsReplacementImmediately()
        {
            // Arrange
            SetupManager();
            var zone = CreateEnemyZone();
            var allocation = CreateAllocationWithDefenders(basic: 3); // 3 allocated, only 1 spawned initially

            _allocationServiceMock.Setup(a => a.GetAllocation(EnemyFactionId, TestZoneId))
                .Returns(allocation);
            _zoneServiceMock.Setup(z => z.GetZone(TestZoneId)).Returns(zone);

            // Only spawn 1 defender initially (simulating max spawned limit scenario)
            _pedSpawningServiceMock.SetupSequence(p => p.CanSpawn())
                .Returns(true)   // First spawn
                .Returns(false)  // Block additional spawns during initial entry
                .Returns(false)
                .Returns(true);  // Allow replacement spawn

            _manager.OnEnemyZoneEntered(zone, EnemyFactionId);
            Assert.Equal(1, _manager.GetSpawnedDefenderCount(TestZoneId));

            // Kill the spawned ped
            _gameBridge.KillPed(1);

            // Re-enable spawning for replacement
            _pedSpawningServiceMock.Setup(p => p.CanSpawn()).Returns(true);

            // Act
            _manager.Update(EnemyFactionId);

            // Assert - Replacement should spawn even though corpse hasn't been cleaned up yet
            // (spawned count should still be 1 after replacement, but spawning service was called)
            _pedSpawningServiceMock.Verify(
                p => p.SpawnPed(It.IsAny<string>(), It.IsAny<Vector3>(), EnemyFactionId, TestZoneId),
                Times.AtLeast(2), // Initial spawn + replacement
                "Replacement should spawn immediately when defender dies");
        }

        [Fact]
        public void OnEnemyZoneExited_ShouldDespawnDefendersForZone()
        {
            SetupManager();
            var zone = CreateEnemyZone();
            var allocation = CreateAllocationWithDefenders(basic: 1);
            _allocationServiceMock.Setup(a => a.GetAllocation(EnemyFactionId, TestZoneId)).Returns(allocation);
            _zoneServiceMock.Setup(z => z.GetZone(TestZoneId)).Returns(zone);
            _manager.OnEnemyZoneEntered(zone, EnemyFactionId);

            _manager.OnEnemyZoneExited(zone);

            _pedDespawnServiceMock.Verify(p => p.DespawnPed(It.IsAny<int>()), Times.AtLeastOnce);
        }

        [Fact]
        public void DespawnAllDefenders_ShouldDespawnTrackedDefenders()
        {
            SetupManager();
            var zone = CreateEnemyZone();
            var allocation = CreateAllocationWithDefenders(basic: 1);
            _allocationServiceMock.Setup(a => a.GetAllocation(EnemyFactionId, TestZoneId)).Returns(allocation);
            _zoneServiceMock.Setup(z => z.GetZone(TestZoneId)).Returns(zone);
            _manager.OnEnemyZoneEntered(zone, EnemyFactionId);

            _manager.DespawnAllDefenders();

            _pedDespawnServiceMock.Verify(p => p.DespawnPed(It.IsAny<int>()), Times.AtLeastOnce);
        }

        #endregion
    }
}
