using System;

namespace FactionWars.Tension.Models
{
    /// <summary>
    /// Represents a covert operation being conducted by one faction against another.
    /// Operations progress through various states and have different effects based on type.
    /// </summary>
    public class CovertOperation
    {
        private CovertOperationStatus _status;
        private bool _wasSuccessful;
        private bool _wasDetected;

        /// <summary>
        /// Unique identifier for this operation.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// The type of covert operation.
        /// </summary>
        public CovertOperationType OperationType { get; }

        /// <summary>
        /// The faction initiating the operation.
        /// </summary>
        public string InitiatorFactionId { get; }

        /// <summary>
        /// The faction being targeted by the operation.
        /// </summary>
        public string TargetFactionId { get; }

        /// <summary>
        /// The zone being targeted, if applicable.
        /// </summary>
        public string? TargetZoneId { get; }

        /// <summary>
        /// The current status of the operation.
        /// </summary>
        public CovertOperationStatus Status => _status;

        /// <summary>
        /// The UTC time when the operation was created.
        /// </summary>
        public DateTime CreatedTime { get; }

        /// <summary>
        /// The UTC time when the operation started execution.
        /// </summary>
        public DateTime? StartTime { get; private set; }

        /// <summary>
        /// The UTC time when the operation completed.
        /// </summary>
        public DateTime? CompletionTime { get; private set; }

        /// <summary>
        /// The base cost in cash for this operation type.
        /// </summary>
        public int BaseCost
        {
            get
            {
                return OperationType switch
                {
                    CovertOperationType.Sabotage => 5000,
                    CovertOperationType.Assassination => 15000,
                    CovertOperationType.Bribery => 10000,
                    _ => 5000
                };
            }
        }

        /// <summary>
        /// The base duration in seconds for this operation type.
        /// </summary>
        public int BaseDurationSeconds
        {
            get
            {
                return OperationType switch
                {
                    CovertOperationType.Sabotage => 60,
                    CovertOperationType.Assassination => 120,
                    CovertOperationType.Bribery => 90,
                    _ => 60
                };
            }
        }

        /// <summary>
        /// The base success chance for this operation type (0.0 to 1.0).
        /// </summary>
        public float BaseSuccessChance
        {
            get
            {
                return OperationType switch
                {
                    CovertOperationType.Sabotage => 0.7f,
                    CovertOperationType.Assassination => 0.4f,
                    CovertOperationType.Bribery => 0.6f,
                    _ => 0.5f
                };
            }
        }

        /// <summary>
        /// The base detection chance for this operation type (0.0 to 1.0).
        /// </summary>
        public float BaseDetectionChance
        {
            get
            {
                return OperationType switch
                {
                    CovertOperationType.Sabotage => 0.3f,
                    CovertOperationType.Assassination => 0.5f,
                    CovertOperationType.Bribery => 0.2f,
                    _ => 0.3f
                };
            }
        }

        /// <summary>
        /// Whether the operation has reached a terminal state.
        /// </summary>
        public bool IsTerminal =>
            _status == CovertOperationStatus.Succeeded ||
            _status == CovertOperationStatus.Failed ||
            _status == CovertOperationStatus.Detected ||
            _status == CovertOperationStatus.Cancelled;

        /// <summary>
        /// Whether the operation achieved its objective (may still have been detected).
        /// </summary>
        public bool WasSuccessful => _wasSuccessful;

        /// <summary>
        /// Whether the operation was detected by the target faction.
        /// </summary>
        public bool WasDetected => _wasDetected;

        /// <summary>
        /// Creates a new covert operation.
        /// </summary>
        /// <param name="operationType">The type of operation.</param>
        /// <param name="initiatorFactionId">The faction initiating the operation.</param>
        /// <param name="targetFactionId">The faction being targeted.</param>
        /// <param name="targetZoneId">Optional zone being targeted.</param>
        /// <exception cref="ArgumentNullException">Thrown if faction IDs are null.</exception>
        /// <exception cref="ArgumentException">Thrown if faction IDs are invalid or the same.</exception>
        public CovertOperation(
            CovertOperationType operationType,
            string initiatorFactionId,
            string targetFactionId,
            string? targetZoneId)
        {
            if (initiatorFactionId == null)
                throw new ArgumentNullException(nameof(initiatorFactionId));
            if (string.IsNullOrWhiteSpace(initiatorFactionId))
                throw new ArgumentException("Initiator faction ID cannot be empty or whitespace.", nameof(initiatorFactionId));

            if (targetFactionId == null)
                throw new ArgumentNullException(nameof(targetFactionId));
            if (string.IsNullOrWhiteSpace(targetFactionId))
                throw new ArgumentException("Target faction ID cannot be empty or whitespace.", nameof(targetFactionId));

            if (initiatorFactionId == targetFactionId)
                throw new ArgumentException("Cannot conduct covert operation against own faction.", nameof(targetFactionId));

            Id = Guid.NewGuid().ToString();
            OperationType = operationType;
            InitiatorFactionId = initiatorFactionId;
            TargetFactionId = targetFactionId;
            TargetZoneId = targetZoneId;
            _status = CovertOperationStatus.Pending;
            CreatedTime = DateTime.UtcNow;
        }

        /// <summary>
        /// Starts the operation execution.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if the operation is not in Pending state.</exception>
        public void Start()
        {
            if (_status != CovertOperationStatus.Pending)
                throw new InvalidOperationException($"Cannot start operation in {_status} state.");

            _status = CovertOperationStatus.InProgress;
            StartTime = DateTime.UtcNow;
        }

        /// <summary>
        /// Completes the operation with the specified outcome.
        /// </summary>
        /// <param name="success">Whether the operation achieved its objective.</param>
        /// <param name="detected">Whether the operation was detected by the target faction.</param>
        /// <exception cref="InvalidOperationException">Thrown if the operation is not in InProgress state.</exception>
        public void Complete(bool success, bool detected)
        {
            if (_status != CovertOperationStatus.InProgress)
                throw new InvalidOperationException($"Cannot complete operation in {_status} state.");

            _wasSuccessful = success;
            _wasDetected = detected;

            if (detected)
            {
                _status = CovertOperationStatus.Detected;
            }
            else if (success)
            {
                _status = CovertOperationStatus.Succeeded;
            }
            else
            {
                _status = CovertOperationStatus.Failed;
            }

            CompletionTime = DateTime.UtcNow;
        }

        /// <summary>
        /// Cancels the operation.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if the operation is already complete.</exception>
        public void Cancel()
        {
            if (IsTerminal)
                throw new InvalidOperationException($"Cannot cancel operation in {_status} state.");

            _status = CovertOperationStatus.Cancelled;
            CompletionTime = DateTime.UtcNow;
        }

        /// <summary>
        /// Checks if this operation involves the specified faction.
        /// </summary>
        /// <param name="factionId">The faction ID to check.</param>
        /// <returns>True if the faction is either the initiator or target.</returns>
        public bool InvolvesFaction(string factionId)
        {
            if (string.IsNullOrEmpty(factionId))
                return false;

            return InitiatorFactionId == factionId || TargetFactionId == factionId;
        }

        public override string ToString()
        {
            return $"CovertOp[{OperationType}]: {InitiatorFactionId} -> {TargetFactionId} ({_status})";
        }
    }
}
