using System;

namespace FactionWars.Tension.Models
{
    /// <summary>
    /// Represents a diplomatic action between two factions. Tracks the lifecycle
    /// from proposal through activation to termination.
    /// </summary>
    public class DiplomaticAction
    {
        /// <summary>
        /// Unique identifier for this diplomatic action.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// The faction that initiated this diplomatic action.
        /// </summary>
        public string InitiatorFactionId { get; }

        /// <summary>
        /// The faction that is the target of this diplomatic action.
        /// </summary>
        public string TargetFactionId { get; }

        /// <summary>
        /// The type of diplomatic action.
        /// </summary>
        public DiplomaticActionType ActionType { get; }

        /// <summary>
        /// The current status of this diplomatic action.
        /// </summary>
        public DiplomaticActionStatus Status { get; private set; }

        /// <summary>
        /// The time when this action was created.
        /// </summary>
        public DateTime CreatedTime { get; }

        /// <summary>
        /// The time when this action was proposed to the target faction.
        /// </summary>
        public DateTime? ProposedTime { get; private set; }

        /// <summary>
        /// The time when this action was accepted by the target faction.
        /// </summary>
        public DateTime? AcceptedTime { get; private set; }

        /// <summary>
        /// The time when this action was activated.
        /// </summary>
        public DateTime? ActivatedTime { get; private set; }

        /// <summary>
        /// The reason for rejection if the action was rejected.
        /// </summary>
        public string? RejectionReason { get; private set; }

        /// <summary>
        /// The faction that violated this agreement if it was broken.
        /// </summary>
        public string? ViolatorFactionId { get; private set; }

        /// <summary>
        /// The reason for violation if the action was broken.
        /// </summary>
        public string? ViolationReason { get; private set; }

        /// <summary>
        /// Creates a new diplomatic action between two factions.
        /// </summary>
        /// <param name="initiatorFactionId">The faction initiating the action.</param>
        /// <param name="targetFactionId">The faction targeted by the action.</param>
        /// <param name="actionType">The type of diplomatic action.</param>
        /// <exception cref="ArgumentNullException">Thrown when faction IDs are null.</exception>
        /// <exception cref="ArgumentException">Thrown when faction IDs are empty, whitespace, or identical.</exception>
        public DiplomaticAction(string initiatorFactionId, string targetFactionId, DiplomaticActionType actionType)
        {
            if (initiatorFactionId == null)
                throw new ArgumentNullException(nameof(initiatorFactionId));
            if (targetFactionId == null)
                throw new ArgumentNullException(nameof(targetFactionId));
            if (string.IsNullOrWhiteSpace(initiatorFactionId))
                throw new ArgumentException("Initiator faction ID cannot be empty or whitespace.", nameof(initiatorFactionId));
            if (string.IsNullOrWhiteSpace(targetFactionId))
                throw new ArgumentException("Target faction ID cannot be empty or whitespace.", nameof(targetFactionId));
            if (initiatorFactionId == targetFactionId)
                throw new ArgumentException("Initiator and target faction IDs cannot be the same.", nameof(targetFactionId));

            Id = Guid.NewGuid().ToString();
            InitiatorFactionId = initiatorFactionId;
            TargetFactionId = targetFactionId;
            ActionType = actionType;
            Status = DiplomaticActionStatus.Proposed;
            CreatedTime = DateTime.UtcNow;
        }

        /// <summary>
        /// Proposes this action to the target faction, transitioning from Proposed to Pending.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when status is not Proposed.</exception>
        public void Propose()
        {
            if (Status != DiplomaticActionStatus.Proposed)
                throw new InvalidOperationException($"Cannot propose action in {Status} state. Must be in Proposed state.");

            Status = DiplomaticActionStatus.Pending;
            ProposedTime = DateTime.UtcNow;
        }

        /// <summary>
        /// Accepts this action, transitioning from Pending to Accepted.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when status is not Pending.</exception>
        public void Accept()
        {
            if (Status != DiplomaticActionStatus.Pending)
                throw new InvalidOperationException($"Cannot accept action in {Status} state. Must be in Pending state.");

            Status = DiplomaticActionStatus.Accepted;
            AcceptedTime = DateTime.UtcNow;
        }

