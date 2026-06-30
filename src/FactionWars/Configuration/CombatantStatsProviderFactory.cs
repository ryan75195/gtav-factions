using System.Collections.Generic;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.Core.Services;

namespace FactionWars.Configuration
{
    /// <summary>Maps a <see cref="CombatantsConfig"/> into an <see cref="ICombatantStatsProvider"/>.
    /// Lives in the config layer so the Core service stays decoupled from config DTOs.</summary>
    public static class CombatantStatsProviderFactory
    {
        public static ICombatantStatsProvider Create(CombatantsConfig config)
        {
            var tables = new Dictionary<CombatantCategory, IReadOnlyDictionary<DefenderRole, RoleStats>>
            {
                [CombatantCategory.Enemies] = Map(config.Enemies),
                [CombatantCategory.Squad] = Map(config.Squad),
                [CombatantCategory.Friendlies] = Map(config.Friendlies),
            };

            var p = config.Player;
            var player = new PlayerStats(p.MaxHealth, p.SpawnArmor, p.OutgoingDamageMultiplier, p.IncomingDamageMultiplier);
            return new CombatantStatsProvider(tables, player);
        }

        private static IReadOnlyDictionary<DefenderRole, RoleStats> Map(CategoryStatsConfig c)
            => new Dictionary<DefenderRole, RoleStats>
            {
                [DefenderRole.Grunt] = ToStats(c.Grunt),
                [DefenderRole.Gunner] = ToStats(c.Gunner),
                [DefenderRole.Rifleman] = ToStats(c.Rifleman),
                [DefenderRole.Rocketeer] = ToStats(c.Rocketeer),
                [DefenderRole.Sniper] = ToStats(c.Sniper),
            };

        private static RoleStats ToStats(RoleStatsConfig r)
            => new RoleStats(r.Health, r.Armor, r.Accuracy, r.Weapon, r.DamageMultiplier);
    }
}
