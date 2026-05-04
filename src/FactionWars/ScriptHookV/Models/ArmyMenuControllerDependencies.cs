using FactionWars.Core.Interfaces;
using FactionWars.Economy.Interfaces;
using FactionWars.Factions.Interfaces;
using FactionWars.UI.Interfaces;

namespace FactionWars.ScriptHookV.Models
{
    public sealed class ArmyMenuControllerDependencies
    {
        public IMenuProvider? MenuProvider { get; set; }
        public IFactionService? FactionService { get; set; }
        public ITroopPurchaseService? PurchaseService { get; set; }
        public IFollowerService? FollowerService { get; set; }
        public IDefenderTierService? TierService { get; set; }
        public IPlayerContext? PlayerContext { get; set; }
    }
}
