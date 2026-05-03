using Xunit;
using Moq;
using FactionWars.UI.Models;
using FactionWars.UI.Interfaces;
using FactionWars.UI.Services;
using FactionWars.Territory.Models;
using FactionWars.Factions.Models;
using FactionWars.Factions.Interfaces;
using FactionWars.Core.Interfaces;
using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Models;
using FactionWars.Core.Models;
using System;
using System.Collections.Generic;

namespace FactionWars.Tests.Unit.UI
{
    public class TerritoryIndicatorDataTests
    {
        [Fact]
        public void Constructor_WithValidValues_SetsPropertiesCorrectly()
        {
            // Arrange & Act
            var color = new FactionColor(255, 0, 0);
            var data = new TerritoryIndicatorData(
                zoneName: "Downtown",
                ownerFactionName: "Michael's Crew",
                ownerFactionColor: color,
                controlPercentage: 75.5f,
                isContested: false,
                isPlayerOwned: true);

            // Assert
            Assert.Equal("Downtown", data.ZoneName);
            Assert.Equal("Michael's Crew", data.OwnerFactionName);
            Assert.Equal(color, data.OwnerFactionColor);
            Assert.Equal(75.5f, data.ControlPercentage);
            Assert.False(data.IsContested);
            Assert.True(data.IsPlayerOwned);
        }

