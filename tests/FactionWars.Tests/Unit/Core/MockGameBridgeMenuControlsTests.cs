using FactionWars.Core.Utils;
using Xunit;

namespace FactionWars.Tests.Unit.Core
{
    public class MockGameBridgeMenuControlsTests
    {
        [Fact]
        public void DisableMenuConflictControlsThisFrame_IncrementsCallCount()
        {
            var bridge = new MockGameBridge();
            Assert.Equal(0, bridge.MenuConflictSuppressCallCount);

            bridge.DisableMenuConflictControlsThisFrame();
            bridge.DisableMenuConflictControlsThisFrame();

            Assert.Equal(2, bridge.MenuConflictSuppressCallCount);
        }

        [Fact]
        public void Reset_ClearsMenuConflictSuppressCallCount()
        {
            var bridge = new MockGameBridge();
            bridge.DisableMenuConflictControlsThisFrame();

            bridge.Reset();

            Assert.Equal(0, bridge.MenuConflictSuppressCallCount);
        }
    }
}
