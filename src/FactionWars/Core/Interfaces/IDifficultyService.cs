using System;
using FactionWars.Core.Models;

namespace FactionWars.Core.Interfaces
{
    /// <summary>
    /// Service for managing game difficulty settings.
    /// Provides access to current difficulty configuration and notifies subscribers when difficulty changes.
    /// </summary>
    public interface IDifficultyService
    {
        /// <summary>
        /// Gets the current difficulty settings.
        /// </summary>
        DifficultySettings Current { get; }

        /// <summary>
        /// Sets the difficulty to the specified level.
        /// If the level is different from the current level, updates the settings and raises the DifficultyChanged event.
        /// If the level is the same as the current level, no action is taken.
        /// </summary>
        /// <param name="level">The difficulty level to set.</param>
        void SetDifficulty(Difficulty level);

        /// <summary>
        /// Event raised when the difficulty level changes.
        /// The event argument contains the new DifficultySettings.
        /// This event is not raised when SetDifficulty is called with the current level.
        /// </summary>
        event EventHandler<DifficultySettings>? DifficultyChanged;
    }
}
