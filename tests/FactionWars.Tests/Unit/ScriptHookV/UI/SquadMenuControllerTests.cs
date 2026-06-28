using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.Economy.Interfaces;
using FactionWars.Economy.Models;
using FactionWars.Factions.Interfaces;
using FactionWars.ScriptHookV.Managers;
using FactionWars.ScriptHookV.Models;
using FactionWars.ScriptHookV.UI;
using FactionWars.Tests.Mocks;
using Moq;
using System;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV.UI
{
    public class SquadMenuControllerTests
    {
        private readonly MockMenuProvider _menuProvider;
        private readonly Mock<ITroopPurchaseService> _purchaseServiceMock;
        private readonly Mock<IFollowerService> _followerServiceMock;
        private readonly Mock<IPlayerContext> _playerContextMock;
        private readonly SquadMenuController _controller;

        private const string PlayerFactionId = "michael";

        public SquadMenuControllerTests()
        {
            _menuProvider = new MockMenuProvider();
            _purchaseServiceMock = new Mock<ITroopPurchaseService>();
            _followerServiceMock = new Mock<IFollowerService>();
            _playerContextMock = new Mock<IPlayerContext>();

            _playerContextMock.Setup(p => p.CurrentFactionId).Returns(PlayerFactionId);

            _purchaseServiceMock.Setup(p => p.GetPlayerMoney()).Returns(25000);
            _purchaseServiceMock.Setup(p => p.GetTroopCost(DefenderRole.Grunt)).Returns(200);
            _purchaseServiceMock.Setup(p => p.GetTroopCost(DefenderRole.Gunner)).Returns(500);
            _purchaseServiceMock.Setup(p => p.GetTroopCost(DefenderRole.Rifleman)).Returns(1000);
            _purchaseServiceMock.Setup(p => p.GetTroopCost(DefenderRole.Rocketeer)).Returns(2000);
            _purchaseServiceMock.Setup(p => p.GetTroopCost(DefenderRole.Sniper)).Returns(1500);

            _followerServiceMock.Setup(f => f.GetFollowerCount(PlayerFactionId)).Returns(0);
            _followerServiceMock.Setup(f => f.GetMaxFollowers()).Returns(6);
            _followerServiceMock.Setup(f => f.GetFollowers(PlayerFactionId)).Returns([]);

            _controller = new SquadMenuController(
                _menuProvider,
                _purchaseServiceMock.Object,
                _followerServiceMock.Object,
                _playerContextMock.Object);
        }

        [Fact]
        public void Constructor_WithNullMenuProvider_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new SquadMenuController(
                null!,
                _purchaseServiceMock.Object,
                _followerServiceMock.Object,
                _playerContextMock.Object));
        }

        [Fact]
        public void Constructor_WithNullPurchaseService_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new SquadMenuController(
                _menuProvider,
                null!,
                _followerServiceMock.Object,
                _playerContextMock.Object));
        }

        [Fact]
        public void Constructor_WithNullFollowerService_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new SquadMenuController(
                _menuProvider,
                _purchaseServiceMock.Object,
                null!,
                _playerContextMock.Object));
        }

        [Fact]
        public void Constructor_WithNullPlayerContext_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new SquadMenuController(
                _menuProvider,
                _purchaseServiceMock.Object,
                _followerServiceMock.Object,
                null!));
        }

        [Fact]
        public void Show_ShouldDisplaySquadMenu()
        {
            // Act
            _controller.Show();

            // Assert
            Assert.True(_menuProvider.IsMenuVisible);
            Assert.Equal(SquadMenuController.MenuId, _menuProvider.CurrentMenuId);
        }

        [Fact]
        public void Show_MenuShouldHaveCorrectTitle()
        {
            // Act
            _controller.Show();

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            Assert.NotNull(menu);
            Assert.Equal("Squad", menu!.Title);
        }

        [Fact]
        public void Show_ShouldIncludeAllFourTierRecruitOptions()
        {
            // Arrange
            _purchaseServiceMock.Setup(p => p.CanAfford(It.IsAny<DefenderRole>(), 1)).Returns(true);

            // Act
            _controller.Show();

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            Assert.NotNull(menu);

            Assert.NotNull(menu!.GetItem(SquadMenuController.RecruitBasicItemId));
            Assert.NotNull(menu.GetItem(SquadMenuController.RecruitMediumItemId));
            Assert.NotNull(menu.GetItem(SquadMenuController.RecruitHeavyItemId));
            Assert.NotNull(menu.GetItem(SquadMenuController.RecruitEliteItemId));
        }

        [Fact]
        public void Show_ShouldIncludeSniperRecruitOption()
        {
            // Arrange
            _purchaseServiceMock.Setup(p => p.CanAfford(It.IsAny<DefenderRole>(), 1)).Returns(true);

            // Act
            _controller.Show();

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            Assert.NotNull(menu);
            var sniperItem = menu!.GetItem(SquadMenuController.RecruitSniperItemId);
            Assert.NotNull(sniperItem);
            Assert.Contains("Sniper", sniperItem!.Text);
        }

        [Fact]
        public void SniperRecruitItem_ShouldShowCorrectCost()
        {
            // Act
            _controller.Show();

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            var sniperItem = menu?.GetItem(SquadMenuController.RecruitSniperItemId);
            Assert.NotNull(sniperItem);
            Assert.Contains("1,500", sniperItem!.Text);
        }

        [Fact]
        public void Show_ShouldIncludeFollowerSummary()
        {
            // Arrange
            _followerServiceMock.Setup(f => f.GetFollowerCount(PlayerFactionId)).Returns(2);
            _followerServiceMock.Setup(f => f.GetMaxFollowers()).Returns(6);

            // Act
            _controller.Show();

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            Assert.NotNull(menu);
            var summaryItem = menu!.GetItem(SquadMenuController.FollowerSummaryItemId);
            Assert.NotNull(summaryItem);
            Assert.Contains("2", summaryItem!.Text);
            Assert.Contains("6", summaryItem.Text);
        }

        [Fact]
        public void Show_ShouldIncludeManageSquadOption()
        {
            // Act
            _controller.Show();

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            Assert.NotNull(menu);
            Assert.NotNull(menu!.GetItem(SquadMenuController.ManageFollowersItemId));
        }

        [Fact]
        public void Show_ShouldIncludeBackButton()
        {
            // Act
            _controller.Show();

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            Assert.NotNull(menu);
            Assert.NotNull(menu!.GetItem(SquadMenuController.BackItemId));
        }

        [Fact]
        public void Show_ShouldHaveNineItems()
        {
            // Act
            _controller.Show();

            // Assert - money display, follower summary, 5 recruit options, manage, back = 9
            var menu = _menuProvider.GetCurrentMenuDefinition();
            Assert.NotNull(menu);
            Assert.Equal(9, menu!.Items.Count);
        }

        [Fact]
        public void ManageFollowers_ShouldShowFollowerListMenu()
        {
            // Arrange
            _controller.Show();

            // Act
            _menuProvider.SimulateItemSelection(SquadMenuController.ManageFollowersItemId);

            // Assert
            Assert.Equal(SquadMenuController.FollowerListMenuId, _menuProvider.CurrentMenuId);
        }

        [Fact]
        public void Back_ShouldRaiseBackRequestedEvent()
        {
            // Arrange
            var eventRaised = false;
            _controller.BackRequested += (s, e) => eventRaised = true;
            _controller.Show();

            // Act
            _menuProvider.SimulateItemSelection(SquadMenuController.BackItemId);

            // Assert
            Assert.True(eventRaised);
        }

        [Fact]
        public void Back_ShouldCloseMenu()
        {
            // Arrange
            _controller.Show();

            // Act
            _menuProvider.SimulateItemSelection(SquadMenuController.BackItemId);

            // Assert
            Assert.False(_menuProvider.IsMenuVisible);
        }

        [Fact]
        public void FollowerListBack_ShouldReturnToSquadMenu()
        {
            // Arrange
            _controller.Show();
            _menuProvider.SimulateItemSelection(SquadMenuController.ManageFollowersItemId);

            // Act
            _menuProvider.SimulateItemSelection(SquadMenuController.FollowerListBackItemId);

            // Assert
            Assert.Equal(SquadMenuController.MenuId, _menuProvider.CurrentMenuId);
        }

        [Fact]
        public void DismissFollower_WithFollowerManager_ShouldUseWorldCleanupPath()
        {
            // Arrange
            var menuProvider = new MockMenuProvider();
            var follower = new Follower(PlayerFactionId, DefenderRole.Grunt, pedHandle: 42);
            var followerManagerMock = new Mock<IFollowerManager>();
            var controller = new SquadMenuController(
                new SquadMenuControllerDependencies
                {
                    MenuProvider = menuProvider,
                    PurchaseService = _purchaseServiceMock.Object,
                    FollowerService = _followerServiceMock.Object,
                    PlayerContext = _playerContextMock.Object
                },
                followerManagerMock.Object);

            _followerServiceMock.Setup(f => f.GetFollowers(PlayerFactionId)).Returns([follower]);
            _followerServiceMock.Setup(f => f.GetFollowerById(follower.Id)).Returns(follower);
            followerManagerMock.Setup(f => f.DismissFollower(follower.Id)).Returns(true);

            controller.Show();
            menuProvider.SimulateItemSelection(SquadMenuController.ManageFollowersItemId);
            menuProvider.SimulateItemSelection($"follower_{follower.Id}");

            // Act
            menuProvider.SimulateItemSelection(SquadMenuController.DismissFollowerItemId);

            // Assert
            followerManagerMock.Verify(f => f.DismissFollower(follower.Id), Times.Once);
            _followerServiceMock.Verify(f => f.DismissFollower(follower.Id), Times.Never);
        }

        [Fact]
        public void EliteRecruitItem_ShouldShowCorrectCost()
        {
            // Act
            _controller.Show();

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            var eliteItem = menu?.GetItem(SquadMenuController.RecruitEliteItemId);
            Assert.NotNull(eliteItem);
            Assert.Contains("2,000", eliteItem!.Text);
        }

        [Fact]
        public void RecruitOptions_ShouldBeDisabledWhenAtMaxFollowers()
        {
            // Arrange
            _followerServiceMock.Setup(f => f.GetFollowerCount(PlayerFactionId)).Returns(6);
            _followerServiceMock.Setup(f => f.GetMaxFollowers()).Returns(6);

            // Act
            _controller.Show();

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            var recruitEliteItem = menu?.GetItem(SquadMenuController.RecruitEliteItemId);
            Assert.False(recruitEliteItem!.IsEnabled);
        }
    }
}
