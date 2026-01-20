namespace FactionWars.UI.Models
{
    /// <summary>
    /// Categories for event feed entries.
    /// </summary>
    public enum EventFeedCategory
    {
        /// <summary>
        /// General events that don't fit other categories.
        /// </summary>
        General = 0,

        /// <summary>
        /// A zone was captured by a faction.
        /// </summary>
        ZoneCaptured = 1,

        /// <summary>
        /// A zone was lost to an enemy faction.
        /// </summary>
        ZoneLost = 2,

        /// <summary>
        /// Combat has started in a zone.
        /// </summary>
        CombatStarted = 3,

        /// <summary>
        /// Combat has ended in a zone.
        /// </summary>
        CombatEnded = 4,

        /// <summary>
        /// Troops were recruited.
        /// </summary>
        TroopsRecruited = 5,

        /// <summary>
        /// Troops were deployed to a zone.
        /// </summary>
        TroopsDeployed = 6,

        /// <summary>
        /// Income was received.
        /// </summary>
        IncomeReceived = 7
    }
}
