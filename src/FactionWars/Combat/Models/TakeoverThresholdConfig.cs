using System;

namespace FactionWars.Combat.Models
{
    /// <summary>
    /// Configuration for zone takeover thresholds.
    /// Defines when attackers win (by reaching a control percentage) and
    /// when defenders win (when attacker control drops below a threshold).
    /// </summary>
    public class TakeoverThresholdConfig
    {
        private float _attackerVictoryThreshold = 100f;
        private float _defenderVictoryThreshold = 0f;
        private float _minimumHoldTime = 5f;

        /// <summary>
        /// The control percentage the attacker must reach to capture the zone.
        /// Must be between 0 and 100. Default is 100 (attacker must fully control zone).
        /// </summary>
        public float AttackerVictoryThreshold
        {
            get => _attackerVictoryThreshold;
            set
            {
                if (value < 0f || value > 100f)
                    throw new ArgumentOutOfRangeException(nameof(value),
                        "AttackerVictoryThreshold must be between 0 and 100.");
                _attackerVictoryThreshold = value;
            }
        }

        /// <summary>
        /// The control percentage at or below which the attacker loses (defender wins).
        /// Must be between 0 and 100. Default is 0 (attacker must lose all control for defender to win).
        /// </summary>
        public float DefenderVictoryThreshold
        {
            get => _defenderVictoryThreshold;
            set
            {
                if (value < 0f || value > 100f)
                    throw new ArgumentOutOfRangeException(nameof(value),
                        "DefenderVictoryThreshold must be between 0 and 100.");
                _defenderVictoryThreshold = value;
            }
        }

        /// <summary>
        /// The minimum time (in seconds) a faction must hold the threshold before victory is confirmed.
        /// Must be non-negative. Default is 5 seconds.
        /// </summary>
        public float MinimumHoldTime
        {
            get => _minimumHoldTime;
            set
            {
                if (value < 0f)
                    throw new ArgumentOutOfRangeException(nameof(value),
                        "MinimumHoldTime cannot be negative.");
                _minimumHoldTime = value;
            }
        }

        /// <summary>
        /// Creates a copy of this configuration.
        /// </summary>
        /// <returns>A new TakeoverThresholdConfig with the same values.</returns>
        public TakeoverThresholdConfig Clone()
        {
            return new TakeoverThresholdConfig
            {
                AttackerVictoryThreshold = _attackerVictoryThreshold,
                DefenderVictoryThreshold = _defenderVictoryThreshold,
                MinimumHoldTime = _minimumHoldTime
            };
        }
    }
}
