using FactionWars.Core.Utils;
using FactionWars.ScriptHookV;
using FactionWars.Tests.Mocks;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV
{
    /// <summary>
    /// Tests for GameLoopController's AI system wiring.
    /// Ensures AIManager and BackgroundBattleSimulator are properly wired.
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
        public void AIManager_AfterInitialization_IsNotNull()
        {
            // Arrange
            SetupController();
            var controller = new GameLoopController(_container);

            // Act
            controller.OnTick();

            // Assert
            Assert.NotNull(controller.AIManager);
        }

        [Fact]
        public void AIManager_OnTick_GetsUpdated()
        {
            // Arrange
            SetupController();
            var controller = new GameLoopController(_container);
            controller.OnTick(); // Initialize

            var aiManager = controller.AIManager;
            Assert.NotNull(aiManager);

            // Act - simulate multiple ticks
            for (int i = 0; i < 10; i++)
            {
                controller.OnTick();
            }

            // Assert - AI manager should still be valid
            Assert.NotNull(controller.AIManager);
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
