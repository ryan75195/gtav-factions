using FactionWars.Core.Interfaces;
using FactionWars.Territory.Models;
using System.Collections.Generic;
using System.Linq;

namespace FactionWars.Persistence.Models
{
    /// <summary>
    /// Data transfer object for serializing Zone data.
    /// </summary>
    public class ZoneData
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public float CenterX { get; set; }
        public float CenterY { get; set; }
        public float CenterZ { get; set; }
        public float Radius { get; set; } = 150f;
        public int StrategicValue { get; set; } = 1;
        public string? OwnerFactionId { get; set; }
        public float ControlPercentage { get; set; }
        public bool IsContested { get; set; }
        public ZoneTrait Traits { get; set; } = ZoneTrait.None;
        public List<string> AdjacentZoneIds { get; set; } = new List<string>();

        /// <summary>
        /// Creates a ZoneData from a Zone model.
        /// </summary>
        public static ZoneData FromZone(Zone zone)
        {
            return new ZoneData
            {
                Id = zone.Id,
                Name = zone.Name,
                CenterX = zone.Center.X,
                CenterY = zone.Center.Y,
                CenterZ = zone.Center.Z,
                Radius = zone.Radius,
                StrategicValue = zone.StrategicValue,
                OwnerFactionId = zone.OwnerFactionId,
                ControlPercentage = zone.ControlPercentage,
                IsContested = zone.IsContested,
                Traits = zone.Traits,
                AdjacentZoneIds = zone.AdjacentZoneIds.ToList()
            };
        }

        /// <summary>
        /// Converts this data object to a Zone model.
        /// </summary>
        public Zone ToZone()
        {
            var zone = new Zone(
                Id,
                Name,
                new Vector3(CenterX, CenterY, CenterZ),
                Radius,
                StrategicValue
            );
            zone.OwnerFactionId = OwnerFactionId;
            zone.ControlPercentage = ControlPercentage;
            zone.IsContested = IsContested;
            zone.Traits = Traits;
            foreach (var adjacentId in AdjacentZoneIds)
            {
                zone.AdjacentZoneIds.Add(adjacentId);
            }
            return zone;
        }
    }
}
