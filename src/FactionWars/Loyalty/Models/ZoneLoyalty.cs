using System;

namespace FactionWars.Loyalty.Models
{
    /// <summary>
    /// Tracks the loyalty of a zone's population toward the controlling faction.
    /// Loyalty affects resource production, defense capabilities, and insurgency risk.
    /// </summary>
    public class ZoneLoyalty : IEquatable<ZoneLoyalty>
    {
        private int _loyaltyValue;

        private const int DefaultInitialLoyalty = 50;
        private const int TransferLoyaltyReset = 30;
        private const int MinLoyalty = 0;
        private const int MaxLoyalty = 100;

        // Loyalty thresholds for levels
        private const int HostileThreshold = 20;
        private const int ResistantThreshold = 40;
        private const int NeutralThreshold = 60;
        private const int SupportiveThreshold = 80;

        // Resource multipliers by loyalty level
        private const float HostileResourceMultiplier = 0.5f;
        private const float ResistantResourceMultiplier = 0.7f;
        private const float NeutralResourceMultiplier = 1.0f;
        private const float SupportiveResourceMultiplier = 1.15f;
        private const float FanaticalResourceMultiplier = 1.3f;

        // Defense bonuses by loyalty level
        private const int HostileDefenseBonus = -20;
        private const int ResistantDefenseBonus = -10;
        private const int NeutralDefenseBonus = 0;
        private const int SupportiveDefenseBonus = 10;
        private const int FanaticalDefenseBonus = 25;

        /// <summary>
        /// The ID of the zone this loyalty tracking applies to.
        /// </summary>
        public string ZoneId { get; }

        /// <summary>
        /// The ID of the faction currently controlling this zone.
        /// </summary>
        public string ControllingFactionId { get; private set; }

        /// <summary>
        /// The ID of the faction that previously controlled this zone, if any.
        /// </summary>
        public string? PreviousFactionId { get; private set; }

        /// <summary>
        /// Current loyalty value (0-100).
        /// </summary>
        public int LoyaltyValue
        {
            get => _loyaltyValue;
            private set => _loyaltyValue = Math.Max(MinLoyalty, Math.Min(MaxLoyalty, value));
        }

        /// <summary>
        /// Number of times control of this zone has been transferred.
        /// </summary>
        public int TransferCount { get; private set; }

        /// <summary>
        /// Number of in-game days the current faction has controlled this zone.
        /// </summary>
        public int DaysUnderControl { get; private set; }

        /// <summary>
        /// Gets the loyalty level classification based on the current loyalty value.
        /// </summary>
        public LoyaltyLevel Level
        {
            get
            {
                if (LoyaltyValue < HostileThreshold)
                    return LoyaltyLevel.Hostile;
                if (LoyaltyValue < ResistantThreshold)
                    return LoyaltyLevel.Resistant;
                if (LoyaltyValue < NeutralThreshold)
                    return LoyaltyLevel.Neutral;
                if (LoyaltyValue < SupportiveThreshold)
                    return LoyaltyLevel.Supportive;
                return LoyaltyLevel.Fanatical;
            }
        }

        /// <summary>
        /// Indicates whether the zone's population is stable (Neutral or higher).
        /// </summary>
        public bool IsStable => Level >= LoyaltyLevel.Neutral;

        /// <summary>
        /// Indicates whether the zone is at risk of insurgency (Hostile level).
        /// </summary>
        public bool IsAtRiskOfInsurgency => Level == LoyaltyLevel.Hostile;

        /// <summary>
        /// Indicates whether the zone is fully loyal (Fanatical level).
        /// </summary>
        public bool IsFullyLoyal => Level == LoyaltyLevel.Fanatical;

        /// <summary>
        /// Gets the resource production multiplier based on current loyalty level.
        /// </summary>
        public float ResourceMultiplier
        {
            get
            {
                return Level switch
                {
                    LoyaltyLevel.Hostile => HostileResourceMultiplier,
                    LoyaltyLevel.Resistant => ResistantResourceMultiplier,
                    LoyaltyLevel.Neutral => NeutralResourceMultiplier,
                    LoyaltyLevel.Supportive => SupportiveResourceMultiplier,
                    LoyaltyLevel.Fanatical => FanaticalResourceMultiplier,
                    _ => NeutralResourceMultiplier
                };
            }
        }

