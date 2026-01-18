namespace FactionWars.Lieutenants.Models
{
    /// <summary>
    /// Contains all zone bonuses provided by a lieutenant.
    /// All values are multipliers where 1.0 = no change.
    /// </summary>
    public class ZoneEffectsSummary
    {
        /// <summary>
        /// Multiplier for attack power in the zone.
        /// </summary>
        public float AttackBonus { get; set; } = 1.0f;

        /// <summary>
        /// Multiplier for defense capability in the zone.
        /// </summary>
        public float DefenseBonus { get; set; } = 1.0f;

        /// <summary>
        /// Multiplier for resource generation in the zone.
        /// </summary>
        public float ResourceBonus { get; set; } = 1.0f;

        /// <summary>
        /// Multiplier for population loyalty gain in the zone.
        /// </summary>
        public float LoyaltyBonus { get; set; } = 1.0f;

        /// <summary>
        /// Multiplier for intelligence gathering in the zone.
        /// </summary>
        public float IntelligenceBonus { get; set; } = 1.0f;

        /// <summary>
        /// Multiplier for covert operations effectiveness.
        /// </summary>
        public float CovertOpsBonus { get; set; } = 1.0f;

        /// <summary>
        /// Multiplier for deterring enemy attacks (reduces attack chance).
        /// </summary>
        public float AttackDeterrenceBonus { get; set; } = 1.0f;

        /// <summary>
        /// Multiplier for experience gain for the lieutenant.
        /// </summary>
        public float ExperienceGainBonus { get; set; } = 1.0f;

        /// <summary>
        /// Creates a new summary with all baseline values (1.0).
        /// </summary>
        public static ZoneEffectsSummary Baseline()
        {
            return new ZoneEffectsSummary();
        }
    }
}
