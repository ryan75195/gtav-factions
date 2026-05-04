using System;

namespace FactionWars.AI.Events
{
    public sealed class TroopsRecruitedEventArgs : EventArgs
    {
        public string FactionId { get; }
        public int TroopsRecruited { get; }
        public int Cost { get; }
        public int CashBefore { get; }
        public int CashAfter { get; }

        public TroopsRecruitedEventArgs(string factionId, int troopsRecruited, int cost,
            int cashBefore, int cashAfter)
        {
            FactionId = factionId ?? throw new ArgumentNullException(nameof(factionId));
            TroopsRecruited = troopsRecruited;
            Cost = cost;
            CashBefore = cashBefore;
            CashAfter = cashAfter;
        }
    }
}
