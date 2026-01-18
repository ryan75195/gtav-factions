using FactionWars.Persistence.Models;
using System;

namespace FactionWars.Core.Interfaces
{
    /// <summary>
    /// Interface for the auto-save service that automatically saves game state at intervals.
    /// </summary>
    public interface IAutoSaveService : IDisposable
    {
        /// <summary>
        /// Gets whether the auto-save service is currently running.
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        /// Gets or sets whether auto-save is enabled.
        /// When disabled, the timer still runs but saves are skipped.
        /// </summary>
        bool IsEnabled { get; set; }

        /// <summary>
        /// Gets the current auto-save interval.
        /// </summary>
        TimeSpan Interval { get; }

        /// <summary>
        /// Gets the time since the last auto-save.
        /// </summary>
        TimeSpan TimeSinceLastSave { get; }

        /// <summary>
        /// Gets the time of the last successful auto-save, or null if none has occurred.
        /// </summary>
        DateTime? LastSaveTime { get; }

        /// <summary>
        /// Gets the total number of successful auto-saves since the service started.
        /// </summary>
        int AutoSaveCount { get; }

        /// <summary>
        /// Gets the full path to the auto-save file.
        /// </summary>
        string AutoSaveFilePath { get; }

        /// <summary>
        /// Starts the auto-save service.
        /// </summary>
        void Start();

        /// <summary>
        /// Stops the auto-save service.
        /// </summary>
        void Stop();

        /// <summary>
        /// Updates the auto-save timer. Should be called each game tick.
        /// </summary>
        /// <param name="deltaTime">The time elapsed since the last update.</param>
        void Update(TimeSpan deltaTime);

        /// <summary>
        /// Triggers an immediate auto-save.
        /// </summary>
        void TriggerSave();

        /// <summary>
        /// Checks if an auto-save file exists.
        /// </summary>
        /// <returns>True if an auto-save exists.</returns>
        bool HasAutoSave();

        /// <summary>
        /// Loads the auto-save file.
        /// </summary>
        /// <returns>The loaded game state.</returns>
        /// <exception cref="InvalidOperationException">Thrown when no auto-save exists.</exception>
        GameState LoadAutoSave();

        /// <summary>
        /// Deletes the auto-save file.
        /// </summary>
        void DeleteAutoSave();

        /// <summary>
        /// Sets a new auto-save interval.
        /// </summary>
        /// <param name="interval">The new interval.</param>
        void SetInterval(TimeSpan interval);

        /// <summary>
        /// Resets the auto-save timer to zero.
        /// </summary>
        void ResetTimer();

        /// <summary>
        /// Event raised when an auto-save starts.
        /// </summary>
        event EventHandler<EventArgs>? AutoSaveStarted;

        /// <summary>
        /// Event raised when an auto-save completes.
        /// </summary>
        event EventHandler<AutoSaveCompletedEventArgs>? AutoSaveCompleted;
    }

    /// <summary>
    /// Event arguments for the AutoSaveCompleted event.
    /// </summary>
    public class AutoSaveCompletedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets whether the auto-save was successful.
        /// </summary>
        public bool Success { get; }

        /// <summary>
        /// Gets the error that occurred, if any.
        /// </summary>
        public Exception? Error { get; }

        /// <summary>
        /// Creates a new AutoSaveCompletedEventArgs for a successful save.
        /// </summary>
        public AutoSaveCompletedEventArgs()
        {
            Success = true;
            Error = null;
        }

        /// <summary>
        /// Creates a new AutoSaveCompletedEventArgs for a failed save.
        /// </summary>
        /// <param name="error">The error that occurred.</param>
        public AutoSaveCompletedEventArgs(Exception error)
        {
            Success = false;
            Error = error;
        }
    }
}
