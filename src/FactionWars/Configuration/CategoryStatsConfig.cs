namespace FactionWars.Configuration
{
    /// <summary>Full per-role stat table for one combatant category.</summary>
    public class CategoryStatsConfig
    {
        public RoleStatsConfig Grunt { get; set; } = new RoleStatsConfig
        { Health = 200, Armor = 50, Accuracy = 0.25f, Weapon = "WEAPON_PISTOL", DamageMultiplier = 1.0f };

        public RoleStatsConfig Gunner { get; set; } = new RoleStatsConfig
        { Health = 350, Armor = 100, Accuracy = 0.45f, Weapon = "WEAPON_SMG", DamageMultiplier = 1.0f };

        public RoleStatsConfig Rifleman { get; set; } = new RoleStatsConfig
        { Health = 500, Armor = 200, Accuracy = 0.60f, Weapon = "WEAPON_CARBINERIFLE", DamageMultiplier = 1.0f };

        public RoleStatsConfig Rocketeer { get; set; } = new RoleStatsConfig
        { Health = 650, Armor = 200, Accuracy = 0.70f, Weapon = "WEAPON_RPG", DamageMultiplier = 1.0f };

        public RoleStatsConfig Sniper { get; set; } = new RoleStatsConfig
        { Health = 275, Armor = 50, Accuracy = 0.70f, Weapon = "WEAPON_SNIPERRIFLE", DamageMultiplier = 1.0f };
    }
}
