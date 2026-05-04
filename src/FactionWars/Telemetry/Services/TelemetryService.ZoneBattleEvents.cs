using System;
using FactionWars.Combat.Models;
using FactionWars.ScriptHookV.Logging;
using FactionWars.Telemetry.Models;
using FactionWars.Territory.Events;

namespace FactionWars.Telemetry.Services
{
    public sealed partial class TelemetryService
    {
        private void OnZoneOwnershipChanged(object? sender, ZoneOwnershipChangedEventArgs e)
        {
            if (_disposed) return;
            FileLogger.Debug($"TelemetryService.OnZoneOwnershipChanged: zone={e.ZoneId} prev={e.PreviousOwner ?? "null"} new={e.NewOwner ?? "null"}");
            try
            {
                var ts = DateTime.Now;
                var pt = _gameStateManager.TotalPlayTimeSeconds;
                if (e.NewOwner == null)
                {
                    _sink.WriteZoneEvent(new ZoneEventRow(ts, pt,
                        ZoneEventType.Neutralized, e.ZoneId, e.PreviousOwner, null));
                    return;
                }

                _sink.WriteZoneEvent(new ZoneEventRow(ts, pt,
                    ZoneEventType.Captured, e.ZoneId, e.PreviousOwner, e.NewOwner));

                if (e.PreviousOwner != null)
                {
                    _sink.WriteZoneEvent(new ZoneEventRow(ts, pt,
                        ZoneEventType.Lost, e.ZoneId, e.PreviousOwner, e.NewOwner));
                }
            }
            catch (Exception ex)
            {
                FileLogger.Error("TelemetryService.OnZoneOwnershipChanged failed", ex);
            }
        }

        private void OnBattleStarted(ZoneBattle b)
        {
            if (_disposed) return;
            FileLogger.Debug($"TelemetryService.OnBattleStarted: zone={b.ZoneId} attacker={b.AttackerFactionId} defender={b.DefenderFactionId}");
            try
            {
                var attackerFactionId = GetBattleFactionId(b, BattleRole.Attacker);
                var defenderFactionId = GetBattleFactionId(b, BattleRole.Defender);
                _sink.WriteBattle(new BattleEventRow(
                    DateTime.Now, _gameStateManager.TotalPlayTimeSeconds,
                    BattleEventType.Started, b.ZoneId,
                    attackerFactionId, defenderFactionId,
                    GetBattleAliveCount(b, BattleRole.Attacker),
                    GetBattleAliveCount(b, BattleRole.Defender),
                    outcome: null, attackerCasualties: 0, defenderCasualties: 0));
            }
            catch (Exception ex)
            {
                FileLogger.Error("TelemetryService.OnBattleStarted failed", ex);
            }
        }

        private void OnBattleEnded(ZoneBattle b, BattleOutcome outcome)
        {
            if (_disposed) return;
            FileLogger.Debug($"TelemetryService.OnBattleEnded: zone={b.ZoneId} outcome={outcome}");
            try
            {
                var attackerFactionId = GetBattleFactionId(b, BattleRole.Attacker);
                var defenderFactionId = GetBattleFactionId(b, BattleRole.Defender);
                var attackerTroops = GetBattleAliveCount(b, BattleRole.Attacker);
                var defenderTroops = GetBattleAliveCount(b, BattleRole.Defender);
                var attackerCasualties = Math.Max(0, b.InitialAttackerTroops - attackerTroops);
                var defenderCasualties = Math.Max(0, b.InitialDefenderTroops - defenderTroops);
                _sink.WriteBattle(new BattleEventRow(
                    DateTime.Now, _gameStateManager.TotalPlayTimeSeconds,
                    BattleEventType.Ended, b.ZoneId,
                    attackerFactionId, defenderFactionId,
                    attackerTroops, defenderTroops,
                    outcome: outcome,
                    attackerCasualties: attackerCasualties,
                    defenderCasualties: defenderCasualties));
            }
            catch (Exception ex)
            {
                FileLogger.Error("TelemetryService.OnBattleEnded failed", ex);
            }
        }

    }
}
