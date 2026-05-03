using System;

namespace FactionWars.Telemetry.Models
{
    public sealed class MatchMetaEventRow
    {
        public DateTime Timestamp { get; }
        public long PlayTimeSeconds { get; }
        public MatchMetaEventType Type { get; }
        public string Details { get; }

        public MatchMetaEventRow(DateTime timestamp, long playTimeSeconds,
            MatchMetaEventType type, string details)
        {
            Timestamp = timestamp;
            PlayTimeSeconds = playTimeSeconds;
            Type = type;
            Details = details ?? string.Empty;
        }
    }
}
