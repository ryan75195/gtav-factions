using FactionWars.Core.Interfaces;
using FactionWars.Factions.Interfaces;
using FactionWars.Factions.Models;
using FactionWars.Persistence.Models;
using FactionWars.ScriptHookV.Persistence;
using FactionWars.Territory.Interfaces;
using FactionWars.Territory.Models;
using Moq;
using System;
using System.Collections.Generic;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV.Persistence
{
    public class GameStateManagerTests
    {
        private readonly Mock<ISaveSlotManager> _mockSaveSlotManager;
        private readonly Mock<IZoneRepository> _mockZoneRepository;
        private readonly Mock<IFactionRepository> _mockFactionRepository;
        private readonly Mock<IFactionRelationshipRepository> _mockRelationshipRepository;
        private readonly GameStateManager _sut;

        public GameStateManagerTests()
        {
            _mockSaveSlotManager = new Mock<ISaveSlotManager>();
            _mockZoneRepository = new Mock<IZoneRepository>();
            _mockFactionRepository = new Mock<IFactionRepository>();
            _mockRelationshipRepository = new Mock<IFactionRelationshipRepository>();

            _sut = new GameStateManager(
                _mockSaveSlotManager.Object,
                _mockZoneRepository.Object,
                _mockFactionRepository.Object,
                _mockRelationshipRepository.Object);
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithNullSaveSlotManager_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new GameStateManager(
                    null!,
                    _mockZoneRepository.Object,
                    _mockFactionRepository.Object,
                    _mockRelationshipRepository.Object));
        }

        [Fact]
        public void Constructor_WithNullZoneRepository_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new GameStateManager(
                    _mockSaveSlotManager.Object,
                    null!,
                    _mockFactionRepository.Object,
                    _mockRelationshipRepository.Object));
        }

        [Fact]
        public void Constructor_WithNullFactionRepository_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new GameStateManager(
                    _mockSaveSlotManager.Object,
                    _mockZoneRepository.Object,
                    null!,
                    _mockRelationshipRepository.Object));
        }

        [Fact]
        public void Constructor_WithNullRelationshipRepository_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new GameStateManager(
                    _mockSaveSlotManager.Object,
                    _mockZoneRepository.Object,
                    _mockFactionRepository.Object,
                    null!));
        }

        [Fact]
        public void Constructor_WithValidDependencies_InitializesCorrectly()
        {
            Assert.NotNull(_sut);
            Assert.False(_sut.HasGameLoaded);
            Assert.Null(_sut.CurrentSaveName);
            Assert.Equal(0, _sut.TotalPlayTimeSeconds);
        }

        #endregion

        #region GetCurrentGameState Tests

        [Fact]
        public void GetCurrentGameState_WhenNoGameLoaded_ReturnsNull()
        {
            var result = _sut.GetCurrentGameState();

            Assert.Null(result);
        }

        [Fact]
        public void GetCurrentGameState_WithLoadedGame_ReturnsGameState()
        {
            // Arrange - Set up repositories with data
            var zones = new List<Zone>
            {
                new Zone("zone1", "Downtown", Vector3.Zero, 100f)
            };
            var factions = new List<Faction>
            {
                new Faction("faction1", "Blue Faction", null, "", new FactionColor(0, 0, 255))
            };
            var factionStates = new List<FactionState>
            {
                new FactionState("faction1")
            };
            var relationships = new List<FactionRelationship>();

            _mockZoneRepository.Setup(r => r.GetAll()).Returns(zones);
            _mockFactionRepository.Setup(r => r.GetAll()).Returns(factions);
            _mockFactionRepository.Setup(r => r.GetAllStates()).Returns(factionStates);
            _mockRelationshipRepository.Setup(r => r.GetAll()).Returns(relationships);

            // Simulate a game being loaded
            _sut.NewGame();

            // Act
            var result = _sut.GetCurrentGameState();

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Zones);
            Assert.Single(result.Factions);
            Assert.Single(result.FactionStates);
        }

        #endregion

        #region SaveToSlot Tests

        [Fact]
        public void SaveToSlot_WhenNoGameLoaded_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => _sut.SaveToSlot(0));
        }

        [Fact]
        public void SaveToSlot_WithLoadedGame_SavesSuccessfully()
        {
            // Arrange
            SetupEmptyRepositories();
            _sut.NewGame();

            // Act
            _sut.SaveToSlot(0, "Test Save");

            // Assert
            _mockSaveSlotManager.Verify(m => m.SaveToSlot(0, It.IsAny<GameState>()), Times.Once);
        }

        [Fact]
        public void SaveToSlot_WithSaveName_SetsSaveNameInGameState()
        {
            // Arrange
            SetupEmptyRepositories();
            _sut.NewGame();
            GameState? savedState = null;
            _mockSaveSlotManager
                .Setup(m => m.SaveToSlot(It.IsAny<int>(), It.IsAny<GameState>()))
                .Callback<int, GameState>((slot, state) => savedState = state);

            // Act
            _sut.SaveToSlot(0, "My Custom Save");

            // Assert
            Assert.NotNull(savedState);
            Assert.Equal("My Custom Save", savedState.SaveName);
        }

        [Fact]
        public void SaveToSlot_RaisesOnGameSavedEvent()
        {
            // Arrange
            SetupEmptyRepositories();
            _sut.NewGame();
            GameStateSavedEventArgs? eventArgs = null;
            _sut.OnGameSaved += (sender, args) => eventArgs = args;

            // Act
            _sut.SaveToSlot(0, "Test Save");

            // Assert
            Assert.NotNull(eventArgs);
            Assert.Equal(0, eventArgs.SlotNumber);
            Assert.Equal("Test Save", eventArgs.SaveName);
            Assert.True(eventArgs.Success);
        }

        [Fact]
        public void SaveToSlot_WhenSaveFails_RaisesEventWithError()
        {
            // Arrange
            SetupEmptyRepositories();
            _sut.NewGame();
            var expectedException = new InvalidOperationException("Save failed");
            _mockSaveSlotManager
                .Setup(m => m.SaveToSlot(It.IsAny<int>(), It.IsAny<GameState>()))
                .Throws(expectedException);

            GameStateSavedEventArgs? eventArgs = null;
            _sut.OnGameSaved += (sender, args) => eventArgs = args;

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => _sut.SaveToSlot(0, "Test Save"));
            Assert.NotNull(eventArgs);
            Assert.False(eventArgs.Success);
            Assert.Equal(expectedException, eventArgs.Error);
        }

        #endregion

        #region LoadFromSlot Tests

        [Fact]
        public void LoadFromSlot_WithValidSlot_LoadsGameState()
        {
            // Arrange
            SetupEmptyRepositories();
            var gameState = CreateTestGameState();
            _mockSaveSlotManager.Setup(m => m.LoadFromSlot(0)).Returns(gameState);

            // Act
            _sut.LoadFromSlot(0);

            // Assert
            Assert.True(_sut.HasGameLoaded);
            Assert.Equal("Test Save", _sut.CurrentSaveName);
        }

        [Fact]
        public void LoadFromSlot_AppliesZonesToRepository()
        {
            // Arrange
            SetupEmptyRepositories();
            var gameState = CreateTestGameState();
            _mockSaveSlotManager.Setup(m => m.LoadFromSlot(0)).Returns(gameState);

            // Act
            _sut.LoadFromSlot(0);

            // Assert
            _mockZoneRepository.Verify(r => r.Clear(), Times.Once);
            _mockZoneRepository.Verify(r => r.Add(It.IsAny<Zone>()), Times.Once);
        }

        [Fact]
        public void LoadFromSlot_AppliesFactionsToRepository()
        {
            // Arrange
            SetupEmptyRepositories();
            var gameState = CreateTestGameState();
            _mockSaveSlotManager.Setup(m => m.LoadFromSlot(0)).Returns(gameState);

            // Act
            _sut.LoadFromSlot(0);

            // Assert
            _mockFactionRepository.Verify(r => r.Clear(), Times.Once);
            _mockFactionRepository.Verify(r => r.Add(It.IsAny<Faction>()), Times.Once);
        }

        [Fact]
        public void LoadFromSlot_AppliesFactionStatesToRepository()
        {
            // Arrange
            SetupEmptyRepositories();
            var gameState = CreateTestGameState();
            _mockSaveSlotManager.Setup(m => m.LoadFromSlot(0)).Returns(gameState);

            // Act
            _sut.LoadFromSlot(0);

            // Assert
            _mockFactionRepository.Verify(r => r.SetState(It.IsAny<FactionState>()), Times.Once);
        }

        [Fact]
        public void LoadFromSlot_AppliesRelationshipsToRepository()
        {
            // Arrange
            SetupEmptyRepositories();
            var gameState = CreateTestGameState();
            gameState.Relationships.Add(new RelationshipData
            {
                FactionId1 = "faction1",
                FactionId2 = "faction2",
                Value = -100
            });
            _mockSaveSlotManager.Setup(m => m.LoadFromSlot(0)).Returns(gameState);

            // Act
            _sut.LoadFromSlot(0);

            // Assert
            _mockRelationshipRepository.Verify(r => r.Clear(), Times.Once);
            _mockRelationshipRepository.Verify(r => r.Add(It.IsAny<FactionRelationship>()), Times.Once);
        }

        [Fact]
        public void LoadFromSlot_RaisesOnGameLoadedEvent()
        {
            // Arrange
            SetupEmptyRepositories();
            var gameState = CreateTestGameState();
            _mockSaveSlotManager.Setup(m => m.LoadFromSlot(0)).Returns(gameState);

            GameStateLoadedEventArgs? eventArgs = null;
            _sut.OnGameLoaded += (sender, args) => eventArgs = args;

            // Act
            _sut.LoadFromSlot(0);

            // Assert
            Assert.NotNull(eventArgs);
            Assert.Equal(0, eventArgs.SlotNumber);
            Assert.Equal("Test Save", eventArgs.SaveName);
            Assert.True(eventArgs.Success);
        }

        [Fact]
        public void LoadFromSlot_WhenLoadFails_RaisesEventWithError()
        {
            // Arrange
            var expectedException = new InvalidOperationException("Load failed");
            _mockSaveSlotManager
                .Setup(m => m.LoadFromSlot(It.IsAny<int>()))
                .Throws(expectedException);

            GameStateLoadedEventArgs? eventArgs = null;
            _sut.OnGameLoaded += (sender, args) => eventArgs = args;

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => _sut.LoadFromSlot(0));
            Assert.NotNull(eventArgs);
            Assert.False(eventArgs.Success);
            Assert.Equal(expectedException, eventArgs.Error);
        }

        [Fact]
        public void LoadFromSlot_RestoresPlayTime()
        {
            // Arrange
            SetupEmptyRepositories();
            var gameState = CreateTestGameState();
            gameState.TotalPlayTimeSeconds = 3600; // 1 hour
            _mockSaveSlotManager.Setup(m => m.LoadFromSlot(0)).Returns(gameState);

            // Act
            _sut.LoadFromSlot(0);

            // Assert
            Assert.Equal(3600, _sut.TotalPlayTimeSeconds);
        }

        #endregion

        #region NewGame Tests

        [Fact]
        public void NewGame_SetsHasGameLoadedToTrue()
        {
            SetupEmptyRepositories();

            _sut.NewGame();

            Assert.True(_sut.HasGameLoaded);
        }

        [Fact]
        public void NewGame_ResetsTotalPlayTime()
        {
            SetupEmptyRepositories();
            _sut.NewGame();
            _sut.UpdatePlayTime(100f);

            _sut.NewGame();

            Assert.Equal(0, _sut.TotalPlayTimeSeconds);
        }

        [Fact]
        public void NewGame_ClearsCurrentSaveName()
        {
            // Arrange
            SetupEmptyRepositories();
            var gameState = CreateTestGameState();
            _mockSaveSlotManager.Setup(m => m.LoadFromSlot(0)).Returns(gameState);
            _sut.LoadFromSlot(0);

            // Act
            _sut.NewGame();

            // Assert
            Assert.Null(_sut.CurrentSaveName);
        }

        #endregion

        #region UpdatePlayTime Tests

        [Fact]
        public void UpdatePlayTime_WhenNoGameLoaded_DoesNothing()
        {
            _sut.UpdatePlayTime(10f);

            Assert.Equal(0, _sut.TotalPlayTimeSeconds);
        }

        [Fact]
        public void UpdatePlayTime_WithLoadedGame_AccumulatesTime()
        {
            SetupEmptyRepositories();
            _sut.NewGame();

            _sut.UpdatePlayTime(1.5f);
            _sut.UpdatePlayTime(2.5f);

            Assert.Equal(4, _sut.TotalPlayTimeSeconds);
        }

        [Fact]
        public void UpdatePlayTime_WithNegativeValue_DoesNotDecrease()
        {
            SetupEmptyRepositories();
            _sut.NewGame();
            _sut.UpdatePlayTime(10f);

            _sut.UpdatePlayTime(-5f);

            Assert.Equal(10, _sut.TotalPlayTimeSeconds);
        }

        #endregion

        #region ApplyGameState Tests

        [Fact]
        public void ApplyGameState_WithNull_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _sut.ApplyGameState(null!));
        }

        [Fact]
        public void ApplyGameState_ClearsAndPopulatesRepositories()
        {
            // Arrange
            SetupEmptyRepositories();
            var gameState = CreateTestGameState();

            // Act
            _sut.ApplyGameState(gameState);

            // Assert
            _mockZoneRepository.Verify(r => r.Clear(), Times.Once);
            _mockFactionRepository.Verify(r => r.Clear(), Times.Once);
            _mockRelationshipRepository.Verify(r => r.Clear(), Times.Once);
        }

        #endregion

        #region Helper Methods

        private void SetupEmptyRepositories()
        {
            _mockZoneRepository.Setup(r => r.GetAll()).Returns(new List<Zone>());
            _mockFactionRepository.Setup(r => r.GetAll()).Returns(new List<Faction>());
            _mockFactionRepository.Setup(r => r.GetAllStates()).Returns(new List<FactionState>());
            _mockRelationshipRepository.Setup(r => r.GetAll()).Returns(new List<FactionRelationship>());
        }

        private static GameState CreateTestGameState()
        {
            var gameState = new GameState
            {
                SaveName = "Test Save",
                TotalPlayTimeSeconds = 100
            };

            gameState.Zones.Add(new ZoneData
            {
                Id = "zone1",
                Name = "Downtown",
                CenterX = 0f,
                CenterY = 0f,
                CenterZ = 0f,
                Radius = 100f,
                OwnerFactionId = "faction1"
            });

            gameState.Factions.Add(new FactionData
            {
                Id = "faction1",
                Name = "Blue Faction",
                ColorR = 0,
                ColorG = 0,
                ColorB = 255
            });

            gameState.FactionStates.Add(new FactionStateData
            {
                FactionId = "faction1",
                Cash = 10000,
                TroopCount = 50
            });

            return gameState;
        }

        #endregion
    }
}
