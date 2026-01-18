using System;

namespace FactionWars.UI.Models
{
    /// <summary>
    /// Data model for the combat HUD display.
    /// Contains all information needed to render the combat status overlay.
    /// </summary>
    public class CombatHudData
    {
        /// <summary>
        /// The ID of the zone where combat is occurring.
        /// </summary>
        public string ZoneId { get; }

        /// <summary>
        /// The display name of the zone.
        /// </summary>
        public string ZoneName { get; }

        /// <summary>
        /// The faction ID of the attacking faction.
        /// </summary>
        public string AttackerFactionId { get; }

        /// <summary>
        /// The faction ID of the defending faction.
        /// </summary>
        public string DefenderFactionId { get; }

        /// <summary>
        /// Percentage of zone control held by the attacker (0-100).
        /// </summary>
        public float AttackerControlPercent { get; }

        /// <summary>
        /// Percentage of zone control held by the defender (0-100).
        /// </summary>
        public float DefenderControlPercent { get; }

        /// <summary>
        /// Number of attacking peds in the zone.
        /// </summary>
        public int AttackerPedCount { get; }

        /// <summary>
        /// Number of defending peds in the zone.
        /// </summary>
        public int DefenderPedCount { get; }

        /// <summary>
        /// Seconds remaining until player can request reinforcements.
        /// </summary>
        public float ReinforcementCooldownSeconds { get; }

        /// <summary>
        /// Whether the player is the attacker (true) or defender (false).
        /// </summary>
        public bool IsPlayerAttacker { get; }

        /// <summary>
        /// Duration of the current combat encounter.
        /// </summary>
        public TimeSpan CombatDuration { get; }

        /// <summary>
        /// Gets the control percentage for the player's faction.
        /// </summary>
        public float PlayerControlPercent => IsPlayerAttacker ? AttackerControlPercent : DefenderControlPercent;

        /// <summary>
        /// Gets the control percentage for the enemy faction.
        /// </summary>
        public float EnemyControlPercent => IsPlayerAttacker ? DefenderControlPercent : AttackerControlPercent;

        /// <summary>
        /// Whether reinforcements are currently on cooldown.
        /// </summary>
        public bool IsReinforcementOnCooldown => ReinforcementCooldownSeconds > 0;

        /// <summary>
        /// Total number of peds in the combat encounter.
        /// </summary>
        public int TotalPedCount => AttackerPedCount + DefenderPedCount;

        /// <summary>
        /// Creates a new combat HUD data instance.
        /// </summary>
        /// <param name="zoneId">The zone ID where combat is occurring.</param>
        /// <param name="zoneName">The display name of the zone.</param>
        /// <param name="attackerFactionId">The attacking faction ID.</param>
        /// <param name="defenderFactionId">The defending faction ID.</param>
        /// <param name="attackerControlPercent">Attacker's control percentage (0-100).</param>
        /// <param name="defenderControlPercent">Defender's control percentage (0-100).</param>
        /// <param name="attackerPedCount">Number of attacking peds.</param>
        /// <param name="defenderPedCount">Number of defending peds.</param>
        /// <param name="reinforcementCooldownSeconds">Seconds until player can request reinforcements.</param>
        /// <param name="isPlayerAttacker">Whether the player is the attacker.</param>
        /// <param name="combatDuration">Duration of the combat encounter.</param>
        public CombatHudData(
            string zoneId,
            string zoneName,
            string attackerFactionId,
            string defenderFactionId,
            float attackerControlPercent,
            float defenderControlPercent,
            int attackerPedCount,
            int defenderPedCount,
            float reinforcementCooldownSeconds,
            bool isPlayerAttacker,
            TimeSpan combatDuration)
        {
            if (zoneId == null)
                throw new ArgumentNullException(nameof(zoneId));
            if (string.IsNullOrWhiteSpace(zoneId))
                throw new ArgumentException("Zone ID cannot be empty or whitespace.", nameof(zoneId));

            if (zoneName == null)
                throw new ArgumentNullException(nameof(zoneName));
            if (string.IsNullOrWhiteSpace(zoneName))
                throw new ArgumentException("Zone name cannot be empty or whitespace.", nameof(zoneName));

            if (attackerFactionId == null)
                throw new ArgumentNullException(nameof(attackerFactionId));
            if (string.IsNullOrWhiteSpace(attackerFactionId))
                throw new ArgumentException("Attacker faction ID cannot be empty or whitespace.", nameof(attackerFactionId));

            if (defenderFactionId == null)
                throw new ArgumentNullException(nameof(defenderFactionId));
            if (string.IsNullOrWhiteSpace(defenderFactionId))
                throw new ArgumentException("Defender faction ID cannot be empty or whitespace.", nameof(defenderFactionId));

            if (attackerControlPercent < 0f || attackerControlPercent > 100f)
                throw new ArgumentOutOfRangeException(nameof(attackerControlPercent), "Control percentage must be between 0 and 100.");

            if (defenderControlPercent < 0f || defenderControlPercent > 100f)
                throw new ArgumentOutOfRangeException(nameof(defenderControlPercent), "Control percentage must be between 0 and 100.");

            if (attackerPedCount < 0)
                throw new ArgumentOutOfRangeException(nameof(attackerPedCount), "Ped count cannot be negative.");

            if (defenderPedCount < 0)
                throw new ArgumentOutOfRangeException(nameof(defenderPedCount), "Ped count cannot be negative.");

            if (reinforcementCooldownSeconds < 0f)
                throw new ArgumentOutOfRangeException(nameof(reinforcementCooldownSeconds), "Cooldown cannot be negative.");

            ZoneId = zoneId;
            ZoneName = zoneName;
            AttackerFactionId = attackerFactionId;
            DefenderFactionId = defenderFactionId;
            AttackerControlPercent = attackerControlPercent;
            DefenderControlPercent = defenderControlPercent;
            AttackerPedCount = attackerPedCount;
            DefenderPedCount = defenderPedCount;
            ReinforcementCooldownSeconds = reinforcementCooldownSeconds;
            IsPlayerAttacker = isPlayerAttacker;
            CombatDuration = combatDuration;
        }
    }
}
