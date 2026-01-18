using System;

namespace FactionWars.AI.Models
{
    /// <summary>
    /// Contains all the scaling parameters for a specific difficulty level.
    /// These settings affect how AI factions behave in terms of resource generation,
    /// attack decisions, troop allocation, and reaction speed.
    /// </summary>
    public class AIDifficultySettings
    {
        /// <summary>
        /// The difficulty level these settings are for.
        /// </summary>
        public AIDifficulty Difficulty { get; }

        /// <summary>
        /// Multiplier for AI resource generation.
        /// Values less than 1.0 reduce resources, greater than 1.0 increase them.
        /// </summary>
        public float ResourceGenerationMultiplier { get; }

        /// <summary>
        /// Multiplier for attack decision thresholds.
        /// Higher values make the AI less likely to attack (requires better conditions).
        /// Lower values make the AI more willing to attack.
        /// </summary>
        public float AttackThresholdMultiplier { get; }

        /// <summary>
        /// Multiplier for troop allocation in actions.
        /// Values less than 1.0 mean AI commits fewer troops, greater than 1.0 means more.
        /// </summary>
        public float TroopAllocationMultiplier { get; }

        /// <summary>
        /// Multiplier for reaction delay times.
        /// Higher values make AI react slower, lower values make it react faster.
        /// </summary>
        public float ReactionDelayMultiplier { get; }

        /// <summary>
        /// Bonus (or penalty if negative) applied to base aggressiveness.
        /// Range is typically -0.5 to +0.5.
        /// </summary>
        public float AggressivenessBonus { get; }

        /// <summary>
        /// The display name for this difficulty level.
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// A description of what this difficulty level means for gameplay.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Creates a new AIDifficultySettings with the specified parameters.
        /// </summary>
        /// <param name="difficulty">The difficulty level.</param>
        /// <param name="resourceGenerationMultiplier">Resource generation multiplier (must be positive).</param>
        /// <param name="attackThresholdMultiplier">Attack threshold multiplier (must be positive).</param>
        /// <param name="troopAllocationMultiplier">Troop allocation multiplier (must be positive).</param>
        /// <param name="reactionDelayMultiplier">Reaction delay multiplier (must be positive).</param>
        /// <param name="aggressivenessBonus">Aggressiveness bonus (-0.5 to 0.5).</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if any multiplier is not positive.</exception>
        public AIDifficultySettings(
            AIDifficulty difficulty,
            float resourceGenerationMultiplier,
            float attackThresholdMultiplier,
            float troopAllocationMultiplier,
            float reactionDelayMultiplier,
            float aggressivenessBonus)
        {
            if (resourceGenerationMultiplier <= 0)
                throw new ArgumentOutOfRangeException(nameof(resourceGenerationMultiplier), "Must be positive.");
            if (attackThresholdMultiplier <= 0)
                throw new ArgumentOutOfRangeException(nameof(attackThresholdMultiplier), "Must be positive.");
            if (troopAllocationMultiplier <= 0)
                throw new ArgumentOutOfRangeException(nameof(troopAllocationMultiplier), "Must be positive.");
            if (reactionDelayMultiplier <= 0)
                throw new ArgumentOutOfRangeException(nameof(reactionDelayMultiplier), "Must be positive.");

            Difficulty = difficulty;
            ResourceGenerationMultiplier = resourceGenerationMultiplier;
            AttackThresholdMultiplier = attackThresholdMultiplier;
            TroopAllocationMultiplier = troopAllocationMultiplier;
            ReactionDelayMultiplier = reactionDelayMultiplier;
            AggressivenessBonus = Math.Max(-0.5f, Math.Min(0.5f, aggressivenessBonus));
            DisplayName = GetDisplayName(difficulty);
            Description = GetDescription(difficulty);
        }

        /// <summary>
        /// Creates AIDifficultySettings for the specified difficulty level using preset values.
        /// </summary>
        /// <param name="difficulty">The difficulty level to create settings for.</param>
        /// <returns>Settings configured for the specified difficulty.</returns>
        public static AIDifficultySettings ForDifficulty(AIDifficulty difficulty)
        {
            return difficulty switch
            {
                AIDifficulty.Easy => new AIDifficultySettings(
                    difficulty: AIDifficulty.Easy,
                    resourceGenerationMultiplier: 0.7f,
                    attackThresholdMultiplier: 1.4f,
                    troopAllocationMultiplier: 0.75f,
                    reactionDelayMultiplier: 1.5f,
                    aggressivenessBonus: -0.15f),

                AIDifficulty.Normal => new AIDifficultySettings(
                    difficulty: AIDifficulty.Normal,
                    resourceGenerationMultiplier: 1.0f,
                    attackThresholdMultiplier: 1.0f,
                    troopAllocationMultiplier: 1.0f,
                    reactionDelayMultiplier: 1.0f,
                    aggressivenessBonus: 0f),

                AIDifficulty.Hard => new AIDifficultySettings(
                    difficulty: AIDifficulty.Hard,
                    resourceGenerationMultiplier: 1.25f,
                    attackThresholdMultiplier: 0.8f,
                    troopAllocationMultiplier: 1.2f,
                    reactionDelayMultiplier: 0.75f,
                    aggressivenessBonus: 0.1f),

                AIDifficulty.Veteran => new AIDifficultySettings(
                    difficulty: AIDifficulty.Veteran,
                    resourceGenerationMultiplier: 1.5f,
                    attackThresholdMultiplier: 0.6f,
                    troopAllocationMultiplier: 1.35f,
                    reactionDelayMultiplier: 0.5f,
                    aggressivenessBonus: 0.2f),

                _ => throw new ArgumentOutOfRangeException(nameof(difficulty), "Unknown difficulty level.")
            };
        }

        private static string GetDisplayName(AIDifficulty difficulty)
        {
            return difficulty switch
            {
                AIDifficulty.Easy => "Easy",
                AIDifficulty.Normal => "Normal",
                AIDifficulty.Hard => "Hard",
                AIDifficulty.Veteran => "Veteran",
                _ => "Unknown"
            };
        }

        private static string GetDescription(AIDifficulty difficulty)
        {
            return difficulty switch
            {
                AIDifficulty.Easy => "AI factions are less aggressive and generate fewer resources. Good for learning the game.",
                AIDifficulty.Normal => "Balanced AI behavior with standard resource generation and attack patterns.",
                AIDifficulty.Hard => "AI factions are more aggressive, generate more resources, and react faster.",
                AIDifficulty.Veteran => "Maximum challenge with highly aggressive AI, fast reactions, and optimal strategies.",
                _ => "Unknown difficulty level."
            };
        }
    }
}
