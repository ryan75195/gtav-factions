using FactionWars.Core.Interfaces;
using FactionWars.Core.Utils;
using Xunit;

namespace FactionWars.Tests.Unit.Core.Utils
{
    public class MockGameBridgeBoundedWanderTests
    {
        [Fact]
        public void TaskPedWanderInBoundedArea_RecordsCallDistinctFromUnbounded()
        {
            var bridge = new MockGameBridge();
            int handle = bridge.CreatePed("test_model", new Vector3(0f, 0f, 0f));

            bridge.TaskPedWanderInBoundedArea(handle, new Vector3(10f, 20f, 0f), 50f);

            Assert.True(bridge.IsPedBoundedWandering(handle));
            Assert.False(bridge.IsPedWandering(handle), "Bounded wander should NOT be reported as plain wander");
            Assert.Equal(new Vector3(10f, 20f, 0f), bridge.GetBoundedWanderCenter(handle));
            Assert.Equal(50f, bridge.GetBoundedWanderRadius(handle));
        }

        [Fact]
        public void TaskPedWanderInBoundedArea_OverwritesPreviousTasks()
        {
            var bridge = new MockGameBridge();
            int handle = bridge.CreatePed("test_model", new Vector3(0f, 0f, 0f));

            bridge.TaskPedWanderInArea(handle, new Vector3(0f, 0f, 0f), 100f);
            bridge.TaskPedWanderInBoundedArea(handle, new Vector3(10f, 20f, 0f), 50f);

            Assert.False(bridge.IsPedWandering(handle));
            Assert.True(bridge.IsPedBoundedWandering(handle));
        }
    }
}
