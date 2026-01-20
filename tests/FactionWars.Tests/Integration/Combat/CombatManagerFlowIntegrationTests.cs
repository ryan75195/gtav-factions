using System.Collections.Generic;
using System.Linq;
using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Models;
using FactionWars.Combat.Pools;
using FactionWars.Combat.Services;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.Core.Services;
using FactionWars.Core.Utils;
using FactionWars.ScriptHookV.Managers;
using FactionWars.Territory.Interfaces;
using FactionWars.Territory.Models;
using FactionWars.Territory.Repositories;
using FactionWars.Territory.Services;
using Xunit;

namespace FactionWars.Tests.Integration.Combat
{
    /// <summary>
    /// Integration tests for the CombatManager combat flow.
    /// Tests the full flow from combat start to combat end, including:
    /// - Zone ownership changes on attacker/defender victory
    /// - Player death triggering retreat
    /// - Wave-based spawning (Heavy → Medium → Basic)
    /// - Combat update loop with ped count changes
    /// Uses real implementations with MockGameBridge for game interactions.
    /// </summary>
    public class CombatManagerFlowIntegrationTests
    {
        private readonly MockGameBridge _gameBridge;
        private readonly InMemoryPedPool _pedPool;
        private readonly IPedSpawningService _pedSpawningService;
        private readonly ISpawnPositionCalculator _spawnPositionCalculator;
        private readonly IControlPercentageCalculator _controlCalculator;
        private readonly ITakeoverDetector _takeoverDetector;
        private readonly ICombatResultHandler _combatResultHandler;
        private readonly IWaveSpawnerService _waveSpawnerService;
        private readonly IFollowerService _followerService;
        private readonly InMemoryZoneRepository _zoneRepository;
        private readonly IZoneService _zoneService;
        private readonly CombatManager _combatManager;

        private const string MichaelFactionId = "faction-michael";
        private const string TrevorFactionId = "faction-trevor";
        private const string FranklinFactionId = "faction-franklin";

        public CombatManagerFlowIntegrationTests()
        {
            // Set up game bridge
            _gameBridge = new MockGameBridge
            {
                PlayerPosition = new Vector3(100, 100, 0),
                PlayerHeading = 0f
            };

            // Set up ped pool and spawning
            _pedPool = new InMemoryPedPool(30);
            _pedSpawningService = new PedSpawningService(_gameBridge, _pedPool);
            _spawnPositionCalculator = new SpawnPositionCalculator(_gameBridge);

            // Set up combat services
            _controlCalculator = new ControlPercentageCalculator();
            _takeoverDetector = new TakeoverDetector();
            _waveSpawnerService = new WaveSpawnerService();
            _followerService = new FollowerService();

            // Set up zone repository and services
            _zoneRepository = new InMemoryZoneRepository();
            _zoneService = new ZoneService(_zoneRepository);
            _combatResultHandler = new CombatResultHandler(_zoneService);

            // Create CombatManager
            _combatManager = new CombatManager(
                _gameBridge,
                _pedPool,
                _pedSpawningService,
                _spawnPositionCalculator,
                _controlCalculator,
                _takeoverDetector,
                _combatResultHandler,
                _waveSpawnerService,
                _followerService);
        }

        #region Combat Start/End Flow Tests

        [Fact]
        public void StartCombat_CreatesNewEncounter_WhenNotInCombat()
        {
            // Arrange
            var zone = CreateAndAddZone("zone-1", "Downtown", TrevorFactionId);

            // Act
            var encounter = _combatManager.StartCombat(zone, MichaelFactionId);

            // Assert
            Assert.NotNull(encounter);
            Assert.True(_combatManager.IsInCombat);
            Assert.Equal(zone.Id, encounter.ZoneId);
            Assert.Equal(MichaelFactionId, encounter.AttackingFactionId);
            Assert.Equal(TrevorFactionId, encounter.DefendingFactionId);
            Assert.True(encounter.IsActive);
        }

