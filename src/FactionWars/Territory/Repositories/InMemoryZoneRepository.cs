using FactionWars.Territory.Interfaces;
using FactionWars.Territory.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FactionWars.Territory.Repositories
{
    /// <summary>
    /// In-memory implementation of IZoneRepository for testing and runtime use.
    /// Thread-safe for basic operations.
    /// </summary>
    public class InMemoryZoneRepository : IZoneRepository
    {
        private readonly Dictionary<string, Zone> _zones = new Dictionary<string, Zone>();
        private readonly object _lock = new object();

        /// <inheritdoc />
        public int Count
        {
            get
            {
                lock (_lock)
                {
                    return _zones.Count;
                }
            }
        }

        /// <inheritdoc />
        public void Add(Zone zone)
        {
            if (zone == null)
                throw new ArgumentNullException(nameof(zone));

            lock (_lock)
            {
                if (_zones.ContainsKey(zone.Id))
                    throw new InvalidOperationException($"A zone with ID '{zone.Id}' already exists.");

                _zones[zone.Id] = zone;
            }
        }

        /// <inheritdoc />
        public Zone? GetById(string id)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Id cannot be empty or whitespace.", nameof(id));

            lock (_lock)
            {
                return _zones.TryGetValue(id, out var zone) ? zone : null;
            }
        }

        /// <inheritdoc />
        public IEnumerable<Zone> GetAll()
        {
            lock (_lock)
            {
                return _zones.Values.ToList();
            }
        }

        /// <inheritdoc />
        public void Update(Zone zone)
        {
            if (zone == null)
                throw new ArgumentNullException(nameof(zone));

            lock (_lock)
            {
                if (!_zones.ContainsKey(zone.Id))
                    throw new InvalidOperationException($"Zone with ID '{zone.Id}' does not exist.");

                _zones[zone.Id] = zone;
            }
        }

        /// <inheritdoc />
        public bool Remove(string id)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));

            lock (_lock)
            {
                return _zones.Remove(id);
            }
        }

        /// <inheritdoc />
        public IEnumerable<Zone> GetByOwner(string? ownerFactionId)
        {
            lock (_lock)
            {
                return _zones.Values
                    .Where(z => z.OwnerFactionId == ownerFactionId)
                    .ToList();
            }
        }

        /// <inheritdoc />
        public IEnumerable<Zone> GetContested()
        {
            lock (_lock)
            {
                return _zones.Values
                    .Where(z => z.IsContested)
                    .ToList();
            }
        }

        /// <inheritdoc />
        public bool Contains(string id)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));

            lock (_lock)
            {
                return _zones.ContainsKey(id);
            }
        }

        /// <inheritdoc />
        public void Clear()
        {
            lock (_lock)
            {
                _zones.Clear();
            }
        }
    }
}
