using System;

namespace FactionWars.Factions.Models
{
    /// <summary>
    /// Represents the relationship between two factions.
    /// Relationships are bidirectional and symmetric - both factions share the same value.
    /// </summary>
    public class FactionRelationship : IEquatable<FactionRelationship>
    {
        /// <summary>
        /// Minimum relationship value (war).
        /// </summary>
        public const int MinValue = -100;

        /// <summary>
        /// Maximum relationship value (allied).
        /// </summary>
        public const int MaxValue = 100;

        private const int WarThreshold = -51;
        private const int HostileThreshold = -26;
        private const int NeutralThreshold = 25;
        private const int FriendlyThreshold = 50;

        private int _value;

        /// <summary>
        /// The ID of the first faction in this relationship.
        /// </summary>
        public string FactionId1 { get; }

        /// <summary>
        /// The ID of the second faction in this relationship.
        /// </summary>
        public string FactionId2 { get; }

        /// <summary>
        /// The current relationship value between MinValue and MaxValue.
        /// </summary>
        public int Value => _value;

        /// <summary>
        /// The current diplomatic status derived from the relationship value.
        /// </summary>
        public RelationshipStatus Status
        {
            get
            {
                if (_value <= WarThreshold)
                    return RelationshipStatus.War;
                if (_value <= HostileThreshold)
                    return RelationshipStatus.Hostile;
                if (_value <= NeutralThreshold)
                    return RelationshipStatus.Neutral;
                if (_value <= FriendlyThreshold)
                    return RelationshipStatus.Friendly;
                return RelationshipStatus.Allied;
            }
        }

        /// <summary>
        /// Creates a new relationship between two factions.
        /// </summary>
        /// <param name="factionId1">The first faction ID.</param>
        /// <param name="factionId2">The second faction ID.</param>
        /// <param name="initialValue">The initial relationship value (default: 0 for neutral).</param>
        /// <exception cref="ArgumentNullException">Thrown if either faction ID is null.</exception>
        /// <exception cref="ArgumentException">Thrown if faction IDs are empty, whitespace, or the same.</exception>
        public FactionRelationship(string factionId1, string factionId2, int initialValue = 0)
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
                throw new ArgumentException("Cannot create a relationship between a faction and itself.", nameof(factionId2));

            FactionId1 = factionId1;
            FactionId2 = factionId2;
            _value = Clamp(initialValue);
        }

        /// <summary>
        /// Sets the relationship value, clamping to valid range.
        /// </summary>
        /// <param name="value">The new relationship value.</param>
        public void SetValue(int value)
        {
            _value = Clamp(value);
        }

        /// <summary>
        /// Adjusts the relationship value by the specified amount, clamping to valid range.
        /// </summary>
        /// <param name="amount">The amount to adjust (positive or negative).</param>
        public void AdjustValue(int amount)
        {
            _value = Clamp(_value + amount);
        }

        /// <summary>
        /// Checks if this relationship involves the specified faction.
        /// </summary>
        /// <param name="factionId">The faction ID to check.</param>
        /// <returns>True if the faction is part of this relationship.</returns>
        public bool ContainsFaction(string factionId)
        {
            if (string.IsNullOrEmpty(factionId))
                return false;

            return FactionId1 == factionId || FactionId2 == factionId;
        }

        /// <summary>
        /// Checks if this relationship involves both specified factions.
        /// </summary>
        /// <param name="factionIdA">The first faction ID to check.</param>
        /// <param name="factionIdB">The second faction ID to check.</param>
        /// <returns>True if both factions are part of this relationship.</returns>
        public bool InvolvesBothFactions(string factionIdA, string factionIdB)
        {
            return (FactionId1 == factionIdA && FactionId2 == factionIdB) ||
                   (FactionId1 == factionIdB && FactionId2 == factionIdA);
        }

        /// <summary>
        /// Gets the other faction in this relationship.
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

        private static int Clamp(int value)
        {
            if (value < MinValue) return MinValue;
            if (value > MaxValue) return MaxValue;
            return value;
        }

        #region Equality

        public bool Equals(FactionRelationship? other)
        {
            if (other is null) return false;
            return InvolvesBothFactions(other.FactionId1, other.FactionId2);
        }

        public override bool Equals(object? obj)
        {
            return obj is FactionRelationship relationship && Equals(relationship);
        }

        public override int GetHashCode()
        {
            // Order-independent hash code
            var hash1 = FactionId1.GetHashCode();
            var hash2 = FactionId2.GetHashCode();
            return hash1 ^ hash2;
        }

        public static bool operator ==(FactionRelationship? left, FactionRelationship? right)
        {
            if (left is null) return right is null;
            return left.Equals(right);
        }

        public static bool operator !=(FactionRelationship? left, FactionRelationship? right)
        {
            return !(left == right);
        }

        #endregion

        public override string ToString()
        {
            return $"Relationship[{FactionId1} <-> {FactionId2}]: {_value} ({Status})";
        }
    }
}
