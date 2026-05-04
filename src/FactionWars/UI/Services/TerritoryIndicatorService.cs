using System;
using System.Linq;
using FactionWars.Combat.Interfaces;
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
        private readonly IZoneBattleManager _battleManager;

        /// <summary>
        /// Gets whether the territory indicator is currently visible.
        /// </summary>
        public bool IsVisible => _renderer.IsVisible;

        /// <summary>
        /// Creates a new territory indicator service.
        /// </summary>
        /// <param name="factionRepository">Repository for faction lookups.</param>
        /// <param name="renderer">The HUD renderer implementation.</param>
        /// <param name="battleManager">The battle manager for 3-way battle data.</param>
        /// <exception cref="ArgumentNullException">Thrown if any parameter is null.</exception>
        public TerritoryIndicatorService(IFactionRepository factionRepository, ITerritoryIndicatorRenderer renderer, IZoneBattleManager battleManager)
        {
            _factionRepository = factionRepository ?? throw new ArgumentNullException(nameof(factionRepository));
            _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
            _battleManager = battleManager ?? throw new ArgumentNullException(nameof(battleManager));
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

            var owner = GetOwnerIndicatorInfo(currentZone, playerFactionId);
            var thirdParty = GetThirdPartyIndicatorInfo(currentZone);

            var indicatorData = new TerritoryIndicatorData(
                zoneName: currentZone.Name,
                ownerFactionName: owner.Name,
                ownerFactionColor: owner.Color,
                controlPercentage: currentZone.ControlPercentage,
                isContested: currentZone.IsContested,
                isPlayerOwned: owner.IsPlayerOwned,
                thirdPartyCount: thirdParty.Count,
                thirdPartyFactionColor: thirdParty.Color);

            _renderer.Render(indicatorData);
        }

        private OwnerIndicatorInfo GetOwnerIndicatorInfo(Zone currentZone, string playerFactionId)
        {
            if (currentZone.OwnerFactionId == null)
                return new OwnerIndicatorInfo(null, null, false);

            var ownerFaction = _factionRepository.GetById(currentZone.OwnerFactionId);
            return ownerFaction != null
                ? new OwnerIndicatorInfo(ownerFaction.Name, ownerFaction.Color, currentZone.OwnerFactionId == playerFactionId)
                : new OwnerIndicatorInfo("Unknown", null, currentZone.OwnerFactionId == playerFactionId);
        }

        private ThirdPartyIndicatorInfo GetThirdPartyIndicatorInfo(Zone currentZone)
        {
            var battle = _battleManager.GetBattleForZone(currentZone.Id);
            if (battle == null)
                return new ThirdPartyIndicatorInfo(0, null);

            var aiAttacker = battle.Attackers.FirstOrDefault(p => !p.IsPlayer);
            bool playerIsAttacking = battle.Attackers.Any(p => p.IsPlayer);
            if (aiAttacker == null || !playerIsAttacking)
                return new ThirdPartyIndicatorInfo(0, null);

            var thirdPartyFaction = _factionRepository.GetById(aiAttacker.FactionId);
            return new ThirdPartyIndicatorInfo(aiAttacker.AliveCount, thirdPartyFaction?.Color);
        }

        private readonly struct OwnerIndicatorInfo
        {
            public OwnerIndicatorInfo(string? name, FactionColor? color, bool isPlayerOwned)
            {
                Name = name;
                Color = color;
                IsPlayerOwned = isPlayerOwned;
            }

            public string? Name { get; }
            public FactionColor? Color { get; }
            public bool IsPlayerOwned { get; }
        }

        private readonly struct ThirdPartyIndicatorInfo
        {
            public ThirdPartyIndicatorInfo(int count, FactionColor? color)
            {
                Count = count;
                Color = color;
            }

            public int Count { get; }
            public FactionColor? Color { get; }
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
