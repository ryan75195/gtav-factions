using FactionWars.Territory.Models;
using System;

namespace FactionWars.ScriptHookV.Managers
{
    /// <summary>
    /// Subset of TerritoryManager exposed to consumers that need zone enter/exit
    /// events and the current zone the player is in. Lets dependents subscribe
    /// without taking a hard reference to the concrete TerritoryManager (and lets
    /// unit tests Mock these events).
    /// </summary>
    public interface ITerritoryEvents
    {
        event EventHandler<Zone>? ZoneEntered;
        event EventHandler<Zone>? ZoneExited;

        /// <summary>
        /// The zone the player is currently inside, or null if outside all zones.
        /// </summary>
        Zone? CurrentZone { get; }
    }
}
