using FactionWars.Core.Interfaces;
using FactionWars.Core.Utils;
using Xunit;

namespace FactionWars.Tests.Unit.Core.Utils
{
    public class MockGameBridgeGoToCoordTests
    {
        [Fact]
        public void TaskGoToCoord_RecordsDestination()
        {
            var bridge = new MockGameBridge();
            int handle = bridge.CreatePed("test_model", new Vector3(0f, 0f, 0f));

            bridge.TaskGoToCoord(handle, new Vector3(50f, 75f, 0f));

            Assert.True(bridge.IsPedGoingToCoord(handle));
            Assert.Equal(new Vector3(50f, 75f, 0f), bridge.GetPedGoToCoordDestination(handle));
        }

        [Fact]
        public void TaskGoToCoord_OverwritesPreviousDestination()
        {
            var bridge = new MockGameBridge();
            int handle = bridge.CreatePed("test_model", new Vector3(0f, 0f, 0f));

            bridge.TaskGoToCoord(handle, new Vector3(50f, 75f, 0f));
            bridge.TaskGoToCoord(handle, new Vector3(100f, 0f, 0f));

            Assert.Equal(new Vector3(100f, 0f, 0f), bridge.GetPedGoToCoordDestination(handle));
        }

        [Fact]
        public void ClearPedTasks_ClearsGoToCoordRecording()
        {
            var bridge = new MockGameBridge();
            int handle = bridge.CreatePed("test_model", new Vector3(0f, 0f, 0f));

            bridge.TaskGoToCoord(handle, new Vector3(50f, 75f, 0f));
            bridge.ClearPedTasks(handle);

            Assert.False(bridge.IsPedGoingToCoord(handle));
        }
    }
}
