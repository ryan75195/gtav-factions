namespace FactionWars.UI.Models
{
    /// <summary>
    /// Contains resource status information for display.
    /// </summary>
    public class ResourceStatusInfo
    {
        /// <summary>
        /// Current cash resources.
        /// </summary>
        public int Cash { get; set; }

        /// <summary>
        /// Current weapons stockpile.
        /// </summary>
        public int Weapons { get; set; }

        /// <summary>
        /// Current recruitment points.
        /// </summary>
        public int RecruitmentPoints { get; set; }

        /// <summary>
        /// Current troop count.
        /// </summary>
        public int TroopCount { get; set; }
    }
}
