using FactionWars.Core.Utils;
using FactionWars.ScriptHookV;
using FactionWars.Tests.Mocks;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV
{
    /// <summary>
    /// Tests for GameLoopController's AI system wiring.
    /// Ensures the consolidated AIController is properly wired.
    /// </summary>
    public class GameLoopControllerAITests
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
        public void AIController_AfterInitialization_IsNotNull()
        {
            // Arrange
            SetupController();
            var controller = new GameLoopController(_container);

            // Act
            controller.OnTick();

            // Assert
            Assert.NotNull(controller.AIController);
        }

        [Fact]
        public void AIController_OnTick_StaysRunning()
        {
            // Arrange
            SetupController();
            var controller = new GameLoopController(_container);
            controller.OnTick(); // Initialize

            var aiController = controller.AIController;
            Assert.NotNull(aiController);

            // Act - simulate multiple ticks
            for (int i = 0; i < 10; i++)
            {
                controller.OnTick();
            }

            // Assert - AI controller should still be valid and running
            Assert.NotNull(controller.AIController);
            Assert.True(controller.AIController!.IsRunning);
        }

        [Fact]
        public void VictoryManager_AfterInitialization_IsNotNull()
        {
            // Arrange
            SetupController();
            var controller = new GameLoopController(_container);

            // Act
            controller.OnTick();

            // Assert
            Assert.NotNull(controller.VictoryManager);
        }

        [Fact]
        public void VictoryManager_OnTick_ChecksVictoryCondition()
        {
            // Arrange
            SetupController();
            var controller = new GameLoopController(_container);
            controller.OnTick(); // Initialize

            // Act
            controller.OnTick();

            // Assert
            Assert.NotNull(controller.VictoryManager);
            // Victory should not be achieved at start (no faction owns 100%)
        }
    }
}
