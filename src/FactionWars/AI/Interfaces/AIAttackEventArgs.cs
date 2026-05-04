using System;

namespace FactionWars.AI.Interfaces
{
    /// <summary>
    /// Event arguments for AI attack events.
    /// </summary>
    public class AIAttackEventArgs : EventArgs
    {
        public string AttackingFactionId { get; }
        public string TargetZoneId { get; }
        public int TroopsCommitted { get; }

        public AIAttackEventArgs(string attackingFactionId, string targetZoneId, int troopsCommitted)
        {
            AttackingFactionId = attackingFactionId;
            TargetZoneId = targetZoneId;
            TroopsCommitted = troopsCommitted;
        }
    }
}
