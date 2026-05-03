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
        /// Number of defenders currently spawned in the zone (0-12).
        /// Only relevant for player-owned zones.
        /// </summary>
        public int DeployedDefenderCount { get; }

        /// <summary>
        /// Number of defenders in reserve waiting to spawn.
        /// Only relevant for player-owned zones.
        /// </summary>
        public int ReserveDefenderCount { get; }

        /// <summary>
        /// Number of player's troops in combat (for enemy zones during takeover).
        /// </summary>
        public int PlayerTroopCount { get; }

        /// <summary>
        /// Number of enemy defenders in combat (for enemy zones during takeover).
        /// </summary>
        public int EnemyDefenderCount { get; }

        /// <summary>
        /// Number of enemy reserves remaining (for enemy zones during takeover).
        /// </summary>
        public int EnemyReserveCount { get; }

        /// <summary>
        /// Count of the AI third-party participant in a 3-way battle (the AI attacker that
        /// isn't the player). Zero in 2-way scenarios.
        /// </summary>
        public int ThirdPartyCount { get; }

        /// <summary>
        /// Faction color of the AI third-party participant. Null when no third party (i.e., 2-way).
        /// </summary>
        public FactionColor? ThirdPartyFactionColor { get; }

        /// <summary>
        /// Creates a new territory indicator data instance.
        /// </summary>
        /// <param name="zoneName">The display name of the zone.</param>
        /// <param name="ownerFactionName">The owning faction's name, or null if neutral.</param>
        /// <param name="ownerFactionColor">The owning faction's color, or null if neutral.</param>
        /// <param name="controlPercentage">The control percentage (0-100).</param>
        /// <param name="isContested">Whether the zone is contested.</param>
        /// <param name="isPlayerOwned">Whether the player's faction owns the zone.</param>
        /// <param name="deployedDefenderCount">Number of defenders currently spawned in the zone.</param>
        /// <param name="reserveDefenderCount">Number of defenders in reserve waiting to spawn.</param>
        /// <param name="playerTroopCount">Number of player's troops in combat.</param>
        /// <param name="enemyDefenderCount">Number of enemy defenders in combat.</param>
        /// <param name="enemyReserveCount">Number of enemy reserves remaining.</param>
        /// <param name="thirdPartyCount">Count of AI third-party troops in a 3-way battle. Zero in 2-way scenarios.</param>
        /// <param name="thirdPartyFactionColor">Faction color of the AI third party. Null in 2-way scenarios.</param>
        /// <exception cref="ArgumentNullException">Thrown if zoneName is null.</exception>
        /// <exception cref="ArgumentException">Thrown if zoneName is empty or whitespace.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if controlPercentage is not between 0 and 100.</exception>
        public TerritoryIndicatorData(
            string zoneName,
            string? ownerFactionName,
            FactionColor? ownerFactionColor,
            float controlPercentage,
            bool isContested,
            bool isPlayerOwned,
            int deployedDefenderCount = 0,
            int reserveDefenderCount = 0,
            int playerTroopCount = 0,
            int enemyDefenderCount = 0,
            int enemyReserveCount = 0,
            int thirdPartyCount = 0,
            FactionColor? thirdPartyFactionColor = null)
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
            DeployedDefenderCount = Math.Max(0, deployedDefenderCount);
            ReserveDefenderCount = Math.Max(0, reserveDefenderCount);
            PlayerTroopCount = Math.Max(0, playerTroopCount);
            EnemyDefenderCount = Math.Max(0, enemyDefenderCount);
            EnemyReserveCount = Math.Max(0, enemyReserveCount);
            ThirdPartyCount = Math.Max(0, thirdPartyCount);
            ThirdPartyFactionColor = thirdPartyFactionColor;
        }
    }
}
