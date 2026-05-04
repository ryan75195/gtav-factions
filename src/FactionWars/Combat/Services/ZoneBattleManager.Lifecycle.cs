using System.Collections.Generic;
using System.Linq;
using FactionWars.Combat.Models;
using FactionWars.Core.Models;
using FactionWars.ScriptHookV.Logging;
using FactionWars.Territory.Models;

namespace FactionWars.Combat.Services
{
    public partial class ZoneBattleManager
    {
        public void EndBattle(string zoneId, BattleOutcome outcome)
        {
            if (string.IsNullOrEmpty(zoneId))
                return;

            if (_battlesByZone.TryGetValue(zoneId, out var battle))
            {
                _battlesByZone.Remove(zoneId);
                BattleEnded?.Invoke(battle, outcome);
            }
        }

        /// <inheritdoc />
        public void OnPlayerEnteredZone(Zone zone)
        {
            if (zone == null)
                return;

            if (_battlesByZone.TryGetValue(zone.Id, out var battle))
            {
                battle.IsPlayerPresent = true;
            }
        }

        /// <inheritdoc />
        public void OnPlayerExitedZone(Zone zone)
        {
            if (zone == null)
                return;

            if (_battlesByZone.TryGetValue(zone.Id, out var battle))
            {
                battle.IsPlayerPresent = false;
            }
        }

        /// <inheritdoc />
        public void SetPlayerFaction(string? factionId)
        {
            _playerFactionId = factionId;
        }

        /// <inheritdoc />
        public void Tick(float deltaTime)
        {
            var battlesToRemove = new List<string>();

            foreach (var kvp in _battlesByZone)
            {
                var battle = kvp.Value;

                // Only process tick-based combat if player is not present
                if (battle.IsPlayerPresent)
                {
                    // Still check for battle end
                    if (!battle.IsOngoing)
                    {
                        EndBattleAtTick(battle, battlesToRemove);
                    }
                    continue;
                }

                // Advance time
                battle.AdvanceTime(deltaTime);

                // Check if it's time for a kill
                if (battle.TimeUntilNextKill <= 0)
                {
                    ProcessKill(battle);
                    battle.ResetKillTimer();

                    // Check if battle ended
                    if (!battle.IsOngoing)
                    {
                        EndBattleAtTick(battle, battlesToRemove);
                    }
                }
            }

            // Remove completed battles
            foreach (var zoneId in battlesToRemove)
            {
                _battlesByZone.Remove(zoneId);
            }
        }

        /// <summary>
        /// Routes a Tick-driven battle end through the same outcome-application
        /// pipeline as <see cref="ResolveBattleIfDone"/>, so player wins still
        /// neutralize the zone (Q5.A) regardless of which path detected the end.
        /// </summary>
        private void EndBattleAtTick(ZoneBattle battle, List<string> battlesToRemove)
        {
            var outcome = DetermineOutcome(battle);
            var alive = battle.Participants.Where(p => p.AliveCount > 0).ToList();
            battlesToRemove.Add(battle.ZoneId);
            ApplyBattleOutcome(battle, outcome, alive);
            BattleEnded?.Invoke(battle, outcome);
        }

        /// <inheritdoc />
        public void ReportTroopKilled(string zoneId, string factionId, DefenderTier tier)
        {
            if (string.IsNullOrEmpty(zoneId) || string.IsNullOrEmpty(factionId))
                return;

            if (!_battlesByZone.TryGetValue(zoneId, out var battle))
                return;

            var victim = battle.Participants.FirstOrDefault(p => p.FactionId == factionId);
            if (victim == null)
            {
                FileLogger.Combat($"ReportTroopKilled: faction '{factionId}' not in battle '{zoneId}'.");
                return;
            }

            bool removed = victim.RemoveTroop(tier);
            if (removed)
            {
                string side = victim.Role == BattleRole.Defender ? "defender" : "attacker";
                TroopKilled?.Invoke(battle, tier, side);
                ResolveBattleIfDone(battle);
            }
        }

        /// <inheritdoc />
        public ZoneBattle? GetBattleForZone(string zoneId)
        {
            if (string.IsNullOrEmpty(zoneId))
                return null;

            _battlesByZone.TryGetValue(zoneId, out var battle);
            return battle;
        }

        /// <inheritdoc />
        public IReadOnlyList<ZoneBattle> GetAllActiveBattles()
        {
            return new List<ZoneBattle>(_battlesByZone.Values).AsReadOnly();
        }

    }
}
