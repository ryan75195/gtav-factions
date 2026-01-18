namespace FactionWars.UI.Models
{
    /// <summary>
    /// Contains faction status information for display.
    /// </summary>
    public class FactionStatusInfo
    {
        /// <summary>
        /// The faction's display name.
        /// </summary>
        public string FactionName { get; set; } = string.Empty;

        /// <summary>
        /// The faction leader's name.
        /// </summary>
        public string? LeaderName { get; set; }

        /// <summary>
        /// Current cash resources.
        /// </summary>
        public int Cash { get; set; }

        /// <summary>
        /// Current troop count.
        /// </summary>
        public int TroopCount { get; set; }

        /// <summary>
        /// Number of zones owned.
        /// </summary>
        public int ZoneCount { get; set; }

        /// <summary>
        /// Current weapons stockpile.
        /// </summary>
        public int Weapons { get; set; }

        /// <summary>
        /// Current recruitment points.
        /// </summary>
        public int RecruitmentPoints { get; set; }

        /// <summary>
        /// Calculated military strength.
        /// </summary>
        public int MilitaryStrength { get; set; }
    }
}