        [Fact]
        public void Constructor_WithNullZoneName_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new TerritoryIndicatorData(
                zoneName: null!,
                ownerFactionName: "Michael's Crew",
                ownerFactionColor: new FactionColor(255, 0, 0),
                controlPercentage: 100f,
                isContested: false,
                isPlayerOwned: true));
        }

        [Fact]
        public void Constructor_WithEmptyZoneName_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new TerritoryIndicatorData(
                zoneName: "",
                ownerFactionName: "Michael's Crew",
                ownerFactionColor: new FactionColor(255, 0, 0),
                controlPercentage: 100f,
                isContested: false,
                isPlayerOwned: true));
        }

        [Fact]
        public void Constructor_WithNullOwnerFactionName_SetsToNeutral()
        {
            // Arrange & Act
            var data = new TerritoryIndicatorData(
                zoneName: "Downtown",
                ownerFactionName: null,
                ownerFactionColor: null,
                controlPercentage: 0f,
                isContested: false,
                isPlayerOwned: false);

            // Assert
            Assert.Null(data.OwnerFactionName);
            Assert.Null(data.OwnerFactionColor);
            Assert.False(data.IsPlayerOwned);
        }

        [Theory]
        [InlineData(-1f)]
        [InlineData(101f)]
        public void Constructor_WithInvalidControlPercentage_ThrowsArgumentOutOfRangeException(float percentage)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new TerritoryIndicatorData(
                zoneName: "Downtown",
                ownerFactionName: "Michael's Crew",
                ownerFactionColor: new FactionColor(255, 0, 0),
                controlPercentage: percentage,
                isContested: false,
                isPlayerOwned: true));
        }

        [Fact]
        public void IsNeutral_ReturnsTrue_WhenNoOwner()
        {
            var data = new TerritoryIndicatorData(
                zoneName: "Downtown",
                ownerFactionName: null,
                ownerFactionColor: null,
                controlPercentage: 0f,
                isContested: false,
                isPlayerOwned: false);

            Assert.True(data.IsNeutral);
        }

        [Fact]
        public void IsNeutral_ReturnsFalse_WhenHasOwner()
        {
            var data = new TerritoryIndicatorData(
                zoneName: "Downtown",
                ownerFactionName: "Michael's Crew",
                ownerFactionColor: new FactionColor(255, 0, 0),
                controlPercentage: 100f,
                isContested: false,
                isPlayerOwned: true);

            Assert.False(data.IsNeutral);
        }

        [Fact]
        public void IsEnemyOwned_ReturnsTrue_WhenHasOwnerAndNotPlayerOwned()
        {
            var data = new TerritoryIndicatorData(
                zoneName: "Downtown",
                ownerFactionName: "Trevor's Crew",
                ownerFactionColor: new FactionColor(255, 128, 0),
                controlPercentage: 100f,
                isContested: false,
                isPlayerOwned: false);

            Assert.True(data.IsEnemyOwned);
        }

        [Fact]
        public void IsEnemyOwned_ReturnsFalse_WhenPlayerOwned()
        {
            var data = new TerritoryIndicatorData(
                zoneName: "Downtown",
                ownerFactionName: "Michael's Crew",
                ownerFactionColor: new FactionColor(255, 0, 0),
                controlPercentage: 100f,
                isContested: false,
                isPlayerOwned: true);

            Assert.False(data.IsEnemyOwned);
        }

        [Fact]
        public void IsEnemyOwned_ReturnsFalse_WhenNeutral()
        {
            var data = new TerritoryIndicatorData(
                zoneName: "Downtown",
                ownerFactionName: null,
                ownerFactionColor: null,
                controlPercentage: 0f,
                isContested: false,
                isPlayerOwned: false);

            Assert.False(data.IsEnemyOwned);
        }

        [Fact]
        public void TerritoryIndicatorData_ExposesThirdPartyFields()
        {
            var data = new TerritoryIndicatorData(
                zoneName: "Test",
                ownerFactionName: null,
                ownerFactionColor: null,
                controlPercentage: 0f,
                isContested: false,
                isPlayerOwned: false,
                thirdPartyCount: 2,
                thirdPartyFactionColor: new FactionColor(255, 150, 0));

            Assert.Equal(2, data.ThirdPartyCount);
            Assert.NotNull(data.ThirdPartyFactionColor);
            Assert.Equal(new FactionColor(255, 150, 0), data.ThirdPartyFactionColor);
        }

        [Fact]
        public void TerritoryIndicatorData_ThirdPartyDefaults_AreZeroAndNull()
        {
            var data = new TerritoryIndicatorData(
                zoneName: "Test",
                ownerFactionName: null,
                ownerFactionColor: null,
                controlPercentage: 0f,
                isContested: false,
                isPlayerOwned: false);

            Assert.Equal(0, data.ThirdPartyCount);
            Assert.Null(data.ThirdPartyFactionColor);
        }
    }

    public class TerritoryIndicatorServiceTests
    {
        private readonly Mock<IFactionRepository> _mockFactionRepository;
        private readonly Mock<ITerritoryIndicatorRenderer> _mockRenderer;
        private readonly Mock<IZoneBattleManager> _mockBattleManager;

        public TerritoryIndicatorServiceTests()
        {
            _mockFactionRepository = new Mock<IFactionRepository>();
            _mockRenderer = new Mock<ITerritoryIndicatorRenderer>();
            _mockBattleManager = new Mock<IZoneBattleManager>();
        }

        [Fact]
        public void Constructor_WithNullFactionRepository_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new TerritoryIndicatorService(null!, _mockRenderer.Object, _mockBattleManager.Object));
        }

        [Fact]
        public void Constructor_WithNullRenderer_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new TerritoryIndicatorService(_mockFactionRepository.Object, null!, _mockBattleManager.Object));
        }

        [Fact]
        public void Constructor_WithNullBattleManager_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new TerritoryIndicatorService(_mockFactionRepository.Object, _mockRenderer.Object, null!));
        }

        [Fact]
        public void Update_WithNullZone_HidesIndicator()
        {
            var service = new TerritoryIndicatorService(_mockFactionRepository.Object, _mockRenderer.Object, _mockBattleManager.Object);

            service.Update(null, "michael");

            _mockRenderer.Verify(r => r.Hide(), Times.Once);
            _mockRenderer.Verify(r => r.Render(It.IsAny<TerritoryIndicatorData>()), Times.Never);
        }

        [Fact]
        public void Update_WithNullPlayerFactionId_ThrowsArgumentNullException()
        {
            var zone = CreateTestZone("zone1", "Downtown", "michael", 100f);
            var service = new TerritoryIndicatorService(_mockFactionRepository.Object, _mockRenderer.Object, _mockBattleManager.Object);

            Assert.Throws<ArgumentNullException>(() => service.Update(zone, null!));
        }

        [Fact]
        public void Update_WithEmptyPlayerFactionId_ThrowsArgumentException()
        {
            var zone = CreateTestZone("zone1", "Downtown", "michael", 100f);
            var service = new TerritoryIndicatorService(_mockFactionRepository.Object, _mockRenderer.Object, _mockBattleManager.Object);

            Assert.Throws<ArgumentException>(() => service.Update(zone, ""));
        }

        [Fact]
        public void Update_WithPlayerOwnedZone_SetsIsPlayerOwnedTrue()
        {
            var zone = CreateTestZone("zone1", "Downtown", "michael", 100f);
            var faction = CreateTestFaction("michael", "Michael's Crew", 0, 100, 200);

            _mockFactionRepository.Setup(r => r.GetById("michael")).Returns(faction);

            var service = new TerritoryIndicatorService(_mockFactionRepository.Object, _mockRenderer.Object, _mockBattleManager.Object);
            service.Update(zone, "michael");

            _mockRenderer.Verify(r => r.Render(It.Is<TerritoryIndicatorData>(d =>
                d.ZoneName == "Downtown" &&
                d.OwnerFactionName == "Michael's Crew" &&
                d.IsPlayerOwned == true &&
                d.ControlPercentage == 100f &&
                !d.IsContested
            )), Times.Once);
        }

        [Fact]
        public void Update_WithEnemyOwnedZone_SetsIsPlayerOwnedFalse()
        {
            var zone = CreateTestZone("zone1", "Downtown", "trevor", 100f);
            var faction = CreateTestFaction("trevor", "Trevor's Enterprises", 255, 128, 0);

            _mockFactionRepository.Setup(r => r.GetById("trevor")).Returns(faction);

            var service = new TerritoryIndicatorService(_mockFactionRepository.Object, _mockRenderer.Object, _mockBattleManager.Object);
            service.Update(zone, "michael");

            _mockRenderer.Verify(r => r.Render(It.Is<TerritoryIndicatorData>(d =>
                d.ZoneName == "Downtown" &&
                d.OwnerFactionName == "Trevor's Enterprises" &&
                d.IsPlayerOwned == false &&
                d.ControlPercentage == 100f
            )), Times.Once);
        }

        [Fact]
        public void Update_WithNeutralZone_SetsOwnerToNull()
        {
            var zone = CreateTestZone("zone1", "Downtown", null, 0f);

            var service = new TerritoryIndicatorService(_mockFactionRepository.Object, _mockRenderer.Object, _mockBattleManager.Object);
            service.Update(zone, "michael");

            _mockRenderer.Verify(r => r.Render(It.Is<TerritoryIndicatorData>(d =>
                d.ZoneName == "Downtown" &&
                d.OwnerFactionName == null &&
                d.OwnerFactionColor == null &&
                d.IsPlayerOwned == false &&
                d.IsNeutral == true
            )), Times.Once);
        }

        [Fact]
        public void Update_WithContestedZone_SetsIsContestedTrue()
        {
            var zone = CreateTestZone("zone1", "Downtown", "michael", 65f);
            zone.IsContested = true;
            var faction = CreateTestFaction("michael", "Michael's Crew", 0, 100, 200);

            _mockFactionRepository.Setup(r => r.GetById("michael")).Returns(faction);

            var service = new TerritoryIndicatorService(_mockFactionRepository.Object, _mockRenderer.Object, _mockBattleManager.Object);
            service.Update(zone, "michael");

            _mockRenderer.Verify(r => r.Render(It.Is<TerritoryIndicatorData>(d =>
                d.IsContested == true &&
                d.ControlPercentage == 65f
            )), Times.Once);
        }

        [Fact]
        public void Update_WithUnknownOwnerFaction_SetsOwnerNameToUnknown()
        {
            var zone = CreateTestZone("zone1", "Downtown", "unknown_faction", 100f);
            _mockFactionRepository.Setup(r => r.GetById("unknown_faction")).Returns((Faction?)null);

            var service = new TerritoryIndicatorService(_mockFactionRepository.Object, _mockRenderer.Object, _mockBattleManager.Object);
            service.Update(zone, "michael");

            _mockRenderer.Verify(r => r.Render(It.Is<TerritoryIndicatorData>(d =>
                d.OwnerFactionName == "Unknown" &&
                d.OwnerFactionColor == null
            )), Times.Once);
        }

        [Fact]
        public void Update_PassesFactionColorToIndicatorData()
        {
            var zone = CreateTestZone("zone1", "Downtown", "michael", 100f);
            var factionColor = new FactionColor(50, 100, 150);
            var faction = new Faction("michael", "Michael's Crew", "Michael De Santa", "", factionColor);

            _mockFactionRepository.Setup(r => r.GetById("michael")).Returns(faction);

            var service = new TerritoryIndicatorService(_mockFactionRepository.Object, _mockRenderer.Object, _mockBattleManager.Object);
            service.Update(zone, "michael");

            _mockRenderer.Verify(r => r.Render(It.Is<TerritoryIndicatorData>(d =>
                d.OwnerFactionColor != null &&
                d.OwnerFactionColor.Value.R == 50 &&
                d.OwnerFactionColor.Value.G == 100 &&
                d.OwnerFactionColor.Value.B == 150
            )), Times.Once);
        }

        [Fact]
        public void Hide_CallsRendererHide()
        {
            var service = new TerritoryIndicatorService(_mockFactionRepository.Object, _mockRenderer.Object, _mockBattleManager.Object);

            service.Hide();

            _mockRenderer.Verify(r => r.Hide(), Times.Once);
        }

        [Fact]
        public void IsVisible_ReturnsRendererVisibility()
        {
            _mockRenderer.Setup(r => r.IsVisible).Returns(true);

            var service = new TerritoryIndicatorService(_mockFactionRepository.Object, _mockRenderer.Object, _mockBattleManager.Object);

            Assert.True(service.IsVisible);
        }

        [Fact]
        public void BuildIndicatorData_PopulatesThirdParty_FromAiAttackerWhenPlayerIsAlsoAttacker()
        {
            // 3-way: defender = michael, attackers = [trevor (AI), player_faction (player)].
            // Third party from player POV = trevor.
            var trevorColor = new FactionColor(255, 150, 0);
            var michaelColor = new FactionColor(0, 100, 255);

            var zone = CreateTestZone("zone_1", "Downtown", "michael", 40f);
            zone.IsContested = true;

            var defender = BattleParticipant.ForAi("michael", BattleRole.Defender,
                new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 5 } });
            var aiAttacker = BattleParticipant.ForAi("trevor", BattleRole.Attacker,
                new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 2 } });
            var playerAttacker = BattleParticipant.ForPlayer("player_faction", BattleRole.Attacker, () => 4);
            var battle = new ZoneBattle("zone_1",
                new List<BattleParticipant> { defender, aiAttacker, playerAttacker },
                playerFactionId: "player_faction");

            _mockBattleManager.Setup(m => m.GetBattleForZone("zone_1")).Returns(battle);

            var michaelFaction = new Faction("michael", "Michael's Crew", null, "", michaelColor);
            var trevorFaction = new Faction("trevor", "Trevor's Enterprises", null, "", trevorColor);
            _mockFactionRepository.Setup(r => r.GetById("michael")).Returns(michaelFaction);
            _mockFactionRepository.Setup(r => r.GetById("trevor")).Returns(trevorFaction);

            var service = new TerritoryIndicatorService(_mockFactionRepository.Object, _mockRenderer.Object, _mockBattleManager.Object);
            service.Update(zone, "player_faction");

            _mockRenderer.Verify(r => r.Render(It.Is<TerritoryIndicatorData>(d =>
                d.ThirdPartyCount == 2 &&
                d.ThirdPartyFactionColor != null &&
                d.ThirdPartyFactionColor.Value.R == 255 &&
                d.ThirdPartyFactionColor.Value.G == 150 &&
                d.ThirdPartyFactionColor.Value.B == 0
            )), Times.Once);
        }

        [Fact]
        public void BuildIndicatorData_ThirdParty_IsZero_InTwoWayBattle()
        {
            var zone = CreateTestZone("zone_1", "Downtown", "michael", 40f);
            zone.IsContested = true;

            // No battle registered -> GetBattleForZone returns null -> no third party
            _mockBattleManager.Setup(m => m.GetBattleForZone("zone_1")).Returns((ZoneBattle?)null);

            var michaelFaction = new Faction("michael", "Michael's Crew", null, "", new FactionColor(0, 100, 255));
            _mockFactionRepository.Setup(r => r.GetById("michael")).Returns(michaelFaction);

            var service = new TerritoryIndicatorService(_mockFactionRepository.Object, _mockRenderer.Object, _mockBattleManager.Object);
            service.Update(zone, "player_faction");

            _mockRenderer.Verify(r => r.Render(It.Is<TerritoryIndicatorData>(d =>
                d.ThirdPartyCount == 0 &&
                d.ThirdPartyFactionColor == null
            )), Times.Once);
        }

        private Zone CreateTestZone(string id, string name, string? ownerId, float controlPercentage)
        {
            var zone = new Zone(id, name, new Vector3(0, 0, 0));
            zone.OwnerFactionId = ownerId;
            zone.ControlPercentage = controlPercentage;
            return zone;
        }

        private Faction CreateTestFaction(string id, string name, byte r, byte g, byte b)
        {
            return new Faction(id, name, null, "", new FactionColor(r, g, b));
        }
    }

    public class TerritoryIndicatorRendererInterfaceTests
    {
        [Fact]
        public void ITerritoryIndicatorRenderer_DefinedCorrectly()
        {
            // Verify interface can be mocked (i.e., it's properly defined)
            var mock = new Mock<ITerritoryIndicatorRenderer>();
            mock.Setup(r => r.Render(It.IsAny<TerritoryIndicatorData>()));
            mock.Setup(r => r.Hide());
            mock.SetupGet(r => r.IsVisible).Returns(true);

            Assert.NotNull(mock.Object);
            Assert.True(mock.Object.IsVisible);
        }
    }
}
