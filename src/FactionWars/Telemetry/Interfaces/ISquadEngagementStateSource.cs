using System.Collections.Generic;
using FactionWars.Telemetry.Models;

namespace FactionWars.Telemetry.Interfaces
{
    /// <summary>Exposes the squad controller's per-ped engagement telemetry without coupling consumers
    /// to its internals: a pull snapshot for periodic sampling and a drain of phase-change events.</summary>
    public interface ISquadEngagementStateSource
    {
        /// <summary>Latest engagement snapshot for <paramref name="handle"/>; false if none recorded.</summary>
        bool TryGetEngagementState(int handle, out SquadEngagementState state);

        /// <summary>Returns and clears the buffered phase-change events since the last drain.</summary>
        IReadOnlyList<EngagementTransition> DrainEngagementTransitions();
    }
}
