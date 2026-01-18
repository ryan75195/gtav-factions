using FactionWars.Core.Interfaces;
using FactionWars.Persistence;
using FactionWars.Persistence.Models;
using Moq;
using System;
using Xunit;

namespace FactionWars.Tests.Unit.Persistence
{
    /// <summary>
    /// Unit tests for AutoSaveService functionality.
    /// </summary>
    public class AutoSaveServiceTests
    {
        private readonly Mock<IPersistenceService> _mockPersistenceService;
        private readonly Mock<IGameStateProvider> _mockGameStateProvider;
        private const string DefaultAutoSaveDirectory = "C:\\Saves";
        private const string DefaultAutoSaveFileName = "autosave.json";

        public AutoSaveServiceTests()
        {
            _mockPersistenceService = new Mock<IPersistenceService>();
            _mockGameStateProvider = new Mock<IGameStateProvider>();
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithValidParameters_CreatesInstance()
        {
            // Arrange & Act
            var service = new AutoSaveService(
                _mockPersistenceService.Object,
                _mockGameStateProvider.Object,
                DefaultAutoSaveDirectory);

            // Assert
            Assert.NotNull(service);
            Assert.False(service.IsRunning);
        }

        [Fact]
        public void Constructor_WithNullPersistenceService_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() => new AutoSaveService(
                null!,
                _mockGameStateProvider.Object,
                DefaultAutoSaveDirectory));
        }