        [Fact]
        public void StartCombat_ReturnsExistingEncounter_WhenAlreadyInCombat()
        {
            // Arrange
            var zone1 = CreateAndAddZone("zone-1", "Downtown", TrevorFactionId);
            var zone2 = CreateAndAddZone("zone-2", "Midtown", TrevorFactionId);

            // Act
            var encounter1 = _combatManager.StartCombat(zone1, MichaelFactionId);
            var encounter2 = _combatManager.StartCombat(zone2, MichaelFactionId);

            // Assert - should return the same encounter
            Assert.Same(encounter1, encounter2);
            Assert.Equal(zone1.Id, encounter2.ZoneId);
        }

        [Fact]
        public void EndCombat_AttackerVictory_CapturesZone()
        {
            // Arrange
            var zone = CreateAndAddZone("zone-1", "Downtown", TrevorFactionId);
            Assert.Equal(TrevorFactionId, zone.OwnerFactionId);

            _combatManager.StartCombat(zone, MichaelFactionId);

            // Act
            _combatManager.EndCombat(CombatStatus.AttackerVictory);

            // Assert
            Assert.False(_combatManager.IsInCombat);
            var updatedZone = _zoneRepository.GetById(zone.Id);
            Assert.Equal(MichaelFactionId, updatedZone!.OwnerFactionId);
            Assert.Equal(100f, updatedZone.ControlPercentage);
        }

        [Fact]
        public void EndCombat_DefenderVictory_KeepsZoneOwnership()
        {
            // Arrange
            var zone = CreateAndAddZone("zone-1", "Downtown", TrevorFactionId);

            _combatManager.StartCombat(zone, MichaelFactionId);

            // Act
            _combatManager.EndCombat(CombatStatus.DefenderVictory);

            // Assert
            Assert.False(_combatManager.IsInCombat);
            var updatedZone = _zoneRepository.GetById(zone.Id);
            Assert.Equal(TrevorFactionId, updatedZone!.OwnerFactionId);
        }

        [Fact]
        public void AbortCombat_DoesNotChangeZoneOwnership()
        {
            // Arrange
            var zone = CreateAndAddZone("zone-1", "Downtown", TrevorFactionId);

            _combatManager.StartCombat(zone, MichaelFactionId);

            // Act
            _combatManager.AbortCombat();

            // Assert
            Assert.False(_combatManager.IsInCombat);
            var updatedZone = _zoneRepository.GetById(zone.Id);
            Assert.Equal(TrevorFactionId, updatedZone!.OwnerFactionId);
        }

        #endregion

        #region Player Death Retreat Tests

        [Fact]
        public void Update_TriggersRetreat_WhenPlayerDies()
        {
            // Arrange
            var zone = CreateAndAddZone("zone-1", "Downtown", TrevorFactionId);
            _combatManager.StartCombat(zone, MichaelFactionId);

            // Simulate player death
            _gameBridge.IsPlayerDeadValue = true;

            // Act
            _combatManager.Update();

            // Assert - combat ended due to retreat
            Assert.False(_combatManager.IsInCombat);
            // Zone ownership should be unchanged (retreat = no capture)
            var updatedZone = _zoneRepository.GetById(zone.Id);
            Assert.Equal(TrevorFactionId, updatedZone!.OwnerFactionId);
        }

        [Fact]
        public void Retreat_EndsCurrentCombat_WithPlayerRetreatStatus()
        {
            // Arrange
            var zone = CreateAndAddZone("zone-1", "Downtown", TrevorFactionId);
            var encounter = _combatManager.StartCombat(zone, MichaelFactionId);
            bool combatEndedRaised = false;
            CombatStatus? endStatus = null;

            _combatManager.CombatEnded += (sender, e) =>
            {
                combatEndedRaised = true;
                endStatus = e.Status;
            };

            // Act
            _combatManager.Retreat();

            // Assert
            Assert.False(_combatManager.IsInCombat);
            Assert.True(combatEndedRaised);
            Assert.Equal(CombatStatus.PlayerRetreat, endStatus);
        }

