using System.Collections.Generic;
using System.Linq;
using FactionWars.Combat.Models;
using FactionWars.Core.Models;
using FactionWars.ScriptHookV.Logging;

namespace FactionWars.ScriptHookV.Managers
{
    public partial class FriendlyDefenderManager
    {
        public void OnBattleEnded(string zoneId)
        {
            _zonesInBattle.Remove(zoneId);

            if (!_spawnedPedTierByZone.TryGetValue(zoneId, out var pedTiers)) return;

            var zone = _zoneService.GetZone(zoneId);
            if (zone == null) return;

            foreach (var pedHandle in pedTiers.Keys)
            {
                // Switch back to bounded walking wander for peaceful patrol
                _gameBridge.TaskPedWanderInBoundedArea(pedHandle, zone.Center, zone.Radius);
            }
        }

        /// <summary>
        /// Despawns all friendly defenders and corpses across all zones.
        /// </summary>
        public void DespawnAllDefenders()
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

            // Also clean up all corpses
            foreach (var pedHandle in _corpseDeathTimes.Keys)
            {
                _pedDespawnService.DeletePedEntity(pedHandle);
            }
            _corpseDeathTimes.Clear();

            _zonesInBattle.Clear();
        }

        /// <summary>
        /// Gets the number of spawned defenders for a specific zone.
        /// </summary>
        /// <param name="zoneId">The zone ID.</param>
        /// <returns>The number of spawned defenders, or 0 if none.</returns>
        public int GetSpawnedDefenderCount(string zoneId)
        {
            return _spawnedPedTierByZone.TryGetValue(zoneId, out var pedTiers) ? pedTiers.Count : 0;
        }

        /// <inheritdoc />
        public IReadOnlyDictionary<int, DefenderTier> GetDefendersInZone(string zoneId)
        {
            if (_spawnedPedTierByZone.TryGetValue(zoneId, out var pedTiers))
            {
                // Return a snapshot copy so callers iterating in a tick aren't affected by
                // concurrent additions/removals from the same tick (e.g. defender death).
                return new Dictionary<int, DefenderTier>(pedTiers);
            }
            return new Dictionary<int, DefenderTier>();
        }

        /// <summary>
        /// Gets the number of spawned defenders of a specific tier in a zone.
        /// </summary>
        /// <param name="zoneId">The zone ID.</param>
        /// <param name="tier">The defender tier to count.</param>
        /// <returns>The number of spawned defenders of that tier.</returns>
        public int GetSpawnedCountByTier(string zoneId, DefenderTier tier)
        {
            if (!_spawnedPedTierByZone.TryGetValue(zoneId, out var pedTiers))
                return 0;

            return pedTiers.Values.Count(t => t == tier);
        }

        /// <summary>
        /// Updates defender state. Should be called each game tick.
        /// Checks for defender deaths, handles cleanup, spawns replacements, and triggers territory loss.
        /// </summary>
    }
}
