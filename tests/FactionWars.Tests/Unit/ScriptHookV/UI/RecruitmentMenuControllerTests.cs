using FactionWars.Core.Interfaces;
using FactionWars.Core.Utils;
using FactionWars.ScriptHookV.UI;
using FactionWars.Tests.Mocks;
using System;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV.UI
{
    public class RecruitmentMenuControllerTests
    {
        private readonly MockMenuProvider _menuProvider;
        private readonly MockGameBridge _gameBridge;
        private readonly RecruitmentMenuController _controller;

        public RecruitmentMenuControllerTests()
        {
            _menuProvider = new MockMenuProvider();
            _gameBridge = new MockGameBridge();

            _controller = new RecruitmentMenuController(_menuProvider, _gameBridge);
        }

        [Fact]
        public void Constructor_WithNullMenuProvider_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new RecruitmentMenuController(null!, _gameBridge));
        }

        [Fact]
        public void Constructor_WithNullGameBridge_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new RecruitmentMenuController(_menuProvider, null!));
        }

        [Fact]
        public void Show_ShouldDisplayRecruitmentMenu()
        {
            // Act
            _controller.Show();

            // Assert
            Assert.True(_menuProvider.IsMenuVisible);
            Assert.Equal(RecruitmentMenuController.MenuId, _menuProvider.CurrentMenuId);
        }

        [Fact]
        public void Show_MenuShouldHaveCorrectTitle()
        {
            // Act
            _controller.Show();

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            Assert.NotNull(menu);
            Assert.Equal("Recruitment", menu!.Title);
        }

        [Fact]
        public void Show_ShouldIncludeCashDisplay()
        {
            // Act
            _controller.Show();

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            Assert.NotNull(menu);
            Assert.NotNull(menu!.GetItem(RecruitmentMenuController.CashDisplayItemId));
        }

        [Fact]
        public void Show_DoesNotIncludeDefendersOption()
        {
            // Act
            _controller.Show();

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            Assert.NotNull(menu);
            Assert.DoesNotContain(menu!.Items, i => i.Id == "defenders");
        }

        [Fact]
        public void Show_ShouldIncludeSquadOption()
        {
            // Act
            _controller.Show();

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            Assert.NotNull(menu);
            var squadItem = menu!.GetItem(RecruitmentMenuController.SquadItemId);
            Assert.NotNull(squadItem);
            Assert.Equal("Squad", squadItem!.Text);
        }

        [Fact]
        public void Show_ShouldIncludeBackButton()
        {
            // Act
            _controller.Show();

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            Assert.NotNull(menu);
            Assert.NotNull(menu!.GetItem(RecruitmentMenuController.BackItemId));
        }

        [Fact]
        public void Show_ShouldHaveThreeItems()
        {
            // Act
            _controller.Show();

            // Assert - cash display, squad, back = 3
            var menu = _menuProvider.GetCurrentMenuDefinition();
            Assert.NotNull(menu);
            Assert.Equal(3, menu!.Items.Count);
        }

        [Fact]
        public void SelectSquad_ShouldRaiseSquadRequestedEvent()
        {
            // Arrange
            var eventRaised = false;
            _controller.SquadRequested += (s, e) => eventRaised = true;
            _controller.Show();

            // Act
            _menuProvider.SimulateItemSelection(RecruitmentMenuController.SquadItemId);

            // Assert
            Assert.True(eventRaised);
        }

        [Fact]
        public void Back_ShouldRaiseBackRequestedEvent()
        {
            // Arrange
            var eventRaised = false;
            _controller.BackRequested += (s, e) => eventRaised = true;
            _controller.Show();

            // Act
            _menuProvider.SimulateItemSelection(RecruitmentMenuController.BackItemId);

            // Assert
            Assert.True(eventRaised);
        }

        [Fact]
        public void Back_ShouldCloseMenu()
        {
            // Arrange
            _controller.Show();

            // Act
            _menuProvider.SimulateItemSelection(RecruitmentMenuController.BackItemId);

            // Assert
            Assert.False(_menuProvider.IsMenuVisible);
        }
    }
}
