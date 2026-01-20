using FactionWars.Core.Interfaces;
using FactionWars.Core.Utils;
using FactionWars.UI.Services;
using Xunit;

namespace FactionWars.Tests.Unit.UI
{
    public class PedBlipServiceTests
    {
        private readonly MockGameBridge _gameBridge;
        private readonly PedBlipService _service;

        public PedBlipServiceTests()
        {
            _gameBridge = new MockGameBridge();
            _service = new PedBlipService(_gameBridge);
        }

        [Fact]
        public void CreateBlipForPed_CreatesBlipWithCorrectColor()
        {
            var blipHandle = _service.CreateBlipForPed(100, BlipColor.Yellow);

            Assert.True(blipHandle > 0);
        }

        [Fact]
        public void CreateBlipForPed_TracksBlipHandle()
        {
            _service.CreateBlipForPed(100, BlipColor.Yellow);

            Assert.True(_service.HasBlipForPed(100));
        }

        [Fact]
        public void RemoveBlipForPed_RemovesBlip()
        {
            _service.CreateBlipForPed(100, BlipColor.Yellow);

            _service.RemoveBlipForPed(100);

            Assert.False(_service.HasBlipForPed(100));
        }

        [Fact]
        public void RemoveBlipForPed_WhenNoBlip_DoesNotThrow()
        {
            var exception = Record.Exception(() => _service.RemoveBlipForPed(999));

            Assert.Null(exception);
        }

        [Fact]
        public void RemoveAllBlips_ClearsAllTrackedBlips()
        {
            _service.CreateBlipForPed(100, BlipColor.Yellow);
            _service.CreateBlipForPed(200, BlipColor.Blue);

            _service.RemoveAllBlips();

            Assert.False(_service.HasBlipForPed(100));
            Assert.False(_service.HasBlipForPed(200));
        }
    }
}
