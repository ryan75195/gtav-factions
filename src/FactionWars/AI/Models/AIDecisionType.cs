namespace FactionWars.AI.Models
{
    /// <summary>
    /// Represents the types of decisions an AI faction can make.
    /// </summary>
    public enum AIDecisionType
    {
        /// <summary>
        /// Attack an enemy or neutral zone to capture it.
        /// </summary>
        Attack,

        /// <summary>
        /// Defend an owned zone that is threatened or contested.
        /// </summary>
        Defend,

        /// <summary>
        /// Send reinforcements to a zone to strengthen its garrison.
        /// </summary>
        Reinforce,

        /// <summary>
        /// Hold current positions without taking action.
        /// </summary>
        Hold,

        /// <summary>
        /// Retreat forces from a zone that cannot be held.
        /// </summary>
        Retreat
    }
}
