using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FactionWars.AI.Models;
using FactionWars.AI.Strategies;
using FactionWars.Combat.Models;
using FactionWars.Combat.Services;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.Core.Services;
using FactionWars.Economy.Services;
using FactionWars.Factions.Interfaces;
using FactionWars.Factions.Models;
using FactionWars.Factions.Repositories;
using FactionWars.Factions.Services;
using FactionWars.Persistence;
using FactionWars.Persistence.Models;
using FactionWars.Territory.Interfaces;
using FactionWars.Territory.Models;
using FactionWars.Territory.Repositories;
using FactionWars.Territory.Services;
using Xunit;

namespace FactionWars.Tests.Integration.Persistence
{
    /// <summary>
    /// Full integration tests for save/load functionality.
    /// Tests the complete round-trip of saving game state to disk and restoring it,
    /// verifying that all game systems remain consistent after a save/load cycle.
    /// </summary>
    public class SaveLoadIntegrationTests : IDisposable
    {
        private const string MichaelFactionId = "faction-michael";
        private const string TrevorFactionId = "faction-trevor";
        private const string FranklinFactionId = "faction-franklin";

        private readonly string _testDirectory;
        private readonly IPersistenceService _persistenceService;
        private readonly ISaveSlotManager _saveSlotManager;

        public SaveLoadIntegrationTests()
        {
            _testDirectory = Path.Combine(Path.GetTempPath(), "FactionWarsIntegrationTests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testDirectory);
            _persistenceService = new JsonPersistenceService();
            _saveSlotManager = new SaveSlotManager(_persistenceService, _testDirectory);
        }

        public void Dispose()
        {
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, recursive: true);
            }
        }

        private string GetTestFilePath(string fileName = "test_save.json")
        {
            return Path.Combine(_testDirectory, fileName);
        }

        #region Full Game State Save/Load Integration

        [Fact]
        public void Integration_SaveAndLoadCompleteGameState_PreservesAllSystems()
        {
            // Arrange: Create a full game world with all systems populated
            var originalWorld = CreateCompleteGameWorld();
            var gameState = CreateGameStateSnapshot(originalWorld);
            var filePath = GetTestFilePath("full_game.json");

            // Act: Save and reload
            _persistenceService.Save(gameState, filePath);
            var loadedState = _persistenceService.Load(filePath);
            var restoredWorld = RestoreGameWorld(loadedState);

            // Assert: All data is preserved
            AssertZonesEqual(originalWorld.ZoneRepo, restoredWorld.ZoneRepo);
            AssertFactionsEqual(originalWorld.FactionRepo, restoredWorld.FactionRepo);
            AssertFactionStatesEqual(originalWorld.FactionService, restoredWorld.FactionService);
        }

        [Fact]
        public async Task Integration_AsyncSaveAndLoad_PreservesGameState()
        {
            // Arrange
            var originalWorld = CreateCompleteGameWorld();
            var gameState = CreateGameStateSnapshot(originalWorld);
            var filePath = GetTestFilePath("async_save.json");

            // Act
            await _persistenceService.SaveAsync(gameState, filePath);
            var loadedState = await _persistenceService.LoadAsync(filePath);
            var restoredWorld = RestoreGameWorld(loadedState);

            // Assert
            AssertZonesEqual(originalWorld.ZoneRepo, restoredWorld.ZoneRepo);
            AssertFactionsEqual(originalWorld.FactionRepo, restoredWorld.FactionRepo);
        }

        [Fact]
        public void Integration_SaveLoadCycle_AfterCombat_PreservesZoneOwnership()
        {
            // Arrange: Create world and simulate combat
            var world = CreateCompleteGameWorld();

            // Michael attacks and captures Trevor's zone
            var trevorZone = world.ZoneRepo.GetAll().First(z => z.OwnerFactionId == TrevorFactionId);
            var encounter = new CombatEncounter("combat-1", trevorZone.Id, MichaelFactionId, TrevorFactionId);
            encounter.AttackerPedCount = 25;
            encounter.DefenderPedCount = 0;

            var controlCalc = new ControlPercentageCalculator();
            controlCalc.ApplyToEncounter(encounter);
            encounter.End(CombatStatus.AttackerVictory);
            world.CombatHandler.ProcessCombatResult(encounter);

            // Zone is now neutral after victory - Michael claims it
            var neutralZone = world.ZoneRepo.GetById(trevorZone.Id)!;
            neutralZone.OwnerFactionId = MichaelFactionId;
            neutralZone.ControlPercentage = 100f;
            world.ZoneRepo.Update(neutralZone);

            // Update faction zone tracking
            world.FactionService.RemoveZoneFromFaction(TrevorFactionId, trevorZone.Id);
            world.FactionService.AddZoneToFaction(MichaelFactionId, trevorZone.Id);

            // Verify pre-save state
            Assert.Equal(MichaelFactionId, world.ZoneService.GetZone(trevorZone.Id)!.OwnerFactionId);
            int michaelZonesBefore = world.FactionService.GetZoneCount(MichaelFactionId);

            // Act: Save and restore
            var gameState = CreateGameStateSnapshot(world);
            var filePath = GetTestFilePath("post_combat.json");
            _persistenceService.Save(gameState, filePath);
            var loadedState = _persistenceService.Load(filePath);
            var restoredWorld = RestoreGameWorld(loadedState);

            // Assert: Zone ownership is preserved
            Assert.Equal(MichaelFactionId, restoredWorld.ZoneService.GetZone(trevorZone.Id)!.OwnerFactionId);
            Assert.Equal(michaelZonesBefore, restoredWorld.FactionService.GetZoneCount(MichaelFactionId));
        }

        [Fact]
        public void Integration_SaveLoadCycle_AfterEconomyTicks_PreservesResources()
        {
            // Arrange: Create world and run economy ticks
            var world = CreateCompleteGameWorld();
            world.TickService.Start();

            // Run several economy ticks
            for (int i = 0; i < 5; i++)
            {
                world.TickService.ForceTick();
            }

            // Capture resource levels
            var michaelCash = world.FactionService.GetFactionState(MichaelFactionId)!.Cash;
            var michaelWeapons = world.FactionService.GetFactionState(MichaelFactionId)!.Weapons;
            var michaelRecruitment = world.FactionService.GetFactionState(MichaelFactionId)!.RecruitmentPoints;

            Assert.True(michaelCash > 0, "Should have accumulated cash");

            // Act: Save and restore
            var gameState = CreateGameStateSnapshot(world);
            var filePath = GetTestFilePath("post_economy.json");
            _persistenceService.Save(gameState, filePath);
            var loadedState = _persistenceService.Load(filePath);
            var restoredWorld = RestoreGameWorld(loadedState);

            // Assert: Resources are preserved
            var restoredMichaelState = restoredWorld.FactionService.GetFactionState(MichaelFactionId);
            Assert.NotNull(restoredMichaelState);
            Assert.Equal(michaelCash, restoredMichaelState.Cash);
            Assert.Equal(michaelWeapons, restoredMichaelState.Weapons);
            Assert.Equal(michaelRecruitment, restoredMichaelState.RecruitmentPoints);
        }

