using System;
using System.Collections.Generic;
using System.Linq;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;

namespace FactionWars.Core.Services
{
    /// <summary>
    /// In-memory implementation of the zone defender allocation repository.
    /// </summary>
    public class InMemoryZoneDefenderAllocationRepository : IZoneDefenderAllocationRepository
    {
        private readonly Dictionary<string, ZoneDefenderAllocation> _allocations = new Dictionary<string, ZoneDefenderAllocation>();

        private static string GetKey(string factionId, string zoneId)
        {
            return $"{factionId}:{zoneId}";
        }

        /// <inheritdoc />
        public void Add(ZoneDefenderAllocation allocation)
        {
            if (allocation == null)
                throw new ArgumentNullException(nameof(allocation));

            var key = GetKey(allocation.FactionId, allocation.ZoneId);
            if (_allocations.ContainsKey(key))
                throw new InvalidOperationException($"Allocation for faction '{allocation.FactionId}' and zone '{allocation.ZoneId}' already exists.");

            _allocations[key] = allocation;
        }

        /// <inheritdoc />
        public void Update(ZoneDefenderAllocation allocation)
        {
            if (allocation == null)
                throw new ArgumentNullException(nameof(allocation));

            var key = GetKey(allocation.FactionId, allocation.ZoneId);
            if (!_allocations.ContainsKey(key))
                throw new InvalidOperationException($"Allocation for faction '{allocation.FactionId}' and zone '{allocation.ZoneId}' does not exist.");

            _allocations[key] = allocation;
        }

        /// <inheritdoc />
        public ZoneDefenderAllocation? Get(string factionId, string zoneId)
        {
            if (factionId == null)
                throw new ArgumentNullException(nameof(factionId));
            if (zoneId == null)
                throw new ArgumentNullException(nameof(zoneId));

            var key = GetKey(factionId, zoneId);
            return _allocations.TryGetValue(key, out var allocation) ? allocation : null;
        }

        /// <inheritdoc />
        public IReadOnlyList<ZoneDefenderAllocation> GetByFaction(string factionId)
        {
            if (factionId == null)
                throw new ArgumentNullException(nameof(factionId));

            return _allocations.Values
                .Where(a => a.FactionId == factionId)
                .ToList();
        }

        /// <inheritdoc />
        public bool Remove(string factionId, string zoneId)
        {
            if (factionId == null)
                throw new ArgumentNullException(nameof(factionId));
            if (zoneId == null)
                throw new ArgumentNullException(nameof(zoneId));

            var key = GetKey(factionId, zoneId);
            return _allocations.Remove(key);
        }

        /// <inheritdoc />
        public IReadOnlyList<ZoneDefenderAllocation> GetAll()
        {
            return _allocations.Values.ToList();
        }

        /// <inheritdoc />
        public void Clear()
        {
            _allocations.Clear();
        }
    }
}
