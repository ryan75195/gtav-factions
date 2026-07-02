using FactionWars.Core.Interfaces;
using FactionWars.Economy.Interfaces;
using FactionWars.ScriptHookV.Managers;
using FactionWars.ScriptHookV.Managers.Interfaces;
using FactionWars.UI.Interfaces;

namespace FactionWars.ScriptHookV.Models
{
    public sealed class SupportCallMenuControllerDependencies
    {
        public IMenuProvider? MenuProvider { get; set; }
        public ISupportPackageService? SupportPackageService { get; set; }
        public ISupportSquadManager? SupportSquadManager { get; set; }
        public ITerritoryEvents? Territory { get; set; }
        public IPlayerContext? PlayerContext { get; set; }
        public IGameBridge? GameBridge { get; set; }
    }
}
