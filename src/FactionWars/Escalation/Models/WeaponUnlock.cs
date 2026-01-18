using System;

namespace FactionWars.Escalation.Models
{
    /// <summary>
    /// Represents a weapon that can be unlocked at a specific escalation tier.
    /// Weapons are defined by their GTA V weapon hash and become available when
    /// a faction reaches the required escalation tier.
    /// </summary>
    public class WeaponUnlock : IEquatable<WeaponUnlock>
    {
        /// <summary>
        /// Default amount of ammo given when spawning a ped with this weapon.
        /// </summary>
        public const int DefaultAmmoAmount = 50;

        /// <summary>
        /// The GTA V weapon hash (e.g., "WEAPON_PISTOL").
        /// </summary>
        public string WeaponHash { get; }

        /// <summary>
        /// The display name shown in UI (e.g., "Pistol").
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// The category this weapon belongs to.
        /// </summary>
        public WeaponCategory Category { get; }

        /// <summary>
        /// The minimum escalation tier required to unlock this weapon.
        /// </summary>
        public EscalationTier RequiredTier { get; }

        /// <summary>
        /// Optional description of the weapon.
        /// </summary>
        public string? Description { get; }

        /// <summary>
        /// The amount of ammo given when spawning a ped with this weapon.
        /// </summary>
        public int AmmoAmount { get; }

        /// <summary>
        /// Creates a new weapon unlock definition.
        /// </summary>
        /// <param name="weaponHash">The GTA V weapon hash.</param>
        /// <param name="displayName">The display name for UI.</param>
        /// <param name="category">The weapon category.</param>
        /// <param name="requiredTier">The minimum tier required to unlock.</param>
        /// <param name="description">Optional description.</param>
        /// <param name="ammoAmount">Amount of ammo (default: 50).</param>
        /// <exception cref="ArgumentNullException">Thrown if weaponHash or displayName is null.</exception>
        /// <exception cref="ArgumentException">Thrown if weaponHash or displayName is empty/whitespace.</exception>
        public WeaponUnlock(
            string weaponHash,
            string displayName,
            WeaponCategory category,
            EscalationTier requiredTier,
            string? description = null,
            int ammoAmount = DefaultAmmoAmount)
        {
            if (weaponHash == null)
                throw new ArgumentNullException(nameof(weaponHash));
            if (string.IsNullOrWhiteSpace(weaponHash))
                throw new ArgumentException("Weapon hash cannot be empty or whitespace.", nameof(weaponHash));
            if (displayName == null)
                throw new ArgumentNullException(nameof(displayName));
            if (string.IsNullOrWhiteSpace(displayName))
                throw new ArgumentException("Display name cannot be empty or whitespace.", nameof(displayName));

            WeaponHash = weaponHash;
            DisplayName = displayName;
            Category = category;
            RequiredTier = requiredTier;
            Description = description;
            AmmoAmount = Math.Max(0, ammoAmount);
        }

        /// <summary>
        /// Checks if this weapon is unlocked at the specified tier.
        /// A weapon is unlocked if the current tier is >= required tier.
        /// </summary>
        /// <param name="tier">The tier to check against.</param>
        /// <returns>True if the weapon is unlocked at this tier.</returns>
        public bool IsUnlockedAtTier(EscalationTier tier)
        {
            return tier >= RequiredTier;
        }

        #region Equality

        public bool Equals(WeaponUnlock? other)
        {
            if (other is null) return false;
            return WeaponHash == other.WeaponHash;
        }

        public override bool Equals(object? obj)
        {
            return obj is WeaponUnlock unlock && Equals(unlock);
        }

        public override int GetHashCode()
        {
            return WeaponHash.GetHashCode();
        }

        public static bool operator ==(WeaponUnlock? left, WeaponUnlock? right)
        {
            if (left is null) return right is null;
            return left.Equals(right);
        }

        public static bool operator !=(WeaponUnlock? left, WeaponUnlock? right)
        {
            return !(left == right);
        }

        #endregion

        public override string ToString()
        {
            return $"WeaponUnlock[{DisplayName}]: {Category}, Tier {RequiredTier}";
        }
    }
}