        [Fact]
        public void Retreat_LeavesZoneOwnershipUnchanged()
        {
            // Arrange
            var zone = CreateAndAddZone("zone-1", "Downtown", TrevorFactionId);
            zone.ControlPercentage = 75f; // Partially contested
            _zoneRepository.Update(zone);

            _combatManager.StartCombat(zone, MichaelFactionId);

            // Act
            _combatManager.Retreat();

            // Assert
            var updatedZone = _zoneRepository.GetById(zone.Id);
            Assert.Equal(TrevorFactionId, updatedZone!.OwnerFactionId);
            // Zone control remains as it was (not reset to 100% like after defender victory)
        }

        #endregion

        #region Wave-Based Spawning Tests

        [Fact]
        public void InitializeWaveSpawning_CreatesWaveState_FromSpawnPlan()
        {
            // Arrange
            var zone = CreateAndAddZone("zone-1", "Downtown", TrevorFactionId);
            _combatManager.StartCombat(zone, MichaelFactionId);

            var spawnPlan = new DefenderSpawnPlan(5, 3, 2); // 5 basic, 3 medium, 2 heavy

            // Act
            _combatManager.InitializeWaveSpawning(spawnPlan);

            // Assert
            Assert.NotNull(_combatManager.CurrentWaveState);
            Assert.Equal(10, _combatManager.GetRemainingDefendersToSpawn());
            Assert.False(_combatManager.IsWaveSpawningComplete());
        }

        [Fact]
        public void GetNextWaveTier_ReturnsHeavyFirst_ThenMediumThenBasic()
        {
            // Arrange
            var zone = CreateAndAddZone("zone-1", "Downtown", TrevorFactionId);
            _combatManager.StartCombat(zone, MichaelFactionId);

            var spawnPlan = new DefenderSpawnPlan(5, 3, 2); // 5 basic, 3 medium, 2 heavy
            _combatManager.InitializeWaveSpawning(spawnPlan);

            // Act & Assert - Heavy should be first
            Assert.Equal(DefenderTier.Heavy, _combatManager.GetNextWaveTier());
        }

        [Fact]
        public void GetWaveSpawnOrder_ReturnsHeavyMediumBasic()
        {
            // Act
            var order = _combatManager.GetWaveSpawnOrder();

            // Assert
            Assert.Equal(3, order.Count);
            Assert.Equal(DefenderTier.Heavy, order[0]);
            Assert.Equal(DefenderTier.Medium, order[1]);
            Assert.Equal(DefenderTier.Basic, order[2]);
        }

        [Fact]
        public void SpawnNextWave_SpawnsHeavyDefenders_UntilComplete()
        {
            // Arrange
            var zone = CreateAndAddZone("zone-1", "Downtown", TrevorFactionId);
            _combatManager.StartCombat(zone, MichaelFactionId);

            var spawnPlan = new DefenderSpawnPlan(0, 0, 3); // Only 3 heavy
            _combatManager.InitializeWaveSpawning(spawnPlan);

            var modelsByTier = new Dictionary<DefenderTier, string>
            {
                { DefenderTier.Heavy, "s_m_y_blackops_01" },
                { DefenderTier.Medium, "s_m_y_armymech_01" },
                { DefenderTier.Basic, "s_m_y_dealer_01" }
            };

            // Act - Spawn all heavy in one wave
            var spawned = _combatManager.SpawnNextWave(modelsByTier, TrevorFactionId, 10);

            // Assert
            Assert.Equal(3, spawned.Count);
            Assert.All(spawned, ped => Assert.True(ped.IsValid));
            Assert.True(_combatManager.IsWaveSpawningComplete());
        }

