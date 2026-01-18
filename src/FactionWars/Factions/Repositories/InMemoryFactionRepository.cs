using FactionWars.Factions.Interfaces;
using FactionWars.Factions.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FactionWars.Factions.Repositories
{
    /// <summary>
    /// In-memory implementation of IFactionRepository.
    /// Stores factions and their states in memory for runtime use.
    /// </summary>
    public class InMemoryFactionRepository : IFactionRepository
    {
        private readonly Dictionary<string, Faction> _factions;
        private readonly Dictionary<string, FactionState> _states;

        public InMemoryFactionRepository()
        {
            _factions = new Dictionary<string, Faction>();
            _states = new Dictionary<string, FactionState>();
        }

        /// <inheritdoc />
        public int Count => _factions.Count;

        /// <inheritdoc />
        public void Add(Faction faction)
        {
            if (faction == null)
                throw new ArgumentNullException(nameof(faction));

            if (_factions.ContainsKey(faction.Id))
                throw new InvalidOperationException($"A faction with ID '{faction.Id}' already exists.");

            _factions[faction.Id] = faction;
        }

        /// <inheritdoc />
        public Faction? GetById(string id)
        {
            ValidateId(id);

            return _factions.TryGetValue(id, out var faction) ? faction : null;
        }

        /// <inheritdoc />
        public IEnumerable<Faction> GetAll()
        {
            return _factions.Values.ToList();
        }

        /// <inheritdoc />
        public void Update(Faction faction)
        {
            if (faction == null)
                throw new ArgumentNullException(nameof(faction));

            if (!_factions.ContainsKey(faction.Id))
                throw new InvalidOperationException($"Faction with ID '{faction.Id}' does not exist.");

            _factions[faction.Id] = faction;
        }

        /// <inheritdoc />
        public bool Remove(string id)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));

            var removed = _factions.Remove(id);
            if (removed)
            {
                _states.Remove(id);
            }
            return removed;
        }

        /// <inheritdoc />
        public bool Contains(string id)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));

            return _factions.ContainsKey(id);
        }

        /// <inheritdoc />
        public void Clear()
        {
            _factions.Clear();
            _states.Clear();
        }

        /// <inheritdoc />
        public IEnumerable<Faction> GetActive()
        {
            return _factions.Values.Where(f => f.IsActive).ToList();
        }

        /// <inheritdoc />
        public FactionState? GetState(string factionId)
        {
            ValidateId(factionId);

            return _states.TryGetValue(factionId, out var state) ? state : null;
        }

        /// <inheritdoc />
        public void SetState(FactionState state)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            if (!_factions.ContainsKey(state.FactionId))
                throw new InvalidOperationException($"Faction with ID '{state.FactionId}' does not exist.");

            _states[state.FactionId] = state;
        }

        /// <inheritdoc />
        public IEnumerable<FactionState> GetAllStates()
        {
            return _states.Values.ToList();
        }

        private static void ValidateId(string id)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("ID cannot be empty or whitespace.", nameof(id));
        }
    }
}
