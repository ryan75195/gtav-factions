using System;
using FactionWars.Factions.Models;

namespace FactionWars.UI.Models
{
    /// <summary>
    /// Data model for the territory indicator HUD display.
    /// Contains all information needed to render the zone status overlay at the top of the screen.
    /// </summary>
    public class TerritoryIndicatorData
    {
        /// <summary>
        /// The display name of the zone.
        /// </summary>
        public string ZoneName { get; }

        /// <summary>
        /// The name of the faction that owns this zone, or null if neutral.
        /// </summary>
        public string? OwnerFactionName { get; }

        /// <summary>
        /// The color of the owning faction, or null if neutral.
        /// </summary>
        public FactionColor? OwnerFactionColor { get; }

        /// <summary>
        /// Current control percentage (0-100) of the zone.
        /// </summary>
        public float ControlPercentage { get; }

        /// <summary>
        /// Whether the zone is currently being contested in combat.
        /// </summary>
        public bool IsContested { get; }

        /// <summary>
        /// Whether the zone is owned by the player's faction.
        /// </summary>
        public bool IsPlayerOwned { get; }

        /// <summary>
        /// Gets whether the zone is neutral (no owner).
        /// </summary>
        public bool IsNeutral => OwnerFactionName == null;

        /// <summary>
        /// Gets whether the zone is owned by an enemy faction.
        /// </summary>
        public bool IsEnemyOwned => !IsNeutral && !IsPlayerOwned;

        /// <summary>
        /// Creates a new territory indicator data instance.
        /// </summary>
        /// <param name="zoneName">The display name of the zone.</param>
        /// <param name="ownerFactionName">The owning faction's name, or null if neutral.</param>
        /// <param name="ownerFactionColor">The owning faction's color, or null if neutral.</param>
        /// <param name="controlPercentage">The control percentage (0-100).</param>
        /// <param name="isContested">Whether the zone is contested.</param>
        /// <param name="isPlayerOwned">Whether the player's faction owns the zone.</param>
        /// <exception cref="ArgumentNullException">Thrown if zoneName is null.</exception>
        /// <exception cref="ArgumentException">Thrown if zoneName is empty or whitespace.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if controlPercentage is not between 0 and 100.</exception>
        public TerritoryIndicatorData(
            string zoneName,
            string? ownerFactionName,
            FactionColor? ownerFactionColor,
            float controlPercentage,
            bool isContested,
            bool isPlayerOwned)
        {
            if (zoneName == null)
                throw new ArgumentNullException(nameof(zoneName));
            if (string.IsNullOrWhiteSpace(zoneName))
                throw new ArgumentException("Zone name cannot be empty or whitespace.", nameof(zoneName));

            if (controlPercentage < 0f || controlPercentage > 100f)
                throw new ArgumentOutOfRangeException(nameof(controlPercentage), "Control percentage must be between 0 and 100.");

            ZoneName = zoneName;
            OwnerFactionName = ownerFactionName;
            OwnerFactionColor = ownerFactionColor;
            ControlPercentage = controlPercentage;
            IsContested = isContested;
            IsPlayerOwned = isPlayerOwned;
        }
    }
}
