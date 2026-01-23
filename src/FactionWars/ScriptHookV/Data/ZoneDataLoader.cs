using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using FactionWars.Core.Interfaces;
using FactionWars.ScriptHookV.Logging;
using FactionWars.Territory.Interfaces;
using FactionWars.Territory.Models;

namespace FactionWars.ScriptHookV.Data
{
    /// <summary>
    /// Loads zone definitions from configuration or embedded data.
    /// Responsible for populating the zone repository with the 31 controllable zones.
    /// </summary>
    public class ZoneDataLoader
    {
        private readonly IZoneRepository _zoneRepository;

        /// <summary>
        /// Creates a new ZoneDataLoader.
        /// </summary>
        /// <param name="zoneRepository">The repository to load zones into.</param>
        /// <exception cref="ArgumentNullException">Thrown if zoneRepository is null.</exception>
        public ZoneDataLoader(IZoneRepository zoneRepository)
        {
            _zoneRepository = zoneRepository ?? throw new ArgumentNullException(nameof(zoneRepository));
        }

        /// <summary>
        /// Loads the default 31 zones covering Los Santos and Blaine County.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if zones have already been loaded.</exception>
        public void LoadDefaultZones()
        {
            if (_zoneRepository.Count > 0)
            {
                throw new InvalidOperationException("Zones have already been loaded.");
            }

            var zones = CreateDefaultZones().ToList();
            foreach (var zone in zones)
            {
                _zoneRepository.Add(zone);
            }

            // Set up zone adjacencies after all zones are loaded
            FileLogger.AI("Setting up zone adjacencies...");
            SetupZoneAdjacencies(_zoneRepository);

            // Log summary of adjacencies
            int totalAdjacencies = 0;
            foreach (var zone in _zoneRepository.GetAll())
            {
                totalAdjacencies += zone.AdjacentZoneIds.Count;
            }
            FileLogger.AI($"Zone adjacencies complete: {_zoneRepository.Count} zones, {totalAdjacencies} total adjacency links");
        }

        /// <summary>
        /// Loads zones from a JSON configuration string.
        /// </summary>
        /// <param name="json">The JSON string containing zone definitions.</param>
        /// <exception cref="ArgumentNullException">Thrown if json is null.</exception>
        /// <exception cref="ArgumentException">Thrown if json is empty or whitespace.</exception>
        public void LoadFromJson(string json)
        {
            if (json == null)
                throw new ArgumentNullException(nameof(json));
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentException("JSON cannot be empty or whitespace.", nameof(json));

            var zoneDtos = JsonConvert.DeserializeObject<List<ZoneDto>>(json);
            if (zoneDtos == null)
                throw new InvalidOperationException("Failed to deserialize zone data.");

            foreach (var dto in zoneDtos)
            {
                var zone = CreateZoneFromDto(dto);
                _zoneRepository.Add(zone);
            }
        }

        /// <summary>
        /// Loads zones from file if it exists, otherwise loads default hardcoded zones.
        /// Also sets up zone adjacencies after loading.
        /// </summary>
        /// <param name="zonesFilePath">Path to the zones.json file.</param>
        public void LoadZonesWithFallback(string zonesFilePath)
        {
            if (_zoneRepository.Count > 0)
            {
                throw new InvalidOperationException("Zones have already been loaded.");
            }

            bool loadedFromFile = LoadFromFile(zonesFilePath);

            if (!loadedFromFile)
            {
                FileLogger.Info("Loading default zones (no zones.json found)");
                var zones = CreateDefaultZones().ToList();
                foreach (var zone in zones)
                {
                    _zoneRepository.Add(zone);
                }
            }

            // Set up zone adjacencies after all zones are loaded
            FileLogger.AI("Setting up zone adjacencies...");
            SetupZoneAdjacencies(_zoneRepository);

            // Log summary
            int totalAdjacencies = 0;
            foreach (var zone in _zoneRepository.GetAll())
            {
                totalAdjacencies += zone.AdjacentZoneIds.Count;
            }
            FileLogger.AI($"Zone loading complete: {_zoneRepository.Count} zones, {totalAdjacencies} total adjacency links");
        }

