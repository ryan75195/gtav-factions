using System;
using FactionWars.Factions.Interfaces;
using FactionWars.Factions.Models;
using FactionWars.Territory.Models;
using FactionWars.UI.Interfaces;
using FactionWars.UI.Models;

namespace FactionWars.UI.Services
{
    /// <summary>
    /// Service for managing the territory indicator HUD.
    /// Coordinates between zone data and the HUD renderer.
    /// </summary>
    public class TerritoryIndicatorService : ITerritoryIndicatorService
    {
        private readonly IFactionRepository _factionRepository;
        private readonly ITerritoryIndicatorRenderer _renderer;

        /// <summary>
        /// Gets whether the territory indicator is currently visible.
        /// </summary>
        public bool IsVisible => _renderer.IsVisible;

        /// <summary>
        /// Creates a new territory indicator service.
        /// </summary>
        /// <param name="factionRepository">Repository for faction lookups.</param>
        /// <param name="renderer">The HUD renderer implementation.</param>
        /// <exception cref="ArgumentNullException">Thrown if any parameter is null.</exception>
        public TerritoryIndicatorService(IFactionRepository factionRepository, ITerritoryIndicatorRenderer renderer)
        {
            _factionRepository = factionRepository ?? throw new ArgumentNullException(nameof(factionRepository));
            _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        }

        /// <summary>
        /// Updates the territory indicator with the current zone data.
        /// </summary>
        /// <param name="currentZone">The zone the player is currently in, or null if outside all zones.</param>
        /// <param name="playerFactionId">The player's faction ID.</param>
        /// <exception cref="ArgumentNullException">Thrown if playerFactionId is null.</exception>
        /// <exception cref="ArgumentException">Thrown if playerFactionId is empty or whitespace.</exception>
        public void Update(Zone? currentZone, string playerFactionId)
        {
            // If player is not in any zone, hide the indicator
            if (currentZone == null)
            {
                _renderer.Hide();
                return;
            }

            // Validate player faction ID
            if (playerFactionId == null)
                throw new ArgumentNullException(nameof(playerFactionId));
            if (string.IsNullOrWhiteSpace(playerFactionId))
                throw new ArgumentException("Player faction ID cannot be empty or whitespace.", nameof(playerFactionId));

            // Get owner faction details
            string? ownerFactionName = null;
            FactionColor? ownerFactionColor = null;
            bool isPlayerOwned = false;

            if (currentZone.OwnerFactionId != null)
            {
                var ownerFaction = _factionRepository.GetById(currentZone.OwnerFactionId);
                if (ownerFaction != null)
                {
                    ownerFactionName = ownerFaction.Name;
                    ownerFactionColor = ownerFaction.Color;
                }
                else
                {
                    ownerFactionName = "Unknown";
                    ownerFactionColor = null;
                }

                isPlayerOwned = currentZone.OwnerFactionId == playerFactionId;
            }

            var indicatorData = new TerritoryIndicatorData(
                zoneName: currentZone.Name,
                ownerFactionName: ownerFactionName,
                ownerFactionColor: ownerFactionColor,
                controlPercentage: currentZone.ControlPercentage,
                isContested: currentZone.IsContested,
                isPlayerOwned: isPlayerOwned);

            _renderer.Render(indicatorData);
        }

        /// <summary>
        /// Hides the territory indicator.
        /// </summary>
        public void Hide()
        {
            _renderer.Hide();
        }
    }
}
