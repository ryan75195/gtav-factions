using System;
using FactionWars.Core.Interfaces;
using FactionWars.ScriptHookV;
using FactionWars.UI.Interfaces;
using FactionWars.UI.Models;
using Moq;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV
{
    public class GameBridgeNotificationRendererTests
    {
        private readonly Mock<IGameBridge> _mockGameBridge;
        private readonly GameBridgeNotificationRenderer _renderer;

        public GameBridgeNotificationRendererTests()
        {
            _mockGameBridge = new Mock<IGameBridge>();
            _renderer = new GameBridgeNotificationRenderer(_mockGameBridge.Object);
        }

        [Fact]
        public void Constructor_WithNullGameBridge_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new GameBridgeNotificationRenderer(null!));
        }

        [Fact]
        public void GameBridgeNotificationRenderer_ImplementsINotificationRenderer()
        {
            Assert.IsAssignableFrom<INotificationRenderer>(_renderer);
        }

        [Fact]
        public void ActiveNotificationCount_InitiallyReturnsZero()
        {
            Assert.Equal(0, _renderer.ActiveNotificationCount);
        }

        [Fact]
        public void ShowNotification_WithValidNotification_CallsGameBridgeShowNotification()
        {
            var notification = new Notification("Title", "Message", NotificationType.Info);

            _renderer.ShowNotification(notification);

            _mockGameBridge.Verify(g => g.ShowNotification(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public void ShowNotification_WithNullNotification_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _renderer.ShowNotification(null!));
        }

        [Fact]
        public void ShowNotification_IncrementsActiveNotificationCount()
        {
            var notification = new Notification("Title", "Message", NotificationType.Info);

            _renderer.ShowNotification(notification);

            Assert.Equal(1, _renderer.ActiveNotificationCount);
        }

        [Fact]
        public void ShowNotification_WithInfoType_FormatsMessageCorrectly()
        {
            var notification = new Notification("Info Title", "Info message", NotificationType.Info);
            string capturedMessage = null!;
            _mockGameBridge.Setup(g => g.ShowNotification(It.IsAny<string>()))
                .Callback<string>(msg => capturedMessage = msg);

            _renderer.ShowNotification(notification);

            Assert.Contains("Info Title", capturedMessage);
            Assert.Contains("Info message", capturedMessage);
        }

        [Fact]
        public void ShowNotification_WithSuccessType_AppliesGreenFormatting()
        {
            var notification = new Notification("Success Title", "Success message", NotificationType.Success);
            string capturedMessage = null!;
            _mockGameBridge.Setup(g => g.ShowNotification(It.IsAny<string>()))
                .Callback<string>(msg => capturedMessage = msg);

            _renderer.ShowNotification(notification);

            Assert.StartsWith("~g~", capturedMessage);
        }

        [Fact]
        public void ShowNotification_WithWarningType_AppliesYellowFormatting()
        {
            var notification = new Notification("Warning Title", "Warning message", NotificationType.Warning);
            string capturedMessage = null!;
            _mockGameBridge.Setup(g => g.ShowNotification(It.IsAny<string>()))
                .Callback<string>(msg => capturedMessage = msg);

            _renderer.ShowNotification(notification);

            Assert.StartsWith("~y~", capturedMessage);
        }

        [Fact]
        public void ShowNotification_WithErrorType_AppliesRedFormatting()
        {
            var notification = new Notification("Error Title", "Error message", NotificationType.Error);
            string capturedMessage = null!;
            _mockGameBridge.Setup(g => g.ShowNotification(It.IsAny<string>()))
                .Callback<string>(msg => capturedMessage = msg);

            _renderer.ShowNotification(notification);

            Assert.StartsWith("~r~", capturedMessage);
        }

        [Fact]
        public void HideNotification_DecrementsActiveNotificationCount()
        {
            var notification = new Notification("Title", "Message", NotificationType.Info);
            _renderer.ShowNotification(notification);

            _renderer.HideNotification(notification.Id);

            Assert.Equal(0, _renderer.ActiveNotificationCount);
        }

        [Fact]
        public void HideNotification_WithUnknownId_DoesNotThrow()
        {
            var exception = Record.Exception(() => _renderer.HideNotification(Guid.NewGuid()));

            Assert.Null(exception);
        }

        [Fact]
        public void ClearAll_SetsActiveNotificationCountToZero()
        {
            var notification1 = new Notification("Title1", "Message1", NotificationType.Info);
            var notification2 = new Notification("Title2", "Message2", NotificationType.Warning);
            _renderer.ShowNotification(notification1);
            _renderer.ShowNotification(notification2);

            _renderer.ClearAll();

            Assert.Equal(0, _renderer.ActiveNotificationCount);
        }

        [Fact]
        public void ShowNotification_MultipleNotifications_TracksCorrectCount()
        {
            var notification1 = new Notification("Title1", "Message1", NotificationType.Info);
            var notification2 = new Notification("Title2", "Message2", NotificationType.Warning);
            var notification3 = new Notification("Title3", "Message3", NotificationType.Success);

            _renderer.ShowNotification(notification1);
            _renderer.ShowNotification(notification2);
            _renderer.ShowNotification(notification3);

            Assert.Equal(3, _renderer.ActiveNotificationCount);
        }
    }
}
