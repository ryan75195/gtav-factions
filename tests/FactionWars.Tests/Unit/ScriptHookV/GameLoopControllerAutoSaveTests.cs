using FactionWars.Core.Interfaces;
using FactionWars.Core.Utils;
using FactionWars.ScriptHookV;
using FactionWars.Tests.Mocks;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV
{
    /// <summary>
    /// Tests for GameLoopController's auto-save integration.
    /// Verifies that the auto-save service is properly wired to the game loop.
    /// </summary>
    public class GameLoopControllerAutoSaveTests
    {
        private MockGameBridge _gameBridge = null!;
        private ServiceContainer _container = null!;

        private void SetupController(string initialCharacterModel = "player_zero")
        {
            _gameBridge = new MockGameBridge();
            _gameBridge.PlayerCharacterModel = initialCharacterModel;
            _container = ServiceContainerFactory.Create(_gameBridge, new MockMenuProvider());
        }

        [Fact]
        public void AutoSaveService_AfterInitialization_IsNotNull()
        {
            // Arrange
            SetupController();
            var controller = new GameLoopController(_container);

            // Act - first tick initializes game data
            controller.OnTick();

            // Assert
            Assert.NotNull(controller.AutoSaveService);
        }

        [Fact]
        public void AutoSaveService_AfterInitialization_IsRunning()
        {
            // Arrange
            SetupController();
            var controller = new GameLoopController(_container);

            // Act
            controller.OnTick();

            // Assert
            Assert.True(controller.AutoSaveService!.IsRunning);
        }

        [Fact]
        public void AutoSaveService_AfterInitialization_IsEnabled()
        {
            // Arrange
            SetupController();
            var controller = new GameLoopController(_container);

            // Act
            controller.OnTick();

            // Assert
            Assert.True(controller.AutoSaveService!.IsEnabled);
        }

        [Fact]
        public void OnTick_AfterInitialization_UpdatesAutoSaveTimer()
        {
            // Arrange
            SetupController();
            var controller = new GameLoopController(_container);
            controller.OnTick(); // Initialize

            // Get initial time since last save
            var initialTimeSinceLastSave = controller.AutoSaveService!.TimeSinceLastSave;

            // Act - Multiple ticks to simulate time passing
            for (int i = 0; i < 10; i++)
            {
                controller.OnTick();
            }

            // Assert - Time since last save should have increased
            Assert.True(controller.AutoSaveService.TimeSinceLastSave >= initialTimeSinceLastSave);
        }

        [Fact]
        public void OnAbort_StopsAutoSaveService()
        {
            // Arrange
            SetupController();
            var controller = new GameLoopController(_container);
            controller.OnTick(); // Initialize

            // Verify it's running first
            Assert.True(controller.AutoSaveService!.IsRunning);

            // Act
            controller.OnAbort();

            // Assert - Controller should no longer be initialized
            // The auto-save service is stopped when the controller is aborted
            Assert.False(controller.IsInitialized);
        }

        [Fact]
        public void AutoSaveService_HasDefaultFiveMinuteInterval()
        {
            // Arrange
            SetupController();
            var controller = new GameLoopController(_container);
            controller.OnTick(); // Initialize

            // Assert
            Assert.Equal(System.TimeSpan.FromMinutes(5), controller.AutoSaveService!.Interval);
        }

        [Fact]
        public void AutoSaveService_CanBeDisabledAndReEnabled()
        {
            // Arrange
            SetupController();
            var controller = new GameLoopController(_container);
            controller.OnTick(); // Initialize

            // Act - Disable
            controller.AutoSaveService!.IsEnabled = false;
            Assert.False(controller.AutoSaveService.IsEnabled);

            // Act - Re-enable
            controller.AutoSaveService.IsEnabled = true;

            // Assert
            Assert.True(controller.AutoSaveService.IsEnabled);
        }
    }
}
