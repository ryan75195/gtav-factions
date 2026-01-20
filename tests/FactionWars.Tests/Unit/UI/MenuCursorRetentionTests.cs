using FactionWars.Tests.Mocks;
using FactionWars.UI.Models;
using Xunit;

namespace FactionWars.Tests.Unit.UI
{
    /// <summary>
    /// Tests for menu cursor retention feature that allows restoring cursor position after menu refresh.
    /// Uses MockMenuProvider since NativeUI requires the GTA V game environment.
    /// </summary>
    public class MenuCursorRetentionTests
    {
        [Fact]
        public void ShowMenu_WithSelectedItemId_SelectsThatItem()
        {
            // Arrange
            var provider = new MockMenuProvider();
            var menu = new MenuDefinition("test-menu", "Test Menu");
            menu.AddItem(new MenuItem("item-1", "First Item"));
            menu.AddItem(new MenuItem("item-2", "Second Item"));
            menu.AddItem(new MenuItem("item-3", "Third Item"));

            // Act
            provider.ShowMenu(menu, selectedItemId: "item-2");

            // Assert
            Assert.Equal(1, provider.SelectedIndex); // 0-indexed, item-2 is at index 1
        }

        [Fact]
        public void ShowMenu_WithInvalidSelectedItemId_SelectsFirstItem()
        {
            // Arrange
            var provider = new MockMenuProvider();
            var menu = new MenuDefinition("test-menu", "Test Menu");
            menu.AddItem(new MenuItem("item-1", "First Item"));
            menu.AddItem(new MenuItem("item-2", "Second Item"));

            // Act
            provider.ShowMenu(menu, selectedItemId: "invalid-id");

            // Assert
            Assert.Equal(0, provider.SelectedIndex);
        }

        [Fact]
        public void ShowMenu_WithNullSelectedItemId_SelectsFirstItem()
        {
            // Arrange
            var provider = new MockMenuProvider();
            var menu = new MenuDefinition("test-menu", "Test Menu");
            menu.AddItem(new MenuItem("item-1", "First Item"));
            menu.AddItem(new MenuItem("item-2", "Second Item"));

            // Act
            provider.ShowMenu(menu, selectedItemId: null);

            // Assert
            Assert.Equal(0, provider.SelectedIndex);
        }

        [Fact]
        public void ShowMenu_WithSelectedItemId_SelectsLastItem()
        {
            // Arrange
            var provider = new MockMenuProvider();
            var menu = new MenuDefinition("test-menu", "Test Menu");
            menu.AddItem(new MenuItem("item-1", "First Item"));
            menu.AddItem(new MenuItem("item-2", "Second Item"));
            menu.AddItem(new MenuItem("item-3", "Third Item"));
            menu.AddItem(new MenuItem("item-4", "Fourth Item"));

            // Act
            provider.ShowMenu(menu, selectedItemId: "item-4");

            // Assert
            Assert.Equal(3, provider.SelectedIndex); // 0-indexed, item-4 is at index 3
        }

        [Fact]
        public void ShowMenu_WithSelectedItemId_SelectsFirstItemWhenMatched()
        {
            // Arrange
            var provider = new MockMenuProvider();
            var menu = new MenuDefinition("test-menu", "Test Menu");
            menu.AddItem(new MenuItem("item-1", "First Item"));
            menu.AddItem(new MenuItem("item-2", "Second Item"));

            // Act
            provider.ShowMenu(menu, selectedItemId: "item-1");

            // Assert
            Assert.Equal(0, provider.SelectedIndex);
        }

        [Fact]
        public void ShowMenu_WithEmptySelectedItemId_SelectsFirstItem()
        {
            // Arrange
            var provider = new MockMenuProvider();
            var menu = new MenuDefinition("test-menu", "Test Menu");
            menu.AddItem(new MenuItem("item-1", "First Item"));
            menu.AddItem(new MenuItem("item-2", "Second Item"));

            // Act
            provider.ShowMenu(menu, selectedItemId: "");

            // Assert
            Assert.Equal(0, provider.SelectedIndex);
        }

        [Fact]
        public void ShowMenu_WithoutSelectedItemId_DefaultsToFirstItem()
        {
            // Arrange
            var provider = new MockMenuProvider();
            var menu = new MenuDefinition("test-menu", "Test Menu");
            menu.AddItem(new MenuItem("item-1", "First Item"));
            menu.AddItem(new MenuItem("item-2", "Second Item"));

            // Act
            provider.ShowMenu(menu); // No selectedItemId parameter

            // Assert
            Assert.Equal(0, provider.SelectedIndex);
        }

        [Fact]
        public void ShowMenu_RefreshWithCursorRetention_MaintainsPosition()
        {
            // Arrange - Simulates a menu refresh after an action
            var provider = new MockMenuProvider();
            var menu1 = new MenuDefinition("army-menu", "Army Menu");
            menu1.AddItem(new MenuItem("purchase-soldier", "Purchase Soldier"));
            menu1.AddItem(new MenuItem("allocate-troops", "Allocate Troops"));
            menu1.AddItem(new MenuItem("back", "Back"));

            // Show initial menu (cursor starts at first item)
            provider.ShowMenu(menu1);
            Assert.Equal(0, provider.SelectedIndex);

            // Act - Refresh menu after action (e.g., purchase completed), maintaining cursor on "allocate-troops"
            var menu2 = new MenuDefinition("army-menu", "Army Menu");
            menu2.AddItem(new MenuItem("purchase-soldier", "Purchase Soldier ($500)"));
            menu2.AddItem(new MenuItem("allocate-troops", "Allocate Troops"));
            menu2.AddItem(new MenuItem("back", "Back"));

            provider.ShowMenu(menu2, selectedItemId: "allocate-troops");

            // Assert - Cursor should be on the "allocate-troops" item
            Assert.Equal(1, provider.SelectedIndex);
        }

        [Fact]
        public void SelectedIndex_WhenNoMenuOpen_ReturnsNegativeOne()
        {
            // Arrange
            var provider = new MockMenuProvider();

            // Assert
            Assert.Equal(-1, provider.SelectedIndex);
        }

        [Fact]
        public void SelectedIndex_AfterCloseMenu_ReturnsNegativeOne()
        {
            // Arrange
            var provider = new MockMenuProvider();
            var menu = new MenuDefinition("test-menu", "Test Menu");
            menu.AddItem(new MenuItem("item-1", "First Item"));
            provider.ShowMenu(menu, selectedItemId: "item-1");
            Assert.Equal(0, provider.SelectedIndex);

            // Act
            provider.CloseMenu();

            // Assert
            Assert.Equal(-1, provider.SelectedIndex);
        }
    }
}
