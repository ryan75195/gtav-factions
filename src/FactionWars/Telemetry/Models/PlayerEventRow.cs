using System;
using FactionWars.Core.Models;

namespace FactionWars.Telemetry.Models
{
    public sealed class PlayerEventRow
    {
        public DateTime Timestamp { get; }
        public long PlayTimeSeconds { get; }
        public PlayerEventType Type { get; }
        public string? ZoneId { get; }
        public string? TargetFaction { get; }
        public DefenderTier? TargetTier { get; }
        public string Details { get; }

        public PlayerEventRow(DateTime timestamp, long playTimeSeconds, PlayerEventType type,
            string? zoneId, string? targetFaction, DefenderTier? targetTier, string details)
        {
            Timestamp = timestamp;
            PlayTimeSeconds = playTimeSeconds;
            Type = type;
            ZoneId = zoneId;
            TargetFaction = targetFaction;
            TargetTier = targetTier;
            Details = details ?? string.Empty;
        }
    }
}
