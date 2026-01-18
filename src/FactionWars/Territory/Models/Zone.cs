using System;
using System.Collections.Generic;
using FactionWars.Core.Interfaces;

namespace FactionWars.Territory.Models
{
    /// <summary>
    /// Represents a controllable territory zone in the game world.
    /// Zones can be owned by factions and provide strategic value.
    /// </summary>
    public class Zone : IEquatable<Zone>
    {
        private float _controlPercentage;

        /// <summary>
        /// Unique identifier for this zone.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Display name for this zone.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Center position of this zone in world coordinates.
        /// </summary>
        public Vector3 Center { get; }

        /// <summary>
        /// Radius of the zone from its center point.
        /// For polygon zones, this represents the bounding radius.
        /// </summary>
        public float Radius { get; }

        /// <summary>
        /// The boundary definition for this zone, containing geometry details.
        /// </summary>
        public ZoneBoundary Boundary { get; }

        /// <summary>
        /// Strategic importance of this zone (1-10 scale).
        /// Higher values mean more valuable territory.
        /// </summary>
        public int StrategicValue { get; }

        /// <summary>
        /// The faction ID that currently owns this zone, or null if neutral.
        /// </summary>
        public string? OwnerFactionId { get; set; }

        /// <summary>
        /// Current control percentage (0-100) of the zone by the controlling faction.
        /// Automatically clamped to valid range.
        /// </summary>
        public float ControlPercentage
        {
            get => _controlPercentage;
            set => _controlPercentage = Math.Max(0f, Math.Min(100f, value));
        }

        /// <summary>
        /// Indicates whether the zone is currently being contested by multiple factions.
        /// </summary>
        public bool IsContested { get; set; }

        /// <summary>
        /// Special characteristics of this zone that affect resource generation and combat.
        /// Multiple traits can be combined using bitwise OR.
        /// </summary>
        public ZoneTrait Traits { get; set; }

        /// <summary>
        /// Creates a new circular zone with the specified properties.
        /// </summary>
        /// <param name="id">Unique identifier for the zone.</param>
        /// <param name="name">Display name for the zone.</param>
        /// <param name="center">Center position in world coordinates.</param>
        /// <param name="radius">Radius of the zone (default: 150).</param>
        /// <param name="strategicValue">Strategic importance 1-10 (default: 1).</param>
        /// <exception cref="ArgumentNullException">Thrown if id or name is null.</exception>
        /// <exception cref="ArgumentException">Thrown if id or name is empty/whitespace.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if radius or strategicValue is invalid.</exception>
        public Zone(string id, string name, Vector3 center, float radius = 150f, int strategicValue = 1)
        {
            ValidateIdAndName(id, name);

            if (radius <= 0)
                throw new ArgumentOutOfRangeException(nameof(radius), "Radius must be greater than zero.");

            if (strategicValue <= 0)
                throw new ArgumentOutOfRangeException(nameof(strategicValue), "Strategic value must be greater than zero.");

            Id = id;
            Name = name;
            Center = center;
            Radius = radius;
            Boundary = ZoneBoundary.CreateCircular(center, radius);
            StrategicValue = strategicValue;
            OwnerFactionId = null;
            ControlPercentage = 0f;
            IsContested = false;
            Traits = ZoneTrait.None;
        }

        /// <summary>
        /// Creates a new polygon zone with the specified vertices.
        /// </summary>
        /// <param name="id">Unique identifier for the zone.</param>
        /// <param name="name">Display name for the zone.</param>
        /// <param name="vertices">The vertices defining the polygon boundary.</param>
        /// <param name="strategicValue">Strategic importance 1-10 (default: 1).</param>
        /// <exception cref="ArgumentNullException">Thrown if id, name, or vertices is null.</exception>
        /// <exception cref="ArgumentException">Thrown if id or name is empty/whitespace, or fewer than 3 vertices.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if strategicValue is invalid.</exception>
        public Zone(string id, string name, IEnumerable<Vector3> vertices, int strategicValue = 1)
        {
            ValidateIdAndName(id, name);

            if (vertices == null)
                throw new ArgumentNullException(nameof(vertices));

            if (strategicValue <= 0)
                throw new ArgumentOutOfRangeException(nameof(strategicValue), "Strategic value must be greater than zero.");

            var boundary = ZoneBoundary.CreatePolygon(vertices);

            Id = id;
            Name = name;
            Boundary = boundary;
            Center = boundary.Center;
            Radius = boundary.BoundingRadius;
            StrategicValue = strategicValue;
            OwnerFactionId = null;
            ControlPercentage = 0f;
            IsContested = false;
            Traits = ZoneTrait.None;
        }

        private static void ValidateIdAndName(string id, string name)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Id cannot be empty or whitespace.", nameof(id));

            if (name == null)
                throw new ArgumentNullException(nameof(name));
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Name cannot be empty.", nameof(name));
        }

        public bool Equals(Zone? other)
        {
            if (other is null) return false;
            return Id == other.Id;
        }

        public override bool Equals(object? obj)
        {
            return obj is Zone zone && Equals(zone);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public static bool operator ==(Zone? left, Zone? right)
        {
            if (left is null) return right is null;
            return left.Equals(right);
        }

        public static bool operator !=(Zone? left, Zone? right)
        {
            return !(left == right);
        }

        public override string ToString()
        {
            return $"Zone[{Id}]: {Name} at {Center}";
        }
    }
}
