using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.Factions.Interfaces;
using FactionWars.Factions.Models;
using FactionWars.ScriptHookV.UI;
using FactionWars.Territory.Interfaces;
using FactionWars.Territory.Models;
using FactionWars.UI.Models;
using Moq;
using System;
using System.Collections.Generic;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV.UI
{
    /// <summary>
    /// Tests for OverviewMenuController displaying faction stats and victory progress.
    /// </summary>
    public class OverviewMenuControllerTests
    {
        private readonly NativeUIMenuProvider _menuProvider;
        private readonly Mock<IFactionService> _factionServiceMock;
        private readonly Mock<IZoneService> _zoneServiceMock;
        private readonly Mock<IPlayerContext> _playerContextMock;
        private readonly OverviewMenuController _controller;

        private const string PlayerFactionId = "michael";
        private const string PlayerFactionName = "De Santa Enterprises";
        private const int TotalZonesInGame = 31;

        public OverviewMenuControllerTests()
        {
            _menuProvider = new NativeUIMenuProvider();
            _factionServiceMock = new Mock<IFactionService>();
            _zoneServiceMock = new Mock<IZoneService>();
            _playerContextMock = new Mock<IPlayerContext>();

            // Setup default player faction
            _playerContextMock.Setup(p => p.CurrentFactionId).Returns(PlayerFactionId);

            // Setup default faction
            var faction = new Faction(PlayerFactionId, PlayerFactionName, "Michael De Santa", "Corporate empire", new FactionColor(0, 0, 255));
            _factionServiceMock.Setup(f => f.GetFaction(PlayerFactionId)).Returns(faction);

            // Setup default faction state
            var factionState = new FactionState(PlayerFactionId, 10000, 50);
            factionState.AddReserveTroops(DefenderTier.Basic, 20);
            factionState.AddReserveTroops(DefenderTier.Medium, 15);
            factionState.AddReserveTroops(DefenderTier.Heavy, 10);
            for (int i = 0; i < 8; i++)
            {
                factionState.AddZone($"zone_{i}");
            }
            _factionServiceMock.Setup(f => f.GetFactionState(PlayerFactionId)).Returns(factionState);

            // Setup total zones
            var allZones = new List<Zone>();
            for (int i = 0; i < TotalZonesInGame; i++)
            {
                allZones.Add(new Zone($"zone_{i}", $"Zone {i}", new Vector3(0, 0, 0), 100f));
            }
            _zoneServiceMock.Setup(z => z.GetAllZones()).Returns(allZones);

            _controller = new OverviewMenuController(
                _menuProvider,
                _factionServiceMock.Object,
                _zoneServiceMock.Object,
                _playerContextMock.Object);
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithNullMenuProvider_ShouldThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new OverviewMenuController(
                null!,
                _factionServiceMock.Object,
                _zoneServiceMock.Object,
                _playerContextMock.Object));
        }

        [Fact]
        public void Constructor_WithNullFactionService_ShouldThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new OverviewMenuController(
                _menuProvider,
                null!,
                _zoneServiceMock.Object,
                _playerContextMock.Object));
        }

        [Fact]
        public void Constructor_WithNullZoneService_ShouldThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new OverviewMenuController(
                _menuProvider,
                _factionServiceMock.Object,
                null!,
                _playerContextMock.Object));
        }

        [Fact]
        public void Constructor_WithNullPlayerContext_ShouldThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new OverviewMenuController(
                _menuProvider,
                _factionServiceMock.Object,
                _zoneServiceMock.Object,
                null!));
        }

        #endregion

        #region Menu Display Tests

        [Fact]
        public void Show_ShouldOpenOverviewMenu()
        {
            // Act
            _controller.Show();

            // Assert
            Assert.True(_menuProvider.IsMenuVisible);
            Assert.Equal(OverviewMenuController.OverviewMenuId, _menuProvider.CurrentMenuId);
        }

        [Fact]
        public void Show_ShouldHaveCorrectTitle()
        {
            // Act
            _controller.Show();

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            Assert.NotNull(menu);
            Assert.Equal("Overview", menu!.Title);
        }

        [Fact]
        public void Show_ShouldHaveFactionNameInSubtitle()
        {
            // Act
            _controller.Show();

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            Assert.NotNull(menu);
            Assert.Contains(PlayerFactionName, menu!.Subtitle);
        }

        #endregion

        #region Faction Stats Display Tests

        [Fact]
        public void Show_ShouldDisplayZonesOwnedItem()
        {
            // Act
            _controller.Show();

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            Assert.NotNull(menu);
            var item = menu!.GetItem(OverviewMenuController.ZonesOwnedItemId);
            Assert.NotNull(item);
            Assert.Contains("8", item!.Text);
            Assert.Contains("31", item.Text);
        }

        [Fact]
        public void Show_ShouldDisplayVictoryProgressItem()
        {
            // Act
            _controller.Show();

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            Assert.NotNull(menu);
            var item = menu!.GetItem(OverviewMenuController.VictoryProgressItemId);
            Assert.NotNull(item);
            // 8 out of 31 = ~25.8%
            Assert.Contains("%", item!.Text);
        }

        [Fact]
        public void Show_ShouldDisplayCashItem()
        {
            // Act
            _controller.Show();

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            Assert.NotNull(menu);
            var item = menu!.GetItem(OverviewMenuController.CashItemId);
            Assert.NotNull(item);
            Assert.Contains("10,000", item!.Text);
        }

        [Fact]
        public void Show_ShouldDisplayTotalTroopsItem()
        {
            // Act
            _controller.Show();

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            Assert.NotNull(menu);
            var item = menu!.GetItem(OverviewMenuController.TotalTroopsItemId);
            Assert.NotNull(item);
            // Total reserve = 20 + 15 + 10 = 45
            Assert.Contains("45", item!.Text);
        }

        [Fact]
        public void Show_ShouldDisplayReserveByTierItem()
        {
            // Act
            _controller.Show();

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            Assert.NotNull(menu);
            var item = menu!.GetItem(OverviewMenuController.ReserveByTierItemId);
            Assert.NotNull(item);
            Assert.Contains("Basic", item!.Description);
            Assert.Contains("20", item.Description);
            Assert.Contains("Medium", item.Description);
            Assert.Contains("15", item.Description);
            Assert.Contains("Heavy", item.Description);
            Assert.Contains("10", item.Description);
        }

        [Fact]
        public void Show_ShouldDisplayMilitaryStrengthItem()
        {
            // Act
            _controller.Show();

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            Assert.NotNull(menu);
            var item = menu!.GetItem(OverviewMenuController.MilitaryStrengthItemId);
            Assert.NotNull(item);
        }

        #endregion

        #region Victory Progress Calculation Tests

        [Fact]
        public void Show_WhenOwning0Zones_ShouldShow0PercentProgress()
        {
            // Arrange
            var emptyState = new FactionState(PlayerFactionId, 10000, 50);
            _factionServiceMock.Setup(f => f.GetFactionState(PlayerFactionId)).Returns(emptyState);

            // Act
            _controller.Show();

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            var item = menu?.GetItem(OverviewMenuController.VictoryProgressItemId);
            Assert.NotNull(item);
            Assert.Contains("0%", item!.Text);
        }

        [Fact]
        public void Show_WhenOwningAllZones_ShouldShow100PercentProgress()
        {
            // Arrange
            var fullState = new FactionState(PlayerFactionId, 10000, 50);
            for (int i = 0; i < TotalZonesInGame; i++)
            {
                fullState.AddZone($"zone_{i}");
            }
            _factionServiceMock.Setup(f => f.GetFactionState(PlayerFactionId)).Returns(fullState);

            // Act
            _controller.Show();

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            var item = menu?.GetItem(OverviewMenuController.VictoryProgressItemId);
            Assert.NotNull(item);
            Assert.Contains("100%", item!.Text);
        }

        #endregion

        #region Faction Data Not Found Tests

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
        public void Show_WhenFactionStateNotFound_ShouldShowZeroValues()
        {
            // Arrange
            _factionServiceMock.Setup(f => f.GetFactionState(PlayerFactionId)).Returns((FactionState?)null);

            // Act
            _controller.Show();

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            var cashItem = menu?.GetItem(OverviewMenuController.CashItemId);
            Assert.NotNull(cashItem);
            Assert.Contains("0", cashItem!.Text);
        }

        #endregion

        #region Back Navigation Tests

        [Fact]
        public void Show_ShouldHaveBackItem()
        {
            // Act
            _controller.Show();

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            Assert.NotNull(menu);
            var item = menu!.GetItem(OverviewMenuController.BackItemId);
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
            _menuProvider.SimulateItemSelection(OverviewMenuController.BackItemId);

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
            _menuProvider.SimulateItemSelection(OverviewMenuController.BackItemId);

            // Assert
            Assert.False(_menuProvider.IsMenuVisible);
        }

        #endregion

        #region Menu Item Order Tests

        [Fact]
        public void Show_MenuItemsShouldBeInCorrectOrder()
        {
            // Act
            _controller.Show();

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            Assert.NotNull(menu);
            Assert.True(menu!.Items.Count >= 7);

            // Verify order: Zones, Victory Progress, Cash, Total Troops, Reserve, Strength, Back
            Assert.Equal(OverviewMenuController.ZonesOwnedItemId, menu.Items[0].Id);
            Assert.Equal(OverviewMenuController.VictoryProgressItemId, menu.Items[1].Id);
            Assert.Equal(OverviewMenuController.CashItemId, menu.Items[2].Id);
            Assert.Equal(OverviewMenuController.TotalTroopsItemId, menu.Items[3].Id);
            Assert.Equal(OverviewMenuController.ReserveByTierItemId, menu.Items[4].Id);
            Assert.Equal(OverviewMenuController.MilitaryStrengthItemId, menu.Items[5].Id);
            Assert.Equal(OverviewMenuController.BackItemId, menu.Items[6].Id);
        }

        #endregion

        #region Items Enabled State Tests

        [Fact]
        public void Show_InfoItemsShouldBeDisabled()
        {
            // Act
            _controller.Show();

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            Assert.NotNull(menu);

            // Info items should be disabled (display only)
            var zonesItem = menu!.GetItem(OverviewMenuController.ZonesOwnedItemId);
            var victoryItem = menu.GetItem(OverviewMenuController.VictoryProgressItemId);
            var cashItem = menu.GetItem(OverviewMenuController.CashItemId);
            var troopsItem = menu.GetItem(OverviewMenuController.TotalTroopsItemId);
            var reserveItem = menu.GetItem(OverviewMenuController.ReserveByTierItemId);
            var strengthItem = menu.GetItem(OverviewMenuController.MilitaryStrengthItemId);

            Assert.False(zonesItem!.IsEnabled);
            Assert.False(victoryItem!.IsEnabled);
            Assert.False(cashItem!.IsEnabled);
            Assert.False(troopsItem!.IsEnabled);
            Assert.False(reserveItem!.IsEnabled);
            Assert.False(strengthItem!.IsEnabled);
        }

        [Fact]
        public void Show_BackItemShouldBeEnabled()
        {
            // Act
            _controller.Show();

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            var backItem = menu?.GetItem(OverviewMenuController.BackItemId);
            Assert.True(backItem!.IsEnabled);
        }

        #endregion
    }
}