        [Fact]
        public void Integration_SaveLoadCycle_PreservesContestedZoneState()
        {
            // Arrange: Create world with contested zones
            var world = CreateCompleteGameWorld();

            var zone = world.ZoneRepo.GetAll().First();
            zone.IsContested = true;
            zone.ControlPercentage = 55.5f;
            world.ZoneRepo.Update(zone);

            // Act: Save and restore
            var gameState = CreateGameStateSnapshot(world);
            var filePath = GetTestFilePath("contested.json");
            _persistenceService.Save(gameState, filePath);
            var loadedState = _persistenceService.Load(filePath);
            var restoredWorld = RestoreGameWorld(loadedState);

            // Assert: Contested state is preserved
            var restoredZone = restoredWorld.ZoneRepo.GetById(zone.Id);
            Assert.NotNull(restoredZone);
            Assert.True(restoredZone.IsContested);
            Assert.Equal(55.5f, restoredZone.ControlPercentage);
        }

        [Fact]
        public void Integration_SaveLoadCycle_PreservesZoneTraits()
        {
            // Arrange: Create zones with various trait combinations
            var world = CreateCompleteGameWorld();

            var zones = world.ZoneRepo.GetAll().ToList();
            zones[0].Traits = ZoneTrait.Commercial | ZoneTrait.HighValue;
            zones[1].Traits = ZoneTrait.Industrial | ZoneTrait.Fortified;
            zones[2].Traits = ZoneTrait.Residential | ZoneTrait.Port;
            foreach (var zone in zones)
            {
                world.ZoneRepo.Update(zone);
            }

            // Act: Save and restore
            var gameState = CreateGameStateSnapshot(world);
            var filePath = GetTestFilePath("traits.json");
            _persistenceService.Save(gameState, filePath);
            var loadedState = _persistenceService.Load(filePath);
            var restoredWorld = RestoreGameWorld(loadedState);

            // Assert: All traits are preserved
            var restoredZones = restoredWorld.ZoneRepo.GetAll().ToList();
            Assert.True(restoredZones[0].Traits.HasFlag(ZoneTrait.Commercial));
            Assert.True(restoredZones[0].Traits.HasFlag(ZoneTrait.HighValue));
            Assert.True(restoredZones[1].Traits.HasFlag(ZoneTrait.Industrial));
            Assert.True(restoredZones[1].Traits.HasFlag(ZoneTrait.Fortified));
            Assert.True(restoredZones[2].Traits.HasFlag(ZoneTrait.Residential));
            Assert.True(restoredZones[2].Traits.HasFlag(ZoneTrait.Port));
        }

        #endregion

        #region Save Slot Integration Tests

        [Fact]
        public void Integration_SaveSlot_FullGameStateCycle()
        {
            // Arrange
            var world = CreateCompleteGameWorld();
            var gameState = CreateGameStateSnapshot(world);
            gameState.SaveName = "Campaign Slot 1";
            gameState.TotalPlayTimeSeconds = 7200;

            // Act: Save to slot, then load
            _saveSlotManager.SaveToSlot(0, gameState);
            var loadedState = _saveSlotManager.LoadFromSlot(0);

            // Assert
            Assert.Equal("Campaign Slot 1", loadedState.SaveName);
            Assert.Equal(7200, loadedState.TotalPlayTimeSeconds);
            Assert.Equal(3, loadedState.Factions.Count);
            Assert.Equal(6, loadedState.Zones.Count);
        }

        [Fact]
        public async Task Integration_SaveSlotAsync_FullGameStateCycle()
        {
            // Arrange
            var world = CreateCompleteGameWorld();
            var gameState = CreateGameStateSnapshot(world);
            gameState.SaveName = "Async Campaign";

            // Act
            await _saveSlotManager.SaveToSlotAsync(1, gameState);
            var loadedState = await _saveSlotManager.LoadFromSlotAsync(1);

            // Assert
            Assert.Equal("Async Campaign", loadedState.SaveName);
            Assert.Equal(3, loadedState.Factions.Count);
        }

        [Fact]
        public void Integration_MultipleSaveSlots_IndependentState()
        {
            // Arrange: Create different game states for different slots
            var world1 = CreateCompleteGameWorld();
            var gameState1 = CreateGameStateSnapshot(world1);
            gameState1.SaveName = "Early Game";

            // Simulate progression - Michael conquers a zone
            var zone = world1.ZoneRepo.GetAll().First(z => z.OwnerFactionId == TrevorFactionId);
            zone.OwnerFactionId = MichaelFactionId;
            world1.ZoneRepo.Update(zone);
            world1.FactionService.RemoveZoneFromFaction(TrevorFactionId, zone.Id);
            world1.FactionService.AddZoneToFaction(MichaelFactionId, zone.Id);
            var gameState2 = CreateGameStateSnapshot(world1);
            gameState2.SaveName = "Late Game";

            // Act: Save both states to different slots
            _saveSlotManager.SaveToSlot(0, gameState1);
            _saveSlotManager.SaveToSlot(1, gameState2);

            // Load both and verify independence
            var loaded1 = _saveSlotManager.LoadFromSlot(0);
            var loaded2 = _saveSlotManager.LoadFromSlot(1);

            // Assert: Slots contain different states
            Assert.Equal("Early Game", loaded1.SaveName);
            Assert.Equal("Late Game", loaded2.SaveName);

            // Zone ownership differs between slots
            var zoneData1 = loaded1.Zones.First(z => z.Id == zone.Id);
            var zoneData2 = loaded2.Zones.First(z => z.Id == zone.Id);
            Assert.Equal(TrevorFactionId, zoneData1.OwnerFactionId);
            Assert.Equal(MichaelFactionId, zoneData2.OwnerFactionId);
        }

        [Fact]
        public void Integration_SaveSlot_CopyAndModify()
        {
            // Arrange
            var world = CreateCompleteGameWorld();
            var gameState = CreateGameStateSnapshot(world);
            gameState.SaveName = "Original";
            _saveSlotManager.SaveToSlot(0, gameState);

            // Act: Copy slot and modify original
            _saveSlotManager.CopySlot(0, 1);

            // Modify the state in slot 0
            var modifiedState = _saveSlotManager.LoadFromSlot(0);
            modifiedState.SaveName = "Modified Original";
            modifiedState.TotalPlayTimeSeconds = 10000;
            _saveSlotManager.SaveToSlot(0, modifiedState);

            // Assert: Copy is unaffected
            var copied = _saveSlotManager.LoadFromSlot(1);
            Assert.Equal("Original", copied.SaveName);
            Assert.NotEqual(10000, copied.TotalPlayTimeSeconds);

            var modified = _saveSlotManager.LoadFromSlot(0);
            Assert.Equal("Modified Original", modified.SaveName);
            Assert.Equal(10000, modified.TotalPlayTimeSeconds);
        }

