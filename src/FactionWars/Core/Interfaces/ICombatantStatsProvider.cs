using FactionWars.Core.Models;

namespace FactionWars.Core.Interfaces
{
    /// <summary>Supplies per-category combat stats, backed by config.json.</summary>
    public interface ICombatantStatsProvider
    {
        RoleStats GetRoleStats(CombatantCategory category, DefenderRole role);
        PlayerStats GetPlayerStats();
    }
}
