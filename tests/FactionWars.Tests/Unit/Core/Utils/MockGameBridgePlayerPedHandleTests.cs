using FactionWars.Core.Utils;
using Xunit;

namespace FactionWars.Tests.Unit.Core.Utils
{
    public class MockGameBridgePlayerPedHandleTests
    {
        [Fact]
        public void PlayerPedHandle_DefaultsToOne()
        {
            // The mock returns a stable non-zero handle by default so test code
            // doesn't have to set it up just to call TaskGoToEntity.
            var bridge = new MockGameBridge();
            Assert.Equal(1, bridge.GetPlayerPedHandle());
        }

        [Fact]
        public void PlayerPedHandle_IsSettable()
        {
            var bridge = new MockGameBridge();
            bridge.PlayerPedHandle = 42;
            Assert.Equal(42, bridge.GetPlayerPedHandle());
        }
    }
}
