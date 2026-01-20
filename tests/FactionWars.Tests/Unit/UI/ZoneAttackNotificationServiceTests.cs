using Xunit;
using Moq;
using FactionWars.UI.Interfaces;
using FactionWars.UI.Services;
using FactionWars.UI.Models;
using FactionWars.Core.Interfaces;
using FactionWars.Territory.Models;
using System;

namespace FactionWars.Tests.Unit.UI
{
    public class ZoneAttackNotificationServiceTests
    {
        private readonly Mock<INotificationService> _mockNotificationService;
        private readonly Mock<IGameBridge> _mockGameBridge;
        private readonly ZoneAttackNotificationService _service;

        public ZoneAttackNotificationServiceTests()
        {
            _mockNotificationService = new Mock<INotificationService>();
            _mockGameBridge = new Mock<IGameBridge>();
            _service = new ZoneAttackNotificationService(_mockNotificationService.Object, _mockGameBridge.Object);
        }

        [Fact]
        public void Constructor_WithNullNotificationService_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new ZoneAttackNotificationService(null!, _mockGameBridge.Object));
        }

        [Fact]
        public void Constructor_WithNullGameBridge_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new ZoneAttackNotificationService(_mockNotificationService.Object, null!));
        }

        [Fact]
        public void NotifyZoneUnderAttack_WithValidZone_ShowsWarningNotification()
        {
            var zone = CreateTestZone("downtown", "Downtown");
            var attackerFactionId = "faction_trevor";

            _service.NotifyZoneUnderAttack(zone, attackerFactionId);

            _mockNotificationService.Verify(n => n.ShowWarning(
                It.Is<string>(s => s.Contains("Under Attack")),
                It.Is<string>(s => s.Contains("Downtown")),
                NotificationPriority.Critical,
                It.IsAny<float>()), Times.Once);
        }

        [Fact]
        public void NotifyZoneUnderAttack_WithNullZone_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                _service.NotifyZoneUnderAttack(null!, "faction_trevor"));
        }

        [Fact]
        public void NotifyZoneUnderAttack_WithNullAttackerFactionId_ThrowsArgumentNullException()
        {
            var zone = CreateTestZone("downtown", "Downtown");

            Assert.Throws<ArgumentNullException>(() =>
                _service.NotifyZoneUnderAttack(zone, null!));
        }

        [Fact]
        public void NotifyZoneUnderAttack_WithEmptyAttackerFactionId_ThrowsArgumentException()
        {
            var zone = CreateTestZone("downtown", "Downtown");

            Assert.Throws<ArgumentException>(() =>
                _service.NotifyZoneUnderAttack(zone, ""));
        }

        [Fact]
        public void NotifyZoneUnderAttack_StoresZoneForWaypoint()
        {
            var zone = CreateTestZone("downtown", "Downtown");
            var attackerFactionId = "faction_trevor";

            _service.NotifyZoneUnderAttack(zone, attackerFactionId);

            Assert.True(_service.HasActiveZoneAttackNotification);
            Assert.Equal("downtown", _service.ActiveAttackedZoneId);
        }

        [Fact]
        public void SetWaypointToAttackedZone_WithActiveNotification_CreatesWaypoint()
        {
            var zone = CreateTestZone("downtown", "Downtown");
            _service.NotifyZoneUnderAttack(zone, "faction_trevor");

            var result = _service.SetWaypointToAttackedZone();

            Assert.True(result);
            _mockGameBridge.Verify(g => g.SetWaypoint(zone.Center), Times.Once);
        }

        [Fact]
        public void SetWaypointToAttackedZone_WithNoActiveNotification_ReturnsFalse()
        {
            var result = _service.SetWaypointToAttackedZone();

            Assert.False(result);
            _mockGameBridge.Verify(g => g.SetWaypoint(It.IsAny<Vector3>()), Times.Never);
        }

        [Fact]
        public void ClearActiveNotification_ClearsActiveZone()
        {
            var zone = CreateTestZone("downtown", "Downtown");
            _service.NotifyZoneUnderAttack(zone, "faction_trevor");

            _service.ClearActiveNotification();

            Assert.False(_service.HasActiveZoneAttackNotification);
            Assert.Null(_service.ActiveAttackedZoneId);
        }

        [Fact]
        public void ClearWaypoint_CallsGameBridgeClearWaypoint()
        {
            _service.ClearWaypoint();

            _mockGameBridge.Verify(g => g.ClearWaypoint(), Times.Once);
        }

        [Fact]
        public void NotifyZoneUnderAttack_ReplacesExistingActiveNotification()
        {
            var zone1 = CreateTestZone("downtown", "Downtown");
            var zone2 = CreateTestZone("vinewood", "Vinewood");

            _service.NotifyZoneUnderAttack(zone1, "faction_trevor");
            _service.NotifyZoneUnderAttack(zone2, "faction_michael");

            Assert.Equal("vinewood", _service.ActiveAttackedZoneId);
        }

        [Fact]
        public void NotifyZoneUnderAttack_IncludesAttackerInMessage()
        {
            var zone = CreateTestZone("downtown", "Downtown");

            _service.NotifyZoneUnderAttack(zone, "faction_trevor");

            _mockNotificationService.Verify(n => n.ShowWarning(
                It.IsAny<string>(),
                It.Is<string>(s => s.Contains("Trevor") || s.Contains("faction_trevor")),
                It.IsAny<NotificationPriority>(),
                It.IsAny<float>()), Times.Once);
        }

        [Fact]
        public void GetActiveAttackedZone_ReturnsZoneWhenActive()
        {
            var zone = CreateTestZone("downtown", "Downtown");
            _service.NotifyZoneUnderAttack(zone, "faction_trevor");

            var result = _service.GetActiveAttackedZone();

            Assert.NotNull(result);
            Assert.Equal("downtown", result.Id);
        }

        [Fact]
        public void GetActiveAttackedZone_ReturnsNullWhenNoActiveNotification()
        {
            var result = _service.GetActiveAttackedZone();

            Assert.Null(result);
        }

        [Fact]
        public void HasWaypointSet_ReturnsFalseInitially()
        {
            Assert.False(_service.HasWaypointSet);
        }

        [Fact]
        public void HasWaypointSet_ReturnsTrueAfterSettingWaypoint()
        {
            var zone = CreateTestZone("downtown", "Downtown");
            _service.NotifyZoneUnderAttack(zone, "faction_trevor");
            _service.SetWaypointToAttackedZone();

            Assert.True(_service.HasWaypointSet);
        }

        [Fact]
        public void ClearWaypoint_SetsHasWaypointSetToFalse()
        {
            var zone = CreateTestZone("downtown", "Downtown");
            _service.NotifyZoneUnderAttack(zone, "faction_trevor");
            _service.SetWaypointToAttackedZone();

            _service.ClearWaypoint();

            Assert.False(_service.HasWaypointSet);
        }

        private Zone CreateTestZone(string id, string name)
        {
            return new Zone(id, name, new Vector3(100f, 200f, 0f), 150f, 5);
        }
    }
}
