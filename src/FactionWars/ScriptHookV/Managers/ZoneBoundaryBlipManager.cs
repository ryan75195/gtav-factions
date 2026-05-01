using FactionWars.Core.Interfaces;
using FactionWars.ScriptHookV.Logging;
using FactionWars.Territory.Models;
using System;

namespace FactionWars.ScriptHookV.Managers
{
    /// <summary>
    /// Renders a translucent radius circle on the minimap and pause map for the
    /// zone the player is currently inside, so they can see the zone boundary
    /// without having to walk it. The blip is created on ZoneEntered and torn
    /// down on ZoneExited, so at most one boundary blip is alive at any time.
    /// </summary>
    public sealed class ZoneBoundaryBlipManager : IDisposable
    {
        /// <summary>
        /// Blip alpha (0-255). Tuned subtle so the road network and zone-owner
        /// blip remain readable through the radius overlay.
        /// </summary>
        private const int BoundaryAlpha = 64;

        private readonly IGameBridge _bridge;
        private readonly ITerritoryEvents _territory;
        private int _activeBlip = -1;
        private bool _disposed;

        public ZoneBoundaryBlipManager(IGameBridge bridge, ITerritoryEvents territory)
        {
            _bridge = bridge ?? throw new ArgumentNullException(nameof(bridge));
            _territory = territory ?? throw new ArgumentNullException(nameof(territory));

            _territory.ZoneEntered += HandleZoneEntered;
            _territory.ZoneExited += HandleZoneExited;
        }

        private void HandleZoneEntered(object? sender, Zone zone)
        {
            if (_disposed || zone == null) return;

            DeleteActiveBlip();

            var handle = _bridge.CreateRadiusBlip(zone.Center, zone.Radius);
            if (handle == -1)
            {
                FileLogger.Warn($"ZoneBoundaryBlipManager: failed to create radius blip for {zone.Name}");
                return;
            }

            _activeBlip = handle;
            _bridge.SetBlipColor(handle, GetBoundaryColor(zone.OwnerFactionId));
            _bridge.SetBlipAlpha(handle, BoundaryAlpha);
            FileLogger.Zone($"ZoneBoundaryBlipManager: drew boundary for {zone.Name} (radius={zone.Radius:F0})");
        }

        private void HandleZoneExited(object? sender, Zone zone)
        {
            if (_disposed) return;
            DeleteActiveBlip();
        }

        private void DeleteActiveBlip()
        {
            if (_activeBlip == -1) return;
            _bridge.DeleteBlip(_activeBlip);
            _activeBlip = -1;
        }

        private static BlipColor GetBoundaryColor(string? factionId)
        {
            if (factionId == null) return BlipColor.White;
            return factionId.ToLowerInvariant() switch
            {
                "michael" => BlipColor.MichaelBlue,
                "trevor" => BlipColor.TrevorOrange,
                "franklin" => BlipColor.FranklinGreen,
                _ => BlipColor.White,
            };
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _territory.ZoneEntered -= HandleZoneEntered;
            _territory.ZoneExited -= HandleZoneExited;
            DeleteActiveBlip();
        }
    }
}
