namespace FactionWars.Tension.Models
{
    /// <summary>
    /// Describes the reason for a warfare state transition between factions.
    /// </summary>
    public enum WarfareStateTransitionReason
    {
        /// <summary>
        /// Tension reached the threshold for the next warfare level.
        /// </summary>
        TensionThresholdReached = 0,

        /// <summary>
        /// Tension decayed over time, allowing deescalation.
        /// </summary>
        TensionDecay = 1,

        /// <summary>
        /// A diplomatic action caused the state change.
        /// </summary>
        DiplomaticAction = 2,

        /// <summary>
        /// A major incident forced immediate escalation.
        /// </summary>
        MajorIncident = 3,

        /// <summary>
        /// One faction formally declared war on another.
        /// </summary>
        DeclarationOfWar = 4,

        /// <summary>
        /// Factions agreed to a peace treaty.
        /// </summary>
        PeaceTreaty = 5,

        /// <summary>
        /// Factions agreed to a temporary ceasefire.
        /// </summary>
        Ceasefire = 6,

        /// <summary>
        /// An external event forced the state change.
        /// </summary>
        ForcedByEvent = 7
    }
}
