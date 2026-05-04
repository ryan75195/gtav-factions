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

        private static string SerializeSnapshot(FactionSnapshot r) => string.Join(",",
            HudTime(r.PlayTimeSeconds), L(r.PlayTimeSeconds), Esc(r.FactionId),
            I(r.Cash), I(r.TotalTroops), I(r.ZonesOwned),
            I(r.Basic), I(r.Medium), I(r.Heavy), I(r.Elite),
            I(r.ReserveTroops), I(r.DeployedTroops));

        private static string SerializeZoneEvent(ZoneEventRow r) => string.Join(",",
            HudTime(r.PlayTimeSeconds), L(r.PlayTimeSeconds),
            r.Type.ToString(), Esc(r.ZoneId),
            Esc(r.PreviousOwner), Esc(r.NewOwner));

        private static string SerializeBattle(BattleEventRow r) => string.Join(",",
            HudTime(r.PlayTimeSeconds), L(r.PlayTimeSeconds),
            r.Type.ToString(), Esc(r.ZoneId),
            Esc(r.AttackerFactionId), Esc(r.DefenderFactionId),
            I(r.AttackerTroops), I(r.DefenderTroops),
            r.Outcome.HasValue ? r.Outcome.Value.ToString() : string.Empty,
            I(r.AttackerCasualties), I(r.DefenderCasualties));

        private static string SerializeDecision(DecisionEventRow r) => string.Join(",",
            HudTime(r.PlayTimeSeconds), L(r.PlayTimeSeconds),
            Esc(r.FactionId), r.Type.ToString(),
            Esc(r.TargetZoneId), I(r.Troops), D(r.Priority),
            r.Executed ? "true" : "false");

        private static string SerializeRecruitment(RecruitmentEventRow r) => string.Join(",",
            HudTime(r.PlayTimeSeconds), L(r.PlayTimeSeconds), Esc(r.FactionId),
            I(r.TroopsRecruited), I(r.Cost), I(r.CashBefore), I(r.CashAfter));

        private static string SerializeAllocation(AllocationEventRow r) => string.Join(",",
            HudTime(r.PlayTimeSeconds), L(r.PlayTimeSeconds),
            Esc(r.FactionId), Esc(r.ZoneId),
            r.Tier.ToString(), I(r.Count), r.Source.ToString());

        private static string SerializeResourceTick(ResourceTickEventRow r) => string.Join(",",
            HudTime(r.PlayTimeSeconds), L(r.PlayTimeSeconds), Esc(r.FactionId),
            I(r.Income), I(r.ZonesContributing));

        private static string SerializeMatchMeta(MatchMetaEventRow r) => string.Join(",",
            HudTime(r.PlayTimeSeconds), L(r.PlayTimeSeconds),
            r.Type.ToString(), Esc(r.Details));

        private static string SerializePlayerEvent(PlayerEventRow r) => string.Join(",",
            HudTime(r.PlayTimeSeconds), L(r.PlayTimeSeconds), r.Type.ToString(),
            Esc(r.ZoneId), Esc(r.TargetFaction),
            r.TargetTier.HasValue ? r.TargetTier.Value.ToString() : string.Empty,
            Esc(r.Details));
    }
}