        [Fact]
        public void SpawnNextWave_ProgressesThroughAllTiers_InOrder()
        {
            // Arrange
            var zone = CreateAndAddZone("zone-1", "Downtown", TrevorFactionId);
            _combatManager.StartCombat(zone, MichaelFactionId);

            var spawnPlan = new DefenderSpawnPlan(2, 2, 2); // 2 of each
            _combatManager.InitializeWaveSpawning(spawnPlan);

            var modelsByTier = new Dictionary<DefenderTier, string>
            {
                { DefenderTier.Heavy, "s_m_y_blackops_01" },
                { DefenderTier.Medium, "s_m_y_armymech_01" },
                { DefenderTier.Basic, "s_m_y_dealer_01" }
            };

            // Act & Assert - First wave: Heavy
            Assert.Equal(DefenderTier.Heavy, _combatManager.GetNextWaveTier());
            var heavy = _combatManager.SpawnNextWave(modelsByTier, TrevorFactionId, 10);
            Assert.Equal(2, heavy.Count);

            // Second wave: Medium
            Assert.Equal(DefenderTier.Medium, _combatManager.GetNextWaveTier());
            var medium = _combatManager.SpawnNextWave(modelsByTier, TrevorFactionId, 10);
            Assert.Equal(2, medium.Count);

            // Third wave: Basic
            Assert.Equal(DefenderTier.Basic, _combatManager.GetNextWaveTier());
            var basic = _combatManager.SpawnNextWave(modelsByTier, TrevorFactionId, 10);
            Assert.Equal(2, basic.Count);

            // All complete
            Assert.Null(_combatManager.GetNextWaveTier());
            Assert.True(_combatManager.IsWaveSpawningComplete());
            Assert.Equal(0, _combatManager.GetRemainingDefendersToSpawn());
        }

        [Fact]
        public void SpawnNextWave_RespectsMaxPerTick_SpawnsIncrementally()
        {
            // Arrange
            var zone = CreateAndAddZone("zone-1", "Downtown", TrevorFactionId);
            _combatManager.StartCombat(zone, MichaelFactionId);

            var spawnPlan = new DefenderSpawnPlan(0, 0, 5); // 5 heavy
            _combatManager.InitializeWaveSpawning(spawnPlan);

            var modelsByTier = new Dictionary<DefenderTier, string>
            {
                { DefenderTier.Heavy, "s_m_y_blackops_01" }
            };

            // Act - Spawn 2 at a time
            var wave1 = _combatManager.SpawnNextWave(modelsByTier, TrevorFactionId, 2);
            var wave2 = _combatManager.SpawnNextWave(modelsByTier, TrevorFactionId, 2);
            var wave3 = _combatManager.SpawnNextWave(modelsByTier, TrevorFactionId, 2);

            // Assert
            Assert.Equal(2, wave1.Count);
            Assert.Equal(2, wave2.Count);
            Assert.Equal(1, wave3.Count); // Only 1 remaining
            Assert.True(_combatManager.IsWaveSpawningComplete());
        }

        #endregion

        #region Combat Update Loop Tests

        [Fact]
        public void Update_CalculatesControlPercentage_BasedOnPedCounts()
        {
            // Arrange
            var zone = CreateAndAddZone("zone-1", "Downtown", TrevorFactionId);
            var encounter = _combatManager.StartCombat(zone, MichaelFactionId);

            // Spawn some peds for each faction
            SpawnPedsForFaction(MichaelFactionId, zone.Id, 5); // 5 attackers
            SpawnPedsForFaction(TrevorFactionId, zone.Id, 5);  // 5 defenders

            // Act
            _combatManager.Update();

            // Assert - 50/50 split
            Assert.Equal(5, encounter.AttackerPedCount);
            Assert.Equal(5, encounter.DefenderPedCount);
            Assert.Equal(50f, encounter.AttackerControlPercentage);
            Assert.Equal(50f, encounter.DefenderControlPercentage);
        }

