using FactionWars.Core.Interfaces;
using FactionWars.Factions.Interfaces;
using FactionWars.ScriptHookV.Logging;
using FactionWars.Territory.Events;
using FactionWars.Territory.Interfaces;
using FactionWars.Territory.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FactionWars.Territory.Services
{
    /// <summary>
    /// Service providing zone-related business logic and queries.
    /// </summary>
    public partial class ZoneService : IZoneService
    {
        private readonly IZoneRepository _repository;
        private readonly IFactionRepository? _factionRepository;

        /// <inheritdoc />
        public event EventHandler<ZoneOwnershipChangedEventArgs>? ZoneOwnershipChanged;

        /// <summary>
        /// Creates a new ZoneService instance.
        /// </summary>
        /// <param name="repository">The zone repository.</param>
        /// <exception cref="ArgumentNullException">Thrown if repository is null.</exception>
        public ZoneService(IZoneRepository repository)
            : this(repository, null)
        {
        }

        /// <summary>
        /// Creates a new ZoneService instance with faction repository for syncing zone ownership.
        /// </summary>
        /// <param name="repository">The zone repository.</param>
        /// <param name="factionRepository">Optional faction repository for keeping FactionState in sync.</param>
        /// <exception cref="ArgumentNullException">Thrown if repository is null.</exception>
        public ZoneService(IZoneRepository repository, IFactionRepository? factionRepository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _factionRepository = factionRepository;
        }

        /// <inheritdoc />
        public Zone? GetZone(string id)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));

            return _repository.GetById(id);
        }

        /// <inheritdoc />
        public IEnumerable<Zone> GetAllZones()
        {
            return _repository.GetAll();
        }

        /// <inheritdoc />
        public Zone? GetZoneAtPosition(Vector3 position)
        {
            var zones = _repository.GetAll();
            Zone? closestZone = null;
            float closestDistance = float.MaxValue;

            foreach (var zone in zones)
            {
                // Use 2D distance for ground-based zone checks
                float distance = zone.Center.DistanceTo2D(position);

                if (distance <= zone.Radius && distance < closestDistance)
                {
                    closestZone = zone;
                    closestDistance = distance;
                }
            }

            return closestZone;
        }

        /// <inheritdoc />
        public IEnumerable<Zone> GetZonesByOwner(string? factionId)
        {
            return _repository.GetByOwner(factionId);
        }

        /// <inheritdoc />
        public IEnumerable<Zone> GetContestedZones()
        {
            return _repository.GetContested();
        }

        /// <inheritdoc />
        public IEnumerable<Zone> GetZonesByTrait(ZoneTrait trait)
        {
            var zones = _repository.GetAll();

            if (trait == ZoneTrait.None)
            {
                return zones.Where(z => z.Traits == ZoneTrait.None);
            }

            return zones.Where(z => z.Traits.HasFlag(trait));
        }

        /// <inheritdoc />
        public IEnumerable<Zone> GetHighValueZones(int count)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), "Count cannot be negative.");

            return _repository.GetAll()
                .OrderByDescending(z => z.StrategicValue)
                .Take(count);
        }

        /// <inheritdoc />
        public bool TransferZoneOwnership(string zoneId, string? newOwnerFactionId)
        {
            if (zoneId == null)
                throw new ArgumentNullException(nameof(zoneId));

            var zone = _repository.GetById(zoneId);
            if (zone == null)
                return false;

            var previousOwner = zone.OwnerFactionId;

            FileLogger.Separator("ZONE OWNERSHIP TRANSFER");
            FileLogger.Zone($"Zone '{zone.Name}' (ID: {zoneId}): {previousOwner ?? "NONE"} -> {newOwnerFactionId ?? "NONE"}");

            zone.OwnerFactionId = newOwnerFactionId;
            zone.ControlPercentage = 100f;
            zone.IsContested = false;

            _repository.Update(zone);

            // Sync FactionState zone lists if faction repository is available
            SyncFactionStateZones(zoneId, previousOwner, newOwnerFactionId);

            if (!string.Equals(previousOwner, newOwnerFactionId, StringComparison.Ordinal))
            {
                ZoneOwnershipChanged?.Invoke(this,
                    new ZoneOwnershipChangedEventArgs(zoneId, previousOwner, newOwnerFactionId));
            }

            return true;
        }

        /// <summary>
        /// Syncs FactionState zone lists when zone ownership changes.
        /// </summary>
        private void SyncFactionStateZones(string zoneId, string? previousOwner, string? newOwner)
        {
            if (_factionRepository == null)
                return;

            // Remove zone from previous owner's state
            if (previousOwner != null)
            {
                var previousState = _factionRepository.GetState(previousOwner);
                if (previousState != null)
                {
                    previousState.RemoveZone(zoneId);
                    _factionRepository.SetState(previousState);
                }
            }

            // Add zone to new owner's state
            if (newOwner != null)
            {
                var newState = _factionRepository.GetState(newOwner);
                if (newState != null)
                {
                    newState.AddZone(zoneId);
                    _factionRepository.SetState(newState);
                }
            }
        }

        /// <inheritdoc />
        public bool UpdateZoneControl(string zoneId, float controlPercentage)
        {
            if (zoneId == null)
                throw new ArgumentNullException(nameof(zoneId));

            var zone = _repository.GetById(zoneId);
            if (zone == null)
                return false;

            zone.ControlPercentage = controlPercentage;
            _repository.Update(zone);
            return true;
        }

        /// <inheritdoc />
        public bool SetZoneContested(string zoneId, bool isContested)
        {
            if (zoneId == null)
                throw new ArgumentNullException(nameof(zoneId));

            var zone = _repository.GetById(zoneId);
            if (zone == null)
                return false;

            zone.IsContested = isContested;
            _repository.Update(zone);
            return true;
        }

        /// <inheritdoc />
        public int GetFactionTerritoryValue(string factionId)
        {
            if (factionId == null)
                throw new ArgumentNullException(nameof(factionId));

            return _repository.GetByOwner(factionId)
                .Sum(z => z.StrategicValue);
        }

        /// <inheritdoc />
        public int GetZoneCount(string? factionId)
        {
            return _repository.GetByOwner(factionId).Count();
        }

        /// <inheritdoc />
        public bool IsPositionInAnyZone(Vector3 position)
        {
            return GetZoneAtPosition(position) != null;
        }

        /// <inheritdoc />
        public IEnumerable<Zone> GetAdjacentZones(string zoneId)
        {
            if (zoneId == null)
                throw new ArgumentNullException(nameof(zoneId));

            var zone = _repository.GetById(zoneId);
            if (zone == null)
                return Enumerable.Empty<Zone>();

            var allZones = _repository.GetAll();
            var adjacentZones = new List<Zone>();

            foreach (var other in allZones)
            {
                if (other.Id == zoneId)
                    continue;

                if (AreZonesAdjacentInternal(zone, other))
                {
                    adjacentZones.Add(other);
                }
            }

            return adjacentZones;
        }

        /// <inheritdoc />
        public bool AreZonesAdjacent(string zoneId1, string zoneId2)
        {
            if (zoneId1 == null)
                throw new ArgumentNullException(nameof(zoneId1));
            if (zoneId2 == null)
                throw new ArgumentNullException(nameof(zoneId2));

            if (zoneId1 == zoneId2)
                return false;

            var zone1 = _repository.GetById(zoneId1);
            var zone2 = _repository.GetById(zoneId2);

            if (zone1 == null || zone2 == null)
                return false;

            return AreZonesAdjacentInternal(zone1, zone2);
        }

        /// <inheritdoc />
    }
}
