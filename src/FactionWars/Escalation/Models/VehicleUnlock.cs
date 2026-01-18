using System;

namespace FactionWars.Escalation.Models
{
    /// <summary>
    /// Represents a vehicle that can be unlocked at a specific escalation tier.
    /// Vehicles are defined by their GTA V model name and become available when
    /// a faction reaches the required escalation tier.
    /// </summary>
    public class VehicleUnlock : IEquatable<VehicleUnlock>
    {
        /// <summary>
        /// Default max speed value when not specified.
        /// </summary>
        public const int DefaultMaxSpeed = 100;

        /// <summary>
        /// The GTA V vehicle model name (e.g., "BLISTA").
        /// </summary>
        public string VehicleModel { get; }

        /// <summary>
        /// The display name shown in UI (e.g., "Blista").
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// The category this vehicle belongs to.
        /// </summary>
        public VehicleCategory Category { get; }

        /// <summary>
        /// The minimum escalation tier required to unlock this vehicle.
        /// </summary>
        public EscalationTier RequiredTier { get; }

        /// <summary>
        /// Optional description of the vehicle.
        /// </summary>
        public string? Description { get; }

        /// <summary>
        /// The maximum speed of the vehicle.
        /// </summary>
        public int MaxSpeed { get; }

        /// <summary>
        /// Creates a new vehicle unlock definition.
        /// </summary>
        /// <param name="vehicleModel">The GTA V vehicle model name.</param>
        /// <param name="displayName">The display name for UI.</param>
        /// <param name="category">The vehicle category.</param>
        /// <param name="requiredTier">The minimum tier required to unlock.</param>
        /// <param name="description">Optional description.</param>
        /// <param name="maxSpeed">Maximum speed of the vehicle (default: 100).</param>
        /// <exception cref="ArgumentNullException">Thrown if vehicleModel or displayName is null.</exception>
        /// <exception cref="ArgumentException">Thrown if vehicleModel or displayName is empty/whitespace.</exception>
        public VehicleUnlock(
            string vehicleModel,
            string displayName,
            VehicleCategory category,
            EscalationTier requiredTier,
            string? description = null,
            int maxSpeed = DefaultMaxSpeed)
        {
            if (vehicleModel == null)
                throw new ArgumentNullException(nameof(vehicleModel));
            if (string.IsNullOrWhiteSpace(vehicleModel))
                throw new ArgumentException("Vehicle model cannot be empty or whitespace.", nameof(vehicleModel));
            if (displayName == null)
                throw new ArgumentNullException(nameof(displayName));
            if (string.IsNullOrWhiteSpace(displayName))
                throw new ArgumentException("Display name cannot be empty or whitespace.", nameof(displayName));

            VehicleModel = vehicleModel;
            DisplayName = displayName;
            Category = category;
            RequiredTier = requiredTier;
            Description = description;
            MaxSpeed = Math.Max(0, maxSpeed);
        }

        /// <summary>
        /// Checks if this vehicle is unlocked at the specified tier.
        /// A vehicle is unlocked if the current tier is >= required tier.
        /// </summary>
        /// <param name="tier">The tier to check against.</param>
        /// <returns>True if the vehicle is unlocked at this tier.</returns>
        public bool IsUnlockedAtTier(EscalationTier tier)
        {
            return tier >= RequiredTier;
        }

        #region Equality

        public bool Equals(VehicleUnlock? other)
        {
            if (other is null) return false;
            return VehicleModel == other.VehicleModel;
        }

        public override bool Equals(object? obj)
        {
            return obj is VehicleUnlock unlock && Equals(unlock);
        }

        public override int GetHashCode()
        {
            return VehicleModel.GetHashCode();
        }

        public static bool operator ==(VehicleUnlock? left, VehicleUnlock? right)
        {
            if (left is null) return right is null;
            return left.Equals(right);
        }

        public static bool operator !=(VehicleUnlock? left, VehicleUnlock? right)
        {
            return !(left == right);
        }

        #endregion

        public override string ToString()
        {
            return $"VehicleUnlock[{DisplayName}]: {Category}, Tier {RequiredTier}";
        }
    }
}
