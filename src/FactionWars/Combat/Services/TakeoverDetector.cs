using System;
using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Models;
using FactionWars.ScriptHookV.Logging;

namespace FactionWars.Combat.Services
{
    /// <summary>
    /// Detects zone takeover thresholds during combat.
    /// Determines when an attacker has successfully captured a zone or
    /// when a defender has successfully repelled an attack.
    /// </summary>
    public class TakeoverDetector : ITakeoverDetector
    {
        private readonly TakeoverThresholdConfig _config;

        /// <summary>
        /// Creates a new TakeoverDetector with default configuration.
        /// </summary>
        public TakeoverDetector() : this(new TakeoverThresholdConfig())
        {
        }

        /// <summary>
        /// Creates a new TakeoverDetector with the specified configuration.
        /// </summary>
        /// <param name="config">The threshold configuration to use.</param>
        /// <exception cref="ArgumentNullException">Thrown if config is null.</exception>
        public TakeoverDetector(TakeoverThresholdConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <inheritdoc/>
        public TakeoverResult CheckTakeover(float attackerControlPercentage, float defenderControlPercentage,
            string attackerFactionId, string defenderFactionId)
        {
            FileLogger.Debug($"TakeoverDetector.CheckTakeover: attacker={attackerControlPercentage:F1}%, defender={defenderControlPercentage:F1}%, attackerFaction={attackerFactionId}, defenderFaction={defenderFactionId}");
            FileLogger.Debug($"TakeoverDetector: Thresholds - AttackerVictory={_config.AttackerVictoryThreshold}%, DefenderVictory={_config.DefenderVictoryThreshold}%");

            // Validate percentages
            if (attackerControlPercentage < 0f || attackerControlPercentage > 100f)
                throw new ArgumentOutOfRangeException(nameof(attackerControlPercentage),
                    "Attacker control percentage must be between 0 and 100.");
            if (defenderControlPercentage < 0f || defenderControlPercentage > 100f)
                throw new ArgumentOutOfRangeException(nameof(defenderControlPercentage),
                    "Defender control percentage must be between 0 and 100.");

            // Validate faction IDs
            if (attackerFactionId == null)
                throw new ArgumentNullException(nameof(attackerFactionId));
            if (string.IsNullOrWhiteSpace(attackerFactionId))
                throw new ArgumentException("Attacker faction ID cannot be empty or whitespace.", nameof(attackerFactionId));
            if (defenderFactionId == null)
                throw new ArgumentNullException(nameof(defenderFactionId));
            if (string.IsNullOrWhiteSpace(defenderFactionId))
                throw new ArgumentException("Defender faction ID cannot be empty or whitespace.", nameof(defenderFactionId));

            // Check for attacker victory
            if (IsAttackerVictory(attackerControlPercentage))
            {
                FileLogger.Combat($"TakeoverDetector: ATTACKER VICTORY! {attackerControlPercentage:F1}% >= {_config.AttackerVictoryThreshold}%");
                return TakeoverResult.AttackerVictory(attackerFactionId, attackerControlPercentage, defenderControlPercentage);
            }

            // Check for defender victory
            if (IsDefenderVictory(attackerControlPercentage))
            {
                FileLogger.Combat($"TakeoverDetector: DEFENDER VICTORY! {attackerControlPercentage:F1}% <= {_config.DefenderVictoryThreshold}%");
                return TakeoverResult.DefenderVictory(defenderFactionId, attackerControlPercentage, defenderControlPercentage);
            }

            // Still in progress
            FileLogger.Debug($"TakeoverDetector: Combat in progress - no victory condition met");
            return TakeoverResult.InProgress(attackerControlPercentage, defenderControlPercentage);
        }

        /// <inheritdoc/>
        public TakeoverResult CheckTakeover(CombatEncounter encounter)
        {
            if (encounter == null)
                throw new ArgumentNullException(nameof(encounter));

            if (!encounter.IsActive)
                throw new InvalidOperationException("Cannot check takeover for an ended encounter.");

            return CheckTakeover(
                encounter.AttackerControlPercentage,
                encounter.DefenderControlPercentage,
                encounter.AttackingFactionId,
                encounter.DefendingFactionId);
        }

        /// <inheritdoc/>
        public bool IsAttackerVictory(float attackerControlPercentage)
        {
            return attackerControlPercentage >= _config.AttackerVictoryThreshold;
        }

        /// <inheritdoc/>
        public bool IsDefenderVictory(float attackerControlPercentage)
        {
            return attackerControlPercentage <= _config.DefenderVictoryThreshold;
        }

        /// <inheritdoc/>
        public float GetProgressToAttackerVictory(float attackerControlPercentage)
        {
            // Progress is calculated from 0 to AttackerVictoryThreshold
            // Map attackerControlPercentage to 0-100 scale
            if (_config.AttackerVictoryThreshold <= 0f)
                return 100f;

            float progress = (attackerControlPercentage / _config.AttackerVictoryThreshold) * 100f;
            return Math.Max(0f, Math.Min(100f, progress));
        }

        /// <inheritdoc/>
        public float GetProgressToDefenderVictory(float attackerControlPercentage)
        {
            // Progress is calculated from 100 down to DefenderVictoryThreshold
            // When attacker is at 100%, defender progress is 0%
            // When attacker is at DefenderVictoryThreshold, defender progress is 100%
            float range = 100f - _config.DefenderVictoryThreshold;
            if (range <= 0f)
                return 100f;

            float distanceFromTop = 100f - attackerControlPercentage;
            float progress = (distanceFromTop / range) * 100f;
            return Math.Max(0f, Math.Min(100f, progress));
        }

        /// <inheritdoc/>
        public TakeoverThresholdConfig GetCurrentConfig()
        {
            return _config.Clone();
        }
    }
}
