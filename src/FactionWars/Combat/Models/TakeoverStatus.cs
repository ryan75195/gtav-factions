namespace FactionWars.Combat.Models
{
    /// <summary>
    /// Represents the status of a zone takeover during combat.
    /// </summary>
    public enum TakeoverStatus
    {
        /// <summary>
        /// The takeover is still in progress - no threshold has been met.
        /// </summary>
        InProgress,

        /// <summary>
        /// The attacker has reached the victory threshold and captured the zone.
        /// </summary>
        AttackerVictory,

        /// <summary>
        /// The defender has successfully repelled the attack.
        /// </summary>
        DefenderVictory
    }
}
