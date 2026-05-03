using System;
using FactionWars.AI.Models;

namespace FactionWars.Telemetry.Models
{
    public sealed class DecisionEventRow
    {
        public DateTime Timestamp { get; }
        public long PlayTimeSeconds { get; }
        public string FactionId { get; }
        public AIDecisionType Type { get; }
        public string? TargetZoneId { get; }
        public int Troops { get; }
        public double Priority { get; }
        public bool Executed { get; }

        public DecisionEventRow(DateTime timestamp, long playTimeSeconds, string factionId,
            AIDecisionType type, string? targetZoneId, int troops, double priority, bool executed)
        {
            Timestamp = timestamp;
            PlayTimeSeconds = playTimeSeconds;
            FactionId = factionId ?? throw new ArgumentNullException(nameof(factionId));
            Type = type;
            TargetZoneId = targetZoneId;
            Troops = troops;
            Priority = priority;
            Executed = executed;
        }
    }
}
