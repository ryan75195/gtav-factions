using System;
using FactionWars.AI.Models;

namespace FactionWars.Balance.Models
{
    /// <summary>
    /// Provides predefined balance configurations for different difficulty levels.
    /// Each preset is tuned to provide a different gameplay experience.
    /// </summary>
    public static class BalancePresets
    {
        /// <summary>
        /// Returns all available preset names.
        /// </summary>
        public static string[] GetAllPresetNames()
        {
            return new[] { "Easy", "Normal", "Hard", "Veteran" };
        }

        /// <summary>
        /// Gets the balance configuration for the specified AI difficulty.
        /// </summary>
        /// <param name="difficulty">The difficulty level.</param>
        /// <returns>A new BalanceConfiguration configured for the difficulty.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if difficulty is invalid.</exception>
        public static BalanceConfiguration ForDifficulty(AIDifficulty difficulty)
        {
            return difficulty switch
            {
                AIDifficulty.Easy => Easy(),
                AIDifficulty.Normal => Normal(),
                AIDifficulty.Hard => Hard(),
                AIDifficulty.Veteran => Veteran(),
                _ => throw new ArgumentOutOfRangeException(nameof(difficulty), difficulty, "Unknown difficulty level.")
            };
        }

        /// <summary>
        /// Easy preset: Player-friendly settings with bonuses and reduced AI pressure.
        /// Ideal for learning the game mechanics.
        /// </summary>
        public static BalanceConfiguration Easy()
        {
            return new BalanceConfiguration
            {
                // Economy - Player generates more resources
                BaseCashGeneration = 100,
                BaseRecruitmentGeneration = 10,
                BaseWeaponsGeneration = 5,
                MaxCashStorage = 150000,      // 50% more storage
                MaxRecruitmentStorage = 1500,
                MaxWeaponsStorage = 750,
                ResourceTickIntervalSeconds = 240f, // 4 minutes - faster resource generation

                // Combat - Easier takeovers
                AttackerVictoryThreshold = 85f,  // Need less control to win
                DefenderVictoryThreshold = 0f,
                MinimumHoldTimeSeconds = 3f,     // Shorter hold time
                MaxActivePeds = 25,              // Fewer enemies

                // Reinforcement - AI has longer cooldowns
                ReinforcementCooldownSeconds = 45f, // 50% longer AI cooldown
                MinPedsPerWave = 3,               // Smaller waves
                MaxPedsPerWave = 7,
                MaxActiveWaves = 2,              // Fewer concurrent waves
                ResourceCostPerPed = 80,         // Cheaper reinforcements for player

                // Player Bonuses - Significant advantages
                PlayerResourceMultiplier = 1.5f,  // +50% resources
                PlayerCombatMultiplier = 1.2f,   // +20% combat effectiveness
                PlayerDefenseMultiplier = 1.25f, // +25% defense

                // AI Aggression - Less aggressive AI
                AIDecisionIntervalSeconds = 8f,   // Slower AI decisions
                AIAggressionMultiplier = 0.7f,   // -30% aggression
                AIAttackCooldownSeconds = 60f,   // Longer cooldown between attacks
                AITroopCommitmentMultiplier = 0.8f, // AI commits fewer troops

                PresetName = "Easy"
            };
        }

        /// <summary>
        /// Normal preset: Balanced gameplay with no artificial bonuses.
        /// The intended standard experience.
        /// </summary>
        public static BalanceConfiguration Normal()
        {
            return new BalanceConfiguration
            {
                // Economy - Standard rates
                BaseCashGeneration = 100,
                BaseRecruitmentGeneration = 10,
                BaseWeaponsGeneration = 5,
                MaxCashStorage = 100000,
                MaxRecruitmentStorage = 1000,
                MaxWeaponsStorage = 500,
                ResourceTickIntervalSeconds = 300f, // 5 minutes

                // Combat - Standard thresholds
                AttackerVictoryThreshold = 100f,
                DefenderVictoryThreshold = 0f,
                MinimumHoldTimeSeconds = 5f,
                MaxActivePeds = 30,

                // Reinforcement - Standard settings
                ReinforcementCooldownSeconds = 30f,
                MinPedsPerWave = 5,
                MaxPedsPerWave = 10,
                MaxActiveWaves = 3,
                ResourceCostPerPed = 100,

                // Player Bonuses - None
                PlayerResourceMultiplier = 1.0f,
                PlayerCombatMultiplier = 1.0f,
                PlayerDefenseMultiplier = 1.0f,

                // AI Aggression - Standard settings
                AIDecisionIntervalSeconds = 5f,   // Standard AI decisions
                AIAggressionMultiplier = 1.0f,   // No modifier
                AIAttackCooldownSeconds = 30f,   // Standard cooldown
                AITroopCommitmentMultiplier = 1.0f, // Standard commitment

                PresetName = "Normal"
            };
        }

