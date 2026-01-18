namespace FactionWars.UI.Models
{
    /// <summary>
    /// Represents the visual color for zone boundary rendering.
    /// Maps to RGB values used when drawing zone outlines on the map.
    /// </summary>
    public enum BoundaryColor
    {
        /// <summary>
        /// Neutral/unowned zone color (white/gray).
        /// </summary>
        Neutral = 0,

        /// <summary>
        /// Michael's faction color (blue).
        /// </summary>
        Michael = 1,

        /// <summary>
        /// Trevor's faction color (orange).
        /// </summary>
        Trevor = 2,

        /// <summary>
        /// Franklin's faction color (green).
        /// </summary>
        Franklin = 3
    }
}
