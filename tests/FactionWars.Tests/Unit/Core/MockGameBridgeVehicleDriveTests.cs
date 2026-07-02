using FactionWars.Core.Interfaces;
using FactionWars.Core.Utils;
using Xunit;

namespace FactionWars.Tests.Unit.Core
{
    public class MockGameBridgeVehicleDriveTests
    {
        [Fact]
        public void TaskVehicleDriveToCoord_TracksDestination()
        {
            var bridge = new MockGameBridge();
            var dest = new Vector3(100f, 200f, 30f);
            bridge.TaskVehicleDriveToCoord(900, dest, 20f, 8f);
            Assert.Equal(dest, bridge.GetVehicleDriveTargetForTest(900));
        }

        [Fact]
        public void GetVehicleDriveTargetForTest_DefaultsToNull()
        {
            Assert.Null(new MockGameBridge().GetVehicleDriveTargetForTest(900));
        }

        [Fact]
        public void Reset_ClearsVehicleDriveTargets()
        {
            var bridge = new MockGameBridge();
            bridge.TaskVehicleDriveToCoord(900, new Vector3(1f, 2f, 3f), 20f, 8f);
            bridge.Reset();
            Assert.Null(bridge.GetVehicleDriveTargetForTest(900));
        }
    }
}
