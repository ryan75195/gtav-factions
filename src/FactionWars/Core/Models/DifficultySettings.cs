using System;

namespace FactionWars.Core.Models
{
    /// <summary>
    /// Represents the settings for a difficulty level.
    /// Contains AI income multiplier and tick interval configuration.
    /// Use static presets (Easy, Normal, Hard) or FromLevel() to get instances.
    /// </summary>
    public sealed class DifficultySettings
    {
        #region Static Presets

        /// <summary>
        /// Easy difficulty settings: 0.75x AI income, 7 minute tick intervals.
        /// </summary>
        public static readonly DifficultySettings Easy = new DifficultySettings(
            Difficulty.Easy,
            aiIncomeMultiplier: 0.75f,
            tickIntervalMinutes: 7
        );

        /// <summary>
        /// Normal difficulty settings: 1.0x AI income, 5 minute tick intervals.
        /// </summary>
        public static readonly DifficultySettings Normal = new DifficultySettings(
            Difficulty.Normal,
            aiIncomeMultiplier: 1.0f,
            tickIntervalMinutes: 5
        );

        /// <summary>
        /// Hard difficulty settings: 1.25x AI income, 3 minute tick intervals.
        /// </summary>
        public static readonly DifficultySettings Hard = new DifficultySettings(
            Difficulty.Hard,
            aiIncomeMultiplier: 1.25f,
            tickIntervalMinutes: 3
        );

        #endregion

        #region Properties

        /// <summary>
        /// The difficulty level this settings object represents.
        /// </summary>
        public Difficulty Level { get; }

        /// <summary>
        /// Multiplier applied to AI faction income generation.
        /// Lower values make the game easier, higher values make it harder.
        /// </summary>
        public float AiIncomeMultiplier { get; }

        /// <summary>
        /// Number of minutes between income ticks.
        /// Shorter intervals make the game harder as AI factions gain resources faster.
        /// </summary>
        public int TickIntervalMinutes { get; }

        /// <summary>
        /// Computed tick interval in seconds (TickIntervalMinutes * 60).
        /// </summary>
        public int TickIntervalSeconds => TickIntervalMinutes * 60;

        #endregion

        #region Constructor

        private DifficultySettings(Difficulty level, float aiIncomeMultiplier, int tickIntervalMinutes)
        {
            Level = level;
            AiIncomeMultiplier = aiIncomeMultiplier;
            TickIntervalMinutes = tickIntervalMinutes;
        }

        #endregion

        #region Static Methods

        /// <summary>
        /// Returns the appropriate DifficultySettings preset for the given difficulty level.
        /// </summary>
        /// <param name="level">The difficulty level.</param>
        /// <returns>The corresponding DifficultySettings preset.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if an invalid difficulty level is provided.</exception>
        public static DifficultySettings FromLevel(Difficulty level)
        {
            switch (level)
            {
                case Difficulty.Easy:
                    return Easy;
                case Difficulty.Normal:
                    return Normal;
                case Difficulty.Hard:
                    return Hard;
                default:
                    throw new ArgumentOutOfRangeException(nameof(level), level, "Unknown difficulty level");
            }
        }

        #endregion
    }
}
