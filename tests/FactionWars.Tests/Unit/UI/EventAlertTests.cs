using Xunit;
using Moq;
using FactionWars.UI.Models;
using FactionWars.UI.Interfaces;
using FactionWars.UI.Services;
using FactionWars.Factions.Models;
using FactionWars.Territory.Models;
using FactionWars.Core.Interfaces;
using System;
using System.Collections.Generic;

namespace FactionWars.Tests.Unit.UI
{
    public class EventAlertTypeTests
    {
        [Fact]
        public void EventAlertType_HasExpectedValues()
        {
            Assert.Equal(0, (int)EventAlertType.ZoneCaptured);
            Assert.Equal(1, (int)EventAlertType.ZoneLost);
            Assert.Equal(2, (int)EventAlertType.AttackIncoming);
            Assert.Equal(3, (int)EventAlertType.AttackLaunched);
            Assert.Equal(4, (int)EventAlertType.ReinforcementsArriving);
            Assert.Equal(5, (int)EventAlertType.ZoneContested);
            Assert.Equal(6, (int)EventAlertType.VictoryImminent);
            Assert.Equal(7, (int)EventAlertType.DefeatImminent);
        }
    }

    public class EventAlertTests
    {
        [Fact]
        public void Constructor_WithValidValues_SetsPropertiesCorrectly()
        {
            var alert = new EventAlert(
                EventAlertType.ZoneCaptured,
                "Downtown",
                "Michael's Crew",
                "Trevor's Gang");

            Assert.Equal(EventAlertType.ZoneCaptured, alert.Type);
            Assert.Equal("Downtown", alert.ZoneName);
            Assert.Equal("Michael's Crew", alert.FactionName);
            Assert.Equal("Trevor's Gang", alert.TargetFactionName);
            Assert.NotEqual(Guid.Empty, alert.Id);
            Assert.True(alert.CreatedAt <= DateTime.UtcNow);
        }

