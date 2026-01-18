using System;

namespace FactionWars.Economy.Interfaces
{
    /// <summary>
    /// Event arguments for when a resource tick occurs.
    /// </summary>
    public class ResourceTickEventArgs : EventArgs
    {
        /// <summary>
        /// The faction ID that received resources.
        /// </summary>
        public string FactionId { get; }

        /// <summary>
        /// Amount of cash generated this tick.
        /// </summary>
        public int CashGenerated { get; }

        /// <summary>
        /// Amount of recruitment points generated this tick.
        /// </summary>
        public int RecruitmentGenerated { get; }

        /// <summary>
        /// Amount of weapons generated this tick.
        /// </summary>
        public int WeaponsGenerated { get; }

        /// <summary>
        /// Creates new resource tick event arguments.
        /// </summary>
        public ResourceTickEventArgs(string factionId, int cash, int recruitment, int weapons)
        {
            FactionId = factionId ?? throw new ArgumentNullException(nameof(factionId));
            CashGenerated = cash;
            RecruitmentGenerated = recruitment;
            WeaponsGenerated = weapons;
        }
    }

    /// <summary>
    /// Service that manages periodic resource generation ticks.
    /// Responsible for tracking time and triggering resource generation for all factions.
    /// </summary>
    public interface IResourceTickService
    {
        /// <summary>
        /// Event raised when a resource tick occurs for a faction.
        /// </summary>
        event EventHandler<ResourceTickEventArgs>? OnResourceTick;

        /// <summary>
        /// Gets the interval between resource ticks in seconds.
        /// </summary>
        int TickIntervalSeconds { get; }

        /// <summary>
        /// Gets the time remaining until the next tick in seconds.
        /// </summary>
        float TimeUntilNextTick { get; }

        /// <summary>
        /// Gets the progress toward the next tick as a percentage (0-100).
        /// </summary>
        float TickProgress { get; }

        /// <summary>
        /// Gets whether the tick service is currently running.
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        /// Starts the resource tick service.
        /// </summary>
        void Start();

        /// <summary>
        /// Stops the resource tick service.
        /// </summary>
        void Stop();

        /// <summary>
        /// Resets the tick timer to zero elapsed time.
        /// </summary>
        void Reset();

        /// <summary>
        /// Updates the tick service with elapsed time.
        /// Should be called each frame/update cycle.
        /// </summary>
        /// <param name="deltaTimeSeconds">The time elapsed since the last update in seconds.</param>
        void Update(float deltaTimeSeconds);

        /// <summary>
        /// Forces an immediate tick for all factions, bypassing the timer.
        /// </summary>
        void ForceTick();

        /// <summary>
        /// Sets the tick interval in seconds.
        /// </summary>
        /// <param name="seconds">The new interval in seconds (must be positive).</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if seconds is not positive.</exception>
        void SetTickInterval(int seconds);
    }
}
