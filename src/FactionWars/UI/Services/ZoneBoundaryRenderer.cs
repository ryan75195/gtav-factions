using System;
using System.Collections.Generic;
using System.Linq;
using FactionWars.Core.Interfaces;
using FactionWars.Factions.Interfaces;
using FactionWars.Territory.Interfaces;
using FactionWars.Territory.Models;
using FactionWars.UI.Interfaces;
using FactionWars.UI.Models;

namespace FactionWars.UI.Services
{
    /// <summary>
    /// Service for rendering zone visual boundaries on the game map.
    /// Handles drawing zone outlines, colors, and visibility.
    /// </summary>
    public class ZoneBoundaryRenderer : IZoneBoundaryRenderer
    {
        private readonly IGameBridge _gameBridge;
        private readonly IZoneService _zoneService;
        private readonly IFactionRepository _factionRepository;
        private readonly Dictionary<string, ZoneBoundaryRenderData> _renderedBoundaries;

        public ZoneBoundaryRenderer(
            IGameBridge gameBridge,
            IZoneService zoneService,
            IFactionRepository factionRepository)
        {
            _gameBridge = gameBridge ?? throw new ArgumentNullException(nameof(gameBridge));
            _zoneService = zoneService ?? throw new ArgumentNullException(nameof(zoneService));
            _factionRepository = factionRepository ?? throw new ArgumentNullException(nameof(factionRepository));
            _renderedBoundaries = new Dictionary<string, ZoneBoundaryRenderData>();
        }

        public void RenderZoneBoundary(Zone zone)
        {
            if (zone == null)
                throw new ArgumentNullException(nameof(zone));

            if (_renderedBoundaries.ContainsKey(zone.Id))
                return;

            var renderData = new ZoneBoundaryRenderData
            {
                ZoneId = zone.Id,
                Center = zone.Center,
                Color = GetBoundaryColorForFaction(zone.OwnerFactionId),
                Alpha = 100,
                IsVisible = true
            };

            if (zone.Boundary.Type == BoundaryType.Circular)
            {
                renderData.RenderType = BoundaryRenderType.Circular;
                renderData.Radius = zone.Radius;
                renderData.Vertices = new List<Vector3>();
            }
            else
            {
                renderData.RenderType = BoundaryRenderType.Polygon;
                renderData.Radius = 0;
                renderData.Vertices = zone.Boundary.Vertices;
            }

            _renderedBoundaries[zone.Id] = renderData;
        }

        public bool RemoveZoneBoundary(string zoneId)
        {
            if (zoneId == null)
                throw new ArgumentNullException(nameof(zoneId));

            return _renderedBoundaries.Remove(zoneId);
        }

        public bool UpdateZoneBoundaryColor(string zoneId, string? factionId)
        {
            if (zoneId == null)
                throw new ArgumentNullException(nameof(zoneId));

            if (!_renderedBoundaries.TryGetValue(zoneId, out var renderData))
                return false;

            renderData.Color = GetBoundaryColorForFaction(factionId);
            return true;
        }

        public int RenderAllZoneBoundaries()
        {
            var zones = _zoneService.GetAllZones();
            foreach (var zone in zones)
            {
                RenderZoneBoundary(zone);
            }
            return _renderedBoundaries.Count;
        }

        public void RemoveAllZoneBoundaries()
        {
            _renderedBoundaries.Clear();
        }

        public bool IsZoneBoundaryRendered(string zoneId)
        {
            return _renderedBoundaries.ContainsKey(zoneId);
        }

        public ZoneBoundaryRenderData? GetZoneBoundaryRenderData(string zoneId)
        {
            return _renderedBoundaries.TryGetValue(zoneId, out var data) ? data : null;
        }

        public int GetRenderedZoneCount()
        {
            return _renderedBoundaries.Count;
        }

        public void SyncWithZone(string zoneId)
        {
            if (zoneId == null)
                throw new ArgumentNullException(nameof(zoneId));

            if (!_renderedBoundaries.TryGetValue(zoneId, out var renderData))
                return;

            var zone = _zoneService.GetZone(zoneId);
            if (zone != null)
            {
                renderData.Color = GetBoundaryColorForFaction(zone.OwnerFactionId);
            }
        }

        public BoundaryColor GetBoundaryColorForFaction(string? factionId)
        {
            if (factionId == null)
                return BoundaryColor.Neutral;

            var faction = _factionRepository.GetById(factionId);
            if (faction == null)
                return BoundaryColor.Neutral;

            // Determine faction type by color values
            if (faction.Color.B > faction.Color.R && faction.Color.B > faction.Color.G)
                return BoundaryColor.Michael;
            if (faction.Color.R > faction.Color.B && faction.Color.R > faction.Color.G)
                return BoundaryColor.Trevor;
            if (faction.Color.G > faction.Color.R && faction.Color.G > faction.Color.B)
                return BoundaryColor.Franklin;

            return BoundaryColor.Neutral;
        }

        public bool SetBoundaryAlpha(string zoneId, int alpha)
        {
            if (zoneId == null)
                throw new ArgumentNullException(nameof(zoneId));

            if (!_renderedBoundaries.TryGetValue(zoneId, out var renderData))
                return false;

            renderData.Alpha = Math.Max(0, Math.Min(255, alpha));
            return true;
        }

        public void SetGlobalBoundaryAlpha(int alpha)
        {
            var clampedAlpha = Math.Max(0, Math.Min(255, alpha));
            foreach (var renderData in _renderedBoundaries.Values)
            {
                renderData.Alpha = clampedAlpha;
            }
        }

        public bool ShowZoneBoundary(string zoneId)
        {
            if (zoneId == null)
                throw new ArgumentNullException(nameof(zoneId));

            if (!_renderedBoundaries.TryGetValue(zoneId, out var renderData))
                return false;

            renderData.IsVisible = true;
            return true;
        }

        public bool HideZoneBoundary(string zoneId)
        {
            if (zoneId == null)
                throw new ArgumentNullException(nameof(zoneId));

            if (!_renderedBoundaries.TryGetValue(zoneId, out var renderData))
                return false;

            renderData.IsVisible = false;
            return true;
        }
    }
}
