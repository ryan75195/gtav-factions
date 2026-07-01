using FactionWars.Core.Interfaces;
using FactionWars.Economy.Interfaces;
using FactionWars.Economy.Services;
using FactionWars.Factions.Interfaces;
using FactionWars.Factions.Models;
using Moq;
using Xunit;

namespace FactionWars.Tests.Unit.Economy
{
    public class SupportPackageServiceTests
    {
        private readonly Mock<IGameBridge> _bridge = new Mock<IGameBridge>();
        private readonly Mock<IFactionService> _factions = new Mock<IFactionService>();
        private readonly FactionState _state = new FactionState("michael", 100000);
        private readonly ISupportPackageService _service;

        public SupportPackageServiceTests()
        {
            _factions.Setup(f => f.GetFactionState("michael")).Returns(_state);
            _service = new SupportPackageService(_bridge.Object, _factions.Object);
        }

        [Fact] public void Cost_Is25000() => Assert.Equal(25000, _service.GetSupportSquadCost());

        [Fact]
        public void CanAfford_TrueWhenEnoughMoney()
        {
            _bridge.Setup(b => b.GetPlayerMoney()).Returns(25000);
            Assert.True(_service.CanAfford());
            _bridge.Setup(b => b.GetPlayerMoney()).Returns(24999);
            Assert.False(_service.CanAfford());
        }

        [Fact]
        public void Purchase_WhenAffordable_DeductsAndIncrementsOwned()
        {
            _bridge.Setup(b => b.GetPlayerMoney()).Returns(100000);
            Assert.True(_service.PurchaseSupportSquad("michael"));
            _bridge.Verify(b => b.AddPlayerMoney(-25000), Times.Once);
            Assert.Equal(1, _service.GetOwnedCount("michael"));
        }

        [Fact]
        public void Purchase_WhenBroke_NoOpAndReturnsFalse()
        {
            _bridge.Setup(b => b.GetPlayerMoney()).Returns(1000);
            Assert.False(_service.PurchaseSupportSquad("michael"));
            _bridge.Verify(b => b.AddPlayerMoney(It.IsAny<int>()), Times.Never);
            Assert.Equal(0, _service.GetOwnedCount("michael"));
        }

        [Fact]
        public void TryConsume_DecrementsWhenOwned_FailsAtZero()
        {
            _bridge.Setup(b => b.GetPlayerMoney()).Returns(100000);
            _service.PurchaseSupportSquad("michael"); // owned = 1
            Assert.True(_service.TryConsume("michael"));
            Assert.Equal(0, _service.GetOwnedCount("michael"));
            Assert.False(_service.TryConsume("michael"));
        }

        [Fact]
        public void GetOwnedCount_NullFactionState_ReturnsZero()
        {
            Assert.Equal(0, _service.GetOwnedCount("unknown"));
        }
    }
}
