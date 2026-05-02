using FactionWars.Core.Interfaces;
using FactionWars.Core.Utils;
using Xunit;

namespace FactionWars.Tests.Unit.Core.Utils
{
    public class MockGameBridgeDoesPedExistTests
    {
        [Fact]
        public void DoesPedExist_TrueForLivingPed()
        {
            var bridge = new MockGameBridge();
            int handle = bridge.CreatePed("a_m_y_business_01", new Vector3(0, 0, 0));

            Assert.True(bridge.DoesPedExist(handle));
        }

        [Fact]
        public void DoesPedExist_TrueForDeadButStillStreamedPed()
        {
            // GTA V keeps a dead ped's entity around for a while before cleanup.
            // DoesPedExist must report true; only IsPedAlive reports false.
            var bridge = new MockGameBridge();
            int handle = bridge.CreatePed("a_m_y_business_01", new Vector3(0, 0, 0));
            bridge.KillPed(handle);

            Assert.True(bridge.DoesPedExist(handle));
            Assert.False(bridge.IsPedAlive(handle));
        }

        [Fact]
        public void DoesPedExist_FalseAfterStreamedOut()
        {
            // Simulates GTA's population manager culling a ped that drifted out of
            // streaming distance — the entity is gone, neither dead nor alive.
            var bridge = new MockGameBridge();
            int handle = bridge.CreatePed("a_m_y_business_01", new Vector3(0, 0, 0));
            bridge.DeletePed(handle);

            Assert.False(bridge.DoesPedExist(handle));
        }

        [Fact]
        public void DoesPedExist_FalseForUnknownHandle()
        {
            var bridge = new MockGameBridge();

            Assert.False(bridge.DoesPedExist(99999));
        }
    }
}
