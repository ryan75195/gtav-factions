using System.Collections.Generic;
using FactionWars.Telemetry.Interfaces;
using FactionWars.Telemetry.Models;

namespace FactionWars.Telemetry.Sinks
{
    /// <summary>
    /// No-op telemetry sink. Used in tests and as an opt-out via DI override.
    /// Every method is safe and discards its input.
    /// </summary>
    public sealed class NullTelemetrySink : ITelemetrySink
    {
        public void SetSaveFile(string saveFilename) { }
        public void WriteSnapshot(IReadOnlyList<FactionSnapshot> rows) { }
        public void WriteZoneEvent(ZoneEventRow row) { }
        public void WriteBattle(BattleEventRow row) { }
        public void WriteDecision(DecisionEventRow row) { }
        public void WriteRecruitment(RecruitmentEventRow row) { }
        public void WriteAllocation(AllocationEventRow row) { }
        public void WriteResourceTick(ResourceTickEventRow row) { }
        public void WriteMatchMeta(MatchMetaEventRow row) { }
        public void WritePlayerEvent(PlayerEventRow row) { }
        public void Dispose() { }
    }
}
