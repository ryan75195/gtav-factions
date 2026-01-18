namespace FactionWars.UI.Models
{
    /// <summary>
    /// Specifies the type of rendering used for a zone boundary.
    /// </summary>
    public enum BoundaryRenderType
    {
        /// <summary>
        /// Circular boundary rendered as a cylindrical marker.
        /// </summary>
        Circular = 0,

        /// <summary>
        /// Polygon boundary rendered as connected line segments.
        /// </summary>
        Polygon = 1
    }
}
