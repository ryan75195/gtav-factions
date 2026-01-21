using System;
using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Models;
using FactionWars.UI.Interfaces;
using FactionWars.UI.Models;

namespace FactionWars.UI.Services
{
    /// <summary>
    /// Service for managing the combat HUD display.
    /// Coordinates between combat encounter data and the HUD renderer.
    /// </summary>
    public class CombatHudService : ICombatHudService
    {
        private readonly IReinforcementService _reinforcementService;
        private readonly ICombatHudRenderer _renderer;

        /// <summary>
        /// Gets whether the combat HUD is currently visible.
        /// </summary>
        public bool IsVisible => _renderer.IsVisible;

        /// <summary>
        /// Creates a new combat HUD service.
        /// </summary>
        /// <param name="reinforcementService">Service for reinforcement cooldown data.</param>
        /// <param name="renderer">The HUD renderer implementation.</param>
        public CombatHudService(IReinforcementService reinforcementService, ICombatHudRenderer renderer)
        {
            _reinforcementService = reinforcementService ?? throw new ArgumentNullException(nameof(reinforcementService));
            _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        }

        /// <summary>
        /// Updates the combat HUD with current encounter data.
        /// </summary>
        /// <param name="encounter">The current combat encounter.</param>
        /// <param name="playerFactionId">The player's faction ID.</param>
        /// <param name="zoneName">The display name of the zone.</param>
        /// <param name="defenderReserveCount">Number of defender reserves remaining.</param>
        public void Update(CombatEncounter encounter, string playerFactionId, string zoneName, int defenderReserveCount = 0)
        {
            if (encounter == null)
                throw new ArgumentNullException(nameof(encounter));
            if (playerFactionId == null)
                throw new ArgumentNullException(nameof(playerFactionId));
            if (string.IsNullOrWhiteSpace(playerFactionId))
                throw new ArgumentException("Player faction ID cannot be empty or whitespace.", nameof(playerFactionId));

            // Hide HUD if encounter is not active
            if (!encounter.IsActive)
            {
                _renderer.HideCombatHud();
                return;
            }

            // Check if player is involved in this combat
            bool isPlayerAttacker = encounter.AttackingFactionId == playerFactionId;
            bool isPlayerDefender = encounter.DefendingFactionId == playerFactionId;

            if (!isPlayerAttacker && !isPlayerDefender)
            {
                // Player is not involved in this combat
                _renderer.HideCombatHud();
                return;
            }

            // Get reinforcement cooldown for player's faction
            float cooldownSeconds = _reinforcementService.GetRemainingCooldown(playerFactionId, encounter);

            var hudData = new CombatHudData(
                zoneId: encounter.ZoneId,
                zoneName: zoneName ?? encounter.ZoneId,
                attackerFactionId: encounter.AttackingFactionId,
                defenderFactionId: encounter.DefendingFactionId,
                attackerControlPercent: encounter.AttackerControlPercentage,
                defenderControlPercent: encounter.DefenderControlPercentage,
                attackerPedCount: encounter.AttackerPedCount,
                defenderPedCount: encounter.DefenderPedCount,
                defenderReserveCount: defenderReserveCount,
                reinforcementCooldownSeconds: cooldownSeconds,
                isPlayerAttacker: isPlayerAttacker,
                combatDuration: encounter.GetDuration());

            _renderer.RenderCombatHud(hudData);
        }

        /// <summary>
        /// Hides the combat HUD.
        /// </summary>
        public void Hide()
        {
            _renderer.HideCombatHud();
        }
    }
}
