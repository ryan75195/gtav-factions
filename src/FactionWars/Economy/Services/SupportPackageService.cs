using System;
using FactionWars.Core.Interfaces;
using FactionWars.Economy.Interfaces;
using FactionWars.Factions.Interfaces;

namespace FactionWars.Economy.Services
{
    /// <inheritdoc />
    public sealed class SupportPackageService : ISupportPackageService
    {
        private const int SupportSquadCost = 25000;

        private readonly IGameBridge _gameBridge;
        private readonly IFactionService _factionService;

        public SupportPackageService(IGameBridge gameBridge, IFactionService factionService)
        {
            _gameBridge = gameBridge ?? throw new ArgumentNullException(nameof(gameBridge));
            _factionService = factionService ?? throw new ArgumentNullException(nameof(factionService));
        }

        public int GetSupportSquadCost() => SupportSquadCost;

        public bool CanAfford() => _gameBridge.GetPlayerMoney() >= SupportSquadCost;

        public bool PurchaseSupportSquad(string factionId)
        {
            var state = _factionService.GetFactionState(factionId);
            if (state == null || _gameBridge.GetPlayerMoney() < SupportSquadCost) return false;
            _gameBridge.AddPlayerMoney(-SupportSquadCost);
            state.AddSupportSquadPackage();
            return true;
        }

        public int GetOwnedCount(string factionId)
            => _factionService.GetFactionState(factionId)?.SupportSquadPackages ?? 0;

        public bool TryConsume(string factionId)
        {
            var state = _factionService.GetFactionState(factionId);
            return state != null && state.TryConsumeSupportSquadPackage();
        }
    }
}
