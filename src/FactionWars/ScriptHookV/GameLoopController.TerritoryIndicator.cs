using FactionWars.Core.Models;
using FactionWars.Territory.Models;
using FactionWars.UI.Models;

namespace FactionWars.ScriptHookV
{
    public partial class GameLoopController
    {
        private void UpdateTerritoryIndicator(string? playerFactionId)
        {
            if (_territoryIndicatorRenderer == null || _territoryManager == null)
                return;

            var currentZone = _territoryManager.CurrentZone;
            if (currentZone == null)
            {
                _territoryIndicatorRenderer.Hide();
                _territoryIndicatorRenderer.Draw();
                return;
            }

            var territoryData = BuildTerritoryIndicatorData(currentZone, playerFactionId);
            _territoryIndicatorRenderer.Render(territoryData);
            _territoryIndicatorRenderer.Draw();
        }

        private TerritoryIndicatorData BuildTerritoryIndicatorData(Zone currentZone, string? playerFactionId)
        {
            var ownerFaction = currentZone.OwnerFactionId != null
                ? _factionService.GetFaction(currentZone.OwnerFactionId)
                : null;
            bool isPlayerOwned = currentZone.OwnerFactionId == playerFactionId;
            var activeBattle = _zoneBattleManager?.GetBattleForZone(currentZone.Id);
            var playerBattle = _zoneBattleManager?.GetPlayerCurrentBattle();
            bool isDefendingBattle = activeBattle != null && activeBattle.DefenderFactionId == playerFactionId;
            bool isPlayerAttackingHere = playerBattle != null && playerBattle.ZoneId == currentZone.Id && playerBattle.IsPlayerAttacking;
            var counts = GetTerritoryHudCounts(currentZone, playerFactionId, isPlayerOwned, activeBattle, playerBattle, isDefendingBattle, isPlayerAttackingHere);

            return new TerritoryIndicatorData(
                currentZone.Name,
                ownerFaction?.Name,
                ownerFaction?.Color,
                currentZone.ControlPercentage,
                isDefendingBattle || isPlayerAttackingHere,
                isPlayerOwned,
                deployedDefenderCount: counts.Deployed,
                reserveDefenderCount: counts.Reserve,
                playerTroopCount: counts.PlayerTroops,
                enemyDefenderCount: counts.EnemyDefenders,
                enemyReserveCount: counts.EnemyReserve);
        }

    }
}
