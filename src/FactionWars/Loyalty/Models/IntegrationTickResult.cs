namespace FactionWars.Loyalty.Models
{
    /// <summary>
    /// Result of processing a daily integration tick for a zone.
    /// </summary>
    public class IntegrationTickResult
    {
        /// <summary>
        /// The ID of the zone that was processed.
        /// </summary>
        public string ZoneId { get; }

        /// <summary>
        /// The amount of progress gained during this tick.
        /// </summary>
        public int ProgressGained { get; }

        /// <summary>
        /// The new total progress after applying the tick.
        /// </summary>
        public int NewProgress { get; }

        /// <summary>
        /// Indicates whether the zone just reached 100% integration.
        /// </summary>
        public bool JustCompleted { get; }

        /// <summary>
        /// Creates a new integration tick result.
        /// </summary>
        /// <param name="zoneId">The zone ID.</param>
        /// <param name="progressGained">Progress gained this tick.</param>
        /// <param name="newProgress">New total progress.</param>
        /// <param name="justCompleted">Whether zone just completed integration.</param>
        public IntegrationTickResult(string zoneId, int progressGained, int newProgress, bool justCompleted)
        {
            ZoneId = zoneId;
            ProgressGained = progressGained;
            NewProgress = newProgress;
            JustCompleted = justCompleted;
        }
    }
}
