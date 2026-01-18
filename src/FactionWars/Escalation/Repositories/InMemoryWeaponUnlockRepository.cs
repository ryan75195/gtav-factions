using System;
using System.Collections.Generic;
using System.Linq;
using FactionWars.Escalation.Interfaces;
using FactionWars.Escalation.Models;

namespace FactionWars.Escalation.Repositories
{
    /// <summary>
    /// In-memory implementation of the weapon unlock repository.
    /// Stores weapon definitions in a dictionary keyed by weapon hash.
    /// </summary>
    public class InMemoryWeaponUnlockRepository : IWeaponUnlockRepository
    {
        private readonly Dictionary<string, WeaponUnlock> _weapons;

        /// <summary>
        /// Creates a new empty weapon unlock repository.
        /// </summary>
        public InMemoryWeaponUnlockRepository()
        {
            _weapons = new Dictionary<string, WeaponUnlock>();
        }

        /// <inheritdoc />
        public int Count => _weapons.Count;

        /// <inheritdoc />
        public bool Add(WeaponUnlock weapon)
        {
            if (weapon == null)
                throw new ArgumentNullException(nameof(weapon));

            if (_weapons.ContainsKey(weapon.WeaponHash))
                return false;

            _weapons[weapon.WeaponHash] = weapon;
            return true;
        }

        /// <inheritdoc />
        public bool Remove(string weaponHash)
        {
            if (weaponHash == null)
                throw new ArgumentNullException(nameof(weaponHash));
            if (string.IsNullOrWhiteSpace(weaponHash))
                throw new ArgumentException("Weapon hash cannot be empty or whitespace.", nameof(weaponHash));

            return _weapons.Remove(weaponHash);
        }

        /// <inheritdoc />
        public WeaponUnlock? GetByHash(string weaponHash)
        {
            if (weaponHash == null)
                throw new ArgumentNullException(nameof(weaponHash));
            if (string.IsNullOrWhiteSpace(weaponHash))
                throw new ArgumentException("Weapon hash cannot be empty or whitespace.", nameof(weaponHash));

            return _weapons.TryGetValue(weaponHash, out var weapon) ? weapon : null;
        }

        /// <inheritdoc />
        public IEnumerable<WeaponUnlock> GetByTier(EscalationTier tier)
        {
            return _weapons.Values.Where(w => w.RequiredTier == tier);
        }

        /// <inheritdoc />
        public IEnumerable<WeaponUnlock> GetByCategory(WeaponCategory category)
        {
            return _weapons.Values.Where(w => w.Category == category);
        }

        /// <inheritdoc />
        public IEnumerable<WeaponUnlock> GetAll()
        {
            return _weapons.Values;
        }

        /// <inheritdoc />
        public IEnumerable<WeaponUnlock> GetUnlockedAtTier(EscalationTier tier)
        {
            return _weapons.Values.Where(w => w.IsUnlockedAtTier(tier));
        }

        /// <inheritdoc />
        public IEnumerable<WeaponUnlock> GetUnlockedAtTierByCategory(EscalationTier tier, WeaponCategory category)
        {
            return _weapons.Values.Where(w => w.IsUnlockedAtTier(tier) && w.Category == category);
        }

        /// <inheritdoc />
        public bool Exists(string weaponHash)
        {
            if (weaponHash == null)
                throw new ArgumentNullException(nameof(weaponHash));

            return _weapons.ContainsKey(weaponHash);
        }

        /// <inheritdoc />
        public void Clear()
        {
            _weapons.Clear();
        }
    }
}
