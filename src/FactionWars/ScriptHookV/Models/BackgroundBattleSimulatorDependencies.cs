using FactionWars.Core.Interfaces;
using FactionWars.Factions.Interfaces;
using FactionWars.Territory.Interfaces;
using FactionWars.UI.Interfaces;

namespace FactionWars.ScriptHookV.Models
{
    public sealed class BackgroundBattleSimulatorDependencies
    {
        public IBattleSimulationService? BattleSimulationService { get; set; }
        public IFactionService? FactionService { get; set; }
        public IZoneService? ZoneService { get; set; }
        public IZoneDefenderAllocationService? AllocationService { get; set; }
        public IEventAlertService? EventAlertService { get; set; }
        public IEventFeedService? EventFeedService { get; set; }
    }
}
