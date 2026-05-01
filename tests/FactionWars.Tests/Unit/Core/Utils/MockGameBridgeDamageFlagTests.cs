using FactionWars.Core.Utils;
using Xunit;

namespace FactionWars.Tests.Unit.Core.Utils
{
    public class MockGameBridgeDamageFlagTests
    {
        [Fact]
        public void ConsumePlayerDamagedByPedFlag_DefaultsToFalse()
        {
            var bridge = new MockGameBridge();
            Assert.False(bridge.ConsumePlayerDamagedByPedFlag());
        }

        [Fact]
        public void ConsumePlayerDamagedByPedFlag_WhenSetTrue_ReturnsTrueOnce_ThenFalse()
        {
            var bridge = new MockGameBridge();
            bridge.PlayerDamagedByPed = true;

            Assert.True(bridge.ConsumePlayerDamagedByPedFlag());
            Assert.False(bridge.ConsumePlayerDamagedByPedFlag());
        }

        [Fact]
        public void ConsumePlayerDamagedByPedFlag_AfterConsume_PlayerDamagedByPedIsFalse()
        {
            var bridge = new MockGameBridge();
            bridge.PlayerDamagedByPed = true;

            bridge.ConsumePlayerDamagedByPedFlag();

            Assert.False(bridge.PlayerDamagedByPed);
        }
    }
}
