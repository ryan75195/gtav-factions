namespace FactionWars.Factions.Models
{
    /// <summary>
    /// Represents the diplomatic status between two factions.
    /// Status is derived from the relationship value.
    /// </summary>
    public enum RelationshipStatus
    {
        /// <summary>
        /// Factions are at war (value below -50). Active combat is expected.
        /// </summary>
        War = 0,

        /// <summary>
        /// Factions are hostile (value -50 to -26). Tensions are high but not yet war.
        /// </summary>
        Hostile = 1,

        /// <summary>
        /// Factions are neutral (value -25 to 25). No special relationship.
        /// </summary>
        Neutral = 2,

        /// <summary>
        /// Factions are friendly (value 26 to 50). Positive relations but not allied.
        /// </summary>
        Friendly = 3,

        /// <summary>
        /// Factions are allied (value above 50). Strong cooperation and mutual defense.
        /// </summary>
        Allied = 4
    }
}
