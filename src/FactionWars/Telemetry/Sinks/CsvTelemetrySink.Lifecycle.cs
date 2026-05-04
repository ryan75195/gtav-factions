using System;
using System.Collections.Generic;
using System.Linq;

namespace FactionWars.Telemetry.Sinks
{
    public sealed partial class CsvTelemetrySink
    {
        public void Dispose()
        {
            lock (_lock)
            {
                if (_disposed) return;
                _disposed = true;
                _bufSnap.Clear(); _bufZone.Clear(); _bufBattle.Clear();
                _bufDecision.Clear(); _bufRecruit.Clear(); _bufAlloc.Clear();
                _bufTick.Clear(); _bufMeta.Clear(); _bufPlayer.Clear();
            }
        }

        private static void BufferLocked<T>(List<T> buffer, IEnumerable<T> rows)
        {
            foreach (var r in rows)
            {
                if (buffer.Count >= BufferCapPerType) buffer.RemoveAt(0);
                buffer.Add(r);
            }
        }

        private void FlushBuffersLocked()
        {
            FlushBufferLocked(_bufSnap, "snapshots.csv", SnapshotHeader, SerializeSnapshot);
            FlushBufferLocked(_bufZone, "zone_events.csv", ZoneEventHeader, SerializeZoneEvent);
            FlushBufferLocked(_bufBattle, "battles.csv", BattleHeader, SerializeBattle);
            FlushBufferLocked(_bufDecision, "decisions.csv", DecisionHeader, SerializeDecision);
            FlushBufferLocked(_bufRecruit, "recruitments.csv", RecruitmentHeader, SerializeRecruitment);
            FlushBufferLocked(_bufAlloc, "allocations.csv", AllocationHeader, SerializeAllocation);
            FlushBufferLocked(_bufTick, "resource_ticks.csv", ResourceTickHeader, SerializeResourceTick);
            FlushBufferLocked(_bufMeta, "match_meta.csv", MatchMetaHeader, SerializeMatchMeta);
            FlushBufferLocked(_bufPlayer, "player_events.csv", PlayerEventHeader, SerializePlayerEvent);
        }

        private void FlushBufferLocked<T>(
            List<T> buffer,
            string fileName,
            string header,
            Func<T, string> serialize)
        {
            if (buffer.Count == 0)
                return;

            AppendLocked(fileName, header, buffer.Select(serialize));
            buffer.Clear();
        }

    }
}
