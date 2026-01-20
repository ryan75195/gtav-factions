using FactionWars.Core.Utils;
using Xunit;

namespace FactionWars.Tests.Unit.Core
{
    public class MockGameBridgeBlipTests
    {
        [Fact]
        public void CreateBlipForPed_ReturnsValidBlipHandle()
        {
            var gameBridge = new MockGameBridge();

            var blipHandle = gameBridge.CreateBlipForPed(123);

            Assert.True(blipHandle > 0);
        }

        [Fact]
        public void CreateBlipForPed_ReturnsUniqueHandles()
        {
            var gameBridge = new MockGameBridge();

            var blip1 = gameBridge.CreateBlipForPed(100);
            var blip2 = gameBridge.CreateBlipForPed(200);

            Assert.NotEqual(blip1, blip2);
        }
    }
}
