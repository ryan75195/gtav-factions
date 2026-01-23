using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.Factions.Interfaces;
using FactionWars.Factions.Models;
using FactionWars.ScriptHookV.UI;
using FactionWars.Tests.Mocks;
using FactionWars.Territory.Interfaces;
using FactionWars.Territory.Models;
using FactionWars.UI.Interfaces;
using FactionWars.UI.Models;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV.UI
{
    /// <summary>
    /// Tests for ZoneManagementMenuController handling zone viewing, troop allocation, and withdrawal.
    /// </summary>
    public class ZoneManagementMenuControllerTests
    {
        private readonly MockMenuProvider _menuProvider;
        private readonly Mock<IFactionService> _factionServiceMock;
        private readonly Mock<IZoneService> _zoneServiceMock;
        private readonly Mock<IPlayerContext> _playerContextMock;
        private readonly Mock<IZoneDefenderAllocationService> _allocationServiceMock;
        private readonly ZoneManagementMenuController _controller;

        private const string PlayerFactionId = "michael";
        private const string PlayerFactionName = "De Santa Enterprises";

        public ZoneManagementMenuControllerTests()
        {
            _menuProvider = new MockMenuProvider();
            _factionServiceMock = new Mock<IFactionService>();
            _zoneServiceMock = new Mock<IZoneService>();
            _playerContextMock = new Mock<IPlayerContext>();
            _allocationServiceMock = new Mock<IZoneDefenderAllocationService>();

            // Setup default player faction
            _playerContextMock.Setup(p => p.CurrentFactionId).Returns(PlayerFactionId);

            // Setup default faction
            var faction = new Faction(PlayerFactionId, PlayerFactionName, "Michael De Santa", "Corporate empire", new FactionColor(0, 0, 255));
            _factionServiceMock.Setup(f => f.GetFaction(PlayerFactionId)).Returns(faction);

            // Setup default faction state with reserves
            // After consolidation, initialTroopCount goes to Basic tier, so we use reserve pool only
            var factionState = new FactionState(PlayerFactionId, 10000);
            factionState.AddReserveTroops(DefenderTier.Basic, 20);
            factionState.AddReserveTroops(DefenderTier.Medium, 15);
            factionState.AddReserveTroops(DefenderTier.Heavy, 10);
            factionState.AddZone("zone_downtown");
            factionState.AddZone("zone_vinewood");
            factionState.AddZone("zone_airport");
            _factionServiceMock.Setup(f => f.GetFactionState(PlayerFactionId)).Returns(factionState);

            // Setup zones
            var playerZones = new List<Zone>
            {
                new Zone("zone_downtown", "Downtown", new Vector3(0, 0, 0), 200f, 5),
                new Zone("zone_vinewood", "Vinewood", new Vector3(100, 100, 0), 150f, 3),
                new Zone("zone_airport", "Airport", new Vector3(-200, -200, 0), 300f, 7)
            };
            _zoneServiceMock.Setup(z => z.GetZonesByOwner(PlayerFactionId)).Returns(playerZones);
            _zoneServiceMock.Setup(z => z.GetZone("zone_downtown")).Returns(playerZones[0]);
            _zoneServiceMock.Setup(z => z.GetZone("zone_vinewood")).Returns(playerZones[1]);
            _zoneServiceMock.Setup(z => z.GetZone("zone_airport")).Returns(playerZones[2]);

            _controller = new ZoneManagementMenuController(
                _menuProvider,
                _factionServiceMock.Object,
                _zoneServiceMock.Object,
                _playerContextMock.Object,
                _allocationServiceMock.Object);
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithNullMenuProvider_ShouldThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new ZoneManagementMenuController(
                null!,
                _factionServiceMock.Object,
                _zoneServiceMock.Object,
                _playerContextMock.Object,
                _allocationServiceMock.Object));
        }

        [Fact]
        public void Constructor_WithNullFactionService_ShouldThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new ZoneManagementMenuController(
                _menuProvider,
                null!,
                _zoneServiceMock.Object,
                _playerContextMock.Object,
                _allocationServiceMock.Object));
        }

        [Fact]
        public void Constructor_WithNullZoneService_ShouldThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new ZoneManagementMenuController(
                _menuProvider,
                _factionServiceMock.Object,
                null!,
                _playerContextMock.Object,
                _allocationServiceMock.Object));
        }

        [Fact]
        public void Constructor_WithNullPlayerContext_ShouldThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new ZoneManagementMenuController(
                _menuProvider,
                _factionServiceMock.Object,
                _zoneServiceMock.Object,
                null!,
                _allocationServiceMock.Object));
        }

        [Fact]
        public void Constructor_WithNullAllocationService_ShouldThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new ZoneManagementMenuController(
                _menuProvider,
                _factionServiceMock.Object,
                _zoneServiceMock.Object,
                _playerContextMock.Object,
                null!));
        }

        #endregion

        #region Menu Display Tests

        [Fact]
        public void Show_ShouldOpenZoneManagementMenu()
        {
            // Act
            _controller.Show();

            // Assert
            Assert.True(_menuProvider.IsMenuVisible);
            Assert.Equal(ZoneManagementMenuController.ZoneManagementMenuId, _menuProvider.CurrentMenuId);
        }

        [Fact]
        public void Show_ShouldHaveCorrectTitle()
        {
            // Act
            _controller.Show();

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            Assert.NotNull(menu);
            Assert.Equal("Zone Management", menu!.Title);
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

        #region Zone List Display Tests

        [Fact]
        public void Show_ShouldListAllOwnedZones()
        {
            // Act
            _controller.Show();

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            Assert.NotNull(menu);

            // Should have items for each zone plus back button
            var zoneItems = menu!.Items.Where(i => i.Id.StartsWith("zone_")).ToList();
            Assert.Equal(3, zoneItems.Count);
        }

        [Fact]
        public void Show_ShouldDisplayZoneNamesInItems()
        {
            // Act
            _controller.Show();

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            Assert.NotNull(menu);

            var downtownItem = menu!.GetItem("zone_downtown");
            var vinewoodItem = menu.GetItem("zone_vinewood");
            var airportItem = menu.GetItem("zone_airport");

            Assert.NotNull(downtownItem);
            Assert.NotNull(vinewoodItem);
            Assert.NotNull(airportItem);

            Assert.Contains("Downtown", downtownItem!.Text);
            Assert.Contains("Vinewood", vinewoodItem!.Text);
            Assert.Contains("Airport", airportItem!.Text);
        }

        [Fact]
        public void Show_ShouldDisplayAllocationInZoneDescription()
        {
            // Arrange
            var allocation = new ZoneDefenderAllocation(PlayerFactionId, "zone_downtown");
            allocation.AddTroops(DefenderTier.Basic, 5);
            allocation.AddTroops(DefenderTier.Medium, 3);
            allocation.AddTroops(DefenderTier.Heavy, 2);
            _allocationServiceMock.Setup(a => a.GetAllocation(PlayerFactionId, "zone_downtown")).Returns(allocation);

            // Act
            _controller.Show();

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            var downtownItem = menu?.GetItem("zone_downtown");
            Assert.NotNull(downtownItem);

            // Should show troop allocation in description
            Assert.Contains("5", downtownItem!.Description);
            Assert.Contains("3", downtownItem.Description);
            Assert.Contains("2", downtownItem.Description);
        }

        [Fact]
        public void Show_WhenNoAllocation_ShouldShowZeroTroops()
        {
            // Arrange
            _allocationServiceMock.Setup(a => a.GetAllocation(PlayerFactionId, "zone_downtown")).Returns((ZoneDefenderAllocation?)null);

            // Act
            _controller.Show();

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            var downtownItem = menu?.GetItem("zone_downtown");
            Assert.NotNull(downtownItem);

            // Should indicate no troops allocated
            Assert.Contains("0", downtownItem!.Description);
        }

        [Fact]
        public void Show_WhenNoOwnedZones_ShouldShowNoZonesMessage()
        {
            // Arrange
            _zoneServiceMock.Setup(z => z.GetZonesByOwner(PlayerFactionId)).Returns(new List<Zone>());
            var emptyState = new FactionState(PlayerFactionId, 10000);
            _factionServiceMock.Setup(f => f.GetFactionState(PlayerFactionId)).Returns(emptyState);

            // Act
            _controller.Show();

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            Assert.NotNull(menu);

            var noZonesItem = menu!.GetItem(ZoneManagementMenuController.NoZonesItemId);
            Assert.NotNull(noZonesItem);
            Assert.False(noZonesItem!.IsEnabled);
        }

        #endregion

        #region Reserve Display Tests

        [Fact]
        public void Show_ShouldDisplayReservePoolSummary()
        {
            // Act
            _controller.Show();

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            Assert.NotNull(menu);

            var reserveItem = menu!.GetItem(ZoneManagementMenuController.ReserveSummaryItemId);
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
            var reserveItem = menu?.GetItem(ZoneManagementMenuController.ReserveSummaryItemId);
            Assert.NotNull(reserveItem);
            Assert.False(reserveItem!.IsEnabled);
        }

        #endregion

        #region Zone Selection Tests

        [Fact]
        public void OnZoneSelected_ShouldShowZoneDetailMenu()
        {
            // Arrange
            _controller.Show();

            // Act
            _menuProvider.SimulateItemSelection("zone_downtown");

            // Assert
            Assert.True(_menuProvider.IsMenuVisible);
            Assert.Equal(ZoneManagementMenuController.ZoneDetailMenuId, _menuProvider.CurrentMenuId);
        }

        [Fact]
        public void ZoneDetailMenu_ShouldShowZoneNameInTitle()
        {
            // Arrange
            _controller.Show();

            // Act
            _menuProvider.SimulateItemSelection("zone_downtown");

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            Assert.NotNull(menu);
            Assert.Contains("Downtown", menu!.Title);
        }

        [Fact]
        public void ZoneDetailMenu_ShouldShowCurrentAllocation()
        {
            // Arrange
            var allocation = new ZoneDefenderAllocation(PlayerFactionId, "zone_downtown");
            allocation.AddTroops(DefenderTier.Basic, 5);
            allocation.AddTroops(DefenderTier.Medium, 3);
            allocation.AddTroops(DefenderTier.Heavy, 2);
            _allocationServiceMock.Setup(a => a.GetAllocation(PlayerFactionId, "zone_downtown")).Returns(allocation);
            _controller.Show();

            // Act
            _menuProvider.SimulateItemSelection("zone_downtown");

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            Assert.NotNull(menu);

            var basicItem = menu!.GetItem(ZoneManagementMenuController.CurrentBasicItemId);
            var mediumItem = menu.GetItem(ZoneManagementMenuController.CurrentMediumItemId);
            var heavyItem = menu.GetItem(ZoneManagementMenuController.CurrentHeavyItemId);

            Assert.NotNull(basicItem);
            Assert.NotNull(mediumItem);
            Assert.NotNull(heavyItem);

            Assert.Contains("5", basicItem!.Text);
            Assert.Contains("3", mediumItem!.Text);
            Assert.Contains("2", heavyItem!.Text);
        }

        [Fact]
        public void ZoneDetailMenu_ShouldHaveAllocateOptions()
        {
            // Arrange
            _controller.Show();

            // Act
            _menuProvider.SimulateItemSelection("zone_downtown");

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            Assert.NotNull(menu);

            var allocateBasicItem = menu!.GetItem(ZoneManagementMenuController.AllocateBasicItemId);
            var allocateMediumItem = menu.GetItem(ZoneManagementMenuController.AllocateMediumItemId);
            var allocateHeavyItem = menu.GetItem(ZoneManagementMenuController.AllocateHeavyItemId);

            Assert.NotNull(allocateBasicItem);
            Assert.NotNull(allocateMediumItem);
            Assert.NotNull(allocateHeavyItem);
        }

        [Fact]
        public void ZoneDetailMenu_ShouldHaveWithdrawOptions()
        {
            // Arrange
            _controller.Show();

            // Act
            _menuProvider.SimulateItemSelection("zone_downtown");

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            Assert.NotNull(menu);

            var withdrawBasicItem = menu!.GetItem(ZoneManagementMenuController.WithdrawBasicItemId);
            var withdrawMediumItem = menu.GetItem(ZoneManagementMenuController.WithdrawMediumItemId);
            var withdrawHeavyItem = menu.GetItem(ZoneManagementMenuController.WithdrawHeavyItemId);

            Assert.NotNull(withdrawBasicItem);
            Assert.NotNull(withdrawMediumItem);
            Assert.NotNull(withdrawHeavyItem);
        }

        #endregion

        #region Allocate Troops Tests

        [Fact]
        public void OnAllocateBasic_ShouldCallAllocationService()
        {
            // Arrange
            var factionState = _factionServiceMock.Object.GetFactionState(PlayerFactionId)!;
            _allocationServiceMock.Setup(a => a.AllocateTroops(factionState, "zone_downtown", DefenderTier.Basic, 1)).Returns(true);
            _controller.Show();
            _menuProvider.SimulateItemSelection("zone_downtown");

            // Act
            _menuProvider.SimulateItemSelection(ZoneManagementMenuController.AllocateBasicItemId);

            // Assert
            _allocationServiceMock.Verify(a => a.AllocateTroops(It.IsAny<FactionState>(), "zone_downtown", DefenderTier.Basic, It.IsAny<int>()), Times.Once);
        }

        [Fact]
        public void OnAllocateMedium_ShouldCallAllocationService()
        {
            // Arrange
            var factionState = _factionServiceMock.Object.GetFactionState(PlayerFactionId)!;
            _allocationServiceMock.Setup(a => a.AllocateTroops(factionState, "zone_downtown", DefenderTier.Medium, 1)).Returns(true);
            _controller.Show();
            _menuProvider.SimulateItemSelection("zone_downtown");

            // Act
            _menuProvider.SimulateItemSelection(ZoneManagementMenuController.AllocateMediumItemId);

            // Assert
            _allocationServiceMock.Verify(a => a.AllocateTroops(It.IsAny<FactionState>(), "zone_downtown", DefenderTier.Medium, It.IsAny<int>()), Times.Once);
        }

        [Fact]
        public void OnAllocateHeavy_ShouldCallAllocationService()
        {
            // Arrange
            var factionState = _factionServiceMock.Object.GetFactionState(PlayerFactionId)!;
            _allocationServiceMock.Setup(a => a.AllocateTroops(factionState, "zone_downtown", DefenderTier.Heavy, 1)).Returns(true);
            _controller.Show();
            _menuProvider.SimulateItemSelection("zone_downtown");

            // Act
            _menuProvider.SimulateItemSelection(ZoneManagementMenuController.AllocateHeavyItemId);

            // Assert
            _allocationServiceMock.Verify(a => a.AllocateTroops(It.IsAny<FactionState>(), "zone_downtown", DefenderTier.Heavy, It.IsAny<int>()), Times.Once);
        }

        [Fact]
        public void OnAllocate_WhenNoReserveTroops_ShouldDisableAllocateButton()
        {
            // Arrange
            // After consolidation, don't use initialTroopCount to get truly empty reserves
            var emptyState = new FactionState(PlayerFactionId, 10000);
            // No reserve troops added
            emptyState.AddZone("zone_downtown");
            _factionServiceMock.Setup(f => f.GetFactionState(PlayerFactionId)).Returns(emptyState);
            _controller.Show();

            // Act
            _menuProvider.SimulateItemSelection("zone_downtown");

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            var allocateBasicItem = menu?.GetItem(ZoneManagementMenuController.AllocateBasicItemId);
            Assert.NotNull(allocateBasicItem);
            Assert.False(allocateBasicItem!.IsEnabled);
        }

        #endregion

        #region Withdraw Troops Tests

        [Fact]
        public void OnWithdrawBasic_ShouldCallAllocationService()
        {
            // Arrange
            var allocation = new ZoneDefenderAllocation(PlayerFactionId, "zone_downtown");
            allocation.AddTroops(DefenderTier.Basic, 5);
            _allocationServiceMock.Setup(a => a.GetAllocation(PlayerFactionId, "zone_downtown")).Returns(allocation);
            _allocationServiceMock.Setup(a => a.WithdrawTroops(It.IsAny<FactionState>(), "zone_downtown", DefenderTier.Basic, 1)).Returns(true);
            _controller.Show();
            _menuProvider.SimulateItemSelection("zone_downtown");

            // Act
            _menuProvider.SimulateItemSelection(ZoneManagementMenuController.WithdrawBasicItemId);

            // Assert
            _allocationServiceMock.Verify(a => a.WithdrawTroops(It.IsAny<FactionState>(), "zone_downtown", DefenderTier.Basic, It.IsAny<int>()), Times.Once);
        }

        [Fact]
        public void OnWithdrawMedium_ShouldCallAllocationService()
        {
            // Arrange
            var allocation = new ZoneDefenderAllocation(PlayerFactionId, "zone_downtown");
            allocation.AddTroops(DefenderTier.Medium, 5);
            _allocationServiceMock.Setup(a => a.GetAllocation(PlayerFactionId, "zone_downtown")).Returns(allocation);
            _allocationServiceMock.Setup(a => a.WithdrawTroops(It.IsAny<FactionState>(), "zone_downtown", DefenderTier.Medium, 1)).Returns(true);
            _controller.Show();
            _menuProvider.SimulateItemSelection("zone_downtown");

            // Act
            _menuProvider.SimulateItemSelection(ZoneManagementMenuController.WithdrawMediumItemId);

            // Assert
            _allocationServiceMock.Verify(a => a.WithdrawTroops(It.IsAny<FactionState>(), "zone_downtown", DefenderTier.Medium, It.IsAny<int>()), Times.Once);
        }

        [Fact]
        public void OnWithdrawHeavy_ShouldCallAllocationService()
        {
            // Arrange
            var allocation = new ZoneDefenderAllocation(PlayerFactionId, "zone_downtown");
            allocation.AddTroops(DefenderTier.Heavy, 5);
            _allocationServiceMock.Setup(a => a.GetAllocation(PlayerFactionId, "zone_downtown")).Returns(allocation);
            _allocationServiceMock.Setup(a => a.WithdrawTroops(It.IsAny<FactionState>(), "zone_downtown", DefenderTier.Heavy, 1)).Returns(true);
            _controller.Show();
            _menuProvider.SimulateItemSelection("zone_downtown");

            // Act
            _menuProvider.SimulateItemSelection(ZoneManagementMenuController.WithdrawHeavyItemId);

            // Assert
            _allocationServiceMock.Verify(a => a.WithdrawTroops(It.IsAny<FactionState>(), "zone_downtown", DefenderTier.Heavy, It.IsAny<int>()), Times.Once);
        }

        [Fact]
        public void OnWithdraw_WhenNoAllocatedTroops_ShouldDisableWithdrawButton()
        {
            // Arrange
            _allocationServiceMock.Setup(a => a.GetAllocation(PlayerFactionId, "zone_downtown")).Returns((ZoneDefenderAllocation?)null);
            _controller.Show();

            // Act
            _menuProvider.SimulateItemSelection("zone_downtown");

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            var withdrawBasicItem = menu?.GetItem(ZoneManagementMenuController.WithdrawBasicItemId);
            Assert.NotNull(withdrawBasicItem);
            Assert.False(withdrawBasicItem!.IsEnabled);
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
            var item = menu!.GetItem(ZoneManagementMenuController.BackItemId);
            Assert.NotNull(item);
            Assert.Equal("Back", item!.Text);
        }

        [Fact]
        public void OnBackSelected_FromMainMenu_ShouldRaiseBackRequestedEvent()
        {
            // Arrange
            bool eventRaised = false;
            _controller.BackRequested += (sender, args) => eventRaised = true;
            _controller.Show();

            // Act
            _menuProvider.SimulateItemSelection(ZoneManagementMenuController.BackItemId);

            // Assert
            Assert.True(eventRaised);
        }

        [Fact]
        public void OnBackSelected_FromMainMenu_ShouldCloseMenu()
        {
            // Arrange
            _controller.BackRequested += (sender, args) => { };
            _controller.Show();

            // Act
            _menuProvider.SimulateItemSelection(ZoneManagementMenuController.BackItemId);

            // Assert
            Assert.False(_menuProvider.IsMenuVisible);
        }

        [Fact]
        public void ZoneDetailMenu_ShouldHaveBackItem()
        {
            // Arrange
            _controller.Show();
            _menuProvider.SimulateItemSelection("zone_downtown");

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            Assert.NotNull(menu);
            var item = menu!.GetItem(ZoneManagementMenuController.DetailBackItemId);
            Assert.NotNull(item);
            Assert.Equal("Back", item!.Text);
        }

        [Fact]
        public void OnBackSelected_FromZoneDetail_ShouldReturnToZoneList()
        {
            // Arrange
            _controller.Show();
            _menuProvider.SimulateItemSelection("zone_downtown");

            // Act
            _menuProvider.SimulateItemSelection(ZoneManagementMenuController.DetailBackItemId);

            // Assert
            Assert.True(_menuProvider.IsMenuVisible);
            Assert.Equal(ZoneManagementMenuController.ZoneManagementMenuId, _menuProvider.CurrentMenuId);
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
        public void Show_WhenFactionStateNotFound_ShouldShowEmptyReserves()
        {
            // Arrange
            _factionServiceMock.Setup(f => f.GetFactionState(PlayerFactionId)).Returns((FactionState?)null);

            // Act
            _controller.Show();

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            var reserveItem = menu?.GetItem(ZoneManagementMenuController.ReserveSummaryItemId);
            Assert.NotNull(reserveItem);
            Assert.Contains("0", reserveItem!.Text);
        }

        #endregion

        #region Zone Item Order Tests

        [Fact]
        public void Show_ZonesShouldBeListedBeforeBackButton()
        {
            // Act
            _controller.Show();

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            Assert.NotNull(menu);

            var backIndex = menu!.Items.ToList().FindIndex(i => i.Id == ZoneManagementMenuController.BackItemId);
            var zoneItems = menu.Items.Where(i => i.Id.StartsWith("zone_")).ToList();

            foreach (var zoneItem in zoneItems)
            {
                var zoneIndex = menu.Items.ToList().FindIndex(i => i.Id == zoneItem.Id);
                Assert.True(zoneIndex < backIndex, $"Zone item {zoneItem.Id} should be before back button");
            }
        }

        #endregion

        #region Zone Enabled State Tests

        [Fact]
        public void Show_ZoneItemsShouldBeEnabled()
        {
            // Act
            _controller.Show();

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            Assert.NotNull(menu);

            var zoneItems = menu!.Items.Where(i => i.Id.StartsWith("zone_")).ToList();
            foreach (var item in zoneItems)
            {
                Assert.True(item.IsEnabled, $"Zone item {item.Id} should be enabled");
            }
        }

        [Fact]
        public void Show_BackItemShouldBeEnabled()
        {
            // Act
            _controller.Show();

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            var backItem = menu?.GetItem(ZoneManagementMenuController.BackItemId);
            Assert.True(backItem!.IsEnabled);
        }

        #endregion

        #region Strategic Value Display Tests

        [Fact]
        public void Show_ShouldDisplayStrategicValueInZoneDescription()
        {
            // Act
            _controller.Show();

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            var airportItem = menu?.GetItem("zone_airport");
            Assert.NotNull(airportItem);

            // Airport has strategic value 7
            Assert.Contains("7", airportItem!.Description);
        }

        #endregion
    }
}
