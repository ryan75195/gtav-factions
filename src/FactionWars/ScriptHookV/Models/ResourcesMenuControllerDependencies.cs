using FactionWars.Core.Interfaces;
using FactionWars.Economy.Interfaces;
using FactionWars.Factions.Interfaces;
using FactionWars.Territory.Interfaces;
using FactionWars.UI.Interfaces;

namespace FactionWars.ScriptHookV.Models
{
    public sealed class ResourcesMenuControllerDependencies
    {
        public IMenuProvider? MenuProvider { get; set; }
        public IFactionService? FactionService { get; set; }
        public IZoneService? ZoneService { get; set; }
        public IPlayerContext? PlayerContext { get; set; }
        public IResourceTickService? ResourceTickService { get; set; }
        public IZoneTraitResourceModifier? ResourceModifier { get; set; }
        public ISupplyLineService? SupplyLineService { get; set; }
    }
}
