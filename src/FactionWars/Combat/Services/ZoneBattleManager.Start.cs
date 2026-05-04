using System;
using System.Collections.Generic;
using FactionWars.Combat.Models;
using FactionWars.Core.Models;

namespace FactionWars.Combat.Services
{
    public partial class ZoneBattleManager
    {
        public ZoneBattle StartBattle(
            string zoneId,
            string attackerFactionId,
            string defenderFactionId,
            Dictionary<DefenderTier, int> attackerTroops,
            Dictionary<DefenderTier, int> defenderTroops)
        {
            if (string.IsNullOrEmpty(zoneId))
                throw new ArgumentNullException(nameof(zoneId));
            if (string.IsNullOrEmpty(attackerFactionId))
                throw new ArgumentNullException(nameof(attackerFactionId));
            if (string.IsNullOrEmpty(defenderFactionId))
                throw new ArgumentNullException(nameof(defenderFactionId));
            if (attackerTroops == null)
                throw new ArgumentNullException(nameof(attackerTroops));
            if (defenderTroops == null)
                throw new ArgumentNullException(nameof(defenderTroops));

            if (_battlesByZone.ContainsKey(zoneId))
            {
                throw new InvalidOperationException($"A battle already exists in zone '{zoneId}'.");
            }

            var battle = new ZoneBattle(
                attackerFactionId: attackerFactionId,
                defenderFactionId: defenderFactionId,
                zoneId: zoneId,
                attackerTroops: attackerTroops,
                defenderTroops: defenderTroops,
                playerFactionId: _playerFactionId);

            // Calculate kill interval based on total troops
            int totalTroops = battle.TotalAttackerTroops + battle.TotalDefenderTroops;
            float duration = Math.Max(MinBattleDuration, Math.Min(MaxBattleDuration, totalTroops * SecondsPerTroop));
            float killInterval = duration / Math.Max(1, totalTroops - 1);
            battle.SetKillInterval(killInterval);

            _battlesByZone[zoneId] = battle;

            BattleStarted?.Invoke(battle);

            return battle;
        }

        /// <inheritdoc />
    }
}
