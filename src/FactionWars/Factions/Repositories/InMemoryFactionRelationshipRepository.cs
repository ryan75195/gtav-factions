using FactionWars.Factions.Interfaces;
using FactionWars.Factions.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FactionWars.Factions.Repositories
{
    /// <summary>
    /// In-memory implementation of the faction relationship repository.
    /// Suitable for testing and runtime storage.
    /// </summary>
    public class InMemoryFactionRelationshipRepository : IFactionRelationshipRepository
    {
        private readonly List<FactionRelationship> _relationships = new List<FactionRelationship>();

        /// <inheritdoc />
        public int Count => _relationships.Count;

        /// <inheritdoc />
        public void Add(FactionRelationship relationship)
        {
            if (relationship == null)
                throw new ArgumentNullException(nameof(relationship));

            if (Contains(relationship.FactionId1, relationship.FactionId2))
                throw new InvalidOperationException(
                    $"A relationship between {relationship.FactionId1} and {relationship.FactionId2} already exists.");

            _relationships.Add(relationship);
        }

        /// <inheritdoc />
        public FactionRelationship? Get(string factionId1, string factionId2)
        {
            if (factionId1 == null)
                throw new ArgumentNullException(nameof(factionId1));
            if (factionId2 == null)
                throw new ArgumentNullException(nameof(factionId2));

            return _relationships.FirstOrDefault(r => r.InvolvesBothFactions(factionId1, factionId2));
        }

        /// <inheritdoc />
        public IEnumerable<FactionRelationship> GetByFaction(string factionId)
        {
            if (factionId == null)
                throw new ArgumentNullException(nameof(factionId));

            return _relationships.Where(r => r.ContainsFaction(factionId));
        }

        /// <inheritdoc />
        public IEnumerable<FactionRelationship> GetAll()
        {
            return _relationships.ToList();
        }

        /// <inheritdoc />
        public void Update(FactionRelationship relationship)
        {
            if (relationship == null)
                throw new ArgumentNullException(nameof(relationship));

            var existing = Get(relationship.FactionId1, relationship.FactionId2);
            if (existing == null)
                throw new InvalidOperationException(
                    $"No relationship exists between {relationship.FactionId1} and {relationship.FactionId2}.");

            // The relationship object is mutable, so if the same instance was retrieved and modified,
            // no action is needed. If a different instance with the same factions is passed,
            // we replace it.
            if (!ReferenceEquals(existing, relationship))
            {
                _relationships.Remove(existing);
                _relationships.Add(relationship);
            }
        }

        /// <inheritdoc />
        public bool Remove(string factionId1, string factionId2)
        {
            if (factionId1 == null)
                throw new ArgumentNullException(nameof(factionId1));
            if (factionId2 == null)
                throw new ArgumentNullException(nameof(factionId2));

            var relationship = Get(factionId1, factionId2);
            if (relationship == null)
                return false;

            return _relationships.Remove(relationship);
        }

        /// <inheritdoc />
        public bool Contains(string factionId1, string factionId2)
        {
            return Get(factionId1, factionId2) != null;
        }

        /// <inheritdoc />
        public void Clear()
        {
            _relationships.Clear();
        }

        /// <inheritdoc />
        public FactionRelationship GetOrCreate(string factionId1, string factionId2, int defaultValue = 0)
        {
            if (factionId1 == null)
                throw new ArgumentNullException(nameof(factionId1));
            if (factionId2 == null)
                throw new ArgumentNullException(nameof(factionId2));

            var existing = Get(factionId1, factionId2);
            if (existing != null)
                return existing;

            var relationship = new FactionRelationship(factionId1, factionId2, defaultValue);
            Add(relationship);
            return relationship;
        }
    }
}
