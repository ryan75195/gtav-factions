using System.Collections.Generic;
using FactionWars.UI.Models;

namespace FactionWars.UI.Interfaces
{
    /// <summary>
    /// Service interface for raising and managing game event alerts.
    /// </summary>
    public interface IEventAlertService
    {
        /// <summary>
        /// Gets the history of raised alerts.
        /// </summary>
        IReadOnlyList<EventAlert> AlertHistory { get; }

        /// <summary>
        /// Raises an event alert, showing it to the player.
        /// </summary>
        /// <param name="alert">The alert to raise.</param>
        void RaiseAlert(EventAlert alert);

        /// <summary>
        /// Raises an alert for a zone being captured.
        /// </summary>
        /// <param name="zoneName">The name of the captured zone.</param>
        /// <param name="factionName">The name of the capturing faction.</param>
        void RaiseZoneCaptured(string zoneName, string factionName);

        /// <summary>
        /// Raises an alert for a zone being lost.
        /// </summary>
        /// <param name="zoneName">The name of the lost zone.</param>
        /// <param name="factionName">The name of the faction that lost the zone.</param>
        /// <param name="attackerName">The name of the attacking faction.</param>
        void RaiseZoneLost(string zoneName, string factionName, string attackerName);

        /// <summary>
        /// Raises an alert for an incoming attack.
        /// </summary>
        /// <param name="zoneName">The name of the zone being attacked.</param>
        /// <param name="defenderName">The name of the defending faction.</param>
        /// <param name="attackerName">The name of the attacking faction.</param>
        void RaiseAttackIncoming(string zoneName, string defenderName, string attackerName);

        /// <summary>
        /// Raises an alert for an attack being launched.
        /// </summary>
        /// <param name="zoneName">The name of the target zone.</param>
        /// <param name="attackerName">The name of the attacking faction.</param>
        /// <param name="defenderName">The name of the defending faction.</param>
        void RaiseAttackLaunched(string zoneName, string attackerName, string defenderName);

        /// <summary>
        /// Raises an alert for reinforcements arriving.
        /// </summary>
        /// <param name="zoneName">The name of the zone receiving reinforcements.</param>
        /// <param name="factionName">The name of the faction receiving reinforcements.</param>
        void RaiseReinforcementsArriving(string zoneName, string factionName);

        /// <summary>
        /// Raises an alert for a zone being contested.
        /// </summary>
        /// <param name="zoneName">The name of the contested zone.</param>
        /// <param name="factionName">The primary faction in the contest.</param>
        /// <param name="opponentName">The opposing faction.</param>
        void RaiseZoneContested(string zoneName, string factionName, string opponentName);

        /// <summary>
        /// Raises an alert for imminent victory.
        /// </summary>
        /// <param name="factionName">The name of the winning faction.</param>
        void RaiseVictoryImminent(string factionName);

        /// <summary>
        /// Raises an alert for imminent defeat.
        /// </summary>
        /// <param name="factionName">The name of the losing faction.</param>
        /// <param name="winnerName">The name of the winning faction.</param>
        void RaiseDefeatImminent(string factionName, string winnerName);

        /// <summary>
        /// Clears the alert history.
        /// </summary>
        void ClearHistory();

        /// <summary>
        /// Gets the most recent alerts.
        /// </summary>
        /// <param name="count">The number of alerts to retrieve.</param>
        /// <returns>A list of the most recent alerts, ordered from newest to oldest.</returns>
        IReadOnlyList<EventAlert> GetRecentAlerts(int count);

        /// <summary>
        /// Gets all alerts of a specific type from history.
        /// </summary>
        /// <param name="type">The type of alerts to retrieve.</param>
        /// <returns>A list of matching alerts.</returns>
        IReadOnlyList<EventAlert> GetAlertsByType(EventAlertType type);
    }
}
