using System;
using FactionWars.Combat.Interfaces;
using FactionWars.Core.Interfaces;
using FactionWars.ScriptHookV.Managers;
using FactionWars.Territory.Models;

namespace FactionWars.ScriptHookV.Models
{
    public sealed class DefenderRallyControllerDependencies
    {
        public IGameBridge? Bridge { get; set; }
        public ITerritoryEvents? Territory { get; set; }
        public IFriendlyDefenderQuery? Defenders { get; set; }
        public ICombatActivityQuery? Combat { get; set; }
        public Func<string?>? CurrentPlayerFactionIdAccessor { get; set; }
        public Func<long>? NowMs { get; set; }
    }
}
