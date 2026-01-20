using System;
using System.Collections.Generic;

namespace FactionWars.Balance.Models
{
    /// <summary>
    /// Central configuration for all balance-related game parameters.
    /// Consolidates economy, combat, reinforcement, and player bonus settings.
    /// </summary>
    public class BalanceConfiguration
    {
        #region Economy Settings

        private int _baseCashGeneration = 100;
        private int _baseRecruitmentGeneration = 10;
        private int _baseWeaponsGeneration = 5;
        private int _maxCashStorage = 100000;
        private int _maxRecruitmentStorage = 1000;
        private int _maxWeaponsStorage = 500;
        private float _resourceTickIntervalSeconds = 300f;

        /// <summary>
        /// Base cash generation per zone per tick.
        /// Default: 100
        /// </summary>
        public int BaseCashGeneration
        {
            get => _baseCashGeneration;
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException(nameof(value), "BaseCashGeneration must be positive.");
                _baseCashGeneration = value;
            }
        }

        /// <summary>
        /// Base recruitment points generation per zone per tick.
        /// Default: 10
        /// </summary>
        public int BaseRecruitmentGeneration
        {
            get => _baseRecruitmentGeneration;
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException(nameof(value), "BaseRecruitmentGeneration must be positive.");
                _baseRecruitmentGeneration = value;
            }
        }

        /// <summary>
        /// Base weapons generation per zone per tick.
        /// Default: 5
        /// </summary>
        public int BaseWeaponsGeneration
        {
            get => _baseWeaponsGeneration;
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException(nameof(value), "BaseWeaponsGeneration must be positive.");
                _baseWeaponsGeneration = value;
            }
        }

        /// <summary>
        /// Maximum cash storage per faction.
        /// Default: 100000
        /// </summary>
        public int MaxCashStorage
        {
            get => _maxCashStorage;
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException(nameof(value), "MaxCashStorage must be positive.");
                _maxCashStorage = value;
            }
        }

        /// <summary>
        /// Maximum recruitment storage per faction.
        /// Default: 1000
        /// </summary>
        public int MaxRecruitmentStorage
        {
            get => _maxRecruitmentStorage;
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException(nameof(value), "MaxRecruitmentStorage must be positive.");
                _maxRecruitmentStorage = value;
            }
        }

        /// <summary>
        /// Maximum weapons storage per faction.
        /// Default: 500
        /// </summary>
        public int MaxWeaponsStorage
        {
            get => _maxWeaponsStorage;
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException(nameof(value), "MaxWeaponsStorage must be positive.");
                _maxWeaponsStorage = value;
            }
        }

        /// <summary>
        /// Interval between resource ticks in seconds.
        /// Default: 300 (5 minutes)
        /// </summary>
        public float ResourceTickIntervalSeconds
        {
            get => _resourceTickIntervalSeconds;
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException(nameof(value), "ResourceTickIntervalSeconds must be positive.");
                _resourceTickIntervalSeconds = value;
            }
        }

        #endregion

        #region Combat Settings

        private float _attackerVictoryThreshold = 100f;
        private float _defenderVictoryThreshold = 0f;
        private float _minimumHoldTimeSeconds = 5f;
        private int _maxActivePeds = 30;

        /// <summary>
        /// Control percentage needed for attacker victory.
        /// Default: 100
        /// </summary>
        public float AttackerVictoryThreshold
        {
            get => _attackerVictoryThreshold;
            set
            {
                if (value < 0f || value > 100f)
                    throw new ArgumentOutOfRangeException(nameof(value), "AttackerVictoryThreshold must be between 0 and 100.");
                _attackerVictoryThreshold = value;
            }
        }

        /// <summary>
        /// Control percentage at or below which defender wins.
        /// Default: 0
        /// </summary>
        public float DefenderVictoryThreshold
        {
            get => _defenderVictoryThreshold;
            set
            {
                if (value < 0f || value > 100f)
                    throw new ArgumentOutOfRangeException(nameof(value), "DefenderVictoryThreshold must be between 0 and 100.");
                _defenderVictoryThreshold = value;
            }
        }

        /// <summary>
        /// Time in seconds to hold threshold before victory.
        /// Default: 5
        /// </summary>
        public float MinimumHoldTimeSeconds
        {
            get => _minimumHoldTimeSeconds;
            set
            {
                if (value < 0f)
                    throw new ArgumentOutOfRangeException(nameof(value), "MinimumHoldTimeSeconds cannot be negative.");
                _minimumHoldTimeSeconds = value;
            }
        }

        /// <summary>
        /// Maximum number of active combat peds.
        /// Default: 30
        /// </summary>
        public int MaxActivePeds
        {
            get => _maxActivePeds;
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException(nameof(value), "MaxActivePeds must be positive.");
                _maxActivePeds = value;
            }
        }

        #endregion

        #region Reinforcement Settings

        private float _reinforcementCooldownSeconds = 30f;
        private int _minPedsPerWave = 5;
        private int _maxPedsPerWave = 10;
        private int _maxActiveWaves = 3;
        private int _resourceCostPerPed = 100;

        /// <summary>
        /// Cooldown between reinforcement waves in seconds.
        /// Default: 30
        /// </summary>
        public float ReinforcementCooldownSeconds
        {
            get => _reinforcementCooldownSeconds;
            set
            {
                if (value < 0f)
                    throw new ArgumentOutOfRangeException(nameof(value), "ReinforcementCooldownSeconds cannot be negative.");
                _reinforcementCooldownSeconds = value;
            }
        }

        /// <summary>
        /// Minimum peds spawned per reinforcement wave.
        /// Default: 5
        /// </summary>
        public int MinPedsPerWave
        {
            get => _minPedsPerWave;
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException(nameof(value), "MinPedsPerWave must be positive.");
                _minPedsPerWave = value;
            }
        }

        /// <summary>
        /// Maximum peds spawned per reinforcement wave.
        /// Default: 10
        /// </summary>
        public int MaxPedsPerWave
        {
            get => _maxPedsPerWave;
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException(nameof(value), "MaxPedsPerWave must be positive.");
                _maxPedsPerWave = value;
            }
        }

        /// <summary>
        /// Maximum concurrent active waves.
        /// Default: 3
        /// </summary>
        public int MaxActiveWaves
        {
            get => _maxActiveWaves;
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException(nameof(value), "MaxActiveWaves must be positive.");
                _maxActiveWaves = value;
            }
        }

        /// <summary>
        /// Resource cost per ped in reinforcements.
        /// Default: 100
        /// </summary>
        public int ResourceCostPerPed
        {
            get => _resourceCostPerPed;
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value), "ResourceCostPerPed cannot be negative.");
                _resourceCostPerPed = value;
            }
        }

        #endregion

        #region AI Aggression Settings

        private float _aiDecisionIntervalSeconds = 5f;
        private float _aiAggressionMultiplier = 1.0f;
        private float _aiAttackCooldownSeconds = 30f;
        private float _aiTroopCommitmentMultiplier = 1.0f;

        /// <summary>
        /// Interval between AI decision cycles in seconds.
        /// Lower values make AI more responsive.
        /// Default: 5
        /// </summary>
        public float AIDecisionIntervalSeconds
        {
            get => _aiDecisionIntervalSeconds;
            set
            {
                if (value <= 0f)
                    throw new ArgumentOutOfRangeException(nameof(value), "AIDecisionIntervalSeconds must be positive.");
                _aiDecisionIntervalSeconds = value;
            }
        }

        /// <summary>
        /// Multiplier applied to AI aggression scores.
        /// Higher values make AI more likely to attack.
        /// Default: 1.0
        /// </summary>
        public float AIAggressionMultiplier
        {
            get => _aiAggressionMultiplier;
            set
            {
                if (value <= 0f)
                    throw new ArgumentOutOfRangeException(nameof(value), "AIAggressionMultiplier must be positive.");
                _aiAggressionMultiplier = value;
            }
        }

        /// <summary>
        /// Cooldown between AI attacks in seconds.
        /// Lower values allow more frequent attacks.
        /// Default: 30
        /// </summary>
        public float AIAttackCooldownSeconds
        {
            get => _aiAttackCooldownSeconds;
            set
            {
                if (value < 0f)
                    throw new ArgumentOutOfRangeException(nameof(value), "AIAttackCooldownSeconds cannot be negative.");
                _aiAttackCooldownSeconds = value;
            }
        }

        /// <summary>
        /// Multiplier for troops AI commits to attacks.
        /// Higher values make AI commit more troops per attack.
        /// Default: 1.0
        /// </summary>
        public float AITroopCommitmentMultiplier
        {
            get => _aiTroopCommitmentMultiplier;
            set
            {
                if (value <= 0f)
                    throw new ArgumentOutOfRangeException(nameof(value), "AITroopCommitmentMultiplier must be positive.");
                _aiTroopCommitmentMultiplier = value;
            }
        }

        #endregion

        #region Player Bonus Settings

        private float _playerResourceMultiplier = 1.0f;
        private float _playerCombatMultiplier = 1.0f;
        private float _playerDefenseMultiplier = 1.0f;

        /// <summary>
        /// Resource generation multiplier for player faction.
        /// Default: 1.0 (no bonus)
        /// </summary>
        public float PlayerResourceMultiplier
        {
            get => _playerResourceMultiplier;
            set
            {
                if (value <= 0f)
                    throw new ArgumentOutOfRangeException(nameof(value), "PlayerResourceMultiplier must be positive.");
                _playerResourceMultiplier = value;
            }
        }

        /// <summary>
        /// Combat effectiveness multiplier for player faction.
        /// Default: 1.0 (no bonus)
        /// </summary>
        public float PlayerCombatMultiplier
        {
            get => _playerCombatMultiplier;
            set
            {
                if (value <= 0f)
                    throw new ArgumentOutOfRangeException(nameof(value), "PlayerCombatMultiplier must be positive.");
                _playerCombatMultiplier = value;
            }
        }

        /// <summary>
        /// Defense multiplier for player faction.
        /// Default: 1.0 (no bonus)
        /// </summary>
        public float PlayerDefenseMultiplier
        {
            get => _playerDefenseMultiplier;
            set
            {
                if (value <= 0f)
                    throw new ArgumentOutOfRangeException(nameof(value), "PlayerDefenseMultiplier must be positive.");
                _playerDefenseMultiplier = value;
            }
        }

        #endregion

        #region Metadata

        /// <summary>
        /// Name of the preset this configuration was based on.
        /// Default: "Default"
        /// </summary>
        public string PresetName { get; set; } = "Default";

        #endregion

        #region Methods

        /// <summary>
        /// Validates the configuration for internal consistency.
        /// </summary>
        /// <returns>Validation result with any errors found.</returns>
        public BalanceValidationResult Validate()
        {
            var errors = new List<string>();

            if (_minPedsPerWave > _maxPedsPerWave)
            {
                errors.Add($"MinPedsPerWave ({_minPedsPerWave}) cannot be greater than MaxPedsPerWave ({_maxPedsPerWave}).");
            }

            if (_defenderVictoryThreshold >= _attackerVictoryThreshold)
            {
                errors.Add($"DefenderVictoryThreshold ({_defenderVictoryThreshold}) must be less than AttackerVictoryThreshold ({_attackerVictoryThreshold}).");
            }

            return new BalanceValidationResult(errors.Count == 0, errors);
        }

        /// <summary>
        /// Creates a deep copy of this configuration.
        /// </summary>
        /// <returns>A new BalanceConfiguration with identical values.</returns>
        public BalanceConfiguration Clone()
        {
            return new BalanceConfiguration
            {
                // Economy
                _baseCashGeneration = _baseCashGeneration,
                _baseRecruitmentGeneration = _baseRecruitmentGeneration,
                _baseWeaponsGeneration = _baseWeaponsGeneration,
                _maxCashStorage = _maxCashStorage,
                _maxRecruitmentStorage = _maxRecruitmentStorage,
                _maxWeaponsStorage = _maxWeaponsStorage,
                _resourceTickIntervalSeconds = _resourceTickIntervalSeconds,

                // Combat
                _attackerVictoryThreshold = _attackerVictoryThreshold,
                _defenderVictoryThreshold = _defenderVictoryThreshold,
                _minimumHoldTimeSeconds = _minimumHoldTimeSeconds,
                _maxActivePeds = _maxActivePeds,

                // Reinforcement
                _reinforcementCooldownSeconds = _reinforcementCooldownSeconds,
                _minPedsPerWave = _minPedsPerWave,
                _maxPedsPerWave = _maxPedsPerWave,
                _maxActiveWaves = _maxActiveWaves,
                _resourceCostPerPed = _resourceCostPerPed,

                // AI Aggression
                _aiDecisionIntervalSeconds = _aiDecisionIntervalSeconds,
                _aiAggressionMultiplier = _aiAggressionMultiplier,
                _aiAttackCooldownSeconds = _aiAttackCooldownSeconds,
                _aiTroopCommitmentMultiplier = _aiTroopCommitmentMultiplier,

                // Player Bonus
                _playerResourceMultiplier = _playerResourceMultiplier,
                _playerCombatMultiplier = _playerCombatMultiplier,
                _playerDefenseMultiplier = _playerDefenseMultiplier,

                // Metadata
                PresetName = PresetName
            };
        }

        #endregion
    }
}
