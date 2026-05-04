using FactionWars.ScriptHookV.UI;
using FactionWars.Tests.Mocks;
using FactionWars.UI.Interfaces;
using FactionWars.UI.Models;
using System;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV.UI
{
    /// <summary>
    /// Tests for MainMenuController handling menu visibility and navigation.
    /// </summary>
    public class MainMenuControllerTests
    {
        private const int F7KeyCode = 118;
        private readonly MockMenuProvider _menuProvider;
        private readonly MainMenuController _controller;

        public MainMenuControllerTests()
        {
            _menuProvider = new MockMenuProvider();
            _controller = new MainMenuController(_menuProvider);
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithNullMenuProvider_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new MainMenuController(null!));
        }

        [Fact]
        public void Constructor_ShouldNotShowMenuInitially()
        {
            // Assert
            Assert.False(_menuProvider.IsMenuVisible);
        }

        #endregion

        #region F7 Key Toggle Tests

        [Fact]
        public void OnKeyDown_WithF7Key_ShouldOpenMenu()
        {
            // Act
            _controller.OnKeyDown(F7KeyCode);

            // Assert
            Assert.True(_menuProvider.IsMenuVisible);
        }

        [Fact]
        public void OnKeyDown_WithF7Key_WhenMenuOpen_ShouldCloseMenu()
        {
            // Arrange - open the menu first
            _controller.OnKeyDown(F7KeyCode);
            Assert.True(_menuProvider.IsMenuVisible);

            // Act - press F7 again
            _controller.OnKeyDown(F7KeyCode);

            // Assert
            Assert.False(_menuProvider.IsMenuVisible);
        }

        [Fact]
        public void OnKeyDown_WithOtherKey_ShouldNotAffectMenu()
        {
            // Act
            _controller.OnKeyDown(65); // 'A' key

            // Assert
            Assert.False(_menuProvider.IsMenuVisible);
        }

        [Fact]
        public void OnKeyDown_WithOtherKey_WhenMenuOpen_ShouldNotCloseMenu()
        {
            // Arrange
            _controller.OnKeyDown(F7KeyCode); // Open menu

            // Act
            _controller.OnKeyDown(65); // Press 'A' key

            // Assert
            Assert.True(_menuProvider.IsMenuVisible);
        }

        #endregion

        #region Main Menu Structure Tests

        [Fact]
        public void OnKeyDown_WithF7Key_ShouldShowMainMenu()
        {
            // Act
            _controller.OnKeyDown(F7KeyCode);

            // Assert
            Assert.Equal(MainMenuController.MainMenuId, _menuProvider.CurrentMenuId);
        }

        [Fact]
        public void ShowMainMenu_ShouldOpenMainMenu()
        {
            _controller.ShowMainMenu();

            Assert.True(_menuProvider.IsMenuVisible);
            Assert.Equal(MainMenuController.MainMenuId, _menuProvider.CurrentMenuId);
        }

        [Fact]
        public void MainMenu_ShouldHaveCorrectTitle()
        {
            // Act
            _controller.OnKeyDown(F7KeyCode);

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            Assert.NotNull(menu);
            Assert.Equal("Faction Wars", menu!.Title);
        }

        [Fact]
        public void MainMenu_ShouldHaveZoneManagementItem()
        {
            // Act
            _controller.OnKeyDown(F7KeyCode);

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            Assert.NotNull(menu);
            var item = menu!.GetItem(MainMenuController.ZoneManagementItemId);
            Assert.NotNull(item);
            Assert.Equal("Zone Management", item!.Text);
        }

        [Fact]
        public void MainMenu_ShouldHaveRecruitmentItem()
        {
            // Act
            _controller.OnKeyDown(F7KeyCode);

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            Assert.NotNull(menu);
            var item = menu!.GetItem(MainMenuController.RecruitmentItemId);
            Assert.NotNull(item);
            Assert.Equal("Recruitment", item!.Text);
        }

        [Fact]
        public void MainMenu_ShouldHaveSettingsItem()
        {
            // Act
            _controller.OnKeyDown(F7KeyCode);

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            Assert.NotNull(menu);
            var item = menu!.GetItem(MainMenuController.SettingsItemId);
            Assert.NotNull(item);
            Assert.Equal("Settings", item!.Text);
        }

        [Fact]
        public void MainMenu_ShouldHaveFourItems()
        {
            // Act
            _controller.OnKeyDown(F7KeyCode);

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            Assert.NotNull(menu);
            Assert.Equal(4, menu!.Items.Count);
        }

        [Fact]
        public void MainMenu_ItemsShouldBeInCorrectOrder()
        {
            // Act
            _controller.OnKeyDown(F7KeyCode);

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            Assert.NotNull(menu);
            Assert.Equal(MainMenuController.ZoneManagementItemId, menu!.Items[0].Id);
            Assert.Equal(MainMenuController.RecruitmentItemId, menu!.Items[1].Id);
            Assert.Equal(MainMenuController.ShopItemId, menu!.Items[2].Id);
            Assert.Equal(MainMenuController.SettingsItemId, menu!.Items[3].Id);
        }

        #endregion

        #region Menu Item Descriptions Tests

        [Fact]
        public void ZoneManagementItem_ShouldHaveDescription()
        {
            // Act
            _controller.OnKeyDown(F7KeyCode);

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            var item = menu?.GetItem(MainMenuController.ZoneManagementItemId);
            Assert.NotNull(item);
            Assert.False(string.IsNullOrEmpty(item!.Description));
        }

        [Fact]
        public void RecruitmentItem_ShouldHaveDescription()
        {
            // Act
            _controller.OnKeyDown(F7KeyCode);

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            var item = menu?.GetItem(MainMenuController.RecruitmentItemId);
            Assert.NotNull(item);
            Assert.False(string.IsNullOrEmpty(item!.Description));
        }

        [Fact]
        public void SettingsItem_ShouldHaveDescription()
        {
            // Act
            _controller.OnKeyDown(F7KeyCode);

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            var item = menu?.GetItem(MainMenuController.SettingsItemId);
            Assert.NotNull(item);
            Assert.False(string.IsNullOrEmpty(item!.Description));
        }

        #endregion

        #region IsMenuOpen Property Tests

        [Fact]
        public void IsMenuOpen_WhenMenuClosed_ShouldReturnFalse()
        {
            // Assert
            Assert.False(_controller.IsMenuOpen);
        }

        [Fact]
        public void IsMenuOpen_WhenMenuOpen_ShouldReturnTrue()
        {
            // Arrange
            _controller.OnKeyDown(F7KeyCode);

            // Assert
            Assert.True(_controller.IsMenuOpen);
        }

        #endregion

        #region Update Tests

        [Fact]
        public void Update_WhenMenuClosed_ShouldNotThrow()
        {
            // Act & Assert
            var exception = Record.Exception(() => _controller.Update());
            Assert.Null(exception);
        }

        [Fact]
        public void Update_WhenMenuOpen_ShouldNotThrow()
        {
            // Arrange
            _controller.OnKeyDown(F7KeyCode);

            // Act & Assert
            var exception = Record.Exception(() => _controller.Update());
            Assert.Null(exception);
        }

        #endregion
    }
}
