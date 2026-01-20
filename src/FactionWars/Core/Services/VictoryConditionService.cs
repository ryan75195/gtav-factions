using System;
using System.Linq;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.Territory.Interfaces;

namespace FactionWars.Core.Services
{
    /// <summary>
    /// Service for detecting and managing victory conditions.
    /// Victory is achieved when a faction controls 100% of all zones.
    /// </summary>
    public sealed class VictoryConditionService : IVictoryConditionService
    {
        private readonly IZoneService _zoneService;

        /// <summary>
        /// Creates a new VictoryConditionService.
        /// </summary>
        /// <param name="zoneService">The zone service for querying zone ownership.</param>
        /// <exception cref="ArgumentNullException">Thrown if zoneService is null.</exception>
        public VictoryConditionService(IZoneService zoneService)
        {
            _zoneService = zoneService ?? throw new ArgumentNullException(nameof(zoneService));
        }

        /// <inheritdoc />
        public VictoryCheckResult CheckVictoryCondition(string factionId)
        {
            if (factionId == null)
                throw new ArgumentNullException(nameof(factionId));

            int zonesOwned = GetFactionZoneCount(factionId);
            int totalZones = GetTotalZoneCount();

            if (totalZones > 0 && zonesOwned == totalZones)
            {
                return VictoryCheckResult.Victory(factionId, zonesOwned, totalZones);
            }

            return VictoryCheckResult.InProgress(factionId, zonesOwned, totalZones);
        }

        /// <inheritdoc />
        public float GetVictoryProgress(string factionId)
        {
            if (factionId == null)
                throw new ArgumentNullException(nameof(factionId));

            int zonesOwned = GetFactionZoneCount(factionId);
            int totalZones = GetTotalZoneCount();

            if (totalZones == 0)
                return 0f;

            return (float)zonesOwned / totalZones * 100f;
        }

        /// <inheritdoc />
        public int GetFactionZoneCount(string factionId)
        {
            if (factionId == null)
                throw new ArgumentNullException(nameof(factionId));

            return _zoneService.GetZoneCount(factionId);
        }

        /// <inheritdoc />
        public int GetTotalZoneCount()
        {
            return _zoneService.GetAllZones().Count();
        }

        /// <inheritdoc />
        public bool IsGameOver()
        {
            int totalZones = GetTotalZoneCount();
            if (totalZones == 0)
                return false;

            // Check all zones and group by owner
            var zones = _zoneService.GetAllZones().ToList();
            var ownerGroups = zones
                .Where(z => z.OwnerFactionId != null)
                .GroupBy(z => z.OwnerFactionId)
                .ToList();

            // Game is over if any faction owns all zones
            return ownerGroups.Any(g => g.Count() == totalZones);
        }

        /// <inheritdoc />
        public string? GetWinningFactionId()
        {
            int totalZones = GetTotalZoneCount();
            if (totalZones == 0)
                return null;

            // Check all zones and group by owner
            var zones = _zoneService.GetAllZones().ToList();
            var ownerGroups = zones
                .Where(z => z.OwnerFactionId != null)
                .GroupBy(z => z.OwnerFactionId)
                .ToList();

            // Find faction that owns all zones
            var winner = ownerGroups.FirstOrDefault(g => g.Count() == totalZones);
            return winner?.Key;
        }
    }
}
