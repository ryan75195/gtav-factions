using FactionWars.ScriptHookV.UI;
using FactionWars.UI.Interfaces;
using FactionWars.UI.Models;
using System;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV.UI
{
    /// <summary>
    /// Tests for NativeUIMenuProvider implementing IMenuProvider.
    /// </summary>
    public class NativeUIMenuProviderTests
    {
        private readonly NativeUIMenuProvider _provider;

        public NativeUIMenuProviderTests()
        {
            _provider = new NativeUIMenuProvider();
        }

        #region Interface Implementation Tests

        [Fact]
        public void NativeUIMenuProvider_ShouldImplementIMenuProvider()
        {
            // Assert
            Assert.IsAssignableFrom<IMenuProvider>(_provider);
        }

        #endregion

        #region Initial State Tests

        [Fact]
        public void IsMenuVisible_WhenNoMenuShown_ShouldReturnFalse()
        {
            // Assert
            Assert.False(_provider.IsMenuVisible);
        }

        [Fact]
        public void CurrentMenuId_WhenNoMenuShown_ShouldReturnNull()
        {
            // Assert
            Assert.Null(_provider.CurrentMenuId);
        }

        #endregion

        #region ShowMenu Tests

        [Fact]
        public void ShowMenu_WithValidDefinition_ShouldSetIsMenuVisibleToTrue()
        {
            // Arrange
            var definition = new MenuDefinition("test_menu", "Test Menu");

            // Act
            _provider.ShowMenu(definition);

            // Assert
            Assert.True(_provider.IsMenuVisible);
        }

        [Fact]
        public void ShowMenu_WithValidDefinition_ShouldSetCurrentMenuId()
        {
            // Arrange
            var definition = new MenuDefinition("test_menu", "Test Menu");

            // Act
            _provider.ShowMenu(definition);

            // Assert
            Assert.Equal("test_menu", _provider.CurrentMenuId);
        }

        [Fact]
        public void ShowMenu_WithNullDefinition_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _provider.ShowMenu(null!));
        }

        [Fact]
        public void ShowMenu_WhenMenuAlreadyOpen_ShouldReplaceWithNewMenu()
        {
            // Arrange
            var definition1 = new MenuDefinition("menu_1", "Menu 1");
            var definition2 = new MenuDefinition("menu_2", "Menu 2");

            // Act
            _provider.ShowMenu(definition1);
            _provider.ShowMenu(definition2);

            // Assert
            Assert.Equal("menu_2", _provider.CurrentMenuId);
            Assert.True(_provider.IsMenuVisible);
        }

        [Fact]
        public void ShowMenu_ShouldStoreMenuDefinition()
        {
            // Arrange
            var definition = new MenuDefinition("test_menu", "Test Menu", "Subtitle");
            definition.AddItem(new MenuItem("item_1", "Item 1"));
            definition.AddItem(new MenuItem("item_2", "Item 2"));

            // Act
            _provider.ShowMenu(definition);

            // Assert
            var currentDefinition = _provider.GetCurrentMenuDefinition();
            Assert.NotNull(currentDefinition);
            Assert.Equal("Test Menu", currentDefinition!.Title);
            Assert.Equal("Subtitle", currentDefinition.Subtitle);
            Assert.Equal(2, currentDefinition.Items.Count);
        }

        #endregion

        #region CloseMenu Tests

        [Fact]
        public void CloseMenu_WhenMenuOpen_ShouldSetIsMenuVisibleToFalse()
        {
            // Arrange
            var definition = new MenuDefinition("test_menu", "Test Menu");
            _provider.ShowMenu(definition);

            // Act
            _provider.CloseMenu();

            // Assert
            Assert.False(_provider.IsMenuVisible);
        }

        [Fact]
        public void CloseMenu_WhenMenuOpen_ShouldSetCurrentMenuIdToNull()
        {
            // Arrange
            var definition = new MenuDefinition("test_menu", "Test Menu");
            _provider.ShowMenu(definition);

            // Act
            _provider.CloseMenu();

            // Assert
            Assert.Null(_provider.CurrentMenuId);
        }

        [Fact]
        public void CloseMenu_WhenNoMenuOpen_ShouldNotThrow()
        {
            // Act & Assert - should not throw
            var exception = Record.Exception(() => _provider.CloseMenu());
            Assert.Null(exception);
        }

        [Fact]
        public void CloseMenu_ShouldRaiseMenuClosedEvent()
        {
            // Arrange
            var definition = new MenuDefinition("test_menu", "Test Menu");
            _provider.ShowMenu(definition);
            var eventRaised = false;
            _provider.MenuClosed += (sender, args) => eventRaised = true;

            // Act
            _provider.CloseMenu();

            // Assert
            Assert.True(eventRaised);
        }

        [Fact]
        public void CloseMenu_WhenNoMenuOpen_ShouldNotRaiseMenuClosedEvent()
        {
            // Arrange
            var eventRaised = false;
            _provider.MenuClosed += (sender, args) => eventRaised = true;

            // Act
            _provider.CloseMenu();

            // Assert
            Assert.False(eventRaised);
        }

        #endregion

        #region Update Tests

        [Fact]
        public void Update_WhenNoMenuOpen_ShouldNotThrow()
        {
            // Act & Assert - should not throw
            var exception = Record.Exception(() => _provider.Update());
            Assert.Null(exception);
        }

        [Fact]
        public void Update_WhenMenuOpen_ShouldNotThrow()
        {
            // Arrange
            var definition = new MenuDefinition("test_menu", "Test Menu");
            _provider.ShowMenu(definition);

            // Act & Assert - should not throw
            var exception = Record.Exception(() => _provider.Update());
            Assert.Null(exception);
        }

        #endregion

        #region ItemSelected Event Tests

        [Fact]
        public void ItemSelected_ShouldBeRaisableBySimulateItemSelection()
        {
            // Arrange
            var definition = new MenuDefinition("test_menu", "Test Menu");
            definition.AddItem(new MenuItem("item_1", "Item 1"));
            _provider.ShowMenu(definition);

            MenuItemSelectedEventArgs? receivedArgs = null;
            _provider.ItemSelected += (sender, args) => receivedArgs = args;

            // Act
            _provider.SimulateItemSelection("item_1");

            // Assert
            Assert.NotNull(receivedArgs);
            Assert.Equal("test_menu", receivedArgs!.MenuId);
            Assert.Equal("item_1", receivedArgs.ItemId);
        }

        [Fact]
        public void SimulateItemSelection_WhenNoMenuOpen_ShouldNotRaiseEvent()
        {
            // Arrange
            MenuItemSelectedEventArgs? receivedArgs = null;
            _provider.ItemSelected += (sender, args) => receivedArgs = args;

            // Act
            _provider.SimulateItemSelection("item_1");

            // Assert
            Assert.Null(receivedArgs);
        }

        [Fact]
        public void SimulateItemSelection_WithInvalidItemId_ShouldNotRaiseEvent()
        {
            // Arrange
            var definition = new MenuDefinition("test_menu", "Test Menu");
            definition.AddItem(new MenuItem("item_1", "Item 1"));
            _provider.ShowMenu(definition);

            MenuItemSelectedEventArgs? receivedArgs = null;
            _provider.ItemSelected += (sender, args) => receivedArgs = args;

            // Act
            _provider.SimulateItemSelection("nonexistent_item");

            // Assert
            Assert.Null(receivedArgs);
        }

        [Fact]
        public void SimulateItemSelection_WithDisabledItem_ShouldNotRaiseEvent()
        {
            // Arrange
            var definition = new MenuDefinition("test_menu", "Test Menu");
            var item = new MenuItem("item_1", "Item 1");
            item.IsEnabled = false;
            definition.AddItem(item);
            _provider.ShowMenu(definition);

            MenuItemSelectedEventArgs? receivedArgs = null;
            _provider.ItemSelected += (sender, args) => receivedArgs = args;

            // Act
            _provider.SimulateItemSelection("item_1");

            // Assert
            Assert.Null(receivedArgs);
        }

        #endregion

        #region GetCurrentMenuDefinition Tests

        [Fact]
        public void GetCurrentMenuDefinition_WhenNoMenuOpen_ShouldReturnNull()
        {
            // Act
            var result = _provider.GetCurrentMenuDefinition();

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetCurrentMenuDefinition_AfterClose_ShouldReturnNull()
        {
            // Arrange
            var definition = new MenuDefinition("test_menu", "Test Menu");
            _provider.ShowMenu(definition);
            _provider.CloseMenu();

            // Act
            var result = _provider.GetCurrentMenuDefinition();

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region Selected Index Tests

        [Fact]
        public void SelectedIndex_WhenMenuOpens_ShouldBeZero()
        {
            // Arrange
            var definition = new MenuDefinition("test_menu", "Test Menu");
            definition.AddItem(new MenuItem("item_1", "Item 1"));
            definition.AddItem(new MenuItem("item_2", "Item 2"));

            // Act
            _provider.ShowMenu(definition);

            // Assert
            Assert.Equal(0, _provider.SelectedIndex);
        }

        [Fact]
        public void SelectedIndex_WhenNoMenuOpen_ShouldBeNegativeOne()
        {
            // Assert
            Assert.Equal(-1, _provider.SelectedIndex);
        }

        [Fact]
        public void MoveSelectionDown_ShouldIncrementSelectedIndex()
        {
            // Arrange
            var definition = new MenuDefinition("test_menu", "Test Menu");
            definition.AddItem(new MenuItem("item_1", "Item 1"));
            definition.AddItem(new MenuItem("item_2", "Item 2"));
            _provider.ShowMenu(definition);

            // Act
            _provider.MoveSelectionDown();

            // Assert
            Assert.Equal(1, _provider.SelectedIndex);
        }

        [Fact]
        public void MoveSelectionUp_ShouldDecrementSelectedIndex()
        {
            // Arrange
            var definition = new MenuDefinition("test_menu", "Test Menu");
            definition.AddItem(new MenuItem("item_1", "Item 1"));
            definition.AddItem(new MenuItem("item_2", "Item 2"));
            _provider.ShowMenu(definition);
            _provider.MoveSelectionDown(); // Now at index 1

            // Act
            _provider.MoveSelectionUp();

            // Assert
            Assert.Equal(0, _provider.SelectedIndex);
        }

        [Fact]
        public void MoveSelectionDown_AtLastItem_ShouldWrapToFirst()
        {
            // Arrange
            var definition = new MenuDefinition("test_menu", "Test Menu");
            definition.AddItem(new MenuItem("item_1", "Item 1"));
            definition.AddItem(new MenuItem("item_2", "Item 2"));
            _provider.ShowMenu(definition);
            _provider.MoveSelectionDown(); // Now at index 1 (last item)

            // Act
            _provider.MoveSelectionDown();

            // Assert
            Assert.Equal(0, _provider.SelectedIndex);
        }

        [Fact]
        public void MoveSelectionUp_AtFirstItem_ShouldWrapToLast()
        {
            // Arrange
            var definition = new MenuDefinition("test_menu", "Test Menu");
            definition.AddItem(new MenuItem("item_1", "Item 1"));
            definition.AddItem(new MenuItem("item_2", "Item 2"));
            _provider.ShowMenu(definition);

            // Act
            _provider.MoveSelectionUp();

            // Assert
            Assert.Equal(1, _provider.SelectedIndex);
        }

        [Fact]
        public void SelectCurrentItem_ShouldRaiseItemSelectedEventForCurrentSelection()
        {
            // Arrange
            var definition = new MenuDefinition("test_menu", "Test Menu");
            definition.AddItem(new MenuItem("item_1", "Item 1"));
            definition.AddItem(new MenuItem("item_2", "Item 2"));
            _provider.ShowMenu(definition);
            _provider.MoveSelectionDown(); // Select item_2

            MenuItemSelectedEventArgs? receivedArgs = null;
            _provider.ItemSelected += (sender, args) => receivedArgs = args;

            // Act
            _provider.SelectCurrentItem();

            // Assert
            Assert.NotNull(receivedArgs);
            Assert.Equal("item_2", receivedArgs!.ItemId);
        }

        [Fact]
        public void MoveSelectionDown_WhenNoMenuOpen_ShouldNotThrow()
        {
            // Act & Assert
            var exception = Record.Exception(() => _provider.MoveSelectionDown());
            Assert.Null(exception);
        }

        [Fact]
        public void MoveSelectionUp_WhenNoMenuOpen_ShouldNotThrow()
        {
            // Act & Assert
            var exception = Record.Exception(() => _provider.MoveSelectionUp());
            Assert.Null(exception);
        }

        [Fact]
        public void SelectCurrentItem_WhenNoMenuOpen_ShouldNotRaiseEvent()
        {
            // Arrange
            MenuItemSelectedEventArgs? receivedArgs = null;
            _provider.ItemSelected += (sender, args) => receivedArgs = args;

            // Act
            _provider.SelectCurrentItem();

            // Assert
            Assert.Null(receivedArgs);
        }

        [Fact]
        public void MoveSelectionDown_ShouldSkipDisabledItems()
        {
            // Arrange
            var definition = new MenuDefinition("test_menu", "Test Menu");
            definition.AddItem(new MenuItem("item_1", "Item 1"));
            var disabledItem = new MenuItem("item_2", "Item 2");
            disabledItem.IsEnabled = false;
            definition.AddItem(disabledItem);
            definition.AddItem(new MenuItem("item_3", "Item 3"));
            _provider.ShowMenu(definition);

            // Act
            _provider.MoveSelectionDown();

            // Assert - should skip item_2 (disabled) and land on item_3
            Assert.Equal(2, _provider.SelectedIndex);
        }

        [Fact]
        public void MoveSelectionUp_ShouldSkipDisabledItems()
        {
            // Arrange
            var definition = new MenuDefinition("test_menu", "Test Menu");
            definition.AddItem(new MenuItem("item_1", "Item 1"));
            var disabledItem = new MenuItem("item_2", "Item 2");
            disabledItem.IsEnabled = false;
            definition.AddItem(disabledItem);
            definition.AddItem(new MenuItem("item_3", "Item 3"));
            _provider.ShowMenu(definition);
            // Move to item_3
            _provider.MoveSelectionDown();

            // Act
            _provider.MoveSelectionUp();

            // Assert - should skip item_2 (disabled) and land on item_1
            Assert.Equal(0, _provider.SelectedIndex);
        }

        #endregion
    }
}