        [Fact]
        public void Update_EndsWithAttackerVictory_WhenAllDefendersEliminated()
        {
            // Arrange
            var zone = CreateAndAddZone("zone-1", "Downtown", TrevorFactionId);
            _combatManager.StartCombat(zone, MichaelFactionId);

            // Only attackers present (defenders eliminated)
            SpawnPedsForFaction(MichaelFactionId, zone.Id, 5);
            // No defenders

            bool combatEnded = false;
            _combatManager.CombatEnded += (sender, e) => combatEnded = true;

            // Act
            _combatManager.Update();

            // Assert
            Assert.True(combatEnded);
            Assert.False(_combatManager.IsInCombat);
            var updatedZone = _zoneRepository.GetById(zone.Id);
            Assert.Equal(MichaelFactionId, updatedZone!.OwnerFactionId);
        }

        [Fact]
        public void Update_EndsWithDefenderVictory_WhenAllAttackersEliminated()
        {
            // Arrange
            var zone = CreateAndAddZone("zone-1", "Downtown", TrevorFactionId);
            _combatManager.StartCombat(zone, MichaelFactionId);

            // Only defenders present (attackers eliminated)
            SpawnPedsForFaction(TrevorFactionId, zone.Id, 5);
            // No attackers

            bool combatEnded = false;
            _combatManager.CombatEnded += (sender, e) => combatEnded = true;

            // Act
            _combatManager.Update();

            // Assert
            Assert.True(combatEnded);
            Assert.False(_combatManager.IsInCombat);
            var updatedZone = _zoneRepository.GetById(zone.Id);
            Assert.Equal(TrevorFactionId, updatedZone!.OwnerFactionId);
        }

        [Fact]
        public void Update_ContinuesCombat_WhenBothSidesHavePeds()
        {
            // Arrange
            var zone = CreateAndAddZone("zone-1", "Downtown", TrevorFactionId);
            _combatManager.StartCombat(zone, MichaelFactionId);

            // Both sides have peds
            SpawnPedsForFaction(MichaelFactionId, zone.Id, 5);
            SpawnPedsForFaction(TrevorFactionId, zone.Id, 5);

            bool combatEnded = false;
            _combatManager.CombatEnded += (sender, e) => combatEnded = true;

            // Act
            _combatManager.Update();

            // Assert - combat still active
            Assert.False(combatEnded);
            Assert.True(_combatManager.IsInCombat);
        }

        #endregion

        #region Defender Spawning Tests

        [Fact]
        public void SpawnDefenders_SpawnsPedsAtNaturalPositions()
        {
            // Arrange
            var zone = CreateAndAddZone("zone-1", "Downtown", TrevorFactionId);
            _combatManager.StartCombat(zone, MichaelFactionId);

            // Act
            var spawned = _combatManager.SpawnDefenders("s_m_y_dealer_01", TrevorFactionId, 3);

            // Assert
            Assert.Equal(3, spawned.Count);
            Assert.All(spawned, ped => Assert.True(ped.IsValid));
            Assert.All(spawned, ped => Assert.Equal(TrevorFactionId, ped.FactionId));
            Assert.All(spawned, ped => Assert.Equal(zone.Id, ped.ZoneId));
        }

        [Fact]
        public void SpawnDefender_SpawnsSinglePed_AtNaturalPosition()
        {
            // Arrange
            var zone = CreateAndAddZone("zone-1", "Downtown", TrevorFactionId);
            _combatManager.StartCombat(zone, MichaelFactionId);

            // Act
            var ped = _combatManager.SpawnDefender("s_m_y_dealer_01", TrevorFactionId);

            // Assert
            Assert.True(ped.IsValid);
            Assert.Equal(TrevorFactionId, ped.FactionId);
            Assert.Equal(zone.Id, ped.ZoneId);
        }

