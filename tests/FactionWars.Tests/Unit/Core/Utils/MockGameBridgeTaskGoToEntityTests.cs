using FactionWars.Core.Interfaces;
using FactionWars.Core.Utils;
using Xunit;

namespace FactionWars.Tests.Unit.Core.Utils
{
    public class MockGameBridgeTaskGoToEntityTests
    {
        [Fact]
        public void TaskGoToEntity_RecordsCallForExistingPed()
        {
            var bridge = new MockGameBridge();
            int pedHandle = bridge.CreatePed("a_m_y_business_01", new Vector3(0, 0, 0));

            bridge.TaskGoToEntity(pedHandle, targetEntityHandle: 100, stoppingRange: 8.0f);

            Assert.True(bridge.IsPedGoingToEntity(pedHandle));
            Assert.Equal(100, bridge.GetGoToEntityTarget(pedHandle));
            Assert.Equal(8.0f, bridge.GetGoToEntityStoppingRange(pedHandle));
        }

        [Fact]
        public void TaskGoToEntity_OverwritesEarlierTask()
        {
            var bridge = new MockGameBridge();
            int pedHandle = bridge.CreatePed("a_m_y_business_01", new Vector3(0, 0, 0));

            bridge.TaskGoToEntity(pedHandle, targetEntityHandle: 100, stoppingRange: 8.0f);
            bridge.TaskGoToEntity(pedHandle, targetEntityHandle: 200, stoppingRange: 5.0f);

            Assert.Equal(200, bridge.GetGoToEntityTarget(pedHandle));
            Assert.Equal(5.0f, bridge.GetGoToEntityStoppingRange(pedHandle));
        }

        [Fact]
        public void TaskGoToEntity_IsClearedByClearPedTasks()
        {
            var bridge = new MockGameBridge();
            int pedHandle = bridge.CreatePed("a_m_y_business_01", new Vector3(0, 0, 0));

            bridge.TaskGoToEntity(pedHandle, targetEntityHandle: 100, stoppingRange: 8.0f);
            bridge.ClearPedTasks(pedHandle);

            Assert.False(bridge.IsPedGoingToEntity(pedHandle));
        }
    }
}
