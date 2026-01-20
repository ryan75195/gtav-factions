using System;
using FactionWars.Core.Interfaces;
using FactionWars.ScriptHookV.Logging;
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
        /// Raised when the player enters a neutral zone (a zone with no owner).
        /// </summary>
        public event EventHandler<Zone>? NeutralZoneEntered;

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
                FileLogger.Zone($"Zone change detected: {_currentZone?.Name ?? "NULL"} -> {newZone?.Name ?? "NULL"}");
                FileLogger.Zone($"Player position: ({playerPosition.X:F1}, {playerPosition.Y:F1}, {playerPosition.Z:F1})");

                // Raise exit event for old zone
                if (_currentZone != null)
                {
                    FileLogger.Zone($"Raising ZoneExited event for {_currentZone.Name}");
                    ZoneExited?.Invoke(this, _currentZone);
                }

                // Update current zone
                _currentZone = newZone;

                // Raise enter event for new zone
                if (_currentZone != null)
                {
                    FileLogger.Zone($"Raising ZoneEntered event for {_currentZone.Name}");
                    ZoneEntered?.Invoke(this, _currentZone);

                    // Check if this is a neutral zone (no owner)
                    if (_currentZone.OwnerFactionId == null)
                    {
                        FileLogger.Zone($"Raising NeutralZoneEntered event for {_currentZone.Name}");
                        NeutralZoneEntered?.Invoke(this, _currentZone);
                    }
                }
                else
                {
                    FileLogger.Zone("Player is not in any defined zone");
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
