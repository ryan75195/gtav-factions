using FactionWars.Core.Models;
using FactionWars.Economy.Models;

namespace FactionWars.Economy.Interfaces
{
    /// <summary>
    /// Service for purchasing troops using player's real GTA V cash.
    /// Purchased troops are added to the faction's reserve pool.
    /// </summary>
    public interface ITroopPurchaseService
    {
        /// <summary>
        /// Checks if the player can afford to purchase the specified troops.
        /// </summary>
        /// <param name="tier">The tier of troops to purchase.</param>
        /// <param name="count">The number of troops to purchase.</param>
        /// <returns>True if the player has sufficient funds.</returns>
        bool CanAfford(DefenderTier tier, int count);

        /// <summary>
        /// Gets the cost of a single troop of the specified tier.
        /// </summary>
        /// <param name="tier">The tier to query.</param>
        /// <returns>The cost in GTA V dollars.</returns>
        int GetTroopCost(DefenderTier tier);

        /// <summary>
        /// Calculates the total cost for purchasing the specified troops.
        /// </summary>
        /// <param name="tier">The tier of troops.</param>
        /// <param name="count">The number of troops.</param>
        /// <returns>The total cost in GTA V dollars.</returns>
        int CalculateTotalCost(DefenderTier tier, int count);

        /// <summary>
        /// Purchases troops and adds them to the faction's reserve pool.
        /// Deducts the cost from the player's real GTA V money.
        /// </summary>
        /// <param name="factionId">The faction to add troops to.</param>
        /// <param name="tier">The tier of troops to purchase.</param>
        /// <param name="count">The number of troops to purchase.</param>
        /// <returns>Result of the purchase operation.</returns>
        TroopPurchaseResult PurchaseTroops(string factionId, DefenderTier tier, int count);

        /// <summary>
        /// Gets the player's current money from GTA V.
        /// </summary>
        /// <returns>The player's current money in GTA V dollars.</returns>
        int GetPlayerMoney();
    }
}
