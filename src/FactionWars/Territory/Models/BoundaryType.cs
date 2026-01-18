namespace FactionWars.Territory.Models
{
    /// <summary>
    /// Specifies the type of zone boundary geometry.
    /// </summary>
    public enum BoundaryType
    {
        /// <summary>
        /// A circular boundary defined by a center point and radius.
        /// </summary>
        Circular,

        /// <summary>
        /// A polygon boundary defined by a list of vertices.
        /// </summary>
        Polygon
    }
}
