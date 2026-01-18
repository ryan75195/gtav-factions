using System;
using FactionWars.UI.Models;

namespace FactionWars.UI.Interfaces
{
    /// <summary>
    /// Interface for rendering notifications to the screen.
    /// Implementations handle the visual display of notifications.
    /// </summary>
    public interface INotificationRenderer
    {
        /// <summary>
        /// Gets the number of notifications currently being displayed.
        /// </summary>
        int ActiveNotificationCount { get; }

        /// <summary>
        /// Shows a notification on screen.
        /// </summary>
        /// <param name="notification">The notification to display.</param>
        void ShowNotification(Notification notification);

        /// <summary>
        /// Hides a specific notification by its ID.
        /// </summary>
        /// <param name="id">The notification ID to hide.</param>
        void HideNotification(Guid id);

        /// <summary>
        /// Clears all currently displayed notifications.
        /// </summary>
        void ClearAll();
    }
}
