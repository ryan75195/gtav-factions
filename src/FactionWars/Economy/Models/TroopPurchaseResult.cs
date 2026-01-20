using FactionWars.Core.Models;

namespace FactionWars.Economy.Models
{
    /// <summary>
    /// Result of a troop purchase operation.
    /// </summary>
    public class TroopPurchaseResult
    {
        /// <summary>
        /// Whether the purchase was successful.
        /// </summary>
        public bool Success { get; }

        /// <summary>
        /// The number of troops successfully purchased.
        /// </summary>
        public int TroopsPurchased { get; }

        /// <summary>
        /// The total cost deducted from player's money.
        /// </summary>
        public int TotalCost { get; }

        /// <summary>
        /// The tier of troops purchased.
        /// </summary>
        public DefenderTier Tier { get; }

        /// <summary>
        /// Error message if purchase failed, null otherwise.
        /// </summary>
        public string? ErrorMessage { get; }

        /// <summary>
        /// Creates a successful purchase result.
        /// </summary>
        public static TroopPurchaseResult Successful(DefenderTier tier, int count, int cost)
        {
            return new TroopPurchaseResult(true, tier, count, cost, null);
        }

        /// <summary>
        /// Creates a failed purchase result.
        /// </summary>
        public static TroopPurchaseResult Failed(DefenderTier tier, string errorMessage)
        {
            return new TroopPurchaseResult(false, tier, 0, 0, errorMessage);
        }

        private TroopPurchaseResult(bool success, DefenderTier tier, int troopsPurchased, int totalCost, string? errorMessage)
        {
            Success = success;
            Tier = tier;
            TroopsPurchased = troopsPurchased;
            TotalCost = totalCost;
            ErrorMessage = errorMessage;
        }
    }
}
