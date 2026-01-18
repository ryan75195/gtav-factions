using System.Collections.Generic;
using System.Linq;

namespace FactionWars.Combat.Models
{
    /// <summary>
    /// Result of a reinforcement request, indicating success or failure with details.
    /// </summary>
    public class ReinforcementResult
    {
        /// <summary>
        /// The status of the reinforcement request.
        /// </summary>
        public ReinforcementResultStatus Status { get; }

        /// <summary>
        /// Whether the request was at least partially successful.
        /// </summary>
        public bool IsSuccess => Status == ReinforcementResultStatus.Success ||
                                  Status == ReinforcementResultStatus.PartialSuccess;

        /// <summary>
        /// The peds that were spawned.
        /// </summary>
        public IReadOnlyList<PedHandle> SpawnedPeds { get; }

        /// <summary>
        /// The number of peds that were successfully spawned.
        /// </summary>
        public int SpawnedCount => SpawnedPeds.Count;

        /// <summary>
        /// The number of peds that were originally requested.
        /// </summary>
        public int RequestedCount { get; }

        /// <summary>
        /// The total resource cost for the spawned peds.
        /// </summary>
        public int ResourceCost { get; }

        /// <summary>
        /// The reason for failure, if any.
        /// </summary>
        public string? FailureReason { get; }

        /// <summary>
        /// Remaining cooldown time in seconds (for OnCooldown status).
        /// </summary>
        public float RemainingCooldown { get; }

        /// <summary>
        /// Required resources (for InsufficientResources status).
        /// </summary>
        public int RequiredResources { get; }

        /// <summary>
        /// Available resources (for InsufficientResources status).
        /// </summary>
        public int AvailableResources { get; }

        /// <summary>
        /// Maximum waves allowed (for MaxWavesReached status).
        /// </summary>
        public int MaxWaves { get; }

        private ReinforcementResult(
            ReinforcementResultStatus status,
            IReadOnlyList<PedHandle>? spawnedPeds = null,
            int requestedCount = 0,
            int resourceCost = 0,
            string? failureReason = null,
            float remainingCooldown = 0f,
            int requiredResources = 0,
            int availableResources = 0,
            int maxWaves = 0)
        {
            Status = status;
            SpawnedPeds = spawnedPeds ?? new List<PedHandle>();
            RequestedCount = requestedCount;
            ResourceCost = resourceCost;
            FailureReason = failureReason;
            RemainingCooldown = remainingCooldown;
            RequiredResources = requiredResources;
            AvailableResources = availableResources;
            MaxWaves = maxWaves;
        }

        /// <summary>
        /// Creates a successful result with all peds spawned.
        /// </summary>
        public static ReinforcementResult Success(IList<PedHandle> spawnedPeds, int resourceCost)
        {
            return new ReinforcementResult(
                ReinforcementResultStatus.Success,
                spawnedPeds.ToList(),
                spawnedPeds.Count,
                resourceCost);
        }

        /// <summary>
        /// Creates a partial success result when not all requested peds could be spawned.
        /// </summary>
        public static ReinforcementResult PartialSuccess(
            IList<PedHandle> spawnedPeds,
            int requestedCount,
            int resourceCost,
            string reason)
        {
            return new ReinforcementResult(
                ReinforcementResultStatus.PartialSuccess,
                spawnedPeds.ToList(),
                requestedCount,
                resourceCost,
                reason);
        }

        /// <summary>
        /// Creates a cooldown failure result.
        /// </summary>
        public static ReinforcementResult OnCooldown(float remainingSeconds)
        {
            return new ReinforcementResult(
                ReinforcementResultStatus.OnCooldown,
                failureReason: $"Reinforcements on cooldown. {remainingSeconds:F1} seconds remaining.",
                remainingCooldown: remainingSeconds);
        }

        /// <summary>
        /// Creates an insufficient resources failure result.
        /// </summary>
        public static ReinforcementResult InsufficientResources(int required, int available)
        {
            return new ReinforcementResult(
                ReinforcementResultStatus.InsufficientResources,
                failureReason: $"Insufficient resources. Required: {required}, Available: {available}",
                requiredResources: required,
                availableResources: available);
        }

        /// <summary>
        /// Creates a pool full failure result.
        /// </summary>
        public static ReinforcementResult PoolFull()
        {
            return new ReinforcementResult(
                ReinforcementResultStatus.PoolFull,
                failureReason: "Ped pool is full. Cannot spawn more reinforcements.");
        }

        /// <summary>
        /// Creates a max waves reached failure result.
        /// </summary>
        public static ReinforcementResult MaxWavesReached(int maxWaves)
        {
            return new ReinforcementResult(
                ReinforcementResultStatus.MaxWavesReached,
                failureReason: $"Maximum reinforcement waves ({maxWaves}) reached.",
                maxWaves: maxWaves);
        }

        /// <summary>
        /// Creates an encounter ended failure result.
        /// </summary>
        public static ReinforcementResult EncounterEnded()
        {
            return new ReinforcementResult(
                ReinforcementResultStatus.EncounterEnded,
                failureReason: "Combat encounter has ended. Cannot request reinforcements.");
        }
    }
}