        /// <summary>
        /// Rejects this action, transitioning from Pending to Rejected.
        /// </summary>
        /// <param name="reason">The reason for rejection.</param>
        /// <exception cref="InvalidOperationException">Thrown when status is not Pending.</exception>
        public void Reject(string reason)
        {
            if (Status != DiplomaticActionStatus.Pending)
                throw new InvalidOperationException($"Cannot reject action in {Status} state. Must be in Pending state.");

            Status = DiplomaticActionStatus.Rejected;
            RejectionReason = reason;
        }

        /// <summary>
        /// Activates this action, transitioning from Accepted to Active.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when status is not Accepted.</exception>
        public void Activate()
        {
            if (Status != DiplomaticActionStatus.Accepted)
                throw new InvalidOperationException($"Cannot activate action in {Status} state. Must be in Accepted state.");

            Status = DiplomaticActionStatus.Active;
            ActivatedTime = DateTime.UtcNow;
        }

        /// <summary>
        /// Expires this action, transitioning from Active to Expired.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when status is not Active.</exception>
        public void Expire()
        {
            if (Status != DiplomaticActionStatus.Active)
                throw new InvalidOperationException($"Cannot expire action in {Status} state. Must be in Active state.");

            Status = DiplomaticActionStatus.Expired;
        }

        /// <summary>
        /// Breaks this action due to a violation, transitioning from Active to Broken.
        /// </summary>
        /// <param name="violatorFactionId">The faction that violated the agreement.</param>
        /// <param name="reason">The reason for the violation.</param>
        /// <exception cref="InvalidOperationException">Thrown when status is not Active.</exception>
        /// <exception cref="ArgumentException">Thrown when violator is not a party to this action.</exception>
        public void Break(string violatorFactionId, string reason)
        {
            if (Status != DiplomaticActionStatus.Active)
                throw new InvalidOperationException($"Cannot break action in {Status} state. Must be in Active state.");

            if (!ContainsFaction(violatorFactionId))
                throw new ArgumentException("Violator must be a party to this diplomatic action.", nameof(violatorFactionId));

            Status = DiplomaticActionStatus.Broken;
            ViolatorFactionId = violatorFactionId;
            ViolationReason = reason;
        }

        /// <summary>
        /// Cancels this action before activation, transitioning to Cancelled.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when action is already Active.</exception>
        public void Cancel()
        {
            if (Status == DiplomaticActionStatus.Active)
                throw new InvalidOperationException("Cannot cancel an active action. Use Break instead.");

            if (IsTerminal)
                throw new InvalidOperationException($"Cannot cancel action in {Status} state. Action is already terminated.");

            Status = DiplomaticActionStatus.Cancelled;
        }

        /// <summary>
        /// Returns true if this action is in a terminal state (cannot transition further).
        /// </summary>
        public bool IsTerminal => Status == DiplomaticActionStatus.Rejected
                               || Status == DiplomaticActionStatus.Expired
                               || Status == DiplomaticActionStatus.Broken
                               || Status == DiplomaticActionStatus.Cancelled;

        /// <summary>
        /// Returns true if this action is awaiting a response from the target faction.
        /// </summary>
        public bool IsPendingResponse => Status == DiplomaticActionStatus.Pending;

        /// <summary>
        /// Returns true if this action is currently active and in effect.
        /// </summary>
        public bool IsActive => Status == DiplomaticActionStatus.Active;

        /// <summary>
        /// Returns true if this action was broken due to a violation.
        /// </summary>
        public bool WasBroken => Status == DiplomaticActionStatus.Broken;

        /// <summary>
        /// Checks if the specified faction is involved in this action.
        /// </summary>
        /// <param name="factionId">The faction ID to check.</param>
        /// <returns>True if the faction is the initiator or target.</returns>
        public bool ContainsFaction(string factionId)
        {
            if (string.IsNullOrEmpty(factionId))
                return false;

            return factionId == InitiatorFactionId || factionId == TargetFactionId;
        }

        /// <summary>
        /// Checks if both specified factions are involved in this action (order independent).
        /// </summary>
        /// <param name="factionId1">The first faction ID.</param>
        /// <param name="factionId2">The second faction ID.</param>
        /// <returns>True if both factions are involved in this action.</returns>
        public bool InvolvesBothFactions(string factionId1, string factionId2)
        {
            return ContainsFaction(factionId1) && ContainsFaction(factionId2);
        }