        [Fact]
        public void CanSpawnDefenders_ReturnsFalse_WhenPoolIsFull()
        {
            // Arrange
            var zone = CreateAndAddZone("zone-1", "Downtown", TrevorFactionId);
            _combatManager.StartCombat(zone, MichaelFactionId);

            // Fill the pool
            for (int i = 0; i < _pedPool.MaxCapacity; i++)
            {
                _combatManager.SpawnDefender("s_m_y_dealer_01", TrevorFactionId);
            }

            // Act & Assert
            Assert.False(_combatManager.CanSpawnDefenders());
            Assert.Equal(0, _combatManager.CanSpawnDefendersCount());
        }

        #endregion

        #region Event Handling Tests

        [Fact]
        public void CombatStarted_IsRaised_WhenCombatBegins()
        {
            // Arrange
            var zone = CreateAndAddZone("zone-1", "Downtown", TrevorFactionId);
            bool eventRaised = false;
            CombatEncounter? eventEncounter = null;

            _combatManager.CombatStarted += (sender, e) =>
            {
                eventRaised = true;
                eventEncounter = e;
            };

            // Act
            var encounter = _combatManager.StartCombat(zone, MichaelFactionId);

            // Assert
            Assert.True(eventRaised);
            Assert.Same(encounter, eventEncounter);
        }

        [Fact]
        public void CombatEnded_IsRaised_WhenCombatEnds()
        {
            // Arrange
            var zone = CreateAndAddZone("zone-1", "Downtown", TrevorFactionId);
            bool eventRaised = false;
            CombatEncounter? eventEncounter = null;

            _combatManager.CombatEnded += (sender, e) =>
            {
                eventRaised = true;
                eventEncounter = e;
            };

            var encounter = _combatManager.StartCombat(zone, MichaelFactionId);

            // Act
            _combatManager.EndCombat(CombatStatus.AttackerVictory);

            // Assert
            Assert.True(eventRaised);
            Assert.Equal(encounter.Id, eventEncounter!.Id);
            Assert.Equal(CombatStatus.AttackerVictory, eventEncounter.Status);
        }

        #endregion

        #region Full Combat Flow Scenarios

        [Fact]
        public void FullFlow_PlayerEntersZone_FightsDefenders_CapturesZone()
        {
            // Arrange: Zone owned by Trevor
            var zone = CreateAndAddZone("zone-1", "Downtown", TrevorFactionId);

            // Step 1: Player enters enemy zone - combat starts
            var encounter = _combatManager.StartCombat(zone, MichaelFactionId);
            Assert.True(_combatManager.IsInCombat);

            // Step 2: Initialize wave spawning for defenders
            var spawnPlan = new DefenderSpawnPlan(2, 1, 1); // 4 defenders total
            _combatManager.InitializeWaveSpawning(spawnPlan);

            // Step 3: Spawn all waves
            var modelsByTier = new Dictionary<DefenderTier, string>
            {
                { DefenderTier.Heavy, "s_m_y_blackops_01" },
                { DefenderTier.Medium, "s_m_y_armymech_01" },
                { DefenderTier.Basic, "s_m_y_dealer_01" }
            };

            // Spawn all waves
            _combatManager.SpawnNextWave(modelsByTier, TrevorFactionId, 10); // Heavy
            _combatManager.SpawnNextWave(modelsByTier, TrevorFactionId, 10); // Medium
            _combatManager.SpawnNextWave(modelsByTier, TrevorFactionId, 10); // Basic

            Assert.True(_combatManager.IsWaveSpawningComplete());

            // Step 4: Player spawns as attacker (simulated by adding to pool)
            SpawnPedsForFaction(MichaelFactionId, zone.Id, 5);

            // Step 5: Update combat - both sides have peds, combat continues
            _combatManager.Update();
            Assert.True(_combatManager.IsInCombat);

            // Step 6: Simulate combat - all defenders die (remove from pool)
            foreach (var ped in _pedPool.GetByFaction(TrevorFactionId).ToList())
            {
                _pedPool.Remove(ped);
            }

            // Step 7: Update - attacker wins
            _combatManager.Update();

            // Assert: Zone captured by Michael
            Assert.False(_combatManager.IsInCombat);
            var updatedZone = _zoneRepository.GetById(zone.Id);
            Assert.Equal(MichaelFactionId, updatedZone!.OwnerFactionId);
            Assert.Equal(100f, updatedZone.ControlPercentage);
        }

