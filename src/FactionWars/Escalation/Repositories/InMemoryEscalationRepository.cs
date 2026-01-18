using System;
using System.Collections.Generic;
using System.Linq;
using FactionWars.Escalation.Interfaces;
using FactionWars.Escalation.Models;

namespace FactionWars.Escalation.Repositories
{
    /// <summary>
    /// In-memory implementation of the escalation repository.
    /// </summary>
    public class InMemoryEscalationRepository : IEscalationRepository
    {
        private readonly Dictionary<string, FactionEscalation> _escalations = new Dictionary<string, FactionEscalation>();

        /// <inheritdoc />
        public bool Add(FactionEscalation escalation)
        {
            if (escalation == null)
                throw new ArgumentNullException(nameof(escalation));

            if (_escalations.ContainsKey(escalation.FactionId))
                return false;

            _escalations[escalation.FactionId] = escalation;
            return true;
        }

        /// <inheritdoc />
        public FactionEscalation? GetByFactionId(string factionId)
        {
            if (factionId == null)
                throw new ArgumentNullException(nameof(factionId));
            if (string.IsNullOrWhiteSpace(factionId))
                throw new ArgumentException("Faction ID cannot be empty or whitespace.", nameof(factionId));

            return _escalations.TryGetValue(factionId, out var escalation) ? escalation : null;
        }

        /// <inheritdoc />
        public IEnumerable<FactionEscalation> GetAll()
        {
            return _escalations.Values.ToList();
        }

        /// <inheritdoc />
        public bool Update(FactionEscalation escalation)
        {
            if (escalation == null)
                throw new ArgumentNullException(nameof(escalation));

            if (!_escalations.ContainsKey(escalation.FactionId))
                return false;

            _escalations[escalation.FactionId] = escalation;
            return true;
        }

        /// <inheritdoc />
        public bool Remove(string factionId)
        {
            if (factionId == null)
                throw new ArgumentNullException(nameof(factionId));

            return _escalations.Remove(factionId);
        }

        /// <inheritdoc />
        public bool Exists(string factionId)
        {
            if (factionId == null)
                throw new ArgumentNullException(nameof(factionId));

            return _escalations.ContainsKey(factionId);
        }

        /// <inheritdoc />
        public FactionEscalation GetOrCreate(string factionId)
        {
            var existing = GetByFactionId(factionId);
            if (existing != null)
                return existing;

            var newEscalation = new FactionEscalation(factionId);
            Add(newEscalation);
            return newEscalation;
        }

        /// <inheritdoc />
        public IEnumerable<FactionEscalation> GetByTier(EscalationTier tier)
        {
            return _escalations.Values.Where(e => e.CurrentTier == tier).ToList();
        }

        /// <inheritdoc />
        public void Clear()
        {
            _escalations.Clear();
        }
    }
}
