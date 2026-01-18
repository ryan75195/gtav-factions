using System;

namespace FactionWars.Factions.Models
{
    /// <summary>
    /// Represents a faction in the territory control system.
    /// Factions can own zones, have resources, and engage in warfare.
    /// </summary>
    public class Faction : IEquatable<Faction>
    {
        /// <summary>
        /// Unique identifier for this faction.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Display name for this faction.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The name of the faction's leader (e.g., Michael De Santa).
        /// </summary>
        public string? Leader { get; }

        /// <summary>
        /// Description of the faction's characteristics and goals.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// The color used to represent this faction on the map and UI.
        /// </summary>
        public FactionColor Color { get; }

        /// <summary>
        /// Whether this faction is currently active in the game.
        /// Inactive factions don't participate in combat or resource generation.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Creates a new faction with the specified properties.
        /// </summary>
        /// <param name="id">Unique identifier for the faction.</param>
        /// <param name="name">Display name for the faction.</param>
        /// <param name="leader">Optional leader name.</param>
        /// <param name="description">Optional description of the faction.</param>
        /// <param name="color">Optional color for map/UI display (defaults to white).</param>
        /// <exception cref="ArgumentNullException">Thrown if id or name is null.</exception>
        /// <exception cref="ArgumentException">Thrown if id or name is empty/whitespace.</exception>
        public Faction(
            string id,
            string name,
            string? leader = null,
            string description = "",
            FactionColor? color = null)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Id cannot be empty or whitespace.", nameof(id));

            if (name == null)
                throw new ArgumentNullException(nameof(name));
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name cannot be empty or whitespace.", nameof(name));

            Id = id;
            Name = name;
            Leader = leader;
            Description = description ?? string.Empty;
            Color = color ?? new FactionColor(255, 255, 255);
            IsActive = true;
        }

        #region Equality

        public bool Equals(Faction? other)
        {
            if (other is null) return false;
            return Id == other.Id;
        }

        public override bool Equals(object? obj)
        {
            return obj is Faction faction && Equals(faction);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public static bool operator ==(Faction? left, Faction? right)
        {
            if (left is null) return right is null;
            return left.Equals(right);
        }

        public static bool operator !=(Faction? left, Faction? right)
        {
            return !(left == right);
        }

        #endregion

        public override string ToString()
        {
            return $"Faction[{Id}]: {Name}";
        }
    }
}
