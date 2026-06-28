using System;
using System.Collections.Generic;
using System.Linq;
using FactionWars.Combat.Models;
using FactionWars.Core.Models;
using FactionWars.ScriptHookV.Logging;
using FactionWars.Territory.Models;

namespace FactionWars.ScriptHookV.Managers
{
    public partial class BattleAttackerManager
    {
        public void OnPlayerZoneExited(Zone zone)
        {
            if (zone == null) return;

            FileLogger.Combat($"BattleAttackerManager: Player exited zone {zone.Id}");

            if (_currentBattleZoneId == zone.Id)
                _currentBattleZoneId = null;

            if (!_spawnedPedTierByZone.TryGetValue(zone.Id, out var pedTiers)) return;

            // Despawn all attackers - the ZoneBattle already tracks the correct count
            // so when player re-enters, spawning will use the current battle state
            foreach (var pedHandle in pedTiers.Keys)
            {
                _pedBlipService.RemoveBlipForPed(pedHandle);
                _pedDespawnService.DespawnPed(pedHandle);
                _corpseDeathTimes.Remove(pedHandle);  // Clear any corpse tracking
            }
            _spawnedPedTierByZone.Remove(zone.Id);
            _spawnedPedFactionByZone.Remove(zone.Id);

            // Also delete any corpses that were from this zone
            var corpsesToRemove = new List<int>(_corpseDeathTimes.Keys);
            foreach (var pedHandle in corpsesToRemove)
            {
                _pedDespawnService.DeletePedEntity(pedHandle);
                _corpseDeathTimes.Remove(pedHandle);
            }
        }

        /// <summary>
        /// Despawns all attackers across all zones.
        /// </summary>
        public void DespawnAllAttackers()
        {
            foreach (var zonePedTiers in _spawnedPedTierByZone.Values)
            {
                foreach (var pedHandle in zonePedTiers.Keys)
                {
                    _pedBlipService.RemoveBlipForPed(pedHandle);
                    _pedDespawnService.DespawnPed(pedHandle);
                }
            }
            _spawnedPedTierByZone.Clear();
            _spawnedPedFactionByZone.Clear();
            _currentBattleZoneId = null;

            // Also delete any remaining corpses
            foreach (var pedHandle in _corpseDeathTimes.Keys)
            {
                _pedDespawnService.DeletePedEntity(pedHandle);
            }
            _corpseDeathTimes.Clear();
        }

        /// <summary>
        /// Gets the number of spawned attackers for a specific zone.
        /// </summary>
        public int GetSpawnedAttackerCount(string zoneId)
        {
            return _spawnedPedTierByZone.TryGetValue(zoneId, out var pedTiers) ? pedTiers.Count : 0;
        }

        /// <summary>
        /// Gets the number of spawned attackers of a specific tier in a zone.
        /// </summary>
        public int GetSpawnedCountByTier(string zoneId, DefenderRole tier)
        {
            if (!_spawnedPedTierByZone.TryGetValue(zoneId, out var pedTiers))
                return 0;

            return pedTiers.Values.Count(t => t == tier);
        }

        /// <summary>
        /// Sets the player's faction ID for determining which zones the player is defending.
        /// Called when the player switches characters.
        /// </summary>
        /// <param name="factionId">The new player faction ID.</param>
        public void SetPlayerFaction(string factionId)
        {
            _playerFactionId = factionId ?? throw new ArgumentNullException(nameof(factionId));
        }

        /// <summary>
        /// Updates attacker state. Should be called each game tick.
        /// Checks for deaths and reports kills to battle manager.
        /// </summary>
    }
}
