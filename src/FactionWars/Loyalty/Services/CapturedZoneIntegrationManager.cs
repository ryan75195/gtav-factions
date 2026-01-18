using FactionWars.Loyalty.Interfaces;
using FactionWars.Loyalty.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FactionWars.Loyalty.Services
{
    /// <summary>
    /// Manages the full lifecycle of integrating captured zones into the controlling faction.
    /// </summary>
    public class CapturedZoneIntegrationManager : ICapturedZoneIntegrationManager
    {
        private readonly IZoneIntegrationService _integrationService;
        private readonly IZoneIntegrationRepository _repository;

        /// <summary>
        /// Creates a new CapturedZoneIntegrationManager.
        /// </summary>
        /// <param name="integrationService">The integration service for calculations.</param>
        /// <param name="repository">The repository for storing integration states.</param>
        public CapturedZoneIntegrationManager(
            IZoneIntegrationService integrationService,
            IZoneIntegrationRepository repository)
        {
            _integrationService = integrationService ?? throw new ArgumentNullException(nameof(integrationService));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        /// <inheritdoc />
        public void OnZoneCaptured(ZoneLoyalty loyalty)
        {
            if (loyalty == null)
                throw new ArgumentNullException(nameof(loyalty));

            if (string.IsNullOrEmpty(loyalty.PreviousFactionId))
                throw new InvalidOperationException("Cannot create integration state without a previous controlling faction.");

            // Remove existing integration state if zone was recaptured
            var existingState = _repository.GetByZoneId(loyalty.ZoneId);
            if (existingState != null)
            {
                _repository.Remove(loyalty.ZoneId);
            }

            // Create new integration state
            var newState = _integrationService.CreateIntegrationState(loyalty);
            _repository.Add(newState);
        }

        /// <inheritdoc />
        public IEnumerable<IntegrationTickResult> ProcessDailyTick()
        {
            var results = new List<IntegrationTickResult>();
            var pendingZones = _repository.GetPendingIntegration().ToList();

            foreach (var state in pendingZones)
            {
                int previousProgress = state.IntegrationProgress;
                int progressGained = _integrationService.CalculateDailyProgress(state);

                _integrationService.ApplyDailyProgress(state);
                _repository.Update(state);

                bool justCompleted = !state.IsFullyIntegrated ? false : previousProgress < 100;

                results.Add(new IntegrationTickResult(
                    state.ZoneId,
                    progressGained,
                    state.IntegrationProgress,
                    justCompleted));
            }

            return results;
        }

        /// <inheritdoc />
        public float GetResourceMultiplier(string zoneId)
        {
            if (string.IsNullOrEmpty(zoneId))
                return 1.0f;

            var state = _repository.GetByZoneId(zoneId);
            if (state == null)
                return 1.0f;

            return _integrationService.CalculateResourcePenalty(state);
        }

        /// <inheritdoc />
        public int GetDefenseModifier(string zoneId)
        {
            if (string.IsNullOrEmpty(zoneId))
                return 0;

            var state = _repository.GetByZoneId(zoneId);
            if (state == null)
                return 0;

            return _integrationService.CalculateDefenseBonus(state);
        }

        /// <inheritdoc />
        public bool IsZoneIntegrating(string zoneId)
        {
            if (string.IsNullOrEmpty(zoneId))
                return false;

            var state = _repository.GetByZoneId(zoneId);
            return state != null && !state.IsFullyIntegrated;
        }

        /// <inheritdoc />
        public int GetIntegrationProgress(string zoneId)
        {
            if (string.IsNullOrEmpty(zoneId))
                return 100;

            var state = _repository.GetByZoneId(zoneId);
            return state?.IntegrationProgress ?? 100;
        }

        /// <inheritdoc />
        public void OnInsurgencyOccurred(string zoneId, InsurgencyLevel level)
        {
            if (string.IsNullOrEmpty(zoneId))
                return;

            var state = _repository.GetByZoneId(zoneId);
            if (state == null)
                return;

            _integrationService.ApplyInsurgencySetback(state, level);
            _repository.Update(state);
        }

        /// <inheritdoc />
        public bool CompleteIntegration(string zoneId)
        {
            if (string.IsNullOrEmpty(zoneId))
                return false;

            var state = _repository.GetByZoneId(zoneId);
            if (state == null || !state.IsFullyIntegrated)
                return false;

            _repository.Remove(zoneId);
            return true;
        }

        /// <inheritdoc />
        public IEnumerable<string> GetIntegratingZonesForFaction(string factionId)
        {
            if (string.IsNullOrEmpty(factionId))
                return Enumerable.Empty<string>();

            return _repository.GetByFaction(factionId)
                .Where(s => !s.IsFullyIntegrated)
                .Select(s => s.ZoneId)
                .ToList();
        }
    }
}
