using FactionWars.Combat.Interfaces;
using FactionWars.Core.Interfaces;
using FactionWars.UI.Interfaces;

namespace FactionWars.ScriptHookV.Models
{
    public sealed class FollowerManagerDependencies
    {
        public IGameBridge? GameBridge { get; set; }
        public IFollowerService? FollowerService { get; set; }
        public IPedSpawningService? PedSpawningService { get; set; }
        public IDefenderRoleService? DefenderRoleService { get; set; }
        public IPedBlipService? PedBlipService { get; set; }
        public IVehicleSeatPriorityService? SeatPriorityService { get; set; }
    }
}
