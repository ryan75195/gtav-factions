using System;
using FactionWars.Core.Models;

namespace FactionWars.Telemetry.Models
{
    public sealed class AllocationEventRow
    {
        public DateTime Timestamp { get; }
        public long PlayTimeSeconds { get; }
        public string FactionId { get; }
        public string ZoneId { get; }
        public DefenderRole Tier { get; }
        public int Count { get; }
        public AllocationSource Source { get; }

        public AllocationEventRow(DateTime timestamp, long playTimeSeconds,
            string factionId, string zoneId, DefenderRole tier, int count, AllocationSource source)
        {
            Timestamp = timestamp;
            PlayTimeSeconds = playTimeSeconds;
            FactionId = factionId ?? throw new ArgumentNullException(nameof(factionId));
            ZoneId = zoneId ?? throw new ArgumentNullException(nameof(zoneId));
            Tier = tier;
            Count = count;
            Source = source;
        }
    }
}
