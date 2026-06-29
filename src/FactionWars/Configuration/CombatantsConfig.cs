namespace FactionWars.Configuration
{
    /// <summary>Per-category combatant stats. Defaults reproduce the pre-config hardcoded values.</summary>
    public class CombatantsConfig
    {
        public PlayerStatsConfig Player { get; set; } = new PlayerStatsConfig();
        public CategoryStatsConfig Enemies { get; set; } = new CategoryStatsConfig();
        public CategoryStatsConfig Squad { get; set; } = new CategoryStatsConfig();
        public CategoryStatsConfig Friendlies { get; set; } = new CategoryStatsConfig();
    }
}
