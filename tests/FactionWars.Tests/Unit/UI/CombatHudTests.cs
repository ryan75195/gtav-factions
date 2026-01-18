using Xunit;
using Moq;
using FactionWars.UI.Models;
using FactionWars.UI.Interfaces;
using FactionWars.UI.Services;
using FactionWars.Combat.Models;
using FactionWars.Combat.Interfaces;
using FactionWars.Core.Interfaces;
using System;

namespace FactionWars.Tests.Unit.UI
{
    public class CombatHudDataTests
    {
        [Fact]
        public void Constructor_WithValidValues_SetsPropertiesCorrectly()
        {
            // Arrange & Act
            var data = new CombatHudData(
                zoneId: "zone1",
                zoneName: "Downtown",
                attackerFactionId: "michael",
                defenderFactionId: "trevor",
                attackerControlPercent: 35.5f,
                defenderControlPercent: 64.5f,
                attackerPedCount: 5,
                defenderPedCount: 8,
                reinforcementCooldownSeconds: 15.5f,
                isPlayerAttacker: true,
                combatDuration: TimeSpan.FromMinutes(2));

            // Assert
            Assert.Equal("zone1", data.ZoneId);
            Assert.Equal("Downtown", data.ZoneName);
            Assert.Equal("michael", data.AttackerFactionId);
            Assert.Equal("trevor", data.DefenderFactionId);
            Assert.Equal(35.5f, data.AttackerControlPercent);
            Assert.Equal(64.5f, data.DefenderControlPercent);
            Assert.Equal(5, data.AttackerPedCount);
            Assert.Equal(8, data.DefenderPedCount);
            Assert.Equal(15.5f, data.ReinforcementCooldownSeconds);
            Assert.True(data.IsPlayerAttacker);
            Assert.Equal(TimeSpan.FromMinutes(2), data.CombatDuration);
        }

