using System;

namespace FactionWars.Tension.Models
{
    /// <summary>
    /// Represents the effect of a successful sabotage operation on a zone.
    /// Effects are temporary and reduce zone capabilities for a duration.
    /// </summary>
    public class SabotageEffect
    {
        private readonly DateTime _createdTime;

        /// <summary>
        /// The zone affected by the sabotage.
        /// </summary>
        public string TargetZoneId { get; }

        /// <summary>
        /// The type of sabotage target.
        /// </summary>
        public SabotageTargetType TargetType { get; }

        /// <summary>
        /// The amount of disruption (0.0 to 1.0) applied to the target.
        /// For example, 0.25 means a 25% reduction in effectiveness.
        /// </summary>
        public float DisruptionAmount { get; }

        /// <summary>
        /// The base duration in seconds for this sabotage type.
        /// </summary>
        public int BaseDurationSeconds
        {
            get
            {
                return TargetType switch
                {
                    SabotageTargetType.ResourceProduction => 300,
                    SabotageTargetType.DefenseRating => 180,
                    SabotageTargetType.RecruitmentRate => 240,
                    SabotageTargetType.SupplyLine => 120,
                    _ => 180
                };
            }
        }

        /// <summary>
        /// The effective duration based on disruption amount.
        /// Higher disruption lasts longer.
        /// </summary>
        public int EffectiveDurationSeconds => (int)(BaseDurationSeconds * (1.0f + DisruptionAmount));

        /// <summary>
        /// The base tension increase caused by this sabotage type.
        /// </summary>
        public int BaseTensionIncrease
        {
            get
            {
                return TargetType switch
                {
                    SabotageTargetType.ResourceProduction => 10,
                    SabotageTargetType.DefenseRating => 15,
                    SabotageTargetType.RecruitmentRate => 8,
                    SabotageTargetType.SupplyLine => 12,
                    _ => 10
                };
            }
        }

        /// <summary>
        /// The effective tension increase based on disruption amount.
        /// </summary>
        public int EffectiveTensionIncrease => (int)(BaseTensionIncrease * (1.0f + DisruptionAmount));

        /// <summary>
        /// Whether the sabotage effect is still active.
        /// </summary>
        public bool IsActive => GetRemainingSeconds() > 0;

        /// <summary>
        /// Creates a new sabotage effect.
        /// </summary>
        /// <param name="targetZoneId">The zone being sabotaged.</param>
        /// <param name="targetType">The type of sabotage target.</param>
        /// <param name="disruptionAmount">The disruption amount (0.0 to 1.0).</param>
        /// <exception cref="ArgumentNullException">Thrown if zone ID is null.</exception>
        /// <exception cref="ArgumentException">Thrown if zone ID is empty or disruption is out of range.</exception>
        public SabotageEffect(string targetZoneId, SabotageTargetType targetType, float disruptionAmount)
        {
            if (targetZoneId == null)
                throw new ArgumentNullException(nameof(targetZoneId));
            if (string.IsNullOrWhiteSpace(targetZoneId))
                throw new ArgumentException("Target zone ID cannot be empty or whitespace.", nameof(targetZoneId));
            if (disruptionAmount < 0f || disruptionAmount > 1f)
                throw new ArgumentException("Disruption amount must be between 0.0 and 1.0.", nameof(disruptionAmount));

            TargetZoneId = targetZoneId;
            TargetType = targetType;
            DisruptionAmount = disruptionAmount;
            _createdTime = DateTime.UtcNow;
        }

        /// <summary>
        /// Gets the remaining duration of this effect in seconds.
        /// </summary>
        /// <returns>The remaining seconds, or 0 if expired.</returns>
        public int GetRemainingSeconds()
        {
            var elapsed = (DateTime.UtcNow - _createdTime).TotalSeconds;
            var remaining = EffectiveDurationSeconds - elapsed;
            return remaining > 0 ? (int)remaining : 0;
        }

        public override string ToString()
        {
            return $"Sabotage[{TargetType}] on {TargetZoneId}: {(int)(DisruptionAmount * 100)}% disruption";
        }
    }
}
