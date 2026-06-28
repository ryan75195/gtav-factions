using System;
using System.Collections.Generic;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.Economy.Interfaces;
using FactionWars.Economy.Services;
using FactionWars.Factions.Interfaces;
using FactionWars.Factions.Models;
using Moq;
using Xunit;

namespace FactionWars.Tests.Unit.Economy
{
    /// <summary>
    /// Tests for the TroopPurchaseService which handles purchasing troops
    /// using player's real GTA V cash and adding them to faction reserve pools.
    /// </summary>
    public class TroopPurchaseServiceTests
    {
        private readonly Mock<IGameBridge> _mockGameBridge;
        private readonly Mock<IDefenderRoleService> _mockDefenderRoleService;
        private readonly Mock<IFactionService> _mockFactionService;
        private readonly ITroopPurchaseService _troopPurchaseService;

        private const string PlayerFactionId = "faction-michael";

        public TroopPurchaseServiceTests()
        {
            _mockGameBridge = new Mock<IGameBridge>();
            _mockDefenderRoleService = new Mock<IDefenderRoleService>();
            _mockFactionService = new Mock<IFactionService>();

            // Default tier costs
            _mockDefenderRoleService.Setup(s => s.GetCost(DefenderRole.Grunt)).Returns(200);
            _mockDefenderRoleService.Setup(s => s.GetCost(DefenderRole.Gunner)).Returns(500);
            _mockDefenderRoleService.Setup(s => s.GetCost(DefenderRole.Rifleman)).Returns(1000);

            _troopPurchaseService = new TroopPurchaseService(
                _mockGameBridge.Object,
                _mockDefenderRoleService.Object,
                _mockFactionService.Object);
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithValidDependencies_CreatesInstance()
        {
            // Arrange & Act
            var service = new TroopPurchaseService(
                _mockGameBridge.Object,
                _mockDefenderRoleService.Object,
                _mockFactionService.Object);

            // Assert
            Assert.NotNull(service);
        }

        [Fact]
        public void Constructor_WithNullGameBridge_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new TroopPurchaseService(
                    null!,
                    _mockDefenderRoleService.Object,
                    _mockFactionService.Object));
            Assert.Equal("gameBridge", exception.ParamName);
        }

