using System;
using FactionWars.Combat.Interfaces;
using FactionWars.Core.Interfaces;
using FactionWars.ScriptHookV.Combat.Interfaces;
using FactionWars.Territory.Interfaces;
using FactionWars.UI.Interfaces;

namespace FactionWars.ScriptHookV.Models
{
    public sealed class EnemyDefenderManagerDependencies
    {
        public IGameBridge? GameBridge { get; set; }
        public IZoneDefenderAllocationService? AllocationService { get; set; }
        public IPedSpawningService? PedSpawningService { get; set; }
        public IPedDespawnService? PedDespawnService { get; set; }
        public IDefenderRoleService? DefenderRoleService { get; set; }
        public IPedBlipService? PedBlipService { get; set; }
        public IZoneService? ZoneService { get; set; }
        public IZoneBattleManager? ZoneBattleManager { get; set; }
        public IZoneCombatantSpawner? Spawner { get; set; }
        public Func<string?>? CurrentPlayerFactionIdAccessor { get; set; }
        public ISniperDeploymentService? SniperDeployment { get; set; }
        public ICombatantStatsProvider? StatsProvider { get; set; }
    }
}
