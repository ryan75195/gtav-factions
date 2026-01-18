using System;

namespace FactionWars.Loyalty.Models
{
    /// <summary>
    /// Tracks the insurgency risk for a zone controlled by a faction.
    /// High risk can lead to uprisings that may flip zone control.
    /// </summary>
    public class InsurgencyRisk : IEquatable<InsurgencyRisk>
    {
        private int _riskLevel;

        private const int MinRisk = 0;
        private const int MaxRisk = 100;

        // Risk level thresholds
        private const int LowThreshold = 25;
        private const int MediumThreshold = 50;
        private const int HighThreshold = 75;
        private const int CriticalThreshold = 90;

        // Uprising chances by level
        private const float NoneUprisingChance = 0.0f;
        private const float LowUprisingChance = 0.05f;
        private const float MediumUprisingChance = 0.15f;
        private const float HighUprisingChance = 0.30f;
        private const float CriticalUprisingChance = 0.50f;

        /// <summary>
        /// The ID of the zone this risk tracking applies to.
        /// </summary>
        public string ZoneId { get; }

        /// <summary>
        /// The ID of the faction currently controlling this zone.
        /// </summary>
        public string ControllingFactionId { get; }

        /// <summary>
        /// The ID of the faction that previously controlled this zone, if any.
        /// Insurgents typically support the previous controlling faction.
        /// </summary>
        public string? PreviousFactionId { get; }

        /// <summary>
        /// Current insurgency risk level (0-100).
        /// </summary>
        public int RiskLevel
        {
            get => _riskLevel;
            private set => _riskLevel = Math.Max(MinRisk, Math.Min(MaxRisk, value));
        }

        /// <summary>
        /// Number of in-game days since the last uprising check.
        /// </summary>
        public int DaysSinceLastCheck { get; private set; }

        /// <summary>
        /// Gets the insurgency level classification based on the current risk value.
        /// </summary>
        public InsurgencyLevel Level
        {
            get
            {
                if (RiskLevel < LowThreshold)
                    return InsurgencyLevel.None;
                if (RiskLevel < MediumThreshold)
                    return InsurgencyLevel.Low;
                if (RiskLevel < HighThreshold)
                    return InsurgencyLevel.Medium;
                if (RiskLevel < CriticalThreshold)
                    return InsurgencyLevel.High;
                return InsurgencyLevel.Critical;
            }
        }

        /// <summary>
        /// Gets the faction ID that insurgents support.
        /// Returns the previous controlling faction if available.
        /// </summary>
        public string? InsurgentFactionId => PreviousFactionId;

        /// <summary>
        /// Gets the probability of an uprising occurring based on current risk level.
        /// </summary>
        public float UprisingChance
        {
            get
            {
                return Level switch
                {
                    InsurgencyLevel.None => NoneUprisingChance,
                    InsurgencyLevel.Low => LowUprisingChance,
                    InsurgencyLevel.Medium => MediumUprisingChance,
                    InsurgencyLevel.High => HighUprisingChance,
                    InsurgencyLevel.Critical => CriticalUprisingChance,
                    _ => NoneUprisingChance
                };
            }
        }

        /// <summary>
        /// Creates a new insurgency risk tracker.
        /// </summary>
        /// <param name="zoneId">The ID of the zone.</param>
        /// <param name="controllingFactionId">The ID of the controlling faction.</param>
        /// <param name="initialRiskLevel">Initial risk level (default 0).</param>
        /// <param name="previousFactionId">Optional previous controlling faction ID.</param>
        public InsurgencyRisk(string zoneId, string controllingFactionId, int initialRiskLevel = 0, string? previousFactionId = null)
        {
            ValidateString(zoneId, nameof(zoneId));
            ValidateString(controllingFactionId, nameof(controllingFactionId));

            ZoneId = zoneId;
            ControllingFactionId = controllingFactionId;
            RiskLevel = initialRiskLevel;
            PreviousFactionId = previousFactionId;
            DaysSinceLastCheck = 0;
        }

        private static void ValidateString(string value, string paramName)
        {
            if (value == null)
                throw new ArgumentNullException(paramName);
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException($"{paramName} cannot be empty or whitespace.", paramName);
        }

        /// <summary>
        /// Adjusts the risk level by the specified amount.
        /// The value is clamped between 0 and 100.
        /// </summary>
        /// <param name="adjustment">Amount to adjust (positive or negative).</param>
        public void AdjustRisk(int adjustment)
        {
            RiskLevel += adjustment;
        }

        /// <summary>
        /// Resets the risk level to zero.
        /// </summary>
        public void ResetRisk()
        {
            RiskLevel = 0;
        }

        /// <summary>
        /// Advances the day counter by one.
        /// </summary>
        public void AdvanceDay()
        {
            DaysSinceLastCheck++;
        }

        /// <summary>
        /// Resets the days since last check counter to zero.
        /// </summary>
        public void ResetDayCounter()
        {
            DaysSinceLastCheck = 0;
        }

        public bool Equals(InsurgencyRisk? other)
        {
            if (other is null) return false;
            return ZoneId == other.ZoneId;
        }

        public override bool Equals(object? obj)
        {
            return obj is InsurgencyRisk risk && Equals(risk);
        }

        public override int GetHashCode()
        {
            return ZoneId.GetHashCode();
        }

        public static bool operator ==(InsurgencyRisk? left, InsurgencyRisk? right)
        {
            if (left is null) return right is null;
            return left.Equals(right);
        }

        public static bool operator !=(InsurgencyRisk? left, InsurgencyRisk? right)
        {
            return !(left == right);
        }

        public override string ToString()
        {
            return $"InsurgencyRisk[{ZoneId}]: {RiskLevel}% ({Level})";
        }
    }
}
