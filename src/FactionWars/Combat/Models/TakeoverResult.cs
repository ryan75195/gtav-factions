using System;

namespace FactionWars.Combat.Models
{
    /// <summary>
    /// Represents the result of checking zone takeover thresholds.
    /// Contains the current status, control percentages, and winner information.
    /// </summary>
    public class TakeoverResult
    {
        /// <summary>
        /// The current status of the takeover check.
        /// </summary>
        public TakeoverStatus Status { get; }

        /// <summary>
        /// Whether the takeover has completed (either attacker or defender victory).
        /// </summary>
        public bool IsTakeoverComplete => Status != TakeoverStatus.InProgress;

        /// <summary>
        /// The current control percentage held by the attacker.
        /// </summary>
        public float AttackerControlPercentage { get; }

        /// <summary>
        /// The current control percentage held by the defender.
        /// </summary>
        public float DefenderControlPercentage { get; }

        /// <summary>
        /// The faction ID of the winner, or null if still in progress.
        /// </summary>
        public string? WinnerFactionId { get; }

        private TakeoverResult(TakeoverStatus status, float attackerPercent, float defenderPercent, string? winnerFactionId)
        {
            Status = status;
            AttackerControlPercentage = attackerPercent;
            DefenderControlPercentage = defenderPercent;
            WinnerFactionId = winnerFactionId;
        }

        /// <summary>
        /// Creates a result indicating the takeover is still in progress.
        /// </summary>
        /// <param name="attackerPercent">The attacker's current control percentage.</param>
        /// <param name="defenderPercent">The defender's current control percentage.</param>
        /// <returns>A new TakeoverResult with InProgress status.</returns>
        public static TakeoverResult InProgress(float attackerPercent, float defenderPercent)
        {
            return new TakeoverResult(TakeoverStatus.InProgress, attackerPercent, defenderPercent, null);
        }

        /// <summary>
        /// Creates a result indicating the attacker has won.
        /// </summary>
        /// <param name="attackerFactionId">The ID of the winning attacker faction.</param>
        /// <param name="attackerPercent">The attacker's final control percentage.</param>
        /// <param name="defenderPercent">The defender's final control percentage.</param>
        /// <returns>A new TakeoverResult with AttackerVictory status.</returns>
        /// <exception cref="ArgumentNullException">Thrown if attackerFactionId is null.</exception>
        /// <exception cref="ArgumentException">Thrown if attackerFactionId is empty or whitespace.</exception>
        public static TakeoverResult AttackerVictory(string attackerFactionId, float attackerPercent, float defenderPercent)
        {
            if (attackerFactionId == null)
                throw new ArgumentNullException(nameof(attackerFactionId));
            if (string.IsNullOrWhiteSpace(attackerFactionId))
                throw new ArgumentException("Attacker faction ID cannot be empty or whitespace.", nameof(attackerFactionId));

            return new TakeoverResult(TakeoverStatus.AttackerVictory, attackerPercent, defenderPercent, attackerFactionId);
        }

        /// <summary>
        /// Creates a result indicating the defender has won.
        /// </summary>
        /// <param name="defenderFactionId">The ID of the winning defender faction.</param>
        /// <param name="attackerPercent">The attacker's final control percentage.</param>
        /// <param name="defenderPercent">The defender's final control percentage.</param>
        /// <returns>A new TakeoverResult with DefenderVictory status.</returns>
        /// <exception cref="ArgumentNullException">Thrown if defenderFactionId is null.</exception>
        /// <exception cref="ArgumentException">Thrown if defenderFactionId is empty or whitespace.</exception>
        public static TakeoverResult DefenderVictory(string defenderFactionId, float attackerPercent, float defenderPercent)
        {
            if (defenderFactionId == null)
                throw new ArgumentNullException(nameof(defenderFactionId));
            if (string.IsNullOrWhiteSpace(defenderFactionId))
                throw new ArgumentException("Defender faction ID cannot be empty or whitespace.", nameof(defenderFactionId));

            return new TakeoverResult(TakeoverStatus.DefenderVictory, attackerPercent, defenderPercent, defenderFactionId);
        }
    }
}
