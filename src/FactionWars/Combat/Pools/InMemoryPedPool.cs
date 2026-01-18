using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace FactionWars.Combat.Pools
{
    /// <summary>
    /// Thread-safe in-memory implementation of IPedPool.
    /// Uses a ConcurrentDictionary for thread-safe operations.
    /// </summary>
    public class InMemoryPedPool : IPedPool
    {
        private readonly ConcurrentDictionary<int, PedHandle> _peds = new ConcurrentDictionary<int, PedHandle>();
        private readonly int _maxCapacity;

        /// <summary>
        /// Creates a new InMemoryPedPool with the specified maximum capacity.
        /// </summary>
        /// <param name="maxCapacity">The maximum number of peds allowed in the pool.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if maxCapacity is less than 1.</exception>
        public InMemoryPedPool(int maxCapacity = 30)
        {
            if (maxCapacity < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(maxCapacity), "Max capacity must be at least 1.");
            }

            _maxCapacity = maxCapacity;
        }

        /// <inheritdoc />
        public int Count => _peds.Count;

        /// <inheritdoc />
        public int MaxCapacity => _maxCapacity;

        /// <inheritdoc />
        public bool IsFull => _peds.Count >= _maxCapacity;

        /// <inheritdoc />
        public int AvailableSlots => Math.Max(0, _maxCapacity - _peds.Count);

        /// <inheritdoc />
        public bool Add(PedHandle ped)
        {
            if (ped == null)
            {
                throw new ArgumentNullException(nameof(ped));
            }

            if (!ped.IsValid)
            {
                return false;
            }

            if (IsFull)
            {
                return false;
            }

            return _peds.TryAdd(ped.Handle, ped);
        }

        /// <inheritdoc />
        public bool Contains(int handle)
        {
            return _peds.ContainsKey(handle);
        }

        /// <inheritdoc />
        public bool Contains(PedHandle ped)
        {
            if (ped == null)
            {
                return false;
            }

            return _peds.ContainsKey(ped.Handle);
        }

        /// <inheritdoc />
        public PedHandle? GetByHandle(int handle)
        {
            return _peds.TryGetValue(handle, out var ped) ? ped : null;
        }

        /// <inheritdoc />
        public IEnumerable<PedHandle> GetAll()
        {
            return _peds.Values.ToList();
        }

        /// <inheritdoc />
        public IEnumerable<PedHandle> GetByFaction(string factionId)
        {
            if (factionId == null)
            {
                throw new ArgumentNullException(nameof(factionId));
            }

            return _peds.Values.Where(p => p.FactionId == factionId).ToList();
        }

        /// <inheritdoc />
        public int GetFactionCount(string factionId)
        {
            if (factionId == null)
            {
                return 0;
            }

            return _peds.Values.Count(p => p.FactionId == factionId);
        }

        /// <inheritdoc />
        public IEnumerable<PedHandle> GetByZone(string zoneId)
        {
            if (zoneId == null)
            {
                throw new ArgumentNullException(nameof(zoneId));
            }

            return _peds.Values.Where(p => p.ZoneId == zoneId).ToList();
        }

        /// <inheritdoc />
        public int GetZoneCount(string zoneId)
        {
            if (zoneId == null)
            {
                return 0;
            }

            return _peds.Values.Count(p => p.ZoneId == zoneId);
        }

        /// <inheritdoc />
        public IEnumerable<PedHandle> GetByFactionAndZone(string factionId, string zoneId)
        {
            return _peds.Values
                .Where(p => p.FactionId == factionId && p.ZoneId == zoneId)
                .ToList();
        }

        /// <inheritdoc />
        public bool Remove(int handle)
        {
            return _peds.TryRemove(handle, out _);
        }

        /// <inheritdoc />
        public bool Remove(PedHandle ped)
        {
            if (ped == null)
            {
                return false;
            }

            return _peds.TryRemove(ped.Handle, out _);
        }

        /// <inheritdoc />
        public IEnumerable<PedHandle> Clear()
        {
            var removed = _peds.Values.ToList();
            _peds.Clear();
            return removed;
        }

        /// <inheritdoc />
        public IEnumerable<PedHandle> GetMarkedForDeletion()
        {
            return _peds.Values.Where(p => p.IsMarkedForDeletion).ToList();
        }

        /// <inheritdoc />
        public IEnumerable<PedHandle> RemoveMarkedForDeletion()
        {
            var markedPeds = _peds.Values.Where(p => p.IsMarkedForDeletion).ToList();

            foreach (var ped in markedPeds)
            {
                _peds.TryRemove(ped.Handle, out _);
            }

            return markedPeds;
        }

        /// <inheritdoc />
        public IEnumerable<PedHandle> GetOldest(int count)
        {
            if (count <= 0)
            {
                return Enumerable.Empty<PedHandle>();
            }

            return _peds.Values
                .OrderBy(p => p.CreatedAt)
                .Take(count)
                .ToList();
        }
    }
}
