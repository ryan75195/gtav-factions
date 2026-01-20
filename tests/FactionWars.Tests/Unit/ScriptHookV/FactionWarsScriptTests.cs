using System;
using FactionWars.Core.Interfaces;
using FactionWars.ScriptHookV;
using Moq;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV
{
    /// <summary>
    /// Tests for FactionWarsScript and its associated game loop controller.
    /// Since GTA.Script cannot be directly tested, we test the GameLoopController
    /// which contains all the testable logic that FactionWarsScript delegates to.
    /// </summary>
    public class FactionWarsScriptTests
    {
        [Fact]
        public void GameLoopController_Constructor_WithValidContainer_ShouldNotThrow()
        {
            // Arrange
            var mockGameBridge = new Mock<IGameBridge>();
            var container = ServiceContainerFactory.Create(mockGameBridge.Object);

            // Act & Assert - should not throw
            var controller = new GameLoopController(container);
            Assert.NotNull(controller);
        }

        [Fact]
        public void GameLoopController_Constructor_WithNullContainer_ShouldThrowArgumentNullException()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentNullException>(() => new GameLoopController(null!));
        }

        [Fact]
        public void GameLoopController_OnTick_ShouldNotThrowOnFirstCall()
        {
            // Arrange
            var mockGameBridge = new Mock<IGameBridge>();
            var container = ServiceContainerFactory.Create(mockGameBridge.Object);
            var controller = new GameLoopController(container);

            // Act & Assert - should not throw
            controller.OnTick();
        }

        [Fact]
        public void GameLoopController_OnKeyDown_ShouldNotThrowWithValidKey()
        {
            // Arrange
            var mockGameBridge = new Mock<IGameBridge>();
            var container = ServiceContainerFactory.Create(mockGameBridge.Object);
            var controller = new GameLoopController(container);

            // Act & Assert - should not throw (F7 = 0x76 = 118)
            controller.OnKeyDown(118);
        }

        [Fact]
        public void GameLoopController_IsInitialized_ShouldBeTrueAfterConstruction()
        {
            // Arrange
            var mockGameBridge = new Mock<IGameBridge>();
            var container = ServiceContainerFactory.Create(mockGameBridge.Object);

            // Act
            var controller = new GameLoopController(container);

            // Assert
            Assert.True(controller.IsInitialized);
        }

        [Fact]
        public void GameLoopController_ServiceContainer_ShouldBeAccessible()
        {
            // Arrange
            var mockGameBridge = new Mock<IGameBridge>();
            var container = ServiceContainerFactory.Create(mockGameBridge.Object);

            // Act
            var controller = new GameLoopController(container);

            // Assert
            Assert.Same(container, controller.ServiceContainer);
        }

        [Fact]
        public void GameLoopController_OnAbort_ShouldSetIsInitializedToFalse()
        {
            // Arrange
            var mockGameBridge = new Mock<IGameBridge>();
            var container = ServiceContainerFactory.Create(mockGameBridge.Object);
            var controller = new GameLoopController(container);

            // Act
            controller.OnAbort();

            // Assert
            Assert.False(controller.IsInitialized);
        }
    }
}
