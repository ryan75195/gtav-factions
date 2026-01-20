using FactionWars.Core.Interfaces;
using FactionWars.ScriptHookV;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV
{
    public class ScriptHookVGameBridgeTests
    {
        [Fact]
        public void GameBridge_ShouldImplementIGameBridge()
        {
            // This test verifies the GameBridge class exists and implements IGameBridge.
            // Actual GTA V native calls cannot be tested without the game running.
            // Arrange & Act & Assert
            Assert.True(typeof(IGameBridge).IsAssignableFrom(typeof(GameBridge)));
        }

        [Fact]
        public void GameBridge_ShouldHaveParameterlessConstructor()
        {
            // Verify GameBridge can be instantiated without dependencies
            // Note: This will only work in the actual game environment
            // For unit tests, we verify the type structure only
            var constructor = typeof(GameBridge).GetConstructor(System.Type.EmptyTypes);
            Assert.NotNull(constructor);
        }

        [Fact]
        public void GameBridge_ShouldHaveGetPlayerCharacterModelMethod()
        {
            // Verify the interface method exists
            var method = typeof(IGameBridge).GetMethod(nameof(IGameBridge.GetPlayerCharacterModel));
            Assert.NotNull(method);
            Assert.Equal(typeof(string), method.ReturnType);
        }
    }
}
