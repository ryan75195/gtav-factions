namespace FactionWars.Configuration
{
    /// <summary>Player-specific combat tunables.</summary>
    public class PlayerStatsConfig
    {
        public int MaxHealth { get; set; } = 200;
        public int SpawnArmor { get; set; } = 0;
        public float OutgoingDamageMultiplier { get; set; } = 1.0f;
        public float IncomingDamageMultiplier { get; set; } = 1.0f;
    }
}
