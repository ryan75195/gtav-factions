using System;
using System.Collections.Generic;
using System.Linq;
using FactionWars.Escalation.Interfaces;
using FactionWars.Escalation.Models;

namespace FactionWars.Escalation.Repositories
{
    /// <summary>
    /// In-memory implementation of the vehicle unlock repository.
    /// Stores vehicle definitions in a dictionary keyed by vehicle model.
    /// </summary>
    public class InMemoryVehicleUnlockRepository : IVehicleUnlockRepository
    {
        private readonly Dictionary<string, VehicleUnlock> _vehicles;

        /// <summary>
        /// Creates a new empty vehicle unlock repository.
        /// </summary>
        public InMemoryVehicleUnlockRepository()
        {
            _vehicles = new Dictionary<string, VehicleUnlock>();
        }

        /// <inheritdoc />
        public int Count => _vehicles.Count;

        /// <inheritdoc />
        public bool Add(VehicleUnlock vehicle)
        {
            if (vehicle == null)
                throw new ArgumentNullException(nameof(vehicle));

            if (_vehicles.ContainsKey(vehicle.VehicleModel))
                return false;

            _vehicles[vehicle.VehicleModel] = vehicle;
            return true;
        }

        /// <inheritdoc />
        public bool Remove(string vehicleModel)
        {
            if (vehicleModel == null)
                throw new ArgumentNullException(nameof(vehicleModel));
            if (string.IsNullOrWhiteSpace(vehicleModel))
                throw new ArgumentException("Vehicle model cannot be empty or whitespace.", nameof(vehicleModel));

            return _vehicles.Remove(vehicleModel);
        }

        /// <inheritdoc />
        public VehicleUnlock? GetByModel(string vehicleModel)
        {
            if (vehicleModel == null)
                throw new ArgumentNullException(nameof(vehicleModel));
            if (string.IsNullOrWhiteSpace(vehicleModel))
                throw new ArgumentException("Vehicle model cannot be empty or whitespace.", nameof(vehicleModel));

            return _vehicles.TryGetValue(vehicleModel, out var vehicle) ? vehicle : null;
        }

        /// <inheritdoc />
        public IEnumerable<VehicleUnlock> GetByTier(EscalationTier tier)
        {
            return _vehicles.Values.Where(v => v.RequiredTier == tier);
        }

        /// <inheritdoc />
        public IEnumerable<VehicleUnlock> GetByCategory(VehicleCategory category)
        {
            return _vehicles.Values.Where(v => v.Category == category);
        }

        /// <inheritdoc />
        public IEnumerable<VehicleUnlock> GetAll()
        {
            return _vehicles.Values;
        }

        /// <inheritdoc />
        public IEnumerable<VehicleUnlock> GetUnlockedAtTier(EscalationTier tier)
        {
            return _vehicles.Values.Where(v => v.IsUnlockedAtTier(tier));
        }

        /// <inheritdoc />
        public IEnumerable<VehicleUnlock> GetUnlockedAtTierByCategory(EscalationTier tier, VehicleCategory category)
        {
            return _vehicles.Values.Where(v => v.IsUnlockedAtTier(tier) && v.Category == category);
        }

        /// <inheritdoc />
        public bool Exists(string vehicleModel)
        {
            if (vehicleModel == null)
                throw new ArgumentNullException(nameof(vehicleModel));

            return _vehicles.ContainsKey(vehicleModel);
        }

        /// <inheritdoc />
        public void Clear()
        {
            _vehicles.Clear();
        }
    }
}
