using System;
using FactionWars.Core.Interfaces;
using FactionWars.Territory.Models;
using FactionWars.UI.Interfaces;
using FactionWars.UI.Models;

namespace FactionWars.UI.Services
{
    /// <summary>
    /// Service for managing zone under attack notifications with waypoint support.
    /// When a player's zone is attacked, shows a notification and allows setting a waypoint
    /// to the zone. Note: Only waypoints are used - no fast travel/teleportation.
    /// </summary>
    public class ZoneAttackNotificationService : IZoneAttackNotificationService
    {
        private const float NotificationDurationSeconds = 10.0f;

        private readonly INotificationService _notificationService;
        private readonly IGameBridge _gameBridge;
        private Zone? _activeAttackedZone;
        private bool _hasWaypointSet;

        /// <summary>
        /// Gets whether there is an active zone attack notification.
        /// </summary>
        public bool HasActiveZoneAttackNotification => _activeAttackedZone != null;

        /// <summary>
        /// Gets the ID of the zone currently under attack, or null if none.
        /// </summary>
        public string? ActiveAttackedZoneId => _activeAttackedZone?.Id;

        /// <summary>
        /// Gets whether a waypoint is currently set to an attacked zone.
        /// </summary>
        public bool HasWaypointSet => _hasWaypointSet;

        /// <summary>
        /// Creates a new zone attack notification service.
        /// </summary>
        /// <param name="notificationService">The notification service for displaying notifications.</param>
        /// <param name="gameBridge">The game bridge for setting waypoints.</param>
        /// <exception cref="ArgumentNullException">Thrown when notificationService or gameBridge is null.</exception>
        public ZoneAttackNotificationService(INotificationService notificationService, IGameBridge gameBridge)
        {
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _gameBridge = gameBridge ?? throw new ArgumentNullException(nameof(gameBridge));
        }

        /// <summary>
        /// Notifies the player that one of their zones is under attack.
        /// </summary>
        /// <param name="zone">The zone being attacked.</param>
        /// <param name="attackerFactionId">The ID of the attacking faction.</param>
        /// <exception cref="ArgumentNullException">Thrown when zone or attackerFactionId is null.</exception>
        /// <exception cref="ArgumentException">Thrown when attackerFactionId is empty or whitespace.</exception>
        public void NotifyZoneUnderAttack(Zone zone, string attackerFactionId)
        {
            if (zone == null)
                throw new ArgumentNullException(nameof(zone));
            if (attackerFactionId == null)
                throw new ArgumentNullException(nameof(attackerFactionId));
            if (string.IsNullOrWhiteSpace(attackerFactionId))
                throw new ArgumentException("Attacker faction ID cannot be empty or whitespace.", nameof(attackerFactionId));

            _activeAttackedZone = zone;

            var attackerName = GetFactionDisplayName(attackerFactionId);
            var title = "Zone Under Attack!";
            var message = $"{zone.Name} is under attack by {attackerName}!";

            _notificationService.ShowWarning(
                title,
                message,
                NotificationPriority.Critical,
                NotificationDurationSeconds);
        }

        /// <summary>
        /// Sets a waypoint to the currently attacked zone.
        /// Note: This only sets a waypoint for navigation - it does not teleport the player.
        /// </summary>
        /// <returns>True if waypoint was set, false if no active notification.</returns>
        public bool SetWaypointToAttackedZone()
        {
            if (_activeAttackedZone == null)
            {
                return false;
            }

            _gameBridge.SetWaypoint(_activeAttackedZone.Center);
            _hasWaypointSet = true;
            return true;
        }

        /// <summary>
        /// Gets the currently attacked zone, if any.
        /// </summary>
        /// <returns>The zone under attack, or null if none.</returns>
        public Zone? GetActiveAttackedZone()
        {
            return _activeAttackedZone;
        }

        /// <summary>
        /// Clears the active zone attack notification.
        /// </summary>
        public void ClearActiveNotification()
        {
            _activeAttackedZone = null;
        }

        /// <summary>
        /// Clears any waypoint set to an attacked zone.
        /// </summary>
        public void ClearWaypoint()
        {
            _gameBridge.ClearWaypoint();
            _hasWaypointSet = false;
        }

        private string GetFactionDisplayName(string factionId)
        {
            // Convert faction IDs to display names
            var lowerFactionId = factionId.ToLowerInvariant();

            if (lowerFactionId.Contains("michael") || lowerFactionId.Contains("santa"))
                return "Michael's Crew";
            if (lowerFactionId.Contains("trevor") || lowerFactionId.Contains("philips"))
                return "Trevor's Gang";
            if (lowerFactionId.Contains("franklin") || lowerFactionId.Contains("clinton"))
                return "Franklin's Crew";

            // Fallback: clean up the faction ID for display
            return factionId.Replace("faction_", "").Replace("_", " ");
        }
    }
}
