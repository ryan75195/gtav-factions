using System;
using System.Collections.Generic;
using FactionWars.Escalation.Interfaces;
using FactionWars.Escalation.Models;

namespace FactionWars.Escalation.Services
{
    /// <summary>
    /// Service providing escalation-related business logic and operations.
    /// </summary>
    public class EscalationService : IEscalationService
    {
        private readonly IEscalationRepository _repository;
        private readonly bool _autoCreateEscalation;

        /// <summary>
        /// Creates a new EscalationService instance.
        /// </summary>
        /// <param name="repository">The escalation repository.</param>
        /// <param name="autoCreateEscalation">Whether to auto-create escalation records when adding points to non-existent factions.</param>
        /// <exception cref="ArgumentNullException">Thrown if repository is null.</exception>
        public EscalationService(IEscalationRepository repository, bool autoCreateEscalation = false)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _autoCreateEscalation = autoCreateEscalation;
        }

        /// <inheritdoc />
        public FactionEscalation? GetEscalation(string factionId)
        {
            ValidateFactionId(factionId);
            return _repository.GetByFactionId(factionId);
        }

        /// <inheritdoc />
        public FactionEscalation GetOrCreateEscalation(string factionId)
        {
            if (factionId == null)
                throw new ArgumentNullException(nameof(factionId));

            return _repository.GetOrCreate(factionId);
        }

        /// <inheritdoc />
        public EscalationTier GetCurrentTier(string factionId)
        {
            if (factionId == null)
                throw new ArgumentNullException(nameof(factionId));

            var escalation = _repository.GetByFactionId(factionId);
            return escalation?.CurrentTier ?? EscalationTier.Tier1;
        }

        /// <inheritdoc />
        public EscalationPointsResult AddEscalationPoints(string factionId, int amount)
        {
            if (factionId == null)
                throw new ArgumentNullException(nameof(factionId));
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be non-negative.");

            var escalation = _repository.GetByFactionId(factionId);
            if (escalation == null)
            {
                if (_autoCreateEscalation)
                {
                    escalation = new FactionEscalation(factionId);
                    _repository.Add(escalation);
                }
                else
                {
                    return EscalationPointsResult.Failed();
                }
            }

            var oldTier = escalation.CurrentTier;
            var tierChanged = escalation.AddPoints(amount);
            _repository.Update(escalation);

            if (tierChanged)
            {
                return EscalationPointsResult.TierChangedResult(oldTier, escalation.CurrentTier, escalation.Points);
            }

            return EscalationPointsResult.Succeeded(escalation.CurrentTier, escalation.Points);
        }

        /// <inheritdoc />
        public EscalationPointsResult RemoveEscalationPoints(string factionId, int amount)
        {
            if (factionId == null)
                throw new ArgumentNullException(nameof(factionId));
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be non-negative.");

            var escalation = _repository.GetByFactionId(factionId);
            if (escalation == null)
            {
                return EscalationPointsResult.Failed();
            }

            var oldTier = escalation.CurrentTier;
            var tierChanged = escalation.RemovePoints(amount);
            _repository.Update(escalation);

            if (tierChanged)
            {
                return EscalationPointsResult.TierChangedResult(oldTier, escalation.CurrentTier, escalation.Points);
            }

            return EscalationPointsResult.Succeeded(escalation.CurrentTier, escalation.Points);
        }

        /// <inheritdoc />
        public bool SetEscalationPoints(string factionId, int points)
        {
            if (factionId == null)
                throw new ArgumentNullException(nameof(factionId));

            var escalation = _repository.GetByFactionId(factionId);
            if (escalation == null)
            {
                return false;
            }

            escalation.SetPoints(points);
            _repository.Update(escalation);
            return true;
        }

        /// <inheritdoc />
        public IEnumerable<FactionEscalation> GetAllEscalations()
        {
            return _repository.GetAll();
        }

        /// <inheritdoc />
        public IEnumerable<FactionEscalation> GetFactionsAtTier(EscalationTier tier)
        {
            return _repository.GetByTier(tier);
        }

        /// <inheritdoc />
        public float GetProgressToNextTier(string factionId)
        {
            if (factionId == null)
                throw new ArgumentNullException(nameof(factionId));

            var escalation = _repository.GetByFactionId(factionId);
            return escalation?.ProgressToNextTier ?? 0f;
        }

        /// <inheritdoc />
        public int GetPointsToNextTier(string factionId)
        {
            if (factionId == null)
                throw new ArgumentNullException(nameof(factionId));

            var escalation = _repository.GetByFactionId(factionId);
            return escalation?.PointsToNextTier ?? int.MaxValue;
        }

        /// <inheritdoc />
        public bool ResetEscalation(string factionId)
        {
            if (factionId == null)
                throw new ArgumentNullException(nameof(factionId));

            var escalation = _repository.GetByFactionId(factionId);
            if (escalation == null)
            {
                return false;
            }

            escalation.SetPoints(0);
            _repository.Update(escalation);
            return true;
        }

        /// <inheritdoc />
        public bool InitializeEscalation(string factionId, int initialPoints = 0)
        {
            if (factionId == null)
                throw new ArgumentNullException(nameof(factionId));

            if (_repository.Exists(factionId))
            {
                return false;
            }

            var escalation = new FactionEscalation(factionId, initialPoints);
            return _repository.Add(escalation);
        }

        /// <inheritdoc />
        public bool RemoveEscalation(string factionId)
        {
            if (factionId == null)
                throw new ArgumentNullException(nameof(factionId));

            return _repository.Remove(factionId);
        }

        private void ValidateFactionId(string factionId)
        {
            if (factionId == null)
                throw new ArgumentNullException(nameof(factionId));
            if (string.IsNullOrWhiteSpace(factionId))
                throw new ArgumentException("Faction ID cannot be empty or whitespace.", nameof(factionId));
        }
    }
}
