using System;
using FactionWars.Core.Models;

namespace FactionWars.ScriptHookV.Managers
{
    /// <summary>
    /// Event args for when a defender dies.
    /// </summary>
    public class DefenderDiedEventArgs : EventArgs
    {
        public string ZoneId { get; }
        public int PedHandle { get; }
        public DefenderTier Tier { get; }

        public DefenderDiedEventArgs(string zoneId, int pedHandle, DefenderTier tier)
        {
            ZoneId = zoneId;
            PedHandle = pedHandle;
            Tier = tier;
        }
    }
}
