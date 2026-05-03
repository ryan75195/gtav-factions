using System;

namespace FactionWars.Telemetry.Models
{
    public sealed class ResourceTickEventRow
    {
        public DateTime Timestamp { get; }
        public long PlayTimeSeconds { get; }
        public string FactionId { get; }
        public int Income { get; }
        public int ZonesContributing { get; }

        public ResourceTickEventRow(DateTime timestamp, long playTimeSeconds, string factionId,
            int income, int zonesContributing)
        {
            Timestamp = timestamp;
            PlayTimeSeconds = playTimeSeconds;
            FactionId = factionId ?? throw new ArgumentNullException(nameof(factionId));
            Income = income;
            ZonesContributing = zonesContributing;
        }
    }
}
