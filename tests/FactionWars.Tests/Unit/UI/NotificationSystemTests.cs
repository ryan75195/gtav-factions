using Xunit;
using Moq;
using FactionWars.UI.Models;
using FactionWars.UI.Interfaces;
using FactionWars.UI.Services;
using System;
using System.Collections.Generic;

namespace FactionWars.Tests.Unit.UI
{
    public class NotificationTypeTests
    {
        [Fact]
        public void NotificationType_HasExpectedValues()
        {
            Assert.Equal(0, (int)NotificationType.Info);
            Assert.Equal(1, (int)NotificationType.Success);
            Assert.Equal(2, (int)NotificationType.Warning);
            Assert.Equal(3, (int)NotificationType.Error);
        }
    }

    public class NotificationPriorityTests
    {
        [Fact]
        public void NotificationPriority_HasExpectedValues()
        {
            Assert.Equal(0, (int)NotificationPriority.Low);
            Assert.Equal(1, (int)NotificationPriority.Normal);
            Assert.Equal(2, (int)NotificationPriority.High);
            Assert.Equal(3, (int)NotificationPriority.Critical);
        }
    }

    public class NotificationTests
    {
        [Fact]
        public void Constructor_WithValidValues_SetsPropertiesCorrectly()
        {
            var notification = new Notification(
                "Zone Captured",
                "Downtown is now under your control!",
                NotificationType.Success,
                NotificationPriority.High,
                5.0f);

            Assert.Equal("Zone Captured", notification.Title);
            Assert.Equal("Downtown is now under your control!", notification.Message);
            Assert.Equal(NotificationType.Success, notification.Type);
            Assert.Equal(NotificationPriority.High, notification.Priority);
            Assert.Equal(5.0f, notification.DurationSeconds);
            Assert.NotEqual(Guid.Empty, notification.Id);
            Assert.True(notification.CreatedAt <= DateTime.UtcNow);
        }

