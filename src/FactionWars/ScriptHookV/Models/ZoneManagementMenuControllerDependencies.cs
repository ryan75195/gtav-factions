using FactionWars.Core.Interfaces;
using FactionWars.Economy.Interfaces;
using FactionWars.Factions.Interfaces;
using FactionWars.Territory.Interfaces;
using FactionWars.UI.Interfaces;

namespace FactionWars.ScriptHookV.Models
{
    public sealed class ZoneManagementMenuControllerDependencies
    {
        public IMenuProvider? MenuProvider { get; set; }
        public IFactionService? FactionService { get; set; }
        public IZoneService? ZoneService { get; set; }
        public IPlayerContext? PlayerContext { get; set; }
        public IZoneDefenderAllocationService? AllocationService { get; set; }
        public IDefenderDeploymentService? DeploymentService { get; set; }
    }
}
