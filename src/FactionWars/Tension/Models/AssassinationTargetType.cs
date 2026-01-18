namespace FactionWars.Tension.Models
{
    /// <summary>
    /// Defines the types of targets for assassination operations.
    /// Higher value targets are harder to eliminate but have greater impact.
    /// </summary>
    public enum AssassinationTargetType
    {
        /// <summary>
        /// A faction lieutenant - high-ranking commander.
        /// Hardest target with the greatest impact on faction effectiveness.
        /// </summary>
        Lieutenant = 0,

        /// <summary>
        /// A high-value member such as a key advisor or specialist.
        /// Moderate difficulty with significant impact.
        /// </summary>
        HighValueMember = 1,

        /// <summary>
        /// An enforcer - skilled combat personnel.
        /// Easier target with localized impact on combat effectiveness.
        /// </summary>
        Enforcer = 2
    }
}
