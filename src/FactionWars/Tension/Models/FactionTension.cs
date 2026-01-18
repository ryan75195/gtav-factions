using System;

namespace FactionWars.Tension.Models
{
    /// <summary>
    /// Represents the tension level between two factions.
    /// Tension is a separate measure from relationship - it represents the build-up towards conflict
    /// and can escalate or de-escalate based on actions and time.
    /// </summary>
    public class FactionTension : IEquatable<FactionTension>
    {
        /// <summary>
        /// Minimum tension value (no tension).
        /// </summary>
        public const int MinValue = 0;

        /// <summary>
        /// Maximum tension value (critical).
        /// </summary>
        public const int MaxValue = 100;

        private const int UneasyThreshold = 25;
        private const int TenseThreshold = 50;
        private const int VolatileThreshold = 75;
        private const int CriticalThreshold = 90;

        private int _value;
        private float _decayRate = 1.0f;

        /// <summary>
        /// The ID of the first faction in this tension relationship.
        /// </summary>
        public string FactionId1 { get; }

        /// <summary>
        /// The ID of the second faction in this tension relationship.
        /// </summary>
        public string FactionId2 { get; }

        /// <summary>
        /// The current tension value between MinValue and MaxValue.
        /// </summary>
        public int Value => _value;

        /// <summary>
        /// The current tension level derived from the tension value.
        /// </summary>
        public TensionLevel Level
        {
            get
            {
                if (_value >= CriticalThreshold)
                    return TensionLevel.Critical;
                if (_value >= VolatileThreshold)
                    return TensionLevel.Volatile;
                if (_value >= TenseThreshold)
                    return TensionLevel.Tense;
                if (_value >= UneasyThreshold)
                    return TensionLevel.Uneasy;
                return TensionLevel.Calm;
            }
        }

        /// <summary>
        /// The rate at which tension decays over time.
        /// </summary>
        public float DecayRate => _decayRate;

        /// <summary>
        /// The UTC time when the tension was last updated.
        /// </summary>
        public DateTime LastUpdateTime { get; private set; }

        /// <summary>
        /// Creates a new tension record between two factions.
        /// </summary>
        /// <param name="factionId1">The first faction ID.</param>
        /// <param name="factionId2">The second faction ID.</param>
        /// <param name="initialValue">The initial tension value (default: 0 for calm).</param>
        /// <exception cref="ArgumentNullException">Thrown if either faction ID is null.</exception>
        /// <exception cref="ArgumentException">Thrown if faction IDs are empty, whitespace, or the same.</exception>
        public FactionTension(string factionId1, string factionId2, int initialValue = 0)
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
                throw new ArgumentException("Cannot create tension between a faction and itself.", nameof(factionId2));

            FactionId1 = factionId1;
            FactionId2 = factionId2;
            _value = Clamp(initialValue);
            LastUpdateTime = DateTime.UtcNow;
        }

        /// <summary>
        /// Increases the tension value by the specified amount.
        /// </summary>
        /// <param name="amount">The amount to increase (must be non-negative).</param>
        /// <exception cref="ArgumentException">Thrown if amount is negative.</exception>
        public void Increase(int amount)
        {
            if (amount < 0)
                throw new ArgumentException("Amount must be non-negative.", nameof(amount));

            _value = Clamp(_value + amount);
            LastUpdateTime = DateTime.UtcNow;
        }

        /// <summary>
        /// Decreases the tension value by the specified amount.
        /// </summary>
        /// <param name="amount">The amount to decrease (must be non-negative).</param>
        /// <exception cref="ArgumentException">Thrown if amount is negative.</exception>
        public void Decrease(int amount)
        {
            if (amount < 0)
                throw new ArgumentException("Amount must be non-negative.", nameof(amount));

            _value = Clamp(_value - amount);
            LastUpdateTime = DateTime.UtcNow;
        }

        /// <summary>
        /// Sets the tension value, clamping to valid range.
        /// </summary>
        /// <param name="value">The new tension value.</param>
        public void SetValue(int value)
        {
            _value = Clamp(value);
            LastUpdateTime = DateTime.UtcNow;
        }

        /// <summary>
        /// Sets the decay rate for tension reduction over time.
        /// </summary>
        /// <param name="rate">The decay rate (must be non-negative).</param>
        /// <exception cref="ArgumentException">Thrown if rate is negative.</exception>
        public void SetDecayRate(float rate)
        {
            if (rate < 0)
                throw new ArgumentException("Decay rate cannot be negative.", nameof(rate));

            _decayRate = rate;
        }

        /// <summary>
        /// Applies decay to the tension value based on the current decay rate.
        /// </summary>
        public void ApplyDecay()
        {
            _value = Clamp(_value - (int)_decayRate);
            LastUpdateTime = DateTime.UtcNow;
        }

        /// <summary>
        /// Checks if this tension involves the specified faction.
        /// </summary>
        /// <param name="factionId">The faction ID to check.</param>
        /// <returns>True if the faction is part of this tension relationship.</returns>
        public bool ContainsFaction(string factionId)
        {
            if (string.IsNullOrEmpty(factionId))
                return false;

            return FactionId1 == factionId || FactionId2 == factionId;
        }

        /// <summary>
        /// Checks if this tension involves both specified factions.
        /// </summary>
        /// <param name="factionIdA">The first faction ID to check.</param>
        /// <param name="factionIdB">The second faction ID to check.</param>
        /// <returns>True if both factions are part of this tension relationship.</returns>
        public bool InvolvesBothFactions(string factionIdA, string factionIdB)
        {
            return (FactionId1 == factionIdA && FactionId2 == factionIdB) ||
                   (FactionId1 == factionIdB && FactionId2 == factionIdA);
        }

        /// <summary>
        /// Gets the other faction in this tension relationship.
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

        public bool Equals(FactionTension? other)
        {
            if (other is null) return false;
            return InvolvesBothFactions(other.FactionId1, other.FactionId2);
        }

        public override bool Equals(object? obj)
        {
            return obj is FactionTension tension && Equals(tension);
        }

        public override int GetHashCode()
        {
            // Order-independent hash code
            var hash1 = FactionId1.GetHashCode();
            var hash2 = FactionId2.GetHashCode();
            return hash1 ^ hash2;
        }

        public static bool operator ==(FactionTension? left, FactionTension? right)
        {
            if (left is null) return right is null;
            return left.Equals(right);
        }

        public static bool operator !=(FactionTension? left, FactionTension? right)
        {
            return !(left == right);
        }

        #endregion

        public override string ToString()
        {
            return $"Tension[{FactionId1} <-> {FactionId2}]: {_value} ({Level})";
        }
    }
}
