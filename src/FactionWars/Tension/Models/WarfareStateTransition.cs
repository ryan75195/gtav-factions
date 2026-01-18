using System;

namespace FactionWars.Tension.Models
{
    /// <summary>
    /// Represents a state change in warfare between two factions.
    /// Records the transition details including previous/new states, reason, and timing.
    /// </summary>
    public class WarfareStateTransition
    {
        /// <summary>
        /// The ID of the first faction in this warfare relationship.
        /// </summary>
        public string FactionId1 { get; }

        /// <summary>
        /// The ID of the second faction in this warfare relationship.
        /// </summary>
        public string FactionId2 { get; }

        /// <summary>
        /// The warfare state before this transition.
        /// </summary>
        public WarfareState PreviousState { get; }

        /// <summary>
        /// The warfare state after this transition.
        /// </summary>
        public WarfareState NewState { get; }

        /// <summary>
        /// The reason for this state transition.
        /// </summary>
        public WarfareStateTransitionReason Reason { get; }

        /// <summary>
        /// The UTC time when this transition occurred.
        /// </summary>
        public DateTime Timestamp { get; }

        /// <summary>
        /// Optional metadata about the transition (e.g., specific trigger details).
        /// </summary>
        public string? Metadata { get; }

        /// <summary>
        /// True if the new state is higher (more intense) than the previous state.
        /// </summary>
        public bool IsEscalation => (int)NewState > (int)PreviousState;

        /// <summary>
        /// True if the new state is lower (less intense) than the previous state.
        /// </summary>
        public bool IsDeescalation => (int)NewState < (int)PreviousState;

        /// <summary>
        /// True if this transition moves from a non-combat state to a combat state.
        /// Combat states are BorderSkirmishes and above.
        /// </summary>
        public bool EntersCombat
        {
            get
            {
                bool wasInCombat = (int)PreviousState >= (int)WarfareState.BorderSkirmishes;
                bool isInCombat = (int)NewState >= (int)WarfareState.BorderSkirmishes;
                return !wasInCombat && isInCombat;
            }
        }

        /// <summary>
        /// True if this transition moves from a combat state to a non-combat state.
        /// </summary>
        public bool ExitsCombat
        {
            get
            {
                bool wasInCombat = (int)PreviousState >= (int)WarfareState.BorderSkirmishes;
                bool isInCombat = (int)NewState >= (int)WarfareState.BorderSkirmishes;
                return wasInCombat && !isInCombat;
            }
        }

        /// <summary>
        /// Creates a new warfare state transition record.
        /// </summary>
        /// <param name="factionId1">The first faction ID.</param>
        /// <param name="factionId2">The second faction ID.</param>
        /// <param name="previousState">The state before transition.</param>
        /// <param name="newState">The state after transition.</param>
        /// <param name="reason">The reason for the transition.</param>
        /// <param name="metadata">Optional metadata about the transition.</param>
        /// <exception cref="ArgumentNullException">Thrown if either faction ID is null.</exception>
        /// <exception cref="ArgumentException">Thrown if faction IDs are empty, whitespace, the same, or if states are equal.</exception>
        public WarfareStateTransition(
            string factionId1,
            string factionId2,
            WarfareState previousState,
            WarfareState newState,
            WarfareStateTransitionReason reason,
            string? metadata = null)
        {
            if (factionId1 == null)
                throw new ArgumentNullException(nameof(factionId1));
            if (string.IsNullOrWhiteSpace(factionId1))
                throw new ArgumentException("Faction ID cannot be empty or whitespace.", nameof(factionId1));

            if (factionId2 == null)
                throw new ArgumentNullException(nameof(factionId2));
            if (string.IsNullOrWhiteSpace(factionId2))
                throw new ArgumentException("Faction ID cannot be empty or whitespace.", nameof(factionId2));

            if (factionId1 == factionId2)
                throw new ArgumentException("Cannot create transition between a faction and itself.", nameof(factionId2));

            if (previousState == newState)
                throw new ArgumentException("Previous state and new state must be different.", nameof(newState));

            FactionId1 = factionId1;
            FactionId2 = factionId2;
            PreviousState = previousState;
            NewState = newState;
            Reason = reason;
            Timestamp = DateTime.UtcNow;
            Metadata = metadata;
        }

        /// <summary>
        /// Checks if this transition involves the specified faction.
        /// </summary>
        /// <param name="factionId">The faction ID to check.</param>
        /// <returns>True if the faction is part of this transition.</returns>
        public bool InvolvesFaction(string factionId)
        {
            if (string.IsNullOrEmpty(factionId))
                return false;

            return FactionId1 == factionId || FactionId2 == factionId;
        }

        public override string ToString()
        {
            var direction = IsEscalation ? "↑" : "↓";
            return $"WarfareTransition[{FactionId1} <-> {FactionId2}]: {PreviousState} {direction} {NewState} ({Reason})";
        }
    }
}
