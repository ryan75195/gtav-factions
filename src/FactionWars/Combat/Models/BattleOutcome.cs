namespace FactionWars.Combat.Models
{
    /// <summary>
    /// Represents the outcome of a battle.
    /// </summary>
    public enum BattleOutcome
    {
        /// <summary>
        /// The attacking faction won (defenders eliminated).
        /// </summary>
        AttackersWon = 0,

        /// <summary>
        /// The defending faction won (attackers eliminated).
        /// </summary>
        DefendersWon = 1,

        /// <summary>
        /// Neither side won decisively (rare - both eliminated).
        /// </summary>
        Draw = 2,

        /// <summary>
        /// Battle was cancelled due to external intervention (e.g., player captured the zone).
        /// </summary>
        Cancelled = 3
    }
}
