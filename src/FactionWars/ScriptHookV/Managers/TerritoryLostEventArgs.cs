using System;

namespace FactionWars.ScriptHookV.Managers
{
    /// <summary>
    /// Event args for when a territory is lost (all defenders died).
    /// </summary>
    public class TerritoryLostEventArgs : EventArgs
    {
        public string ZoneId { get; }

        public TerritoryLostEventArgs(string zoneId)
        {
            ZoneId = zoneId;
        }
    }
}
