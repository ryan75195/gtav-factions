using FactionWars.Core.Utils;
using Xunit;

namespace FactionWars.Tests.Unit.Core
{
    public class MockGameBridgeCanLeaveVehicleTests
    {
        [Fact]
        public void GetPedCanLeaveVehicleForTest_DefaultsToTrue()
        {
            var bridge = new MockGameBridge();
            Assert.True(bridge.GetPedCanLeaveVehicleForTest(42));
        }

        [Fact]
        public void SetPedCanLeaveVehicle_TracksValue()
        {
            var bridge = new MockGameBridge();
            bridge.SetPedCanLeaveVehicle(42, false);
            Assert.False(bridge.GetPedCanLeaveVehicleForTest(42));

            bridge.SetPedCanLeaveVehicle(42, true);
            Assert.True(bridge.GetPedCanLeaveVehicleForTest(42));
        }

        [Fact]
        public void Reset_RestoresCanLeaveVehicleDefault()
        {
            var bridge = new MockGameBridge();
            bridge.SetPedCanLeaveVehicle(42, false);
            bridge.Reset();
            Assert.True(bridge.GetPedCanLeaveVehicleForTest(42));
        }
    }
}
