namespace FactionWars.UI.Models
{
    /// <summary>
    /// Types of game events that trigger alerts.
    /// </summary>
    public enum EventAlertType
    {
        /// <summary>
        /// A zone has been captured by the player's faction.
        /// </summary>
        ZoneCaptured = 0,

        /// <summary>
        /// A zone has been lost to an enemy faction.
        /// </summary>
        ZoneLost = 1,

        /// <summary>
        /// An enemy faction is attacking one of the player's zones.
        /// </summary>
        AttackIncoming = 2,

        /// <summary>
        /// The player's faction has launched an attack on an enemy zone.
        /// </summary>
        AttackLaunched = 3,

        /// <summary>
        /// Reinforcements are arriving at a zone.
        /// </summary>
        ReinforcementsArriving = 4,

        /// <summary>
        /// A zone is being contested by multiple factions.
        /// </summary>
        ZoneContested = 5,

        /// <summary>
        /// The player's faction is close to winning.
        /// </summary>
        VictoryImminent = 6,

        /// <summary>
        /// The player's faction is close to losing.
        /// </summary>
        DefeatImminent = 7
    }
}
