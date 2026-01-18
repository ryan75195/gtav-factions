using System;

namespace FactionWars.Lieutenants.Models
{
    /// <summary>
    /// Represents a mission to flip (defect) an enemy lieutenant to the initiator's faction.
    /// </summary>
    public class FlipMission
    {
        private const int FlipMissionBaseCost = 15000;
        private const int FlipMissionBaseDurationSeconds = 120;
        private const float FlipMissionBaseSuccessChance = 0.4f;
        private const float FlipMissionBaseDetectionChance = 0.3f;

        /// <summary>
        /// Unique identifier for this mission.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// The ID of the lieutenant targeted for flipping.
        /// </summary>
        public string TargetLieutenantId { get; }

        /// <summary>
        /// The ID of the faction initiating the flip mission.
        /// </summary>
        public string InitiatorFactionId { get; }

        /// <summary>
        /// The ID of the faction the target lieutenant currently belongs to.
        /// </summary>
        public string TargetFactionId { get; }

        /// <summary>
        /// The bribe amount offered to the lieutenant.
        /// </summary>
        public int BribeAmount { get; }

        /// <summary>
        /// The current status of the mission.
        /// </summary>
        public FlipMissionStatus Status { get; private set; }

        /// <summary>
        /// The time when the mission was created.
        /// </summary>
        public DateTime CreatedTime { get; }

        /// <summary>
        /// The time when the mission was started, if applicable.
        /// </summary>
        public DateTime? StartTime { get; private set; }

        /// <summary>
        /// The time when the mission was completed, if applicable.
        /// </summary>
        public DateTime? CompletionTime { get; private set; }

        /// <summary>
        /// Whether the mission was successful (only valid after completion).
        /// </summary>
        public bool WasSuccessful { get; private set; }

        /// <summary>
        /// Whether the mission was detected (only valid after completion).
        /// </summary>
        public bool WasDetected { get; private set; }

        /// <summary>
        /// The base cost for this mission (before bribe).
        /// </summary>
        public int BaseCost => FlipMissionBaseCost;

        /// <summary>
        /// The total cost for this mission (base + bribe).
        /// </summary>
        public int TotalCost => BaseCost + BribeAmount;

        /// <summary>
        /// The base duration in seconds for this mission.
        /// </summary>
        public int BaseDurationSeconds => FlipMissionBaseDurationSeconds;

        /// <summary>
        /// The base success chance for this mission type.
        /// </summary>
        public float BaseSuccessChance => FlipMissionBaseSuccessChance;

        /// <summary>
        /// The base detection chance for this mission type.
        /// </summary>
        public float BaseDetectionChance => FlipMissionBaseDetectionChance;

        /// <summary>
        /// Whether this mission is in a terminal state.
        /// </summary>
        public bool IsTerminal =>
            Status == FlipMissionStatus.Succeeded ||
            Status == FlipMissionStatus.Failed ||
            Status == FlipMissionStatus.Cancelled ||
            Status == FlipMissionStatus.Detected;

        /// <summary>
        /// Creates a new flip mission.
        /// </summary>
        /// <param name="targetLieutenant">The lieutenant to target for flipping.</param>
        /// <param name="initiatorFactionId">The faction initiating the mission.</param>
        /// <param name="bribeAmount">Optional bribe amount to offer.</param>
        /// <exception cref="ArgumentNullException">Thrown if required parameters are null.</exception>
        /// <exception cref="ArgumentException">Thrown if parameters are invalid.</exception>
        public FlipMission(Lieutenant targetLieutenant, string initiatorFactionId, int bribeAmount = 0)
        {
            if (targetLieutenant == null)
                throw new ArgumentNullException(nameof(targetLieutenant));

            if (initiatorFactionId == null)
                throw new ArgumentNullException(nameof(initiatorFactionId));

            if (string.IsNullOrWhiteSpace(initiatorFactionId))
                throw new ArgumentException("Initiator faction ID cannot be empty or whitespace.", nameof(initiatorFactionId));

            if (targetLieutenant.FactionId == initiatorFactionId)
                throw new ArgumentException("Cannot create flip mission for lieutenant in own faction.", nameof(initiatorFactionId));

            if (targetLieutenant.Status == LieutenantStatus.Deceased)
                throw new ArgumentException("Cannot create flip mission for deceased lieutenant.", nameof(targetLieutenant));

            Id = Guid.NewGuid().ToString();
            TargetLieutenantId = targetLieutenant.Id;
            InitiatorFactionId = initiatorFactionId;
            TargetFactionId = targetLieutenant.FactionId;
            BribeAmount = Math.Max(0, bribeAmount);
            Status = FlipMissionStatus.Pending;
            CreatedTime = DateTime.UtcNow;
        }

        /// <summary>
        /// Starts the mission.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if mission is not in Pending status.</exception>
        public void Start()
        {
            if (Status != FlipMissionStatus.Pending)
                throw new InvalidOperationException($"Cannot start mission in {Status} status.");

            Status = FlipMissionStatus.InProgress;
            StartTime = DateTime.UtcNow;
        }

        /// <summary>
        /// Completes the mission with the specified outcome.
        /// </summary>
        /// <param name="success">Whether the mission was successful.</param>
        /// <param name="detected">Whether the mission was detected.</param>
        /// <exception cref="InvalidOperationException">Thrown if mission is not in InProgress status.</exception>
        public void Complete(bool success, bool detected)
        {
            if (Status != FlipMissionStatus.InProgress)
                throw new InvalidOperationException($"Cannot complete mission in {Status} status.");

            WasSuccessful = success;
            WasDetected = detected;
            CompletionTime = DateTime.UtcNow;

            if (detected)
            {
                Status = FlipMissionStatus.Detected;
            }
            else if (success)
            {
                Status = FlipMissionStatus.Succeeded;
            }
            else
            {
                Status = FlipMissionStatus.Failed;
            }
        }

        /// <summary>
        /// Cancels the mission.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if mission is already in a terminal state.</exception>
        public void Cancel()
        {
            if (IsTerminal)
                throw new InvalidOperationException($"Cannot cancel mission in {Status} status.");

            Status = FlipMissionStatus.Cancelled;
            CompletionTime = DateTime.UtcNow;
        }

        /// <summary>
        /// Checks if this mission involves the specified faction.
        /// </summary>
        /// <param name="factionId">The faction ID to check.</param>
        /// <returns>True if the faction is involved (as initiator or target).</returns>
        public bool InvolvesFaction(string factionId)
        {
            if (string.IsNullOrEmpty(factionId))
                return false;

            return InitiatorFactionId == factionId || TargetFactionId == factionId;
        }

        public override string ToString()
        {
            return $"FlipMission[{Id}]: {InitiatorFactionId} -> {TargetLieutenantId} ({Status})";
        }
    }
}
