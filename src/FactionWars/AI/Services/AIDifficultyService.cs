using System;
using FactionWars.AI.Interfaces;
using FactionWars.AI.Models;

namespace FactionWars.AI.Services
{
    /// <summary>
    /// Service for managing AI difficulty settings and scaling AI behavior.
    /// Provides methods to scale resource generation, troop allocation, aggressiveness,
    /// attack thresholds, and reaction times based on the current difficulty level.
    /// </summary>
    public class AIDifficultyService : IAIDifficultyService
    {
        private AIDifficulty _currentDifficulty;
        private AIDifficultySettings _currentSettings;

        /// <summary>
        /// Minimum reaction delay in milliseconds to prevent instant reactions.
        /// </summary>
        private const int MinimumReactionDelayMs = 50;

        /// <summary>
        /// Gets the current difficulty level.
        /// </summary>
        public AIDifficulty CurrentDifficulty => _currentDifficulty;

        /// <summary>
        /// Creates a new AIDifficultyService with Normal difficulty.
        /// </summary>
        public AIDifficultyService() : this(AIDifficulty.Normal)
        {
        }

        /// <summary>
        /// Creates a new AIDifficultyService with the specified initial difficulty.
        /// </summary>
        /// <param name="initialDifficulty">The initial difficulty level.</param>
        public AIDifficultyService(AIDifficulty initialDifficulty)
        {
            _currentDifficulty = initialDifficulty;
            _currentSettings = AIDifficultySettings.ForDifficulty(initialDifficulty);
        }

        /// <summary>
        /// Sets the difficulty level and updates the internal settings.
        /// </summary>
        /// <param name="difficulty">The new difficulty level.</param>
        public void SetDifficulty(AIDifficulty difficulty)
        {
            _currentDifficulty = difficulty;
            _currentSettings = AIDifficultySettings.ForDifficulty(difficulty);
        }

        /// <summary>
        /// Gets the current difficulty settings.
        /// </summary>
        /// <returns>The settings for the current difficulty level.</returns>
        public AIDifficultySettings GetSettings()
        {
            return _currentSettings;
        }

        /// <summary>
        /// Scales a base resource generation amount by the difficulty multiplier.
        /// </summary>
        /// <param name="baseAmount">The base resource amount before scaling.</param>
        /// <returns>The scaled resource amount (always >= 0).</returns>
        public int ScaleResourceGeneration(int baseAmount)
        {
            if (baseAmount <= 0)
                return 0;

            return Math.Max(0, (int)(baseAmount * _currentSettings.ResourceGenerationMultiplier));
        }

        /// <summary>
        /// Scales a troop allocation by the difficulty multiplier.
        /// </summary>
        /// <param name="baseTroops">The base troop count before scaling.</param>
        /// <param name="minimumTroops">Optional minimum troop count to enforce (default 1).</param>
        /// <returns>The scaled troop count (at least minimumTroops if baseTroops > 0).</returns>
        public int ScaleTroopAllocation(int baseTroops, int minimumTroops = 1)
        {
            if (baseTroops <= 0)
                return 0;

            int scaled = (int)(baseTroops * _currentSettings.TroopAllocationMultiplier);
            return Math.Max(minimumTroops, scaled);
        }

        /// <summary>
        /// Scales the base aggressiveness by applying the difficulty bonus.
        /// </summary>
        /// <param name="baseAggressiveness">The base aggressiveness (0-1).</param>
        /// <returns>The scaled aggressiveness, clamped to 0-1 range.</returns>
        public float ScaleAggressiveness(float baseAggressiveness)
        {
            float scaled = baseAggressiveness + _currentSettings.AggressivenessBonus;
            return Math.Max(0f, Math.Min(1f, scaled));
        }

        /// <summary>
        /// Scales an attack threshold by the difficulty multiplier.
        /// Higher difficulty = lower threshold = AI attacks more readily.
        /// </summary>
        /// <param name="baseThreshold">The base attack threshold.</param>
        /// <returns>The scaled threshold.</returns>
        public float ScaleAttackThreshold(float baseThreshold)
        {
            return baseThreshold * _currentSettings.AttackThresholdMultiplier;
        }

        /// <summary>
        /// Scales a reaction delay by the difficulty multiplier.
        /// Higher difficulty = lower delay = faster AI reactions.
        /// </summary>
        /// <param name="baseDelayMs">The base delay in milliseconds.</param>
        /// <returns>The scaled delay in milliseconds (at least MinimumReactionDelayMs).</returns>
        public int ScaleReactionDelay(int baseDelayMs)
        {
            if (baseDelayMs <= 0)
                return MinimumReactionDelayMs;

            int scaled = (int)(baseDelayMs * _currentSettings.ReactionDelayMultiplier);
            return Math.Max(MinimumReactionDelayMs, scaled);
        }
    }
}
