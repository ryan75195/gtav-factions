using System;

namespace FactionWars.Telemetry.Models
{
    public sealed class ZoneEventRow
    {
        public DateTime Timestamp { get; }
        public long PlayTimeSeconds { get; }
        public ZoneEventType Type { get; }
        public string ZoneId { get; }
        public string? PreviousOwner { get; }
        public string? NewOwner { get; }

        public ZoneEventRow(DateTime timestamp, long playTimeSeconds,
            ZoneEventType type, string zoneId, string? previousOwner, string? newOwner)
        {
            Timestamp = timestamp;
            PlayTimeSeconds = playTimeSeconds;
            Type = type;
            ZoneId = zoneId ?? throw new ArgumentNullException(nameof(zoneId));
            PreviousOwner = previousOwner;
            NewOwner = newOwner;
        }
    }
}
