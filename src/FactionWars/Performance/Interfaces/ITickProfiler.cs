using System;

namespace FactionWars.Performance.Interfaces
{
    /// <summary>
    /// Times the named phases of a single game tick and emits diagnostics when a tick
    /// runs slow (per-phase breakdown) or while it is running slow (phase breadcrumb).
    /// </summary>
    public interface ITickProfiler
    {
        /// <summary>Marks the start of a tick; resets per-phase state.</summary>
        void BeginTick();

        /// <summary>Times <paramref name="body"/> as the named phase. Rethrows body exceptions after recording.</summary>
        void Measure(string phaseName, Action body);

        /// <summary>Marks the end of a tick; reports a summary if the tick was slow.</summary>
        void EndTick();
    }
}