        [Fact]
        public void Constructor_WithNullZoneName_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new EventAlert(
                EventAlertType.ZoneCaptured,
                null!,
                "Faction",
                null));
        }

        [Fact]
        public void Constructor_WithEmptyZoneName_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new EventAlert(
                EventAlertType.ZoneCaptured,
                "",
                "Faction",
                null));
        }

        [Fact]
        public void Constructor_WithNullFactionName_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new EventAlert(
                EventAlertType.ZoneCaptured,
                "Downtown",
                null!,
                null));
        }

        [Fact]
        public void Constructor_WithEmptyFactionName_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new EventAlert(
                EventAlertType.ZoneCaptured,
                "Downtown",
                "",
                null));
        }

        [Fact]
        public void Constructor_WithNullTargetFaction_IsValid()
        {
            var alert = new EventAlert(
                EventAlertType.ZoneCaptured,
                "Downtown",
                "Michael's Crew",
                null);

            Assert.Null(alert.TargetFactionName);
        }

        [Fact]
        public void Equals_WithSameId_ReturnsTrue()
        {
            var alert = new EventAlert(EventAlertType.ZoneCaptured, "Downtown", "Faction", null);
            Assert.True(alert.Equals(alert));
        }

        [Fact]
        public void Equals_WithDifferentAlerts_ReturnsFalse()
        {
            var alert1 = new EventAlert(EventAlertType.ZoneCaptured, "Downtown", "Faction", null);
            var alert2 = new EventAlert(EventAlertType.ZoneCaptured, "Downtown", "Faction", null);
            Assert.False(alert1.Equals(alert2));
        }

        [Fact]
        public void GetHashCode_ReturnsIdHashCode()
        {
            var alert = new EventAlert(EventAlertType.ZoneCaptured, "Downtown", "Faction", null);
            Assert.Equal(alert.Id.GetHashCode(), alert.GetHashCode());
        }

        [Theory]
        [InlineData(EventAlertType.ZoneCaptured)]
        [InlineData(EventAlertType.ZoneLost)]
        [InlineData(EventAlertType.AttackIncoming)]
        [InlineData(EventAlertType.AttackLaunched)]
        public void GetTitle_ReturnsAppropriateTitle(EventAlertType alertType)
        {
            var alert = new EventAlert(alertType, "Downtown", "Faction", null);
            var title = alert.GetTitle();

            Assert.False(string.IsNullOrEmpty(title));
        }

        [Fact]
        public void GetTitle_ForZoneCaptured_ReturnsZoneCapturedTitle()
        {
            var alert = new EventAlert(EventAlertType.ZoneCaptured, "Downtown", "Faction", null);
            Assert.Equal("Zone Captured", alert.GetTitle());
        }

        [Fact]
        public void GetTitle_ForZoneLost_ReturnsZoneLostTitle()
        {
            var alert = new EventAlert(EventAlertType.ZoneLost, "Downtown", "Faction", null);
            Assert.Equal("Zone Lost", alert.GetTitle());
        }

        [Fact]
        public void GetTitle_ForAttackIncoming_ReturnsAttackIncomingTitle()
        {
            var alert = new EventAlert(EventAlertType.AttackIncoming, "Downtown", "Faction", null);
            Assert.Equal("Attack Incoming", alert.GetTitle());
        }

        [Fact]
        public void GetTitle_ForAttackLaunched_ReturnsAttackLaunchedTitle()
        {
            var alert = new EventAlert(EventAlertType.AttackLaunched, "Downtown", "Faction", null);
            Assert.Equal("Attack Launched", alert.GetTitle());
        }

        [Fact]
        public void GetMessage_ForZoneCaptured_IncludesZoneAndFaction()
        {
            var alert = new EventAlert(EventAlertType.ZoneCaptured, "Downtown", "Michael's Crew", null);
            var message = alert.GetMessage();

            Assert.Contains("Downtown", message);
            Assert.Contains("Michael's Crew", message);
        }

        [Fact]
        public void GetMessage_ForZoneLost_IncludesZoneAndAttacker()
        {
            var alert = new EventAlert(EventAlertType.ZoneLost, "Downtown", "Michael's Crew", "Trevor's Gang");
            var message = alert.GetMessage();

            Assert.Contains("Downtown", message);
            Assert.Contains("Trevor's Gang", message);
        }

        [Fact]
        public void GetMessage_ForAttackIncoming_IncludesZoneAndAttacker()
        {
            var alert = new EventAlert(EventAlertType.AttackIncoming, "Downtown", "Michael's Crew", "Trevor's Gang");
            var message = alert.GetMessage();

            Assert.Contains("Downtown", message);
            Assert.Contains("Trevor's Gang", message);
        }

        [Fact]
        public void GetNotificationType_ForZoneCaptured_ReturnsSuccess()
        {
            var alert = new EventAlert(EventAlertType.ZoneCaptured, "Downtown", "Faction", null);
            Assert.Equal(NotificationType.Success, alert.GetNotificationType());
        }

        [Fact]
        public void GetNotificationType_ForZoneLost_ReturnsError()
        {
            var alert = new EventAlert(EventAlertType.ZoneLost, "Downtown", "Faction", null);
            Assert.Equal(NotificationType.Error, alert.GetNotificationType());
        }

        [Fact]
        public void GetNotificationType_ForAttackIncoming_ReturnsWarning()
        {
            var alert = new EventAlert(EventAlertType.AttackIncoming, "Downtown", "Faction", null);
            Assert.Equal(NotificationType.Warning, alert.GetNotificationType());
        }

        [Fact]
        public void GetNotificationType_ForAttackLaunched_ReturnsInfo()
        {
            var alert = new EventAlert(EventAlertType.AttackLaunched, "Downtown", "Faction", null);
            Assert.Equal(NotificationType.Info, alert.GetNotificationType());
        }

        [Fact]
        public void GetNotificationPriority_ForZoneCaptured_ReturnsHigh()
        {
            var alert = new EventAlert(EventAlertType.ZoneCaptured, "Downtown", "Faction", null);
            Assert.Equal(NotificationPriority.High, alert.GetNotificationPriority());
        }

        [Fact]
        public void GetNotificationPriority_ForAttackIncoming_ReturnsCritical()
        {
            var alert = new EventAlert(EventAlertType.AttackIncoming, "Downtown", "Faction", null);
            Assert.Equal(NotificationPriority.Critical, alert.GetNotificationPriority());
        }

        [Fact]
        public void GetNotificationPriority_ForVictoryImminent_ReturnsCritical()
        {
            var alert = new EventAlert(EventAlertType.VictoryImminent, "Downtown", "Faction", null);
            Assert.Equal(NotificationPriority.Critical, alert.GetNotificationPriority());
        }

        [Fact]
        public void GetNotificationPriority_ForDefeatImminent_ReturnsCritical()
        {
            var alert = new EventAlert(EventAlertType.DefeatImminent, "Downtown", "Faction", null);
            Assert.Equal(NotificationPriority.Critical, alert.GetNotificationPriority());
        }
    }

    public class EventAlertServiceTests
    {
        private readonly Mock<INotificationService> _mockNotificationService;
        private readonly EventAlertService _service;

        public EventAlertServiceTests()
        {
            _mockNotificationService = new Mock<INotificationService>();
            _service = new EventAlertService(_mockNotificationService.Object);
        }

        [Fact]
        public void Constructor_WithNullNotificationService_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new EventAlertService(null!));
        }

        [Fact]
        public void RaiseAlert_WithEventAlert_ShowsNotification()
        {
            var alert = new EventAlert(EventAlertType.ZoneCaptured, "Downtown", "Michael's Crew", null);

            _service.RaiseAlert(alert);

            _mockNotificationService.Verify(n => n.Show(It.Is<Notification>(notif =>
                notif.Title == "Zone Captured" &&
                notif.Type == NotificationType.Success &&
                notif.Priority == NotificationPriority.High
            )), Times.Once);
        }

        [Fact]
        public void RaiseAlert_WithNullAlert_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _service.RaiseAlert(null!));
        }

        [Fact]
        public void RaiseZoneCaptured_CreatesAndShowsZoneCapturedAlert()
        {
            _service.RaiseZoneCaptured("Downtown", "Michael's Crew");

            _mockNotificationService.Verify(n => n.Show(It.Is<Notification>(notif =>
                notif.Title == "Zone Captured" &&
                notif.Message.Contains("Downtown") &&
                notif.Message.Contains("Michael's Crew") &&
                notif.Type == NotificationType.Success
            )), Times.Once);
        }

        [Fact]
        public void RaiseZoneLost_CreatesAndShowsZoneLostAlert()
        {
            _service.RaiseZoneLost("Downtown", "Michael's Crew", "Trevor's Gang");

            _mockNotificationService.Verify(n => n.Show(It.Is<Notification>(notif =>
                notif.Title == "Zone Lost" &&
                notif.Message.Contains("Downtown") &&
                notif.Type == NotificationType.Error
            )), Times.Once);
        }

        [Fact]
        public void RaiseAttackIncoming_CreatesAndShowsAttackIncomingAlert()
        {
            _service.RaiseAttackIncoming("Downtown", "Michael's Crew", "Trevor's Gang");

            _mockNotificationService.Verify(n => n.Show(It.Is<Notification>(notif =>
                notif.Title == "Attack Incoming" &&
                notif.Message.Contains("Downtown") &&
                notif.Message.Contains("Trevor's Gang") &&
                notif.Type == NotificationType.Warning &&
                notif.Priority == NotificationPriority.Critical
            )), Times.Once);
        }

        [Fact]
        public void RaiseAttackLaunched_CreatesAndShowsAttackLaunchedAlert()
        {
            _service.RaiseAttackLaunched("Downtown", "Michael's Crew", "Trevor's Gang");

            _mockNotificationService.Verify(n => n.Show(It.Is<Notification>(notif =>
                notif.Title == "Attack Launched" &&
                notif.Message.Contains("Downtown") &&
                notif.Type == NotificationType.Info
            )), Times.Once);
        }

        [Fact]
        public void RaiseReinforcementsArriving_CreatesAndShowsReinforcementsAlert()
        {
            _service.RaiseReinforcementsArriving("Downtown", "Michael's Crew");

            _mockNotificationService.Verify(n => n.Show(It.Is<Notification>(notif =>
                notif.Title == "Reinforcements" &&
                notif.Message.Contains("Downtown")
            )), Times.Once);
        }

        [Fact]
        public void RaiseZoneContested_CreatesAndShowsContestedAlert()
        {
            _service.RaiseZoneContested("Downtown", "Michael's Crew", "Trevor's Gang");

            _mockNotificationService.Verify(n => n.Show(It.Is<Notification>(notif =>
                notif.Title == "Zone Contested" &&
                notif.Message.Contains("Downtown")
            )), Times.Once);
        }

        [Fact]
        public void RaiseVictoryImminent_CreatesAndShowsVictoryAlert()
        {
            _service.RaiseVictoryImminent("Michael's Crew");

            _mockNotificationService.Verify(n => n.Show(It.Is<Notification>(notif =>
                notif.Title == "Victory Imminent" &&
                notif.Priority == NotificationPriority.Critical
            )), Times.Once);
        }

        [Fact]
        public void RaiseDefeatImminent_CreatesAndShowsDefeatAlert()
        {
            _service.RaiseDefeatImminent("Michael's Crew", "Trevor's Gang");

            _mockNotificationService.Verify(n => n.Show(It.Is<Notification>(notif =>
                notif.Title == "Defeat Imminent" &&
                notif.Priority == NotificationPriority.Critical
            )), Times.Once);
        }

        [Fact]
        public void AlertHistory_IsInitiallyEmpty()
        {
            Assert.Empty(_service.AlertHistory);
        }

        [Fact]
        public void RaiseAlert_AddsAlertToHistory()
        {
            var alert = new EventAlert(EventAlertType.ZoneCaptured, "Downtown", "Michael's Crew", null);

            _service.RaiseAlert(alert);

            Assert.Single(_service.AlertHistory);
            Assert.Contains(alert, _service.AlertHistory);
        }

        [Fact]
        public void ClearHistory_RemovesAllAlerts()
        {
            _service.RaiseZoneCaptured("Downtown", "Faction");
            _service.RaiseZoneLost("Uptown", "Faction", "Enemy");

            _service.ClearHistory();

            Assert.Empty(_service.AlertHistory);
        }

        [Fact]
        public void AlertHistory_MaintainsMaximumSize()
        {
            var service = new EventAlertService(_mockNotificationService.Object, maxHistorySize: 3);

            service.RaiseZoneCaptured("Zone1", "Faction");
            service.RaiseZoneCaptured("Zone2", "Faction");
            service.RaiseZoneCaptured("Zone3", "Faction");
            service.RaiseZoneCaptured("Zone4", "Faction");

            Assert.Equal(3, service.AlertHistory.Count);
            // Most recent should be at the end
            Assert.Equal("Zone4", service.AlertHistory[2].ZoneName);
        }

        [Fact]
        public void GetRecentAlerts_ReturnsRequestedCount()
        {
            _service.RaiseZoneCaptured("Zone1", "Faction");
            _service.RaiseZoneCaptured("Zone2", "Faction");
            _service.RaiseZoneCaptured("Zone3", "Faction");

            var recent = _service.GetRecentAlerts(2);

            Assert.Equal(2, recent.Count);
        }

        [Fact]
        public void GetRecentAlerts_ReturnsMostRecent()
        {
            _service.RaiseZoneCaptured("Zone1", "Faction");
            _service.RaiseZoneCaptured("Zone2", "Faction");
            _service.RaiseZoneCaptured("Zone3", "Faction");

            var recent = _service.GetRecentAlerts(2);

            Assert.Equal("Zone3", recent[0].ZoneName);
            Assert.Equal("Zone2", recent[1].ZoneName);
        }

        [Fact]
        public void GetAlertsByType_ReturnsMatchingAlerts()
        {
            _service.RaiseZoneCaptured("Zone1", "Faction");
            _service.RaiseZoneLost("Zone2", "Faction", "Enemy");
            _service.RaiseZoneCaptured("Zone3", "Faction");

            var captured = _service.GetAlertsByType(EventAlertType.ZoneCaptured);

            Assert.Equal(2, captured.Count);
            Assert.All(captured, a => Assert.Equal(EventAlertType.ZoneCaptured, a.Type));
        }
    }
}
