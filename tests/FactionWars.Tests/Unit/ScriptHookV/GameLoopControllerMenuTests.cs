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

        #region Controller Hold-To-Repeat Tests

        private const int ControlFrontendAccept = 201; // A/Cross button

        [Fact]
        public void OnTick_AfterControllerAcceptReleased_ClearsSelectKeyHeld()
        {
            // Arrange
            var gameBridge = new MockGameBridge { PlayerCharacterModel = "player_zero" };
            var menuProvider = new MockMenuProvider();
            var container = ServiceContainerFactory.Create(gameBridge, menuProvider);
            var controller = new GameLoopController(container);
            controller.OnTick(); // Initialize

            // Act - press the controller A button
            gameBridge.SetControlPressed(ControlFrontendAccept, true);
            controller.OnTick();
            Assert.True(menuProvider.SelectKeyHeld, "Holding A should mark the select key as held");

            // Act - release the controller A button
            gameBridge.SetControlPressed(ControlFrontendAccept, false);
            controller.OnTick();

            // Assert - releasing A must clear the held state (otherwise hold-to-repeat spams)
            Assert.False(menuProvider.SelectKeyHeld, "Releasing A must clear the select key held state");
        }

        [Fact]
        public void OnTick_AfterKeyboardEnterReleased_ClearsSelectKeyHeld()
        {
            // Arrange
            const int EnterKeyCode = 0x0D;
            var gameBridge = new MockGameBridge { PlayerCharacterModel = "player_zero" };
            var menuProvider = new MockMenuProvider();
            var container = ServiceContainerFactory.Create(gameBridge, menuProvider);
            var controller = new GameLoopController(container);
            controller.OnTick(); // Initialize

            // Act - hold the keyboard Enter key
            controller.OnKeyDown(EnterKeyCode);
            controller.OnTick();
            Assert.True(menuProvider.SelectKeyHeld, "Holding Enter should mark the select key as held");

            // Act - release the keyboard Enter key
            controller.OnKeyUp(EnterKeyCode);
            controller.OnTick();

            // Assert - keyboard hold-to-repeat still clears correctly after consolidation
            Assert.False(menuProvider.SelectKeyHeld, "Releasing Enter must clear the select key held state");
        }

        [Fact]
        public void OnTick_HeldMenuNavigation_SuppressedWithinCooldownThenAllowedAfter()
        {
            // Arrange
            const int FrontendDown = 187;
            const int FrontendUp = 188;
            var gameBridge = new MockGameBridge { PlayerCharacterModel = "player_zero" };
            var menuProvider = new MockMenuProvider();
            var container = ServiceContainerFactory.Create(gameBridge, menuProvider);
            var controller = new GameLoopController(container);
            controller.OnTick();             // Initialize
            controller.OnKeyDown(F7KeyCode); // Open menu
            Assert.True(menuProvider.IsMenuVisible);

            // Frame 1: fresh press of Down -> allowed (NativeUI sees the input)
            gameBridge.GameTime = 1000;
            gameBridge.SetControlPressed(FrontendDown, true);
            gameBridge.ClearDisabledControls();
            controller.OnTick();
            Assert.DoesNotContain(FrontendDown, gameBridge.DisabledControls);

            // Frame 2: still held only 40ms later -> within cooldown -> suppressed
            gameBridge.GameTime = 1040;
            gameBridge.ClearDisabledControls();
            controller.OnTick();
            Assert.Contains(FrontendDown, gameBridge.DisabledControls);
            Assert.Contains(FrontendUp, gameBridge.DisabledControls);

            // Frame 3: still held, well past cooldown -> allowed again
            gameBridge.GameTime = 1500;
            gameBridge.ClearDisabledControls();
            controller.OnTick();
            Assert.DoesNotContain(FrontendDown, gameBridge.DisabledControls);
        }

        [Fact]
        public void OnTick_NoMenuVisible_DoesNotSuppressNavigation()
        {
            // Arrange
            const int FrontendDown = 187;
            var gameBridge = new MockGameBridge { PlayerCharacterModel = "player_zero" };
            var menuProvider = new MockMenuProvider();
            var container = ServiceContainerFactory.Create(gameBridge, menuProvider);
            var controller = new GameLoopController(container);
            controller.OnTick(); // Initialize, no menu open

            // Act - hold Down across frames within what would be the cooldown window
            gameBridge.GameTime = 1000;
            gameBridge.SetControlPressed(FrontendDown, true);
            controller.OnTick();
            gameBridge.GameTime = 1040;
            gameBridge.ClearDisabledControls();
            controller.OnTick();

            // Assert - with no menu open, navigation throttle must not touch controls
            Assert.Empty(gameBridge.DisabledControls);
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