        [Fact]
        public void FullFlow_PlayerDiesInCombat_RetreatsAndZoneUnchanged()
        {
            // Arrange: Zone owned by Trevor
            var zone = CreateAndAddZone("zone-1", "Downtown", TrevorFactionId);

            // Step 1: Player attacks zone
            _combatManager.StartCombat(zone, MichaelFactionId);

            // Step 2: Spawn some defenders
            var spawnPlan = new DefenderSpawnPlan(3, 0, 0);
            _combatManager.InitializeWaveSpawning(spawnPlan);

            var modelsByTier = new Dictionary<DefenderTier, string>
            {
                { DefenderTier.Basic, "s_m_y_dealer_01" }
            };
            _combatManager.SpawnNextWave(modelsByTier, TrevorFactionId, 10);

            // Step 3: Add player as attacker
            SpawnPedsForFaction(MichaelFactionId, zone.Id, 1);

            // Step 4: Combat in progress
            _combatManager.Update();
            Assert.True(_combatManager.IsInCombat);

            // Step 5: Player dies
            _gameBridge.IsPlayerDeadValue = true;
            _combatManager.Update();

            // Assert: Combat ended as retreat, zone unchanged
            Assert.False(_combatManager.IsInCombat);
            var updatedZone = _zoneRepository.GetById(zone.Id);
            Assert.Equal(TrevorFactionId, updatedZone!.OwnerFactionId);
        }

        [Fact]
        public void FullFlow_DefendersRepelAttacker_ZoneRemainsSafe()
        {
            // Arrange: Zone owned by Trevor
            var zone = CreateAndAddZone("zone-1", "Downtown", TrevorFactionId);

            // Step 1: Player attacks zone
            _combatManager.StartCombat(zone, MichaelFactionId);

            // Step 2: Strong defenders
            SpawnPedsForFaction(TrevorFactionId, zone.Id, 10);

            // Step 3: Weak attacker
            SpawnPedsForFaction(MichaelFactionId, zone.Id, 1);

            // Step 4: Update - combat in progress
            _combatManager.Update();
            Assert.True(_combatManager.IsInCombat);

            // Step 5: Attackers eliminated
            foreach (var ped in _pedPool.GetByFaction(MichaelFactionId).ToList())
            {
                _pedPool.Remove(ped);
            }

            // Step 6: Update - defender victory
            _combatManager.Update();

            // Assert: Zone still owned by Trevor
            Assert.False(_combatManager.IsInCombat);
            var updatedZone = _zoneRepository.GetById(zone.Id);
            Assert.Equal(TrevorFactionId, updatedZone!.OwnerFactionId);
            Assert.Equal(100f, updatedZone.ControlPercentage);
        }

        #endregion

        #region Helper Methods

        private Zone CreateAndAddZone(string id, string name, string ownerFactionId)
        {
            var zone = new Zone(id, name, new Vector3(100, 100, 0), 200f, 5);
            zone.OwnerFactionId = ownerFactionId;
            zone.ControlPercentage = 100f;
            _zoneRepository.Add(zone);
            return zone;
        }

        private void SpawnPedsForFaction(string factionId, string zoneId, int count)
        {
            for (int i = 0; i < count; i++)
            {
                _pedSpawningService.SpawnPed("s_m_y_dealer_01", new Vector3(100, 100, 0), factionId, zoneId);
            }
        }

        #endregion
    }
}
