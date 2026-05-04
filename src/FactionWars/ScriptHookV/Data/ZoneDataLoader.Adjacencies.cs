using System;
using System.Collections.Generic;
using System.Linq;
using FactionWars.Core.Interfaces;
using FactionWars.ScriptHookV.Logging;
using FactionWars.Territory.Interfaces;
using Newtonsoft.Json;

namespace FactionWars.ScriptHookV.Data
{
    public partial class ZoneDataLoader
    {
        public static void SetupZoneAdjacencies(IZoneRepository zoneRepository)
        {
            // Check if we have the default zones by looking for known zone IDs
            var downtown = zoneRepository.GetById("downtown");
            var vinewood = zoneRepository.GetById("vinewood");

            // If we have the default zone IDs, use hardcoded adjacencies
            if (downtown != null && vinewood != null && zoneRepository.Count >= 30)
            {
                SetupDefaultAdjacencies(zoneRepository);
            }
            else
            {
                // Custom zones - compute adjacencies by proximity
                FileLogger.Info("Using proximity-based adjacency computation for custom zones");
                ComputeAdjacenciesByProximity(zoneRepository);
            }
        }

        /// <summary>
        /// Computes zone adjacencies based on proximity (overlapping or close zones).
        /// Used when loading custom zones from JSON that don't have explicit adjacencies.
        /// </summary>
        /// <param name="zoneRepository">The zone repository containing loaded zones.</param>
        /// <param name="proximityMultiplier">Zones are adjacent if distance &lt; (radius1 + radius2) * multiplier. Default 1.2.</param>
        public static void ComputeAdjacenciesByProximity(IZoneRepository zoneRepository, float proximityMultiplier = 1.2f)
        {
            var zones = zoneRepository.GetAll().ToList();

            for (int i = 0; i < zones.Count; i++)
            {
                for (int j = i + 1; j < zones.Count; j++)
                {
                    var zone1 = zones[i];
                    var zone2 = zones[j];

                    float distance = CalculateDistance(zone1.Center, zone2.Center);
                    float threshold = (zone1.Radius + zone2.Radius) * proximityMultiplier;

                    if (distance < threshold)
                    {
                        if (!zone1.AdjacentZoneIds.Contains(zone2.Id))
                            zone1.AdjacentZoneIds.Add(zone2.Id);
                        if (!zone2.AdjacentZoneIds.Contains(zone1.Id))
                            zone2.AdjacentZoneIds.Add(zone1.Id);
                    }
                }
            }
        }

