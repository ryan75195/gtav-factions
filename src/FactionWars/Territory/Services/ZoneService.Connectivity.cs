using System;
using FactionWars.Territory.Models;
using System.Collections.Generic;
using System.Linq;

namespace FactionWars.Territory.Services
{
    public partial class ZoneService
    {
        public IEnumerable<Zone> GetConnectedZones(string zoneId)
        {
            if (zoneId == null)
                throw new ArgumentNullException(nameof(zoneId));

            var startZone = _repository.GetById(zoneId);
            if (startZone == null)
                return Enumerable.Empty<Zone>();

            var visited = new HashSet<string> { zoneId };
            var queue = new Queue<Zone>();
            var connected = new List<Zone>();

            // Start with adjacent zones
            foreach (var adjacent in GetAdjacentZones(zoneId))
            {
                if (!visited.Contains(adjacent.Id))
                {
                    visited.Add(adjacent.Id);
                    queue.Enqueue(adjacent);
                    connected.Add(adjacent);
                }
            }

            // BFS to find all connected zones
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (var adjacent in GetAdjacentZones(current.Id))
                {
                    if (!visited.Contains(adjacent.Id))
                    {
                        visited.Add(adjacent.Id);
                        queue.Enqueue(adjacent);
                        connected.Add(adjacent);
                    }
                }
            }

            return connected;
        }

        /// <inheritdoc />
        public IEnumerable<Zone> GetConnectedZonesByOwner(string zoneId, string factionId)
        {
            if (zoneId == null)
                throw new ArgumentNullException(nameof(zoneId));
            if (factionId == null)
                throw new ArgumentNullException(nameof(factionId));

            var startZone = _repository.GetById(zoneId);
            if (startZone == null || startZone.OwnerFactionId != factionId)
                return Enumerable.Empty<Zone>();

            var visited = new HashSet<string> { zoneId };
            var queue = new Queue<Zone>();
            var connected = new List<Zone>();

            // Start with adjacent zones owned by the same faction
            foreach (var adjacent in GetAdjacentZones(zoneId))
            {
                if (adjacent.OwnerFactionId == factionId && !visited.Contains(adjacent.Id))
                {
                    visited.Add(adjacent.Id);
                    queue.Enqueue(adjacent);
                    connected.Add(adjacent);
                }
            }

            // BFS to find all connected zones owned by the faction
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (var adjacent in GetAdjacentZones(current.Id))
                {
                    if (adjacent.OwnerFactionId == factionId && !visited.Contains(adjacent.Id))
                    {
                        visited.Add(adjacent.Id);
                        queue.Enqueue(adjacent);
                        connected.Add(adjacent);
                    }
                }
            }

            return connected;
        }

        /// <summary>
        /// Internal method to check if two zones are adjacent based on 2D distance and radii.
        /// </summary>
        private static bool AreZonesAdjacentInternal(Zone zone1, Zone zone2)
        {
            // Use 2D distance for adjacency calculation (ignoring Z axis)
            float distance = zone1.Center.DistanceTo2D(zone2.Center);
            float combinedRadius = zone1.Radius + zone2.Radius;

            // Zones are adjacent if they touch or overlap
            return distance <= combinedRadius;
        }
    }
}