        [Fact]
        public void Constructor_WithNullGameStateProvider_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() => new AutoSaveService(
                _mockPersistenceService.Object,
                null!,
                DefaultAutoSaveDirectory));
        }

        [Fact]
        public void Constructor_WithNullSaveDirectory_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() => new AutoSaveService(
                _mockPersistenceService.Object,
                _mockGameStateProvider.Object,
                null!));
        }

        [Fact]
        public void Constructor_WithEmptySaveDirectory_ThrowsArgumentException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentException>(() => new AutoSaveService(
                _mockPersistenceService.Object,
                _mockGameStateProvider.Object,
                string.Empty));
        }

        [Fact]
        public void Constructor_WithCustomInterval_SetsInterval()
        {
            // Arrange
            var customInterval = TimeSpan.FromMinutes(10);

            // Act
            var service = new AutoSaveService(
                _mockPersistenceService.Object,
                _mockGameStateProvider.Object,
                DefaultAutoSaveDirectory,
                customInterval);

            // Assert
            Assert.Equal(customInterval, service.Interval);
        }

        [Fact]
        public void Constructor_WithZeroInterval_ThrowsArgumentException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentException>(() => new AutoSaveService(
                _mockPersistenceService.Object,
                _mockGameStateProvider.Object,
                DefaultAutoSaveDirectory,
                TimeSpan.Zero));
        }

        [Fact]
        public void Constructor_WithNegativeInterval_ThrowsArgumentException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentException>(() => new AutoSaveService(
                _mockPersistenceService.Object,
                _mockGameStateProvider.Object,
                DefaultAutoSaveDirectory,
                TimeSpan.FromMinutes(-5)));
        }

        [Fact]
        public void Constructor_DefaultInterval_IsFiveMinutes()
        {
            // Arrange & Act
            var service = new AutoSaveService(
                _mockPersistenceService.Object,
                _mockGameStateProvider.Object,
                DefaultAutoSaveDirectory);

            // Assert
            Assert.Equal(TimeSpan.FromMinutes(5), service.Interval);
        }

        [Fact]
        public void Constructor_SetsAutoSavePath_Correctly()
        {
            // Arrange & Act
            var service = new AutoSaveService(
                _mockPersistenceService.Object,
                _mockGameStateProvider.Object,
                DefaultAutoSaveDirectory);

            // Assert
            var expectedPath = System.IO.Path.Combine(DefaultAutoSaveDirectory, DefaultAutoSaveFileName);
            Assert.Equal(expectedPath, service.AutoSaveFilePath);
        }

        [Fact]
        public void Constructor_WithCustomFileName_SetsPath()
        {
            // Arrange
            var customFileName = "custom_autosave.json";

            // Act
            var service = new AutoSaveService(
                _mockPersistenceService.Object,
                _mockGameStateProvider.Object,
                DefaultAutoSaveDirectory,
                TimeSpan.FromMinutes(5),
                customFileName);

            // Assert
            var expectedPath = System.IO.Path.Combine(DefaultAutoSaveDirectory, customFileName);
            Assert.Equal(expectedPath, service.AutoSaveFilePath);
        }

        #endregion

        #region Start/Stop Tests

        [Fact]
        public void Start_SetsIsRunningToTrue()
        {
            // Arrange
            var service = new AutoSaveService(
                _mockPersistenceService.Object,
                _mockGameStateProvider.Object,
                DefaultAutoSaveDirectory);

            // Act
            service.Start();

            // Assert
            Assert.True(service.IsRunning);
        }

        [Fact]
        public void Start_WhenAlreadyRunning_DoesNotThrow()
        {
            // Arrange
            var service = new AutoSaveService(
                _mockPersistenceService.Object,
                _mockGameStateProvider.Object,
                DefaultAutoSaveDirectory);
            service.Start();

            // Act & Assert
            var exception = Record.Exception(() => service.Start());
            Assert.Null(exception);
        }

        [Fact]
        public void Stop_SetsIsRunningToFalse()
        {
            // Arrange
            var service = new AutoSaveService(
                _mockPersistenceService.Object,
                _mockGameStateProvider.Object,
                DefaultAutoSaveDirectory);
            service.Start();

            // Act
            service.Stop();

            // Assert
            Assert.False(service.IsRunning);
        }

        [Fact]
        public void Stop_WhenNotRunning_DoesNotThrow()
        {
            // Arrange
            var service = new AutoSaveService(
                _mockPersistenceService.Object,
                _mockGameStateProvider.Object,
                DefaultAutoSaveDirectory);

            // Act & Assert
            var exception = Record.Exception(() => service.Stop());
            Assert.Null(exception);
        }

        [Fact]
        public void Start_AfterStop_CanRestartSuccessfully()
        {
            // Arrange
            var service = new AutoSaveService(
                _mockPersistenceService.Object,
                _mockGameStateProvider.Object,
                DefaultAutoSaveDirectory);

            // Act
            service.Start();
            service.Stop();
            service.Start();

            // Assert
            Assert.True(service.IsRunning);
        }

        #endregion

        #region TriggerSave Tests

        [Fact]
        public void TriggerSave_SavesCurrentGameState()
        {
            // Arrange
            var gameState = new GameState { SaveName = "Auto Save" };
            _mockGameStateProvider.Setup(p => p.GetCurrentGameState()).Returns(gameState);

            var service = new AutoSaveService(
                _mockPersistenceService.Object,
                _mockGameStateProvider.Object,
                DefaultAutoSaveDirectory);

            // Act
            service.TriggerSave();

            // Assert
            _mockPersistenceService.Verify(
                p => p.Save(It.IsAny<GameState>(), service.AutoSaveFilePath),
                Times.Once);
        }

        [Fact]
        public void TriggerSave_UpdatesSaveNameToAutoSave()
        {
            // Arrange
            var gameState = new GameState { SaveName = "Original Name" };
            _mockGameStateProvider.Setup(p => p.GetCurrentGameState()).Returns(gameState);

            GameState? savedState = null;
            _mockPersistenceService.Setup(p => p.Save(It.IsAny<GameState>(), It.IsAny<string>()))
                .Callback<GameState, string>((state, path) => savedState = state);

            var service = new AutoSaveService(
                _mockPersistenceService.Object,
                _mockGameStateProvider.Object,
                DefaultAutoSaveDirectory);

            // Act
            service.TriggerSave();

            // Assert
            Assert.NotNull(savedState);
            Assert.Equal("Auto Save", savedState.SaveName);
        }

        [Fact]
        public void TriggerSave_UpdatesModifiedTimestamp()
        {
            // Arrange
            var oldTimestamp = DateTime.UtcNow.AddDays(-1);
            var gameState = new GameState { ModifiedAt = oldTimestamp };
            _mockGameStateProvider.Setup(p => p.GetCurrentGameState()).Returns(gameState);

            GameState? savedState = null;
            _mockPersistenceService.Setup(p => p.Save(It.IsAny<GameState>(), It.IsAny<string>()))
                .Callback<GameState, string>((state, path) => savedState = state);

            var service = new AutoSaveService(
                _mockPersistenceService.Object,
                _mockGameStateProvider.Object,
                DefaultAutoSaveDirectory);

            // Act
            service.TriggerSave();

            // Assert
            Assert.NotNull(savedState);
            Assert.True(savedState.ModifiedAt > oldTimestamp);
        }

        [Fact]
        public void TriggerSave_WhenGameStateProviderReturnsNull_DoesNotSave()
        {
            // Arrange
            _mockGameStateProvider.Setup(p => p.GetCurrentGameState()).Returns((GameState?)null);

            var service = new AutoSaveService(
                _mockPersistenceService.Object,
                _mockGameStateProvider.Object,
                DefaultAutoSaveDirectory);

            // Act
            service.TriggerSave();

            // Assert
            _mockPersistenceService.Verify(
                p => p.Save(It.IsAny<GameState>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public void TriggerSave_UpdatesLastSaveTime()
        {
            // Arrange
            var gameState = new GameState();
            _mockGameStateProvider.Setup(p => p.GetCurrentGameState()).Returns(gameState);

            var service = new AutoSaveService(
                _mockPersistenceService.Object,
                _mockGameStateProvider.Object,
                DefaultAutoSaveDirectory);

            Assert.Null(service.LastSaveTime);

            // Act
            service.TriggerSave();

            // Assert
            Assert.NotNull(service.LastSaveTime);
            Assert.True(DateTime.UtcNow - service.LastSaveTime.Value < TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void TriggerSave_IncrementsAutoSaveCount()
        {
            // Arrange
            var gameState = new GameState();
            _mockGameStateProvider.Setup(p => p.GetCurrentGameState()).Returns(gameState);

            var service = new AutoSaveService(
                _mockPersistenceService.Object,
                _mockGameStateProvider.Object,
                DefaultAutoSaveDirectory);

            Assert.Equal(0, service.AutoSaveCount);

            // Act
            service.TriggerSave();
            service.TriggerSave();

            // Assert
            Assert.Equal(2, service.AutoSaveCount);
        }

        #endregion

        #region Event Tests

        [Fact]
        public void TriggerSave_RaisesAutoSaveStartedEvent()
        {
            // Arrange
            var gameState = new GameState();
            _mockGameStateProvider.Setup(p => p.GetCurrentGameState()).Returns(gameState);

            var service = new AutoSaveService(
                _mockPersistenceService.Object,
                _mockGameStateProvider.Object,
                DefaultAutoSaveDirectory);

            bool eventRaised = false;
            service.AutoSaveStarted += (sender, args) => eventRaised = true;

            // Act
            service.TriggerSave();

            // Assert
            Assert.True(eventRaised);
        }

        [Fact]
        public void TriggerSave_OnSuccess_RaisesAutoSaveCompletedEvent()
        {
            // Arrange
            var gameState = new GameState();
            _mockGameStateProvider.Setup(p => p.GetCurrentGameState()).Returns(gameState);

            var service = new AutoSaveService(
                _mockPersistenceService.Object,
                _mockGameStateProvider.Object,
                DefaultAutoSaveDirectory);

            bool eventRaised = false;
            AutoSaveCompletedEventArgs? eventArgs = null;
            service.AutoSaveCompleted += (sender, args) =>
            {
                eventRaised = true;
                eventArgs = args;
            };

            // Act
            service.TriggerSave();

            // Assert
            Assert.True(eventRaised);
            Assert.NotNull(eventArgs);
            Assert.True(eventArgs.Success);
            Assert.Null(eventArgs.Error);
        }

        [Fact]
        public void TriggerSave_OnFailure_RaisesAutoSaveCompletedEventWithError()
        {
            // Arrange
            var gameState = new GameState();
            _mockGameStateProvider.Setup(p => p.GetCurrentGameState()).Returns(gameState);
            _mockPersistenceService.Setup(p => p.Save(It.IsAny<GameState>(), It.IsAny<string>()))
                .Throws(new InvalidOperationException("Save failed"));

            var service = new AutoSaveService(
                _mockPersistenceService.Object,
                _mockGameStateProvider.Object,
                DefaultAutoSaveDirectory);

            bool eventRaised = false;
            AutoSaveCompletedEventArgs? eventArgs = null;
            service.AutoSaveCompleted += (sender, args) =>
            {
                eventRaised = true;
                eventArgs = args;
            };

            // Act
            service.TriggerSave();

            // Assert
            Assert.True(eventRaised);
            Assert.NotNull(eventArgs);
            Assert.False(eventArgs.Success);
            Assert.NotNull(eventArgs.Error);
            Assert.IsType<InvalidOperationException>(eventArgs.Error);
        }

        [Fact]
        public void TriggerSave_OnFailure_DoesNotIncrementAutoSaveCount()
        {
            // Arrange
            var gameState = new GameState();
            _mockGameStateProvider.Setup(p => p.GetCurrentGameState()).Returns(gameState);
            _mockPersistenceService.Setup(p => p.Save(It.IsAny<GameState>(), It.IsAny<string>()))
                .Throws(new InvalidOperationException("Save failed"));

            var service = new AutoSaveService(
                _mockPersistenceService.Object,
                _mockGameStateProvider.Object,
                DefaultAutoSaveDirectory);

            // Act
            service.TriggerSave();

            // Assert
            Assert.Equal(0, service.AutoSaveCount);
        }

        #endregion

        #region Update (Tick) Tests

        [Fact]
        public void Update_WhenNotRunning_DoesNotSave()
        {
            // Arrange
            var gameState = new GameState();
            _mockGameStateProvider.Setup(p => p.GetCurrentGameState()).Returns(gameState);

            var service = new AutoSaveService(
                _mockPersistenceService.Object,
                _mockGameStateProvider.Object,
                DefaultAutoSaveDirectory,
                TimeSpan.FromMilliseconds(100));

            // Act
            service.Update(TimeSpan.FromMilliseconds(200));

            // Assert
            _mockPersistenceService.Verify(
                p => p.Save(It.IsAny<GameState>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public void Update_WhenRunning_AccumulatesTime()
        {
            // Arrange
            var service = new AutoSaveService(
                _mockPersistenceService.Object,
                _mockGameStateProvider.Object,
                DefaultAutoSaveDirectory,
                TimeSpan.FromMinutes(5));
            service.Start();

            // Act
            service.Update(TimeSpan.FromMinutes(2));

            // Assert
            Assert.Equal(TimeSpan.FromMinutes(2), service.TimeSinceLastSave);
        }

        [Fact]
        public void Update_WhenIntervalReached_TriggersSave()
        {
            // Arrange
            var gameState = new GameState();
            _mockGameStateProvider.Setup(p => p.GetCurrentGameState()).Returns(gameState);

            var service = new AutoSaveService(
                _mockPersistenceService.Object,
                _mockGameStateProvider.Object,
                DefaultAutoSaveDirectory,
                TimeSpan.FromMinutes(5));
            service.Start();

            // Act
            service.Update(TimeSpan.FromMinutes(5));

            // Assert
            _mockPersistenceService.Verify(
                p => p.Save(It.IsAny<GameState>(), It.IsAny<string>()),
                Times.Once);
        }

        [Fact]
        public void Update_WhenIntervalReached_ResetsTimer()
        {
            // Arrange
            var gameState = new GameState();
            _mockGameStateProvider.Setup(p => p.GetCurrentGameState()).Returns(gameState);

            var service = new AutoSaveService(
                _mockPersistenceService.Object,
                _mockGameStateProvider.Object,
                DefaultAutoSaveDirectory,
                TimeSpan.FromMinutes(5));
            service.Start();

            // Act
            service.Update(TimeSpan.FromMinutes(6));

            // Assert
            Assert.Equal(TimeSpan.FromMinutes(1), service.TimeSinceLastSave);
        }

        [Fact]
        public void Update_WhenIntervalExceededMultipleTimes_SavesOnlyOnce()
        {
            // Arrange
            var gameState = new GameState();
            _mockGameStateProvider.Setup(p => p.GetCurrentGameState()).Returns(gameState);

            var service = new AutoSaveService(
                _mockPersistenceService.Object,
                _mockGameStateProvider.Object,
                DefaultAutoSaveDirectory,
                TimeSpan.FromMinutes(5));
            service.Start();

            // Act - pass time that would trigger 3 saves
            service.Update(TimeSpan.FromMinutes(17));

            // Assert - should only save once per Update call
            _mockPersistenceService.Verify(
                p => p.Save(It.IsAny<GameState>(), It.IsAny<string>()),
                Times.Once);
        }

        [Fact]
        public void Update_MultipleCallsAccumulatingToInterval_TriggersSave()
        {
            // Arrange
            var gameState = new GameState();
            _mockGameStateProvider.Setup(p => p.GetCurrentGameState()).Returns(gameState);

            var service = new AutoSaveService(
                _mockPersistenceService.Object,
                _mockGameStateProvider.Object,
                DefaultAutoSaveDirectory,
                TimeSpan.FromMinutes(5));
            service.Start();

            // Act
            service.Update(TimeSpan.FromMinutes(2));
            service.Update(TimeSpan.FromMinutes(2));
            service.Update(TimeSpan.FromMinutes(2));

            // Assert - 6 minutes total, should trigger once at 5 minutes
            _mockPersistenceService.Verify(
                p => p.Save(It.IsAny<GameState>(), It.IsAny<string>()),
                Times.Once);
        }

        #endregion

        #region HasAutoSave Tests

        [Fact]
        public void HasAutoSave_WhenAutoSaveExists_ReturnsTrue()
        {
            // Arrange
            var service = new AutoSaveService(
                _mockPersistenceService.Object,
                _mockGameStateProvider.Object,
                DefaultAutoSaveDirectory);

            _mockPersistenceService.Setup(p => p.Exists(service.AutoSaveFilePath)).Returns(true);

            // Act
            var result = service.HasAutoSave();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void HasAutoSave_WhenAutoSaveDoesNotExist_ReturnsFalse()
        {
            // Arrange
            var service = new AutoSaveService(
                _mockPersistenceService.Object,
                _mockGameStateProvider.Object,
                DefaultAutoSaveDirectory);

            _mockPersistenceService.Setup(p => p.Exists(service.AutoSaveFilePath)).Returns(false);

            // Act
            var result = service.HasAutoSave();

            // Assert
            Assert.False(result);
        }

        #endregion

        #region LoadAutoSave Tests

        [Fact]
        public void LoadAutoSave_WhenAutoSaveExists_ReturnsGameState()
        {
            // Arrange
            var expectedGameState = new GameState { SaveName = "Auto Save" };
            var service = new AutoSaveService(
                _mockPersistenceService.Object,
                _mockGameStateProvider.Object,
                DefaultAutoSaveDirectory);

            _mockPersistenceService.Setup(p => p.Exists(service.AutoSaveFilePath)).Returns(true);
            _mockPersistenceService.Setup(p => p.Load(service.AutoSaveFilePath)).Returns(expectedGameState);

            // Act
            var result = service.LoadAutoSave();

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Auto Save", result.SaveName);
        }

        [Fact]
        public void LoadAutoSave_WhenNoAutoSaveExists_ThrowsInvalidOperationException()
        {
            // Arrange
            var service = new AutoSaveService(
                _mockPersistenceService.Object,
                _mockGameStateProvider.Object,
                DefaultAutoSaveDirectory);

            _mockPersistenceService.Setup(p => p.Exists(service.AutoSaveFilePath)).Returns(false);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => service.LoadAutoSave());
        }

        #endregion

        #region DeleteAutoSave Tests

        [Fact]
        public void DeleteAutoSave_CallsDeleteOnPersistenceService()
        {
            // Arrange
            var service = new AutoSaveService(
                _mockPersistenceService.Object,
                _mockGameStateProvider.Object,
                DefaultAutoSaveDirectory);

            // Act
            service.DeleteAutoSave();

            // Assert
            _mockPersistenceService.Verify(
                p => p.Delete(service.AutoSaveFilePath),
                Times.Once);
        }

        #endregion

        #region Enabled/Disabled Tests

        [Fact]
        public void IsEnabled_DefaultsToTrue()
        {
            // Arrange & Act
            var service = new AutoSaveService(
                _mockPersistenceService.Object,
                _mockGameStateProvider.Object,
                DefaultAutoSaveDirectory);

            // Assert
            Assert.True(service.IsEnabled);
        }

        [Fact]
        public void SetEnabled_ToFalse_DisablesAutoSave()
        {
            // Arrange
            var gameState = new GameState();
            _mockGameStateProvider.Setup(p => p.GetCurrentGameState()).Returns(gameState);

            var service = new AutoSaveService(
                _mockPersistenceService.Object,
                _mockGameStateProvider.Object,
                DefaultAutoSaveDirectory,
                TimeSpan.FromMinutes(5));
            service.Start();
            service.IsEnabled = false;

            // Act
            service.Update(TimeSpan.FromMinutes(10));

            // Assert - should not save when disabled
            _mockPersistenceService.Verify(
                p => p.Save(It.IsAny<GameState>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public void SetEnabled_ToTrue_ReEnablesAutoSave()
        {
            // Arrange
            var gameState = new GameState();
            _mockGameStateProvider.Setup(p => p.GetCurrentGameState()).Returns(gameState);

            var service = new AutoSaveService(
                _mockPersistenceService.Object,
                _mockGameStateProvider.Object,
                DefaultAutoSaveDirectory,
                TimeSpan.FromMinutes(5));
            service.Start();
            service.IsEnabled = false;
            service.Update(TimeSpan.FromMinutes(6));
            service.IsEnabled = true;

            // Act
            service.Update(TimeSpan.FromMinutes(5));

            // Assert
            _mockPersistenceService.Verify(
                p => p.Save(It.IsAny<GameState>(), It.IsAny<string>()),
                Times.Once);
        }

        #endregion

        #region Interval Change Tests

        [Fact]
        public void SetInterval_ChangesInterval()
        {
            // Arrange
            var service = new AutoSaveService(
                _mockPersistenceService.Object,
                _mockGameStateProvider.Object,
                DefaultAutoSaveDirectory,
                TimeSpan.FromMinutes(5));

            // Act
            service.SetInterval(TimeSpan.FromMinutes(10));

            // Assert
            Assert.Equal(TimeSpan.FromMinutes(10), service.Interval);
        }

        [Fact]
        public void SetInterval_WithZero_ThrowsArgumentException()
        {
            // Arrange
            var service = new AutoSaveService(
                _mockPersistenceService.Object,
                _mockGameStateProvider.Object,
                DefaultAutoSaveDirectory);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => service.SetInterval(TimeSpan.Zero));
        }

        [Fact]
        public void SetInterval_WithNegative_ThrowsArgumentException()
        {
            // Arrange
            var service = new AutoSaveService(
                _mockPersistenceService.Object,
                _mockGameStateProvider.Object,
                DefaultAutoSaveDirectory);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => service.SetInterval(TimeSpan.FromMinutes(-5)));
        }

        #endregion

        #region ResetTimer Tests

        [Fact]
        public void ResetTimer_ResetsTimeSinceLastSave()
        {
            // Arrange
            var service = new AutoSaveService(
                _mockPersistenceService.Object,
                _mockGameStateProvider.Object,
                DefaultAutoSaveDirectory,
                TimeSpan.FromMinutes(5));
            service.Start();
            service.Update(TimeSpan.FromMinutes(3));

            // Act
            service.ResetTimer();

            // Assert
            Assert.Equal(TimeSpan.Zero, service.TimeSinceLastSave);
        }

        #endregion

        #region Dispose Tests

        [Fact]
        public void Dispose_StopsService()
        {
            // Arrange
            var service = new AutoSaveService(
                _mockPersistenceService.Object,
                _mockGameStateProvider.Object,
                DefaultAutoSaveDirectory);
            service.Start();

            // Act
            service.Dispose();

            // Assert
            Assert.False(service.IsRunning);
        }

        [Fact]
        public void Dispose_CanBeCalledMultipleTimes()
        {
            // Arrange
            var service = new AutoSaveService(
                _mockPersistenceService.Object,
                _mockGameStateProvider.Object,
                DefaultAutoSaveDirectory);

            // Act & Assert
            var exception = Record.Exception(() =>
            {
                service.Dispose();
                service.Dispose();
            });

            Assert.Null(exception);
        }

        #endregion
    }
}
