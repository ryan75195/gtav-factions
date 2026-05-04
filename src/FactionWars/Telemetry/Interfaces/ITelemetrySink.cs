using System;
using System.Collections.Generic;
using FactionWars.Telemetry.Models;

namespace FactionWars.Telemetry.Interfaces
{
    /// <summary>
    /// Sink for telemetry events. Implementations decide where rows go (CSV, no-op, etc.).
    /// All Write methods MUST be safe to call before SetSaveFile is called — implementations
    /// buffer in memory until a save filename is known.
    /// </summary>
    public interface ITelemetrySink : IDisposable
    {
        void SetSaveFile(string saveFilename);
        void WriteSnapshot(IReadOnlyList<FactionSnapshot> rows);
        void WriteZoneEvent(ZoneEventRow row);
        void WriteBattle(BattleEventRow row);
        void WriteDecision(DecisionEventRow row);
        void WriteRecruitment(RecruitmentEventRow row);
        void WriteAllocation(AllocationEventRow row);
        void WriteResourceTick(ResourceTickEventRow row);
        void WriteMatchMeta(MatchMetaEventRow row);
        void WritePlayerEvent(PlayerEventRow row);
    }
}