        /// <summary>
        /// Loads zones from a JSON file.
        /// </summary>
        /// <param name="filePath">Path to the zones.json file.</param>
        /// <returns>True if file was loaded, false if file doesn't exist.</returns>
        public bool LoadFromFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                FileLogger.Info($"Zones file not found: {filePath}");
                return false;
            }

            try
            {
                var json = File.ReadAllText(filePath);
                var wrapper = JsonConvert.DeserializeObject<ZonesFileWrapper>(json);

                if (wrapper?.Zones == null || wrapper.Zones.Count == 0)
                {
                    FileLogger.Warn($"Zones file is empty or invalid: {filePath}");
                    return false;
                }

                foreach (var dto in wrapper.Zones)
                {
                    var zone = CreateZoneFromDto(dto);
                    _zoneRepository.Add(zone);
                }

                FileLogger.Info($"Loaded {wrapper.Zones.Count} zones from {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                FileLogger.Error($"Failed to load zones from {filePath}", ex);
                return false;
            }
        }

        private static Zone CreateZoneFromDto(ZoneDto dto)
        {
            var center = new Vector3(dto.CenterX, dto.CenterY, dto.CenterZ);
            var zone = new Zone(dto.Id, dto.Name, center, dto.Radius, dto.StrategicValue);
            zone.Traits = ParseTraits(dto.TraitsRaw);
            return zone;
        }

