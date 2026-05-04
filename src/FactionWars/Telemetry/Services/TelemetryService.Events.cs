using System;
using FactionWars.AI.Interfaces;
using FactionWars.Combat.Events;
using FactionWars.Core.Interfaces;
using FactionWars.Economy.Interfaces;
using FactionWars.ScriptHookV.Logging;
using FactionWars.ScriptHookV.Managers;
using FactionWars.Telemetry.Models;
using FactionWars.Territory.Events;

namespace FactionWars.Telemetry.Services
{
    public sealed partial class TelemetryService
    {
        private void OnAIDecision(object? sender, AIDecisionEventArgs e)
        {
            if (_disposed) return;
            try
            {
                _sink.WriteDecision(new DecisionEventRow(
                    DateTime.Now, _gameStateManager.TotalPlayTimeSeconds,
                    e.FactionId, e.Decision.DecisionType, e.Decision.TargetZoneId,
                    e.Decision.TroopsToCommit, e.Decision.Priority, executed: true));
            }
            catch (Exception ex)
            {
                FileLogger.Error("TelemetryService.OnAIDecision failed", ex);
            }
        }

        private void OnTroopsAllocated(object? sender, TroopsAllocatedEventArgs e)
        {
            if (_disposed) return;
            try
            {
                // Default Source to AI for now — player allocation isn't currently wired
                // through this event. Source becomes meaningful once UI flows are added.
                _sink.WriteAllocation(new AllocationEventRow(
                    DateTime.Now, _gameStateManager.TotalPlayTimeSeconds,
                    e.FactionId, e.ZoneId, e.Tier, e.Count, AllocationSource.AI));
            }
            catch (Exception ex)
            {
                FileLogger.Error("TelemetryService.OnTroopsAllocated failed", ex);
            }
        }

        private void OnTroopsRecruited(object? sender, FactionWars.AI.Events.TroopsRecruitedEventArgs e)
        {
            if (_disposed) return;
            FileLogger.Debug($"TelemetryService.OnTroopsRecruited: faction={e.FactionId} troops={e.TroopsRecruited}");
            try
            {
                _sink.WriteRecruitment(new RecruitmentEventRow(
                    DateTime.Now, _gameStateManager.TotalPlayTimeSeconds,
                    e.FactionId, e.TroopsRecruited, e.Cost, e.CashBefore, e.CashAfter));
            }
            catch (Exception ex)
            {
                FileLogger.Error("TelemetryService.OnTroopsRecruited failed", ex);
            }
        }

        private void OnResourceTick(object? sender, ResourceTickEventArgs e)
        {
            if (_disposed) return;
            try
            {
                // ZonesContributing is set to 0 because the ResourceTickEventArgs does not
                // expose a zone count, and we deliberately do NOT couple TelemetryService
                // to IZoneService for an extra side-call: the current event set is the
                // contract, and a richer DTO is a future enhancement (see plan task 11).
                _sink.WriteResourceTick(new ResourceTickEventRow(
                    DateTime.Now, _gameStateManager.TotalPlayTimeSeconds,
                    e.FactionId, income: e.CashGenerated, zonesContributing: 0));
            }
            catch (Exception ex)
            {
                FileLogger.Error("TelemetryService.OnResourceTick failed", ex);
            }
        }

        private void OnAttackerKilled(object? sender, AttackerKilledEventArgs e)
        {
            if (_disposed) return;
            try
            {
                var row = PlayerKillResolver.Resolve(e, _getPlayerPedHandle(),
                    DateTime.Now, _gameStateManager.TotalPlayTimeSeconds);
                if (row != null) _sink.WritePlayerEvent(row);
            }
            catch (Exception ex)
            {
                FileLogger.Error("TelemetryService.OnAttackerKilled failed", ex);
            }
        }

    }
}
