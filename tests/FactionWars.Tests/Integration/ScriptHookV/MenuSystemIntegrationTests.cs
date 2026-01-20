using FactionWars.Core.Utils;
using FactionWars.ScriptHookV;
using FactionWars.ScriptHookV.UI;
using FactionWars.Tests.Mocks;
using FactionWars.UI.Interfaces;
using Xunit;

namespace FactionWars.Tests.Integration.ScriptHookV
{
    /// <summary>
    /// Integration tests verifying the complete menu system:
    /// - F7 opens the main menu
    /// - All submenus (Overview, Zone Management, Army, Resources, Settings) work correctly
    /// - Navigation between menus functions properly
    /// </summary>
    public class MenuSystemIntegrationTests
    {
        private const int F7KeyCode = 118;

        private readonly MockGameBridge _gameBridge;
        private readonly ServiceContainer _container;
        private readonly GameLoopController _controller;
        private readonly MockMenuProvider _menuProvider;

        public MenuSystemIntegrationTests()
        {
            _gameBridge = new MockGameBridge
            {
                PlayerCharacterModel = "player_zero" // Michael
            };

            _menuProvider = new MockMenuProvider();
            _container = ServiceContainerFactory.Create(_gameBridge, _menuProvider);
            _controller = new GameLoopController(_container);

            // Initialize the controller
            _controller.OnTick();
        }

        #region F7 Key Opens Menu Tests

        [Fact]
        public void F7Key_OpensMainMenu()
        {
            // Act
            _controller.OnKeyDown(F7KeyCode);

            // Assert
            Assert.True(_menuProvider.IsMenuVisible);
            Assert.Equal(MainMenuController.MainMenuId, _menuProvider.CurrentMenuId);
        }

        [Fact]
        public void F7Key_WhenMenuOpen_ClosesMenu()
        {
            // Arrange
            _controller.OnKeyDown(F7KeyCode); // Open
            Assert.True(_menuProvider.IsMenuVisible);

            // Act
            _controller.OnKeyDown(F7KeyCode); // Close

            // Assert
            Assert.False(_menuProvider.IsMenuVisible);
        }

        [Fact]
        public void MainMenu_HasAllFiveSubmenus()
        {
            // Arrange
            _controller.OnKeyDown(F7KeyCode);

            // Act
            var menu = _menuProvider.GetCurrentMenuDefinition();

            // Assert
            Assert.NotNull(menu);
            Assert.Equal(5, menu!.Items.Count);
            Assert.NotNull(menu.GetItem(MainMenuController.OverviewItemId));
            Assert.NotNull(menu.GetItem(MainMenuController.ZoneManagementItemId));
            Assert.NotNull(menu.GetItem(MainMenuController.ArmyItemId));
            Assert.NotNull(menu.GetItem(MainMenuController.ResourcesItemId));
            Assert.NotNull(menu.GetItem(MainMenuController.SettingsItemId));
        }

        #endregion

        #region Overview Submenu Tests

        [Fact]
        public void OverviewSubmenu_OpensWhenSelected()
        {
            // Arrange
            _controller.OnKeyDown(F7KeyCode);

            // Act
            _menuProvider.SimulateItemSelection(MainMenuController.OverviewItemId);

            // Assert
            Assert.True(_menuProvider.IsMenuVisible);
            Assert.Equal(OverviewMenuController.OverviewMenuId, _menuProvider.CurrentMenuId);
        }

        [Fact]
        public void OverviewSubmenu_DisplaysFactionStats()
        {
            // Arrange
            _controller.OnKeyDown(F7KeyCode);
            _menuProvider.SimulateItemSelection(MainMenuController.OverviewItemId);

            // Act
            var menu = _menuProvider.GetCurrentMenuDefinition();

            // Assert
            Assert.NotNull(menu);
            Assert.NotNull(menu!.GetItem(OverviewMenuController.ZonesOwnedItemId));
            Assert.NotNull(menu.GetItem(OverviewMenuController.VictoryProgressItemId));
            Assert.NotNull(menu.GetItem(OverviewMenuController.CashItemId));
            Assert.NotNull(menu.GetItem(OverviewMenuController.TotalTroopsItemId));
            Assert.NotNull(menu.GetItem(OverviewMenuController.BackItemId));
        }

