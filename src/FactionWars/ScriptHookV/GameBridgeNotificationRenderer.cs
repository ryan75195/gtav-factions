using System;
using System.Collections.Generic;
using FactionWars.Core.Interfaces;
using FactionWars.UI.Interfaces;
using FactionWars.UI.Models;

namespace FactionWars.ScriptHookV
{
    /// <summary>
    /// Notification renderer that uses the game bridge to display notifications.
    /// Uses GTA V's native notification system.
    /// </summary>
    public class GameBridgeNotificationRenderer : INotificationRenderer
    {
        private readonly IGameBridge _gameBridge;
        private readonly Dictionary<Guid, Notification> _activeNotifications;

        /// <inheritdoc />
        public int ActiveNotificationCount => _activeNotifications.Count;

        /// <summary>
        /// Creates a new GameBridgeNotificationRenderer.
        /// </summary>
        /// <param name="gameBridge">The game bridge for showing notifications.</param>
        /// <exception cref="ArgumentNullException">Thrown if gameBridge is null.</exception>
        public GameBridgeNotificationRenderer(IGameBridge gameBridge)
        {
            _gameBridge = gameBridge ?? throw new ArgumentNullException(nameof(gameBridge));
            _activeNotifications = new Dictionary<Guid, Notification>();
        }

        /// <inheritdoc />
        public void ShowNotification(Notification notification)
        {
            if (notification == null)
                throw new ArgumentNullException(nameof(notification));

            // Format message with title
            var formattedMessage = $"~b~{notification.Title}~w~\n{notification.Message}";

            // Apply type-specific formatting
            formattedMessage = ApplyTypeFormatting(formattedMessage, notification.Type);

            // Show using game bridge
            _gameBridge.ShowNotification(formattedMessage);

            // Track the notification
            _activeNotifications[notification.Id] = notification;
        }

        /// <inheritdoc />
        public void HideNotification(Guid id)
        {
            // GTA V notifications auto-dismiss, so we just remove from tracking
            _activeNotifications.Remove(id);
        }

        /// <inheritdoc />
        public void ClearAll()
        {
            _activeNotifications.Clear();
        }

        /// <summary>
        /// Applies GTA V text formatting based on notification type.
        /// </summary>
        private string ApplyTypeFormatting(string message, NotificationType type)
        {
            return type switch
            {
                NotificationType.Success => $"~g~{message}",
                NotificationType.Warning => $"~y~{message}",
                NotificationType.Error => $"~r~{message}",
                _ => message
            };
        }
    }
}
