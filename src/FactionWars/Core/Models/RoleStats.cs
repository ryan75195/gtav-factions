namespace FactionWars.Core.Models
{
    /// <summary>Immutable per-role combat stats resolved for a category.</summary>
    public sealed class RoleStats
    {
        public RoleStats(int health, int armor, float accuracy, string weapon, float damageMultiplier)
        {
            Health = health; Armor = armor; Accuracy = accuracy;
            Weapon = weapon; DamageMultiplier = damageMultiplier;
        }

        public int Health { get; }
        public int Armor { get; }
        public float Accuracy { get; }
        public string Weapon { get; }
        public float DamageMultiplier { get; }
    }
}
