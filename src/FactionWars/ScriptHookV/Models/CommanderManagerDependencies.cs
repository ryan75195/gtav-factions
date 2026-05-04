using FactionWars.Combat.Interfaces;
using FactionWars.Core.Interfaces;
using FactionWars.Territory.Interfaces;
using FactionWars.UI.Interfaces;

namespace FactionWars.ScriptHookV.Models
{
    public sealed class CommanderManagerDependencies
    {
        public IGameBridge? GameBridge { get; set; }
        public IPedSpawningService? PedSpawningService { get; set; }
        public IPedDespawnService? PedDespawnService { get; set; }
        public IPedBlipService? PedBlipService { get; set; }
        public IZoneService? ZoneService { get; set; }
    }
}
