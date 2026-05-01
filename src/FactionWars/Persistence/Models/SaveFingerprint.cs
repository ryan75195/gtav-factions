namespace FactionWars.Persistence.Models
{
    /// <summary>
    /// Stable identification of a GTA V save based on in-game state values that
    /// are preserved by the savegame and restored exactly on load.
    /// </summary>
    public sealed class SaveFingerprint
    {
        /// <summary>Primary key. Monotonically increasing seconds-played, persisted in the save.</summary>
        public long TotalPlayTimeSeconds { get; set; }

        /// <summary>Tiebreaker. Player's GTA money at save time.</summary>
        public int Money { get; set; }

        /// <summary>Tiebreaker. Number of completed story missions.</summary>
        public int CompletedMissionCount { get; set; }

        /// <summary>Tiebreaker. In-game clock as minutes-of-day (HH*60+MM).</summary>
        public int InGameClockMinutes { get; set; }

        /// <summary>True if all four fields are equal.</summary>
        public bool ExactMatch(SaveFingerprint other)
        {
            if (other == null) return false;
            return TotalPlayTimeSeconds == other.TotalPlayTimeSeconds
                && Money == other.Money
                && CompletedMissionCount == other.CompletedMissionCount
                && InGameClockMinutes == other.InGameClockMinutes;
        }

        /// <summary>True if only TotalPlayTimeSeconds matches (used for O(1) lookup).</summary>
        public bool PrimaryMatch(SaveFingerprint other)
        {
            if (other == null) return false;
            return TotalPlayTimeSeconds == other.TotalPlayTimeSeconds;
        }
    }
}
