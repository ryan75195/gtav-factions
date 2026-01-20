using FactionWars.Core.Interfaces;
using FactionWars.ScriptHookV.Persistence;
using Moq;
using System;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV.Persistence
{
    public class GameStateCoordinatorTests
    {
        private readonly Mock<IGameStateManager> _mockGameStateManager;
        private readonly GameStateCoordinator _sut;

        public GameStateCoordinatorTests()
        {
            _mockGameStateManager = new Mock<IGameStateManager>();
            _sut = new GameStateCoordinator(_mockGameStateManager.Object);
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithNullGameStateManager_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new GameStateCoordinator(null!));
        }

        [Fact]
        public void Constructor_WithValidGameStateManager_CreatesInstance()
        {
            var coordinator = new GameStateCoordinator(_mockGameStateManager.Object);
            Assert.NotNull(coordinator);
        }

        #endregion

        #region SaveToSlot Tests

        [Fact]
        public void SaveToSlot_DelegatesToGameStateManager()
        {
            // Arrange
            _mockGameStateManager.Setup(m => m.HasGameLoaded).Returns(true);

            // Act
            _sut.SaveToSlot(0);

            // Assert
            _mockGameStateManager.Verify(m => m.SaveToSlot(0, null), Times.Once);
        }

        [Fact]
        public void SaveToSlot_WithDifferentSlotNumbers_PassesCorrectSlot()
        {
            // Arrange
            _mockGameStateManager.Setup(m => m.HasGameLoaded).Returns(true);

            // Act
            _sut.SaveToSlot(5);

            // Assert
            _mockGameStateManager.Verify(m => m.SaveToSlot(5, null), Times.Once);
        }

        [Fact]
        public void SaveToSlot_SetsIsSavingDuringSave()
        {
            // Arrange
            _mockGameStateManager.Setup(m => m.HasGameLoaded).Returns(true);
            bool wasSavingDuringSave = false;
            _mockGameStateManager
                .Setup(m => m.SaveToSlot(It.IsAny<int>(), It.IsAny<string?>()))
                .Callback(() => wasSavingDuringSave = _sut.IsSaving);

            // Act
            _sut.SaveToSlot(0);

            // Assert
            Assert.True(wasSavingDuringSave);
            Assert.False(_sut.IsSaving); // Should be false after completion
        }

        #endregion

        #region LoadFromSlot Tests

        [Fact]
        public void LoadFromSlot_DelegatesToGameStateManager()
        {
            // Act
            _sut.LoadFromSlot(0);

            // Assert
            _mockGameStateManager.Verify(m => m.LoadFromSlot(0), Times.Once);
        }

        [Fact]
        public void LoadFromSlot_WithDifferentSlotNumbers_PassesCorrectSlot()
        {
            // Act
            _sut.LoadFromSlot(7);

            // Assert
            _mockGameStateManager.Verify(m => m.LoadFromSlot(7), Times.Once);
        }

        [Fact]
        public void LoadFromSlot_SetsIsLoadingDuringLoad()
        {
            // Arrange
            bool wasLoadingDuringLoad = false;
            _mockGameStateManager
                .Setup(m => m.LoadFromSlot(It.IsAny<int>()))
                .Callback(() => wasLoadingDuringLoad = _sut.IsLoading);

            // Act
            _sut.LoadFromSlot(0);

            // Assert
            Assert.True(wasLoadingDuringLoad);
            Assert.False(_sut.IsLoading); // Should be false after completion
        }

        #endregion

        #region IsSaving Property Tests

        [Fact]
        public void IsSaving_InitiallyFalse()
        {
            Assert.False(_sut.IsSaving);
        }

        #endregion

        #region IsLoading Property Tests

        [Fact]
        public void IsLoading_InitiallyFalse()
        {
            Assert.False(_sut.IsLoading);
        }

        #endregion

        #region Exception Handling Tests

        [Fact]
        public void SaveToSlot_WhenExceptionOccurs_ResetsIsSaving()
        {
            // Arrange
            _mockGameStateManager.Setup(m => m.HasGameLoaded).Returns(true);
            _mockGameStateManager
                .Setup(m => m.SaveToSlot(It.IsAny<int>(), It.IsAny<string?>()))
                .Throws(new InvalidOperationException("Save failed"));

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => _sut.SaveToSlot(0));
            Assert.False(_sut.IsSaving);
            Assert.Equal("Save failed", exception.Message);
        }

        [Fact]
        public void LoadFromSlot_WhenExceptionOccurs_ResetsIsLoading()
        {
            // Arrange
            _mockGameStateManager
                .Setup(m => m.LoadFromSlot(It.IsAny<int>()))
                .Throws(new InvalidOperationException("Load failed"));

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => _sut.LoadFromSlot(0));
            Assert.False(_sut.IsLoading);
            Assert.Equal("Load failed", exception.Message);
        }

        #endregion
    }
}
