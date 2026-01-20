using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.Economy.Interfaces;
using FactionWars.Economy.Models;
using FactionWars.Factions.Interfaces;
using FactionWars.Factions.Models;
using FactionWars.ScriptHookV.UI;
using FactionWars.UI.Models;
using Moq;
using System;
using System.Linq;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV.UI
{
    /// <summary>
    /// Tests for ArmyMenuController handling troop purchasing, reserve viewing, and follower management.
    /// </summary>
    public class ArmyMenuControllerTests
    {
        private readonly NativeUIMenuProvider _menuProvider;
        private readonly Mock<IFactionService> _factionServiceMock;
        private readonly Mock<ITroopPurchaseService> _purchaseServiceMock;
        private readonly Mock<IFollowerService> _followerServiceMock;
        private readonly Mock<IDefenderTierService> _tierServiceMock;
        private readonly Mock<IPlayerContext> _playerContextMock;
        private readonly ArmyMenuController _controller;

        private const string PlayerFactionId = "michael";
        private const string PlayerFactionName = "De Santa Enterprises";
        private const int PlayerMoney = 25000;

        public ArmyMenuControllerTests()
        {
            _menuProvider = new NativeUIMenuProvider();
            _factionServiceMock = new Mock<IFactionService>();
            _purchaseServiceMock = new Mock<ITroopPurchaseService>();
            _followerServiceMock = new Mock<IFollowerService>();
            _tierServiceMock = new Mock<IDefenderTierService>();
            _playerContextMock = new Mock<IPlayerContext>();

            // Setup default player faction
            _playerContextMock.Setup(p => p.CurrentFactionId).Returns(PlayerFactionId);

            // Setup default faction
            var faction = new Faction(PlayerFactionId, PlayerFactionName, "Michael De Santa", "Corporate empire", new FactionColor(0, 0, 255));
            _factionServiceMock.Setup(f => f.GetFaction(PlayerFactionId)).Returns(faction);

            // Setup default faction state with reserves
            var factionState = new FactionState(PlayerFactionId, 10000, 50);
            factionState.AddReserveTroops(DefenderTier.Basic, 20);
            factionState.AddReserveTroops(DefenderTier.Medium, 15);
            factionState.AddReserveTroops(DefenderTier.Heavy, 10);
            _factionServiceMock.Setup(f => f.GetFactionState(PlayerFactionId)).Returns(factionState);

            // Setup tier costs
            _tierServiceMock.Setup(t => t.GetCost(DefenderTier.Basic)).Returns(200);
            _tierServiceMock.Setup(t => t.GetCost(DefenderTier.Medium)).Returns(500);
            _tierServiceMock.Setup(t => t.GetCost(DefenderTier.Heavy)).Returns(1000);

            // Setup player money
            _purchaseServiceMock.Setup(p => p.GetPlayerMoney()).Returns(PlayerMoney);
            _purchaseServiceMock.Setup(p => p.GetTroopCost(DefenderTier.Basic)).Returns(200);
            _purchaseServiceMock.Setup(p => p.GetTroopCost(DefenderTier.Medium)).Returns(500);
            _purchaseServiceMock.Setup(p => p.GetTroopCost(DefenderTier.Heavy)).Returns(1000);

            // Setup follower service
            _followerServiceMock.Setup(f => f.GetFollowerCount(PlayerFactionId)).Returns(0);
            _followerServiceMock.Setup(f => f.GetMaxFollowers()).Returns(6);
            _followerServiceMock.Setup(f => f.GetFollowers(PlayerFactionId)).Returns(Array.Empty<Follower>());

            _controller = new ArmyMenuController(
                _menuProvider,
                _factionServiceMock.Object,
                _purchaseServiceMock.Object,
                _followerServiceMock.Object,
                _tierServiceMock.Object,
                _playerContextMock.Object);
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithNullMenuProvider_ShouldThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new ArmyMenuController(
                null!,
                _factionServiceMock.Object,
                _purchaseServiceMock.Object,
                _followerServiceMock.Object,
                _tierServiceMock.Object,
                _playerContextMock.Object));
        }

        [Fact]
        public void Constructor_WithNullFactionService_ShouldThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new ArmyMenuController(
                _menuProvider,
                null!,
                _purchaseServiceMock.Object,
                _followerServiceMock.Object,
                _tierServiceMock.Object,
                _playerContextMock.Object));
        }

        [Fact]
        public void Constructor_WithNullPurchaseService_ShouldThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new ArmyMenuController(
                _menuProvider,
                _factionServiceMock.Object,
                null!,
                _followerServiceMock.Object,
                _tierServiceMock.Object,
                _playerContextMock.Object));
        }

        [Fact]
        public void Constructor_WithNullFollowerService_ShouldThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new ArmyMenuController(
                _menuProvider,
                _factionServiceMock.Object,
                _purchaseServiceMock.Object,
                null!,
                _tierServiceMock.Object,
                _playerContextMock.Object));
        }

        [Fact]
        public void Constructor_WithNullTierService_ShouldThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new ArmyMenuController(
                _menuProvider,
                _factionServiceMock.Object,
                _purchaseServiceMock.Object,
                _followerServiceMock.Object,
                null!,
                _playerContextMock.Object));
        }

        [Fact]
        public void Constructor_WithNullPlayerContext_ShouldThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new ArmyMenuController(
                _menuProvider,
                _factionServiceMock.Object,
                _purchaseServiceMock.Object,
                _followerServiceMock.Object,
                _tierServiceMock.Object,
                null!));
        }

        #endregion

        #region Menu Display Tests

        [Fact]
        public void Show_ShouldOpenArmyMenu()
        {
            // Act
            _controller.Show();

            // Assert
            Assert.True(_menuProvider.IsMenuVisible);
            Assert.Equal(ArmyMenuController.ArmyMenuId, _menuProvider.CurrentMenuId);
        }

        [Fact]
        public void Show_ShouldHaveCorrectTitle()
        {
            // Act
            _controller.Show();

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            Assert.NotNull(menu);
            Assert.Equal("Army", menu!.Title);
        }

        [Fact]
        public void Show_ShouldDisplayPlayerMoney()
        {
            // Act
            _controller.Show();

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            Assert.NotNull(menu);

            var moneyItem = menu!.GetItem(ArmyMenuController.MoneyDisplayItemId);
            Assert.NotNull(moneyItem);
            // Money is formatted with comma separator
            Assert.Contains("25", moneyItem!.Text);
            Assert.Contains("000", moneyItem.Text);
        }

        #endregion

        #region Reserve Display Tests

        [Fact]
        public void Show_ShouldDisplayReserveSummary()
        {
            // Act
            _controller.Show();

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            Assert.NotNull(menu);

            var reserveItem = menu!.GetItem(ArmyMenuController.ReserveSummaryItemId);
            Assert.NotNull(reserveItem);
            Assert.Contains("20", reserveItem!.Text); // Basic
            Assert.Contains("15", reserveItem.Text); // Medium
            Assert.Contains("10", reserveItem.Text); // Heavy
        }

        [Fact]
        public void Show_ReserveItemShouldBeDisabled()
        {
            // Act
            _controller.Show();

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            var reserveItem = menu?.GetItem(ArmyMenuController.ReserveSummaryItemId);
            Assert.NotNull(reserveItem);
            Assert.False(reserveItem!.IsEnabled);
        }

        #endregion

        #region Purchase Section Tests

        [Fact]
        public void Show_ShouldHavePurchaseBasicOption()
        {
            // Act
            _controller.Show();

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            Assert.NotNull(menu);

            var purchaseBasicItem = menu!.GetItem(ArmyMenuController.PurchaseBasicItemId);
            Assert.NotNull(purchaseBasicItem);
            Assert.Contains("200", purchaseBasicItem!.Text); // Cost
        }

        [Fact]
        public void Show_ShouldHavePurchaseMediumOption()
        {
            // Act
            _controller.Show();

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            Assert.NotNull(menu);

            var purchaseMediumItem = menu!.GetItem(ArmyMenuController.PurchaseMediumItemId);
            Assert.NotNull(purchaseMediumItem);
            Assert.Contains("500", purchaseMediumItem!.Text); // Cost
        }

        [Fact]
        public void Show_ShouldHavePurchaseHeavyOption()
        {
            // Act
            _controller.Show();

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            Assert.NotNull(menu);

            var purchaseHeavyItem = menu!.GetItem(ArmyMenuController.PurchaseHeavyItemId);
            Assert.NotNull(purchaseHeavyItem);
            Assert.Contains("1000", purchaseHeavyItem!.Text); // Cost
        }

        [Fact]
        public void Show_PurchaseOptionsShouldBeEnabledWhenCanAfford()
        {
            // Arrange
            _purchaseServiceMock.Setup(p => p.CanAfford(DefenderTier.Basic, 1)).Returns(true);
            _purchaseServiceMock.Setup(p => p.CanAfford(DefenderTier.Medium, 1)).Returns(true);
            _purchaseServiceMock.Setup(p => p.CanAfford(DefenderTier.Heavy, 1)).Returns(true);

            // Act
            _controller.Show();

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            var purchaseBasicItem = menu?.GetItem(ArmyMenuController.PurchaseBasicItemId);
            var purchaseMediumItem = menu?.GetItem(ArmyMenuController.PurchaseMediumItemId);
            var purchaseHeavyItem = menu?.GetItem(ArmyMenuController.PurchaseHeavyItemId);

            Assert.True(purchaseBasicItem!.IsEnabled);
            Assert.True(purchaseMediumItem!.IsEnabled);
            Assert.True(purchaseHeavyItem!.IsEnabled);
        }

        [Fact]
        public void Show_PurchaseOptionsShouldBeDisabledWhenCannotAfford()
        {
            // Arrange
            _purchaseServiceMock.Setup(p => p.CanAfford(DefenderTier.Basic, 1)).Returns(false);
            _purchaseServiceMock.Setup(p => p.CanAfford(DefenderTier.Medium, 1)).Returns(false);
            _purchaseServiceMock.Setup(p => p.CanAfford(DefenderTier.Heavy, 1)).Returns(false);

            // Act
            _controller.Show();

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            var purchaseBasicItem = menu?.GetItem(ArmyMenuController.PurchaseBasicItemId);
            var purchaseMediumItem = menu?.GetItem(ArmyMenuController.PurchaseMediumItemId);
            var purchaseHeavyItem = menu?.GetItem(ArmyMenuController.PurchaseHeavyItemId);

            Assert.False(purchaseBasicItem!.IsEnabled);
            Assert.False(purchaseMediumItem!.IsEnabled);
            Assert.False(purchaseHeavyItem!.IsEnabled);
        }

        #endregion

        #region Purchase Actions Tests

        [Fact]
        public void OnPurchaseBasic_ShouldCallPurchaseService()
        {
            // Arrange
            _purchaseServiceMock.Setup(p => p.CanAfford(DefenderTier.Basic, 1)).Returns(true);
            _purchaseServiceMock.Setup(p => p.PurchaseTroops(PlayerFactionId, DefenderTier.Basic, 1))
                .Returns(TroopPurchaseResult.Successful(DefenderTier.Basic, 1, 200));
            _controller.Show();

            // Act
            _menuProvider.SimulateItemSelection(ArmyMenuController.PurchaseBasicItemId);

            // Assert
            _purchaseServiceMock.Verify(p => p.PurchaseTroops(PlayerFactionId, DefenderTier.Basic, 1), Times.Once);
        }

        [Fact]
        public void OnPurchaseMedium_ShouldCallPurchaseService()
        {
            // Arrange
            _purchaseServiceMock.Setup(p => p.CanAfford(DefenderTier.Medium, 1)).Returns(true);
            _purchaseServiceMock.Setup(p => p.PurchaseTroops(PlayerFactionId, DefenderTier.Medium, 1))
                .Returns(TroopPurchaseResult.Successful(DefenderTier.Medium, 1, 500));
            _controller.Show();

            // Act
            _menuProvider.SimulateItemSelection(ArmyMenuController.PurchaseMediumItemId);

            // Assert
            _purchaseServiceMock.Verify(p => p.PurchaseTroops(PlayerFactionId, DefenderTier.Medium, 1), Times.Once);
        }

        [Fact]
        public void OnPurchaseHeavy_ShouldCallPurchaseService()
        {
            // Arrange
            _purchaseServiceMock.Setup(p => p.CanAfford(DefenderTier.Heavy, 1)).Returns(true);
            _purchaseServiceMock.Setup(p => p.PurchaseTroops(PlayerFactionId, DefenderTier.Heavy, 1))
                .Returns(TroopPurchaseResult.Successful(DefenderTier.Heavy, 1, 1000));
            _controller.Show();

            // Act
            _menuProvider.SimulateItemSelection(ArmyMenuController.PurchaseHeavyItemId);

            // Assert
            _purchaseServiceMock.Verify(p => p.PurchaseTroops(PlayerFactionId, DefenderTier.Heavy, 1), Times.Once);
        }

        [Fact]
        public void OnPurchase_ShouldRefreshMenuAfterPurchase()
        {
            // Arrange
            _purchaseServiceMock.Setup(p => p.CanAfford(DefenderTier.Basic, 1)).Returns(true);
            _purchaseServiceMock.Setup(p => p.PurchaseTroops(PlayerFactionId, DefenderTier.Basic, 1))
                .Returns(TroopPurchaseResult.Successful(DefenderTier.Basic, 1, 200));
            _controller.Show();

            // Act
            _menuProvider.SimulateItemSelection(ArmyMenuController.PurchaseBasicItemId);

            // Assert - menu should still be visible and showing army menu
            Assert.True(_menuProvider.IsMenuVisible);
            Assert.Equal(ArmyMenuController.ArmyMenuId, _menuProvider.CurrentMenuId);
        }

        #endregion

        #region Follower Section Tests

        [Fact]
        public void Show_ShouldDisplayFollowerCount()
        {
            // Arrange
            _followerServiceMock.Setup(f => f.GetFollowerCount(PlayerFactionId)).Returns(3);
            _followerServiceMock.Setup(f => f.GetMaxFollowers()).Returns(6);

            // Act
            _controller.Show();

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            Assert.NotNull(menu);

            var followerItem = menu!.GetItem(ArmyMenuController.FollowerSummaryItemId);
            Assert.NotNull(followerItem);
            Assert.Contains("3", followerItem!.Text);
            Assert.Contains("6", followerItem.Text);
        }

        [Fact]
        public void Show_ShouldHaveRecruitFollowerOptions()
        {
            // Act
            _controller.Show();

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            Assert.NotNull(menu);

            var recruitBasicItem = menu!.GetItem(ArmyMenuController.RecruitBasicItemId);
            var recruitMediumItem = menu.GetItem(ArmyMenuController.RecruitMediumItemId);
            var recruitHeavyItem = menu.GetItem(ArmyMenuController.RecruitHeavyItemId);

            Assert.NotNull(recruitBasicItem);
            Assert.NotNull(recruitMediumItem);
            Assert.NotNull(recruitHeavyItem);
        }

        [Fact]
        public void Show_RecruitOptionsShouldBeEnabledWhenNotAtMaxFollowers()
        {
            // Arrange
            _followerServiceMock.Setup(f => f.GetFollowerCount(PlayerFactionId)).Returns(3);
            _followerServiceMock.Setup(f => f.GetMaxFollowers()).Returns(6);
            _purchaseServiceMock.Setup(p => p.CanAfford(It.IsAny<DefenderTier>(), 1)).Returns(true);

            // Act
            _controller.Show();

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            var recruitBasicItem = menu?.GetItem(ArmyMenuController.RecruitBasicItemId);
            Assert.True(recruitBasicItem!.IsEnabled);
        }

        [Fact]
        public void Show_RecruitOptionsShouldBeDisabledWhenAtMaxFollowers()
        {
            // Arrange
            _followerServiceMock.Setup(f => f.GetFollowerCount(PlayerFactionId)).Returns(6);
            _followerServiceMock.Setup(f => f.GetMaxFollowers()).Returns(6);

            // Act
            _controller.Show();

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            var recruitBasicItem = menu?.GetItem(ArmyMenuController.RecruitBasicItemId);
            var recruitMediumItem = menu?.GetItem(ArmyMenuController.RecruitMediumItemId);
            var recruitHeavyItem = menu?.GetItem(ArmyMenuController.RecruitHeavyItemId);

            Assert.False(recruitBasicItem!.IsEnabled);
            Assert.False(recruitMediumItem!.IsEnabled);
            Assert.False(recruitHeavyItem!.IsEnabled);
        }

        [Fact]
        public void Show_RecruitOptionsShouldBeDisabledWhenCannotAfford()
        {
            // Arrange
            _followerServiceMock.Setup(f => f.GetFollowerCount(PlayerFactionId)).Returns(0);
            _followerServiceMock.Setup(f => f.GetMaxFollowers()).Returns(6);
            _purchaseServiceMock.Setup(p => p.CanAfford(It.IsAny<DefenderTier>(), 1)).Returns(false);

            // Act
            _controller.Show();

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            var recruitBasicItem = menu?.GetItem(ArmyMenuController.RecruitBasicItemId);
            Assert.False(recruitBasicItem!.IsEnabled);
        }

        #endregion

        #region Recruit Actions Tests

        [Fact]
        public void OnRecruitBasic_ShouldCallFollowerService()
        {
            // Arrange
            _followerServiceMock.Setup(f => f.GetFollowerCount(PlayerFactionId)).Returns(0);
            _purchaseServiceMock.Setup(p => p.CanAfford(DefenderTier.Basic, 1)).Returns(true);
            _purchaseServiceMock.Setup(p => p.PurchaseTroops(PlayerFactionId, DefenderTier.Basic, 1))
                .Returns(TroopPurchaseResult.Successful(DefenderTier.Basic, 1, 200));
            _followerServiceMock.Setup(f => f.Recruit(PlayerFactionId, DefenderTier.Basic))
                .Returns(FollowerRecruitResult.Succeeded(new Follower(PlayerFactionId, DefenderTier.Basic)));
            _controller.Show();

            // Act
            _menuProvider.SimulateItemSelection(ArmyMenuController.RecruitBasicItemId);

            // Assert
            _followerServiceMock.Verify(f => f.Recruit(PlayerFactionId, DefenderTier.Basic), Times.Once);
        }

        [Fact]
        public void OnRecruitMedium_ShouldCallFollowerService()
        {
            // Arrange
            _followerServiceMock.Setup(f => f.GetFollowerCount(PlayerFactionId)).Returns(0);
            _purchaseServiceMock.Setup(p => p.CanAfford(DefenderTier.Medium, 1)).Returns(true);
            _purchaseServiceMock.Setup(p => p.PurchaseTroops(PlayerFactionId, DefenderTier.Medium, 1))
                .Returns(TroopPurchaseResult.Successful(DefenderTier.Medium, 1, 500));
            _followerServiceMock.Setup(f => f.Recruit(PlayerFactionId, DefenderTier.Medium))
                .Returns(FollowerRecruitResult.Succeeded(new Follower(PlayerFactionId, DefenderTier.Medium)));
            _controller.Show();

            // Act
            _menuProvider.SimulateItemSelection(ArmyMenuController.RecruitMediumItemId);

            // Assert
            _followerServiceMock.Verify(f => f.Recruit(PlayerFactionId, DefenderTier.Medium), Times.Once);
        }

        [Fact]
        public void OnRecruitHeavy_ShouldCallFollowerService()
        {
            // Arrange
            _followerServiceMock.Setup(f => f.GetFollowerCount(PlayerFactionId)).Returns(0);
            _purchaseServiceMock.Setup(p => p.CanAfford(DefenderTier.Heavy, 1)).Returns(true);
            _purchaseServiceMock.Setup(p => p.PurchaseTroops(PlayerFactionId, DefenderTier.Heavy, 1))
                .Returns(TroopPurchaseResult.Successful(DefenderTier.Heavy, 1, 1000));
            _followerServiceMock.Setup(f => f.Recruit(PlayerFactionId, DefenderTier.Heavy))
                .Returns(FollowerRecruitResult.Succeeded(new Follower(PlayerFactionId, DefenderTier.Heavy)));
            _controller.Show();

            // Act
            _menuProvider.SimulateItemSelection(ArmyMenuController.RecruitHeavyItemId);

            // Assert
            _followerServiceMock.Verify(f => f.Recruit(PlayerFactionId, DefenderTier.Heavy), Times.Once);
        }

        [Fact]
        public void OnRecruit_ShouldDeductMoneyOnSuccess()
        {
            // Arrange
            _followerServiceMock.Setup(f => f.GetFollowerCount(PlayerFactionId)).Returns(0);
            _purchaseServiceMock.Setup(p => p.CanAfford(DefenderTier.Basic, 1)).Returns(true);
            _purchaseServiceMock.Setup(p => p.PurchaseTroops(PlayerFactionId, DefenderTier.Basic, 1))
                .Returns(TroopPurchaseResult.Successful(DefenderTier.Basic, 1, 200));
            _followerServiceMock.Setup(f => f.Recruit(PlayerFactionId, DefenderTier.Basic))
                .Returns(FollowerRecruitResult.Succeeded(new Follower(PlayerFactionId, DefenderTier.Basic)));
            _controller.Show();

            // Act
            _menuProvider.SimulateItemSelection(ArmyMenuController.RecruitBasicItemId);

            // Assert - PurchaseTroops should be called to handle money deduction
            // Note: The actual money deduction is handled through the game bridge
            // For followers, we use the same cost as troops
            _purchaseServiceMock.Verify(p => p.PurchaseTroops(PlayerFactionId, DefenderTier.Basic, 1), Times.Once);
        }

        #endregion

        #region Follower List Tests

        [Fact]
        public void Show_ShouldHaveManageFollowersOption()
        {
            // Act
            _controller.Show();

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            Assert.NotNull(menu);

            var manageItem = menu!.GetItem(ArmyMenuController.ManageFollowersItemId);
            Assert.NotNull(manageItem);
        }

        [Fact]
        public void OnManageFollowers_ShouldShowFollowerListMenu()
        {
            // Arrange
            var follower = new Follower(PlayerFactionId, DefenderTier.Basic);
            _followerServiceMock.Setup(f => f.GetFollowers(PlayerFactionId))
                .Returns(new[] { follower });
            _followerServiceMock.Setup(f => f.GetFollowerCount(PlayerFactionId)).Returns(1);
            _controller.Show();

            // Act
            _menuProvider.SimulateItemSelection(ArmyMenuController.ManageFollowersItemId);

            // Assert
            Assert.True(_menuProvider.IsMenuVisible);
            Assert.Equal(ArmyMenuController.FollowerListMenuId, _menuProvider.CurrentMenuId);
        }

        [Fact]
        public void FollowerListMenu_ShouldListAllFollowers()
        {
            // Arrange
            var follower1 = new Follower(PlayerFactionId, DefenderTier.Basic);
            var follower2 = new Follower(PlayerFactionId, DefenderTier.Heavy);
            _followerServiceMock.Setup(f => f.GetFollowers(PlayerFactionId))
                .Returns(new[] { follower1, follower2 });
            _followerServiceMock.Setup(f => f.GetFollowerCount(PlayerFactionId)).Returns(2);
            _controller.Show();
            _menuProvider.SimulateItemSelection(ArmyMenuController.ManageFollowersItemId);

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            Assert.NotNull(menu);

            // Should have 2 follower items (excluding the back button which also starts with "follower_")
            var followerItems = menu!.Items.Where(i =>
                i.Id.StartsWith("follower_") && i.Id != ArmyMenuController.FollowerListBackItemId).ToList();
            Assert.Equal(2, followerItems.Count);
        }

        [Fact]
        public void FollowerListMenu_WhenEmpty_ShouldShowNoFollowersMessage()
        {
            // Arrange
            _followerServiceMock.Setup(f => f.GetFollowers(PlayerFactionId))
                .Returns(Array.Empty<Follower>());
            _followerServiceMock.Setup(f => f.GetFollowerCount(PlayerFactionId)).Returns(0);
            _controller.Show();

            // Act
            _menuProvider.SimulateItemSelection(ArmyMenuController.ManageFollowersItemId);

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            Assert.NotNull(menu);

            var noFollowersItem = menu!.GetItem(ArmyMenuController.NoFollowersItemId);
            Assert.NotNull(noFollowersItem);
            Assert.False(noFollowersItem!.IsEnabled);
        }

        [Fact]
        public void FollowerListMenu_ShouldHaveBackOption()
        {
            // Arrange
            _controller.Show();

            // Act
            _menuProvider.SimulateItemSelection(ArmyMenuController.ManageFollowersItemId);

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            Assert.NotNull(menu);

            var backItem = menu!.GetItem(ArmyMenuController.FollowerListBackItemId);
            Assert.NotNull(backItem);
        }

        [Fact]
        public void OnFollowerListBack_ShouldReturnToArmyMenu()
        {
            // Arrange
            _controller.Show();
            _menuProvider.SimulateItemSelection(ArmyMenuController.ManageFollowersItemId);

            // Act
            _menuProvider.SimulateItemSelection(ArmyMenuController.FollowerListBackItemId);

            // Assert
            Assert.True(_menuProvider.IsMenuVisible);
            Assert.Equal(ArmyMenuController.ArmyMenuId, _menuProvider.CurrentMenuId);
        }

        #endregion

        #region Follower Detail Tests

        [Fact]
        public void OnFollowerSelected_ShouldShowFollowerDetailMenu()
        {
            // Arrange
            var follower = new Follower(PlayerFactionId, DefenderTier.Medium);
            _followerServiceMock.Setup(f => f.GetFollowers(PlayerFactionId))
                .Returns(new[] { follower });
            _followerServiceMock.Setup(f => f.GetFollowerById(follower.Id))
                .Returns(follower);
            _followerServiceMock.Setup(f => f.GetFollowerCount(PlayerFactionId)).Returns(1);
            _controller.Show();
            _menuProvider.SimulateItemSelection(ArmyMenuController.ManageFollowersItemId);

            // Act
            _menuProvider.SimulateItemSelection($"follower_{follower.Id}");

            // Assert
            Assert.True(_menuProvider.IsMenuVisible);
            Assert.Equal(ArmyMenuController.FollowerDetailMenuId, _menuProvider.CurrentMenuId);
        }

        [Fact]
        public void FollowerDetailMenu_ShouldShowFollowerTier()
        {
            // Arrange
            var follower = new Follower(PlayerFactionId, DefenderTier.Heavy);
            _followerServiceMock.Setup(f => f.GetFollowers(PlayerFactionId))
                .Returns(new[] { follower });
            _followerServiceMock.Setup(f => f.GetFollowerById(follower.Id))
                .Returns(follower);
            _followerServiceMock.Setup(f => f.GetFollowerCount(PlayerFactionId)).Returns(1);
            _controller.Show();
            _menuProvider.SimulateItemSelection(ArmyMenuController.ManageFollowersItemId);

            // Act
            _menuProvider.SimulateItemSelection($"follower_{follower.Id}");

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            Assert.NotNull(menu);
            Assert.Contains("Heavy", menu!.Title);
        }

        [Fact]
        public void FollowerDetailMenu_ShouldHaveDismissOption()
        {
            // Arrange
            var follower = new Follower(PlayerFactionId, DefenderTier.Basic);
            _followerServiceMock.Setup(f => f.GetFollowers(PlayerFactionId))
                .Returns(new[] { follower });
            _followerServiceMock.Setup(f => f.GetFollowerById(follower.Id))
                .Returns(follower);
            _followerServiceMock.Setup(f => f.GetFollowerCount(PlayerFactionId)).Returns(1);
            _controller.Show();
            _menuProvider.SimulateItemSelection(ArmyMenuController.ManageFollowersItemId);
            _menuProvider.SimulateItemSelection($"follower_{follower.Id}");

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            var dismissItem = menu?.GetItem(ArmyMenuController.DismissFollowerItemId);
            Assert.NotNull(dismissItem);
        }

        [Fact]
        public void OnDismissFollower_ShouldCallFollowerService()
        {
            // Arrange
            var follower = new Follower(PlayerFactionId, DefenderTier.Basic);
            _followerServiceMock.Setup(f => f.GetFollowers(PlayerFactionId))
                .Returns(new[] { follower });
            _followerServiceMock.Setup(f => f.GetFollowerById(follower.Id))
                .Returns(follower);
            _followerServiceMock.Setup(f => f.GetFollowerCount(PlayerFactionId)).Returns(1);
            _followerServiceMock.Setup(f => f.DismissFollower(follower.Id)).Returns(true);
            _controller.Show();
            _menuProvider.SimulateItemSelection(ArmyMenuController.ManageFollowersItemId);
            _menuProvider.SimulateItemSelection($"follower_{follower.Id}");

            // Act
            _menuProvider.SimulateItemSelection(ArmyMenuController.DismissFollowerItemId);

            // Assert
            _followerServiceMock.Verify(f => f.DismissFollower(follower.Id), Times.Once);
        }

        [Fact]
        public void OnDismissFollower_ShouldReturnToFollowerList()
        {
            // Arrange
            var follower = new Follower(PlayerFactionId, DefenderTier.Basic);
            _followerServiceMock.Setup(f => f.GetFollowers(PlayerFactionId))
                .Returns(new[] { follower });
            _followerServiceMock.Setup(f => f.GetFollowerById(follower.Id))
                .Returns(follower);
            _followerServiceMock.Setup(f => f.GetFollowerCount(PlayerFactionId)).Returns(1);
            _followerServiceMock.Setup(f => f.DismissFollower(follower.Id)).Returns(true);
            _controller.Show();
            _menuProvider.SimulateItemSelection(ArmyMenuController.ManageFollowersItemId);
            _menuProvider.SimulateItemSelection($"follower_{follower.Id}");

            // Act
            _menuProvider.SimulateItemSelection(ArmyMenuController.DismissFollowerItemId);

            // Assert
            Assert.True(_menuProvider.IsMenuVisible);
            Assert.Equal(ArmyMenuController.FollowerListMenuId, _menuProvider.CurrentMenuId);
        }

        #endregion

        #region Navigation Tests

        [Fact]
        public void Show_ShouldHaveBackItem()
        {
            // Act
            _controller.Show();

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            Assert.NotNull(menu);
            var item = menu!.GetItem(ArmyMenuController.BackItemId);
            Assert.NotNull(item);
            Assert.Equal("Back", item!.Text);
        }

        [Fact]
        public void OnBackSelected_ShouldRaiseBackRequestedEvent()
        {
            // Arrange
            bool eventRaised = false;
            _controller.BackRequested += (sender, args) => eventRaised = true;
            _controller.Show();

            // Act
            _menuProvider.SimulateItemSelection(ArmyMenuController.BackItemId);

            // Assert
            Assert.True(eventRaised);
        }

        [Fact]
        public void OnBackSelected_ShouldCloseMenu()
        {
            // Arrange
            _controller.BackRequested += (sender, args) => { };
            _controller.Show();

            // Act
            _menuProvider.SimulateItemSelection(ArmyMenuController.BackItemId);

            // Assert
            Assert.False(_menuProvider.IsMenuVisible);
        }

        #endregion

        #region No Player Faction Tests

        [Fact]
        public void Show_WhenNoPlayerFaction_ShouldStillShowMenu()
        {
            // Arrange
            _playerContextMock.Setup(p => p.CurrentFactionId).Returns((string?)null);

            // Act
            _controller.Show();

            // Assert
            Assert.True(_menuProvider.IsMenuVisible);
        }

        [Fact]
        public void Show_WhenNoPlayerFaction_ShouldDisablePurchaseOptions()
        {
            // Arrange
            _playerContextMock.Setup(p => p.CurrentFactionId).Returns((string?)null);

            // Act
            _controller.Show();

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            var purchaseBasicItem = menu?.GetItem(ArmyMenuController.PurchaseBasicItemId);
            var purchaseMediumItem = menu?.GetItem(ArmyMenuController.PurchaseMediumItemId);
            var purchaseHeavyItem = menu?.GetItem(ArmyMenuController.PurchaseHeavyItemId);

            Assert.False(purchaseBasicItem!.IsEnabled);
            Assert.False(purchaseMediumItem!.IsEnabled);
            Assert.False(purchaseHeavyItem!.IsEnabled);
        }

        #endregion
    }
}