        [Fact]
        public void Integration_SaveSlot_OverwriteExisting()
        {
            // Arrange
            var world = CreateCompleteGameWorld();
            var gameState1 = CreateGameStateSnapshot(world);
            gameState1.SaveName = "First Save";
            _saveSlotManager.SaveToSlot(0, gameState1);

            // Create new state with different data
            var gameState2 = CreateGameStateSnapshot(world);
            gameState2.SaveName = "Second Save";
            gameState2.TotalPlayTimeSeconds = 99999;

            // Act: Overwrite slot
            _saveSlotManager.SaveToSlot(0, gameState2);

            // Assert
            var loaded = _saveSlotManager.LoadFromSlot(0);
            Assert.Equal("Second Save", loaded.SaveName);
            Assert.Equal(99999, loaded.TotalPlayTimeSeconds);
        }

        #endregion

        #region AutoSave Integration Tests

        [Fact]
        public void Integration_AutoSave_SavesAndLoadsGameState()
        {
            // Arrange
            var world = CreateCompleteGameWorld();
            var gameStateProvider = new TestGameStateProvider(world);
            var autoSaveService = new AutoSaveService(
                _persistenceService,
                gameStateProvider,
                _testDirectory,
                TimeSpan.FromSeconds(1));

            // Act: Trigger auto-save
            autoSaveService.Start();
            autoSaveService.TriggerSave();

            // Assert: Auto-save exists and contains valid data
            Assert.True(autoSaveService.HasAutoSave());
            var loadedState = autoSaveService.LoadAutoSave();
            Assert.Equal("Auto Save", loadedState.SaveName);
            Assert.Equal(3, loadedState.Factions.Count);
            Assert.Equal(6, loadedState.Zones.Count);
        }

        [Fact]
        public void Integration_AutoSave_UpdateTriggersAtInterval()
        {
            // Arrange
            var world = CreateCompleteGameWorld();
            var gameStateProvider = new TestGameStateProvider(world);
            var autoSaveService = new AutoSaveService(
                _persistenceService,
                gameStateProvider,
                _testDirectory,
                TimeSpan.FromMilliseconds(100));

            autoSaveService.Start();
            autoSaveService.IsEnabled = true;

            // Act: Simulate time passing
            autoSaveService.Update(TimeSpan.FromMilliseconds(50));
            Assert.False(autoSaveService.HasAutoSave()); // Not yet

            autoSaveService.Update(TimeSpan.FromMilliseconds(60));
            Assert.True(autoSaveService.HasAutoSave()); // Should have triggered

            // Assert
            Assert.Equal(1, autoSaveService.AutoSaveCount);
        }

        [Fact]
        public void Integration_AutoSave_DeleteRemovesFile()
        {
            // Arrange
            var world = CreateCompleteGameWorld();
            var gameStateProvider = new TestGameStateProvider(world);
            var autoSaveService = new AutoSaveService(
                _persistenceService,
                gameStateProvider,
                _testDirectory);

            autoSaveService.TriggerSave();
            Assert.True(autoSaveService.HasAutoSave());

            // Act
            autoSaveService.DeleteAutoSave();

            // Assert
            Assert.False(autoSaveService.HasAutoSave());
        }

        #endregion

        #region Edge Cases and Error Recovery

        [Fact]
        public void Integration_SaveLoad_EmptyZoneList_HandledGracefully()
        {
            // Arrange: Create game state with no zones
            var gameState = new GameState
            {
                SaveName = "Empty World",
                Factions = new List<FactionData>
                {
                    new FactionData { Id = MichaelFactionId, Name = "Michael's Crew" }
                }
            };
            var filePath = GetTestFilePath("empty_zones.json");

            // Act
            _persistenceService.Save(gameState, filePath);
            var loadedState = _persistenceService.Load(filePath);

            // Assert
            Assert.Empty(loadedState.Zones);
            Assert.Single(loadedState.Factions);
        }

        [Fact]
        public void Integration_SaveLoad_NullOwnerFactionId_Preserved()
        {
            // Arrange: Zone with no owner
            var gameState = new GameState { SaveName = "Neutral Zone Test" };
            gameState.Zones.Add(new ZoneData
            {
                Id = "neutral-zone",
                Name = "Neutral Territory",
                OwnerFactionId = null,
                ControlPercentage = 0f
            });
            var filePath = GetTestFilePath("neutral.json");

            // Act
            _persistenceService.Save(gameState, filePath);
            var loadedState = _persistenceService.Load(filePath);

            // Assert
            Assert.Null(loadedState.Zones[0].OwnerFactionId);
        }

        [Fact]
        public void Integration_SaveLoad_LargeResourceValues_Preserved()
        {
            // Arrange: Test with large numbers
            var gameState = new GameState { SaveName = "Rich Faction" };
            gameState.FactionStates.Add(new FactionStateData
            {
                FactionId = MichaelFactionId,
                Cash = int.MaxValue,
                Weapons = 999999,
                RecruitmentPoints = 500000,
                TroopCount = 10000,
                OwnedZoneIds = new List<string>()
            });
            var filePath = GetTestFilePath("large_values.json");

            // Act
            _persistenceService.Save(gameState, filePath);
            var loadedState = _persistenceService.Load(filePath);

            // Assert
            var state = loadedState.FactionStates[0];
            Assert.Equal(int.MaxValue, state.Cash);
            Assert.Equal(999999, state.Weapons);
            Assert.Equal(500000, state.RecruitmentPoints);
            Assert.Equal(10000, state.TroopCount);
        }

        [Fact]
        public void Integration_SaveLoad_ZeroControlPercentage_Preserved()
        {
            // Arrange
            var gameState = new GameState { SaveName = "Zero Control" };
            gameState.Zones.Add(new ZoneData
            {
                Id = "zone-1",
                Name = "Lost Zone",
                OwnerFactionId = MichaelFactionId,
                ControlPercentage = 0f,
                IsContested = true
            });
            var filePath = GetTestFilePath("zero_control.json");

            // Act
            _persistenceService.Save(gameState, filePath);
            var loadedState = _persistenceService.Load(filePath);

            // Assert
            Assert.Equal(0f, loadedState.Zones[0].ControlPercentage);
        }

        [Fact]
        public void Integration_SaveLoad_ExtremeRelationshipValues()
        {
            // Arrange
            var gameState = new GameState { SaveName = "Extreme Relationships" };
            gameState.Relationships.Add(new RelationshipData
            {
                FactionId1 = MichaelFactionId,
                FactionId2 = TrevorFactionId,
                Value = -100 // Maximum hostility
            });
            gameState.Relationships.Add(new RelationshipData
            {
                FactionId1 = MichaelFactionId,
                FactionId2 = FranklinFactionId,
                Value = 100 // Maximum alliance
            });
            var filePath = GetTestFilePath("extreme_relations.json");

            // Act
            _persistenceService.Save(gameState, filePath);
            var loadedState = _persistenceService.Load(filePath);

            // Assert
            Assert.Equal(-100, loadedState.Relationships[0].Value);
            Assert.Equal(100, loadedState.Relationships[1].Value);
        }

