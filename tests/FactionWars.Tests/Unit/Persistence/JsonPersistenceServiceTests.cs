using FactionWars.Core.Interfaces;
using FactionWars.Factions.Models;
using FactionWars.Persistence;
using FactionWars.Persistence.Models;
using FactionWars.Territory.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace FactionWars.Tests.Unit.Persistence
{
    public class JsonPersistenceServiceTests : IDisposable
    {
        private readonly string _testDirectory;
        private readonly IPersistenceService _service;

        public JsonPersistenceServiceTests()
        {
            _testDirectory = Path.Combine(Path.GetTempPath(), "FactionWarsTests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testDirectory);
            _service = new JsonPersistenceService();
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

        #region Save/Load Basic Operations

        [Fact]
        public void Save_ShouldCreateFile()
        {
            // Arrange
            var gameState = new GameState { SaveName = "Test Save" };
            var filePath = GetTestFilePath();

            // Act
            _service.Save(gameState, filePath);

            // Assert
            Assert.True(File.Exists(filePath));
        }

        [Fact]
        public void Load_ShouldReturnSavedGameState()
        {
            // Arrange
            var gameState = new GameState { SaveName = "My Test Save" };
            var filePath = GetTestFilePath();
            _service.Save(gameState, filePath);

            // Act
            var loaded = _service.Load(filePath);

            // Assert
            Assert.NotNull(loaded);
            Assert.Equal("My Test Save", loaded.SaveName);
        }

        [Fact]
        public void Load_NonExistentFile_ShouldThrowFileNotFoundException()
        {
            // Arrange
            var filePath = GetTestFilePath("nonexistent.json");

            // Act & Assert
            Assert.Throws<FileNotFoundException>(() => _service.Load(filePath));
        }

        [Fact]
        public void Exists_WhenFileExists_ShouldReturnTrue()
        {
            // Arrange
            var gameState = new GameState();
            var filePath = GetTestFilePath();
            _service.Save(gameState, filePath);

            // Act
            var exists = _service.Exists(filePath);

            // Assert
            Assert.True(exists);
        }

        [Fact]
        public void Exists_WhenFileDoesNotExist_ShouldReturnFalse()
        {
            // Arrange
            var filePath = GetTestFilePath("nonexistent.json");

            // Act
            var exists = _service.Exists(filePath);

            // Assert
            Assert.False(exists);
        }

        [Fact]
        public void Delete_ShouldRemoveFile()
        {
            // Arrange
            var gameState = new GameState();
            var filePath = GetTestFilePath();
            _service.Save(gameState, filePath);
            Assert.True(File.Exists(filePath));

            // Act
            _service.Delete(filePath);

            // Assert
            Assert.False(File.Exists(filePath));
        }

        [Fact]
        public void Delete_NonExistentFile_ShouldNotThrow()
        {
            // Arrange
            var filePath = GetTestFilePath("nonexistent.json");

            // Act & Assert - should not throw
            var exception = Record.Exception(() => _service.Delete(filePath));
            Assert.Null(exception);
        }

        #endregion

        #region Async Operations

        [Fact]
        public async Task SaveAsync_ShouldCreateFile()
        {
            // Arrange
            var gameState = new GameState { SaveName = "Async Save" };
            var filePath = GetTestFilePath();

            // Act
            await _service.SaveAsync(gameState, filePath);

            // Assert
            Assert.True(File.Exists(filePath));
        }

        [Fact]
        public async Task LoadAsync_ShouldReturnSavedGameState()
        {
            // Arrange
            var gameState = new GameState { SaveName = "Async Load Test" };
            var filePath = GetTestFilePath();
            await _service.SaveAsync(gameState, filePath);

            // Act
            var loaded = await _service.LoadAsync(filePath);

            // Assert
            Assert.NotNull(loaded);
            Assert.Equal("Async Load Test", loaded.SaveName);
        }

        [Fact]
        public async Task LoadAsync_NonExistentFile_ShouldThrowFileNotFoundException()
        {
            // Arrange
            var filePath = GetTestFilePath("nonexistent.json");

            // Act & Assert
            await Assert.ThrowsAsync<FileNotFoundException>(() => _service.LoadAsync(filePath));
        }

        #endregion

        #region Metadata Preservation

        [Fact]
        public void SaveLoad_ShouldPreserveVersion()
        {
            // Arrange
            var gameState = new GameState { Version = 2 };
            var filePath = GetTestFilePath();

            // Act
            _service.Save(gameState, filePath);
            var loaded = _service.Load(filePath);

            // Assert
            Assert.Equal(2, loaded.Version);
        }

        [Fact]
        public void SaveLoad_ShouldPreserveTimestamps()
        {
            // Arrange
            var gameState = new GameState();
            gameState.TotalPlayTimeSeconds = 7200;
            var createdAt = gameState.CreatedAt;
            var modifiedAt = gameState.ModifiedAt;
            var filePath = GetTestFilePath();

            // Act
            _service.Save(gameState, filePath);
            var loaded = _service.Load(filePath);

            // Assert
            Assert.Equal(createdAt, loaded.CreatedAt, TimeSpan.FromSeconds(1));
            Assert.Equal(modifiedAt, loaded.ModifiedAt, TimeSpan.FromSeconds(1));
            Assert.Equal(7200, loaded.TotalPlayTimeSeconds);
        }

        [Fact]
        public void SaveLoad_ShouldPreserveSaveName()
        {
            // Arrange
            var gameState = new GameState { SaveName = "Campaign Save #1" };
            var filePath = GetTestFilePath();

            // Act
            _service.Save(gameState, filePath);
            var loaded = _service.Load(filePath);

            // Assert
            Assert.Equal("Campaign Save #1", loaded.SaveName);
        }

        #endregion

        #region Faction Data Round Trip

        [Fact]
        public void SaveLoad_ShouldPreserveFactionData()
        {
            // Arrange
            var gameState = new GameState();
            gameState.Factions.Add(new FactionData
            {
                Id = "faction_michael",
                Name = "Michael's Crew",
                Leader = "Michael De Santa",
                Description = "A professional crew from Los Santos",
                ColorR = 0,
                ColorG = 100,
                ColorB = 255,
                ColorA = 200,
                IsActive = true
            });
            var filePath = GetTestFilePath();

            // Act
            _service.Save(gameState, filePath);
            var loaded = _service.Load(filePath);

            // Assert
            Assert.Single(loaded.Factions);
            var faction = loaded.Factions[0];
            Assert.Equal("faction_michael", faction.Id);
            Assert.Equal("Michael's Crew", faction.Name);
            Assert.Equal("Michael De Santa", faction.Leader);
            Assert.Equal("A professional crew from Los Santos", faction.Description);
            Assert.Equal(0, faction.ColorR);
            Assert.Equal(100, faction.ColorG);
            Assert.Equal(255, faction.ColorB);
            Assert.Equal(200, faction.ColorA);
            Assert.True(faction.IsActive);
        }

        [Fact]
        public void SaveLoad_ShouldPreserveMultipleFactions()
        {
            // Arrange
            var gameState = new GameState();
            gameState.Factions.Add(new FactionData { Id = "faction_michael", Name = "Michael's Crew" });
            gameState.Factions.Add(new FactionData { Id = "faction_trevor", Name = "Trevor's Gang" });
            gameState.Factions.Add(new FactionData { Id = "faction_franklin", Name = "Franklin's Family" });
            var filePath = GetTestFilePath();

            // Act
            _service.Save(gameState, filePath);
            var loaded = _service.Load(filePath);

            // Assert
            Assert.Equal(3, loaded.Factions.Count);
            Assert.Contains(loaded.Factions, f => f.Id == "faction_michael");
            Assert.Contains(loaded.Factions, f => f.Id == "faction_trevor");
            Assert.Contains(loaded.Factions, f => f.Id == "faction_franklin");
        }

        [Fact]
        public void SaveLoad_ShouldPreserveFactionWithNullLeader()
        {
            // Arrange
            var gameState = new GameState();
            gameState.Factions.Add(new FactionData
            {
                Id = "faction_neutral",
                Name = "Neutral Forces",
                Leader = null
            });
            var filePath = GetTestFilePath();

            // Act
            _service.Save(gameState, filePath);
            var loaded = _service.Load(filePath);

            // Assert
            Assert.Single(loaded.Factions);
            Assert.Null(loaded.Factions[0].Leader);
        }

        #endregion

        #region FactionState Data Round Trip

        [Fact]
        public void SaveLoad_ShouldPreserveFactionStateData()
        {
            // Arrange
            var gameState = new GameState();
            gameState.FactionStates.Add(new FactionStateData
            {
                FactionId = "faction_michael",
                Cash = 75000,
                RecruitmentPoints = 150,
                Weapons = 40,
                TroopCount = 50,
                OwnedZoneIds = new List<string> { "zone_downtown", "zone_vinewood", "zone_beach" }
            });
            var filePath = GetTestFilePath();

            // Act
            _service.Save(gameState, filePath);
            var loaded = _service.Load(filePath);

            // Assert
            Assert.Single(loaded.FactionStates);
            var state = loaded.FactionStates[0];
            Assert.Equal("faction_michael", state.FactionId);
            Assert.Equal(75000, state.Cash);
            Assert.Equal(150, state.RecruitmentPoints);
            Assert.Equal(40, state.Weapons);
            Assert.Equal(50, state.TroopCount);
            Assert.Equal(3, state.OwnedZoneIds.Count);
            Assert.Contains("zone_downtown", state.OwnedZoneIds);
            Assert.Contains("zone_vinewood", state.OwnedZoneIds);
            Assert.Contains("zone_beach", state.OwnedZoneIds);
        }

        [Fact]
        public void SaveLoad_ShouldPreserveFactionStateWithEmptyZones()
        {
            // Arrange
            var gameState = new GameState();
            gameState.FactionStates.Add(new FactionStateData
            {
                FactionId = "faction_new",
                Cash = 1000,
                OwnedZoneIds = new List<string>()
            });
            var filePath = GetTestFilePath();

            // Act
            _service.Save(gameState, filePath);
            var loaded = _service.Load(filePath);

            // Assert
            Assert.Single(loaded.FactionStates);
            Assert.Empty(loaded.FactionStates[0].OwnedZoneIds);
        }

        #endregion

        #region Zone Data Round Trip

        [Fact]
        public void SaveLoad_ShouldPreserveZoneData()
        {
            // Arrange
            var gameState = new GameState();
            gameState.Zones.Add(new ZoneData
            {
                Id = "zone_downtown",
                Name = "Downtown Los Santos",
                CenterX = 150.5f,
                CenterY = -200.3f,
                CenterZ = 30.0f,
                Radius = 175.5f,
                StrategicValue = 8,
                OwnerFactionId = "faction_michael",
                ControlPercentage = 92.5f,
                IsContested = false,
                Traits = ZoneTrait.Commercial | ZoneTrait.HighValue
            });
            var filePath = GetTestFilePath();

            // Act
            _service.Save(gameState, filePath);
            var loaded = _service.Load(filePath);

            // Assert
            Assert.Single(loaded.Zones);
            var zone = loaded.Zones[0];
            Assert.Equal("zone_downtown", zone.Id);
            Assert.Equal("Downtown Los Santos", zone.Name);
            Assert.Equal(150.5f, zone.CenterX);
            Assert.Equal(-200.3f, zone.CenterY);
            Assert.Equal(30.0f, zone.CenterZ);
            Assert.Equal(175.5f, zone.Radius);
            Assert.Equal(8, zone.StrategicValue);
            Assert.Equal("faction_michael", zone.OwnerFactionId);
            Assert.Equal(92.5f, zone.ControlPercentage);
            Assert.False(zone.IsContested);
            Assert.True(zone.Traits.HasFlag(ZoneTrait.Commercial));
            Assert.True(zone.Traits.HasFlag(ZoneTrait.HighValue));
        }

        [Fact]
        public void SaveLoad_ShouldPreserveNeutralZone()
        {
            // Arrange
            var gameState = new GameState();
            gameState.Zones.Add(new ZoneData
            {
                Id = "zone_neutral",
                Name = "Neutral Territory",
                OwnerFactionId = null,
                ControlPercentage = 0f,
                IsContested = false
            });
            var filePath = GetTestFilePath();

            // Act
            _service.Save(gameState, filePath);
            var loaded = _service.Load(filePath);

            // Assert
            Assert.Single(loaded.Zones);
            Assert.Null(loaded.Zones[0].OwnerFactionId);
            Assert.Equal(0f, loaded.Zones[0].ControlPercentage);
        }

        [Fact]
        public void SaveLoad_ShouldPreserveContestedZone()
        {
            // Arrange
            var gameState = new GameState();
            gameState.Zones.Add(new ZoneData
            {
                Id = "zone_battleground",
                Name = "Battleground",
                OwnerFactionId = "faction_michael",
                ControlPercentage = 55.0f,
                IsContested = true
            });
            var filePath = GetTestFilePath();

            // Act
            _service.Save(gameState, filePath);
            var loaded = _service.Load(filePath);

            // Assert
            Assert.True(loaded.Zones[0].IsContested);
            Assert.Equal(55.0f, loaded.Zones[0].ControlPercentage);
        }

        [Fact]
        public void SaveLoad_ShouldPreserveAllZoneTraits()
        {
            // Arrange
            var gameState = new GameState();
            var allTraits = ZoneTrait.Commercial | ZoneTrait.Industrial | ZoneTrait.Residential |
                           ZoneTrait.Port | ZoneTrait.HighValue | ZoneTrait.Fortified;
            gameState.Zones.Add(new ZoneData
            {
                Id = "zone_mega",
                Name = "Mega Zone",
                Traits = allTraits
            });
            var filePath = GetTestFilePath();

            // Act
            _service.Save(gameState, filePath);
            var loaded = _service.Load(filePath);

            // Assert
            Assert.Equal(allTraits, loaded.Zones[0].Traits);
        }

        #endregion

        #region Relationship Data Round Trip

        [Fact]
        public void SaveLoad_ShouldPreserveRelationshipData()
        {
            // Arrange
            var gameState = new GameState();
            gameState.Relationships.Add(new RelationshipData
            {
                FactionId1 = "faction_michael",
                FactionId2 = "faction_trevor",
                Value = -50
            });
            var filePath = GetTestFilePath();

            // Act
            _service.Save(gameState, filePath);
            var loaded = _service.Load(filePath);

            // Assert
            Assert.Single(loaded.Relationships);
            var rel = loaded.Relationships[0];
            Assert.Equal("faction_michael", rel.FactionId1);
            Assert.Equal("faction_trevor", rel.FactionId2);
            Assert.Equal(-50, rel.Value);
        }

        [Fact]
        public void SaveLoad_ShouldPreserveMultipleRelationships()
        {
            // Arrange
            var gameState = new GameState();
            gameState.Relationships.Add(new RelationshipData { FactionId1 = "faction_michael", FactionId2 = "faction_trevor", Value = -30 });
            gameState.Relationships.Add(new RelationshipData { FactionId1 = "faction_michael", FactionId2 = "faction_franklin", Value = 50 });
            gameState.Relationships.Add(new RelationshipData { FactionId1 = "faction_trevor", FactionId2 = "faction_franklin", Value = 10 });
            var filePath = GetTestFilePath();

            // Act
            _service.Save(gameState, filePath);
            var loaded = _service.Load(filePath);

            // Assert
            Assert.Equal(3, loaded.Relationships.Count);
        }

        [Fact]
        public void SaveLoad_ShouldPreserveExtremeRelationshipValues()
        {
            // Arrange
            var gameState = new GameState();
            gameState.Relationships.Add(new RelationshipData { FactionId1 = "a", FactionId2 = "b", Value = -100 });
            gameState.Relationships.Add(new RelationshipData { FactionId1 = "a", FactionId2 = "c", Value = 100 });
            gameState.Relationships.Add(new RelationshipData { FactionId1 = "b", FactionId2 = "c", Value = 0 });
            var filePath = GetTestFilePath();

            // Act
            _service.Save(gameState, filePath);
            var loaded = _service.Load(filePath);

            // Assert
            Assert.Contains(loaded.Relationships, r => r.Value == -100);
            Assert.Contains(loaded.Relationships, r => r.Value == 100);
            Assert.Contains(loaded.Relationships, r => r.Value == 0);
        }

        #endregion

        #region Complete Game State Round Trip

        [Fact]
        public void SaveLoad_ShouldPreserveCompleteGameState()
        {
            // Arrange
            var factions = new List<Faction>
            {
                new Faction("faction_michael", "Michael's Crew", "Michael", "Professional crew", new FactionColor(0, 100, 255)),
                new Faction("faction_trevor", "Trevor's Gang", "Trevor", "Chaotic crew", new FactionColor(255, 100, 0)),
                new Faction("faction_franklin", "Franklin's Family", "Franklin", "Street crew", new FactionColor(0, 200, 0))
            };
            var states = new List<FactionState>
            {
                new FactionState("faction_michael", 50000, 30),
                new FactionState("faction_trevor", 25000, 45),
                new FactionState("faction_franklin", 35000, 35)
            };
            states[0].AddZone("zone_downtown");
            states[0].AddZone("zone_vinewood");
            states[1].AddZone("zone_sandy_shores");
            states[2].AddZone("zone_grove");

            var zones = new List<Zone>
            {
                new Zone("zone_downtown", "Downtown", new Vector3(0, 0, 0), 150f, 5),
                new Zone("zone_vinewood", "Vinewood", new Vector3(100, 100, 0), 150f, 4),
                new Zone("zone_sandy_shores", "Sandy Shores", new Vector3(-500, 500, 0), 200f, 3),
                new Zone("zone_grove", "Grove Street", new Vector3(-200, -200, 0), 100f, 2)
            };
            zones[0].OwnerFactionId = "faction_michael";
            zones[1].OwnerFactionId = "faction_michael";
            zones[2].OwnerFactionId = "faction_trevor";
            zones[3].OwnerFactionId = "faction_franklin";

            var relationships = new List<FactionRelationship>
            {
                new FactionRelationship("faction_michael", "faction_trevor", -30),
                new FactionRelationship("faction_michael", "faction_franklin", 50),
                new FactionRelationship("faction_trevor", "faction_franklin", 10)
            };

            var originalGameState = GameState.CreateSnapshot(factions, states, zones, relationships);
            originalGameState.SaveName = "Full Campaign Save";
            originalGameState.TotalPlayTimeSeconds = 18000;
            var filePath = GetTestFilePath();

            // Act
            _service.Save(originalGameState, filePath);
            var loaded = _service.Load(filePath);

            // Assert
            Assert.Equal("Full Campaign Save", loaded.SaveName);
            Assert.Equal(18000, loaded.TotalPlayTimeSeconds);
            Assert.Equal(3, loaded.Factions.Count);
            Assert.Equal(3, loaded.FactionStates.Count);
            Assert.Equal(4, loaded.Zones.Count);
            Assert.Equal(3, loaded.Relationships.Count);
        }

        [Fact]
        public void SaveLoad_ShouldPreserveEmptyGameState()
        {
            // Arrange
            var gameState = new GameState { SaveName = "Empty Save" };
            var filePath = GetTestFilePath();

            // Act
            _service.Save(gameState, filePath);
            var loaded = _service.Load(filePath);

            // Assert
            Assert.Equal("Empty Save", loaded.SaveName);
            Assert.Empty(loaded.Factions);
            Assert.Empty(loaded.FactionStates);
            Assert.Empty(loaded.Zones);
            Assert.Empty(loaded.Relationships);
        }

        #endregion

        #region Edge Cases and Error Handling

        [Fact]
        public void Save_WithNullGameState_ShouldThrowArgumentNullException()
        {
            // Arrange
            var filePath = GetTestFilePath();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _service.Save(null!, filePath));
        }

        [Fact]
        public void Save_WithNullFilePath_ShouldThrowArgumentNullException()
        {
            // Arrange
            var gameState = new GameState();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _service.Save(gameState, null!));
        }

        [Fact]
        public void Save_WithEmptyFilePath_ShouldThrowArgumentException()
        {
            // Arrange
            var gameState = new GameState();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => _service.Save(gameState, ""));
        }

        [Fact]
        public void Load_WithNullFilePath_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _service.Load(null!));
        }

        [Fact]
        public void Load_WithEmptyFilePath_ShouldThrowArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => _service.Load(""));
        }

        [Fact]
        public void Load_WithInvalidJson_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var filePath = GetTestFilePath();
            File.WriteAllText(filePath, "{ invalid json content");

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => _service.Load(filePath));
        }

        [Fact]
        public void Save_ShouldOverwriteExistingFile()
        {
            // Arrange
            var filePath = GetTestFilePath();
            var gameState1 = new GameState { SaveName = "First Save" };
            var gameState2 = new GameState { SaveName = "Second Save" };

            // Act
            _service.Save(gameState1, filePath);
            _service.Save(gameState2, filePath);
            var loaded = _service.Load(filePath);

            // Assert
            Assert.Equal("Second Save", loaded.SaveName);
        }

        [Fact]
        public void SaveLoad_ShouldHandleSpecialCharactersInSaveName()
        {
            // Arrange
            var gameState = new GameState { SaveName = "Save with \"quotes\" and 'apostrophes' & symbols <>" };
            var filePath = GetTestFilePath();

            // Act
            _service.Save(gameState, filePath);
            var loaded = _service.Load(filePath);

            // Assert
            Assert.Equal("Save with \"quotes\" and 'apostrophes' & symbols <>", loaded.SaveName);
        }

        [Fact]
        public void SaveLoad_ShouldHandleUnicodeCharacters()
        {
            // Arrange
            var gameState = new GameState { SaveName = "保存ファイル 日本語 🎮" };
            var filePath = GetTestFilePath();

            // Act
            _service.Save(gameState, filePath);
            var loaded = _service.Load(filePath);

            // Assert
            Assert.Equal("保存ファイル 日本語 🎮", loaded.SaveName);
        }

        #endregion

        #region File Format

        [Fact]
        public void Save_ShouldCreateReadableJsonFile()
        {
            // Arrange
            var gameState = new GameState { SaveName = "Readable Save" };
            var filePath = GetTestFilePath();

            // Act
            _service.Save(gameState, filePath);
            var json = File.ReadAllText(filePath);

            // Assert
            Assert.Contains("SaveName", json);
            Assert.Contains("Readable Save", json);
            Assert.Contains("Version", json);
        }

        [Fact]
        public void Save_ShouldProducePrettyPrintedJson()
        {
            // Arrange
            var gameState = new GameState { SaveName = "Test" };
            var filePath = GetTestFilePath();

            // Act
            _service.Save(gameState, filePath);
            var json = File.ReadAllText(filePath);

            // Assert - pretty printed JSON should have newlines and indentation
            Assert.Contains("\n", json);
        }

        #endregion
    }
}
