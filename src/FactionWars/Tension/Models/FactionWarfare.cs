using System;
using System.Collections.Generic;

namespace FactionWars.Tension.Models
{
    /// <summary>
    /// Tracks the warfare state between two factions.
    /// Maintains current state, history of transitions, and timing information.
    /// </summary>
    public class FactionWarfare : IEquatable<FactionWarfare>
    {
        private readonly List<WarfareStateTransition> _transitionHistory = new List<WarfareStateTransition>();
        private WarfareState _currentState;
        private DateTime _stateEnteredTime;

        /// <summary>
        /// The ID of the first faction in this warfare relationship.
        /// </summary>
        public string FactionId1 { get; }

        /// <summary>
        /// The ID of the second faction in this warfare relationship.
        /// </summary>
        public string FactionId2 { get; }

        /// <summary>
        /// The current warfare state between the two factions.
        /// </summary>
        public WarfareState CurrentState => _currentState;

        /// <summary>
        /// The UTC time when the current state was entered.
        /// </summary>
        public DateTime StateEnteredTime => _stateEnteredTime;

        /// <summary>
        /// The duration spent in the current state.
        /// </summary>
        public TimeSpan TimeInCurrentState => DateTime.UtcNow - _stateEnteredTime;

        /// <summary>
        /// The history of state transitions for this warfare relationship.
        /// </summary>
        public IReadOnlyList<WarfareStateTransition> TransitionHistory => _transitionHistory.AsReadOnly();

        /// <summary>
        /// True if the factions are at peace (no conflict).
        /// </summary>
        public bool IsAtPeace => _currentState == WarfareState.Peace;

        /// <summary>
        /// True if the factions are in active combat (BorderSkirmishes or above).
        /// </summary>
        public bool IsInCombat => (int)_currentState >= (int)WarfareState.BorderSkirmishes;

        /// <summary>
        /// True if the factions are hostile (ColdWar or above).
        /// </summary>
        public bool IsHostile => (int)_currentState >= (int)WarfareState.ColdWar;

        /// <summary>
        /// True if the factions are in total war (maximum escalation).
        /// </summary>
        public bool IsInTotalWar => _currentState == WarfareState.TotalWar;

        /// <summary>
        /// Creates a new warfare tracking record between two factions.
        /// </summary>
        /// <param name="factionId1">The first faction ID.</param>
        /// <param name="factionId2">The second faction ID.</param>
        /// <param name="initialState">The initial warfare state (default: Peace).</param>
        /// <exception cref="ArgumentNullException">Thrown if either faction ID is null.</exception>
        /// <exception cref="ArgumentException">Thrown if faction IDs are empty, whitespace, or the same.</exception>
        public FactionWarfare(string factionId1, string factionId2, WarfareState initialState = WarfareState.Peace)
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
                throw new ArgumentException("Cannot create warfare between a faction and itself.", nameof(factionId2));

            FactionId1 = factionId1;
            FactionId2 = factionId2;
            _currentState = initialState;
            _stateEnteredTime = DateTime.UtcNow;
        }

        /// <summary>
        /// Transitions to a new warfare state.
        /// </summary>
        /// <param name="newState">The new warfare state.</param>
        /// <param name="reason">The reason for the transition.</param>
        /// <param name="metadata">Optional metadata about the transition.</param>
        /// <returns>The transition record.</returns>
        /// <exception cref="InvalidOperationException">Thrown if attempting to transition to the same state.</exception>
        public WarfareStateTransition TransitionTo(WarfareState newState, WarfareStateTransitionReason reason, string? metadata = null)
        {
            if (newState == _currentState)
                throw new InvalidOperationException($"Cannot transition to the same state: {_currentState}");

            var transition = new WarfareStateTransition(
                FactionId1,
                FactionId2,
                _currentState,
                newState,
                reason,
                metadata);

            _transitionHistory.Add(transition);
            _currentState = newState;
            _stateEnteredTime = DateTime.UtcNow;

            return transition;
        }

        /// <summary>
        /// Checks if this warfare involves the specified faction.
        /// </summary>
        /// <param name="factionId">The faction ID to check.</param>
        /// <returns>True if the faction is part of this warfare relationship.</returns>
        public bool ContainsFaction(string factionId)
        {
            if (string.IsNullOrEmpty(factionId))
                return false;

            return FactionId1 == factionId || FactionId2 == factionId;
        }

        /// <summary>
        /// Checks if this warfare involves both specified factions.
        /// </summary>
        /// <param name="factionIdA">The first faction ID to check.</param>
        /// <param name="factionIdB">The second faction ID to check.</param>
        /// <returns>True if both factions are part of this warfare relationship.</returns>
        public bool InvolvesBothFactions(string factionIdA, string factionIdB)
        {
            return (FactionId1 == factionIdA && FactionId2 == factionIdB) ||
                   (FactionId1 == factionIdB && FactionId2 == factionIdA);
        }

        /// <summary>
        /// Gets the other faction in this warfare relationship.
        /// </summary>
        /// <param name="factionId">One of the factions in the relationship.</param>
        /// <returns>The ID of the other faction, or null if the given faction isn't in this relationship.</returns>
        public string? GetOtherFaction(string factionId)
        {
            if (FactionId1 == factionId)
                return FactionId2;
            if (FactionId2 == factionId)
                return FactionId1;
            return null;
        }

        #region Equality

        public bool Equals(FactionWarfare? other)
        {
            if (other is null) return false;
            return InvolvesBothFactions(other.FactionId1, other.FactionId2);
        }

        public override bool Equals(object? obj)
        {
            return obj is FactionWarfare warfare && Equals(warfare);
        }

        public override int GetHashCode()
        {
            // Order-independent hash code
            var hash1 = FactionId1.GetHashCode();
            var hash2 = FactionId2.GetHashCode();
            return hash1 ^ hash2;
        }

        public static bool operator ==(FactionWarfare? left, FactionWarfare? right)
        {
            if (left is null) return right is null;
            return left.Equals(right);
        }

        public static bool operator !=(FactionWarfare? left, FactionWarfare? right)
        {
            return !(left == right);
        }

        #endregion

        public override string ToString()
        {
            return $"Warfare[{FactionId1} <-> {FactionId2}]: {_currentState}";
        }
    }
}
