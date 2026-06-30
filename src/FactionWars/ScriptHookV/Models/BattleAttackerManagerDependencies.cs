using FactionWars.Combat.Interfaces;
using FactionWars.Core.Interfaces;
using FactionWars.Factions.Interfaces;
using FactionWars.ScriptHookV.Combat.Interfaces;
using FactionWars.Territory.Interfaces;
using FactionWars.UI.Interfaces;

namespace FactionWars.ScriptHookV.Models
{
    public sealed class BattleAttackerManagerDependencies
    {
        public IGameBridge? GameBridge { get; set; }
        public IZoneBattleManager? ZoneBattleManager { get; set; }
        public IPedSpawningService? PedSpawningService { get; set; }
        public IPedDespawnService? PedDespawnService { get; set; }
        public IDefenderRoleService? DefenderRoleService { get; set; }
        public IPedBlipService? PedBlipService { get; set; }
        public IZoneService? ZoneService { get; set; }
        public IFactionService? FactionService { get; set; }
        public IZoneCombatantSpawner? Spawner { get; set; }
        public ICombatantStatsProvider? StatsProvider { get; set; }
    }
}