        [Fact]
        public void OverviewSubmenu_BackReturnsToMainMenu()
        {
            // Arrange
            _controller.OnKeyDown(F7KeyCode);
            _menuProvider.SimulateItemSelection(MainMenuController.OverviewItemId);
            Assert.Equal(OverviewMenuController.OverviewMenuId, _menuProvider.CurrentMenuId);

            // Act
            _menuProvider.SimulateItemSelection(OverviewMenuController.BackItemId);

            // Assert - Menu should reopen at main menu
            Assert.True(_menuProvider.IsMenuVisible);
            Assert.Equal(MainMenuController.MainMenuId, _menuProvider.CurrentMenuId);
        }

        #endregion

        #region Zone Management Submenu Tests

        [Fact]
        public void ZoneManagementSubmenu_OpensWhenSelected()
        {
            // Arrange
            _controller.OnKeyDown(F7KeyCode);

            // Act
            _menuProvider.SimulateItemSelection(MainMenuController.ZoneManagementItemId);

            // Assert
            Assert.True(_menuProvider.IsMenuVisible);
            Assert.Equal(ZoneManagementMenuController.ZoneManagementMenuId, _menuProvider.CurrentMenuId);
        }

        [Fact]
        public void ZoneManagementSubmenu_BackReturnsToMainMenu()
        {
            // Arrange
            _controller.OnKeyDown(F7KeyCode);
            _menuProvider.SimulateItemSelection(MainMenuController.ZoneManagementItemId);

            // Act
            _menuProvider.SimulateItemSelection(ZoneManagementMenuController.BackItemId);

            // Assert
            Assert.True(_menuProvider.IsMenuVisible);
            Assert.Equal(MainMenuController.MainMenuId, _menuProvider.CurrentMenuId);
        }

        #endregion

        #region Army Submenu Tests

        [Fact]
        public void ArmySubmenu_OpensWhenSelected()
        {
            // Arrange
            _controller.OnKeyDown(F7KeyCode);

            // Act
            _menuProvider.SimulateItemSelection(MainMenuController.ArmyItemId);

            // Assert
            Assert.True(_menuProvider.IsMenuVisible);
            Assert.Equal(ArmyMenuController.ArmyMenuId, _menuProvider.CurrentMenuId);
        }

        [Fact]
        public void ArmySubmenu_DisplaysPurchaseOptions()
        {
            // Arrange
            _controller.OnKeyDown(F7KeyCode);
            _menuProvider.SimulateItemSelection(MainMenuController.ArmyItemId);

            // Act
            var menu = _menuProvider.GetCurrentMenuDefinition();

            // Assert
            Assert.NotNull(menu);
            Assert.NotNull(menu!.GetItem(ArmyMenuController.PurchaseBasicItemId));
            Assert.NotNull(menu.GetItem(ArmyMenuController.PurchaseMediumItemId));
            Assert.NotNull(menu.GetItem(ArmyMenuController.PurchaseHeavyItemId));
            Assert.NotNull(menu.GetItem(ArmyMenuController.BackItemId));
        }

        [Fact]
        public void ArmySubmenu_BackReturnsToMainMenu()
        {
            // Arrange
            _controller.OnKeyDown(F7KeyCode);
            _menuProvider.SimulateItemSelection(MainMenuController.ArmyItemId);

            // Act
            _menuProvider.SimulateItemSelection(ArmyMenuController.BackItemId);

            // Assert
            Assert.True(_menuProvider.IsMenuVisible);
            Assert.Equal(MainMenuController.MainMenuId, _menuProvider.CurrentMenuId);
        }

        #endregion

        #region Resources Submenu Tests

        [Fact]
        public void ResourcesSubmenu_OpensWhenSelected()
        {
            // Arrange
            _controller.OnKeyDown(F7KeyCode);

            // Act
            _menuProvider.SimulateItemSelection(MainMenuController.ResourcesItemId);

            // Assert
            Assert.True(_menuProvider.IsMenuVisible);
            Assert.Equal(ResourcesMenuController.ResourcesMenuId, _menuProvider.CurrentMenuId);
        }

