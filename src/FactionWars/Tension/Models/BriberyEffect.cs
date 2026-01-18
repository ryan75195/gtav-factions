using System;

namespace FactionWars.Tension.Models
{
    /// <summary>
    /// Represents the effect of a successful bribery operation.
    /// Effects vary based on the type of target bribed.
    /// </summary>
    public class BriberyEffect
    {
        private const int BaseResourceDiversionCost = 5000;
        private const float MinDiversionRate = 0.05f;
        private const float MaxDiversionRate = 0.25f;

        /// <summary>
        /// The faction that was bribed.
        /// </summary>
        public string TargetFactionId { get; }

        /// <summary>
        /// The type of bribery target.
        /// </summary>
        public BriberyTargetType TargetType { get; }

        /// <summary>
        /// The amount of cash spent on the bribe.
        /// </summary>
        public int BribeAmount { get; }

        /// <summary>
        /// The UTC time when the bribery occurred.
        /// </summary>
        public DateTime Timestamp { get; }

        /// <summary>
        /// The base tension change for this bribery type.
        /// Negative values reduce tension, positive values increase it.
        /// </summary>
        public int BaseTensionChange
        {
            get
            {
                return TargetType switch
                {
                    BriberyTargetType.IntelligenceAsset => -5,
                    BriberyTargetType.ResourceDiversion => 8,
                    BriberyTargetType.DefectorRecruitment => 12,
                    BriberyTargetType.TensionReduction => -15,
                    _ => 0
                };
            }
        }

        /// <summary>
        /// The effective tension change scaled by bribe amount.
        /// </summary>
        public int EffectiveTensionChange
        {
            get
            {
                float multiplier = Math.Min(2.0f, BribeAmount / 10000f);
                return (int)(BaseTensionChange * (1.0f + multiplier * 0.5f));
            }
        }

        /// <summary>
        /// The duration of the bribery effect in seconds.
        /// Returns 0 for permanent or instant effects.
        /// </summary>
        public int EffectDurationSeconds
        {
            get
            {
                return TargetType switch
                {
                    BriberyTargetType.IntelligenceAsset => 600,
                    BriberyTargetType.ResourceDiversion => 300,
                    BriberyTargetType.DefectorRecruitment => 0, // Permanent
                    BriberyTargetType.TensionReduction => 0, // Instant
                    _ => 0
                };
            }
        }

        /// <summary>
        /// Whether this bribery provides intelligence.
        /// </summary>
        public bool ProvidesIntelligence => TargetType == BriberyTargetType.IntelligenceAsset;

        /// <summary>
        /// Whether this bribery diverts resources.
        /// </summary>
        public bool DiversResources => TargetType == BriberyTargetType.ResourceDiversion;

        /// <summary>
        /// Whether this bribery recruits a defector.
        /// </summary>
        public bool RecruitDefector => TargetType == BriberyTargetType.DefectorRecruitment;

        /// <summary>
        /// The rate at which resources are diverted (0.0 to 1.0).
        /// Only applicable for ResourceDiversion type.
        /// </summary>
        public float ResourceDiversionRate
        {
            get
            {
                if (TargetType != BriberyTargetType.ResourceDiversion)
                    return 0f;

                // Scale from MinDiversionRate to MaxDiversionRate based on bribe amount
                float ratio = Math.Min(1.0f, (float)(BribeAmount - 2000) / 18000f);
                return MinDiversionRate + ratio * (MaxDiversionRate - MinDiversionRate);
            }
        }

        /// <summary>
        /// The base detection risk for this bribery type (0.0 to 1.0).
        /// </summary>
        public float BaseDetectionRisk
        {
            get
            {
                return TargetType switch
                {
                    BriberyTargetType.IntelligenceAsset => 0.1f,
                    BriberyTargetType.ResourceDiversion => 0.25f,
                    BriberyTargetType.DefectorRecruitment => 0.4f,
                    BriberyTargetType.TensionReduction => 0.15f,
                    _ => 0.2f
                };
            }
        }

        /// <summary>
        /// Additional tension bonus if the bribery was detected.
        /// </summary>
        public int DetectionTensionBonus => 15;

        /// <summary>
        /// Creates a new bribery effect.
        /// </summary>
        /// <param name="targetFactionId">The faction being bribed.</param>
        /// <param name="targetType">The type of bribery target.</param>
        /// <param name="bribeAmount">The cash amount spent on the bribe.</param>
        /// <exception cref="ArgumentNullException">Thrown if faction ID is null.</exception>
        /// <exception cref="ArgumentException">Thrown if faction ID is empty or bribe amount is invalid.</exception>
        public BriberyEffect(string targetFactionId, BriberyTargetType targetType, int bribeAmount)
        {
            if (targetFactionId == null)
                throw new ArgumentNullException(nameof(targetFactionId));
            if (string.IsNullOrWhiteSpace(targetFactionId))
                throw new ArgumentException("Target faction ID cannot be empty or whitespace.", nameof(targetFactionId));
            if (bribeAmount <= 0)
                throw new ArgumentException("Bribe amount must be positive.", nameof(bribeAmount));

            TargetFactionId = targetFactionId;
            TargetType = targetType;
            BribeAmount = bribeAmount;
            Timestamp = DateTime.UtcNow;
        }

        public override string ToString()
        {
            return $"Bribery[{TargetType}] on {TargetFactionId}: ${BribeAmount}";
        }
    }
}
