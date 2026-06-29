namespace FactionWars.Configuration
{
    /// <summary>Combat stats for one defender role within a category.</summary>
    public class RoleStatsConfig
    {
        public int Health { get; set; }
        public int Armor { get; set; }
        public float Accuracy { get; set; }
        public string Weapon { get; set; } = "WEAPON_PISTOL";
        public float DamageMultiplier { get; set; } = 1.0f;
    }
}
