using FactionWars.Combat.Models;

namespace FactionWars.Combat.Interfaces
{
    /// <summary>
    /// Interface for detecting zone takeover thresholds during combat.
    /// Determines when an attacker has successfully captured a zone or
    /// when a defender has successfully repelled an attack.
    /// </summary>
    public interface ITakeoverDetector
    {
        /// <summary>
        /// Checks if a takeover threshold has been reached based on control percentages.
        /// </summary>
        /// <param name="attackerControlPercentage">The attacker's current control percentage (0-100).</param>
        /// <param name="defenderControlPercentage">The defender's current control percentage (0-100).</param>
        /// <param name="attackerFactionId">The ID of the attacking faction.</param>
        /// <param name="defenderFactionId">The ID of the defending faction.</param>
        /// <returns>A TakeoverResult indicating the current status.</returns>
        TakeoverResult CheckTakeover(float attackerControlPercentage, float defenderControlPercentage,
            string attackerFactionId, string defenderFactionId);

        /// <summary>
        /// Checks if a takeover threshold has been reached for a combat encounter.
        /// </summary>
        /// <param name="encounter">The combat encounter to check.</param>
        /// <returns>A TakeoverResult indicating the current status.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if encounter is null.</exception>
        /// <exception cref="System.InvalidOperationException">Thrown if the encounter has already ended.</exception>
        TakeoverResult CheckTakeover(CombatEncounter encounter);

        /// <summary>
        /// Checks if the attacker control percentage meets or exceeds the victory threshold.
        /// </summary>
        /// <param name="attackerControlPercentage">The attacker's current control percentage.</param>
        /// <returns>True if the attacker has reached the victory threshold.</returns>
        bool IsAttackerVictory(float attackerControlPercentage);

        /// <summary>
        /// Checks if the attacker control percentage is at or below the defender victory threshold.
        /// </summary>
        /// <param name="attackerControlPercentage">The attacker's current control percentage.</param>
        /// <returns>True if the defender has achieved victory.</returns>
        bool IsDefenderVictory(float attackerControlPercentage);

        /// <summary>
        /// Gets the attacker's progress toward victory as a percentage (0-100).
        /// </summary>
        /// <param name="attackerControlPercentage">The attacker's current control percentage.</param>
        /// <returns>Progress toward attacker victory (0-100).</returns>
        float GetProgressToAttackerVictory(float attackerControlPercentage);

        /// <summary>
        /// Gets the defender's progress toward victory as a percentage (0-100).
        /// </summary>
        /// <param name="attackerControlPercentage">The attacker's current control percentage.</param>
        /// <returns>Progress toward defender victory (0-100).</returns>
        float GetProgressToDefenderVictory(float attackerControlPercentage);

        /// <summary>
        /// Gets the current threshold configuration.
        /// </summary>
        /// <returns>A copy of the current configuration.</returns>
        TakeoverThresholdConfig GetCurrentConfig();
    }
}
