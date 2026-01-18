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
            AssertRelationshipsEqual(originalWorld.RelationshipRepo, restoredWorld.RelationshipRepo);
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

        [Fact]
        public void Integration_SaveLoadCycle_PreservesFactionRelationships()
        {
            // Arrange: Create world with various relationship states
            var world = CreateCompleteGameWorld();

            world.RelationshipService.DeclareWar(MichaelFactionId, TrevorFactionId);
            world.RelationshipService.FormAlliance(MichaelFactionId, FranklinFactionId);
            world.RelationshipService.SetRelationshipValue(TrevorFactionId, FranklinFactionId, -25);

            // Act: Save and restore
            var gameState = CreateGameStateSnapshot(world);
            var filePath = GetTestFilePath("relationships.json");
            _persistenceService.Save(gameState, filePath);
            var loadedState = _persistenceService.Load(filePath);
            var restoredWorld = RestoreGameWorld(loadedState);

            // Assert: Relationships are preserved
            Assert.True(restoredWorld.RelationshipService.AreAtWar(MichaelFactionId, TrevorFactionId));
            Assert.True(restoredWorld.RelationshipService.AreAllied(MichaelFactionId, FranklinFactionId));
            Assert.Equal(-25, restoredWorld.RelationshipService.GetRelationshipValue(TrevorFactionId, FranklinFactionId));
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
            // Arrange: Create a large game state (many zones, many relationships)
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

            // Add many relationships
            for (int i = 0; i < 10; i++)
            {
                for (int j = i + 1; j < 10; j++)
                {
                    gameState.Relationships.Add(new RelationshipData
                    {
                        FactionId1 = $"faction-{i}",
                        FactionId2 = $"faction-{j}",
                        Value = (i + j) % 201 - 100 // -100 to 100
                    });
                }
            }

            var filePath = GetTestFilePath("large_world.json");

            // Act
            _persistenceService.Save(gameState, filePath);
            var loadedState = _persistenceService.Load(filePath);

            // Assert
            Assert.Equal(10, loadedState.Factions.Count);
            Assert.Equal(50, loadedState.Zones.Count);
            Assert.Equal(45, loadedState.Relationships.Count); // 10 choose 2 = 45
        }

        #endregion

        #region Helper Methods

        private GameWorld CreateCompleteGameWorld()
        {
            var zoneRepo = new InMemoryZoneRepository();
            var zoneService = new ZoneService(zoneRepo);
            var factionRepo = new InMemoryFactionRepository();
            var factionService = new FactionService(factionRepo);
            var relationshipRepo = new InMemoryFactionRelationshipRepository();
            var relationshipService = new FactionRelationshipService(factionRepo, relationshipRepo);
            var combatHandler = new CombatResultHandler(zoneService);
            var resourceModifier = new ZoneTraitResourceModifier();
            var tickService = new ResourceTickService(factionService, zoneService, resourceModifier, 300);

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
                RelationshipRepo = relationshipRepo,
                RelationshipService = relationshipService,
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
            var relationships = world.RelationshipRepo.GetAll().ToList();

            return GameState.CreateSnapshot(factions, factionStates!, zones, relationships);
        }

        private GameWorld RestoreGameWorld(GameState gameState)
        {
            var zoneRepo = new InMemoryZoneRepository();
            var zoneService = new ZoneService(zoneRepo);
            var factionRepo = new InMemoryFactionRepository();
            var factionService = new FactionService(factionRepo);
            var relationshipRepo = new InMemoryFactionRelationshipRepository();
            var relationshipService = new FactionRelationshipService(factionRepo, relationshipRepo);
            var combatHandler = new CombatResultHandler(zoneService);
            var resourceModifier = new ZoneTraitResourceModifier();
            var tickService = new ResourceTickService(factionService, zoneService, resourceModifier, 300);

            // Restore factions
            foreach (var factionData in gameState.Factions)
            {
                factionRepo.Add(factionData.ToFaction());
            }

            // Restore faction states
            foreach (var stateData in gameState.FactionStates)
            {
                factionService.InitializeFactionState(stateData.FactionId, stateData.Cash, stateData.TroopCount);
                var state = factionService.GetFactionState(stateData.FactionId);
                if (state != null)
                {
                    state.Weapons = stateData.Weapons;
                    state.RecruitmentPoints = stateData.RecruitmentPoints;
                    foreach (var zoneId in stateData.OwnedZoneIds)
                    {
                        state.AddZone(zoneId);
                    }
                }
            }

            // Restore zones
            foreach (var zoneData in gameState.Zones)
            {
                zoneRepo.Add(zoneData.ToZone());
            }

            // Restore relationships
            foreach (var relData in gameState.Relationships)
            {
                relationshipRepo.Add(relData.ToFactionRelationship());
            }

            return new GameWorld
            {
                ZoneRepo = zoneRepo,
                ZoneService = zoneService,
                FactionRepo = factionRepo,
                FactionService = factionService,
                RelationshipRepo = relationshipRepo,
                RelationshipService = relationshipService,
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

        private void AssertRelationshipsEqual(IFactionRelationshipRepository expected, IFactionRelationshipRepository actual)
        {
            var expectedRels = expected.GetAll().OrderBy(r => r.FactionId1 + r.FactionId2).ToList();
            var actualRels = actual.GetAll().OrderBy(r => r.FactionId1 + r.FactionId2).ToList();

            Assert.Equal(expectedRels.Count, actualRels.Count);

            for (int i = 0; i < expectedRels.Count; i++)
            {
                Assert.Equal(expectedRels[i].FactionId1, actualRels[i].FactionId1);
                Assert.Equal(expectedRels[i].FactionId2, actualRels[i].FactionId2);
                Assert.Equal(expectedRels[i].Value, actualRels[i].Value);
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
            public InMemoryFactionRelationshipRepository RelationshipRepo { get; set; } = null!;
            public FactionRelationshipService RelationshipService { get; set; } = null!;
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
                var relationships = _world.RelationshipRepo.GetAll().ToList();

                return GameState.CreateSnapshot(factions, factionStates!, zones, relationships);
            }
        }

        #endregion
    }
}
