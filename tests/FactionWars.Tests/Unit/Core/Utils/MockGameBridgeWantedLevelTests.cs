using FactionWars.Core.Utils;
using Xunit;

namespace FactionWars.Tests.Unit.Core.Utils
{
    public class MockGameBridgeWantedLevelTests
    {
        [Fact]
        public void WantedLevel_DefaultsToZero()
        {
            var bridge = new MockGameBridge();
            Assert.Equal(0, bridge.GetWantedLevel());
        }

        [Fact]
        public void WantedLevel_IsSettableAndReturnedFromGetter()
        {
            var bridge = new MockGameBridge();
            bridge.WantedLevel = 3;
            Assert.Equal(3, bridge.GetWantedLevel());
        }
    }
}
