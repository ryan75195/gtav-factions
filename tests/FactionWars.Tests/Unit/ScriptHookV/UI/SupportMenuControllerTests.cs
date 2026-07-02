using FactionWars.Core.Interfaces;
using FactionWars.Core.Utils;
using FactionWars.Economy.Interfaces;
using FactionWars.ScriptHookV.UI;
using FactionWars.UI.Interfaces;
using FactionWars.UI.Models;
using Moq;
using System;
using System.Linq;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV.UI
{
    public class SupportMenuControllerTests
    {
        private readonly Mock<IMenuProvider> _menuProviderMock;
        private readonly MockGameBridge _gameBridge;
        private readonly Mock<ISupportPackageService> _supportServiceMock;
        private readonly Mock<IPlayerContext> _playerContextMock;
        private MenuDefinition? _lastShownMenu;

        public SupportMenuControllerTests()
        {
            _menuProviderMock = new Mock<IMenuProvider>();
            _gameBridge = new MockGameBridge();
            _gameBridge.PlayerMoney = 50000;
            _supportServiceMock = new Mock<ISupportPackageService>();
            _supportServiceMock.Setup(s => s.GetSupportSquadCost()).Returns(25000);
            _playerContextMock = new Mock<IPlayerContext>();
            _playerContextMock.Setup(p => p.CurrentFactionId).Returns("michael");

            _menuProviderMock.Setup(m => m.ShowMenu(It.IsAny<MenuDefinition>(), It.IsAny<string?>()))
                .Callback<MenuDefinition, string?>((menu, _) => _lastShownMenu = menu);
        }

        private SupportMenuController CreateController()
        {
            return new SupportMenuController(
                _menuProviderMock.Object, _gameBridge, _supportServiceMock.Object, _playerContextMock.Object);
        }

        [Fact]
        public void Show_ListsSupportSquadItemWithCost()
        {
            // Arrange
            var controller = CreateController();

            // Act
            controller.Show();

            // Assert
            Assert.NotNull(_lastShownMenu);
            var item = _lastShownMenu!.Items.FirstOrDefault(i => i.Id == SupportMenuController.SupportSquadItemId);
            Assert.NotNull(item);
            Assert.Contains("25,000", item!.Text);
        }

        [Fact]
        public void Show_DisplaysOwnedCount()
        {
            // Arrange
            _supportServiceMock.Setup(s => s.GetOwnedCount("michael")).Returns(3);
            var controller = CreateController();

            // Act
            controller.Show();

            // Assert
            Assert.NotNull(_lastShownMenu);
            var ownedItem = _lastShownMenu!.Items.FirstOrDefault(i => i.Id == SupportMenuController.OwnedDisplayItemId);
            Assert.NotNull(ownedItem);
            Assert.Contains("3", ownedItem!.Text);
        }

        [Fact]
        public void Show_DisablesSupportSquadItemWhenCannotAfford()
        {
            // Arrange
            _supportServiceMock.Setup(s => s.CanAfford()).Returns(false);
            var controller = CreateController();

            // Act
            controller.Show();

            // Assert
            Assert.NotNull(_lastShownMenu);
            var item = _lastShownMenu!.Items.FirstOrDefault(i => i.Id == SupportMenuController.SupportSquadItemId);
            Assert.NotNull(item);
            Assert.False(item!.IsEnabled);
        }

        [Fact]
        public void Show_EnablesSupportSquadItemWhenAffordable()
        {
            // Arrange
            _supportServiceMock.Setup(s => s.CanAfford()).Returns(true);
            var controller = CreateController();

            // Act
            controller.Show();

            // Assert
            Assert.NotNull(_lastShownMenu);
            var item = _lastShownMenu!.Items.FirstOrDefault(i => i.Id == SupportMenuController.SupportSquadItemId);
            Assert.NotNull(item);
            Assert.True(item!.IsEnabled);
        }

        [Fact]
        public void SelectingSupportSquad_PurchasesForCurrentFaction()
        {
            // Arrange
            _supportServiceMock.Setup(s => s.CanAfford()).Returns(true);
            _supportServiceMock.Setup(s => s.PurchaseSupportSquad("michael")).Returns(true);
            var controller = CreateController();
            controller.Show();

            // Act
            _menuProviderMock.Raise(m => m.ItemSelected += null,
                new MenuItemSelectedEventArgs(SupportMenuController.MenuId, SupportMenuController.SupportSquadItemId));

            // Assert
            _supportServiceMock.Verify(s => s.PurchaseSupportSquad("michael"), Times.Once);
        }

        [Fact]
        public void SelectingSupportSquad_ShowsNotificationOnSuccess()
        {
            // Arrange
            _supportServiceMock.Setup(s => s.PurchaseSupportSquad("michael")).Returns(true);
            var controller = CreateController();
            controller.Show();

            // Act
            _menuProviderMock.Raise(m => m.ItemSelected += null,
                new MenuItemSelectedEventArgs(SupportMenuController.MenuId, SupportMenuController.SupportSquadItemId));

            // Assert
            Assert.Contains(_gameBridge.Notifications, n => n.Contains("Support Squad"));
        }

        [Fact]
        public void SelectingBack_RaisesBackRequestedAndClosesMenu()
        {
            // Arrange
            var controller = CreateController();
            controller.Show();
            var wasRaised = false;
            controller.BackRequested += (s, e) => wasRaised = true;

            // Act
            _menuProviderMock.Raise(m => m.ItemSelected += null,
                new MenuItemSelectedEventArgs(SupportMenuController.MenuId, SupportMenuController.BackItemId));

            // Assert
            Assert.True(wasRaised);
            _menuProviderMock.Verify(m => m.CloseMenu(), Times.Once);
        }
    }
}