        /// <summary>
        /// Gets the other faction involved in this action.
        /// </summary>
        /// <param name="factionId">The faction ID of one party.</param>
        /// <returns>The faction ID of the other party, or null if not involved.</returns>
        public string? GetOtherFaction(string factionId)
        {
            if (factionId == InitiatorFactionId)
                return TargetFactionId;
            if (factionId == TargetFactionId)
                return InitiatorFactionId;
            return null;
        }

        /// <summary>
        /// Gets the default duration in seconds for this action type.
        /// Returns 0 for permanent actions.
        /// </summary>
        public int DefaultDurationSeconds => ActionType switch
        {
            DiplomaticActionType.Ceasefire => 300,           // 5 minutes
            DiplomaticActionType.NonAggressionPact => 600,   // 10 minutes
            DiplomaticActionType.TradeAgreement => 900,      // 15 minutes
            DiplomaticActionType.MutualDefense => 1200,      // 20 minutes
            DiplomaticActionType.Alliance => 0,              // Permanent
            DiplomaticActionType.DeclarationOfWar => 0,      // Until peace
            DiplomaticActionType.PeaceTreaty => 1800,        // 30 minutes
            DiplomaticActionType.TerritorialConcession => 0, // Instant
            _ => 0
        };

        /// <summary>
        /// Gets the remaining duration in seconds if the action is active with a duration.
        /// Returns null if not active or if the action is permanent.
        /// </summary>
        public int? RemainingDurationSeconds
        {
            get
            {
                if (!IsActive || ActivatedTime == null)
                    return null;

                if (DefaultDurationSeconds == 0)
                    return null;

                var elapsed = (int)(DateTime.UtcNow - ActivatedTime.Value).TotalSeconds;
                var remaining = DefaultDurationSeconds - elapsed;
                return remaining > 0 ? remaining : 0;
            }
        }

        /// <summary>
        /// Gets the tension impact when this action becomes active.
        /// Negative values reduce tension, positive values increase it.
        /// </summary>
        public int TensionImpact => ActionType switch
        {
            DiplomaticActionType.Ceasefire => -20,
            DiplomaticActionType.NonAggressionPact => -15,
            DiplomaticActionType.TradeAgreement => -10,
            DiplomaticActionType.MutualDefense => -25,
            DiplomaticActionType.Alliance => -40,
            DiplomaticActionType.DeclarationOfWar => 50,
            DiplomaticActionType.PeaceTreaty => -35,
            DiplomaticActionType.TerritorialConcession => -15,
            _ => 0
        };

        /// <summary>
        /// Gets the tension penalty when this action is violated/broken.
        /// </summary>
        public int ViolationTensionPenalty => ActionType switch
        {
            DiplomaticActionType.Ceasefire => 30,
            DiplomaticActionType.NonAggressionPact => 25,
            DiplomaticActionType.TradeAgreement => 15,
            DiplomaticActionType.MutualDefense => 40,
            DiplomaticActionType.Alliance => 50,
            DiplomaticActionType.DeclarationOfWar => 0,
            DiplomaticActionType.PeaceTreaty => 45,
            DiplomaticActionType.TerritorialConcession => 20,
            _ => 10
        };

        /// <summary>
        /// Gets the target warfare state when this action becomes active.
        /// Returns null if the action does not affect warfare state.
        /// </summary>
        public WarfareState? TargetWarfareState => ActionType switch
        {
            DiplomaticActionType.Ceasefire => WarfareState.ColdWar,
            DiplomaticActionType.DeclarationOfWar => WarfareState.OpenWarfare,
            DiplomaticActionType.PeaceTreaty => WarfareState.Peace,
            _ => null
        };

        /// <summary>
        /// Gets whether this action requires mutual agreement (acceptance by target).
        /// DeclarationOfWar is unilateral and does not require acceptance.
        /// </summary>
        public bool RequiresMutualAgreement => ActionType != DiplomaticActionType.DeclarationOfWar;

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{ActionType} between {InitiatorFactionId} and {TargetFactionId} ({Status})";
        }
    }
}
