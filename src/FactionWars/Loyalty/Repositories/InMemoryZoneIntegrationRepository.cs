using FactionWars.Loyalty.Interfaces;
using FactionWars.Loyalty.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FactionWars.Loyalty.Repositories
{
    /// <summary>
    /// In-memory implementation of the zone integration repository.
    /// </summary>
    public class InMemoryZoneIntegrationRepository : IZoneIntegrationRepository
    {
        private readonly Dictionary<string, ZoneIntegrationState> _states = new Dictionary<string, ZoneIntegrationState>();

        /// <inheritdoc />
        public void Add(ZoneIntegrationState state)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            if (_states.ContainsKey(state.ZoneId))
                throw new InvalidOperationException($"Integration state for zone '{state.ZoneId}' already exists.");

            _states[state.ZoneId] = state;
        }

        /// <inheritdoc />
        public ZoneIntegrationState? GetByZoneId(string zoneId)
        {
            if (zoneId == null)
                throw new ArgumentNullException(nameof(zoneId));

            return _states.TryGetValue(zoneId, out var state) ? state : null;
        }

        /// <inheritdoc />
        public bool Remove(string zoneId)
        {
            if (zoneId == null)
                throw new ArgumentNullException(nameof(zoneId));

            return _states.Remove(zoneId);
        }

        /// <inheritdoc />
        public IEnumerable<ZoneIntegrationState> GetAll()
        {
            return _states.Values.ToList();
        }

        /// <inheritdoc />
        public IEnumerable<ZoneIntegrationState> GetByFaction(string factionId)
        {
            if (factionId == null)
                throw new ArgumentNullException(nameof(factionId));

            return _states.Values
                .Where(s => s.NewControllerFactionId == factionId)
                .ToList();
        }

        /// <inheritdoc />
        public IEnumerable<ZoneIntegrationState> GetPendingIntegration()
        {
            return _states.Values
                .Where(s => !s.IsFullyIntegrated)
                .ToList();
        }

        /// <inheritdoc />
        public void Update(ZoneIntegrationState state)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            if (!_states.ContainsKey(state.ZoneId))
                throw new InvalidOperationException($"Integration state for zone '{state.ZoneId}' does not exist.");

            _states[state.ZoneId] = state;
        }
    }
}
