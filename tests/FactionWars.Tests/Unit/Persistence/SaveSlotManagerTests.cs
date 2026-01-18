using FactionWars.Core.Interfaces;
using FactionWars.Persistence;
using FactionWars.Persistence.Models;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace FactionWars.Tests.Unit.Persistence
{
    public class SaveSlotManagerTests : IDisposable
    {
        private readonly string _testDirectory;
        private readonly Mock<IPersistenceService> _mockPersistenceService;
        private readonly ISaveSlotManager _slotManager;

        public SaveSlotManagerTests()
        {
            _testDirectory = Path.Combine(Path.GetTempPath(), "FactionWarsTests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testDirectory);
            _mockPersistenceService = new Mock<IPersistenceService>();
            _slotManager = new SaveSlotManager(_mockPersistenceService.Object, _testDirectory);
        }

        public void Dispose()
        {
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, recursive: true);
            }
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithNullPersistenceService_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new SaveSlotManager(null!, _testDirectory));
        }

        [Fact]
        public void Constructor_WithNullSaveDirectory_ShouldThrowArgumentNullException()
        {
            // Arrange
            var mockService = new Mock<IPersistenceService>();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new SaveSlotManager(mockService.Object, null!));
        }

        [Fact]
        public void Constructor_WithEmptySaveDirectory_ShouldThrowArgumentException()
        {
            // Arrange
            var mockService = new Mock<IPersistenceService>();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => new SaveSlotManager(mockService.Object, ""));
        }

        [Fact]
        public void Constructor_ShouldCreateDirectoryIfNotExists()
        {
            // Arrange
            var newDirectory = Path.Combine(_testDirectory, "NewSaveFolder");
            Assert.False(Directory.Exists(newDirectory));

            // Act
            var manager = new SaveSlotManager(_mockPersistenceService.Object, newDirectory);

            // Assert
            Assert.True(Directory.Exists(newDirectory));
        }

        #endregion

        #region MaxSlots Property Tests

        [Fact]
        public void MaxSlots_ShouldReturnDefaultValue()
        {
            // Assert
            Assert.Equal(10, _slotManager.MaxSlots);
        }

        [Fact]
        public void Constructor_WithCustomMaxSlots_ShouldSetMaxSlots()
        {
            // Arrange
            var manager = new SaveSlotManager(_mockPersistenceService.Object, _testDirectory, maxSlots: 5);

            // Assert
            Assert.Equal(5, manager.MaxSlots);
        }

        [Fact]
        public void Constructor_WithZeroMaxSlots_ShouldThrowArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                new SaveSlotManager(_mockPersistenceService.Object, _testDirectory, maxSlots: 0));
        }

        [Fact]
        public void Constructor_WithNegativeMaxSlots_ShouldThrowArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                new SaveSlotManager(_mockPersistenceService.Object, _testDirectory, maxSlots: -1));
        }

        #endregion

        #region GetSlotFilePath Tests

        [Fact]
        public void GetSlotFilePath_ShouldReturnCorrectPath()
        {
            // Act
            var path = _slotManager.GetSlotFilePath(1);

            // Assert
            var expectedPath = Path.Combine(_testDirectory, "save_slot_1.json");
            Assert.Equal(expectedPath, path);
        }

        [Fact]
        public void GetSlotFilePath_WithSlotZero_ShouldReturnCorrectPath()
        {
            // Act
            var path = _slotManager.GetSlotFilePath(0);

            // Assert
            var expectedPath = Path.Combine(_testDirectory, "save_slot_0.json");
            Assert.Equal(expectedPath, path);
        }

        [Fact]
        public void GetSlotFilePath_WithNegativeSlot_ShouldThrowArgumentOutOfRangeException()
        {
            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => _slotManager.GetSlotFilePath(-1));
        }

        [Fact]
        public void GetSlotFilePath_WithSlotExceedingMax_ShouldThrowArgumentOutOfRangeException()
        {
            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => _slotManager.GetSlotFilePath(10));
        }

        #endregion

        #region IsSlotOccupied Tests

        [Fact]
        public void IsSlotOccupied_WhenSlotIsEmpty_ShouldReturnFalse()
        {
            // Arrange
            _mockPersistenceService.Setup(s => s.Exists(It.IsAny<string>())).Returns(false);

            // Act
            var occupied = _slotManager.IsSlotOccupied(0);

            // Assert
            Assert.False(occupied);
        }

        [Fact]
        public void IsSlotOccupied_WhenSlotHasSave_ShouldReturnTrue()
        {
            // Arrange
            var slotPath = _slotManager.GetSlotFilePath(0);
            _mockPersistenceService.Setup(s => s.Exists(slotPath)).Returns(true);

            // Act
            var occupied = _slotManager.IsSlotOccupied(0);

            // Assert
            Assert.True(occupied);
        }

        [Fact]
        public void IsSlotOccupied_WithInvalidSlot_ShouldThrowArgumentOutOfRangeException()
        {
            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => _slotManager.IsSlotOccupied(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => _slotManager.IsSlotOccupied(10));
        }

        #endregion

        #region SaveToSlot Tests

        [Fact]
        public void SaveToSlot_ShouldCallPersistenceServiceSave()
        {
            // Arrange
            var gameState = new GameState { SaveName = "Test Save" };
            var slotPath = _slotManager.GetSlotFilePath(0);

            // Act
            _slotManager.SaveToSlot(0, gameState);

            // Assert
            _mockPersistenceService.Verify(s => s.Save(gameState, slotPath), Times.Once);
        }

        [Fact]
        public void SaveToSlot_WithInvalidSlot_ShouldThrowArgumentOutOfRangeException()
        {
            // Arrange
            var gameState = new GameState();

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => _slotManager.SaveToSlot(-1, gameState));
            Assert.Throws<ArgumentOutOfRangeException>(() => _slotManager.SaveToSlot(10, gameState));
        }

        [Fact]
        public void SaveToSlot_WithNullGameState_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _slotManager.SaveToSlot(0, null!));
        }

        [Fact]
        public async Task SaveToSlotAsync_ShouldCallPersistenceServiceSaveAsync()
        {
            // Arrange
            var gameState = new GameState { SaveName = "Async Test Save" };
            var slotPath = _slotManager.GetSlotFilePath(0);

            // Act
            await _slotManager.SaveToSlotAsync(0, gameState);

            // Assert
            _mockPersistenceService.Verify(s => s.SaveAsync(gameState, slotPath), Times.Once);
        }

        #endregion

        #region LoadFromSlot Tests

        [Fact]
        public void LoadFromSlot_ShouldCallPersistenceServiceLoad()
        {
            // Arrange
            var expectedGameState = new GameState { SaveName = "Loaded Save" };
            var slotPath = _slotManager.GetSlotFilePath(0);
            _mockPersistenceService.Setup(s => s.Exists(slotPath)).Returns(true);
            _mockPersistenceService.Setup(s => s.Load(slotPath)).Returns(expectedGameState);

            // Act
            var result = _slotManager.LoadFromSlot(0);

            // Assert
            Assert.Equal("Loaded Save", result.SaveName);
            _mockPersistenceService.Verify(s => s.Load(slotPath), Times.Once);
        }

        [Fact]
        public void LoadFromSlot_WhenSlotIsEmpty_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var slotPath = _slotManager.GetSlotFilePath(0);
            _mockPersistenceService.Setup(s => s.Exists(slotPath)).Returns(false);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => _slotManager.LoadFromSlot(0));
        }

        [Fact]
        public void LoadFromSlot_WithInvalidSlot_ShouldThrowArgumentOutOfRangeException()
        {
            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => _slotManager.LoadFromSlot(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => _slotManager.LoadFromSlot(10));
        }

        [Fact]
        public async Task LoadFromSlotAsync_ShouldCallPersistenceServiceLoadAsync()
        {
            // Arrange
            var expectedGameState = new GameState { SaveName = "Async Loaded Save" };
            var slotPath = _slotManager.GetSlotFilePath(0);
            _mockPersistenceService.Setup(s => s.Exists(slotPath)).Returns(true);
            _mockPersistenceService.Setup(s => s.LoadAsync(slotPath)).ReturnsAsync(expectedGameState);

            // Act
            var result = await _slotManager.LoadFromSlotAsync(0);

            // Assert
            Assert.Equal("Async Loaded Save", result.SaveName);
        }

        #endregion

        #region DeleteSlot Tests

        [Fact]
        public void DeleteSlot_ShouldCallPersistenceServiceDelete()
        {
            // Arrange
            var slotPath = _slotManager.GetSlotFilePath(0);

            // Act
            _slotManager.DeleteSlot(0);

            // Assert
            _mockPersistenceService.Verify(s => s.Delete(slotPath), Times.Once);
        }

        [Fact]
        public void DeleteSlot_WithInvalidSlot_ShouldThrowArgumentOutOfRangeException()
        {
            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => _slotManager.DeleteSlot(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => _slotManager.DeleteSlot(10));
        }

        #endregion

        #region GetSlotInfo Tests

        [Fact]
        public void GetSlotInfo_WhenSlotIsEmpty_ShouldReturnEmptySlotInfo()
        {
            // Arrange
            var slotPath = _slotManager.GetSlotFilePath(0);
            _mockPersistenceService.Setup(s => s.Exists(slotPath)).Returns(false);

            // Act
            var info = _slotManager.GetSlotInfo(0);

            // Assert
            Assert.NotNull(info);
            Assert.Equal(0, info.SlotNumber);
            Assert.False(info.IsOccupied);
            Assert.Null(info.SaveName);
            Assert.Null(info.ModifiedAt);
            Assert.Equal(0, info.TotalPlayTimeSeconds);
        }

        [Fact]
        public void GetSlotInfo_WhenSlotHasSave_ShouldReturnSlotInfoFromGameState()
        {
            // Arrange
            var slotPath = _slotManager.GetSlotFilePath(0);
            var gameState = new GameState
            {
                SaveName = "Campaign Save",
                TotalPlayTimeSeconds = 7200
            };
            _mockPersistenceService.Setup(s => s.Exists(slotPath)).Returns(true);
            _mockPersistenceService.Setup(s => s.Load(slotPath)).Returns(gameState);

            // Act
            var info = _slotManager.GetSlotInfo(0);

            // Assert
            Assert.NotNull(info);
            Assert.Equal(0, info.SlotNumber);
            Assert.True(info.IsOccupied);
            Assert.Equal("Campaign Save", info.SaveName);
            Assert.Equal(7200, info.TotalPlayTimeSeconds);
            Assert.NotNull(info.ModifiedAt);
        }

        [Fact]
        public void GetSlotInfo_WithInvalidSlot_ShouldThrowArgumentOutOfRangeException()
        {
            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => _slotManager.GetSlotInfo(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => _slotManager.GetSlotInfo(10));
        }

        #endregion

        #region GetAllSlotInfo Tests

        [Fact]
        public void GetAllSlotInfo_ShouldReturnInfoForAllSlots()
        {
            // Arrange
            _mockPersistenceService.Setup(s => s.Exists(It.IsAny<string>())).Returns(false);

            // Act
            var allInfo = _slotManager.GetAllSlotInfo();

            // Assert
            Assert.Equal(10, allInfo.Count);
            for (int i = 0; i < 10; i++)
            {
                Assert.Equal(i, allInfo[i].SlotNumber);
                Assert.False(allInfo[i].IsOccupied);
            }
        }

        [Fact]
        public void GetAllSlotInfo_WithMixedSlots_ShouldReturnCorrectStatus()
        {
            // Arrange
            var slot1Path = _slotManager.GetSlotFilePath(1);
            var slot3Path = _slotManager.GetSlotFilePath(3);
            var gameState1 = new GameState { SaveName = "Save 1" };
            var gameState3 = new GameState { SaveName = "Save 3" };

            _mockPersistenceService.Setup(s => s.Exists(It.IsAny<string>())).Returns(false);
            _mockPersistenceService.Setup(s => s.Exists(slot1Path)).Returns(true);
            _mockPersistenceService.Setup(s => s.Exists(slot3Path)).Returns(true);
            _mockPersistenceService.Setup(s => s.Load(slot1Path)).Returns(gameState1);
            _mockPersistenceService.Setup(s => s.Load(slot3Path)).Returns(gameState3);

            // Act
            var allInfo = _slotManager.GetAllSlotInfo();

            // Assert
            Assert.False(allInfo[0].IsOccupied);
            Assert.True(allInfo[1].IsOccupied);
            Assert.Equal("Save 1", allInfo[1].SaveName);
            Assert.False(allInfo[2].IsOccupied);
            Assert.True(allInfo[3].IsOccupied);
            Assert.Equal("Save 3", allInfo[3].SaveName);
        }

        #endregion

        #region GetFirstEmptySlot Tests

        [Fact]
        public void GetFirstEmptySlot_WhenAllSlotsEmpty_ShouldReturnZero()
        {
            // Arrange
            _mockPersistenceService.Setup(s => s.Exists(It.IsAny<string>())).Returns(false);

            // Act
            var slot = _slotManager.GetFirstEmptySlot();

            // Assert
            Assert.Equal(0, slot);
        }

        [Fact]
        public void GetFirstEmptySlot_WhenFirstSlotOccupied_ShouldReturnOne()
        {
            // Arrange
            var slot0Path = _slotManager.GetSlotFilePath(0);
            _mockPersistenceService.Setup(s => s.Exists(It.IsAny<string>())).Returns(false);
            _mockPersistenceService.Setup(s => s.Exists(slot0Path)).Returns(true);

            // Act
            var slot = _slotManager.GetFirstEmptySlot();

            // Assert
            Assert.Equal(1, slot);
        }

        [Fact]
        public void GetFirstEmptySlot_WhenAllSlotsOccupied_ShouldReturnNull()
        {
            // Arrange
            _mockPersistenceService.Setup(s => s.Exists(It.IsAny<string>())).Returns(true);

            // Act
            var slot = _slotManager.GetFirstEmptySlot();

            // Assert
            Assert.Null(slot);
        }

        [Fact]
        public void GetFirstEmptySlot_WithGapsInSlots_ShouldReturnFirstGap()
        {
            // Arrange - slots 0, 1, 3 are occupied, slot 2 is empty
            var slot0Path = _slotManager.GetSlotFilePath(0);
            var slot1Path = _slotManager.GetSlotFilePath(1);
            var slot2Path = _slotManager.GetSlotFilePath(2);
            var slot3Path = _slotManager.GetSlotFilePath(3);
            _mockPersistenceService.Setup(s => s.Exists(It.IsAny<string>())).Returns(false);
            _mockPersistenceService.Setup(s => s.Exists(slot0Path)).Returns(true);
            _mockPersistenceService.Setup(s => s.Exists(slot1Path)).Returns(true);
            _mockPersistenceService.Setup(s => s.Exists(slot3Path)).Returns(true);

            // Act
            var slot = _slotManager.GetFirstEmptySlot();

            // Assert
            Assert.Equal(2, slot);
        }

        #endregion

        #region GetOccupiedSlotCount Tests

        [Fact]
        public void GetOccupiedSlotCount_WhenAllSlotsEmpty_ShouldReturnZero()
        {
            // Arrange
            _mockPersistenceService.Setup(s => s.Exists(It.IsAny<string>())).Returns(false);

            // Act
            var count = _slotManager.GetOccupiedSlotCount();

            // Assert
            Assert.Equal(0, count);
        }

        [Fact]
        public void GetOccupiedSlotCount_WithOccupiedSlots_ShouldReturnCorrectCount()
        {
            // Arrange - slots 0, 2, 5 are occupied
            var slot0Path = _slotManager.GetSlotFilePath(0);
            var slot2Path = _slotManager.GetSlotFilePath(2);
            var slot5Path = _slotManager.GetSlotFilePath(5);
            _mockPersistenceService.Setup(s => s.Exists(It.IsAny<string>())).Returns(false);
            _mockPersistenceService.Setup(s => s.Exists(slot0Path)).Returns(true);
            _mockPersistenceService.Setup(s => s.Exists(slot2Path)).Returns(true);
            _mockPersistenceService.Setup(s => s.Exists(slot5Path)).Returns(true);

            // Act
            var count = _slotManager.GetOccupiedSlotCount();

            // Assert
            Assert.Equal(3, count);
        }

        [Fact]
        public void GetOccupiedSlotCount_WhenAllSlotsFull_ShouldReturnMaxSlots()
        {
            // Arrange
            _mockPersistenceService.Setup(s => s.Exists(It.IsAny<string>())).Returns(true);

            // Act
            var count = _slotManager.GetOccupiedSlotCount();

            // Assert
            Assert.Equal(10, count);
        }

        #endregion

        #region CopySlot Tests

        [Fact]
        public void CopySlot_ShouldCopyGameStateToDestinationSlot()
        {
            // Arrange
            var srcPath = _slotManager.GetSlotFilePath(0);
            var destPath = _slotManager.GetSlotFilePath(1);
            var gameState = new GameState { SaveName = "Original Save" };
            _mockPersistenceService.Setup(s => s.Exists(srcPath)).Returns(true);
            _mockPersistenceService.Setup(s => s.Load(srcPath)).Returns(gameState);

            // Act
            _slotManager.CopySlot(0, 1);

            // Assert
            _mockPersistenceService.Verify(s => s.Load(srcPath), Times.Once);
            _mockPersistenceService.Verify(s => s.Save(It.Is<GameState>(gs => gs.SaveName == "Original Save"), destPath), Times.Once);
        }

        [Fact]
        public void CopySlot_WhenSourceIsEmpty_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var srcPath = _slotManager.GetSlotFilePath(0);
            _mockPersistenceService.Setup(s => s.Exists(srcPath)).Returns(false);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => _slotManager.CopySlot(0, 1));
        }

        [Fact]
        public void CopySlot_WithSameSourceAndDestination_ShouldThrowArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => _slotManager.CopySlot(0, 0));
        }

        [Fact]
        public void CopySlot_WithInvalidSlots_ShouldThrowArgumentOutOfRangeException()
        {
            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => _slotManager.CopySlot(-1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => _slotManager.CopySlot(0, 10));
        }

        #endregion

        #region SaveDirectory Property Tests

        [Fact]
        public void SaveDirectory_ShouldReturnConfiguredDirectory()
        {
            // Assert
            Assert.Equal(_testDirectory, _slotManager.SaveDirectory);
        }

        #endregion
    }

    #region SaveSlotInfo Model Tests

    public class SaveSlotInfoTests
    {
        [Fact]
        public void Constructor_ShouldSetSlotNumber()
        {
            // Act
            var info = new SaveSlotInfo(5);

            // Assert
            Assert.Equal(5, info.SlotNumber);
            Assert.False(info.IsOccupied);
        }

        [Fact]
        public void SetOccupied_ShouldSetAllProperties()
        {
            // Arrange
            var info = new SaveSlotInfo(0);
            var modifiedAt = DateTime.UtcNow;

            // Act
            info.SetOccupied("Test Save", modifiedAt, 3600);

            // Assert
            Assert.True(info.IsOccupied);
            Assert.Equal("Test Save", info.SaveName);
            Assert.Equal(modifiedAt, info.ModifiedAt);
            Assert.Equal(3600, info.TotalPlayTimeSeconds);
        }

        [Fact]
        public void FormattedPlayTime_ShouldReturnCorrectFormat()
        {
            // Arrange
            var info = new SaveSlotInfo(0);
            info.SetOccupied("Test", DateTime.UtcNow, 3725); // 1h 2m 5s

            // Act
            var formatted = info.FormattedPlayTime;

            // Assert
            Assert.Equal("1h 2m", formatted);
        }

        [Fact]
        public void FormattedPlayTime_WithZeroTime_ShouldReturnZeroMinutes()
        {
            // Arrange
            var info = new SaveSlotInfo(0);
            info.SetOccupied("Test", DateTime.UtcNow, 0);

            // Act
            var formatted = info.FormattedPlayTime;

            // Assert
            Assert.Equal("0m", formatted);
        }

        [Fact]
        public void FormattedPlayTime_WithOnlyMinutes_ShouldOmitHours()
        {
            // Arrange
            var info = new SaveSlotInfo(0);
            info.SetOccupied("Test", DateTime.UtcNow, 1800); // 30 minutes

            // Act
            var formatted = info.FormattedPlayTime;

            // Assert
            Assert.Equal("30m", formatted);
        }
    }

    #endregion
}
