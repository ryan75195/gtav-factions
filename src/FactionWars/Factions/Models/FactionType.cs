namespace FactionWars.Factions.Models
{
    /// <summary>
    /// Represents the three main faction types in the game, each led by one of the protagonists.
    /// Each faction type has unique characteristics and bonuses that affect gameplay.
    /// </summary>
    public enum FactionType
    {
        /// <summary>
        /// Michael De Santa's faction - calculated, focused on high-value targets and defense.
        /// </summary>
        Michael,

        /// <summary>
        /// Trevor Philips' faction - aggressive, combat-focused with high offense but lower defense.
        /// </summary>
        Trevor,

        /// <summary>
        /// Franklin Clinton's faction - opportunistic, balanced with mobility advantages.
        /// </summary>
        Franklin
    }
}
