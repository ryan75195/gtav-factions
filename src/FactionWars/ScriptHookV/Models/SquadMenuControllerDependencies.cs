using FactionWars.Core.Interfaces;
using FactionWars.Economy.Interfaces;
using FactionWars.UI.Interfaces;

namespace FactionWars.ScriptHookV.Models
{
    public sealed class SquadMenuControllerDependencies
    {
        public IMenuProvider? MenuProvider { get; set; }
        public ITroopPurchaseService? PurchaseService { get; set; }
        public IFollowerService? FollowerService { get; set; }
        public IPlayerContext? PlayerContext { get; set; }
    }
}