        [Fact]
        public void Constructor_WithNullDefenderRoleService_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new TroopPurchaseService(
                    _mockGameBridge.Object,
                    null!,
                    _mockFactionService.Object));
            Assert.Equal("defenderRoleService", exception.ParamName);
        }

        [Fact]
        public void Constructor_WithNullFactionService_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new TroopPurchaseService(
                    _mockGameBridge.Object,
                    _mockDefenderRoleService.Object,
                    null!));
            Assert.Equal("factionService", exception.ParamName);
        }

        #endregion

        #region CanAfford Tests

        [Fact]
        public void CanAfford_WithSufficientMoney_ReturnsTrue()
        {
            // Arrange
            _mockGameBridge.Setup(g => g.GetPlayerMoney()).Returns(1000);

            // Act - 5 basic troops at $200 each = $1000
            bool canAfford = _troopPurchaseService.CanAfford(DefenderRole.Grunt, 5);

            // Assert
            Assert.True(canAfford);
        }

        [Fact]
        public void CanAfford_WithInsufficientMoney_ReturnsFalse()
        {
            // Arrange
            _mockGameBridge.Setup(g => g.GetPlayerMoney()).Returns(500);

            // Act - 5 basic troops at $200 each = $1000 needed
            bool canAfford = _troopPurchaseService.CanAfford(DefenderRole.Grunt, 5);

            // Assert
            Assert.False(canAfford);
        }

        [Fact]
        public void CanAfford_WithExactMoney_ReturnsTrue()
        {
            // Arrange
            _mockGameBridge.Setup(g => g.GetPlayerMoney()).Returns(500);

            // Act - 1 medium troop at $500
            bool canAfford = _troopPurchaseService.CanAfford(DefenderRole.Gunner, 1);

            // Assert
            Assert.True(canAfford);
        }

        [Fact]
        public void CanAfford_WithZeroCount_ReturnsTrue()
        {
            // Arrange
            _mockGameBridge.Setup(g => g.GetPlayerMoney()).Returns(0);

            // Act
            bool canAfford = _troopPurchaseService.CanAfford(DefenderRole.Rifleman, 0);

            // Assert
            Assert.True(canAfford);
        }

        [Fact]
        public void CanAfford_WithNegativeCount_ThrowsArgumentOutOfRangeException()
        {
            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                _troopPurchaseService.CanAfford(DefenderRole.Grunt, -1));
        }

        #endregion

        #region GetTroopCost Tests

        [Fact]
        public void GetTroopCost_ReturnsCorrectCostFromTierService()
        {
            // Act
            int basicCost = _troopPurchaseService.GetTroopCost(DefenderRole.Grunt);
            int mediumCost = _troopPurchaseService.GetTroopCost(DefenderRole.Gunner);
            int heavyCost = _troopPurchaseService.GetTroopCost(DefenderRole.Rifleman);

            // Assert
            Assert.Equal(200, basicCost);
            Assert.Equal(500, mediumCost);
            Assert.Equal(1000, heavyCost);
        }

        #endregion

        #region CalculateTotalCost Tests

        [Fact]
        public void CalculateTotalCost_WithSingleTier_ReturnsCorrectTotal()
        {
            // Act
            int cost = _troopPurchaseService.CalculateTotalCost(DefenderRole.Grunt, 5);

            // Assert - 5 x $200 = $1000
            Assert.Equal(1000, cost);
        }

        [Fact]
        public void CalculateTotalCost_WithZeroCount_ReturnsZero()
        {
            // Act
            int cost = _troopPurchaseService.CalculateTotalCost(DefenderRole.Rifleman, 0);

            // Assert
            Assert.Equal(0, cost);
        }

        #endregion

        #region PurchaseTroops Tests

        [Fact]
        public void PurchaseTroops_WithSufficientMoney_DeductsFromPlayerMoney()
        {
            // Arrange
            _mockGameBridge.Setup(g => g.GetPlayerMoney()).Returns(2000);
            var factionState = new FactionState(PlayerFactionId);
            _mockFactionService.Setup(f => f.GetFactionState(PlayerFactionId)).Returns(factionState);

            // Act - Buy 5 basic troops at $200 each = $1000
            var result = _troopPurchaseService.PurchaseTroops(PlayerFactionId, DefenderRole.Grunt, 5);

            // Assert
            Assert.True(result.Success);
            _mockGameBridge.Verify(g => g.AddPlayerMoney(-1000), Times.Once);
        }

        [Fact]
        public void PurchaseTroops_WithSufficientMoney_AddsTroopsToFactionReserve()
        {
            // Arrange
            _mockGameBridge.Setup(g => g.GetPlayerMoney()).Returns(2000);
            var factionState = new FactionState(PlayerFactionId);
            _mockFactionService.Setup(f => f.GetFactionState(PlayerFactionId)).Returns(factionState);

            // Act - Buy 5 basic troops
            var result = _troopPurchaseService.PurchaseTroops(PlayerFactionId, DefenderRole.Grunt, 5);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(5, result.TroopsPurchased);
            Assert.Equal(1000, result.TotalCost);
            _mockFactionService.Verify(
                f => f.AddReserveTroops(PlayerFactionId, DefenderRole.Grunt, 5),
                Times.Once);
        }

        [Fact]
        public void PurchaseTroops_WithInsufficientMoney_ReturnsFalse()
        {
            // Arrange
            _mockGameBridge.Setup(g => g.GetPlayerMoney()).Returns(500);

            // Act - Try to buy 5 basic troops ($1000 needed)
            var result = _troopPurchaseService.PurchaseTroops(PlayerFactionId, DefenderRole.Grunt, 5);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(0, result.TroopsPurchased);
            _mockGameBridge.Verify(g => g.AddPlayerMoney(It.IsAny<int>()), Times.Never);
            _mockFactionService.Verify(
                f => f.AddReserveTroops(It.IsAny<string>(), It.IsAny<DefenderRole>(), It.IsAny<int>()),
                Times.Never);
        }

        [Fact]
        public void PurchaseTroops_WithZeroCount_ReturnsSuccessWithNoChanges()
        {
            // Arrange
            _mockGameBridge.Setup(g => g.GetPlayerMoney()).Returns(2000);

            // Act
            var result = _troopPurchaseService.PurchaseTroops(PlayerFactionId, DefenderRole.Grunt, 0);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(0, result.TroopsPurchased);
            Assert.Equal(0, result.TotalCost);
            _mockGameBridge.Verify(g => g.AddPlayerMoney(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public void PurchaseTroops_WithNegativeCount_ThrowsArgumentOutOfRangeException()
        {
            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                _troopPurchaseService.PurchaseTroops(PlayerFactionId, DefenderRole.Grunt, -1));
        }

        [Fact]
        public void PurchaseTroops_WithNullFactionId_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                _troopPurchaseService.PurchaseTroops(null!, DefenderRole.Grunt, 5));
            Assert.Equal("factionId", exception.ParamName);
        }

        [Fact]
        public void PurchaseTroops_Medium_CorrectCostDeducted()
        {
            // Arrange
            _mockGameBridge.Setup(g => g.GetPlayerMoney()).Returns(2500);
            var factionState = new FactionState(PlayerFactionId);
            _mockFactionService.Setup(f => f.GetFactionState(PlayerFactionId)).Returns(factionState);

            // Act - Buy 3 medium troops at $500 each = $1500
            var result = _troopPurchaseService.PurchaseTroops(PlayerFactionId, DefenderRole.Gunner, 3);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(1500, result.TotalCost);
            _mockGameBridge.Verify(g => g.AddPlayerMoney(-1500), Times.Once);
        }

        [Fact]
        public void PurchaseTroops_Heavy_CorrectCostDeducted()
        {
            // Arrange
            _mockGameBridge.Setup(g => g.GetPlayerMoney()).Returns(5000);
            var factionState = new FactionState(PlayerFactionId);
            _mockFactionService.Setup(f => f.GetFactionState(PlayerFactionId)).Returns(factionState);

            // Act - Buy 2 heavy troops at $1000 each = $2000
            var result = _troopPurchaseService.PurchaseTroops(PlayerFactionId, DefenderRole.Rifleman, 2);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(2000, result.TotalCost);
            _mockGameBridge.Verify(g => g.AddPlayerMoney(-2000), Times.Once);
        }

        #endregion

        #region GetPlayerMoney Tests

        [Fact]
        public void GetPlayerMoney_ReturnsCurrentPlayerMoney()
        {
            // Arrange
            _mockGameBridge.Setup(g => g.GetPlayerMoney()).Returns(12345);

            // Act
            int money = _troopPurchaseService.GetPlayerMoney();

            // Assert
            Assert.Equal(12345, money);
        }

        #endregion
    }
}
