using System;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;

namespace FactionWars.Core.Services
{
    /// <summary>
    /// Service for managing game difficulty settings.
    /// Tracks current difficulty and notifies subscribers when it changes.
    /// </summary>
    public sealed class DifficultyService : IDifficultyService
    {
        private DifficultySettings _current;

        /// <summary>
        /// Creates a new DifficultyService with the specified initial difficulty.
        /// </summary>
        /// <param name="initialDifficulty">The initial difficulty level. Defaults to Normal.</param>
        public DifficultyService(Difficulty initialDifficulty = Difficulty.Normal)
        {
            _current = DifficultySettings.FromLevel(initialDifficulty);
        }

        /// <inheritdoc />
        public DifficultySettings Current => _current;

        /// <inheritdoc />
        public event EventHandler<DifficultySettings>? DifficultyChanged;

        /// <inheritdoc />
        public void SetDifficulty(Difficulty level)
        {
            if (_current.Level == level)
            {
                return;
            }

            _current = DifficultySettings.FromLevel(level);
            DifficultyChanged?.Invoke(this, _current);
        }
    }
}
