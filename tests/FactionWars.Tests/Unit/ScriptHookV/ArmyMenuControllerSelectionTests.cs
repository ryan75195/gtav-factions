using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.Economy.Interfaces;
using FactionWars.Economy.Models;
using FactionWars.Factions.Interfaces;
using FactionWars.Factions.Models;
using FactionWars.ScriptHookV.UI;
using FactionWars.Tests.Mocks;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV
{
    /// <summary>
    /// Tests for ArmyMenuController cursor retention (selection tracking) after actions.
    /// </summary>
    public class ArmyMenuControllerSelectionTests
    {
        private readonly MockMenuProvider _menuProvider;
        private readonly Mock<IFactionService> _factionServiceMock;
        private readonly Mock<ITroopPurchaseService> _purchaseServiceMock;
        private readonly Mock<IFollowerService> _followerServiceMock;
        private readonly Mock<IDefenderRoleService> _tierServiceMock;
        private readonly Mock<IPlayerContext> _playerContextMock;
        private readonly ArmyMenuController _controller;

        private const string PlayerFactionId = "michael";
        private const string PlayerFactionName = "De Santa Enterprises";
        private const int PlayerMoney = 25000;

        public ArmyMenuControllerSelectionTests()
        {
            _menuProvider = new MockMenuProvider();
            _factionServiceMock = new Mock<IFactionService>();
            _purchaseServiceMock = new Mock<ITroopPurchaseService>();
            _followerServiceMock = new Mock<IFollowerService>();
            _tierServiceMock = new Mock<IDefenderRoleService>();
            _playerContextMock = new Mock<IPlayerContext>();

            // Setup default player faction
            _playerContextMock.Setup(p => p.CurrentFactionId).Returns(PlayerFactionId);

            // Setup default faction
            var faction = new Faction(PlayerFactionId, PlayerFactionName, "Michael De Santa", "Corporate empire", new FactionColor(0, 0, 255));
            _factionServiceMock.Setup(f => f.GetFaction(PlayerFactionId)).Returns(faction);

            // Setup default faction state with reserves
            var factionState = new FactionState(PlayerFactionId, 10000, 50);
            factionState.AddReserveTroops(DefenderRole.Grunt, 20);
            factionState.AddReserveTroops(DefenderRole.Gunner, 15);
            factionState.AddReserveTroops(DefenderRole.Rifleman, 10);
            _factionServiceMock.Setup(f => f.GetFactionState(PlayerFactionId)).Returns(factionState);

            // Setup tier costs
            _tierServiceMock.Setup(t => t.GetCost(DefenderRole.Grunt)).Returns(200);
            _tierServiceMock.Setup(t => t.GetCost(DefenderRole.Gunner)).Returns(500);
            _tierServiceMock.Setup(t => t.GetCost(DefenderRole.Rifleman)).Returns(1000);

            // Setup player money and affordability
            _purchaseServiceMock.Setup(p => p.GetPlayerMoney()).Returns(PlayerMoney);
            _purchaseServiceMock.Setup(p => p.GetTroopCost(DefenderRole.Grunt)).Returns(200);
            _purchaseServiceMock.Setup(p => p.GetTroopCost(DefenderRole.Gunner)).Returns(500);
            _purchaseServiceMock.Setup(p => p.GetTroopCost(DefenderRole.Rifleman)).Returns(1000);
            _purchaseServiceMock.Setup(p => p.CanAfford(It.IsAny<DefenderRole>(), 1)).Returns(true);

            // Setup follower service
            _followerServiceMock.Setup(f => f.GetFollowerCount(PlayerFactionId)).Returns(0);
            _followerServiceMock.Setup(f => f.GetMaxFollowers()).Returns(6);
            _followerServiceMock.Setup(f => f.GetFollowers(PlayerFactionId)).Returns(Array.Empty<Follower>());

            // Setup successful purchase/recruit results
            _purchaseServiceMock.Setup(p => p.PurchaseTroops(PlayerFactionId, It.IsAny<DefenderRole>(), 1))
                .Returns((string fId, DefenderRole tier, int count) =>
                    TroopPurchaseResult.Successful(tier, count, _purchaseServiceMock.Object.GetTroopCost(tier)));
            _followerServiceMock.Setup(f => f.Recruit(PlayerFactionId, It.IsAny<DefenderRole>()))
                .Returns((string fId, DefenderRole tier) =>
                    FollowerRecruitResult.Succeeded(new Follower(fId, tier)));

            _controller = new ArmyMenuController(
                _menuProvider,
                _factionServiceMock.Object,
                _purchaseServiceMock.Object,
                _followerServiceMock.Object,
                _tierServiceMock.Object,
                _playerContextMock.Object);
        }

        /// <summary>
        /// Helper to find the index of an item by ID in a menu's item list.
        /// </summary>
        private static int FindItemIndex(IReadOnlyList<FactionWars.UI.Models.MenuItem> items, string itemId)
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].Id == itemId)
                    return i;
            }
            return -1;
        }

        #region Purchase Action Selection Tests

        [Fact]
        public void AfterPurchaseBasic_MenuRefreshesWithSameItemSelected()
        {
            // Arrange
            _controller.Show();

            // Act - simulate selecting purchase_basic
            _menuProvider.SimulateItemSelection(ArmyMenuController.PurchaseBasicItemId);

            // Assert - menu should refresh with the same item selected
            Assert.True(_menuProvider.IsMenuVisible);
            Assert.Equal(ArmyMenuController.ArmyMenuId, _menuProvider.CurrentMenuId);

            // Verify the selected index corresponds to the purchase_basic item
            var menu = _menuProvider.GetCurrentMenuDefinition();
            Assert.NotNull(menu);
            var expectedIndex = FindItemIndex(menu!.Items, ArmyMenuController.PurchaseBasicItemId);
            Assert.Equal(expectedIndex, _menuProvider.SelectedIndex);
        }

        [Fact]
        public void AfterPurchaseMedium_MenuRefreshesWithSameItemSelected()
        {
            // Arrange
            _controller.Show();

            // Act
            _menuProvider.SimulateItemSelection(ArmyMenuController.PurchaseMediumItemId);

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            Assert.NotNull(menu);
            var expectedIndex = FindItemIndex(menu!.Items, ArmyMenuController.PurchaseMediumItemId);
            Assert.Equal(expectedIndex, _menuProvider.SelectedIndex);
        }

        [Fact]
        public void AfterPurchaseHeavy_MenuRefreshesWithSameItemSelected()
        {
            // Arrange
            _controller.Show();

            // Act
            _menuProvider.SimulateItemSelection(ArmyMenuController.PurchaseHeavyItemId);

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            Assert.NotNull(menu);
            var expectedIndex = FindItemIndex(menu!.Items, ArmyMenuController.PurchaseHeavyItemId);
            Assert.Equal(expectedIndex, _menuProvider.SelectedIndex);
        }

        #endregion

        #region Recruit Action Selection Tests

        [Fact]
        public void AfterRecruitBasic_MenuRefreshesWithSameItemSelected()
        {
            // Arrange
            _controller.Show();

            // Act
            _menuProvider.SimulateItemSelection(ArmyMenuController.RecruitBasicItemId);

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            Assert.NotNull(menu);
            var expectedIndex = FindItemIndex(menu!.Items, ArmyMenuController.RecruitBasicItemId);
            Assert.Equal(expectedIndex, _menuProvider.SelectedIndex);
        }

        [Fact]
        public void AfterRecruitMedium_MenuRefreshesWithSameItemSelected()
        {
            // Arrange
            _controller.Show();

            // Act
            _menuProvider.SimulateItemSelection(ArmyMenuController.RecruitMediumItemId);

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            Assert.NotNull(menu);
            var expectedIndex = FindItemIndex(menu!.Items, ArmyMenuController.RecruitMediumItemId);
            Assert.Equal(expectedIndex, _menuProvider.SelectedIndex);
        }

        [Fact]
        public void AfterRecruitHeavy_MenuRefreshesWithSameItemSelected()
        {
            // Arrange
            _controller.Show();

            // Act
            _menuProvider.SimulateItemSelection(ArmyMenuController.RecruitHeavyItemId);

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            Assert.NotNull(menu);
            var expectedIndex = FindItemIndex(menu!.Items, ArmyMenuController.RecruitHeavyItemId);
            Assert.Equal(expectedIndex, _menuProvider.SelectedIndex);
        }

        #endregion

        #region Navigation Clears Selection Tests

        [Fact]
        public void Show_ResetsSelectionToFirstItem()
        {
            // Arrange - first show and select an item
            _controller.Show();
            _menuProvider.SimulateItemSelection(ArmyMenuController.PurchaseHeavyItemId);

            // Act - show again (fresh open)
            _controller.Show();

            // Assert - should reset to first item (index 0)
            Assert.Equal(0, _menuProvider.SelectedIndex);
        }

        [Fact]
        public void NavigateToManageFollowers_ClearsSelection()
        {
            // Arrange
            _controller.Show();
            // First make a purchase to set _lastSelectedItemId
            _menuProvider.SimulateItemSelection(ArmyMenuController.PurchaseBasicItemId);

            // Act - navigate to manage followers
            _menuProvider.SimulateItemSelection(ArmyMenuController.ManageFollowersItemId);

            // Assert - should be at follower list menu with default selection
            Assert.Equal(ArmyMenuController.FollowerListMenuId, _menuProvider.CurrentMenuId);
            Assert.Equal(0, _menuProvider.SelectedIndex);
        }

        [Fact]
        public void BackFromFollowerList_DoesNotRetainPreviousArmyMenuSelection()
        {
            // Arrange
            _controller.Show();
            _menuProvider.SimulateItemSelection(ArmyMenuController.ManageFollowersItemId);

            // Act - go back to army menu
            _menuProvider.SimulateItemSelection(ArmyMenuController.FollowerListBackItemId);

            // Assert - back at army menu, selection should be at first item (no retention after navigation)
            Assert.Equal(ArmyMenuController.ArmyMenuId, _menuProvider.CurrentMenuId);
            // When navigating to manage followers, selection was cleared
            // When coming back, no selectedItemId is passed (since it was cleared)
            Assert.Equal(0, _menuProvider.SelectedIndex);
        }

        #endregion

        #region Multiple Actions Selection Tests

        [Fact]
        public void MultiplePurchases_RetainSelectionThroughout()
        {
            // Arrange
            _controller.Show();

            // Act - simulate multiple purchases of the same type
            _menuProvider.SimulateItemSelection(ArmyMenuController.PurchaseMediumItemId);
            _menuProvider.SimulateItemSelection(ArmyMenuController.PurchaseMediumItemId);
            _menuProvider.SimulateItemSelection(ArmyMenuController.PurchaseMediumItemId);

            // Assert - should still be on purchase_medium
            var menu = _menuProvider.GetCurrentMenuDefinition();
            Assert.NotNull(menu);
            var expectedIndex = FindItemIndex(menu!.Items, ArmyMenuController.PurchaseMediumItemId);
            Assert.Equal(expectedIndex, _menuProvider.SelectedIndex);
        }

        [Fact]
        public void SwitchingBetweenPurchases_UpdatesSelectionCorrectly()
        {
            // Arrange
            _controller.Show();

            // Act - purchase basic, then heavy
            _menuProvider.SimulateItemSelection(ArmyMenuController.PurchaseBasicItemId);
            _menuProvider.SimulateItemSelection(ArmyMenuController.PurchaseHeavyItemId);

            // Assert - should be on purchase_heavy
            var menu = _menuProvider.GetCurrentMenuDefinition();
            Assert.NotNull(menu);
            var expectedIndex = FindItemIndex(menu!.Items, ArmyMenuController.PurchaseHeavyItemId);
            Assert.Equal(expectedIndex, _menuProvider.SelectedIndex);
        }

        #endregion
    }
}
