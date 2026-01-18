using System;
using System.Collections.Generic;
using System.Linq;
using FactionWars.Core.Interfaces;

namespace FactionWars.Territory.Models
{
    /// <summary>
    /// Represents a zone boundary that can be either circular or polygon-based.
    /// Provides coordinate-based containment testing for determining if a point is within a zone.
    /// Uses 2D (X, Y) coordinates, ignoring Z for containment tests (suitable for GTA V map zones).
    /// </summary>
    public class ZoneBoundary
    {
        private readonly float _radius;
        private readonly IReadOnlyList<Vector3> _vertices;

        /// <summary>
        /// The type of boundary geometry.
        /// </summary>
        public BoundaryType Type { get; }

        /// <summary>
        /// The center point of the boundary.
        /// For circular boundaries, this is the specified center.
        /// For polygon boundaries, this is the centroid of all vertices.
        /// </summary>
        public Vector3 Center { get; }

        /// <summary>
        /// The bounding radius of the boundary.
        /// For circular boundaries, this is the specified radius.
        /// For polygon boundaries, this is the distance from center to the farthest vertex.
        /// </summary>
        public float BoundingRadius { get; }

        /// <summary>
        /// Gets the vertices defining this boundary.
        /// For circular boundaries, returns an empty list.
        /// For polygon boundaries, returns the list of vertices.
        /// </summary>
        public IReadOnlyList<Vector3> Vertices => _vertices;

        private ZoneBoundary(BoundaryType type, Vector3 center, float radius, IReadOnlyList<Vector3> vertices)
        {
            Type = type;
            Center = center;
            BoundingRadius = radius;
            _radius = radius;
            _vertices = vertices;
        }

        /// <summary>
        /// Creates a circular zone boundary.
        /// </summary>
        /// <param name="center">The center point of the circle.</param>
        /// <param name="radius">The radius of the circle.</param>
        /// <returns>A new circular ZoneBoundary.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if radius is less than or equal to zero.</exception>
        public static ZoneBoundary CreateCircular(Vector3 center, float radius)
        {
            if (radius <= 0)
                throw new ArgumentOutOfRangeException(nameof(radius), "Radius must be greater than zero.");

            return new ZoneBoundary(BoundaryType.Circular, center, radius, Array.Empty<Vector3>());
        }

        /// <summary>
        /// Creates a polygon zone boundary from a list of vertices.
        /// </summary>
        /// <param name="vertices">The vertices defining the polygon, in order.</param>
        /// <returns>A new polygon ZoneBoundary.</returns>
        /// <exception cref="ArgumentNullException">Thrown if vertices is null.</exception>
        /// <exception cref="ArgumentException">Thrown if fewer than 3 vertices are provided.</exception>
        public static ZoneBoundary CreatePolygon(IEnumerable<Vector3> vertices)
        {
            if (vertices == null)
                throw new ArgumentNullException(nameof(vertices));

            var vertexList = vertices.ToList();

            if (vertexList.Count < 3)
                throw new ArgumentException("A polygon boundary requires at least 3 vertices.", nameof(vertices));

            // Calculate centroid
            float sumX = 0, sumY = 0, sumZ = 0;
            foreach (var v in vertexList)
            {
                sumX += v.X;
                sumY += v.Y;
                sumZ += v.Z;
            }
            var center = new Vector3(sumX / vertexList.Count, sumY / vertexList.Count, sumZ / vertexList.Count);

            // Calculate bounding radius (distance to farthest vertex)
            float maxDistance = 0;
            foreach (var v in vertexList)
            {
                var distance = center.DistanceTo2D(v);
                if (distance > maxDistance)
                    maxDistance = distance;
            }

            return new ZoneBoundary(BoundaryType.Polygon, center, maxDistance, vertexList.AsReadOnly());
        }

        /// <summary>
        /// Determines if a point is contained within this boundary using 2D (X, Y) coordinates.
        /// Z coordinate is ignored for containment tests.
        /// </summary>
        /// <param name="point">The point to test.</param>
        /// <returns>True if the point is inside or on the boundary, false otherwise.</returns>
        public bool Contains(Vector3 point)
        {
            return Type == BoundaryType.Circular
                ? ContainsCircular(point)
                : ContainsPolygon(point);
        }

        private bool ContainsCircular(Vector3 point)
        {
            var distance = Center.DistanceTo2D(point);
            return distance <= _radius;
        }

        private bool ContainsPolygon(Vector3 point)
        {
            // Use ray casting algorithm for point-in-polygon test
            // Also check if point is on edge or vertex

            // First check if point is on any vertex
            foreach (var vertex in _vertices)
            {
                if (Math.Abs(point.X - vertex.X) < 0.0001f && Math.Abs(point.Y - vertex.Y) < 0.0001f)
                    return true;
            }

            // Check if point is on any edge
            for (int i = 0; i < _vertices.Count; i++)
            {
                var v1 = _vertices[i];
                var v2 = _vertices[(i + 1) % _vertices.Count];

                if (IsPointOnLineSegment(point, v1, v2))
                    return true;
            }

            // Ray casting algorithm for point-in-polygon
            bool inside = false;
            int n = _vertices.Count;

            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                var vi = _vertices[i];
                var vj = _vertices[j];

                if (((vi.Y > point.Y) != (vj.Y > point.Y)) &&
                    (point.X < (vj.X - vi.X) * (point.Y - vi.Y) / (vj.Y - vi.Y) + vi.X))
                {
                    inside = !inside;
                }
            }

            return inside;
        }

        private static bool IsPointOnLineSegment(Vector3 point, Vector3 lineStart, Vector3 lineEnd)
        {
            // Check if point is on line segment using cross product and bounding box
            float crossProduct = (point.Y - lineStart.Y) * (lineEnd.X - lineStart.X) -
                                 (point.X - lineStart.X) * (lineEnd.Y - lineStart.Y);

            if (Math.Abs(crossProduct) > 0.0001f)
                return false;

            // Check bounding box
            float minX = Math.Min(lineStart.X, lineEnd.X);
            float maxX = Math.Max(lineStart.X, lineEnd.X);
            float minY = Math.Min(lineStart.Y, lineEnd.Y);
            float maxY = Math.Max(lineStart.Y, lineEnd.Y);

            return point.X >= minX - 0.0001f && point.X <= maxX + 0.0001f &&
                   point.Y >= minY - 0.0001f && point.Y <= maxY + 0.0001f;
        }
    }
}
