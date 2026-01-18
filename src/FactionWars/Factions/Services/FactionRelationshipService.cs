using FactionWars.Factions.Interfaces;
using FactionWars.Factions.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FactionWars.Factions.Services
{
    /// <summary>
    /// Service providing faction relationship business logic.
    /// </summary>
    public class FactionRelationshipService : IFactionRelationshipService
    {
        private readonly IFactionRepository _factionRepository;
        private readonly IFactionRelationshipRepository _relationshipRepository;

        /// <summary>
        /// Creates a new FactionRelationshipService instance.
        /// </summary>
        /// <param name="factionRepository">The faction repository.</param>
        /// <param name="relationshipRepository">The relationship repository.</param>
        /// <exception cref="ArgumentNullException">Thrown if either repository is null.</exception>
        public FactionRelationshipService(
            IFactionRepository factionRepository,
            IFactionRelationshipRepository relationshipRepository)
        {
            _factionRepository = factionRepository ?? throw new ArgumentNullException(nameof(factionRepository));
            _relationshipRepository = relationshipRepository ?? throw new ArgumentNullException(nameof(relationshipRepository));
        }

        /// <inheritdoc />
        public FactionRelationship? GetRelationship(string factionId1, string factionId2)
        {
            if (factionId1 == null)
                throw new ArgumentNullException(nameof(factionId1));
            if (factionId2 == null)
                throw new ArgumentNullException(nameof(factionId2));

            return _relationshipRepository.Get(factionId1, factionId2);
        }

        /// <inheritdoc />
        public int GetRelationshipValue(string factionId1, string factionId2)
        {
            if (factionId1 == null)
                throw new ArgumentNullException(nameof(factionId1));
            if (factionId2 == null)
                throw new ArgumentNullException(nameof(factionId2));

            var relationship = _relationshipRepository.Get(factionId1, factionId2);
            return relationship?.Value ?? 0;
        }

        /// <inheritdoc />
        public RelationshipStatus GetRelationshipStatus(string factionId1, string factionId2)
        {
            if (factionId1 == null)
                throw new ArgumentNullException(nameof(factionId1));
            if (factionId2 == null)
                throw new ArgumentNullException(nameof(factionId2));

            var relationship = _relationshipRepository.Get(factionId1, factionId2);
            return relationship?.Status ?? RelationshipStatus.Neutral;
        }

        /// <inheritdoc />
        public bool SetRelationshipValue(string factionId1, string factionId2, int value)
        {
            if (factionId1 == null)
                throw new ArgumentNullException(nameof(factionId1));
            if (factionId2 == null)
                throw new ArgumentNullException(nameof(factionId2));

            if (!_factionRepository.Contains(factionId1) || !_factionRepository.Contains(factionId2))
                return false;

            var relationship = _relationshipRepository.GetOrCreate(factionId1, factionId2);
            relationship.SetValue(value);
            _relationshipRepository.Update(relationship);
            return true;
        }

        /// <inheritdoc />
        public bool AdjustRelationship(string factionId1, string factionId2, int amount)
        {
            if (factionId1 == null)
                throw new ArgumentNullException(nameof(factionId1));
            if (factionId2 == null)
                throw new ArgumentNullException(nameof(factionId2));

            if (!_factionRepository.Contains(factionId1) || !_factionRepository.Contains(factionId2))
                return false;

            var relationship = _relationshipRepository.GetOrCreate(factionId1, factionId2);
            relationship.AdjustValue(amount);
            _relationshipRepository.Update(relationship);
            return true;
        }

        /// <inheritdoc />
        public IEnumerable<FactionRelationship> GetAllRelationshipsForFaction(string factionId)
        {
            if (factionId == null)
                throw new ArgumentNullException(nameof(factionId));

            return _relationshipRepository.GetByFaction(factionId);
        }

        /// <inheritdoc />
        public IEnumerable<string> GetEnemies(string factionId)
        {
            if (factionId == null)
                throw new ArgumentNullException(nameof(factionId));

            return _relationshipRepository.GetByFaction(factionId)
                .Where(r => r.Value < 0)
                .Select(r => r.GetOtherFaction(factionId)!)
                .Where(id => id != null);
        }

        /// <inheritdoc />
        public IEnumerable<string> GetAllies(string factionId)
        {
            if (factionId == null)
                throw new ArgumentNullException(nameof(factionId));

            return _relationshipRepository.GetByFaction(factionId)
                .Where(r => r.Status == RelationshipStatus.Allied)
                .Select(r => r.GetOtherFaction(factionId)!)
                .Where(id => id != null);
        }

        /// <inheritdoc />
        public bool AreAtWar(string factionId1, string factionId2)
        {
            if (factionId1 == null)
                throw new ArgumentNullException(nameof(factionId1));
            if (factionId2 == null)
                throw new ArgumentNullException(nameof(factionId2));

            return GetRelationshipStatus(factionId1, factionId2) == RelationshipStatus.War;
        }

        /// <inheritdoc />
        public bool AreAllied(string factionId1, string factionId2)
        {
            if (factionId1 == null)
                throw new ArgumentNullException(nameof(factionId1));
            if (factionId2 == null)
                throw new ArgumentNullException(nameof(factionId2));

            return GetRelationshipStatus(factionId1, factionId2) == RelationshipStatus.Allied;
        }

        /// <inheritdoc />
        public void InitializeAllRelationships(int defaultValue = 0)
        {
            var factions = _factionRepository.GetAll().ToList();

            for (int i = 0; i < factions.Count; i++)
            {
                for (int j = i + 1; j < factions.Count; j++)
                {
                    var factionId1 = factions[i].Id;
                    var factionId2 = factions[j].Id;

                    if (!_relationshipRepository.Contains(factionId1, factionId2))
                    {
                        var relationship = new FactionRelationship(factionId1, factionId2, defaultValue);
                        _relationshipRepository.Add(relationship);
                    }
                }
            }
        }

        /// <inheritdoc />
        public bool DeclareWar(string factionId1, string factionId2)
        {
            if (factionId1 == null)
                throw new ArgumentNullException(nameof(factionId1));
            if (factionId2 == null)
                throw new ArgumentNullException(nameof(factionId2));

            return SetRelationshipValue(factionId1, factionId2, FactionRelationship.MinValue);
        }

        /// <inheritdoc />
        public bool FormAlliance(string factionId1, string factionId2)
        {
            if (factionId1 == null)
                throw new ArgumentNullException(nameof(factionId1));
            if (factionId2 == null)
                throw new ArgumentNullException(nameof(factionId2));

            return SetRelationshipValue(factionId1, factionId2, FactionRelationship.MaxValue);
        }

        /// <inheritdoc />
        public bool MakePeace(string factionId1, string factionId2)
        {
            if (factionId1 == null)
                throw new ArgumentNullException(nameof(factionId1));
            if (factionId2 == null)
                throw new ArgumentNullException(nameof(factionId2));

            return SetRelationshipValue(factionId1, factionId2, 0);
        }
    }
}
