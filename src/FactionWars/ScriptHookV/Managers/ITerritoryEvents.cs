using FactionWars.Territory.Models;
using System;

namespace FactionWars.ScriptHookV.Managers
{
    /// <summary>
    /// Subset of TerritoryManager exposed to consumers that only need zone enter/exit
    /// events. Lets dependents subscribe without taking a hard reference to the
    /// concrete TerritoryManager (and lets unit tests Mock these events).
    /// </summary>
    public interface ITerritoryEvents
    {
        event EventHandler<Zone>? ZoneEntered;
        event EventHandler<Zone>? ZoneExited;
    }
}
