using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.Economy.Interfaces;
using FactionWars.Economy.Models;
using FactionWars.Factions.Interfaces;
using FactionWars.Factions.Models;
using FactionWars.ScriptHookV.Models;
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
    /// Tests for ZoneManagementMenuController handling zone viewing and buy-and-deploy.
    /// </summary>
    public class ZoneManagementMenuControllerTests
    {
        private readonly MockMenuProvider _menuProvider;
        private readonly Mock<IFactionService> _factionServiceMock;
        private readonly Mock<IZoneService> _zoneServiceMock;
        private readonly Mock<IPlayerContext> _playerContextMock;
        private readonly Mock<IZoneDefenderAllocationService> _allocationServiceMock;
        private readonly Mock<IDefenderDeploymentService> _deploymentServiceMock = new Mock<IDefenderDeploymentService>();
        private readonly ZoneManagementMenuController _controller;

        private const string PlayerFactionId = "michael";
        private const string PlayerFactionName = "De Santa Enterprises";

        private ZoneManagementMenuControllerDependencies Deps() => new ZoneManagementMenuControllerDependencies
        {
            MenuProvider = _menuProvider,
            FactionService = _factionServiceMock.Object,
            ZoneService = _zoneServiceMock.Object,
            PlayerContext = _playerContextMock.Object,
            AllocationService = _allocationServiceMock.Object,
            DeploymentService = _deploymentServiceMock.Object
        };

        public ZoneManagementMenuControllerTests()
        {
            _menuProvider = new MockMenuProvider();
            _factionServiceMock = new Mock<IFactionService>();
            _zoneServiceMock = new Mock<IZoneService>();
            _playerContextMock = new Mock<IPlayerContext>();
            _allocationServiceMock = new Mock<IZoneDefenderAllocationService>();

            // Default deployment service: costs 1000, always affordable
            _deploymentServiceMock.Setup(d => d.GetTroopCost(It.IsAny<DefenderRole>())).Returns(1000);
            _deploymentServiceMock.Setup(d => d.CanAfford(It.IsAny<DefenderRole>(), 1)).Returns(true);

            // Setup default player faction
            _playerContextMock.Setup(p => p.CurrentFactionId).Returns(PlayerFactionId);

            // Setup default faction
            var faction = new Faction(PlayerFactionId, PlayerFactionName, "Michael De Santa", "Corporate empire", new FactionColor(0, 0, 255));
            _factionServiceMock.Setup(f => f.GetFaction(PlayerFactionId)).Returns(faction);

            // Setup default faction state with reserves
            var factionState = new FactionState(PlayerFactionId, 10000);
            factionState.AddReserveTroops(DefenderRole.Grunt, 20);
            factionState.AddReserveTroops(DefenderRole.Gunner, 15);
            factionState.AddReserveTroops(DefenderRole.Rifleman, 10);
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

            _controller = new ZoneManagementMenuController(Deps());
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithNullMenuProvider_ShouldThrowArgumentNullException()
        {
            var deps = Deps();
            deps.MenuProvider = null;
            Assert.Throws<ArgumentNullException>(() => new ZoneManagementMenuController(deps));
        }

        [Fact]
        public void Constructor_WithNullFactionService_ShouldThrowArgumentNullException()
        {
            var deps = Deps();
            deps.FactionService = null;
            Assert.Throws<ArgumentNullException>(() => new ZoneManagementMenuController(deps));
        }

        [Fact]
        public void Constructor_WithNullZoneService_ShouldThrowArgumentNullException()
        {
            var deps = Deps();
            deps.ZoneService = null;
            Assert.Throws<ArgumentNullException>(() => new ZoneManagementMenuController(deps));
        }

        [Fact]
        public void Constructor_WithNullPlayerContext_ShouldThrowArgumentNullException()
        {
            var deps = Deps();
            deps.PlayerContext = null;
            Assert.Throws<ArgumentNullException>(() => new ZoneManagementMenuController(deps));
        }

        [Fact]
        public void Constructor_WithNullAllocationService_ShouldThrowArgumentNullException()
        {
            var deps = Deps();
            deps.AllocationService = null;
            Assert.Throws<ArgumentNullException>(() => new ZoneManagementMenuController(deps));
        }

        [Fact]
        public void Constructor_WithNullDeploymentService_ShouldThrowArgumentNullException()
        {
            var deps = Deps();
            deps.DeploymentService = null;
            Assert.Throws<ArgumentNullException>(() => new ZoneManagementMenuController(deps));
        }

        #endregion

        #region Menu Display Tests

        [Fact]
        public void Show_ShouldOpenZoneManagementMenu()
        {
            _controller.Show();

            Assert.True(_menuProvider.IsMenuVisible);
            Assert.Equal(ZoneManagementMenuController.ZoneManagementMenuId, _menuProvider.CurrentMenuId);
        }

        [Fact]
        public void Show_ShouldHaveCorrectTitle()
        {
            _controller.Show();

            var menu = _menuProvider.GetCurrentMenuDefinition();
            Assert.NotNull(menu);
            Assert.Equal("Zone Management", menu!.Title);
        }

        [Fact]
        public void Show_ShouldHaveFactionNameInSubtitle()
        {
            _controller.Show();

            var menu = _menuProvider.GetCurrentMenuDefinition();
            Assert.NotNull(menu);
            Assert.Contains(PlayerFactionName, menu!.Subtitle);
        }

        #endregion

        #region Zone List Display Tests

        [Fact]
        public void Show_ShouldListAllOwnedZones()
        {
            _controller.Show();

            var menu = _menuProvider.GetCurrentMenuDefinition();
            Assert.NotNull(menu);

            var zoneItems = menu!.Items.Where(i => i.Id.StartsWith("zone_")).ToList();
            Assert.Equal(3, zoneItems.Count);
        }

        [Fact]
        public void Show_ShouldDisplayZoneNamesInItems()
        {
            _controller.Show();

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
        public void Show_ShouldDisplayTotalTroopCountInZoneText()
        {
            // Arrange
            var allocation = new ZoneDefenderAllocation(PlayerFactionId, "zone_downtown");
            allocation.AddTroops(DefenderRole.Grunt, 5);
            allocation.AddTroops(DefenderRole.Gunner, 3);
            allocation.AddTroops(DefenderRole.Rifleman, 2);
            _allocationServiceMock.Setup(a => a.GetAllocation(PlayerFactionId, "zone_downtown")).Returns(allocation);

            _controller.Show();

            var menu = _menuProvider.GetCurrentMenuDefinition();
            var downtownItem = menu?.GetItem("zone_downtown");
            Assert.NotNull(downtownItem);

            // Should show total troops (5+3+2=10) in item text
            Assert.Contains("10", downtownItem!.Text);
        }

        [Fact]
        public void Show_WhenNoAllocation_ShouldShowZeroTroops()
        {
            _allocationServiceMock.Setup(a => a.GetAllocation(PlayerFactionId, "zone_downtown")).Returns((ZoneDefenderAllocation?)null);

            _controller.Show();

            var menu = _menuProvider.GetCurrentMenuDefinition();
            var downtownItem = menu?.GetItem("zone_downtown");
            Assert.NotNull(downtownItem);

            // Should indicate no troops in item text
            Assert.Contains("0", downtownItem!.Text);
        }

        [Fact]
        public void Show_WhenNoOwnedZones_ShouldShowNoZonesMessage()
        {
            _zoneServiceMock.Setup(z => z.GetZonesByOwner(PlayerFactionId)).Returns(new List<Zone>());
            var emptyState = new FactionState(PlayerFactionId, 10000);
            _factionServiceMock.Setup(f => f.GetFactionState(PlayerFactionId)).Returns(emptyState);

            _controller.Show();

            var menu = _menuProvider.GetCurrentMenuDefinition();
            Assert.NotNull(menu);

            var noZonesItem = menu!.GetItem(ZoneManagementMenuController.NoZonesItemId);
            Assert.NotNull(noZonesItem);
            Assert.False(noZonesItem!.IsEnabled);
        }

        #endregion

        #region Zone List Menu Content Tests

        [Fact]
        public void Show_ZoneListMenu_HasNoReserveSummaryItem()
        {
            _controller.Show();

            var menu = _menuProvider.GetCurrentMenuDefinition();
            Assert.NotNull(menu);

            // Reserve summary was removed; zone list should not contain it
            Assert.Null(menu!.GetItem("reserve_summary"));
        }

        [Fact]
        public void ZoneDetailMenu_CurrentAllocationItems_AreDisabled()
        {
            _controller.Show();
            _menuProvider.SimulateItemSelection("zone_downtown");

            var menu = _menuProvider.GetCurrentMenuDefinition();
            Assert.NotNull(menu);

            var basicItem = menu!.GetItem(ZoneManagementMenuController.CurrentBasicItemId);
            var mediumItem = menu.GetItem(ZoneManagementMenuController.CurrentMediumItemId);
            var heavyItem = menu.GetItem(ZoneManagementMenuController.CurrentHeavyItemId);

            Assert.NotNull(basicItem);
            Assert.NotNull(mediumItem);
            Assert.NotNull(heavyItem);

            Assert.False(basicItem!.IsEnabled);
            Assert.False(mediumItem!.IsEnabled);
            Assert.False(heavyItem!.IsEnabled);
        }

        #endregion

        #region Zone Selection Tests

        [Fact]
        public void OnZoneSelected_ShouldShowZoneDetailMenu()
        {
            _controller.Show();

            _menuProvider.SimulateItemSelection("zone_downtown");

            Assert.True(_menuProvider.IsMenuVisible);
            Assert.Equal(ZoneManagementMenuController.ZoneDetailMenuId, _menuProvider.CurrentMenuId);
        }

        [Fact]
        public void ZoneDetailMenu_ShouldShowZoneNameInTitle()
        {
            _controller.Show();

            _menuProvider.SimulateItemSelection("zone_downtown");

            var menu = _menuProvider.GetCurrentMenuDefinition();
            Assert.NotNull(menu);
            Assert.Contains("Downtown", menu!.Title);
        }

        [Fact]
        public void ZoneDetailMenu_ShouldShowCurrentAllocation()
        {
            // Arrange
            var allocation = new ZoneDefenderAllocation(PlayerFactionId, "zone_downtown");
            allocation.AddTroops(DefenderRole.Grunt, 5);
            allocation.AddTroops(DefenderRole.Gunner, 3);
            allocation.AddTroops(DefenderRole.Rifleman, 2);
            _allocationServiceMock.Setup(a => a.GetAllocation(PlayerFactionId, "zone_downtown")).Returns(allocation);
            _controller.Show();

            _menuProvider.SimulateItemSelection("zone_downtown");

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
        public void ZoneDetailMenu_ShouldHaveDeployOptions()
        {
            _controller.Show();

            _menuProvider.SimulateItemSelection("zone_downtown");

            var menu = _menuProvider.GetCurrentMenuDefinition();
            Assert.NotNull(menu);

            Assert.NotNull(menu!.GetItem(ZoneManagementMenuController.DeployBasicItemId));
            Assert.NotNull(menu.GetItem(ZoneManagementMenuController.DeployMediumItemId));
            Assert.NotNull(menu.GetItem(ZoneManagementMenuController.DeployHeavyItemId));
            Assert.NotNull(menu.GetItem(ZoneManagementMenuController.DeployEliteItemId));
            Assert.NotNull(menu.GetItem(ZoneManagementMenuController.DeploySniperItemId));
        }

        [Fact]
        public void ZoneDetailMenu_ShouldShowAllFiveTierAllocations()
        {
            _controller.Show();

            _menuProvider.SimulateItemSelection("zone_downtown");

            var menu = _menuProvider.GetCurrentMenuDefinition();
            Assert.NotNull(menu);

            Assert.NotNull(menu!.GetItem(ZoneManagementMenuController.CurrentBasicItemId));
            Assert.NotNull(menu.GetItem(ZoneManagementMenuController.CurrentMediumItemId));
            Assert.NotNull(menu.GetItem(ZoneManagementMenuController.CurrentHeavyItemId));
            Assert.NotNull(menu.GetItem(ZoneManagementMenuController.CurrentEliteItemId));
            Assert.NotNull(menu.GetItem(ZoneManagementMenuController.CurrentSniperItemId));
        }

        #endregion

        #region Deploy Troops Tests

        [Fact]
        public void OnDeployBasic_ShouldCallBuyAndDeploy()
        {
            _deploymentServiceMock.Setup(d => d.BuyAndDeploy(It.IsAny<FactionState>(), "zone_downtown", DefenderRole.Grunt, 1))
                .Returns(DeploymentResult.Deployed(DefenderRole.Grunt, 1, 1000));
            _controller.Show();
            _menuProvider.SimulateItemSelection("zone_downtown");

            _menuProvider.SimulateItemSelection(ZoneManagementMenuController.DeployBasicItemId);

            _deploymentServiceMock.Verify(d => d.BuyAndDeploy(It.IsAny<FactionState>(), "zone_downtown", DefenderRole.Grunt, It.IsAny<int>()), Times.Once);
        }

        [Fact]
        public void OnDeployMedium_ShouldCallBuyAndDeploy()
        {
            _deploymentServiceMock.Setup(d => d.BuyAndDeploy(It.IsAny<FactionState>(), "zone_downtown", DefenderRole.Gunner, 1))
                .Returns(DeploymentResult.Deployed(DefenderRole.Gunner, 1, 1000));
            _controller.Show();
            _menuProvider.SimulateItemSelection("zone_downtown");

            _menuProvider.SimulateItemSelection(ZoneManagementMenuController.DeployMediumItemId);

            _deploymentServiceMock.Verify(d => d.BuyAndDeploy(It.IsAny<FactionState>(), "zone_downtown", DefenderRole.Gunner, It.IsAny<int>()), Times.Once);
        }

        [Fact]
        public void OnDeployHeavy_ShouldCallBuyAndDeploy()
        {
            _deploymentServiceMock.Setup(d => d.BuyAndDeploy(It.IsAny<FactionState>(), "zone_downtown", DefenderRole.Rifleman, 1))
                .Returns(DeploymentResult.Deployed(DefenderRole.Rifleman, 1, 1000));
            _controller.Show();
            _menuProvider.SimulateItemSelection("zone_downtown");

            _menuProvider.SimulateItemSelection(ZoneManagementMenuController.DeployHeavyItemId);

            _deploymentServiceMock.Verify(d => d.BuyAndDeploy(It.IsAny<FactionState>(), "zone_downtown", DefenderRole.Rifleman, It.IsAny<int>()), Times.Once);
        }

        [Fact]
        public void OnDeploy_WhenUnaffordable_ShouldDisableDeployButton()
        {
            // Override: all tiers unaffordable
            _deploymentServiceMock.Setup(d => d.CanAfford(It.IsAny<DefenderRole>(), 1)).Returns(false);
            _controller.Show();

            _menuProvider.SimulateItemSelection("zone_downtown");

            var menu = _menuProvider.GetCurrentMenuDefinition();
            var deployBasicItem = menu?.GetItem(ZoneManagementMenuController.DeployBasicItemId);
            Assert.NotNull(deployBasicItem);
            Assert.False(deployBasicItem!.IsEnabled);
        }

        #endregion

        #region Additional Deploy Coverage Tests

        [Fact]
        public void OnDeployElite_ShouldCallBuyAndDeploy()
        {
            _deploymentServiceMock.Setup(d => d.BuyAndDeploy(It.IsAny<FactionState>(), "zone_downtown", DefenderRole.Rocketeer, 1))
                .Returns(DeploymentResult.Deployed(DefenderRole.Rocketeer, 1, 2000));
            _controller.Show();
            _menuProvider.SimulateItemSelection("zone_downtown");

            _menuProvider.SimulateItemSelection(ZoneManagementMenuController.DeployEliteItemId);

            _deploymentServiceMock.Verify(d => d.BuyAndDeploy(It.IsAny<FactionState>(), "zone_downtown", DefenderRole.Rocketeer, It.IsAny<int>()), Times.Once);
        }

        [Fact]
        public void OnDeploySniper_ShouldCallBuyAndDeploy()
        {
            _deploymentServiceMock.Setup(d => d.BuyAndDeploy(It.IsAny<FactionState>(), "zone_downtown", DefenderRole.Sniper, 1))
                .Returns(DeploymentResult.Deployed(DefenderRole.Sniper, 1, 3000));
            _controller.Show();
            _menuProvider.SimulateItemSelection("zone_downtown");

            _menuProvider.SimulateItemSelection(ZoneManagementMenuController.DeploySniperItemId);

            _deploymentServiceMock.Verify(d => d.BuyAndDeploy(It.IsAny<FactionState>(), "zone_downtown", DefenderRole.Sniper, It.IsAny<int>()), Times.Once);
        }

        [Fact]
        public void OnDeploy_RefreshesDetailMenu()
        {
            _deploymentServiceMock.Setup(d => d.BuyAndDeploy(It.IsAny<FactionState>(), "zone_downtown", DefenderRole.Grunt, 1))
                .Returns(DeploymentResult.Deployed(DefenderRole.Grunt, 1, 1000));
            _controller.Show();
            _menuProvider.SimulateItemSelection("zone_downtown");

            _menuProvider.SimulateItemSelection(ZoneManagementMenuController.DeployBasicItemId);

            // After deploy, the detail menu should still be showing (refreshed)
            Assert.Equal(ZoneManagementMenuController.ZoneDetailMenuId, _menuProvider.CurrentMenuId);
        }

        [Fact]
        public void ZoneDetailMenu_DeployItems_AreLabeledWithCost()
        {
            _deploymentServiceMock.Setup(d => d.GetTroopCost(DefenderRole.Grunt)).Returns(500);
            _controller.Show();
            _menuProvider.SimulateItemSelection("zone_downtown");

            var menu = _menuProvider.GetCurrentMenuDefinition();
            var deployBasicItem = menu?.GetItem(ZoneManagementMenuController.DeployBasicItemId);
            Assert.NotNull(deployBasicItem);
            Assert.Contains("500", deployBasicItem!.Text);
        }

        #endregion

        #region Navigation Tests

        [Fact]
        public void Show_ShouldHaveBackItem()
        {
            _controller.Show();

            var menu = _menuProvider.GetCurrentMenuDefinition();
            Assert.NotNull(menu);
            var item = menu!.GetItem(ZoneManagementMenuController.BackItemId);
            Assert.NotNull(item);
            Assert.Equal("Back", item!.Text);
        }

        [Fact]
        public void OnBackSelected_FromMainMenu_ShouldRaiseBackRequestedEvent()
        {
            bool eventRaised = false;
            _controller.BackRequested += (sender, args) => eventRaised = true;
            _controller.Show();

            _menuProvider.SimulateItemSelection(ZoneManagementMenuController.BackItemId);

            Assert.True(eventRaised);
        }

        [Fact]
        public void OnBackSelected_FromMainMenu_ShouldCloseMenu()
        {
            _controller.BackRequested += (sender, args) => { };
            _controller.Show();

            _menuProvider.SimulateItemSelection(ZoneManagementMenuController.BackItemId);

            Assert.False(_menuProvider.IsMenuVisible);
        }

        [Fact]
        public void ZoneDetailMenu_ShouldHaveBackItem()
        {
            _controller.Show();
            _menuProvider.SimulateItemSelection("zone_downtown");

            var menu = _menuProvider.GetCurrentMenuDefinition();
            Assert.NotNull(menu);
            var item = menu!.GetItem(ZoneManagementMenuController.DetailBackItemId);
            Assert.NotNull(item);
            Assert.Equal("Back", item!.Text);
        }

        [Fact]
        public void OnBackSelected_FromZoneDetail_ShouldReturnToZoneList()
        {
            _controller.Show();
            _menuProvider.SimulateItemSelection("zone_downtown");

            _menuProvider.SimulateItemSelection(ZoneManagementMenuController.DetailBackItemId);

            Assert.True(_menuProvider.IsMenuVisible);
            Assert.Equal(ZoneManagementMenuController.ZoneManagementMenuId, _menuProvider.CurrentMenuId);
        }

        #endregion

        #region No Player Faction Tests

        [Fact]
        public void Show_WhenNoPlayerFaction_ShouldStillShowMenu()
        {
            _playerContextMock.Setup(p => p.CurrentFactionId).Returns((string?)null);

            _controller.Show();

            Assert.True(_menuProvider.IsMenuVisible);
        }

        [Fact]
        public void ZoneDetailMenu_WhenFactionStateNull_ShowsZeroCurrentAllocation()
        {
            _factionServiceMock.Setup(f => f.GetFactionState(PlayerFactionId)).Returns((FactionState?)null);
            _controller.Show();
            _menuProvider.SimulateItemSelection("zone_downtown");

            var menu = _menuProvider.GetCurrentMenuDefinition();
            var basicItem = menu?.GetItem(ZoneManagementMenuController.CurrentBasicItemId);
            Assert.NotNull(basicItem);
            Assert.Contains("0", basicItem!.Text);
        }

        #endregion

        #region Zone Item Order Tests

        [Fact]
        public void Show_ZonesShouldBeListedBeforeBackButton()
        {
            _controller.Show();

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
            _controller.Show();

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
            _controller.Show();

            var menu = _menuProvider.GetCurrentMenuDefinition();
            var backItem = menu?.GetItem(ZoneManagementMenuController.BackItemId);
            Assert.True(backItem!.IsEnabled);
        }

        #endregion

        #region Strategic Value Display Tests

        [Fact]
        public void Show_ShouldDisplayStrategicValueInZoneDescription()
        {
            _controller.Show();

            var menu = _menuProvider.GetCurrentMenuDefinition();
            var airportItem = menu?.GetItem("zone_airport");
            Assert.NotNull(airportItem);

            // Airport has strategic value 7
            Assert.Contains("7", airportItem!.Description);
        }

        #endregion

        #region Buy And Deploy Tests

        [Fact]
        public void DeployItem_WhenSelected_CallsBuyAndDeployForThatTier()
        {
            _deploymentServiceMock.Setup(d => d.BuyAndDeploy(It.IsAny<FactionState>(), "zone_downtown", DefenderRole.Rifleman, 1))
                .Returns(DeploymentResult.Deployed(DefenderRole.Rifleman, 1, 1000));

            _controller.Show();
            _menuProvider.SimulateItemSelection("zone_downtown");
            _menuProvider.SimulateItemSelection(ZoneManagementMenuController.DeployHeavyItemId);

            _deploymentServiceMock.Verify(d => d.BuyAndDeploy(It.IsAny<FactionState>(), "zone_downtown", DefenderRole.Rifleman, 1), Times.Once);
        }

        [Fact]
        public void DeployItem_WhenUnaffordable_IsDisabledAndShowsCost()
        {
            _deploymentServiceMock.Setup(d => d.GetTroopCost(DefenderRole.Rocketeer)).Returns(2000);
            _deploymentServiceMock.Setup(d => d.CanAfford(DefenderRole.Rocketeer, 1)).Returns(false);

            _controller.Show();
            _menuProvider.SimulateItemSelection("zone_downtown");

            var item = _menuProvider.GetCurrentMenuDefinition()!.Items.Single(i => i.Id == ZoneManagementMenuController.DeployEliteItemId);
            Assert.False(item.IsEnabled);
            Assert.Contains("2000", item.Text);
        }

        [Fact]
        public void ZoneDetailMenu_HasNoWithdrawItems()
        {
            _controller.Show();
            _menuProvider.SimulateItemSelection("zone_downtown");

            Assert.DoesNotContain(_menuProvider.GetCurrentMenuDefinition()!.Items, i => i.Id.StartsWith("withdraw_"));
        }

        #endregion
    }
}
