namespace FactionWars.AI.Models
{
    /// <summary>
    /// Represents the type of response an AI faction takes to player aggression.
    /// Values are ordered by aggressiveness level.
    /// </summary>
    public enum AggressionResponseType
    {
        /// <summary>
        /// No response - aggression level is too low or no resources to respond.
        /// </summary>
        None = 0,

        /// <summary>
        /// Defensive response - reinforce attacked zones and strengthen defenses.
        /// </summary>
        Defensive = 1,

        /// <summary>
        /// Retaliatory response - counter-attack the aggressor's territory.
        /// </summary>
        Retaliation = 2,

        /// <summary>
        /// Escalated response - full-scale war declaration with maximum commitment.
        /// </summary>
        Escalation = 3
    }
}
