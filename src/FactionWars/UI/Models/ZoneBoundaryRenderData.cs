using System.Collections.Generic;
using FactionWars.Core.Interfaces;

namespace FactionWars.UI.Models
{
    /// <summary>
    /// Contains rendering data for a zone boundary.
    /// Used to track the visual state of rendered zone boundaries.
    /// </summary>
    public class ZoneBoundaryRenderData
    {
        /// <summary>
        /// Gets or sets the zone ID this render data belongs to.
        /// </summary>
        public string ZoneId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the render type (circular or polygon).
        /// </summary>
        public BoundaryRenderType RenderType { get; set; }

        /// <summary>
        /// Gets or sets the center point (for circular boundaries) or centroid (for polygons).
        /// </summary>
        public Vector3 Center { get; set; }

        /// <summary>
        /// Gets or sets the radius (for circular boundaries). Zero for polygons.
        /// </summary>
        public float Radius { get; set; }

        /// <summary>
        /// Gets or sets the vertices defining the boundary (for polygon boundaries).
        /// Empty for circular boundaries.
        /// </summary>
        public IReadOnlyList<Vector3> Vertices { get; set; } = new List<Vector3>();

        /// <summary>
        /// Gets or sets the color of the boundary.
        /// </summary>
        public BoundaryColor Color { get; set; } = BoundaryColor.Neutral;

        /// <summary>
        /// Gets or sets the alpha/transparency value (0-255).
        /// 255 is fully opaque, 0 is fully transparent.
        /// </summary>
        public int Alpha { get; set; } = 100;

        /// <summary>
        /// Gets or sets whether the boundary is currently visible.
        /// </summary>
        public bool IsVisible { get; set; } = true;
    }
}
