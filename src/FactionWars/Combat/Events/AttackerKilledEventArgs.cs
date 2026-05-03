using System;
using FactionWars.Core.Models;

namespace FactionWars.Combat.Events
{
    public sealed class AttackerKilledEventArgs : EventArgs
    {
        public string ZoneId { get; }
        public string FactionId { get; }
        public DefenderTier Tier { get; }
        public int PedHandle { get; }
        public int KillerPedHandle { get; }

        public AttackerKilledEventArgs(string zoneId, string factionId, DefenderTier tier,
            int pedHandle, int killerPedHandle)
        {
            ZoneId = zoneId ?? throw new ArgumentNullException(nameof(zoneId));
            FactionId = factionId ?? throw new ArgumentNullException(nameof(factionId));
            Tier = tier;
            PedHandle = pedHandle;
            KillerPedHandle = killerPedHandle;
        }
    }
}