        [Fact]
        public void ResourcesSubmenu_DisplaysIncomeBreakdown()
        {
            // Arrange
            _controller.OnKeyDown(F7KeyCode);
            _menuProvider.SimulateItemSelection(MainMenuController.ResourcesItemId);

            // Act
            var menu = _menuProvider.GetCurrentMenuDefinition();

            // Assert
            Assert.NotNull(menu);
            Assert.NotNull(menu!.GetItem(ResourcesMenuController.NextTickItemId));
            Assert.NotNull(menu.GetItem(ResourcesMenuController.TotalCashItemId));
            Assert.NotNull(menu.GetItem(ResourcesMenuController.TotalRecruitmentItemId));
            Assert.NotNull(menu.GetItem(ResourcesMenuController.TotalWeaponsItemId));
            Assert.NotNull(menu.GetItem(ResourcesMenuController.BackItemId));
        }

        [Fact]
        public void ResourcesSubmenu_BackReturnsToMainMenu()
        {
            // Arrange
            _controller.OnKeyDown(F7KeyCode);
            _menuProvider.SimulateItemSelection(MainMenuController.ResourcesItemId);

            // Act
            _menuProvider.SimulateItemSelection(ResourcesMenuController.BackItemId);

            // Assert
            Assert.True(_menuProvider.IsMenuVisible);
            Assert.Equal(MainMenuController.MainMenuId, _menuProvider.CurrentMenuId);
        }

        #endregion

        #region Settings Submenu Tests

        [Fact]
        public void SettingsSubmenu_OpensWhenSelected()
        {
            // Arrange
            _controller.OnKeyDown(F7KeyCode);

            // Act
            _menuProvider.SimulateItemSelection(MainMenuController.SettingsItemId);

            // Assert
            Assert.True(_menuProvider.IsMenuVisible);
            Assert.Equal(SettingsMenuController.SettingsMenuId, _menuProvider.CurrentMenuId);
        }

        [Fact]
        public void SettingsSubmenu_DisplaysOptions()
        {
            // Arrange
            _controller.OnKeyDown(F7KeyCode);
            _menuProvider.SimulateItemSelection(MainMenuController.SettingsItemId);

            // Act
            var menu = _menuProvider.GetCurrentMenuDefinition();

            // Assert
            Assert.NotNull(menu);
            Assert.NotNull(menu!.GetItem(SettingsMenuController.SaveGameItemId));
            Assert.NotNull(menu.GetItem(SettingsMenuController.LoadGameItemId));
            Assert.NotNull(menu.GetItem(SettingsMenuController.DebugModeItemId));
            Assert.NotNull(menu.GetItem(SettingsMenuController.BackItemId));
        }

        [Fact]
        public void SettingsSubmenu_BackReturnsToMainMenu()
        {
            // Arrange
            _controller.OnKeyDown(F7KeyCode);
            _menuProvider.SimulateItemSelection(MainMenuController.SettingsItemId);

            // Act
            _menuProvider.SimulateItemSelection(SettingsMenuController.BackItemId);

            // Assert
            Assert.True(_menuProvider.IsMenuVisible);
            Assert.Equal(MainMenuController.MainMenuId, _menuProvider.CurrentMenuId);
        }

        #endregion

        #region Menu Update Tests

        [Fact]
        public void MenuUpdate_WhenMenuOpen_DoesNotThrow()
        {
            // Arrange
            _controller.OnKeyDown(F7KeyCode);

            // Act & Assert
            var exception = Record.Exception(() => _controller.OnTick());
            Assert.Null(exception);
        }

        [Fact]
        public void MenuUpdate_WhenSubMenuOpen_DoesNotThrow()
        {
            // Arrange
            _controller.OnKeyDown(F7KeyCode);
            _menuProvider.SimulateItemSelection(MainMenuController.OverviewItemId);

            // Act & Assert
            var exception = Record.Exception(() => _controller.OnTick());
            Assert.Null(exception);
        }

        #endregion
    }
}
