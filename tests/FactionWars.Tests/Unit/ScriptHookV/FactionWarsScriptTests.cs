using System;
using System.IO;
using FactionWars.Core.Interfaces;
using FactionWars.ScriptHookV;
using FactionWars.Tests.Mocks;
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
        private static Mock<IGameBridge> CreateMockGameBridge()
        {
            var mock = new Mock<IGameBridge>();
            mock.Setup(x => x.GetScriptsDirectory()).Returns(Path.GetTempPath());
            return mock;
        }

        [Fact]
        public void GameLoopController_Constructor_WithValidContainer_ShouldNotThrow()
        {
            // Arrange
            var mockGameBridge = CreateMockGameBridge();
            var container = ServiceContainerFactory.Create(mockGameBridge.Object, new MockMenuProvider());

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
            var mockGameBridge = CreateMockGameBridge();
            var container = ServiceContainerFactory.Create(mockGameBridge.Object, new MockMenuProvider());
            var controller = new GameLoopController(container);

            // Act & Assert - should not throw
            controller.OnTick();
        }

        [Fact]
        public void GameLoopController_OnKeyDown_ShouldNotThrowWithValidKey()
        {
            // Arrange
            var mockGameBridge = CreateMockGameBridge();
            var container = ServiceContainerFactory.Create(mockGameBridge.Object, new MockMenuProvider());
            var controller = new GameLoopController(container);

            // Act & Assert - should not throw (F7 = 0x76 = 118)
            controller.OnKeyDown(118);
        }

        [Fact]
        public void GameLoopController_IsInitialized_ShouldBeTrueAfterConstruction()
        {
            // Arrange
            var mockGameBridge = CreateMockGameBridge();
            var container = ServiceContainerFactory.Create(mockGameBridge.Object, new MockMenuProvider());

            // Act
            var controller = new GameLoopController(container);

            // Assert
            Assert.True(controller.IsInitialized);
        }

        [Fact]
        public void GameLoopController_ServiceContainer_ShouldBeAccessible()
        {
            // Arrange
            var mockGameBridge = CreateMockGameBridge();
            var container = ServiceContainerFactory.Create(mockGameBridge.Object, new MockMenuProvider());

            // Act
            var controller = new GameLoopController(container);

            // Assert
            Assert.Same(container, controller.ServiceContainer);
        }

        [Fact]
        public void GameLoopController_OnAbort_ShouldSetIsInitializedToFalse()
        {
            // Arrange
            var mockGameBridge = CreateMockGameBridge();
            var container = ServiceContainerFactory.Create(mockGameBridge.Object, new MockMenuProvider());
            var controller = new GameLoopController(container);

            // Act
            controller.OnAbort();

            // Assert
            Assert.False(controller.IsInitialized);
        }
    }
}
