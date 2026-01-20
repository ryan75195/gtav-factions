using System;

namespace FactionWars.Core.Models
{
    /// <summary>
    /// Represents a composition of troops by tier (Basic, Medium, Heavy).
    /// Used for tracking troops in battles, reserves, and zone allocations.
    /// </summary>
    public sealed class TroopComposition : IEquatable<TroopComposition>
    {
        /// <summary>
        /// Combat strength modifier for Basic tier troops.
        /// </summary>
        public const float BasicModifier = 1.0f;

        /// <summary>
        /// Combat strength modifier for Medium tier troops.
        /// </summary>
        public const float MediumModifier = 1.5f;

        /// <summary>
        /// Combat strength modifier for Heavy tier troops.
        /// </summary>
        public const float HeavyModifier = 2.0f;

        /// <summary>
        /// Number of Basic tier troops.
        /// </summary>
        public int Basic { get; }

        /// <summary>
        /// Number of Medium tier troops.
        /// </summary>
        public int Medium { get; }

        /// <summary>
        /// Number of Heavy tier troops.
        /// </summary>
        public int Heavy { get; }

        /// <summary>
        /// Total count of all troops regardless of tier.
        /// </summary>
        public int TotalCount => Basic + Medium + Heavy;

        /// <summary>
        /// Total combat strength calculated using tier modifiers.
        /// Basic=1.0, Medium=1.5, Heavy=2.0
        /// </summary>
        public float TotalStrength => (Basic * BasicModifier) + (Medium * MediumModifier) + (Heavy * HeavyModifier);

        /// <summary>
        /// Returns true if there are no troops.
        /// </summary>
        public bool IsEmpty => TotalCount == 0;

        /// <summary>
        /// An empty troop composition with zero of each tier.
        /// </summary>
        public static TroopComposition Empty { get; } = new TroopComposition(0, 0, 0);

        /// <summary>
        /// Creates a new troop composition.
        /// </summary>
        /// <param name="basic">Number of Basic tier troops (must be non-negative).</param>
        /// <param name="medium">Number of Medium tier troops (must be non-negative).</param>
        /// <param name="heavy">Number of Heavy tier troops (must be non-negative).</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if any value is negative.</exception>
        public TroopComposition(int basic, int medium, int heavy)
        {
            if (basic < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(basic), "Basic troops cannot be negative.");
            }

            if (medium < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(medium), "Medium troops cannot be negative.");
            }

            if (heavy < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(heavy), "Heavy troops cannot be negative.");
            }

            Basic = basic;
            Medium = medium;
            Heavy = heavy;
        }

        /// <summary>
        /// Creates a new TroopComposition by subtracting casualties from this composition.
        /// Values are clamped to zero (no negative troops).
        /// </summary>
        /// <param name="casualties">The casualties to subtract.</param>
        /// <returns>A new TroopComposition with remaining troops.</returns>
        /// <exception cref="ArgumentNullException">Thrown if casualties is null.</exception>
        public TroopComposition Subtract(TroopComposition casualties)
        {
            if (casualties == null)
            {
                throw new ArgumentNullException(nameof(casualties));
            }

            return new TroopComposition(
                basic: Math.Max(0, Basic - casualties.Basic),
                medium: Math.Max(0, Medium - casualties.Medium),
                heavy: Math.Max(0, Heavy - casualties.Heavy));
        }

        /// <summary>
        /// Creates a new TroopComposition by adding troops to this composition.
        /// </summary>
        /// <param name="other">The troops to add.</param>
        /// <returns>A new TroopComposition with combined troops.</returns>
        /// <exception cref="ArgumentNullException">Thrown if other is null.</exception>
        public TroopComposition Add(TroopComposition other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            return new TroopComposition(
                basic: Basic + other.Basic,
                medium: Medium + other.Medium,
                heavy: Heavy + other.Heavy);
        }

        public bool Equals(TroopComposition? other)
        {
            if (other is null)
            {
                return false;
            }

            return Basic == other.Basic && Medium == other.Medium && Heavy == other.Heavy;
        }

        public override bool Equals(object? obj)
        {
            return obj is TroopComposition other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;
                hash = hash * 31 + Basic.GetHashCode();
                hash = hash * 31 + Medium.GetHashCode();
                hash = hash * 31 + Heavy.GetHashCode();
                return hash;
            }
        }

        public override string ToString()
        {
            return $"TroopComposition(Basic={Basic}, Medium={Medium}, Heavy={Heavy}, Strength={TotalStrength:F1})";
        }

        public static bool operator ==(TroopComposition? left, TroopComposition? right)
        {
            if (left is null)
            {
                return right is null;
            }

            return left.Equals(right);
        }

        public static bool operator !=(TroopComposition? left, TroopComposition? right)
        {
            return !(left == right);
        }
    }
}