        [Fact]
        public void Constructor_WithNullZoneId_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new CombatHudData(
                zoneId: null!,
                zoneName: "Downtown",
                attackerFactionId: "michael",
                defenderFactionId: "trevor",
                attackerControlPercent: 50f,
                defenderControlPercent: 50f,
                attackerPedCount: 5,
                defenderPedCount: 5,
                reinforcementCooldownSeconds: 0f,
                isPlayerAttacker: true,
                combatDuration: TimeSpan.Zero));
        }

        [Fact]
        public void Constructor_WithEmptyZoneId_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new CombatHudData(
                zoneId: "",
                zoneName: "Downtown",
                attackerFactionId: "michael",
                defenderFactionId: "trevor",
                attackerControlPercent: 50f,
                defenderControlPercent: 50f,
                attackerPedCount: 5,
                defenderPedCount: 5,
                reinforcementCooldownSeconds: 0f,
                isPlayerAttacker: true,
                combatDuration: TimeSpan.Zero));
        }

        [Fact]
        public void Constructor_WithNullZoneName_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new CombatHudData(
                zoneId: "zone1",
                zoneName: null!,
                attackerFactionId: "michael",
                defenderFactionId: "trevor",
                attackerControlPercent: 50f,
                defenderControlPercent: 50f,
                attackerPedCount: 5,
                defenderPedCount: 5,
                reinforcementCooldownSeconds: 0f,
                isPlayerAttacker: true,
                combatDuration: TimeSpan.Zero));
        }

        [Fact]
        public void Constructor_WithNullAttackerFactionId_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new CombatHudData(
                zoneId: "zone1",
                zoneName: "Downtown",
                attackerFactionId: null!,
                defenderFactionId: "trevor",
                attackerControlPercent: 50f,
                defenderControlPercent: 50f,
                attackerPedCount: 5,
                defenderPedCount: 5,
                reinforcementCooldownSeconds: 0f,
                isPlayerAttacker: true,
                combatDuration: TimeSpan.Zero));
        }

        [Fact]
        public void Constructor_WithNullDefenderFactionId_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new CombatHudData(
                zoneId: "zone1",
                zoneName: "Downtown",
                attackerFactionId: "michael",
                defenderFactionId: null!,
                attackerControlPercent: 50f,
                defenderControlPercent: 50f,
                attackerPedCount: 5,
                defenderPedCount: 5,
                reinforcementCooldownSeconds: 0f,
                isPlayerAttacker: true,
                combatDuration: TimeSpan.Zero));
        }

        [Theory]
        [InlineData(-1f)]
        [InlineData(101f)]
        public void Constructor_WithInvalidAttackerControlPercent_ThrowsArgumentOutOfRangeException(float percent)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new CombatHudData(
                zoneId: "zone1",
                zoneName: "Downtown",
                attackerFactionId: "michael",
                defenderFactionId: "trevor",
                attackerControlPercent: percent,
                defenderControlPercent: 50f,
                attackerPedCount: 5,
                defenderPedCount: 5,
                reinforcementCooldownSeconds: 0f,
                isPlayerAttacker: true,
                combatDuration: TimeSpan.Zero));
        }

        [Theory]
        [InlineData(-1f)]
        [InlineData(101f)]
        public void Constructor_WithInvalidDefenderControlPercent_ThrowsArgumentOutOfRangeException(float percent)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new CombatHudData(
                zoneId: "zone1",
                zoneName: "Downtown",
                attackerFactionId: "michael",
                defenderFactionId: "trevor",
                attackerControlPercent: 50f,
                defenderControlPercent: percent,
                attackerPedCount: 5,
                defenderPedCount: 5,
                reinforcementCooldownSeconds: 0f,
                isPlayerAttacker: true,
                combatDuration: TimeSpan.Zero));
        }

        [Fact]
        public void Constructor_WithNegativePedCounts_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new CombatHudData(
                zoneId: "zone1",
                zoneName: "Downtown",
                attackerFactionId: "michael",
                defenderFactionId: "trevor",
                attackerControlPercent: 50f,
                defenderControlPercent: 50f,
                attackerPedCount: -1,
                defenderPedCount: 5,
                reinforcementCooldownSeconds: 0f,
                isPlayerAttacker: true,
                combatDuration: TimeSpan.Zero));
        }

        [Fact]
        public void Constructor_WithNegativeReinforcementCooldown_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new CombatHudData(
                zoneId: "zone1",
                zoneName: "Downtown",
                attackerFactionId: "michael",
                defenderFactionId: "trevor",
                attackerControlPercent: 50f,
                defenderControlPercent: 50f,
                attackerPedCount: 5,
                defenderPedCount: 5,
                reinforcementCooldownSeconds: -1f,
                isPlayerAttacker: true,
                combatDuration: TimeSpan.Zero));
        }

        [Fact]
        public void PlayerControlPercent_ReturnsAttackerControl_WhenPlayerIsAttacker()
        {
            var data = new CombatHudData(
                zoneId: "zone1",
                zoneName: "Downtown",
                attackerFactionId: "michael",
                defenderFactionId: "trevor",
                attackerControlPercent: 35f,
                defenderControlPercent: 65f,
                attackerPedCount: 5,
                defenderPedCount: 8,
                reinforcementCooldownSeconds: 0f,
                isPlayerAttacker: true,
                combatDuration: TimeSpan.Zero);

            Assert.Equal(35f, data.PlayerControlPercent);
        }

        [Fact]
        public void PlayerControlPercent_ReturnsDefenderControl_WhenPlayerIsDefender()
        {
            var data = new CombatHudData(
                zoneId: "zone1",
                zoneName: "Downtown",
                attackerFactionId: "michael",
                defenderFactionId: "trevor",
                attackerControlPercent: 35f,
                defenderControlPercent: 65f,
                attackerPedCount: 5,
                defenderPedCount: 8,
                reinforcementCooldownSeconds: 0f,
                isPlayerAttacker: false,
                combatDuration: TimeSpan.Zero);

            Assert.Equal(65f, data.PlayerControlPercent);
        }

        [Fact]
        public void EnemyControlPercent_ReturnsDefenderControl_WhenPlayerIsAttacker()
        {
            var data = new CombatHudData(
                zoneId: "zone1",
                zoneName: "Downtown",
                attackerFactionId: "michael",
                defenderFactionId: "trevor",
                attackerControlPercent: 35f,
                defenderControlPercent: 65f,
                attackerPedCount: 5,
                defenderPedCount: 8,
                reinforcementCooldownSeconds: 0f,
                isPlayerAttacker: true,
                combatDuration: TimeSpan.Zero);

            Assert.Equal(65f, data.EnemyControlPercent);
        }

        [Fact]
        public void EnemyControlPercent_ReturnsAttackerControl_WhenPlayerIsDefender()
        {
            var data = new CombatHudData(
                zoneId: "zone1",
                zoneName: "Downtown",
                attackerFactionId: "michael",
                defenderFactionId: "trevor",
                attackerControlPercent: 35f,
                defenderControlPercent: 65f,
                attackerPedCount: 5,
                defenderPedCount: 8,
                reinforcementCooldownSeconds: 0f,
                isPlayerAttacker: false,
                combatDuration: TimeSpan.Zero);

            Assert.Equal(35f, data.EnemyControlPercent);
        }

        [Fact]
        public void IsReinforcementOnCooldown_ReturnsTrue_WhenCooldownGreaterThanZero()
        {
            var data = new CombatHudData(
                zoneId: "zone1",
                zoneName: "Downtown",
                attackerFactionId: "michael",
                defenderFactionId: "trevor",
                attackerControlPercent: 50f,
                defenderControlPercent: 50f,
                attackerPedCount: 5,
                defenderPedCount: 5,
                reinforcementCooldownSeconds: 10.5f,
                isPlayerAttacker: true,
                combatDuration: TimeSpan.Zero);

            Assert.True(data.IsReinforcementOnCooldown);
        }

        [Fact]
        public void IsReinforcementOnCooldown_ReturnsFalse_WhenCooldownIsZero()
        {
            var data = new CombatHudData(
                zoneId: "zone1",
                zoneName: "Downtown",
                attackerFactionId: "michael",
                defenderFactionId: "trevor",
                attackerControlPercent: 50f,
                defenderControlPercent: 50f,
                attackerPedCount: 5,
                defenderPedCount: 5,
                reinforcementCooldownSeconds: 0f,
                isPlayerAttacker: true,
                combatDuration: TimeSpan.Zero);

            Assert.False(data.IsReinforcementOnCooldown);
        }

        [Fact]
        public void TotalPedCount_ReturnsSumOfBothSides()
        {
            var data = new CombatHudData(
                zoneId: "zone1",
                zoneName: "Downtown",
                attackerFactionId: "michael",
                defenderFactionId: "trevor",
                attackerControlPercent: 50f,
                defenderControlPercent: 50f,
                attackerPedCount: 5,
                defenderPedCount: 8,
                reinforcementCooldownSeconds: 0f,
                isPlayerAttacker: true,
                combatDuration: TimeSpan.Zero);

            Assert.Equal(13, data.TotalPedCount);
        }
    }

    public class CombatHudServiceTests
    {
        private readonly Mock<IReinforcementService> _mockReinforcementService;
        private readonly Mock<ICombatHudRenderer> _mockRenderer;

        public CombatHudServiceTests()
        {
            _mockReinforcementService = new Mock<IReinforcementService>();
            _mockRenderer = new Mock<ICombatHudRenderer>();
        }

        [Fact]
        public void Constructor_WithNullReinforcementService_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new CombatHudService(null!, _mockRenderer.Object));
        }

        [Fact]
        public void Constructor_WithNullRenderer_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new CombatHudService(_mockReinforcementService.Object, null!));
        }

        [Fact]
        public void Update_WithNullEncounter_ThrowsArgumentNullException()
        {
            var service = new CombatHudService(_mockReinforcementService.Object, _mockRenderer.Object);

            Assert.Throws<ArgumentNullException>(() => service.Update(null!, "player-faction", "Zone Name"));
        }

        [Fact]
        public void Update_WithNullPlayerFactionId_ThrowsArgumentNullException()
        {
            var encounter = new CombatEncounter("enc1", "zone1", "michael", "trevor");
            var service = new CombatHudService(_mockReinforcementService.Object, _mockRenderer.Object);

            Assert.Throws<ArgumentNullException>(() => service.Update(encounter, null!, "Zone Name"));
        }

        [Fact]
        public void Update_WithActiveEncounter_CallsRenderer()
        {
            var encounter = new CombatEncounter("enc1", "zone1", "michael", "trevor");
            encounter.AttackerControlPercentage = 40f;
            encounter.DefenderControlPercentage = 60f;
            encounter.AttackerPedCount = 5;
            encounter.DefenderPedCount = 8;

            _mockReinforcementService.Setup(s => s.GetRemainingCooldown("michael", encounter)).Returns(15f);

            var service = new CombatHudService(_mockReinforcementService.Object, _mockRenderer.Object);
            service.Update(encounter, "michael", "Downtown");

            _mockRenderer.Verify(r => r.RenderCombatHud(It.Is<CombatHudData>(d =>
                d.ZoneId == "zone1" &&
                d.ZoneName == "Downtown" &&
                d.AttackerFactionId == "michael" &&
                d.DefenderFactionId == "trevor" &&
                d.AttackerControlPercent == 40f &&
                d.DefenderControlPercent == 60f &&
                d.AttackerPedCount == 5 &&
                d.DefenderPedCount == 8 &&
                d.ReinforcementCooldownSeconds == 15f &&
                d.IsPlayerAttacker == true
            )), Times.Once);
        }

        [Fact]
        public void Update_WithPlayerAsDefender_SetsIsPlayerAttackerToFalse()
        {
            var encounter = new CombatEncounter("enc1", "zone1", "michael", "trevor");

            _mockReinforcementService.Setup(s => s.GetRemainingCooldown("trevor", encounter)).Returns(0f);

            var service = new CombatHudService(_mockReinforcementService.Object, _mockRenderer.Object);
            service.Update(encounter, "trevor", "Downtown");

            _mockRenderer.Verify(r => r.RenderCombatHud(It.Is<CombatHudData>(d =>
                d.IsPlayerAttacker == false
            )), Times.Once);
        }

        [Fact]
        public void Update_GetsReinforcementCooldownForPlayerFaction()
        {
            var encounter = new CombatEncounter("enc1", "zone1", "michael", "trevor");
            _mockReinforcementService.Setup(s => s.GetRemainingCooldown("trevor", encounter)).Returns(25.5f);

            var service = new CombatHudService(_mockReinforcementService.Object, _mockRenderer.Object);
            service.Update(encounter, "trevor", "Downtown");

            _mockReinforcementService.Verify(s => s.GetRemainingCooldown("trevor", encounter), Times.Once);
            _mockRenderer.Verify(r => r.RenderCombatHud(It.Is<CombatHudData>(d =>
                d.ReinforcementCooldownSeconds == 25.5f
            )), Times.Once);
        }

        [Fact]
        public void Hide_CallsRendererHide()
        {
            var service = new CombatHudService(_mockReinforcementService.Object, _mockRenderer.Object);
            service.Hide();

            _mockRenderer.Verify(r => r.HideCombatHud(), Times.Once);
        }

        [Fact]
        public void IsVisible_ReturnsRendererVisibility()
        {
            _mockRenderer.Setup(r => r.IsVisible).Returns(true);

            var service = new CombatHudService(_mockReinforcementService.Object, _mockRenderer.Object);

            Assert.True(service.IsVisible);
        }

        [Fact]
        public void Update_WithInactiveEncounter_HidesHud()
        {
            var encounter = new CombatEncounter("enc1", "zone1", "michael", "trevor");
            encounter.End(CombatStatus.AttackerVictory);

            var service = new CombatHudService(_mockReinforcementService.Object, _mockRenderer.Object);
            service.Update(encounter, "michael", "Downtown");

            _mockRenderer.Verify(r => r.HideCombatHud(), Times.Once);
            _mockRenderer.Verify(r => r.RenderCombatHud(It.IsAny<CombatHudData>()), Times.Never);
        }

        [Fact]
        public void Update_WithPlayerNotInCombat_HidesHud()
        {
            var encounter = new CombatEncounter("enc1", "zone1", "michael", "trevor");

            var service = new CombatHudService(_mockReinforcementService.Object, _mockRenderer.Object);
            service.Update(encounter, "franklin", "Downtown"); // Player is neither attacker nor defender

            _mockRenderer.Verify(r => r.HideCombatHud(), Times.Once);
            _mockRenderer.Verify(r => r.RenderCombatHud(It.IsAny<CombatHudData>()), Times.Never);
        }
    }

    public class CombatHudRendererInterfaceTests
    {
        [Fact]
        public void ICombatHudRenderer_DefinedCorrectly()
        {
            // Verify interface can be mocked (i.e., it's properly defined)
            var mock = new Mock<ICombatHudRenderer>();
            mock.Setup(r => r.RenderCombatHud(It.IsAny<CombatHudData>()));
            mock.Setup(r => r.HideCombatHud());
            mock.SetupGet(r => r.IsVisible).Returns(true);

            Assert.NotNull(mock.Object);
            Assert.True(mock.Object.IsVisible);
        }
    }
}
