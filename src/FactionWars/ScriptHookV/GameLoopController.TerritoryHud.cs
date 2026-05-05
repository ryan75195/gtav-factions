using System;
using System.Linq;
using FactionWars.Combat.Models;
using FactionWars.Territory.Models;

namespace FactionWars.ScriptHookV
{
    public partial class GameLoopController
    {
        private TerritoryHudCounts GetTerritoryHudCounts(
            Zone currentZone,
            string? playerFactionId,
            bool isPlayerOwned,
            ZoneBattle? activeBattle,
            ZoneBattle? playerBattle,
            bool isDefendingBattle,
            bool isPlayerAttackingHere)
        {
            if (isDefendingBattle && activeBattle != null)
                return GetDefendingBattleHudCounts(currentZone, activeBattle);

            if (isPlayerOwned && _friendlyDefenderManager != null)
                return GetOwnedZoneHudCounts(currentZone, playerFactionId);

            if (isPlayerAttackingHere && playerBattle != null)
                return GetAttackingBattleHudCounts(playerBattle);

            return new TerritoryHudCounts();
        }

        private TerritoryHudCounts GetDefendingBattleHudCounts(Zone currentZone, ZoneBattle activeBattle)
        {
            int deployed = _friendlyDefenderManager?.GetSpawnedDefenderCount(currentZone.Id) ?? 0;
            int enemyDefenders = _battleAttackerManager?.GetSpawnedAttackerCount(currentZone.Id) ?? 0;
            return new TerritoryHudCounts
            {
                Deployed = deployed,
                Reserve = Math.Max(0, activeBattle.TotalDefenderTroops - deployed),
                EnemyDefenders = enemyDefenders,
                EnemyReserve = Math.Max(0, activeBattle.TotalAttackerTroops - enemyDefenders)
            };
        }

        private TerritoryHudCounts GetOwnedZoneHudCounts(Zone currentZone, string? playerFactionId)
        {
            int deployed = _friendlyDefenderManager!.GetSpawnedDefenderCount(currentZone.Id);
            int reserve = 0;
            if (_allocationService != null && playerFactionId != null)
            {
                var allocation = _allocationService.GetAllocation(playerFactionId, currentZone.Id);
                reserve = allocation != null ? Math.Max(0, allocation.TotalTroops - deployed) : 0;
            }

            return new TerritoryHudCounts { Deployed = deployed, Reserve = reserve };
        }

        private TerritoryHudCounts GetAttackingBattleHudCounts(ZoneBattle playerBattle)
        {
            var playerParticipant = playerBattle.Attackers.FirstOrDefault(p => p.IsPlayer);
            int spawnedEnemyDefenders = _enemyDefenderManager?.GetSpawnedDefenderCount(playerBattle.ZoneId) ?? 0;
            return new TerritoryHudCounts
            {
                PlayerTroops = playerParticipant?.AliveCount ?? 0,
                EnemyDefenders = spawnedEnemyDefenders,
                EnemyReserve = Math.Max(0, playerBattle.Defender.AliveCount - spawnedEnemyDefenders)
            };
        }

        private struct TerritoryHudCounts
        {
            public int Deployed;
            public int Reserve;
            public int PlayerTroops;
            public int EnemyDefenders;
            public int EnemyReserve;
        }

        /// <summary>
        /// Updates and draws the battle HUD showing active AI battles.
        /// </summary>
    }
}