        [Fact]
        public void Integration_SaveLoad_VeryLongPlayTime_Preserved()
        {
            // Arrange: 1000 hours of playtime
            var gameState = new GameState
            {
                SaveName = "Veteran Save",
                TotalPlayTimeSeconds = 3600000
            };
            var filePath = GetTestFilePath("long_play.json");

            // Act
            _persistenceService.Save(gameState, filePath);
            var loadedState = _persistenceService.Load(filePath);

            // Assert
            Assert.Equal(3600000, loadedState.TotalPlayTimeSeconds);
        }

        #endregion

        #region Save/Load Integrity Tests

        [Fact]
        public void Integrity_SaveLoadCycle_PreservesReservePoolByTier()
        {
            // Arrange: Create world with specific reserve pool values
            var world = CreateCompleteGameWorld();

            // After consolidation, initialTroopCount in CreateCompleteGameWorld adds to Basic tier
            // Michael starts with 30 Basic, Trevor with 25 Basic, Franklin with 20 Basic
            // Add more troops to reserve pool by tier
            var michaelState = world.FactionService.GetFactionState(MichaelFactionId)!;
            michaelState.AddReserveTroops(DefenderTier.Basic, 70);   // 30 + 70 = 100
            michaelState.AddReserveTroops(DefenderTier.Medium, 50);
            michaelState.AddReserveTroops(DefenderTier.Heavy, 25);

            var trevorState = world.FactionService.GetFactionState(TrevorFactionId)!;
            trevorState.AddReserveTroops(DefenderTier.Basic, 55);    // 25 + 55 = 80
            trevorState.AddReserveTroops(DefenderTier.Medium, 40);
            trevorState.AddReserveTroops(DefenderTier.Heavy, 20);

            // Act: Save and restore
            var gameState = CreateGameStateSnapshot(world);
            var filePath = GetTestFilePath("reserve_pool.json");
            _persistenceService.Save(gameState, filePath);
            var loadedState = _persistenceService.Load(filePath);
            var restoredWorld = RestoreGameWorld(loadedState);

            // Assert: Reserve pool is preserved by tier
            var restoredMichaelState = restoredWorld.FactionService.GetFactionState(MichaelFactionId)!;
            Assert.Equal(100, restoredMichaelState.GetReserveTroops(DefenderTier.Basic));
            Assert.Equal(50, restoredMichaelState.GetReserveTroops(DefenderTier.Medium));
            Assert.Equal(25, restoredMichaelState.GetReserveTroops(DefenderTier.Heavy));

            var restoredTrevorState = restoredWorld.FactionService.GetFactionState(TrevorFactionId)!;
            Assert.Equal(80, restoredTrevorState.GetReserveTroops(DefenderTier.Basic));
            Assert.Equal(40, restoredTrevorState.GetReserveTroops(DefenderTier.Medium));
            Assert.Equal(20, restoredTrevorState.GetReserveTroops(DefenderTier.Heavy));
        }

        [Fact]
        public void Integrity_SaveLoadCycle_EmptyReservePool_PreservesZeroValues()
        {
            // Arrange: Create world with empty reserve pools (Medium/Heavy only)
            var world = CreateCompleteGameWorld();
            // After consolidation, initialTroopCount goes to Basic tier
            // CreateCompleteGameWorld initializes Michael with 30 Basic
            // So we test that Medium and Heavy (which are 0) are preserved

            // Act: Save and restore
            var gameState = CreateGameStateSnapshot(world);
            var filePath = GetTestFilePath("empty_reserve.json");
            _persistenceService.Save(gameState, filePath);
            var loadedState = _persistenceService.Load(filePath);
            var restoredWorld = RestoreGameWorld(loadedState);

            // Assert: Zero values for Medium and Heavy are preserved
            var restoredMichaelState = restoredWorld.FactionService.GetFactionState(MichaelFactionId)!;
            Assert.Equal(30, restoredMichaelState.GetReserveTroops(DefenderTier.Basic)); // From initialTroopCount
            Assert.Equal(0, restoredMichaelState.GetReserveTroops(DefenderTier.Medium));
            Assert.Equal(0, restoredMichaelState.GetReserveTroops(DefenderTier.Heavy));
        }

        [Fact]
        public void Integrity_CorruptedJsonFile_ThrowsInvalidOperationException()
        {
            // Arrange: Write corrupted JSON to file
            var filePath = GetTestFilePath("corrupted.json");
            File.WriteAllText(filePath, "{ invalid json content not closed properly");

            // Act & Assert: Should throw InvalidOperationException
            Assert.Throws<InvalidOperationException>(() => _persistenceService.Load(filePath));
        }

