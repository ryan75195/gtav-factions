using System;
using System.Collections.Generic;
using System.Linq;
using FactionWars.Economy.Interfaces;
using FactionWars.Territory.Interfaces;
using FactionWars.Territory.Models;

namespace FactionWars.Economy.Services
{
    /// <summary>
    /// Service for managing supply line connectivity between zones.
    /// Supply lines determine resource efficiency based on territorial connectivity to headquarters.
    /// </summary>
    public class SupplyLineService : ISupplyLineService
    {
        /// <summary>
        /// The efficiency multiplier applied to zones that are disconnected from headquarters.
        /// Default is 50% (0.5f).
        /// </summary>
        public const float DisconnectedEfficiency = 0.5f;

        private readonly IZoneService _zoneService;
        private readonly Dictionary<string, string> _headquarters; // factionId -> zoneId

        /// <summary>
        /// Creates a new SupplyLineService instance.
        /// </summary>
        /// <param name="zoneService">The zone service for zone queries.</param>
        /// <exception cref="ArgumentNullException">Thrown if zoneService is null.</exception>
        public SupplyLineService(IZoneService zoneService)
        {
            _zoneService = zoneService ?? throw new ArgumentNullException(nameof(zoneService));
            _headquarters = new Dictionary<string, string>();
        }

        /// <inheritdoc />
        public bool SetHeadquarters(string factionId, string zoneId)
        {
            ValidateFactionId(factionId);
            ValidateZoneId(zoneId);

            var zone = _zoneService.GetZone(zoneId);
            if (zone == null)
                return false;

            if (zone.OwnerFactionId != factionId)
                return false;

            _headquarters[factionId] = zoneId;
            return true;
        }

        /// <inheritdoc />
        public string? GetHeadquarters(string factionId)
        {
            ValidateFactionId(factionId);

            return _headquarters.TryGetValue(factionId, out var hqZoneId) ? hqZoneId : null;
        }

        /// <inheritdoc />
        public void ClearHeadquarters(string factionId)
        {
            if (factionId == null)
                throw new ArgumentNullException(nameof(factionId));

            _headquarters.Remove(factionId);
        }

        /// <inheritdoc />
        public bool IsConnectedToHeadquarters(string factionId, string zoneId)
        {
            ValidateFactionId(factionId);
            ValidateZoneId(zoneId);

            var hqZoneId = GetHeadquarters(factionId);
            if (hqZoneId == null)
                return false;

            // If zone is the HQ itself, it's connected
            if (zoneId == hqZoneId)
                return true;

            // Check if zone is in the connected zones from HQ
            var connectedZones = _zoneService.GetConnectedZonesByOwner(hqZoneId, factionId);
            return connectedZones.Any(z => z.Id == zoneId);
        }

        /// <inheritdoc />
        public float GetSupplyLineEfficiency(string factionId, string zoneId)
        {
            ValidateFactionId(factionId);
            ValidateZoneId(zoneId);

            var hqZoneId = GetHeadquarters(factionId);

            // If no HQ set, all zones are self-sufficient
            if (hqZoneId == null)
                return 1.0f;

            // Connected zones get full efficiency
            if (IsConnectedToHeadquarters(factionId, zoneId))
                return 1.0f;

            // Disconnected zones get reduced efficiency
            return DisconnectedEfficiency;
        }

        /// <inheritdoc />
        public IEnumerable<Zone> GetConnectedZones(string factionId)
        {
            ValidateFactionId(factionId);

            var hqZoneId = GetHeadquarters(factionId);
            if (hqZoneId == null)
                return Enumerable.Empty<Zone>();

            var hqZone = _zoneService.GetZone(hqZoneId);
            if (hqZone == null)
                return Enumerable.Empty<Zone>();

            var connected = new List<Zone> { hqZone };
            connected.AddRange(_zoneService.GetConnectedZonesByOwner(hqZoneId, factionId));

            return connected;
        }

        /// <inheritdoc />
        public IEnumerable<Zone> GetDisconnectedZones(string factionId)
        {
            ValidateFactionId(factionId);

            var hqZoneId = GetHeadquarters(factionId);

            // If no HQ, all zones are considered connected (self-sufficient)
            if (hqZoneId == null)
                return Enumerable.Empty<Zone>();

            var allOwnedZones = _zoneService.GetZonesByOwner(factionId);
            var connectedZones = GetConnectedZones(factionId);
            var connectedIds = new HashSet<string>(connectedZones.Select(z => z.Id));

            return allOwnedZones.Where(z => !connectedIds.Contains(z.Id));
        }

        /// <inheritdoc />
        public bool HasSupplyLine(string factionId, string zoneId)
        {
            ValidateFactionId(factionId);
            ValidateZoneId(zoneId);

            var hqZoneId = GetHeadquarters(factionId);

            // If no HQ set, all zones are self-sufficient (have supply line)
            if (hqZoneId == null)
                return true;

            return IsConnectedToHeadquarters(factionId, zoneId);
        }

        private void ValidateFactionId(string factionId)
        {
            if (factionId == null)
                throw new ArgumentNullException(nameof(factionId));
            if (string.IsNullOrWhiteSpace(factionId))
                throw new ArgumentException("Faction ID cannot be empty or whitespace.", nameof(factionId));
        }

        private void ValidateZoneId(string zoneId)
        {
            if (zoneId == null)
                throw new ArgumentNullException(nameof(zoneId));
            if (string.IsNullOrWhiteSpace(zoneId))
                throw new ArgumentException("Zone ID cannot be empty or whitespace.", nameof(zoneId));
        }
    }
}
