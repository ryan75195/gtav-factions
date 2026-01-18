using System;

namespace FactionWars.Persistence.Models
{
    /// <summary>
    /// Contains information about a save slot without loading the full game state.
    /// </summary>
    public class SaveSlotInfo
    {
        /// <summary>
        /// The slot number (0-based).
        /// </summary>
        public int SlotNumber { get; }

        /// <summary>
        /// Whether this slot contains a save file.
        /// </summary>
        public bool IsOccupied { get; private set; }

        /// <summary>
        /// The user-defined name of the save, or null if slot is empty.
        /// </summary>
        public string? SaveName { get; private set; }

        /// <summary>
        /// When the save was last modified, or null if slot is empty.
        /// </summary>
        public DateTime? ModifiedAt { get; private set; }

        /// <summary>
        /// Total play time in seconds, or 0 if slot is empty.
        /// </summary>
        public long TotalPlayTimeSeconds { get; private set; }

        /// <summary>
        /// Creates a new SaveSlotInfo for the specified slot number.
        /// </summary>
        /// <param name="slotNumber">The slot number (0-based).</param>
        public SaveSlotInfo(int slotNumber)
        {
            SlotNumber = slotNumber;
            IsOccupied = false;
            SaveName = null;
            ModifiedAt = null;
            TotalPlayTimeSeconds = 0;
        }

        /// <summary>
        /// Sets this slot as occupied with the given save data.
        /// </summary>
        /// <param name="saveName">The name of the save.</param>
        /// <param name="modifiedAt">When the save was last modified.</param>
        /// <param name="totalPlayTimeSeconds">Total play time in seconds.</param>
        public void SetOccupied(string saveName, DateTime modifiedAt, long totalPlayTimeSeconds)
        {
            IsOccupied = true;
            SaveName = saveName;
            ModifiedAt = modifiedAt;
            TotalPlayTimeSeconds = totalPlayTimeSeconds;
        }

        /// <summary>
        /// Gets the play time formatted as a human-readable string.
        /// </summary>
        public string FormattedPlayTime
        {
            get
            {
                var hours = TotalPlayTimeSeconds / 3600;
                var minutes = (TotalPlayTimeSeconds % 3600) / 60;

                if (hours > 0)
                {
                    return $"{hours}h {minutes}m";
                }

                return $"{minutes}m";
            }
        }
    }
}
