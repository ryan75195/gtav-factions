using System;

namespace FactionWars.Combat.Models
{
    /// <summary>
    /// Represents an active or completed combat encounter in a zone.
    /// Tracks the attacking and defending factions, ped counts, and control percentages.
    /// </summary>
    public class CombatEncounter : IEquatable<CombatEncounter>
    {
        private float _attackerControlPercentage;
        private float _defenderControlPercentage;
        private int _attackerPedCount;
        private int _defenderPedCount;

        /// <summary>
        /// Unique identifier for this combat encounter.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// The zone where this combat is taking place.
        /// </summary>
        public string ZoneId { get; }

        /// <summary>
        /// The faction initiating the attack.
        /// </summary>
        public string AttackingFactionId { get; }

        /// <summary>
        /// The faction defending the zone.
        /// </summary>
        public string DefendingFactionId { get; }

        /// <summary>
        /// The UTC time when this encounter started.
        /// </summary>
        public DateTime StartedAt { get; }

        /// <summary>
        /// The UTC time when this encounter ended, or null if still active.
        /// </summary>
        public DateTime? EndedAt { get; private set; }

        /// <summary>
        /// Current status of the combat encounter.
        /// </summary>
        public CombatStatus Status { get; private set; }

        /// <summary>
        /// Whether the combat encounter is currently active.
        /// </summary>
        public bool IsActive => Status == CombatStatus.InProgress;

        /// <summary>
        /// Percentage of zone control held by the attacker (0-100).
        /// </summary>
        public float AttackerControlPercentage
        {
            get => _attackerControlPercentage;
            set => _attackerControlPercentage = Math.Max(0f, Math.Min(100f, value));
        }

        /// <summary>
        /// Percentage of zone control held by the defender (0-100).
        /// </summary>
        public float DefenderControlPercentage
        {
            get => _defenderControlPercentage;
            set => _defenderControlPercentage = Math.Max(0f, Math.Min(100f, value));
        }

        /// <summary>
        /// Number of attacking peds currently in the zone.
        /// </summary>
        public int AttackerPedCount
        {
            get => _attackerPedCount;
            set => _attackerPedCount = Math.Max(0, value);
        }

        /// <summary>
        /// Number of defending peds currently in the zone.
        /// </summary>
        public int DefenderPedCount
        {
            get => _defenderPedCount;
            set => _defenderPedCount = Math.Max(0, value);
        }

        /// <summary>
        /// Total number of peds in the encounter.
        /// </summary>
        public int TotalPedCount => AttackerPedCount + DefenderPedCount;

        /// <summary>
        /// The faction ID of the winner, or null if no winner yet or stalemate/aborted.
        /// </summary>
        public string? WinnerFactionId
        {
            get
            {
                return Status switch
                {
                    CombatStatus.AttackerVictory => AttackingFactionId,
                    CombatStatus.DefenderVictory => DefendingFactionId,
                    _ => null
                };
            }
        }

        /// <summary>
        /// Creates a new combat encounter.
        /// </summary>
        /// <param name="id">Unique identifier for this encounter.</param>
        /// <param name="zoneId">The zone where combat takes place.</param>
        /// <param name="attackingFactionId">The faction initiating the attack.</param>
        /// <param name="defendingFactionId">The faction defending the zone.</param>
        /// <exception cref="ArgumentNullException">Thrown if any required parameter is null.</exception>
        /// <exception cref="ArgumentException">Thrown if any parameter is empty/whitespace or if attacker equals defender.</exception>
        public CombatEncounter(string id, string zoneId, string attackingFactionId, string defendingFactionId)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Id cannot be empty or whitespace.", nameof(id));

            if (zoneId == null)
                throw new ArgumentNullException(nameof(zoneId));
            if (string.IsNullOrWhiteSpace(zoneId))
                throw new ArgumentException("ZoneId cannot be empty or whitespace.", nameof(zoneId));

            if (attackingFactionId == null)
                throw new ArgumentNullException(nameof(attackingFactionId));
            if (string.IsNullOrWhiteSpace(attackingFactionId))
                throw new ArgumentException("AttackingFactionId cannot be empty or whitespace.", nameof(attackingFactionId));

            if (defendingFactionId == null)
                throw new ArgumentNullException(nameof(defendingFactionId));
            if (string.IsNullOrWhiteSpace(defendingFactionId))
                throw new ArgumentException("DefendingFactionId cannot be empty or whitespace.", nameof(defendingFactionId));

            if (attackingFactionId == defendingFactionId)
                throw new ArgumentException("Attacking faction cannot be the same as defending faction.");

            Id = id;
            ZoneId = zoneId;
            AttackingFactionId = attackingFactionId;
            DefendingFactionId = defendingFactionId;
            StartedAt = DateTime.UtcNow;
            EndedAt = null;
            Status = CombatStatus.InProgress;
            _attackerControlPercentage = 0f;
            _defenderControlPercentage = 100f;
            _attackerPedCount = 0;
            _defenderPedCount = 0;
        }

        /// <summary>
        /// Ends the combat encounter with the specified status.
        /// </summary>
        /// <param name="status">The final status (cannot be InProgress).</param>
        /// <exception cref="ArgumentException">Thrown if status is InProgress.</exception>
        /// <exception cref="InvalidOperationException">Thrown if combat is already ended.</exception>
        public void End(CombatStatus status)
        {
            if (status == CombatStatus.InProgress)
                throw new ArgumentException("Cannot end combat with InProgress status.", nameof(status));

            if (!IsActive)
                throw new InvalidOperationException("Combat encounter has already ended.");

            Status = status;
            EndedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Gets the duration of the combat encounter.
        /// If still active, returns time since start. If ended, returns the total duration.
        /// </summary>
        /// <returns>The duration of the encounter.</returns>
        public TimeSpan GetDuration()
        {
            if (EndedAt.HasValue)
                return EndedAt.Value - StartedAt;

            return DateTime.UtcNow - StartedAt;
        }

        #region Equality

        public bool Equals(CombatEncounter? other)
        {
            if (other is null) return false;
            return Id == other.Id;
        }

        public override bool Equals(object? obj)
        {
            return obj is CombatEncounter encounter && Equals(encounter);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public static bool operator ==(CombatEncounter? left, CombatEncounter? right)
        {
            if (left is null) return right is null;
            return left.Equals(right);
        }

        public static bool operator !=(CombatEncounter? left, CombatEncounter? right)
        {
            return !(left == right);
        }

        #endregion

        public override string ToString()
        {
            return $"CombatEncounter[{Id}] in {ZoneId}: {AttackingFactionId} vs {DefendingFactionId} - {Status}";
        }
    }
}
