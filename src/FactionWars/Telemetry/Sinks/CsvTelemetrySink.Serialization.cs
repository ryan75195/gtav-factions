using System;
using System.Globalization;
using System.Linq;
using FactionWars.Telemetry.Models;

namespace FactionWars.Telemetry.Sinks
{
    public sealed partial class CsvTelemetrySink
    {
        private static string HudTime(long playTimeSeconds)
        {
            if (playTimeSeconds < 0) playTimeSeconds = 0;
            var hours = playTimeSeconds / 3600;
            var minutes = (playTimeSeconds % 3600) / 60;
            var seconds = playTimeSeconds % 60;
            return string.Format(CultureInfo.InvariantCulture, "{0:D2}:{1:D2}:{2:D2}", hours, minutes, seconds);
        }
        private static string Esc(string? v) => CsvFieldEscaper.Escape(v);
        private static string I(int v) => v.ToString(CultureInfo.InvariantCulture);
        private static string L(long v) => v.ToString(CultureInfo.InvariantCulture);
        private static string D(double v) => v.ToString("G", CultureInfo.InvariantCulture);
        private static string Utc(DateTime v) => v.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
        private static string CreateSessionId()
            => DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffZ", CultureInfo.InvariantCulture)
                + "-"
                + Guid.NewGuid().ToString("N").Substring(0, 8);

        private string Prefix(DateTime timestamp, long playTimeSeconds) => string.Join(",",
            Esc(_sessionId), Utc(timestamp), HudTime(playTimeSeconds), L(playTimeSeconds));

        private string SerializeSnapshot(FactionSnapshot r) => string.Join(",",
            Prefix(r.Timestamp, r.PlayTimeSeconds), Esc(r.FactionId),
            I(r.Cash), I(r.TotalTroops), I(r.ZonesOwned),
            I(r.Basic), I(r.Medium), I(r.Heavy), I(r.Elite),
            I(r.ReserveTroops), I(r.DeployedTroops));

        private string SerializeZoneEvent(ZoneEventRow r) => string.Join(",",
            Prefix(r.Timestamp, r.PlayTimeSeconds), r.Type.ToString(), Esc(r.ZoneId),
            Esc(r.PreviousOwner), Esc(r.NewOwner));

        private string SerializeBattle(BattleEventRow r) => string.Join(",",
            Prefix(r.Timestamp, r.PlayTimeSeconds), r.Type.ToString(), Esc(r.ZoneId),
            Esc(r.AttackerFactionId), Esc(r.DefenderFactionId),
            I(r.AttackerTroops), I(r.DefenderTroops),
            r.Outcome.HasValue ? r.Outcome.Value.ToString() : string.Empty,
            I(r.AttackerCasualties), I(r.DefenderCasualties));

        private string SerializeDecision(DecisionEventRow r) => string.Join(",",
            Prefix(r.Timestamp, r.PlayTimeSeconds), Esc(r.FactionId), r.Type.ToString(),
            Esc(r.TargetZoneId), I(r.Troops), D(r.Priority),
            r.Executed ? "true" : "false");

        private string SerializeRecruitment(RecruitmentEventRow r) => string.Join(",",
            Prefix(r.Timestamp, r.PlayTimeSeconds), Esc(r.FactionId),
            I(r.TroopsRecruited), I(r.Cost), I(r.CashBefore), I(r.CashAfter));

        private string SerializeAllocation(AllocationEventRow r) => string.Join(",",
            Prefix(r.Timestamp, r.PlayTimeSeconds), Esc(r.FactionId), Esc(r.ZoneId),
            r.Tier.ToString(), I(r.Count), r.Source.ToString());

        private string SerializeResourceTick(ResourceTickEventRow r) => string.Join(",",
            Prefix(r.Timestamp, r.PlayTimeSeconds), Esc(r.FactionId),
            I(r.Income), I(r.ZonesContributing));

        private string SerializeMatchMeta(MatchMetaEventRow r) => string.Join(",",
            Prefix(r.Timestamp, r.PlayTimeSeconds), r.Type.ToString(), Esc(r.Details));

        private string SerializePlayerEvent(PlayerEventRow r) => string.Join(",",
            Prefix(r.Timestamp, r.PlayTimeSeconds), r.Type.ToString(),
            Esc(r.ZoneId), Esc(r.TargetFaction),
            r.TargetTier.HasValue ? r.TargetTier.Value.ToString() : string.Empty,
            Esc(r.Details));
    }
}
