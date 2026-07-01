using FactionWars.Core.Interfaces;
using FactionWars.ScriptHookV.Combat.Interfaces;
using FactionWars.Territory.Interfaces;

namespace FactionWars.ScriptHookV.Models
{
    public sealed class SupportSquadManagerDependencies
    {
        public IGameBridge? GameBridge { get; set; }
        public IZoneCombatantSpawner? Spawner { get; set; }
        public ICombatantStatsProvider? StatsProvider { get; set; }
        public IZoneService? ZoneService { get; set; }
    }
}
