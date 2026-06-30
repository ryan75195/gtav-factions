using System;
using System.Collections.Generic;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;

namespace FactionWars.Core.Services
{
    /// <inheritdoc />
    public sealed class CombatantStatsProvider : ICombatantStatsProvider
    {
        private readonly IReadOnlyDictionary<CombatantCategory, IReadOnlyDictionary<DefenderRole, RoleStats>> _roleTables;
        private readonly PlayerStats _playerStats;

        public CombatantStatsProvider(
            IReadOnlyDictionary<CombatantCategory, IReadOnlyDictionary<DefenderRole, RoleStats>> roleTables,
            PlayerStats playerStats)
        {
            _roleTables = roleTables ?? throw new ArgumentNullException(nameof(roleTables));
            _playerStats = playerStats ?? throw new ArgumentNullException(nameof(playerStats));
        }

        public RoleStats GetRoleStats(CombatantCategory category, DefenderRole role)
        {
            if (!_roleTables.TryGetValue(category, out var table))
            {
                throw new ArgumentOutOfRangeException(nameof(category), category,
                    "No per-role stats for this category; Player uses GetPlayerStats().");
            }

            if (!table.TryGetValue(role, out var stats))
            {
                throw new ArgumentOutOfRangeException(nameof(role), role, null);
            }

            return stats;
        }

        public PlayerStats GetPlayerStats() => _playerStats;
    }
}
