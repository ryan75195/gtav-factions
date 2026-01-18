using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Models;
using FactionWars.Core.Interfaces;
using System;
using System.Collections.Generic;

namespace FactionWars.Combat.Services
{
    /// <summary>
    /// Service for spawning peds with faction relationship groups.
    /// Coordinates between the game bridge (for actual ped creation) and the ped pool (for tracking).
    /// </summary>
    public class PedSpawningService : IPedSpawningService
    {
        private readonly IGameBridge _gameBridge;
        private readonly IPedPool _pedPool;

        /// <summary>
        /// Creates a new PedSpawningService.
        /// </summary>
        /// <param name="gameBridge">The game bridge for creating peds in the game world.</param>
        /// <param name="pedPool">The ped pool for tracking spawned peds.</param>
        /// <exception cref="ArgumentNullException">Thrown if gameBridge or pedPool is null.</exception>
        public PedSpawningService(IGameBridge gameBridge, IPedPool pedPool)
        {
            _gameBridge = gameBridge ?? throw new ArgumentNullException(nameof(gameBridge));
            _pedPool = pedPool ?? throw new ArgumentNullException(nameof(pedPool));
        }

        /// <inheritdoc />
        public PedHandle SpawnPed(string modelName, Vector3 position, string factionId, string? zoneId)
        {
            ValidateModelName(modelName);
            ValidateFactionId(factionId);

            // Check if pool has space before creating the ped in game
            if (_pedPool.IsFull)
            {
                return PedHandle.Invalid;
            }

            // Create the ped in the game world
            int handle = _gameBridge.CreatePed(modelName, position);

            if (handle < 0)
            {
                return PedHandle.Invalid;
            }

            // Set the relationship group based on faction
            string relationshipGroup = GetRelationshipGroup(factionId);
            _gameBridge.SetPedRelationshipGroup(handle, relationshipGroup);

            // Create the ped handle with metadata
            var pedHandle = new PedHandle(
                handle,
                factionId: factionId,
                spawnPosition: position,
                modelName: modelName,
                zoneId: zoneId);

            // Add to the pool
            if (!_pedPool.Add(pedHandle))
            {
                // Failed to add to pool, clean up the ped
                _gameBridge.DeletePed(handle);
                return PedHandle.Invalid;
            }

            return pedHandle;
        }

        /// <inheritdoc />
        public IList<PedHandle> SpawnMultiplePeds(string modelName, Vector3 position, string factionId, string? zoneId, int count)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), "Count cannot be negative.");
            }

            var spawnedPeds = new List<PedHandle>();

            for (int i = 0; i < count; i++)
            {
                if (!CanSpawn())
                {
                    break;
                }

                var ped = SpawnPed(modelName, position, factionId, zoneId);
                if (ped.IsValid)
                {
                    spawnedPeds.Add(ped);
                }
                else
                {
                    // If spawn failed unexpectedly (not due to pool full), stop trying
                    break;
                }
            }

            return spawnedPeds;
        }

        /// <inheritdoc />
        public bool CanSpawn()
        {
            return !_pedPool.IsFull;
        }

        /// <inheritdoc />
        public int CanSpawnCount()
        {
            return _pedPool.AvailableSlots;
        }

        /// <inheritdoc />
        public string GetRelationshipGroup(string factionId)
        {
            // Normalize to uppercase for GTA V relationship groups
            return factionId.ToUpperInvariant();
        }

        private void ValidateModelName(string modelName)
        {
            if (modelName == null)
            {
                throw new ArgumentNullException(nameof(modelName));
            }

            if (string.IsNullOrWhiteSpace(modelName))
            {
                throw new ArgumentException("Model name cannot be empty or whitespace.", nameof(modelName));
            }
        }

        private void ValidateFactionId(string factionId)
        {
            if (factionId == null)
            {
                throw new ArgumentNullException(nameof(factionId));
            }

            if (string.IsNullOrWhiteSpace(factionId))
            {
                throw new ArgumentException("Faction ID cannot be empty or whitespace.", nameof(factionId));
            }
        }
    }
}
