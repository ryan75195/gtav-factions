namespace FactionWars.Tension.Models
{
    /// <summary>
    /// Defines the types of events that can cause tension to escalate between factions.
    /// Each trigger type has a different base tension increase value.
    /// </summary>
    public enum TensionTriggerType
    {
        /// <summary>
        /// A faction member entered another faction's territory without permission.
        /// Base tension increase: 5
        /// </summary>
        BorderIncursion = 0,

        /// <summary>
        /// A faction initiated an attack on another faction's zone.
        /// Base tension increase: 15
        /// </summary>
        ZoneAttack = 1,

        /// <summary>
        /// A faction successfully captured a zone from another faction.
        /// Base tension increase: 25
        /// </summary>
        ZoneCapture = 2,

        /// <summary>
        /// A faction member was killed by another faction.
        /// Base tension increase: 10
        /// </summary>
        MemberKilled = 3,

        /// <summary>
        /// A faction lieutenant or leader was killed.
        /// Base tension increase: 30
        /// </summary>
        LeaderKilled = 4,

        /// <summary>
        /// Resources were stolen or raided from a faction.
        /// Base tension increase: 8
        /// </summary>
        ResourceRaided = 5,

        /// <summary>
        /// An act of sabotage was committed against a faction.
        /// Base tension increase: 12
        /// </summary>
        Sabotage = 6,

        /// <summary>
        /// A faction is amassing forces near another faction's territory.
        /// Base tension increase: 7
        /// </summary>
        TerritoryThreat = 7,

        /// <summary>
        /// Multiple aggressive acts have been committed in a short time period.
        /// Base tension increase: 20
        /// </summary>
        RepeatedAggression = 8,

        /// <summary>
        /// An allied faction was attacked.
        /// Base tension increase: 10
        /// </summary>
        AllyAttacked = 9
    }
}
