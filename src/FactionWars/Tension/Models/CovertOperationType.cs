namespace FactionWars.Tension.Models
{
    /// <summary>
    /// Defines the types of covert operations that can be conducted against enemy factions.
    /// Each type has different costs, success rates, and effects.
    /// </summary>
    public enum CovertOperationType
    {
        /// <summary>
        /// Sabotage operations target zone infrastructure to disrupt resource production,
        /// defense ratings, or supply lines. Moderate cost, high success rate.
        /// </summary>
        Sabotage = 0,

        /// <summary>
        /// Assassination operations target key personnel to reduce faction effectiveness
        /// and morale. High cost, low success rate, high tension impact.
        /// </summary>
        Assassination = 1,

        /// <summary>
        /// Bribery operations corrupt enemy faction members to gain intelligence,
        /// divert resources, or recruit defectors. Variable cost based on target.
        /// </summary>
        Bribery = 2
    }
}
