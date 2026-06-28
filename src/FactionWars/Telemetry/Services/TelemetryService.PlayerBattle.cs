using System;
using System.Linq;
using FactionWars.Combat.Models;
using FactionWars.ScriptHookV.Logging;
using FactionWars.Telemetry.Models;

namespace FactionWars.Telemetry.Services
{
    public sealed partial class TelemetryService
    {
        private void PollPlayerBattleState()
        {
            if (_zoneBattleManager == null) return;

            var battle = _zoneBattleManager.GetPlayerCurrentBattle();
            var zoneId = battle?.ZoneId;

            if (zoneId == _currentPlayerBattleZoneId)
            {
                _playerBattleEndedSinceLastPoll = false;
                return;
            }

            TryWritePlayerBattleTransition(battle, zoneId);
        }

        private void TryWritePlayerBattleTransition(ZoneBattle? battle, string? zoneId)
        {
            try
            {
                WritePlayerBattleExitIfNeeded(battle);
                WritePlayerBattleEnterIfNeeded(battle);
                _currentPlayerBattleZoneId = zoneId;
                _playerBattleEndedSinceLastPoll = false;
            }
            catch (Exception ex)
            {
                FileLogger.Error("TelemetryService.PollPlayerBattleState failed", ex);
            }
        }

        private void WritePlayerBattleExitIfNeeded(ZoneBattle? battle)
        {
            if (_currentPlayerBattleZoneId == null) return;

            var type = _playerBattleEndedSinceLastPoll
                ? PlayerEventType.BattleExited
                : PlayerEventType.BattleAbandoned;
            _sink.WritePlayerEvent(new PlayerEventRow(
                DateTime.Now,
                _gameStateManager.TotalPlayTimeSeconds,
                type,
                _currentPlayerBattleZoneId,
                targetFaction: null,
                targetTier: null,
                details: battle == null ? string.Empty : BuildBattleDetails(battle)));
        }

        private void WritePlayerBattleEnterIfNeeded(ZoneBattle? battle)
        {
            if (battle == null) return;

            _sink.WritePlayerEvent(new PlayerEventRow(
                DateTime.Now,
                _gameStateManager.TotalPlayTimeSeconds,
                PlayerEventType.BattleEntered,
                battle.ZoneId,
                GetOpposingFactionId(battle),
                targetTier: null,
                details: BuildBattleDetails(battle)));
        }

        private void WritePlayerBattleDeath()
        {
            if (_zoneBattleManager == null) return;

            var battle = _zoneBattleManager.GetPlayerCurrentBattle();
            if (battle == null) return;

            _sink.WritePlayerEvent(new PlayerEventRow(
                DateTime.Now,
                _gameStateManager.TotalPlayTimeSeconds,
                PlayerEventType.BattleDeath,
                battle.ZoneId,
                GetOpposingFactionId(battle),
                targetTier: null,
                details: BuildBattleDetails(battle)));
        }

        private static string BuildBattleDetails(ZoneBattle battle)
        {
            var player = battle.Participants.FirstOrDefault(p => p.IsPlayer);
            var details = new PlayerBattleDetails
            {
                BattleId = battle.Id,
                PlayerFaction = player?.FactionId,
                PlayerRole = player?.Role.ToString(),
                AttackerFactions = [.. battle.Attackers.Select(p => p.FactionId)],
                DefenderFaction = battle.DefenderFactionId,
                AttackersAlive = battle.TotalAttackerTroops,
                DefendersAlive = battle.TotalDefenderTroops
            };
            return Newtonsoft.Json.JsonConvert.SerializeObject(details);
        }

        private static string? GetOpposingFactionId(ZoneBattle battle)
        {
            var player = battle.Participants.FirstOrDefault(p => p.IsPlayer);
            if (player == null) return null;
            if (player.Role == BattleRole.Defender)
                return battle.Attackers.FirstOrDefault(p => !p.IsPlayer)?.FactionId;
            return battle.DefenderFactionId;
        }

        private sealed class PlayerBattleDetails
        {
            [Newtonsoft.Json.JsonProperty("battle_id")]
            public string BattleId { get; set; } = string.Empty;

            [Newtonsoft.Json.JsonProperty("player_faction")]
            public string? PlayerFaction { get; set; }

            [Newtonsoft.Json.JsonProperty("player_role")]
            public string? PlayerRole { get; set; }

            [Newtonsoft.Json.JsonProperty("attacker_factions")]
            public string[] AttackerFactions { get; set; } = [];

            [Newtonsoft.Json.JsonProperty("defender_faction")]
            public string DefenderFaction { get; set; } = string.Empty;

            [Newtonsoft.Json.JsonProperty("attackers_alive")]
            public int AttackersAlive { get; set; }

            [Newtonsoft.Json.JsonProperty("defenders_alive")]
            public int DefendersAlive { get; set; }
        }
    }
}
