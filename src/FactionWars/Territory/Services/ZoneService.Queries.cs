using System;
using FactionWars.Core.Interfaces;
using FactionWars.Territory.Models;
using System.Collections.Generic;
using System.Linq;

namespace FactionWars.Territory.Services
{
    public partial class ZoneService
    {
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
