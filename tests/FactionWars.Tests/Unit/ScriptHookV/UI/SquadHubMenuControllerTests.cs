using FactionWars.Core.Utils;
using FactionWars.ScriptHookV.UI;
using FactionWars.Tests.Mocks;
using System;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV.UI
{
    public class SquadHubMenuControllerTests
    {
        private readonly MockMenuProvider _menuProvider;
        private readonly MockGameBridge _gameBridge;
        private readonly SquadHubMenuController _controller;

        public SquadHubMenuControllerTests()
        {
            _menuProvider = new MockMenuProvider();
            _gameBridge = new MockGameBridge();

            _controller = new SquadHubMenuController(_menuProvider, _gameBridge);
        }

        [Fact]
        public void Constructor_WithNullMenuProvider_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new SquadHubMenuController(null!, _gameBridge));
        }

        [Fact]
        public void Constructor_WithNullGameBridge_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new SquadHubMenuController(_menuProvider, null!));
        }

        [Fact]
        public void Show_ShouldDisplaySquadHubMenu()
        {
            _controller.Show();

            Assert.True(_menuProvider.IsMenuVisible);
            Assert.Equal(SquadHubMenuController.MenuId, _menuProvider.CurrentMenuId);
        }

        [Fact]
        public void Show_ShouldIncludeManageSquadItem()
        {
            _controller.Show();

            var menu = _menuProvider.GetCurrentMenuDefinition();
            Assert.NotNull(menu);
            Assert.NotNull(menu!.GetItem(SquadHubMenuController.ManageSquadItemId));
        }

        [Fact]
        public void Show_ShouldIncludeSupportItem()
        {
            _controller.Show();

            var menu = _menuProvider.GetCurrentMenuDefinition();
            Assert.NotNull(menu);
            Assert.NotNull(menu!.GetItem(SquadHubMenuController.SupportItemId));
        }

        [Fact]
        public void Show_ShouldIncludeBackItem()
        {
            _controller.Show();

            var menu = _menuProvider.GetCurrentMenuDefinition();
            Assert.NotNull(menu);
            Assert.NotNull(menu!.GetItem(SquadHubMenuController.BackItemId));
        }

        [Fact]
        public void Show_ShouldHaveExactlyThreeItems()
        {
            _controller.Show();

            var menu = _menuProvider.GetCurrentMenuDefinition();
            Assert.NotNull(menu);
            Assert.Equal(3, menu!.Items.Count);
        }

        [Fact]
        public void SelectManageSquad_ShouldRaiseManageSquadRequestedEvent()
        {
            var eventRaised = false;
            _controller.ManageSquadRequested += (s, e) => eventRaised = true;
            _controller.Show();

            _menuProvider.SimulateItemSelection(SquadHubMenuController.ManageSquadItemId);

            Assert.True(eventRaised);
        }

        [Fact]
        public void SelectSupport_ShouldRaiseSupportRequestedEvent()
        {
            var eventRaised = false;
            _controller.SupportRequested += (s, e) => eventRaised = true;
            _controller.Show();

            _menuProvider.SimulateItemSelection(SquadHubMenuController.SupportItemId);

            Assert.True(eventRaised);
        }

        [Fact]
        public void Back_ShouldRaiseBackRequestedEvent()
        {
            var eventRaised = false;
            _controller.BackRequested += (s, e) => eventRaised = true;
            _controller.Show();

            _menuProvider.SimulateItemSelection(SquadHubMenuController.BackItemId);

            Assert.True(eventRaised);
        }

        [Fact]
        public void Back_ShouldCloseMenu()
        {
            _controller.Show();

            _menuProvider.SimulateItemSelection(SquadHubMenuController.BackItemId);

            Assert.False(_menuProvider.IsMenuVisible);
        }
    }
}
