using System.Collections.Generic;
using FactionWars.Telemetry.Models;

namespace FactionWars.Telemetry.Interfaces
{
    /// <summary>
    /// Implemented by managers that own combat peds (followers, defenders, attackers). Exposes the
    /// handles + kind/role they already track so the behavior sampler stays decoupled from each
    /// manager's internals. No new ownership — just exposure.
    /// </summary>
    public interface ITrackedCombatantSource
    {
        IReadOnlyList<TrackedCombatant> GetTrackedCombatants();
    }
}
