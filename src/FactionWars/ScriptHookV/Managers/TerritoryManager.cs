using System;
using FactionWars.Core.Interfaces;
using FactionWars.Territory.Interfaces;
using FactionWars.Territory.Models;

namespace FactionWars.ScriptHookV.Managers
{
    /// <summary>
    /// Manages territory detection based on player position.
    /// Tracks when the player enters and exits zones and raises appropriate events.
    /// </summary>
    public class TerritoryManager
    {
        private readonly IGameBridge _gameBridge;
        private readonly IZoneService _zoneService;
        private Zone? _currentZone;

        /// <summary>
        /// Gets the zone the player is currently in, or null if not in any zone.
        /// </summary>
        public Zone? CurrentZone => _currentZone;

        /// <summary>
        /// Raised when the player enters a zone.
        /// </summary>
        public event EventHandler<Zone>? ZoneEntered;

        /// <summary>
        /// Raised when the player exits a zone.
        /// </summary>
        public event EventHandler<Zone>? ZoneExited;

        /// <summary>
        /// Creates a new TerritoryManager.
        /// </summary>
        /// <param name="gameBridge">The game bridge for getting player position.</param>
        /// <param name="zoneService">The zone service for zone detection.</param>
        /// <exception cref="ArgumentNullException">Thrown if any parameter is null.</exception>
        public TerritoryManager(IGameBridge gameBridge, IZoneService zoneService)
        {
            _gameBridge = gameBridge ?? throw new ArgumentNullException(nameof(gameBridge));
            _zoneService = zoneService ?? throw new ArgumentNullException(nameof(zoneService));
        }

        /// <summary>
        /// Updates the territory detection based on the player's current position.
        /// Should be called each game tick.
        /// </summary>
        public void Update()
        {
            var playerPosition = _gameBridge.GetPlayerPosition();
            var newZone = _zoneService.GetZoneAtPosition(playerPosition);

            // Check if zone changed
            if (!ZonesAreEqual(_currentZone, newZone))
            {
                // Raise exit event for old zone
                if (_currentZone != null)
                {
                    ZoneExited?.Invoke(this, _currentZone);
                }

                // Update current zone
                _currentZone = newZone;

                // Raise enter event for new zone
                if (_currentZone != null)
                {
                    ZoneEntered?.Invoke(this, _currentZone);
                }
            }
        }

        /// <summary>
        /// Checks if the player is currently in territory owned by an enemy faction.
        /// </summary>
        /// <param name="playerFactionId">The player's faction ID.</param>
        /// <returns>True if in enemy territory, false otherwise.</returns>
        public bool IsInEnemyTerritory(string playerFactionId)
        {
            if (_currentZone == null)
                return false;

            // Neutral zones (no owner) are not enemy territory
            if (_currentZone.OwnerFactionId == null)
                return false;

            // If zone is owned by player's faction, it's not enemy territory
            if (_currentZone.OwnerFactionId == playerFactionId)
                return false;

            // Zone is owned by another faction
            return true;
        }

        private static bool ZonesAreEqual(Zone? zone1, Zone? zone2)
        {
            if (zone1 == null && zone2 == null)
                return true;
            if (zone1 == null || zone2 == null)
                return false;
            return zone1.Id == zone2.Id;
        }
    }
}