        [Fact]
        public void Integrity_TruncatedJsonFile_ThrowsInvalidOperationException()
        {
            // Arrange: Write truncated JSON
            var filePath = GetTestFilePath("truncated.json");
            File.WriteAllText(filePath, "{\"Version\":1,\"SaveName\":\"Test\",\"Factions\":[{\"Id\":\"fac");

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => _persistenceService.Load(filePath));
        }

        [Fact]
        public void Integrity_EmptyFile_ThrowsInvalidOperationException()
        {
            // Arrange: Write empty file
            var filePath = GetTestFilePath("empty.json");
            File.WriteAllText(filePath, "");

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => _persistenceService.Load(filePath));
        }

        [Fact]
        public void Integrity_NonexistentFile_ThrowsFileNotFoundException()
        {
            // Arrange
            var filePath = GetTestFilePath("nonexistent_file.json");

            // Act & Assert
            Assert.Throws<FileNotFoundException>(() => _persistenceService.Load(filePath));
        }

        [Fact]
        public void Integrity_MultipleSaveLoadCycles_MaintainsDataConsistency()
        {
            // Arrange: Create initial state
            var world = CreateCompleteGameWorld();
            var michaelState = world.FactionService.GetFactionState(MichaelFactionId)!;
            michaelState.AddReserveTroops(DefenderTier.Heavy, 50);
            michaelState.Cash = 99999;

            var filePath = GetTestFilePath("multi_cycle.json");

            // Act: Perform 5 save/load cycles
            for (int cycle = 0; cycle < 5; cycle++)
            {
                var gameState = CreateGameStateSnapshot(world);
                _persistenceService.Save(gameState, filePath);
                var loadedState = _persistenceService.Load(filePath);
                world = RestoreGameWorld(loadedState);
            }

            // Assert: Data is still consistent after multiple cycles
            var finalMichaelState = world.FactionService.GetFactionState(MichaelFactionId)!;
            Assert.Equal(50, finalMichaelState.GetReserveTroops(DefenderTier.Heavy));
            Assert.Equal(99999, finalMichaelState.Cash);
            Assert.Equal(2, world.FactionService.GetZoneCount(MichaelFactionId));
        }

        [Fact]
        public void Integrity_ReferentialConsistency_ZoneOwnerMatchesFactionOwnedZones()
        {
            // Arrange: Create world and save
            var world = CreateCompleteGameWorld();
            var gameState = CreateGameStateSnapshot(world);
            var filePath = GetTestFilePath("referential.json");
            _persistenceService.Save(gameState, filePath);
            var loadedState = _persistenceService.Load(filePath);

            // Act & Assert: For each zone with an owner, the owner's FactionState should include that zone
            foreach (var zoneData in loadedState.Zones)
            {
                if (zoneData.OwnerFactionId != null)
                {
                    var ownerState = loadedState.FactionStates.Find(fs => fs.FactionId == zoneData.OwnerFactionId);
                    Assert.NotNull(ownerState);
                    Assert.Contains(zoneData.Id, ownerState.OwnedZoneIds);
                }
            }

            // And vice versa: each zone in FactionState.OwnedZoneIds should have that faction as owner
            foreach (var factionState in loadedState.FactionStates)
            {
                foreach (var zoneId in factionState.OwnedZoneIds)
                {
                    var zone = loadedState.Zones.Find(z => z.Id == zoneId);
                    Assert.NotNull(zone);
                    Assert.Equal(factionState.FactionId, zone.OwnerFactionId);
                }
            }
        }

        [Fact]
        public void Integrity_ValidatorCatchesReferentialInconsistency_FactionStateReferencesNonexistentZone()
        {
            // Arrange: Create game state with inconsistent data
            var gameState = new GameState { SaveName = "Inconsistent" };
            gameState.Factions.Add(new FactionData { Id = "faction-1", Name = "Test Faction" });
            gameState.FactionStates.Add(new FactionStateData
            {
                FactionId = "faction-1",
                Cash = 1000,
                OwnedZoneIds = new List<string> { "zone-that-does-not-exist" }
            });
            gameState.Zones.Add(new ZoneData
            {
                Id = "zone-1",
                Name = "Real Zone",
                OwnerFactionId = "faction-1"
            });

            var validator = new SaveFileValidator();

            // Act
            var result = validator.Validate(gameState);

            // Assert: Validator catches the inconsistency
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Contains("zone") && e.Contains("does not exist"));
        }

        [Fact]
        public void Integrity_SaveLoadWithValidation_RejectsInvalidGameState()
        {
            // Arrange: Create an invalid game state
            var invalidState = new GameState
            {
                Version = -1, // Invalid
                SaveName = "Invalid",
                TotalPlayTimeSeconds = -100 // Invalid
            };
            var filePath = GetTestFilePath("invalid_state.json");

            // Save it anyway
            _persistenceService.Save(invalidState, filePath);

            // Load and validate
            var loadedState = _persistenceService.Load(filePath);
            var validator = new SaveFileValidator();
            var result = validator.Validate(loadedState);

            // Assert: Validation fails
            Assert.False(result.IsValid);
            Assert.True(result.Errors.Count >= 2);
        }

        [Fact]
        public void Integrity_PreservesSpecialFloatValues_ControlPercentageBoundaries()
        {
            // Arrange: Test boundary values
            var gameState = new GameState { SaveName = "Boundaries" };
            gameState.Factions.Add(new FactionData { Id = "faction-1", Name = "Test" });
            gameState.Zones.Add(new ZoneData
            {
                Id = "zone-0",
                Name = "Zero Control",
                OwnerFactionId = "faction-1",
                ControlPercentage = 0f
            });
            gameState.Zones.Add(new ZoneData
            {
                Id = "zone-100",
                Name = "Full Control",
                OwnerFactionId = "faction-1",
                ControlPercentage = 100f
            });
            gameState.Zones.Add(new ZoneData
            {
                Id = "zone-partial",
                Name = "Partial Control",
                OwnerFactionId = "faction-1",
                ControlPercentage = 33.333f
            });

            var filePath = GetTestFilePath("boundaries.json");

            // Act
            _persistenceService.Save(gameState, filePath);
            var loadedState = _persistenceService.Load(filePath);

            // Assert
            Assert.Equal(0f, loadedState.Zones.Find(z => z.Id == "zone-0")!.ControlPercentage);
            Assert.Equal(100f, loadedState.Zones.Find(z => z.Id == "zone-100")!.ControlPercentage);
            Assert.Equal(33.333f, loadedState.Zones.Find(z => z.Id == "zone-partial")!.ControlPercentage, precision: 3);
        }

        [Fact]
        public void Integrity_PreservesUnicodeCharacters_InSaveNameAndFactionNames()
        {
            // Arrange: Unicode characters in names
            var gameState = new GameState { SaveName = "保存游戏 - Campaign" };
            gameState.Factions.Add(new FactionData
            {
                Id = "faction-unicode",
                Name = "Команда Михаила", // Russian
                Leader = "ミカエル" // Japanese
            });

            var filePath = GetTestFilePath("unicode.json");

            // Act
            _persistenceService.Save(gameState, filePath);
            var loadedState = _persistenceService.Load(filePath);

            // Assert
            Assert.Equal("保存游戏 - Campaign", loadedState.SaveName);
            Assert.Equal("Команда Михаила", loadedState.Factions[0].Name);
            Assert.Equal("ミカエル", loadedState.Factions[0].Leader);
        }

        [Fact]
        public void Integrity_PreservesDateTimePrecision()
        {
            // Arrange: Specific timestamp
            var specificTime = new DateTime(2025, 6, 15, 14, 30, 45, DateTimeKind.Utc);
            var gameState = new GameState
            {
                SaveName = "Timestamp Test",
                CreatedAt = specificTime,
                ModifiedAt = specificTime.AddHours(2)
            };

            var filePath = GetTestFilePath("timestamp.json");

            // Act
            _persistenceService.Save(gameState, filePath);
            var loadedState = _persistenceService.Load(filePath);

            // Assert: Timestamps preserved to second precision
            Assert.Equal(specificTime.Year, loadedState.CreatedAt.Year);
            Assert.Equal(specificTime.Month, loadedState.CreatedAt.Month);
            Assert.Equal(specificTime.Day, loadedState.CreatedAt.Day);
            Assert.Equal(specificTime.Hour, loadedState.CreatedAt.Hour);
            Assert.Equal(specificTime.Minute, loadedState.CreatedAt.Minute);
            Assert.Equal(specificTime.Second, loadedState.CreatedAt.Second);
        }

        [Fact]
        public void Integrity_AllDefenderTiers_SurviveSerializationRoundTrip()
        {
            // Arrange: Test all defender tiers in reserve pool
            var gameState = new GameState { SaveName = "All Tiers" };
            gameState.Factions.Add(new FactionData { Id = "faction-1", Name = "Test" });
            gameState.FactionStates.Add(new FactionStateData
            {
                FactionId = "faction-1",
                Cash = 5000,
                ReservePool = new Dictionary<DefenderTier, int>
                {
                    { DefenderTier.Basic, 111 },
                    { DefenderTier.Medium, 222 },
                    { DefenderTier.Heavy, 333 }
                }
            });

            var filePath = GetTestFilePath("all_tiers.json");

            // Act
            _persistenceService.Save(gameState, filePath);
            var loadedState = _persistenceService.Load(filePath);

            // Assert
            var loadedFactionState = loadedState.FactionStates[0];
            Assert.Equal(111, loadedFactionState.ReservePool[DefenderTier.Basic]);
            Assert.Equal(222, loadedFactionState.ReservePool[DefenderTier.Medium]);
            Assert.Equal(333, loadedFactionState.ReservePool[DefenderTier.Heavy]);
        }

        #endregion

        #region Save/Load + Victory Verification Tests

        [Fact]
        public void Integration_SaveLoadNearVictory_ThenCaptureLastZone_TriggersVictoryAt100Percent()
        {
            // Arrange: Create world where Michael controls all but one zone
            var world = CreateCompleteGameWorld();

            // Give Michael control of 5 of 6 zones, Franklin keeps the last one
            var franklinZones = world.ZoneRepo.GetAll()
                .Where(z => z.OwnerFactionId == FranklinFactionId)
                .ToList();
            var trevorZones = world.ZoneRepo.GetAll()
                .Where(z => z.OwnerFactionId == TrevorFactionId)
                .ToList();

            // Transfer Trevor's zones to Michael
            foreach (var zone in trevorZones)
            {
                zone.OwnerFactionId = MichaelFactionId;
                world.ZoneRepo.Update(zone);
                world.FactionService.RemoveZoneFromFaction(TrevorFactionId, zone.Id);
                world.FactionService.AddZoneToFaction(MichaelFactionId, zone.Id);
            }

            // Transfer one of Franklin's zones to Michael (keep one for the last capture)
            if (franklinZones.Count > 1)
            {
                var transferZone = franklinZones[0];
                transferZone.OwnerFactionId = MichaelFactionId;
                world.ZoneRepo.Update(transferZone);
                world.FactionService.RemoveZoneFromFaction(FranklinFactionId, transferZone.Id);
                world.FactionService.AddZoneToFaction(MichaelFactionId, transferZone.Id);
            }

            // Verify pre-save state: Michael controls 5/6 zones
            var victoryService = new VictoryConditionService(world.ZoneService);
            float progressBeforeSave = victoryService.GetVictoryProgress(MichaelFactionId);
            Assert.True(progressBeforeSave > 80f && progressBeforeSave < 100f,
                $"Expected progress > 80% and < 100%, got {progressBeforeSave}%");
            Assert.False(victoryService.IsGameOver(), "Game should not be over yet");

            // Act 1: Save the near-victory state
            var gameState = CreateGameStateSnapshot(world);
            gameState.SaveName = "Near Victory Save";
            var filePath = GetTestFilePath("near_victory.json");
            _persistenceService.Save(gameState, filePath);

            // Act 2: Load the save
            var loadedState = _persistenceService.Load(filePath);
            var restoredWorld = RestoreGameWorld(loadedState);
            var restoredVictoryService = new VictoryConditionService(restoredWorld.ZoneService);

            // Verify restored progress matches original
            float progressAfterLoad = restoredVictoryService.GetVictoryProgress(MichaelFactionId);
            Assert.Equal(progressBeforeSave, progressAfterLoad, precision: 2);
            Assert.False(restoredVictoryService.IsGameOver(), "Game should still not be over after load");

            // Act 3: Capture the last remaining zone (Franklin's last zone)
            var lastFranklinZone = restoredWorld.ZoneRepo.GetAll()
                .First(z => z.OwnerFactionId == FranklinFactionId);

            var encounter = new CombatEncounter("final-combat", lastFranklinZone.Id, MichaelFactionId, FranklinFactionId);
            encounter.AttackerPedCount = 25;
            encounter.DefenderPedCount = 0;

            var controlCalc = new ControlPercentageCalculator();
            controlCalc.ApplyToEncounter(encounter);
            encounter.End(CombatStatus.AttackerVictory);
            restoredWorld.CombatHandler.ProcessCombatResult(encounter);

            // Zone is now neutral after victory - Michael claims it
            var neutralZone = restoredWorld.ZoneRepo.GetById(lastFranklinZone.Id)!;
            neutralZone.OwnerFactionId = MichaelFactionId;
            neutralZone.ControlPercentage = 100f;
            restoredWorld.ZoneRepo.Update(neutralZone);

            // Update faction tracking
            restoredWorld.FactionService.RemoveZoneFromFaction(FranklinFactionId, lastFranklinZone.Id);
            restoredWorld.FactionService.AddZoneToFaction(MichaelFactionId, lastFranklinZone.Id);

            // Assert: Victory condition triggers at 100%
            float finalProgress = restoredVictoryService.GetVictoryProgress(MichaelFactionId);
            Assert.Equal(100f, finalProgress);

            var victoryResult = restoredVictoryService.CheckVictoryCondition(MichaelFactionId);
            Assert.True(victoryResult.IsVictory, "Should be victory when controlling 100% of zones");
            Assert.Equal(MichaelFactionId, victoryResult.FactionId);
            Assert.Equal(restoredVictoryService.GetTotalZoneCount(), victoryResult.ZonesOwned);

            Assert.True(restoredVictoryService.IsGameOver(), "Game should be over");
            Assert.Equal(MichaelFactionId, restoredVictoryService.GetWinningFactionId());
        }

        [Fact]
        public void Integration_SaveLoadAfterVictory_PreservesVictoryState()
        {
            // Arrange: Create world where Michael has already won
            var world = CreateCompleteGameWorld();

            // Transfer all zones to Michael
            var allZones = world.ZoneRepo.GetAll().ToList();
            foreach (var zone in allZones)
            {
                if (zone.OwnerFactionId != MichaelFactionId)
                {
                    var previousOwner = zone.OwnerFactionId;
                    zone.OwnerFactionId = MichaelFactionId;
                    world.ZoneRepo.Update(zone);
                    if (previousOwner != null)
                    {
                        world.FactionService.RemoveZoneFromFaction(previousOwner, zone.Id);
                    }
                    world.FactionService.AddZoneToFaction(MichaelFactionId, zone.Id);
                }
            }

            // Verify victory state before save
            var victoryService = new VictoryConditionService(world.ZoneService);
            Assert.True(victoryService.IsGameOver());
            Assert.Equal(MichaelFactionId, victoryService.GetWinningFactionId());
            Assert.Equal(100f, victoryService.GetVictoryProgress(MichaelFactionId));

            // Act: Save and load
            var gameState = CreateGameStateSnapshot(world);
            gameState.SaveName = "Victory Save";
            var filePath = GetTestFilePath("victory.json");
            _persistenceService.Save(gameState, filePath);
            var loadedState = _persistenceService.Load(filePath);
            var restoredWorld = RestoreGameWorld(loadedState);

            // Assert: Victory state is preserved
            var restoredVictoryService = new VictoryConditionService(restoredWorld.ZoneService);
            Assert.True(restoredVictoryService.IsGameOver(), "Victory state should be preserved after load");
            Assert.Equal(MichaelFactionId, restoredVictoryService.GetWinningFactionId());
            Assert.Equal(100f, restoredVictoryService.GetVictoryProgress(MichaelFactionId));

            var victoryResult = restoredVictoryService.CheckVictoryCondition(MichaelFactionId);
            Assert.True(victoryResult.IsVictory);
            Assert.Equal(6, victoryResult.ZonesOwned); // All 6 zones
            Assert.Equal(6, victoryResult.TotalZones);
        }

        #endregion

        #region AI State Preservation Tests

        [Fact]
        public void Integration_SaveLoad_AICanContinueAfterRestore()
        {
            // Arrange: Create world, make AI decisions, save
            var world = CreateCompleteGameWorld();

            // Run economy to give factions resources
            world.TickService.Start();
            world.TickService.ForceTick();

            // Get AI decision before save
            var michaelStrategy = new MichaelAIStrategy();
            var michaelState = world.FactionService.GetFactionState(MichaelFactionId)!;
            var michaelFaction = world.FactionRepo.GetById(MichaelFactionId)!;
            var allZones = world.ZoneRepo.GetAll().ToList();
            var michaelZones = allZones.Where(z => z.OwnerFactionId == MichaelFactionId).ToList();
            var otherFactions = world.FactionRepo.GetAll()
                .Where(f => f.Id != MichaelFactionId)
                .ToList();

            var contextBefore = new AIContext(
                michaelFaction,
                michaelState,
                michaelZones,
                allZones,
                otherFactions);

            var decisionsBefore = michaelStrategy.MakeDecisions(contextBefore);

            // Act: Save and restore
            var gameState = CreateGameStateSnapshot(world);
            var filePath = GetTestFilePath("ai_continuation.json");
            _persistenceService.Save(gameState, filePath);
            var loadedState = _persistenceService.Load(filePath);
            var restoredWorld = RestoreGameWorld(loadedState);

            // Create AI context from restored state
            var restoredMichaelState = restoredWorld.FactionService.GetFactionState(MichaelFactionId)!;
            var restoredMichaelFaction = restoredWorld.FactionRepo.GetById(MichaelFactionId)!;
            var restoredAllZones = restoredWorld.ZoneRepo.GetAll().ToList();
            var restoredMichaelZones = restoredAllZones.Where(z => z.OwnerFactionId == MichaelFactionId).ToList();
            var restoredOtherFactions = restoredWorld.FactionRepo.GetAll()
                .Where(f => f.Id != MichaelFactionId)
                .ToList();

            var contextAfter = new AIContext(
                restoredMichaelFaction,
                restoredMichaelState,
                restoredMichaelZones,
                restoredAllZones,
                restoredOtherFactions);

            var decisionsAfter = michaelStrategy.MakeDecisions(contextAfter);

            // Assert: AI can still make decisions (state is consistent)
            Assert.NotNull(decisionsAfter);
            Assert.Equal(decisionsBefore.Count, decisionsAfter.Count);
        }

        #endregion

        #region Performance Tests

        [Fact]
        public void Integration_SaveLoad_LargeGameState_CompletesSuccessfully()
        {
            // Arrange: Create a large game state (many zones)
            var gameState = new GameState { SaveName = "Large World" };

            // Add many factions
            for (int f = 0; f < 10; f++)
            {
                gameState.Factions.Add(new FactionData
                {
                    Id = $"faction-{f}",
                    Name = $"Faction {f}",
                    IsActive = true
                });
                gameState.FactionStates.Add(new FactionStateData
                {
                    FactionId = $"faction-{f}",
                    Cash = 10000 * f,
                    OwnedZoneIds = new List<string>()
                });
            }

            // Add many zones
            for (int z = 0; z < 50; z++)
            {
                var zoneData = new ZoneData
                {
                    Id = $"zone-{z}",
                    Name = $"Zone {z}",
                    OwnerFactionId = $"faction-{z % 10}",
                    ControlPercentage = 100f,
                    Traits = (ZoneTrait)(z % 7)
                };
                gameState.Zones.Add(zoneData);
                gameState.FactionStates[z % 10].OwnedZoneIds.Add(zoneData.Id);
            }

            var filePath = GetTestFilePath("large_world.json");

            // Act
            _persistenceService.Save(gameState, filePath);
            var loadedState = _persistenceService.Load(filePath);

            // Assert
            Assert.Equal(10, loadedState.Factions.Count);
            Assert.Equal(50, loadedState.Zones.Count);
        }

        #endregion

        #region Helper Methods

        private GameWorld CreateCompleteGameWorld()
        {
            var zoneRepo = new InMemoryZoneRepository();
            var zoneService = new ZoneService(zoneRepo);
            var factionRepo = new InMemoryFactionRepository();
            var factionService = new FactionService(factionRepo);
            var combatHandler = new CombatResultHandler(zoneService);
            var resourceModifier = new ZoneTraitResourceModifier();
            var supplyLineService = new SupplyLineService(zoneService);
            var tickService = new ResourceTickService(factionService, zoneService, resourceModifier, supplyLineService, 300);

            // Create factions
            var michael = new Faction(MichaelFactionId, "Michael's Crew", "Michael", "Professional crew",
                new FactionColor(0, 100, 255));
            var trevor = new Faction(TrevorFactionId, "Trevor's Gang", "Trevor", "Chaotic crew",
                new FactionColor(255, 100, 0));
            var franklin = new Faction(FranklinFactionId, "Franklin's Family", "Franklin", "Street crew",
                new FactionColor(0, 200, 0));

            factionRepo.Add(michael);
            factionRepo.Add(trevor);
            factionRepo.Add(franklin);

            factionService.InitializeFactionState(MichaelFactionId, 10000, 30);
            factionService.InitializeFactionState(TrevorFactionId, 8000, 25);
            factionService.InitializeFactionState(FranklinFactionId, 6000, 20);

            // Create zones
            var zones = new[]
            {
                new Zone("zone-downtown", "Downtown", new Vector3(0, 0, 0), 200f, 8),
                new Zone("zone-vinewood", "Vinewood", new Vector3(500, 200, 0), 150f, 7),
                new Zone("zone-beach", "Beach", new Vector3(-200, 400, 0), 175f, 5),
                new Zone("zone-sandy", "Sandy Shores", new Vector3(-800, 800, 0), 200f, 4),
                new Zone("zone-grove", "Grove Street", new Vector3(-300, -300, 0), 125f, 6),
                new Zone("zone-airport", "Airport", new Vector3(100, -500, 0), 250f, 9)
            };

            zones[0].OwnerFactionId = MichaelFactionId;
            zones[0].Traits = ZoneTrait.Commercial;
            zones[1].OwnerFactionId = MichaelFactionId;
            zones[1].Traits = ZoneTrait.Residential;
            zones[2].OwnerFactionId = TrevorFactionId;
            zones[2].Traits = ZoneTrait.Port;
            zones[3].OwnerFactionId = TrevorFactionId;
            zones[3].Traits = ZoneTrait.Industrial;
            zones[4].OwnerFactionId = FranklinFactionId;
            zones[4].Traits = ZoneTrait.Residential;
            zones[5].OwnerFactionId = FranklinFactionId;
            zones[5].Traits = ZoneTrait.HighValue;

            foreach (var zone in zones)
            {
                zone.ControlPercentage = 100f;
                zoneRepo.Add(zone);
            }

            factionService.AddZoneToFaction(MichaelFactionId, "zone-downtown");
            factionService.AddZoneToFaction(MichaelFactionId, "zone-vinewood");
            factionService.AddZoneToFaction(TrevorFactionId, "zone-beach");
            factionService.AddZoneToFaction(TrevorFactionId, "zone-sandy");
            factionService.AddZoneToFaction(FranklinFactionId, "zone-grove");
            factionService.AddZoneToFaction(FranklinFactionId, "zone-airport");

            return new GameWorld
            {
                ZoneRepo = zoneRepo,
                ZoneService = zoneService,
                FactionRepo = factionRepo,
                FactionService = factionService,
                CombatHandler = combatHandler,
                TickService = tickService
            };
        }

        private GameState CreateGameStateSnapshot(GameWorld world)
        {
            var factions = world.FactionRepo.GetAll().ToList();
            var factionStates = factions
                .Select(f => world.FactionService.GetFactionState(f.Id))
                .Where(s => s != null)
                .ToList();
            var zones = world.ZoneRepo.GetAll().ToList();

            return GameState.CreateSnapshot(factions, factionStates!, zones, Enumerable.Empty<FactionRelationship>());
        }

        private GameWorld RestoreGameWorld(GameState gameState)
        {
            var zoneRepo = new InMemoryZoneRepository();
            var zoneService = new ZoneService(zoneRepo);
            var factionRepo = new InMemoryFactionRepository();
            var factionService = new FactionService(factionRepo);
            var combatHandler = new CombatResultHandler(zoneService);
            var resourceModifier = new ZoneTraitResourceModifier();
            var supplyLineService = new SupplyLineService(zoneService);
            var tickService = new ResourceTickService(factionService, zoneService, resourceModifier, supplyLineService, 300);

            // Restore factions
            foreach (var factionData in gameState.Factions)
            {
                factionRepo.Add(factionData.ToFaction());
            }

            // Restore faction states
            foreach (var stateData in gameState.FactionStates)
            {
                // After consolidation, TroopCount is computed from reserve pool
                // So we pass 0 for initialTroops and restore reserve pool separately
                factionService.InitializeFactionState(stateData.FactionId, stateData.Cash, 0);
                var state = factionService.GetFactionState(stateData.FactionId);
                if (state != null)
                {
                    state.Weapons = stateData.Weapons;
                    state.RecruitmentPoints = stateData.RecruitmentPoints;
                    foreach (var zoneId in stateData.OwnedZoneIds)
                    {
                        state.AddZone(zoneId);
                    }
                    // Restore reserve pool by tier (this is the source of truth for TroopCount)
                    foreach (var kvp in stateData.ReservePool)
                    {
                        state.AddReserveTroops(kvp.Key, kvp.Value);
                    }
                }
            }

            // Restore zones
            foreach (var zoneData in gameState.Zones)
            {
                zoneRepo.Add(zoneData.ToZone());
            }

            return new GameWorld
            {
                ZoneRepo = zoneRepo,
                ZoneService = zoneService,
                FactionRepo = factionRepo,
                FactionService = factionService,
                CombatHandler = combatHandler,
                TickService = tickService
            };
        }

        private void AssertZonesEqual(IZoneRepository expected, IZoneRepository actual)
        {
            var expectedZones = expected.GetAll().OrderBy(z => z.Id).ToList();
            var actualZones = actual.GetAll().OrderBy(z => z.Id).ToList();

            Assert.Equal(expectedZones.Count, actualZones.Count);

            for (int i = 0; i < expectedZones.Count; i++)
            {
                Assert.Equal(expectedZones[i].Id, actualZones[i].Id);
                Assert.Equal(expectedZones[i].Name, actualZones[i].Name);
                Assert.Equal(expectedZones[i].OwnerFactionId, actualZones[i].OwnerFactionId);
                Assert.Equal(expectedZones[i].ControlPercentage, actualZones[i].ControlPercentage);
                Assert.Equal(expectedZones[i].IsContested, actualZones[i].IsContested);
                Assert.Equal(expectedZones[i].Traits, actualZones[i].Traits);
                Assert.Equal(expectedZones[i].StrategicValue, actualZones[i].StrategicValue);
            }
        }

        private void AssertFactionsEqual(IFactionRepository expected, IFactionRepository actual)
        {
            var expectedFactions = expected.GetAll().OrderBy(f => f.Id).ToList();
            var actualFactions = actual.GetAll().OrderBy(f => f.Id).ToList();

            Assert.Equal(expectedFactions.Count, actualFactions.Count);

            for (int i = 0; i < expectedFactions.Count; i++)
            {
                Assert.Equal(expectedFactions[i].Id, actualFactions[i].Id);
                Assert.Equal(expectedFactions[i].Name, actualFactions[i].Name);
                Assert.Equal(expectedFactions[i].Leader, actualFactions[i].Leader);
                Assert.Equal(expectedFactions[i].IsActive, actualFactions[i].IsActive);
            }
        }

        private void AssertFactionStatesEqual(IFactionService expected, IFactionService actual)
        {
            var factionIds = new[] { MichaelFactionId, TrevorFactionId, FranklinFactionId };

            foreach (var id in factionIds)
            {
                var expectedState = expected.GetFactionState(id);
                var actualState = actual.GetFactionState(id);

                if (expectedState != null)
                {
                    Assert.NotNull(actualState);
                    Assert.Equal(expectedState.Cash, actualState.Cash);
                    Assert.Equal(expectedState.Weapons, actualState.Weapons);
                    Assert.Equal(expectedState.RecruitmentPoints, actualState.RecruitmentPoints);
                    Assert.Equal(expectedState.TroopCount, actualState.TroopCount);
                }
            }
        }

        #endregion

        #region Helper Classes

        private class GameWorld
        {
            public InMemoryZoneRepository ZoneRepo { get; set; } = null!;
            public ZoneService ZoneService { get; set; } = null!;
            public InMemoryFactionRepository FactionRepo { get; set; } = null!;
            public FactionService FactionService { get; set; } = null!;
            public CombatResultHandler CombatHandler { get; set; } = null!;
            public ResourceTickService TickService { get; set; } = null!;
        }

        private class TestGameStateProvider : IGameStateProvider
        {
            private readonly GameWorld _world;

            public TestGameStateProvider(GameWorld world)
            {
                _world = world;
            }

            public GameState? GetCurrentGameState()
            {
                var factions = _world.FactionRepo.GetAll().ToList();
                var factionStates = factions
                    .Select(f => _world.FactionService.GetFactionState(f.Id))
                    .Where(s => s != null)
                    .ToList();
                var zones = _world.ZoneRepo.GetAll().ToList();

                return GameState.CreateSnapshot(factions, factionStates!, zones, Enumerable.Empty<FactionRelationship>());
            }
        }

        #endregion
    }
}
