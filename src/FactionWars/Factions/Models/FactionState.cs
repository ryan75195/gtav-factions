using System;
using System.Collections.Generic;
using System.Linq;
using FactionWars.Core.Models;

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
        private readonly Dictionary<DefenderRole, int> _reservePool;
        private int _cash;
        private int _recruitmentPoints;
        private int _weapons;

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
        /// Computed from the total reserve pool across all tiers.
        /// </summary>
        public int TroopCount => TotalReserveTroops;

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
        public int MilitaryStrength => TotalReserveTroops + (_weapons * WeaponMultiplier);

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
            _reservePool = new Dictionary<DefenderRole, int>
            {
                { DefenderRole.Grunt, Math.Max(0, initialTroopCount) },
                { DefenderRole.Gunner, 0 },
                { DefenderRole.Rifleman, 0 }
            };
            _cash = Math.Max(0, initialCash);
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
        /// Troops are added to the Basic tier reserve pool.
        /// </summary>
        /// <param name="count">The number of troops to recruit (must be non-negative).</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if count is negative.</exception>
        public void RecruitTroops(int count)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), "Count cannot be negative.");

            AddReserveTroops(DefenderRole.Grunt, count);
        }

        /// <summary>
        /// Reduces the faction's troop count (e.g., due to combat losses).
        /// Troops are removed from the reserve pool in tier order: Basic first, then Medium, then Heavy.
        /// The troop count will not go below zero.
        /// </summary>
        /// <param name="count">The number of troops lost (must be non-negative).</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if count is negative.</exception>
        public void LoseTroops(int count)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), "Count cannot be negative.");

            var remaining = count;
            var tierOrder = new[] { DefenderRole.Grunt, DefenderRole.Gunner, DefenderRole.Rifleman };

            foreach (var tier in tierOrder)
            {
                if (remaining <= 0)
                    break;

                var available = _reservePool.TryGetValue(tier, out var current) ? current : 0;
                var toRemove = Math.Min(remaining, available);
                _reservePool[tier] = available - toRemove;
                remaining -= toRemove;
            }
        }

        /// <summary>
        /// Checks if the faction has at least a specified number of troops.
        /// </summary>
        /// <param name="count">The minimum troop count to check for.</param>
        /// <returns>True if the faction has enough troops, false otherwise.</returns>
        public bool HasTroops(int count)
        {
            return TotalReserveTroops >= count;
        }

        #endregion

        #region Reserve Pool Operations

        /// <summary>
        /// Total number of troops across all tiers in the reserve pool.
        /// </summary>
        public int TotalReserveTroops => _reservePool.Values.Sum();

        /// <summary>
        /// Gets the number of reserve troops for a specific tier.
        /// </summary>
        /// <param name="tier">The defender tier to query.</param>
        /// <returns>The number of troops in that tier's reserve.</returns>
        public int GetReserveTroops(DefenderRole tier)
        {
            return _reservePool.TryGetValue(tier, out var count) ? count : 0;
        }

        /// <summary>
        /// Adds troops to the reserve pool for a specific tier.
        /// </summary>
        /// <param name="tier">The defender tier to add to.</param>
        /// <param name="count">The number of troops to add (must be non-negative).</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if count is negative.</exception>
        public void AddReserveTroops(DefenderRole tier, int count)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), "Count cannot be negative.");

            if (!_reservePool.ContainsKey(tier))
                _reservePool[tier] = 0;

            _reservePool[tier] += count;
        }

        /// <summary>
        /// Removes troops from the reserve pool for a specific tier.
        /// </summary>
        /// <param name="tier">The defender tier to remove from.</param>
        /// <param name="count">The number of troops to remove (must be non-negative).</param>
        /// <returns>True if the troops were removed, false if insufficient troops.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if count is negative.</exception>
        public bool RemoveReserveTroops(DefenderRole tier, int count)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), "Count cannot be negative.");

            if (!_reservePool.TryGetValue(tier, out var current) || current < count)
                return false;

            _reservePool[tier] = current - count;
            return true;
        }

        /// <summary>
        /// Checks if the reserve pool has at least a specified number of troops for a tier.
        /// </summary>
        /// <param name="tier">The defender tier to check.</param>
        /// <param name="count">The minimum troop count to check for.</param>
        /// <returns>True if the tier has enough troops, false otherwise.</returns>
        public bool HasReserveTroops(DefenderRole tier, int count)
        {
            return _reservePool.TryGetValue(tier, out var current) && current >= count;
        }

        /// <summary>
        /// Returns a copy of the reserve pool dictionary.
        /// Modifying the returned dictionary will not affect the internal state.
        /// </summary>
        /// <returns>A new dictionary with the reserve pool counts by tier.</returns>
        public Dictionary<DefenderRole, int> GetReservePoolCopy()
        {
            return new Dictionary<DefenderRole, int>(_reservePool);
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
            return $"FactionState[{FactionId}]: Cash={_cash}, Troops={TotalReserveTroops}, Zones={ZoneCount}";
        }
    }
}
