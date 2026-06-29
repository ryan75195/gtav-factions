namespace FactionWars.Configuration.Interfaces
{
    /// <summary>Per-category combatant stats configuration contract.</summary>
    public interface ICombatantsConfig
    {
        PlayerStatsConfig Player { get; }
        CategoryStatsConfig Enemies { get; }
        CategoryStatsConfig Squad { get; }
        CategoryStatsConfig Friendlies { get; }
    }
}
