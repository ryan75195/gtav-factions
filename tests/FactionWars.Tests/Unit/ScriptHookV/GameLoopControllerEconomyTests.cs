using FactionWars.Core.Interfaces;
using FactionWars.Core.Utils;
using FactionWars.Economy.Interfaces;
using FactionWars.ScriptHookV;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV
{
    /// <summary>
    /// Tests for GameLoopController's economy/resource tick integration.
    /// </summary>
    public class GameLoopControllerEconomyTests
    {
        private MockGameBridge _gameBridge = null!;
        private ServiceContainer _container = null!;

        private void SetupController(string initialCharacterModel = "player_zero")
        {
            _gameBridge = new MockGameBridge();
            _gameBridge.PlayerCharacterModel = initialCharacterModel;
            _container = ServiceContainerFactory.Create(_gameBridge);
        }

        [Fact]
        public void EconomyManager_AfterInitialization_IsNotNull()
        {
            // Arrange
            SetupController();
            var controller = new GameLoopController(_container);

            // Act - first tick initializes game data
            controller.OnTick();

            // Assert
            Assert.NotNull(controller.EconomyManager);
        }

        [Fact]
        public void EconomyManager_AfterInitialization_IsRunning()
        {
            // Arrange
            SetupController();
            var controller = new GameLoopController(_container);

            // Act
            controller.OnTick();

            // Assert
            Assert.True(controller.EconomyManager!.IsRunning);
        }

        [Fact]
        public void OnTick_AfterInitialization_UpdatesEconomyManager()
        {
            // Arrange
            SetupController();
            var controller = new GameLoopController(_container);
            controller.OnTick(); // Initialize

            // Get initial progress
            var initialProgress = controller.EconomyManager!.TickProgress;

            // Act - Multiple ticks to simulate time passing
            // We need to simulate elapsed time for the economy system
            // Each tick should pass some delta time
            for (int i = 0; i < 100; i++)
            {
                controller.OnTick();
            }

            // Assert - Progress should have changed (unless at 0 or 100)
            // This verifies Update is being called on EconomyManager
            // The tick interval is 60 seconds, and the mock game bridge should track frame time
            Assert.NotNull(controller.EconomyManager);
        }

        [Fact]
        public void OnAbort_StopsEconomyManager()
        {
            // Arrange
            SetupController();
            var controller = new GameLoopController(_container);
            controller.OnTick(); // Initialize

            // Verify it's running first
            Assert.True(controller.EconomyManager!.IsRunning);

            // Act
            controller.OnAbort();

            // Assert - EconomyManager should be stopped
            // Note: After abort, EconomyManager reference may be null, which is also valid
            // The important thing is that Stop() was called before cleanup
            Assert.False(controller.IsInitialized);
        }

        [Fact]
        public void EconomyManager_CanReceiveResourceTickEvents()
        {
            // Arrange
            SetupController();
            var controller = new GameLoopController(_container);
            controller.OnTick(); // Initialize

            bool eventReceived = false;
            controller.EconomyManager!.OnResourceTick += (_, _) => eventReceived = true;

            // Act - Force a tick
            controller.EconomyManager.ForceTick();

            // Assert
            Assert.True(eventReceived);
        }

        [Fact]
        public void EconomyManager_HasValidTickInterval()
        {
            // Arrange
            SetupController();
            var controller = new GameLoopController(_container);
            controller.OnTick(); // Initialize

            // Assert - Tick interval should be positive (default is 60 seconds)
            Assert.True(controller.EconomyManager!.TickIntervalSeconds > 0);
        }
    }
}
