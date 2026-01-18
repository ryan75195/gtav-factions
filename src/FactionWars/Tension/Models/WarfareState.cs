namespace FactionWars.Tension.Models
{
    /// <summary>
    /// Represents the state of warfare between two factions.
    /// States escalate from Peace to TotalWar based on tension and events.
    /// </summary>
    public enum WarfareState
    {
        /// <summary>
        /// No active conflict. Normal operations and interactions.
        /// </summary>
        Peace = 0,

        /// <summary>
        /// Indirect conflict. Spying, sabotage, and economic warfare but no direct combat.
        /// </summary>
        ColdWar = 1,

        /// <summary>
        /// Small-scale fights at territorial borders. Limited combat engagement.
        /// </summary>
        BorderSkirmishes = 2,

        /// <summary>
        /// Full-scale direct conflict. Active warfare with coordinated attacks.
        /// </summary>
        OpenWarfare = 3,

        /// <summary>
        /// All-out warfare with no restrictions. Maximum escalation.
        /// </summary>
        TotalWar = 4
    }
}
