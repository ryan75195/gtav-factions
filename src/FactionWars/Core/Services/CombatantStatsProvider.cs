using System;
using FactionWars.Configuration;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;

namespace FactionWars.Core.Services
{
    /// <inheritdoc />
    public sealed class CombatantStatsProvider : ICombatantStatsProvider
    {
        private readonly CombatantsConfig _config;

        public CombatantStatsProvider(CombatantsConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public RoleStats GetRoleStats(CombatantCategory category, DefenderRole role)
        {
            var table = TableFor(category);
            var r = RoleConfigFor(table, role);
            return new RoleStats(r.Health, r.Armor, r.Accuracy, r.Weapon, r.DamageMultiplier);
        }

        public PlayerStats GetPlayerStats()
        {
            var p = _config.Player;
            return new PlayerStats(p.MaxHealth, p.SpawnArmor, p.OutgoingDamageMultiplier, p.IncomingDamageMultiplier);
        }

        private CategoryStatsConfig TableFor(CombatantCategory category)
        {
            switch (category)
            {
                case CombatantCategory.Squad: return _config.Squad;
                case CombatantCategory.Friendlies: return _config.Friendlies;
                case CombatantCategory.Enemies: return _config.Enemies;
                default: throw new ArgumentOutOfRangeException(nameof(category), category, "Player has no per-role stats; use GetPlayerStats().");
            }
        }

        private static RoleStatsConfig RoleConfigFor(CategoryStatsConfig table, DefenderRole role)
        {
            switch (role)
            {
                case DefenderRole.Grunt: return table.Grunt;
                case DefenderRole.Gunner: return table.Gunner;
                case DefenderRole.Rifleman: return table.Rifleman;
                case DefenderRole.Rocketeer: return table.Rocketeer;
                case DefenderRole.Sniper: return table.Sniper;
                default: throw new ArgumentOutOfRangeException(nameof(role), role, null);
            }
        }
    }
}
