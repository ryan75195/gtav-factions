using System;

namespace FactionWars.Escalation.Models
{
    /// <summary>
    /// Represents the escalation level for a faction.
    /// Escalation progresses as factions engage in warfare, unlocking better weapons and vehicles.
    /// </summary>
    public class FactionEscalation : IEquatable<FactionEscalation>
    {
        /// <summary>
        /// Minimum escalation points (no escalation).
        /// </summary>
        public const int MinPoints = 0;

        /// <summary>
        /// Maximum escalation points (full escalation).
        /// </summary>
        public const int MaxPoints = 10000;

        /// <summary>
        /// Points required to reach Tier 2.
        /// </summary>
        public const int Tier2Threshold = 1000;

        /// <summary>
        /// Points required to reach Tier 3.
        /// </summary>
        public const int Tier3Threshold = 3000;

        /// <summary>
        /// Points required to reach Tier 4.
        /// </summary>
        public const int Tier4Threshold = 6000;

        /// <summary>
        /// Points required to reach Tier 5.
        /// </summary>
        public const int Tier5Threshold = 9000;

        private int _points;
        private EscalationTier _previousTier;

        /// <summary>
        /// The ID of the faction this escalation belongs to.
        /// </summary>
        public string FactionId { get; }

        /// <summary>
        /// The current escalation points.
        /// </summary>
        public int Points => _points;

        /// <summary>
        /// The current escalation tier derived from the points.
        /// </summary>
        public EscalationTier CurrentTier => GetTierFromPoints(_points);

        /// <summary>
        /// The previous escalation tier before the last tier change.
        /// </summary>
        public EscalationTier PreviousTier => _previousTier;

        /// <summary>
        /// The UTC time when the escalation was last updated.
        /// </summary>
        public DateTime LastUpdateTime { get; private set; }

        /// <summary>
        /// The progress percentage towards the next tier (0-100).
        /// Returns 100 if already at max tier.
        /// </summary>
        public float ProgressToNextTier
        {
            get
            {
                var currentTier = CurrentTier;
                if (currentTier == EscalationTier.Tier5)
                {
                    return 100f;
                }

                var currentThreshold = GetThresholdForTier(currentTier);
                var nextThreshold = GetThresholdForTier(currentTier + 1);
                var range = nextThreshold - currentThreshold;
                var progress = _points - currentThreshold;

                return (float)progress / range * 100f;
            }
        }

        /// <summary>
        /// The number of points needed to reach the next tier.
        /// Returns 0 if already at max tier.
        /// </summary>
        public int PointsToNextTier
        {
            get
            {
                var currentTier = CurrentTier;
                if (currentTier == EscalationTier.Tier5)
                {
                    return 0;
                }

                var nextThreshold = GetThresholdForTier(currentTier + 1);
                return nextThreshold - _points;
            }
        }

        /// <summary>
        /// Creates a new escalation record for a faction.
        /// </summary>
        /// <param name="factionId">The faction ID.</param>
        /// <param name="initialPoints">The initial escalation points (default: 0).</param>
        /// <exception cref="ArgumentNullException">Thrown if faction ID is null.</exception>
        /// <exception cref="ArgumentException">Thrown if faction ID is empty or whitespace.</exception>
        public FactionEscalation(string factionId, int initialPoints = 0)
        {
            if (factionId == null)
                throw new ArgumentNullException(nameof(factionId));
            if (string.IsNullOrWhiteSpace(factionId))
                throw new ArgumentException("Faction ID cannot be empty or whitespace.", nameof(factionId));

            FactionId = factionId;
            _points = Clamp(initialPoints);
            _previousTier = CurrentTier;
            LastUpdateTime = DateTime.UtcNow;
        }

        /// <summary>
        /// Adds escalation points.
        /// </summary>
        /// <param name="amount">The amount to add (must be non-negative).</param>
        /// <returns>True if the tier changed as a result, false otherwise.</returns>
        /// <exception cref="ArgumentException">Thrown if amount is negative.</exception>
        public bool AddPoints(int amount)
        {
            if (amount < 0)
                throw new ArgumentException("Amount must be non-negative.", nameof(amount));

            var oldTier = CurrentTier;
            _points = Clamp(_points + amount);
            LastUpdateTime = DateTime.UtcNow;

            var newTier = CurrentTier;
            if (oldTier != newTier)
            {
                _previousTier = oldTier;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Removes escalation points.
        /// </summary>
        /// <param name="amount">The amount to remove (must be non-negative).</param>
        /// <returns>True if the tier changed as a result, false otherwise.</returns>
        /// <exception cref="ArgumentException">Thrown if amount is negative.</exception>
        public bool RemovePoints(int amount)
        {
            if (amount < 0)
                throw new ArgumentException("Amount must be non-negative.", nameof(amount));

            var oldTier = CurrentTier;
            _points = Clamp(_points - amount);
            LastUpdateTime = DateTime.UtcNow;

            var newTier = CurrentTier;
            if (oldTier != newTier)
            {
                _previousTier = oldTier;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Sets the escalation points, clamping to valid range.
        /// </summary>
        /// <param name="value">The new points value.</param>
        public void SetPoints(int value)
        {
            var oldTier = CurrentTier;
            _points = Clamp(value);
            LastUpdateTime = DateTime.UtcNow;

            var newTier = CurrentTier;
            if (oldTier != newTier)
            {
                _previousTier = oldTier;
            }
        }

        /// <summary>
        /// Gets the point threshold required for a specific tier.
        /// </summary>
        /// <param name="tier">The tier to get the threshold for.</param>
        /// <returns>The minimum points required for that tier.</returns>
        public static int GetThresholdForTier(EscalationTier tier)
        {
            switch (tier)
            {
                case EscalationTier.Tier1:
                    return 0;
                case EscalationTier.Tier2:
                    return Tier2Threshold;
                case EscalationTier.Tier3:
                    return Tier3Threshold;
                case EscalationTier.Tier4:
                    return Tier4Threshold;
                case EscalationTier.Tier5:
                    return Tier5Threshold;
                default:
                    return 0;
            }
        }

        private static EscalationTier GetTierFromPoints(int points)
        {
            if (points >= Tier5Threshold)
                return EscalationTier.Tier5;
            if (points >= Tier4Threshold)
                return EscalationTier.Tier4;
            if (points >= Tier3Threshold)
                return EscalationTier.Tier3;
            if (points >= Tier2Threshold)
                return EscalationTier.Tier2;
            return EscalationTier.Tier1;
        }

        private static int Clamp(int value)
        {
            if (value < MinPoints) return MinPoints;
            if (value > MaxPoints) return MaxPoints;
            return value;
        }

        #region Equality

        public bool Equals(FactionEscalation? other)
        {
            if (other is null) return false;
            return FactionId == other.FactionId;
        }

        public override bool Equals(object? obj)
        {
            return obj is FactionEscalation escalation && Equals(escalation);
        }

        public override int GetHashCode()
        {
            return FactionId.GetHashCode();
        }

        public static bool operator ==(FactionEscalation? left, FactionEscalation? right)
        {
            if (left is null) return right is null;
            return left.Equals(right);
        }

        public static bool operator !=(FactionEscalation? left, FactionEscalation? right)
        {
            return !(left == right);
        }

        #endregion

        public override string ToString()
        {
            return $"Escalation[{FactionId}]: {_points} pts ({CurrentTier})";
        }
    }
}
