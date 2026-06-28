using FactionWars.Core.Interfaces;

namespace FactionWars.Combat.Models
{
    /// <summary>The centre and radius that Hold Area / Search &amp; Destroy operate within.</summary>
    public readonly struct AreaAnchor
    {
        public Vector3 Center { get; }
        public float Radius { get; }

        public AreaAnchor(Vector3 center, float radius)
        {
            Center = center;
            Radius = radius;
        }
    }
}
