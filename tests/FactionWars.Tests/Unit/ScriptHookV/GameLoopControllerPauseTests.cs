using System.Threading;
using FactionWars.Core.Utils;
using FactionWars.ScriptHookV;
using FactionWars.Tests.Mocks;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV
{
    public class GameLoopControllerPauseTests
    {
        [Fact]
        public void OnTick_WhenGamePaused_DoesNotAdvanceGameStatePlayTime()
        {
            var bridge = new MockGameBridge
            {
                PlayerCharacterModel = "player_zero"
            };
            var container = ServiceContainerFactory.Create(bridge, new MockMenuProvider());
            var controller = new GameLoopController(container);

            controller.OnTick();
            var gameStateManager = controller.ServiceContainer
                .Resolve<FactionWars.ScriptHookV.Persistence.IGameStateManager>();
            var playTimeBeforePause = gameStateManager.TotalPlayTimeSeconds;

            bridge.IsGamePausedValue = true;
            Thread.Sleep(1100);
            controller.OnTick();

            Assert.Equal(playTimeBeforePause, gameStateManager.TotalPlayTimeSeconds);
        }

        [Fact]
        public void MockGameBridge_IsGamePaused_DefaultsToFalse()
        {
            var bridge = new MockGameBridge();

            Assert.False(bridge.IsGamePaused());
        }
    }
}
