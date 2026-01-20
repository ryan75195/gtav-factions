namespace FactionWars.Core.Models
{
    /// <summary>
    /// Represents the quality tier of a defender troop.
    /// Different tiers have different costs, health, armor, weapons, and accuracy.
    /// </summary>
    public enum DefenderTier
    {
        /// <summary>
        /// Basic tier defenders - cheapest but least effective.
        /// Cost: $200, Health: 100, Armor: None, Weapon: Pistol, Accuracy: 0.3
        /// </summary>
        Basic = 0,

        /// <summary>
        /// Medium tier defenders - balanced cost and effectiveness.
        /// Cost: $500, Health: 150, Armor: Light, Weapon: SMG, Accuracy: 0.5
        /// </summary>
        Medium = 1,

        /// <summary>
        /// Heavy tier defenders - most expensive but most effective.
        /// Cost: $1000, Health: 200, Armor: Full, Weapon: Carbine, Accuracy: 0.7
        /// </summary>
        Heavy = 2
    }
}
