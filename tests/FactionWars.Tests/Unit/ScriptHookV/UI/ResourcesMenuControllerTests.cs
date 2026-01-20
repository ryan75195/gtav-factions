using FactionWars.Core.Interfaces;
using FactionWars.Economy.Interfaces;
using FactionWars.Economy.Models;
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
    /// Tests for ResourcesMenuController displaying income breakdown.
    /// </summary>
    public class ResourcesMenuControllerTests
    {
        private readonly NativeUIMenuProvider _menuProvider;
        private readonly Mock<IFactionService> _factionServiceMock;
        private readonly Mock<IZoneService> _zoneServiceMock;
        private readonly Mock<IPlayerContext> _playerContextMock;
        private readonly Mock<IResourceTickService> _resourceTickServiceMock;
        private readonly Mock<IZoneTraitResourceModifier> _resourceModifierMock;
        private readonly Mock<ISupplyLineService> _supplyLineServiceMock;
        private readonly ResourcesMenuController _controller;

        private const string PlayerFactionId = "michael";
        private const string PlayerFactionName = "De Santa Enterprises";

        public ResourcesMenuControllerTests()
        {
            _menuProvider = new NativeUIMenuProvider();
            _factionServiceMock = new Mock<IFactionService>();
            _zoneServiceMock = new Mock<IZoneService>();
            _playerContextMock = new Mock<IPlayerContext>();
            _resourceTickServiceMock = new Mock<IResourceTickService>();
            _resourceModifierMock = new Mock<IZoneTraitResourceModifier>();
            _supplyLineServiceMock = new Mock<ISupplyLineService>();

            // Setup default player faction
            _playerContextMock.Setup(p => p.CurrentFactionId).Returns(PlayerFactionId);

            // Setup default faction
            var faction = new Faction(PlayerFactionId, PlayerFactionName, "Michael De Santa", "Corporate empire", new FactionColor(0, 0, 255));
            _factionServiceMock.Setup(f => f.GetFaction(PlayerFactionId)).Returns(faction);

            // Setup default faction state
            var factionState = new FactionState(PlayerFactionId, 10000, 50);
            _factionServiceMock.Setup(f => f.GetFactionState(PlayerFactionId)).Returns(factionState);

            // Setup zones owned by player
            var zones = new List<Zone>
            {
                CreateZone("vinewood", "Vinewood", 2, ZoneTrait.Commercial),
                CreateZone("downtown", "Downtown", 1, ZoneTrait.Industrial),
                CreateZone("airport", "Airport", 1, ZoneTrait.None)
            };
            _zoneServiceMock.Setup(z => z.GetZonesByOwner(PlayerFactionId)).Returns(zones);

            // Setup resource tick service
            _resourceTickServiceMock.Setup(r => r.TickIntervalSeconds).Returns(300);
            _resourceTickServiceMock.Setup(r => r.TimeUntilNextTick).Returns(180f);
            _resourceTickServiceMock.Setup(r => r.TickProgress).Returns(40f);
            _resourceTickServiceMock.Setup(r => r.IsRunning).Returns(true);

            // Setup resource modifiers (1.5 for commercial cash, 1.0 otherwise)
            _resourceModifierMock.Setup(m => m.GetModifier(ZoneTrait.Commercial, ResourceType.Cash)).Returns(1.5f);
            _resourceModifierMock.Setup(m => m.GetModifier(ZoneTrait.Commercial, ResourceType.Recruitment)).Returns(1.0f);
            _resourceModifierMock.Setup(m => m.GetModifier(ZoneTrait.Commercial, ResourceType.Weapons)).Returns(1.0f);
            _resourceModifierMock.Setup(m => m.GetModifier(ZoneTrait.Industrial, ResourceType.Cash)).Returns(1.0f);
            _resourceModifierMock.Setup(m => m.GetModifier(ZoneTrait.Industrial, ResourceType.Recruitment)).Returns(1.0f);
            _resourceModifierMock.Setup(m => m.GetModifier(ZoneTrait.Industrial, ResourceType.Weapons)).Returns(1.5f);
            _resourceModifierMock.Setup(m => m.GetModifier(ZoneTrait.None, ResourceType.Cash)).Returns(1.0f);
            _resourceModifierMock.Setup(m => m.GetModifier(ZoneTrait.None, ResourceType.Recruitment)).Returns(1.0f);
            _resourceModifierMock.Setup(m => m.GetModifier(ZoneTrait.None, ResourceType.Weapons)).Returns(1.0f);

            // Setup supply line efficiency (all connected = 1.0)
            _supplyLineServiceMock.Setup(s => s.GetSupplyLineEfficiency(PlayerFactionId, It.IsAny<string>())).Returns(1.0f);

            _controller = new ResourcesMenuController(
                _menuProvider,
                _factionServiceMock.Object,
                _zoneServiceMock.Object,
                _playerContextMock.Object,
                _resourceTickServiceMock.Object,
                _resourceModifierMock.Object,
                _supplyLineServiceMock.Object);
        }

        private Zone CreateZone(string id, string name, int strategicValue, ZoneTrait traits)
        {
            var zone = new Zone(id, name, new Vector3(0, 0, 0), 100f, strategicValue);
            zone.Traits = traits;
            return zone;
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithNullMenuProvider_ShouldThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new ResourcesMenuController(
                null!,
                _factionServiceMock.Object,
                _zoneServiceMock.Object,
                _playerContextMock.Object,
                _resourceTickServiceMock.Object,
                _resourceModifierMock.Object,
                _supplyLineServiceMock.Object));
        }

        [Fact]
        public void Constructor_WithNullFactionService_ShouldThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new ResourcesMenuController(
                _menuProvider,
                null!,
                _zoneServiceMock.Object,
                _playerContextMock.Object,
                _resourceTickServiceMock.Object,
                _resourceModifierMock.Object,
                _supplyLineServiceMock.Object));
        }

        [Fact]
        public void Constructor_WithNullZoneService_ShouldThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new ResourcesMenuController(
                _menuProvider,
                _factionServiceMock.Object,
                null!,
                _playerContextMock.Object,
                _resourceTickServiceMock.Object,
                _resourceModifierMock.Object,
                _supplyLineServiceMock.Object));
        }

        [Fact]
        public void Constructor_WithNullPlayerContext_ShouldThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new ResourcesMenuController(
                _menuProvider,
                _factionServiceMock.Object,
                _zoneServiceMock.Object,
                null!,
                _resourceTickServiceMock.Object,
                _resourceModifierMock.Object,
                _supplyLineServiceMock.Object));
        }

        [Fact]
        public void Constructor_WithNullResourceTickService_ShouldThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new ResourcesMenuController(
                _menuProvider,
                _factionServiceMock.Object,
                _zoneServiceMock.Object,
                _playerContextMock.Object,
                null!,
                _resourceModifierMock.Object,
                _supplyLineServiceMock.Object));
        }

        [Fact]
        public void Constructor_WithNullResourceModifier_ShouldThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new ResourcesMenuController(
                _menuProvider,
                _factionServiceMock.Object,
                _zoneServiceMock.Object,
                _playerContextMock.Object,
                _resourceTickServiceMock.Object,
                null!,
                _supplyLineServiceMock.Object));
        }

        [Fact]
        public void Constructor_WithNullSupplyLineService_ShouldThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new ResourcesMenuController(
                _menuProvider,
                _factionServiceMock.Object,
                _zoneServiceMock.Object,
                _playerContextMock.Object,
                _resourceTickServiceMock.Object,
                _resourceModifierMock.Object,
                null!));
        }

        #endregion

        #region Menu Display Tests

        [Fact]
        public void Show_ShouldOpenResourcesMenu()
        {
            // Act
            _controller.Show();

            // Assert
            Assert.True(_menuProvider.IsMenuVisible);
            Assert.Equal(ResourcesMenuController.ResourcesMenuId, _menuProvider.CurrentMenuId);
        }

        [Fact]
        public void Show_ShouldHaveCorrectTitle()
        {
            // Act
            _controller.Show();

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            Assert.NotNull(menu);
            Assert.Equal("Resources", menu!.Title);
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

        #region Next Tick Display Tests

        [Fact]
        public void Show_ShouldDisplayNextTickItem()
        {
            // Act
            _controller.Show();

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            Assert.NotNull(menu);
            var item = menu!.GetItem(ResourcesMenuController.NextTickItemId);
            Assert.NotNull(item);
            Assert.Contains("Next Tick", item!.Text);
        }

        [Fact]
        public void Show_ShouldDisplayTimeUntilNextTick()
        {
            // Arrange - 180 seconds = 3 minutes
            _resourceTickServiceMock.Setup(r => r.TimeUntilNextTick).Returns(180f);

            // Act
            _controller.Show();

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            var item = menu?.GetItem(ResourcesMenuController.NextTickItemId);
            Assert.NotNull(item);
            Assert.Contains("3:00", item!.Text);
        }

        #endregion

        #region Total Income Display Tests

        [Fact]
        public void Show_ShouldDisplayTotalCashIncomeItem()
        {
            // Act
            _controller.Show();

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            Assert.NotNull(menu);
            var item = menu!.GetItem(ResourcesMenuController.TotalCashItemId);
            Assert.NotNull(item);
            Assert.Contains("Cash", item!.Text);
            Assert.Contains("$", item.Text);
        }

        [Fact]
        public void Show_ShouldDisplayTotalRecruitmentIncomeItem()
        {
            // Act
            _controller.Show();

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            Assert.NotNull(menu);
            var item = menu!.GetItem(ResourcesMenuController.TotalRecruitmentItemId);
            Assert.NotNull(item);
            Assert.Contains("Recruitment", item!.Text);
        }

        [Fact]
        public void Show_ShouldDisplayTotalWeaponsIncomeItem()
        {
            // Act
            _controller.Show();

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            Assert.NotNull(menu);
            var item = menu!.GetItem(ResourcesMenuController.TotalWeaponsItemId);
            Assert.NotNull(item);
            Assert.Contains("Weapons", item!.Text);
        }

        #endregion

        #region Zone Income Breakdown Tests

        [Fact]
        public void Show_ShouldDisplayZoneBreakdownHeader()
        {
            // Act
            _controller.Show();

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            Assert.NotNull(menu);
            var item = menu!.GetItem(ResourcesMenuController.ZoneBreakdownHeaderItemId);
            Assert.NotNull(item);
            Assert.Contains("Zone Breakdown", item!.Text);
        }

        [Fact]
        public void Show_ShouldDisplayZoneIncomeItems()
        {
            // Act
            _controller.Show();

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            Assert.NotNull(menu);

            // Should have items for each zone
            var vinewoodItem = menu!.GetItem("zone_vinewood");
            var downtownItem = menu.GetItem("zone_downtown");
            var airportItem = menu.GetItem("zone_airport");

            Assert.NotNull(vinewoodItem);
            Assert.NotNull(downtownItem);
            Assert.NotNull(airportItem);

            Assert.Contains("Vinewood", vinewoodItem!.Text);
            Assert.Contains("Downtown", downtownItem!.Text);
            Assert.Contains("Airport", airportItem!.Text);
        }

        [Fact]
        public void Show_ZoneItems_ShouldShowCashIncome()
        {
            // Act
            _controller.Show();

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            var vinewoodItem = menu?.GetItem("zone_vinewood");
            Assert.NotNull(vinewoodItem);
            // Vinewood has strategic value 2 with Commercial trait (1.5x cash modifier)
            // Base cash = 100 * 2 = 200, * 1.5 trait = 300
            Assert.Contains("$", vinewoodItem!.Description);
        }

        #endregion

        #region Income Calculation Tests

        [Fact]
        public void Show_ShouldCalculateTotalIncomeFromAllZones()
        {
            // Act
            _controller.Show();

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            var cashItem = menu?.GetItem(ResourcesMenuController.TotalCashItemId);
            Assert.NotNull(cashItem);
            // Total cash should be sum of all zones
            // Verify it shows a positive number
            Assert.Matches(@"\$\d+", cashItem!.Text);
        }

        [Fact]
        public void Show_WhenNoZones_ShouldShowZeroIncome()
        {
            // Arrange
            _zoneServiceMock.Setup(z => z.GetZonesByOwner(PlayerFactionId)).Returns(new List<Zone>());

            // Act
            _controller.Show();

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            var cashItem = menu?.GetItem(ResourcesMenuController.TotalCashItemId);
            Assert.NotNull(cashItem);
            Assert.Contains("$0", cashItem!.Text);
        }

        [Fact]
        public void Show_WhenZoneDisconnected_ShouldShowReducedIncome()
        {
            // Arrange - Downtown is disconnected with 50% efficiency
            _supplyLineServiceMock.Setup(s => s.GetSupplyLineEfficiency(PlayerFactionId, "downtown")).Returns(0.5f);

            // Act
            _controller.Show();

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            var downtownItem = menu?.GetItem("zone_downtown");
            Assert.NotNull(downtownItem);
            // Description should mention reduced efficiency or show reduced income
            Assert.Contains("50%", downtownItem!.Description);
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
            var item = menu!.GetItem(ResourcesMenuController.BackItemId);
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
            _menuProvider.SimulateItemSelection(ResourcesMenuController.BackItemId);

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
            _menuProvider.SimulateItemSelection(ResourcesMenuController.BackItemId);

            // Assert
            Assert.False(_menuProvider.IsMenuVisible);
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

            var nextTickItem = menu!.GetItem(ResourcesMenuController.NextTickItemId);
            var cashItem = menu.GetItem(ResourcesMenuController.TotalCashItemId);
            var recruitmentItem = menu.GetItem(ResourcesMenuController.TotalRecruitmentItemId);
            var weaponsItem = menu.GetItem(ResourcesMenuController.TotalWeaponsItemId);
            var headerItem = menu.GetItem(ResourcesMenuController.ZoneBreakdownHeaderItemId);

            Assert.False(nextTickItem!.IsEnabled);
            Assert.False(cashItem!.IsEnabled);
            Assert.False(recruitmentItem!.IsEnabled);
            Assert.False(weaponsItem!.IsEnabled);
            Assert.False(headerItem!.IsEnabled);
        }

        [Fact]
        public void Show_ZoneItemsShouldBeDisabled()
        {
            // Act
            _controller.Show();

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            var vinewoodItem = menu?.GetItem("zone_vinewood");
            Assert.False(vinewoodItem!.IsEnabled);
        }

        [Fact]
        public void Show_BackItemShouldBeEnabled()
        {
            // Act
            _controller.Show();

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            var backItem = menu?.GetItem(ResourcesMenuController.BackItemId);
            Assert.True(backItem!.IsEnabled);
        }

        #endregion

        #region Null Player Context Tests

        [Fact]
        public void Show_WhenNoPlayerFaction_ShouldStillShowMenu()
        {
            // Arrange
            _playerContextMock.Setup(p => p.CurrentFactionId).Returns((string?)null);
            _zoneServiceMock.Setup(z => z.GetZonesByOwner(It.IsAny<string>())).Returns(new List<Zone>());

            // Act
            _controller.Show();

            // Assert
            Assert.True(_menuProvider.IsMenuVisible);
        }

        [Fact]
        public void Show_WhenNoPlayerFaction_ShouldShowUnknownInSubtitle()
        {
            // Arrange
            _playerContextMock.Setup(p => p.CurrentFactionId).Returns((string?)null);
            _zoneServiceMock.Setup(z => z.GetZonesByOwner(It.IsAny<string>())).Returns(new List<Zone>());

            // Act
            _controller.Show();

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            Assert.Contains("Unknown", menu!.Subtitle);
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

            // Order: Next Tick, Total Cash, Total Recruitment, Total Weapons, Zone Breakdown Header, Zones..., Back
            Assert.Equal(ResourcesMenuController.NextTickItemId, menu!.Items[0].Id);
            Assert.Equal(ResourcesMenuController.TotalCashItemId, menu.Items[1].Id);
            Assert.Equal(ResourcesMenuController.TotalRecruitmentItemId, menu.Items[2].Id);
            Assert.Equal(ResourcesMenuController.TotalWeaponsItemId, menu.Items[3].Id);
            Assert.Equal(ResourcesMenuController.ZoneBreakdownHeaderItemId, menu.Items[4].Id);
            // Zone items follow, then Back is last
            Assert.Equal(ResourcesMenuController.BackItemId, menu.Items[menu.Items.Count - 1].Id);
        }

        #endregion
    }
}
