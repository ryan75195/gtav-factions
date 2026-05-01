using System;
using System.Collections.Generic;
using System.Linq;
using FactionWars.Core.Interfaces;
using FactionWars.Factions.Interfaces;
using FactionWars.ScriptHookV.Logging;
using FactionWars.Territory.Interfaces;
using FactionWars.Territory.Models;

namespace FactionWars.ScriptHookV.Managers
{
    /// <summary>
    /// Manages map blips for all zones, displaying territory ownership on the minimap and pause map.
    /// </summary>
    public class MapBlipManager : IDisposable
    {
        /// <summary>
        /// GTA V blip sprite ID for skull and crossbones icon.
        /// </summary>
        private const int SkullBlipSprite = 84;

        private readonly IGameBridge _gameBridge;
        private readonly IZoneRepository _zoneRepository;
        private readonly IFactionService _factionService;
        private readonly Dictionary<string, int> _zoneBlips;
        private bool _disposed;

        /// <summary>
        /// Creates a new MapBlipManager.
        /// </summary>
        /// <param name="gameBridge">The game bridge for creating and managing blips.</param>
        /// <param name="zoneRepository">The zone repository for accessing zone data.</param>
        /// <param name="factionService">The faction service for faction information.</param>
        /// <exception cref="ArgumentNullException">Thrown if any parameter is null.</exception>
        public MapBlipManager(IGameBridge gameBridge, IZoneRepository zoneRepository, IFactionService factionService)
        {
            _gameBridge = gameBridge ?? throw new ArgumentNullException(nameof(gameBridge));
            _zoneRepository = zoneRepository ?? throw new ArgumentNullException(nameof(zoneRepository));
            _factionService = factionService ?? throw new ArgumentNullException(nameof(factionService));
            _zoneBlips = new Dictionary<string, int>();
        }

        /// <summary>
        /// Initializes blips for all zones in the repository.
        /// If called multiple times, will delete old blips first.
        /// </summary>
        public void Initialize()
        {
            FileLogger.Separator("MAP BLIP INITIALIZATION");

            // Clean up existing blips if re-initializing
            CleanupBlips();

            // Create blips for all zones
            foreach (var zone in _zoneRepository.GetAll())
            {
                var blipHandle = _gameBridge.CreateBlip(zone.Center);

                // Only track and configure successful blip creation
                if (blipHandle != -1)
                {
                    _zoneBlips[zone.Id] = blipHandle;
                    var sprite = SkullBlipSprite;
                    _gameBridge.SetBlipSprite(blipHandle, sprite);
                    _gameBridge.SetBlipName(blipHandle, zone.Name);
                    var color = GetBlipColorForFaction(zone.OwnerFactionId);
                    _gameBridge.SetBlipColor(blipHandle, color);

                    FileLogger.Zone($"Blip created: '{zone.Name}' (ID: {zone.Id}) -> Owner: {zone.OwnerFactionId ?? "NONE"} -> Color: {color} -> Sprite: {sprite}");
                }
                else
                {
                    FileLogger.Warn($"Failed to create blip for zone '{zone.Name}' (ID: {zone.Id})");
                }
            }

            FileLogger.Info($"MapBlipManager: Created {_zoneBlips.Count} zone blips");
        }

        /// <summary>
        /// Updates all blip colors and sprites based on current zone ownership and contest status.
        /// </summary>
        public void UpdateBlipColors()
        {
            foreach (var zone in _zoneRepository.GetAll())
            {
                if (_zoneBlips.TryGetValue(zone.Id, out var blipHandle))
                {
                    var color = GetBlipColorForFaction(zone.OwnerFactionId);
                    _gameBridge.SetBlipColor(blipHandle, color);
                }
            }
        }

        /// <summary>
        /// Updates the blip color and sprite for a specific zone.
        /// </summary>
        /// <param name="zoneId">The ID of the zone to update.</param>
        public void UpdateBlipColor(string zoneId)
        {
            if (_zoneBlips.TryGetValue(zoneId, out var blipHandle))
            {
                var zone = _zoneRepository.GetById(zoneId);
                if (zone != null)
                {
                    var color = GetBlipColorForFaction(zone.OwnerFactionId);
                    _gameBridge.SetBlipColor(blipHandle, color);
                    FileLogger.Zone($"Blip updated: '{zone.Name}' -> Owner: {zone.OwnerFactionId ?? "NONE"} -> Color: {color} -> Contested: {zone.IsContested}");
                }
            }
        }

        /// <summary>
        /// Gets the blip handle for a specific zone.
        /// </summary>
        /// <param name="zoneId">The ID of the zone.</param>
        /// <returns>The blip handle, or -1 if no blip exists for the zone.</returns>
        public int GetBlipHandle(string zoneId)
        {
            return _zoneBlips.TryGetValue(zoneId, out var handle) ? handle : -1;
        }

        /// <summary>
        /// Disposes of all managed blips.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;

            CleanupBlips();
            _disposed = true;
        }

        private void CleanupBlips()
        {
            foreach (var blipHandle in _zoneBlips.Values)
            {
                _gameBridge.DeleteBlip(blipHandle);
            }
            _zoneBlips.Clear();
        }

        /// <summary>
        /// Gets the appropriate blip color for a faction.
        /// </summary>
        /// <param name="factionId">The faction ID, or null for neutral zones.</param>
        /// <returns>The appropriate blip color.</returns>
        private BlipColor GetBlipColorForFaction(string? factionId)
        {
            if (factionId == null)
            {
                return BlipColor.White;
            }

            // Map faction IDs to their character-specific colors
            return factionId.ToLowerInvariant() switch
            {
                "michael" => BlipColor.MichaelBlue,
                "trevor" => BlipColor.TrevorOrange,
                "franklin" => BlipColor.FranklinGreen,
                _ => BlipColor.White
            };
        }
    }
}
