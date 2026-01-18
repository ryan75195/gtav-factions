using System.Collections.Generic;
using FactionWars.Lieutenants.Models;

namespace FactionWars.Lieutenants.Interfaces
{
    /// <summary>
    /// Service for managing flip missions - operations to convince enemy lieutenants to defect.
    /// </summary>
    public interface IFlipMissionService
    {
        /// <summary>
        /// Creates a new flip mission targeting a lieutenant.
        /// </summary>
        /// <param name="targetLieutenant">The lieutenant to target.</param>
        /// <param name="initiatorFactionId">The faction initiating the mission.</param>
        /// <param name="bribeAmount">Optional bribe amount to offer.</param>
        /// <returns>The created mission.</returns>
        FlipMission CreateMission(Lieutenant targetLieutenant, string initiatorFactionId, int bribeAmount = 0);

        /// <summary>
        /// Starts a pending mission.
        /// </summary>
        /// <param name="mission">The mission to start.</param>
        void StartMission(FlipMission mission);

        /// <summary>
        /// Executes a mission and determines the outcome.
        /// </summary>
        /// <param name="mission">The mission to execute.</param>
        /// <param name="targetLieutenant">The target lieutenant.</param>
        /// <returns>The outcome of the mission.</returns>
        FlipMissionOutcome ExecuteMission(FlipMission mission, Lieutenant targetLieutenant);

        /// <summary>
        /// Cancels a mission that is pending or in progress.
        /// </summary>
        /// <param name="mission">The mission to cancel.</param>
        void CancelMission(FlipMission mission);

        /// <summary>
        /// Checks whether a flip mission can be created for a lieutenant.
        /// </summary>
        /// <param name="lieutenant">The lieutenant to target.</param>
        /// <param name="initiatorFactionId">The faction initiating the mission.</param>
        /// <returns>True if a mission can be created.</returns>
        bool CanCreateMission(Lieutenant? lieutenant, string initiatorFactionId);

        /// <summary>
        /// Gets all active (non-terminal) missions.
        /// </summary>
        /// <returns>A collection of active missions.</returns>
        IReadOnlyCollection<FlipMission> GetActiveMissions();

        /// <summary>
        /// Gets all completed (terminal) missions.
        /// </summary>
        /// <returns>A collection of completed missions.</returns>
        IReadOnlyCollection<FlipMission> GetCompletedMissions();

        /// <summary>
        /// Gets all missions involving the specified faction.
        /// </summary>
        /// <param name="factionId">The faction ID.</param>
        /// <returns>A collection of missions.</returns>
        IReadOnlyCollection<FlipMission> GetMissionsForFaction(string factionId);

        /// <summary>
        /// Gets the active mission for a specific lieutenant, if any.
        /// </summary>
        /// <param name="lieutenantId">The lieutenant ID.</param>
        /// <returns>The active mission, or null if none.</returns>
        FlipMission? GetMissionByLieutenant(string lieutenantId);

        /// <summary>
        /// Estimates the cost for a flip mission on a lieutenant.
        /// </summary>
        /// <param name="lieutenant">The lieutenant to evaluate.</param>
        /// <returns>The estimated cost.</returns>
        int EstimateMissionCost(Lieutenant lieutenant);

        /// <summary>
        /// Gets the recommended bribe amount for a lieutenant.
        /// </summary>
        /// <param name="lieutenant">The lieutenant to evaluate.</param>
        /// <returns>The recommended bribe amount.</returns>
        int GetRecommendedBribe(Lieutenant lieutenant);
    }
}
