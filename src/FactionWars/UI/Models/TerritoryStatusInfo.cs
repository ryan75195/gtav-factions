using System.Collections.Generic;

namespace FactionWars.UI.Models
{
    /// <summary>
    /// Contains territory status information for display.
    /// </summary>
    public class TerritoryStatusInfo
    {
        /// <summary>
        /// Number of zones owned.
        /// </summary>
        public int ZoneCount { get; set; }

        /// <summary>
        /// List of owned zone IDs.
        /// </summary>
        public IReadOnlyList<string> ZoneIds { get; set; } = new List<string>();
    }
}
