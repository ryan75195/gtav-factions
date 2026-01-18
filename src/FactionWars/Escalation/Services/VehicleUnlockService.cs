using System;
using System.Collections.Generic;
using System.Linq;
using FactionWars.Escalation.Interfaces;
using FactionWars.Escalation.Models;

namespace FactionWars.Escalation.Services
{
    /// <summary>
    /// Service for managing vehicle unlocks based on faction escalation tiers.
    /// Provides business logic for querying and managing available vehicles.
    /// </summary>
    public class VehicleUnlockService : IVehicleUnlockService
    {
        private readonly IVehicleUnlockRepository _repository;
        private readonly IEscalationService _escalationService;
        private readonly Random _random;

        /// <summary>
        /// Creates a new vehicle unlock service.
        /// </summary>
        /// <param name="repository">The vehicle unlock repository.</param>
        /// <param name="escalationService">The escalation service for tier lookups.</param>
        /// <exception cref="ArgumentNullException">Thrown if any dependency is null.</exception>
        public VehicleUnlockService(IVehicleUnlockRepository repository, IEscalationService escalationService)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _escalationService = escalationService ?? throw new ArgumentNullException(nameof(escalationService));
            _random = new Random();
        }

        /// <inheritdoc />
        public IEnumerable<VehicleUnlock> GetAvailableVehicles(string factionId)
        {
            ValidateFactionId(factionId);
            var tier = _escalationService.GetCurrentTier(factionId);
            return _repository.GetUnlockedAtTier(tier);
        }

        /// <inheritdoc />
        public IEnumerable<VehicleUnlock> GetAvailableVehiclesByCategory(string factionId, VehicleCategory category)
        {
            ValidateFactionId(factionId);
            var tier = _escalationService.GetCurrentTier(factionId);
            return _repository.GetUnlockedAtTierByCategory(tier, category);
        }

        /// <inheritdoc />
        public bool IsVehicleAvailable(string factionId, string vehicleModel)
        {
            ValidateFactionId(factionId);
            if (vehicleModel == null)
                throw new ArgumentNullException(nameof(vehicleModel));

            var vehicle = _repository.GetByModel(vehicleModel);
            if (vehicle == null)
                return false;

            var tier = _escalationService.GetCurrentTier(factionId);
            return vehicle.IsUnlockedAtTier(tier);
        }

        /// <inheritdoc />
        public IEnumerable<VehicleUnlock> GetNewlyUnlockedVehicles(EscalationTier previousTier, EscalationTier newTier)
        {
            if (newTier <= previousTier)
                return Enumerable.Empty<VehicleUnlock>();

            var result = new List<VehicleUnlock>();
            for (var tier = previousTier + 1; tier <= newTier; tier++)
            {
                result.AddRange(_repository.GetByTier(tier));
            }
            return result;
        }

        /// <inheritdoc />
        public VehicleUnlock? GetRandomVehicleForTier(EscalationTier tier)
        {
            var vehicles = _repository.GetUnlockedAtTier(tier).ToList();
            if (vehicles.Count == 0)
                return null;

            return vehicles[_random.Next(vehicles.Count)];
        }

        /// <inheritdoc />
        public VehicleUnlock? GetRandomVehicleForFaction(string factionId)
        {
            ValidateFactionId(factionId);
            var tier = _escalationService.GetCurrentTier(factionId);
            return GetRandomVehicleForTier(tier);
        }

        /// <inheritdoc />
        public VehicleUnlock? GetRandomVehicleForFactionByCategory(string factionId, VehicleCategory category)
        {
            ValidateFactionId(factionId);
            var tier = _escalationService.GetCurrentTier(factionId);
            var vehicles = _repository.GetUnlockedAtTierByCategory(tier, category).ToList();

            if (vehicles.Count == 0)
                return null;

            return vehicles[_random.Next(vehicles.Count)];
        }

        /// <inheritdoc />
        public bool RegisterVehicle(VehicleUnlock vehicle)
        {
            if (vehicle == null)
                throw new ArgumentNullException(nameof(vehicle));

            return _repository.Add(vehicle);
        }

        /// <inheritdoc />
        public bool UnregisterVehicle(string vehicleModel)
        {
            if (vehicleModel == null)
                throw new ArgumentNullException(nameof(vehicleModel));

            return _repository.Remove(vehicleModel);
        }

        /// <inheritdoc />
        public IEnumerable<VehicleUnlock> GetAllRegisteredVehicles()
        {
            return _repository.GetAll();
        }

        /// <inheritdoc />
        public VehicleUnlock? GetVehicleInfo(string vehicleModel)
        {
            if (vehicleModel == null)
                throw new ArgumentNullException(nameof(vehicleModel));

            return _repository.GetByModel(vehicleModel);
        }

        /// <inheritdoc />
        public IEnumerable<VehicleUnlock> GetVehiclesNewlyUnlockedForFaction(string factionId)
        {
            ValidateFactionId(factionId);

            var escalation = _escalationService.GetEscalation(factionId);
            if (escalation == null)
                return Enumerable.Empty<VehicleUnlock>();

            return GetNewlyUnlockedVehicles(escalation.PreviousTier, escalation.CurrentTier);
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
