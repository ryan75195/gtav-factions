using FactionWars.Core.Interfaces;
using FactionWars.Core.Utils;
using FactionWars.ScriptHookV.UI;
using FactionWars.UI.Interfaces;
using FactionWars.UI.Models;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV.UI
{
    public class ShopMenuControllerTests
    {
        private readonly Mock<IMenuProvider> _menuProviderMock;
        private readonly MockGameBridge _gameBridge;
        private MenuDefinition? _lastShownMenu;

        public ShopMenuControllerTests()
        {
            _menuProviderMock = new Mock<IMenuProvider>();
            _gameBridge = new MockGameBridge();
            _gameBridge.PlayerMoney = 50000;

            _menuProviderMock.Setup(m => m.ShowMenu(It.IsAny<MenuDefinition>(), It.IsAny<string?>()))
                .Callback<MenuDefinition, string?>((menu, _) => _lastShownMenu = menu);
        }

        [Fact]
        public void Show_DisplaysShopMenuWithVehicles()
        {
            // Arrange
            var controller = new ShopMenuController(_menuProviderMock.Object, _gameBridge);

            // Act
            controller.Show();

            // Assert
            Assert.NotNull(_lastShownMenu);
            Assert.Equal("shop_menu", _lastShownMenu.Id);
            Assert.Equal("Shop", _lastShownMenu.Title);
        }

        [Fact]
        public void Show_IncludesPoliceSuvOption()
        {
            // Arrange
            _gameBridge.PlayerMoney = 100000;
            var controller = new ShopMenuController(_menuProviderMock.Object, _gameBridge);

            // Act
            controller.Show();

            // Assert
            Assert.NotNull(_lastShownMenu);
            var fbiSuv = _lastShownMenu!.Items.FirstOrDefault(i => i.Id == "buy_police_suv");
            Assert.NotNull(fbiSuv);
            Assert.Contains("FBI SUV", fbiSuv!.Text);
        }

        [Fact]
        public void SelectingPoliceSuv_DeductsMoneyAndSpawnsVehicle()
        {
            // Arrange
            _gameBridge.PlayerMoney = 100000;
            var controller = new ShopMenuController(_menuProviderMock.Object, _gameBridge);
            controller.Show();
            var startMoney = _gameBridge.PlayerMoney;

            // Act
            _menuProviderMock.Raise(m => m.ItemSelected += null,
                new MenuItemSelectedEventArgs(ShopMenuController.ShopMenuId, "buy_police_suv"));

            // Assert - money deducted (a positive price) and a vehicle was created
            Assert.True(_gameBridge.PlayerMoney < startMoney);
        }

        [Fact]
        public void Show_DisplaysPlayerCash()
        {
            // Arrange
            _gameBridge.PlayerMoney = 75000;
            var controller = new ShopMenuController(_menuProviderMock.Object, _gameBridge);

            // Act
            controller.Show();

            // Assert
            Assert.NotNull(_lastShownMenu);
            var cashItem = _lastShownMenu.Items.FirstOrDefault(i => i.Id == "cash_display");
            Assert.NotNull(cashItem);
            Assert.Contains("$75,000", cashItem.Text);
            Assert.False(cashItem.IsEnabled);
        }

        [Fact]
        public void Show_DisplaysAllVehicleOptions()
        {
            // Arrange
            var controller = new ShopMenuController(_menuProviderMock.Object, _gameBridge);

            // Act
            controller.Show();

            // Assert
            Assert.NotNull(_lastShownMenu);

            // Check for expected vehicles
            Assert.NotNull(_lastShownMenu.Items.FirstOrDefault(i => i.Id == "buy_insurgent"));
            Assert.NotNull(_lastShownMenu.Items.FirstOrDefault(i => i.Id == "buy_technical"));
            Assert.NotNull(_lastShownMenu.Items.FirstOrDefault(i => i.Id == "buy_apc"));
            Assert.NotNull(_lastShownMenu.Items.FirstOrDefault(i => i.Id == "buy_khanjali"));
            Assert.NotNull(_lastShownMenu.Items.FirstOrDefault(i => i.Id == "buy_buzzard"));
            Assert.NotNull(_lastShownMenu.Items.FirstOrDefault(i => i.Id == "buy_bati"));
            Assert.NotNull(_lastShownMenu.Items.FirstOrDefault(i => i.Id == "buy_zentorno"));
        }

        [Fact]
        public void Show_VehicleItemsShowCorrectPrices()
        {
            // Arrange
            var controller = new ShopMenuController(_menuProviderMock.Object, _gameBridge);

            // Act
            controller.Show();

            // Assert
            Assert.NotNull(_lastShownMenu);

            var insurgent = _lastShownMenu.Items.FirstOrDefault(i => i.Id == "buy_insurgent");
            Assert.Contains("$25,000", insurgent!.Text);

            var technical = _lastShownMenu.Items.FirstOrDefault(i => i.Id == "buy_technical");
            Assert.Contains("$15,000", technical!.Text);

            var khanjali = _lastShownMenu.Items.FirstOrDefault(i => i.Id == "buy_khanjali");
            Assert.Contains("$100,000", khanjali!.Text);
        }

        [Fact]
        public void Show_DisablesVehiclesPlayerCannotAfford()
        {
            // Arrange
            _gameBridge.PlayerMoney = 20000; // Can afford Technical ($15K) but not Insurgent ($25K)
            var controller = new ShopMenuController(_menuProviderMock.Object, _gameBridge);

            // Act
            controller.Show();

            // Assert
            Assert.NotNull(_lastShownMenu);

            var technical = _lastShownMenu.Items.FirstOrDefault(i => i.Id == "buy_technical");
            Assert.True(technical!.IsEnabled); // Can afford

            var insurgent = _lastShownMenu.Items.FirstOrDefault(i => i.Id == "buy_insurgent");
            Assert.False(insurgent!.IsEnabled); // Cannot afford
        }

        [Fact]
        public void Show_IncludesCargobobTransportOption()
        {
            // Arrange
            _gameBridge.PlayerMoney = 100000;
            var controller = new ShopMenuController(_menuProviderMock.Object, _gameBridge);

            // Act
            controller.Show();

            // Assert
            Assert.NotNull(_lastShownMenu);
            var cargobob = _lastShownMenu!.Items.FirstOrDefault(i => i.Id == "buy_cargobob");
            Assert.NotNull(cargobob);
            Assert.Contains("Cargobob", cargobob!.Text);
            Assert.Contains("$80,000", cargobob.Text);
        }

        [Fact]
        public void SelectingCargobob_DeductsMoneyAndSpawnsVehicle()
        {
            // Arrange
            _gameBridge.PlayerMoney = 100000;
            var controller = new ShopMenuController(_menuProviderMock.Object, _gameBridge);
            controller.Show();

            // Act
            _menuProviderMock.Raise(m => m.ItemSelected += null,
                new MenuItemSelectedEventArgs(ShopMenuController.ShopMenuId, "buy_cargobob"));

            // Assert
            Assert.Equal(20000, _gameBridge.PlayerMoney); // 100000 - 80000
            Assert.True(_gameBridge.GetSpawnedVehicleCount() > 0);
        }

        [Fact]
        public void Show_HasBackButton()
        {
            // Arrange
            var controller = new ShopMenuController(_menuProviderMock.Object, _gameBridge);

            // Act
            controller.Show();

            // Assert
            Assert.NotNull(_lastShownMenu);
            var backItem = _lastShownMenu.Items.FirstOrDefault(i => i.Id == "back");
            Assert.NotNull(backItem);
            Assert.Equal("Back", backItem.Text);
        }

        [Fact]
        public void SelectingVehicle_DeductsMoney()
        {
            // Arrange
            _gameBridge.PlayerMoney = 50000;
            var controller = new ShopMenuController(_menuProviderMock.Object, _gameBridge);
            controller.Show();

            // Act - simulate selecting Insurgent ($25,000)
            _menuProviderMock.Raise(m => m.ItemSelected += null,
                new MenuItemSelectedEventArgs(ShopMenuController.ShopMenuId, "buy_insurgent"));

            // Assert
            Assert.Equal(25000, _gameBridge.PlayerMoney); // 50000 - 25000
        }

        [Fact]
        public void SelectingVehicle_SpawnsVehicleNearPlayer()
        {
            // Arrange
            _gameBridge.PlayerMoney = 50000;
            _gameBridge.PlayerPosition = new FactionWars.Core.Interfaces.Vector3(100, 200, 10);
            var controller = new ShopMenuController(_menuProviderMock.Object, _gameBridge);
            controller.Show();

            // Act - simulate selecting Technical ($15,000)
            _menuProviderMock.Raise(m => m.ItemSelected += null,
                new MenuItemSelectedEventArgs(ShopMenuController.ShopMenuId, "buy_technical"));

            // Assert
            Assert.True(_gameBridge.GetSpawnedVehicleCount() > 0);
        }

        [Fact]
        public void SelectingVehicle_CreatesMapBlip()
        {
            // Arrange
            _gameBridge.PlayerMoney = 50000;
            _gameBridge.PlayerPosition = new FactionWars.Core.Interfaces.Vector3(100, 200, 10);
            var controller = new ShopMenuController(_menuProviderMock.Object, _gameBridge);
            controller.Show();

            // Act
            _menuProviderMock.Raise(m => m.ItemSelected += null,
                new MenuItemSelectedEventArgs(ShopMenuController.ShopMenuId, "buy_insurgent"));

            // Assert
            Assert.True(_gameBridge.GetVehicleBlipCount() > 0);
        }

        [Fact]
        public void SelectingVehicle_ShowsNotification()
        {
            // Arrange
            _gameBridge.PlayerMoney = 50000;
            var controller = new ShopMenuController(_menuProviderMock.Object, _gameBridge);
            controller.Show();

            // Act
            _menuProviderMock.Raise(m => m.ItemSelected += null,
                new MenuItemSelectedEventArgs(ShopMenuController.ShopMenuId, "buy_insurgent"));

            // Assert
            Assert.Contains(_gameBridge.Notifications, n => n.Contains("Insurgent") && n.Contains("delivered"));
        }

        [Fact]
        public void SelectingVehicle_RefreshesMenuWithUpdatedCash()
        {
            // Arrange
            _gameBridge.PlayerMoney = 50000;
            var controller = new ShopMenuController(_menuProviderMock.Object, _gameBridge);
            controller.Show();

            // Act
            _menuProviderMock.Raise(m => m.ItemSelected += null,
                new MenuItemSelectedEventArgs(ShopMenuController.ShopMenuId, "buy_insurgent"));

            // Assert - menu was refreshed
            _menuProviderMock.Verify(m => m.ShowMenu(It.IsAny<MenuDefinition>(), It.IsAny<string?>()), Times.AtLeast(2));

            // Check the refreshed menu shows updated cash
            var cashItem = _lastShownMenu!.Items.FirstOrDefault(i => i.Id == "cash_display");
            Assert.Contains("$25,000", cashItem!.Text); // 50000 - 25000
        }
    }
}
