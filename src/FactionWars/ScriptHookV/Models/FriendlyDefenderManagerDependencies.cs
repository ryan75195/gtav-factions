using FactionWars.Combat.Interfaces;
using FactionWars.Core.Interfaces;
using FactionWars.ScriptHookV.Combat.Interfaces;
using FactionWars.Territory.Interfaces;
using FactionWars.UI.Interfaces;

namespace FactionWars.ScriptHookV.Models
{
    public sealed class FriendlyDefenderManagerDependencies
    {
        public IGameBridge? GameBridge { get; set; }
        public IZoneDefenderAllocationService? AllocationService { get; set; }
        public IPedSpawningService? PedSpawningService { get; set; }
        public IPedDespawnService? PedDespawnService { get; set; }
        public IDefenderRoleService? DefenderRoleService { get; set; }
        public IPedBlipService? PedBlipService { get; set; }
        public IZoneService? ZoneService { get; set; }
        public IZoneCombatantSpawner? Spawner { get; set; }
        public ISniperDeploymentService? SniperDeployment { get; set; }
    }
}
