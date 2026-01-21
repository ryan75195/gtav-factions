using System;
using System.Collections.Generic;
using System.Linq;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.Factions.Models;

namespace FactionWars.Core.Services
{
    /// <summary>
    /// Service for managing the allocation of defender troops from a faction's
    /// reserve pool to specific zones.
    /// </summary>
    public class ZoneDefenderAllocationService : IZoneDefenderAllocationService
    {
        private readonly IZoneDefenderAllocationRepository _repository;

        /// <inheritdoc />
        public event EventHandler<TroopsAllocatedEventArgs>? TroopsAllocated;

        /// <summary>
        /// Creates a new zone defender allocation service.
        /// </summary>
        /// <param name="repository">The repository for storing allocations.</param>
        /// <exception cref="ArgumentNullException">Thrown if repository is null.</exception>
        public ZoneDefenderAllocationService(IZoneDefenderAllocationRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        /// <inheritdoc />
        public bool AllocateTroops(FactionState factionState, string zoneId, DefenderTier tier, int count)
        {
            if (factionState == null)
                throw new ArgumentNullException(nameof(factionState));
            if (zoneId == null)
                throw new ArgumentNullException(nameof(zoneId));
            if (string.IsNullOrWhiteSpace(zoneId))
                throw new ArgumentException("Zone ID cannot be empty or whitespace.", nameof(zoneId));
            if (count <= 0)
                throw new ArgumentOutOfRangeException(nameof(count), "Count must be greater than zero.");

            // Check if faction has enough reserve troops
            if (!factionState.HasReserveTroops(tier, count))
                return false;

            // Deduct from reserve
            if (!factionState.RemoveReserveTroops(tier, count))
                return false;

            // Get or create allocation
            var allocation = _repository.Get(factionState.FactionId, zoneId);
            if (allocation == null)
            {
                allocation = new ZoneDefenderAllocation(factionState.FactionId, zoneId);
                allocation.AddTroops(tier, count);
                _repository.Add(allocation);
            }
            else
            {
                allocation.AddTroops(tier, count);
                _repository.Update(allocation);
            }

            // Raise event so listeners can spawn defenders immediately if player is in zone
            TroopsAllocated?.Invoke(this, new TroopsAllocatedEventArgs(factionState.FactionId, zoneId, tier, count));

            return true;
        }

        /// <inheritdoc />
        public ZoneDefenderAllocation? GetAllocation(string factionId, string zoneId)
        {
            if (factionId == null)
                throw new ArgumentNullException(nameof(factionId));
            if (zoneId == null)
                throw new ArgumentNullException(nameof(zoneId));

            return _repository.Get(factionId, zoneId);
        }

        /// <inheritdoc />
        public IReadOnlyList<ZoneDefenderAllocation> GetAllocationsForFaction(string factionId)
        {
            if (factionId == null)
                throw new ArgumentNullException(nameof(factionId));

            return _repository.GetByFaction(factionId);
        }

        /// <inheritdoc />
        public int GetTotalAllocatedTroops(string factionId)
        {
            if (factionId == null)
                throw new ArgumentNullException(nameof(factionId));

            var allocations = _repository.GetByFaction(factionId);
            return allocations.Sum(a => a.TotalTroops);
        }

        /// <inheritdoc />
        public bool WithdrawTroops(FactionState factionState, string zoneId, DefenderTier tier, int count)
        {
            if (factionState == null)
                throw new ArgumentNullException(nameof(factionState));
            if (zoneId == null)
                throw new ArgumentNullException(nameof(zoneId));
            if (string.IsNullOrWhiteSpace(zoneId))
                throw new ArgumentException("Zone ID cannot be empty or whitespace.", nameof(zoneId));
            if (count <= 0)
                throw new ArgumentOutOfRangeException(nameof(count), "Count must be greater than zero.");

            // Get the allocation for this zone
            var allocation = _repository.Get(factionState.FactionId, zoneId);
            if (allocation == null)
                return false;

            // Check if allocation has enough troops of this tier
            if (!allocation.HasTroops(tier, count))
                return false;

            // Remove from allocation
            if (!allocation.RemoveTroops(tier, count))
                return false;

            // Add back to faction's reserve pool
            factionState.AddReserveTroops(tier, count);

            // Update the repository
            _repository.Update(allocation);

            return true;
        }

        /// <inheritdoc />
        public void SetAllocation(string factionId, string zoneId, DefenderTier tier, int count)
        {
            if (factionId == null)
                throw new ArgumentNullException(nameof(factionId));
            if (string.IsNullOrWhiteSpace(factionId))
                throw new ArgumentException("Faction ID cannot be empty or whitespace.", nameof(factionId));
            if (zoneId == null)
                throw new ArgumentNullException(nameof(zoneId));
            if (string.IsNullOrWhiteSpace(zoneId))
                throw new ArgumentException("Zone ID cannot be empty or whitespace.", nameof(zoneId));
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), "Count cannot be negative.");

            // Get or create allocation
            var allocation = _repository.Get(factionId, zoneId);
            if (allocation == null)
            {
                allocation = new ZoneDefenderAllocation(factionId, zoneId);
                if (count > 0)
                {
                    allocation.AddTroops(tier, count);
                }
                _repository.Add(allocation);
            }
            else
            {
                // Clear existing troops of this tier and set new count
                var existingCount = allocation.GetTroopCount(tier);
                if (existingCount > 0)
                {
                    allocation.RemoveTroops(tier, existingCount);
                }
                if (count > 0)
                {
                    allocation.AddTroops(tier, count);
                }
                _repository.Update(allocation);
            }
        }
    }
}
