using FactionWars.Core.Interfaces;
using FactionWars.Core.Utils;
using Xunit;

namespace FactionWars.Tests.Unit.Core.Utils
{
    public class MockGameBridgeTaskFollowToOffsetFromEntityTests
    {
        [Fact]
        public void TaskFollowToOffsetFromEntity_RecordsCallForExistingPed()
        {
            var bridge = new MockGameBridge();
            int pedHandle = bridge.CreatePed("a_m_y_business_01", new Vector3(0, 0, 0));

            bridge.TaskFollowToOffsetFromEntity(
                pedHandle,
                targetEntityHandle: 99,
                offset: new Vector3(0, 0, 0),
                moveBlendRatio: 3.0f,
                stoppingRadius: 4.0f,
                persistFollowing: true);

            Assert.True(bridge.IsPedFollowingEntity(pedHandle));
            Assert.Equal(99, bridge.GetFollowEntityTarget(pedHandle));
            Assert.Equal(4.0f, bridge.GetFollowEntityStoppingRadius(pedHandle));
        }

        [Fact]
        public void TaskFollowToOffsetFromEntity_OverwritesEarlierTask()
        {
            var bridge = new MockGameBridge();
            int pedHandle = bridge.CreatePed("a_m_y_business_01", new Vector3(0, 0, 0));

            bridge.TaskFollowToOffsetFromEntity(pedHandle, 100, new Vector3(0, 0, 0), 3.0f, 4.0f, true);
            bridge.TaskFollowToOffsetFromEntity(pedHandle, 200, new Vector3(0, 0, 0), 3.0f, 5.0f, true);

            Assert.Equal(200, bridge.GetFollowEntityTarget(pedHandle));
            Assert.Equal(5.0f, bridge.GetFollowEntityStoppingRadius(pedHandle));
        }

        [Fact]
        public void TaskFollowToOffsetFromEntity_IsClearedByClearPedTasks()
        {
            var bridge = new MockGameBridge();
            int pedHandle = bridge.CreatePed("a_m_y_business_01", new Vector3(0, 0, 0));

            bridge.TaskFollowToOffsetFromEntity(pedHandle, 100, new Vector3(0, 0, 0), 3.0f, 4.0f, true);
            bridge.ClearPedTasks(pedHandle);

            Assert.False(bridge.IsPedFollowingEntity(pedHandle));
        }

        [Fact]
        public void TaskFollowToOffsetFromEntity_ReplacesGoToEntityState()
        {
            // Mirrors GTA V: every TASK_X is a primary-task replacement.
            var bridge = new MockGameBridge();
            int pedHandle = bridge.CreatePed("a_m_y_business_01", new Vector3(0, 0, 0));

            bridge.TaskGoToEntity(pedHandle, 100, 8.0f);
            Assert.True(bridge.IsPedGoingToEntity(pedHandle));

            bridge.TaskFollowToOffsetFromEntity(pedHandle, 100, new Vector3(0, 0, 0), 3.0f, 4.0f, true);

            Assert.False(bridge.IsPedGoingToEntity(pedHandle));
            Assert.True(bridge.IsPedFollowingEntity(pedHandle));
        }
    }
}
