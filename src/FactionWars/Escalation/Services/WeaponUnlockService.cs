using System;
using System.Collections.Generic;
using System.Linq;
using FactionWars.Escalation.Interfaces;
using FactionWars.Escalation.Models;

namespace FactionWars.Escalation.Services
{
    /// <summary>
    /// Service for managing weapon unlocks based on faction escalation tiers.
    /// Provides business logic for querying and managing available weapons.
    /// </summary>
    public class WeaponUnlockService : IWeaponUnlockService
    {
        private readonly IWeaponUnlockRepository _repository;
        private readonly IEscalationService _escalationService;
        private readonly Random _random;

        /// <summary>
        /// Creates a new weapon unlock service.
        /// </summary>
        /// <param name="repository">The weapon unlock repository.</param>
        /// <param name="escalationService">The escalation service for tier lookups.</param>
        /// <exception cref="ArgumentNullException">Thrown if any dependency is null.</exception>
        public WeaponUnlockService(IWeaponUnlockRepository repository, IEscalationService escalationService)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _escalationService = escalationService ?? throw new ArgumentNullException(nameof(escalationService));
            _random = new Random();
        }

        /// <inheritdoc />
        public IEnumerable<WeaponUnlock> GetAvailableWeapons(string factionId)
        {
            ValidateFactionId(factionId);
            var tier = _escalationService.GetCurrentTier(factionId);
            return _repository.GetUnlockedAtTier(tier);
        }

        /// <inheritdoc />
        public IEnumerable<WeaponUnlock> GetAvailableWeaponsByCategory(string factionId, WeaponCategory category)
        {
            ValidateFactionId(factionId);
            var tier = _escalationService.GetCurrentTier(factionId);
            return _repository.GetUnlockedAtTierByCategory(tier, category);
        }

        /// <inheritdoc />
        public bool IsWeaponAvailable(string factionId, string weaponHash)
        {
            ValidateFactionId(factionId);
            if (weaponHash == null)
                throw new ArgumentNullException(nameof(weaponHash));

            var weapon = _repository.GetByHash(weaponHash);
            if (weapon == null)
                return false;

            var tier = _escalationService.GetCurrentTier(factionId);
            return weapon.IsUnlockedAtTier(tier);
        }

        /// <inheritdoc />
        public IEnumerable<WeaponUnlock> GetNewlyUnlockedWeapons(EscalationTier previousTier, EscalationTier newTier)
        {
            if (newTier <= previousTier)
                return Enumerable.Empty<WeaponUnlock>();

            var result = new List<WeaponUnlock>();
            for (var tier = previousTier + 1; tier <= newTier; tier++)
            {
                result.AddRange(_repository.GetByTier(tier));
            }
            return result;
        }

        /// <inheritdoc />
        public WeaponUnlock? GetRandomWeaponForTier(EscalationTier tier)
        {
            var weapons = _repository.GetUnlockedAtTier(tier).ToList();
            if (weapons.Count == 0)
                return null;

            return weapons[_random.Next(weapons.Count)];
        }

        /// <inheritdoc />
        public WeaponUnlock? GetRandomWeaponForFaction(string factionId)
        {
            ValidateFactionId(factionId);
            var tier = _escalationService.GetCurrentTier(factionId);
            return GetRandomWeaponForTier(tier);
        }

        /// <inheritdoc />
        public WeaponUnlock? GetRandomWeaponForFactionByCategory(string factionId, WeaponCategory category)
        {
            ValidateFactionId(factionId);
            var tier = _escalationService.GetCurrentTier(factionId);
            var weapons = _repository.GetUnlockedAtTierByCategory(tier, category).ToList();

            if (weapons.Count == 0)
                return null;

            return weapons[_random.Next(weapons.Count)];
        }

        /// <inheritdoc />
        public bool RegisterWeapon(WeaponUnlock weapon)
        {
            if (weapon == null)
                throw new ArgumentNullException(nameof(weapon));

            return _repository.Add(weapon);
        }

        /// <inheritdoc />
        public bool UnregisterWeapon(string weaponHash)
        {
            if (weaponHash == null)
                throw new ArgumentNullException(nameof(weaponHash));

            return _repository.Remove(weaponHash);
        }

        /// <inheritdoc />
        public IEnumerable<WeaponUnlock> GetAllRegisteredWeapons()
        {
            return _repository.GetAll();
        }

        /// <inheritdoc />
        public WeaponUnlock? GetWeaponInfo(string weaponHash)
        {
            if (weaponHash == null)
                throw new ArgumentNullException(nameof(weaponHash));

            return _repository.GetByHash(weaponHash);
        }

        /// <inheritdoc />
        public IEnumerable<WeaponUnlock> GetWeaponsNewlyUnlockedForFaction(string factionId)
        {
            ValidateFactionId(factionId);

            var escalation = _escalationService.GetEscalation(factionId);
            if (escalation == null)
                return Enumerable.Empty<WeaponUnlock>();

            return GetNewlyUnlockedWeapons(escalation.PreviousTier, escalation.CurrentTier);
        }

        private static void ValidateFactionId(string factionId)
        {
            if (factionId == null)
                throw new ArgumentNullException(nameof(factionId));
            if (string.IsNullOrWhiteSpace(factionId))
                throw new ArgumentException("Faction ID cannot be empty or whitespace.", nameof(factionId));
        }
    }
}
