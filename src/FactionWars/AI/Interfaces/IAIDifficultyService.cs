using FactionWars.AI.Models;

namespace FactionWars.AI.Interfaces
{
    /// <summary>
    /// Service interface for managing AI difficulty settings.
    /// Provides methods to get/set difficulty and scale various AI parameters.
    /// </summary>
    public interface IAIDifficultyService
    {
        /// <summary>
        /// Gets the current difficulty level.
        /// </summary>
        AIDifficulty CurrentDifficulty { get; }

        /// <summary>
        /// Sets the difficulty level.
        /// </summary>
        /// <param name="difficulty">The new difficulty level to set.</param>
        void SetDifficulty(AIDifficulty difficulty);

        /// <summary>
        /// Gets the current difficulty settings.
        /// </summary>
        /// <returns>The settings for the current difficulty level.</returns>
        AIDifficultySettings GetSettings();

        /// <summary>
        /// Scales a base resource generation amount by the difficulty multiplier.
        /// </summary>
        /// <param name="baseAmount">The base resource amount before scaling.</param>
        /// <returns>The scaled resource amount.</returns>
        int ScaleResourceGeneration(int baseAmount);

        /// <summary>
        /// Scales a troop allocation by the difficulty multiplier.
        /// </summary>
        /// <param name="baseTroops">The base troop count before scaling.</param>
        /// <param name="minimumTroops">Optional minimum troop count to enforce.</param>
        /// <returns>The scaled troop count.</returns>
        int ScaleTroopAllocation(int baseTroops, int minimumTroops = 1);

        /// <summary>
        /// Scales the base aggressiveness by applying the difficulty bonus.
        /// </summary>
        /// <param name="baseAggressiveness">The base aggressiveness (0-1).</param>
        /// <returns>The scaled aggressiveness, clamped to 0-1 range.</returns>
        float ScaleAggressiveness(float baseAggressiveness);

        /// <summary>
        /// Scales an attack threshold by the difficulty multiplier.
        /// </summary>
        /// <param name="baseThreshold">The base attack threshold.</param>
        /// <returns>The scaled threshold.</returns>
        float ScaleAttackThreshold(float baseThreshold);

        /// <summary>
        /// Scales a reaction delay by the difficulty multiplier.
        /// </summary>
        /// <param name="baseDelayMs">The base delay in milliseconds.</param>
        /// <returns>The scaled delay in milliseconds.</returns>
        int ScaleReactionDelay(int baseDelayMs);
    }
}
