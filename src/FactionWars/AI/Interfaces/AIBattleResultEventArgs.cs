using System;

namespace FactionWars.AI.Interfaces
{
    /// <summary>
    /// Event arguments for AI battle resolution events.
    /// </summary>
    public class AIBattleResultEventArgs : EventArgs
    {
        public string AttackingFactionId { get; }
        public string DefendingFactionId { get; }
        public string ZoneId { get; }
        public bool AttackerWon { get; }
        public int AttackerLosses { get; }
        public int DefenderLosses { get; }

        public AIBattleResultEventArgs(
            string attackingFactionId,
            string defendingFactionId,
            string zoneId,
            bool attackerWon,
            int attackerLosses,
            int defenderLosses)
        {
            AttackingFactionId = attackingFactionId;
            DefendingFactionId = defendingFactionId;
            ZoneId = zoneId;
            AttackerWon = attackerWon;
            AttackerLosses = attackerLosses;
            DefenderLosses = defenderLosses;
        }
    }
}
