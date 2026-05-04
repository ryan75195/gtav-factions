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
    public partial class ZoneDataLoader
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

    }
}
