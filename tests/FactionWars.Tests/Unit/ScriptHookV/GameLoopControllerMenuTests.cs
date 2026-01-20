using FactionWars.Core.Utils;
using FactionWars.ScriptHookV;
using FactionWars.ScriptHookV.UI;
using FactionWars.Tests.Mocks;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV
{
    /// <summary>
    /// Tests for GameLoopController's menu functionality.
    /// </summary>
    public class GameLoopControllerMenuTests
    {
        private const int F7KeyCode = 118;
        private MockGameBridge _gameBridge = null!;
        private ServiceContainer _container = null!;

        private GameLoopController SetupController()
        {
            _gameBridge = new MockGameBridge();
            _gameBridge.PlayerCharacterModel = "player_zero"; // Michael
            _container = ServiceContainerFactory.Create(_gameBridge, new MockMenuProvider());
            return new GameLoopController(_container);
        }

        #region MainMenuController Property Tests

        [Fact]
        public void MainMenuController_AfterInitialization_ShouldNotBeNull()
        {
            // Arrange
            var controller = SetupController();

            // Act - Initialize by calling first tick
            controller.OnTick();

            // Assert
            Assert.NotNull(controller.MainMenuController);
        }

        #endregion

        #region F7 Key Handling Tests

        [Fact]
        public void OnKeyDown_WithF7Key_ShouldOpenMenu()
        {
            // Arrange
            var controller = SetupController();
            controller.OnTick(); // Initialize

            // Act
            controller.OnKeyDown(F7KeyCode);

            // Assert
            Assert.True(controller.MainMenuController!.IsMenuOpen);
        }

        [Fact]
        public void OnKeyDown_WithF7KeyTwice_ShouldCloseMenu()
        {
            // Arrange
            var controller = SetupController();
            controller.OnTick(); // Initialize

            // Act
            controller.OnKeyDown(F7KeyCode); // Open
            controller.OnKeyDown(F7KeyCode); // Close

            // Assert
            Assert.False(controller.MainMenuController!.IsMenuOpen);
        }

        [Fact]
        public void OnKeyDown_WithOtherKey_ShouldNotOpenMenu()
        {
            // Arrange
            var controller = SetupController();
            controller.OnTick(); // Initialize

            // Act
            controller.OnKeyDown(65); // 'A' key

            // Assert
            Assert.False(controller.MainMenuController!.IsMenuOpen);
        }

        [Fact]
        public void OnKeyDown_BeforeInitialization_ShouldNotThrow()
        {
            // Arrange
            var controller = SetupController();
            // Note: not calling OnTick to skip initialization

            // Act & Assert - should not throw
            var exception = Record.Exception(() => controller.OnKeyDown(F7KeyCode));
            Assert.Null(exception);
        }

        #endregion

        #region Menu Update Tests

        [Fact]
        public void OnTick_WhenMenuOpen_ShouldUpdateMenu()
        {
            // Arrange
            var controller = SetupController();
            controller.OnTick(); // Initialize
            controller.OnKeyDown(F7KeyCode); // Open menu

            // Act & Assert - should not throw
            var exception = Record.Exception(() => controller.OnTick());
            Assert.Null(exception);
        }

        #endregion
    }
}
