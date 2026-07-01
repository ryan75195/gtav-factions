namespace FactionWars.Economy.Interfaces
{
    /// <summary>Buys, tracks, and consumes support-squad packages (a callable battle reinforcement).</summary>
    public interface ISupportPackageService
    {
        int GetSupportSquadCost();
        bool CanAfford();
        bool PurchaseSupportSquad(string factionId);
        int GetOwnedCount(string factionId);
        bool TryConsume(string factionId);
    }
}
