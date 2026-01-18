namespace FactionWars.Loyalty.Models
{
    /// <summary>
    /// Types of modifiers that affect zone population loyalty.
    /// </summary>
    public enum LoyaltyModifierType
    {
        /// <summary>
        /// Daily passive loyalty gain for time under faction control.
        /// Gradual improvement as the population becomes accustomed to new leadership.
        /// </summary>
        TimeBasedGain = 0,

        /// <summary>
        /// Bonus loyalty from winning defensive battles in the zone.
        /// Population gains confidence in the faction's ability to protect them.
        /// </summary>
        CombatVictory = 1,

        /// <summary>
        /// Penalty from losing defensive battles in the zone.
        /// Population loses confidence in the faction's protection capabilities.
        /// </summary>
        CombatDefeat = 2,

        /// <summary>
        /// Bonus from investing resources into the zone (infrastructure, services).
        /// Population appreciates investment in their community.
        /// </summary>
        ResourceInvestment = 3,

        /// <summary>
        /// Penalty from aggressive faction presence or civilian casualties.
        /// Heavy-handed tactics reduce population support.
        /// </summary>
        Oppression = 4,

        /// <summary>
        /// Bonus from propaganda and public relations operations.
        /// Temporary boost to loyalty through media and messaging.
        /// </summary>
        Propaganda = 5,

        /// <summary>
        /// Influence from neighboring zones' loyalty levels.
        /// Loyalty spreads from adjacent territories (positive or negative).
        /// </summary>
        NeighborInfluence = 6,

        /// <summary>
        /// Penalty from recent conquest of the zone.
        /// Population resents new occupiers, effect diminishes over time.
        /// </summary>
        RecentConquest = 7
    }
}
