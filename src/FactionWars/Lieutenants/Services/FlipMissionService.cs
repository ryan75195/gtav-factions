using System;
using System.Collections.Generic;
using System.Linq;
using FactionWars.Lieutenants.Interfaces;
using FactionWars.Lieutenants.Models;

namespace FactionWars.Lieutenants.Services
{
    /// <summary>
    /// Service for managing flip missions - operations to convince enemy lieutenants to defect.
    /// </summary>
    public class FlipMissionService : IFlipMissionService
    {
        private const int BaseMissionCost = 15000;
        private const int CostPerLevel = 5000;

        private readonly IDefectionService _defectionService;
        private readonly IRandomProvider _randomProvider;
        private readonly List<FlipMission> _activeMissions;
        private readonly List<FlipMission> _completedMissions;

        /// <summary>
        /// Creates a new FlipMissionService.
        /// </summary>
        /// <param name="defectionService">The defection service for handling actual defection attempts.</param>
        /// <param name="randomProvider">The random provider for detection rolls.</param>
        /// <exception cref="ArgumentNullException">Thrown if dependencies are null.</exception>
        public FlipMissionService(IDefectionService defectionService, IRandomProvider randomProvider)
        {
            _defectionService = defectionService ?? throw new ArgumentNullException(nameof(defectionService));
            _randomProvider = randomProvider ?? throw new ArgumentNullException(nameof(randomProvider));
            _activeMissions = new List<FlipMission>();
            _completedMissions = new List<FlipMission>();
        }

        /// <inheritdoc />
        public FlipMission CreateMission(Lieutenant targetLieutenant, string initiatorFactionId, int bribeAmount = 0)
        {
            if (targetLieutenant == null)
                throw new ArgumentNullException(nameof(targetLieutenant));
            if (initiatorFactionId == null)
                throw new ArgumentNullException(nameof(initiatorFactionId));

            var mission = new FlipMission(targetLieutenant, initiatorFactionId, bribeAmount);
            _activeMissions.Add(mission);
            return mission;
        }

        /// <inheritdoc />
        public void StartMission(FlipMission mission)
        {
            if (mission == null)
                throw new ArgumentNullException(nameof(mission));

            mission.Start();
        }

        /// <inheritdoc />
        public FlipMissionOutcome ExecuteMission(FlipMission mission, Lieutenant targetLieutenant)
        {
            if (mission == null)
                throw new ArgumentNullException(nameof(mission));
            if (targetLieutenant == null)
                throw new ArgumentNullException(nameof(targetLieutenant));

            if (mission.Status != FlipMissionStatus.InProgress)
                throw new InvalidOperationException("Mission must be in progress to execute.");

            if (mission.TargetLieutenantId != targetLieutenant.Id)
                throw new ArgumentException($"Lieutenant does not match mission target.");

            // Attempt the defection
            var defectionResult = _defectionService.AttemptDefection(
                targetLieutenant,
                mission.InitiatorFactionId,
                mission.BribeAmount);

            // Calculate detection
            double detectionRoll = _randomProvider.NextDouble();
            bool detected = detectionRoll < mission.BaseDetectionChance;

            // Complete the mission
            mission.Complete(defectionResult.Success, detected);

            // Move from active to completed
            _activeMissions.Remove(mission);
            _completedMissions.Add(mission);

            // Return the outcome
            if (defectionResult.Success)
            {
                return FlipMissionOutcome.Succeeded(detected);
            }
            else
            {
                return FlipMissionOutcome.Failed(detected, defectionResult.FailureReason);
            }
        }

        /// <inheritdoc />
        public void CancelMission(FlipMission mission)
        {
            if (mission == null)
                throw new ArgumentNullException(nameof(mission));

            mission.Cancel();
            _activeMissions.Remove(mission);
        }

        /// <inheritdoc />
        public bool CanCreateMission(Lieutenant? lieutenant, string initiatorFactionId)
        {
            if (lieutenant == null)
                return false;

            if (string.IsNullOrEmpty(initiatorFactionId))
                return false;

            // Cannot flip your own lieutenant
            if (lieutenant.FactionId == initiatorFactionId)
                return false;

            // Cannot flip deceased lieutenants
            if (lieutenant.Status == LieutenantStatus.Deceased)
                return false;

            // Cannot flip if captured by someone other than the initiator
            if (lieutenant.Status == LieutenantStatus.Captured &&
                lieutenant.CapturedByFactionId != initiatorFactionId)
                return false;

            // Cannot have multiple active missions for the same lieutenant
            if (_activeMissions.Any(m => m.TargetLieutenantId == lieutenant.Id))
                return false;

            return true;
        }

        /// <inheritdoc />
        public IReadOnlyCollection<FlipMission> GetActiveMissions()
        {
            return _activeMissions.AsReadOnly();
        }

        /// <inheritdoc />
        public IReadOnlyCollection<FlipMission> GetCompletedMissions()
        {
            return _completedMissions.AsReadOnly();
        }

        /// <inheritdoc />
        public IReadOnlyCollection<FlipMission> GetMissionsForFaction(string factionId)
        {
            if (string.IsNullOrEmpty(factionId))
                return Array.Empty<FlipMission>();

            return _activeMissions
                .Where(m => m.InvolvesFaction(factionId))
                .ToList()
                .AsReadOnly();
        }

        /// <inheritdoc />
        public FlipMission? GetMissionByLieutenant(string lieutenantId)
        {
            if (string.IsNullOrEmpty(lieutenantId))
                return null;

            return _activeMissions.FirstOrDefault(m => m.TargetLieutenantId == lieutenantId);
        }

        /// <inheritdoc />
        public int EstimateMissionCost(Lieutenant lieutenant)
        {
            if (lieutenant == null)
                throw new ArgumentNullException(nameof(lieutenant));

            return BaseMissionCost + (lieutenant.Level * CostPerLevel);
        }

        /// <inheritdoc />
        public int GetRecommendedBribe(Lieutenant lieutenant)
        {
            if (lieutenant == null)
                throw new ArgumentNullException(nameof(lieutenant));

            return _defectionService.GetRequiredBribeForGuaranteedDefection(lieutenant);
        }
    }
}
