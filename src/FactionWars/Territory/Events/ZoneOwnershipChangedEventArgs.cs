using System;

namespace FactionWars.Territory.Events
{
    public sealed class ZoneOwnershipChangedEventArgs : EventArgs
    {
        public string ZoneId { get; }
        public string? PreviousOwner { get; }
        public string? NewOwner { get; }

        public ZoneOwnershipChangedEventArgs(string zoneId, string? previousOwner, string? newOwner)
        {
            ZoneId = zoneId ?? throw new ArgumentNullException(nameof(zoneId));
            PreviousOwner = previousOwner;
            NewOwner = newOwner;
        }
    }
}
