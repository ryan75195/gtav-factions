using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Models;
using FactionWars.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FactionWars.Combat.Services
{
    /// <summary>
    /// Service for despawning peds from the game world.
    /// Coordinates between the game bridge (for deleting peds) and the ped pool (for tracking).
    /// </summary>
    public class PedDespawnService : IPedDespawnService
    {
        private readonly IGameBridge _gameBridge;
        private readonly IPedPool _pedPool;

        /// <summary>
        /// Creates a new PedDespawnService.
        /// </summary>
        /// <param name="gameBridge">The game bridge for deleting peds from the game world.</param>
        /// <param name="pedPool">The ped pool for tracking spawned peds.</param>
        /// <exception cref="ArgumentNullException">Thrown if gameBridge or pedPool is null.</exception>
        public PedDespawnService(IGameBridge gameBridge, IPedPool pedPool)
        {
            _gameBridge = gameBridge ?? throw new ArgumentNullException(nameof(gameBridge));
            _pedPool = pedPool ?? throw new ArgumentNullException(nameof(pedPool));
        }

        /// <inheritdoc />
        public bool DespawnPed(PedHandle ped)
        {
            if (ped == null)
            {
                throw new ArgumentNullException(nameof(ped));
            }

            if (!ped.IsValid)
            {
                return false;
            }

            if (!_pedPool.Contains(ped))
            {
                return false;
            }

            _gameBridge.DeletePed(ped.Handle);
            _pedPool.Remove(ped);
            return true;
        }

        /// <inheritdoc />
        public bool DespawnPed(int handle)
        {
            var ped = _pedPool.GetByHandle(handle);
            if (ped == null)
            {
                return false;
            }

            _gameBridge.DeletePed(handle);
            _pedPool.Remove(handle);
            return true;
        }

        /// <inheritdoc />
        public bool UntrackPed(int handle)
        {
            var ped = _pedPool.GetByHandle(handle);
            if (ped == null)
            {
                return false;
            }

            _pedPool.Remove(handle);
            return true;
        }

        /// <inheritdoc />
        public void DeletePedEntity(int handle)
        {
            _gameBridge.DeletePed(handle);
        }

        /// <inheritdoc />
        public IList<PedHandle> DespawnDeadPeds()
        {
            var deadPeds = _pedPool.GetAll()
                .Where(p => !_gameBridge.IsPedAlive(p.Handle))
                .ToList();

            foreach (var ped in deadPeds)
            {
                _gameBridge.DeletePed(ped.Handle);
                _pedPool.Remove(ped);
            }

            return deadPeds;
        }

        /// <inheritdoc />
        public IList<PedHandle> DespawnPedsByDistance(float maxDistance)
        {
            if (maxDistance < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxDistance), "Distance must be non-negative.");
            }

            var playerPosition = _gameBridge.GetPlayerPosition();
            var farPeds = _pedPool.GetAll()
                .Where(p => CalculateDistance(playerPosition, p.SpawnPosition) > maxDistance)
                .ToList();

            foreach (var ped in farPeds)
            {
                _gameBridge.DeletePed(ped.Handle);
                _pedPool.Remove(ped);
            }

            return farPeds;
        }

        /// <inheritdoc />
        public IList<PedHandle> DespawnMarkedForDeletion()
        {
            var markedPeds = _pedPool.GetMarkedForDeletion().ToList();

            foreach (var ped in markedPeds)
            {
                _gameBridge.DeletePed(ped.Handle);
                _pedPool.Remove(ped);
            }

            return markedPeds;
        }

        /// <inheritdoc />
        public IList<PedHandle> DespawnPedsByZone(string zoneId)
        {
            if (zoneId == null)
            {
                throw new ArgumentNullException(nameof(zoneId));
            }

            var zonePeds = _pedPool.GetByZone(zoneId).ToList();

            foreach (var ped in zonePeds)
            {
                _gameBridge.DeletePed(ped.Handle);
                _pedPool.Remove(ped);
            }

            return zonePeds;
        }

        /// <inheritdoc />
        public IList<PedHandle> DespawnPedsByFaction(string factionId)
        {
            if (factionId == null)
            {
                throw new ArgumentNullException(nameof(factionId));
            }

            var factionPeds = _pedPool.GetByFaction(factionId).ToList();

            foreach (var ped in factionPeds)
            {
                _gameBridge.DeletePed(ped.Handle);
                _pedPool.Remove(ped);
            }

            return factionPeds;
        }

        /// <inheritdoc />
        public IList<PedHandle> DespawnAll()
        {
            var allPeds = _pedPool.GetAll().ToList();

            foreach (var ped in allPeds)
            {
                _gameBridge.DeletePed(ped.Handle);
            }

            _pedPool.Clear();

            return allPeds;
        }

        /// <inheritdoc />
        public IList<PedHandle> DespawnOldest(int count)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), "Count must be non-negative.");
            }

            if (count == 0)
            {
                return new List<PedHandle>();
            }

            var oldestPeds = _pedPool.GetOldest(count).ToList();

            foreach (var ped in oldestPeds)
            {
                _gameBridge.DeletePed(ped.Handle);
                _pedPool.Remove(ped);
            }

            return oldestPeds;
        }

        /// <inheritdoc />
        public IList<PedHandle> DespawnPedsByFactionAndZone(string factionId, string zoneId)
        {
            if (factionId == null)
            {
                throw new ArgumentNullException(nameof(factionId));
            }

            if (zoneId == null)
            {
                throw new ArgumentNullException(nameof(zoneId));
            }

            var matchingPeds = _pedPool.GetByFactionAndZone(factionId, zoneId).ToList();

            foreach (var ped in matchingPeds)
            {
                _gameBridge.DeletePed(ped.Handle);
                _pedPool.Remove(ped);
            }

            return matchingPeds;
        }

        /// <summary>
        /// Calculates the 3D distance between two positions.
        /// </summary>
        private static float CalculateDistance(Vector3 a, Vector3 b)
        {
            float dx = a.X - b.X;
            float dy = a.Y - b.Y;
            float dz = a.Z - b.Z;
            return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }
    }
}
