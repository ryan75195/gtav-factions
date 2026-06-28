using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.Economy.Interfaces;
using FactionWars.Economy.Models;
using FactionWars.Factions.Interfaces;
using FactionWars.Factions.Models;
using FactionWars.ScriptHookV.UI;
using FactionWars.Tests.Mocks;
using Moq;
using System;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV.UI
{
    public class DefendersMenuControllerTests
    {
        private readonly MockMenuProvider _menuProvider;
        private readonly Mock<IFactionService> _factionServiceMock;
        private readonly Mock<ITroopPurchaseService> _purchaseServiceMock;
        private readonly Mock<IPlayerContext> _playerContextMock;
        private readonly DefendersMenuController _controller;

        private const string PlayerFactionId = "michael";

        public DefendersMenuControllerTests()
        {
            _menuProvider = new MockMenuProvider();
            _factionServiceMock = new Mock<IFactionService>();
            _purchaseServiceMock = new Mock<ITroopPurchaseService>();
            _playerContextMock = new Mock<IPlayerContext>();

            _playerContextMock.Setup(p => p.CurrentFactionId).Returns(PlayerFactionId);

            var factionState = new FactionState(PlayerFactionId, 10000);
            factionState.AddReserveTroops(DefenderRole.Grunt, 20);
            factionState.AddReserveTroops(DefenderRole.Gunner, 15);
            factionState.AddReserveTroops(DefenderRole.Rifleman, 10);
            factionState.AddReserveTroops(DefenderRole.Rocketeer, 5);
            _factionServiceMock.Setup(f => f.GetFactionState(PlayerFactionId)).Returns(factionState);

            _purchaseServiceMock.Setup(p => p.GetPlayerMoney()).Returns(25000);
            _purchaseServiceMock.Setup(p => p.GetTroopCost(DefenderRole.Grunt)).Returns(200);
            _purchaseServiceMock.Setup(p => p.GetTroopCost(DefenderRole.Gunner)).Returns(500);
            _purchaseServiceMock.Setup(p => p.GetTroopCost(DefenderRole.Rifleman)).Returns(1000);
            _purchaseServiceMock.Setup(p => p.GetTroopCost(DefenderRole.Rocketeer)).Returns(2000);
            _purchaseServiceMock.Setup(p => p.GetTroopCost(DefenderRole.Sniper)).Returns(1500);

            _controller = new DefendersMenuController(
                _menuProvider,
                _factionServiceMock.Object,
                _purchaseServiceMock.Object,
                _playerContextMock.Object);
        }

        [Fact]
        public void Constructor_WithNullMenuProvider_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new DefendersMenuController(
                null!,
                _factionServiceMock.Object,
                _purchaseServiceMock.Object,
                _playerContextMock.Object));
        }

        [Fact]
        public void Constructor_WithNullFactionService_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new DefendersMenuController(
                _menuProvider,
                null!,
                _purchaseServiceMock.Object,
                _playerContextMock.Object));
        }

        [Fact]
        public void Constructor_WithNullPurchaseService_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new DefendersMenuController(
                _menuProvider,
                _factionServiceMock.Object,
                null!,
                _playerContextMock.Object));
        }

        [Fact]
        public void Constructor_WithNullPlayerContext_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new DefendersMenuController(
                _menuProvider,
                _factionServiceMock.Object,
                _purchaseServiceMock.Object,
                null!));
        }

        [Fact]
        public void Show_ShouldDisplayDefendersMenu()
        {
            // Act
            _controller.Show();

            // Assert
            Assert.True(_menuProvider.IsMenuVisible);
            Assert.Equal(DefendersMenuController.MenuId, _menuProvider.CurrentMenuId);
        }

        [Fact]
        public void Show_MenuShouldHaveCorrectTitle()
        {
            // Act
            _controller.Show();

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            Assert.NotNull(menu);
            Assert.Equal("Defenders", menu!.Title);
        }

        [Fact]
        public void Show_ShouldIncludeAllFourTierPurchaseOptions()
        {
            // Arrange
            _purchaseServiceMock.Setup(p => p.CanAfford(It.IsAny<DefenderRole>(), 1)).Returns(true);

            // Act
            _controller.Show();

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            Assert.NotNull(menu);

            Assert.NotNull(menu!.GetItem(DefendersMenuController.PurchaseBasicItemId));
            Assert.NotNull(menu.GetItem(DefendersMenuController.PurchaseMediumItemId));
            Assert.NotNull(menu.GetItem(DefendersMenuController.PurchaseHeavyItemId));
            Assert.NotNull(menu.GetItem(DefendersMenuController.PurchaseEliteItemId));
        }

        [Fact]
        public void Show_ShouldIncludeReserveSummary()
        {
            // Act
            _controller.Show();

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            Assert.NotNull(menu);
            var reserveItem = menu!.GetItem(DefendersMenuController.ReserveSummaryItemId);
            Assert.NotNull(reserveItem);
            Assert.Contains("20", reserveItem!.Text); // Basic
            Assert.Contains("15", reserveItem.Text); // Medium
            Assert.Contains("10", reserveItem.Text); // Heavy
            Assert.Contains("5", reserveItem.Text);  // Elite
        }

        [Fact]
        public void Show_ShouldIncludeBackButton()
        {
            // Act
            _controller.Show();

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            Assert.NotNull(menu);
            Assert.NotNull(menu!.GetItem(DefendersMenuController.BackItemId));
        }

        [Fact]
        public void Show_ShouldHaveEightItems()
        {
            // Act
            _controller.Show();

            // Assert - money display, reserve summary, 5 purchase options, back = 8
            var menu = _menuProvider.GetCurrentMenuDefinition();
            Assert.NotNull(menu);
            Assert.Equal(8, menu!.Items.Count);
        }

        [Fact]
        public void PurchaseBasic_WithSufficientFunds_ShouldCallPurchaseService()
        {
            // Arrange
            _purchaseServiceMock.Setup(p => p.CanAfford(DefenderRole.Grunt, 1)).Returns(true);
            _purchaseServiceMock.Setup(p => p.PurchaseTroops(PlayerFactionId, DefenderRole.Grunt, 1))
                .Returns(TroopPurchaseResult.Successful(DefenderRole.Grunt, 1, 200));
            _controller.Show();

            // Act
            _menuProvider.SimulateItemSelection(DefendersMenuController.PurchaseBasicItemId);

            // Assert
            _purchaseServiceMock.Verify(p => p.PurchaseTroops(PlayerFactionId, DefenderRole.Grunt, 1), Times.Once);
        }

        [Fact]
        public void PurchaseElite_WithSufficientFunds_ShouldCallPurchaseService()
        {
            // Arrange
            _purchaseServiceMock.Setup(p => p.CanAfford(DefenderRole.Rocketeer, 1)).Returns(true);
            _purchaseServiceMock.Setup(p => p.PurchaseTroops(PlayerFactionId, DefenderRole.Rocketeer, 1))
                .Returns(TroopPurchaseResult.Successful(DefenderRole.Rocketeer, 1, 2000));
            _controller.Show();

            // Act
            _menuProvider.SimulateItemSelection(DefendersMenuController.PurchaseEliteItemId);

            // Assert
            _purchaseServiceMock.Verify(p => p.PurchaseTroops(PlayerFactionId, DefenderRole.Rocketeer, 1), Times.Once);
        }

        [Fact]
        public void Back_ShouldRaiseBackRequestedEvent()
        {
            // Arrange
            var eventRaised = false;
            _controller.BackRequested += (s, e) => eventRaised = true;
            _controller.Show();

            // Act
            _menuProvider.SimulateItemSelection(DefendersMenuController.BackItemId);

            // Assert
            Assert.True(eventRaised);
        }

        [Fact]
        public void Back_ShouldCloseMenu()
        {
            // Arrange
            _controller.Show();

            // Act
            _menuProvider.SimulateItemSelection(DefendersMenuController.BackItemId);

            // Assert
            Assert.False(_menuProvider.IsMenuVisible);
        }

        [Fact]
        public void ElitePurchaseItem_ShouldShowCorrectCost()
        {
            // Act
            _controller.Show();

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            var eliteItem = menu?.GetItem(DefendersMenuController.PurchaseEliteItemId);
            Assert.NotNull(eliteItem);
            Assert.Contains("2,000", eliteItem!.Text);
        }

        [Fact]
        public void Show_ShouldIncludeSniperPurchaseOption()
        {
            // Arrange
            _purchaseServiceMock.Setup(p => p.CanAfford(DefenderRole.Sniper, 1)).Returns(true);

            // Act
            _controller.Show();

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            Assert.NotNull(menu);
            var sniperItem = menu!.GetItem(DefendersMenuController.PurchaseSniperItemId);
            Assert.NotNull(sniperItem);
            Assert.Contains("Sniper", sniperItem!.Text);
        }

        [Fact]
        public void PurchaseSniper_WithSufficientFunds_ShouldCallPurchaseService()
        {
            // Arrange
            _purchaseServiceMock.Setup(p => p.CanAfford(DefenderRole.Sniper, 1)).Returns(true);
            _purchaseServiceMock.Setup(p => p.PurchaseTroops(PlayerFactionId, DefenderRole.Sniper, 1))
                .Returns(TroopPurchaseResult.Successful(DefenderRole.Sniper, 1, 1500));
            _controller.Show();

            // Act
            _menuProvider.SimulateItemSelection(DefendersMenuController.PurchaseSniperItemId);

            // Assert
            _purchaseServiceMock.Verify(p => p.PurchaseTroops(PlayerFactionId, DefenderRole.Sniper, 1), Times.Once);
        }
    }
}