        [Fact]
        public void Constructor_WithNullTitle_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new Notification(
                null!,
                "Message",
                NotificationType.Info,
                NotificationPriority.Normal,
                3.0f));
        }

        [Fact]
        public void Constructor_WithEmptyTitle_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new Notification(
                "",
                "Message",
                NotificationType.Info,
                NotificationPriority.Normal,
                3.0f));
        }

        [Fact]
        public void Constructor_WithWhitespaceTitle_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new Notification(
                "   ",
                "Message",
                NotificationType.Info,
                NotificationPriority.Normal,
                3.0f));
        }

        [Fact]
        public void Constructor_WithNullMessage_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new Notification(
                "Title",
                null!,
                NotificationType.Info,
                NotificationPriority.Normal,
                3.0f));
        }

        [Fact]
        public void Constructor_WithEmptyMessage_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new Notification(
                "Title",
                "",
                NotificationType.Info,
                NotificationPriority.Normal,
                3.0f));
        }

        [Theory]
        [InlineData(0f)]
        [InlineData(-1f)]
        public void Constructor_WithInvalidDuration_ThrowsArgumentOutOfRangeException(float duration)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new Notification(
                "Title",
                "Message",
                NotificationType.Info,
                NotificationPriority.Normal,
                duration));
        }

        [Fact]
        public void Constructor_WithDefaultDuration_Uses3Seconds()
        {
            var notification = new Notification("Title", "Message", NotificationType.Info);

            Assert.Equal(3.0f, notification.DurationSeconds);
        }

        [Fact]
        public void Constructor_WithDefaultPriority_UsesNormal()
        {
            var notification = new Notification("Title", "Message", NotificationType.Info);

            Assert.Equal(NotificationPriority.Normal, notification.Priority);
        }

        [Fact]
        public void Equals_WithSameId_ReturnsTrue()
        {
            var notification1 = new Notification("Title", "Message", NotificationType.Info);

            Assert.True(notification1.Equals(notification1));
        }

        [Fact]
        public void Equals_WithDifferentNotifications_ReturnsFalse()
        {
            var notification1 = new Notification("Title1", "Message", NotificationType.Info);
            var notification2 = new Notification("Title2", "Message", NotificationType.Info);

            Assert.False(notification1.Equals(notification2));
        }

        [Fact]
        public void GetHashCode_ReturnsIdHashCode()
        {
            var notification = new Notification("Title", "Message", NotificationType.Info);

            Assert.Equal(notification.Id.GetHashCode(), notification.GetHashCode());
        }
    }

    public class NotificationRendererInterfaceTests
    {
        [Fact]
        public void INotificationRenderer_CanBeMocked()
        {
            var mock = new Mock<INotificationRenderer>();
            mock.Setup(r => r.ShowNotification(It.IsAny<Notification>()));
            mock.Setup(r => r.HideNotification(It.IsAny<Guid>()));
            mock.Setup(r => r.ClearAll());
            mock.SetupGet(r => r.ActiveNotificationCount).Returns(2);

            Assert.NotNull(mock.Object);
            Assert.Equal(2, mock.Object.ActiveNotificationCount);
        }
    }

    public class NotificationServiceTests
    {
        private readonly Mock<INotificationRenderer> _mockRenderer;
        private readonly NotificationService _service;

        public NotificationServiceTests()
        {
            _mockRenderer = new Mock<INotificationRenderer>();
            _service = new NotificationService(_mockRenderer.Object);
        }

        [Fact]
        public void Constructor_WithNullRenderer_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new NotificationService(null!));
        }

        [Fact]
        public void Show_WithNotification_QueuesAndShowsNotification()
        {
            var notification = new Notification("Title", "Message", NotificationType.Info);

            _service.Show(notification);

            _mockRenderer.Verify(r => r.ShowNotification(notification), Times.Once);
        }

        [Fact]
        public void Show_WithNullNotification_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _service.Show(null!));
        }

        [Fact]
        public void ShowInfo_CreatesInfoNotification()
        {
            _service.ShowInfo("Info Title", "Info message");

            _mockRenderer.Verify(r => r.ShowNotification(It.Is<Notification>(n =>
                n.Title == "Info Title" &&
                n.Message == "Info message" &&
                n.Type == NotificationType.Info
            )), Times.Once);
        }

        [Fact]
        public void ShowSuccess_CreatesSuccessNotification()
        {
            _service.ShowSuccess("Success Title", "Success message");

            _mockRenderer.Verify(r => r.ShowNotification(It.Is<Notification>(n =>
                n.Title == "Success Title" &&
                n.Message == "Success message" &&
                n.Type == NotificationType.Success
            )), Times.Once);
        }

        [Fact]
        public void ShowWarning_CreatesWarningNotification()
        {
            _service.ShowWarning("Warning Title", "Warning message");

            _mockRenderer.Verify(r => r.ShowNotification(It.Is<Notification>(n =>
                n.Title == "Warning Title" &&
                n.Message == "Warning message" &&
                n.Type == NotificationType.Warning
            )), Times.Once);
        }

        [Fact]
        public void ShowError_CreatesErrorNotification()
        {
            _service.ShowError("Error Title", "Error message");

            _mockRenderer.Verify(r => r.ShowNotification(It.Is<Notification>(n =>
                n.Title == "Error Title" &&
                n.Message == "Error message" &&
                n.Type == NotificationType.Error
            )), Times.Once);
        }

        [Fact]
        public void Dismiss_CallsRendererHideNotification()
        {
            var id = Guid.NewGuid();

            _service.Dismiss(id);

            _mockRenderer.Verify(r => r.HideNotification(id), Times.Once);
        }

        [Fact]
        public void ClearAll_CallsRendererClearAll()
        {
            _service.ClearAll();

            _mockRenderer.Verify(r => r.ClearAll(), Times.Once);
        }

        [Fact]
        public void ActiveNotificationCount_ReturnsRendererCount()
        {
            _mockRenderer.Setup(r => r.ActiveNotificationCount).Returns(5);

            Assert.Equal(5, _service.ActiveNotificationCount);
        }

        [Fact]
        public void Show_WithHighPriorityNotification_ShowsImmediately()
        {
            var lowPriority = new Notification("Low", "Low message", NotificationType.Info, NotificationPriority.Low);
            var highPriority = new Notification("High", "High message", NotificationType.Warning, NotificationPriority.High);

            _service.Show(lowPriority);
            _service.Show(highPriority);

            // Both should be shown
            _mockRenderer.Verify(r => r.ShowNotification(lowPriority), Times.Once);
            _mockRenderer.Verify(r => r.ShowNotification(highPriority), Times.Once);
        }

        [Fact]
        public void ShowWithPriority_CreatesNotificationWithPriority()
        {
            _service.ShowInfo("Title", "Message", NotificationPriority.Critical);

            _mockRenderer.Verify(r => r.ShowNotification(It.Is<Notification>(n =>
                n.Priority == NotificationPriority.Critical
            )), Times.Once);
        }

        [Fact]
        public void ShowWithDuration_CreatesNotificationWithCustomDuration()
        {
            _service.ShowInfo("Title", "Message", NotificationPriority.Normal, 10.0f);

            _mockRenderer.Verify(r => r.ShowNotification(It.Is<Notification>(n =>
                n.DurationSeconds == 10.0f
            )), Times.Once);
        }

        [Fact]
        public void Show_WithMaxNotificationsReached_QueuesNotification()
        {
            var service = new NotificationService(_mockRenderer.Object, maxVisibleNotifications: 3);

            _mockRenderer.Setup(r => r.ActiveNotificationCount).Returns(3);

            var notification = new Notification("Title", "Message", NotificationType.Info);
            service.Show(notification);

            // Should be queued, not immediately shown
            Assert.Equal(1, service.QueuedNotificationCount);
        }

        [Fact]
        public void QueuedNotificationCount_ReturnsCorrectCount()
        {
            var service = new NotificationService(_mockRenderer.Object, maxVisibleNotifications: 1);

            // First notification can be shown (count is 0)
            // Subsequent notifications get queued (count is 1)
            _mockRenderer.SetupSequence(r => r.ActiveNotificationCount)
                .Returns(0)  // First Show - slot available, notification is shown
                .Returns(1)  // Second Show - at capacity, notification is queued
                .Returns(1); // Third Show - at capacity, notification is queued

            service.Show(new Notification("Title1", "Message1", NotificationType.Info));
            service.Show(new Notification("Title2", "Message2", NotificationType.Info));
            service.Show(new Notification("Title3", "Message3", NotificationType.Info));

            Assert.Equal(2, service.QueuedNotificationCount);
        }

        [Fact]
        public void ProcessQueue_ShowsQueuedNotification_WhenSlotAvailable()
        {
            var service = new NotificationService(_mockRenderer.Object, maxVisibleNotifications: 1);

            var notification = new Notification("Queued", "Queued message", NotificationType.Info);

            // First call returns 1 (full), second returns 0 (slot available)
            _mockRenderer.SetupSequence(r => r.ActiveNotificationCount)
                .Returns(1)  // First Show - notification gets queued
                .Returns(0); // ProcessQueue - slot is available

            service.Show(notification);
            Assert.Equal(1, service.QueuedNotificationCount);

            service.ProcessQueue();

            _mockRenderer.Verify(r => r.ShowNotification(notification), Times.Once);
            Assert.Equal(0, service.QueuedNotificationCount);
        }
    }
}
