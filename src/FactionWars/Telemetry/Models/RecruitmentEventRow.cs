using System;

namespace FactionWars.Telemetry.Models
{
    public sealed class RecruitmentEventRow
    {
        public DateTime Timestamp { get; }
        public long PlayTimeSeconds { get; }
        public string FactionId { get; }
        public int TroopsRecruited { get; }
        public int Cost { get; }
        public int CashBefore { get; }
        public int CashAfter { get; }

        public RecruitmentEventRow(DateTime timestamp, long playTimeSeconds, string factionId,
            int troopsRecruited, int cost, int cashBefore, int cashAfter)
        {
            Timestamp = timestamp;
            PlayTimeSeconds = playTimeSeconds;
            FactionId = factionId ?? throw new ArgumentNullException(nameof(factionId));
            TroopsRecruited = troopsRecruited;
            Cost = cost;
            CashBefore = cashBefore;
            CashAfter = cashAfter;
        }
    }
}
