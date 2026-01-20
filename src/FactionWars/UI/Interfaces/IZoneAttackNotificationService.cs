using FactionWars.Territory.Models;

namespace FactionWars.UI.Interfaces
{
    /// <summary>
    /// Service interface for managing zone under attack notifications with waypoint support.
    /// Handles notifications when a player's zone is attacked and allows setting waypoints to the attacked zone.
    /// </summary>
    public interface IZoneAttackNotificationService
    {
        /// <summary>
        /// Gets whether there is an active zone attack notification.
        /// </summary>
        bool HasActiveZoneAttackNotification { get; }

        /// <summary>
        /// Gets the ID of the zone currently under attack, or null if none.
        /// </summary>
        string? ActiveAttackedZoneId { get; }

        /// <summary>
        /// Gets whether a waypoint is currently set to an attacked zone.
        /// </summary>
        bool HasWaypointSet { get; }

        /// <summary>
        /// Notifies the player that one of their zones is under attack.
        /// </summary>
        /// <param name="zone">The zone being attacked.</param>
        /// <param name="attackerFactionId">The ID of the attacking faction.</param>
        void NotifyZoneUnderAttack(Zone zone, string attackerFactionId);

        /// <summary>
        /// Sets a waypoint to the currently attacked zone.
        /// Note: This only sets a waypoint for navigation - it does not teleport the player.
        /// </summary>
        /// <returns>True if waypoint was set, false if no active notification.</returns>
        bool SetWaypointToAttackedZone();

        /// <summary>
        /// Gets the currently attacked zone, if any.
        /// </summary>
        /// <returns>The zone under attack, or null if none.</returns>
        Zone? GetActiveAttackedZone();

        /// <summary>
        /// Clears the active zone attack notification.
        /// </summary>
        void ClearActiveNotification();

        /// <summary>
        /// Clears any waypoint set to an attacked zone.
        /// </summary>
        void ClearWaypoint();
    }
}
