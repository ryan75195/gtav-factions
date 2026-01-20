using System;
using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Models;
using FactionWars.Territory.Interfaces;

namespace FactionWars.Combat.Services
{
    /// <summary>
    /// Processes completed combat encounters and updates zone state accordingly.
    /// Handles ownership transfers, control percentages, and contested states.
    /// </summary>
    public class CombatResultHandler : ICombatResultHandler
    {
        private readonly IZoneService _zoneService;

        /// <summary>
        /// Creates a new CombatResultHandler.
        /// </summary>
        /// <param name="zoneService">The zone service for updating zone state.</param>
        /// <exception cref="ArgumentNullException">Thrown if zoneService is null.</exception>
        public CombatResultHandler(IZoneService zoneService)
        {
            _zoneService = zoneService ?? throw new ArgumentNullException(nameof(zoneService));
        }

        /// <inheritdoc />
        public CombatProcessingResult ProcessCombatResult(CombatEncounter encounter)
        {
            if (encounter == null)
                throw new ArgumentNullException(nameof(encounter));

            if (encounter.IsActive)
                throw new InvalidOperationException("Cannot process combat result: encounter is still in progress.");

            return encounter.Status switch
            {
                CombatStatus.AttackerVictory => ProcessAttackerVictory(encounter),
                CombatStatus.DefenderVictory => ProcessDefenderVictory(encounter),
                CombatStatus.Stalemate => ProcessStalemate(encounter),
                CombatStatus.Aborted => ProcessAborted(encounter),
                _ => CombatProcessingResult.Failure(CombatResultOutcome.ZoneNotFound, encounter.ZoneId)
            };
        }

        private CombatProcessingResult ProcessAttackerVictory(CombatEncounter encounter)
        {
            // NEW: Victory makes zone neutral, not captured
            // Player must then claim it by paying for a troop
            var transferSuccess = _zoneService.TransferZoneOwnership(
                encounter.ZoneId,
                null);  // null = neutral

            if (!transferSuccess)
            {
                return CombatProcessingResult.Failure(
                    CombatResultOutcome.ZoneNotFound,
                    encounter.ZoneId);
            }

            // Set control to 0% (neutral)
            _zoneService.UpdateZoneControl(encounter.ZoneId, 0f);

            // Clear contested state
            _zoneService.SetZoneContested(encounter.ZoneId, false);

            return CombatProcessingResult.Success(
                CombatResultOutcome.ZoneNeutralized,  // New outcome type
                encounter.ZoneId,
                null,  // No new owner yet
                encounter.DefendingFactionId);
        }

        private CombatProcessingResult ProcessDefenderVictory(CombatEncounter encounter)
        {
            // Set control to 100% for the defender (who retains ownership)
            var updateSuccess = _zoneService.UpdateZoneControl(encounter.ZoneId, 100f);

            if (!updateSuccess)
            {
                return CombatProcessingResult.Failure(
                    CombatResultOutcome.ZoneNotFound,
                    encounter.ZoneId);
            }

            // Clear contested state
            _zoneService.SetZoneContested(encounter.ZoneId, false);

            return CombatProcessingResult.Success(
                CombatResultOutcome.ZoneDefended,
                encounter.ZoneId,
                encounter.DefendingFactionId,
                encounter.DefendingFactionId);
        }

        private CombatProcessingResult ProcessStalemate(CombatEncounter encounter)
        {
            // Keep the defender's current control percentage
            var updateSuccess = _zoneService.UpdateZoneControl(
                encounter.ZoneId,
                encounter.DefenderControlPercentage);

            if (!updateSuccess)
            {
                return CombatProcessingResult.Failure(
                    CombatResultOutcome.ZoneNotFound,
                    encounter.ZoneId);
            }

            // Clear contested state
            _zoneService.SetZoneContested(encounter.ZoneId, false);

            return CombatProcessingResult.Success(
                CombatResultOutcome.Stalemate,
                encounter.ZoneId,
                encounter.DefendingFactionId,
                encounter.DefendingFactionId);
        }

        private CombatProcessingResult ProcessAborted(CombatEncounter encounter)
        {
            // Keep the defender's current control percentage
            var updateSuccess = _zoneService.UpdateZoneControl(
                encounter.ZoneId,
                encounter.DefenderControlPercentage);

            if (!updateSuccess)
            {
                return CombatProcessingResult.Failure(
                    CombatResultOutcome.ZoneNotFound,
                    encounter.ZoneId);
            }

            // Clear contested state
            _zoneService.SetZoneContested(encounter.ZoneId, false);

            return CombatProcessingResult.Success(
                CombatResultOutcome.Aborted,
                encounter.ZoneId,
                encounter.DefendingFactionId,
                encounter.DefendingFactionId);
        }
    }
}
