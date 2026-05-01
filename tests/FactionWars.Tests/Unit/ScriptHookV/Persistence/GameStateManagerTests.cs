using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.Factions.Interfaces;
using FactionWars.Factions.Models;
using FactionWars.Persistence;
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
        private readonly Mock<ISidecarStore> _mockSidecarStore;
        private readonly Mock<IZoneRepository> _mockZoneRepository;
        private readonly Mock<IFactionRepository> _mockFactionRepository;
        private readonly Mock<IZoneDefenderAllocationRepository> _mockAllocationRepository;
        private readonly GameStateManager _sut;

        public GameStateManagerTests()
        {
            _mockSidecarStore = new Mock<ISidecarStore>();
            _mockZoneRepository = new Mock<IZoneRepository>();
            _mockFactionRepository = new Mock<IFactionRepository>();
            _mockAllocationRepository = new Mock<IZoneDefenderAllocationRepository>();

            _sut = new GameStateManager(
                _mockSidecarStore.Object,
                _mockZoneRepository.Object,
                _mockFactionRepository.Object,
                _mockAllocationRepository.Object);
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithNullSidecarStore_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new GameStateManager(
                    null!,
                    _mockZoneRepository.Object,
                    _mockFactionRepository.Object,
                    _mockAllocationRepository.Object));
        }

        [Fact]
        public void Constructor_WithNullZoneRepository_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new GameStateManager(
                    _mockSidecarStore.Object,
                    null!,
                    _mockFactionRepository.Object,
                    _mockAllocationRepository.Object));
        }

        [Fact]
        public void Constructor_WithNullFactionRepository_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new GameStateManager(
                    _mockSidecarStore.Object,
                    _mockZoneRepository.Object,
                    null!,
                    _mockAllocationRepository.Object));
        }

        [Fact]
        public void Constructor_WithNullAllocationRepository_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new GameStateManager(
                    _mockSidecarStore.Object,
                    _mockZoneRepository.Object,
                    _mockFactionRepository.Object,
                    null!));
        }

        [Fact]
        public void Constructor_WithValidDependencies_InitializesCorrectly()
        {
            Assert.NotNull(_sut);
            Assert.False(_sut.HasGameLoaded);
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
            _mockZoneRepository.Setup(r => r.GetAll()).Returns(zones);
            _mockFactionRepository.Setup(r => r.GetAll()).Returns(factions);
            _mockFactionRepository.Setup(r => r.GetAllStates()).Returns(factionStates);

            _sut.NewGame();

            var result = _sut.GetCurrentGameState();

            Assert.NotNull(result);
            Assert.Single(result.Zones);
            Assert.Single(result.Factions);
            Assert.Single(result.FactionStates);
        }

        #endregion

        #region WriteCurrentSidecar Tests

        [Fact]
        public void WriteCurrentSidecar_CallsStoreWithExpectedPayload()
        {
            SetupEmptyRepositories();
            _sut.NewGame();
            var fp = new SaveFingerprint { TotalPlayTimeSeconds = 12340, Money = 50000, CompletedMissionCount = 23, InGameClockMinutes = 854 };
            var pos = new PlayerPosition { X = 1, Y = 2, Z = 3, Heading = 90 };

            _sut.WriteCurrentSidecar(fp, pos, "SGTA00003");

            _mockSidecarStore.Verify(
                s => s.WriteSidecar(It.Is<Sidecar>(sc =>
                    sc.Fingerprint.TotalPlayTimeSeconds == 12340 &&
                    sc.NativeSaveFilename == "SGTA00003" &&
                    sc.PlayerPosition.X == 1f)),
                Times.Once);
        }

        [Fact]
        public void WriteCurrentSidecar_NoGameLoaded_DoesNotCallStore()
        {
            var fp = new SaveFingerprint { TotalPlayTimeSeconds = 12340 };
            var pos = new PlayerPosition();

            _sut.WriteCurrentSidecar(fp, pos, "SGTA00003");

            _mockSidecarStore.Verify(s => s.WriteSidecar(It.IsAny<Sidecar>()), Times.Never);
        }

        [Fact]
        public void WriteCurrentSidecar_RaisesOnGameSavedEvent()
        {
            SetupEmptyRepositories();
            _sut.NewGame();
            _mockSidecarStore.Setup(s => s.WriteSidecar(It.IsAny<Sidecar>())).Returns(true);
            GameStateSavedEventArgs? eventArgs = null;
            _sut.OnGameSaved += (_, args) => eventArgs = args;

            var fp = new SaveFingerprint { TotalPlayTimeSeconds = 12340 };
            var pos = new PlayerPosition();
            _sut.WriteCurrentSidecar(fp, pos, "SGTA00003");

            Assert.NotNull(eventArgs);
            Assert.True(eventArgs!.Success);
        }

        [Fact]
        public void WriteCurrentSidecar_StoreReturnsFalse_RaisesEventWithFailure()
        {
            SetupEmptyRepositories();
            _sut.NewGame();
            _mockSidecarStore.Setup(s => s.WriteSidecar(It.IsAny<Sidecar>())).Returns(false);
            GameStateSavedEventArgs? eventArgs = null;
            _sut.OnGameSaved += (_, args) => eventArgs = args;

            var fp = new SaveFingerprint { TotalPlayTimeSeconds = 12340 };
            var pos = new PlayerPosition();
            _sut.WriteCurrentSidecar(fp, pos, "SGTA00003");

            Assert.NotNull(eventArgs);
            Assert.False(eventArgs!.Success);
        }

        #endregion

        [Fact]
        public void NewGame_ResetsDifficultyToNormal()
        {
            SetupEmptyRepositories();
            _sut.SetCurrentDifficulty(Difficulty.Hard);

            _sut.NewGame();

            var snapshot = _sut.GetCurrentGameState();
            Assert.NotNull(snapshot);
            Assert.Equal(Difficulty.Normal, snapshot!.Difficulty);
        }

        #region HydrateFromSidecar Tests

        [Fact]
        public void HydrateFromSidecar_AppliesGameStateAndMarksLoaded()
        {
            SetupEmptyRepositories();
            var sidecar = new Sidecar
            {
                Fingerprint = new SaveFingerprint { TotalPlayTimeSeconds = 12340 },
                GameState = new GameState { SaveName = "test", TotalPlayTimeSeconds = 12340 },
            };

            _sut.HydrateFromSidecar(sidecar);

            Assert.True(_sut.HasGameLoaded);
            Assert.Equal(12340, _sut.TotalPlayTimeSeconds);
        }

        [Fact]
        public void HydrateFromSidecar_RaisesOnGameLoadedEvent()
        {
            SetupEmptyRepositories();
            var sidecar = new Sidecar
            {
                Fingerprint = new SaveFingerprint { TotalPlayTimeSeconds = 12340 },
                GameState = new GameState { SaveName = "loaded-save" },
            };
            GameStateLoadedEventArgs? eventArgs = null;
            _sut.OnGameLoaded += (_, args) => eventArgs = args;

            _sut.HydrateFromSidecar(sidecar);

            Assert.NotNull(eventArgs);
            Assert.True(eventArgs!.Success);
            Assert.Equal("loaded-save", eventArgs.SaveName);
        }

        [Fact]
        public void HydrateFromSidecar_NullSidecar_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => _sut.HydrateFromSidecar(null!));
        }

        [Fact]
        public void HydrateFromSidecar_NullGameState_Throws()
        {
            var sidecar = new Sidecar
            {
                Fingerprint = new SaveFingerprint(),
                GameState = null!,
            };

            Assert.Throws<ArgumentException>(() => _sut.HydrateFromSidecar(sidecar));
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
            SetupEmptyRepositories();
            var gameState = CreateTestGameState();

            _sut.ApplyGameState(gameState);

            _mockZoneRepository.Verify(r => r.Clear(), Times.Once);
            _mockFactionRepository.Verify(r => r.Clear(), Times.Once);
        }

        #endregion

        #region Helper Methods

        private void SetupEmptyRepositories()
        {
            _mockZoneRepository.Setup(r => r.GetAll()).Returns(new List<Zone>());
            _mockFactionRepository.Setup(r => r.GetAll()).Returns(new List<Faction>());
            _mockFactionRepository.Setup(r => r.GetAllStates()).Returns(new List<FactionState>());
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
