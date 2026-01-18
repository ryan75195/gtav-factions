namespace FactionWars.Loyalty.Models
{
    /// <summary>
    /// Represents the loyalty level of a zone's population toward the controlling faction.
    /// Higher levels provide bonuses to resource production and defense.
    /// </summary>
    public enum LoyaltyLevel
    {
        /// <summary>
        /// 0-19% loyalty. Population actively resists the faction.
        /// High risk of insurgency. Resource production at 50%, defense -20%.
        /// </summary>
        Hostile = 0,

        /// <summary>
        /// 20-39% loyalty. Population is resistant to faction control.
        /// Resource production at 70%, defense -10%.
        /// </summary>
        Resistant = 1,

        /// <summary>
        /// 40-59% loyalty. Population is indifferent to faction control.
        /// Normal resource production, no defense bonus.
        /// </summary>
        Neutral = 2,

        /// <summary>
        /// 60-79% loyalty. Population supports the faction.
        /// Resource production at 115%, defense +10%.
        /// </summary>
        Supportive = 3,

        /// <summary>
        /// 80-100% loyalty. Population is fanatically loyal.
        /// Resource production at 130%, defense +25%.
        /// </summary>
        Fanatical = 4
    }
}
