using System;
using System.Collections.Generic;
using FactionWars.Core.Interfaces;
using FactionWars.Factions.Interfaces;
using FactionWars.Territory.Interfaces;
using FactionWars.Territory.Models;
using FactionWars.UI.Interfaces;

namespace FactionWars.UI.Services
{
    /// <summary>
    /// Service for managing map blips representing zones.
    /// Handles creation, update, and removal of blips on the game map.
    /// </summary>
    public class MapBlipService : IMapBlipService
    {
        private readonly IGameBridge _gameBridge;
        private readonly IZoneService _zoneService;
        private readonly IFactionRepository _factionRepository;
        private readonly Dictionary<string, int> _zoneBlips;

        /// <summary>
        /// Creates a new MapBlipService.
        /// </summary>
        /// <param name="gameBridge">The game bridge for native calls.</param>
        /// <param name="zoneService">The zone service for zone queries.</param>
        /// <param name="factionRepository">The faction repository for faction lookups.</param>
        /// <exception cref="ArgumentNullException">Thrown if any parameter is null.</exception>
        public MapBlipService(
            IGameBridge gameBridge,
            IZoneService zoneService,
            IFactionRepository factionRepository)
        {
            _gameBridge = gameBridge ?? throw new ArgumentNullException(nameof(gameBridge));
            _zoneService = zoneService ?? throw new ArgumentNullException(nameof(zoneService));
            _factionRepository = factionRepository ?? throw new ArgumentNullException(nameof(factionRepository));
            _zoneBlips = new Dictionary<string, int>();
        }

        /// <inheritdoc />
        public int CreateBlipForZone(Zone zone)
        {
            if (zone == null)
                throw new ArgumentNullException(nameof(zone));

            // Return existing blip if already tracked
            if (_zoneBlips.TryGetValue(zone.Id, out var existingHandle))
            {
                return existingHandle;
            }

            // Create new blip at zone center
            var blipHandle = _gameBridge.CreateBlip(zone.Center);

            // Set color based on faction ownership
            var blipColor = GetBlipColorForFaction(zone.OwnerFactionId);
            _gameBridge.SetBlipColor(blipHandle, blipColor);

            // Track the blip
            _zoneBlips[zone.Id] = blipHandle;

            return blipHandle;
        }

        /// <inheritdoc />
        public bool RemoveBlipForZone(string zoneId)
        {
            if (zoneId == null)
                throw new ArgumentNullException(nameof(zoneId));

            if (!_zoneBlips.TryGetValue(zoneId, out var blipHandle))
            {
                return false;
            }

            _gameBridge.DeleteBlip(blipHandle);
            _zoneBlips.Remove(zoneId);
            return true;
        }

        /// <inheritdoc />
        public bool UpdateBlipColor(string zoneId, string? factionId)
        {
            if (zoneId == null)
                throw new ArgumentNullException(nameof(zoneId));

            if (!_zoneBlips.TryGetValue(zoneId, out var blipHandle))
            {
                return false;
            }

            var blipColor = GetBlipColorForFaction(factionId);
            _gameBridge.SetBlipColor(blipHandle, blipColor);
            return true;
        }

        /// <inheritdoc />
        public int CreateBlipsForAllZones()
        {
            var zones = _zoneService.GetAllZones();
            var count = 0;

            foreach (var zone in zones)
            {
                CreateBlipForZone(zone);
                count++;
            }

            return count;
        }

        /// <inheritdoc />
        public void RemoveAllBlips()
        {
            foreach (var kvp in _zoneBlips)
            {
                _gameBridge.DeleteBlip(kvp.Value);
            }
            _zoneBlips.Clear();
        }

        /// <inheritdoc />
        public int? GetBlipHandle(string zoneId)
        {
            if (zoneId == null)
                throw new ArgumentNullException(nameof(zoneId));

            if (_zoneBlips.TryGetValue(zoneId, out var handle))
            {
                return handle;
            }
            return null;
        }

        /// <inheritdoc />
        public bool HasBlipForZone(string zoneId)
        {
            if (zoneId == null)
                throw new ArgumentNullException(nameof(zoneId));

            return _zoneBlips.ContainsKey(zoneId);
        }

        /// <inheritdoc />
        public BlipColor GetBlipColorForFaction(string? factionId)
        {
            if (factionId == null)
            {
                return BlipColor.White;
            }

            var faction = _factionRepository.GetById(factionId);
            if (faction == null)
            {
                return BlipColor.White;
            }

            // Map faction ID to appropriate blip color
            var normalizedId = factionId.ToLowerInvariant();
            if (normalizedId.Contains("michael"))
            {
                return BlipColor.MichaelBlue;
            }
            if (normalizedId.Contains("trevor"))
            {
                return BlipColor.TrevorOrange;
            }
            if (normalizedId.Contains("franklin"))
            {
                return BlipColor.FranklinGreen;
            }

            // Default to neutral for unknown factions
            return BlipColor.White;
        }

        /// <inheritdoc />
        public void SyncBlipWithZone(string zoneId)
        {
            if (zoneId == null)
                throw new ArgumentNullException(nameof(zoneId));

            if (!_zoneBlips.ContainsKey(zoneId))
            {
                return;
            }

            var zone = _zoneService.GetZone(zoneId);
            if (zone != null)
            {
                UpdateBlipColor(zoneId, zone.OwnerFactionId);
            }
        }

        /// <inheritdoc />
        public int GetTrackedZoneCount()
        {
            return _zoneBlips.Count;
        }
    }
}
