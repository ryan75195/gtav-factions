using System;
using FactionWars.Core.Models;

namespace FactionWars.Core.Interfaces
{
    /// <summary>
    /// Event args for when troops are allocated to a zone.
    /// </summary>
    public class TroopsAllocatedEventArgs : EventArgs
    {
        public string FactionId { get; }
        public string ZoneId { get; }
        public DefenderTier Tier { get; }
        public int Count { get; }

        public TroopsAllocatedEventArgs(string factionId, string zoneId, DefenderTier tier, int count)
        {
            FactionId = factionId;
            ZoneId = zoneId;
            Tier = tier;
            Count = count;
        }
    }
}
