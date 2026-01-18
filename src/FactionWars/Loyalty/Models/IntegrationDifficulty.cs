namespace FactionWars.Loyalty.Models
{
    /// <summary>
    /// Represents the difficulty level of integrating a captured zone into a faction.
    /// Higher difficulty means slower integration progress and more resistance.
    /// </summary>
    public enum IntegrationDifficulty
    {
        /// <summary>
        /// Easy integration. High starting loyalty, minimal resistance.
        /// Daily progress: ~8%
        /// </summary>
        Easy = 0,

        /// <summary>
        /// Moderate integration. Neutral starting loyalty.
        /// Daily progress: ~5%
        /// </summary>
        Moderate = 1,

        /// <summary>
        /// Challenging integration. Resistant population.
        /// Daily progress: ~3%
        /// </summary>
        Challenging = 2,

        /// <summary>
        /// Severe integration difficulty. Hostile population.
        /// Daily progress: ~2%
        /// </summary>
        Severe = 3,

        /// <summary>
        /// Extreme integration difficulty. Very hostile with multiple prior transfers.
        /// Daily progress: ~1%
        /// </summary>
        Extreme = 4
    }
}
