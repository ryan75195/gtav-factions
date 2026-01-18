namespace FactionWars.Tension.Models
{
    /// <summary>
    /// Defines the types of targets that can be sabotaged in a zone.
    /// Each target type has different effects and durations.
    /// </summary>
    public enum SabotageTargetType
    {
        /// <summary>
        /// Sabotage resource production facilities.
        /// Reduces cash/resource generation from the zone.
        /// </summary>
        ResourceProduction = 0,

        /// <summary>
        /// Sabotage defensive installations.
        /// Reduces the zone's defense rating temporarily.
        /// </summary>
        DefenseRating = 1,

        /// <summary>
        /// Sabotage recruitment centers.
        /// Reduces the zone's ability to generate recruitment points.
        /// </summary>
        RecruitmentRate = 2,

        /// <summary>
        /// Sabotage supply lines to/from the zone.
        /// Disrupts resource flow and reinforcement capabilities.
        /// </summary>
        SupplyLine = 3
    }
}