        private static ZoneTrait ParseTraits(object? traitsRaw)
        {
            if (traitsRaw == null)
                return ZoneTrait.None;

            ZoneTrait result = ZoneTrait.None;

            // Handle JArray (from JSON array)
            if (traitsRaw is JArray jArray)
            {
                foreach (var item in jArray)
                {
                    var traitStr = item.ToString().Trim();
                    if (Enum.TryParse<ZoneTrait>(traitStr, true, out var trait))
                    {
                        result |= trait;
                    }
                }
                return result;
            }

            // Handle string (comma-separated)
            var traitsString = traitsRaw.ToString();
            if (string.IsNullOrWhiteSpace(traitsString))
                return ZoneTrait.None;

            var parts = traitsString.Split(new[] { ',', '|' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                if (Enum.TryParse<ZoneTrait>(part.Trim(), true, out var trait))
                {
                    result |= trait;
                }
            }
            return result;
        }

        /// <summary>
        /// Creates the default 31 zones for Los Santos and Blaine County.
        /// Based on actual GTA V map locations and coordinates.
        /// </summary>
        private static IEnumerable<Zone> CreateDefaultZones()
        {
            // Los Santos - Urban Core
            yield return CreateZone("downtown", "Downtown", -250f, -850f, 200f, 8, ZoneTrait.Commercial | ZoneTrait.HighValue);
            yield return CreateZone("vinewood", "Vinewood", 300f, 150f, 250f, 7, ZoneTrait.Commercial | ZoneTrait.Residential);
            yield return CreateZone("rockford_hills", "Rockford Hills", -550f, 100f, 200f, 6, ZoneTrait.Residential | ZoneTrait.HighValue);
            yield return CreateZone("little_seoul", "Little Seoul", -750f, -1000f, 180f, 5, ZoneTrait.Commercial);
            yield return CreateZone("pillbox_hill", "Pillbox Hill", -100f, -1100f, 150f, 6, ZoneTrait.Commercial);
            yield return CreateZone("mission_row", "Mission Row", 400f, -1000f, 150f, 5, ZoneTrait.Industrial);
            yield return CreateZone("strawberry", "Strawberry", 200f, -1400f, 150f, 4, ZoneTrait.Residential);
            yield return CreateZone("davis", "Davis", 100f, -1700f, 200f, 4, ZoneTrait.Residential);
            yield return CreateZone("rancho", "Rancho", 400f, -1700f, 180f, 3, ZoneTrait.Residential);
            yield return CreateZone("la_puerta", "La Puerta", -1000f, -1500f, 200f, 5, ZoneTrait.Residential);
            yield return CreateZone("vespucci", "Vespucci", -1100f, -1200f, 200f, 5, ZoneTrait.Commercial | ZoneTrait.Residential);
            yield return CreateZone("del_perro", "Del Perro", -1700f, -800f, 250f, 6, ZoneTrait.Commercial);
            yield return CreateZone("morningwood", "Morningwood", -1300f, -600f, 180f, 4, ZoneTrait.Residential);

            // Los Santos - Port & Industrial
            yield return CreateZone("port_of_los_santos", "Port of Los Santos", 200f, -2900f, 400f, 8, ZoneTrait.Port | ZoneTrait.Industrial);
            yield return CreateZone("elysian_island", "Elysian Island", -300f, -2700f, 300f, 6, ZoneTrait.Port | ZoneTrait.Industrial);
            yield return CreateZone("cypress_flats", "Cypress Flats", 700f, -2300f, 200f, 4, ZoneTrait.Industrial);
            yield return CreateZone("terminal", "Terminal", -400f, -2200f, 180f, 4, ZoneTrait.Industrial);

            // Los Santos - Airport
            yield return CreateZone("lsia", "Los Santos International", -1200f, -2500f, 500f, 9, ZoneTrait.Airfield | ZoneTrait.HighValue);

            // Blaine County - Sandy Shores Area
            yield return CreateZone("sandy_shores", "Sandy Shores", 1700f, 3700f, 350f, 5, ZoneTrait.Residential);
            yield return CreateZone("harmony", "Harmony", 600f, 2800f, 200f, 3, ZoneTrait.Residential);
            yield return CreateZone("grapeseed", "Grapeseed", 1700f, 4800f, 250f, 4, ZoneTrait.Residential);
            yield return CreateZone("alamo_sea", "Alamo Sea", 600f, 4000f, 300f, 3, ZoneTrait.None);

            // Blaine County - Mount Chiliad Area
            yield return CreateZone("paleto_bay", "Paleto Bay", -200f, 6300f, 350f, 6, ZoneTrait.Port | ZoneTrait.Residential);
            yield return CreateZone("paleto_forest", "Paleto Forest", -700f, 5500f, 400f, 2, ZoneTrait.None);
            yield return CreateZone("chiliad_wilderness", "Chiliad Wilderness", 500f, 5500f, 400f, 2, ZoneTrait.Fortified);

            // Blaine County - Desert
            yield return CreateZone("grand_senora_desert", "Grand Senora Desert", 2500f, 3000f, 400f, 3, ZoneTrait.None);
            yield return CreateZone("trevor_airfield", "Trevor's Airfield", 1750f, 3250f, 250f, 7, ZoneTrait.Airfield);

            // Northern Suburbs
            yield return CreateZone("vinewood_hills", "Vinewood Hills", 600f, 600f, 300f, 5, ZoneTrait.Residential | ZoneTrait.Fortified);
            yield return CreateZone("richman", "Richman", -1600f, 200f, 250f, 6, ZoneTrait.Residential | ZoneTrait.HighValue);
            yield return CreateZone("mirror_park", "Mirror Park", 1100f, -700f, 200f, 4, ZoneTrait.Residential);
            yield return CreateZone("east_los_santos", "East Los Santos", 1200f, -1700f, 300f, 4, ZoneTrait.Industrial | ZoneTrait.Residential);
        }

        private static Zone CreateZone(string id, string name, float x, float y, float radius, int strategicValue, ZoneTrait traits)
        {
            // Z coordinate for zones - ground level approximation
            const float groundLevel = 30f;
            var center = new Vector3(x, y, groundLevel);
            var zone = new Zone(id, name, center, radius, strategicValue);
            zone.Traits = traits;
            return zone;
        }

        /// <summary>
        /// Sets up adjacency relationships between zones based on GTA V geography.
        /// Adjacencies are bidirectional - if A borders B, then B borders A.
        /// Can be called after loading zones from a save to restore adjacency data.
        /// </summary>
        /// <param name="zoneRepository">The zone repository containing loaded zones.</param>
        public static void SetupZoneAdjacencies(IZoneRepository zoneRepository)
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
