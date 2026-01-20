using System.Collections.Generic;
using FactionWars.UI.Models;

namespace FactionWars.UI.Interfaces
{
    /// <summary>
    /// Service interface for managing the event feed display.
    /// The event feed shows recent world events in a scrolling list at the bottom-left of the screen.
    /// </summary>
    public interface IEventFeedService
    {
        /// <summary>
        /// Gets the current list of feed entries, ordered from newest to oldest.
        /// </summary>
        IReadOnlyList<EventFeedEntry> Entries { get; }

        /// <summary>
        /// Gets the maximum number of entries to keep in the feed.
        /// </summary>
        int MaxEntries { get; }

        /// <summary>
        /// Gets the number of entries currently in the feed.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Gets whether the feed is empty.
        /// </summary>
        bool IsEmpty { get; }

        /// <summary>
        /// Adds an entry to the feed.
        /// </summary>
        /// <param name="entry">The entry to add.</param>
        void AddEntry(EventFeedEntry entry);

        /// <summary>
        /// Adds a zone captured event to the feed.
        /// </summary>
        /// <param name="zoneName">The name of the captured zone.</param>
        /// <param name="factionName">The name of the faction that captured it.</param>
        void AddZoneCaptured(string zoneName, string factionName);

        /// <summary>
        /// Adds a zone lost event to the feed.
        /// </summary>
        /// <param name="zoneName">The name of the lost zone.</param>
        /// <param name="losingFactionName">The name of the faction that lost the zone.</param>
        /// <param name="capturingFactionName">The name of the faction that captured the zone.</param>
        void AddZoneLost(string zoneName, string losingFactionName, string capturingFactionName);

        /// <summary>
        /// Adds a combat started event to the feed.
        /// </summary>
        /// <param name="zoneName">The name of the zone where combat started.</param>
        /// <param name="attackerName">The name of the attacking faction.</param>
        /// <param name="defenderName">The name of the defending faction.</param>
        void AddCombatStarted(string zoneName, string attackerName, string defenderName);

        /// <summary>
        /// Adds a combat ended event to the feed.
        /// </summary>
        /// <param name="zoneName">The name of the zone where combat ended.</param>
        /// <param name="defenderName">The name of the defending faction.</param>
        /// <param name="defenderWon">Whether the defender won the combat.</param>
        void AddCombatEnded(string zoneName, string defenderName, bool defenderWon);

        /// <summary>
        /// Adds a troops recruited event to the feed.
        /// </summary>
        /// <param name="factionName">The name of the faction that recruited troops.</param>
        /// <param name="count">The number of troops recruited.</param>
        void AddTroopsRecruited(string factionName, int count);

        /// <summary>
        /// Adds a troops deployed event to the feed.
        /// </summary>
        /// <param name="zoneName">The name of the zone where troops were deployed.</param>
        /// <param name="factionName">The name of the faction that deployed troops.</param>
        /// <param name="count">The number of troops deployed.</param>
        void AddTroopsDeployed(string zoneName, string factionName, int count);

        /// <summary>
        /// Adds an income received event to the feed.
        /// </summary>
        /// <param name="factionName">The name of the faction that received income.</param>
        /// <param name="amount">The amount of income received.</param>
        void AddIncomeReceived(string factionName, int amount);

        /// <summary>
        /// Adds a general event to the feed.
        /// </summary>
        /// <param name="message">The event message.</param>
        /// <param name="factionName">Optional faction name for color-coding.</param>
        void AddGeneral(string message, string? factionName = null);

        /// <summary>
        /// Clears all entries from the feed.
        /// </summary>
        void Clear();

        /// <summary>
        /// Gets entries of a specific category.
        /// </summary>
        /// <param name="category">The category to filter by.</param>
        /// <returns>A list of matching entries.</returns>
        IReadOnlyList<EventFeedEntry> GetEntriesByCategory(EventFeedCategory category);

        /// <summary>
        /// Gets entries for a specific faction.
        /// </summary>
        /// <param name="factionName">The faction name to filter by.</param>
        /// <returns>A list of matching entries.</returns>
        IReadOnlyList<EventFeedEntry> GetEntriesByFaction(string factionName);
    }
}
