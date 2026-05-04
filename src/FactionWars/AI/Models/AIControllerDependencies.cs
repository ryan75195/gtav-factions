using System.Collections.Generic;
using FactionWars.AI.Interfaces;
using FactionWars.Combat.Interfaces;
using FactionWars.Core.Interfaces;
using FactionWars.Factions.Interfaces;
using FactionWars.Territory.Interfaces;

namespace FactionWars.AI.Models
{
    public sealed class AIControllerDependencies
    {
        public IFactionService? FactionService { get; set; }
        public IZoneService? ZoneService { get; set; }
        public IBattleSimulationService? BattleSimulationService { get; set; }
        public IZoneDefenderAllocationService? AllocationService { get; set; }
        public IGameBridge? GameBridge { get; set; }
        public IDictionary<string, IAIStrategy>? Strategies { get; set; }
        public IZoneBattleManager? ZoneBattleManager { get; set; }
    }
}
