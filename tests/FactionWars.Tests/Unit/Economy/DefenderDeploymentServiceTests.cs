using System;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.Economy.Interfaces;
using FactionWars.Economy.Models;
using FactionWars.Economy.Services;
using FactionWars.Factions.Models;
using Moq;
using Xunit;

namespace FactionWars.Tests.Unit.Economy
{
    public class DefenderDeploymentServiceTests
    {
        private readonly Mock<ITroopPurchaseService> _purchase = new Mock<ITroopPurchaseService>();
        private readonly Mock<IZoneDefenderAllocationService> _alloc = new Mock<IZoneDefenderAllocationService>();
        private readonly FactionState _faction = new FactionState("michael", 10000);
        private readonly IDefenderDeploymentService _service;

        public DefenderDeploymentServiceTests()
        {
            _service = new DefenderDeploymentService(_purchase.Object, _alloc.Object);
        }

        [Fact]
        public void BuyAndDeploy_WhenAffordable_PurchasesThenAllocatesAndReturnsSuccess()
        {
            _purchase.Setup(p => p.CanAfford(DefenderRole.Rifleman, 1)).Returns(true);
            _purchase.Setup(p => p.CalculateTotalCost(DefenderRole.Rifleman, 1)).Returns(1000);

            var result = _service.BuyAndDeploy(_faction, "zone_downtown", DefenderRole.Rifleman, 1);

            Assert.True(result.Success);
            Assert.Equal(1000, result.TotalCost);
            _purchase.Verify(p => p.PurchaseTroops("michael", DefenderRole.Rifleman, 1), Times.Once);
            _alloc.Verify(a => a.AllocateTroops(_faction, "zone_downtown", DefenderRole.Rifleman, 1), Times.Once);
        }

        [Fact]
        public void BuyAndDeploy_WhenUnaffordable_DoesNothingAndReturnsInsufficientFunds()
        {
            _purchase.Setup(p => p.CanAfford(DefenderRole.Rocketeer, 1)).Returns(false);
            _purchase.Setup(p => p.CalculateTotalCost(DefenderRole.Rocketeer, 1)).Returns(2000);

            var result = _service.BuyAndDeploy(_faction, "zone_downtown", DefenderRole.Rocketeer, 1);

            Assert.False(result.Success);
            Assert.Equal(DeploymentStatus.InsufficientFunds, result.Status);
            _purchase.Verify(p => p.PurchaseTroops(It.IsAny<string>(), It.IsAny<DefenderRole>(), It.IsAny<int>()), Times.Never);
            _alloc.Verify(a => a.AllocateTroops(It.IsAny<FactionState>(), It.IsAny<string>(), It.IsAny<DefenderRole>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public void GetTroopCost_ForwardsToPurchaseService()
        {
            _purchase.Setup(p => p.GetTroopCost(DefenderRole.Sniper)).Returns(1500);
            Assert.Equal(1500, _service.GetTroopCost(DefenderRole.Sniper));
        }

        [Fact]
        public void CanAfford_ForwardsToPurchaseService()
        {
            _purchase.Setup(p => p.CanAfford(DefenderRole.Rifleman, 5)).Returns(true);
            Assert.True(_service.CanAfford(DefenderRole.Rifleman, 5));
        }

        [Fact]
        public void BuyAndDeploy_WithNullFactionState_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                _service.BuyAndDeploy(null!, "zone_downtown", DefenderRole.Grunt, 1));
        }

        [Fact]
        public void BuyAndDeploy_WithEmptyZoneId_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                _service.BuyAndDeploy(_faction, "   ", DefenderRole.Grunt, 1));
        }

        [Fact]
        public void BuyAndDeploy_WithNonPositiveCount_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                _service.BuyAndDeploy(_faction, "zone_downtown", DefenderRole.Grunt, 0));
        }

        [Fact]
        public void BuyAndDeploy_WithCountGreaterThanOne_PassesCountToBothServicesAndReturnsTotalCost()
        {
            _purchase.Setup(p => p.CanAfford(DefenderRole.Gunner, 3)).Returns(true);
            _purchase.Setup(p => p.CalculateTotalCost(DefenderRole.Gunner, 3)).Returns(1500);

            var result = _service.BuyAndDeploy(_faction, "zone_downtown", DefenderRole.Gunner, 3);

            Assert.True(result.Success);
            Assert.Equal(1500, result.TotalCost);
            _purchase.Verify(p => p.PurchaseTroops("michael", DefenderRole.Gunner, 3), Times.Once);
            _alloc.Verify(a => a.AllocateTroops(_faction, "zone_downtown", DefenderRole.Gunner, 3), Times.Once);
        }
    }
}
