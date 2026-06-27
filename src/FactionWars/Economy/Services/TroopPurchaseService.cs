using System;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.Economy.Interfaces;
using FactionWars.Economy.Models;
using FactionWars.Factions.Interfaces;

namespace FactionWars.Economy.Services
{
    /// <summary>
    /// Service for purchasing troops using player's real GTA V cash.
    /// Purchased troops are added to the faction's reserve pool.
    /// </summary>
    public class TroopPurchaseService : ITroopPurchaseService
    {
        private readonly IGameBridge _gameBridge;
        private readonly IDefenderRoleService _defenderRoleService;
        private readonly IFactionService _factionService;

        /// <summary>
        /// Creates a new TroopPurchaseService.
        /// </summary>
        /// <param name="gameBridge">The game bridge for GTA V interactions.</param>
        /// <param name="defenderRoleService">Service for defender tier costs.</param>
        /// <param name="factionService">Service for faction operations.</param>
        /// <exception cref="ArgumentNullException">Thrown if any parameter is null.</exception>
        public TroopPurchaseService(
            IGameBridge gameBridge,
            IDefenderRoleService defenderRoleService,
            IFactionService factionService)
        {
            _gameBridge = gameBridge ?? throw new ArgumentNullException(nameof(gameBridge));
            _defenderRoleService = defenderRoleService ?? throw new ArgumentNullException(nameof(defenderRoleService));
            _factionService = factionService ?? throw new ArgumentNullException(nameof(factionService));
        }

        /// <inheritdoc />
        public bool CanAfford(DefenderRole tier, int count)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), "Count cannot be negative.");

            if (count == 0)
                return true;

            int totalCost = CalculateTotalCost(tier, count);
            int playerMoney = _gameBridge.GetPlayerMoney();
            return playerMoney >= totalCost;
        }

        /// <inheritdoc />
        public int GetTroopCost(DefenderRole tier)
        {
            return _defenderRoleService.GetCost(tier);
        }

        /// <inheritdoc />
        public int CalculateTotalCost(DefenderRole tier, int count)
        {
            return GetTroopCost(tier) * count;
        }

        /// <inheritdoc />
        public TroopPurchaseResult PurchaseTroops(string factionId, DefenderRole tier, int count)
        {
            if (factionId == null)
                throw new ArgumentNullException(nameof(factionId));
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), "Count cannot be negative.");

            // Zero count is a success with no action
            if (count == 0)
                return TroopPurchaseResult.Successful(tier, 0, 0);

            int totalCost = CalculateTotalCost(tier, count);
            int playerMoney = _gameBridge.GetPlayerMoney();

            // Check if player can afford
            if (playerMoney < totalCost)
                return TroopPurchaseResult.Failed(tier, "Insufficient funds");

            // Deduct money from player (negative amount to subtract)
            _gameBridge.AddPlayerMoney(-totalCost);

            // Add troops to faction reserve pool
            _factionService.AddReserveTroops(factionId, tier, count);

            return TroopPurchaseResult.Successful(tier, count, totalCost);
        }

        /// <inheritdoc />
        public int GetPlayerMoney()
        {
            return _gameBridge.GetPlayerMoney();
        }
    }
}
