using FactionWars.UI.Interfaces;
using FactionWars.UI.Models;
using System;
using Xunit;

namespace FactionWars.Tests.Unit.UI
{
    /// <summary>
    /// Tests for menu model classes (MenuItem, MenuDefinition).
    /// </summary>
    public class MenuModelTests
    {
        #region MenuItem Tests

        [Fact]
        public void MenuItem_Constructor_ShouldSetProperties()
        {
            // Act
            var item = new MenuItem("test_id", "Test Text", "Test Description");

            // Assert
            Assert.Equal("test_id", item.Id);
            Assert.Equal("Test Text", item.Text);
            Assert.Equal("Test Description", item.Description);
            Assert.True(item.IsEnabled);
        }

        [Fact]
        public void MenuItem_Constructor_ShouldDefaultDescriptionToEmpty()
        {
            // Act
            var item = new MenuItem("test_id", "Test Text");

            // Assert
            Assert.Equal(string.Empty, item.Description);
        }

        [Fact]
        public void MenuItem_Constructor_ShouldDefaultEnabledToTrue()
        {
            // Act
            var item = new MenuItem("test_id", "Test Text");

            // Assert
            Assert.True(item.IsEnabled);
        }

        [Fact]
        public void MenuItem_Constructor_ShouldThrowForNullId()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new MenuItem(null!, "Test Text"));
        }

        [Fact]
        public void MenuItem_Constructor_ShouldThrowForEmptyId()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => new MenuItem("", "Test Text"));
        }

        [Fact]
        public void MenuItem_Constructor_ShouldThrowForWhitespaceId()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => new MenuItem("   ", "Test Text"));
        }

        [Fact]
        public void MenuItem_Constructor_ShouldThrowForNullText()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new MenuItem("test_id", null!));
        }

        [Fact]
        public void MenuItem_Constructor_ShouldThrowForEmptyText()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => new MenuItem("test_id", ""));
        }

        [Fact]
        public void MenuItem_Constructor_ShouldThrowForWhitespaceText()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => new MenuItem("test_id", "   "));
        }

        [Fact]
        public void MenuItem_IsEnabled_ShouldBeSettable()
        {
            // Arrange
            var item = new MenuItem("test_id", "Test Text");

            // Act
            item.IsEnabled = false;

            // Assert
            Assert.False(item.IsEnabled);
        }

        #endregion

        #region MenuDefinition Tests

        [Fact]
        public void MenuDefinition_Constructor_ShouldSetProperties()
        {
            // Act
            var menu = new MenuDefinition("menu_id", "Menu Title", "Menu Subtitle");

            // Assert
            Assert.Equal("menu_id", menu.Id);
            Assert.Equal("Menu Title", menu.Title);
            Assert.Equal("Menu Subtitle", menu.Subtitle);
            Assert.Empty(menu.Items);
        }

        [Fact]
        public void MenuDefinition_Constructor_ShouldDefaultSubtitleToEmpty()
        {
            // Act
            var menu = new MenuDefinition("menu_id", "Menu Title");

            // Assert
            Assert.Equal(string.Empty, menu.Subtitle);
        }

        [Fact]
        public void MenuDefinition_Constructor_ShouldThrowForNullId()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new MenuDefinition(null!, "Menu Title"));
        }

        [Fact]
        public void MenuDefinition_Constructor_ShouldThrowForEmptyId()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => new MenuDefinition("", "Menu Title"));
        }

        [Fact]
        public void MenuDefinition_Constructor_ShouldThrowForWhitespaceId()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => new MenuDefinition("   ", "Menu Title"));
        }

        [Fact]
        public void MenuDefinition_Constructor_ShouldThrowForNullTitle()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new MenuDefinition("menu_id", null!));
        }

        [Fact]
        public void MenuDefinition_Constructor_ShouldThrowForEmptyTitle()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => new MenuDefinition("menu_id", ""));
        }

        [Fact]
        public void MenuDefinition_Constructor_ShouldThrowForWhitespaceTitle()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => new MenuDefinition("menu_id", "   "));
        }

        [Fact]
        public void MenuDefinition_AddItem_ShouldAddItemToList()
        {
            // Arrange
            var menu = new MenuDefinition("menu_id", "Menu Title");
            var item = new MenuItem("item_id", "Item Text");

            // Act
            menu.AddItem(item);

            // Assert
            Assert.Single(menu.Items);
            Assert.Same(item, menu.Items[0]);
        }

        [Fact]
        public void MenuDefinition_AddItem_ShouldThrowForNullItem()
        {
            // Arrange
            var menu = new MenuDefinition("menu_id", "Menu Title");

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => menu.AddItem(null!));
        }

        [Fact]
        public void MenuDefinition_AddItem_ShouldAllowMultipleItems()
        {
            // Arrange
            var menu = new MenuDefinition("menu_id", "Menu Title");
            var item1 = new MenuItem("item_1", "Item 1");
            var item2 = new MenuItem("item_2", "Item 2");
            var item3 = new MenuItem("item_3", "Item 3");

            // Act
            menu.AddItem(item1);
            menu.AddItem(item2);
            menu.AddItem(item3);

            // Assert
            Assert.Equal(3, menu.Items.Count);
            Assert.Same(item1, menu.Items[0]);
            Assert.Same(item2, menu.Items[1]);
            Assert.Same(item3, menu.Items[2]);
        }

        [Fact]
        public void MenuDefinition_ClearItems_ShouldRemoveAllItems()
        {
            // Arrange
            var menu = new MenuDefinition("menu_id", "Menu Title");
            menu.AddItem(new MenuItem("item_1", "Item 1"));
            menu.AddItem(new MenuItem("item_2", "Item 2"));

            // Act
            menu.ClearItems();

            // Assert
            Assert.Empty(menu.Items);
        }

        [Fact]
        public void MenuDefinition_GetItem_ShouldReturnItemById()
        {
            // Arrange
            var menu = new MenuDefinition("menu_id", "Menu Title");
            var item1 = new MenuItem("item_1", "Item 1");
            var item2 = new MenuItem("item_2", "Item 2");
            menu.AddItem(item1);
            menu.AddItem(item2);

            // Act
            var result = menu.GetItem("item_2");

            // Assert
            Assert.Same(item2, result);
        }

        [Fact]
        public void MenuDefinition_GetItem_ShouldReturnNullForUnknownId()
        {
            // Arrange
            var menu = new MenuDefinition("menu_id", "Menu Title");
            menu.AddItem(new MenuItem("item_1", "Item 1"));

            // Act
            var result = menu.GetItem("unknown");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void MenuDefinition_GetItem_ShouldReturnNullForNullId()
        {
            // Arrange
            var menu = new MenuDefinition("menu_id", "Menu Title");
            menu.AddItem(new MenuItem("item_1", "Item 1"));

            // Act
            var result = menu.GetItem(null!);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void MenuDefinition_GetItem_ShouldReturnNullForEmptyId()
        {
            // Arrange
            var menu = new MenuDefinition("menu_id", "Menu Title");
            menu.AddItem(new MenuItem("item_1", "Item 1"));

            // Act
            var result = menu.GetItem("");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void MenuDefinition_Items_ShouldBeReadOnly()
        {
            // Arrange
            var menu = new MenuDefinition("menu_id", "Menu Title");

            // Assert - Items property returns a read-only collection
            Assert.IsAssignableFrom<System.Collections.Generic.IReadOnlyList<MenuItem>>(menu.Items);
        }

        #endregion

        #region MenuItemSelectedEventArgs Tests

        [Fact]
        public void MenuItemSelectedEventArgs_Constructor_ShouldSetProperties()
        {
            // Act
            var args = new MenuItemSelectedEventArgs("menu_id", "item_id");

            // Assert
            Assert.Equal("menu_id", args.MenuId);
            Assert.Equal("item_id", args.ItemId);
        }

        [Fact]
        public void MenuItemSelectedEventArgs_Constructor_ShouldHandleNullMenuId()
        {
            // Act
            var args = new MenuItemSelectedEventArgs(null!, "item_id");

            // Assert
            Assert.Equal(string.Empty, args.MenuId);
        }

        [Fact]
        public void MenuItemSelectedEventArgs_Constructor_ShouldHandleNullItemId()
        {
            // Act
            var args = new MenuItemSelectedEventArgs("menu_id", null!);

            // Assert
            Assert.Equal(string.Empty, args.ItemId);
        }

        #endregion
    }
}
