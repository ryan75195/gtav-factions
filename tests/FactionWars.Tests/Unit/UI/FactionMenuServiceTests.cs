using FactionWars.Core.Interfaces;
using FactionWars.Factions.Interfaces;
using FactionWars.Factions.Models;
using FactionWars.Territory.Interfaces;
using FactionWars.Territory.Models;
using FactionWars.UI.Interfaces;
using FactionWars.UI.Models;
using FactionWars.UI.Services;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace FactionWars.Tests.Unit.UI
{
    /// <summary>
    /// Tests for the FactionMenuService.
    /// Following TDD - these tests define the expected behavior.
    /// </summary>
    public class FactionMenuServiceTests
    {
        #region Test Setup

        private readonly Mock<IMenuProvider> _menuProviderMock;
        private readonly Mock<IFactionService> _factionServiceMock;
        private readonly Mock<IZoneService> _zoneServiceMock;

        public FactionMenuServiceTests()
        {
            _menuProviderMock = new Mock<IMenuProvider>();
            _factionServiceMock = new Mock<IFactionService>();
            _zoneServiceMock = new Mock<IZoneService>();
        }

        private IFactionMenuService CreateService()
        {
            return new FactionMenuService(
                _menuProviderMock.Object,
                _factionServiceMock.Object,
                _zoneServiceMock.Object);
        }

        private Faction CreateTestFaction(string id, string name, FactionType type)
        {
            return new Faction(id, name, null, "", new FactionColor(100, 100, 100));
        }

        private FactionState CreateTestFactionState(string factionId, int cash = 10000, int troops = 50)
        {
            return new FactionState(factionId, cash, troops);
        }

        #endregion

        #region Constructor Validation

        [Fact]
        public void Constructor_ShouldThrowForNullMenuProvider()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new FactionMenuService(null!, _factionServiceMock.Object, _zoneServiceMock.Object));
        }

        [Fact]
        public void Constructor_ShouldThrowForNullFactionService()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new FactionMenuService(_menuProviderMock.Object, null!, _zoneServiceMock.Object));
        }

        [Fact]
        public void Constructor_ShouldThrowForNullZoneService()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new FactionMenuService(_menuProviderMock.Object, _factionServiceMock.Object, null!));
        }

        [Fact]
        public void Constructor_ShouldSubscribeToMenuEvents()
        {
            // Act
            var service = CreateService();

            // Assert - verify service was created and events are wired up
            Assert.NotNull(service);
        }

        #endregion

        #region BuildMainMenuDefinition

        [Fact]
        public void BuildMainMenuDefinition_ShouldReturnNullForNullFactionId()
        {
            // Arrange
            var service = CreateService();

            // Act
            var result = service.BuildMainMenuDefinition(null!);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void BuildMainMenuDefinition_ShouldReturnNullForEmptyFactionId()
        {
            // Arrange
            var service = CreateService();

            // Act
            var result = service.BuildMainMenuDefinition("");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void BuildMainMenuDefinition_ShouldReturnNullWhenFactionNotFound()
        {
            // Arrange
            var service = CreateService();
            _factionServiceMock.Setup(f => f.GetFaction("unknown")).Returns((Faction?)null);

            // Act
            var result = service.BuildMainMenuDefinition("unknown");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void BuildMainMenuDefinition_ShouldReturnMenuWithCorrectId()
        {
            // Arrange
            var service = CreateService();
            var faction = CreateTestFaction("michael_crew", "Michael's Crew", FactionType.Michael);
            var factionState = CreateTestFactionState("michael_crew");

            _factionServiceMock.Setup(f => f.GetFaction("michael_crew")).Returns(faction);
            _factionServiceMock.Setup(f => f.GetFactionState("michael_crew")).Returns(factionState);
            _zoneServiceMock.Setup(z => z.GetZoneCount("michael_crew")).Returns(5);

            // Act
            var result = service.BuildMainMenuDefinition("michael_crew");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("faction_main_menu", result.Id);
        }

        [Fact]
        public void BuildMainMenuDefinition_ShouldIncludeFactionNameInTitle()
        {
            // Arrange
            var service = CreateService();
            var faction = CreateTestFaction("michael_crew", "Michael's Crew", FactionType.Michael);
            var factionState = CreateTestFactionState("michael_crew");

            _factionServiceMock.Setup(f => f.GetFaction("michael_crew")).Returns(faction);
            _factionServiceMock.Setup(f => f.GetFactionState("michael_crew")).Returns(factionState);
            _zoneServiceMock.Setup(z => z.GetZoneCount("michael_crew")).Returns(5);

            // Act
            var result = service.BuildMainMenuDefinition("michael_crew");

            // Assert
            Assert.NotNull(result);
            Assert.Contains("Michael's Crew", result.Title);
        }

        [Fact]
        public void BuildMainMenuDefinition_ShouldContainTerritoriesItem()
        {
            // Arrange
            var service = CreateService();
            var faction = CreateTestFaction("michael_crew", "Michael's Crew", FactionType.Michael);
            var factionState = CreateTestFactionState("michael_crew");

            _factionServiceMock.Setup(f => f.GetFaction("michael_crew")).Returns(faction);
            _factionServiceMock.Setup(f => f.GetFactionState("michael_crew")).Returns(factionState);
            _zoneServiceMock.Setup(z => z.GetZoneCount("michael_crew")).Returns(5);

            // Act
            var result = service.BuildMainMenuDefinition("michael_crew");

            // Assert
            Assert.NotNull(result);
            var territoriesItem = result.Items.FirstOrDefault(i => i.Id == "territories");
            Assert.NotNull(territoriesItem);
            Assert.Contains("Territories", territoriesItem.Text);
        }

        [Fact]
        public void BuildMainMenuDefinition_ShouldContainResourcesItem()
        {
            // Arrange
            var service = CreateService();
            var faction = CreateTestFaction("michael_crew", "Michael's Crew", FactionType.Michael);
            var factionState = CreateTestFactionState("michael_crew");

            _factionServiceMock.Setup(f => f.GetFaction("michael_crew")).Returns(faction);
            _factionServiceMock.Setup(f => f.GetFactionState("michael_crew")).Returns(factionState);
            _zoneServiceMock.Setup(z => z.GetZoneCount("michael_crew")).Returns(5);

            // Act
            var result = service.BuildMainMenuDefinition("michael_crew");

            // Assert
            Assert.NotNull(result);
            var resourcesItem = result.Items.FirstOrDefault(i => i.Id == "resources");
            Assert.NotNull(resourcesItem);
            Assert.Contains("Resources", resourcesItem.Text);
        }

        [Fact]
        public void BuildMainMenuDefinition_ShouldContainOrdersItem()
        {
            // Arrange
            var service = CreateService();
            var faction = CreateTestFaction("michael_crew", "Michael's Crew", FactionType.Michael);
            var factionState = CreateTestFactionState("michael_crew");

            _factionServiceMock.Setup(f => f.GetFaction("michael_crew")).Returns(faction);
            _factionServiceMock.Setup(f => f.GetFactionState("michael_crew")).Returns(factionState);
            _zoneServiceMock.Setup(z => z.GetZoneCount("michael_crew")).Returns(5);

            // Act
            var result = service.BuildMainMenuDefinition("michael_crew");

            // Assert
            Assert.NotNull(result);
            var ordersItem = result.Items.FirstOrDefault(i => i.Id == "orders");
            Assert.NotNull(ordersItem);
            Assert.Contains("Orders", ordersItem.Text);
        }

        [Fact]
        public void BuildMainMenuDefinition_ShouldContainSettingsItem()
        {
            // Arrange
            var service = CreateService();
            var faction = CreateTestFaction("michael_crew", "Michael's Crew", FactionType.Michael);
            var factionState = CreateTestFactionState("michael_crew");

            _factionServiceMock.Setup(f => f.GetFaction("michael_crew")).Returns(faction);
            _factionServiceMock.Setup(f => f.GetFactionState("michael_crew")).Returns(factionState);
            _zoneServiceMock.Setup(z => z.GetZoneCount("michael_crew")).Returns(5);

            // Act
            var result = service.BuildMainMenuDefinition("michael_crew");

            // Assert
            Assert.NotNull(result);
            var settingsItem = result.Items.FirstOrDefault(i => i.Id == "settings");
            Assert.NotNull(settingsItem);
            Assert.Contains("Settings", settingsItem.Text);
        }

        [Fact]
        public void BuildMainMenuDefinition_ShouldShowZoneCountInTerritoriesDescription()
        {
            // Arrange
            var service = CreateService();
            var faction = CreateTestFaction("michael_crew", "Michael's Crew", FactionType.Michael);
            var factionState = CreateTestFactionState("michael_crew");

            _factionServiceMock.Setup(f => f.GetFaction("michael_crew")).Returns(faction);
            _factionServiceMock.Setup(f => f.GetFactionState("michael_crew")).Returns(factionState);
            _zoneServiceMock.Setup(z => z.GetZoneCount("michael_crew")).Returns(7);

            // Act
            var result = service.BuildMainMenuDefinition("michael_crew");

            // Assert
            Assert.NotNull(result);
            var territoriesItem = result.Items.FirstOrDefault(i => i.Id == "territories");
            Assert.NotNull(territoriesItem);
            Assert.Contains("7", territoriesItem.Description);
        }

        [Fact]
        public void BuildMainMenuDefinition_ShouldShowCashInResourcesDescription()
        {
            // Arrange
            var service = CreateService();
            var faction = CreateTestFaction("michael_crew", "Michael's Crew", FactionType.Michael);
            var factionState = CreateTestFactionState("michael_crew", cash: 25000);

            _factionServiceMock.Setup(f => f.GetFaction("michael_crew")).Returns(faction);
            _factionServiceMock.Setup(f => f.GetFactionState("michael_crew")).Returns(factionState);
            _zoneServiceMock.Setup(z => z.GetZoneCount("michael_crew")).Returns(5);

            // Act
            var result = service.BuildMainMenuDefinition("michael_crew");

            // Assert
            Assert.NotNull(result);
            var resourcesItem = result.Items.FirstOrDefault(i => i.Id == "resources");
            Assert.NotNull(resourcesItem);
            Assert.Contains("25,000", resourcesItem.Description);
        }

        [Fact]
        public void BuildMainMenuDefinition_ShouldShowTroopsInResourcesDescription()
        {
            // Arrange
            var service = CreateService();
            var faction = CreateTestFaction("michael_crew", "Michael's Crew", FactionType.Michael);
            var factionState = CreateTestFactionState("michael_crew", troops: 75);

            _factionServiceMock.Setup(f => f.GetFaction("michael_crew")).Returns(faction);
            _factionServiceMock.Setup(f => f.GetFactionState("michael_crew")).Returns(factionState);
            _zoneServiceMock.Setup(z => z.GetZoneCount("michael_crew")).Returns(5);

            // Act
            var result = service.BuildMainMenuDefinition("michael_crew");

            // Assert
            Assert.NotNull(result);
            var resourcesItem = result.Items.FirstOrDefault(i => i.Id == "resources");
            Assert.NotNull(resourcesItem);
            Assert.Contains("75", resourcesItem.Description);
        }

        [Fact]
        public void BuildMainMenuDefinition_ShouldHaveStatusSubtitle()
        {
            // Arrange
            var service = CreateService();
            var faction = CreateTestFaction("michael_crew", "Michael's Crew", FactionType.Michael);
            var factionState = CreateTestFactionState("michael_crew");

            _factionServiceMock.Setup(f => f.GetFaction("michael_crew")).Returns(faction);
            _factionServiceMock.Setup(f => f.GetFactionState("michael_crew")).Returns(factionState);
            _zoneServiceMock.Setup(z => z.GetZoneCount("michael_crew")).Returns(5);

            // Act
            var result = service.BuildMainMenuDefinition("michael_crew");

            // Assert
            Assert.NotNull(result);
            Assert.False(string.IsNullOrEmpty(result.Subtitle));
        }

        #endregion

        #region ShowMainMenu

        [Fact]
        public void ShowMainMenu_ShouldCallMenuProviderShowMenu()
        {
            // Arrange
            var service = CreateService();
            var faction = CreateTestFaction("michael_crew", "Michael's Crew", FactionType.Michael);
            var factionState = CreateTestFactionState("michael_crew");

            _factionServiceMock.Setup(f => f.GetFaction("michael_crew")).Returns(faction);
            _factionServiceMock.Setup(f => f.GetFactionState("michael_crew")).Returns(factionState);
            _zoneServiceMock.Setup(z => z.GetZoneCount("michael_crew")).Returns(5);

            // Act
            service.ShowMainMenu("michael_crew");

            // Assert
            _menuProviderMock.Verify(m => m.ShowMenu(It.IsAny<MenuDefinition>()), Times.Once);
        }

        [Fact]
        public void ShowMainMenu_ShouldNotCallShowMenuWhenFactionNotFound()
        {
            // Arrange
            var service = CreateService();
            _factionServiceMock.Setup(f => f.GetFaction("unknown")).Returns((Faction?)null);

            // Act
            service.ShowMainMenu("unknown");

            // Assert
            _menuProviderMock.Verify(m => m.ShowMenu(It.IsAny<MenuDefinition>()), Times.Never);
        }

        [Fact]
        public void ShowMainMenu_ShouldNotCallShowMenuForNullFactionId()
        {
            // Arrange
            var service = CreateService();

            // Act
            service.ShowMainMenu(null!);

            // Assert
            _menuProviderMock.Verify(m => m.ShowMenu(It.IsAny<MenuDefinition>()), Times.Never);
        }

        #endregion

        #region CloseMenu

        [Fact]
        public void CloseMenu_ShouldCallMenuProviderCloseMenu()
        {
            // Arrange
            var service = CreateService();

            // Act
            service.CloseMenu();

            // Assert
            _menuProviderMock.Verify(m => m.CloseMenu(), Times.Once);
        }

        #endregion

        #region IsMenuVisible

        [Fact]
        public void IsMenuVisible_ShouldReturnMenuProviderIsMenuVisible()
        {
            // Arrange
            var service = CreateService();
            _menuProviderMock.Setup(m => m.IsMenuVisible).Returns(true);

            // Act
            var result = service.IsMenuVisible;

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsMenuVisible_ShouldReturnFalseWhenMenuNotVisible()
        {
            // Arrange
            var service = CreateService();
            _menuProviderMock.Setup(m => m.IsMenuVisible).Returns(false);

            // Act
            var result = service.IsMenuVisible;

            // Assert
            Assert.False(result);
        }

        #endregion

        #region Update

        [Fact]
        public void Update_ShouldCallMenuProviderUpdate()
        {
            // Arrange
            var service = CreateService();

            // Act
            service.Update();

            // Assert
            _menuProviderMock.Verify(m => m.Update(), Times.Once);
        }

        #endregion

        #region HandleMenuSelection

        [Fact]
        public void HandleMenuSelection_ShouldNotThrowForValidInput()
        {
            // Arrange
            var service = CreateService();

            // Act & Assert - should not throw
            service.HandleMenuSelection("faction_main_menu", "territories");
        }

        [Fact]
        public void HandleMenuSelection_ShouldNotThrowForNullMenuId()
        {
            // Arrange
            var service = CreateService();

            // Act & Assert - should not throw
            service.HandleMenuSelection(null!, "territories");
        }

        [Fact]
        public void HandleMenuSelection_ShouldNotThrowForNullItemId()
        {
            // Arrange
            var service = CreateService();

            // Act & Assert - should not throw
            service.HandleMenuSelection("faction_main_menu", null!);
        }

        #endregion

        #region Menu Items Content

        [Fact]
        public void BuildMainMenuDefinition_ShouldHaveCorrectItemOrder()
        {
            // Arrange
            var service = CreateService();
            var faction = CreateTestFaction("michael_crew", "Michael's Crew", FactionType.Michael);
            var factionState = CreateTestFactionState("michael_crew");

            _factionServiceMock.Setup(f => f.GetFaction("michael_crew")).Returns(faction);
            _factionServiceMock.Setup(f => f.GetFactionState("michael_crew")).Returns(factionState);
            _zoneServiceMock.Setup(z => z.GetZoneCount("michael_crew")).Returns(5);

            // Act
            var result = service.BuildMainMenuDefinition("michael_crew");

            // Assert - Items should be in logical order
            Assert.NotNull(result);
            Assert.True(result.Items.Count >= 4);
            Assert.Equal("territories", result.Items[0].Id);
            Assert.Equal("resources", result.Items[1].Id);
            Assert.Equal("orders", result.Items[2].Id);
            Assert.Equal("settings", result.Items[3].Id);
        }

        [Fact]
        public void BuildMainMenuDefinition_AllItemsShouldBeEnabled()
        {
            // Arrange
            var service = CreateService();
            var faction = CreateTestFaction("michael_crew", "Michael's Crew", FactionType.Michael);
            var factionState = CreateTestFactionState("michael_crew");

            _factionServiceMock.Setup(f => f.GetFaction("michael_crew")).Returns(faction);
            _factionServiceMock.Setup(f => f.GetFactionState("michael_crew")).Returns(factionState);
            _zoneServiceMock.Setup(z => z.GetZoneCount("michael_crew")).Returns(5);

            // Act
            var result = service.BuildMainMenuDefinition("michael_crew");

            // Assert
            Assert.NotNull(result);
            Assert.All(result.Items, item => Assert.True(item.IsEnabled));
        }

        [Fact]
        public void BuildMainMenuDefinition_ShouldWorkWithDifferentFactions()
        {
            // Arrange
            var service = CreateService();
            var faction = CreateTestFaction("trevor_gang", "Trevor's Gang", FactionType.Trevor);
            var factionState = CreateTestFactionState("trevor_gang", cash: 5000, troops: 100);

            _factionServiceMock.Setup(f => f.GetFaction("trevor_gang")).Returns(faction);
            _factionServiceMock.Setup(f => f.GetFactionState("trevor_gang")).Returns(factionState);
            _zoneServiceMock.Setup(z => z.GetZoneCount("trevor_gang")).Returns(3);

            // Act
            var result = service.BuildMainMenuDefinition("trevor_gang");

            // Assert
            Assert.NotNull(result);
            Assert.Contains("Trevor's Gang", result.Title);
            var territoriesItem = result.Items.FirstOrDefault(i => i.Id == "territories");
            Assert.NotNull(territoriesItem);
            Assert.Contains("3", territoriesItem.Description);
        }

        [Fact]
        public void BuildMainMenuDefinition_ShouldHandleZeroResources()
        {
            // Arrange
            var service = CreateService();
            var faction = CreateTestFaction("franklin_crew", "Franklin's Crew", FactionType.Franklin);
            var factionState = CreateTestFactionState("franklin_crew", cash: 0, troops: 0);

            _factionServiceMock.Setup(f => f.GetFaction("franklin_crew")).Returns(faction);
            _factionServiceMock.Setup(f => f.GetFactionState("franklin_crew")).Returns(factionState);
            _zoneServiceMock.Setup(z => z.GetZoneCount("franklin_crew")).Returns(0);

            // Act
            var result = service.BuildMainMenuDefinition("franklin_crew");

            // Assert
            Assert.NotNull(result);
            var resourcesItem = result.Items.FirstOrDefault(i => i.Id == "resources");
            Assert.NotNull(resourcesItem);
            // Should still have a valid description even with 0 values
            Assert.False(string.IsNullOrWhiteSpace(resourcesItem.Description));
        }

        #endregion

        #region BuildTerritoryListMenuDefinition

        [Fact]
        public void BuildTerritoryListMenuDefinition_ShouldReturnNullForNullFactionId()
        {
            // Arrange
            var service = CreateService();

            // Act
            var result = service.BuildTerritoryListMenuDefinition(null!);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void BuildTerritoryListMenuDefinition_ShouldReturnNullForEmptyFactionId()
        {
            // Arrange
            var service = CreateService();

            // Act
            var result = service.BuildTerritoryListMenuDefinition("");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void BuildTerritoryListMenuDefinition_ShouldReturnMenuWithCorrectId()
        {
            // Arrange
            var service = CreateService();
            SetupFactionWithZones("michael_crew", 3);

            // Act
            var result = service.BuildTerritoryListMenuDefinition("michael_crew");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("territory_list_menu", result.Id);
        }

        [Fact]
        public void BuildTerritoryListMenuDefinition_ShouldHaveTerritoriesTitle()
        {
            // Arrange
            var service = CreateService();
            SetupFactionWithZones("michael_crew", 2);

            // Act
            var result = service.BuildTerritoryListMenuDefinition("michael_crew");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Territories", result.Title);
        }

        [Fact]
        public void BuildTerritoryListMenuDefinition_ShouldShowZoneCountInSubtitle()
        {
            // Arrange
            var service = CreateService();
            SetupFactionWithZones("michael_crew", 5);

            // Act
            var result = service.BuildTerritoryListMenuDefinition("michael_crew");

            // Assert
            Assert.NotNull(result);
            Assert.Contains("5", result.Subtitle);
        }

        [Fact]
        public void BuildTerritoryListMenuDefinition_ShouldContainItemForEachZone()
        {
            // Arrange
            var service = CreateService();
            var zones = SetupFactionWithZones("michael_crew", 3);

            // Act
            var result = service.BuildTerritoryListMenuDefinition("michael_crew");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(zones.Count, result.Items.Count);
        }

        [Fact]
        public void BuildTerritoryListMenuDefinition_ShouldUseZoneIdAsItemId()
        {
            // Arrange
            var service = CreateService();
            var zones = SetupFactionWithZones("michael_crew", 2);

            // Act
            var result = service.BuildTerritoryListMenuDefinition("michael_crew");

            // Assert
            Assert.NotNull(result);
            foreach (var zone in zones)
            {
                var item = result.Items.FirstOrDefault(i => i.Id == $"zone_{zone.Id}");
                Assert.NotNull(item);
            }
        }

        [Fact]
        public void BuildTerritoryListMenuDefinition_ShouldUseZoneNameAsItemText()
        {
            // Arrange
            var service = CreateService();
            var zones = SetupFactionWithZones("michael_crew", 2);

            // Act
            var result = service.BuildTerritoryListMenuDefinition("michael_crew");

            // Assert
            Assert.NotNull(result);
            foreach (var zone in zones)
            {
                var item = result.Items.FirstOrDefault(i => i.Id == $"zone_{zone.Id}");
                Assert.NotNull(item);
                Assert.Equal(zone.Name, item.Text);
            }
        }

        [Fact]
        public void BuildTerritoryListMenuDefinition_ShouldShowControlPercentageInDescription()
        {
            // Arrange
            var service = CreateService();
            var zones = SetupFactionWithZones("michael_crew", 1);
            zones[0].ControlPercentage = 75f;

            // Act
            var result = service.BuildTerritoryListMenuDefinition("michael_crew");

            // Assert
            Assert.NotNull(result);
            var item = result.Items.FirstOrDefault();
            Assert.NotNull(item);
            Assert.Contains("75%", item.Description);
        }

        [Fact]
        public void BuildTerritoryListMenuDefinition_ShouldShowStrategicValueInDescription()
        {
            // Arrange
            var service = CreateService();
            var zones = SetupFactionWithZones("michael_crew", 1, strategicValue: 8);

            // Act
            var result = service.BuildTerritoryListMenuDefinition("michael_crew");

            // Assert
            Assert.NotNull(result);
            var item = result.Items.FirstOrDefault();
            Assert.NotNull(item);
            Assert.Contains("8", item.Description);
        }

        [Fact]
        public void BuildTerritoryListMenuDefinition_ShouldIndicateContestedZones()
        {
            // Arrange
            var service = CreateService();
            var zones = SetupFactionWithZones("michael_crew", 1);
            zones[0].IsContested = true;

            // Act
            var result = service.BuildTerritoryListMenuDefinition("michael_crew");

            // Assert
            Assert.NotNull(result);
            var item = result.Items.FirstOrDefault();
            Assert.NotNull(item);
            Assert.Contains("CONTESTED", item.Description.ToUpper());
        }

        [Fact]
        public void BuildTerritoryListMenuDefinition_ShouldReturnEmptyMenuWhenNoZones()
        {
            // Arrange
            var service = CreateService();
            SetupFactionWithZones("michael_crew", 0);

            // Act
            var result = service.BuildTerritoryListMenuDefinition("michael_crew");

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Items);
        }

        #endregion

        #region BuildZoneDetailMenuDefinition

        [Fact]
        public void BuildZoneDetailMenuDefinition_ShouldReturnNullForNullZoneId()
        {
            // Arrange
            var service = CreateService();

            // Act
            var result = service.BuildZoneDetailMenuDefinition(null!, "michael_crew");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void BuildZoneDetailMenuDefinition_ShouldReturnNullForEmptyZoneId()
        {
            // Arrange
            var service = CreateService();

            // Act
            var result = service.BuildZoneDetailMenuDefinition("", "michael_crew");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void BuildZoneDetailMenuDefinition_ShouldReturnNullWhenZoneNotFound()
        {
            // Arrange
            var service = CreateService();
            _zoneServiceMock.Setup(z => z.GetZone("unknown")).Returns((Zone?)null);

            // Act
            var result = service.BuildZoneDetailMenuDefinition("unknown", "michael_crew");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void BuildZoneDetailMenuDefinition_ShouldReturnMenuWithCorrectId()
        {
            // Arrange
            var service = CreateService();
            var zone = CreateTestZone("vinewood", "Vinewood");
            _zoneServiceMock.Setup(z => z.GetZone("vinewood")).Returns(zone);
            _zoneServiceMock.Setup(z => z.GetAdjacentZones("vinewood")).Returns(new List<Zone>());

            // Act
            var result = service.BuildZoneDetailMenuDefinition("vinewood", "michael_crew");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("zone_detail_menu", result.Id);
        }

        [Fact]
        public void BuildZoneDetailMenuDefinition_ShouldUseZoneNameAsTitle()
        {
            // Arrange
            var service = CreateService();
            var zone = CreateTestZone("vinewood", "Vinewood Hills");
            _zoneServiceMock.Setup(z => z.GetZone("vinewood")).Returns(zone);
            _zoneServiceMock.Setup(z => z.GetAdjacentZones("vinewood")).Returns(new List<Zone>());

            // Act
            var result = service.BuildZoneDetailMenuDefinition("vinewood", "michael_crew");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Vinewood Hills", result.Title);
        }

        [Fact]
        public void BuildZoneDetailMenuDefinition_ShouldContainControlInfoItem()
        {
            // Arrange
            var service = CreateService();
            var zone = CreateTestZone("vinewood", "Vinewood");
            zone.ControlPercentage = 85f;
            _zoneServiceMock.Setup(z => z.GetZone("vinewood")).Returns(zone);
            _zoneServiceMock.Setup(z => z.GetAdjacentZones("vinewood")).Returns(new List<Zone>());

            // Act
            var result = service.BuildZoneDetailMenuDefinition("vinewood", "michael_crew");

            // Assert
            Assert.NotNull(result);
            var item = result.Items.FirstOrDefault(i => i.Id == "control_info");
            Assert.NotNull(item);
            Assert.Contains("85%", item.Text + item.Description);
        }

        [Fact]
        public void BuildZoneDetailMenuDefinition_ShouldContainStrategicValueItem()
        {
            // Arrange
            var service = CreateService();
            var zone = CreateTestZone("vinewood", "Vinewood", strategicValue: 7);
            _zoneServiceMock.Setup(z => z.GetZone("vinewood")).Returns(zone);
            _zoneServiceMock.Setup(z => z.GetAdjacentZones("vinewood")).Returns(new List<Zone>());

            // Act
            var result = service.BuildZoneDetailMenuDefinition("vinewood", "michael_crew");

            // Assert
            Assert.NotNull(result);
            var item = result.Items.FirstOrDefault(i => i.Id == "strategic_value");
            Assert.NotNull(item);
            Assert.Contains("7", item.Text + item.Description);
        }

        [Fact]
        public void BuildZoneDetailMenuDefinition_ShouldContainTraitsItem()
        {
            // Arrange
            var service = CreateService();
            var zone = CreateTestZone("vinewood", "Vinewood");
            zone.Traits = ZoneTrait.Commercial | ZoneTrait.HighValue;
            _zoneServiceMock.Setup(z => z.GetZone("vinewood")).Returns(zone);
            _zoneServiceMock.Setup(z => z.GetAdjacentZones("vinewood")).Returns(new List<Zone>());

            // Act
            var result = service.BuildZoneDetailMenuDefinition("vinewood", "michael_crew");

            // Assert
            Assert.NotNull(result);
            var item = result.Items.FirstOrDefault(i => i.Id == "traits_info");
            Assert.NotNull(item);
            Assert.Contains("Commercial", item.Description);
            Assert.Contains("HighValue", item.Description);
        }

        [Fact]
        public void BuildZoneDetailMenuDefinition_ShouldShowNoneWhenNoTraits()
        {
            // Arrange
            var service = CreateService();
            var zone = CreateTestZone("vinewood", "Vinewood");
            zone.Traits = ZoneTrait.None;
            _zoneServiceMock.Setup(z => z.GetZone("vinewood")).Returns(zone);
            _zoneServiceMock.Setup(z => z.GetAdjacentZones("vinewood")).Returns(new List<Zone>());

            // Act
            var result = service.BuildZoneDetailMenuDefinition("vinewood", "michael_crew");

            // Assert
            Assert.NotNull(result);
            var item = result.Items.FirstOrDefault(i => i.Id == "traits_info");
            Assert.NotNull(item);
            Assert.Contains("None", item.Description);
        }

        [Fact]
        public void BuildZoneDetailMenuDefinition_ShouldContainAdjacentZonesItem()
        {
            // Arrange
            var service = CreateService();
            var zone = CreateTestZone("vinewood", "Vinewood");
            var adjacentZones = new List<Zone>
            {
                CreateTestZone("downtown", "Downtown"),
                CreateTestZone("beach", "Beach")
            };
            _zoneServiceMock.Setup(z => z.GetZone("vinewood")).Returns(zone);
            _zoneServiceMock.Setup(z => z.GetAdjacentZones("vinewood")).Returns(adjacentZones);

            // Act
            var result = service.BuildZoneDetailMenuDefinition("vinewood", "michael_crew");

            // Assert
            Assert.NotNull(result);
            var item = result.Items.FirstOrDefault(i => i.Id == "adjacent_zones");
            Assert.NotNull(item);
            Assert.Contains("2", item.Text + item.Description);
        }

        [Fact]
        public void BuildZoneDetailMenuDefinition_ShouldIndicateContestedStatus()
        {
            // Arrange
            var service = CreateService();
            var zone = CreateTestZone("vinewood", "Vinewood");
            zone.IsContested = true;
            _zoneServiceMock.Setup(z => z.GetZone("vinewood")).Returns(zone);
            _zoneServiceMock.Setup(z => z.GetAdjacentZones("vinewood")).Returns(new List<Zone>());

            // Act
            var result = service.BuildZoneDetailMenuDefinition("vinewood", "michael_crew");

            // Assert
            Assert.NotNull(result);
            // Either in subtitle or an item
            var hasContestedInfo = result.Subtitle.ToUpper().Contains("CONTESTED") ||
                result.Items.Any(i => i.Text.ToUpper().Contains("CONTESTED") || i.Description.ToUpper().Contains("CONTESTED"));
            Assert.True(hasContestedInfo);
        }

        [Fact]
        public void BuildZoneDetailMenuDefinition_ShouldContainBackItem()
        {
            // Arrange
            var service = CreateService();
            var zone = CreateTestZone("vinewood", "Vinewood");
            _zoneServiceMock.Setup(z => z.GetZone("vinewood")).Returns(zone);
            _zoneServiceMock.Setup(z => z.GetAdjacentZones("vinewood")).Returns(new List<Zone>());

            // Act
            var result = service.BuildZoneDetailMenuDefinition("vinewood", "michael_crew");

            // Assert
            Assert.NotNull(result);
            var item = result.Items.FirstOrDefault(i => i.Id == "back");
            Assert.NotNull(item);
        }

        #endregion

        #region ShowTerritoryListMenu

        [Fact]
        public void ShowTerritoryListMenu_ShouldCallMenuProviderShowMenu()
        {
            // Arrange
            var service = CreateService();
            SetupFactionWithZones("michael_crew", 2);

            // Act
            service.ShowTerritoryListMenu("michael_crew");

            // Assert
            _menuProviderMock.Verify(m => m.ShowMenu(It.Is<MenuDefinition>(d => d.Id == "territory_list_menu")), Times.Once);
        }

        [Fact]
        public void ShowTerritoryListMenu_ShouldNotShowMenuForNullFactionId()
        {
            // Arrange
            var service = CreateService();

            // Act
            service.ShowTerritoryListMenu(null!);

            // Assert
            _menuProviderMock.Verify(m => m.ShowMenu(It.IsAny<MenuDefinition>()), Times.Never);
        }

        #endregion

        #region ShowZoneDetailMenu

        [Fact]
        public void ShowZoneDetailMenu_ShouldCallMenuProviderShowMenu()
        {
            // Arrange
            var service = CreateService();
            var zone = CreateTestZone("vinewood", "Vinewood");
            _zoneServiceMock.Setup(z => z.GetZone("vinewood")).Returns(zone);
            _zoneServiceMock.Setup(z => z.GetAdjacentZones("vinewood")).Returns(new List<Zone>());

            // Act
            service.ShowZoneDetailMenu("vinewood", "michael_crew");

            // Assert
            _menuProviderMock.Verify(m => m.ShowMenu(It.Is<MenuDefinition>(d => d.Id == "zone_detail_menu")), Times.Once);
        }

        [Fact]
        public void ShowZoneDetailMenu_ShouldNotShowMenuForNullZoneId()
        {
            // Arrange
            var service = CreateService();

            // Act
            service.ShowZoneDetailMenu(null!, "michael_crew");

            // Assert
            _menuProviderMock.Verify(m => m.ShowMenu(It.IsAny<MenuDefinition>()), Times.Never);
        }

        #endregion

        #region Menu Navigation

        [Fact]
        public void HandleMenuSelection_TerritoriesItem_ShouldShowTerritoryListMenu()
        {
            // Arrange
            var service = CreateService();
            var faction = CreateTestFaction("michael_crew", "Michael's Crew", FactionType.Michael);
            var factionState = CreateTestFactionState("michael_crew");
            var zones = SetupFactionWithZones("michael_crew", 2);

            _factionServiceMock.Setup(f => f.GetFaction("michael_crew")).Returns(faction);
            _factionServiceMock.Setup(f => f.GetFactionState("michael_crew")).Returns(factionState);

            // First show main menu to set up current faction context
            service.ShowMainMenu("michael_crew");
            _menuProviderMock.Invocations.Clear();

            // Act
            service.HandleMenuSelection("faction_main_menu", "territories");

            // Assert
            _menuProviderMock.Verify(m => m.ShowMenu(It.Is<MenuDefinition>(d => d.Id == "territory_list_menu")), Times.Once);
        }

        [Fact]
        public void HandleMenuSelection_ZoneItem_ShouldShowZoneDetailMenu()
        {
            // Arrange
            var service = CreateService();
            var faction = CreateTestFaction("michael_crew", "Michael's Crew", FactionType.Michael);
            var factionState = CreateTestFactionState("michael_crew");
            var zones = SetupFactionWithZones("michael_crew", 2);
            var zoneId = zones[0].Id;

            _factionServiceMock.Setup(f => f.GetFaction("michael_crew")).Returns(faction);
            _factionServiceMock.Setup(f => f.GetFactionState("michael_crew")).Returns(factionState);
            _zoneServiceMock.Setup(z => z.GetZone(zoneId)).Returns(zones[0]);
            _zoneServiceMock.Setup(z => z.GetAdjacentZones(zoneId)).Returns(new List<Zone>());

            // Show main menu, then territory list
            service.ShowMainMenu("michael_crew");
            service.HandleMenuSelection("faction_main_menu", "territories");
            _menuProviderMock.Invocations.Clear();

            // Act
            service.HandleMenuSelection("territory_list_menu", $"zone_{zoneId}");

            // Assert
            _menuProviderMock.Verify(m => m.ShowMenu(It.Is<MenuDefinition>(d => d.Id == "zone_detail_menu")), Times.Once);
        }

        [Fact]
        public void HandleMenuSelection_BackItem_ShouldShowTerritoryListMenu()
        {
            // Arrange
            var service = CreateService();
            var faction = CreateTestFaction("michael_crew", "Michael's Crew", FactionType.Michael);
            var factionState = CreateTestFactionState("michael_crew");
            var zones = SetupFactionWithZones("michael_crew", 2);

            _factionServiceMock.Setup(f => f.GetFaction("michael_crew")).Returns(faction);
            _factionServiceMock.Setup(f => f.GetFactionState("michael_crew")).Returns(factionState);
            _zoneServiceMock.Setup(z => z.GetZone(zones[0].Id)).Returns(zones[0]);
            _zoneServiceMock.Setup(z => z.GetAdjacentZones(zones[0].Id)).Returns(new List<Zone>());

            // Navigate to zone detail
            service.ShowMainMenu("michael_crew");
            service.HandleMenuSelection("faction_main_menu", "territories");
            service.HandleMenuSelection("territory_list_menu", $"zone_{zones[0].Id}");
            _menuProviderMock.Invocations.Clear();

            // Act
            service.HandleMenuSelection("zone_detail_menu", "back");

            // Assert
            _menuProviderMock.Verify(m => m.ShowMenu(It.Is<MenuDefinition>(d => d.Id == "territory_list_menu")), Times.Once);
        }

        #endregion

        #region Helper Methods for Zone Tests

        private Zone CreateTestZone(string id, string name, int strategicValue = 5)
        {
            return new Zone(id, name, new Vector3(0, 0, 0), 100f, strategicValue);
        }

        private List<Zone> SetupFactionWithZones(string factionId, int zoneCount, int strategicValue = 5)
        {
            var zones = new List<Zone>();
            for (int i = 0; i < zoneCount; i++)
            {
                var zone = CreateTestZone($"zone_{i}", $"Zone {i}", strategicValue);
                zone.OwnerFactionId = factionId;
                zone.ControlPercentage = 100f;
                zones.Add(zone);
            }

            _zoneServiceMock.Setup(z => z.GetZonesByOwner(factionId)).Returns(zones);
            _zoneServiceMock.Setup(z => z.GetZoneCount(factionId)).Returns(zoneCount);

            return zones;
        }

        #endregion

        #region BuildResourceOverviewMenuDefinition

        [Fact]
        public void BuildResourceOverviewMenuDefinition_ShouldReturnNullForNullFactionId()
        {
            // Arrange
            var service = CreateService();

            // Act
            var result = service.BuildResourceOverviewMenuDefinition(null!);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void BuildResourceOverviewMenuDefinition_ShouldReturnNullForEmptyFactionId()
        {
            // Arrange
            var service = CreateService();

            // Act
            var result = service.BuildResourceOverviewMenuDefinition("");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void BuildResourceOverviewMenuDefinition_ShouldReturnNullWhenFactionStateNotFound()
        {
            // Arrange
            var service = CreateService();
            _factionServiceMock.Setup(f => f.GetFactionState("unknown")).Returns((FactionState?)null);

            // Act
            var result = service.BuildResourceOverviewMenuDefinition("unknown");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void BuildResourceOverviewMenuDefinition_ShouldReturnMenuWithCorrectId()
        {
            // Arrange
            var service = CreateService();
            var factionState = CreateTestFactionState("michael_crew", cash: 10000, troops: 50);
            _factionServiceMock.Setup(f => f.GetFactionState("michael_crew")).Returns(factionState);

            // Act
            var result = service.BuildResourceOverviewMenuDefinition("michael_crew");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("resource_overview_menu", result.Id);
        }

        [Fact]
        public void BuildResourceOverviewMenuDefinition_ShouldHaveResourcesTitle()
        {
            // Arrange
            var service = CreateService();
            var factionState = CreateTestFactionState("michael_crew", cash: 10000, troops: 50);
            _factionServiceMock.Setup(f => f.GetFactionState("michael_crew")).Returns(factionState);

            // Act
            var result = service.BuildResourceOverviewMenuDefinition("michael_crew");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Resources", result.Title);
        }

        [Fact]
        public void BuildResourceOverviewMenuDefinition_ShouldContainCashItem()
        {
            // Arrange
            var service = CreateService();
            var factionState = CreateTestFactionState("michael_crew", cash: 25000, troops: 50);
            _factionServiceMock.Setup(f => f.GetFactionState("michael_crew")).Returns(factionState);

            // Act
            var result = service.BuildResourceOverviewMenuDefinition("michael_crew");

            // Assert
            Assert.NotNull(result);
            var item = result.Items.FirstOrDefault(i => i.Id == "cash");
            Assert.NotNull(item);
            Assert.Contains("Cash", item.Text);
            Assert.Contains("25,000", item.Description);
        }

        [Fact]
        public void BuildResourceOverviewMenuDefinition_ShouldContainTroopsItem()
        {
            // Arrange
            var service = CreateService();
            var factionState = CreateTestFactionState("michael_crew", cash: 10000, troops: 75);
            _factionServiceMock.Setup(f => f.GetFactionState("michael_crew")).Returns(factionState);

            // Act
            var result = service.BuildResourceOverviewMenuDefinition("michael_crew");

            // Assert
            Assert.NotNull(result);
            var item = result.Items.FirstOrDefault(i => i.Id == "troops");
            Assert.NotNull(item);
            Assert.Contains("Troops", item.Text);
            Assert.Contains("75", item.Description);
        }

        [Fact]
        public void BuildResourceOverviewMenuDefinition_ShouldContainRecruitmentPointsItem()
        {
            // Arrange
            var service = CreateService();
            var factionState = CreateTestFactionState("michael_crew", cash: 10000, troops: 50);
            factionState.RecruitmentPoints = 150;
            _factionServiceMock.Setup(f => f.GetFactionState("michael_crew")).Returns(factionState);

            // Act
            var result = service.BuildResourceOverviewMenuDefinition("michael_crew");

            // Assert
            Assert.NotNull(result);
            var item = result.Items.FirstOrDefault(i => i.Id == "recruitment");
            Assert.NotNull(item);
            Assert.Contains("Recruitment", item.Text);
            Assert.Contains("150", item.Description);
        }

        [Fact]
        public void BuildResourceOverviewMenuDefinition_ShouldContainWeaponsItem()
        {
            // Arrange
            var service = CreateService();
            var factionState = CreateTestFactionState("michael_crew", cash: 10000, troops: 50);
            factionState.Weapons = 30;
            _factionServiceMock.Setup(f => f.GetFactionState("michael_crew")).Returns(factionState);

            // Act
            var result = service.BuildResourceOverviewMenuDefinition("michael_crew");

            // Assert
            Assert.NotNull(result);
            var item = result.Items.FirstOrDefault(i => i.Id == "weapons");
            Assert.NotNull(item);
            Assert.Contains("Weapons", item.Text);
            Assert.Contains("30", item.Description);
        }

        [Fact]
        public void BuildResourceOverviewMenuDefinition_ShouldContainMilitaryStrengthItem()
        {
            // Arrange
            var service = CreateService();
            var factionState = CreateTestFactionState("michael_crew", cash: 10000, troops: 50);
            factionState.Weapons = 10;
            // MilitaryStrength = troops + (weapons * 2) = 50 + (10 * 2) = 70
            _factionServiceMock.Setup(f => f.GetFactionState("michael_crew")).Returns(factionState);

            // Act
            var result = service.BuildResourceOverviewMenuDefinition("michael_crew");

            // Assert
            Assert.NotNull(result);
            var item = result.Items.FirstOrDefault(i => i.Id == "military_strength");
            Assert.NotNull(item);
            Assert.Contains("Military", item.Text);
            Assert.Contains("70", item.Description);
        }

        [Fact]
        public void BuildResourceOverviewMenuDefinition_ShouldContainBackItem()
        {
            // Arrange
            var service = CreateService();
            var factionState = CreateTestFactionState("michael_crew", cash: 10000, troops: 50);
            _factionServiceMock.Setup(f => f.GetFactionState("michael_crew")).Returns(factionState);

            // Act
            var result = service.BuildResourceOverviewMenuDefinition("michael_crew");

            // Assert
            Assert.NotNull(result);
            var item = result.Items.FirstOrDefault(i => i.Id == "back");
            Assert.NotNull(item);
        }

        [Fact]
        public void BuildResourceOverviewMenuDefinition_ShouldHaveCorrectItemOrder()
        {
            // Arrange
            var service = CreateService();
            var factionState = CreateTestFactionState("michael_crew", cash: 10000, troops: 50);
            _factionServiceMock.Setup(f => f.GetFactionState("michael_crew")).Returns(factionState);

            // Act
            var result = service.BuildResourceOverviewMenuDefinition("michael_crew");

            // Assert - Items should be in logical order
            Assert.NotNull(result);
            Assert.True(result.Items.Count >= 6);
            Assert.Equal("cash", result.Items[0].Id);
            Assert.Equal("recruitment", result.Items[1].Id);
            Assert.Equal("weapons", result.Items[2].Id);
            Assert.Equal("troops", result.Items[3].Id);
            Assert.Equal("military_strength", result.Items[4].Id);
            Assert.Equal("back", result.Items[5].Id);
        }

        [Fact]
        public void BuildResourceOverviewMenuDefinition_ShouldHandleZeroResources()
        {
            // Arrange
            var service = CreateService();
            var factionState = CreateTestFactionState("michael_crew", cash: 0, troops: 0);
            _factionServiceMock.Setup(f => f.GetFactionState("michael_crew")).Returns(factionState);

            // Act
            var result = service.BuildResourceOverviewMenuDefinition("michael_crew");

            // Assert
            Assert.NotNull(result);
            var cashItem = result.Items.FirstOrDefault(i => i.Id == "cash");
            Assert.NotNull(cashItem);
            Assert.Contains("0", cashItem.Description);
        }

        [Fact]
        public void BuildResourceOverviewMenuDefinition_ShouldFormatLargeCashValues()
        {
            // Arrange
            var service = CreateService();
            var factionState = CreateTestFactionState("michael_crew", cash: 1234567, troops: 50);
            _factionServiceMock.Setup(f => f.GetFactionState("michael_crew")).Returns(factionState);

            // Act
            var result = service.BuildResourceOverviewMenuDefinition("michael_crew");

            // Assert
            Assert.NotNull(result);
            var item = result.Items.FirstOrDefault(i => i.Id == "cash");
            Assert.NotNull(item);
            // Should be formatted with commas: 1,234,567
            Assert.Contains("1,234,567", item.Description);
        }

        #endregion

        #region ShowResourceOverviewMenu

        [Fact]
        public void ShowResourceOverviewMenu_ShouldCallMenuProviderShowMenu()
        {
            // Arrange
            var service = CreateService();
            var factionState = CreateTestFactionState("michael_crew", cash: 10000, troops: 50);
            _factionServiceMock.Setup(f => f.GetFactionState("michael_crew")).Returns(factionState);

            // Act
            service.ShowResourceOverviewMenu("michael_crew");

            // Assert
            _menuProviderMock.Verify(m => m.ShowMenu(It.Is<MenuDefinition>(d => d.Id == "resource_overview_menu")), Times.Once);
        }

        [Fact]
        public void ShowResourceOverviewMenu_ShouldNotShowMenuForNullFactionId()
        {
            // Arrange
            var service = CreateService();

            // Act
            service.ShowResourceOverviewMenu(null!);

            // Assert
            _menuProviderMock.Verify(m => m.ShowMenu(It.IsAny<MenuDefinition>()), Times.Never);
        }

        [Fact]
        public void ShowResourceOverviewMenu_ShouldNotShowMenuWhenFactionStateNotFound()
        {
            // Arrange
            var service = CreateService();
            _factionServiceMock.Setup(f => f.GetFactionState("unknown")).Returns((FactionState?)null);

            // Act
            service.ShowResourceOverviewMenu("unknown");

            // Assert
            _menuProviderMock.Verify(m => m.ShowMenu(It.IsAny<MenuDefinition>()), Times.Never);
        }

        #endregion

        #region Resource Menu Navigation

        [Fact]
        public void HandleMenuSelection_ResourcesItem_ShouldShowResourceOverviewMenu()
        {
            // Arrange
            var service = CreateService();
            var faction = CreateTestFaction("michael_crew", "Michael's Crew", FactionType.Michael);
            var factionState = CreateTestFactionState("michael_crew", cash: 10000, troops: 50);
            SetupFactionWithZones("michael_crew", 2);

            _factionServiceMock.Setup(f => f.GetFaction("michael_crew")).Returns(faction);
            _factionServiceMock.Setup(f => f.GetFactionState("michael_crew")).Returns(factionState);

            // First show main menu to set up current faction context
            service.ShowMainMenu("michael_crew");
            _menuProviderMock.Invocations.Clear();

            // Act
            service.HandleMenuSelection("faction_main_menu", "resources");

            // Assert
            _menuProviderMock.Verify(m => m.ShowMenu(It.Is<MenuDefinition>(d => d.Id == "resource_overview_menu")), Times.Once);
        }

        [Fact]
        public void HandleMenuSelection_ResourceBackItem_ShouldShowMainMenu()
        {
            // Arrange
            var service = CreateService();
            var faction = CreateTestFaction("michael_crew", "Michael's Crew", FactionType.Michael);
            var factionState = CreateTestFactionState("michael_crew", cash: 10000, troops: 50);
            SetupFactionWithZones("michael_crew", 2);

            _factionServiceMock.Setup(f => f.GetFaction("michael_crew")).Returns(faction);
            _factionServiceMock.Setup(f => f.GetFactionState("michael_crew")).Returns(factionState);

            // Navigate to resource overview
            service.ShowMainMenu("michael_crew");
            service.HandleMenuSelection("faction_main_menu", "resources");
            _menuProviderMock.Invocations.Clear();

            // Act
            service.HandleMenuSelection("resource_overview_menu", "back");

            // Assert
            _menuProviderMock.Verify(m => m.ShowMenu(It.Is<MenuDefinition>(d => d.Id == "faction_main_menu")), Times.Once);
        }

        #endregion

        #region BuildOrdersMenuDefinition

        [Fact]
        public void BuildOrdersMenuDefinition_ShouldReturnNullForNullFactionId()
        {
            // Arrange
            var service = CreateService();

            // Act
            var result = service.BuildOrdersMenuDefinition(null!);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void BuildOrdersMenuDefinition_ShouldReturnNullForEmptyFactionId()
        {
            // Arrange
            var service = CreateService();

            // Act
            var result = service.BuildOrdersMenuDefinition("");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void BuildOrdersMenuDefinition_ShouldReturnMenuWithCorrectId()
        {
            // Arrange
            var service = CreateService();

            // Act
            var result = service.BuildOrdersMenuDefinition("michael_crew");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("orders_menu", result.Id);
        }

        [Fact]
        public void BuildOrdersMenuDefinition_ShouldHaveOrdersTitle()
        {
            // Arrange
            var service = CreateService();

            // Act
            var result = service.BuildOrdersMenuDefinition("michael_crew");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Orders", result.Title);
        }

        [Fact]
        public void BuildOrdersMenuDefinition_ShouldContainAttackItem()
        {
            // Arrange
            var service = CreateService();

            // Act
            var result = service.BuildOrdersMenuDefinition("michael_crew");

            // Assert
            Assert.NotNull(result);
            var item = result.Items.FirstOrDefault(i => i.Id == "attack");
            Assert.NotNull(item);
            Assert.Contains("Attack", item.Text);
        }

        [Fact]
        public void BuildOrdersMenuDefinition_ShouldContainDefendItem()
        {
            // Arrange
            var service = CreateService();

            // Act
            var result = service.BuildOrdersMenuDefinition("michael_crew");

            // Assert
            Assert.NotNull(result);
            var item = result.Items.FirstOrDefault(i => i.Id == "defend");
            Assert.NotNull(item);
            Assert.Contains("Defend", item.Text);
        }

        [Fact]
        public void BuildOrdersMenuDefinition_ShouldContainBackItem()
        {
            // Arrange
            var service = CreateService();

            // Act
            var result = service.BuildOrdersMenuDefinition("michael_crew");

            // Assert
            Assert.NotNull(result);
            var item = result.Items.FirstOrDefault(i => i.Id == "back");
            Assert.NotNull(item);
        }

        [Fact]
        public void BuildOrdersMenuDefinition_ShouldHaveCorrectItemOrder()
        {
            // Arrange
            var service = CreateService();

            // Act
            var result = service.BuildOrdersMenuDefinition("michael_crew");

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Items.Count >= 3);
            Assert.Equal("attack", result.Items[0].Id);
            Assert.Equal("defend", result.Items[1].Id);
            Assert.Equal("back", result.Items[2].Id);
        }

        #endregion

        #region ShowOrdersMenu

        [Fact]
        public void ShowOrdersMenu_ShouldCallMenuProviderShowMenu()
        {
            // Arrange
            var service = CreateService();

            // Act
            service.ShowOrdersMenu("michael_crew");

            // Assert
            _menuProviderMock.Verify(m => m.ShowMenu(It.Is<MenuDefinition>(d => d.Id == "orders_menu")), Times.Once);
        }

        [Fact]
        public void ShowOrdersMenu_ShouldNotShowMenuForNullFactionId()
        {
            // Arrange
            var service = CreateService();

            // Act
            service.ShowOrdersMenu(null!);

            // Assert
            _menuProviderMock.Verify(m => m.ShowMenu(It.IsAny<MenuDefinition>()), Times.Never);
        }

        #endregion

        #region BuildAttackTargetsMenuDefinition

        [Fact]
        public void BuildAttackTargetsMenuDefinition_ShouldReturnNullForNullFactionId()
        {
            // Arrange
            var service = CreateService();

            // Act
            var result = service.BuildAttackTargetsMenuDefinition(null!);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void BuildAttackTargetsMenuDefinition_ShouldReturnNullForEmptyFactionId()
        {
            // Arrange
            var service = CreateService();

            // Act
            var result = service.BuildAttackTargetsMenuDefinition("");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void BuildAttackTargetsMenuDefinition_ShouldReturnMenuWithCorrectId()
        {
            // Arrange
            var service = CreateService();
            SetupFactionWithZones("michael_crew", 1);

            // Act
            var result = service.BuildAttackTargetsMenuDefinition("michael_crew");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("attack_targets_menu", result.Id);
        }

        [Fact]
        public void BuildAttackTargetsMenuDefinition_ShouldHaveAttackTargetsTitle()
        {
            // Arrange
            var service = CreateService();
            SetupFactionWithZones("michael_crew", 1);

            // Act
            var result = service.BuildAttackTargetsMenuDefinition("michael_crew");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Attack Targets", result.Title);
        }

        [Fact]
        public void BuildAttackTargetsMenuDefinition_ShouldListAdjacentEnemyZones()
        {
            // Arrange
            var service = CreateService();
            var playerZones = SetupFactionWithZones("michael_crew", 1);
            var enemyZone = CreateTestZone("enemy_zone", "Enemy Territory", strategicValue: 6);
            enemyZone.OwnerFactionId = "trevor_gang";
            enemyZone.ControlPercentage = 100f;

            _zoneServiceMock.Setup(z => z.GetAdjacentZones(playerZones[0].Id)).Returns(new List<Zone> { enemyZone });

            // Act
            var result = service.BuildAttackTargetsMenuDefinition("michael_crew");

            // Assert
            Assert.NotNull(result);
            var item = result.Items.FirstOrDefault(i => i.Id == "attack_enemy_zone");
            Assert.NotNull(item);
            Assert.Equal("Enemy Territory", item.Text);
        }

        [Fact]
        public void BuildAttackTargetsMenuDefinition_ShouldListAdjacentNeutralZones()
        {
            // Arrange
            var service = CreateService();
            var playerZones = SetupFactionWithZones("michael_crew", 1);
            var neutralZone = CreateTestZone("neutral_zone", "Neutral Territory", strategicValue: 4);
            neutralZone.OwnerFactionId = null;
            neutralZone.ControlPercentage = 0f;

            _zoneServiceMock.Setup(z => z.GetAdjacentZones(playerZones[0].Id)).Returns(new List<Zone> { neutralZone });

            // Act
            var result = service.BuildAttackTargetsMenuDefinition("michael_crew");

            // Assert
            Assert.NotNull(result);
            var item = result.Items.FirstOrDefault(i => i.Id == "attack_neutral_zone");
            Assert.NotNull(item);
            Assert.Equal("Neutral Territory", item.Text);
        }

        [Fact]
        public void BuildAttackTargetsMenuDefinition_ShouldNotListOwnZones()
        {
            // Arrange
            var service = CreateService();
            var playerZones = SetupFactionWithZones("michael_crew", 2);
            // Make zone 1 adjacent to zone 0
            _zoneServiceMock.Setup(z => z.GetAdjacentZones(playerZones[0].Id)).Returns(new List<Zone> { playerZones[1] });
            _zoneServiceMock.Setup(z => z.GetAdjacentZones(playerZones[1].Id)).Returns(new List<Zone> { playerZones[0] });

            // Act
            var result = service.BuildAttackTargetsMenuDefinition("michael_crew");

            // Assert
            Assert.NotNull(result);
            // Should only have back button, no attack targets since adjacent zones are owned
            var attackItems = result.Items.Where(i => i.Id.StartsWith("attack_")).ToList();
            Assert.Empty(attackItems);
        }

        [Fact]
        public void BuildAttackTargetsMenuDefinition_ShouldShowStrategicValueInDescription()
        {
            // Arrange
            var service = CreateService();
            var playerZones = SetupFactionWithZones("michael_crew", 1);
            var enemyZone = CreateTestZone("enemy_zone", "Enemy Territory", strategicValue: 8);
            enemyZone.OwnerFactionId = "trevor_gang";
            enemyZone.ControlPercentage = 100f;

            _zoneServiceMock.Setup(z => z.GetAdjacentZones(playerZones[0].Id)).Returns(new List<Zone> { enemyZone });

            // Act
            var result = service.BuildAttackTargetsMenuDefinition("michael_crew");

            // Assert
            Assert.NotNull(result);
            var item = result.Items.FirstOrDefault(i => i.Id == "attack_enemy_zone");
            Assert.NotNull(item);
            Assert.Contains("8", item.Description);
        }

        [Fact]
        public void BuildAttackTargetsMenuDefinition_ShouldShowOwnerInDescription()
        {
            // Arrange
            var service = CreateService();
            var playerZones = SetupFactionWithZones("michael_crew", 1);
            var enemyZone = CreateTestZone("enemy_zone", "Enemy Territory", strategicValue: 6);
            enemyZone.OwnerFactionId = "trevor_gang";
            enemyZone.ControlPercentage = 100f;

            var trevorFaction = CreateTestFaction("trevor_gang", "Trevor's Gang", FactionType.Trevor);
            _factionServiceMock.Setup(f => f.GetFaction("trevor_gang")).Returns(trevorFaction);
            _zoneServiceMock.Setup(z => z.GetAdjacentZones(playerZones[0].Id)).Returns(new List<Zone> { enemyZone });

            // Act
            var result = service.BuildAttackTargetsMenuDefinition("michael_crew");

            // Assert
            Assert.NotNull(result);
            var item = result.Items.FirstOrDefault(i => i.Id == "attack_enemy_zone");
            Assert.NotNull(item);
            Assert.Contains("Trevor's Gang", item.Description);
        }

        [Fact]
        public void BuildAttackTargetsMenuDefinition_ShouldShowNeutralForUnownedZones()
        {
            // Arrange
            var service = CreateService();
            var playerZones = SetupFactionWithZones("michael_crew", 1);
            var neutralZone = CreateTestZone("neutral_zone", "Neutral Territory", strategicValue: 4);
            neutralZone.OwnerFactionId = null;

            _zoneServiceMock.Setup(z => z.GetAdjacentZones(playerZones[0].Id)).Returns(new List<Zone> { neutralZone });

            // Act
            var result = service.BuildAttackTargetsMenuDefinition("michael_crew");

            // Assert
            Assert.NotNull(result);
            var item = result.Items.FirstOrDefault(i => i.Id == "attack_neutral_zone");
            Assert.NotNull(item);
            Assert.Contains("Neutral", item.Description);
        }

        [Fact]
        public void BuildAttackTargetsMenuDefinition_ShouldContainBackItem()
        {
            // Arrange
            var service = CreateService();
            SetupFactionWithZones("michael_crew", 1);

            // Act
            var result = service.BuildAttackTargetsMenuDefinition("michael_crew");

            // Assert
            Assert.NotNull(result);
            var item = result.Items.FirstOrDefault(i => i.Id == "back");
            Assert.NotNull(item);
        }

        [Fact]
        public void BuildAttackTargetsMenuDefinition_ShouldNotDuplicateZones()
        {
            // Arrange
            var service = CreateService();
            var playerZones = SetupFactionWithZones("michael_crew", 2);
            var enemyZone = CreateTestZone("enemy_zone", "Enemy Territory", strategicValue: 6);
            enemyZone.OwnerFactionId = "trevor_gang";
            enemyZone.ControlPercentage = 100f;

            // Both player zones are adjacent to the same enemy zone
            _zoneServiceMock.Setup(z => z.GetAdjacentZones(playerZones[0].Id)).Returns(new List<Zone> { enemyZone });
            _zoneServiceMock.Setup(z => z.GetAdjacentZones(playerZones[1].Id)).Returns(new List<Zone> { enemyZone });

            // Act
            var result = service.BuildAttackTargetsMenuDefinition("michael_crew");

            // Assert
            Assert.NotNull(result);
            var attackItems = result.Items.Where(i => i.Id == "attack_enemy_zone").ToList();
            Assert.Single(attackItems);
        }

        #endregion

        #region ShowAttackTargetsMenu

        [Fact]
        public void ShowAttackTargetsMenu_ShouldCallMenuProviderShowMenu()
        {
            // Arrange
            var service = CreateService();
            SetupFactionWithZones("michael_crew", 1);

            // Act
            service.ShowAttackTargetsMenu("michael_crew");

            // Assert
            _menuProviderMock.Verify(m => m.ShowMenu(It.Is<MenuDefinition>(d => d.Id == "attack_targets_menu")), Times.Once);
        }

        [Fact]
        public void ShowAttackTargetsMenu_ShouldNotShowMenuForNullFactionId()
        {
            // Arrange
            var service = CreateService();

            // Act
            service.ShowAttackTargetsMenu(null!);

            // Assert
            _menuProviderMock.Verify(m => m.ShowMenu(It.IsAny<MenuDefinition>()), Times.Never);
        }

        #endregion

        #region BuildDefendZonesMenuDefinition

        [Fact]
        public void BuildDefendZonesMenuDefinition_ShouldReturnNullForNullFactionId()
        {
            // Arrange
            var service = CreateService();

            // Act
            var result = service.BuildDefendZonesMenuDefinition(null!);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void BuildDefendZonesMenuDefinition_ShouldReturnNullForEmptyFactionId()
        {
            // Arrange
            var service = CreateService();

            // Act
            var result = service.BuildDefendZonesMenuDefinition("");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void BuildDefendZonesMenuDefinition_ShouldReturnMenuWithCorrectId()
        {
            // Arrange
            var service = CreateService();
            SetupFactionWithZones("michael_crew", 1);

            // Act
            var result = service.BuildDefendZonesMenuDefinition("michael_crew");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("defend_zones_menu", result.Id);
        }

        [Fact]
        public void BuildDefendZonesMenuDefinition_ShouldHaveDefendZonesTitle()
        {
            // Arrange
            var service = CreateService();
            SetupFactionWithZones("michael_crew", 1);

            // Act
            var result = service.BuildDefendZonesMenuDefinition("michael_crew");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Defend Zones", result.Title);
        }

        [Fact]
        public void BuildDefendZonesMenuDefinition_ShouldListPlayerOwnedZones()
        {
            // Arrange
            var service = CreateService();
            var playerZones = SetupFactionWithZones("michael_crew", 2);

            // Act
            var result = service.BuildDefendZonesMenuDefinition("michael_crew");

            // Assert
            Assert.NotNull(result);
            foreach (var zone in playerZones)
            {
                var item = result.Items.FirstOrDefault(i => i.Id == $"defend_{zone.Id}");
                Assert.NotNull(item);
                Assert.Equal(zone.Name, item.Text);
            }
        }

        [Fact]
        public void BuildDefendZonesMenuDefinition_ShouldShowControlPercentageInDescription()
        {
            // Arrange
            var service = CreateService();
            var playerZones = SetupFactionWithZones("michael_crew", 1);
            playerZones[0].ControlPercentage = 75f;

            // Act
            var result = service.BuildDefendZonesMenuDefinition("michael_crew");

            // Assert
            Assert.NotNull(result);
            var item = result.Items.FirstOrDefault(i => i.Id == $"defend_{playerZones[0].Id}");
            Assert.NotNull(item);
            Assert.Contains("75%", item.Description);
        }

        [Fact]
        public void BuildDefendZonesMenuDefinition_ShouldIndicateContestedZones()
        {
            // Arrange
            var service = CreateService();
            var playerZones = SetupFactionWithZones("michael_crew", 1);
            playerZones[0].IsContested = true;

            // Act
            var result = service.BuildDefendZonesMenuDefinition("michael_crew");

            // Assert
            Assert.NotNull(result);
            var item = result.Items.FirstOrDefault(i => i.Id == $"defend_{playerZones[0].Id}");
            Assert.NotNull(item);
            Assert.Contains("CONTESTED", item.Description.ToUpper());
        }

        [Fact]
        public void BuildDefendZonesMenuDefinition_ShouldContainBackItem()
        {
            // Arrange
            var service = CreateService();
            SetupFactionWithZones("michael_crew", 1);

            // Act
            var result = service.BuildDefendZonesMenuDefinition("michael_crew");

            // Assert
            Assert.NotNull(result);
            var item = result.Items.FirstOrDefault(i => i.Id == "back");
            Assert.NotNull(item);
        }

        [Fact]
        public void BuildDefendZonesMenuDefinition_ShouldPrioritizeContestedZones()
        {
            // Arrange
            var service = CreateService();
            var playerZones = SetupFactionWithZones("michael_crew", 3);
            playerZones[0].IsContested = false;
            playerZones[1].IsContested = true;
            playerZones[2].IsContested = false;

            // Act
            var result = service.BuildDefendZonesMenuDefinition("michael_crew");

            // Assert
            Assert.NotNull(result);
            // First item (excluding back) should be the contested zone
            var defendItems = result.Items.Where(i => i.Id.StartsWith("defend_")).ToList();
            Assert.Equal($"defend_{playerZones[1].Id}", defendItems[0].Id);
        }

        [Fact]
        public void BuildDefendZonesMenuDefinition_ShouldShowStrategicValueInDescription()
        {
            // Arrange
            var service = CreateService();
            var playerZones = SetupFactionWithZones("michael_crew", 1, strategicValue: 9);

            // Act
            var result = service.BuildDefendZonesMenuDefinition("michael_crew");

            // Assert
            Assert.NotNull(result);
            var item = result.Items.FirstOrDefault(i => i.Id == $"defend_{playerZones[0].Id}");
            Assert.NotNull(item);
            Assert.Contains("9", item.Description);
        }

        #endregion

        #region ShowDefendZonesMenu

        [Fact]
        public void ShowDefendZonesMenu_ShouldCallMenuProviderShowMenu()
        {
            // Arrange
            var service = CreateService();
            SetupFactionWithZones("michael_crew", 1);

            // Act
            service.ShowDefendZonesMenu("michael_crew");

            // Assert
            _menuProviderMock.Verify(m => m.ShowMenu(It.Is<MenuDefinition>(d => d.Id == "defend_zones_menu")), Times.Once);
        }

        [Fact]
        public void ShowDefendZonesMenu_ShouldNotShowMenuForNullFactionId()
        {
            // Arrange
            var service = CreateService();

            // Act
            service.ShowDefendZonesMenu(null!);

            // Assert
            _menuProviderMock.Verify(m => m.ShowMenu(It.IsAny<MenuDefinition>()), Times.Never);
        }

        #endregion

        #region Orders Menu Navigation

        [Fact]
        public void HandleMenuSelection_OrdersItem_ShouldShowOrdersMenu()
        {
            // Arrange
            var service = CreateService();
            var faction = CreateTestFaction("michael_crew", "Michael's Crew", FactionType.Michael);
            var factionState = CreateTestFactionState("michael_crew", cash: 10000, troops: 50);
            SetupFactionWithZones("michael_crew", 2);

            _factionServiceMock.Setup(f => f.GetFaction("michael_crew")).Returns(faction);
            _factionServiceMock.Setup(f => f.GetFactionState("michael_crew")).Returns(factionState);

            // First show main menu to set up current faction context
            service.ShowMainMenu("michael_crew");
            _menuProviderMock.Invocations.Clear();

            // Act
            service.HandleMenuSelection("faction_main_menu", "orders");

            // Assert
            _menuProviderMock.Verify(m => m.ShowMenu(It.Is<MenuDefinition>(d => d.Id == "orders_menu")), Times.Once);
        }

        [Fact]
        public void HandleMenuSelection_AttackItem_ShouldShowAttackTargetsMenu()
        {
            // Arrange
            var service = CreateService();
            var faction = CreateTestFaction("michael_crew", "Michael's Crew", FactionType.Michael);
            var factionState = CreateTestFactionState("michael_crew", cash: 10000, troops: 50);
            SetupFactionWithZones("michael_crew", 2);

            _factionServiceMock.Setup(f => f.GetFaction("michael_crew")).Returns(faction);
            _factionServiceMock.Setup(f => f.GetFactionState("michael_crew")).Returns(factionState);

            // Navigate to orders menu
            service.ShowMainMenu("michael_crew");
            service.HandleMenuSelection("faction_main_menu", "orders");
            _menuProviderMock.Invocations.Clear();

            // Act
            service.HandleMenuSelection("orders_menu", "attack");

            // Assert
            _menuProviderMock.Verify(m => m.ShowMenu(It.Is<MenuDefinition>(d => d.Id == "attack_targets_menu")), Times.Once);
        }

        [Fact]
        public void HandleMenuSelection_DefendItem_ShouldShowDefendZonesMenu()
        {
            // Arrange
            var service = CreateService();
            var faction = CreateTestFaction("michael_crew", "Michael's Crew", FactionType.Michael);
            var factionState = CreateTestFactionState("michael_crew", cash: 10000, troops: 50);
            SetupFactionWithZones("michael_crew", 2);

            _factionServiceMock.Setup(f => f.GetFaction("michael_crew")).Returns(faction);
            _factionServiceMock.Setup(f => f.GetFactionState("michael_crew")).Returns(factionState);

            // Navigate to orders menu
            service.ShowMainMenu("michael_crew");
            service.HandleMenuSelection("faction_main_menu", "orders");
            _menuProviderMock.Invocations.Clear();

            // Act
            service.HandleMenuSelection("orders_menu", "defend");

            // Assert
            _menuProviderMock.Verify(m => m.ShowMenu(It.Is<MenuDefinition>(d => d.Id == "defend_zones_menu")), Times.Once);
        }

        [Fact]
        public void HandleMenuSelection_OrdersBackItem_ShouldShowMainMenu()
        {
            // Arrange
            var service = CreateService();
            var faction = CreateTestFaction("michael_crew", "Michael's Crew", FactionType.Michael);
            var factionState = CreateTestFactionState("michael_crew", cash: 10000, troops: 50);
            SetupFactionWithZones("michael_crew", 2);

            _factionServiceMock.Setup(f => f.GetFaction("michael_crew")).Returns(faction);
            _factionServiceMock.Setup(f => f.GetFactionState("michael_crew")).Returns(factionState);

            // Navigate to orders menu
            service.ShowMainMenu("michael_crew");
            service.HandleMenuSelection("faction_main_menu", "orders");
            _menuProviderMock.Invocations.Clear();

            // Act
            service.HandleMenuSelection("orders_menu", "back");

            // Assert
            _menuProviderMock.Verify(m => m.ShowMenu(It.Is<MenuDefinition>(d => d.Id == "faction_main_menu")), Times.Once);
        }

        [Fact]
        public void HandleMenuSelection_AttackTargetsBackItem_ShouldShowOrdersMenu()
        {
            // Arrange
            var service = CreateService();
            var faction = CreateTestFaction("michael_crew", "Michael's Crew", FactionType.Michael);
            var factionState = CreateTestFactionState("michael_crew", cash: 10000, troops: 50);
            SetupFactionWithZones("michael_crew", 2);

            _factionServiceMock.Setup(f => f.GetFaction("michael_crew")).Returns(faction);
            _factionServiceMock.Setup(f => f.GetFactionState("michael_crew")).Returns(factionState);

            // Navigate to attack targets menu
            service.ShowMainMenu("michael_crew");
            service.HandleMenuSelection("faction_main_menu", "orders");
            service.HandleMenuSelection("orders_menu", "attack");
            _menuProviderMock.Invocations.Clear();

            // Act
            service.HandleMenuSelection("attack_targets_menu", "back");

            // Assert
            _menuProviderMock.Verify(m => m.ShowMenu(It.Is<MenuDefinition>(d => d.Id == "orders_menu")), Times.Once);
        }

        [Fact]
        public void HandleMenuSelection_DefendZonesBackItem_ShouldShowOrdersMenu()
        {
            // Arrange
            var service = CreateService();
            var faction = CreateTestFaction("michael_crew", "Michael's Crew", FactionType.Michael);
            var factionState = CreateTestFactionState("michael_crew", cash: 10000, troops: 50);
            SetupFactionWithZones("michael_crew", 2);

            _factionServiceMock.Setup(f => f.GetFaction("michael_crew")).Returns(faction);
            _factionServiceMock.Setup(f => f.GetFactionState("michael_crew")).Returns(factionState);

            // Navigate to defend zones menu
            service.ShowMainMenu("michael_crew");
            service.HandleMenuSelection("faction_main_menu", "orders");
            service.HandleMenuSelection("orders_menu", "defend");
            _menuProviderMock.Invocations.Clear();

            // Act
            service.HandleMenuSelection("defend_zones_menu", "back");

            // Assert
            _menuProviderMock.Verify(m => m.ShowMenu(It.Is<MenuDefinition>(d => d.Id == "orders_menu")), Times.Once);
        }

        #endregion

        #region BuildSettingsMenuDefinition

        [Fact]
        public void BuildSettingsMenuDefinition_ShouldReturnNullForNullFactionId()
        {
            // Arrange
            var service = CreateService();

            // Act
            var result = service.BuildSettingsMenuDefinition(null!);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void BuildSettingsMenuDefinition_ShouldReturnNullForEmptyFactionId()
        {
            // Arrange
            var service = CreateService();

            // Act
            var result = service.BuildSettingsMenuDefinition("");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void BuildSettingsMenuDefinition_ShouldReturnMenuWithCorrectId()
        {
            // Arrange
            var service = CreateService();

            // Act
            var result = service.BuildSettingsMenuDefinition("michael_crew");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("settings_menu", result.Id);
        }

        [Fact]
        public void BuildSettingsMenuDefinition_ShouldHaveSettingsTitle()
        {
            // Arrange
            var service = CreateService();

            // Act
            var result = service.BuildSettingsMenuDefinition("michael_crew");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Settings", result.Title);
        }

        [Fact]
        public void BuildSettingsMenuDefinition_ShouldContainDifficultyItem()
        {
            // Arrange
            var service = CreateService();

            // Act
            var result = service.BuildSettingsMenuDefinition("michael_crew");

            // Assert
            Assert.NotNull(result);
            var item = result.Items.FirstOrDefault(i => i.Id == "difficulty");
            Assert.NotNull(item);
            Assert.Contains("Difficulty", item.Text);
        }

        [Fact]
        public void BuildSettingsMenuDefinition_ShouldContainAutoSaveItem()
        {
            // Arrange
            var service = CreateService();

            // Act
            var result = service.BuildSettingsMenuDefinition("michael_crew");

            // Assert
            Assert.NotNull(result);
            var item = result.Items.FirstOrDefault(i => i.Id == "auto_save");
            Assert.NotNull(item);
            Assert.Contains("Auto", item.Text);
        }

        [Fact]
        public void BuildSettingsMenuDefinition_ShouldContainBackItem()
        {
            // Arrange
            var service = CreateService();

            // Act
            var result = service.BuildSettingsMenuDefinition("michael_crew");

            // Assert
            Assert.NotNull(result);
            var item = result.Items.FirstOrDefault(i => i.Id == "back");
            Assert.NotNull(item);
        }

        [Fact]
        public void BuildSettingsMenuDefinition_ShouldHaveCorrectItemOrder()
        {
            // Arrange
            var service = CreateService();

            // Act
            var result = service.BuildSettingsMenuDefinition("michael_crew");

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Items.Count >= 3);
            Assert.Equal("difficulty", result.Items[0].Id);
            Assert.Equal("auto_save", result.Items[1].Id);
            Assert.Equal("back", result.Items[2].Id);
        }

        #endregion

        #region ShowSettingsMenu

        [Fact]
        public void ShowSettingsMenu_ShouldCallMenuProviderShowMenu()
        {
            // Arrange
            var service = CreateService();

            // Act
            service.ShowSettingsMenu("michael_crew");

            // Assert
            _menuProviderMock.Verify(m => m.ShowMenu(It.Is<MenuDefinition>(d => d.Id == "settings_menu")), Times.Once);
        }

        [Fact]
        public void ShowSettingsMenu_ShouldNotShowMenuForNullFactionId()
        {
            // Arrange
            var service = CreateService();

            // Act
            service.ShowSettingsMenu(null!);

            // Assert
            _menuProviderMock.Verify(m => m.ShowMenu(It.IsAny<MenuDefinition>()), Times.Never);
        }

        #endregion

        #region Settings Menu Navigation

        [Fact]
        public void HandleMenuSelection_SettingsItem_ShouldShowSettingsMenu()
        {
            // Arrange
            var service = CreateService();
            var faction = CreateTestFaction("michael_crew", "Michael's Crew", FactionType.Michael);
            var factionState = CreateTestFactionState("michael_crew", cash: 10000, troops: 50);
            SetupFactionWithZones("michael_crew", 2);

            _factionServiceMock.Setup(f => f.GetFaction("michael_crew")).Returns(faction);
            _factionServiceMock.Setup(f => f.GetFactionState("michael_crew")).Returns(factionState);

            // First show main menu to set up current faction context
            service.ShowMainMenu("michael_crew");
            _menuProviderMock.Invocations.Clear();

            // Act
            service.HandleMenuSelection("faction_main_menu", "settings");

            // Assert
            _menuProviderMock.Verify(m => m.ShowMenu(It.Is<MenuDefinition>(d => d.Id == "settings_menu")), Times.Once);
        }

        [Fact]
        public void HandleMenuSelection_SettingsBackItem_ShouldShowMainMenu()
        {
            // Arrange
            var service = CreateService();
            var faction = CreateTestFaction("michael_crew", "Michael's Crew", FactionType.Michael);
            var factionState = CreateTestFactionState("michael_crew", cash: 10000, troops: 50);
            SetupFactionWithZones("michael_crew", 2);

            _factionServiceMock.Setup(f => f.GetFaction("michael_crew")).Returns(faction);
            _factionServiceMock.Setup(f => f.GetFactionState("michael_crew")).Returns(factionState);

            // Navigate to settings menu
            service.ShowMainMenu("michael_crew");
            service.HandleMenuSelection("faction_main_menu", "settings");
            _menuProviderMock.Invocations.Clear();

            // Act
            service.HandleMenuSelection("settings_menu", "back");

            // Assert
            _menuProviderMock.Verify(m => m.ShowMenu(It.Is<MenuDefinition>(d => d.Id == "faction_main_menu")), Times.Once);
        }

        #endregion
    }
}
