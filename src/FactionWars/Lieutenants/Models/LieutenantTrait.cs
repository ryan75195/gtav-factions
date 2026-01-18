namespace FactionWars.Lieutenants.Models
{
    /// <summary>
    /// Represents character traits that a lieutenant can possess.
    /// Traits affect zone bonuses, combat effectiveness, and defection chances.
    /// </summary>
    public enum LieutenantTrait
    {
        /// <summary>
        /// Aggressive - Increases attack power in zone.
        /// </summary>
        Aggressive,

        /// <summary>
        /// Defensive - Increases defense capability in zone.
        /// </summary>
        Defensive,

        /// <summary>
        /// Cunning - Increases effectiveness of covert operations.
        /// </summary>
        Cunning,

        /// <summary>
        /// Charismatic - Increases population loyalty gain in zone.
        /// </summary>
        Charismatic,

        /// <summary>
        /// Resourceful - Increases resource generation in zone.
        /// </summary>
        Resourceful,

        /// <summary>
        /// Ruthless - Increases fear and reduces enemy morale.
        /// </summary>
        Ruthless,

        /// <summary>
        /// Loyal - Less likely to defect, harder to flip.
        /// </summary>
        Loyal,

        /// <summary>
        /// Ambitious - More likely to defect if loyalty is low.
        /// </summary>
        Ambitious,

        /// <summary>
        /// Veteran - Gains experience faster, more effective in combat.
        /// </summary>
        Veteran,

        /// <summary>
        /// Connected - Provides intelligence about enemy movements.
        /// </summary>
        Connected,

        /// <summary>
        /// Intimidating - Reduces chance of zone attacks.
        /// </summary>
        Intimidating,

        /// <summary>
        /// Corrupt - Can be bribed more easily but generates more cash.
        /// </summary>
        Corrupt
    }
}
