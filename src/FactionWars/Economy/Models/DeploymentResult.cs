using FactionWars.Core.Models;

namespace FactionWars.Economy.Models
{
    /// <summary>Result of buying and deploying defenders directly to a zone.</summary>
    public class DeploymentResult
    {
        public DeploymentStatus Status { get; }
        public bool Success => Status == DeploymentStatus.Success;
        public DefenderRole Tier { get; }
        public int Count { get; }
        public int TotalCost { get; }
        public string Message { get; }

        public static DeploymentResult Deployed(DefenderRole tier, int count, int cost) =>
            new DeploymentResult(DeploymentStatus.Success, tier, count, cost, $"Deployed {count} {tier}");

        public static DeploymentResult Unaffordable(DefenderRole tier, int count, int cost) =>
            new DeploymentResult(DeploymentStatus.InsufficientFunds, tier, count, cost, "Not enough cash");

        private DeploymentResult(DeploymentStatus status, DefenderRole tier, int count, int totalCost, string message)
        {
            Status = status;
            Tier = tier;
            Count = count;
            TotalCost = totalCost;
            Message = message;
        }
    }
}
