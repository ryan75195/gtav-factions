namespace FactionWars.Combat.Models
{
    /// <summary>
    /// Represents the current status of a combat encounter.
    /// </summary>
    public enum CombatStatus
    {
        /// <summary>
        /// Combat is currently active and ongoing.
        /// </summary>
        InProgress,

        /// <summary>
        /// The attacking faction has won and will take control of the zone.
        /// </summary>
        AttackerVictory,

        /// <summary>
        /// The defending faction has successfully held the zone.
        /// </summary>
        DefenderVictory,

        /// <summary>
        /// Combat ended in a stalemate - no clear winner.
        /// </summary>
        Stalemate,

        /// <summary>
        /// Combat was aborted (e.g., player left area, time limit).
        /// </summary>
        Aborted,

        /// <summary>
        /// Player retreated from combat (e.g., player died in contested zone).
        /// Zone ownership remains unchanged.
        /// </summary>
        PlayerRetreat
    }
}
