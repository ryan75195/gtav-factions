using FactionWars.Core.Utils;
using Xunit;

namespace FactionWars.Tests.Unit.Core.Utils
{
    public class MockGameBridgeLineOfSightTests
    {
        [Fact]
        public void HasClearLineOfSight_DefaultsFalse()
        {
            var bridge = new MockGameBridge();
            Assert.False(bridge.HasClearLineOfSight(1, 2));
        }

        [Fact]
        public void HasClearLineOfSight_ReturnsWhatWasSet()
        {
            var bridge = new MockGameBridge();
            bridge.SetLineOfSight(1, 2, true);
            Assert.True(bridge.HasClearLineOfSight(1, 2));
            Assert.False(bridge.HasClearLineOfSight(2, 1)); // direction-specific unless also set
        }
    }
}
