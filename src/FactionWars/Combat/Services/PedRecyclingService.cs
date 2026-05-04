using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Models;
using FactionWars.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FactionWars.Combat.Services
{
    /// <summary>
    /// Service for recycling peds instead of destroying and recreating them.
    /// Recycling repurposes existing game entities with new faction/zone assignments,
    /// which is more efficient than full delete/create cycles.
    /// </summary>
    public class PedRecyclingService : IPedRecyclingService
    {
        private readonly IGameBridge _gameBridge;
        private readonly IPedPool _pedPool;

        /// <summary>
        /// Creates a new PedRecyclingService.
        /// </summary>
        /// <param name="gameBridge">The game bridge for manipulating peds in the game world.</param>
        /// <param name="pedPool">The ped pool for tracking peds.</param>
        /// <exception cref="ArgumentNullException">Thrown if gameBridge or pedPool is null.</exception>
        public PedRecyclingService(IGameBridge gameBridge, IPedPool pedPool)
        {
            _gameBridge = gameBridge ?? throw new ArgumentNullException(nameof(gameBridge));
            _pedPool = pedPool ?? throw new ArgumentNullException(nameof(pedPool));
        }

        /// <inheritdoc />
        public IEnumerable<PedHandle> GetRecyclablePeds()
        {
            return _pedPool.GetAll()
                .Where(p => IsRecyclable(p))
                .ToList();
        }

        /// <inheritdoc />
        public int GetRecyclableCount()
        {
            return _pedPool.GetAll().Count(p => IsRecyclable(p));
        }

        /// <inheritdoc />
        public bool HasRecyclablePeds()
        {
            return _pedPool.GetAll().Any(p => IsRecyclable(p));
        }

        /// <inheritdoc />
        public PedHandle? GetNextRecyclableCandidate()
        {
            // Prefer dead peds first (they're already not moving/fighting)
            var deadPed = _pedPool.GetAll()
                .FirstOrDefault(p => !_gameBridge.IsPedAlive(p.Handle) && !p.IsRecycled);

            if (deadPed != null)
            {
                return deadPed;
            }

            // Then try peds marked for deletion
            return _pedPool.GetAll()
                .FirstOrDefault(p => p.IsMarkedForDeletion && !p.IsRecycled);
        }

        /// <inheritdoc />
        public PedHandle? RecyclePed(PedHandle pedHandle, string newFactionId, Vector3 newPosition, string? newZoneId)
        {
            var poolPed = GetRecyclablePoolPed(pedHandle, newFactionId);
            if (poolPed == null)
                return null;

            // Revive the ped if dead
            if (!_gameBridge.IsPedAlive(pedHandle.Handle))
            {
                _gameBridge.RevivePed(pedHandle.Handle);
            }

            // Move the ped to the new position
            _gameBridge.SetPedPosition(pedHandle.Handle, newPosition);

            // Change the relationship group to the new faction
            string relationshipGroup = GetRelationshipGroup(newFactionId);
            _gameBridge.SetPedRelationshipGroup(pedHandle.Handle, relationshipGroup);

            // Create a new PedHandle with updated metadata
            var newPedHandle = new PedHandle(
                pedHandle.Handle,
                factionId: newFactionId,
                spawnPosition: newPosition,
                modelName: poolPed.ModelName,
                zoneId: newZoneId);

            // Remove the old ped from the pool and add the new one
            _pedPool.Remove(pedHandle.Handle);
            _pedPool.Add(newPedHandle);

            return newPedHandle;
        }

        private PedHandle? GetRecyclablePoolPed(PedHandle pedHandle, string newFactionId)
        {
            if (pedHandle == null)
            {
                throw new ArgumentNullException(nameof(pedHandle));
            }

            if (newFactionId == null)
            {
                throw new ArgumentNullException(nameof(newFactionId));
            }

            if (string.IsNullOrEmpty(newFactionId))
            {
                throw new ArgumentException("Faction ID cannot be empty.", nameof(newFactionId));
            }

            if (!pedHandle.IsValid)
            {
                return null;
            }

            var poolPed = _pedPool.GetByHandle(pedHandle.Handle);
            return poolPed != null && IsRecyclable(poolPed) ? poolPed : null;
        }

        /// <inheritdoc />
        public PedHandle? RecyclePed(int handle, string newFactionId, Vector3 newPosition, string? newZoneId)
        {
            var pedHandle = _pedPool.GetByHandle(handle);
            if (pedHandle == null)
            {
                return null;
            }

            return RecyclePed(pedHandle, newFactionId, newPosition, newZoneId);
        }

        /// <inheritdoc />
        public IList<PedHandle> RecycleDeadPeds(string newFactionId, Vector3 newPosition, string? newZoneId, int maxCount)
        {
            if (newFactionId == null)
            {
                throw new ArgumentNullException(nameof(newFactionId));
            }

            if (maxCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxCount), "Max count cannot be negative.");
            }

            var recycledPeds = new List<PedHandle>();

            // Get dead peds that can be recycled
            var deadPeds = _pedPool.GetAll()
                .Where(p => !_gameBridge.IsPedAlive(p.Handle) && !p.IsRecycled)
                .Take(maxCount)
                .ToList();

            foreach (var ped in deadPeds)
            {
                var recycledPed = RecyclePed(ped, newFactionId, newPosition, newZoneId);
                if (recycledPed != null)
                {
                    recycledPeds.Add(recycledPed);
                }
            }

            return recycledPeds;
        }

        /// <inheritdoc />
        public bool MarkAsRecycled(PedHandle pedHandle)
        {
            if (pedHandle == null || !pedHandle.IsValid)
            {
                return false;
            }

            pedHandle.MarkAsRecycled();
            return true;
        }

        /// <summary>
        /// Determines if a ped is eligible for recycling.
        /// A ped is recyclable if it's dead or marked for deletion, and hasn't been recycled yet.
        /// </summary>
        private bool IsRecyclable(PedHandle ped)
        {
            if (ped.IsRecycled)
            {
                return false;
            }

            // Dead peds are recyclable
            if (!_gameBridge.IsPedAlive(ped.Handle))
            {
                return true;
            }

            // Peds marked for deletion are recyclable
            if (ped.IsMarkedForDeletion)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Gets the relationship group name for a faction ID.
        /// </summary>
        private string GetRelationshipGroup(string factionId)
        {
            return factionId.ToUpperInvariant();
        }
    }
}
