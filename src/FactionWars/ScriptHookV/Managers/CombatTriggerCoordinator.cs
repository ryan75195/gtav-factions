using System;
using FactionWars.Territory.Models;

namespace FactionWars.ScriptHookV.Managers
{
    /// <summary>
    /// Coordinates combat triggers based on territory events.
    /// Listens to TerritoryManager zone enter/exit events and triggers/aborts combat accordingly.
    /// </summary>
    public class CombatTriggerCoordinator
    {
        private readonly TerritoryManager _territoryManager;
        private readonly CombatManager _combatManager;
        private string _playerFactionId;

        /// <summary>
        /// Gets or sets the player's current faction ID.
        /// When the player switches characters, this should be updated.
        /// </summary>
        public string PlayerFactionId
        {
            get => _playerFactionId;
            set => _playerFactionId = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// Creates a new CombatTriggerCoordinator.
        /// </summary>
        /// <param name="territoryManager">The territory manager for zone events.</param>
        /// <param name="combatManager">The combat manager to trigger combat on.</param>
        /// <param name="playerFactionId">The player's initial faction ID.</param>
        /// <exception cref="ArgumentNullException">Thrown if any parameter is null.</exception>
        public CombatTriggerCoordinator(
            TerritoryManager territoryManager,
            CombatManager combatManager,
            string playerFactionId)
        {
            _territoryManager = territoryManager ?? throw new ArgumentNullException(nameof(territoryManager));
            _combatManager = combatManager ?? throw new ArgumentNullException(nameof(combatManager));
            _playerFactionId = playerFactionId ?? throw new ArgumentNullException(nameof(playerFactionId));

            // Subscribe to territory events
            _territoryManager.ZoneEntered += OnZoneEntered;
            _territoryManager.ZoneExited += OnZoneExited;
        }

        /// <summary>
        /// Handles the player entering a zone.
        /// Triggers combat if the zone is owned by an enemy faction.
        /// </summary>
        private void OnZoneEntered(object? sender, Zone zone)
        {
            // Don't trigger combat if zone is neutral (no owner)
            if (zone.OwnerFactionId == null)
                return;

            // Don't trigger combat if zone is owned by player's faction
            if (zone.OwnerFactionId == _playerFactionId)
                return;

            // Don't trigger if already in combat
            if (_combatManager.IsInCombat)
                return;

            // Start combat - player is attacking enemy zone
            _combatManager.StartCombat(zone, _playerFactionId);
        }

        /// <summary>
        /// Handles the player exiting a zone.
        /// Aborts combat if the player leaves while in combat.
        /// </summary>
        private void OnZoneExited(object? sender, Zone zone)
        {
            // If in combat and the zone being exited is the combat zone, abort combat
            if (_combatManager.IsInCombat &&
                _combatManager.CurrentEncounter != null &&
                _combatManager.CurrentEncounter.ZoneId == zone.Id)
            {
                _combatManager.AbortCombat();
            }
        }

        /// <summary>
        /// Unsubscribes from territory events. Call this when disposing the coordinator.
        /// </summary>
        public void Dispose()
        {
            _territoryManager.ZoneEntered -= OnZoneEntered;
            _territoryManager.ZoneExited -= OnZoneExited;
        }
    }
}
