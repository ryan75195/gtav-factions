using System;
using System.Collections.Generic;

namespace FactionWars.Factions.Models
{
    /// <summary>
    /// Represents the current state of a faction including resources, army, and owned zones.
    /// This is the mutable runtime state that changes during gameplay.
    /// </summary>
    public class FactionState : IEquatable<FactionState>
    {
        private const int WeaponMultiplier = 2;

        private readonly HashSet<string> _ownedZoneIds;
        private int _cash;
        private int _recruitmentPoints;
        private int _weapons;
        private int _troopCount;

        /// <summary>
        /// The ID of the faction this state belongs to.
        /// </summary>
        public string FactionId { get; }

        /// <summary>
        /// Current cash resources available to the faction.
        /// Automatically clamped to non-negative values.
        /// </summary>
        public int Cash
        {
            get => _cash;
            set => _cash = Math.Max(0, value);
        }

        /// <summary>
        /// Current recruitment points available for recruiting troops.
        /// Automatically clamped to non-negative values.
        /// </summary>
        public int RecruitmentPoints
        {
            get => _recruitmentPoints;
            set => _recruitmentPoints = Math.Max(0, value);
        }

        /// <summary>
        /// Current weapons stockpile. Weapons enhance military strength.
        /// Automatically clamped to non-negative values.
        /// </summary>
        public int Weapons
        {
            get => _weapons;
            set => _weapons = Math.Max(0, value);
        }

        /// <summary>
        /// Current number of troops in the faction's army.
        /// Automatically clamped to non-negative values.
        /// </summary>
        public int TroopCount
        {
            get => _troopCount;
            set => _troopCount = Math.Max(0, value);
        }

        /// <summary>
        /// Read-only collection of zone IDs owned by this faction.
        /// </summary>
        public IReadOnlyCollection<string> OwnedZoneIds => _ownedZoneIds;

        /// <summary>
        /// The number of zones currently owned by this faction.
        /// </summary>
        public int ZoneCount => _ownedZoneIds.Count;

        /// <summary>
        /// Calculated military strength combining troops and weapons.
        /// Weapons provide a multiplier to effective strength.
        /// </summary>
        public int MilitaryStrength => _troopCount + (_weapons * WeaponMultiplier);

        /// <summary>
        /// Creates a new faction state with optional initial values.
        /// </summary>
        /// <param name="factionId">The ID of the faction this state belongs to.</param>
        /// <param name="initialCash">Starting cash (default: 0).</param>
        /// <param name="initialTroopCount">Starting troop count (default: 0).</param>
        /// <exception cref="ArgumentNullException">Thrown if factionId is null.</exception>
        /// <exception cref="ArgumentException">Thrown if factionId is empty or whitespace.</exception>
        public FactionState(string factionId, int initialCash = 0, int initialTroopCount = 0)
        {
            if (factionId == null)
                throw new ArgumentNullException(nameof(factionId));
            if (string.IsNullOrWhiteSpace(factionId))
                throw new ArgumentException("Faction ID cannot be empty or whitespace.", nameof(factionId));

            FactionId = factionId;
            _ownedZoneIds = new HashSet<string>();
            _cash = Math.Max(0, initialCash);
            _troopCount = Math.Max(0, initialTroopCount);
            _recruitmentPoints = 0;
            _weapons = 0;
        }

        #region Zone Management

        /// <summary>
        /// Adds a zone to this faction's ownership.
        /// </summary>
        /// <param name="zoneId">The ID of the zone to add.</param>
        /// <exception cref="ArgumentNullException">Thrown if zoneId is null.</exception>
        /// <exception cref="ArgumentException">Thrown if zoneId is empty or whitespace.</exception>
        public void AddZone(string zoneId)
        {
            if (zoneId == null)
                throw new ArgumentNullException(nameof(zoneId));
            if (string.IsNullOrWhiteSpace(zoneId))
                throw new ArgumentException("Zone ID cannot be empty or whitespace.", nameof(zoneId));

            _ownedZoneIds.Add(zoneId);
        }

        /// <summary>
        /// Removes a zone from this faction's ownership.
        /// </summary>
        /// <param name="zoneId">The ID of the zone to remove.</param>
        /// <returns>True if the zone was removed, false if it wasn't owned.</returns>
        public bool RemoveZone(string zoneId)
        {
            if (string.IsNullOrWhiteSpace(zoneId))
                return false;

            return _ownedZoneIds.Remove(zoneId);
        }

        /// <summary>
        /// Checks if this faction owns a specific zone.
        /// </summary>
        /// <param name="zoneId">The ID of the zone to check.</param>
        /// <returns>True if the faction owns the zone, false otherwise.</returns>
        public bool OwnsZone(string zoneId)
        {
            if (string.IsNullOrWhiteSpace(zoneId))
                return false;

            return _ownedZoneIds.Contains(zoneId);
        }

        #endregion

        #region Cash Operations

        /// <summary>
        /// Adds cash to the faction's resources.
        /// </summary>
        /// <param name="amount">The amount of cash to add (must be non-negative).</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if amount is negative.</exception>
        public void AddCash(int amount)
        {
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount), "Amount cannot be negative.");

            _cash += amount;
        }

        /// <summary>
        /// Attempts to spend cash from the faction's resources.
        /// </summary>
        /// <param name="amount">The amount of cash to spend (must be non-negative).</param>
        /// <returns>True if the purchase was successful, false if insufficient funds.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if amount is negative.</exception>
        public bool SpendCash(int amount)
        {
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount), "Amount cannot be negative.");

            if (_cash < amount)
                return false;

            _cash -= amount;
            return true;
        }

        /// <summary>
        /// Checks if the faction can afford a purchase.
        /// </summary>
        /// <param name="amount">The amount to check.</param>
        /// <returns>True if the faction has sufficient funds, false otherwise.</returns>
        public bool CanAfford(int amount)
        {
            return _cash >= amount;
        }

        #endregion

        #region Troop Operations

        /// <summary>
        /// Recruits additional troops into the faction's army.
        /// </summary>
        /// <param name="count">The number of troops to recruit (must be non-negative).</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if count is negative.</exception>
        public void RecruitTroops(int count)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), "Count cannot be negative.");

            _troopCount += count;
        }

        /// <summary>
        /// Reduces the faction's troop count (e.g., due to combat losses).
        /// The troop count will not go below zero.
        /// </summary>
        /// <param name="count">The number of troops lost (must be non-negative).</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if count is negative.</exception>
        public void LoseTroops(int count)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), "Count cannot be negative.");

            _troopCount = Math.Max(0, _troopCount - count);
        }

        /// <summary>
        /// Checks if the faction has at least a specified number of troops.
        /// </summary>
        /// <param name="count">The minimum troop count to check for.</param>
        /// <returns>True if the faction has enough troops, false otherwise.</returns>
        public bool HasTroops(int count)
        {
            return _troopCount >= count;
        }

        #endregion

        #region Equality

        public bool Equals(FactionState? other)
        {
            if (other is null) return false;
            return FactionId == other.FactionId;
        }

        public override bool Equals(object? obj)
        {
            return obj is FactionState state && Equals(state);
        }

        public override int GetHashCode()
        {
            return FactionId.GetHashCode();
        }

        public static bool operator ==(FactionState? left, FactionState? right)
        {
            if (left is null) return right is null;
            return left.Equals(right);
        }

        public static bool operator !=(FactionState? left, FactionState? right)
        {
            return !(left == right);
        }

        #endregion

        public override string ToString()
        {
            return $"FactionState[{FactionId}]: Cash={_cash}, Troops={_troopCount}, Zones={ZoneCount}";
        }
    }
}
