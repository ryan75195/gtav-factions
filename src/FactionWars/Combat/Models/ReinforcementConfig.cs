using System;

namespace FactionWars.Combat.Models
{
    /// <summary>
    /// Configuration settings for reinforcement mechanics during combat.
    /// Controls timing, wave sizes, and resource costs.
    /// </summary>
    public class ReinforcementConfig
    {
        private float _cooldownSeconds = 30f;
        private int _minPedsPerWave = 5;
        private int _maxPedsPerWave = 10;
        private int _maxActiveWaves = 3;
        private int _resourceCostPerPed = 100;

        /// <summary>
        /// Cooldown time in seconds between reinforcement requests for a faction.
        /// Default: 30 seconds.
        /// </summary>
        public float CooldownSeconds
        {
            get => _cooldownSeconds;
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value), "Cooldown cannot be negative.");
                _cooldownSeconds = value;
            }
        }

        /// <summary>
        /// Minimum number of peds spawned per reinforcement wave.
        /// Default: 5.
        /// </summary>
        public int MinPedsPerWave
        {
            get => _minPedsPerWave;
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException(nameof(value), "Minimum peds per wave must be at least 1.");
                _minPedsPerWave = value;
            }
        }

        /// <summary>
        /// Maximum number of peds spawned per reinforcement wave.
        /// Default: 10.
        /// </summary>
        public int MaxPedsPerWave
        {
            get => _maxPedsPerWave;
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException(nameof(value), "Maximum peds per wave must be at least 1.");
                _maxPedsPerWave = value;
            }
        }

        /// <summary>
        /// Maximum number of active reinforcement waves per faction per encounter.
        /// Default: 3.
        /// </summary>
        public int MaxActiveWaves
        {
            get => _maxActiveWaves;
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException(nameof(value), "Maximum active waves must be at least 1.");
                _maxActiveWaves = value;
            }
        }

        /// <summary>
        /// Whether reinforcements require spending resources.
        /// Default: true.
        /// </summary>
        public bool RequiresResources { get; set; } = true;

        /// <summary>
        /// Resource cost per ped spawned.
        /// Default: 100.
        /// </summary>
        public int ResourceCostPerPed
        {
            get => _resourceCostPerPed;
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value), "Resource cost cannot be negative.");
                _resourceCostPerPed = value;
            }
        }

        /// <summary>
        /// Validates the configuration, ensuring MinPedsPerWave is not greater than MaxPedsPerWave.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if configuration is invalid.</exception>
        public void Validate()
        {
            if (_minPedsPerWave > _maxPedsPerWave)
            {
                throw new InvalidOperationException(
                    $"MinPedsPerWave ({_minPedsPerWave}) cannot be greater than MaxPedsPerWave ({_maxPedsPerWave}).");
            }
        }
    }
}