        /// <summary>
        /// Gets the defense bonus percentage based on current loyalty level.
        /// </summary>
        public int DefenseBonus
        {
            get
            {
                return Level switch
                {
                    LoyaltyLevel.Hostile => HostileDefenseBonus,
                    LoyaltyLevel.Resistant => ResistantDefenseBonus,
                    LoyaltyLevel.Neutral => NeutralDefenseBonus,
                    LoyaltyLevel.Supportive => SupportiveDefenseBonus,
                    LoyaltyLevel.Fanatical => FanaticalDefenseBonus,
                    _ => NeutralDefenseBonus
                };
            }
        }

        /// <summary>
        /// Creates a new zone loyalty tracker.
        /// </summary>
        /// <param name="zoneId">The ID of the zone.</param>
        /// <param name="controllingFactionId">The ID of the controlling faction.</param>
        /// <param name="initialLoyalty">Initial loyalty value (default 50).</param>
        /// <param name="previousFactionId">Optional previous controlling faction ID.</param>
        public ZoneLoyalty(string zoneId, string controllingFactionId, int initialLoyalty = DefaultInitialLoyalty, string? previousFactionId = null)
        {
            ValidateString(zoneId, nameof(zoneId));
            ValidateString(controllingFactionId, nameof(controllingFactionId));

            ZoneId = zoneId;
            ControllingFactionId = controllingFactionId;
            LoyaltyValue = initialLoyalty;
            PreviousFactionId = previousFactionId;
            TransferCount = 0;
            DaysUnderControl = 0;
        }

        private static void ValidateString(string value, string paramName)
        {
            if (value == null)
                throw new ArgumentNullException(paramName);
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException($"{paramName} cannot be empty or whitespace.", paramName);
        }

        /// <summary>
        /// Adjusts the loyalty value by the specified amount.
        /// The value is clamped between 0 and 100.
        /// </summary>
        /// <param name="adjustment">Amount to adjust (positive or negative).</param>
        public void AdjustLoyalty(int adjustment)
        {
            LoyaltyValue += adjustment;
        }

        /// <summary>
        /// Sets the loyalty value to a specific amount.
        /// The value is clamped between 0 and 100.
        /// </summary>
        /// <param name="value">The new loyalty value.</param>
        public void SetLoyalty(int value)
        {
            LoyaltyValue = value;
        }

        /// <summary>
        /// Transfers control of this zone to a new faction.
        /// Resets loyalty to the transfer threshold and stores the previous faction.
        /// </summary>
        /// <param name="newFactionId">The ID of the new controlling faction.</param>
        public void TransferControl(string newFactionId)
        {
            ValidateString(newFactionId, nameof(newFactionId));

            if (newFactionId == ControllingFactionId)
                throw new InvalidOperationException("Cannot transfer control to the same faction.");

            PreviousFactionId = ControllingFactionId;
            ControllingFactionId = newFactionId;
            LoyaltyValue = TransferLoyaltyReset;
            TransferCount++;
            DaysUnderControl = 0;
        }

        /// <summary>
        /// Advances the day counter by one.
        /// </summary>
        public void AdvanceDay()
        {
            DaysUnderControl++;
        }

        /// <summary>
        /// Advances the day counter by the specified amount.
        /// </summary>
        /// <param name="days">Number of days to advance.</param>
        public void AdvanceDays(int days)
        {
            if (days > 0)
                DaysUnderControl += days;
        }

        public bool Equals(ZoneLoyalty? other)
        {
            if (other is null) return false;
            return ZoneId == other.ZoneId;
        }

        public override bool Equals(object? obj)
        {
            return obj is ZoneLoyalty loyalty && Equals(loyalty);
        }

        public override int GetHashCode()
        {
            return ZoneId.GetHashCode();
        }

        public static bool operator ==(ZoneLoyalty? left, ZoneLoyalty? right)
        {
            if (left is null) return right is null;
            return left.Equals(right);
        }

        public static bool operator !=(ZoneLoyalty? left, ZoneLoyalty? right)
        {
            return !(left == right);
        }

        public override string ToString()
        {
            return $"ZoneLoyalty[{ZoneId}]: {LoyaltyValue}% ({Level})";
        }
    }
}
