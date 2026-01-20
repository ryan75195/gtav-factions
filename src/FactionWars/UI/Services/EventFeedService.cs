using System;
using System.Collections.Generic;
using System.Linq;
using FactionWars.Core.Interfaces;
using FactionWars.UI.Interfaces;
using FactionWars.UI.Models;

namespace FactionWars.UI.Services
{
    /// <summary>
    /// Service for managing the event feed display.
    /// Maintains a fixed-size list of recent events for rendering in the UI.
    /// </summary>
    public class EventFeedService : IEventFeedService
    {
        private readonly ITimeProvider _timeProvider;
        private readonly List<EventFeedEntry> _entries;
        private readonly int _maxEntries;

        /// <inheritdoc />
        public IReadOnlyList<EventFeedEntry> Entries => _entries.AsReadOnly();

        /// <inheritdoc />
        public int MaxEntries => _maxEntries;

        /// <inheritdoc />
        public int Count => _entries.Count;

        /// <inheritdoc />
        public bool IsEmpty => _entries.Count == 0;

        /// <summary>
        /// Creates a new EventFeedService.
        /// </summary>
        /// <param name="timeProvider">The time provider for timestamps.</param>
        /// <param name="maxEntries">The maximum number of entries to keep (default: 6).</param>
        public EventFeedService(ITimeProvider timeProvider, int maxEntries = 6)
        {
            _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
            _maxEntries = maxEntries;
            _entries = new List<EventFeedEntry>();
        }

        /// <inheritdoc />
        public void AddEntry(EventFeedEntry entry)
        {
            if (entry == null)
                throw new ArgumentNullException(nameof(entry));

            // Insert at the beginning (newest first)
            _entries.Insert(0, entry);

            // Remove oldest entries if over limit
            while (_entries.Count > _maxEntries)
            {
                _entries.RemoveAt(_entries.Count - 1);
            }
        }

        /// <inheritdoc />
        public void AddZoneCaptured(string zoneName, string factionName)
        {
            var message = $"{factionName} captured {zoneName}!";
            var entry = new EventFeedEntry(message, EventFeedCategory.ZoneCaptured, factionName, _timeProvider.UtcNow);
            AddEntry(entry);
        }

        /// <inheritdoc />
        public void AddZoneLost(string zoneName, string losingFactionName, string capturingFactionName)
        {
            var message = $"{losingFactionName} lost {zoneName} to {capturingFactionName}!";
            var entry = new EventFeedEntry(message, EventFeedCategory.ZoneLost, capturingFactionName, _timeProvider.UtcNow);
            AddEntry(entry);
        }

        /// <inheritdoc />
        public void AddCombatStarted(string zoneName, string attackerName, string defenderName)
        {
            var message = $"Combat started in {zoneName}! {attackerName} vs {defenderName}";
            var entry = new EventFeedEntry(message, EventFeedCategory.CombatStarted, attackerName, _timeProvider.UtcNow);
            AddEntry(entry);
        }

        /// <inheritdoc />
        public void AddCombatEnded(string zoneName, string defenderName, bool defenderWon)
        {
            var message = defenderWon
                ? $"{defenderName} defended {zoneName}!"
                : $"{defenderName} lost {zoneName}!";
            var entry = new EventFeedEntry(message, EventFeedCategory.CombatEnded, defenderName, _timeProvider.UtcNow);
            AddEntry(entry);
        }

        /// <inheritdoc />
        public void AddTroopsRecruited(string factionName, int count)
        {
            var message = $"{factionName} recruited {count} troops";
            var entry = new EventFeedEntry(message, EventFeedCategory.TroopsRecruited, factionName, _timeProvider.UtcNow);
            AddEntry(entry);
        }

        /// <inheritdoc />
        public void AddTroopsDeployed(string zoneName, string factionName, int count)
        {
            var message = $"{factionName} deployed {count} troops to {zoneName}";
            var entry = new EventFeedEntry(message, EventFeedCategory.TroopsDeployed, factionName, _timeProvider.UtcNow);
            AddEntry(entry);
        }

        /// <inheritdoc />
        public void AddIncomeReceived(string factionName, int amount)
        {
            var message = $"{factionName} received ${amount} income";
            var entry = new EventFeedEntry(message, EventFeedCategory.IncomeReceived, factionName, _timeProvider.UtcNow);
            AddEntry(entry);
        }

        /// <inheritdoc />
        public void AddGeneral(string message, string? factionName = null)
        {
            var entry = new EventFeedEntry(message, EventFeedCategory.General, factionName, _timeProvider.UtcNow);
            AddEntry(entry);
        }

        /// <inheritdoc />
        public void Clear()
        {
            _entries.Clear();
        }

        /// <inheritdoc />
        public IReadOnlyList<EventFeedEntry> GetEntriesByCategory(EventFeedCategory category)
        {
            return _entries.Where(e => e.Category == category).ToList().AsReadOnly();
        }

        /// <inheritdoc />
        public IReadOnlyList<EventFeedEntry> GetEntriesByFaction(string factionName)
        {
            return _entries.Where(e => e.FactionName == factionName).ToList().AsReadOnly();
        }
    }
}
