namespace FactionWars.Combat.Models
{
    /// <summary>
    /// Represents the outcome of processing a completed combat encounter.
    /// </summary>
    public enum CombatResultOutcome
    {
        /// <summary>
        /// The attacking faction successfully captured the zone.
        /// </summary>
        ZoneCaptured,

        /// <summary>
        /// The defending faction successfully defended the zone.
        /// </summary>
        ZoneDefended,

        /// <summary>
        /// The combat ended in a stalemate with no ownership change.
        /// </summary>
        Stalemate,

        /// <summary>
        /// The combat was aborted before completion.
        /// </summary>
        Aborted,

        /// <summary>
        /// The zone specified in the encounter was not found.
        /// </summary>
        ZoneNotFound
    }
}
