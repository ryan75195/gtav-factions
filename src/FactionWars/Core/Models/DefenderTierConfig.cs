namespace FactionWars.Core.Models
{
    /// <summary>
    /// Configuration data for a defender tier including cost, stats, and equipment.
    /// </summary>
    public class DefenderTierConfig
    {
        /// <summary>
        /// The tier this configuration applies to.
        /// </summary>
        public DefenderTier Tier { get; }

        /// <summary>
        /// The cost in dollars to purchase one defender of this tier.
        /// </summary>
        public int Cost { get; }

        /// <summary>
        /// The health points for defenders of this tier.
        /// </summary>
        public int Health { get; }

        /// <summary>
        /// The armor value for defenders of this tier (0 = none).
        /// </summary>
        public int Armor { get; }

        /// <summary>
        /// The weapon type name for defenders of this tier.
        /// </summary>
        public string Weapon { get; }

        /// <summary>
        /// The accuracy value for defenders of this tier (0.0 to 1.0).
        /// </summary>
        public float Accuracy { get; }

        /// <summary>
        /// The combat strength modifier for battle simulation.
        /// Basic=1.0, Medium=1.5, Heavy=2.0
        /// </summary>
        public float CombatModifier { get; }

        /// <summary>
        /// Whether live peds of this tier can enter ragdoll from impacts.
        /// </summary>
        public bool RagdollEnabled { get; }

        /// <summary>
        /// Creates a new defender tier configuration.
        /// </summary>
        /// <param name="tier">The tier this configuration applies to.</param>
        /// <param name="cost">The cost to purchase one defender.</param>
        /// <param name="health">The health points.</param>
        /// <param name="armor">The armor value.</param>
        /// <param name="weapon">The weapon type name.</param>
        /// <param name="accuracy">The accuracy value (0.0 to 1.0).</param>
        /// <param name="combatModifier">The combat strength modifier.</param>
        /// <param name="ragdollEnabled">Whether live peds of this tier can ragdoll.</param>
        public DefenderTierConfig(
            DefenderTier tier,
            int cost,
            int health,
            int armor,
            string weapon,
            float accuracy,
            float combatModifier,
            bool ragdollEnabled = true)
        {
            Tier = tier;
            Cost = cost;
            Health = health;
            Armor = armor;
            Weapon = weapon;
            Accuracy = accuracy;
            CombatModifier = combatModifier;
            RagdollEnabled = ragdollEnabled;
        }
    }
}