        /// <summary>
        /// Hard preset: Challenging settings with reduced player advantages.
        /// For experienced players seeking a challenge.
        /// </summary>
        public static BalanceConfiguration Hard()
        {
            return new BalanceConfiguration
            {
                // Economy - Reduced caps, standard generation
                BaseCashGeneration = 100,
                BaseRecruitmentGeneration = 10,
                BaseWeaponsGeneration = 5,
                MaxCashStorage = 75000,       // 25% less storage
                MaxRecruitmentStorage = 750,
                MaxWeaponsStorage = 400,
                ResourceTickIntervalSeconds = 360f, // 6 minutes - slower generation

                // Combat - Harder takeovers
                AttackerVictoryThreshold = 100f,
                DefenderVictoryThreshold = 5f,   // Defender has small buffer
                MinimumHoldTimeSeconds = 8f,     // Longer hold required
                MaxActivePeds = 35,              // More enemies

                // Reinforcement - More aggressive AI
                ReinforcementCooldownSeconds = 25f, // Faster AI reinforcements
                MinPedsPerWave = 6,
                MaxPedsPerWave = 12,              // Larger waves
                MaxActiveWaves = 4,              // More concurrent waves
                ResourceCostPerPed = 120,        // More expensive for player

                // Player Bonuses - Slight penalty
                PlayerResourceMultiplier = 0.9f, // -10% resources
                PlayerCombatMultiplier = 1.0f,
                PlayerDefenseMultiplier = 1.0f,

                // AI Aggression - More aggressive AI
                AIDecisionIntervalSeconds = 4f,   // Faster AI decisions
                AIAggressionMultiplier = 1.2f,   // +20% aggression
                AIAttackCooldownSeconds = 20f,   // Shorter cooldown
                AITroopCommitmentMultiplier = 1.1f, // AI commits more troops

                PresetName = "Hard"
            };
        }

        /// <summary>
        /// Veteran preset: Maximum challenge with significant AI advantages.
        /// For players who have mastered the game.
        /// </summary>
        public static BalanceConfiguration Veteran()
        {
            return new BalanceConfiguration
            {
                // Economy - Severely limited player resources
                BaseCashGeneration = 100,
                BaseRecruitmentGeneration = 10,
                BaseWeaponsGeneration = 5,
                MaxCashStorage = 50000,       // 50% less storage
                MaxRecruitmentStorage = 500,
                MaxWeaponsStorage = 250,
                ResourceTickIntervalSeconds = 420f, // 7 minutes - much slower generation

                // Combat - Very hard takeovers
                AttackerVictoryThreshold = 100f,
                DefenderVictoryThreshold = 10f,  // Larger defender buffer
                MinimumHoldTimeSeconds = 12f,    // Much longer hold required
                MaxActivePeds = 40,              // Maximum enemy pressure

                // Reinforcement - Aggressive AI, expensive for player
                ReinforcementCooldownSeconds = 20f, // Very fast AI reinforcements
                MinPedsPerWave = 8,
                MaxPedsPerWave = 15,              // Very large waves
                MaxActiveWaves = 5,              // Many concurrent waves
                ResourceCostPerPed = 150,        // Very expensive for player

                // Player Bonuses - Significant penalty
                PlayerResourceMultiplier = 0.75f, // -25% resources
                PlayerCombatMultiplier = 0.9f,   // -10% combat effectiveness
                PlayerDefenseMultiplier = 0.9f,  // -10% defense

                // AI Aggression - Very aggressive AI
                AIDecisionIntervalSeconds = 3f,   // Very fast AI decisions
                AIAggressionMultiplier = 1.5f,   // +50% aggression
                AIAttackCooldownSeconds = 15f,   // Very short cooldown
                AITroopCommitmentMultiplier = 1.25f, // AI commits significantly more troops

                PresetName = "Veteran"
            };
        }
    }
}