        /// <summary>
        /// Calculates the 2D distance between two points (ignoring Z coordinate).
        /// </summary>
        private static float CalculateDistance(Vector3 a, Vector3 b)
        {
            float dx = a.X - b.X;
            float dy = a.Y - b.Y;
            // Ignore Z for 2D map distance
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>
        /// Sets up hardcoded adjacencies for the default 31 zones.
        /// </summary>
        private static void SetupDefaultAdjacencies(IZoneRepository zoneRepository)
        {
            // Los Santos Urban Core adjacencies
            SetAdjacent(zoneRepository, "downtown", "vinewood", "pillbox_hill", "little_seoul", "mission_row", "strawberry");
            SetAdjacent(zoneRepository, "vinewood", "downtown", "vinewood_hills", "rockford_hills", "mirror_park");
            SetAdjacent(zoneRepository, "rockford_hills", "vinewood", "morningwood", "richman", "del_perro");
            SetAdjacent(zoneRepository, "little_seoul", "downtown", "pillbox_hill", "vespucci", "la_puerta");
            SetAdjacent(zoneRepository, "pillbox_hill", "downtown", "little_seoul", "mission_row", "strawberry");
            SetAdjacent(zoneRepository, "mission_row", "downtown", "pillbox_hill", "strawberry", "rancho", "mirror_park");
            SetAdjacent(zoneRepository, "strawberry", "downtown", "pillbox_hill", "mission_row", "davis", "rancho");
            SetAdjacent(zoneRepository, "davis", "strawberry", "rancho", "cypress_flats", "terminal");
            SetAdjacent(zoneRepository, "rancho", "mission_row", "strawberry", "davis", "east_los_santos", "cypress_flats");
            SetAdjacent(zoneRepository, "la_puerta", "little_seoul", "vespucci", "terminal", "lsia");
            SetAdjacent(zoneRepository, "vespucci", "little_seoul", "la_puerta", "del_perro", "morningwood");
            SetAdjacent(zoneRepository, "del_perro", "vespucci", "morningwood", "rockford_hills", "richman");
            SetAdjacent(zoneRepository, "morningwood", "vespucci", "del_perro", "rockford_hills");

            // Port & Industrial adjacencies
            SetAdjacent(zoneRepository, "port_of_los_santos", "elysian_island", "cypress_flats");
            SetAdjacent(zoneRepository, "elysian_island", "port_of_los_santos", "terminal", "lsia");
            SetAdjacent(zoneRepository, "cypress_flats", "davis", "rancho", "port_of_los_santos", "east_los_santos");
            SetAdjacent(zoneRepository, "terminal", "davis", "la_puerta", "elysian_island", "lsia");

            // Airport adjacencies
            SetAdjacent(zoneRepository, "lsia", "la_puerta", "terminal", "elysian_island");

            // Northern Suburbs adjacencies
            SetAdjacent(zoneRepository, "vinewood_hills", "vinewood", "mirror_park", "harmony");
            SetAdjacent(zoneRepository, "richman", "del_perro", "rockford_hills");
            SetAdjacent(zoneRepository, "mirror_park", "vinewood", "mission_row", "vinewood_hills", "east_los_santos");
            SetAdjacent(zoneRepository, "east_los_santos", "rancho", "mirror_park", "cypress_flats", "harmony");

            // Blaine County - Sandy Shores Area adjacencies
            SetAdjacent(zoneRepository, "sandy_shores", "trevor_airfield", "alamo_sea", "grapeseed", "grand_senora_desert");
            SetAdjacent(zoneRepository, "harmony", "vinewood_hills", "east_los_santos", "alamo_sea", "grand_senora_desert");
            SetAdjacent(zoneRepository, "grapeseed", "sandy_shores", "alamo_sea", "paleto_forest", "chiliad_wilderness");
            SetAdjacent(zoneRepository, "alamo_sea", "sandy_shores", "harmony", "grapeseed", "chiliad_wilderness");

            // Blaine County - Mount Chiliad Area adjacencies
            SetAdjacent(zoneRepository, "paleto_bay", "paleto_forest", "chiliad_wilderness");
            SetAdjacent(zoneRepository, "paleto_forest", "paleto_bay", "grapeseed", "chiliad_wilderness");
            SetAdjacent(zoneRepository, "chiliad_wilderness", "paleto_bay", "paleto_forest", "grapeseed", "alamo_sea");

            // Blaine County - Desert adjacencies
            SetAdjacent(zoneRepository, "grand_senora_desert", "sandy_shores", "harmony", "trevor_airfield");
            SetAdjacent(zoneRepository, "trevor_airfield", "sandy_shores", "grand_senora_desert");
        }

        /// <summary>
        /// Helper to set up adjacency for a zone with multiple neighbors.
        /// </summary>
        private static void SetAdjacent(IZoneRepository zoneRepository, string zoneId, params string[] adjacentIds)
        {
            var zone = zoneRepository.GetById(zoneId);
            if (zone == null) return;

            foreach (var adjacentId in adjacentIds)
            {
                if (!zone.AdjacentZoneIds.Contains(adjacentId))
                {
                    zone.AdjacentZoneIds.Add(adjacentId);
                }

                // Also add reverse adjacency
                var adjacentZone = zoneRepository.GetById(adjacentId);
                if (adjacentZone != null && !adjacentZone.AdjacentZoneIds.Contains(zoneId))
                {
                    adjacentZone.AdjacentZoneIds.Add(zoneId);
                }
            }
        }

        /// <summary>
        /// DTO for deserializing zone data from JSON.
        /// </summary>
        private class ZoneDto
        {
            public string Id { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public float CenterX { get; set; }
            public float CenterY { get; set; }
            public float CenterZ { get; set; }
            public float Radius { get; set; } = 150f;
            public int StrategicValue { get; set; } = 1;

            // Support both string (comma-separated) and array formats
            [JsonProperty("traits")]
            public object? TraitsRaw { get; set; }

            public string? InitialOwner { get; set; }

            // Adjacent zone IDs (optional - if not provided, computed by proximity)
            public List<string>? AdjacentZones { get; set; }
        }

        /// <summary>
        /// Wrapper for the zones.json file format.
        /// </summary>
        private class ZonesFileWrapper
        {
            public List<ZoneDto> Zones { get; set; } = new List<ZoneDto>();
        }
    }
}
