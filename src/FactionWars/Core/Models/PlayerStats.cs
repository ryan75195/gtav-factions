namespace FactionWars.Core.Models
{
    /// <summary>Immutable player combat tunables.</summary>
    public sealed class PlayerStats
    {
        public PlayerStats(int maxHealth, int spawnArmor, float outgoingDamageMultiplier, float incomingDamageMultiplier)
        {
            MaxHealth = maxHealth; SpawnArmor = spawnArmor;
            OutgoingDamageMultiplier = outgoingDamageMultiplier;
            IncomingDamageMultiplier = incomingDamageMultiplier;
        }

        public int MaxHealth { get; }
        public int SpawnArmor { get; }
        public float OutgoingDamageMultiplier { get; }
        public float IncomingDamageMultiplier { get; }
    }
}
