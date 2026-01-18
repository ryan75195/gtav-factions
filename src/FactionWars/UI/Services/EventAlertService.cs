using System;
using System.Collections.Generic;
using System.Linq;
using FactionWars.UI.Interfaces;
using FactionWars.UI.Models;

namespace FactionWars.UI.Services
{
    /// <summary>
    /// Service for raising and managing game event alerts.
    /// </summary>
    public class EventAlertService : IEventAlertService
    {
        private const int DefaultMaxHistorySize = 100;
        private const float DefaultAlertDuration = 5.0f;

        private readonly INotificationService _notificationService;
        private readonly int _maxHistorySize;
        private readonly List<EventAlert> _alertHistory;

        /// <summary>
        /// Gets the history of raised alerts.
        /// </summary>
        public IReadOnlyList<EventAlert> AlertHistory => _alertHistory.AsReadOnly();

        /// <summary>
        /// Creates a new event alert service with default settings.
        /// </summary>
        /// <param name="notificationService">The notification service for displaying alerts.</param>
        public EventAlertService(INotificationService notificationService)
            : this(notificationService, DefaultMaxHistorySize)
        {
        }

        /// <summary>
        /// Creates a new event alert service with custom settings.
        /// </summary>
        /// <param name="notificationService">The notification service for displaying alerts.</param>
        /// <param name="maxHistorySize">Maximum number of alerts to keep in history.</param>
        public EventAlertService(INotificationService notificationService, int maxHistorySize)
        {
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _maxHistorySize = maxHistorySize > 0 ? maxHistorySize : DefaultMaxHistorySize;
            _alertHistory = new List<EventAlert>();
        }

        /// <summary>
        /// Raises an event alert, showing it to the player.
        /// </summary>
        public void RaiseAlert(EventAlert alert)
        {
            if (alert == null)
                throw new ArgumentNullException(nameof(alert));

            // Add to history
            AddToHistory(alert);

            // Create and show notification
            var notification = new Notification(
                alert.GetTitle(),
                alert.GetMessage(),
                alert.GetNotificationType(),
                alert.GetNotificationPriority(),
                DefaultAlertDuration);

            _notificationService.Show(notification);
        }

        /// <summary>
        /// Raises an alert for a zone being captured.
        /// </summary>
        public void RaiseZoneCaptured(string zoneName, string factionName)
        {
            var alert = new EventAlert(EventAlertType.ZoneCaptured, zoneName, factionName, null);
            RaiseAlert(alert);
        }

        /// <summary>
        /// Raises an alert for a zone being lost.
        /// </summary>
        public void RaiseZoneLost(string zoneName, string factionName, string attackerName)
        {
            var alert = new EventAlert(EventAlertType.ZoneLost, zoneName, factionName, attackerName);
            RaiseAlert(alert);
        }

        /// <summary>
        /// Raises an alert for an incoming attack.
        /// </summary>
        public void RaiseAttackIncoming(string zoneName, string defenderName, string attackerName)
        {
            var alert = new EventAlert(EventAlertType.AttackIncoming, zoneName, defenderName, attackerName);
            RaiseAlert(alert);
        }

        /// <summary>
        /// Raises an alert for an attack being launched.
        /// </summary>
        public void RaiseAttackLaunched(string zoneName, string attackerName, string defenderName)
        {
            var alert = new EventAlert(EventAlertType.AttackLaunched, zoneName, attackerName, defenderName);
            RaiseAlert(alert);
        }

        /// <summary>
        /// Raises an alert for reinforcements arriving.
        /// </summary>
        public void RaiseReinforcementsArriving(string zoneName, string factionName)
        {
            var alert = new EventAlert(EventAlertType.ReinforcementsArriving, zoneName, factionName, null);
            RaiseAlert(alert);
        }

        /// <summary>
        /// Raises an alert for a zone being contested.
        /// </summary>
        public void RaiseZoneContested(string zoneName, string factionName, string opponentName)
        {
            var alert = new EventAlert(EventAlertType.ZoneContested, zoneName, factionName, opponentName);
            RaiseAlert(alert);
        }

        /// <summary>
        /// Raises an alert for imminent victory.
        /// </summary>
        public void RaiseVictoryImminent(string factionName)
        {
            // Use a placeholder zone name since victory is global
            var alert = new EventAlert(EventAlertType.VictoryImminent, "Los Santos", factionName, null);
            RaiseAlert(alert);
        }

        /// <summary>
        /// Raises an alert for imminent defeat.
        /// </summary>
        public void RaiseDefeatImminent(string factionName, string winnerName)
        {
            // Use a placeholder zone name since defeat is global
            var alert = new EventAlert(EventAlertType.DefeatImminent, "Los Santos", factionName, winnerName);
            RaiseAlert(alert);
        }

        /// <summary>
        /// Clears the alert history.
        /// </summary>
        public void ClearHistory()
        {
            _alertHistory.Clear();
        }

        /// <summary>
        /// Gets the most recent alerts.
        /// </summary>
        public IReadOnlyList<EventAlert> GetRecentAlerts(int count)
        {
            if (count <= 0)
                return new List<EventAlert>().AsReadOnly();

            return _alertHistory
                .Skip(Math.Max(0, _alertHistory.Count - count))
                .Reverse()
                .ToList()
                .AsReadOnly();
        }

        /// <summary>
        /// Gets all alerts of a specific type from history.
        /// </summary>
        public IReadOnlyList<EventAlert> GetAlertsByType(EventAlertType type)
        {
            return _alertHistory
                .Where(a => a.Type == type)
                .ToList()
                .AsReadOnly();
        }

        private void AddToHistory(EventAlert alert)
        {
            _alertHistory.Add(alert);

            // Trim history if it exceeds max size
            while (_alertHistory.Count > _maxHistorySize)
            {
                _alertHistory.RemoveAt(0);
            }
        }
    }
}
