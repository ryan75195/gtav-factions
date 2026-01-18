namespace FactionWars.Tension.Models
{
    /// <summary>
    /// Represents the types of diplomatic actions that can be performed between factions.
    /// Each type has different effects on tension, warfare state, and faction relationships.
    /// </summary>
    public enum DiplomaticActionType
    {
        /// <summary>
        /// A temporary agreement to stop fighting. Prevents combat between factions.
        /// </summary>
        Ceasefire = 0,

        /// <summary>
        /// An agreement not to attack each other. Prevents direct aggression.
        /// </summary>
        NonAggressionPact = 1,

        /// <summary>
        /// An agreement to trade resources. Provides economic bonuses to both factions.
        /// </summary>
        TradeAgreement = 2,

        /// <summary>
        /// An agreement to defend each other. Requires support when ally is attacked.
        /// </summary>
        MutualDefense = 3,

        /// <summary>
        /// A full alliance between factions. Strongest bond with combat and economic bonuses.
        /// </summary>
        Alliance = 4,

        /// <summary>
        /// A formal declaration of war. Increases tension and enables open warfare.
        /// </summary>
        DeclarationOfWar = 5,

        /// <summary>
        /// A treaty to end hostilities. Transitions to peace and significantly reduces tension.
        /// </summary>
        PeaceTreaty = 6,

        /// <summary>
        /// Transfer of territory as part of a deal. Instant action with no ongoing effects.
        /// </summary>
        TerritorialConcession = 7
    }
}
